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
    public class BM44ChunkTests
    {
        [Fact()]
        public void BM44ChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            BM44Chunk unk = new BM44Chunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.BM44, unk.ChunkType);
            Assert.Equal<string>(ChunkType.BM44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [Fact()]
        public void ChunkTypeTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);
            BM44Chunk unk = new BM44Chunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.BM44, unk.ChunkType);
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void DecodeImageTest()
        {

        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ImageTest()
        {

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

            BM44Chunk unk = new BM44Chunk(readerMock.Object, null, null, null, 1024);
            Assert.Equal<ChunkType>(ChunkType.BM44, unk.ChunkType);
            Assert.Equal<string>(ChunkType.BM44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            unk.ReadData(reader);
            Assert.Equal<long>(2048, reader.Position);
        }

    }
}