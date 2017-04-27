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
                Assert.NotNull(codec.DataStream);
            }
        }

        [Fact()]
        public void ZPCodecTest003()
        {
            ZPCodec codec = new ZPCodec();
            Assert.True(codec.DjvuCompat);
            Assert.False(codec.Encoding);
        }

        [Fact()]
        public void ZPCodecTest004()
        {
            ZPCodec codec = new ZPCodec();
            Assert.True(codec.DjvuCompat);
            Assert.False(codec.Encoding);
            ZPTable[] table = codec.CreateDefaultTable();
            for(int i = 0; i < table.Length; i++)
            {
                var row = table[i];
                Assert.Equal<uint>(row.PValue, codec.PArray[i]);
                Assert.Equal<uint>(row.MValue, codec.MArray[i]);
                Assert.Equal<uint>(row.Down, codec.Down[i]);
                Assert.Equal<uint>(row.Up, codec.Up[i]);
            }
        }

        [Fact()]
        public void ZPCodecTest005()
        {
            ZPCodec codec = new ZPCodec(null, true);
            Assert.True(codec.DjvuCompat);
            Assert.True(codec.Encoding);
        }

        [Fact()]
        public void ZPCodecTest006()
        {
            ZPCodec codec = new ZPCodec(null, true, false);
            Assert.False(codec.DjvuCompat);
            Assert.True(codec.Encoding);
        }

        [Fact()]
        public void ZPCodecTest007()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ZPCodec codec = new ZPCodec(stream, true, false);
                Assert.NotNull(codec.DataStream);
                Assert.Same(stream, codec.DataStream);
                Assert.False(codec.DjvuCompat);
                Assert.True(codec.Encoding);
            }
        }

        [Fact()]
        public void ZPCodecTest008()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ZPCodec codec = new ZPCodec(stream, false, false);
                Assert.NotNull(codec.DataStream);
                Assert.Same(stream, codec.DataStream);
                Assert.False(codec.DjvuCompat);
                Assert.False(codec.Encoding);
            }
        }

        [Fact()]
        public void DefaultTable001()
        {
            ZPCodec codec = new ZPCodec();
            Assert.True(codec.DjvuCompat);
            Assert.False(codec.Encoding);
            ZPTable[] table = codec.CreateDefaultTable();
            ZPTable[] defaultTable = codec.DefaultTable;
            Assert.NotSame(table, codec.DefaultTable);
            Assert.Same(defaultTable, codec.DefaultTable);
            for (int i = 0; i < table.Length; i++)
            {
                var row = table[i];
                var defRow = defaultTable[i];
                Assert.Equal<uint>(row.PValue, defRow.PValue);
                Assert.Equal<uint>(row.MValue, defRow.MValue);
                Assert.Equal<uint>(row.Down, defRow.Down);
                Assert.Equal<uint>(row.Up, defRow.Up);
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