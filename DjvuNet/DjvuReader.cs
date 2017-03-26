// <copyright file="DjvuReader.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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

        internal Encoding _currentEncoding;

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
        /// Creates DjvuReader for the resource indicated by Uri
        /// </summary>
        /// <param name="link"></param>
        public DjvuReader(Uri link) : base(GetWebStream(link)) 
        {
        }

        /// <summary>
        /// Creates the DjvuReader from the stream
        /// </summary>
        /// <param name="stream"></param>
        public DjvuReader(Stream stream)
            : base(stream)
        {
        }

        public DjvuReader(string location)
            : base(new FileStream(location, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            _location = location;
        }

        #endregion Constructors

        #region Internal Methods

        internal static Stream GetWebStream(Uri link)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));

            WebClient client = new WebClient();
            return client.OpenRead(link);
        }

        #endregion Internal Methods

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
        public BzzReader GetBZZEncodedReader(long length)
        {
            // Read the bytes into a stream to decode
            MemoryStream memStream = new MemoryStream(ReadBytes(checked((int)length)));

            return new BzzReader(new BSInputStream(memStream));
        }

        /// <summary>
        /// Gets a reader for the BZZ data
        /// </summary>
        /// <returns></returns>
        public BzzReader GetBZZEncodedReader()
        {
            return new BzzReader(new BSInputStream(BaseStream));
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
        public string ReadUnknownLengthString(bool skipBOM = true)
        {
            Encoding enc = null;
            int length = 0;
            using(MemoryStream stream = ReadStringBytes(out enc, out length, skipBOM))
            {
                byte[] test = stream.GetBuffer();
                if (enc == null)
                {
                    if (_currentEncoding != null)
                        enc = _currentEncoding;
                    else
                        enc = _currentEncoding = new UTF8Encoding();
                }
                return enc.GetString(test, 0, length);
            }
        }

        internal virtual MemoryStream ReadStringBytes(out Encoding enc, out int readBytes, bool skipBOM = true, int bufferSize = 1024)
        {
            enc = null;
            int bytesRead = 0;
            MemoryStream ms = new MemoryStream(bufferSize);
            byte[] buffer = new byte[bufferSize];
            while (true)
            {
                int result = Read(buffer, 0, bufferSize);
                    
                if (!skipBOM)
                    ms.Write(buffer, 0, result);
                else
                {
                    enc = CheckEncodingSignature(buffer, ms, ref result);
                    skipBOM = false;
                }

                bytesRead += result;

                // Check if we read to the end of the stream
                if (result < bufferSize)
                    break;
            }
            readBytes = bytesRead;

            return ms;
        }

        /// <summary>
        /// Function verifies if Encoding Scheme Signature is present, decodes it,
        /// creates Encoding object with detected encoding and writes to passed Stream
        /// skipping decoded signature (Byte Order Mark - BOM).
        /// For more information see http://www.unicode.org/versions/Unicode9.0.0/ch23.pdf
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="stream"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        internal static Encoding CheckEncodingSignature(byte[] buffer, Stream stream, ref int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (count < 4)
                throw new ArgumentOutOfRangeException(
                    "To verify Encoding Scheme Signature caller should pass buffer with at least 4 bytes.", 
                    nameof(count));

            if (buffer.Length < count)
                throw new ArgumentException(nameof(buffer));

            byte[] checkBuffer = new byte[4];
            Buffer.BlockCopy(buffer, 0, checkBuffer, 0, 4);
            uint testValue = BitConverter.ToUInt32(checkBuffer, 0);

            // If data are coming from file need to compensate
            // for Encoding Scheme Signature which may be present
            // http://www.unicode.org/versions/Unicode9.0.0/ch23.pdf
            // Encoding Scheme Signature
            // UTF-8                    EF BB BF
            // UTF-16 Big-endian        FE FF
            // UTF-16 Little-endian     FF FE
            // UTF-32 Big-endian        00 00 FE FF
            // UTF-32 Little-endian     FF FE 00 00

            Encoding detectedEncoding = null;

            switch (testValue & 0x00ffffff)
            {
                // UTF-8  EF BB BF
                case 0x00bfbbef:
                    detectedEncoding = new UTF8Encoding(false);
                    stream.Write(buffer, 3, count - 3);
                    count -= 3;
                    return detectedEncoding;
            }

            switch(testValue & 0xffffffff)
            {
                // UTF-32 Big-endian  00 00 FE FF
                case 0xfffe0000:
                    detectedEncoding = new UTF32Encoding(true, false);
                    goto process;

                // UTF-32 Little-endian  FF FE 00 00
                case 0x0000feff:
                    detectedEncoding = new UTF32Encoding(false, false);

                    process:
                    stream.Write(buffer, 4, count - 4);
                    count -= 4;
                    return detectedEncoding;

            }

            switch(testValue & 0x0000ffff)
            {
                // UTF-16 Big-endian   FE FF
                case 0x0000fffe:
                    detectedEncoding = new UnicodeEncoding(true, false);
                    goto process;

                // UTF-16 Little-endian   FF FE
                case 0x0000feff:
                    detectedEncoding = new UnicodeEncoding(false, false);

                    process:
                    stream.Write(buffer, 2, count - 2);
                    count -= 2;
                    return detectedEncoding;

            }
            return null;
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
            return $"{this.GetType().Name} {{ Position: {Position}, Length: {this.BaseStream?.Length} BaseStream: {BaseStream} }}";
        }

        #endregion Public Methods

        #region Private Methods

        #endregion Private Methods
    }
}