using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Tests;
using Moq;
using DjvuNet.Tests.Xunit;

namespace DjvuNet.DataChunks.Tests
{
    public class FGbzChunkTests
    {

        public static IEnumerable<object[]> FgbzTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();
                string directory = Path.Combine(Util.RepoRoot, "artifacts", "data");
                string[] testFiles = System.IO.Directory.GetFiles(directory, "*.fgbz");
                for(int i = 0; i < testFiles.Length; i++)
                {
                    string testNoStr = Path.GetFileNameWithoutExtension(testFiles[i])
                        .Substring(4, 3).TrimStart(new char[] { '0' });
                    int testNumber = int.Parse(testNoStr);
                    var jsonDoc = UtilJson.GetJsonDocument(testNumber - 1);

                    retVal.Add(new object[]
                    {
                        testFiles[i],
                        jsonDoc,
                    });

                } 
                return retVal;
            }
        }

        [Fact()]
        public void FGbzChunkTest001()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup<long>(x => x.Position).Returns(1024);
            // readerMock.SetupAllProperties();

            Mock<IDjvuDocument> docMock = new Mock<IDjvuDocument>();
            Mock<IDjvuElement> parentMock = new Mock<IDjvuElement>();

            IDjvuReader reader = readerMock.Object;
            IDjvuElement parent = parentMock.Object;
            IDjvuDocument document = docMock.Object;

            FGbzChunk chunk = new FGbzChunk(reader, parent, document, "FGBZ", 2048);
            Assert.False(chunk.IsInitialized);
            Assert.Equal<long>(1024, chunk.DataOffset);
            Assert.Equal<long>(2048, chunk.Length);
            Assert.Same(reader, chunk.Reader);
            Assert.Same(parent, chunk.Parent);
            Assert.Same(document, chunk.Document);
        }

        [Fact()]
        public void FGbzChunkTest002()
        {
            FGbzChunk chunk = new FGbzChunk();
            Assert.Equal<ChunkType>(ChunkType.FGbz, chunk.ChunkType);
            Assert.Equal<String>("FGbz", chunk.Name);
        }

        [Fact()]
        public void ReadDataTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupAllProperties();
            readerMock.Object.Position = 1024;

            Mock<IDjvuDocument> docMock = new Mock<IDjvuDocument>();
            Mock<IDjvuElement> parentMock = new Mock<IDjvuElement>();

            IDjvuReader reader = readerMock.Object;
            IDjvuElement parent = parentMock.Object;
            IDjvuDocument document = docMock.Object;

            FGbzChunk chunk = new FGbzChunk(reader, parent, document, "FGBZ", 2048);
            Assert.False(chunk.IsInitialized);

            chunk.ReadData(reader);
            long positionSum = chunk.DataOffset + chunk.Length;
            Assert.Equal<long>(reader.Position, positionSum);
        }

        [DjvuTheory]
        [MemberData(nameof(FgbzTestData))]
        public void Palette_Theory(string testFile, DjvuJsonDocument doc)
        {

            byte[] buffer = null;
            using (FileStream fs = File.OpenRead(testFile))
            {
                buffer = new byte[fs.Length];
                int result = fs.Read(buffer, 0, buffer.Length);
                Assert.Equal<long>(fs.Length, result);
            }
            Mock<IDjvuDocument> docMock = new Mock<IDjvuDocument>();
            Mock<IDjvuElement> parentMock = new Mock<IDjvuElement>();
            IDjvuElement parent = parentMock.Object;
            IDjvuDocument document = docMock.Object;

            using (MemoryStream ms = new MemoryStream(buffer, false))
            using (IDjvuReader reader = new DjvuReader(ms))
            {
                FGbzChunk chunk = new FGbzChunk(reader, parent, document, "FGBZ", buffer.Length);
                Assert.False(chunk.IsInitialized);
                var testChunk = doc.Data.Pages[0].Children
                    .Where(x => x.ID == "FGbz")
                    .FirstOrDefault<DjvuJsonDocument.Chunk>();
                Assert.Equal<int>((int)testChunk?.Version.Value, chunk.Version);
                Assert.Equal<int>((int)testChunk.Colors.Value, (int) chunk.Palette?.PaletteColors?.Length);
            }

        }
    }
}
