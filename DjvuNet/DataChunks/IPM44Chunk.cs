using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    public interface IPM44Chunk
    {
        ChunkType ChunkType { get; }

        IWPixelMap Image { get; }

        IWPixelMap ProgressiveDecodeBackground(IWPixelMap pixelMap);

        void ReadData(IDjvuReader reader);
    }
}