using System;
using System.IO;
using System.Text;

using DjvuNet.Compression;

namespace DjvuNet
{
    public partial class DjvuWriter : BinaryWriter, IDjvuWriter
    {
        #region Private Members

        /// <summary>
        /// Full path to the djvu file
        /// </summary>
        private readonly string _Location;

        internal Encoding _CurrentEncoding;

        #endregion Private Members

        #region Public Properties

        #region Position

        /// <summary>
        /// Gets or sets the position in the reader
        /// </summary>
        public long Position
        {
            get { return BaseStream.Position; }

            set
            {
                if (BaseStream.Position != value)
                {
                    BaseStream.Position = value;
                }
            }
        }

        #endregion Position

        #endregion Public Properties

        #region Constructors


        /// <summary>
        /// Creates the DjvuWriter from the stream
        /// </summary>
        /// <param name="stream"></param>
        public DjvuWriter(Stream stream)
            : base(stream)
        {
        }

        public DjvuWriter(string filePath)
            : base(GetFile(filePath))
        {
            _Location = filePath;
        }

        #endregion Constructors

        #region Internal Methods

        internal static FileStream GetFile(string filePath)
        {
            FileStream stream = new FileStream(filePath, FileMode.Create,
                FileAccess.ReadWrite, FileShare.ReadWrite);
            return stream;
        }

        #endregion Internal Methods

        #region Public Methods

        public void WriteJPEGImage(byte[] image)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Gets an encoding writer for the BZZ data
        /// </summary>
        /// <returns></returns>
        public BzzWriter GetBZZEncodedWriter(long length = 4096, int blockSize = 4096)
        {
            MemoryStream memStream = new MemoryStream((int)length);
            return new BzzWriter(new BSOutputStream(memStream, blockSize));
        }

        /// <summary>
        /// Gets an encoding writer for the BZZ data
        /// </summary>
        /// <returns></returns>
        public BzzWriter GetBZZEncodedWriter()
        {
            return GetBZZEncodedWriter(4096);
        }

        /// <summary>
        /// Reads a 3 sbyte unsigned integer value
        /// </summary>
        /// <returns></returns>
        public void WriteUInt24BigEndian(uint value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            Write(buffer, 1, 3);
        }

        /// <summary>
        /// Reads a 3 sbyte signed integer value
        /// </summary>
        /// <returns></returns>
        public void WriteInt24BigEndian(int value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            Write(buffer, 1, 3);
        }

        /// <summary>
        /// Reads a 3 sbyte unsigned integer value
        /// </summary>
        /// <returns></returns>
        public void WriteUInt24(uint value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Write(buffer, 0, 3);
        }

        /// <summary>
        /// Reads a 3 sbyte signed integer value
        /// </summary>
        /// <returns></returns>
        public void WriteInt24(int value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Write(buffer, 0, 3);
        }

        public void WriteInt16BigEndian(short value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            Write(buffer, 0, 2);
        }

        public void WriteInt32BigEndian(int value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            Write(buffer, 0, 4);
        }

        public void WriteInt64BigEndian(long value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            Write(buffer, 0, 8);
        }

        public void WriteUInt16BigEndian(ushort value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            Write(buffer, 0, 2);
        }

        public void WriteUInt32BigEndian(uint value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            Write(buffer, 0, 4);
        }

        /// <summary>
        /// Writes UInt64 in Big Endian order.
        /// </summary>
        /// <param name="value"></param>
        public void WriteUInt64BigEndian(ulong value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            Write(buffer, 0, 8);
        }

        /// <summary>
        /// Writes the bytes of a UTF8 string without BOM.
        /// </summary>
        /// <returns></returns>
        public long WriteUTF8String(string value)
        {
            UTF8Encoding encoding = new UTF8Encoding(false);
            return WriteString(value, encoding);
        }

        /// <summary>
        /// Writes all bytes of a UTF7 string.
        /// </summary>
        /// <returns></returns>
        public long WriteUTF7String(string value)
        {
            UTF7Encoding encoding = new UTF7Encoding(true);
            return WriteString(value, encoding);
        }

        /// <summary>
        /// Writes a Unicode UTF-16 encoded string.
        /// </summary>
        /// <returns></returns>
        public long WriteString(string value, bool skipBOM = true)
        {
            UnicodeEncoding encoding = null;
            if (skipBOM)
            {
                encoding = new UnicodeEncoding(false, false);
            }
            else
            {
                encoding = new UnicodeEncoding(false, true);
            }

            return WriteString(value, encoding);
        }

        /// <summary>
        /// Writes bytes of the string using passed encoder.
        /// Encoders may emit BOM if not created with parameters
        /// explicitly asking not to do that.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="encoding"></param>
        /// <returns>Number of bytes written.</returns>
        public long WriteString(string value, Encoding encoding)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            byte[] buffer = encoding.GetBytes(value);
            Write(buffer, 0, buffer.Length);
            return buffer.Length;
        }

        /// <summary>
        /// Clones the reader for parallel reading at the given position
        /// </summary>
        /// <returns></returns>
        public DjvuWriter CloneWriter(long position)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clones the reader for parallel reading at the given position
        /// </summary>
        /// <returns></returns>
        public DjvuWriter CloneWriter(long position, long length)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"{{{this.GetType().Name} {{ Position: 0x{Position:x}, Length: 0x{BaseStream.Length:x} BaseStream: {BaseStream} }}}}";
        }

        #endregion Public Methods

        #region Private Methods

        #endregion Private Methods
    }
}
