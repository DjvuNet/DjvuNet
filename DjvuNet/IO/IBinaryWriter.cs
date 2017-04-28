using System.IO;

namespace DjvuNet
{
    public interface IBinaryWriter
    {
        Stream BaseStream { get; }

        void Close();

        void Dispose();

        void Flush();

        long Seek(int offset, SeekOrigin origin);

        void Write(decimal value);

        void Write(float value);

        void Write(long value);

        void Write(short value);

        void Write(uint value);

        void Write(ushort value);

        void Write(ulong value);

        void Write(string value);

        void Write(sbyte value);

        void Write(int value);

        void Write(double value);

        void Write(byte[] buffer);

        void Write(char ch);

        void Write(char[] chars);

        void Write(byte value);

        void Write(bool value);

        void Write(char[] chars, int index, int count);

        void Write(byte[] buffer, int index, int count);
    }
}