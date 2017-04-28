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

namespace DjvuNet.DataChunks.Tests
{
    public class NavmChunkTests
    {
        public static IEnumerable<object[]> NavmTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                foreach(DjvuJsonDocument doc in DjvuNet.Tests.UtilJson.JsonDocuments)
                {
                    retVal.Add(new object[]
                    {
                        doc.DocumentFile.Replace("json", "djvu"),
                        doc
                    });
                }

                return retVal;
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(NavmTestData))]
        public void NavmChunk_Theory(string djvuFile, DjvuJsonDocument doc)
        {
            using(DjvuDocument document = new DjvuDocument(djvuFile))
            {
                Util.VerifyDjvuDocument(doc.Data.Pages.Length, document);
                IDjvuElement rootForm = document.RootForm;
                var navm = rootForm.Children.Where(x => x.ChunkType == ChunkType.Navm).FirstOrDefault() as NavmChunk;
                if (navm != null)
                {
                    
                }
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