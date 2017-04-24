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
    public class ZPCodecTests
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

        [Fact()]
        public void ZPCodecTest001()
        {
            ZPCodec codec = new ZPCodec();
            Assert.NotNull(codec.FFZT);
            Assert.Equal<int>(256, codec.FFZT.Length);
        }

        [Fact()]
        public void ZPCodecTest002()
        {
            byte[] buffer = BzzCompressedTestBuffer;
            using(MemoryStream stream = new MemoryStream(buffer, false))
            {
                ZPCodec codec = new ZPCodec(stream);
                Assert.NotNull(codec.InputStream);
            }
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void IWDecoderTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void DecodeSubTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void DecodeSubNolearnTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void DecodeSubSimpleTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void DecoderTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void DecoderTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void FFZTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void InitTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void NewZPTableTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void PreloadTest()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}