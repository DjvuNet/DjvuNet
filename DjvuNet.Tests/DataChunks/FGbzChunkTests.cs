using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DjvuNet.Tests;
using Moq;
using DjvuNet.Tests.Xunit;
using System.Runtime.CompilerServices;

namespace DjvuNet.DataChunks.Tests
{
    public class FGbzChunkTests
    {

        public static IEnumerable<object[]> FgbzTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();
                string[] testFiles = System.IO.Directory.GetFiles(Util.ArtifactsDataPath, "*.fgbz");
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
            Assert.Equal("FGbz", chunk.Name);
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
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Palette_Theory(string file, DjvuJsonDocument doc)
        {

            byte[] buffer = null;
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
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

        [Fact()]
        public void ColorPalette001()
        {
            string filePath = Path.Combine(Util.ArtifactsDataPath, "test003C_P01.fgbz");
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                FGbzChunk th = new FGbzChunk(reader, null, null, "FGBZ", stream.Length);
                Assert.Equal<ChunkType>(ChunkType.FGbz, th.ChunkType);
                Assert.Equal(stream.Length, th.Length);

                var palette = th.Palette;
                Assert.NotNull(palette);
                Assert.IsType<ColorPalette>(palette);
                Assert.False(th.HasShapeTableData);

                reader.Position = 0;
                var colPal = new ColorPalette(reader, th);
                th.Palette = colPal;
                var palette2 = th.Palette;
                Assert.NotNull(palette2);
                Assert.IsType<ColorPalette>(palette2);
                Assert.NotSame(palette, palette2);
                Assert.Same(palette2, colPal);
            }
        }
    }
}
