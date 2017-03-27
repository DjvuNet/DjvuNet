using Xunit;
using DjvuNet;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public void DjvuReaderTest()
        {
            using (MemoryStream stream = new MemoryStream())
            using (DjvuReader reader = new DjvuReader(stream))
            {
                Assert.NotNull(reader);
                Assert.IsType<MemoryStream>(reader.BaseStream);
            }
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void DjvuReaderTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetJPEGImageTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetFixedLengthStreamTest()
        {
            Assert.True(false, "This test needs an implementation");
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

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetBZZEncodedReaderTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadNullTerminatedStringTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadUInt24MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadInt24MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadUInt24Test()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadInt24Test()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadInt16MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadInt32MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadInt64MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadUInt16MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadUInt32MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadUInt64MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadUTF8StringTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadUTF7StringTest()
        {
            Assert.True(false, "This test needs an implementation");
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

                Assert.NotNull(result);
                Assert.False(String.IsNullOrWhiteSpace(result));

                string expectedResult = OriginalBzzTestString;
                Assert.NotNull(expectedResult);
                Assert.False(String.IsNullOrWhiteSpace(expectedResult));

                Assert.Equal<int>(expectedResult.Length, result.Length);
            }
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void CloneReaderTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void CloneReaderTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ToStringTest()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}