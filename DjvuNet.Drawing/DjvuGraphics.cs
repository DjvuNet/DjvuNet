using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DjvuNet.DataChunks;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.JB2;
using DjvuNet.Utilities;
using DjvuNet.Wavelet;
using Bitmap = System.Drawing.Bitmap;
using Rectangle = System.Drawing.Rectangle;
using GBitmap = DjvuNet.Graphics.Bitmap;
using GMap = DjvuNet.Graphics.IMap;
using GRect = DjvuNet.Graphics.Rectangle;

namespace DjvuNet.Drawing
{
    public static class DjvuGraphics
    {
        /// <summary>
        /// Gets the image for the page
        /// </summary>
        /// <returns>
        /// <see cref="Bitmap"/>Bitmap image.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe Bitmap BuildImage(this DjvuImage image, int subsample = 1)
        {
            Verify.SubsampleRange(subsample);

            lock (image.LoadingLock)
            {
                Bitmap background = image.GetBackgroundImage(subsample, true);

                // TODO ETW logging goes here

                using (Bitmap foreground = image.GetForegroundImage(subsample, true))
                {
                    using (Bitmap mask = image.GetMaskImage(subsample, true))
                    {
                        image.HasLoaded = true;

                        BitmapData backgroundData =
                            background.LockBits(new Rectangle(0, 0, background.Width, background.Height),
                                                ImageLockMode.ReadWrite, background.PixelFormat);
                        int backgroundPixelSize = GetPixelSize(backgroundData.PixelFormat);

                        BitmapData foregroundData =
                            foreground.LockBits(new Rectangle(0, 0, foreground.Width, foreground.Height),
                                                ImageLockMode.ReadOnly, foreground.PixelFormat);
                        int foregroundPixelSize = GetPixelSize(foregroundData.PixelFormat);

                        BitmapData maskData = mask.LockBits(new Rectangle(0, 0, mask.Width, mask.Height),
                                                            ImageLockMode.ReadOnly, mask.PixelFormat);

                        //int maskPixelSize = GetPixelSize(maskData);

                        int bgndHeight = background.Height;
                        int bgndWidth = background.Width;

                        int fgndHeight = foreground.Height;
                        int fgndWidth = foreground.Width;

                        int maskHeight = mask.Height;
                        int maskWidth = mask.Width;

                        int maskbgnH = maskHeight / bgndHeight;
                        int maskfgnH = maskHeight / fgndHeight;

                        int maskbgnW = maskWidth / bgndWidth;
                        int maskfgnW = maskWidth / fgndWidth;

                        //Parallel.For(
                        //    0,
                        //    height,
                        //    y =>
                        //    {

                        for (int y = 0, yf = 0, yb = 0; y < maskHeight && yb < bgndHeight && yf < fgndHeight; ++y, yf = yb = y)
                        {
                            byte* maskRow = (byte*)maskData.Scan0 + (y * maskData.Stride);
                            DjvuNet.Graphics.Pixel* backgroundRow = (DjvuNet.Graphics.Pixel*)(backgroundData.Scan0 + (yb * backgroundData.Stride));
                            DjvuNet.Graphics.Pixel* foregroundRow = (DjvuNet.Graphics.Pixel*)(foregroundData.Scan0 + (yf * foregroundData.Stride));

                            for (int x = 0, xf = 0, xb = 0; x < bgndWidth && xb < maskWidth && xf < fgndWidth; x++)
                            {
                                // Check if the mask byte is set
                                if (maskRow[x] > 0)
                                {
                                    DjvuNet.Graphics.Pixel xF = foregroundRow[xf];

                                    if (image.IsInverted)
                                    {
                                        backgroundRow[xb] = InvertColor(xF);
                                    }
                                    else
                                    {
                                        backgroundRow[xb] = xF;
                                    }
                                }
                                else if (image.IsInverted)
                                {
                                    backgroundRow[xb] = InvertColor(backgroundRow[xb]);
                                }

                                if (x >= 0)
                                {
                                    if (x % maskbgnW == 0)
                                    {
                                        xb++;
                                    }

                                    if (x % maskfgnW == 0)
                                    {
                                        xf++;
                                    }
                                }
                            }

                            if (y >= 0)
                            {
                                if (y % maskbgnH == 0)
                                {
                                    yb++;
                                }

                                if (y % maskfgnH == 0)
                                {
                                    yf++;
                                }
                            }
                        }
                        //});

                        mask.UnlockBits(maskData);
                        foreground.UnlockBits(foregroundData);
                        background.UnlockBits(backgroundData);

                        return background;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a complete image for the page
        /// </summary>
        /// <returns>
        /// <see cref="Bitmap"/>Bitmap page image.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static Bitmap BuildPageImage(this DjvuPageVisuals image, bool rebuild = false)
        {
            const int subsample = 1;

            int width = image.Page.Width / subsample;
            int height = image.Page.Height / subsample;
            GMap map = null;
            Rectangle rect = new Rectangle(0, 0, width, height);
            Bitmap retVal = null;

            if (rebuild || image.Image == null)
            {
                map = image.Page.GetMap(new GRect(0, 0, width, height), subsample, null);
                if (map == null)
                {
                    return new Bitmap(width, height);
                }

                if (map.BytesPerPixel == 3)
                {
                    const PixelFormat format = PixelFormat.Format24bppRgb;
                    retVal = ImageFromMap(map, rect, format);
                }
                else if (map.BytesPerPixel == 1)
                {
                    const PixelFormat format = PixelFormat.Format8bppIndexed;
                    retVal = ImageFromMap(map, rect, format);
                }
            }
            else
            {
                retVal = image.Image;
            }

            if (map.BytesPerPixel == 3 && image.IsInverted)
            {
                retVal = DjvuImage.InvertColor(retVal);
            }
            else if (map.BytesPerPixel == 1)
            {
                System.Drawing.Imaging.ColorPalette palette = retVal.Palette;

                if (!image.IsInverted)
                {
                    for (int i = 0; i < 256; i++)
                    {
                        palette.Entries[i] = Color.FromArgb(i, i, i);
                    }

                    retVal.Palette = palette;
                }
                else
                {
                    int j = 0;
                    for (int i = 0; i < 256; i++)
                    {
                        j = 255 - i;
                        palette.Entries[i] = Color.FromArgb(j, j, j);
                    }
                    retVal.Palette = palette;
                }
            }

            return retVal;

            //int[] pixels = new int[width * height];

            //map.FillRgbPixels(0, 0, width, height, pixels, 0, width);
            //var image = ConvertDataToImage(pixels);

            //if (IsInverted == true)
            //    image = InvertImage(image);

            //return image;
        }

        /// <summary>
        /// Converts the pixel data to a bitmap image
        /// </summary>
        /// <param name="pixels"></param>
        /// <returns></returns>
        public static unsafe Bitmap ConvertDataToImage(int[] pixels, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bits = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bmp.PixelFormat);

            // Value of 4 is the size of PixelFormat.Format32bppArgb
            // keep it synchronized with used Bitmap PixelFormat
            int bytesPerRow = width * 4;

            fixed (int* pixelsPtr = pixels)
            {
                byte* pixelsNativePtr = (byte*)pixelsPtr;
                for (int y = 0; y < height; y++)
                {
                    var rowPtr = (int*)((byte*)bits.Scan0 + (y * bits.Stride));
                    pixelsNativePtr += (y * bytesPerRow);
                    MemoryUtilities.MoveMemory(rowPtr, pixelsNativePtr, bytesPerRow);
                }
            }

            bmp.UnlockBits(bits);

            return bmp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="imageColor"></param>
        /// <returns></returns>
        public static Bitmap CreateBlankImage(this DjvuImage image, Brush imageColor)
        {
            return CreateBlankImage(imageColor, image.Page.Width, image.Page.Height);
        }

        /// <summary>
        /// Creates a blank image with the given color
        /// </summary>
        /// <param name="imageColor"></param>
        /// <returns></returns>
        public static Bitmap CreateBlankImage(Brush imageColor, int width, int height)
        {
            Bitmap newBackground = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            // Fill the whole image with white
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(newBackground))
            {
                g.FillRegion(imageColor, new Region(new Rectangle(0, 0, width, height)));
            }

            return newBackground;
        }

        /// <summary>
        /// Extracts a thumbnail image for the page
        /// </summary>
        /// <returns></returns>
        public static Bitmap ExtractThumbnailImage(this DjvuImage image)
        {
            if (image.Page.Thumbnail != null)
            {
                return image.Page.Thumbnail.Image.ToImage();
            }

            Bitmap result = BuildImage(image);
            var scaleAmount = (double)128 / result.Width;

            return DjvuImage.ResizeImage(result, (int)(result.Width * scaleAmount), (int)(result.Height * scaleAmount));
        }

        /// <summary>
        /// Gets the background image for the page
        /// </summary>
        /// <returns></returns>
        public static Bitmap GetBackgroundImage(this DjvuImage image, int subsample, bool resizeImage = false)
        {
            Verify.SubsampleRange(subsample);

            int width = image.Page.Width;
            int height = image.Page.Height;

            BG44Chunk[] backgrounds = image.Page.PageForm?.GetChildrenItems<BG44Chunk>();

            if ((backgrounds == null || backgrounds.Length == 0) && width > 0 && height > 0)
            {
                return CreateBlankImage(Brushes.White, width, height);
            }

            // Get the composite background image
            Wavelet.IInterWavePixelMap backgroundMap = null;

            lock (image.LoadingLock)
            {
                foreach (BG44Chunk background in backgrounds)
                {
                    if (backgroundMap == null)
                    {
                        // Get the initial image
                        backgroundMap = background.BackgroundImage;
                    }
                    else
                    {
                        if (!image.IsBackgroundDecoded)
                        {
                            background.ProgressiveDecodeBackground(backgroundMap);
                        }
                    }
                }

                image.IsBackgroundDecoded = true;
            }

            Bitmap result = backgroundMap.GetPixelMap().ToImage();

            if (resizeImage)
            {
                int newWidth = width / subsample;
                int newHeight = height / subsample;
                return ResizeImage(result, newWidth, newHeight);
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        /// Gets the foreground image for the page.
        /// </summary>
        /// <param name="resizeToPage"></param>
        /// <returns></returns>
        public static Bitmap GetForegroundImage(this DjvuImage image, int subsample, bool resizeImage = false)
        {
            Verify.SubsampleRange(subsample);

            lock (image.LoadingLock)
            {
                Bitmap result = null;

                JB2Image jb2image = null;
                IInterWavePixelMap iwPixelMap = image.Page.ForegroundIWPixelMap;

                if (iwPixelMap != null)
                {
                    result = image.Page.ForegroundIWPixelMap.GetPixelMap().ToImage();
                }
                else if ((jb2image = image.Page.ForegroundJB2Image) != null)
                {
                    if (image.Page.ForegroundPalette == null)
                    {
                        result = jb2image.GetBitmap(1, GBitmap.BorderSize).ToImage();
                    }
                    else
                    {
                        result = jb2image.GetPixelMap(image.Page.ForegroundPalette, 1, 16).ToImage();
                    }
                }
                else if (iwPixelMap == null && jb2image == null)
                {
                    result = CreateBlankImage(Brushes.Black, image.Page.Width / subsample, image.Page.Height / subsample);
                }

                return resizeImage ? ResizeImage(result, image.Page.Width / subsample, image.Page.Height / subsample) : result;
            }
        }

        /// <summary>
        /// Gets mask image for the page.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="subsample"></param>
        /// <param name="resizeImage"></param>
        /// <returns></returns>
        public static Bitmap GetMaskImage(this DjvuImage image, int subsample, bool resizeImage = false)
        {
            Verify.SubsampleRange(subsample);

            if (image.Page.ForegroundJB2Image == null)
            {
                return new Bitmap(image.Page.Width / subsample, image.Page.Height / subsample, PixelFormat.Format8bppIndexed);
            }

            lock (image.LoadingLock)
            {
                Bitmap result = image.Page.ForegroundJB2Image.GetBitmap(subsample, GBitmap.BorderSize).ToImage();
                return resizeImage ? ResizeImage(result, image.Page.Width / subsample, image.Page.Height / subsample) : result;
            }
        }

        public static Bitmap CopyDataToBitmap(int width, int height, IntPtr data, long length, PixelFormat format, int bytesPerSrcRow = 0)
        {
            Bitmap bmp = null;
            BitmapData bmpData = null;

            try
            {
                bmp = new Bitmap(width, height, format);
                bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);

                int pixelSize = Image.GetPixelFormatSize(bmp.PixelFormat)/8;
                int bytesPerRow = bytesPerSrcRow == 0 ? bmp.Width * pixelSize : bytesPerSrcRow;

                IntPtr dataPtr = bmpData.Scan0;

                for (int i = 0; i < height; i++)
                {
                    MemoryUtilities.MoveMemory(dataPtr, data, bytesPerRow);
                    dataPtr = (IntPtr)((long)dataPtr + bmpData.Stride);
                    data = (IntPtr)((long)data + bytesPerRow);
                }
            }
            catch (Exception ex)
            {
                throw new DjvuAggregateException(ex);
            }
            finally
            {
                bmp?.UnlockBits(bmpData);
            }

            return bmp;
        }

        /// <summary>
        /// Gets the pixel size for the pixel data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPixelSize(PixelFormat data)
        {
            switch (data)
            {
                case PixelFormat.Format8bppIndexed:
                    return 1;
                case PixelFormat.Format16bppGrayScale:
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                case PixelFormat.Format16bppArgb1555:
                    return 2;
                case PixelFormat.Format24bppRgb:
                    return 3;
                case PixelFormat.Canonical:
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppRgb:
                    return 4;
                case PixelFormat.Format48bppRgb:
                    return 6;
                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                    return 8;
            }

            throw new DjvuFormatException("Unsupported image format: " + data);
        }

        /// <summary>
        /// Utility conversion method allowing to convert object implementing <see cref="DjvuNet.Graphics.IMap"/>
        /// interface to <see cref="Bitmap"/> object.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="rect"></param>
        /// <param name="format"></param>
        /// <returns>Returns <see cref="Bitmap"/> object which should be disposed after use by caller. </returns>
        public static Bitmap ImageFromMap(GMap map, Rectangle rect, PixelFormat format)
        {
            Bitmap retVal = new Bitmap(rect.Width, rect.Height, format);

            BitmapData bmpData = retVal.LockBits(rect, ImageLockMode.WriteOnly, format);

            int pixelSize = GetPixelSize(format);
            int bytesPerRow = pixelSize * rect.Width;

            GCHandle hMapData = GCHandle.Alloc(map.Data, GCHandleType.Pinned);
            IntPtr pMapData = hMapData.AddrOfPinnedObject();

            for (int i = 0; i < rect.Height; i++)
            {
                IntPtr destPtr = bmpData.Scan0 + (bmpData.Stride * i);
                IntPtr srcPtr = (IntPtr)((long)pMapData + (i * bytesPerRow));

                MemoryUtilities.MoveMemory(destPtr, srcPtr, bytesPerRow);
            }

            if (hMapData.IsAllocated)
            {
                hMapData.Free();
            }

            retVal.UnlockBits(bmpData);
            return retVal;
        }

        /// <summary>
        /// Invert <see cref="Bitmap"/> color.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Inverts the color value
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint InvertColor(uint color)
        {
            return 0x00FFFFFFu ^ color;
        }

        /// <summary>
        /// Inverts the color value
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int InvertColor(int color)
        {
            return 0x00FFFFFF ^ color;
        }

        /// <summary>
        /// Invert <see cref="DjvuNet.Graphics.Pixel"/> color.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Graphics.Pixel InvertColor(Graphics.Pixel color)
        {
            return new Graphics.Pixel((sbyte)(color.Blue ^ unchecked((sbyte)0xff)), (sbyte)(color.Green ^ unchecked((sbyte)0xff)), (sbyte)(color.Red ^ unchecked((sbyte)0xff)));
        }

        /// <summary>
        /// Resizes the <see cref="System.Drawing.Bitmap"/> to the new dimensions.
        /// </summary>
        /// <param name="srcImage"></param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        public static Bitmap ResizeImage(Bitmap srcImage, int newWidth, int newHeight)
        {
            if (srcImage == null)
            {
                throw new DjvuArgumentNullException(nameof(srcImage));
            }

            // Check if the image needs resizing
            if (srcImage.Width == newWidth && srcImage.Height == newHeight)
            {
                return srcImage;
            }

            if (newWidth <= 0 || newHeight <= 0)
            {
                throw new DjvuArgumentException(
                    $"Invalid new image dimensions width: {newWidth}, height: {newHeight}",
                    nameof(newWidth) + " " + nameof(newHeight));
            }

            // Resize the image
            Bitmap newImage = new Bitmap(newWidth, newHeight, srcImage.PixelFormat);

            using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new Rectangle(0, 0, newWidth, newHeight));
            }

            srcImage.Dispose();

            return newImage;
        }

        /// <summary>
        /// Convert <see cref="DjvuNet.Graphics.Map"/> to <see cref="System.Drawing.Bitmap"/>
        /// </summary>
        /// <param name="map">Source image</param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public static Bitmap ToImage(this Map map, RotateFlipType rotation = RotateFlipType.Rotate180FlipX)
        {
            PixelFormat format;
            if (map.BytesPerPixel == 1)
                format = PixelFormat.Format8bppIndexed;
            else if (map.BytesPerPixel == 2)
                format = PixelFormat.Format16bppRgb555;
            else if (map.BytesPerPixel == 3)
                format = PixelFormat.Format24bppRgb;
            else if (map.BytesPerPixel == 4)
                format = PixelFormat.Format32bppArgb;
            else if (map.BytesPerPixel == 6)
                format = PixelFormat.Format48bppRgb;
            else if (map.BytesPerPixel == 8)
                format = PixelFormat.Format64bppArgb;
            else
                throw new DjvuFormatException($"Unsupported pixel format for byte count: {map.BytesPerPixel}");

            int bytesPerRow = map switch
            {
                DjvuNet.Graphics.Bitmap { BytesPerRow: int bitmapBytesPerRow } => bitmapBytesPerRow,
                DjvuNet.Graphics.PixelMap _ => map.BytesPerPixel * map.Width,
                { } => throw new DjvuNotSupportedException($"Unsupported type: {map.GetType()}"),
            };

            GCHandle hData = default;
            Bitmap image = null;
            try
            {
                hData = GCHandle.Alloc(map.Data, GCHandleType.Pinned);
                image = CopyDataToBitmap(map.Width, map.Height, hData.AddrOfPinnedObject(), map.Data.Length, format, bytesPerRow);
            }
            catch (ArgumentException aex)
            {
                throw new DjvuAggregateException("Failed to copy data to Sytem.Drawing.Bitmap.", aex);
            }
            finally
            {
                if (hData.IsAllocated)
                {
                    hData.Free();
                }
            }

            image.RotateFlip(rotation);

            return image;
        }

        /// <summary>
        /// Transform a <see cref="System.Drawing.Bitmap"/> based on tranformation encoded in the <see cref="System.Drawing.Imaging.ColorMatrix"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="colorMatrix"></param>
        /// <returns></returns>
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
