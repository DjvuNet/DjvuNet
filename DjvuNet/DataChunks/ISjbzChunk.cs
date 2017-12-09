using DjvuNet.JB2;

namespace DjvuNet.DataChunks
{
    public interface ISjbzChunk : IDjvuNode
    {
        JB2Image Image { get; }
    }
}
