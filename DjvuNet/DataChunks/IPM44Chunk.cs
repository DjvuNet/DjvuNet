using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    public interface IPM44Chunk
    {
        ChunkType ChunkType { get; }

        IInterWavePixelMap Image { get; }

        IInterWavePixelMap ProgressiveDecodeBackground(IInterWavePixelMap pixelMap);

        void ReadData(IDjvuReader reader);
    }
}