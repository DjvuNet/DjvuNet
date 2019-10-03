using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DjvuNet.DataChunks;
using DjvuNet.Errors;
using DjvuNet.JB2;
using DjvuNet.Utilities;
using DjvuNet.Wavelet;
using Bitmap = System.Drawing.Bitmap;
using GMap = DjvuNet.Graphics.IMap;
using GRect = DjvuNet.Graphics.Rectangle;
using Rectangle = System.Drawing.Rectangle;

namespace DjvuNet
{
    public class DjvuImage : IDjvuImage
    {
        private IDjvuPage _Page;
        private IDjvuDocument _Document;

        /// <summary>
        /// True if the page has been previously loaded, false otherwise
        /// </summary>
        private bool _HasLoaded;
        private object _LoadingLock = new object();
        private bool _IsBackgroundDecoded;
        private bool _IsInverted;

        public IDjvuPage Page { get { return _Page; } }

        public IDjvuDocument Document { get { return _Document; } }

        /// <summary>
        /// True if the image is inverted, false otherwise
        /// </summary>
        public bool IsInverted
        {
            get { return _IsInverted; }

            set
            {
                if (_IsInverted != value)
                {
                    _IsInverted = value;
                    OnPropertyChanged(nameof(IsInverted));
                }
            }
        }

        #region IsPageImageCached

        private bool _IsPageImageCached;

        /// <summary>
        /// True if the page image is cached, false otherwise
        /// </summary>
        public bool IsPageImageCached
        {
            get { return _IsPageImageCached; }

            set
            {
                if (_IsPageImageCached != value)
                {
                    _IsPageImageCached = value;
                    _Image = null;
                    OnPropertyChanged(nameof(IsPageImageCached));
                }
            }
        }

        #endregion IsPageImageCached

        public event PropertyChangedEventHandler PropertyChanged;

        public DjvuImage() { }

        public DjvuImage(IDjvuPage page)
        {
            _Page = page ?? throw new DjvuArgumentNullException(nameof(page));
            _Page.PropertyChanged += PagePropertyChanged;
            _Document = page.Document;
        }

        private void PagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_Page.IsInverted):
                    IsInverted = _Page.IsInverted;
                    ClearImage();
                    ThumbnailImage = InvertImage(ThumbnailImage);
                    break;
            }
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
            {
                g.FillRegion(imageColor, new Region(new System.Drawing.Rectangle(0, 0, width, height)));
            }

            return newBackground;
        }

        public static System.Drawing.Bitmap ImageFromMap(GMap map, Rectangle rect, PixelFormat format)
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
                IntPtr srcPtr = (IntPtr)((long) pMapData + (i * bytesPerRow));

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
        /// Resizes the image to the new dimensions
        /// </summary>
        /// <param name="srcImage"></param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap ResizeImage(System.Drawing.Bitmap srcImage, int newWidth, int newHeight)
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
            {
                return null;
            }

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData bits = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bmp.PixelFormat);

            // Value of 4 is the size of PixelFormat.Format32bppArgb
            // keep it synchronized with used Bitmap PixelFormat
            int bytesPerRow = width * 4;

            fixed (int* pixelsPtr = pixels)
            {
                byte* pixelsNativePtr = (byte*) pixelsPtr;
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
        /// Gets the pixel size for the pixel data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetPixelSize(PixelFormat data)
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

        #region Image

        private System.Drawing.Bitmap _Image;

        /// <summary>
        /// Gets the image for the page
        /// </summary>
        public System.Drawing.Bitmap Image
        {
            get
            {
                if (_Image == null)
                {
                    _Image = BuildImage();
                    OnPropertyChanged(nameof(Image));
                }

                return _Image;
            }

            internal set
            {
                if (_Image != value)
                {
                    _Image = value;
                    OnPropertyChanged(nameof(Image));
                }
            }
        }

        #endregion Image

        #region ThumbnailImage

        private System.Drawing.Bitmap _ThumbnailImage;

        /// <summary>
        /// Gets or sets the thumbnail image for the page
        /// </summary>
        public System.Drawing.Bitmap ThumbnailImage
        {
            get { return _ThumbnailImage; }

            set
            {
                if (ThumbnailImage != value)
                {
                    _ThumbnailImage = value;
                    OnPropertyChanged(nameof(ThumbnailImage));
                }
            }
        }

        #endregion ThumbnailImage

        #region IDisposable implementation

        protected bool _Disposed;

        public bool Disposed { get { return _Disposed; } }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
            }

            _Image?.Dispose();
            _Image = null;

            _ThumbnailImage?.Dispose();
            _ThumbnailImage = null;

            _Disposed = true;
        }

        ~DjvuImage()
        {
            Dispose(false);
        }

        #endregion IDisposable implementation

        /// <summary>
        /// Preload the page images by rendering background, foreground and text images.
        /// </summary>
        public void Preload()
        {
            lock (_LoadingLock)
            {
                if (!_HasLoaded)
                {
                    // Build all the images
                    GetBackgroundImage(1, true);
                    GetForegroundImage(1, true);
                    GetTextImage(1, true);

                    _HasLoaded = true;
                }
            }
        }

        /// <summary>
        /// Clears the stored image from memory
        /// </summary>
        public void ClearImage()
        {
            IsPageImageCached = false;

            if (_Image != null)
            {
                _Image.Dispose();
                _Image = null;
            }
        }

        /// <summary>
        /// Resizes the pages image to the new dimensions
        /// </summary>
        /// <param name="srcImage"></param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        public System.Drawing.Bitmap ResizeImage(int newWidth, int newHeight)
        {
            return DjvuImage.ResizeImage(Image, newWidth, newHeight);
        }

        /// <summary>
        /// Extracts a thumbnail image for the page
        /// </summary>
        /// <returns></returns>
        public Bitmap ExtractThumbnailImage()
        {
            if (_Page.Thumbnail != null)
            {
                return _Page.Thumbnail.Image.ToImage();
            }

            Bitmap result = BuildImage();
            var scaleAmount = (double)128 / result.Width;

            result = DjvuImage.ResizeImage(result, (int)(result.Width * scaleAmount), (int)(result.Height * scaleAmount));

            return result;
        }

        /// <summary>
        /// Gets a complete image for the page
        /// </summary>
        /// <returns>
        /// <see cref="System.Drawing.Bitmap"/>Bitmap page image.
        /// </returns>
