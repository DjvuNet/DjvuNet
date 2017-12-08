using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    public interface IBG44Chunk : IDjvuNode
    {
        IInterWavePixelMap BackgroundImage { get; }

        IInterWavePixelMap ProgressiveDecodeBackground(IInterWavePixelMap backgroundMap);
    }
}
