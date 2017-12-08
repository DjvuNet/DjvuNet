using DjvuNet.JB2;

namespace DjvuNet.DataChunks
{
    public interface IDjbzChunk : IDjvuNode
    {
        JB2Dictionary ShapeDictionary { get; }
    }
}
