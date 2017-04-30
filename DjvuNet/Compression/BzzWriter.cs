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

        public BzzWriter(Stream stream, int blockSize = 4096) 
            : base(new BSOutputStream(stream, blockSize))
        {

        }

        public BzzWriter(string filePath, int blockSize = 4096) 
            : base(new BSOutputStream(GetFile(filePath), blockSize))
        {

        }

        public override void Flush()
        {
            base.Flush();
        }

        public override void Write(string value)
        {
            Write(value, new UTF8Encoding(false));
        }

        private void Write(string value, Encoding encoding)
        {
            byte[] buffer = new byte[encoding.GetByteCount(value) + 1];
            encoding.GetBytes(value.ToCharArray(), 0, value.Length, buffer, 0);
            base.Write(buffer, 0, buffer.Length);
        }

    }
}