#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public System.Drawing.Bitmap BuildPageImage(bool rebuild = false)
        {
            const int subsample = 1;

            int width = _Page.Width / subsample;
            int height = _Page.Height / subsample;
            GMap map = null;
            Rectangle rect = new Rectangle(0, 0, width, height);
            Bitmap retVal = null;

            if (rebuild || _Image == null)
            {
                map = _Page.GetMap(new GRect(0, 0, width, height), subsample, null);
                if (map == null)
                {
                    return new Bitmap(width, height);
                }

                if (map.BytesPerPixel == 3)
                {
                    const PixelFormat format = PixelFormat.Format24bppRgb;
                    retVal = DjvuImage.ImageFromMap(map, rect, format);
                }
                else if (map.BytesPerPixel == 1)
                {
                    const PixelFormat format = PixelFormat.Format8bppIndexed;
                    retVal = DjvuImage.ImageFromMap(map, rect, format);
                }
            }
            else
            {
                retVal = _Image;
            }

            if (map.BytesPerPixel == 3 && IsInverted)
            {
                retVal = DjvuImage.InvertColor(retVal);
            }
            else if (map.BytesPerPixel == 1)
            {
                System.Drawing.Imaging.ColorPalette palette = retVal.Palette;

                if (!IsInverted)
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
            //
            // TODO Fix image skew
            //

            Verify.SubsampleRange(subsample);

            lock (_LoadingLock)
            {
                Stopwatch stopWatch = Stopwatch.StartNew();

                System.Drawing.Bitmap background = GetBackgroundImage(subsample, false);

                stopWatch.Stop();

                // TODO ETW logging goes here

                stopWatch.Restart();

                using (System.Drawing.Bitmap foreground = GetForegroundImage(subsample, false))
                {
                    stopWatch.Stop();
                    // TODO ETW logging goes here

                    stopWatch.Restart();

                    using (System.Drawing.Bitmap mask = GetTextImage(subsample, false))
                    {
                        stopWatch.Stop();
                        // TODO ETW logging goes here

                        stopWatch.Restart();

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

                        for (int y = 0, yf = 0, yb = 0; y < maskHeight && yb < bgndHeight && yf < fgndHeight; y++)
                        {
                            byte* maskRow = (byte*)maskData.Scan0 + (y * maskData.Stride);
                            uint* backgroundRow = (uint*)(backgroundData.Scan0 + (yb * backgroundData.Stride));
                            uint* foregroundRow = (uint*)(foregroundData.Scan0 + (yf * foregroundData.Stride));

                            for (int x = 0, xf = 0, xb = 0; x < bgndWidth && xb < maskWidth && xf < fgndWidth; x++)
                            {
                                // Check if the mask byte is set
                                if (maskRow[x] > 0)
                                {
                                    uint xF = foregroundRow[xf];

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
                                    uint xB = backgroundRow[xb];
                                    backgroundRow[xb] = InvertColor(xB);
                                }

                                if (x > 0)
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

                            if (y > 0)
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

                        stopWatch.Stop();
                        // TODO ETW logging goes here

                        return background;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the foreground image for the page
        /// </summary>
        /// <param name="resizeToPage"></param>
        /// <returns></returns>
        internal System.Drawing.Bitmap GetForegroundImage(int subsample, bool resizeImage = false)
        {
            Verify.SubsampleRange(subsample);

            lock (_LoadingLock)
            {
                Bitmap result = null;

                JB2Image jb2image = null;
                IInterWavePixelMap iwPixelMap = _Page.ForegroundIWPixelMap;

                if (iwPixelMap != null)
                {
                    result = _Page.ForegroundIWPixelMap.GetPixelMap().ToImage();
                }
                else if ((jb2image = _Page.ForegroundJB2Image) != null)
                {
                    result = jb2image.GetBitmap().ToImage();
                }
                else if (iwPixelMap == null && jb2image == null)
                {
                    result = DjvuImage.CreateBlankImage(Brushes.Black, _Page.Width / subsample, _Page.Height / subsample);
                }

                return resizeImage ? DjvuImage.ResizeImage(result, _Page.Width / subsample, _Page.Height / subsample) : result;
            }
        }

        internal System.Drawing.Bitmap GetTextImage(int subsample, bool resizeImage = false)
        {
            Verify.SubsampleRange(subsample);

            if (_Page.ForegroundJB2Image == null)
            {
                return new System.Drawing.Bitmap(_Page.Width / subsample, _Page.Height / subsample);
            }

            lock (_LoadingLock)
            {
                Bitmap result = _Page.ForegroundJB2Image.GetBitmap(subsample, 4).ToImage();
                return resizeImage ? DjvuImage.ResizeImage(result, _Page.Width / subsample, _Page.Height / subsample) : result;
            }
        }

        /// <summary>
        /// Gets the background image for the page
        /// </summary>
        /// <returns></returns>
        internal System.Drawing.Bitmap GetBackgroundImage(int subsample, bool resizeImage = false)
        {
            Verify.SubsampleRange(subsample);

            int width = _Page.Width;
            int height = _Page.Height;

            BG44Chunk[] backgrounds = _Page.PageForm?.GetChildrenItems<BG44Chunk>();

            if ((backgrounds == null || backgrounds.Length == 0) && width > 0 && height > 0)
            {
                return DjvuImage.CreateBlankImage(Brushes.White, width, height);
            }

            // Get the composite background image
            Wavelet.IInterWavePixelMap backgroundMap = null;

            lock (_LoadingLock)
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
                        if (!_IsBackgroundDecoded)
                        {
                            background.ProgressiveDecodeBackground(backgroundMap);
                        }
                    }
                }

                _IsBackgroundDecoded = true;
            }

            Bitmap result = backgroundMap.GetPixelMap().ToImage();

            if (resizeImage)
            {
                int newWidth = width / subsample;
                int newHeight = height / subsample;
                return DjvuImage.ResizeImage(result, newWidth, newHeight);
            }
            else
            {
                return result;
            }
        }

        internal static unsafe System.Drawing.Bitmap InvertImage(System.Drawing.Bitmap sourceBitmap)
        {
            if (sourceBitmap == null)
            {
                return null;
            }

            Bitmap invertedBitmap = Unsafe.As<System.Drawing.Bitmap>(sourceBitmap.Clone());

            BitmapData imageData = invertedBitmap.LockBits(new System.Drawing.Rectangle(0, 0, invertedBitmap.Width, invertedBitmap.Height),
                                                  ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            int height = invertedBitmap.Height;
            int width = invertedBitmap.Width;

            for (int y = 0; y < height; y++)
            //Parallel.For(
            //    0,
            //    height,
            //    y =>
            {
                uint* imageRow = (uint*)(imageData.Scan0 + (y * imageData.Stride));

                for (int x = 0; x < width; x++)
                {
                    // Check if the mask byte is set
                    imageRow[x] = InvertColor(imageRow[x]);
                }
                //});
            }

            invertedBitmap.UnlockBits(imageData);

            return invertedBitmap;
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
        /// Sends the property changed notification
        /// </summary>
        /// <param name="property"></param>
        protected void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
