using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using DjvuNet.DataChunks;
using DjvuNet.DataChunks.Directory;
using DjvuNet.DataChunks.Graphics;
using DjvuNet.DataChunks.Text;
using DjvuNet.Graphics;
using DjvuNet.JB2;
using DjvuNet.Wavelet;

namespace DjvuNet
{
    public interface IDjvuPage
    {
        IWPixelMap BackgroundIWPixelMap { get; }

        bool Disposed { get; }

        DjvuDocument Document { get; }

        IWPixelMap ForegroundIWPixelMap { get; }

        JB2Image ForegroundJB2Image { get; }

        ColorPalette ForegroundPalette { get; }

        PixelMap ForegroundPixelMap { get; }

        DirmComponent Header { get; }

        int Height { get; }

        System.Drawing.Bitmap Image { get; }

        IReadOnlyList<DjviChunk> Includes { get; }

        InfoChunk Info { get; }

        bool IsColor { get; }

        bool IsInverted { get; set; }

        bool IsPageImageCached { get; set; }

        DjvuFormElement PageForm { get; }

        int PageNumber { get; }

        string Text { get; }

        TextChunk TextChunk { get; }

        ThumChunk Thumbnail { get; }

        System.Drawing.Bitmap ThumbnailImage { get; set; }

        int Width { get; }

        event PropertyChangedEventHandler PropertyChanged;

        Graphics.Bitmap BuildBitmap(Graphics.Rectangle rect, int subsample, int align, System.Drawing.Bitmap retVal);

        System.Drawing.Bitmap BuildImage(int subsample = 1);

        System.Drawing.Bitmap BuildPageImage();

        void ClearImage();

        void Dispose();

        System.Drawing.Bitmap ExtractThumbnailImage();

        PixelMap GetBgPixmap(Graphics.Rectangle rect, int subsample, double gamma, PixelMap retval);

        Graphics.Bitmap GetBitmap(Graphics.Rectangle rect, int subsample, int align, Graphics.Bitmap retval);

        Graphics.Bitmap GetBitmapList(Graphics.Rectangle rect, int subsample, int align, List<int> components);

        Map GetMap(Graphics.Rectangle segment, int subsample, Map retval);

        PixelMap GetPixelMap(Graphics.Rectangle rect, int subsample, double gamma, PixelMap retval);

        string GetTextForLocation(System.Drawing.Rectangle rect);

        void Preload();

        System.Drawing.Bitmap ResizeImage(int newWidth, int newHeight);
    }
}