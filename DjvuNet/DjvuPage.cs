// -----------------------------------------------------------------------
// <copyright file="DjvuPage.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using DjvuNet.DataChunks;
using DjvuNet.Graphics;
using DjvuNet.JB2;
using DjvuNet.Utilities;
using DjvuNet.Wavelet;
using Bitmap = System.Drawing.Bitmap;
using ColorPalette = DjvuNet.DataChunks.ColorPalette;
using GBitmap = DjvuNet.Graphics.IBitmap;
using GMap = DjvuNet.Graphics.IMap;
using GPixelReference = DjvuNet.Graphics.IPixelReference;
using GPixmap = DjvuNet.Graphics.IPixelMap;
using GRect = DjvuNet.Graphics.Rectangle;
using Rectangle = System.Drawing.Rectangle;

namespace DjvuNet
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DjvuPage : INotifyPropertyChanged, IDisposable, IDjvuPage
    {
        #region Private Members

        /// <summary>
        /// True if the page has been previously loaded, false otherwise
        /// </summary>
        private bool _HasLoaded;

        private object _LoadingLock = new object();
        private bool _IsBackgroundDecoded;

        private DjvuImage _DjvuImage;

        #endregion Private Members

        #region Public Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Public Events

        #region Public Properties

        #region Thumbnail

        private ITH44Chunk _Thumbnail;

        /// <summary>
        /// Gets the thumbnail for the page
        /// </summary>
        public ITH44Chunk Thumbnail
        {
            get { return _Thumbnail; }

            internal set
            {
                if (_Thumbnail != value)
                {
                    _Thumbnail = value;
                    OnPropertyChanged(nameof(Thumbnail));
                }
            }
        }

        #endregion Thumbnail

        #region Document

        private IDjvuDocument _Document;

        /// <summary>
        /// Gets the document the page belongs to
        /// </summary>
        public IDjvuDocument Document
        {
            get { return _Document; }

            internal set
            {
                if (_Document != value)
                {
                    _Document = value;
                    OnPropertyChanged(nameof(Document));
                }
            }
        }

        #endregion Document

        #region IncludedItems

        private List<IDjviChunk> _Includes;

        /// <summary>
        /// Gets the included items
        /// </summary>
        public IReadOnlyList<IDjviChunk> IncludeFiles
        {
            get { return _Includes; }

            internal set
            {
                if (_Includes != value)
                {
                    _Includes = (List<IDjviChunk>) value;
                    OnPropertyChanged(nameof(IncludeFiles));
                }
            }
        }

        #endregion IncludedItems

        #region PageForm

        private DjvuFormElement _PageForm;

        /// <summary>
        /// Gets the form chunk for the page
        /// </summary>
        public DjvuFormElement PageForm
        {
            get { return _PageForm; }

            internal set
            {
                if (_PageForm != value)
                {
                    _PageForm = value;
                    OnPropertyChanged(nameof(PageForm));
                }
            }
        }

        #endregion PageForm

        #region Info

        private InfoChunk _Info;

        /// <summary>
        /// Gets the info chunk for the page
        /// </summary>
        public InfoChunk Info
        {
            get
            {
                if (_Info == null)
                {
                    var chunk = PageForm.Children
                        .FirstOrDefault<IDjvuNode>(x => x.ChunkType == ChunkType.Info);
                    _Info = chunk as InfoChunk;
                    if (_Info != null)
                        OnPropertyChanged(nameof(Info));
                }

                return _Info;
            }
        }

        #endregion Info

        #region Width

        /// <summary>
        /// Gets the width of the page
        /// </summary>
        public int Width { get; internal set; }

        #endregion Width

        #region Height

        /// <summary>
        /// Gets the height of the page
        /// </summary>
        public int Height { get; internal set; }

        #endregion Height

        #region Header

        private DirmComponent _Header;

        /// <summary>
        /// Gets directory header for the page
        /// </summary>
        public DirmComponent Header
        {
            get { return _Header; }

            internal set
            {
                if (_Header != value)
                {
                    _Header = value;
                    OnPropertyChanged(nameof(Header));
                }
            }
        }

        #endregion Header

        #region Text

        private DataChunks.TextChunk _TextChunk;

        /// <summary>
        /// Gets the text chunk for the page
        /// </summary>
        public DataChunks.TextChunk TextChunk
        {
            get
            {
                if (_TextChunk == null)
                {
                    _TextChunk = (TextChunk) PageForm.Children.FirstOrDefault(
                        x => x.ChunkType == ChunkType.Txtz);
                    if (_TextChunk != null)
                        OnPropertyChanged(nameof(TextChunk)); ;
                }

                return _TextChunk;
            }
        }

        private String _Text;

        public String Text
        {
            get
            {
                if (_Text == null)
                {
                    _Text = TextChunk?.Text;
                    if (_Text == null)
                        _Text = String.Empty;
                }

                return _Text;
            }
        }

        #endregion Text

        #region ForegroundJB2Image

        private JB2.JB2Image _ForegroundJB2Image;

        /// <summary>
        /// Gets the foreground image
        /// </summary>
        public JB2.JB2Image ForegroundJB2Image
        {
            get
            {
                if (_ForegroundJB2Image == null)
                {
                    // Get the first chunk if present
                    var chunk = (SjbzChunk)PageForm?.Children
                        .FirstOrDefault<IDjvuNode>(x => x.ChunkType == ChunkType.Sjbz);

                    if (chunk != null)
                    {
                        _ForegroundJB2Image = chunk.Image;
                        OnPropertyChanged(nameof(ForegroundJB2Image));
                    }
                }

                return _ForegroundJB2Image;
            }
        }

        #endregion ForegroundJB2Image

        #region ForegroundIWPixelMap

        private Wavelet.IInterWavePixelMap _ForegroundIWPixelMap;

        /// <summary>
        /// Gets the Foreground pixel map
        /// </summary>
        public Wavelet.IInterWavePixelMap ForegroundIWPixelMap
        {
            get
            {
                if (_ForegroundIWPixelMap == null)
                {
                    var chunk = (FG44Chunk)PageForm?.Children
                        .FirstOrDefault<IDjvuNode>(x => x.ChunkType == ChunkType.FG44);

                    if (chunk != null)
                    {
                        _ForegroundIWPixelMap = chunk.ForegroundImage;
                        OnPropertyChanged(nameof(_ForegroundIWPixelMap));
                    }
                }

                return _ForegroundIWPixelMap;
            }
        }

        #endregion ForegroundIWPixelMap

        #region BackgroundIWPixelMap

        private Wavelet.IInterWavePixelMap _BackgroundIWPixelMap;

        /// <summary>
        /// Gets the background pixel map
        /// </summary>
        public Wavelet.IInterWavePixelMap BackgroundIWPixelMap
        {
            get
            {
                if (_BackgroundIWPixelMap == null)
                {
                    var chunk = (BG44Chunk)PageForm?.Children
                        .FirstOrDefault<IDjvuNode>(x => x.ChunkType == ChunkType.BG44);

                    if (chunk != null)
                    {
                        _BackgroundIWPixelMap = chunk.BackgroundImage;
                        OnPropertyChanged(nameof(_BackgroundIWPixelMap));
                    }
                }

                return _BackgroundIWPixelMap;
            }
        }

        #endregion BackgroundIWPixelMap

        #region ForegroundPalette

        private ColorPalette _ForegroundPalette;

        /// <summary>
        /// Gets the palette for the foreground
        /// </summary>
        public ColorPalette ForegroundPalette
        {
            get
            {
                if (_ForegroundPalette == null)
                {
                    DjvmChunk root = Document.RootForm as DjvmChunk;
                    // TODO - verify if tests or this code is failing to handle palette correctly
                    FGbzChunk result = (FGbzChunk)PageForm.Children
                        .FirstOrDefault<IDjvuNode>(x => x.ChunkType == ChunkType.FGbz);

                    _ForegroundPalette = result?.Palette;
                    if (_ForegroundPalette != null)
                        OnPropertyChanged(nameof(ForegroundPalette));
                }

                return _ForegroundPalette;
            }
        }

        #endregion ForegroundPalette

        #region ForegroundPixelMap

        private GPixmap _ForegroundPixelMap;

        /// <summary>
        /// Gets the pixel map for the foreground
        /// </summary>
        public GPixmap ForegroundPixelMap
        {
            get
            {
                if (_ForegroundPixelMap == null)
                {
                    _ForegroundPixelMap = ForegroundIWPixelMap.GetPixelMap();
                    if (_ForegroundPixelMap != null)
                        OnPropertyChanged(nameof(ForegroundPixelMap));
                }

                return _ForegroundPixelMap;
            }
        }

        #endregion ForegroundPixelMap

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
                    OnPropertyChanged(nameof(Image)); ;
                }

                return _Image;
            }

            internal set
            {
                if (_Image != value)
                {
                    _Image = value;
                    OnPropertyChanged(nameof(Image)); ;
                }
            }
        }

        #endregion Image

        #region IsInverted

        private bool _IsInverted;

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
                    ClearImage();
                    ThumbnailImage = InvertImage(ThumbnailImage);
                    OnPropertyChanged(nameof(IsInverted));
                }
            }
        }

        #endregion IsInverted

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

        #region PageNumber

        private int _PageNumber;

        /// <summary>
        /// Gets the number of the page
        /// </summary>
        public int PageNumber
        {
            get { return _PageNumber; }

            internal set
            {
                if (_PageNumber != value)
                {
                    _PageNumber = value;
                    OnPropertyChanged(nameof(PageNumber));
                }
            }
        }

        #endregion PageNumber

        #region IsColor

        /// <summary>
        /// True if this is photo or compound
        /// </summary>
        public bool IsColor
        {
            get { return IsLegalCompound() || IsLegalBilevel(); }
        }

        #endregion IsColor

        public float Gamma;

        #endregion Public Properties

        #region Constructors

        internal DjvuPage() { }

        public DjvuPage(int pageNumber, IDjvuDocument document, DirmComponent header,
            ITH44Chunk thumbnail, IReadOnlyList<IDjviChunk> includedItems, DjvuFormElement form)
        {
            PageNumber = pageNumber;
            Document = document;
            Header = header;
            Thumbnail = thumbnail;
            IncludeFiles = includedItems;
            PageForm = form;
            _DjvuImage = new DjvuImage(this);
            PropertyChanged += DjvuPage_PropertyChanged;

            if (form.ChunkType != ChunkType.BM44Form && form.ChunkType != ChunkType.PM44Form && Info == null)
            {
                throw new DjvuFormatException(
                    $"Page {PageNumber} does not have associated Info chunk." +
                    "Page is invalid and can not be displayed");
            }
            else if (form.ChunkType == ChunkType.BM44Form || form.ChunkType == ChunkType.PM44Form)
            {
                throw new NotImplementedException();
            }
        }

        private void DjvuPage_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(Info):
                    Width = Info.Width;
                    Height = Info.Height;
                    Gamma = Info.Gamma;
                    break;
            }
        }

        #endregion Constructors

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

        ~DjvuPage()
        {
            Dispose(false);
        }

        #endregion IDisposable implementation

        #region Public Methods

        /// <summary>
        /// Preloads the page
        /// </summary>
        public void Preload()
        {
            lock (_LoadingLock)
            {
                if (_HasLoaded == false)
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
        /// Gets the text for the rectangle location
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public string GetTextForLocation(Rectangle rect)
        {
            if (TextChunk == null || TextChunk.Zone == null)
                return "";

            StringBuilder text = new StringBuilder();

            TextZone[] textItems = TextChunk.Zone.OrientedSearchForText(rect, Height);

            TextZone currentParent = null;

            foreach (TextZone item in textItems)
            {
                if (currentParent != item.Parent)
                {
                    text.AppendLine();
                    currentParent = item.Parent;
                }

                if (item.Parent == currentParent)
                    text.Append(item.Text + " ");
            }

            return text.ToString().Trim();
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
            if (Thumbnail != null)
                return Thumbnail.Image.ToImage();

            var result = BuildImage();
            var scaleAmount = (double)128 / result.Width;

            result = DjvuImage.ResizeImage(result, (int)(result.Width * scaleAmount), (int)(result.Height * scaleAmount));

            return result;
        }

        /// <summary>
        /// Gets the background pixmap
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="subSample"></param>
        /// <param name="gamma"></param>
        /// <param name="retval"></param>
        /// <returns></returns>
        public GPixmap GetBgPixmap(GRect rect, int subsample, double gamma, GPixmap retval)
        {
            Verify.SubsampleRange(subsample);

            GPixmap pMap = null;
            int width = Width;
            int height = Height;

            if (width <= 0 || height <= 0)
                return null;

            double gammaCorr = 1.0D;

            if (gamma > 0.0D && Gamma > 0)
            {
                gammaCorr = gamma / Gamma;
            }

            if (gammaCorr < 0.10000000000000001D)
            {
                gammaCorr = 0.10000000000000001D;
            }
            else if (gammaCorr > 10D)
            {
                gammaCorr = 10D;
            }

            IInterWavePixelMap bgIWPixmap = BackgroundIWPixelMap;

            if (bgIWPixmap != null)
            {
                int iwWidth = bgIWPixmap.Width;
                int iwHeight = bgIWPixmap.Height;

                if (iwWidth == 0 || iwHeight == 0)
                    return null;

                int red = ComputeRed(width, height, iwWidth, iwHeight);

                if (red < 1 || red > 12)
                    return null;

                if (subsample == red)
                {
                    pMap = bgIWPixmap.GetPixelMap(1, rect, retval);
                }
                else if (subsample == (2 * red))
                {
                    pMap = bgIWPixmap.GetPixelMap(2, rect, retval);
                }
                else if (subsample == (4 * red))
                {
                    pMap = bgIWPixmap.GetPixelMap(4, rect, retval);
                }
                else if (subsample == (8 * red))
                {
                    pMap = bgIWPixmap.GetPixelMap(8, rect, retval);
                }
                else if ((red * 4) == (subsample * 3))
                {
                    GRect xrect = new GRect();
                    xrect.Right = (int)Math.Floor(rect.Right * 4D / 3D);
                    xrect.Bottom = (int)Math.Floor(rect.Bottom * 4D / 3D);
                    xrect.Left = (int)Math.Ceiling((double)rect.Left * 4D / 3D);
                    xrect.Top = (int)Math.Ceiling((double)rect.Top * 4D / 3D);

                    GRect nrect = new GRect(0, 0, rect.Width, rect.Height);

                    if (xrect.Left > iwWidth)
                        xrect.Left = iwWidth;

                    if (xrect.Top > iwHeight)
                        xrect.Top = iwHeight;

                    GPixmap iwPMap = bgIWPixmap.GetPixelMap(1, xrect, null);
                    pMap = (retval != null) ? retval : new PixelMap();
                    pMap.Downsample43(iwPMap, nrect);
                }
                else
                {
                    int po2 = 16;

                    while (po2 > 1 && subsample < po2 * red)
                        po2 >>= 1;

                    int inw = ((iwWidth + po2) - 1) / po2;
                    int inh = ((iwHeight + po2) - 1) / po2;
                    int outw = ((width + subsample) - 1) / subsample;
                    int outh = ((height + subsample) - 1) / subsample;
                    PixelMapScaler mapScaler = new PixelMapScaler(inw, inh, outw, outh);

                    mapScaler.SetHorzRatio(red * po2, subsample);
                    mapScaler.SetVertRatio(red * po2, subsample);

                    GRect xrect = mapScaler.GetRequiredRect(rect);
                    GPixmap iwPMap = bgIWPixmap.GetPixelMap(po2, xrect, null);
                    pMap = (retval != null) ? retval : new PixelMap();

                    mapScaler.Scale(xrect, iwPMap, rect, pMap);
                }

                if (pMap != null && gammaCorr != 1.0D)
                {
                    pMap.ApplyGammaCorrection(gammaCorr);

                    for (int i = 0; i < 9; i++)
                        pMap.ApplyGammaCorrection(gammaCorr);
                }

                return pMap;
            }
            else
                return null;
        }

        public GBitmap BuildBitmap(Graphics.Rectangle rect, int subsample, int align, Bitmap retVal)
        {
            // TODO verify use of seemingly excessive retVal parameter
            Verify.SubsampleRange(subsample);
            return GetBitmap(rect, subsample, align, null);
        }

        public GPixmap GetPixelMap(GRect rect, int subsample, double gamma, GPixmap retval)
        {
            Verify.SubsampleRange(subsample);

            if (rect == null || rect.Empty)
                return (retval == null) ? (new PixelMap()) : retval.Init(0, 0, null);

            GPixmap bg = GetBgPixmap(rect, subsample, gamma, retval);
            if (ForegroundJB2Image != null)
            {
                if (bg == null)
                {
                    bg = (retval == null) ? new PixelMap() : retval;
                    bg.Init(rect.Height, rect.Width, _IsInverted ? Pixel.BlackPixel : Pixel.WhitePixel);
                }
                if (Stencil(bg, rect, subsample, gamma))
                    retval = bg;
            }
            else
                retval = bg;

            return retval;
        }

        /// <summary>
        /// Gets a complete image for the page
        /// </summary>
        /// <returns>
        /// <see cref="System.Drawing.Bitmap"/>Bitmap page image.
        /// </returns>
        public System.Drawing.Bitmap BuildPageImage(bool rebuild = false)
        {
            int subsample = 1;

            int width = Width / subsample;
            int height = Height / subsample;
            GMap map = null;
            Rectangle rect = new Rectangle(0, 0, width, height);
            Bitmap retVal = null;

            if (rebuild || _Image == null)
            {
                map = GetMap(new GRect(0, 0, width, height), subsample, null);
                if (map == null)
                    return new Bitmap(width, height);
                if (map.BytesPerPixel == 3)
                {
                    PixelFormat format = PixelFormat.Format24bppRgb;
                    retVal = DjvuImage.ImageFromMap(map, rect, format);
                }
                else if (map.BytesPerPixel == 1)
                {
                    PixelFormat format = PixelFormat.Format8bppIndexed;
                    retVal = DjvuImage.ImageFromMap(map, rect, format);
                }
            }
            else
                retVal = _Image;

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
                        palette.Entries[i] = Color.FromArgb(i, i, i);
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
                        int backgroundPixelSize = GetPixelSize(backgroundData.PixelFormat);

                        BitmapData foregroundData =
                            foreground.LockBits(new System.Drawing.Rectangle(0, 0, foreground.Width, foreground.Height),
                                                ImageLockMode.ReadOnly, foreground.PixelFormat);
                        int foregroundPixelSize = GetPixelSize(foregroundData.PixelFormat);

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
                        ;
                        for (int y = 0, yf = 0, yb = 0; y < maskHeight  && yb < bgndHeight && yf < fgndHeight; y++)
                        {
                            byte* maskRow = (byte*) maskData.Scan0 + (y * maskData.Stride);
                            uint* backgroundRow = (uint*)(backgroundData.Scan0 + (yb * backgroundData.Stride));
                            uint* foregroundRow = (uint*)(foregroundData.Scan0 + (yf * foregroundData.Stride));

                            for (int x = 0, xf = 0, xb = 0; x < bgndWidth && xb < maskWidth && xf < fgndWidth ; x++)
                            {
                                // Check if the mask byte is set
                                if (maskRow[x] > 0)
                                {
                                    uint xF = foregroundRow[xf];

                                    if (_IsInverted)
                                        backgroundRow[xb] = InvertColor(xF);
                                    else
                                        backgroundRow[xb] = xF;
                                }
                                else if (_IsInverted == true)
                                {
                                    uint xB = backgroundRow[xb];
                                    backgroundRow[xb] = InvertColor(xB);
                                }

                                if (x > 0)
                                {
                                    if (x % maskbgnW == 0)
                                        xb++;

                                    if (x % maskfgnW == 0)
                                        xf++;
                                }
                            }

                            if (y > 0)
                            {
                                if (y % maskbgnH == 0)
                                    yb++;

                                if (y % maskfgnH == 0)
                                    yf++;
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

        public GBitmap GetBitmap(GRect rect, int subsample, int align, GBitmap retval)
        {
            Verify.SubsampleRange(subsample);
            return GetBitmapList(rect, 1, 1, null);
        }

        public GBitmap GetBitmapList(GRect rect, int subsample, int align, List<int> components)
        {
            Verify.SubsampleRange(subsample);

            if (rect.Empty)
                return new Graphics.Bitmap();

            int width = Width;
            int height = Height;

            JB2Image fgJb2 = ForegroundJB2Image;

            if (width != 0 && height != 0 && fgJb2 != null && fgJb2.Width == width && fgJb2.Height == height)
                return fgJb2.GetBitmap(rect, subsample, align, 0, components);

            return null;
        }

        public GMap GetMap(GRect segment, int subsample, GMap retval)
        {
            Verify.SubsampleRange(subsample);

            if (IsColor)
                retval = GetPixelMap(segment, subsample, 0.0D,
                    (retval is GPixmap) ? (GPixmap)retval : null);
            else
                retval = GetBitmap(segment, subsample, 1,
                        (retval is GBitmap) ? (GBitmap)retval : null);

            return retval;
        }

        #endregion Public Methods

        /// <summary>
        /// Sends the property changed notification
        /// </summary>
        /// <param name="property"></param>
        protected void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        #region Private Methods

        internal bool IsLegalBilevel()
        {
            int width = Width;
            int height = Height;

            if (width <= 0 || height <= 0)
                return false;

            JB2Image fgJb2 = ForegroundJB2Image;

            if (fgJb2 == null || fgJb2.Width != width || fgJb2.Height != height)
                return false;

            return !(BackgroundIWPixelMap != null || ForegroundIWPixelMap != null
                     || ForegroundPalette != null);
        }

        internal bool IsLegalCompound()
        {
            int width = Width;
            int height = Height;

            if (width <= 0 || height <= 0)
                return false;

            JB2Image fgJb2 = ForegroundJB2Image;

            if (fgJb2 == null || fgJb2.Width != width || fgJb2.Height != height)
                return false;

            // There is no need to synchronize since we won't access data which could be updated.
            IInterWavePixelMap bgIWPixmap = (IInterWavePixelMap)BackgroundIWPixelMap;
            int bgred = 0;

            if (bgIWPixmap != null)
                bgred = ComputeRed(width, height, bgIWPixmap.Width, bgIWPixmap.Height);

            if ((bgred < 1) || (bgred > 12))
                return false;

            int fgred = 0;

            if (ForegroundIWPixelMap != null)
            {
                GPixmap fgPixmap = ForegroundPixelMap;
                fgred = ComputeRed( width, height, fgPixmap.Width, fgPixmap.Height);
            }

            return ((fgred >= 1) && (fgred <= 12));
        }

        internal bool Stencil(IPixelMap pm, Graphics.Rectangle rect, int subsample, double gamma)
        {
            Verify.SubsampleRange(subsample);

            int width = Width;
            int height = Height;

            if (width <= 0 || height <= 0)
                return false;

            double gammaCorr = 1.0D;

            if (gamma > 0.0D)
            {
                gammaCorr = gamma / Gamma;
            }

            if (gammaCorr < 0.10000000000000001D)
            {
                gammaCorr = 0.10000000000000001D;
            }
            else if (gammaCorr > 10D)
            {
                gammaCorr = 10D;
            }

            JB2Image fgJb2 = ForegroundJB2Image;

            if (fgJb2 != null)
            {
                ColorPalette fgPalette = ForegroundPalette;

                if (fgPalette != null)
                {
                    List<int> components = new List<int>();
                    GBitmap bm = GetBitmapList(rect, subsample, 1, components);

                    if (fgJb2.Blits.Count != fgPalette.BlitColors?.Length)
                    {
                        pm.Attenuate(bm, 0, 0);

                        return false;
                    }

                    GPixmap colors =
                      new PixelMap().Init(1, fgPalette.PaletteColors.Length, null);

                    GPixelReference color = colors.CreateGPixelReference(0);

                    for (int i = 0; i < colors.Width; color.IncOffset())
                        fgPalette.IndexToColor(i++, color);

                    colors.ApplyGammaCorrection(gammaCorr);

                    List<int> compset = new List<int>();

                    while (components.Count > 0)
                    {
                        int lastx = 0;
                        int colorindex = fgPalette.BlitColors[components[0]];
                        GRect comprect = new GRect();
                        compset = new List<int>();

                        for (int pos = 0; pos < components.Count; )
                        {
                            int blitno = ((int)components[pos]);
                            JB2Blit pblit = fgJb2.Blits[blitno];

                            if (pblit.Left < lastx)
                            {
                                break;
                            }

                            lastx = pblit.Left;

                            if (fgPalette.BlitColors[blitno] == colorindex)
                            {
                                JB2Shape pshape = fgJb2.GetShape(pblit.ShapeNumber);
                                GRect xrect = new GRect(pblit.Left, pblit.Bottom,
                                    pshape.Bitmap.Width, pshape.Bitmap.Height);

                                comprect.Recthull(comprect, xrect);
                                compset.Add(components[pos]);
                                components.RemoveAt(pos);
                            }
                            else
                            {
                                pos++;
                            }
                        }

                        comprect.XMin /= subsample;
                        comprect.YMin /= subsample;
                        comprect.XMax = ((comprect.XMax + subsample) - 1) / subsample;
                        comprect.YMax = ((comprect.YMax + subsample) - 1) / subsample;
                        comprect.Intersect(comprect, rect);

                        if (comprect.Empty)
                        {
                            continue;
                        }

                        bm = new DjvuNet.Graphics.Bitmap();
                        bm.Init(comprect.Height, comprect.Width, 0);
                        bm.Grays = 1 + (subsample * subsample);

                        int rxmin = comprect.XMin * subsample;
                        int rymin = comprect.YMin * subsample;

                        for (int pos = 0; pos < compset.Count; ++pos)
                        {
                            int blitno = ((int)compset[pos]);
                            JB2Blit pblit = fgJb2.Blits[blitno];
                            JB2Shape pshape = fgJb2.GetShape(pblit.ShapeNumber);
                            bm.Blit(
                              pshape.Bitmap,
                              pblit.Left - rxmin,
                              pblit.Bottom - rymin,
                              subsample);
                        }

                        color.SetOffset(colorindex);
                        pm.Blit(
                          bm,
                          comprect.XMin - rect.XMin,
                          comprect.YMin - rect.YMin,
                          color);
                    }

                    return true;
                }

                // Three layer model.
                IInterWavePixelMap fgIWPixmap = ForegroundIWPixelMap;

                if (fgIWPixmap != null)
                {
                    GBitmap bm = GetBitmap(rect, subsample, 1, null);

                    if (bm != null && pm != null)
                    {
                        GPixmap fgPixmap = ForegroundPixelMap;
                        int w = fgPixmap.Width;
                        int h = fgPixmap.Height;
                        int red = ComputeRed(width, height, w, h);

                        //          if((red < 1) || (red > 12))
                        if (red < 1 || red > 16)
                            return false;
                        //
                        //          int supersample = (red <= subsample)
                        //            ? 1
                        //            : (red / subsample);
                        //          int wantedred = supersample * subsample;
                        //
                        //          if(red == wantedred)
                        //          {
                        //            pm.stencil(bm, fgPixmap, supersample, rect, gamma_correction);
                        //
                        //            return 1;
                        //          }
                        pm.Stencil(bm, fgPixmap, red, subsample, rect, gammaCorr);
                        return true;
                    }
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ComputeRed(int w, int h, int rw, int rh)
        {
            for (int red = 1; red < 16; red++)
            {
                if (((((w + red) - 1) / red) == rw) && ((((h + red) - 1) / red) == rh))
                    return red;
            }

            return 16;
        }

        internal static unsafe System.Drawing.Bitmap InvertImage(System.Drawing.Bitmap invertImage)
        {
            if (invertImage == null)
                return null;

            var image = Unsafe.As<System.Drawing.Bitmap>(invertImage.Clone());

            BitmapData imageData = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                                                  ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            int height = image.Height;
            int width = image.Width;

            for(int y = 0; y < height; y++)
            //Parallel.For(
            //    0,
            //    height,
            //    y =>
            {
                uint* imageRow = (uint*)(imageData.Scan0 + (y * imageData.Stride));

                for (int x = 0; x < width; x++)
                    // Check if the mask byte is set
                    imageRow[x] = InvertColor(imageRow[x]);
                //});
            }

            image.UnlockBits(imageData);

            return image;
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
                var iwPixelMap = ForegroundIWPixelMap;
                if (iwPixelMap != null)
                {
                    result = ForegroundIWPixelMap.GetPixelMap().ToImage();
                }
                else if ((jb2image = ForegroundJB2Image) != null)
                {
                    result = jb2image.GetBitmap().ToImage();
                }
                else if (iwPixelMap == null && jb2image == null)
                {
                    result = DjvuImage.CreateBlankImage(Brushes.Black, Width / subsample, Height / subsample);
                }

                return resizeImage == true ? DjvuImage.ResizeImage(result, Width / subsample, Height / subsample) : result;
            }
        }

        internal System.Drawing.Bitmap GetTextImage(int subsample, bool resizeImage = false)
        {
            Verify.SubsampleRange(subsample);

            if (ForegroundJB2Image == null)
                return new System.Drawing.Bitmap(Width / subsample, Height / subsample);

            lock (_LoadingLock)
            {
                var result = ForegroundJB2Image.GetBitmap(subsample, 4).ToImage();
                return resizeImage == true ? DjvuImage.ResizeImage(result, Width / subsample, Height / subsample) : result;
            }
        }

        /// <summary>
        /// Gets the background image for the page
        /// </summary>
        /// <returns></returns>
        internal System.Drawing.Bitmap GetBackgroundImage(int subsample, bool resizeImage = false)
        {
            Verify.SubsampleRange(subsample);

            int width = Width;
            int height = Height;

            BG44Chunk[] backgrounds = PageForm?.GetChildrenItems<BG44Chunk>();

            if ((backgrounds == null || backgrounds.Length == 0) && width > 0 && height > 0)
                return DjvuImage.CreateBlankImage(Brushes.White, width, height);

            // Get the composite background image
            Wavelet.IInterWavePixelMap backgroundMap = null;

            lock (_LoadingLock)
            {
                foreach (var background in backgrounds)
                {
                    if (backgroundMap == null)
                        // Get the initial image
                        backgroundMap = background.BackgroundImage;
                    else
                    {
                        if (_IsBackgroundDecoded == false)
                            background.ProgressiveDecodeBackground(backgroundMap);
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
                return result;
        }

        #endregion Private Methods
    }
}