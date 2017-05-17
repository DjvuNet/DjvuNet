using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using DjvuNet.Tests;
using System.IO;
using DjvuNet.Tests.Xunit;
using DjvuNet.Errors;

namespace DjvuNet.DataChunks.Tests
{
    public class NavmChunkTests
    {
        public static IEnumerable<object[]> NavmTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                string[] files = System.IO.Directory.GetFiles(Util.ArtifactsDataPath, "*.navm");
                foreach (string f in files)
                {
                    // TODO verify DjvuLibre handling of that file navm chunk
                    if (f.EndsWith("test053C.navm"))
                        continue;

                    retVal.Add(new object[] { f });
                }

                return retVal;
            }
        }
 
        [DjvuTheory]
        [MemberData(nameof(NavmTestData))]
        public void NavmChunk_Theory(string file)
        {
            Mock<IDjvuDocument> docMock = new Mock<IDjvuDocument>();
            List<IDjvuPage> pages = new List<IDjvuPage>();
            docMock.Setup(x => x.Pages).Returns(pages);
            for (int i = 0; i < 300; i++)
            {
                Mock<IDjvuPage> pageMock = new Mock<IDjvuPage>();
                pageMock.Setup(x => x.PageNumber).Returns(i + 1);
                pages.Add(pageMock.Object);
            }

            Mock<DjvuFormElement> parentMock = new Mock<DjvuFormElement> { CallBase = true };

            using (FileStream stream = new FileStream(file, FileMode.Open))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                NavmChunk unk = new NavmChunk(reader, parentMock.Object, docMock.Object, "NAVM", stream.Length);
                Assert.Equal<ChunkType>(ChunkType.Navm, unk.ChunkType);
                Assert.Equal(stream.Length, unk.Length);

                var bookmarks = unk.Bookmarks;
                Assert.NotNull(bookmarks);

                foreach(var b in bookmarks)
                    Assert.NotNull(b.Children);
            }
        }

        [Fact]
        public void InvalidBookmarkFormat()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test053C.navm");
            Mock<IDjvuDocument> docMock = new Mock<IDjvuDocument>();
            List<IDjvuPage> pages = new List<IDjvuPage>();
            docMock.Setup(x => x.Pages).Returns(pages);
            for (int i = 0; i < 300; i++)
            {
                Mock<IDjvuPage> pageMock = new Mock<IDjvuPage>();
                pageMock.Setup(x => x.PageNumber).Returns(i + 1);
                pages.Add(pageMock.Object);
            }

            Mock<DjvuFormElement> parentMock = new Mock<DjvuFormElement> { CallBase = true };

            using (FileStream stream = new FileStream(file, FileMode.Open))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                NavmChunk unk = new NavmChunk(reader, parentMock.Object, docMock.Object, "NAVM", stream.Length);
                Assert.Equal<ChunkType>(ChunkType.Navm, unk.ChunkType);
                Assert.Equal(stream.Length, unk.Length);

                Assert.Throws<DjvuFormatException>(() => unk.Bookmarks);
            }
        }

        [Fact]
        public void BookmarkOutOfRange()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C.navm");
            Mock<IDjvuDocument> docMock = new Mock<IDjvuDocument>();
            List<IDjvuPage> pages = new List<IDjvuPage>();
            docMock.Setup(x => x.Pages).Returns(pages);
            for (int i = 0; i < 3; i++)
            {
                Mock<IDjvuPage> pageMock = new Mock<IDjvuPage>();
                pageMock.Setup(x => x.PageNumber).Returns(i + 1);
                pages.Add(pageMock.Object);
            }

            Mock<DjvuFormElement> parentMock = new Mock<DjvuFormElement> { CallBase = true };

            using (FileStream stream = new FileStream(file, FileMode.Open))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                NavmChunk unk = new NavmChunk(reader, parentMock.Object, docMock.Object, "NAVM", stream.Length);
                Assert.Equal<ChunkType>(ChunkType.Navm, unk.ChunkType);
                Assert.Equal(stream.Length, unk.Length);

                Assert.Throws<DjvuArgumentOutOfRangeException>(() => unk.Bookmarks);
            }
        }

        [Fact()]
        public void NavmChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            NavmChunk unk = new NavmChunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.Navm, unk.ChunkType);
            Assert.Equal<string>(ChunkType.Navm.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [Fact()]
        public void ReadDataTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            NavmChunk unk = new NavmChunk(readerMock.Object, null, null, null, 1024);
            Assert.Equal<ChunkType>(ChunkType.Navm, unk.ChunkType);
            Assert.Equal<string>(ChunkType.Navm.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            unk.ReadData(reader);
            Assert.Equal<long>(2048, reader.Position);
        }
    }
}