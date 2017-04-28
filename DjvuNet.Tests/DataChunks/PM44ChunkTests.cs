using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;

namespace DjvuNet.DataChunks.Tests
{
    public class PM44ChunkTests
    {
        [Fact()]
        public void PM44ChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            PM44Chunk unk = new PM44Chunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.PM44, unk.ChunkType);
            Assert.Equal<string>(ChunkType.PM44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ProgressiveDecodeBackgroundTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void ReadDataTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            PM44Chunk unk = new PM44Chunk(readerMock.Object, null, null, null, 1024);
            Assert.Equal<ChunkType>(ChunkType.PM44, unk.ChunkType);
            Assert.Equal<string>(ChunkType.PM44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            unk.ReadData(reader);
            Assert.Equal<long>(2048, reader.Position);
        }
    }
}