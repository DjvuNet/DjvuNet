using DjvuNet.JB2;

namespace DjvuNet.DataChunks
{
    public interface IWmrmChunk : IDjvuNode
    {
        JB2Image WatermarkImage { get; }
    }
}
