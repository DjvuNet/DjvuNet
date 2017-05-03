using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using System.IO;
using DjvuNet.Tests;
using DjvuNet.Tests.Xunit;

namespace DjvuNet.DataChunks.Tests
{
    public class TxtzChunkTests
    {

        public static IEnumerable<object[]> TextChunkTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                string[] files = System.IO.Directory.GetFiles(Util.ArtifactsDataPath, "*.txtz");
                foreach (string f in files)
                    retVal.Add(new object[] { f });

                return retVal;
            }
        }

        [Fact()]
        public void TxtzChunkTest001()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            TxtzChunk unk = new TxtzChunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.Txtz, unk.ChunkType);
            Assert.Equal<string>(ChunkType.Txtz.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [Fact()]
        public void TxtzChunkTest002()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test077C_P01.txtz");

            using (FileStream stream = new FileStream(file, FileMode.Open))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                TxtzChunk unk = new TxtzChunk(reader, null, null, null, stream.Length);

                Assert.Equal<ChunkType>(ChunkType.Txtz, unk.ChunkType);
                Assert.Equal<string>(ChunkType.Txtz.ToString(), unk.Name);
                Assert.Equal<long>(0, unk.DataOffset);
                Assert.True(unk.TextLength > 0);
                Assert.NotNull(unk.Text);
                Assert.Equal(unk.TextLength, unk.Text.Length);
                Assert.Equal(1, unk.Version);
            }
        }

        [Theory]
        [MemberData(nameof(TextChunkTestData))]
        public void TxtzChunk_Theory(string file)
        {
            using (FileStream stream = new FileStream(file, FileMode.Open))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                TxtzChunk unk = new TxtzChunk(reader, null, null, null, stream.Length);

                Assert.Equal<ChunkType>(ChunkType.Txtz, unk.ChunkType);
                Assert.Equal<string>(ChunkType.Txtz.ToString(), unk.Name);
                Assert.Equal<long>(0, unk.DataOffset);
                Assert.True(unk.TextLength > 0);
                Assert.NotNull(unk.Text);
                Assert.Equal(unk.TextLength, unk.Text.Length);
                Assert.Equal(1, unk.Version);
            }
        }
    }
}