using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            ZPCodec codec = new ZPCodec { DjvuCompat = compatibility };
            return codec.Initializa(stream);
        }
    }
}
