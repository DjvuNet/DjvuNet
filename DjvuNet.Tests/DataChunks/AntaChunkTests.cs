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
    public class AntaChunkTests
    {
        [Fact()]
        public void AntaChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            AntaChunk unk = new AntaChunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.Anta, unk.ChunkType);
            Assert.Equal(ChunkType.Anta.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }
    }
}
