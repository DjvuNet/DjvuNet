using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public interface IStream
    {
        bool CanRead { get; }
        bool CanSeek { get; }
        bool CanTimeout { get; }
        bool CanWrite { get; }
        long Length { get; }
        long Position { get; set; }
        int ReadTimeout { get; set; }
        int WriteTimeout { get; set; }

        void Close();
        void CopyTo(Stream destination);
        void CopyTo(Stream destination, int bufferSize);
        Task CopyToAsync(Stream destination);
        Task CopyToAsync(Stream destination, int bufferSize);
        Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken);
        void Dispose();
        void Flush();
        Task FlushAsync();
        Task FlushAsync(CancellationToken cancellationToken);
        int Read(byte[] buffer, int offset, int count);
        Task<int> ReadAsync(byte[] buffer, int offset, int count);
        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        int ReadByte();
        long Seek(long offset, SeekOrigin origin);
        void SetLength(long value);
        void Write(byte[] buffer, int offset, int count);
        Task WriteAsync(byte[] buffer, int offset, int count);
        Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        void WriteByte(byte value);
    }
}