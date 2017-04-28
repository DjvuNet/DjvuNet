using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DjvuNet.Compression
{
    public class BzzWriter : DjvuWriter
    {
        public BzzWriter(BSInputStream bsStream) : base(bsStream)
        {

        }

        public BzzWriter(string filePath) : base(new BSInputStream(GetFile(filePath)))
        {

        }
    }
}
