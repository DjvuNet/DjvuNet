using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Moq;

namespace DjvuNet.DataChunks.Tests
{
    public class UnknownChunkTests
    {
        [Fact()]
        public void UnknownChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            UnknownChunk unk = new UnknownChunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.Unknown, unk.ChunkType);
            Assert.Equal<string>(ChunkType.Unknown.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [Fact()]
        public void ReadDataTest()
        {
            using (MemoryStream stream = new MemoryStream())
            using (DjvuReader reader = new DjvuReader(stream))
            {
                UnknownChunk unk = new UnknownChunk(reader, null, null, null, 1024);
                unk.ReadData(reader);
                Assert.Equal<long>(1024, reader.Position);
            }
        }

        [Fact()]
        public void DataTest()
        {
            using (MemoryStream stream = new MemoryStream(4096))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                UnknownChunk unk = new UnknownChunk(reader, null, null, null, 1024);
                unk.ReadData(reader);
                IDjvuReader r = unk.Data;
                Assert.Equal<long>(0, r.Position);
                Assert.NotSame(reader, r);
            }
        }
    }
}