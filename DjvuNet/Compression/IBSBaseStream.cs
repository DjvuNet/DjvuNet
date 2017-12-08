namespace DjvuNet.Compression
{
    public interface IBSBaseStream
    {
        long Tell();

        void Flush();

    }
}
