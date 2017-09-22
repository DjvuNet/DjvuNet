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
    public class BGjpChunkTests
    {
        [Fact()]
        public void BGjpChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            BGjpChunk unk = new BGjpChunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.BGjp, unk.ChunkType);
            Assert.Equal(ChunkType.BGjp.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [Fact()]
        public void ReadDataTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            BGjpChunk unk = new BGjpChunk(readerMock.Object, null, null, null, 1024);
            Assert.Equal<ChunkType>(ChunkType.BGjp, unk.ChunkType);
            Assert.Equal(ChunkType.BGjp.ToString(), unk.Name);
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

            BGjpChunk unk = new BGjpChunk(reader, null, null, null, 2375);
            Assert.Equal<ChunkType>(ChunkType.BGjp, unk.ChunkType);
            Assert.Equal(ChunkType.BGjp.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            byte[] buffer = unk.DecodeImageData();
            Assert.Equal<long>(unk.Length, buffer.Length);
        }

        [Fact()]
        public void BackgroundImageTest001()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);
            readerMock.Setup(x => x.ReadBytes(It.IsAny<int>())).Returns((int x) => new byte[x]);
            readerMock.Setup(x => x.CloneReader(It.IsAny<long>())).Returns(readerMock.Object);
            readerMock.Setup(x => x.CloneReaderToMemory(It.IsAny<long>(), It.IsAny<long>())).Returns(readerMock.Object);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            BGjpChunk unk = new BGjpChunk(readerMock.Object, null, null, null, 2375);
            Assert.Equal<ChunkType>(ChunkType.BGjp, unk.ChunkType);
            Assert.Equal(ChunkType.BGjp.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            byte[] buffer = unk.BackgroundImage;
            Assert.Equal<long>(unk.Length, buffer.Length);
        }

        [Fact()]
        public void BackgroundImageTest002()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);

            readerMock.Setup(x => x.ReadBytes(It.IsAny<int>())).Returns((int x) => new byte[x]);
            readerMock.Setup(x => x.CloneReaderToMemory(It.IsAny<long>(), It.IsAny<long>())).Returns(readerMock.Object);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            BGjpChunk unk = new BGjpChunk(readerMock.Object, null, null, null, 2375);
            Assert.Equal<ChunkType>(ChunkType.BGjp, unk.ChunkType);
            Assert.Equal(ChunkType.BGjp.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            byte[] buffer = new byte[128];
            unk.BackgroundImage = buffer;
            Assert.Equal<long>(unk.BackgroundImage.Length, buffer.Length);

            // TODO Rethink implementation - data length during edition should be associated with node Length prop

            byte[] testBuffer = unk.BackgroundImage;
            Assert.NotEqual(2375, testBuffer.Length);
        }
    }
}
