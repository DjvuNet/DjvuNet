using System.IO;

namespace DjvuNet.Compression
{

    public interface IDataCoderFactory
    {
        IDataCoder CreateCoder(Stream stream, bool encoding = false);

        IDataCoder CreateCoder(Stream stream, bool encoding = false, bool compatibility = true);
    }
}
