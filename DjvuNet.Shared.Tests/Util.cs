using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using DjvuNet.DjvuLibre;
using Xunit;

namespace DjvuNet.Tests
{
    public static partial class Util
    {
        private static string _ArtifactsPath;
        private static string _ArtifactsDataPath;
        private static string _ArtifactsJsonPath;

        private static SortedDictionary<int, Tuple<int, int, DocumentType, string> > _TestDocumentData;

        static Util()
        {
            _TestDocumentData = new SortedDictionary<int, Tuple< int, int, DocumentType, string>> ();

            for(int i = 1; i <= 77; i++)
            {
                using (DjvuNet.DjvuLibre.DjvuDocumentInfo docInfo = 
                    DjvuLibre.DjvuDocumentInfo.CreateDjvuDocumentInfo(GetTestFilePath(i)))
                {
                    var docData = Tuple.Create<int, int, DocumentType, string>(
                        docInfo.PageCount, docInfo.FileCount, docInfo.DocumentType, null /* docInfo.DumpDocumentData(true)*/);
                    _TestDocumentData.Add(i, docData);

                }
            }

        }

        public static int GetTestDocumentPageCount(int index)
        {
            return _TestDocumentData[index].Item1;
        }

        public static int GetTestDocumentFileCount(int index)
        {
            return _TestDocumentData[index].Item2;
        }

        public static DocumentType GetTestDocumentType(int index)
        {
            return _TestDocumentData[index].Item3;
        }

        public static string GetTestDocumentJsonDump(int index)
        {
            return _TestDocumentData[index].Item4;
        }

        public static void FailOnException(Exception ex, string message, params object[] data)
        {
            string info = $"\nTest Failed -> Unexpected Exception: " + 
                $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")}\n\n";

            if (data != null && data.Length > 0)
                info += (String.Format(message, data) + "\n" + ex.ToString());
            else
                info += (message + "\n" + ex.ToString());

            Assert.True(false, info);
        }

        public static string GetTestFilePathTemplate()
        {
            char dirSep = Path.DirectorySeparatorChar;
            string filePathTempl = $"artifacts{dirSep}test{{0:00#}}C.djvu";
            string rootDir = Util.RepoRoot;
            filePathTempl = Path.Combine(rootDir, filePathTempl);
            return filePathTempl;
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
                    return _ArtifactsPath;
                else
                {
                    _ArtifactsPath = Path.Combine(Util.RepoRoot, "artifacts");
                    return _ArtifactsPath;
                }
            }
        }

        public static string ArtifactsDataPath
        {
            get
            {
                if (_ArtifactsDataPath != null)
                    return _ArtifactsDataPath;
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
                    return _ArtifactsJsonPath;
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

            for (int i = 0; i < refBuffer.Length; i++)
                Assert.True(refBuffer[i] == buffer[i],
                    $"Binary data diff at index: {i}, expected: {refBuffer[i]}, actual: {buffer[i]}");
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

        public static bool CompareImages(Bitmap image1, Bitmap image2)
        {
            if (image1 == null || image2 == null)
                return false;

            if (image1.PixelFormat != image2.PixelFormat)
                return false;
            if (image1.Width != image2.Width || image1.Height != image2.Height)
                return false;

            Rectangle rect = new Rectangle(0, 0, image1.Width, image1.Height);
            BitmapData img1 = image1.LockBits(rect, ImageLockMode.ReadOnly, image1.PixelFormat);
            BitmapData img2 = image2.LockBits(rect, ImageLockMode.ReadOnly, image1.PixelFormat);

            bool result = CompareImagesInternal(img1, img2);

            image1.UnlockBits(img1);
            image2.UnlockBits(img2);

            return result;
        }

        public static bool CompareImages(BitmapData image1, BitmapData image2)
        {
            if (image1.PixelFormat != image2.PixelFormat)
                return false;
            if (image1.Width != image2.Width || image1.Height != image2.Height)
                return false;

            return CompareImagesInternal(image1, image2);
        }

        private static bool CompareImagesInternal(BitmapData image1, BitmapData image2)
        {
            if (Environment.Is64BitProcess)
                return CompareImages64(image1, image2);
            else
                return CompareImages32(image1, image2);
        }

        private static bool CompareImages64(BitmapData image1, BitmapData image2)
        {
            int pixelSize = Image.GetPixelFormatSize(image1.PixelFormat);
            int bufferSize = (pixelSize / 8) * image1.Width * image1.Height;
            int remainder = bufferSize % 8;

            unsafe
            {
                ulong* longCheckSize = (ulong*)(image1.Scan0 + bufferSize - remainder);

                ulong* lp = (ulong*)image1.Scan0;
                ulong* rp = (ulong*)image2.Scan0;

                for (; lp < longCheckSize; lp++, rp++)
                {
                    if (*lp != *rp)
                        return false;
                }

                byte* lb = (byte*)lp;
                byte* rb = (byte*)rp;

                for(int i = 0; i < remainder; i++, lb++, rb++)
                {
                    if (*lb != *rb)
                        return false;
                }
            }

            return true;
        }

        private static bool CompareImages32(BitmapData image1, BitmapData image2)
        {
            int pixelSize = Image.GetPixelFormatSize(image1.PixelFormat);
            int bufferSize = (pixelSize/8) * image1.Width * image1.Height;
            int remainder = bufferSize % 4;

            unsafe
            {
                uint* longCheckSize = (uint*)(image1.Scan0 + bufferSize - remainder);

                uint* lp = (uint*)image1.Scan0;    
                uint* rp = (uint*)image2.Scan0;

                for (; lp < longCheckSize; lp++, rp++)
                {
                    if (*lp != *rp)
                        return false;
                }

                byte* lb = (byte*)lp;
                byte* rb = (byte*)rp;

                for (int i = 0; i < remainder; i++, lb++, rb++)
                {
                    if (*lb != *rb)
                        return false;
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
