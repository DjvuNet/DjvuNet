namespace DjvuNet.DataChunks
{
    public interface IAnnotationChunk : IDjvuNode
    {
        Annotation[] Annotations { get; }
    }
}
