using System.Collections.Generic;

namespace DjvuNet.DataChunks
{
    public interface IDirmChunk : IDjvuNode
    {
        IReadOnlyList<DirmComponent> Components { get; }
        bool IsBundled { get; }
        int Version { get; }
    }
}