using DjvuNet.Compression;
using System.Text;

namespace DjvuNet
{
    public interface IDjvuWriter : IBinaryWriter
    {
        long Position { get; set; }

        DjvuWriter CloneWriter(long position);

        DjvuWriter CloneWriter(long position, long length);

        BzzWriter GetBZZEncodedWriter();

        BzzWriter GetBZZEncodedWriter(long length = 4096, int blockSize = 4096);

        string ToString();

        void WriteInt16BigEndian(short value);

        void WriteInt24(int value);

        void WriteInt24BigEndian(int value);

        void WriteInt32BigEndian(int value);

        void WriteInt64BigEndian(long value);

        void WriteJPEGImage(byte[] image);

        long WriteString(string value, bool skipBOM = true);

        long WriteString(string value, Encoding encoding);

        void WriteUInt16BigEndian(ushort value);

        void WriteUInt24(uint value);

        void WriteUInt24BigEndian(uint value);

        void WriteUInt32BigEndian(uint value);

        void WriteUInt64BigEndian(ulong value);

        long WriteUTF7String(string value);

        long WriteUTF8String(string value);
    }
}