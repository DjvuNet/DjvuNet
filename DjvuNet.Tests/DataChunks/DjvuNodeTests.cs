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
        public void IsFormChunkTest()
        {
            Assert.True(DjvuNode.IsFormChunk(ChunkType.Djvi));
            Assert.True(DjvuNode.IsFormChunk(ChunkType.Djvm));
            Assert.True(DjvuNode.IsFormChunk(ChunkType.Djvu));
            Assert.True(DjvuNode.IsFormChunk(ChunkType.Thum));
            Assert.True(DjvuNode.IsFormChunk(ChunkType.Form));

            Assert.False(DjvuNode.IsFormChunk(ChunkType.Anta));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.Antz));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.BG44));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.BGjp));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.Cida));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.Dirm));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.Djbz));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.FG44));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.FGbz));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.FGjp));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.Incl));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.Info));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.Navm));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.Sjbz));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.Smmr));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.Text));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.TH44));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.Txta));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.Txtz));

            Assert.False(DjvuNode.IsFormChunk(ChunkType.Unknown));
            Assert.False(DjvuNode.IsFormChunk(ChunkType.WMRM));
        }

        [Fact()]
        public void IsRootFormChildTest()
        {
            Assert.True(DjvuNode.IsRootFormChild(ChunkType.Dirm));
            Assert.True(DjvuNode.IsRootFormChild(ChunkType.Djvi));
            Assert.True(DjvuNode.IsRootFormChild(ChunkType.Djvu));
            Assert.True(DjvuNode.IsRootFormChild(ChunkType.Navm));
            Assert.True(DjvuNode.IsRootFormChild(ChunkType.Thum));
            Assert.True(DjvuNode.IsRootFormChild(ChunkType.Form));

            Assert.False(DjvuNode.IsRootFormChild(ChunkType.Djvm));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.Anta));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.Antz));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.BG44));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.BGjp));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.Cida));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.Djbz));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.FG44));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.FGbz));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.FGjp));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.Incl));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.Info));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.Sjbz));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.Smmr));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.Text));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.TH44));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.Txta));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.Txtz));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.Unknown));
            Assert.False(DjvuNode.IsRootFormChild(ChunkType.WMRM));
        }

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
        public void BuildIffChunkTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadDataTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void WriteDataTest()
        {
            Mock<DjvuNode> nodeMock = new Mock<DjvuNode>() { CallBase = true };
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();

            using (MemoryStream stream = new MemoryStream(4096))
            using (DjvuWriter writer = new DjvuWriter(stream))
            {

                nodeMock.Object.DataOffset = 1024;
                nodeMock.Object.Length = 2048;
                nodeMock.Object.Reader = readerMock.Object;
                nodeMock.Setup<ChunkType>(x => x.ChunkType).Returns(ChunkType.Form);
                nodeMock.Object.ChunkID = "DTID";

                byte[] buffer = new byte[nodeMock.Object.Length];
                nodeMock.Setup<byte[]>(x => x.ChunkData).Returns(buffer);
            }
        }
    }
}