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
    public class WmrmChunkTests
    {
        [Fact()]
        public void WmrmChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            WmrmChunk unk = new WmrmChunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.Wmrm, unk.ChunkType);
            Assert.Equal(ChunkType.Wmrm.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [Fact()]
        public void ReadDataTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            WmrmChunk unk = new WmrmChunk(readerMock.Object, null, null, null, 1024);
            Assert.Equal<ChunkType>(ChunkType.Wmrm, unk.ChunkType);
            Assert.Equal(ChunkType.Wmrm.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            unk.ReadData(reader);
            Assert.Equal<long>(2048, reader.Position);        }
    }
}
