namespace DjvuNet.DataChunks
{
    public interface IBGjpChunk : IDjvuNode
    {
        byte[] BackgroundImage { get; }
    }
}
