namespace DjvuNet.DataChunks
{
    public interface IInclChunk : IDjvuNode
    {
        string IncludeID { get; }
    }
}