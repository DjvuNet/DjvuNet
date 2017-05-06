namespace DjvuNet.DataChunks
{
    public interface IDjviChunk : IDjvuElement
    {
        string Dictionary { get; set; }
    }
}