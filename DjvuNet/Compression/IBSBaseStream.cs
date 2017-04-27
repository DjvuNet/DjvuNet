using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Compression
{
    public interface IBSBaseStream
    {
        long Tell();

        void Flush();

    }
}
