using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DjvuNet
{
    public partial class DjvuWriter : BinaryWriter, IDjvuWriter
    {
        public void Close()
        {
            Dispose(true);
        }
    }
}
