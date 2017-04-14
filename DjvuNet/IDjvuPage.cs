using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using DjvuNet.DataChunks;
using DjvuNet.Graphics;
using DjvuNet.JB2;
using DjvuNet.Wavelet;

namespace DjvuNet
{
    public interface IDjvuPage
    {
        bool Disposed { get; }

        DjvuDocument Document { get; }

        DirmComponent Header { get; }

        int Height { get; }

        IReadOnlyList<DjviChunk> IncludeFiles { get; }

        InfoChunk Info { get; }

        DjvuFormElement PageForm { get; }

        int PageNumber { get; }

        string Text { get; }

        TextChunk TextChunk { get; }

        int Width { get; }

        event PropertyChangedEventHandler PropertyChanged;

        void ClearImage();

        void Dispose();

        string GetTextForLocation(System.Drawing.Rectangle rect);

        void Preload();

        IWPixelMap BackgroundIWPixelMap { get; }

        IWPixelMap ForegroundIWPixelMap { get; }

        JB2Image ForegroundJB2Image { get; }

        ColorPalette ForegroundPalette { get; }

        PixelMap ForegroundPixelMap { get; }

        System.Drawing.Bitmap Image { get; }

        bool IsColor { get; }

        bool IsInverted { get; set; }

        bool IsPageImageCached { get; set; }

        ThumChunk Thumbnail { get; }

        System.Drawing.Bitmap ThumbnailImage { get; set; }

        Graphics.Bitmap BuildBitmap(Graphics.Rectangle rect, int subsample, int align, System.Drawing.Bitmap retVal);

        System.Drawing.Bitmap BuildImage(int subsample = 1);

        System.Drawing.Bitmap BuildPageImage();

        System.Drawing.Bitmap ExtractThumbnailImage();

        PixelMap GetBgPixmap(Graphics.Rectangle rect, int subsample, double gamma, PixelMap retval);

        Graphics.Bitmap GetBitmap(Graphics.Rectangle rect, int subsample, int align, Graphics.Bitmap retval);

        Graphics.Bitmap GetBitmapList(Graphics.Rectangle rect, int subsample, int align, List<int> components);

        Map GetMap(Graphics.Rectangle segment, int subsample, Map retval);

        PixelMap GetPixelMap(Graphics.Rectangle rect, int subsample, double gamma, PixelMap retval);

        System.Drawing.Bitmap ResizeImage(int newWidth, int newHeight);
    }
}