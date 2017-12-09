namespace DjvuNet.DataChunks
{
    public interface IFGjpChunk : IDjvuNode
    {
        byte[] ForegroundImage { get; }
    }
}
