using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    public interface IFG44Chunk : IDjvuNode
    {
        IWPixelMap ForegroundImage { get; }
    }
}