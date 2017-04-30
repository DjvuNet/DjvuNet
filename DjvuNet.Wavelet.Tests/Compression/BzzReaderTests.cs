using Xunit;
using DjvuNet.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DjvuNet.Tests;

namespace DjvuNet.Compression.Tests
{
    public class BzzReaderTests
    {
        [Fact()]
        public void ReadUTF8String()
        {
            string testText = "Hello bzz! \r\n";

            string filePath = Path.Combine(Util.ArtifactsDataPath, "testhello.bzz");
            byte[] testBuffer = Util.ReadFileToEnd(filePath);

            using (MemoryStream readStream = new MemoryStream(testBuffer))
            using (BzzReader reader = new BzzReader(new BSInputStream(readStream)))
            {

                string testResult = reader.ReadUTF8String(13);
                Assert.False(String.IsNullOrWhiteSpace(testResult));
                Assert.Equal(testText, testResult);
            }
        }

        [Fact()]
        public void BzzReaderTest001()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BzzReader reader = new BzzReader(stream))
            {
                Assert.NotSame(stream, reader.BaseStream);
                Assert.IsType<BSInputStream>(reader.BaseStream);
            }
        }

        [Fact()]
        public void BzzReaderTest002()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BSInputStream inputStream = new BSInputStream(stream))
            using (BzzReader reader = new BzzReader(inputStream))
            {
                Assert.NotSame(stream, reader.BaseStream);
                Assert.IsType<BSInputStream>(reader.BaseStream);
                Assert.Same(inputStream, reader.BaseStream);
            }
        }

        [Fact()]
        public void ReadNullTerminatedStringTest()
        {
            string filePath = Path.Combine(Util.ArtifactsDataPath, "testbzznbmont.bz");
            string testPath = Path.Combine(Util.ArtifactsDataPath, "testbzznbmont.obz");
            byte[] sourceBuffer = Util.ReadFileToEnd(filePath);
            byte[] testBuffer = Util.ReadFileToEnd(testPath);
            using (MemoryStream readStream = new MemoryStream(sourceBuffer))
            using (BzzReader reader = new BzzReader(new BSInputStream(readStream)))
            {
                string expected = new UTF8Encoding(false).GetString(testBuffer, 0, testBuffer.Length - 1);
                string testResult = reader.ReadNullTerminatedString();
                Assert.False(String.IsNullOrWhiteSpace(testResult));
                Assert.Equal<int>(expected.Length, testResult.Length);
                Assert.Equal(expected, testResult);
            }
        }
    }
}