using DjvuNet.Graphics;
using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    public interface ITH44Chunk : IDjvuNode
    {
        PixelMap Image { get; }

        IInterWavePixelMap Thumbnail { get; }
    }
}