using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DjvuNet.Compression;

namespace DjvuNet
{
    public class DjvuWriter : BinaryWriter, IDjvuWriter
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
                if (Position != value)
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
            FileStream stream = new FileStream(filePath, FileMode.OpenOrCreate, 
                FileAccess.ReadWrite, FileShare.ReadWrite);
            return stream;
        }

        #endregion Internal Methods

        #region Public Methods

        public long WriteJPEGImage(byte[] image)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Gets an encoding writer for the BZZ data
        /// </summary>
        /// <returns></returns>
        public BzzWriter GetBZZEncodedWriter(long length = 4096)
        {
            MemoryStream memStream = new MemoryStream((int)length);
            return new BzzWriter(new BSInputStream(memStream));
        }

        /// <summary>
        /// Gets an encoding writer for the BZZ data
        /// </summary>
        /// <returns></returns>
        public BzzWriter GetBZZEncodedWriter()
        {
            return new BzzWriter(new BSInputStream(BaseStream));
        }

        /// <summary>
        /// Reads a 3 sbyte unsigned integer value
        /// </summary>
        /// <returns></returns>
        public void WriteUInt24BigEndian(uint value)
        {

        }

        /// <summary>
        /// Reads a 3 sbyte signed integer value
        /// </summary>
        /// <returns></returns>
        public void WriteInt24BigEndian(int value)
        {
        }

        /// <summary>
        /// Reads a 3 sbyte unsigned integer value
        /// </summary>
        /// <returns></returns>
        public void WriteUInt24(uint value)
        {

        }

        /// <summary>
        /// Reads a 3 sbyte signed integer value
        /// </summary>
        /// <returns></returns>
        public void WriteInt24(int value)
        {
        }

        /// <summary>
        /// Reads a 2-sbyte signed integer from the current stream and advances 
        /// the current position of the stream by two bytes.
        /// </summary>
        /// <returns>
        /// A 2-sbyte signed integer read from the current stream.
        /// </returns>
        /// <exception cref="T:System.IO.EndOfStreamException">
        /// The end of the stream is reached. 
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// The stream is closed. 
        /// </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <filterpriority>2</filterpriority>
        public void WriteInt16BigEndian(short value)
        {
        }

        /// <summary>
        /// Reads a 4-sbyte signed integer from the current stream and advances the current position of the stream by four bytes.
        /// </summary>
        /// <returns>
        /// A 4-sbyte signed integer read from the current stream.
        /// </returns>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached. </exception><exception cref="T:System.ObjectDisposedException">The stream is closed. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>2</filterpriority>
        public void WriteInt32BigEndian(int value)
        {
        }

        /// <summary>
        /// Reads an 8-sbyte signed integer from the current stream and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <returns>
        /// An 8-sbyte signed integer read from the current stream.
        /// </returns>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached. </exception><exception cref="T:System.ObjectDisposedException">The stream is closed. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>2</filterpriority>
        public void WriteInt64BigEndian(long value)
        {
        }

        /// <summary>
        /// Reads a 2-sbyte unsigned integer from the current stream using little-endian encoding and advances the position of the stream by two bytes.
        /// </summary>
        /// <returns>
        /// A 2-sbyte unsigned integer read from this stream.
        /// </returns>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached. </exception><exception cref="T:System.ObjectDisposedException">The stream is closed. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>2</filterpriority>
        public void WriteUInt16BigEndian(ushort value)
        {
        }

        /// <summary>
        /// Reads a 4-sbyte unsigned integer from the current stream and advances the position of the stream by four bytes.
        /// </summary>
        /// <returns>
        /// A 4-sbyte unsigned integer read from this stream.
        /// </returns>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached. </exception><exception cref="T:System.ObjectDisposedException">The stream is closed. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>2</filterpriority>
        public void WriteUInt32BigEndian(uint value)
        {
        }

        /// <summary>
        /// Reads an 8-sbyte unsigned integer from the current stream and advances 
        /// the position of the stream by eight bytes.
        /// </summary>
        /// <returns>
        /// An 8-sbyte unsigned integer read from this stream.
        /// </returns>
        /// <exception cref="T:System.IO.EndOfStreamException">
        /// The end of the stream is reached. 
        /// </exception>
        /// <exception cref="T:System.IO.IOException">
        /// An I/O error occurs. 
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// The stream is closed. 
        /// </exception>
        /// <filterpriority>2</filterpriority>
        public void WriteUInt64BigEndian(ulong value)
        {
        }

        /// <summary>
        /// Reads the bytes into a UTF8 string
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public long WriteUTF8String(string value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads the bytes into a UTF7 string
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public long WriteUTF7String(string value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads a string which terminates at EOS
        /// </summary>
        /// <returns></returns>
        public long WriteString(string value, bool skipBOM = true)
        {
            throw new NotImplementedException();
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
            return $"{{{this.GetType().Name} {{ Position: 0x{Position:x}, Length: 0x{this.BaseStream?.Length:x} BaseStream: {BaseStream} }}}}";
        }

        #endregion Public Methods

        #region Private Methods

        #endregion Private Methods
    }
}
