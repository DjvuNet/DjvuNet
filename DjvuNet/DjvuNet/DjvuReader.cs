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
        #region Private Variables

        /// <summary>
        /// Full path to the djvu file
        /// </summary>
        private readonly string _location;

        #endregion Private Variables

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
        /// Reads a string which is null terminated
        /// </summary>
        /// <returns></returns>
        public string ReadNullTerminatedString()
        {
            StringBuilder builder = new StringBuilder();

            while (true)
            {
                sbyte value = ReadSByte();

                if (value == 0) break;
                builder.Append((char)value);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Reads a 3 sbyte unsigned integer value
        /// </summary>
        /// <returns></returns>
        public uint ReadUInt24MSB()
        {
            var b1 = ReadByte();
            var b2 = ReadByte();
            var b3 = ReadByte();
            return
                (((uint)b1) << 16) |
                (((uint)b2) << 8) |
                ((uint)b3);
        }

        /// <summary>
        /// Reads a 3 sbyte signed integer value
        /// </summary>
        /// <returns></returns>
        public int ReadInt24MSB()
        {
            var b1 = ReadByte();
            var b2 = ReadByte();
            var b3 = ReadByte();
            return
                (((int)b1) << 16) |
                (((int)b2) << 8) |
                ((int)b3);
        }

        /// <summary>
        /// Reads a 3 sbyte unsigned integer value
        /// </summary>
        /// <returns></returns>
        public uint ReadUInt24()
        {
            var b1 = ReadByte();
            var b2 = ReadByte();
            var b3 = ReadByte();
            return
                (((uint)b3) << 16) |
                (((uint)b2) << 8) |
                ((uint)b1);
        }

        /// <summary>
        /// Reads a 3 sbyte signed integer value
        /// </summary>
        /// <returns></returns>
        public int ReadInt24()
        {
            var b1 = ReadByte();
            var b2 = ReadByte();
            var b3 = ReadByte();
            return
                (((int)b3) << 16) |
                (((int)b2) << 8) |
                ((int)b1);
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
            var value = base.ReadInt16();
            return IPAddress.HostToNetworkOrder(value);
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
            var value = base.ReadInt32();
            return IPAddress.HostToNetworkOrder(value);
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
            var value = base.ReadInt64();
            return IPAddress.HostToNetworkOrder(value);
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
            unchecked
            {
                var value = base.ReadUInt16();
                return (ushort)IPAddress.HostToNetworkOrder((int)value);
            }
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
            unchecked
            {
                var value = base.ReadUInt32();
                return (uint)IPAddress.HostToNetworkOrder((int)value);
            }
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
            unchecked
            {
                // TODO Fix implementation
                var value = base.ReadUInt64();
                return (ulong)IPAddress.HostToNetworkOrder((long)value);
            }
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
            List<byte> stringBytes = new List<byte>();

            int bufferSize = 4096;

            while (true)
            {
                byte[] buffer = ReadBytes(bufferSize);
                stringBytes.AddRange(buffer);

                // Check if we read to the end of the stream
                if (buffer.Length != bufferSize)
                {
                    break;
                }
            }

            return Encoding.UTF8.GetString(stringBytes.ToArray());
        }

        /// <summary>
        /// Clones the reader for parallel reading at the given position
        /// </summary>
        /// <returns></returns>
        public DjvuReader CloneReader(long position)
        {
            DjvuReader newReader = null;

            // Get rid of not properly synchronized clones
            newReader =  _location != null ? new DjvuReader(_location) : new DjvuReader(BaseStream);
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