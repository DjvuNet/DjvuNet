using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using DjvuNet.Tests.Xunit;
using System.IO;
using DjvuNet.Tests;

namespace DjvuNet.DataChunks.Tests
{
    public class AntzChunkTests
    {

        public static IEnumerable<object[]> AntzTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                string[] files = System.IO.Directory.GetFiles(Util.ArtifactsDataPath, "*.antz");
                foreach(string f in files)
                {
                    retVal.Add(new object[]
                    {
                        Path.GetFileName(f),
                        f
                    });
                }

                return retVal;
            }
        }

        [Fact()]
        public void AntzChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            AntzChunk unk = new AntzChunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.Antz, unk.ChunkType);
            Assert.Equal(ChunkType.Antz.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [DjvuTheory]
        [MemberData(nameof(AntzTestData))]
        public void AntzChunk_Theory(string fileName, string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                AntzChunk unk = new AntzChunk(reader, null, null, null, stream.Length);
                var annotations = unk.Annotations;
                Assert.NotNull(annotations);
            }
        }

        [Fact]
        public void AnnotationsTest()
        {
            string filePath = Path.Combine(Util.ArtifactsDataPath, "test004C_P01.antz");
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                AntzChunk unk = new AntzChunk(reader, null, null, null, stream.Length);
                var annotations = unk.Annotations;
                Assert.NotNull(annotations);
                Assert.Equal(0x22, annotations.Length);
            }
        }
    }
}
