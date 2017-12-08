using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    public interface IFG44Chunk : IDjvuNode
    {
        IInterWavePixelMap ForegroundImage { get; }
    }
}
