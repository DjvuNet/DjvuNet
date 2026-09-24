using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DjvuNet.DjvuLibre;
using DjvuNet.Errors;
using DjvuNet.Serialization;
using Xunit;
using SysGraphics = System.Drawing.Graphics;
using SysBitmap = System.Drawing.Bitmap;

namespace DjvuNet.Tests
{
    [Flags]
    public enum JB2EncoderTestCoverage
    {
        Default = 1,
        AllZero = 2,
        Marks = 4,
        Shared = 8,
        AllVariants = AllZero | Marks | Shared,
        All = Default | AllVariants
    }

    [Flags]
    public enum TestCoverage
    {
        All,
        UniqueOnly,
        DjbzNotNull
    }

    public enum PixelSize : byte
    {
        _1bpp = 1,
        _2bpp = 2,
        _4bpp = 4,
        _8bpp = 8,
        _16bpp = 16,
        _24bpp = 24,
        _32bpp = 32,
        _48bpp = 48
    }

    public enum ChannelSize : byte
    {
        _1bit = 1,
        _2bit = 2,
        _4bit = 4,
        _8bit = 8,
        _10bit = 10,
        _12bit = 12,
        _14bit = 14,
        _16bit = 16
    }

    public static class FormatSizeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint AsUintBytes(this PixelSize size) => (uint)size / 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AsIntBytes(this PixelSize size) => (int)size / 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AsDoubleBits(this PixelSize size) => (double)size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AsIntBits(this ChannelSize size) => (int)size;
    }

    public static partial class Util
    {
        public const int DefaultCleanupTimeout = 500;
        private static string _ArtifactsPath;
        private static string _ArtifactsContentPath;
        private static string _ArtifactsDataPath;
        private static string _ArtifactsJsonPath;

        private static SortedDictionary<int, Tuple<int, int, DocumentType, string> > _TestDocumentData;

        public static SortedDictionary<int, Tuple<int, int, DocumentType, string>> TestDocumentData
        {
            get
            {
                if (_TestDocumentData != null)
                {
                    return _TestDocumentData;
                }
                else
                {
                    var dict = new SortedDictionary<int, Tuple<int, int, DocumentType, string>>();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

                    for (int i = 1; i <= 77; i++)
                    {
                        string filePath = GetTestFilePath(i);
                        filePath = Path.Combine(Util.ArtifactsJsonPath,
                            Path.GetFileNameWithoutExtension(filePath) + ".json");

                        string json = File.ReadAllText(filePath, new UTF8Encoding(false));

                        DjvuDoc doc = JsonSerializer.Deserialize<DjvuDoc>(json, options);

                        Tuple<int, int, DocumentType, string> docData;
                        if (doc.DjvuData is DjvmForm djvm)
                        {
                            var docType = (DocumentType) Enum.Parse(typeof(DocumentType), djvm.Dirm.DocumentType, true);

                            docData = Tuple.Create<int, int, DocumentType, string>(
                                djvm.Dirm.PageCount, djvm.Dirm.FileCount, docType, null);
                        }
                        else
                        {
                            var djvu = doc.DjvuData as DjvuForm;
                            docData = Tuple.Create<int, int, DocumentType, string>(
                                1, 1, DocumentType.SinglePage, null);
                        }
                        if (!dict.ContainsKey(i))
                            dict.Add(i, docData);
                    }

                    _TestDocumentData = dict;
                    return _TestDocumentData;
                }
            }
        }

        public static int GetTestDocumentPageCount(int index)
        {
            return TestDocumentData[index].Item1;
        }

        public static int GetTestDocumentFileCount(int index)
        {
            return TestDocumentData[index].Item2;
        }

        public static DocumentType GetTestDocumentType(int index)
        {
            return TestDocumentData[index].Item3;
        }

        public static string GetTestDocumentJsonDump(int index)
        {
            return TestDocumentData[index].Item4;
        }

        public static void FailOnException(Exception ex, string message, params object[] data)
        {
            string info = $"\nTest Failed -> Unexpected Exception: " +
                $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")}\n\n";

            if (data?.Length > 0)
            {
                info += (String.Format(message, data) + "\n" + ex.ToString());
            }
            else
            {
                info += (message + "\n" + ex.ToString());
            }

            Assert.Fail(info);
        }

        public static string GetTestFilePathTemplate()
        {
            char dirSep = Path.DirectorySeparatorChar;
            string filePathTempl = $"artifacts{dirSep}test{{0:00#}}C.djvu";
            string rootDir = Util.RepoRoot;
            return Path.Combine(rootDir, filePathTempl);
        }

        public static string GetTestFilePath(int index)
        {
            string filePathTempl = GetTestFilePathTemplate();
            string filePath = String.Format(filePathTempl, index);
            return filePath;
        }

        public static byte[] ReadFileToEnd(string bzzFile)
        {
            using (FileStream stream = File.OpenRead(Path.Combine(Util.RepoRoot, bzzFile)))
            {
                byte[] buffer = new byte[stream.Length];
                int countRead = stream.Read(buffer, 0, buffer.Length);
                if (countRead != buffer.Length)
                    throw new IOException($"Unable to read file with test data: {bzzFile}");
                return buffer;
            }
        }

        public static string ArtifactsPath
        {
            get
            {
                if (_ArtifactsPath != null)
                {
                    return _ArtifactsPath;
                }
                else
                {
                    _ArtifactsPath = Path.Combine(Util.RepoRoot, "artifacts");
                    return _ArtifactsPath;
                }
            }
        }

        // Pre-compile at class level for performance:
        private static readonly Regex ArchiveSignatureRegex = new Regex(
            @"-\d+\.\d+\.\d+(?:\.\d+)?(?:-[a-zA-Z0-9\-\.]+)?_\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}-(report|asm)$",
            RegexOptions.Compiled);

        /// <summary>
        /// Determines if a file has already been processed and archived by the benchmark reporting engine.
        /// </summary>
        /// <remarks>
        /// The goal of this function is Archival Integrity. Historical benchmark data must remain
        /// untouched as an immutable record of past optimizations.
        /// By detecting the complete, unique signature of the DjvuNet archival engine—which consists of
        /// the dynamic repository version string (Major.Minor.yyDOY.Order[-hash[-dev]]), the execution timestamp,
        /// and the final "-report" or "-asm" suffix—the engine can safely skip
        /// this file, preventing data falsification or "filename snowballing".
        /// </remarks>
        /// <param name="fileNameWithoutExtension">The name of the file to check, without its extension.</param>
        /// <returns>True if the file contains the full archival signature; otherwise, False.</returns>
        public static bool IsArchivedBenchmarkFile(string fileNameWithoutExtension)
        {
            return ArchiveSignatureRegex.IsMatch(fileNameWithoutExtension);
        }

        /// <summary>
        /// Generates a new archival filename by appending the version, timestamp, and correct report suffix.
        /// </summary>
        /// <remarks>
        /// The goal is to safely track optimization progress over time by injecting the repository state
        /// and execution time into the filename. To ensure a consistent signature, we strip the native
        /// BenchmarkDotNet suffixes (like -report-github) and enforce our own "-report" or "-asm" suffix at the end.
        /// </remarks>
        /// <param name="originalNameWithoutExtension">The original file name generated by BDN, without the extension.</param>
        /// <param name="suffix">The archival suffix containing the repo version and run timestamp (e.g. "-0.10.26146.2-d5580f5-dev_2026-05-29_20-43-00").</param>
        /// <returns>The fully formatted archival filename without extension.</returns>
        public static string GetArchiveFileName(string originalNameWithoutExtension, string suffix)
        {
            string name = originalNameWithoutExtension;
            bool isAsm = name.EndsWith("-asm");

            // Strip any native BDN suffix starting with -report (e.g., -report, -report-default, -report-github)
            name = Regex.Replace(name, @"-report(-[a-zA-Z0-9\-]+)?$", "");

            // Strip -asm if it's there
            if (isAsm) name = name.Substring(0, name.Length - 4);

            if (isAsm)
                return name + suffix + "-asm";
            else
                return name + suffix + "-report";
        }

        public static string ArtifactsContent
        {
            get
            {
                if (_ArtifactsContentPath != null)
                {
                    return _ArtifactsContentPath;
                }
                else
                {
                    _ArtifactsContentPath = Path.Combine(ArtifactsPath, "content");
                    return _ArtifactsContentPath;
                }
            }
        }

        public static string ArtifactsDataPath
        {
            get
            {
                if (_ArtifactsDataPath != null)
                {
                    return _ArtifactsDataPath;
                }
                else
                {
                    _ArtifactsDataPath = Path.Combine(ArtifactsPath, "data");
                    return _ArtifactsDataPath;
                }
            }
        }

        public static string ArtifactsJsonPath
        {
            get
            {
                if (_ArtifactsJsonPath != null)
                {
                    return _ArtifactsJsonPath;
                }
                else
                {
                    _ArtifactsJsonPath = Path.Combine(ArtifactsPath, "json");
                    return _ArtifactsJsonPath;
                }
            }
        }

        /// <summary>
        /// Calculates the optimal number of threads for parallel execution of the ImageBinaryDiff operation.
        /// Returns 1 to indicate the caller should bypass the TPL and fall back to single-threaded logic
        /// (either Scalar SWAR or SIMD vectors based on payload size).
        /// </summary>
        /// <remarks>
        /// ARCHITECTURAL RATIONALE:
        /// This step-function matrix is derived from extensive benchmarking of the TPL (Task Parallel Library) overhead
        /// versus branchless scalar and SIMD throughput. Below is the empirical data demonstrating the exact threshold crossings
        /// where scaling threads becomes profitable, measured in Time per Operation (Real).
        ///
        /// | Image Size | Scalar (1T) | V128 (1T) | V256 (1T) | P128 (2T) | P128 (4T) | P128 (6T) | P256 (2T) | P256 (4T) | P256 (6T) |
        /// |-----------:|------------:|----------:|----------:|----------:|----------:|----------:|----------:|----------:|----------:|
        /// |      1,024 |     1.48 µs | 307.63 ns | 257.33 ns |   1.81 µs |   2.61 µs |   2.84 µs |   1.62 µs |   2.26 µs |   2.44 µs |
        /// |      4,096 |     5.81 µs |   1.16 µs |   1.01 µs |   3.04 µs |   4.16 µs |   4.89 µs |   2.82 µs |   3.57 µs |   4.20 µs |
        /// |      9,216 |    12.92 µs |   2.52 µs |   2.27 µs |   4.33 µs |   5.96 µs |   6.43 µs |   4.09 µs |   5.44 µs |   5.74 µs |
        /// |     16,384 |    22.81 µs |   4.74 µs |   4.20 µs |   5.97 µs |   7.08 µs |   8.54 µs |   5.96 µs |   6.68 µs |   8.10 µs |
        /// |     36,864 |    51.65 µs |  10.86 µs |   9.44 µs |   9.83 µs |  10.40 µs |  11.47 µs | *9.30 µs* |   9.98 µs |  11.41 µs |
        /// |     65,536 |    93.38 µs |  18.58 µs |  16.28 µs |  15.97 µs | *15.76 µs*|  16.93 µs | *15.15 µs*|  15.40 µs |  16.85 µs |
        /// |    262,144 |   368.95 µs |  71.36 µs |  63.92 µs |  53.23 µs |  54.78 µs | *53.05 µs*| *50.21 µs*|  51.34 µs |  51.94 µs |
        /// |  1,048,576 |  1475.20 µs | 298.98 µs | 267.57 µs | 194.36 µs | *191.18 µs*| 199.30 µs| *189.10 µs*| 193.02 µs| 201.82 µs |
        /// |  2,096,704 |  2907.10 µs | 612.94 µs | 525.37 µs | 391.38 µs | *387.39 µs*| 399.87 µs| *377.31 µs*| 379.46 µs| 398.25 µs |
        /// |  4,194,304 |  5788.80 µs |1180.90 µs |1067.40 µs | 779.52 µs | *772.18 µs*| 801.53 µs| *749.09 µs*| 771.75 µs| 786.01 µs |
        /// | 20,081,328 | 28.1979 ms  | 5.7602 ms | 1.0482 ms | *3.6345 ms*| 3.6394 ms | 3.7612 ms | *3.5494 ms*| 3.6197 ms | 3.7942 ms |
        ///
        /// 1. Vector256 (AVX2) Saturation: The AVX2 absolute difference loop is highly efficient but completely saturates
        ///    the dual-channel memory bus at exactly 2 threads. Scaling to 4 or 6 threads consistently yields slower execution times
        ///    (e.g. 20M pixels: 2T = 3.54ms vs 4T = 3.61ms vs 6T = 3.79ms). Therefore, AVX2 is strictly capped at 2 threads.
        ///
        /// 2. Vector128 (SSSE3/AdvSimd) Saturation: Because Vector128 ingests data half as fast as AVX2, the memory bus
        ///    saturation point is pushed slightly higher. For payloads over 1M pixels, 4 threads consistently outperforms
        ///    2 threads (e.g. 4M pixels: 2T = 779.52us vs 4T = 772.18us). Therefore, Vector128 is capped at 4 threads.
        ///
        /// 3. Amortization: The TPL overhead is too heavy for small payloads. Single-threaded SIMD execution is universally
        ///    faster until payloads exceed ~36,000 pixels, where parallel AVX2 (2T) becomes profitable. For Vector128,
        ///    amortization requires a slightly higher payload (~65,000 pixels) to overcome the TPL overhead.
        ///
        /// MEMORY TOPOLOGY DISCLAIMER:
        /// These scaling metrics are calibrated against standard dual-channel (2-channel) memory configurations typical
        /// of consumer hardware (e.g. Ryzen 3600). We did not benchmark quad-channel or octa-channel configurations.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetThreadCountForImageBinaryDiff(long pixelCount)
        {
            int targetThreads = 1;
            int maxAvailableThreads = Environment.ProcessorCount;

            if (Avx2.IsSupported)
            {
                // AVX2 Memory Bus Saturation Matrix
                // Amortization crosses at 36,864 pixels. Memory completely saturates at 2 threads.
                if (pixelCount >= 36_000)
                    targetThreads = 2;
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                // Vector128 Memory Bus Saturation Matrix
                // Amortization crosses at 65,536 pixels. Slower compute saturates memory at 4 threads.
                if (pixelCount >= 1_000_000)
                    targetThreads = 4;
                else if (pixelCount >= 65_000)
                    targetThreads = 2;
            }

            return Math.Min(targetThreads, maxAvailableThreads);
        }

        public static void AssertBufferEqal(byte[] buffer, byte[] refBuffer)
        {
            Assert.True(refBuffer.Length == buffer.Length,
                $"Output length mismatch. Expected: {refBuffer.Length}, actual: {buffer.Length}");

            unsafe
            {
                fixed(byte* buf = buffer, refbuf = refBuffer)
                {
                    int* pbuf = (int*) buf;
                    int* pref = (int*)refbuf;

                    int* end = pbuf + refBuffer.Length;

                    int div = refBuffer.Length / sizeof(int);

                    for (int i = 0; i < div; i++)
                    {
                        if (*pbuf++ != *pref++)
                        {
                            Assert.True(false);
                        }
                    }

                    int rem = refBuffer.Length % sizeof(int);

                    if (rem > 0)
                    {
                        byte* bbuf = buf;
                        byte* bref = refbuf;
                        for (int i = 0; i < rem; i++)
                            Assert.Equal(*bbuf++, *bref++);
                    }
                }
            }
        }

        public static List<string> TestUnicodeStrings
        {
            get
            {
                List<string> retVal = new List<string>(new string[]
                {
                    "免去于革胜的全国社会保障基金理事会副理事长",
                    "재정난이 심해져 조직 내 구조조정과 임금 삭감이",
                    "وتهدف العملية إلى حماية المدنيين ومنع تحركات الحوثيين وقوات الرئيس المخلوع علي عبد الله صالح، وتوسيع وتوطيد التعاون",
                    "กรมศิลปากรได้พิจารณาแล้วเห็นว่า ตาม พ.ร.บ.โบราณสถาน โบราณวัตถุ ศิลปวัตถุ และพิพิธภัณฑสถานแห่งชาติ พ.ศ.2504 แก้ไขเพิ่มเติม",
                    "След като месеци наред отричаше да има подобно намерение, сега Мей сподели",
                    "Die Premierministerin Großbritanniens erhofft sich von Neuwahlen ein stärkeres Mandat für die Verhandlungen mit Brüssel",
                    "Παρά τις προβλέψεις για άμεσο οικονομικό κίνδυνο, μετά το δημοψήφισμα του περασμένου καλοκαιριού είδαμε ότι η εμπιστοσύνη",
                    "על-פי הערכות, נתניהו לא עודכן על קיום מסיבת העיתונאים של כחלון וגם לא על תוכן התוכנית שהוצגה",
                    "ホテル近くに横浜市の新市庁舎が移転することから「外国人観光客の増加も見込まれるが",
                    "В ходы войны город Идлиб переходил из рук в руки, но в итоге остался под контролем оппозиционеров",
                    "बयान में कहा गया, ‘एनएसए मैकमास्टर ने भारत-अमेरिका के सामरिक रिश्तों पर जोर दिया और भारत के एक",
                    "Mağazalarla ek 300 kişiye istihdam sağlayacaklarının altını çizen Serbes, dolaylı olarak da 1000 kişiye iş yaratılacağını belirtti",
                    "Cả hai đội đều có những thay đổi về đội hình ra sân. Bale vắng mặt nên Isco được đá chính trên hàng công",
                    "Nie wszystko dało się przewidzieć, stąd drobne opóźnienie – tłumaczy Sylwester Puczen, rzecznik Toru Służewiec",
                    "Guðlaugur Þór Þórðarson utanríkisráðherra átti í dag fund með Boris Johnson, utanríkisráðherra Bretlands í Lundúnum þar sem þeir ræddu útgöngu Breta úr Evrópusambandinu og leiðir til að efla samskipti Íslands og Bretlands",
                    "Konservatiivipuolueen kannattajilleen ja toimittajille lähettämässä kirjeessäkin puhutaan pelitermein \"vahvemmasta kädestä\" eli pääministerille halutaan paremmat kortit käteen kun hän lähtee EU",
                    "Det er mye som lykkes for Ap-leder Jonas Gahr Støre. På borgerlig side er samarbeidet gått surt, og kaoset truer. Meningsmålingene har gitt Ap",
                    "Em carta divulgada na segunda-feira (17), o ex-presidente da Câmara Eduardo Cunha rebatou as afirmações do presidente",
                });

                return retVal;
            }
        }

        private static bool IsImageBinaryComparable(SysBitmap image1, SysBitmap image2, out bool pixelFormatMismatch)
        {
            bool result = true;
            pixelFormatMismatch = false;

            if (image1 == null || image2 == null)
            {
                result = false;
            }
            else if (image1.PixelFormat != image2.PixelFormat || image1.PixelFormat != PixelFormat.Format24bppRgb)
            {
                pixelFormatMismatch = true;
            }
            else if (image1.Width != image2.Width || image1.Height != image2.Height)
            {
                result = false;
            }

            return result;
        }

        private static bool IsImageBinaryComparable(BitmapData image1, BitmapData image2)
        {
            bool result = true;

            if (image1 == null || image2 == null)
            {
                result = false;
            }
            else if (image1.PixelFormat != image2.PixelFormat)
            {
                result = false;
            }
            else if (image1.Width != image2.Width || image1.Height != image2.Height)
            {
                result = false;
            }

            return result;
        }

        public static unsafe bool ImageBinarySimilarity(SysBitmap oracle, Graphics.PixelMap map, double diffThreshold = 0.0, bool logDiff = false, string message = null)
        {
            if (oracle.Width != map.Width || oracle.Height != map.Height)
            {
                return false;
            }

            Rectangle rect = new Rectangle(0, 0, oracle.Width, oracle.Height);
            BitmapData data = oracle.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            
            try
            {
                int rowSize = map.Width * 3;
                fixed (sbyte* pMap = map.Data)
                {
                    // Oracle is Top-Down, Map is Bottom-Up. We pass negative stride to SIMD diff.
                    byte* pOracleStart = (byte*)data.Scan0 + ImageMemoryOffset(map.Height - 1, data.Stride);
                    double diff = ImageBinaryDiff(
                        pOracleStart, 
                        (byte*)pMap, 
                        map.Width, 
                        map.Height, 
                        -data.Stride, 
                        rowSize, 
                        Tests.PixelSize._24bpp, 
                        Tests.ChannelSize._8bit);
                        
                    if (logDiff)
                    {
                        Console.WriteLine((message ?? "") + $" Image diff: {diff:#0.000000}, passed: {diff <= diffThreshold}");
                    }
                        
                    return diff <= diffThreshold;
                }
            }
            finally
            {
                oracle.UnlockBits(data);
            }
        }

        public static unsafe bool ImageBinarySimilarity(SysBitmap oracle, ref Graphics.Bitmap map, double diffThreshold = 0.0, bool logDiff = false, string message = null)
        {
            if (oracle == null || map.Data == null || oracle.Width != map.Width || oracle.Height != map.Height) return false;

            Rectangle rect = new Rectangle(0, 0, oracle.Width, oracle.Height);
            BitmapData data = oracle.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            
            try
            {
                byte* pOracleStart = (byte*)data.Scan0 + ImageMemoryOffset(map.Height - 1, data.Stride);
                byte* pMap = (byte*)map.GetRow(0);
                
                double diff = ImageBinaryDiff(
                    pOracleStart, 
                    pMap, 
                    map.Width, 
                    map.Height, 
                    -data.Stride, 
                    map.BytesPerRow, 
                    Tests.PixelSize._8bpp, 
                    Tests.ChannelSize._8bit);
                    
                if (logDiff) Console.WriteLine((message ?? "") + $" Image diff: {diff:#0.000000}, passed: {diff <= diffThreshold}");
                return diff <= diffThreshold;
            }
            finally
            {
                oracle.UnlockBits(data);
            }
        }

        public static unsafe ValueTuple<bool, double> ImageBinarySimilarity(ref Graphics.Bitmap oracle, ref Graphics.Bitmap map, double diffThreshold = 0.0, bool logDiff = false, string message = null)
        {
            if (oracle.Data == null || map.Data == null || oracle.Width != map.Width || oracle.Height != map.Height)
                return (false, double.NaN);
            
            double diff = ImageBinaryDiff(
                (byte*)oracle.GetRow(0), (byte*)map.GetRow(0), 
                map.Width, map.Height, 
                oracle.BytesPerRow, map.BytesPerRow, 
                Tests.PixelSize._8bpp, Tests.ChannelSize._8bit);
                    
            if (logDiff)
                Console.WriteLine((message ?? "") + $" Image diff: {diff:#0.000000}, passed: {diff <= diffThreshold}");

            return (diff <= diffThreshold, diff); 
        }

        public static bool ImageBinarySimilarity(SysBitmap image1, SysBitmap image2, double diffThreshold = 0.0, bool logDiff = false, string message = null)
        {
            double diff;
            bool result = ImageBinarySimilarity(image1, image2, out diff, diffThreshold);

            if (logDiff)
            {
                Console.WriteLine((message != null ? message : "") + $" Image diff: {diff:#0.000000}, passed: {result}");
            }

            return result;
        }

        public static bool ImageBinarySimilarity(SysBitmap image1, SysBitmap image2, out double diffValue, double diffThreshold = 0.0)
        {
            bool formatMismatch;
            bool result = IsImageBinaryComparable(image1, image2, out formatMismatch);

            diffValue = double.NaN;
            SysBitmap bmp1 = null;
            SysBitmap bmp2 = null;

            try
            {

                if (result && formatMismatch)
                {
                    if (image1.PixelFormat != PixelFormat.Format24bppRgb)
                    {
                        bmp1 = new SysBitmap(image1.Width, image1.Height, PixelFormat.Format24bppRgb);
                        using SysGraphics gfx = SysGraphics.FromImage(bmp1);
                        gfx.DrawImage(image1, new Rectangle(0, 0, image1.Width, image1.Height));
                    }

                    if (image2.PixelFormat != PixelFormat.Format24bppRgb)
                    {
                        bmp2 = new SysBitmap(image2.Width, image2.Height, PixelFormat.Format24bppRgb);
                        using SysGraphics gfx = SysGraphics.FromImage(bmp2);
                        gfx.DrawImage(image2, new Rectangle(0, 0, image2.Width, image2.Height));
                    }
                }

                if (result)
                {
                    Rectangle rect = new Rectangle(0, 0, image1.Width, image1.Height);
                    BitmapData img1 = bmp1?.LockBits(rect, ImageLockMode.ReadOnly, bmp1.PixelFormat) ?? image1.LockBits(rect, ImageLockMode.ReadOnly, image1.PixelFormat);
                    BitmapData img2 = bmp2?.LockBits(rect, ImageLockMode.ReadOnly, bmp2.PixelFormat) ?? image2.LockBits(rect, ImageLockMode.ReadOnly, image2.PixelFormat);

                    result = (diffValue = ImageBinarySimilarity(img1, img2)) <= diffThreshold;

                    if (bmp1 != null)
                    {
                        bmp1?.UnlockBits(img1);
                    }
                    else
                    {
                        image1.UnlockBits(img1);
                    }

                    if (bmp2 != null)
                    {
                        bmp2.UnlockBits(img2);
                    }
                    else
                    {
                        image2.UnlockBits(img2);
                    }
                }
            }
            finally
            {
                if (bmp1 != null)
                {
                    bmp1.Dispose();
                }

                if (bmp2 != null)
                {
                    bmp2.Dispose();
                }
            }

            return result;
        }

        /// <summary>
        /// Calculate average pixel binary diff between images
        /// </summary>
        /// <param name="imageData1"></param>
        /// <param name="imageData2"></param>
        /// <returns></returns>
        public static double ImageBinarySimilarity(BitmapData imageData1, BitmapData imageData2)
        {
            if (IsImageBinaryComparable(imageData1, imageData2))
            {
                return imageData1.PixelFormat switch
                {
                    PixelFormat.Format32bppArgb => ImageBinaryDiff(imageData1, imageData2, PixelSize._32bpp),
                    PixelFormat.Format24bppRgb => ImageBinaryDiff(imageData1, imageData2),
                    PixelFormat.Format8bppIndexed => ImageBinaryDiff(imageData1, imageData2, PixelSize._8bpp),
                    PixelFormat.Format16bppGrayScale => ImageBinaryDiff(imageData1, imageData2, PixelSize._16bpp, ChannelSize._16bit),
                    _ => throw new ArgumentException("Unsupported Image PixelFormat", nameof(imageData1.PixelFormat))
                };
            }
            else
            {
                return 1.0;
            }
        }

        /// <summary>
        /// Calculates the average absolute difference per channel per pixel across the whole image using raw pointers.
        /// It is the caller's responsibility to ensure that both image buffers share the exact same stride layout.
        /// </summary>
        /// <param name="ptr1">Pointer to the first image buffer.</param>
        /// <param name="ptr2">Pointer to the second image buffer.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <param name="stride1">The row stride of the first image (in bytes). This parameter serves a dual function: 1) its magnitude accounts for row memory padding, and 2) its sign accounts for the direction of processing (a negative reverse stride allows comparing images existing in different coordinate spaces, e.g., top-down vs bottom-up). Both functions combine naturally.</param>
        /// <param name="stride2">The row stride of the second image (in bytes). It shares the exact same dual functionality as stride1, allowing fully independent memory layout and coordinate space comparisons. If 0, it falls back to stride1.</param>
        /// <param name="pixelSize">The size of a single pixel in bits (e.g., 24 for 24bpp RGB, 32 for ARGB).</param>
        /// <param name="channelSize">The size of a single color channel in bits (e.g., 8 for standard RGB channels).</param>
        /// <returns>A ratio between 0.0 (identical) and 1.0 (completely opposite) representing the average pixel difference.</returns>
        internal static unsafe double ImageBinaryDiff(byte* ptr1, byte* ptr2, int width, int height, int stride1, int stride2 = 0, PixelSize pixelSize = PixelSize._24bpp, ChannelSize channelSize = ChannelSize._8bit)
        {
            if (ptr1 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(ptr1), "First image buffer pointer cannot be null.");
            }

            if (ptr2 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(ptr2), "Second image buffer pointer cannot be null.");
            }

            if (width <= 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            }

            if (height <= 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            }

            if ((byte)pixelSize == 0 || (byte)pixelSize % 8 != 0)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            }

            if (channelSize != ChannelSize._8bit)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));
            }
            
            int actualStride2 = stride2 == 0 ? stride1 : stride2;

            uint widthBytes = (uint)width * pixelSize.AsUintBytes();
            if ((ulong)Math.Abs((long)stride1) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride1 must be large enough to contain the width bytes.", nameof(stride1));
            }

            if ((ulong)Math.Abs((long)actualStride2) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride2 must be large enough to contain the width bytes.", nameof(actualStride2));
            }

            int threadCount = GetThreadCountForImageBinaryDiff((long)width * height);

            if (threadCount == 1)
            {
                if (Avx2.IsSupported)
                {
                    if (widthBytes >= 32)
                    {
                        return ImageDiffVector256(ptr1, ptr2, (uint)width, (uint)height, stride1, actualStride2, pixelSize, channelSize);
                    }
                    else if (widthBytes >= 16)
                    {
                        return ImageDiffVector128(ptr1, ptr2, (uint)width, (uint)height, stride1, actualStride2, pixelSize, channelSize);
                    }
                    else
                    {
                        return ImageBinaryDiffScalar(ptr1, ptr2, (uint)width, (uint)height, stride1, actualStride2, pixelSize, channelSize);
                    }
                }
                else if (Vector128.IsHardwareAccelerated)
                {
                    if (widthBytes >= 16)
                    {
                        return ImageDiffVector128(ptr1, ptr2, (uint)width, (uint)height, stride1, actualStride2, pixelSize, channelSize);
                    }
                    else
                    {
                        return ImageBinaryDiffScalar(ptr1, ptr2, (uint)width, (uint)height, stride1, actualStride2, pixelSize, channelSize);
                    }
                }
                else
                {
                    return ImageBinaryDiffScalar(ptr1, ptr2, (uint)width, (uint)height, stride1, actualStride2, pixelSize, channelSize);
                }
            }
            else
            {
                ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = threadCount };
                if (Avx2.IsSupported)
                {
                    if (widthBytes >= 32)
                    {
                        return ImageDiffParallel256(ptr1, ptr2, (uint)width, (uint)height, stride1, actualStride2, options, pixelSize, channelSize);
                    }
                    else if (widthBytes >= 16)
                    {
                        return ImageDiffParallel128(ptr1, ptr2, (uint)width, (uint)height, stride1, actualStride2, options, pixelSize, channelSize);
                    }
                    else
                    {
                        return ImageBinaryDiffScalar(ptr1, ptr2, (uint)width, (uint)height, stride1, actualStride2, pixelSize, channelSize);
                    }
                }
                else if (Vector128.IsHardwareAccelerated)
                {
                    if (widthBytes >= 16)
                    {
                        return ImageDiffParallel128(ptr1, ptr2, (uint)width, (uint)height, stride1, actualStride2, options, pixelSize, channelSize);
                    }
                    else
                    {
                        return ImageBinaryDiffScalar(ptr1, ptr2, (uint)width, (uint)height, stride1, actualStride2, pixelSize, channelSize);
                    }
                }
                else
                {
                    return ImageBinaryDiffScalar(ptr1, ptr2, (uint)width, (uint)height, stride1, actualStride2, pixelSize, channelSize);
                }
            }
        }

        /// <summary>
        /// Calculates the average absolute difference per channel per pixel across the whole image using BitmapData.
        /// </summary>
        /// <param name="imageData1">The first image data object.</param>
        /// <param name="imageData2">The second image data object.</param>
        /// <param name="pixelSize">The size of a single pixel in bits (e.g., 24 for 24bpp RGB, 32 for ARGB).</param>
        /// <param name="channelSize">The size of a single color channel in bits (e.g., 8 for standard RGB channels).</param>
        /// <returns>A ratio between 0.0 (identical) and 1.0 (completely opposite) representing the average pixel difference.</returns>
        internal static unsafe double ImageBinaryDiff(BitmapData imageData1, BitmapData imageData2, PixelSize pixelSize = PixelSize._24bpp, ChannelSize channelSize = ChannelSize._8bit)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && (imageData1.Stride < 0 || imageData2.Stride < 0))
            {
                DjvuExceptionUtil.ThrowNotSupported("Negative stride memory parity checking is not supported on non-Windows OSes due to upstream libgdiplus bounds bugs.");
            }

            if (imageData1 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(imageData1), "First image data cannot be null.");
            }

            if (imageData2 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(imageData2), "Second image data cannot be null.");
            }

            if ((byte)pixelSize == 0 || (byte)pixelSize % 8 != 0)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            }

            if (channelSize != ChannelSize._8bit)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));
            }

            return ImageBinaryDiff((byte*)imageData1.Scan0, (byte*)imageData2.Scan0, imageData1.Width, imageData1.Height, imageData1.Stride, imageData2.Stride, pixelSize, channelSize);
        }

        /// <summary>
        /// Calculates the aggregate average absolute difference per pixel across an entire image using SIMD hardware acceleration.
        /// </summary>
        /// <param name="scan0_1">Pointer to the start of the first image buffer in memory.</param>
        /// <param name="scan0_2">Pointer to the start of the second image buffer in memory.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <param name="stride">The row stride in bytes, including any memory alignment padding.</param>
        /// <param name="pixelSize">The total size of a single pixel in bits (e.g., 24 for RGB).</param>
        /// <param name="channelSize">The size of a single color channel in bits (e.g., 8 for standard channels).</param>
        /// <returns>A <see cref="double"/> representing the average pixel difference across the image channels.</returns>
        /// <remarks>
        /// <para>
        /// It is strictly the caller's responsibility to ensure that both pointers reference buffers allocated with the
        /// exact same stride layout. The method natively calculates the visible byte width and uses linear byte processing
        /// to ensure memory stride padding is never read.
        /// </para>
        /// <para>
        /// When an image row's visible width is not perfectly aligned to a hardware vector boundary (32 bytes for AVX2,
        /// 16 bytes for Vector128), this method implements a tail-shift strategy to process the remainder bytes. The
        /// pointer for the final vector load is shifted backwards by a calculated offset, guaranteeing the load ends
        /// exactly on the last valid byte of the row without over-reading into the uninitialized padding.
        /// </para>
        /// <para>
        /// To prevent overlapping bytes (which were already processed in the previous SIMD iteration) from being double-counted
        /// in the accumulation, a dynamic bitmask is generated via a sequence comparison. This mask zeroes out the overlapping
        /// bytes using a bitwise AND operation, zerping them before the Sum of Absolute Differences (SAD) calculation occurs.
        /// </para>
        /// <para>
        /// The Vector128 fallback path optimizes pipeline throughput by accumulating differences into smaller lanes for up
        /// to 255 inner-loop iterations before flushing to larger row accumulators. This prevents accumulation overflow on
        /// large images while avoiding the latency of horizontal widening instructions in the hot path.
        /// </para>
        /// </remarks>
        internal static unsafe double ImageBinaryDiffCore(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride, PixelSize pixelSize, ChannelSize channelSize)
        {
            return ImageBinaryDiffCore(scan0_1, scan0_2, width, height, stride, stride, pixelSize, channelSize);
        }

        /// <summary>
        /// Calculates the aggregate average absolute difference per pixel across an entire image using SIMD hardware acceleration.
        /// </summary>
        /// <param name="scan0_1">Pointer to the start of the first image buffer in memory.</param>
        /// <param name="scan0_2">Pointer to the start of the second image buffer in memory.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <param name="stride1">The row stride of the first image (in bytes). This parameter serves a dual function: 1) its magnitude accounts for row memory padding, and 2) its sign accounts for the direction of processing (a negative reverse stride allows comparing images existing in different coordinate spaces, e.g., top-down vs bottom-up). Both functions combine naturally.</param>
        /// <param name="stride2">The row stride of the second image (in bytes). It shares the exact same dual functionality as stride1, allowing fully independent memory layout and coordinate space comparisons. If 0, it falls back to stride1.</param>
        /// <param name="pixelSize">The total size of a single pixel in bits (e.g., 24 for RGB).</param>
        /// <param name="channelSize">The size of a single color channel in bits (e.g., 8 for standard channels).</param>
        /// <returns>A <see cref="double"/> representing the average pixel difference across the image channels.</returns>
        internal static unsafe double ImageBinaryDiffCore(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride1, int stride2, PixelSize pixelSize, ChannelSize channelSize)
        {
            if (scan0_1 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_1), "First image buffer pointer cannot be null.");
            }

            if (scan0_2 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_2), "Second image buffer pointer cannot be null.");
            }

            if (width == 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            }

            if (height == 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            }

            if ((byte)pixelSize == 0 || (byte)pixelSize % 8 != 0)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            }

            if (channelSize != ChannelSize._8bit)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));
            }

            ulong widthBytes = (ulong)width * pixelSize.AsUintBytes();
            if ((ulong)Math.Abs((long)stride1) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride1 must be large enough to contain the width bytes.", nameof(stride1));
            }

            if ((ulong)Math.Abs((long)stride2) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride2 must be large enough to contain the width bytes.", nameof(stride2));
            }

            uint pixelSizeInBytes = (uint)pixelSize / 8;
            double result = 0.0;

        #if NETCOREAPP
            if (Avx2.IsSupported && widthBytes >= 32)
            {
                ulong vectorBound = widthBytes >= 32 ? widthBytes - 32 : 0;
                int tailShift = (int)((32 - (widthBytes % 32)) % 32);

                Vector256<ulong> resultVecU = Vector256<ulong>.Zero;

                Vector256<sbyte> seq256 = Vector256.Create(
                    (sbyte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                    16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31);
                Vector256<sbyte> threshold = Vector256.Create((sbyte)(tailShift - 1));
                Vector256<byte> tailMask = Avx2.CompareGreaterThan(seq256, threshold).AsByte();

                for (uint i = 0; i < height; i++)
                {
                    byte* p1 = scan0_1 + ((long)i * stride1);
                    byte* p2 = scan0_2 + ((long)i * stride2);
                    ulong x = 0;

                    while (x + 96 <= vectorBound)
                    {
                        Vector256<byte> r11 = Avx2.LoadDquVector256(p1);
                        Vector256<byte> r21 = Avx2.LoadDquVector256(p2);
                        Vector256<byte> r12 = Avx2.LoadDquVector256(p1 + 32);
                        Vector256<byte> r22 = Avx2.LoadDquVector256(p2 + 32);
                        Vector256<ushort> diff1 = Avx2.SumAbsoluteDifferences(r11, r21);
                        Vector256<ushort> diff2 = Avx2.SumAbsoluteDifferences(r12, r22);

                        Vector256<byte> r13 = Avx2.LoadDquVector256(p1 + 64);
                        Vector256<byte> r23 = Avx2.LoadDquVector256(p2 + 64);
                        Vector256<byte> r14 = Avx2.LoadDquVector256(p1 + 96);
                        Vector256<byte> r24 = Avx2.LoadDquVector256(p2 + 96);
                        Vector256<ushort> diff3 = Avx2.SumAbsoluteDifferences(r13, r23);
                        Vector256<ushort> diff4 = Avx2.SumAbsoluteDifferences(r14, r24);

                        Vector256<ulong> diff12 = Avx2.Add(diff1.AsUInt64(), diff2.AsUInt64());
                        Vector256<ulong> diff34 = Avx2.Add(diff3.AsUInt64(), diff4.AsUInt64());
                        resultVecU = Avx2.Add(resultVecU, Avx2.Add(diff12, diff34));

                        p1 += 128;
                        p2 += 128;
                        x += 128;
                    }

                    while (x <= vectorBound)
                    {
                        Vector256<byte> r1 = Avx2.LoadDquVector256(p1);
                        Vector256<byte> r2 = Avx2.LoadDquVector256(p2);
                        Vector256<ushort> diff = Avx2.SumAbsoluteDifferences(r1, r2);

                        resultVecU = Avx2.Add(resultVecU, diff.AsUInt64());

                        p1 += 32;
                        p2 += 32;
                        x += 32;
                    }

                    if (x < widthBytes)
                    {
                        Vector256<byte> r1 = Avx2.LoadDquVector256(p1 - tailShift);
                        Vector256<byte> r2 = Avx2.LoadDquVector256(p2 - tailShift);

                        r1 = Avx2.And(r1, tailMask);
                        r2 = Avx2.And(r2, tailMask);

                        Vector256<ushort> diff = Avx2.SumAbsoluteDifferences(r1, r2);
                        resultVecU = Avx2.Add(resultVecU, diff.AsUInt64());
                    }
                }

                result += Vector256.Sum(resultVecU);
            }
            else if (Vector128.IsHardwareAccelerated && widthBytes >= 16)
            {
                ulong vectorBound = widthBytes >= 16 ? widthBytes - 16 : 0;
                int tailShift = (int)((16 - (widthBytes % 16)) % 16);

                Vector128<ulong> imageAccum64L = Vector128<ulong>.Zero;
                Vector128<ulong> imageAccum64H = Vector128<ulong>.Zero;

                Vector128<sbyte> seq128 = Vector128.Create((sbyte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
                Vector128<sbyte> threshold = Vector128.Create((sbyte)(tailShift - 1));
                Vector128<byte> tailMask = Vector128.GreaterThan(seq128, threshold).AsByte();

                for (uint i = 0; i < height; i++)
                {
                    byte* p1 = scan0_1 + ((long)i * stride1);
                    byte* p2 = scan0_2 + ((long)i * stride2);
                    ulong x = 0;

                    while (x <= vectorBound)
                    {
                        ulong batchLimit = Math.Min(x + (255 * 16), vectorBound + 16);
                        if (batchLimit > vectorBound) batchLimit = vectorBound + 1; // Ensure we only run up to vectorBound

                        Vector128<ushort> batchAccum16L = Vector128<ushort>.Zero;
                        Vector128<ushort> batchAccum16H = Vector128<ushort>.Zero;

                        while (x < batchLimit)
                        {
                            Vector128<byte> r1 = Vector128.Load(p1);
                            Vector128<byte> r2 = Vector128.Load(p2);

                            Vector128<byte> diff = Vector128.Subtract(Vector128.Max(r1, r2), Vector128.Min(r1, r2));

                            batchAccum16L = Vector128.Add(batchAccum16L, Vector128.WidenLower(diff));
                            batchAccum16H = Vector128.Add(batchAccum16H, Vector128.WidenUpper(diff));

                            p1 += 16;
                            p2 += 16;
                            x += 16;
                        }

                        Vector128<uint> batch32L = Vector128.Add(Vector128.WidenLower(batchAccum16L), Vector128.WidenUpper(batchAccum16L));
                        Vector128<uint> batch32H = Vector128.Add(Vector128.WidenLower(batchAccum16H), Vector128.WidenUpper(batchAccum16H));

                        imageAccum64L = Vector128.Add(imageAccum64L, Vector128.Add(Vector128.WidenLower(batch32L), Vector128.WidenUpper(batch32L)));
                        imageAccum64H = Vector128.Add(imageAccum64H, Vector128.Add(Vector128.WidenLower(batch32H), Vector128.WidenUpper(batch32H)));
                    }

                    if (x < widthBytes)
                    {
                        Vector128<byte> r1 = Vector128.Load(p1 - tailShift);
                        Vector128<byte> r2 = Vector128.Load(p2 - tailShift);

                        r1 = Vector128.BitwiseAnd(r1, tailMask);
                        r2 = Vector128.BitwiseAnd(r2, tailMask);

                        Vector128<byte> diff = Vector128.Subtract(Vector128.Max(r1, r2), Vector128.Min(r1, r2));

                        Vector128<ushort> sum16L = Vector128.WidenLower(diff);
                        Vector128<ushort> sum16H = Vector128.WidenUpper(diff);

                        Vector128<uint> sum32L = Vector128.Add(Vector128.WidenLower(sum16L), Vector128.WidenUpper(sum16L));
                        Vector128<uint> sum32H = Vector128.Add(Vector128.WidenLower(sum16H), Vector128.WidenUpper(sum16H));

                        imageAccum64L = Vector128.Add(imageAccum64L, Vector128.Add(Vector128.WidenLower(sum32L), Vector128.WidenUpper(sum32L)));
                        imageAccum64H = Vector128.Add(imageAccum64H, Vector128.Add(Vector128.WidenLower(sum32H), Vector128.WidenUpper(sum32H)));
                    }
                }

                Vector128<ulong> finalSum64 = Vector128.Add(imageAccum64L, imageAccum64H);
                result += Vector128.Sum(finalSum64);
            }
            else
        #endif
            {
                result += ImageBinaryDiffSimdFallback(scan0_1, scan0_2, (uint)widthBytes, height, stride1, stride2);
            }

            double maxChannelValue = (1L << channelSize.AsIntBits()) - 1;
            return result / ((double)width * height * (pixelSize.AsDoubleBits() / channelSize.AsIntBits()) * maxChannelValue);
        }

        /// <summary>
        /// A highly robust, hardware-aligned memory dumping engine designed to visualize discrepancies between two 
        /// pixel buffers (e.g., Oracle vs. Test). It treats the image as a stride-aligned 2D memory map, ensuring that 
        /// asymmetric strides and buffer padding lengths are rendered in perfectly synchronized columns.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The engine scans for mismatches at the pixel level but renders the diagnostic output at the byte level. 
        /// This decoupled approach allows developers to inspect memory corruption, bit-shifts, and trailing padding 
        /// values directly. Absolute byte offsets are provided for each rendered row, enabling rapid translation to 
        /// conditional hardware breakpoints in a debugger (e.g., <c>address + offset</c>).
        /// </para>
        /// <para>
        /// By supporting negative strides natively, this engine can directly compare buffers mapped in inverse 
        /// coordinate systems (e.g., Bottom-Up C# arrays vs. Top-Down GDI+ unmanaged surfaces) without pre-processing.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// Legend: [O]racle (Padding: 0), [T]est (Padding: 6), [D]ifference (Out-of-bounds/No-padding indicated by --)
        /// 
        /// Mismatch Block 1
        /// Row: 4 | Column: 40 | Byte Offset In Row: 120
        /// 0x0000016E O: FF FF FF FF -- -- -- -- -- -- 
        /// 0x00000186 T: 00 00 00 00 00 00 00 00 00 00 
        ///            D: FF FF FF FF                   
        /// </code>
        /// </example>
        /// <param name="oracle">Pointer to the start of the primary (Oracle) image buffer.</param>
        /// <param name="data">Pointer to the start of the secondary (Test) image buffer.</param>
        /// <param name="width">The active logical pixel width of the images.</param>
        /// <param name="height">The active logical pixel height of the images.</param>
        /// <param name="strideOracle">The byte stride of the primary buffer. Negative values dictate a bottom-up memory layout.</param>
        /// <param name="strideData">The byte stride of the secondary buffer. Negative values dictate a bottom-up memory layout.</param>
        /// <param name="pixelSize">The bit depth of a single pixel. Sub-byte values (e.g., 1bpp) will be expanded to their byte containers.</param>
        /// <param name="channelSize">The bit depth of a single color channel.</param>
        /// <param name="sb">The string builder responsible for receiving the output. Minimizes I/O blocking during high-frequency parallel tests.</param>
        /// <param name="leadingPixels">The number of matching, non-corrupted pixels to render before the detected mismatch to provide context.</param>
        /// <param name="maxMismatchPixels">The maximum continuous pixel limit to render in a single diagnostic block to prevent terminal flooding.</param>
        /// <param name="maxBlocks">The maximum number of disconnected mismatch patches to render. Allows discovery of patchy artifacts.</param>
        /// <param name="bytesPerLine">The maximum visual byte width per rendered hex line. 64 is default to fit standard terminal windows.</param>
        private static ReadOnlySpan<char> BitonalChars => 
        [
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
            'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
            'U', 'V', 'W', 'X', 'Y', 'Z'
        ];

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void FormatByte(byte val, int colInChunk, int bitsPerPixel, bool is8bppBitonal, StringBuilder sb, char boundaryChar = ' ')
        {
            if (bitsPerPixel >= 8) 
            {
                if (is8bppBitonal)
                {
                    Span<char> bitonalChars = stackalloc char[4];
                    bitonalChars[0] = val <= 35 ? BitonalChars[val] : '#';
                    bitonalChars[1] = boundaryChar;
                    int bitonalLen = 2;
                    if ((colInChunk & 7) == 7) 
                    {
                        bitonalChars[2] = ' ';
                        bitonalChars[3] = ' ';
                        bitonalLen = 4;
                    }
                    sb.Append(bitonalChars.Slice(0, bitonalLen));
                    return;
                }
                Span<char> hexChars = stackalloc char[3];
                hexChars[0] = BitonalChars[val >> 4];
                hexChars[1] = BitonalChars[val & 0x0F];
                hexChars[2] = boundaryChar;
                sb.Append(hexChars);
                return;
            }
            Span<char> chars = stackalloc char[11];
            int pos = 0;
            for (int i = 7; i >= 0; i--)
            {
                chars[pos++] = (val & (1 << i)) != 0 ? '1' : '0';
                if (i > 0 && ((bitsPerPixel == 4 && i == 4) || (bitsPerPixel == 2 && (i % 2) == 0)))
                {
                    chars[pos++] = ' ';
                }
            }
            sb.Append(chars.Slice(0, pos));
            sb.Append(boundaryChar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void FormatMissingByte(int colInChunk, int bitsPerPixel, bool is8bppBitonal, StringBuilder sb, char boundaryChar = ' ')
        {
            if (is8bppBitonal) 
            {
                Span<char> missChars = stackalloc char[4];
                missChars[0] = '-';
                missChars[1] = boundaryChar;
                int missLen = 2;
                if ((colInChunk & 7) == 7) 
                {
                    missChars[2] = ' ';
                    missChars[3] = ' ';
                    missLen = 4;
                }
                sb.Append(missChars.Slice(0, missLen));
                return;
            }
            sb.Append(bitsPerPixel >= 8 ? "--" : 
                      bitsPerPixel == 4 ? "---- ----" : 
                      bitsPerPixel == 2 ? "-- -- -- --" : 
                      "--------");
            sb.Append(boundaryChar);
        }

        /// <summary>
        /// Calculates the physical unmanaged memory byte offset for a given logical row and column.
        /// Unmanaged Contract: A negative stride naturally steps backwards from the highest memory address.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ImageMemoryOffset(int row, int stride, int column = 0, int leftPadding = 0)
        {
            return  (row * stride) + column + leftPadding;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int PixelsToBytes(int pixels, int bitsPerPixel, int bytesPerPixel)
        {
            return bitsPerPixel >= 8 ? (pixels * bytesPerPixel) : ((pixels * bitsPerPixel) / 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int BytesToPixels(int bytes, int bitsPerPixel, int bytesPerPixel)
        {
            return bitsPerPixel >= 8 ? (bytes / bytesPerPixel) : ((bytes * 8) / bitsPerPixel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void FormatHexDump(
            byte* buffer, int chunkStart, int chunkEnd, 
            int dumpLineWidth, int maxLeftPad, int bufferLeftPad, int bufferAbsStride, 
            int stride, int height, int startPixelLimit, int endPixelLimit,
            int bitsPerPixel, bool is8bppBitonal, StringBuilder sb)
        {
            for (int curr = chunkStart; curr < chunkEnd; curr++)
            {
                int r = curr / dumpLineWidth;
                int v = curr % dumpLineWidth;
                char bChar = (v == startPixelLimit) ? '[' : ((v == endPixelLimit) ? ']' : ' ');
                int rowByteIndex = v - maxLeftPad + bufferLeftPad;
                
                if (r < height && rowByteIndex >= 0 && rowByteIndex < bufferAbsStride) 
                {
                    int memOffset = ImageMemoryOffset(r, stride, rowByteIndex);
                    FormatByte(buffer[memOffset], curr - chunkStart, bitsPerPixel, is8bppBitonal, sb, bChar);
                }
                else 
                {
                    FormatMissingByte(curr - chunkStart, bitsPerPixel, is8bppBitonal, sb, bChar);
                }
            }
            sb.AppendLine();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool FormatHexDumpV2(
            byte* buffer, int chunkStart, int chunkEnd, 
            int dumpLineWidth, int maxLeftPad, int bufferLeftPad, int bufferAbsStride, 
            int stride, int height, int startPixelLimit, int endPixelLimit,
            int bitsPerPixel, bool is8bppBitonal, bool carryBracket, StringBuilder sb)
        {
            if (carryBracket)
            {
                sb.Append('[');
            }
            else
            {
                sb.Append(' ');
            }

            bool nextCarry = false;

            for (int curr = chunkStart; curr < chunkEnd; curr++)
            {
                int r = curr / dumpLineWidth;
                int v = curr % dumpLineWidth;
                
                char bChar = ' ';
                if (v == startPixelLimit)
                {
                    if (curr == chunkEnd - 1)
                    {
                        nextCarry = true; // Defer printing to the next chunk
                    }
                    else
                    {
                        bChar = '[';
                    }
                }
                else if (v == endPixelLimit)
                {
                    bChar = ']';
                }

                int rowByteIndex = v - maxLeftPad + bufferLeftPad;
                
                if (r < height && rowByteIndex >= 0 && rowByteIndex < bufferAbsStride) 
                {
                    int memOffset = ImageMemoryOffset(r, stride, rowByteIndex);
                    FormatByte(buffer[memOffset], curr - chunkStart, bitsPerPixel, is8bppBitonal, sb, bChar);
                }
                else 
                {
                    FormatMissingByte(curr - chunkStart, bitsPerPixel, is8bppBitonal, sb, bChar);
                }
            }
            sb.AppendLine();
            return nextCarry;
        }



        ///// <summary>
        ///// Performs a direct unmanaged memory comparison between two image buffers and formats detailed diagnostic output for mismatching regions.
        ///// </summary>
        ///// <param name="oracle">Pointer to the logical first row (Row 0) of the reference image buffer. For negative strides, this must point to the highest valid memory address of the active buffer.</param>
        ///// <param name="data">Pointer to the logical first row (Row 0) of the test image buffer. For negative strides, this must point to the highest valid memory address of the active buffer.</param>
        ///// <param name="width">The visual width of the active image data in pixels.</param>
        ///// <param name="height">The visual height of the active image data in rows.</param>
        ///// <param name="strideOracle">The signed bytes per row of the oracle buffer. A negative value instructs the engine to step backwards in memory.</param>
        ///// <param name="strideData">The signed bytes per row of the data buffer. A negative value instructs the engine to step backwards in memory.</param>
        ///// <param name="pixelSize">The structural bit depth of a single pixel.</param>
        ///// <param name="channelSize">The bit depth of a single color channel.</param>
        ///// <param name="sb">The string builder where the diagnostic output is appended.</param>
        ///// <param name="leadingPixels">The number of matching pixels to print before the mismatch occurs for contextual debugging.</param>
        ///// <param name="maxMismatchPixels">The maximum number of pixels to process in a single mismatch block.</param>
        ///// <param name="maxBlocks">The maximum number of distinct mismatch blocks to log before aborting.</param>
        ///// <param name="bytesPerLine">The maximum number of bytes to render per visual line of the console output.</param>
        ///// <param name="comparePadding">Determines if the unmanaged memory padding regions should be evaluated for mismatches.</param>
        //internal static unsafe void DumpImageMismatchCore(
        //    byte* oracle, byte* data,
        //    int width, int height,
        //    int strideOracle, int strideData,
        //    PixelSize pixelSize, ChannelSize channelSize,
        //    StringBuilder sb,
        //    uint leadingPixels = 4,
        //    uint maxMismatchPixels = 32,
        //    uint maxBlocks = 1,
        //    uint bytesPerLine = 64,
        //    bool comparePadding = false)
        //{
        //    DumpImageMismatchCore(oracle, data, width, height, strideOracle, strideData, 0, 0, pixelSize, channelSize, sb, leadingPixels, maxMismatchPixels, maxBlocks, bytesPerLine, comparePadding);
        //}

        ///// <summary>
        ///// Performs a direct unmanaged memory comparison between two image buffers and formats detailed diagnostic output for mismatching regions.
        ///// </summary>
        ///// <param name="oracle">Pointer to the logical first row (Row 0) of the reference image buffer. For negative strides, this must point to the highest valid memory address of the active buffer.</param>
        ///// <param name="data">Pointer to the logical first row (Row 0) of the test image buffer. For negative strides, this must point to the highest valid memory address of the active buffer.</param>
        ///// <param name="width">The visual width of the active image data in pixels.</param>
        ///// <param name="height">The visual height of the active image data in rows.</param>
        ///// <param name="strideOracle">The signed bytes per row of the oracle buffer. A negative value instructs the engine to step backwards in memory.</param>
        ///// <param name="strideData">The signed bytes per row of the data buffer. A negative value instructs the engine to step backwards in memory.</param>
        ///// <param name="oracleLeftPad">The number of padding bytes located before the active visual boundary in the oracle buffer.</param>
        ///// <param name="dataLeftPad">The number of padding bytes located before the active visual boundary in the data buffer.</param>
        ///// <param name="pixelSize">The structural bit depth of a single pixel.</param>
        ///// <param name="channelSize">The bit depth of a single color channel.</param>
        ///// <param name="sb">The string builder where the diagnostic output is appended.</param>
        ///// <param name="leadingPixels">The number of matching pixels to print before the mismatch occurs for contextual debugging.</param>
        ///// <param name="maxMismatchPixels">The maximum number of pixels to process in a single mismatch block.</param>
        ///// <param name="maxBlocks">The maximum number of distinct mismatch blocks to log before aborting.</param>
        ///// <param name="bytesPerLine">The maximum number of bytes to render per visual line of the console output.</param>
        ///// <param name="comparePadding">Determines if the unmanaged memory padding regions should be evaluated for mismatches.</param>
        //internal static unsafe void DumpImageMismatchCore(
        //    byte* oracle, byte* data, 
        //    int width, int height, 
        //    int strideOracle, int strideData, 
        //    int oracleLeftPad, int dataLeftPad, 
        //    PixelSize pixelSize, ChannelSize channelSize, 
        //    StringBuilder sb,
        //    uint leadingPixels = 4, 
        //    uint maxMismatchPixels = 32,
        //    uint maxBlocks = 1,
        //    uint bytesPerLine = 64,
        //    bool comparePadding = false)
        //{
        //    if (oracle == null || data == null || sb == null)
        //    {
        //        return;
        //    }

        //    if (width <= 0 || height <= 0 || (int)pixelSize <= 0)
        //    {
        //        return;
        //    }

        //    if (strideOracle == 0 || strideData == 0)
        //    {
        //        return;
        //    }

        //    int bitsPerPixel = (int)pixelSize;
        //    int bytesPerPixel = Math.Max(1, bitsPerPixel / 8);
        //    int imageWidthInBytes = PixelsToBytes(width, bitsPerPixel, bytesPerPixel);

        //    int oracleAbsStride = Math.Abs(strideOracle);
        //    int dataAbsStride = Math.Abs(strideData);

        //    if (oracleAbsStride < imageWidthInBytes)
        //    {
        //        DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width,
        //            $"Oracle stride absolute value ({oracleAbsStride}) is less than image width expressed in bytes ({imageWidthInBytes}).");
        //    }

        //    if (dataAbsStride < imageWidthInBytes)
        //    {
        //        DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width,
        //            $"Data stride absolute value ({dataAbsStride}) is less than image width expressed in bytes ({imageWidthInBytes}).");
        //    }

        //    bytesPerLine = Math.Max(4u, bytesPerLine);
        //    if (bitsPerPixel < 8)
        //    {
        //        bytesPerLine = Math.Max(4u, bytesPerLine / 8);
        //    }

        //    maxMismatchPixels = Math.Max(1u, maxMismatchPixels);
        //    maxBlocks = Math.Max(1u, maxBlocks);

        //    if (leadingPixels >= maxMismatchPixels)
        //    {
        //        throw new ArgumentException(
        //            $"leadingPixels ({leadingPixels}) must be strictly less than maxMismatchPixels ({maxMismatchPixels}) to guarantee the mismatched pixel is rendered within the diagnostic block.", 
        //            nameof(leadingPixels));
        //    }

        //    bool is8bppBitonal = (bitsPerPixel == 8 && (int)channelSize == 1);
        //    bool is1bppBitonal = (bitsPerPixel == 1 && (int)channelSize == 1);

        //    int blocksDumped = 0;
        //    int leadingBytes = (int)leadingPixels * bytesPerPixel;
        //    int maxMismatchBytes = (int)maxMismatchPixels * bytesPerPixel;
            
        //    int maxLeftPad = Math.Max(oracleLeftPad, dataLeftPad);

        //    // TDOD: that's wrong - requires fixing - stride does not include left padding but only width * bytesPerPixel and right padding
        //    // Fix requires removal of oracleLeftPad from equation - currently its passed always as 0 for Bitmap so tests pass
        //    int oracleRightPad = Math.Max(0, oracleAbsStride - imageWidthInBytes /* - oracleLeftPad*/);
        //    int dataRightPad = Math.Max(0, dataAbsStride - imageWidthInBytes /* - dataLeftPad*/);
        //    int maxRightPad = Math.Max(oracleRightPad, dataRightPad);
            
        //    int dumpLineWidth = Math.Max(oracleAbsStride, dataAbsStride); // maxLeftPad +  imageWidthInBytes + maxRightPad;
            
        //    int oracleTotalPad = oracleRightPad + oracleLeftPad;
        //    int dataTotalPad = dataRightPad + dataLeftPad;

        //    string GetPadString(int pad)
        //    {
        //        return $"Pad: {pad}";
        //    }

        //    sb.AppendLine(
        //        $"Legend: [O]racle ({GetPadString(Math.Max(0, oracleAbsStride - imageWidthInBytes))}), " +
        //        $"[T]est ({GetPadString(Math.Max(0, dataAbsStride - imageWidthInBytes))}), [D]ifference (No buffer: -)");

        //    if (is8bppBitonal)
        //    {
        //        sb.AppendLine("Legend (8bpp Bitonal): Base36 (0-Z) encoding, value > 35: #");
        //    }
        //    sb.AppendLine($"Format: {bitsPerPixel}bpp (Channel: {(int)channelSize}bit) | Area: {width} x {height} pixels");

        //    int startByte = 0;
        //    for (int y = 0; y < height; y++)
        //    {
        //        int rowOffsetOracle = ImageMemoryOffset(y, strideOracle, oracleLeftPad);
        //        int rowOffsetData = ImageMemoryOffset(y, strideData, dataLeftPad);

        //        int rowBytesToCompare = comparePadding 
        //            ? Math.Min(oracleAbsStride, dataAbsStride) 
        //            : PixelsToBytes(width, bitsPerPixel, bytesPerPixel);

        //        if (comparePadding && y == 0)
        //        {
        //            rowOffsetOracle = 0;
        //            rowOffsetData = 0;
        //            rowBytesToCompare += Math.Min(oracleLeftPad, dataLeftPad);
        //        }

        //        int currentByte = startByte;
        //        startByte = 0; // reset for subsequent rows

        //        while (currentByte < rowBytesToCompare)
        //        {
        //            ReadOnlySpan<byte> oracleSpan = new ReadOnlySpan<byte>(oracle + rowOffsetOracle + currentByte, rowBytesToCompare - currentByte);
        //            ReadOnlySpan<byte> dataSpan = new ReadOnlySpan<byte>(data + rowOffsetData + currentByte, rowBytesToCompare - currentByte);
                    
        //            int commonLen = oracleSpan.CommonPrefixLength(dataSpan);
                    
        //            if (commonLen == oracleSpan.Length)
        //            {
        //                break; 
        //            }

        //            int mismatchByte = currentByte + commonLen;
        //            int x = BytesToPixels(mismatchByte, bitsPerPixel, bytesPerPixel);
        //            int pxByte = PixelsToBytes(x, bitsPerPixel, bytesPerPixel);

        //            int padAdjust = (comparePadding && y == 0) ? Math.Min(oracleLeftPad, dataLeftPad) : 0;
        //            int mismatchDumpIndex = y * dumpLineWidth + maxLeftPad - padAdjust + pxByte;
        //            int dumpStartIndex = Math.Max(0, mismatchDumpIndex - leadingBytes);
                    
        //            int subunitSize = 1;
        //            if (is8bppBitonal) subunitSize = 8;
        //            else if (is1bppBitonal) subunitSize = 1; // 1 byte = 8 pixels

        //            dumpStartIndex = (dumpStartIndex / subunitSize) * subunitSize;
                    
        //            int dumpEndIndex = Math.Min(height * dumpLineWidth, mismatchDumpIndex + maxMismatchBytes);

        //            sb.AppendLine($"\nMismatch Block {blocksDumped + 1}");
                    
        //            // Determine the raw byte offset of the mismatch relative to the start of the pixel payload.
        //            // The Bitmap format consists of a single global Border (leading bytes) before Row 0,
        //            // followed by Rows consisting of Pixels and Right Padding. There is no per-row Left Padding.
        //            // Therefore, we calculate the absolute linear distance from the start of the row's pixels.
        //            int rowByteOffset = mismatchDumpIndex - (y * dumpLineWidth + maxLeftPad);
        //            int visualWidthBytes = width * bytesPerPixel;
                    
        //            int reportedY = y;
        //            string colString;
                    
        //            // If the byte offset is negative, the mismatch occurred in the global Left Border.
        //            // Due to the physical geometry of Bitmap, this can ONLY happen before Row 0's pixels.
        //            if (rowByteOffset < 0)
        //            {
        //                colString = $"[Leading Padding: {rowByteOffset} bytes (to row start)]";
        //            }
        //            // If the byte offset exceeds the pixel width, it falls into the Right Padding 
        //            // (the memory between the end of Row y and the start of Row y+1).
        //            else if (rowByteOffset >= visualWidthBytes)
        //            {
        //                int rightOffset = rowByteOffset - visualWidthBytes + 1;
        //                colString = $"[Border: {rightOffset} bytes (from row end)]";
        //            }
        //            // Otherwise, the mismatch is squarely within the active pixel payload (so we calculate pixel column).
        //            else
        //            {
        //                int pixelCol = BytesToPixels(rowByteOffset, bitsPerPixel, bytesPerPixel);
        //                colString = $"{pixelCol}";
        //            }

        //            sb.AppendLine($"Row: {reportedY} | Column: {colString} | Byte Offset In Row: {rowByteOffset}");

        //            int startPixelLimit = maxLeftPad > 0 ? maxLeftPad - 1 : -1;
        //            int endPixelLimit = maxLeftPad > 0 ? maxLeftPad + (width * bytesPerPixel) - 1 : -1;

        //                for (int chunkStart = dumpStartIndex; chunkStart < dumpEndIndex; chunkStart += (int)bytesPerLine)
        //                {
        //                    int chunkEnd = Math.Min(dumpEndIndex, chunkStart + (int)bytesPerLine);
        //                    int dumpRow = chunkStart / dumpLineWidth;
        //                    int dumpCol = chunkStart % dumpLineWidth;
                            
        //                    // Oracle Hex Dump
        //                    int oracleRowByteIndex = dumpCol - maxLeftPad + oracleLeftPad;
        //                    int oracleBufferIndex = ImageMemoryOffset(dumpRow, strideOracle, oracleRowByteIndex);
        //                    sb.Append($"0x{oracleBufferIndex:X8} O: ");
        //                    FormatHexDump(
        //                        oracle, chunkStart, chunkEnd, dumpLineWidth, maxLeftPad, oracleLeftPad, oracleAbsStride,
        //                        strideOracle, height, startPixelLimit, endPixelLimit, bitsPerPixel, is8bppBitonal, sb);

        //                    // Test Hex Dump
        //                    int dataRowByteIndex = dumpCol - maxLeftPad + dataLeftPad;
        //                    int dataBufferIndex = ImageMemoryOffset(dumpRow, strideData, dataRowByteIndex);
        //                    sb.Append($"0x{dataBufferIndex:X8} T: ");
        //                    FormatHexDump(
        //                        data, chunkStart, chunkEnd, dumpLineWidth, maxLeftPad, dataLeftPad, dataAbsStride,
        //                        strideData, height, startPixelLimit, endPixelLimit, bitsPerPixel, is8bppBitonal, sb);

        //                    // Difference Dump
        //                    sb.Append($"           D: ");
        //                    for (int curr = chunkStart; curr < chunkEnd; curr++)
        //                    {
        //                        int r = curr / dumpLineWidth;
        //                        int v = curr % dumpLineWidth;
        //                        char bChar = (v == startPixelLimit) ? '[' : ((v == endPixelLimit) ? ']' : ' ');
        //                        int cO = v - maxLeftPad + oracleLeftPad;
        //                        int cT = v - maxLeftPad + dataLeftPad;
                                
        //                        if (r < height && cO >= 0 && cO < oracleAbsStride && cT >= 0 && cT < dataAbsStride) 
        //                        {
        //                            int memOffsetOracle = ImageMemoryOffset(r, strideOracle, cO);
        //                            int memOffsetData = ImageMemoryOffset(r, strideData, cT);
                                    
        //                            byte diff = (bitsPerPixel < 8)
        //                                ? (byte)(oracle[memOffsetOracle] ^ data[memOffsetData]) 
        //                                : (byte)Math.Abs(oracle[memOffsetOracle] - data[memOffsetData]);
                                        
        //                            FormatByte(diff, curr - chunkStart, bitsPerPixel, is8bppBitonal, sb, bChar);
        //                        }
        //                        else 
        //                        {
        //                            FormatMissingByte(curr - chunkStart, bitsPerPixel, is8bppBitonal, sb, bChar);
        //                        }
        //                    }
        //                    sb.AppendLine();
        //                    sb.AppendLine(); // Empty separator between line chunks
        //                }

        //                blocksDumped++;
        //                if (blocksDumped >= maxBlocks) return;

        //                int nextLogical = dumpEndIndex;
        //                int nextY = nextLogical / dumpLineWidth;
        //                int nextC = nextLogical % dumpLineWidth;

        //                int padAdjustForNext = comparePadding ? Math.Min(oracleLeftPad, dataLeftPad) : 0;
        //                int nextPxByte = nextC - maxLeftPad + padAdjustForNext;

        //                if (nextY > y)
        //                {
        //                    y = nextY - 1; // Outer loop will increment
        //                    startByte = Math.Max(0, nextPxByte);
        //                    break; 
        //                }
        //                else
        //                {
        //                    currentByte = Math.Max(0, nextPxByte);
        //                    if (currentByte <= pxByte) currentByte = pxByte + 1; // prevent infinite loops if block was 0 length (which shouldn't happen, but just in case)
        //                }
        //        }
        //    }
        //}

        /// <summary>
        /// Performs a direct unmanaged memory comparison between two image buffers and formats detailed diagnostic output for mismatching regions.
        /// </summary>
        /// <param name="oracle">Pointer to the logical first row (Row 0) of the reference image buffer. For negative strides, this must point to the highest valid memory address of the active buffer.</param>
        /// <param name="data">Pointer to the logical first row (Row 0) of the test image buffer. For negative strides, this must point to the highest valid memory address of the active buffer.</param>
        /// <param name="width">The visual width of the active image data in pixels.</param>
        /// <param name="height">The visual height of the active image data in rows.</param>
        /// <param name="strideOracle">The signed bytes per row of the oracle buffer. A negative value instructs the engine to step backwards in memory.</param>
        /// <param name="strideData">The signed bytes per row of the data buffer. A negative value instructs the engine to step backwards in memory.</param>
        /// <param name="pixelSize">The structural bit depth of a single pixel.</param>
        /// <param name="channelSize">The bit depth of a single color channel.</param>
        /// <param name="sb">The string builder where the diagnostic output is appended.</param>
        /// <param name="leadingPixels">The number of matching pixels to print before the mismatch occurs for contextual debugging.</param>
        /// <param name="maxMismatchPixels">The maximum number of pixels to process in a single mismatch block.</param>
        /// <param name="maxBlocks">The maximum number of distinct mismatch blocks to log before aborting.</param>
        /// <param name="bytesPerLine">The maximum number of bytes to render per visual line of the console output.</param>
        /// <param name="comparePadding">Determines if the unmanaged memory padding regions should be evaluated for mismatches.</param>
        internal static unsafe void DumpImageMismatchCore(
            byte* oracle, byte* data,
            int width, int height,
            int strideOracle, int strideData,
            PixelSize pixelSize, ChannelSize channelSize,
            StringBuilder sb,
            uint leadingPixels = 4,
            uint maxMismatchPixels = 32,
            uint maxBlocks = 1,
            uint bytesPerLine = 64,
            bool comparePadding = false)
        {
            DumpImageMismatchCore(oracle, data, width, height, strideOracle, strideData, 0, 0, pixelSize, channelSize, sb, leadingPixels, maxMismatchPixels, maxBlocks, bytesPerLine, comparePadding);
        }

        /// <summary>
        /// Performs a direct unmanaged memory comparison between two image buffers and formats detailed diagnostic output for mismatching regions.
        /// </summary>
        /// <param name="oracle">Pointer to the logical first row (Row 0) of the reference image buffer. For negative strides, this must point to the highest valid memory address of the active buffer.</param>
        /// <param name="data">Pointer to the logical first row (Row 0) of the test image buffer. For negative strides, this must point to the highest valid memory address of the active buffer.</param>
        /// <param name="width">The visual width of the active image data in pixels.</param>
        /// <param name="height">The visual height of the active image data in rows.</param>
        /// <param name="strideOracle">The signed bytes per row of the oracle buffer. A negative value instructs the engine to step backwards in memory.</param>
        /// <param name="strideData">The signed bytes per row of the data buffer. A negative value instructs the engine to step backwards in memory.</param>
        /// <param name="oracleLeftPad">The number of padding bytes located before the active visual boundary in the oracle buffer.</param>
        /// <param name="dataLeftPad">The number of padding bytes located before the active visual boundary in the data buffer.</param>
        /// <param name="pixelSize">The structural bit depth of a single pixel.</param>
        /// <param name="channelSize">The bit depth of a single color channel.</param>
        /// <param name="sb">The string builder where the diagnostic output is appended.</param>
        /// <param name="leadingPixels">The number of matching pixels to print before the mismatch occurs for contextual debugging.</param>
        /// <param name="maxMismatchPixels">The maximum number of pixels to process in a single mismatch block.</param>
        /// <param name="maxBlocks">The maximum number of distinct mismatch blocks to log before aborting.</param>
        /// <param name="bytesPerLine">The maximum number of bytes to render per visual line of the console output.</param>
        /// <param name="comparePadding">Determines if the unmanaged memory padding regions should be evaluated for mismatches.</param>
        internal static unsafe void DumpImageMismatchCore(
            byte* oracle, byte* data,
            int width, int height,
            int strideOracle, int strideData,
            int oracleLeftPad, int dataLeftPad,
            PixelSize pixelSize, ChannelSize channelSize,
            StringBuilder sb,
            uint leadingPixels = 4,
            uint maxMismatchPixels = 32,
            uint maxBlocks = 1,
            uint bytesPerLine = 64,
            bool comparePadding = false)
        {
            if (oracle == null || data == null || sb == null)
            {
                return;
            }

            if (width <= 0 || height <= 0 || (int)pixelSize <= 0)
            {
                return;
            }

            if (strideOracle == 0 || strideData == 0)
            {
                return;
            }

            int bitsPerPixel = (int)pixelSize;
            int bytesPerPixel = Math.Max(1, bitsPerPixel / 8);
            int imageWidthInBytes = PixelsToBytes(width, bitsPerPixel, bytesPerPixel);

            int oracleAbsStride = Math.Abs(strideOracle);
            int dataAbsStride = Math.Abs(strideData);

            if (oracleAbsStride < imageWidthInBytes)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width,
                    $"Oracle stride absolute value ({oracleAbsStride}) is less than image width expressed in bytes ({imageWidthInBytes}).");
            }

            if (dataAbsStride < imageWidthInBytes)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width,
                    $"Data stride absolute value ({dataAbsStride}) is less than image width expressed in bytes ({imageWidthInBytes}).");
            }

            if (oracleLeftPad < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(oracleLeftPad), oracleLeftPad, "Oracle left padding must be positive or zero.");
            }

            if (dataLeftPad < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(dataLeftPad), dataLeftPad, "Data left padding must be positive or zero.");
            }

            bytesPerLine = Math.Max(4u, bytesPerLine);
            if (bitsPerPixel < 8)
            {
                bytesPerLine = Math.Max(4u, bytesPerLine / 8);
            }

            maxMismatchPixels = Math.Max(1u, maxMismatchPixels);
            maxBlocks = Math.Max(1u, maxBlocks);

            if (leadingPixels >= maxMismatchPixels)
            {
                throw new ArgumentException(
                    $"leadingPixels ({leadingPixels}) must be strictly less than maxMismatchPixels ({maxMismatchPixels}) to guarantee the mismatched pixel is rendered within the diagnostic block.",
                    nameof(leadingPixels));
            }

            bool is8bppBitonal = (bitsPerPixel == 8 && (int)channelSize == 1);
            bool is1bppBitonal = (bitsPerPixel == 1 && (int)channelSize == 1);

            int blocksDumped = 0;
            int leadingBytes = (int)leadingPixels * bytesPerPixel;
            int maxMismatchBytes = (int)maxMismatchPixels * bytesPerPixel;

            int maxLeftPad = Math.Max(oracleLeftPad, dataLeftPad);

            // TDOD: that's wrong - requires fixing - stride does not include left padding but only width * bytesPerPixel and right padding
            // Fix requires removal of oracleLeftPad from equation - currently its passed always as 0 for Bitmap so tests pass
            int oracleRightPad = Math.Max(0, oracleAbsStride - imageWidthInBytes /* - oracleLeftPad*/);
            int dataRightPad = Math.Max(0, dataAbsStride - imageWidthInBytes /* - dataLeftPad*/);
            int maxRightPad = Math.Max(oracleRightPad, dataRightPad);

            int dumpLineWidth = Math.Max(oracleAbsStride, dataAbsStride); // maxLeftPad +  imageWidthInBytes + maxRightPad;

            int oracleTotalPad = oracleRightPad + oracleLeftPad;
            int dataTotalPad = dataRightPad + dataLeftPad;

            string GetPadString(int pad)
            {
                return $"Pad: {pad}";
            }

            sb.AppendLine(
                $"Legend: [O]racle ({GetPadString(Math.Max(0, oracleAbsStride - imageWidthInBytes))}), " +
                $"[T]est ({GetPadString(Math.Max(0, dataAbsStride - imageWidthInBytes))}), [D]ifference (No buffer: -)");

            if (is8bppBitonal)
            {
                sb.AppendLine("Legend (8bpp Bitonal): Base36 (0-Z) encoding, value > 35: #");
            }
            sb.AppendLine($"Format: {bitsPerPixel}bpp (Channel: {(int)channelSize}bit) | Area: {width} x {height} pixels");

            int startByte = 0;
            for (int y = 0; y < height; y++)
            {
                int rowOffsetOracle = ImageMemoryOffset(y, strideOracle, oracleLeftPad);
                int rowOffsetData = ImageMemoryOffset(y, strideData, dataLeftPad);

                int rowBytesToCompare = comparePadding
                    ? Math.Min(oracleAbsStride, dataAbsStride)
                    : PixelsToBytes(width, bitsPerPixel, bytesPerPixel);

                int overlap = 0;
                if (comparePadding && y == 0)
                {
                    overlap = Math.Min(oracleLeftPad, dataLeftPad);
                    rowOffsetOracle = ImageMemoryOffset(0, strideOracle, oracleLeftPad - overlap);
                    rowOffsetData = ImageMemoryOffset(0, strideData, dataLeftPad - overlap);
                    rowBytesToCompare += overlap;
                }

                int currentByte = startByte;
                startByte = 0; // reset for subsequent rows

                while (currentByte < rowBytesToCompare)
                {
                    ReadOnlySpan<byte> oracleSpan = new ReadOnlySpan<byte>(oracle + rowOffsetOracle + currentByte, rowBytesToCompare - currentByte);
                    ReadOnlySpan<byte> dataSpan = new ReadOnlySpan<byte>(data + rowOffsetData + currentByte, rowBytesToCompare - currentByte);

                    int commonLen = oracleSpan.CommonPrefixLength(dataSpan);

                    if (commonLen == oracleSpan.Length)
                    {
                        break;
                    }

                    int mismatchByte = currentByte + commonLen;
                    int rowByteIndex = mismatchByte - overlap;
                    
                    int pixelMismatchByteIndex;
                    if (rowByteIndex < 0)
                    {
                        pixelMismatchByteIndex = rowByteIndex;
                    }
                    else
                    {
                        int x = BytesToPixels(rowByteIndex, bitsPerPixel, bytesPerPixel);
                        pixelMismatchByteIndex = PixelsToBytes(x, bitsPerPixel, bytesPerPixel);
                    }

                    int mismatchDumpIndex = y * dumpLineWidth + maxLeftPad + pixelMismatchByteIndex;
                    int dumpStartIndex = Math.Max(0, mismatchDumpIndex - leadingBytes);

                    int subunitSize = 1;
                    if (is8bppBitonal)
                        subunitSize = 8;
                    else if (is1bppBitonal)
                        subunitSize = 1; // 1 byte = 8 pixels

                    dumpStartIndex = (dumpStartIndex / subunitSize) * subunitSize;

                    int dumpEndIndex = Math.Min(height * dumpLineWidth, mismatchDumpIndex + maxMismatchBytes);

                    sb.AppendLine($"\nMismatch Block {blocksDumped + 1}");

                    // Determine the raw byte offset of the mismatch relative to the start of the pixel payload.
                    // The Bitmap format consists of a single global Border (leading bytes) before Row 0,
                    // followed by Rows consisting of Pixels and Right Padding. There is no per-row Left Padding.
                    // Therefore, we calculate the absolute linear distance from the start of the row's pixels.
                    //int rowByteIndex = mismatchDumpIndex - (y * dumpLineWidth + maxLeftPad);
                    int visualWidthBytes = width * bytesPerPixel;

                    int reportedY = y;
                    string colString;

                    // If the byte offset is negative, the mismatch occurred in the global Left Border.
                    // Due to the physical geometry of Bitmap, this can ONLY happen before Row 0's pixels.
                    if (rowByteIndex < 0)
                    {
                        colString = $"[Leading Padding: {rowByteIndex} bytes (to row start)]";
                    }
                    // If the byte offset exceeds the pixel width, it falls into the Right Padding 
                    // (the memory between the end of Row y and the start of Row y+1).
                    else if (rowByteIndex >= visualWidthBytes)
                    {
                        int rightOffset = rowByteIndex - visualWidthBytes + 1;
                        colString = $"[Border: {rightOffset} bytes (from row end)]";
                    }
                    // Otherwise, the mismatch is squarely within the active pixel payload (so we calculate pixel column).
                    else
                    {
                        int pixelCol = BytesToPixels(rowByteIndex, bitsPerPixel, bytesPerPixel);
                        colString = $"{pixelCol}";
                    }

                    sb.AppendLine($"Row: {reportedY} | Column: {colString} | Byte Offset In Row: {rowByteIndex}");

                    int startPixelLimit = maxLeftPad > 0 ? maxLeftPad - 1 : -1;
                    int endPixelLimit = maxLeftPad > 0 ? maxLeftPad + imageWidthInBytes - 1 : -1;

                    bool carryOracle = false;
                    bool carryData = false;
                    bool carryDiff = false;

                    for (int chunkStart = dumpStartIndex; chunkStart < dumpEndIndex; chunkStart += (int)bytesPerLine)
                    {
                        int chunkEnd = Math.Min(dumpEndIndex, chunkStart + (int)bytesPerLine);
                        int dumpRow = chunkStart / dumpLineWidth;
                        int dumpCol = chunkStart % dumpLineWidth;

                        // Oracle Hex Dump
                        int oracleRowByteIndex = dumpCol - maxLeftPad + oracleLeftPad;
                        int oracleBufferIndex = ImageMemoryOffset(dumpRow, strideOracle, oracleRowByteIndex);
                        sb.Append($"0x{oracleBufferIndex:X8} O: ");
                        carryOracle = FormatHexDumpV2(
                            oracle, chunkStart, chunkEnd, dumpLineWidth, maxLeftPad, oracleLeftPad, oracleAbsStride,
                            strideOracle, height, startPixelLimit, endPixelLimit, bitsPerPixel, is8bppBitonal, carryOracle, sb);

                        // Test Hex Dump
                        int dataRowByteIndex = dumpCol - maxLeftPad + dataLeftPad;
                        int dataBufferIndex = ImageMemoryOffset(dumpRow, strideData, dataRowByteIndex);
                        sb.Append($"0x{dataBufferIndex:X8} T: ");
                        carryData = FormatHexDumpV2(
                            data, chunkStart, chunkEnd, dumpLineWidth, maxLeftPad, dataLeftPad, dataAbsStride,
                            strideData, height, startPixelLimit, endPixelLimit, bitsPerPixel, is8bppBitonal, carryData, sb);

                        // Difference Dump
                        sb.Append($"           D: ");
                        if (carryDiff)
                        {
                            sb.Append('[');
                        }
                        else
                        {
                            sb.Append(' ');
                        }
                        bool nextCarryDiff = false;

                        for (int curr = chunkStart; curr < chunkEnd; curr++)
                        {
                            int r = curr / dumpLineWidth;
                            int v = curr % dumpLineWidth;
                            
                            char bChar = ' ';
                            if (v == startPixelLimit)
                            {
                                if (curr == chunkEnd - 1)
                                {
                                    nextCarryDiff = true;
                                }
                                else
                                {
                                    bChar = '[';
                                }
                            }
                            else if (v == endPixelLimit)
                            {
                                bChar = ']';
                            }
                            
                            int cO = v - maxLeftPad + oracleLeftPad;
                            int cT = v - maxLeftPad + dataLeftPad;

                            if (r < height && cO >= 0 && cO < oracleAbsStride && cT >= 0 && cT < dataAbsStride)
                            {
                                int memOffsetOracle = ImageMemoryOffset(r, strideOracle, cO);
                                int memOffsetData = ImageMemoryOffset(r, strideData, cT);

                                byte diff = (bitsPerPixel < 8)
                                    ? (byte)(oracle[memOffsetOracle] ^ data[memOffsetData])
                                    : (byte)Math.Abs(oracle[memOffsetOracle] - data[memOffsetData]);

                                FormatByte(diff, curr - chunkStart, bitsPerPixel, is8bppBitonal, sb, bChar);
                            }
                            else
                            {
                                FormatMissingByte(curr - chunkStart, bitsPerPixel, is8bppBitonal, sb, bChar);
                            }
                        }
                        sb.AppendLine();
                        carryDiff = nextCarryDiff;
                        sb.AppendLine(); // Empty separator between line chunks
                    }

                    blocksDumped++;
                    if (blocksDumped >= maxBlocks)
                        return;

                    int nextLogical = dumpEndIndex;
                    int nextY = nextLogical / dumpLineWidth;
                    int nextC = nextLogical % dumpLineWidth;

                    int padAdjustForNext = comparePadding ? Math.Min(oracleLeftPad, dataLeftPad) : 0;
                    int nextPxByte = nextC - maxLeftPad + padAdjustForNext;

                    if (nextY > y)
                    {
                        y = nextY - 1; // Outer loop will increment
                        startByte = Math.Max(0, nextPxByte);
                        break;
                    }
                    else
                    {
                        currentByte = Math.Max(0, nextPxByte);
                        if (currentByte <= pixelMismatchByteIndex)
                            currentByte = pixelMismatchByteIndex + 1; // prevent infinite loops if block was 0 length (which shouldn't happen, but just in case)
                    }
                }
            }
        }

        public static unsafe void DumpImageMismatch(SysBitmap oracle, SysBitmap data, int fileIndex, string mapType,
            uint leadingPixels = 4,
            uint maxMismatchPixels = 32,
            uint maxBlocks = 1,
            uint bytesPerLine = 64,
            bool comparePadding = false)
        {
            try
            {
                if (oracle == null || data == null)
                {
                    Console.WriteLine($"\n{mapType} Mismatch Details for test0{fileIndex:00#}C.djvu: One or both image references are null.");
                    return;
                }
                if (oracle.Width == 0 || oracle.Height == 0)
                {
                    Console.WriteLine($"\n{mapType} Mismatch Details for test0{fileIndex:00#}C.djvu: Dimensions are {oracle.Width}x{oracle.Height}. Skipping.");
                    return;
                }
                if (oracle.Width != data.Width || oracle.Height != data.Height)
                {
                    Console.WriteLine($"\n{mapType} Mismatch Details for test0{fileIndex:00#}C.djvu: Dimensions mismatch ({oracle.Width}x{oracle.Height} vs {data.Width}x{data.Height}).");
                    return;
                }
                if (oracle.PixelFormat != data.PixelFormat)
                {
                    Console.WriteLine($"\n{mapType} Mismatch Details for test0{fileIndex:00#}C.djvu: PixelFormat mismatch ({oracle.PixelFormat} vs {data.PixelFormat}).");
                    return;
                }

                BitmapData data1 = null;
                BitmapData data2 = null;
                try
                {
                    data1 = oracle.LockBits(new Rectangle(0, 0, oracle.Width, oracle.Height), ImageLockMode.ReadOnly, oracle.PixelFormat);
                    data2 = data.LockBits(new Rectangle(0, 0, data.Width, data.Height), ImageLockMode.ReadOnly, data.PixelFormat);
                    
                    int bitsPerPixel = Image.GetPixelFormatSize(oracle.PixelFormat);
                    
                    ChannelSize cSize = ChannelSize._8bit;
                    if (bitsPerPixel == 1) cSize = ChannelSize._1bit;
                    else if (bitsPerPixel == 2) cSize = ChannelSize._2bit;
                    else if (bitsPerPixel == 4) cSize = ChannelSize._4bit;
                    else if (bitsPerPixel >= 48) cSize = ChannelSize._16bit;

                    StringBuilder sb = new StringBuilder(1024);
                    sb.AppendLine($"\n{mapType} Mismatches for test0{fileIndex:00#}C.djvu:");
                    
                    DumpImageMismatchCore((byte*)data1.Scan0, (byte*)data2.Scan0, oracle.Width, oracle.Height, 
                        data1.Stride, data2.Stride, 0, 0,
                        (PixelSize)bitsPerPixel, cSize, sb,
                        leadingPixels, maxMismatchPixels, maxBlocks, bytesPerLine, comparePadding);
                        
                    Console.Write(sb.ToString());
                }
                finally
                {
                    if (data1 != null) oracle.UnlockBits(data1);
                    if (data2 != null) data.UnlockBits(data2);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError dumping mismatch details for test0{fileIndex:00#}C.djvu: {ex.Message}");
            }
        }

        ///// <summary>
        ///// Dumps formatted hex diagnostic blocks to the console when two DjvuNet Graphics.Bitmap buffers mismatch.
        ///// Extracts geometric topology dynamically from the Graphics.Bitmap properties.
        ///// </summary>
        ///// <param name="oracle">Reference to the primary (Oracle) Bitmap object.</param>
        ///// <param name="testImage">Reference to the secondary (Test) Bitmap object.</param>
        ///// <param name="fileIndex">The numeric test index mapped to the file name, for console logging.</param>
        ///// <param name="leadingPixels">The number of matching, non-corrupted pixels to render before the detected mismatch to provide context.</param>
        ///// <param name="maxMismatchPixels">The maximum continuous pixel limit to render in a single diagnostic block to prevent terminal flooding.</param>
        ///// <param name="maxBlocks">The maximum number of disconnected mismatch patches to render.</param>
        ///// <param name="bytesPerLine">The maximum visual byte width per rendered hex line.</param>
        //public static unsafe void DumpImageMismatch(
        //    ref Graphics.Bitmap oracle, ref Graphics.Bitmap data, int fileIndex,
        //    bool bitonal = false,
        //    uint leadingPixels = 4,
        //    uint maxMismatchPixels = 32,
        //    uint maxBlocks = 1,
        //    uint bytesPerLine = 64,
        //    bool comparePadding = false)
        //{
        //    try
        //    {
        //        string typeName = typeof(Graphics.Bitmap).FullName;

        //        if (Unsafe.IsNullRef(ref oracle) || Unsafe.IsNullRef(ref data))
        //        {
        //            Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: One or both image references are null.");
        //            return;
        //        }
                
        //        if (oracle.Data == null && oracle.RleData != null) oracle.Decompress();
        //        if (data.Data == null && data.RleData != null) data.Decompress();
                
        //        if (oracle.Data == null || data.Data == null)
        //        {
        //            Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: One or both image Data buffers are null.");
        //            return;
        //        }
                
        //        long oracleExpected = (long)oracle.Height * oracle.BytesPerRow;
        //        long dataExpected = (long)data.Height * data.BytesPerRow;
                
        //        if (oracle.Data.Length < oracleExpected || data.Data.Length < dataExpected)
        //        {
        //            Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: One or both image Data buffers are undersized.");
        //            return;
        //        }
                
        //        if (oracle.Width == 0 || oracle.Height == 0)
        //        {
        //            Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: Dimensions are {oracle.Width}x{oracle.Height}. Border checks are not visualized.");
        //            return;
        //        }
                
        //        if (oracle.Width != data.Width || oracle.Height != data.Height)
        //        {
        //            Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: Dimensions mismatch ({oracle.Width}x{oracle.Height} vs {data.Width}x{data.Height}).");
        //            return;
        //        }

        //        StringBuilder sb = new StringBuilder(1024);
        //        sb.AppendLine($"\n{typeName} Mismatches for test0{fileIndex:00#}C.djvu:");
                
        //        DumpImageMismatchCore((byte*)oracle.DataPointer, (byte*)data.DataPointer, oracle.Width, oracle.Height, 
        //            oracle.BytesPerRow, data.BytesPerRow, 
        //            oracle.Border, data.Border,
        //            PixelSize._8bpp, bitonal ? ChannelSize._1bit : ChannelSize._8bit, sb,
        //            leadingPixels, maxMismatchPixels, maxBlocks, bytesPerLine, comparePadding);
                    
        //        Console.Write(sb.ToString());
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"\nError dumping mismatch details for test0{fileIndex:00#}C.djvu: {ex.Message}");
        //    }
        //}

        /// <summary>
        /// Dumps formatted hex diagnostic blocks to the console when two DjvuNet Graphics.Bitmap buffers mismatch.
        /// Extracts geometric topology dynamically from the Graphics.Bitmap properties.
        /// </summary>
        /// <param name="oracle">Reference to the primary (Oracle) Bitmap object.</param>
        /// <param name="testImage">Reference to the secondary (Test) Bitmap object.</param>
        /// <param name="fileIndex">The numeric test index mapped to the file name, for console logging.</param>
        /// <param name="leadingPixels">The number of matching, non-corrupted pixels to render before the detected mismatch to provide context.</param>
        /// <param name="maxMismatchPixels">The maximum continuous pixel limit to render in a single diagnostic block to prevent terminal flooding.</param>
        /// <param name="maxBlocks">The maximum number of disconnected mismatch patches to render.</param>
        /// <param name="bytesPerLine">The maximum visual byte width per rendered hex line.</param>
        public static unsafe void DumpImageMismatch(
            ref Graphics.Bitmap oracle, ref Graphics.Bitmap data, int fileIndex,
            bool bitonal = false,
            uint leadingPixels = 4,
            uint maxMismatchPixels = 32,
            uint maxBlocks = 1,
            uint bytesPerLine = 64,
            bool comparePadding = false)
        {
            try
            {
                string typeName = typeof(Graphics.Bitmap).FullName;

                if (Unsafe.IsNullRef(ref oracle) || Unsafe.IsNullRef(ref data))
                {
                    Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: One or both image references are null.");
                    return;
                }

                if (oracle.Data == null && oracle.RleData != null)
                    oracle.Decompress();
                if (data.Data == null && data.RleData != null)
                    data.Decompress();

                if (oracle.Data == null || data.Data == null)
                {
                    Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: One or both image Data buffers are null.");
                    return;
                }

                long oracleExpected = (long)oracle.Height * oracle.BytesPerRow;
                long dataExpected = (long)data.Height * data.BytesPerRow;

                if (oracle.Data.Length < oracleExpected || data.Data.Length < dataExpected)
                {
                    Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: One or both image Data buffers are undersized.");
                    return;
                }

                if (oracle.Width == 0 || oracle.Height == 0)
                {
                    Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: Dimensions are {oracle.Width}x{oracle.Height}. Border checks are not visualized.");
                    return;
                }

                if (oracle.Width != data.Width || oracle.Height != data.Height)
                {
                    Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: Dimensions mismatch ({oracle.Width}x{oracle.Height} vs {data.Width}x{data.Height}).");
                    return;
                }

                StringBuilder sb = new StringBuilder(1024);
                sb.AppendLine($"\n{typeName} Mismatches for test0{fileIndex:00#}C.djvu:");

                DumpImageMismatchCore((byte*)oracle.DataPointer, (byte*)data.DataPointer, oracle.Width, oracle.Height,
                    oracle.BytesPerRow, data.BytesPerRow,
                    oracle.Border, data.Border,
                    PixelSize._8bpp, bitonal ? ChannelSize._1bit : ChannelSize._8bit, sb,
                    leadingPixels, maxMismatchPixels, maxBlocks, bytesPerLine, comparePadding);

                Console.Write(sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError dumping mismatch details for test0{fileIndex:00#}C.djvu: {ex.Message}");
            }
        }

        public static unsafe void DumpImageMismatch(
            Graphics.PixelMap oracle, Graphics.PixelMap data, int fileIndex,
            uint leadingPixels = 4,
            uint maxMismatchPixels = 32,
            uint maxBlocks = 1,
            uint bytesPerLine = 64)
        {
            try
            {
                string typeName = typeof(Graphics.PixelMap).FullName;

                if (oracle == null || data == null)
                {
                    Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: One or both image references are null.");
                    return;
                }
                
                if (oracle.Width == 0 || oracle.Height == 0)
                {
                    Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: Dimensions are {oracle.Width}x{oracle.Height}.");
                    return;
                }
                
                if (oracle.Data == null || data.Data == null)
                {
                    Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: One or both image Data buffers are null.");
                    return;
                }
                
                long oracleExpected = (long)oracle.Width * oracle.Height * Graphics.PixelMap.BytesPerPixel;
                long dataExpected = (long)data.Width * data.Height * Graphics.PixelMap.BytesPerPixel;
                
                if (oracle.Data.Length < oracleExpected || data.Data.Length < dataExpected)
                {
                    Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: One or both image Data buffers are undersized.");
                    return;
                }
                
                if (oracle.Width != data.Width || oracle.Height != data.Height)
                {
                    Console.WriteLine($"{typeName} Mismatch Details for test0{fileIndex:00#}C.djvu: Dimensions mismatch ({oracle.Width}x{oracle.Height} vs {data.Width}x{data.Height}).");
                    return;
                }

                StringBuilder sb = new StringBuilder(1024);
                sb.AppendLine($"\n{typeName} Mismatches for test0{fileIndex:00#}C.djvu:");
                
                fixed (sbyte* pOracle = oracle.Data)
                fixed (sbyte* pData = data.Data)
                {
                    // PixelMap has no physical native borders or padding offsets
                    // GetRowSize() returns pixel width. Stride requires byte width.
                    DumpImageMismatchCore((byte*)pOracle, (byte*)pData, oracle.Width, oracle.Height, 
                        oracle.Width * Graphics.PixelMap.BytesPerPixel, data.Width * Graphics.PixelMap.BytesPerPixel, 
                        0, 0, 
                        PixelSize._24bpp, ChannelSize._8bit, sb,
                        leadingPixels, maxMismatchPixels, maxBlocks, bytesPerLine, false);
                }
                    
                Console.Write(sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError dumping mismatch details for test0{fileIndex:00#}C.djvu: {ex.Message}");
            }
        }

        /// <summary>
        /// Dumps formatted hex and binary diagnostic blocks to the console when two raw image buffers mismatch.
        /// Extracts geometric topology dynamically from the provided byte length and width based on pixel bit-depth.
        /// </summary>
        /// <param name="pOracle">Pointer to the primary (Oracle) image buffer.</param>
        /// <param name="pData">Pointer to the secondary (Test) image buffer.</param>
        /// <param name="length">The total length of the image buffers in bytes.</param>
        /// <param name="width">The logical pixel width of the images.</param>
        /// <param name="fileIndex">The numeric test index mapped to the file name, for console logging.</param>
        /// <param name="mapType">The string label of the map type (e.g. 'Foreground', 'Background') for console logging.</param>
        /// <param name="pixelSize">The structural bit depth of a single pixel. Default is 24bpp.</param>
        /// <param name="channelSize">The bit depth of a single color channel. Default is 8bit.</param>
        /// <param name="leadingPixels">The number of matching, non-corrupted pixels to render before the detected mismatch to provide context.</param>
        /// <param name="maxMismatchPixels">The maximum continuous pixel limit to render in a single diagnostic block to prevent terminal flooding.</param>
        /// <param name="maxBlocks">The maximum number of disconnected mismatch patches to render.</param>
        /// <param name="bytesPerLine">The maximum visual byte width per rendered hex line.</param>
        public static unsafe void DumpImageMismatch(
            byte* pOracle, byte* pData, 
            int length, int width, 
            int fileIndex, string mapType, 
            PixelSize pixelSize = PixelSize._24bpp, 
            ChannelSize channelSize = ChannelSize._8bit,
            uint leadingPixels = 4,
            uint maxMismatchPixels = 32,
            uint maxBlocks = 1,
            uint bytesPerLine = 64,
            bool comparePadding = false)
        {
            try
            {
                if (pOracle == null || pData == null)
                {
                    Console.WriteLine($"\n{mapType} Mismatch Details for test0{fileIndex:00#}C.djvu: One or both image references are null.");
                    return;
                }
                
                int bitsPerPixel = (int)pixelSize;
                int bytesPerPixel = Math.Max(1, bitsPerPixel / 8);
                
                int stride = bitsPerPixel >= 8 ? (width * bytesPerPixel) : ((width * bitsPerPixel) / 8);
                int height = stride > 0 ? length / stride : 1;
                if (height == 0) height = 1;
                
                StringBuilder sb = new StringBuilder(1024);
                sb.AppendLine($"\n{mapType} Mismatches for test0{fileIndex:00#}C.djvu:");
                
                DumpImageMismatchCore(pOracle, pData, width, height, 
                    stride, stride, 
                    pixelSize, channelSize, sb,
                    leadingPixels, maxMismatchPixels, maxBlocks, bytesPerLine, comparePadding);
                    
                Console.Write(sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError dumping mismatch details for test0{fileIndex:00#}C.djvu: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the intermediate Sum of Absolute Differences (SAD) between two image buffers using highly optimized Vector256 (AVX2) instructions.
        /// This raw sum is passed to the parent method to compute the final normalized image difference ratio.
        /// </summary>
        /// <param name="scan0_1">Pointer to the start of the first image buffer in memory.</param>
        /// <param name="scan0_2">Pointer to the start of the second image buffer in memory.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The total number of rows (height) to process.</param>
        /// <param name="stride">The row stride in bytes, including any memory alignment padding.</param>
        /// <param name="pixelSize">The size of a pixel in bits.</param>
        /// <param name="channelSize">The size of a color channel in bits.</param>
        /// <returns>A <see cref="double"/> representing the final normalized fractional image difference ratio.</returns>
        /// <exception cref="PlatformNotSupportedException">Thrown when the CPU does not support AVX2 hardware acceleration.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="width"/> produces a byte width less than 32 bytes, which would cause an unsafe memory overread.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static unsafe double ImageDiffVector256(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride, PixelSize pixelSize = PixelSize._24bpp, ChannelSize channelSize = ChannelSize._8bit)
        {
            return ImageDiffVector256(scan0_1, scan0_2, width, height, stride, stride, pixelSize, channelSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static unsafe double ImageDiffVector256(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride1, int stride2, PixelSize pixelSize = PixelSize._24bpp, ChannelSize channelSize = ChannelSize._8bit)
        {
            if (scan0_1 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_1), "First image buffer pointer cannot be null.");
            }

            if (scan0_2 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_2), "Second image buffer pointer cannot be null.");
            }

            if (width == 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            }

            if (height == 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            }

            if ((byte)pixelSize == 0 || (byte)pixelSize % 8 != 0)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            }

            if (channelSize != ChannelSize._8bit)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));
            }

            ulong widthBytes = (ulong)width * pixelSize.AsUintBytes();
            if ((ulong)Math.Abs((long)stride1) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride1 must be large enough to contain the width bytes.", nameof(stride1));
            }

            if ((ulong)Math.Abs((long)stride2) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride2 must be large enough to contain the width bytes.", nameof(stride2));
            }

            if (!Avx2.IsSupported)
            {
                throw new PlatformNotSupportedException("AVX2 hardware acceleration is not supported on this platform.");
            }
            else if (widthBytes < 32)
            {
                throw new DjvuArgumentOutOfRangeException(nameof(width), widthBytes, "Buffer width must be at least 32 bytes to fill a Vector256 (AVX2) register.");
            }

            ulong vectorBound = widthBytes >= 32 ? widthBytes - 32 : 0;
            int tailShift = (int)((32 - (widthBytes % 32)) % 32);

            Vector256<ulong> resultVecU = Vector256<ulong>.Zero;

            Vector256<sbyte> seq256 = Vector256.Create(
                (sbyte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31);
            Vector256<sbyte> threshold = Vector256.Create((sbyte)(tailShift - 1));
            Vector256<byte> tailMask = Avx2.CompareGreaterThan(seq256, threshold).AsByte();

            for (uint i = 0; i < height; i++)
            {
                byte* p1 = scan0_1 + ((long)i * stride1);
                byte* p2 = scan0_2 + ((long)i * stride2);
                ulong x = 0;

                while (x + 96 <= vectorBound)
                {
                    Vector256<byte> r11 = Avx2.LoadDquVector256(p1);
                    Vector256<byte> r21 = Avx2.LoadDquVector256(p2);
                    Vector256<byte> r12 = Avx2.LoadDquVector256(p1 + 32);
                    Vector256<byte> r22 = Avx2.LoadDquVector256(p2 + 32);
                    Vector256<ushort> diff1 = Avx2.SumAbsoluteDifferences(r11, r21);
                    Vector256<ushort> diff2 = Avx2.SumAbsoluteDifferences(r12, r22);

                    Vector256<byte> r13 = Avx2.LoadDquVector256(p1 + 64);
                    Vector256<byte> r23 = Avx2.LoadDquVector256(p2 + 64);
                    Vector256<byte> r14 = Avx2.LoadDquVector256(p1 + 96);
                    Vector256<byte> r24 = Avx2.LoadDquVector256(p2 + 96);
                    Vector256<ushort> diff3 = Avx2.SumAbsoluteDifferences(r13, r23);
                    Vector256<ushort> diff4 = Avx2.SumAbsoluteDifferences(r14, r24);

                    Vector256<ulong> diff12 = Avx2.Add(diff1.AsUInt64(), diff2.AsUInt64());
                    Vector256<ulong> diff34 = Avx2.Add(diff3.AsUInt64(), diff4.AsUInt64());
                    resultVecU = Avx2.Add(resultVecU, Avx2.Add(diff12, diff34));

                    p1 += 128;
                    p2 += 128;
                    x += 128;
                }

                while (x <= vectorBound)
                {
                    Vector256<byte> r1 = Avx2.LoadDquVector256(p1);
                    Vector256<byte> r2 = Avx2.LoadDquVector256(p2);
                    Vector256<ushort> diff = Avx2.SumAbsoluteDifferences(r1, r2);

                    resultVecU = Avx2.Add(resultVecU, diff.AsUInt64());

                    p1 += 32;
                    p2 += 32;
                    x += 32;
                }

                if (x < widthBytes)
                {
                    Vector256<byte> r1 = Avx2.LoadDquVector256(p1 - tailShift);
                    Vector256<byte> r2 = Avx2.LoadDquVector256(p2 - tailShift);

                    r1 = Avx2.And(r1, tailMask);
                    r2 = Avx2.And(r2, tailMask);

                    Vector256<ushort> diff = Avx2.SumAbsoluteDifferences(r1, r2);
                    resultVecU = Avx2.Add(resultVecU, diff.AsUInt64());
                }
            }

            double result = Vector256.Sum(resultVecU);

            double maxChannelValue = (1L << channelSize.AsIntBits()) - 1;
            return result / ((double)width * height * (pixelSize.AsDoubleBits() / channelSize.AsIntBits()) * maxChannelValue);
        }

        /// <summary>
        /// Calculates the intermediate Sum of Absolute Differences (SAD) between two image buffers using highly optimized Vector128 (SSSE3/AdvSimd) instructions.
        /// This raw sum is passed to the parent method to compute the final normalized image difference ratio.
        /// </summary>
        /// <param name="scan0_1">Pointer to the start of the first image buffer in memory.</param>
        /// <param name="scan0_2">Pointer to the start of the second image buffer in memory.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The total number of rows (height) to process.</param>
        /// <param name="stride">The row stride in bytes, including any memory alignment padding.</param>
        /// <param name="pixelSize">The size of a pixel in bits.</param>
        /// <param name="channelSize">The size of a color channel in bits.</param>
        /// <returns>A <see cref="double"/> representing the final normalized fractional image difference ratio.</returns>
        /// <exception cref="PlatformNotSupportedException">Thrown when the CPU does not support Vector128 hardware acceleration.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="width"/> produces a byte width less than 16 bytes, which would cause an unsafe memory overread.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static unsafe double ImageDiffVector128(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride, PixelSize pixelSize = PixelSize._24bpp, ChannelSize channelSize = ChannelSize._8bit)
        {
            return ImageDiffVector128(scan0_1, scan0_2, width, height, stride, stride, pixelSize, channelSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static unsafe double ImageDiffVector128(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride1, int stride2, PixelSize pixelSize = PixelSize._24bpp, ChannelSize channelSize = ChannelSize._8bit)
        {
            if (scan0_1 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_1), "First image buffer pointer cannot be null.");
            }

            if (scan0_2 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_2), "Second image buffer pointer cannot be null.");
            }

            if (width == 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            }

            if (height == 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            }

            if ((byte)pixelSize == 0 || (byte)pixelSize % 8 != 0)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            }

            if (channelSize != ChannelSize._8bit)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));
            }

            ulong widthBytes = (ulong)width * pixelSize.AsUintBytes();
            if ((ulong)Math.Abs((long)stride1) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride1 must be large enough to contain the width bytes.", nameof(stride1));
            }

            if ((ulong)Math.Abs((long)stride2) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride2 must be large enough to contain the width bytes.", nameof(stride2));
            }

            if (!Vector128.IsHardwareAccelerated)
            {
                throw new PlatformNotSupportedException("Vector128 hardware acceleration is not supported on this platform.");
            }
            else if (widthBytes < 16)
            {
                throw new DjvuArgumentOutOfRangeException(nameof(width), widthBytes, "Buffer width must be at least 16 bytes to fill a Vector128 register.");
            }

            double result = 0.0;
            ulong vectorBound = widthBytes >= 16 ? widthBytes - 16 : 0;
            int tailShift = (int)((16 - (widthBytes % 16)) % 16);

            Vector128<ulong> imageAccum64L = Vector128<ulong>.Zero;
            Vector128<ulong> imageAccum64H = Vector128<ulong>.Zero;

            Vector128<sbyte> seq128 = Vector128.Create((sbyte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
            Vector128<sbyte> threshold = Vector128.Create((sbyte)(tailShift - 1));
            Vector128<byte> tailMask = Vector128.GreaterThan(seq128, threshold).AsByte();

            for (uint i = 0; i < height; i++)
            {
                byte* p1 = scan0_1 + ((long)i * stride1);
                byte* p2 = scan0_2 + ((long)i * stride2);
                ulong x = 0;

                while (x <= vectorBound)
                {
                    ulong batchLimit = Math.Min(x + (255 * 16), vectorBound + 16);
                    if (batchLimit > vectorBound) batchLimit = vectorBound + 1; // Ensure we only run up to vectorBound

                    Vector128<ushort> batchAccum16L = Vector128<ushort>.Zero;
                    Vector128<ushort> batchAccum16H = Vector128<ushort>.Zero;

                    while (x < batchLimit)
                    {
                        Vector128<byte> r1 = Vector128.Load(p1);
                        Vector128<byte> r2 = Vector128.Load(p2);

                        Vector128<byte> diff = Vector128.Subtract(Vector128.Max(r1, r2), Vector128.Min(r1, r2));

                        batchAccum16L = Vector128.Add(batchAccum16L, Vector128.WidenLower(diff));
                        batchAccum16H = Vector128.Add(batchAccum16H, Vector128.WidenUpper(diff));

                        p1 += 16;
                        p2 += 16;
                        x += 16;
                    }

                    Vector128<uint> batch32L = Vector128.Add(Vector128.WidenLower(batchAccum16L), Vector128.WidenUpper(batchAccum16L));
                    Vector128<uint> batch32H = Vector128.Add(Vector128.WidenLower(batchAccum16H), Vector128.WidenUpper(batchAccum16H));

                    imageAccum64L = Vector128.Add(imageAccum64L, Vector128.Add(Vector128.WidenLower(batch32L), Vector128.WidenUpper(batch32L)));
                    imageAccum64H = Vector128.Add(imageAccum64H, Vector128.Add(Vector128.WidenLower(batch32H), Vector128.WidenUpper(batch32H)));
                }

                if (x < widthBytes)
                {
                    Vector128<byte> r1 = Vector128.Load(p1 - tailShift);
                    Vector128<byte> r2 = Vector128.Load(p2 - tailShift);

                    r1 = Vector128.BitwiseAnd(r1, tailMask);
                    r2 = Vector128.BitwiseAnd(r2, tailMask);

                    Vector128<byte> diff = Vector128.Subtract(Vector128.Max(r1, r2), Vector128.Min(r1, r2));

                    Vector128<ushort> sum16L = Vector128.WidenLower(diff);
                    Vector128<ushort> sum16H = Vector128.WidenUpper(diff);

                    Vector128<uint> sum32L = Vector128.Add(Vector128.WidenLower(sum16L), Vector128.WidenUpper(sum16L));
                    Vector128<uint> sum32H = Vector128.Add(Vector128.WidenLower(sum16H), Vector128.WidenUpper(sum16H));

                    imageAccum64L = Vector128.Add(imageAccum64L, Vector128.Add(Vector128.WidenLower(sum32L), Vector128.WidenUpper(sum32L)));
                    imageAccum64H = Vector128.Add(imageAccum64H, Vector128.Add(Vector128.WidenLower(sum32H), Vector128.WidenUpper(sum32H)));
                }
            }

            Vector128<ulong> finalSum64 = Vector128.Add(imageAccum64L, imageAccum64H);
            result += Vector128.Sum(finalSum64);

            double maxChannelValue = (1L << channelSize.AsIntBits()) - 1;
            return result / ((double)width * height * (pixelSize.AsDoubleBits() / channelSize.AsIntBits()) * maxChannelValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static unsafe double ImageDiffParallel256(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride, ParallelOptions options, PixelSize pixelSize = PixelSize._24bpp, ChannelSize channelSize = ChannelSize._8bit)
        {
            return ImageDiffParallel256(scan0_1, scan0_2, width, height, stride, stride, options, pixelSize, channelSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static unsafe double ImageDiffParallel256(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride1, int stride2, ParallelOptions options, PixelSize pixelSize = PixelSize._24bpp, ChannelSize channelSize = ChannelSize._8bit)
        {
            if (scan0_1 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_1), "First image buffer pointer cannot be null.");
            }

            if (scan0_2 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_2), "Second image buffer pointer cannot be null.");
            }

            if (width == 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            }

            if (height == 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            }

            if ((byte)pixelSize == 0 || (byte)pixelSize % 8 != 0)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            }

            if (channelSize != ChannelSize._8bit)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));
            }

            ulong widthBytes = (ulong)width * pixelSize.AsUintBytes();
            if ((ulong)Math.Abs((long)stride1) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride1 must be large enough to contain the width bytes.", nameof(stride1));
            }

            if ((ulong)Math.Abs((long)stride2) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride2 must be large enough to contain the width bytes.", nameof(stride2));
            }

            if (!Avx2.IsSupported)
            {
                throw new PlatformNotSupportedException("AVX2 hardware acceleration is not supported on this platform.");
            }

            if (widthBytes < 32)
            {
                throw new DjvuArgumentOutOfRangeException(nameof(width), widthBytes, "Buffer width must be at least 32 bytes to fill a Vector256 (AVX2) register.");
            }

            object resultLock = new object();
            ulong totalResult = 0;

            ulong vectorBound = widthBytes >= 32 ? widthBytes - 32 : 0;
            int tailShift = (int)((32 - (widthBytes % 32)) % 32);

            Vector256<sbyte> seq256 = Vector256.Create(
                (sbyte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31);
            Vector256<sbyte> threshold = Vector256.Create((sbyte)(tailShift - 1));
            Vector256<byte> tailMask = Avx2.CompareGreaterThan(seq256, threshold).AsByte();

            Parallel.For(0L, (long)height, options,
                () => Vector256<ulong>.Zero,
                (long y, ParallelLoopState loopState, Vector256<ulong> localAccumVec) =>
                {
                    byte* p1 = scan0_1 + (y * stride1);
                    byte* p2 = scan0_2 + (y * stride2);
                    ulong x = 0;
                    Vector256<ulong> resultVecU = Vector256<ulong>.Zero;

                    while (x + 96 <= vectorBound)
                    {
                        Vector256<byte> r11 = Avx2.LoadDquVector256(p1);
                        Vector256<byte> r21 = Avx2.LoadDquVector256(p2);
                        Vector256<byte> r12 = Avx2.LoadDquVector256(p1 + 32);
                        Vector256<byte> r22 = Avx2.LoadDquVector256(p2 + 32);
                        Vector256<ushort> diff1 = Avx2.SumAbsoluteDifferences(r11, r21);
                        Vector256<ushort> diff2 = Avx2.SumAbsoluteDifferences(r12, r22);

                        Vector256<byte> r13 = Avx2.LoadDquVector256(p1 + 64);
                        Vector256<byte> r23 = Avx2.LoadDquVector256(p2 + 64);
                        Vector256<byte> r14 = Avx2.LoadDquVector256(p1 + 96);
                        Vector256<byte> r24 = Avx2.LoadDquVector256(p2 + 96);
                        Vector256<ushort> diff3 = Avx2.SumAbsoluteDifferences(r13, r23);
                        Vector256<ushort> diff4 = Avx2.SumAbsoluteDifferences(r14, r24);

                        Vector256<ulong> diff12 = Avx2.Add(diff1.AsUInt64(), diff2.AsUInt64());
                        Vector256<ulong> diff34 = Avx2.Add(diff3.AsUInt64(), diff4.AsUInt64());
                        resultVecU = Avx2.Add(resultVecU, Avx2.Add(diff12, diff34));

                        p1 += 128;
                        p2 += 128;
                        x += 128;
                    }

                    while (x <= vectorBound)
                    {
                        Vector256<byte> r1 = Avx2.LoadDquVector256(p1);
                        Vector256<byte> r2 = Avx2.LoadDquVector256(p2);
                        Vector256<ushort> diff = Avx2.SumAbsoluteDifferences(r1, r2);
                        resultVecU = Avx2.Add(resultVecU, diff.AsUInt64());
                        p1 += 32;
                        p2 += 32;
                        x += 32;
                    }

                    if (x < widthBytes)
                    {
                        Vector256<byte> r1 = Avx2.LoadDquVector256(p1 - tailShift);
                        Vector256<byte> r2 = Avx2.LoadDquVector256(p2 - tailShift);
                        r1 = Avx2.And(r1, tailMask);
                        r2 = Avx2.And(r2, tailMask);
                        Vector256<ushort> diff = Avx2.SumAbsoluteDifferences(r1, r2);
                        resultVecU = Avx2.Add(resultVecU, diff.AsUInt64());
                    }

                    return Avx2.Add(localAccumVec, resultVecU);
                },
                localAccumVec =>
                {
                    ulong rowSum = (ulong)Vector256.Sum(localAccumVec);
                    lock (resultLock)
                    {
                        totalResult += rowSum;
                    }
                }
            );

            double maxChannelValue = (1L << channelSize.AsIntBits()) - 1;
            return (double)totalResult / ((double)width * height * (pixelSize.AsDoubleBits() / channelSize.AsIntBits()) * maxChannelValue);
                }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static unsafe double ImageDiffParallel128(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride, ParallelOptions options, PixelSize pixelSize = PixelSize._24bpp, ChannelSize channelSize = ChannelSize._8bit)
        {
            return ImageDiffParallel128(scan0_1, scan0_2, width, height, stride, stride, options, pixelSize, channelSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static unsafe double ImageDiffParallel128(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride1, int stride2, ParallelOptions options, PixelSize pixelSize = PixelSize._24bpp, ChannelSize channelSize = ChannelSize._8bit)
        {
            if (scan0_1 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_1), "First image buffer pointer cannot be null.");
            }

            if (scan0_2 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_2), "Second image buffer pointer cannot be null.");
            }

            if (width == 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            }

            if (height == 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            }

            if ((byte)pixelSize == 0 || (byte)pixelSize % 8 != 0)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            }

            if (channelSize != ChannelSize._8bit)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));
            }

            ulong widthBytes = (ulong)width * pixelSize.AsUintBytes();
            if ((ulong)Math.Abs((long)stride1) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride1 must be large enough to contain the width bytes.", nameof(stride1));
            }

            if ((ulong)Math.Abs((long)stride2) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride2 must be large enough to contain the width bytes.", nameof(stride2));
            }

            if (!Vector128.IsHardwareAccelerated)
            {
                throw new PlatformNotSupportedException("Vector128 hardware acceleration is not supported on this platform.");
            }

            if (widthBytes < 16)
            {
                throw new DjvuArgumentOutOfRangeException(nameof(width), widthBytes, "Buffer width must be at least 16 bytes to fill a Vector128 register.");
            }

            object resultLock = new object();
            ulong totalResult = 0;

            ulong vectorBound = widthBytes >= 16 ? widthBytes - 16 : 0;
            int tailShift = (int)((16 - (widthBytes % 16)) % 16);

            Vector128<sbyte> seq128 = Vector128.Create((sbyte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
            Vector128<sbyte> threshold = Vector128.Create((sbyte)(tailShift - 1));
            Vector128<byte> tailMask = Vector128.GreaterThan(seq128, threshold).AsByte();

            Parallel.For(0L, (long)height, options,
                () => (Vector128<ulong>.Zero, Vector128<ulong>.Zero),
                (long y, ParallelLoopState loopState, (Vector128<ulong> Item1, Vector128<ulong> Item2) localAccumTuple) =>
                {
                    byte* p1 = scan0_1 + (y * stride1);
                    byte* p2 = scan0_2 + (y * stride2);
                    ulong x = 0;

                    Vector128<ulong> imageAccum64L = Vector128<ulong>.Zero;
                    Vector128<ulong> imageAccum64H = Vector128<ulong>.Zero;

                    while (x <= vectorBound)
                    {
                        ulong batchLimit = Math.Min(x + (255 * 16), vectorBound + 16);
                        if (batchLimit > vectorBound) batchLimit = vectorBound + 1;

                        Vector128<ushort> batchAccum16L = Vector128<ushort>.Zero;
                        Vector128<ushort> batchAccum16H = Vector128<ushort>.Zero;

                        while (x < batchLimit)
                        {
                            Vector128<byte> r1 = Vector128.Load(p1);
                            Vector128<byte> r2 = Vector128.Load(p2);
                            Vector128<byte> diff = Vector128.Subtract(Vector128.Max(r1, r2), Vector128.Min(r1, r2));

                            batchAccum16L = Vector128.Add(batchAccum16L, Vector128.WidenLower(diff));
                            batchAccum16H = Vector128.Add(batchAccum16H, Vector128.WidenUpper(diff));

                            p1 += 16;
                            p2 += 16;
                            x += 16;
                        }

                        Vector128<uint> batch32L = Vector128.Add(Vector128.WidenLower(batchAccum16L), Vector128.WidenUpper(batchAccum16L));
                        Vector128<uint> batch32H = Vector128.Add(Vector128.WidenLower(batchAccum16H), Vector128.WidenUpper(batchAccum16H));

                        imageAccum64L = Vector128.Add(imageAccum64L, Vector128.Add(Vector128.WidenLower(batch32L), Vector128.WidenUpper(batch32L)));
                        imageAccum64H = Vector128.Add(imageAccum64H, Vector128.Add(Vector128.WidenLower(batch32H), Vector128.WidenUpper(batch32H)));
                    }

                    if (x < widthBytes)
                    {
                        Vector128<byte> r1 = Vector128.Load(p1 - tailShift);
                        Vector128<byte> r2 = Vector128.Load(p2 - tailShift);

                        r1 = Vector128.BitwiseAnd(r1, tailMask);
                        r2 = Vector128.BitwiseAnd(r2, tailMask);

                        Vector128<byte> diff = Vector128.Subtract(Vector128.Max(r1, r2), Vector128.Min(r1, r2));

                        Vector128<ushort> sum16L = Vector128.WidenLower(diff);
                        Vector128<ushort> sum16H = Vector128.WidenUpper(diff);

                        Vector128<uint> sum32L = Vector128.Add(Vector128.WidenLower(sum16L), Vector128.WidenUpper(sum16L));
                        Vector128<uint> sum32H = Vector128.Add(Vector128.WidenLower(sum16H), Vector128.WidenUpper(sum16H));

                        imageAccum64L = Vector128.Add(imageAccum64L, Vector128.Add(Vector128.WidenLower(sum32L), Vector128.WidenUpper(sum32L)));
                        imageAccum64H = Vector128.Add(imageAccum64H, Vector128.Add(Vector128.WidenLower(sum32H), Vector128.WidenUpper(sum32H)));
                    }

                    return (Vector128.Add(localAccumTuple.Item1, imageAccum64L), Vector128.Add(localAccumTuple.Item2, imageAccum64H));
                },
                localAccumTuple =>
                {
                    Vector128<ulong> finalSum64 = Vector128.Add(localAccumTuple.Item1, localAccumTuple.Item2);
                    ulong rowSum = (ulong)Vector128.Sum(finalSum64);
                    lock (resultLock)
                    {
                        totalResult += rowSum;
                    }
                }
            );

            double maxChannelValue = (1L << channelSize.AsIntBits()) - 1;
            return (double)totalResult / ((double)width * height * (pixelSize.AsDoubleBits() / channelSize.AsIntBits()) * maxChannelValue);
        }

        /// <summary>
        /// A highly optimized, branchless scalar fallback for calculating aggregate absolute image differences.
        /// </summary>
        /// <param name="scan0_1">Pointer to the start of the first image buffer in memory.</param>
        /// <param name="scan0_2">Pointer to the start of the second image buffer in memory.</param>
        /// <param name="widthBytes">The exact number of visible bytes per row, excluding any stride padding.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <param name="stride">The row stride in bytes, including any memory alignment padding.</param>
        /// <returns>A <see cref="double"/> representing the raw sum of all absolute byte differences across the entire image.</returns>
        /// <remarks>
        /// <para>
        /// This method operates strictly on the calculated visible bytes rather than the full row stride to guarantee
        /// that memory row-padding bytes are never processed. It iterates over the continuous byte streams linearly,
        /// natively supporting various pixel formats without format-specific conditional branching.
        /// </para>
        /// <para>
        /// Floating-point conversions are deferred until the very end of the calculation. The inner loop uses an integer
        /// accumulator to track byte differences, avoiding the multi-cycle latency of floating-point addition in the
        /// tight iteration path.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong ImageBinaryDiffSimdFallback(byte* scan0_1, byte* scan0_2, uint widthBytes, uint height, int stride1, int stride2)
        {
            ulong result = 0;
            for (ulong i = 0; i < height; i++)
            {
                byte* p1 = scan0_1 + ((long)i * stride1);
                byte* p2 = scan0_2 + ((long)i * stride2);

                ulong wb = 0;
                // Unrolled calculation explicitly reading 8 individual bytes
                // The .NET JIT optimizes this extremely well (often into SIMD) while avoiding the shifting bugs
                for (; wb + 8 <= widthBytes; wb += 8)
                {
                    int d0 = p1[0] - p2[0];
                    int d1 = p1[1] - p2[1];
                    int d2 = p1[2] - p2[2];
                    int d3 = p1[3] - p2[3];
                    int d4 = p1[4] - p2[4];
                    int d5 = p1[5] - p2[5];
                    int d6 = p1[6] - p2[6];
                    int d7 = p1[7] - p2[7];

                    uint a0 = (uint)((d0 ^ (d0 >> 31)) - (d0 >> 31));
                    uint a1 = (uint)((d1 ^ (d1 >> 31)) - (d1 >> 31));
                    uint a2 = (uint)((d2 ^ (d2 >> 31)) - (d2 >> 31));
                    uint a3 = (uint)((d3 ^ (d3 >> 31)) - (d3 >> 31));
                    uint a4 = (uint)((d4 ^ (d4 >> 31)) - (d4 >> 31));
                    uint a5 = (uint)((d5 ^ (d5 >> 31)) - (d5 >> 31));
                    uint a6 = (uint)((d6 ^ (d6 >> 31)) - (d6 >> 31));
                    uint a7 = (uint)((d7 ^ (d7 >> 31)) - (d7 >> 31));

                    result += a0 + a1 + a2 + a3 + a4 + a5 + a6 + a7;

                    p1 += 8;
                    p2 += 8;
                }

                for (; wb < widthBytes; wb++)
                {
                    int d = p1[0] - p2[0];
                    result += (uint)((d ^ (d >> 31)) - (d >> 31));
                    p1++;
                    p2++;
                }
            }
            return result;
        }

        /// <summary>
        /// Calculates the final normalized image difference ratio from the raw sum of absolute byte differences.
        /// </summary>
        /// <param name="scan0_1"></param>
        /// <param name="scan0_2"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="stride">The row stride in bytes.</param>
        /// <param name="pixelSize"></param>
        /// <param name="channelSize"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe double ImageBinaryDiffScalar(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride, PixelSize pixelSize = PixelSize._24bpp, ChannelSize channelSize = ChannelSize._8bit)
        {
            return ImageBinaryDiffScalar(scan0_1, scan0_2, width, height, stride, stride, pixelSize, channelSize);
        }

        /// <summary>
        /// Calculates the difference between two image buffers using a scalar fallback loop.
        /// </summary>
        /// <param name="scan0_1">Pointer to the start of the first image buffer.</param>
        /// <param name="scan0_2">Pointer to the start of the second image buffer.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <param name="stride1">The row stride of the first image (in bytes). This parameter serves a dual function: 1) its magnitude accounts for row memory padding, and 2) its sign accounts for the direction of processing (a negative reverse stride allows comparing images existing in different coordinate spaces, e.g., top-down vs bottom-up). Both functions combine naturally.</param>
        /// <param name="stride2">The row stride of the second image (in bytes). It shares the exact same dual functionality as stride1, allowing fully independent memory layout and coordinate space comparisons. If 0, it falls back to stride1.</param>
        /// <param name="pixelSize">The total size of a single pixel in bits.</param>
        /// <param name="channelSize">The size of a single color channel in bits.</param>
        /// <returns>A double representing the average pixel difference.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe double ImageBinaryDiffScalar(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride1, int stride2, PixelSize pixelSize = PixelSize._24bpp, ChannelSize channelSize = ChannelSize._8bit)
        {
            if (scan0_1 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_1), "First image buffer pointer cannot be null.");
            }

            if (scan0_2 == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_2), "Second image buffer pointer cannot be null.");
            }

            if (width == 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            }

            if (height == 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            }

            if ((byte)pixelSize == 0 || (byte)pixelSize % 8 != 0)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            }

            if (channelSize != ChannelSize._8bit)
            {
                DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));
            }

            ulong widthBytes = (ulong)width * pixelSize.AsUintBytes();
            if ((ulong)Math.Abs((long)stride1) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride1 must be large enough to contain the width bytes.", nameof(stride1));
            }

            if ((ulong)Math.Abs((long)stride2) < widthBytes)
            {
                DjvuExceptionUtil.ThrowArgument("Stride2 must be large enough to contain the width bytes.", nameof(stride2));
            }

            ulong result = ImageBinaryDiffSimdFallback(scan0_1, scan0_2, (uint)widthBytes, height, stride1, stride2);
            double maxChannelValue = (1L << channelSize.AsIntBits()) - 1;
            return (double)result / (width * height * (pixelSize.AsDoubleBits() / channelSize.AsIntBits()) * maxChannelValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe double GetPixelDiff(byte* pixel1, byte* pixel2)
        {
            float r1 = (float)(*pixel1);
            float g1 = (float)(*(++pixel1));
            float b1 = (float)(*(++pixel1));

            float r2 = (float)(*pixel2);
            float g2 = (float)(*(++pixel2));
            float b2 = (float)(*(++pixel2));

#if NETCOREAPP
            return MathF.Abs(r1 - r2) + MathF.Abs(g1 - g2) + MathF.Abs(b1 - b2);
#else
            return Math.Abs(r1 - r2) + Math.Abs(g1 - g2) + Math.Abs(b1 - b2);
#endif
        }

        public static bool CompareImages(SysBitmap image1, SysBitmap image2)
        {
            bool pixelFormatMismatch = false;
            bool result = IsImageBinaryComparable(image1, image2, out pixelFormatMismatch);

            if (result)
            {
                Rectangle rect = new Rectangle(0, 0, image1.Width, image1.Height);
                BitmapData img1 = image1.LockBits(rect, ImageLockMode.ReadOnly, image1.PixelFormat);
                BitmapData img2 = image2.LockBits(rect, ImageLockMode.ReadOnly, image1.PixelFormat);

                result = CompareImagesInternal(img1, img2);

                image1.UnlockBits(img1);
                image2.UnlockBits(img2);
            }

            return result;
        }

        public static bool CompareImages(BitmapData image1, BitmapData image2)
        {
            if (image1.PixelFormat != image2.PixelFormat)
            {
                return false;
            }

            if (image1.Width != image2.Width || image1.Height != image2.Height)
            {
                return false;
            }

            return CompareImagesInternal(image1, image2);
        }

        private static bool CompareImagesInternal(BitmapData image1, BitmapData image2)
        {
            if (Environment.Is64BitProcess)
            {
                return CompareImages64(image1, image2);
            }
            else
            {
                return CompareImages32(image1, image2);
            }
        }

        private static bool CompareImages64(BitmapData image1, BitmapData image2)
        {
            int pixelSize = Image.GetPixelFormatSize(image1.PixelFormat);

            unsafe
            {
                ulong rowSize = (ulong) (((uint)pixelSize / 8) * image1.Width);
                ulong rowSizeWithPadding = (ulong) image1.Stride;
                ulong* longCheckSize;

                ulong* lp, lpRow = (ulong*)image1.Scan0;
                ulong* rp, rpRow = (ulong*)image2.Scan0;

                for (uint i = 0; i < image1.Height; i++)
                {
                    lp = (ulong*)(((byte*) lpRow) + (i * rowSizeWithPadding));
                    rp = (ulong*)(((byte*) rpRow) + (i * rowSizeWithPadding));
                    longCheckSize = (ulong*) (((byte*) lp) + rowSize);

                    for (; lp < longCheckSize; lp++, rp++)
                    {
                        if (*lp != *rp)
                        {
                            return false;
                        }
                    }

                    int remainder = 0;

                    if ((remainder = (int) (longCheckSize - lp)) > 0)
                    {
                        byte* lb = (byte*)lp;
                        byte* rb = (byte*)rp;

                        for (int ii = 0; ii < remainder; ii++, lb++, rb++)
                        {
                            if (*lb != *rb)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private static bool CompareImages32(BitmapData image1, BitmapData image2)
        {
            int pixelSize = Image.GetPixelFormatSize(image1.PixelFormat);

            unsafe
            {
                uint rowSize = (uint)(((uint)pixelSize / 8) * image1.Width);
                uint rowSizeWithPadding = (uint)image1.Stride;
                uint* longCheckSize;

                uint* lp, lpRow = (uint*)image1.Scan0;
                uint* rp, rpRow = (uint*)image2.Scan0;

                for (uint i = 0; i < image1.Height; i++)
                {
                    lp = (uint*)(((byte*)lpRow) + (i * rowSizeWithPadding));
                    rp = (uint*)(((byte*)rpRow) + (i * rowSizeWithPadding));
                    longCheckSize = (uint*)(((byte*)lp) + rowSize);

                    for (; lp < longCheckSize; lp++, rp++)
                    {
                        if (*lp != *rp)
                        {
                            return false;
                        }
                    }

                    int remainder = 0;

                    if ((remainder = (int)(longCheckSize - lp)) > 0)
                    {
                        byte* lb = (byte*)lp;
                        byte* rb = (byte*)rp;

                        for (int ii = 0; ii < remainder; ii++, lb++, rb++)
                        {
                            if (*lb != *rb)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        public static SysBitmap InvertColor(SysBitmap source)
        {
            ColorMatrix colorMatrix = new ColorMatrix(
                new float[][]
                {
                    new float[] {-1, 0,  0,  0,  0},
                    new float[] {0, -1,  0,  0,  0},
                    new float[] {0,  0, -1,  0,  0},
                    new float[] {0,  0,  0,  1,  0},
                    new float[] {1,  1,  1,  0,  1}
                });

            return TransformBitmap(source, colorMatrix);
        }

        public static SysBitmap TransformBitmap(SysBitmap source, ColorMatrix colorMatrix)
        {
            SysBitmap result = new SysBitmap(source.Width, source.Height, source.PixelFormat);

            using (SysGraphics g = SysGraphics.FromImage(result))
            {
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(colorMatrix);
                    Rectangle rect = new Rectangle(0, 0, source.Width, source.Height);
                    g.DrawImage(source, rect, 0, 0, source.Width, source.Height,
                        GraphicsUnit.Pixel, attributes);
                }
            }
            return result;
        }

        private static readonly Lazy<Dictionary<string, string>> _sjbzToDjbzMap = new Lazy<Dictionary<string, string>>(() =>
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (object[] row in GetJB2ImageTestData(null, null, TestCoverage.All, 1))
            {
                string djbz = (string)row[0];
                string sjbz = (string)row[1];
                if (sjbz != null)
                {
                    string key = Path.GetFileName(sjbz);
                    if (!map.ContainsKey(key))
                        map.Add(key, djbz);
                }
            }
            return map;
        });

        public static string GetDjbzForSjbz(string sjbzFileName)
        {
            if (string.IsNullOrWhiteSpace(sjbzFileName)) return null;
            string key = Path.GetFileName(sjbzFileName);
            return _sjbzToDjbzMap.Value.TryGetValue(key, out string djbz) ? djbz : null;
        }

        public static IEnumerable<object[]> GetJB2ImageTestData(int[] skipDocs = null, string[] skipChunks = null, TestCoverage coverage = TestCoverage.All, int step = 1)
        {
            string mapPath = Path.Combine(ArtifactsDataPath, "extracted", "jb2_chunk_map.json");
            if (!File.Exists(mapPath)) yield break;

            HashSet<string> seenDjbz = new HashSet<string>();

            string json = File.ReadAllText(mapPath, new UTF8Encoding(false));
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                int index = 0;
                foreach (JsonElement element in doc.RootElement.EnumerateArray())
                {
                    int docIndex = element[0].GetInt32();
                    if (skipDocs != null && skipDocs.Contains(docIndex))
                        continue;

                    string djbzName = element[2].ValueKind == JsonValueKind.Null ? null : element[2].GetString();
                    string sjbzName = element[3].GetString();

                    if (skipChunks != null && skipChunks.Contains(sjbzName))
                        continue;

                    if (coverage.HasFlag(TestCoverage.UniqueOnly) && djbzName != null)
                    {
                        if (!seenDjbz.Add(djbzName))
                            continue;
                    }

                    string extractedDjbzName = djbzName != null ? Path.Combine("extracted", djbzName.Replace(@"\", @"")) : null;
                    sjbzName = (coverage.HasFlag(TestCoverage.DjbzNotNull) && djbzName == null) ? null : Path.Combine("extracted", sjbzName.Replace(@"\", @""));

                    if (sjbzName == null)
                        continue;

                    index++;
                    if (index % step != 0)
                        continue;

                    yield return new object[]
                    {
                        // Get names and sanitize Json escapes if present
                        extractedDjbzName, sjbzName
                    };
                }
            }
        }

        public static IEnumerable<object[]> GetJB2DictionaryTestData(int[] skipDocs = null, string[] skipChunks = null)
        {
            string mapPath = Path.Combine(ArtifactsDataPath, "extracted", "jb2_chunk_map.json");
            if (!File.Exists(mapPath)) yield break;

            HashSet<string> seen = new HashSet<string>();

            string json = File.ReadAllText(mapPath, new UTF8Encoding(false));
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                foreach (JsonElement element in doc.RootElement.EnumerateArray())
                {
                    int docIndex = element[0].GetInt32();
                    if (skipDocs != null && skipDocs.Contains(docIndex))
                        continue;

                    string djbzName = element[2].ValueKind == JsonValueKind.Null ? null : element[2].GetString();

                    if (djbzName == null) continue;

                    if (skipChunks != null && skipChunks.Contains(djbzName))
                        continue;

                    if (seen.Add(djbzName))
                    {
                        yield return new object[] { Path.Combine("extracted", djbzName.Replace(@"\", @"")) };
                    }
                }
            }
        }

        public static int CalculateBufferSize(int height, int rowSize, int border = 0)
        {
            long requiredSize = ((long)height * rowSize) + border;
            if (requiredSize > int.MaxValue || requiredSize < 0)
            {
                DjvuExceptionUtil.ThrowInvalidOperation(
                    $"Calculated buffer size exceeds maximum allowed limit. Height: {height}, RowSize: {rowSize}, Border: {border}");
            }
            return (int)requiredSize;
        }

        public static IEnumerable<object[]> GetExtractedRareVariantPayloads(
            int[] skipDocs = null,
            string[] skipChunks = null,
            TestCoverage coverage = TestCoverage.All,
            JB2EncoderTestCoverage encoderCoverage = JB2EncoderTestCoverage.AllVariants,
            int step = 1)
        {
            string outDir = Path.Combine(ArtifactsDataPath, "extracted");
            if (!Directory.Exists(outDir)) yield break;

            var suffixes = new List<string>();
            if (encoderCoverage.HasFlag(JB2EncoderTestCoverage.AllZero)) suffixes.Add("_allzero");
            if (encoderCoverage.HasFlag(JB2EncoderTestCoverage.Shared)) suffixes.Add("_shared");
            if (encoderCoverage.HasFlag(JB2EncoderTestCoverage.Marks)) suffixes.Add("_marks");

            int count = 0;
            foreach (object[] testData in GetJB2ImageTestData(skipDocs, skipChunks, coverage))
            {
                string djbzRelative = (string)testData[0];
                string sjbzRelative = (string)testData[1];

                string sjbzName = Path.GetFileName(sjbzRelative);
                string baseName = Path.GetFileNameWithoutExtension(sjbzName);
                string ext = Path.GetExtension(sjbzName);

                string originalSjbzPath = Path.Combine(ArtifactsDataPath, sjbzRelative);
                string djbzPath = djbzRelative != null ? Path.Combine(ArtifactsDataPath, djbzRelative) : null;

                if (encoderCoverage.HasFlag(JB2EncoderTestCoverage.Default))
                {
                    if (count++ % step == 0) yield return new object[] { originalSjbzPath, originalSjbzPath, djbzPath };
                }

                foreach (string suffix in suffixes)
                {
                    string variantFile = Path.Combine(outDir, baseName + suffix + ext);
                    if (File.Exists(variantFile))
                    {
                        if (count++ % step == 0) yield return new object[] { variantFile, originalSjbzPath, djbzPath };
                    }
                }
            }
        }

        public static IEnumerable<object[]> ZeroEncodingJB2ImageTestData()
        {
            string[] zeroFiles = new string[]
            {
                "test002C_P02.sjbz", "test008C_P07.sjbz", "test015C_P10.sjbz", "test023C_P09.sjbz", "test059C_P04.sjbz",
                "test002C_P03.sjbz", "test009C_P03.sjbz", "test015C_P11.sjbz", "test023C_P11.sjbz", "test059C_P08.sjbz",
                "test002C_P06.sjbz", "test009C_P07.sjbz", "test016C_P02.sjbz", "test023C_P12.sjbz", "test059C_P14.sjbz",
                "test002C_P10.sjbz", "test010C_P02.sjbz", "test016C_P03.sjbz", "test023C_P14.sjbz", "test059C_P44.sjbz",
                "test002C_P11.sjbz", "test010C_P03.sjbz", "test016C_P05.sjbz", "test023C_P15.sjbz", "test059C_P93.sjbz",
                "test003C_P07.sjbz", "test010C_P05.sjbz", "test016C_P07.sjbz", "test023C_P17.sjbz", "test061C_P106.sjbz",
                "test003C_P09.sjbz", "test010C_P06.sjbz", "test016C_P09.sjbz", "test023C_P18.sjbz", "test061C_P116.sjbz",
                "test003C_P11.sjbz", "test010C_P07.sjbz", "test016C_P10.sjbz", "test023C_P19.sjbz", "test061C_P123.sjbz",
                "test003C_P12.sjbz", "test010C_P08.sjbz", "test016C_P11.sjbz", "test023C_P23.sjbz", "test061C_P88.sjbz",
                "test003C_P13.sjbz", "test010C_P09.sjbz", "test016C_P12.sjbz", "test023C_P24.sjbz", "test065C_P01.sjbz",
                "test003C_P19.sjbz", "test010C_P10.sjbz", "test016C_P13.sjbz", "test023C_P25.sjbz", "test072C_P16.sjbz",
                "test003C_P20.sjbz", "test010C_P11.sjbz", "test016C_P14.sjbz", "test023C_P26.sjbz", "test074C_P05.sjbz",
                "test003C_P24.sjbz", "test010C_P12.sjbz", "test016C_P16.sjbz", "test023C_P27.sjbz", "test076C_P04.sjbz",
                "test003C_P27.sjbz", "test011C_P09.sjbz", "test016C_P17.sjbz", "test023C_P28.sjbz", "test076C_P05.sjbz",
                "test003C_P28.sjbz", "test011C_P10.sjbz", "test016C_P18.sjbz", "test023C_P30.sjbz", "test076C_P06.sjbz",
                "test003C_P31.sjbz", "test011C_P12.sjbz", "test016C_P19.sjbz", "test023C_P31.sjbz", "test076C_P10.sjbz",
                "test003C_P32.sjbz", "test012C_P03.sjbz", "test017C_P02.sjbz", "test023C_P32.sjbz", "test076C_P11.sjbz",
                "test003C_P34.sjbz", "test012C_P05.sjbz", "test018C_P04.sjbz", "test023C_P33.sjbz", "test076C_P13.sjbz",
                "test003C_P35.sjbz", "test012C_P07.sjbz", "test018C_P05.sjbz", "test024C_P02.sjbz", "test076C_P14.sjbz",
                "test003C_P36.sjbz", "test012C_P10.sjbz", "test018C_P06.sjbz", "test024C_P03.sjbz", "test076C_P15.sjbz",
                "test003C_P39.sjbz", "test012C_P11.sjbz", "test018C_P07.sjbz", "test024C_P04.sjbz", "test076C_P16.sjbz",
                "test003C_P41.sjbz", "test012C_P12.sjbz", "test018C_P08.sjbz", "test024C_P05.sjbz", "test076C_P17.sjbz",
                "test003C_P44.sjbz", "test012C_P13.sjbz", "test018C_P09.sjbz", "test024C_P07.sjbz", "test076C_P18.sjbz",
                "test003C_P46.sjbz", "test013C_P03.sjbz", "test019C_P04.sjbz", "test024C_P08.sjbz", "test077C_P02.sjbz",
                "test003C_P47.sjbz", "test013C_P05.sjbz", "test019C_P05.sjbz", "test024C_P11.sjbz", "test077C_P03.sjbz",
                "test003C_P51.sjbz", "test013C_P06.sjbz", "test019C_P06.sjbz", "test024C_P12.sjbz", "test077C_P04.sjbz",
                "test003C_P64.sjbz", "test013C_P07.sjbz", "test019C_P07.sjbz", "test025C_P02.sjbz", "test077C_P06.sjbz",
                "test003C_P69.sjbz", "test013C_P08.sjbz", "test019C_P08.sjbz", "test026C_P04.sjbz", "test077C_P07.sjbz",
                "test003C_P70.sjbz", "test013C_P09.sjbz", "test019C_P09.sjbz", "test027C_P06.sjbz", "test077C_P08.sjbz",
                "test003C_P71.sjbz", "test013C_P10.sjbz", "test019C_P11.sjbz", "test028C_P02.sjbz", "test077C_P09.sjbz",
                "test003C_P76.sjbz", "test013C_P11.sjbz", "test020C_P04.sjbz", "test028C_P04.sjbz", "test077C_P10.sjbz",
                "test003C_P82.sjbz", "test013C_P12.sjbz", "test020C_P05.sjbz", "test028C_P05.sjbz", "test077C_P11.sjbz",
                "test003C_P84.sjbz", "test013C_P13.sjbz", "test020C_P06.sjbz", "test028C_P06.sjbz", "test077C_P12.sjbz",
                "test003C_P87.sjbz", "test013C_P14.sjbz", "test020C_P08.sjbz", "test028C_P07.sjbz", "test077C_P16.sjbz",
                "test003C_P89.sjbz", "test013C_P15.sjbz", "test020C_P09.sjbz", "test028C_P08.sjbz", "test077C_P17.sjbz",
                "test003C_P90.sjbz", "test013C_P16.sjbz", "test020C_P10.sjbz", "test028C_P09.sjbz", "test077C_P18.sjbz",
                "test003C_P91.sjbz", "test013C_P17.sjbz", "test020C_P11.sjbz", "test028C_P10.sjbz", "test077C_P19.sjbz",
                "test003C_P92.sjbz", "test013C_P18.sjbz", "test020C_P13.sjbz", "test028C_P12.sjbz", "test077C_P20.sjbz",
                "test003C_P94.sjbz", "test014C_P02.sjbz", "test020C_P14.sjbz", "test028C_P14.sjbz", "test079C_P02.sjbz",
                "test003C_P95.sjbz", "test014C_P03.sjbz", "test021C_P02.sjbz", "test028C_P16.sjbz", "test079C_P03.sjbz",
                "test003C_P96.sjbz", "test014C_P04.sjbz", "test021C_P06.sjbz", "test028C_P17.sjbz", "test079C_P04.sjbz",
                "test003C_P97.sjbz", "test014C_P05.sjbz", "test022C_P02.sjbz", "test028C_P19.sjbz", "test079C_P07.sjbz",
                "test003C_P98.sjbz", "test014C_P06.sjbz", "test022C_P03.sjbz", "test028C_P20.sjbz", "test079C_P08.sjbz",
                "test003C_P99.sjbz", "test014C_P07.sjbz", "test022C_P04.sjbz", "test029C_P02.sjbz", "test079C_P09.sjbz",
                "test004C_P05.sjbz", "test014C_P08.sjbz", "test022C_P05.sjbz", "test029C_P07.sjbz", "test079C_P15.sjbz",
                "test005C_P03.sjbz", "test014C_P09.sjbz", "test022C_P06.sjbz", "test029C_P08.sjbz", "test079C_P16.sjbz",
                "test005C_P06.sjbz", "test014C_P10.sjbz", "test022C_P07.sjbz", "test045C_P02.sjbz", "test079C_P17.sjbz",
                "test005C_P07.sjbz", "test014C_P11.sjbz", "test022C_P09.sjbz", "test053C_P02.sjbz", "test079C_P19.sjbz",
                "test005C_P10.sjbz", "test014C_P12.sjbz", "test022C_P10.sjbz", "test053C_P04.sjbz", "test079C_P20.sjbz",
                "test006C_P07.sjbz", "test014C_P14.sjbz", "test022C_P11.sjbz", "test054C_P30.sjbz", "test079C_P22.sjbz",
                "test007C_P02.sjbz", "test014C_P15.sjbz", "test022C_P13.sjbz", "test056C_P06.sjbz", "test079C_P23.sjbz",
                "test007C_P04.sjbz", "test015C_P02.sjbz", "test022C_P14.sjbz", "test058C_P03.sjbz", "test079C_P24.sjbz",
                "test007C_P05.sjbz", "test015C_P03.sjbz", "test022C_P15.sjbz", "test058C_P05.sjbz", "test079C_P25.sjbz",
                "test007C_P06.sjbz", "test015C_P04.sjbz", "test022C_P16.sjbz", "test058C_P17.sjbz", "test079C_P26.sjbz",
                "test007C_P09.sjbz", "test015C_P05.sjbz", "test022C_P18.sjbz", "test058C_P21.sjbz", "test079C_P33.sjbz",
                "test007C_P10.sjbz", "test015C_P06.sjbz", "test023C_P02.sjbz", "test058C_P23.sjbz", "test079C_P34.sjbz",
                "test007C_P11.sjbz", "test015C_P07.sjbz", "test023C_P03.sjbz", "test058C_P27.sjbz", "test079C_P35.sjbz",
                "test008C_P02.sjbz", "test015C_P08.sjbz", "test023C_P05.sjbz", "test058C_P29.sjbz", "test079C_P36.sjbz",
                "test008C_P06.sjbz", "test015C_P09.sjbz", "test023C_P06.sjbz", "test059C_P02.sjbz"
            };

            var baseMapping = new Dictionary<string, Tuple<string, string>>();
            foreach (object[] data in GetJB2ImageTestData(null, null, TestCoverage.All))
            {
                string djbz = (string)data[0];
                string sjbz = (string)data[1];
                if (sjbz != null)
                {
                    string baseSjbz = Path.GetFileName(sjbz);
                    baseMapping[baseSjbz] = new Tuple<string, string>(djbz, sjbz);
                }
            }

            foreach (string baseName in zeroFiles)
            {
                if (baseMapping.TryGetValue(baseName, out Tuple<string, string> originalData))
                {
                    yield return new object[] { originalData.Item1, originalData.Item2};
                }
            }
        }

        /// <summary>
        /// Executes a setup action on the main test thread while holding a lock, 
        /// triggers an action on a separate background thread, 
        /// and safely releases the lock before validating that the background thread 
        /// threw the expected exception.
        /// </summary>
        /// <typeparam name="TException">
        /// The type of Exception expected to be thrown by the background action.
        /// </typeparam>
        /// <param name="lockAcquisition">
        /// The delegate executing lock acquisition logic on the main thread. 
        /// Designed to support OS-thread-affine primitives (e.g., System.Threading.Lock, Monitor). 
        /// Asynchronous locking patterns utilizing the async state machine 
        /// (e.g., SemaphoreSlim.WaitAsync) must be avoided within this delegate.
        /// </param>
        /// <param name="lockRelease">
        /// The delegate executing lock release logic on the main thread.
        /// </param>
        /// <param name="backgroundAction">
        /// The delegate executing the test logic on the isolated background thread.
        /// </param>
        /// <param name="timeout">
        /// The duration in milliseconds to wait for the background thread to finish execution.
        /// </param>
        /// <param name="cleanupTimeout">
        /// The duration in milliseconds to wait for an unresponsive background thread 
        /// to gracefully terminate after an interrupt is issued, before abandoning it. 
        /// Defaults to DefaultCleanupTimeout (500).
        /// </param>
        /// <returns>
        /// A Task returning the exception of type <typeparamref name="TException"/> 
        /// thrown by the background action.
        /// </returns>
        /// <remarks>
        /// This method executes synchronously and utilizes blocking operations (Wait) 
        /// instead of asynchronous yields (await). Synchronous blocking guarantees that 
        /// the execution does not yield the thread to the ThreadPool.
        /// 
        /// This mechanism preserves OS-thread affinity for lock synchronization 
        /// primitives (e.g., System.Threading.Lock), ensuring that the <paramref name="lockAcquisition"/> 
        /// and <paramref name="lockRelease"/> delegates execute on the same managed thread.
        /// 
        /// While sync-over-async is documented as an anti-pattern in production code, 
        /// it is utilized here in the test infrastructure to prevent 
        /// SynchronizationLockException when testing thread-affine locks.
        /// </remarks>
        public static Task<TException> ThrowsAsync<TException>(
            Action lockAcquisition,
            Action lockRelease,
            Action backgroundAction,
            int timeout = 5000,
            int cleanupTimeout = DefaultCleanupTimeout) where TException : Exception
        {
            // Do not use 'using' to prevent ObjectDisposedException on background threads if a timeout occurs
            var backgroundCanStart = new SemaphoreSlim(0, 1);
            var backgroundIsDone = new SemaphoreSlim(0, 1);

            // 1. Acquire the lock on the main test thread
            lockAcquisition();

            Exception backgroundException = null;
            Thread backgroundThread = null;

            try
            {
                // 2. Provision a separate background thread using Thread
                backgroundThread = new Thread(() =>
                {
                    try
                    {
                        try
                        {
                            // Wait for the main thread signal
                            if (!backgroundCanStart.Wait(timeout))
                            {
                                DjvuExceptionUtil.ThrowTimeoutException("Test timed out waiting for the background thread to be allowed to start.");
                            }

                            backgroundAction();
                        }
                        catch (Exception ex)
                        {
                            backgroundException = ex;
                        }
                        finally
                        {
                            // Signal the main thread that the background work hit its exception/block
                            backgroundIsDone.Release();
                        }
                    }
                    catch (Exception globalEx)
                    {
                        // Prevent unhandled exceptions from terminating the test host process.
                        // Catches exceptions thrown by SemaphoreSlim.Release or thread abort mechanisms.
                        Debug.WriteLine($"[ThrowsAsync Background Error] {globalEx}");
                        Console.WriteLine($"[ThrowsAsync Background Error] {globalEx}");
                    }
                });
                
                backgroundThread.IsBackground = true;
                backgroundThread.Name = "ThrowsAsync_BackgroundWorker";
                backgroundThread.Start();

                // 3. Allow background thread to run
                backgroundCanStart.Release();

                // 4. Main thread blocks SYNCHRONOUSLY here until the background action performs its work.
                // This guarantees we never yield the OS thread, strictly preserving Lock affinity.
                if (!backgroundIsDone.Wait(timeout))
                {
                    DjvuExceptionUtil.ThrowTimeoutException("Test timed out waiting for the background thread to finish execution.");
                }

                // Capture exception state while lock is still held
                if (backgroundException != null)
                {
                    // Execute Assert.Throws on the exact captured exception to satisfy xUnit formatting
                    var exception = Assert.Throws<TException>(() => ExceptionDispatchInfo.Capture(backgroundException).Throw());
                    return Task.FromResult(exception);
                }

                // If no exception occurred, trigger failure by passing empty action
                var missingEx = Assert.Throws<TException>(() => { });
                return Task.FromResult(missingEx);
            }
            finally
            {
                try
                {
                    // 5. CRITICAL: Guarantee the lock is freed on the main thread no matter what
                    lockRelease();
                }
                finally
                {
                    // 6. If the thread is still alive after lock release, forcefully interrupt it
                    if (backgroundThread != null && backgroundThread.IsAlive)
                    {
                        try
                        {
                            backgroundThread.Interrupt();
                            backgroundThread.Join(cleanupTimeout);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ThrowsAsync Teardown Warning] {ex}");
                            Console.WriteLine($"[ThrowsAsync Teardown Warning] {ex}");
                        }
                    }
                }
            }
        }
    }
}
