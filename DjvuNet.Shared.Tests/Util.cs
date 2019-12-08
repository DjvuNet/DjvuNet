using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using DjvuNet.DjvuLibre;
using DjvuNet.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace DjvuNet.Tests
{
    public static partial class Util
    {
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
                    _TestDocumentData = new SortedDictionary<int, Tuple<int, int, DocumentType, string>>();
                    JsonConverter[] converters = new JsonConverter[]
                    {
                        new DjvuDocConverter(),
                        new NodeBaseConverter(),
                    };

                    for (int i = 1; i <= 77; i++)
                    {
                        string filePath = GetTestFilePath(i);
                        filePath = Path.Combine(Util.ArtifactsJsonPath,
                            Path.GetFileNameWithoutExtension(filePath) + ".json");

                        string json = File.ReadAllText(filePath, new UTF8Encoding(false));
                        DjvuDoc doc = JsonConvert.DeserializeObject<DjvuDoc>(json, converters);
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
                        if (!_TestDocumentData.ContainsKey(i))
                            _TestDocumentData.Add(i, docData);
                    }

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

            Assert.True(false, info);
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

        public static List<String> TestUnicodeStrings
        {
            get
            {
                List<String> retVal = new List<string>(new string[]
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
                Console.WriteLine((message != null ? message : "") + $" Image diff: {diff:#0.0000}, passed: {result}");
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
                        using var gfx = System.Drawing.Graphics.FromImage(bmp1);
                        gfx.DrawImage(image1, new Rectangle(0, 0, image1.Width, image1.Height));
                    }

                    if (image2.PixelFormat != PixelFormat.Format24bppRgb)
                    {
                        bmp2 = new Bitmap(image2.Width, image2.Height, PixelFormat.Format24bppRgb);
                        using var gfx = System.Drawing.Graphics.FromImage(bmp2);
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

        internal static unsafe double ImageBinaryDiff(BitmapData imageData1, BitmapData imageData2, int pixelSize = 24, int channelSize = 8)
        {
            if (channelSize % 8 != 0)
            {
                throw new ArgumentException("Method supports only multiple of 8 bits channel sizes");
            }

            uint pixelSizeInBytes = (uint) Image.GetPixelFormatSize(imageData1.PixelFormat) / 8;
            uint width = (uint)imageData1.Width;
            uint height = (uint)imageData1.Height;
            uint widthBytes = width * pixelSizeInBytes;

            double result = 0.0;

            if (Avx2.IsSupported)
            {
                uint widthBytesAvx2LRem = widthBytes % 128;
                uint widthBytesAvx2L = widthBytes - widthBytesAvx2LRem;
                uint widthBytesAvx2SRem = widthBytesAvx2LRem % 32;
                uint oneVectorRounds = widthBytesAvx2LRem / 32;

                Vector256<double> mask = Vector256.Create((double)0x0010000000000000);
                Vector256<ulong> resultVecU = Vector256<ulong>.Zero;

                for (uint i = 0; i < height; i++)
                {
                    byte* pixelRow1 = (byte*)((long)imageData1.Scan0 + (i * imageData1.Stride));
                    byte* pixelRow2 = (byte*)((long)imageData2.Scan0 + (i * imageData2.Stride));
                    uint rowPos = i * width;

                    for (uint wb = 0; wb < widthBytesAvx2L; wb += 32)
                    {
                        Vector256<byte> r11 = Avx2.LoadDquVector256(pixelRow1 + wb);
                        Vector256<byte> r21 = Avx2.LoadDquVector256(pixelRow2 + wb);

                        Vector256<byte> r12 = Avx2.LoadDquVector256(pixelRow1 + (wb += 32));
                        Vector256<byte> r22 = Avx2.LoadDquVector256(pixelRow2 + wb);

                        Vector256<ushort> diff1 = Avx2.SumAbsoluteDifferences(r11, r21);

                        Vector256<byte> r13 = Avx2.LoadDquVector256(pixelRow1 + (wb += 32));
                        Vector256<byte> r23 = Avx2.LoadDquVector256(pixelRow2 + wb);

                        Vector256<ushort> diff2 = Avx2.SumAbsoluteDifferences(r12, r22);

                        Vector256<byte> r14 = Avx2.LoadDquVector256(pixelRow1 + (wb += 32));
                        Vector256<byte> r24 = Avx2.LoadDquVector256(pixelRow2 + wb);

                        Vector256<ushort> diff3 = Avx2.SumAbsoluteDifferences(r13, r23);
                        Vector256<ushort> diff4 = Avx2.SumAbsoluteDifferences(r14, r24);

                        Vector256<ulong> diff12 = Avx2.Add(diff1.AsUInt64(), diff2.AsUInt64());
                        Vector256<ulong> diff34 = Avx2.Add(diff3.AsUInt64(), diff4.AsUInt64());

                        diff12 = Avx2.Add(diff12, diff34);
                        resultVecU = Avx2.Add(resultVecU, diff12);
                    }

                    if (oneVectorRounds > 0)
                    {
                        uint rounds = oneVectorRounds;
                        pixelRow1 += widthBytesAvx2L;
                        pixelRow2 += widthBytesAvx2L;

                        while (rounds > 0)
                        {
                            Vector256<byte> r11 = Avx2.LoadDquVector256(pixelRow1);
                            Vector256<byte> r21 = Avx2.LoadDquVector256(pixelRow2);
                            Vector256<ushort> diff1 = Avx2.SumAbsoluteDifferences(r11, r21);

                            resultVecU = Avx2.Add(resultVecU, diff1.AsUInt64());

                            pixelRow1 += 32;
                            pixelRow2 += 32;
                            rounds--;
                        }
                    }

                    if (widthBytesAvx2SRem > 0)
                    {
                        uint pixelReminder = widthBytesAvx2SRem % 3;
                        uint pixelsBytesRemaining = widthBytesAvx2SRem - pixelReminder;

                        for (uint p = 0; p < pixelsBytesRemaining; p += 3, pixelRow1 += 3, pixelRow2 += 3)
                        {
                            result += GetPixelDiff(pixelRow1, pixelRow2);
                        }

                        while (pixelReminder > 0)
                        {
                            result += (MathF.Abs((float)*(pixelRow1++) - (float)*(pixelRow2++)));
                            pixelReminder--;
                        }
                    }
                }

                Vector256<ulong> tmpVec1 = Avx2.Or(resultVecU, mask.AsUInt64());
                Vector256<double> diff1Double = Avx2.Subtract(tmpVec1.AsDouble(), mask);

                Vector256<double> result1 = Avx2.HorizontalAdd(diff1Double, diff1Double);

                Vector128<double> lowVec = Avx2.ExtractVector128(result1, 0b0);
                Vector128<double> hiVec = Avx2.ExtractVector128(result1, 0b1);
                Vector128<double> resultVec = Avx2.AddScalar(lowVec, hiVec);

                result += resultVec.GetElement(0);
            } // end of: if (Avx2.IsSupported) {}
            // else if (Sse2.IsSupported) {}
            else
            {
                for (uint i = 0; i < height; i++)
                {
                    byte* pixelRow1 = (byte*)((long)imageData1.Scan0 + (i * imageData1.Stride));
                    byte* pixelRow2 = (byte*)((long)imageData2.Scan0 + (i * imageData2.Stride));
                    uint rowPos = i * width;

                    for (uint wb = 0; wb < widthBytes; wb += pixelSizeInBytes)
                    {
                        result += GetPixelDiff(pixelRow1 + wb, pixelRow2 + wb);
                    }
                }
            }

            double maxChannelValue = (1 << channelSize) - 1;
            return result / (width * height * ((double)pixelSize / channelSize) * maxChannelValue);
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

            return MathF.Abs(r1 - r2) + MathF.Abs(g1 - g2) + MathF.Abs(b1 - b2);
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

            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(result))
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
    }
}
