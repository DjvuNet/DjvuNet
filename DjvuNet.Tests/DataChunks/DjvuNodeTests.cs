using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.DataChunks;
using Moq;
using Xunit;

namespace DjvuNet.DataChunks.Tests
{
    public class DjvuNodeTests
    {
        [Fact()]
        public void InitializeTest()
        {
            Mock<DjvuNode> nodeMock = new Mock<DjvuNode>() { CallBase = true };
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();

            nodeMock.Object.DataOffset = 1024; 
            nodeMock.Object.Reader = readerMock.Object;

            Assert.False(nodeMock.Object.IsInitialized);

            nodeMock.Object.Initialize();

            Assert.True(nodeMock.Object.IsInitialized);
            nodeMock.Verify(x => x.ReadData(readerMock.Object), Times.Once);
            readerMock.VerifySet(x => x.Position = 1024, Times.Once);
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetChildrenItemsTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void ToStringTest()
        {
            Mock<DjvuNode> nodeMock = new Mock<DjvuNode>() { CallBase = true };
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();

            nodeMock.Object.DataOffset = 1024;
            nodeMock.Object.Length = 2048;
            nodeMock.Object.Reader = readerMock.Object;
            nodeMock.Setup<ChunkType>(x => x.ChunkType).Returns(ChunkType.Form);
            nodeMock.Object.ChunkID = "DjvuTestID";

            string test = nodeMock.Object.ToString();
            Assert.Contains("Form", test);
            Assert.Contains("DjvuTestID", test);
            Assert.Contains("1024", test);
            Assert.Contains("2048", test);
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void DjvuNodeTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadDataTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void WriteDataTest001()
        {
            Mock<DjvuNode> nodeMock = new Mock<DjvuNode>() { CallBase = true };
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();

            using (MemoryStream stream = new MemoryStream(4096))
            using (DjvuWriter writer = new DjvuWriter(stream))
            {
                string chunkID = "FORM";
                string startOfChunkData = "Start of ChunkData";
                string endOfChunkData = "End of ChunkData";

                nodeMock.Object.DataOffset = 1024;
                nodeMock.Object.Length = 2048;
                nodeMock.Object.Reader = readerMock.Object;
                nodeMock.Setup<ChunkType>(x => x.ChunkType).Returns(ChunkType.Form);
                nodeMock.Object.ChunkID = chunkID;

                byte[] buffer = new byte[nodeMock.Object.Length];
                byte[] startData = Encoding.ASCII.GetBytes(startOfChunkData);
                Buffer.BlockCopy(startData, 0, buffer, 0, startData.Length);
                byte[] endOfData = Encoding.ASCII.GetBytes(endOfChunkData);
                Buffer.BlockCopy(endOfData, 0, buffer, buffer.Length - endOfData.Length, endOfData.Length);

                nodeMock.Setup<byte[]>(x => x.ChunkData).Returns(buffer);

                nodeMock.Object.WriteData(writer);

                byte[] testBuffer = stream.GetBuffer();

                string nodeName = Encoding.UTF8.GetString(testBuffer, 0, 4);
                Assert.False(String.IsNullOrWhiteSpace(nodeName));
                Assert.Equal<string>(chunkID, nodeName);

                byte[] lengthBytes = new byte[4];
                Buffer.BlockCopy(testBuffer, 4, lengthBytes, 0, 4);
                Array.Reverse(lengthBytes);
                uint dataLength = BitConverter.ToUInt32(lengthBytes, 0);
                Assert.Equal<uint>((uint)nodeMock.Object.Length, dataLength);

                string startOfData = Encoding.ASCII.GetString(testBuffer, 8, startData.Length);
                Assert.False(String.IsNullOrWhiteSpace(startOfData));
                Assert.Equal<string>(startOfChunkData, startOfData);

                string endData = Encoding.ASCII.GetString(testBuffer, 8 + (int)nodeMock.Object.Length - endOfData.Length, endOfData.Length);
                Assert.False(String.IsNullOrWhiteSpace(endData));
                Assert.Equal<string>(endOfChunkData, endData);
            }
        }

        [Fact()]
        public void WriteDataTest002()
        {
            Mock<DjvuNode> nodeMock = new Mock<DjvuNode>() { CallBase = true };
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();

            using (MemoryStream stream = new MemoryStream(4096))
            using (DjvuWriter writer = new DjvuWriter(stream))
            {
                string chunkID = "FORM";

                nodeMock.Object.DataOffset = 1024;
                nodeMock.Object.Length = 2048;
                nodeMock.Object.Reader = readerMock.Object;
                nodeMock.Setup<ChunkType>(x => x.ChunkType).Returns(ChunkType.Form);
                nodeMock.Object.ChunkID = chunkID;

                byte[] buffer = new byte[nodeMock.Object.Length];
                nodeMock.Setup<byte[]>(x => x.ChunkData).Returns(buffer);

                Assert.Throws<ArgumentNullException>("writer", () => nodeMock.Object.WriteData(null));

            }
        }
    }
}