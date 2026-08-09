using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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

        private static bool IsImageBinaryComparable(Bitmap image1, Bitmap image2, out bool pixelFormatMismatch)
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

        public static bool CompareImagesForBinarySimilarity(Bitmap image1, Bitmap image2, double diffThreshold = 0.05, bool logDiff = false, string message = null)
        {
            double diff;
            bool result = CompareImagesForBinarySimilarity(image1, image2, out diff, diffThreshold);

            if (logDiff)
            {
                Console.WriteLine((message != null ? message : "") + $" Image diff: {diff:#0.000000}, passed: {result}");
            }

            return result;
        }

        public static bool CompareImagesForBinarySimilarity(Bitmap image1, Bitmap image2, out double diffValue, double diffThreshold = 0.05)
        {
            bool formatMismatch;
            bool result = IsImageBinaryComparable(image1, image2, out formatMismatch);

            diffValue = double.NaN;
            Bitmap bmp1 = null;
            Bitmap bmp2 = null;

            try
            {

                if (result && formatMismatch)
                {
                    if (image1.PixelFormat != PixelFormat.Format24bppRgb)
                    {
                        bmp1 = new Bitmap(image1.Width, image1.Height, PixelFormat.Format24bppRgb);
                        using SysGraphics gfx = SysGraphics.FromImage(bmp1);
                        gfx.DrawImage(image1, new Rectangle(0, 0, image1.Width, image1.Height));
                    }

                    if (image2.PixelFormat != PixelFormat.Format24bppRgb)
                    {
                        bmp2 = new Bitmap(image2.Width, image2.Height, PixelFormat.Format24bppRgb);
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
                    PixelFormat.Format32bppArgb => ImageBinaryDiff(imageData1, imageData2, 32),
                    PixelFormat.Format24bppRgb => ImageBinaryDiff(imageData1, imageData2),
                    PixelFormat.Format8bppIndexed => ImageBinaryDiff(imageData1, imageData2, 8),
                    PixelFormat.Format16bppGrayScale => ImageBinaryDiff(imageData1, imageData2, 16, 16),
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
        /// <param name="stride">The row stride (in bytes), including any padding and preserving the sign (for bottom-up images).</param>
        /// <param name="pixelSize">The size of a single pixel in bits (e.g., 24 for 24bpp RGB, 32 for ARGB).</param>
        /// <param name="channelSize">The size of a single color channel in bits (e.g., 8 for standard RGB channels).</param>
        /// <returns>A ratio between 0.0 (identical) and 1.0 (completely opposite) representing the average pixel difference.</returns>
        internal static unsafe double ImageBinaryDiff(byte* ptr1, byte* ptr2, int width, int height, int stride, int pixelSize = 24, int channelSize = 8)
        {
            if (ptr1 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(ptr1), "First image buffer pointer cannot be null.");
            if (ptr2 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(ptr2), "Second image buffer pointer cannot be null.");
            if (width <= 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            if (height <= 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            if (pixelSize <= 0 || pixelSize % 8 != 0) DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            if (channelSize != 8) DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));

            uint widthBytes = (uint)width * (uint)(pixelSize / 8);
            if ((ulong)Math.Abs((long)stride) < widthBytes) DjvuExceptionUtil.ThrowArgument("Stride must be large enough to contain the width bytes.", nameof(stride));

            int threadCount = GetThreadCountForImageBinaryDiff((long)width * height);

            if (threadCount == 1)
            {
                if (Avx2.IsSupported)
                {
                    if (widthBytes >= 32)
                    {
                        return ImageDiffVector256(ptr1, ptr2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                    }
                    else if (widthBytes >= 16)
                    {
                        return ImageDiffVector128(ptr1, ptr2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                    }
                    else
                    {
                        return ImageBinaryDiffScalar(ptr1, ptr2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                    }
                }
                else if (Vector128.IsHardwareAccelerated)
                {
                    if (widthBytes >= 16)
                    {
                        return ImageDiffVector128(ptr1, ptr2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                    }
                    else
                    {
                        return ImageBinaryDiffScalar(ptr1, ptr2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                    }
                }
                else
                {
                    return ImageBinaryDiffScalar(ptr1, ptr2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                }
            }
            else
            {
                ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = threadCount };
                if (Avx2.IsSupported)
                {
                    if (widthBytes >= 32)
                    {
                        return ImageDiffParallel256(ptr1, ptr2, (uint)width, (uint)height, stride, options, pixelSize, channelSize);
                    }
                    else if (widthBytes >= 16)
                    {
                        return ImageDiffParallel128(ptr1, ptr2, (uint)width, (uint)height, stride, options, pixelSize, channelSize);
                    }
                    else
                    {
                        return ImageBinaryDiffScalar(ptr1, ptr2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                    }
                }
                else if (Vector128.IsHardwareAccelerated)
                {
                    if (widthBytes >= 16)
                    {
                        return ImageDiffParallel128(ptr1, ptr2, (uint)width, (uint)height, stride, options, pixelSize, channelSize);
                    }
                    else
                    {
                        return ImageBinaryDiffScalar(ptr1, ptr2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                    }
                }
                else
                {
                    return ImageBinaryDiffScalar(ptr1, ptr2, (uint)width, (uint)height, stride, pixelSize, channelSize);
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
        internal static unsafe double ImageBinaryDiff(BitmapData imageData1, BitmapData imageData2, int pixelSize = 24, int channelSize = 8)
        {
            if (imageData1 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(imageData1), "First image data cannot be null.");
            if (imageData2 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(imageData2), "Second image data cannot be null.");
            if (pixelSize <= 0 || pixelSize % 8 != 0) DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            if (channelSize != 8) DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));

            if (imageData1.Stride != imageData2.Stride)
            {
                // We do not support comparing images with different strides.
                return 1.0;
            }

            int stride = imageData1.Stride;

            return ImageBinaryDiff((byte*)imageData1.Scan0, (byte*)imageData2.Scan0, imageData1.Width, imageData1.Height, stride, pixelSize, channelSize);
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
        internal static unsafe double ImageBinaryDiffCore(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride, int pixelSize, int channelSize)
        {
            if (scan0_1 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_1), "First image buffer pointer cannot be null.");
            if (scan0_2 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_2), "Second image buffer pointer cannot be null.");
            if (width == 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            if (height == 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            if (pixelSize <= 0 || pixelSize % 8 != 0) DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            if (channelSize != 8) DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));

            ulong widthBytes = (ulong)width * (uint)(pixelSize / 8);
            if ((ulong)Math.Abs((long)stride) < widthBytes) DjvuExceptionUtil.ThrowArgument("Stride must be large enough to contain the width bytes.", nameof(stride));

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
                    byte* p1 = scan0_1 + ((long)i * stride);
                    byte* p2 = scan0_2 + ((long)i * stride);
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
                    byte* p1 = scan0_1 + ((long)i * stride);
                    byte* p2 = scan0_2 + ((long)i * stride);
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
                result += ImageBinaryDiffSimdFallback(scan0_1, scan0_2, (uint)widthBytes, height, stride);
            }

            double maxChannelValue = (1L << channelSize) - 1;
            return result / ((double)width * height * ((double)pixelSize / channelSize) * maxChannelValue);
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
        internal static unsafe double ImageDiffVector256(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride, int pixelSize = 24, int channelSize = 8)
        {
            if (scan0_1 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_1), "First image buffer pointer cannot be null.");
            if (scan0_2 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_2), "Second image buffer pointer cannot be null.");
            if (width == 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            if (height == 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            if (pixelSize <= 0 || pixelSize % 8 != 0) DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            if (channelSize != 8) DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));

            ulong widthBytes = (ulong)width * (uint)(pixelSize / 8);
            if ((ulong)Math.Abs((long)stride) < widthBytes) DjvuExceptionUtil.ThrowArgument("Stride must be large enough to contain the width bytes.", nameof(stride));

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
                byte* p1 = scan0_1 + ((long)i * stride);
                byte* p2 = scan0_2 + ((long)i * stride);
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

            double maxChannelValue = (1L << channelSize) - 1;
            return result / ((double)width * height * ((double)pixelSize / channelSize) * maxChannelValue);
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
        internal static unsafe double ImageDiffVector128(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride, int pixelSize = 24, int channelSize = 8)
        {
            if (scan0_1 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_1), "First image buffer pointer cannot be null.");
            if (scan0_2 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_2), "Second image buffer pointer cannot be null.");
            if (width == 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            if (height == 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            if (pixelSize <= 0 || pixelSize % 8 != 0) DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            if (channelSize != 8) DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));

            ulong widthBytes = (ulong)width * (uint)(pixelSize / 8);
            if ((ulong)Math.Abs((long)stride) < widthBytes) DjvuExceptionUtil.ThrowArgument("Stride must be large enough to contain the width bytes.", nameof(stride));

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
                byte* p1 = scan0_1 + ((long)i * stride);
                byte* p2 = scan0_2 + ((long)i * stride);
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

            double maxChannelValue = (1L << channelSize) - 1;
            return result / ((double)width * height * ((double)pixelSize / channelSize) * maxChannelValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static unsafe double ImageDiffParallel256(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride, ParallelOptions options, int pixelSize = 24, int channelSize = 8)
        {
            if (scan0_1 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_1), "First image buffer pointer cannot be null.");
            if (scan0_2 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_2), "Second image buffer pointer cannot be null.");
            if (width == 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            if (height == 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            if (pixelSize <= 0 || pixelSize % 8 != 0) DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            if (channelSize != 8) DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));

            ulong widthBytes = (ulong)width * (uint)(pixelSize / 8);
            if ((ulong)Math.Abs((long)stride) < widthBytes) DjvuExceptionUtil.ThrowArgument("Stride must be large enough to contain the width bytes.", nameof(stride));

            if (!Avx2.IsSupported) throw new PlatformNotSupportedException("AVX2 hardware acceleration is not supported on this platform.");
            if (widthBytes < 32) throw new DjvuArgumentOutOfRangeException(nameof(width), widthBytes, "Buffer width must be at least 32 bytes to fill a Vector256 (AVX2) register.");

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
                    byte* p1 = scan0_1 + (y * stride);
                    byte* p2 = scan0_2 + (y * stride);
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

            double maxChannelValue = (1L << channelSize) - 1;
            return (double)totalResult / ((double)width * height * ((double)pixelSize / channelSize) * maxChannelValue);
                }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static unsafe double ImageDiffParallel128(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride, ParallelOptions options, int pixelSize = 24, int channelSize = 8)
        {
            if (scan0_1 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_1), "First image buffer pointer cannot be null.");
            if (scan0_2 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_2), "Second image buffer pointer cannot be null.");
            if (width == 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            if (height == 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            if (pixelSize <= 0 || pixelSize % 8 != 0) DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            if (channelSize != 8) DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));

            ulong widthBytes = (ulong)width * (uint)(pixelSize / 8);
            if ((ulong)Math.Abs((long)stride) < widthBytes) DjvuExceptionUtil.ThrowArgument("Stride must be large enough to contain the width bytes.", nameof(stride));

            if (!Vector128.IsHardwareAccelerated) throw new PlatformNotSupportedException("Vector128 hardware acceleration is not supported on this platform.");
            if (widthBytes < 16) throw new DjvuArgumentOutOfRangeException(nameof(width), widthBytes, "Buffer width must be at least 16 bytes to fill a Vector128 register.");

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
                    byte* p1 = scan0_1 + (y * stride);
                    byte* p2 = scan0_2 + (y * stride);
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

            double maxChannelValue = (1L << channelSize) - 1;
            return (double)totalResult / ((double)width * height * ((double)pixelSize / channelSize) * maxChannelValue);
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
        private static unsafe ulong ImageBinaryDiffSimdFallback(byte* scan0_1, byte* scan0_2, uint widthBytes, uint height, int stride)
        {
            ulong result = 0;
            for (ulong i = 0; i < height; i++)
            {
                byte* p1 = scan0_1 + ((long)i * stride);
                byte* p2 = scan0_2 + ((long)i * stride);

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
        /// <param name="stride"></param>
        /// <param name="pixelSize"></param>
        /// <param name="channelSize"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe double ImageBinaryDiffScalar(byte* scan0_1, byte* scan0_2, uint width, uint height, int stride, int pixelSize = 24, int channelSize = 8)
        {
            if (scan0_1 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_1), "First image buffer pointer cannot be null.");
            if (scan0_2 == null) DjvuExceptionUtil.ThrowArgumentNull(nameof(scan0_2), "Second image buffer pointer cannot be null.");
            if (width == 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be positive.");
            if (height == 0) DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be positive.");
            if (pixelSize <= 0 || pixelSize % 8 != 0) DjvuExceptionUtil.ThrowArgument("Method supports only pixel sizes that are a positive multiple of 8 bits.", nameof(pixelSize));
            if (channelSize != 8) DjvuExceptionUtil.ThrowArgument("Method supports only 8-bit channel sizes.", nameof(channelSize));

            ulong widthBytes = (ulong)width * (uint)(pixelSize / 8);
            if ((ulong)Math.Abs((long)stride) < widthBytes) DjvuExceptionUtil.ThrowArgument("Stride must be large enough to contain the width bytes.", nameof(stride));

            ulong result = ImageBinaryDiffSimdFallback(scan0_1, scan0_2, (uint)widthBytes, height, stride);
            double maxChannelValue = (1L << channelSize) - 1;
            return (double)result / (width * height * ((double)pixelSize / channelSize) * maxChannelValue);
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

        public static bool CompareImages(Bitmap image1, Bitmap image2)
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
                ulong rowSize = (ulong) ((pixelSize / 8) * image1.Width);
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
                uint rowSize = (uint)((pixelSize / 8) * image1.Width);
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

        public static Bitmap InvertColor(Bitmap source)
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

        public static Bitmap TransformBitmap(Bitmap source, ColorMatrix colorMatrix)
        {
            Bitmap result = new Bitmap(source.Width, source.Height, source.PixelFormat);

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
