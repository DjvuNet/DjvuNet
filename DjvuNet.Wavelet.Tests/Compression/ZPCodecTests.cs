using Xunit;
using DjvuNet.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DjvuNet.Tests;
using System.Runtime.CompilerServices;

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
            using (ZPCodec codec = new ZPCodec())
            {
                Assert.NotNull(codec.FFZT);
                Assert.Equal<int>(256, codec.FFZT.Length);
            }
        }

        [Fact()]
        public void ZPCodecTest002()
        {
            byte[] buffer = BzzCompressedTestBuffer;
            using(MemoryStream stream = new MemoryStream(buffer, false))
            using (ZPCodec codec = new ZPCodec(stream))
            {
                Assert.NotNull(codec.DataStream);
            }
        }

        [Fact()]
        public void ZPCodecTest003()
        {
            using (ZPCodec codec = new ZPCodec())
            {
                Assert.True(codec.DjvuCompat);
                Assert.False(codec.Encoding);
            }
        }

        [Fact()]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ZPCodecTest004()
        {
            using (ZPCodec codec = new ZPCodec())
            {
                Assert.True(codec.DjvuCompat);
                Assert.False(codec.Encoding);
                ZPTable[] table = codec.CreateDefaultTable();
                for (int i = 0; i < table.Length; i++)
                {
                    ZPTable row = table[i];
                    Assert.Equal<uint>(row.PValue, codec._PArray[i]);
                    Assert.Equal<uint>(row.MValue, codec._MArray[i]);
                    Assert.Equal<uint>(row.Down, codec._Down[i]);
                    Assert.Equal<uint>(row.Up, codec._Up[i]);
                }
            }
        }

        [Fact()]
        public void ZPCodecTest005()
        {
            using (ZPCodec codec = new ZPCodec(null, true))
            {
                Assert.True(codec.DjvuCompat);
                Assert.True(codec.Encoding);
            }
        }

        [Fact()]
        public void ZPCodecTest006()
        {
            using (ZPCodec codec = new ZPCodec(null, true, false))
            {
                Assert.False(codec.DjvuCompat);
                Assert.True(codec.Encoding);
            }
        }

        [Fact()]
        public void ZPCodecTest007()
        {
            using (MemoryStream stream = new MemoryStream())
            using(ZPCodec codec = new ZPCodec(stream, true, false))
            {
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
            using (ZPCodec codec = new ZPCodec(stream, false, false))
            {
                Assert.NotNull(codec.DataStream);
                Assert.Same(stream, codec.DataStream);
                Assert.False(codec.DjvuCompat);
                Assert.False(codec.Encoding);
            }
        }

        [Fact()]
        public void DefaultTable001()
        {
            using (ZPCodec codec = new ZPCodec())
            {
                Assert.True(codec.DjvuCompat);
                Assert.False(codec.Encoding);
                ZPTable[] table = codec.CreateDefaultTable();
                ZPTable[] defaultTable = codec.DefaultTable;
                Assert.NotSame(table, codec.DefaultTable);
                Assert.Same(defaultTable, codec.DefaultTable);
                for (int i = 0; i < table.Length; i++)
                {
                    ZPTable row = table[i];
                    ZPTable defRow = defaultTable[i];
                    Assert.Equal<uint>(row.PValue, defRow.PValue);
                    Assert.Equal<uint>(row.MValue, defRow.MValue);
                    Assert.Equal<uint>(row.Down, defRow.Down);
                    Assert.Equal<uint>(row.Up, defRow.Up);
                }
            }
        }

        [Fact()]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void IWEncoderTest001()
        {
            string filePath = Path.Combine(Util.ArtifactsDataPath, "testbzz.obz");
            byte[] buffer = Util.ReadFileToEnd(filePath);

            using (MemoryStream stream = new MemoryStream())
            using (ZPCodec codec = new ZPCodec(stream, true, true))
            {
                Assert.True(codec.DjvuCompat);
                Assert.True(codec.Encoding);
                Assert.Same(stream, codec.DataStream);
                Assert.Equal(0, stream.Position);

                for (int b = 0; b < buffer.Length; b++)
                {
                    byte data = buffer[b];

                    for (int i = 0; i < 8; i++)
                    {
                        bool value = ((data >> i) & 1) == 1;
                        codec.IWEncoder(value);
                    }
                }

                codec.Flush();
                Assert.Equal(buffer.Length, stream.Position);
            }
        }

        [Fact()]
        public void IWEncoderTest002()
        {
            using (MemoryStream stream = new MemoryStream())
            using (ZPCodec codec = new ZPCodec(stream, true, true))
            {
                Assert.True(codec.DjvuCompat);
                Assert.True(codec.Encoding);
                Assert.Same(stream, codec.DataStream);
                Assert.Equal(0, stream.Position);

                const ulong hashLong = 0x0184b6730184b673;

                for (int i = 0; i < 64; i++)
                {
                    bool value = ((hashLong >> i) & 1) == 1;
                    codec.IWEncoder(value);
                }

                codec.Flush();
                Assert.Equal(8, stream.Position);

                ulong codedHash = BitConverter.ToUInt64(stream.GetBuffer(), 0);
                Assert.Equal(0x7fde92317fde9231u, codedHash);
            }
        }

        [Fact()]
        public void DecoderNoLearn001()
        {
            string filePath = Path.Combine(Util.ArtifactsDataPath, "test043C.json.bzz");
            byte[] testBuffer = Util.ReadFileToEnd(filePath);
            using (MemoryStream readStream = new MemoryStream(testBuffer))
            using (BzzReader reader = new BzzReader(new BSInputStream(readStream)))
            {
                byte[] result = new byte[testBuffer.Length];
                int readBytes = reader.Read(result, 0, result.Length/2);
                BSInputStream bsStream = reader.BaseStream as BSInputStream;
                Assert.NotNull(bsStream);

                byte ctx = 0x79;
                int decoded = bsStream.Coder.DecoderNoLearn(ref ctx);
            }
        }

        [Fact()]
        public void DecoderNoLearn002()
        {
            string filePath = Path.Combine(Util.ArtifactsDataPath, "test043C.json.bzz");
            byte[] testBuffer = Util.ReadFileToEnd(filePath);
            using (MemoryStream readStream = new MemoryStream(testBuffer))
            using (BzzReader reader = new BzzReader(new BSInputStream(readStream)))
            {
                byte[] result = new byte[testBuffer.Length];
                int readBytes = reader.Read(result, 0, result.Length / 2);
                BSInputStream bsStream = reader.BaseStream as BSInputStream;
                Assert.NotNull(bsStream);

                byte ctx = 0x01;
                int decoded = bsStream.Coder.DecoderNoLearn(ref ctx);
            }
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

        [Fact()]
        public void DisposeTest001()
        {
            string filePath = Path.Combine(Util.ArtifactsDataPath, "testhello.obz");
            string outFilePath = filePath + ".bz3";

            try
            {
                byte[] buffer = Util.ReadFileToEnd(filePath);
                long bytesWritten = 0;

                using (Stream stream = new FileStream(
                    outFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (BSOutputStream writer = new BSOutputStream(stream, 4096))
                {
                    writer.Write(buffer, 0, buffer.Length / 2);
                    bytesWritten = stream.Position;
                    Assert.False(writer.Coder.Disposed);
                    writer.Coder.Dispose();
                    Assert.True(writer.Coder.Disposed);
                }
            }
            finally
            {
                if (File.Exists(outFilePath))
                    File.Delete(outFilePath);
            }
        }

        [Fact()]
        public void EncoderNoLearn001()
        {
            string filePath = Path.Combine(Util.ArtifactsDataPath, "testhello.obz");
            string outFilePath = filePath + ".bz3";

            try
            {
                byte[] buffer = Util.ReadFileToEnd(filePath);
                long bytesWritten = 0;

                using (Stream stream = new FileStream(
                    outFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (BSOutputStream writer = new BSOutputStream(stream, 4096))
                {
                    writer.Write(buffer, 0, buffer.Length/2);
                    bytesWritten = stream.Position;
                    byte ctx = 0x01;
                    writer.Coder.EncoderNoLearn(1, ref ctx);
                    ctx = 0x01;
                    writer.Coder.EncoderNoLearn(0, ref ctx);
                }
            }
            finally
            {
                if (File.Exists(outFilePath))
                    File.Delete(outFilePath);
            }
        }

        [Fact()]
        public void FFZTest()
        {
            using (MemoryStream stream = new MemoryStream())
            using (ZPCodec codec = new ZPCodec(stream, true, true))
            {
                Assert.True(codec.DjvuCompat);
                Assert.True(codec.Encoding);
                Assert.Same(stream, codec.DataStream);
                // TODO Finish test implementation
                sbyte[] ffzt = codec.FFZT;
            }
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

        [Fact()]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void StateTest001()
        {
            string filePath = Path.Combine(Util.ArtifactsDataPath, "DjvuNet.pdb");
            string outFilePath = filePath + ".bz3";

            try
            {
                byte[] buffer = Util.ReadFileToEnd(filePath);

                byte[] data = new byte[buffer.Length + 32];
                Buffer.BlockCopy(buffer, 0, data, 0, buffer.Length);
                long bytesWritten = 0;

                using (Stream stream = new FileStream(
                    outFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (BSOutputStream writer = new BSOutputStream(stream, 4096))
                {
                    writer.Write(data, 0, 4096);
                    byte state = writer.Coder.State(0.2f);
                    Assert.Equal(22, state);
                    writer.Write(data, 0, buffer.Length - 4096);
                    bytesWritten = stream.Position;
                }
            }
            finally
            {
                if (File.Exists(outFilePath))
                    File.Delete(outFilePath);
            }
        }

        [Fact()]
        public void StateTest002()
        {
            string filePath = Path.Combine(Util.ArtifactsDataPath, "testhello.obz");
            string outFilePath = filePath + ".bz3";

            try
            {
                byte[] buffer = Util.ReadFileToEnd(filePath);

                byte[] data = new byte[buffer.Length + 32];
                Buffer.BlockCopy(buffer, 0, data, 0, buffer.Length);
                long bytesWritten = 0;

                using (Stream stream = new FileStream(
                    outFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (BSOutputStream writer = new BSOutputStream(stream, 4096))
                {
                    writer.Write(data, 0, 8);
                    byte state = writer.Coder.State(0.5f);
                    Assert.Equal(2, state);
                    writer.Write(data, 0, buffer.Length - 8);
                    bytesWritten = stream.Position;
                }
            }
            finally
            {
                if (File.Exists(outFilePath))
                    File.Delete(outFilePath);
            }
        }
    }
}
