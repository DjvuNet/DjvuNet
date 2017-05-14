using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using System.IO;
using DjvuNet.Graphics;
using DjvuNet.Tests;
using DjvuNet.Tests.Xunit;
using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks.Tests
{
    public class TH44ChunkTests
    {
        [Fact()]
        public void TH44ChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            TH44Chunk unk = new TH44Chunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.TH44, unk.ChunkType);
            Assert.Equal<string>(ChunkType.TH44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [Fact()]
        public void ReadDataTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            TH44Chunk unk = new TH44Chunk(reader, null, null, null, 1024);
            Assert.Equal<ChunkType>(ChunkType.TH44, unk.ChunkType);
            Assert.Equal<string>(ChunkType.TH44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            unk.ReadData(reader);
            Assert.Equal<long>(2048, reader.Position);
        }

        [Fact]
        public void ThumbnailTest053()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test053C_01_01.th44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                TH44Chunk th = new TH44Chunk(reader, null, null, "TH44", stream.Length);
                var thumb = th.Thumbnail;
                Assert.NotNull(thumb);
                Assert.Equal(128, thumb.Height);
                Assert.Equal(91, thumb.Width);
            }
        }

        [Fact]
        public void ThumbnailTest053s()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test053C_01_01.th44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                TH44Chunk th = new TH44Chunk(reader, null, null, "TH44", stream.Length);
                var thumb = th.Thumbnail;
                Assert.NotNull(thumb);
                Assert.Equal(128, thumb.Height);
                Assert.Equal(91, thumb.Width);

                th.Thumbnail = null;
                var thumb2 = th.Thumbnail;
                Assert.NotNull(thumb2);
                Assert.Equal(128, thumb2.Height);
                Assert.Equal(91, thumb2.Width);

                Assert.NotSame(thumb, thumb2);

                var map = new InterWavePixelMapDecoder();
                th.Thumbnail = map;

                var thumb3 = th.Thumbnail;
                Assert.NotNull(thumb3);
                Assert.Same(map, thumb3);
            }
        }

        [Fact]
        public void ImageTest053()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test053C_01_01.th44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                TH44Chunk th = new TH44Chunk(reader, null, null, "TH44", stream.Length);
                var thumb = th.Thumbnail;
                Assert.NotNull(thumb);
                Assert.Equal(128, thumb.Height);
                Assert.Equal(91, thumb.Width);

                var img1 = th.Image;
                var img2 = th.Image;
                Assert.NotNull(img1);
                Assert.NotNull(img2);
                Assert.Same(img1, img2);

                Assert.Equal(thumb.Width, img1.ImageWidth);
                Assert.Equal(thumb.Height, img1.ImageHeight);
            }
        }

        public static IEnumerable<object[]> TH44TestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                string[] files = System.IO.Directory.GetFiles(Util.ArtifactsDataPath, "*.th44");

                int select = 0;
                foreach (string f in files)
                {
                    if (select % 3 == 0)
                    {
                        string fileName = Path.GetFileName(f);
                        retVal.Add(new object[]
                        {
                            fileName,
                            f
                        });
                    }
                    select++;
                }

                return retVal;
            }
        }


        [DjvuTheory]
        [MemberData(nameof(TH44TestData))]
        public void TH44Chunk_Theory(string fileName, string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                TH44Chunk th = new TH44Chunk(reader, null, null, "TH44", stream.Length);
                Assert.Equal<ChunkType>(ChunkType.TH44, th.ChunkType);
                Assert.Equal(stream.Length, th.Length);

                var image = th.Image;
                Assert.NotNull(image);
                Assert.IsType<PixelMap>(image);
                Assert.True(image.ImageWidth >= 64 && image.ImageWidth < 512);
            }
        }
    }
}