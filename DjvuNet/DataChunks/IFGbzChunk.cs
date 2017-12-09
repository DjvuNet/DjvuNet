namespace DjvuNet.DataChunks
{
    public interface IFGbzChunk : IDjvuNode
    {
        bool HasShapeTableData { get; }
        ColorPalette Palette { get; }
        int Version { get; }
    }
}
