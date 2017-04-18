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
    public class SjbzChunkTests
    {
        [Fact()]
        public void SjbzChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup<long>(x => x.Position).Returns(1024);
            // readerMock.SetupAllProperties();

            Mock<IDjvuDocument> docMock = new Mock<IDjvuDocument>();
            Mock<IDjvuElement> parentMock = new Mock<IDjvuElement>();

            IDjvuReader reader = readerMock.Object;
            IDjvuElement parent = parentMock.Object;
            IDjvuDocument document = docMock.Object;

            SjbzChunk chunk = new SjbzChunk(reader, parent, document, "SJBZ", 2048);
            Assert.False(chunk.IsInitialized);
            Assert.Equal<long>(1024, chunk.DataOffset);
            Assert.Equal<long>(2048, chunk.Length);
            Assert.Same(reader, chunk.Reader);
            Assert.Same(parent, chunk.Parent);
            Assert.Same(document, chunk.Document);
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

            SjbzChunk chunk = new SjbzChunk(reader, parent, document, "SJBZ", 2048);
            Assert.False(chunk.IsInitialized);

            chunk.ReadData(reader);
            Assert.Equal<long>(reader.Position, chunk.DataOffset + chunk.Length);
        }

        [Fact()]
        public void ImageTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupAllProperties();
            readerMock.Object.Position = 1024;

            Mock<IDjvuDocument> docMock = new Mock<IDjvuDocument>();
            Mock<IDjvuElement> parentMock = new Mock<IDjvuElement>();

            IDjvuReader reader = readerMock.Object;
            IDjvuElement parent = parentMock.Object;
            IDjvuDocument document = docMock.Object;

            SjbzChunk chunk = new SjbzChunk(reader, parent, document, "SJBZ", 2048);
            Assert.False(chunk.IsInitialized);

            chunk.ReadData(reader);
            Assert.Equal<long>(reader.Position, chunk.DataOffset + chunk.Length);

        }
    }
}