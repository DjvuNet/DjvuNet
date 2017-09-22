using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using System.IO;
using DjvuNet.Tests;
using DjvuNet.Wavelet;
using DjvuNet.Tests.Xunit;

namespace DjvuNet.DataChunks.Tests
{
    public class FG44ChunkTests
    {
        [Fact()]
        public void FG44ChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            FG44Chunk unk = new FG44Chunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.FG44, unk.ChunkType);
            Assert.Equal(ChunkType.FG44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [Fact()]
        public void ReadDataTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            FG44Chunk unk = new FG44Chunk(readerMock.Object, null, null, null, 1024);
            Assert.Equal<ChunkType>(ChunkType.FG44, unk.ChunkType);
            Assert.Equal(ChunkType.FG44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            unk.ReadData(reader);
            Assert.Equal<long>(2048, reader.Position);
        }

        [Fact()]
        public void ForegroundImageTest()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test037C_P01.fg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                FG44Chunk unk = new FG44Chunk(reader, null, null, "FG44", stream.Length);
                var img = unk.ForegroundImage;
                Assert.NotNull(img);
                Assert.NotEqual(0, img.Width);
                Assert.NotEqual(0, img.Height);

                var map = new InterWavePixelMapDecoder();
                Assert.Equal(0, map.Width);
                Assert.Equal(0, map.Height);

                unk.ForegroundImage = map;

                Assert.NotSame(img, map);

                img = unk.ForegroundImage;
                Assert.Same(img, map);
                Assert.Equal(0, img.Width);
                Assert.Equal(0, img.Height);
            }
        }

        public static IEnumerable<object[]> FG44TestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                string[] files = System.IO.Directory.GetFiles(Util.ArtifactsDataPath, "*.fg44");

                foreach (string f in files)
                {
                    string fileName = Path.GetFileName(f);
                    retVal.Add(new object[]
                    {
                        fileName,
                        f
                    });
                }

                return retVal;
            }
        }

        [DjvuTheory]
        [MemberData(nameof(FG44TestData))]
        public void FG44Chunk_Theory(string fileName, string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                FG44Chunk th = new FG44Chunk(reader, null, null, "FG44", stream.Length);
                Assert.Equal<ChunkType>(ChunkType.FG44, th.ChunkType);
                Assert.Equal(stream.Length, th.Length);

                var image = th.ForegroundImage;
                Assert.NotNull(image);
                Assert.IsType<InterWavePixelMapDecoder>(image);
                Assert.True(image.Width >= 0 && image.Height > 0);
            }
        }

        [Fact]
        public void ForegroundImageTest002()
        {
            string filePath = Path.Combine(Util.ArtifactsDataPath, "test037C_P01.fg44");
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                FG44Chunk th = new FG44Chunk(reader, null, null, "FG44", stream.Length);
                Assert.Equal<ChunkType>(ChunkType.FG44, th.ChunkType);
                Assert.Equal(stream.Length, th.Length);

                var image = th.ForegroundImage;
                Assert.NotNull(image);
                Assert.IsType<InterWavePixelMapDecoder>(image);
                Assert.True(image.Width >= 0 && image.Height > 0);

                var map = new InterWavePixelMapDecoder();
                th.ForegroundImage = map;
                var image2 = th.ForegroundImage;
                Assert.NotNull(image2);
                Assert.NotSame(image, image2);
                Assert.Same(map, image2);
                Assert.True(image2.Width == 0 && image2.Height == 0);
            }
        }
    }
}
