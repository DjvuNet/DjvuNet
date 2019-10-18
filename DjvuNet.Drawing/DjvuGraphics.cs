using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.Wavelet;

namespace DjvuNet.Drawing
{
    public static class DjvuGraphics
    {
        /// <summary>
        /// Gets the image for the page
        /// </summary>
        /// <returns>
        /// <see cref="System.Drawing.Bitmap"/>Bitmap image.
        /// </returns>
#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public unsafe System.Drawing.Bitmap BuildImage(int subsample = 1)
        {
            Verify.SubsampleRange(subsample);

            lock (_LoadingLock)
            {
                System.Drawing.Bitmap background = GetBackgroundImage(subsample, true);

                // TODO ETW logging goes here

                using (System.Drawing.Bitmap foreground = GetForegroundImage(subsample, true))
                {
                    using (System.Drawing.Bitmap mask = GetMaskImage(subsample, true))
                    {
                        _HasLoaded = true;

                        BitmapData backgroundData =
                            background.LockBits(new System.Drawing.Rectangle(0, 0, background.Width, background.Height),
                                                ImageLockMode.ReadWrite, background.PixelFormat);
                        int backgroundPixelSize = DjvuImage.GetPixelSize(backgroundData.PixelFormat);

                        BitmapData foregroundData =
                            foreground.LockBits(new System.Drawing.Rectangle(0, 0, foreground.Width, foreground.Height),
                                                ImageLockMode.ReadOnly, foreground.PixelFormat);
                        int foregroundPixelSize = DjvuImage.GetPixelSize(foregroundData.PixelFormat);

                        BitmapData maskData = mask.LockBits(new System.Drawing.Rectangle(0, 0, mask.Width, mask.Height),
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

                                    if (_IsInverted)
                                    {
                                        backgroundRow[xb] = InvertColor(xF);
                                    }
                                    else
                                    {
                                        backgroundRow[xb] = xF;
                                    }
                                }
                                else if (_IsInverted)
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

        public static System.Drawing.Bitmap ToImage(this Map map, RotateFlipType rotation = RotateFlipType.Rotate180FlipX)
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
                DjvuNet.Graphics.PixelMap _ => map.BytesPerPixel * map. Width,
                { } => throw new DjvuNotSupportedException($"Unsupported type: {map.GetType()}"),
            };

            GCHandle hData = default;
            System.Drawing.Bitmap image = null;
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

        public static System.Drawing.Bitmap CopyDataToBitmap(int width, int height, IntPtr data, long length, PixelFormat format, int bytesPerSrcRow = 0)
        {
            System.Drawing.Bitmap bmp = null;
            BitmapData bmpData = null;

            try
            {
                bmp = new System.Drawing.Bitmap(width, height, format);
                bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);

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
    }
}
