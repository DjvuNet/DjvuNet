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

        public static bool CompareImages(BitmapData image1, BitmapData image2)
        {
            if (image1.PixelFormat != image2.PixelFormat)
                return false;
            if (image1.Width != image2.Width || image1.Height != image2.Height)
                return false;

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
