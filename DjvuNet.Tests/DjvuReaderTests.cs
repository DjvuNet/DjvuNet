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
                using (StreamReader reader = new StreamReader(txtFile, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        [Fact(Skip = "Not implemented")]
        public void DjvuReaderTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void DjvuReaderTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void GetJPEGImageTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void GetFixedLengthStreamTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void GetBZZEncodedReaderTest001()
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

                for(int i = 0; i < expected.Length; i++)
                {
                    if (expected[i] != result[i])
                        Assert.True(false, $"String mismatch at character position {i}, expected character:" + 
                            $" \"{expected[i]}\" (0x{(byte)expected[i]:x})," + 
                            $" actual character \"{result[i]}\" (0x{(byte)result[i]:x})");
                }
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

        [Fact(Skip = "Not implemented")]
        public void CheckEncodingSignatureTest()
        {

        }

        [Fact()]
        public void GetBZZEncodedReaderTest002()
        {
            string bzzFile = Path.Combine(Util.RepoRoot, "artifacts", "data", "testbzz.bz");
            string origTextFile = Path.Combine(Util.RepoRoot, "artifacts", "data", "testbzz.obz");
            using (DjvuReader reader = new DjvuReader(bzzFile))
            {
                BzzReader bzzReader = reader.GetBZZEncodedReader();
                string result = bzzReader.ReadUnknownLengthString(false);

                Assert.NotNull(result);
                Assert.False(String.IsNullOrWhiteSpace(result));
                Assert.True(result.Contains("described in ITU-T Recommendation T.44, ISO/IEC 16485. In this model, an image is"));

                string expected = OriginalBzzTestString;
                Assert.NotNull(expected);
                Assert.False(String.IsNullOrWhiteSpace(expected));

                if (expected.Length > result.Length)
                {
                    if ((expected.Length - result.Length) == 1 && expected[expected.Length - 1] != '\0')
                        Assert.True(false, "String mismatch is not due to null terminating character.");
                    else if ((expected.Length - result.Length) == 1)
                    {
                        expected = expected.Substring(0, result.Length);
                    }
                }

                Assert.Equal<string>(expected, result);
            }

        }

        [Fact()]
        public void GetBZZEncodedReaderTest003()
        {
            string bzzFile = Path.Combine(Util.RepoRoot, "artifacts", "data", "testbzz.bz");
            string origTextFile = Path.Combine(Util.RepoRoot, "artifacts", "data", "testbzz.obz");
            using (DjvuReader reader = new DjvuReader(bzzFile))
            using (StreamReader reader2 = new StreamReader(origTextFile, Encoding.UTF8))
            {
                BzzReader bzzReader = reader.GetBZZEncodedReader();
                string result = bzzReader.ReadUnknownLengthString(false);

                Assert.NotNull(result);
                Assert.False(String.IsNullOrWhiteSpace(result));

                string expectedResult = reader2.ReadToEnd().Trim();
                Assert.NotNull(expectedResult);
                Assert.False(String.IsNullOrWhiteSpace(expectedResult));

                Assert.Equal<int>(expectedResult.Length, result.Length);
            }
        }

        [Fact(Skip = "Not implemented")]
        public void GetBZZEncodedReaderTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadNullTerminatedStringTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadUInt24MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadInt24MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadUInt24Test()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadInt24Test()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadInt16MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadInt32MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadInt64MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadUInt16MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadUInt32MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadUInt64MSBTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadUTF8StringTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadUTF7StringTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ReadUnknownLengthStringTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void CloneReaderTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void CloneReaderTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ToStringTest()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}