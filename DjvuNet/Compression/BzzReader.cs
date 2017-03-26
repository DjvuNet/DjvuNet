using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet;

namespace DjvuNet.Compression
{
    public class BzzReader : DjvuReader
    {
        public BzzReader(Stream stream) : base(new BSInputStream(stream))
        {

        }

        /// <summary>
        /// Creates instance of BzzReader. This overload prevents using
        /// constructor overload with BSInputStream parameter which
        /// will bypass underlying BSInputStream creation.
        /// </summary>
        /// <param name="stream"></param>
        public BzzReader(MemoryStream stream) : base (new BSInputStream(stream))
        {

        }

        /// <summary>
        /// Create instance of BzzReader using BSInputStream
        /// </summary>
        /// <param name="stream"></param>
        public BzzReader(BSInputStream stream): base(stream)
        {

        }

        /// <summary>
        /// Reads UTF8 encoded, null terminated string.
        /// </summary>
        /// <returns></returns>
        public string ReadNullTerminatedString(int bufferSize = 128)
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
                    // increase is exponential by doubling previous length
                    if (i >= buffer.Length)
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


        internal override MemoryStream ReadStringBytes(out Encoding enc, out int readBytes, bool skipBOM = true, int bufferSize = 1024)
        {
            enc = null;
            int bytesRead = 0;
            int streamLength = 0;

            MemoryStream targetStream = new MemoryStream(bufferSize);
            byte[] buffer = new byte[bufferSize];
            while (true)
            {
                int result = Read(buffer, 0, bufferSize);

                if (!skipBOM)
                    targetStream.Write(buffer, 0, result);
                else
                {
                    enc = CheckEncodingSignature(buffer, targetStream, ref result);
                    skipBOM = false;
                }

                if (bytesRead == 0)
                    streamLength = (int)BaseStream.Length - 1 - (bufferSize - result);

                bytesRead += result;

                // TODO verify if BSInputStream always creates data buffer 
                //      longer by 1 byte than decoded data and appends null

                if (streamLength > 0 && bytesRead < streamLength)
                {
                    if ((streamLength - bytesRead) < bufferSize)
                        bufferSize = streamLength - bytesRead;
                }

                if (bytesRead >= streamLength)
                    break;
            }

            readBytes = bytesRead;

            return targetStream;
            
        }
    }
}
