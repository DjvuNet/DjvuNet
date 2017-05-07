using Xunit;
using DjvuNet.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.DataChunks;
using Moq;
using DjvuNet.Tests.Xunit;

namespace DjvuNet.Parser.Tests
{
    public class DjvuParserTests
    {

        public static IEnumerable<object[]> NodeTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { null, null, null, ChunkType.Anta, "ANTA", 1 });
                retVal.Add(new object[] { null, null, null, ChunkType.Antz, "ANTZ", 2 });
                retVal.Add(new object[] { null, null, null, ChunkType.BG44, "BG44", 3 });
                retVal.Add(new object[] { null, null, null, ChunkType.BGjp, "BGJP", 4 });
                retVal.Add(new object[] { null, null, null, ChunkType.Cida, "CIDA", 5 });
                retVal.Add(new object[] { null, null, null, ChunkType.Dirm, "DIRM", 6 });
                retVal.Add(new object[] { null, null, null, ChunkType.Djbz, "DJBZ", 7 });
                retVal.Add(new object[] { null, null, null, ChunkType.Djvi, "DJVI", 8 });
                retVal.Add(new object[] { null, null, null, ChunkType.Djvm, "DJVM", 9 });
                retVal.Add(new object[] { null, null, null, ChunkType.Djvu, "DJVU", 10 });
                retVal.Add(new object[] { null, null, null, ChunkType.FG44, "FG44", 11 });
                retVal.Add(new object[] { null, null, null, ChunkType.FGbz, "FGBZ", 12 });
                retVal.Add(new object[] { null, null, null, ChunkType.FGjp, "FGJP", 13 });
                retVal.Add(new object[] { null, null, null, ChunkType.Incl, "INCL", 14 });
                retVal.Add(new object[] { null, null, null, ChunkType.Info, "INFO", 15 });
                retVal.Add(new object[] { null, null, null, ChunkType.Navm, "NAVM", 16 });
                retVal.Add(new object[] { null, null, null, ChunkType.Sjbz, "SJBZ", 17 });
                retVal.Add(new object[] { null, null, null, ChunkType.Smmr, "SMMR", 18 });
                retVal.Add(new object[] { null, null, null, ChunkType.TH44, "TH44", 19 });
                retVal.Add(new object[] { null, null, null, ChunkType.Thum, "THUM", 20 });
                retVal.Add(new object[] { null, null, null, ChunkType.Txta, "TXTA", 21 });
                retVal.Add(new object[] { null, null, null, ChunkType.Txtz, "TXTZ", 22 });
                retVal.Add(new object[] { null, null, null, ChunkType.Wmrm, "WMRM", 23 });
                retVal.Add(new object[] { null, null, null, ChunkType.BM44, "BM44", 24 });
                retVal.Add(new object[] { null, null, null, ChunkType.PM44, "PM44", 25 });
                retVal.Add(new object[] { null, null, null, ChunkType.BM44Form, "BM44", 26 });
                retVal.Add(new object[] { null, null, null, ChunkType.PM44Form, "PM44", 27 });

                return retVal;
            }
        }

        public static IEnumerable<object[]> RootFormChildTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                return retVal;
            }
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ParseDocumentTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [DjvuTheory]
        [MemberData(nameof(NodeTestData))]
        public void CreateDjvuNode_Theory(IDjvuReader reader, IDjvuDocument rootDocument,
            IDjvuElement parent, ChunkType chunkType,
            string chunkID = "", long length = 0)
        {
            if (reader == null)
            {
                Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
                readerMock.Setup<long>(x => x.Position).Returns(1024);
                reader = readerMock.Object;
            }

            IDjvuNode node = DjvuParser.CreateDjvuNode(reader, rootDocument, parent, chunkType, chunkID, length);
            Assert.NotNull(node);
            Assert.Equal<ChunkType>(chunkType, node.ChunkType);
            Assert.Equal<string>(chunkID, node.ChunkID);
            Assert.Equal<long>(length, node.Length);
            Assert.Equal<long>(reader.Position, node.DataOffset); 
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetChunkTypeTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void IsFormChunkTest()
        {
            Assert.True(DjvuParser.IsFormChunk(ChunkType.Djvi));
            Assert.True(DjvuParser.IsFormChunk(ChunkType.Djvm));
            Assert.True(DjvuParser.IsFormChunk(ChunkType.Djvu));
            Assert.True(DjvuParser.IsFormChunk(ChunkType.Thum));
            Assert.True(DjvuParser.IsFormChunk(ChunkType.BM44Form));
            Assert.True(DjvuParser.IsFormChunk(ChunkType.PM44Form));
            Assert.True(DjvuParser.IsFormChunk(ChunkType.Form));

            Assert.False(DjvuParser.IsFormChunk(ChunkType.Anta));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Antz));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.BG44));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.BGjp));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Cida));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Dirm));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Djbz));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.FG44));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.FGbz));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.FGjp));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Incl));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Info));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Navm));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Sjbz));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Smmr));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Text));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.TH44));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Txta));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Txtz));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.BM44));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.PM44));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Unknown));
            Assert.False(DjvuParser.IsFormChunk(ChunkType.Wmrm));
        }

        [Fact()]
        public void IsRootFormChildTest()
        {
            Assert.True(DjvuParser.IsRootFormChild(ChunkType.Dirm));
            Assert.True(DjvuParser.IsRootFormChild(ChunkType.Djvi));
            Assert.True(DjvuParser.IsRootFormChild(ChunkType.Djvu));
            Assert.True(DjvuParser.IsRootFormChild(ChunkType.Navm));
            Assert.True(DjvuParser.IsRootFormChild(ChunkType.Thum));
            Assert.True(DjvuParser.IsRootFormChild(ChunkType.Form));
            Assert.True(DjvuParser.IsRootFormChild(ChunkType.PM44, ChunkType.PM44Form));
            Assert.True(DjvuParser.IsRootFormChild(ChunkType.BM44, ChunkType.BM44Form));

            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Djvm));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Anta));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Antz));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.BG44));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.BM44));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.BM44Form));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.PM44));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.PM44Form));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.BGjp));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Cida));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Djbz));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.FG44));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.FGbz));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.FGjp));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Incl));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Info));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Sjbz));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Smmr));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Text));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.TH44));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Txta));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Txtz));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Unknown));
            Assert.False(DjvuParser.IsRootFormChild(ChunkType.Wmrm));
        }

    }
}