using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DjvuNet.Wavelet
{
    /// <summary>
    /// Object representation of serialized IW44 image header
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct ImageDataHeader
    {
        [FieldOffset(0)]
        public fixed byte Data[9];

        [FieldOffset(0)]
        public byte Serial;

        [FieldOffset(1)]
        public byte Slices;

        [FieldOffset(2)]
        public byte MajorVersion;

        [FieldOffset(3)]
        public byte MinorVersion;

        [FieldOffset(4)]
        public byte WidthHi;

        [FieldOffset(5)]
        public byte WidthLo;

        [FieldOffset(6)]
        public byte HeightHi;

        [FieldOffset(7)]
        public byte HeightLo;

        [FieldOffset(8)]
        public byte CrCbDelay;

        public unsafe void ReadHeader(IDjvuReader reader)
        {
            byte[] buffer = null;
            if (reader.PeekChar() == 0)
                buffer = reader.ReadBytes(9);
            else
                buffer = reader.ReadBytes(2);

            fixed (byte* d = Data)
                Marshal.Copy(buffer, 0, (IntPtr)d, buffer.Length);
        }

        public void WriteHeader(Stream writer)
        {
            if (Serial != 0)
                WriteBaseHeader(writer);
            else
            {
                WriteBaseHeader(writer);
                WriteExtendedHeader(writer);
            }
        }

        public void WriteExtendedHeader(Stream writer)
        {
            writer.WriteByte(MajorVersion);
            writer.WriteByte(MinorVersion);
            writer.WriteByte(WidthHi);
            writer.WriteByte(WidthLo);
            writer.WriteByte(HeightHi);
            writer.WriteByte(HeightLo);
            writer.WriteByte(CrCbDelay);
        }

        public void WriteBaseHeader(Stream writer)
        {
            writer.WriteByte(Serial);
            writer.WriteByte(Slices);
        }
    }
}
