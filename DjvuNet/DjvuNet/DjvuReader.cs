// <copyright file="DjvuReader.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using DjvuNet.Compression;

namespace DjvuNet
{
    // TODO File feature request with .NET CoreClr project to change BinaryReader API
    // to enable > 4 GB reads support - data got lot bigger now

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DjvuReader : BinaryReader
    {
        #region Private Members

        /// <summary>
        /// Full path to the djvu file
        /// </summary>
        private readonly string _location;

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
        /// Creates the DjvuReader from the stream
        /// </summary>
        /// <param name="stream"></param>
        public DjvuReader(Stream stream)
            : base(stream)
        {
            // Nothing
        }

        public DjvuReader(string location)
            : base(new FileStream(location, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            _location = location;
        }

        #endregion Constructors

        #region Public Methods

        public Image GetJPEGImage(long length)
        {
            MemoryStream mem = new MemoryStream(ReadBytes(checked((int)length)));
            return Image.FromStream(mem);
        }

        /// <summary>
        /// Gets a fixed length binary stream
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public DjvuReader GetFixedLengthStream(long length)
        {
            MemoryStream mem = new MemoryStream(ReadBytes(checked((int)length)));

            return new DjvuReader(mem);
        }

        /// <summary>
        /// Gets a reader for the BZZ data
        /// </summary>
        /// <returns></returns>
        public DjvuReader GetBZZEncodedReader(long length)
        {
            // Read the bytes into a stream to decode
            MemoryStream memStream = new MemoryStream(ReadBytes(checked((int)length)));

            return new DjvuReader(new BSInputStream(memStream));
        }

        /// <summary>
        /// Gets a reader for the BZZ data
        /// </summary>
        /// <returns></returns>
        public DjvuReader GetBZZEncodedReader()
        {
            return new DjvuReader(new BSInputStream(BaseStream));
        }

        /// <summary>
        /// Reads UTF8 encoded, null terminated string.
        /// </summary>
        /// <returns></returns>
        public string ReadNullTerminatedString(int bufferSize = 64)
        {
            try
            {
                byte[] buffer = new byte[bufferSize];
                int i = 0;
                for (; ; i++)
                {
                    byte b = ReadByte();
                    if (b == 0)
                        break;

                    // Increase buffer size if string is longer
                    if (i >= (buffer.Length - 1))
                    {
                        byte[] copyBuffer = new byte[buffer.Length * 2];
                        Buffer.BlockCopy(buffer, 0, copyBuffer, 0, buffer.Length);
                        buffer = copyBuffer;
                    }

                    buffer[i] = b;
                }

                if (i <= 0)
                    return String.Empty;
                else
                {
                    byte[] copyBuffer = new byte[i];
                    Buffer.BlockCopy(buffer, 0, copyBuffer, 0, copyBuffer.Length);
                    return Encoding.UTF8.GetString(copyBuffer);
                }
            }
            catch (Exception err)
            {
                throw new DjvuFormatException("Error while reading null terminated string.", err);
            }
        }

        /// <summary>
        /// Reads a 3 sbyte unsigned integer value
        /// </summary>
        /// <returns></returns>
        public uint ReadUInt24MSB()
        {
            byte[] buffer = new byte[4];
            Read(buffer, 1, 3);
            Array.Reverse(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }

        /// <summary>
        /// Reads a 3 sbyte signed integer value
        /// </summary>
        /// <returns></returns>
        public int ReadInt24MSB()
        {
            byte[] buffer = new byte[4];
            Read(buffer, 1, 3);
            Array.Reverse(buffer);
            return BitConverter.ToInt32(buffer, 0);
        }

        /// <summary>
        /// Reads a 3 sbyte unsigned integer value
        /// </summary>
        /// <returns></returns>
        public uint ReadUInt24()
        {
            byte[] buffer = new byte[4];
            Read(buffer, 1, 3);
            return BitConverter.ToUInt32(buffer, 0);
        }

        /// <summary>
        /// Reads a 3 sbyte signed integer value
        /// </summary>
        /// <returns></returns>
        public int ReadInt24()
        {
            byte[] buffer = new byte[4];
            Read(buffer, 1, 3);
            return BitConverter.ToInt32(buffer, 0);
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
        public short ReadInt16MSB()
        {
            var value = ReadBytes(2);
            Array.Reverse(value);
            return BitConverter.ToInt16(value, 0);
        }

        /// <summary>
        /// Reads a 4-sbyte signed integer from the current stream and advances the current position of the stream by four bytes.
        /// </summary>
        /// <returns>
        /// A 4-sbyte signed integer read from the current stream.
        /// </returns>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached. </exception><exception cref="T:System.ObjectDisposedException">The stream is closed. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>2</filterpriority>
        public int ReadInt32MSB()
        {
            var value = ReadBytes(4);
            Array.Reverse(value);
            return BitConverter.ToInt32(value, 0);
        }

        /// <summary>
        /// Reads an 8-sbyte signed integer from the current stream and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <returns>
        /// An 8-sbyte signed integer read from the current stream.
        /// </returns>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached. </exception><exception cref="T:System.ObjectDisposedException">The stream is closed. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>2</filterpriority>
        public long ReadInt64MSB()
        {
            var value = ReadBytes(8);
            Array.Reverse(value);
            return BitConverter.ToInt64(value, 0);
        }

        /// <summary>
        /// Reads a 2-sbyte unsigned integer from the current stream using little-endian encoding and advances the position of the stream by two bytes.
        /// </summary>
        /// <returns>
        /// A 2-sbyte unsigned integer read from this stream.
        /// </returns>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached. </exception><exception cref="T:System.ObjectDisposedException">The stream is closed. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>2</filterpriority>
        public ushort ReadUInt16MSB()
        {
            var value = ReadBytes(2);
            Array.Reverse(value);
            return BitConverter.ToUInt16(value, 0);
        }

        /// <summary>
        /// Reads a 4-sbyte unsigned integer from the current stream and advances the position of the stream by four bytes.
        /// </summary>
        /// <returns>
        /// A 4-sbyte unsigned integer read from this stream.
        /// </returns>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached. </exception><exception cref="T:System.ObjectDisposedException">The stream is closed. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>2</filterpriority>
        public uint ReadUInt32MSB()
        {
            var value = ReadBytes(4);
            Array.Reverse(value);
            return BitConverter.ToUInt32(value, 0);
        }

        /// <summary>
        /// Reads an 8-sbyte unsigned integer from the current stream and advances the position of the stream by eight bytes.
        /// </summary>
        /// <returns>
        /// An 8-sbyte unsigned integer read from this stream.
        /// </returns>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the stream is reached. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.ObjectDisposedException">The stream is closed. </exception><filterpriority>2</filterpriority>
        public ulong ReadUInt64MSB()
        {
            var value = ReadBytes(8);
            Array.Reverse(value);
            return BitConverter.ToUInt64(value, 0);
        }

        /// <summary>
        /// Reads the bytes into a UTF8 string
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public string ReadUTF8String(long length)
        {
            byte[] data = ReadBytes(checked((int)length));
            return Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// Reads the bytes into a UTF7 string
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public string ReadUTF7String(long length)
        {
            byte[] data = ReadBytes(checked((int)length));
            return Encoding.UTF7.GetString(data);
        }

        /// <summary>
        /// Reads a string which terminates at EOS
        /// </summary>
        /// <returns></returns>
        public string ReadUnknownLengthString()
        {
            int bufferSize = 1024;
            byte[] strBuffer = null;
            using (MemoryStream ms = new MemoryStream(bufferSize))
            {
                byte[] buffer = new byte[bufferSize];
                while (true)
                {
                    int result = Read(buffer, 0, bufferSize);
                    ms.Write(buffer, 0, result);

                    // Check if we read to the end of the stream
                    if (buffer.Length != bufferSize)
                        break;
                }
                strBuffer = ms.GetBuffer();
            }

            return Encoding.UTF8.GetString(strBuffer);
        }

        /// <summary>
        /// Clones the reader for parallel reading at the given position
        /// </summary>
        /// <returns></returns>
        public DjvuReader CloneReader(long position)
        {
            DjvuReader newReader = null;

            // Get rid of not properly synchronized clones
            newReader = _location != null ? new DjvuReader(_location) : new DjvuReader(BaseStream);
            newReader.Position = position;

            return newReader;
        }

        /// <summary>
        /// Clones the reader for parallel reading at the given position
        /// </summary>
        /// <returns></returns>
        public DjvuReader CloneReader(long position, long length)
        {
            DjvuReader newReader = null;

            // Clone the reader
            newReader = _location != null ? new DjvuReader(_location) : new DjvuReader(BaseStream);
            newReader.Position = position;

            return newReader.GetFixedLengthStream(checked((int)length));
        }

        public override string ToString()
        {
            return $"{base.ToString()} {{ Position {Position}, Length {this.BaseStream?.Length} }}";
        }

        #endregion Public Methods

        #region Private Methods

        #endregion Private Methods
    }
}