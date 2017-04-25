using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Compression
{

    public interface IDataCoderFactory
    {
        IDataCoder CreateCoder(Stream stream, bool encoding = false);

        IDataCoder CreateCoder(Stream stream, bool encoding = false, bool compatibility = true);
    }
}
