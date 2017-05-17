using Xunit;
using DjvuNet.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DjvuNet.Tests;
using DjvuNet.Tests.Mocks;

namespace DjvuNet.Compression.Tests
{
    public class BzzWriterTests
    {
        [Fact(Skip = "Bug in BzzWriter"), Trait("Category", "Skip")]
        [Trait("Category", "Bugtrack")]
        public void BzzWriterTest001()
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
                using (BzzReader reader = new BzzReader(new BSInputStream(readStream)))
                {

                    string testResult = reader.ReadUTF8String(buffer.Length);
                    Assert.False(String.IsNullOrWhiteSpace(testResult));
                    // TODO track bug causing roundtrip errors
                    // Assert.Equal(testText, testResult);
                }
            }
        }

        [Fact(Skip = "Bug in BzzWriter"), Trait("Category", "Skip")]
        [Trait("Category", "Bugtrack")]
        public void BzzWriterTest002()
        {
            UTF8Encoding encoding = new UTF8Encoding(false);
            string filePath = Path.Combine(Util.RepoRoot, "artifacts", "data", "testbzz.obz");
            string testText = File.ReadAllText(filePath, encoding);
            using (Stream stream = File.Create(filePath.Replace(".obz", ".bz3"), 8192))
            using (BzzWriter writer = new BzzWriter(stream))
            {
                byte[] buffer = encoding.GetBytes(testText);
                writer.Write(buffer, 0, buffer.Length);
                writer.Flush();
            }
        }

        [Fact(Skip = "Bug in BzzWriter"), Trait("Category", "Skip")]
        [Trait("Category", "Bugtrack")]
        public void BzzWriterTest003()
        {
            UTF8Encoding encoding = new UTF8Encoding(false);
            string filePath = Path.Combine(Util.RepoRoot, "artifacts", "data", "testhello.obz");
            filePath = filePath.Replace(".obz", ".bz3");
            string testText = "Hello bzz! \r\n";
            long bytesWritten = 0;
            using (Stream stream = File.Create(filePath, 8192))
            using (BzzWriter writer = new BzzWriter(stream))
            {
                // TODO track BzzWriter bug causing side effects during write
                writer.Write(testText);
                writer.Flush();

                bytesWritten = stream.Position;
            }

            byte[] testBuffer = Util.ReadFileToEnd(filePath);
            //byte[] readBuffer = new byte[bytesWritten + 32];
            //Buffer.BlockCopy(testBuffer, 0, readBuffer, 0, (int)bytesWritten);

            using (MemoryStream readStream = new MemoryStream(testBuffer))
            using (BzzReader reader = new BzzReader(new BSInputStream(readStream)))
            {

                string testResult = reader.ReadUTF8String(testText.Length);
                Assert.False(String.IsNullOrWhiteSpace(testResult));
                // Assert.Equal(testText, testResult);
            }
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void BzzWriterTest004()
        {
 
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