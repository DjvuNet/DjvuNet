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

        IDjvuDocument Document { get; }

        DirmComponent Header { get; }

        int Height { get; }

        IReadOnlyList<IDjviChunk> IncludeFiles { get; }

        InfoChunk Info { get; }

        DjvuFormElement PageForm { get; }

        int PageNumber { get; }

        string Text { get; }

        TextChunk TextChunk { get; }

        int Width { get; }

        event PropertyChangedEventHandler PropertyChanged;

        //void ClearImage();

        void Dispose();

        string GetTextForLocation(System.Drawing.Rectangle rect);

        //void Preload();

        IInterWavePixelMap BackgroundIWPixelMap { get; }

        IInterWavePixelMap ForegroundIWPixelMap { get; }

        JB2Image ForegroundJB2Image { get; }

        ColorPalette ForegroundPalette { get; }

        IPixelMap ForegroundPixelMap { get; }

        bool IsColor { get; }

        bool IsInverted { get; set; }

        //bool IsPageImageCached { get; set; }

        ITH44Chunk Thumbnail { get; }

        //System.Drawing.Bitmap ThumbnailImage { get; set; }

        IBitmap BuildBitmap(Graphics.Rectangle rect, int subsample, int align, System.Drawing.Bitmap retVal);

        //System.Drawing.Bitmap BuildImage(int subsample = 1);

        //System.Drawing.Bitmap BuildPageImage(bool rebuild = false);

        //System.Drawing.Bitmap ExtractThumbnailImage();

        IPixelMap GetBgPixmap(Graphics.Rectangle rect, int subsample, double gamma, IPixelMap retval);

        Graphics.IBitmap GetBitmap(Graphics.Rectangle rect, int subsample, int align, Graphics.IBitmap retval);

        Graphics.IBitmap GetBitmapList(Graphics.Rectangle rect, int subsample, int align, List<int> components);

        IMap GetMap(Graphics.Rectangle segment, int subsample, IMap retval);

        IPixelMap GetPixelMap(Graphics.Rectangle rect, int subsample, double gamma, IPixelMap retval);

        IDjvuImage Image { get; }
    }
}
