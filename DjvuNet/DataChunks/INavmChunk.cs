using System.Collections.Generic;

namespace DjvuNet.DataChunks
{
    public interface INavmChunk : IDjvuNode
    {
        IReadOnlyList<IBookmark> Bookmarks { get; }
    }
}
