using DjvuNet.Graphics;
using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    public interface ITH44Chunk : IDjvuNode
    {
        IPixelMap Image { get; }

        IInterWavePixelMap Thumbnail { get; }
    }
}