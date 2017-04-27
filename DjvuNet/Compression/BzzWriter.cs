using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace DjvuNet.Compression
{
    /// <summary>
    /// BzzWriter encodes (compresses) plain data using bzz coder while writing to output stream.   
    /// </summary>
    public class BzzWriter : DjvuWriter
    {
        public BzzWriter(BSOutputStream bsStream) : base(bsStream)
        {

        }

        public BzzWriter(Stream stream, int blockSize = 4096) : base(new BSOutputStream(stream, blockSize))
        {

        }

        public BzzWriter(string filePath, int blockSize = 4096) : base(new BSOutputStream(GetFile(filePath), blockSize))
        {

        }
    }
}
