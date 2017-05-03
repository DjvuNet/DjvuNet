namespace DjvuNet.DataChunks
{
    public interface IBookmark
    {
        IBookmark[] Children { get; }

        IDjvuDocument Document { get; }

        string Name { get; }

        IBookmark Parent { get; }

        INavmChunk NavmNode { get; }

        IDjvuPage ReferencedPage { get; }

        int TotalBookmarks { get; }

        string Url { get; }

    }
}