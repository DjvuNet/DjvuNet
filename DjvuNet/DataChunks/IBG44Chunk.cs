using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    public interface IBG44Chunk : IDjvuNode
    {
        IWPixelMap BackgroundImage { get; }

        IWPixelMap ProgressiveDecodeBackground(IWPixelMap backgroundMap);
    }
}