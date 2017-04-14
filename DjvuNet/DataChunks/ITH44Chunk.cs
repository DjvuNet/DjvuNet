using System.Drawing;
using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    public interface ITH44Chunk : IDjvuNode
    {
        Bitmap Image { get; }
        IWPixelMap Thumbnail { get; }
    }
}