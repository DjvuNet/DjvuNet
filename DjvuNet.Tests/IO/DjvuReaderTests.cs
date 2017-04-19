using Xunit;
using DjvuNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DjvuNet.Compression;

namespace DjvuNet.Tests
{
    public class DjvuReaderTests
    {
        public static byte[] BzzCompressedTestBuffer
        {
            get
            {
                string bzzFile = Path.Combine(Util.RepoRoot, "artifacts", "data", "testbzz.bz");
                using (FileStream stream = File.OpenRead(Path.Combine(Util.RepoRoot, bzzFile)))
                {
                    byte[] buffer = new byte[stream.Length];
                    int countRead = stream.Read(buffer, 0, buffer.Length);
                    if (countRead != buffer.Length)
                        throw new IOException($"Unable to read file with test data: {bzzFile}");
                    return buffer;
                }
            }
        }

        public static byte[] OriginalBzzTestBuffer
        {
            get
            {
                string bzzFile = Path.Combine(Util.RepoRoot, "artifacts", "data", "testbzz.obz");
                return ReadFileToEnd(bzzFile);
            }
        }

        public static byte[] ReadFileToEnd(string bzzFile)
        {
            using (FileStream stream = File.OpenRead(Path.Combine(Util.RepoRoot, bzzFile)))
            {
                byte[] buffer = new byte[stream.Length];
                int countRead = stream.Read(buffer, 0, buffer.Length);
                if (countRead != buffer.Length)
                    throw new IOException($"Unable to read file with test data: {bzzFile}");
                return buffer;
            }
        }

        public static string OriginalBzzTestString
        {
            get
            {
                string txtFile = Path.Combine(Util.RepoRoot, "artifacts", "data", "testbzz.obz");
                byte[] buffer = ReadFileToEnd(txtFile);
                // Skip BOM
                return Encoding.UTF8.GetString(buffer, 3, buffer.Length - 3);
            }
        }

        [Fact]
        public void DjvuReaderTest001()
        {
            using (MemoryStream stream = new MemoryStream())
            using (DjvuReader reader = new DjvuReader(stream))
            {
                Assert.NotNull(reader);
                Assert.IsType<MemoryStream>(reader.BaseStream);
            }
        }

        [Fact()]
        public void DjvuReaderTest002()
        {
            using (DjvuReader reader = new DjvuReader(Util.GetTestFilePath(2)))
            {
                Assert.NotNull(reader);
                Assert.IsType<FileStream>(reader.BaseStream);
                FileStream fs = reader.BaseStream as FileStream;
                Assert.True(fs.CanRead && fs.CanSeek && !fs.CanWrite);
            }
        }

        [Fact()]
        public void DjvuReaderTest003()
        {
            Uri filePath = new Uri(Util.GetTestFilePath(2));
            using (DjvuReader reader = new DjvuReader(filePath))
            {
                Assert.NotNull(reader);
                // Verify that BaseStream is System.Net.FileWebStream
                FileStream fs = reader.BaseStream as FileStream;
                Assert.NotNull(fs);
                Assert.IsNotType<FileStream>(reader.BaseStream);
                Assert.True(fs.CanRead && fs.CanSeek && !fs.CanWrite);
                Assert.Equal<String>("System.Net.FileWebStream", fs.GetType().FullName);
            }
        }

        [Fact()]
        public void DjvuReaderTest004()
        {
            Uri filePath = new Uri("https://upload.wikimedia.org/wikipedia/commons/6/69/Tain_Bo_Cuailnge.djvu");
            using (DjvuReader reader = new DjvuReader(filePath))
            {
                Assert.NotNull(reader);
                Assert.NotNull(reader.BaseStream);
                MemoryStream s = reader.BaseStream as MemoryStream;
                Assert.True(s.CanRead && s.CanSeek && !s.CanWrite);
                Assert.IsType<MemoryStream>(s);
            }
        }

