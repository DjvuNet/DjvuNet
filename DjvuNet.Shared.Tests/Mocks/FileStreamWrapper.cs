using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DjvuNet.Tests.Mocks
{
    public class FileStreamWrapper : Stream
    {
        public FileStreamWrapper(string path, FileMode mode, FileAccess access, FileShare share) 
            : base()
        {

        }

        public override void Close()
        {
            base.Close();
        }

        public override void Flush()
        {
        }

        public void Flush(bool flushToDisk)
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return base.FlushAsync(cancellationToken);
        }

        public override long Length => 0;

        public override long Position
        {
            get { return 0; }
            set
            {
                long x = value;
            }
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override int Read(byte[] array, int offset, int count)
        {
            return 0;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int ReadByte()
        {
            return base.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
        }

        public override void Write(byte[] array, int offset, int count)
        {
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            base.WriteByte(value);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
 
    }
}
