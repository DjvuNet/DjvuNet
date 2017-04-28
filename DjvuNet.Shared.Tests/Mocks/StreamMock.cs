using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DjvuNet.Tests
{
    public class StreamMock : Stream, IStream
    {
        public override bool CanRead { get { return true; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanTimeout
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool CanWrite { get { return false; } }

        public override long Length { get { return 1024; } }

        public override long Position { get { return 20; } set { } }

        public override int ReadTimeout { get; set; }

        public override int WriteTimeout { get; set; }

        public override void Close()
        {
            throw new NotImplementedException();
        }

        public new void CopyTo(Stream destination)
        {
            throw new NotImplementedException();
        }

        public new void CopyTo(Stream destination, int bufferSize)
        {
            throw new NotImplementedException();
        }

        public new Task CopyToAsync(Stream destination)
        {
            throw new NotImplementedException();
        }

        public new Task CopyToAsync(Stream destination, int bufferSize)
        {
            throw new NotImplementedException();
        }

        public new Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override void Flush() { }

        public new Task FlushAsync()
        {
            throw new NotImplementedException();
        }

        public new Task FlushAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public new Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public new Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override int ReadByte()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public new Task WriteAsync(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public new Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override void WriteByte(byte value)
        {
            throw new NotImplementedException();
        }
    }
}
