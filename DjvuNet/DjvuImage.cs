using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Errors;
using DjvuNet.Wavelet;
using GMap = DjvuNet.Graphics.IMap;

namespace DjvuNet
{
    public class DjvuImage
    {
        private IDjvuPage _Page;
        private IDjvuDocument _Document;

        public IDjvuPage Page { get { return _Page; } }

        public IDjvuDocument Document { get { return _Document; } }

        public DjvuImage() { }

        public DjvuImage(IDjvuPage page)
        {
            if (page == null)
                throw new DjvuArgumentNullException(nameof(page));

            _Page = page;
            _Document = page.Document;
        }

        public System.Drawing.Bitmap CreateBlankImage(Brush imageColor)
        {
            return CreateBlankImage(imageColor, _Page.Width, _Page.Height);
        }

        /// <summary>
        /// Creates a blank image with the given color
        /// </summary>
        /// <param name="imageColor"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap CreateBlankImage(Brush imageColor, int width, int height)
        {
            System.Drawing.Bitmap newBackground = new System.Drawing.Bitmap(width, height);

            // Fill the whole image with white
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(newBackground))
                g.FillRegion(imageColor, new Region(new System.Drawing.Rectangle(0, 0, width, height)));

            return newBackground;
        }

        public static Bitmap ImageFromMap(GMap map, Rectangle rect, PixelFormat format)
        {
            Bitmap retVal = new Bitmap(rect.Width, rect.Height, format);
            BitmapData bmpData = retVal.LockBits(rect, ImageLockMode.WriteOnly, format);
            GCHandle hMapData = GCHandle.Alloc(map.Data, GCHandleType.Pinned);
            IntPtr pMapData = hMapData.AddrOfPinnedObject();
            MemoryUtilities.MoveMemory(bmpData.Scan0, pMapData, map.Data.Length);
            hMapData.Free();
            retVal.UnlockBits(bmpData);
            return retVal;
        }

        /// <summary>
        /// Resizes the image to the new dimensions
        /// </summary>
        /// <param name="srcImage"></param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap ResizeImage(System.Drawing.Bitmap srcImage, int newWidth, int newHeight)
        {
            if (srcImage == null)
                throw new DjvuArgumentNullException(nameof(srcImage));

            // Check if the image needs resizing
            if (srcImage.Width == newWidth && srcImage.Height == newHeight)
                return srcImage;

            if (newWidth <= 0 || newHeight <= 0)
                throw new DjvuArgumentException(
                    $"Invalid new image dimensions width: {newWidth}, height: {newHeight}",
                    nameof(newWidth) + " " + nameof(newHeight));

            // Resize the image
            System.Drawing.Bitmap newImage = new System.Drawing.Bitmap(newWidth, newHeight);

            using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new System.Drawing.Rectangle(0, 0, newWidth, newHeight));
            }

            srcImage.Dispose();

            return newImage;
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

        /// <summary>
        /// Converts the pixel data to a bitmap image
        /// </summary>
        /// <param name="pixels"></param>
        /// <returns></returns>
        internal static unsafe System.Drawing.Bitmap ConvertDataToImage(int[] pixels, int width, int height)
        {
            if (width <= 0 || height <= 0)
                return null;

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData bits = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bmp.PixelFormat);

            for (int y = 0; y < height; y++)
            {
                var row = (int*)((byte*)bits.Scan0 + (y * bits.Stride));

                for (int x = 0; x < width; x++)
                    row[x] = pixels[y * width + x];
            }

            bmp.UnlockBits(bits);

            return bmp;
        }
    }
}
