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
    public class AntzChunkTests
    {
        [Fact()]
        public void AntzChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            AntzChunk unk = new AntzChunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.Antz, unk.ChunkType);
            Assert.Equal<string>(ChunkType.Antz.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }
    }
}