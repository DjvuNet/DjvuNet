using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DjvuNet
{
    public partial class DjvuReader : BinaryReader, IDjvuReader
    {
        public void Close()
        {
            Dispose(true);
        }
    }
}
