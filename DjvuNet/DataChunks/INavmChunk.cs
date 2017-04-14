
namespace DjvuNet.DataChunks
{
    public interface INavmChunk : IDjvuNode
    {
        Bookmark[] Bookmarks { get; }
    }
}