        [Fact()]
        public void DjvuReaderTest005()
        {
            Assert.Throws<WebException>(() =>
           {
               Uri filePath = new Uri("https://upload.wikimedia.org/wikipedia/commons/6/69/No_such_file.djvu");
               using (DjvuReader reader = new DjvuReader(filePath)) { }
           });
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetJPEGImageTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void GetFixedLengthStreamTest()
        {
            using(DjvuReader reader = new DjvuReader(Util.GetTestFilePath(1)))
            {
                long length = 4096;
                DjvuReader rf = reader.GetFixedLengthStream(length);
                Assert.NotNull(rf);
                Assert.Equal<long>(length, rf.BaseStream.Length);
                Assert.False(rf.BaseStream.CanWrite);
                Assert.IsType<MemoryStream>(rf.BaseStream);
            }
        }

        [Fact]
        public void ReadStringBytesTest001()
        {
            byte[] bzzBuffer = BzzCompressedTestBuffer;
            using (MemoryStream memStream = new MemoryStream(BzzCompressedTestBuffer, false))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                byte[] origBuffer = OriginalBzzTestBuffer;
                BzzReader bzReader = reader.GetBZZEncodedReader();
                Encoding enc = null;
                int length = 0;
                byte[] buffer = bzReader.ReadStringBytes(out enc, out length, false).GetBuffer();

                for (int i = 0; i < origBuffer.Length; i++)
                {
                    if (buffer[i] != origBuffer[i])
                        Assert.True(false, $"Buffer mismatch at position {i}, expected byte:" +
                            $" (0x{origBuffer[i]:x})," +
                            $" actual byte (0x{buffer[i]:x})");
                }
            }
        }

        [Fact]
        public void ReadStringBytesTest002()
        {
            byte[] bzzBuffer = BzzCompressedTestBuffer;
            using (MemoryStream memStream = new MemoryStream(BzzCompressedTestBuffer, false))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                byte[] origBuffer = OriginalBzzTestBuffer;
                BzzReader bzReader = reader.GetBZZEncodedReader();
                Encoding enc = null;
                byte[] buffer = null;
                int bufferLength = 0;
                using (MemoryStream stream = bzReader.ReadStringBytes(out enc, out bufferLength, false))
                {
                    byte[] srcBuffer = stream.GetBuffer();
                    buffer = new byte[bufferLength];
                    Buffer.BlockCopy(srcBuffer, 0, buffer, 0, bufferLength);
                }

                Assert.Equal<int>(origBuffer.Length, buffer.Length);
            }
        }

        [Fact()]
        public void CheckEncodingSignatureTest001()
        {
            // Encoding Scheme Signature
            // UTF-8                    EF BB BF
            byte[] buffer = new byte[] { 0xEF, 0xBB, 0xBF, 0x42, 0x43, 0x44, 0x45, 0x46 };
            int bufferLength = buffer.Length;
            int bomLength = 3;
            using (MemoryStream stream = new MemoryStream())
            {
                Encoding result = DjvuReader.CheckEncodingSignature(buffer, stream, ref bufferLength);
                Assert.NotNull(result);
                Assert.IsType<UTF8Encoding>(result);

                byte[] bom = ((UTF8Encoding)result).GetPreamble();
                Assert.NotNull(bom);
                Assert.Equal<int>(0, bom.Length);

                Assert.Equal<long>(buffer.Length - bomLength, stream.Position);
                Assert.Equal<int>(buffer.Length - bomLength, bufferLength);
            }
        }

