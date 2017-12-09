using System.IO;

namespace DjvuNet.Compression
{
    public class ZPCoderFactory : IDataCoderFactory
    {
        public IDataCoder CreateCoder(Stream stream, bool encoding = false)
        {
            return CreateCoder(stream, encoding, true);
        }

        public IDataCoder CreateCoder(Stream stream, bool encoding = false, bool compatibility = true)
        {
            ZPCodec codec = new ZPCodec { DjvuCompat = compatibility, Encoding = encoding };
            return codec.Initializa(stream);
        }
    }
}
