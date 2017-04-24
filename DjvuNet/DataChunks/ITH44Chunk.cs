using System.Drawing;
using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    public interface ITH44Chunk : IDjvuNode
    {
        Bitmap Image { get; }
        IInterWavePixelMap Thumbnail { get; }
    }
}