        [Fact()]
        public void CheckEncodingSignatureTest002()
        {
            // Encoding Scheme Signature
            // UTF-16 Big-endian        FE FF
            byte[] buffer = new byte[] { 0xFE, 0xFF, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46 };
            int bufferLength = buffer.Length;
            int bomLength = 2;
            using (MemoryStream stream = new MemoryStream())
            {
                Encoding result = DjvuReader.CheckEncodingSignature(buffer, stream, ref bufferLength);
                Assert.NotNull(result);

                var resultU = result as UnicodeEncoding;
                Assert.NotNull(result);
                Assert.True(resultU.EncodingName.Contains("Big-Endian"));

                byte[] bom = resultU.GetPreamble();
                Assert.NotNull(bom);
                Assert.Equal<int>(0, bom.Length);

                Assert.Equal<long>(buffer.Length - bomLength, stream.Position);
                Assert.Equal<int>(buffer.Length - bomLength, bufferLength);
            }
        }

        [Fact()]
        public void CheckEncodingSignatureTest003()
        {
            // Encoding Scheme Signature
            // UTF-16 Little-endian     FF FE
            byte[] buffer = new byte[] { 0xFF, 0xFE, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46 };
            int bufferLength = buffer.Length;
            int bomLength = 2;
            using (MemoryStream stream = new MemoryStream())
            {
                Encoding result = DjvuReader.CheckEncodingSignature(buffer, stream, ref bufferLength);
                Assert.NotNull(result);

                var resultU = result as UnicodeEncoding;
                Assert.NotNull(result);
                Assert.False(resultU.EncodingName.Contains("Big-Endian"));

                byte[] bom = resultU.GetPreamble();
                Assert.NotNull(bom);
                Assert.Equal<int>(0, bom.Length);

                Assert.Equal<long>(buffer.Length - bomLength, stream.Position);
                Assert.Equal<int>(buffer.Length - bomLength, bufferLength);
            }
        }

