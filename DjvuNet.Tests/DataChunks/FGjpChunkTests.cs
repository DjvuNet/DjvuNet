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
    public class FGjpChunkTests
    {
        [Fact()]
        public void FGjpChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            FGjpChunk unk = new FGjpChunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.FGjp, unk.ChunkType);
            Assert.Equal<string>(ChunkType.FGjp.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [Fact()]
        public void ReadDataTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            FGjpChunk unk = new FGjpChunk(readerMock.Object, null, null, null, 1024);
            Assert.Equal<ChunkType>(ChunkType.FGjp, unk.ChunkType);
            Assert.Equal<string>(ChunkType.FGjp.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            unk.ReadData(reader);
            Assert.Equal<long>(2048, reader.Position);
        }

        [Fact()]
        public void DecodeImageDataTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);
            readerMock.Setup(x => x.ReadBytes(It.IsAny<int>())).Returns((int x) => new byte[x]);
            readerMock.Setup(x => x.CloneReader(It.IsAny<long>())).Returns(readerMock.Object);
            readerMock.Setup(x => x.CloneReaderToMemory(It.IsAny<long>(), It.IsAny<long>())).Returns(readerMock.Object);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            FGjpChunk unk = new FGjpChunk(reader, null, null, null, 2375);
            Assert.Equal<ChunkType>(ChunkType.FGjp, unk.ChunkType);
            Assert.Equal<string>(ChunkType.FGjp.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            byte[] buffer = unk.DecodeImageData();
            Assert.Equal<long>(unk.Length, buffer.Length);
        }

        [Fact()]
        public void ForegroundImageTest001()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);
            readerMock.Setup(x => x.ReadBytes(It.IsAny<int>())).Returns((int x) => new byte[x]);
            readerMock.Setup(x => x.CloneReader(It.IsAny<long>())).Returns(readerMock.Object);
            readerMock.Setup(x => x.CloneReaderToMemory(It.IsAny<long>(), It.IsAny<long>())).Returns(readerMock.Object);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            FGjpChunk unk = new FGjpChunk(readerMock.Object, null, null, null, 2375);
            Assert.Equal<ChunkType>(ChunkType.FGjp, unk.ChunkType);
            Assert.Equal<string>(ChunkType.FGjp.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            byte[] buffer = unk.ForegroundImage;
            Assert.Equal<long>(unk.Length, buffer.Length);
        }

        [Fact()]
        public void ForegroundImageTest002()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);

            readerMock.Setup(x => x.ReadBytes(It.IsAny<int>())).Returns((int x) => new byte[x]);
            readerMock.Setup(x => x.CloneReaderToMemory(It.IsAny<long>(), It.IsAny<long>())).Returns(readerMock.Object);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            FGjpChunk unk = new FGjpChunk(readerMock.Object, null, null, null, 2375);
            Assert.Equal<ChunkType>(ChunkType.FGjp, unk.ChunkType);
            Assert.Equal<string>(ChunkType.FGjp.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            byte[] buffer = new byte[128];
            unk.ForegroundImage = buffer;
            Assert.Equal<long>(unk.ForegroundImage.Length, buffer.Length);

            // TODO Rethink implementation - data length during edition should be associated with node Length prop

            byte[] testBuffer = unk.ForegroundImage;
            Assert.NotEqual(testBuffer.Length, 2375);
        }
    }
}