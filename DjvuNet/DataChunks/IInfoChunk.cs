namespace DjvuNet.DataChunks
{
    public interface IInfoChunk : IDjvuNode
    {
        int DPI { get; }
        float Gamma { get; }
        int Height { get; }
        byte MajorVersion { get; }
        byte MinorVersion { get; }
        PageRotations PageRotation { get; }
        int Width { get; }
    }
}