        [Fact()]
        public void CheckEncodingSignatureTest004()
        {
            // Encoding Scheme Signature
            // UTF-32 Big-endian        00 00 FE FF
            byte[] buffer = new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x43, 0x44, 0x45, 0x46 };
            int bufferLength = buffer.Length;
            int bomLength = 4;
            using (MemoryStream stream = new MemoryStream())
            {
                Encoding result = DjvuReader.CheckEncodingSignature(buffer, stream, ref bufferLength);
                Assert.NotNull(result);

                var resultU = result as UTF32Encoding;
                Assert.NotNull(result);
                Assert.True(resultU.EncodingName.Contains("Big-Endian"));

                byte[] bom = resultU.GetPreamble();
                Assert.NotNull(bom);
                Assert.Equal<int>(0, bom.Length);

                Assert.Equal<long>(buffer.Length - bomLength, stream.Position);
                Assert.Equal<int>(buffer.Length - bomLength, bufferLength);
            }
        }

        [Fact()]
        public void CheckEncodingSignatureTest005()
        {
            // Encoding Scheme Signature
            // UTF-32 Little-endian     FF FE 00 00
            byte[] buffer = new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x43, 0x44, 0x45, 0x46 };
            int bufferLength = buffer.Length;
            int bomLength = 4;
            using (MemoryStream stream = new MemoryStream())
            {
                Encoding result = DjvuReader.CheckEncodingSignature(buffer, stream, ref bufferLength);
                Assert.NotNull(result);

                var resultU = result as UTF32Encoding;
                Assert.NotNull(result);
                Assert.False(resultU.EncodingName.Contains("Big-Endian"));

                byte[] bom = resultU.GetPreamble();
                Assert.NotNull(bom);
                Assert.Equal<int>(0, bom.Length);

                Assert.Equal<long>(buffer.Length - bomLength, stream.Position);
                Assert.Equal<int>(buffer.Length - bomLength, bufferLength);
            }
        }

        [Fact()]
        public void CheckEncodingSignatureTest006()
        {
            // Encoding Scheme Signature
            // UTF-32 Little-endian     FF FE 00 00
            int bufferLength = 5;
            using (MemoryStream stream = new MemoryStream())
            {
                Assert.Throws<ArgumentNullException>("buffer",
                    () => DjvuReader.CheckEncodingSignature(null, stream, ref bufferLength));
            }
        }

        [Fact()]
        public void CheckEncodingSignatureTest007()
        {
            // Encoding Scheme Signature
            // UTF-32 Little-endian     FF FE 00 00
            byte[] buffer = new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x43, 0x44, 0x45, 0x46 };
            int bufferLength = buffer.Length;
            using (MemoryStream stream = new MemoryStream())
            {
                Assert.Throws<ArgumentNullException>("stream",
                    () => DjvuReader.CheckEncodingSignature(buffer, null, ref bufferLength));
            }
        }

        [Fact()]
        public void CheckEncodingSignatureTest008()
        {
            // Encoding Scheme Signature
            // UTF-32 Little-endian     FF FE 00 00
            byte[] buffer = new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x43, 0x44, 0x45, 0x46 };
            int bufferLength = 3;
            using (MemoryStream stream = new MemoryStream())
            {
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => DjvuReader.CheckEncodingSignature(buffer, stream, ref bufferLength));
            }
        }

        [Fact()]
        public void CheckEncodingSignatureTest009()
        {
            // No Encoding Scheme Signature
            byte[] buffer = new byte[] { 0x39, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46 };
            int bufferLength = buffer.Length;
            using (MemoryStream stream = new MemoryStream())
            {
                Encoding result = DjvuReader.CheckEncodingSignature(buffer, stream, ref bufferLength);
                Assert.Null(result);

                Assert.Equal<long>(0, stream.Position);
                Assert.Equal<int>(buffer.Length, bufferLength);
            }
        }

        [Fact()]
        public void GetBZZEncodedReaderTest001()
        {
            byte[] bzzBuffer = BzzCompressedTestBuffer;
            using (MemoryStream memStream = new MemoryStream(bzzBuffer, false))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                BzzReader bzzReader = reader.GetBZZEncodedReader();
                Assert.NotNull(bzzReader);
                Assert.IsType<BzzReader>(bzzReader);
            }
        }

        [Fact()]
        public void GetBZZEncodedReaderTest002()
        {
            string bzzFile = Path.Combine(Util.RepoRoot, "artifacts", "data", "testbzz.bz");
            using (DjvuReader reader = new DjvuReader(bzzFile))
            {
                BzzReader bzzReader = reader.GetBZZEncodedReader();
                Assert.NotNull(bzzReader);
                Assert.IsType<BzzReader>(bzzReader);
                Assert.IsType<BSInputStream>(bzzReader.BaseStream);
                BSInputStream bsStream = bzzReader.BaseStream as BSInputStream;
                Assert.NotNull(bsStream.ZpCoder?.InputStream);
                Assert.IsType<FileStream>(bsStream.ZpCoder.InputStream);
            }
        }

        [Fact()]
        public void GetBZZEncodedReaderTest003()
        {
            byte[] bzzBuffer = BzzCompressedTestBuffer;
            using (MemoryStream memStream = new MemoryStream(bzzBuffer, false))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                BzzReader bzzReader = reader.GetBZZEncodedReader();
                Assert.NotNull(bzzReader);
                Assert.IsType<BzzReader>(bzzReader);
                Assert.IsType<BSInputStream>(bzzReader.BaseStream);
                BSInputStream bsStream = bzzReader.BaseStream as BSInputStream;
                Assert.NotNull(bsStream.ZpCoder?.InputStream);
                Assert.IsType<MemoryStream>(bsStream.ZpCoder.InputStream);
            }
        }

        [Fact()]
        public void GetBZZEncodedReaderTest004()
        {
            byte[] bzzBuffer = BzzCompressedTestBuffer;
            using (MemoryStream memStream = new MemoryStream(bzzBuffer, false))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                BzzReader bzzReader = reader.GetBZZEncodedReader(1024);
                Assert.NotNull(bzzReader);
                Assert.IsType<BzzReader>(bzzReader);
                Assert.IsType<BSInputStream>(bzzReader.BaseStream);
            }
        }

        [Fact()]
        public void GetBZZEncodedReaderTest005()
        {
            byte[] bzzBuffer = BzzCompressedTestBuffer;
            using (MemoryStream memStream = new MemoryStream(bzzBuffer, false))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                BzzReader bzzReader = reader.GetBZZEncodedReader(1024);
                Assert.NotNull(bzzReader);
                Assert.IsType<BzzReader>(bzzReader);
                Assert.IsType<BSInputStream>(bzzReader.BaseStream);
                BSInputStream bsStream = bzzReader.BaseStream as BSInputStream;
                Assert.NotNull(bsStream.ZpCoder?.InputStream);
                Assert.IsType<MemoryStream>(bsStream.ZpCoder.InputStream);
            }
        }


        [Fact()]
        public void ReadUInt24BigEndianTest001()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                uint result = reader.ReadUInt24BigEndian();
                Assert.Equal<uint>(0x0080ff00, result);
                Assert.Equal<long>(3, reader.Position);
            }
        }

        [Fact()]
        public void ReadUInt24BigEndianTest002()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int advanceReader = 2;
                reader.Position = advanceReader;
                uint result = reader.ReadUInt24BigEndian();
                Assert.Equal<uint>(0x000000ff, result);
                Assert.Equal<long>(3 + advanceReader, reader.Position);
            }
        }

        [Fact()]
        public void ReadInt24BigEndianTest001()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int result = reader.ReadInt24BigEndian();
                Assert.Equal<int>(unchecked((int)0xff80ff00), result);
                Assert.Equal<long>(3, reader.Position);
            }
        }

        [Fact()]
        public void ReadInt24BigEndianTest002()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int advanceReader = 2;
                reader.Position = advanceReader;
                int result = reader.ReadInt24BigEndian();
                Assert.Equal<int>(0x000000ff, result);
                Assert.Equal<long>(3 + advanceReader, reader.Position);
            }
        }

        [Fact()]
        public void ReadInt24BigEndianTest003()
        {
            byte[] buffer = new byte[] { 0xff, 0x00, 0xff, 0xff, 0xff, 0x01, 0x80, 0x01 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int advanceReader = 2;
                reader.Position = advanceReader;
                int result = reader.ReadInt24BigEndian();
                Assert.Equal<int>(-1, result);
                Assert.Equal<long>(3 + advanceReader, reader.Position);
            }
        }

        [Fact()]
        public void ReadInt24BigEndianTest004()
        {
            byte[] buffer = new byte[] { 0xff, 0x00, 0x80, 0x00, 0xff, 0x01, 0x80, 0x01 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int advanceReader = 2;
                reader.Position = advanceReader;
                int result = reader.ReadInt24BigEndian();
                Assert.Equal<int>(unchecked((int)0xff8000ff), result);
                Assert.Equal<long>(3 + advanceReader, reader.Position);
            }
        }

        [Fact()]
        public void ReadUInt24Test001()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int advanceReader = 2;
                reader.Position = advanceReader;
                uint result = reader.ReadUInt24();
                Assert.Equal<uint>(0x00ff0000, result);
                Assert.Equal<long>(3 + advanceReader, reader.Position);
            }
        }

        [Fact()]
        public void ReadUInt24Test002()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int advanceReader = 2;
                reader.Position = advanceReader;
                uint result = reader.ReadUInt24();
                Assert.Equal<uint>(0x00ff0000, result);
                Assert.Equal<long>(3 + advanceReader, reader.Position);
            }
        }

        [Fact()]
        public void ReadInt24Test001()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int result = reader.ReadInt24();
                int expected = BitConverter.ToInt32(buffer, 0);
                Assert.Equal<int>(expected, result);
                Assert.Equal<long>(3, reader.Position);
            }
        }

        [Fact()]
        public void ReadInt24Test002()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int advanceReader = 2;
                reader.Position = advanceReader;
                buffer[5] = 0x00;
                int result = reader.ReadInt24();
                int expected = BitConverter.ToInt32(buffer, 2);
                Assert.Equal<int>(expected, result);
                Assert.Equal<long>(3 + advanceReader, reader.Position);
            }
        }

        [Fact()]
        public void ReadInt16BigEndianTest001()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01, 0x00 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                short result = reader.ReadInt16BigEndian();
                short expected = BitConverter.ToInt16(new byte[] { 0xff, 0x80 }, 0);
                Assert.Equal<short>(expected, result);
                Assert.Equal<long>(sizeof(short), reader.Position);
            }
        }

        [Fact()]
        public void ReadInt16BigEndianTest002()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01, 0x00 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int advanceReader = 7;
                reader.Position = advanceReader;
                int result = reader.ReadInt16BigEndian();
                Assert.Equal<int>(0x0100, result);
                Assert.Equal<long>(sizeof(short) + advanceReader, reader.Position);
            }
        }

        [Fact()]
        public void ReadInt32BigEndianTest001()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01, 0x00 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int result = reader.ReadInt32BigEndian();
                int expected = BitConverter.ToInt32(new byte[] { 0x00, 0x00, 0xff, 0x80 }, 0);
                Assert.Equal<int>(expected, result);
                Assert.Equal<long>(sizeof(int), reader.Position);
            }
        }

        [Fact()]
        public void ReadInt32BigEndianTest002()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01, 0x00 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int advanceReader = 2;
                reader.Position = advanceReader;
                int result = reader.ReadInt32BigEndian();
                int expected = BitConverter.ToInt32(new byte[] { 0x01, 0xff, 0x00, 0x00 }, 0);
                Assert.Equal<int>(expected, result);
                Assert.Equal<long>(sizeof(int) + advanceReader, reader.Position);
            }
        }

        [Fact()]
        public void ReadInt64BigEndianTest001()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01, 0x00, 0x80 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                long result = reader.ReadInt64BigEndian();
                long expected = BitConverter.ToInt64(new byte[] { 0x01, 0x80, 0x01, 0xff, 0x00, 0x00, 0xff, 0x80 }, 0);
                Assert.Equal<long>(expected, result);
                Assert.Equal<long>(sizeof(long), reader.Position);
            }
        }

        [Fact()]
        public void ReadInt64BigEndianTest002()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01, 0x00, 0x80 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int advanceReader = 2;
                reader.Position = advanceReader;
                long result = reader.ReadInt64BigEndian();
                long expected = BitConverter.ToInt64(
                    new byte[] { 0x80, 0x00, 0x01, 0x80, 0x01, 0xff, 0x00, 0x00, }, 0);
                Assert.Equal<long>(expected, result);
                Assert.Equal<long>(sizeof(long) + advanceReader, reader.Position);
            }
        }

        [Fact()]
        public void ReadUInt16BigEndianTest001()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01, 0x00 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                ushort result = reader.ReadUInt16BigEndian();
                ushort expected = BitConverter.ToUInt16(new byte[] { 0xff, 0x80 }, 0);
                Assert.Equal<ushort>(expected, result);
                Assert.Equal<long>(sizeof(ushort), reader.Position);
            }
        }

        [Fact()]
        public void ReadUInt16BigEndianTest002()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01, 0x00 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int advanceReader = 4;
                reader.Position = advanceReader;
                ushort result = reader.ReadUInt16BigEndian();
                ushort expected = BitConverter.ToUInt16(new byte[] { 0x01, 0xff }, 0);
                Assert.Equal<ushort>(expected, result);
                Assert.Equal<long>(sizeof(ushort) + advanceReader, reader.Position);
            }
        }

        [Fact()]
        public void ReadUInt32BigEndianTest001()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01, 0x00 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                uint result = reader.ReadUInt32BigEndian();
                uint expected = BitConverter.ToUInt32(new byte[] { 0x00, 0x00, 0xff, 0x80 }, 0);
                Assert.Equal<uint>(expected, result);
                Assert.Equal<long>(sizeof(uint), reader.Position);
            }
        }

        [Fact()]
        public void ReadUInt32BigEndianTest002()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01, 0x00 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int advanceReader = 2;
                reader.Position = advanceReader;
                uint result = reader.ReadUInt32BigEndian();
                uint expected = BitConverter.ToUInt32(new byte[] { 0x01, 0xff, 0x00, 0x00 }, 0);
                Assert.Equal<uint>(expected, result);
                Assert.Equal<long>(sizeof(uint) + advanceReader, reader.Position);
            }
        }

        [Fact()]
        public void ReadUInt64BigEndianTest001()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01, 0x00, 0x80 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                ulong result = reader.ReadUInt64BigEndian();
                ulong expected = BitConverter.ToUInt64(new byte[] { 0x01, 0x80, 0x01, 0xff, 0x00, 0x00, 0xff, 0x80 }, 0);
                Assert.Equal<ulong>(expected, result);
                Assert.Equal<long>(sizeof(ulong), reader.Position);
            }
        }

        [Fact()]
        public void ReadUInt64BigEndianTest002()
        {
            byte[] buffer = new byte[] { 0x80, 0xff, 0x00, 0x00, 0xff, 0x01, 0x80, 0x01, 0x00, 0x80 };
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                Assert.Equal<long>(0, reader.Position);
                Assert.Equal<long>(buffer.Length, reader.BaseStream.Length);
                int advanceReader = 2;
                reader.Position = advanceReader;
                ulong result = reader.ReadUInt64BigEndian();
                ulong expected = BitConverter.ToUInt64(
                    new byte[] { 0x80, 0x00, 0x01, 0x80, 0x01, 0xff, 0x00, 0x00, }, 0);
                Assert.Equal<ulong>(expected, result);
                Assert.Equal<long>(sizeof(ulong) + advanceReader, reader.Position);
            }
        }

        [Fact()]
        public void ReadUTF8StringTest()
        {
            string expected = "This is a test text: łążźćń Vor nicht einmal fünf 百度目前是全球最大的中文搜索引擎，2000年1月创立于北京中关村。百度的使命是让人们最便捷地获取信";
            UTF8Encoding utf8 = new UTF8Encoding(false);
            byte[] buffer = utf8.GetBytes(expected);
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                string result = reader.ReadUTF8String(buffer.Length);
                Assert.False(String.IsNullOrWhiteSpace(result));
                Assert.Equal(0, String.CompareOrdinal(expected, result));
            }

        }

        [Fact()]
        public void ReadUTF7StringTest()
        {
            string expected = "This is a test text: łążźćń Vor nicht einmal fünf 百度目前是全球最大的中文搜索引擎，2000年1月创立于北京中关村。百度的使命是让人们最便捷地获取信";
            UTF7Encoding utf7 = new UTF7Encoding(false);
            byte[] buffer = utf7.GetBytes(expected);
            using (MemoryStream memStream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                string result = reader.ReadUTF7String(buffer.Length);
                Assert.False(String.IsNullOrWhiteSpace(result));
                Assert.Equal(0, String.CompareOrdinal(expected, result));
            }
        }

        [Fact()]
        public void ReadUnknownLengthStringTest001()
        {
            byte[] bzzBuffer = BzzCompressedTestBuffer;
            using (MemoryStream memStream = new MemoryStream(BzzCompressedTestBuffer, false))
            using (DjvuReader reader = new DjvuReader(memStream))
            {
                BzzReader bzzReader = reader.GetBZZEncodedReader();
                String result = bzzReader.ReadUnknownLengthString();

                Assert.NotNull(result);
                Assert.False(String.IsNullOrWhiteSpace(result));
                Assert.True(result.StartsWith("========================================================================================="), "String.StartsWith");
                Assert.True(result.Contains("Test start - bzz format encoding"));
                Assert.True(result.Contains("described in ITU-T Recommendation T.44, ISO/IEC 16485. In this model, an image is"));
                Assert.True(result.Contains("Test end bzz format encoding"));
                string expected = OriginalBzzTestString;

                for (int i = 0; i < expected.Length; i++)
                {
                    if (expected[i] != result[i])
                        Assert.True(false, $"String mismatch at character position {i}, expected character:" +
                            $" \"{expected[i]}\" (0x{(byte)expected[i]:x})," +
                            $" actual character \"{result[i]}\" (0x{(byte)result[i]:x})");
                }
            }
        }

        [Fact()]
        public void ReadUnknownLengthStringTest002()
        {
            string bzzFile = Path.Combine(Util.RepoRoot, "artifacts", "data", "testbzz.bz");
            using (DjvuReader reader = new DjvuReader(bzzFile))
            {
                BzzReader bzzReader = reader.GetBZZEncodedReader();
                string result = bzzReader.ReadUnknownLengthString();

                Assert.NotNull(result);
                Assert.False(String.IsNullOrWhiteSpace(result));
                Assert.True(result.Contains("described in ITU-T Recommendation T.44, ISO/IEC 16485. In this model, an image is"));

                string expected = OriginalBzzTestString;
                Assert.NotNull(expected);
                Assert.False(String.IsNullOrWhiteSpace(expected));

                Assert.Equal<string>(expected, result);
            }
        }

        [Fact()]
        public void ReadUnknownLengthStringTest003()
        {
            string bzzFile = Path.Combine(Util.RepoRoot, "artifacts", "data", "testbzz.bz");
            string origTextFile = Path.Combine(Util.RepoRoot, "artifacts", "data", "testbzz.obz");
            using (DjvuReader reader = new DjvuReader(bzzFile))
            {
                BzzReader bzzReader = reader.GetBZZEncodedReader();
                string result = bzzReader.ReadUnknownLengthString();

                Assert.False(String.IsNullOrWhiteSpace(result));

                string expectedResult = OriginalBzzTestString;
                Assert.False(String.IsNullOrWhiteSpace(expectedResult));

                Assert.Equal<int>(expectedResult.Length, result.Length);
            }
        }

        [Fact()]
        public void CloneReaderTest()
        {
            using (DjvuReader reader = new DjvuReader(Util.GetTestFilePath(1)))
            {
                var reader2 = reader.CloneReader(reader.BaseStream.Length / 2);
                Assert.NotNull(reader2);
                Assert.NotSame(reader, reader2);
                Assert.Equal<long>(reader.BaseStream.Length / 2, reader2.BaseStream.Position);
            }
        }

        [Fact()]
        public void CloneReaderTest1()
        {
            using (DjvuReader reader = new DjvuReader(Util.GetTestFilePath(1)))
            {
                long length = 4096;
                var reader2 = reader.CloneReader(reader.BaseStream.Length / 2, length);
                Assert.NotNull(reader2);
                Assert.NotSame(reader, reader2);
                Assert.Equal<long>(reader.BaseStream.Position / 2, reader2.BaseStream.Position);
                Assert.Equal<long>(length, reader2.BaseStream.Length);
            }
        }

        [Fact()]
        public void ToStringTest()
        {
            int length = 1024 * 1024;
            using (MemoryStream stream = new MemoryStream(new byte[length]))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                string result = reader.ToString();
                Assert.NotNull(result);
                Assert.True(result.Contains("DjvuReader"));
                Assert.True(result.Contains("MemoryStream"));
                Assert.True(result.Contains("Position: 0x0"));
                Assert.True(result.Contains("Length: 0x100000"));
            }
        }
    }
}