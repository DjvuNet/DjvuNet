using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using GdiColorPalette = System.Drawing.Imaging.ColorPalette;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using DjvuNet.DataChunks;
using DjvuNet.Errors;
using DjvuNet.Extensions;
using DjvuNet.JB2;
using DjvuNet.Utilities;
using DjvuNet.Wavelet;
using Bitmap = System.Drawing.Bitmap;
using GdiGraphics = System.Drawing.Graphics;
using PixelMap = DjvuNet.Graphics.PixelMap;
using GBitmap = DjvuNet.Graphics.Bitmap;
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
        private bool _IsBackgroundDecoded;
        private bool _IsInverted;

        public object LoadingLock = new object();

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

        public bool IsBackgroundDecoded
        {
            get { return _IsBackgroundDecoded; }
            set
            {
                if (_IsBackgroundDecoded != value)
                {
                    _IsBackgroundDecoded = value;
                    OnPropertyChanged(nameof(IsBackgroundDecoded));
                }
            }
        }


        public bool HasLoaded
        {
            get { return _HasLoaded; }
            set
            {
                if (_HasLoaded != value)
                {
                    _HasLoaded = value;
                    OnPropertyChanged(nameof(HasLoaded));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DjvuImage() { }

        public DjvuImage(IDjvuPage page)
        {
            _Page = page ?? throw new DjvuArgumentNullException(nameof(page));
            _Page.PropertyChanged += PagePropertyChanged;
            _Document = page.Document;
            var doc = _Document as DjvuDocument;

            if (doc != null)
            {
                IsTesting = doc.IsTesting;
            }
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

        public Bitmap CreateBlankImage(Brush imageColor)
        {
            return CreateBlankImage(imageColor, _Page.Width, _Page.Height);
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
            using (GdiGraphics g = GdiGraphics.FromImage(newBackground))
            {
                g.FillRegion(imageColor, new Region(new System.Drawing.Rectangle(0, 0, width, height)));
            }

            return newBackground;
        }


        /// <summary>
        /// Resizes the image to the new dimensions
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

            using (GdiGraphics gr = GdiGraphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new System.Drawing.Rectangle(0, 0, newWidth, newHeight));
            }

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

            using (GdiGraphics g = GdiGraphics.FromImage(result))
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
        internal static unsafe Bitmap ConvertDataToImage(int[] pixels, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bits = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bmp.PixelFormat);

            // Value of 4 is the size of PixelFormat.Format32bppArgb
            // keep it synchronized with used Bitmap PixelFormat
            int bytesPerRow = width * 4;

            fixed (int* pixelsPtr = pixels)
            {
                byte* pixelsNativePtr = (byte*) pixelsPtr;
                for (int y = 0; y < height; y++)
                {
                    var rowPtr = (byte*)bits.Scan0 + (y * bits.Stride);
                    var srcRow = pixelsNativePtr + (y * bytesPerRow);
                    MemoryUtilities.MoveMemory(rowPtr, srcRow, bytesPerRow);
                }
            }

            bmp.UnlockBits(bits);

            return bmp;
        }

        internal static unsafe Bitmap ConvertDataToImageVectorized(int[] pixels, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bits = null;
            try
            {
                bits = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bmp.PixelFormat);


                // Value of 4 is the size of PixelFormat.Format32bppArgb
                // keep it synchronized with used Bitmap PixelFormat
                int bytesPerRow = width * 4;

                fixed (int* pixelsPtr = pixels)
                {
                    byte* pixelsNativePtr = (byte*)pixelsPtr;
                    for (int y = 0; y < height; y++)
                    {
                        var rowPtr = (byte*)bits.Scan0 + (y * bits.Stride);
                        var srcRow = pixelsNativePtr + (y * bytesPerRow);

#if NETCOREAPP
                        if (Vector256.IsHardwareAccelerated)
                        {
                            int x = 0;
                            for (; x <= bytesPerRow - 128; x += 128)
                            {
                                var v0 = Vector256.Load(srcRow + x);
                                var v1 = Vector256.Load(srcRow + x + 32);
                                var v2 = Vector256.Load(srcRow + x + 64);
                                var v3 = Vector256.Load(srcRow + x + 96);

                                v0.Store(rowPtr + x);
                                v1.Store(rowPtr + x + 32);
                                v2.Store(rowPtr + x + 64);
                                v3.Store(rowPtr + x + 96);
                            }
                            for (; x <= bytesPerRow - 32; x += 32)
                            {
                                Vector256.Load(srcRow + x).Store(rowPtr + x);
                            }
                            for (; x < bytesPerRow; x++)
                            {
                                rowPtr[x] = srcRow[x];
                            }
                        }
                        else
#endif
                        {
                            throw new PlatformNotSupportedException("Vectorized conversion is not supported on this platform.");
                        }
                    }
                }
            }
            finally
            {
                if (bits != null)
                {
                    bmp.UnlockBits(bits);
                }
            }

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

        private Bitmap _Image;

        /// <summary>
        /// Gets the image for the page
        /// </summary>
        public Bitmap Image
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

        private Bitmap _ThumbnailImage;

        /// <summary>
        /// Gets or sets the thumbnail image for the page
        /// </summary>
        public Bitmap ThumbnailImage
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

        #region Test Caching

        internal bool IsTesting { get; set; }

        private Bitmap _BackgroundImage;
        private Bitmap _ForegroundImage;
        private Bitmap _MaskImage;

        /// <summary>
        /// Lazy-initialized property to cache the intermediate background image during test execution.
        /// This is internal and conditional (IsTesting) to prevent memory bloat in production environments.
        /// Call ClearCachedIntermediateImages() to release the unmanaged resources when the test concludes.
        /// </summary>
        internal Bitmap BackgroundImage
        {
            get
            {
                if (_BackgroundImage == null && IsTesting)
                {
                    _BackgroundImage = GetBackgroundImage(1, true);
                }
                return _BackgroundImage;
            }
            set { _BackgroundImage = value; }
        }

        /// <summary>
        /// Lazy-initialized property to cache the intermediate foreground image during test execution.
        /// This is internal and conditional (IsTesting) to prevent memory bloat in production environments.
        /// Call ClearCachedIntermediateImages() to release the unmanaged resources when the test concludes.
        /// </summary>
        internal Bitmap ForegroundImage
        {
            get
            {
                if (_ForegroundImage == null && IsTesting)
                {
                    _ForegroundImage = GetForegroundImage(1, true);
                }
                return _ForegroundImage;
            }
            set { _ForegroundImage = value; }
        }

        /// <summary>
        /// Lazy-initialized property to cache the intermediate mask image during test execution.
        /// This is internal and conditional (IsTesting) to prevent memory bloat in production environments.
        /// Call ClearCachedIntermediateImages() to release the unmanaged resources when the test concludes.
        /// </summary>
        internal Bitmap MaskImage
        {
            get
            {
                if (_MaskImage == null && IsTesting)
                {
                    _MaskImage = GetMaskImage(1, true);
                }
                return _MaskImage;
            }
            set { _MaskImage = value; }
        }

        public void ClearIntermediateImages()
        {
            _BackgroundImage?.Dispose();
            _BackgroundImage = null;

            _ForegroundImage?.Dispose();
            _ForegroundImage = null;

            _MaskImage?.Dispose();
            _MaskImage = null;
        }

        #endregion Test Caching

        #region IDisposable implementation

        protected bool _Disposed;

        public bool Disposed { get { return _Disposed; } }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
            {
                return;
            }

            if (disposing)
            {
            }

            _Image?.Dispose();
            _Image = null;

            _ThumbnailImage?.Dispose();
            _ThumbnailImage = null;

            ClearIntermediateImages();

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
            lock (LoadingLock)
            {
                if (!HasLoaded)
                {
                    // Build all the images
                    GetBackgroundImage(1, true);
                    GetForegroundImage(1, true);
                    GetMaskImage(1, true);

                    HasLoaded = true;
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
        public Bitmap ResizeImage(int newWidth, int newHeight)
        {
            return DjvuImage.ResizeImage(Image, newWidth, newHeight);
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
        public Bitmap BuildPageImage(bool rebuild = false)
        {
            const int subsample = 1;

            int width = _Page.Width / subsample;
            int height = _Page.Height / subsample;
            Rectangle rect = new Rectangle(0, 0, width, height);
            Bitmap retVal = null;

            if (rebuild || _Image == null)
            {
                if (_Page.IsColor)
                {
                    PixelMap pixelMap = _Page.GetPixelMap(new GRect(0, 0, width, height), subsample, 0.0D, null);
                    if (pixelMap == null)
                    {
                        return new Bitmap(width, height);
                    }
                    retVal = pixelMap.ToImage();

                    if (IsInverted)
                    {
                        retVal = DjvuImage.InvertColor(retVal);
                    }
                }
                else
                {
                    GBitmap gBitmap = _Page.GetBitmap(new GRect(0, 0, width, height), subsample, 1);
                    if (gBitmap.Data == null)
                    {
                        return new Bitmap(width, height);
                    }
                    
                    retVal = gBitmap.ToImage();

                    GdiColorPalette palette = retVal.Palette;

                    if (!IsInverted)
                    {
                        for (int i = 0; i < 256; i++)
                        {
                            palette.Entries[i] = Color.FromArgb(i, i, i);
                        }
                    }
                    else
                    {
                        int j = 0;
                        for (int i = 0; i < 256; i++)
                        {
                            j = 255 - i;
                            palette.Entries[i] = Color.FromArgb(j, j, j);
                        }
                    }
                    
                    retVal.Palette = palette;
                }
            }
            else
            {
                retVal = _Image;

                // Handle cached image inversion
                if (IsInverted)
                {
                    if (_Page.IsColor)
                    {
                        retVal = DjvuImage.InvertColor(retVal);
                    }
                    else
                    {
                        // Clone utilizes a lightweight GDI+ copy-on-write mechanism. It shares the 
                        // underlying pixel memory but isolates the ColorPalette in the metadata,
                        // allowing us to safely mutate the clone's palette without a race condition.
                        retVal = retVal.Clone(new Rectangle(0, 0, retVal.Width, retVal.Height), retVal.PixelFormat);
                        GdiColorPalette palette = retVal.Palette;
                        for (int i = 0; i < 256; i++)
                        {
                            int j = 255 - i;
                            palette.Entries[i] = Color.FromArgb(j, j, j);
                        }
                        retVal.Palette = palette;
                    }
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
        public unsafe Bitmap BuildImage(int subsample = 1)
        {
            Verify.SubsampleRange(subsample);

            lock (LoadingLock)
            {
                bool useCache = IsTesting && subsample == 1;

                Bitmap background = useCache ? BackgroundImage : GetBackgroundImage(subsample, true);
                if (useCache && background != null)
                {
                    // Clone the background safely to avoid GDI+ "generic error" from Bitmap.Clone()
                    Bitmap safeClone = new Bitmap(background.Width, background.Height, background.PixelFormat);
                    // Handle indexed formats vs non-indexed formats safely
                    if ((background.PixelFormat & System.Drawing.Imaging.PixelFormat.Indexed) != 0)
                    {
                        var targetData = safeClone.LockBits(new System.Drawing.Rectangle(0, 0, safeClone.Width, safeClone.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, safeClone.PixelFormat);
                        var sourceData = background.LockBits(new System.Drawing.Rectangle(0, 0, background.Width, background.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, background.PixelFormat);
                        unsafe
                        {
                            int targetBytes = Math.Abs(targetData.Stride) * targetData.Height;
                            int sourceBytes = Math.Abs(sourceData.Stride) * sourceData.Height;
                            System.Buffer.MemoryCopy(sourceData.Scan0.ToPointer(), targetData.Scan0.ToPointer(), targetBytes, sourceBytes);
                        }
                        safeClone.UnlockBits(targetData);
                        background.UnlockBits(sourceData);
                        // Copy palette
                        var pal = safeClone.Palette;
                        for (int i = 0; i < background.Palette.Entries.Length; i++) pal.Entries[i] = background.Palette.Entries[i];
                        safeClone.Palette = pal;
                    }
                    else
                    {
                        using (GdiGraphics g = GdiGraphics.FromImage(safeClone))
                        {
                            g.DrawImage(background, new System.Drawing.Rectangle(0, 0, safeClone.Width, safeClone.Height));
                        }
                    }
                    background = safeClone;
                }

                // TODO ETW logging goes here

                Bitmap foreground = useCache ? ForegroundImage : GetForegroundImage(subsample, true);
                Bitmap mask = useCache ? MaskImage : GetMaskImage(subsample, true);

                try
                {
                    HasLoaded = true;

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

                    for (int y = 0, yf = 0, yb = 0; y < maskHeight && yb < bgndHeight && yf < fgndHeight; ++y)
                    {
                        byte* maskRow = (byte*)maskData.Scan0 + (y * maskData.Stride);
                        DjvuNet.Graphics.Pixel* backgroundRow = (DjvuNet.Graphics.Pixel*)(backgroundData.Scan0 + (yb * backgroundData.Stride));
                        DjvuNet.Graphics.Pixel* foregroundRow = (DjvuNet.Graphics.Pixel*)(foregroundData.Scan0 + (yf * foregroundData.Stride));

                        for (int x = 0, xf = 0, xb = 0; x < maskWidth && xb < bgndWidth && xf < fgndWidth; x++)
                        {
                            // Check if the mask byte is set
                            if (maskRow[x] > 0)
                            {
                                DjvuNet.Graphics.Pixel xF =  foregroundRow[xf];

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

                            if ((x + 1) % maskbgnW == 0)
                            {
                                xb++;
                            }

                            if ((x + 1) % maskfgnW == 0)
                            {
                                xf++;
                            }
                        }

                        if ((y + 1) % maskbgnH == 0)
                        {
                            yb++;
                        }

                        if ((y + 1) % maskfgnH == 0)
                        {
                            yf++;
                        }
                    }
                    //});

                    mask.UnlockBits(maskData);
                    foreground.UnlockBits(foregroundData);
                    background.UnlockBits(backgroundData);

                    return background;

                }
                finally
                {
                    if (!useCache)
                    {
                        foreground?.Dispose();
                        mask?.Dispose();
                    }
                }
            }
        }

        public Graphics.PixelMap BuildImageMap()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Extracts a thumbnail image for the page
        /// </summary>
        /// <returns></returns>
        public Bitmap ExtractThumbnailImage()
        {
            if (_Page.Thumbnail != null)
            {
                return ((Graphics.PixelMap)_Page.Thumbnail.Image).ToImage();
            }

            Bitmap result = Image;
            var scaleAmount = (double)128 / result.Width;

            return ResizeImage(result, (int)(result.Width * scaleAmount), (int)(result.Height * scaleAmount));
        }

        /// <summary>
        /// Gets the foreground image for the page
        /// </summary>
        /// <param name="resizeToPage"></param>
        /// <returns></returns>
        internal Bitmap GetForegroundImage(int subsample, bool resizeImage = false)
        {
            Verify.SubsampleRange(subsample);

            lock (LoadingLock)
            {
                Bitmap result = null;

                JB2Image jb2Image = null;
                IInterWavePixelMap iwPixelMap = _Page.ForegroundIWPixelMap;

                if (iwPixelMap != null)
                {
                    result = _Page.ForegroundIWPixelMap.GetPixelMap().ToImage();
                }
                else if ((jb2Image = _Page.ForegroundJB2Image) != null)
                {
                    if (_Page.ForegroundPalette == null)
                    {
                        GBitmap bmp = jb2Image.GetBitmap(1, GBitmap.BorderSize);
                        result = bmp.ToImage();
                    }
                    else
                    {
                        result = jb2Image.GetPixelMap(_Page.ForegroundPalette, 1, 16).ToImage();
                    }
                }
                else if (iwPixelMap == null && jb2Image == null)
                {
                    result = CreateBlankImage(Brushes.Black, _Page.Width / subsample, _Page.Height / subsample);
                }

                return resizeImage ? ResizeImage(result, _Page.Width / subsample, _Page.Height / subsample) : result;
            }
        }

        //internal DjvuNet.Graphics.IMap GetForegroundMap()
        //{
        //    lock (LoadingLock)
        //    {
        //        DjvuNet.Graphics.IMap result = null;
        //        JB2Image jb2image = null;
        //        IInterWavePixelMap iwPixelMap = _Page.ForegroundIWPixelMap;

        //        if (iwPixelMap != null)
        //        {
        //            result = _Page.ForegroundIWPixelMap.GetPixelMap();
        //        }
        //        else if ((jb2image = _Page.ForegroundJB2Image) != null)
        //        {
        //            if (_Page.ForegroundPalette == null)
        //            {
        //                result = jb2image.GetBitmap(1, GBitmap.BorderSize);
        //            }
        //            else
        //            {
        //                result = jb2image.GetPixelMap(_Page.ForegroundPalette, 1, 16);
        //            }
        //        }
        //        else if (iwPixelMap == null && jb2image == null)
        //        {
        //            result = new GBitmap(_Page.Height, _Page.Width, GBitmap.BorderSize);
        //        }

        //        return result;
        //    }
        //}

        internal Bitmap GetMaskImage(int subsample, bool resizeImage = false)
        {
            Verify.SubsampleRange(subsample);

            if (_Page.ForegroundJB2Image == null)
            {
                return new Bitmap(_Page.Width / subsample, _Page.Height / subsample, PixelFormat.Format8bppIndexed);
            }

            lock (LoadingLock)
            {
                JB2Image jb2Image = _Page.ForegroundJB2Image;
                GBitmap bmp = jb2Image.GetBitmap(subsample, GBitmap.BorderSize);
                Bitmap result = bmp.ToImage();
                return resizeImage ? DjvuImage.ResizeImage(result, _Page.Width / subsample, _Page.Height / subsample) : result;
            }
        }

        internal GBitmap GetMaskBitmap()
        {
            lock (LoadingLock)
            {
                JB2Image jb2Image = _Page.ForegroundJB2Image;
                if (jb2Image != null)
                {
                    return jb2Image.GetBitmap(1, GBitmap.BorderSize);
                }
                else
                {
                    return default;
                }
            }
        }

        /// <summary>
        /// Gets the background image for the page
        /// </summary>
        /// <returns></returns>
        internal Bitmap GetBackgroundImage(int subsample, bool resizeImage = false)
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

            lock (LoadingLock)
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
                        if (!IsBackgroundDecoded)
                        {
                            background.ProgressiveDecodeBackground(backgroundMap);
                        }
                    }
                }

                IsBackgroundDecoded = true;
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

        internal DjvuNet.Graphics.PixelMap GetBackgroundMap(bool rebuild = false)
        {
            if (!rebuild && IsBackgroundDecoded)
            {
                return _Page.PageForm?.GetChildrenItems<BG44Chunk>().FirstOrDefault()?.BackgroundImage.GetPixelMap() ?? null;
            }
            else if (rebuild && IsBackgroundDecoded)
            {
                IsBackgroundDecoded = false;
            }

            int width = _Page.Width;
            int height = _Page.Height;

            BG44Chunk[] backgrounds = _Page.PageForm?.GetChildrenItems<BG44Chunk>();

            if ((backgrounds == null || backgrounds.Length == 0) && width > 0 && height > 0)
            {
                return new Graphics.PixelMap(new sbyte[width * height], width, height);
            }

            // Get the composite background image
            Wavelet.IInterWavePixelMap backgroundMap = null;

            lock (LoadingLock)
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
                        if (!IsBackgroundDecoded)
                        {
                            background.ProgressiveDecodeBackground(backgroundMap);
                        }
                    }
                }

                IsBackgroundDecoded = true;
            }

            return backgroundMap.GetPixelMap();
        }

        internal static unsafe Bitmap InvertImage(Bitmap sourceBitmap)
        {
            if (sourceBitmap == null)
            {
                return null;
            }

            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                Bitmap invertedBitmap = Unsafe.As<Bitmap>(sourceBitmap.Clone());
                BitmapData imageData = invertedBitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height),
                                                      ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                byte* imagePtrBytes = (byte*)imageData.Scan0;
                int stride = imageData.Stride;

#if NETCOREAPP
                if (Vector256.IsHardwareAccelerated)
                {
                    var xorMask = Vector256.Create(0x00FFFFFFu);

                    for (int y = 0; y < height; y++)
                    {
                        uint* imageRow = (uint*)(imagePtrBytes + (y * stride));
                        int x = 0;

                        for (; x <= width - 32; x += 32)
                        {
                            var v0 = Vector256.Load(imageRow + x);
                            var v1 = Vector256.Load(imageRow + x + 8);
                            var v2 = Vector256.Load(imageRow + x + 16);
                            var v3 = Vector256.Load(imageRow + x + 24);

                            var dst0 = v0 ^ xorMask;
                            var dst1 = v1 ^ xorMask;
                            var dst2 = v2 ^ xorMask;
                            var dst3 = v3 ^ xorMask;

                            dst0.Store(imageRow + x);
                            dst1.Store(imageRow + x + 8);
                            dst2.Store(imageRow + x + 16);
                            dst3.Store(imageRow + x + 24);
                        }

                        for (; x <= width - 8; x += 8)
                        {
                            var srcVec = Vector256.Load(imageRow + x);
                            var dstVec = srcVec ^ xorMask;
                            dstVec.Store(imageRow + x);
                        }

                        for (; x < width; x++)
                        {
                            imageRow[x] = InvertColor(imageRow[x]);
                        }
                    }
                }
                else
#endif
                {
                    for (int y = 0; y < height; y++)
                    {
                        uint* imageRow = (uint*)(imagePtrBytes + (y * stride));

                        for (int x = 0; x < width; x++)
                        {
                            imageRow[x] = InvertColor(imageRow[x]);
                        }
                    }
                }

                invertedBitmap.UnlockBits(imageData);
                return invertedBitmap;
            }
            else
            {
                // Unix / libgdiplus bypass
                Bitmap invertedBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                BitmapData srcData = sourceBitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, sourceBitmap.PixelFormat);
                BitmapData dstData = invertedBitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                try
                {
                    byte* srcPtrBytes = (byte*)srcData.Scan0;
                    byte* dstPtrBytes = (byte*)dstData.Scan0;
                    int srcStride = srcData.Stride;
                    int dstStride = dstData.Stride;

                    if (sourceBitmap.PixelFormat == PixelFormat.Format8bppIndexed)
                    {
                        Color[] palette = sourceBitmap.Palette.Entries;
                        uint* invPalette = stackalloc uint[256];
                        for (int i = 0; i < palette.Length && i < 256; i++)
                        {
                            invPalette[i] = (uint)palette[i].ToArgb() ^ 0x00FFFFFFu;
                        }

                        for (int y = 0; y < height; y++)
                        {
                            byte* srcRow = srcPtrBytes + (y * srcStride);
                            uint* dstRow = (uint*)(dstPtrBytes + (y * dstStride));
                            for (int x = 0; x < width; x++)
                            {
                                dstRow[x] = invPalette[srcRow[x]];
                            }
                        }
                    }
                    else
                    {
                        sourceBitmap.UnlockBits(srcData);
                        srcData = sourceBitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                        srcPtrBytes = (byte*)srcData.Scan0;
                        srcStride = srcData.Stride;

#if NETCOREAPP
                        if (Vector256.IsHardwareAccelerated)
                        {
                            var xorMask = Vector256.Create(0x00FFFFFFu);
                            for (int y = 0; y < height; y++)
                            {
#if DEBUG
                                if (y == 0) Console.WriteLine("[DjvuNet] Executing Vector256.IsHardwareAccelerated path in InvertImage (Unix) - Start row 0");
#endif
                                uint* srcRow = (uint*)(srcPtrBytes + (y * srcStride));
                                uint* dstRow = (uint*)(dstPtrBytes + (y * dstStride));
                                int x = 0;
                                for (; x <= width - 32; x += 32)
                                {
                                    var v0 = Vector256.Load(srcRow + x);
                                    var v1 = Vector256.Load(srcRow + x + 8);
                                    var v2 = Vector256.Load(srcRow + x + 16);
                                    var v3 = Vector256.Load(srcRow + x + 24);

                                    var dst0 = v0 ^ xorMask;
                                    var dst1 = v1 ^ xorMask;
                                    var dst2 = v2 ^ xorMask;
                                    var dst3 = v3 ^ xorMask;

                                    dst0.Store(dstRow + x);
                                    dst1.Store(dstRow + x + 8);
                                    dst2.Store(dstRow + x + 16);
                                    dst3.Store(dstRow + x + 24);
                                }
                                for (; x <= width - 8; x += 8)
                                {
                                    var srcVec = Vector256.Load(srcRow + x);
                                    var dstVec = srcVec ^ xorMask;
                                    dstVec.Store(dstRow + x);
                                }
                                for (; x < width; x++)
                                {
                                    dstRow[x] = srcRow[x] ^ 0x00FFFFFFu;
                                }
#if DEBUG
                                if (y == height - 1) Console.WriteLine($"[DjvuNet] Executing Vector256.IsHardwareAccelerated path in InvertImage (Unix) - End row {height}");
#endif
                            }
                        }
                        else
#endif
                        {
                            for (int y = 0; y < height; y++)
                            {
                                uint* srcRow = (uint*)(srcPtrBytes + (y * srcStride));
                                uint* dstRow = (uint*)(dstPtrBytes + (y * dstStride));
                                for (int x = 0; x < width; x++)
                                {
                                    dstRow[x] = srcRow[x] ^ 0x00FFFFFFu;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    sourceBitmap.UnlockBits(srcData);
                    invertedBitmap.UnlockBits(dstData);
                }

                return invertedBitmap;
            }
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

        internal static Graphics.Pixel InvertColor(Graphics.Pixel color)
        {
            return new Graphics.Pixel((sbyte)(color.Blue ^ unchecked((sbyte)0xff)), (sbyte)(color.Green ^ unchecked((sbyte)0xff)), (sbyte)(color.Red ^ unchecked((sbyte)0xff)));
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
