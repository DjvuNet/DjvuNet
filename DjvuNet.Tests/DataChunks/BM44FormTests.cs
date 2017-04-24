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
    public class BM44FormTests
    {
        [Fact()]
        public void BM44FormTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            BM44Form unk = new BM44Form(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.BM44Form, unk.ChunkType);
            Assert.Equal<string>(ChunkType.BM44Form.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [Fact()]
        public void WriteDataTest()
        {
            byte[] buffer = new byte[2048];

            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            Mock<DjvuNode> nodeMock1 = new Mock<DjvuNode> { CallBase = true };
            nodeMock1.Object.ChunkData = buffer;
            nodeMock1.Setup(x => x.Name).Returns("BM44");
            nodeMock1.Object.Length = buffer.Length;

            Mock<DjvuNode> nodeMock2 = new Mock<DjvuNode> { CallBase = true };
            nodeMock2.Object.ChunkData = buffer;
            nodeMock2.Setup(x => x.Name).Returns("BM44");
            nodeMock2.Object.Length = buffer.Length;

            Mock<IDjvuWriter> writerMock = new Mock<IDjvuWriter>();
            writerMock.Setup<long>(x => x.WriteUTF8String(It.IsAny<string>())).Returns(4);
            writerMock.Setup(x => x.WriteUInt32BigEndian(It.IsAny<uint>()));
            writerMock.Setup(x => x.Write(buffer, 0, buffer.Length));

            BM44Form form = new BM44Form(readerMock.Object, null, null, "BM44Form", 2048);
            form.ChunkData = buffer;

            int result = form.AddChild(nodeMock1.Object);
            Assert.Equal<int>(1, result);
            Assert.Equal<int>(1, form.Children.Count);

            result = form.InsertChild(0, nodeMock2.Object);
            Assert.Equal<int>(2, result);
            Assert.Equal<int>(2, form.Children.Count);

            form.WriteData(writerMock.Object);

            writerMock.Verify(x => x.WriteUTF8String("FORM"), Times.Once());
            writerMock.Verify(x => x.WriteUTF8String("BM44"), Times.Exactly(3));
            writerMock.Verify(x => x.WriteUInt32BigEndian((uint)buffer.Length), Times.Exactly(2));
            writerMock.Verify(x => x.WriteUInt32BigEndian(((uint)buffer.Length + 8) * 2), Times.Exactly(1));
            writerMock.Verify(x => x.Write(buffer, 0, buffer.Length), Times.Exactly(2));
        }
    }
}