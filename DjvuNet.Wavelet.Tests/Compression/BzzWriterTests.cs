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
    public class BzzWriterTests
    {
        [Fact(Skip = "Blocking CI"), Trait("Category", "Skip")]
        public void BzzWriterTest()
        {
            UTF8Encoding encoding = new UTF8Encoding(false);
            string filePath = Path.Combine(Util.RepoRoot, "artifacts", "data", "testbzznbmont.obz");
            string testText = File.ReadAllText(filePath, encoding);
            using(MemoryStream stream = new MemoryStream())
            using (BzzWriter writer = new BzzWriter(stream))
            {
                byte[] buffer = encoding.GetBytes(testText);
                writer.Write(buffer, 0, buffer.Length);
                writer.Flush();

                long bytesWritten = stream.Position;
                stream.Position = 0;
                byte[] testBuffer = stream.GetBuffer();
                byte[] readBuffer = new byte[bytesWritten + 32];
                Buffer.BlockCopy(testBuffer, 0, readBuffer, 0, (int) bytesWritten);

                using(MemoryStream readStream = new MemoryStream(readBuffer))
                using(DjvuReader reader = new DjvuReader(readStream))
                {
                    string testResult = reader.ReadUnknownLengthString(false);
                    Assert.False(String.IsNullOrWhiteSpace(testResult));
                    Assert.Equal(testText, testResult);
                }
            }
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void BzzWriterTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void BzzWriterTest2()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}