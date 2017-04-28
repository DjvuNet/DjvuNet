

namespace DjvuNet.DataChunks
{
    public interface ITextChunk : IDjvuNode
    {
        string Text { get; }

        int TextLength { get; }

        int Version { get; }

        TextZone Zone { get; }
    }
}