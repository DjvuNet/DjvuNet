using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using DjvuNet.Wavelet;
using System.IO;
using DjvuNet.Tests;

namespace DjvuNet.DataChunks.Tests
{
    public class BM44ChunkTests
    {
        [Fact()]
        public void BM44ChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            BM44Chunk unk = new BM44Chunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.BM44, unk.ChunkType);
            Assert.Equal(ChunkType.BM44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [Fact()]
        public void ChunkTypeTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);
            BM44Chunk unk = new BM44Chunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.BM44, unk.ChunkType);
        }

        [Fact()]
        public void DecodeImageTest()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test001C_P01.fg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                BM44Chunk unk = new BM44Chunk(reader, null, null, null, reader.Length);
                Assert.Equal<ChunkType>(ChunkType.BM44, unk.ChunkType);
                Assert.Equal(ChunkType.BM44.ToString(), unk.Name);
                Assert.Equal<long>(0, unk.DataOffset);
                Assert.Equal<long>(reader.Length, unk.Length);

                IInterWavePixelMap map = unk.DecodeImage();
                Assert.NotNull(map);
            }
        }

        [Fact()]
        public void ImageTest()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test001C_P01.fg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                BM44Chunk unk = new BM44Chunk(reader, null, null, null, reader.Length);
                Assert.Equal<ChunkType>(ChunkType.BM44, unk.ChunkType);
                Assert.Equal(ChunkType.BM44.ToString(), unk.Name);
                Assert.Equal<long>(0, unk.DataOffset);
                Assert.Equal<long>(reader.Length, unk.Length);

                IInterWavePixelMap map = unk.Image;
                Assert.NotNull(map);
                Assert.Same(map, unk.Image);

                var testMap = new InterWavePixelMapDecoder();
                unk.Image = testMap;
                Assert.NotSame(map, unk.Image);
                Assert.Same(testMap, unk.Image);
            }
        }

        [Fact()]
        public void ProgressiveDecodeBackgroundTest()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test001C_P01.fg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                BM44Chunk unk = new BM44Chunk(reader, null, null, null, reader.Length);
                Assert.Equal<ChunkType>(ChunkType.BM44, unk.ChunkType);
                Assert.Equal(ChunkType.BM44.ToString(), unk.Name);
                Assert.Equal<long>(0, unk.DataOffset);
                Assert.Equal<long>(reader.Length, unk.Length);

                var map = new InterWavePixelMapDecoder();
                IInterWavePixelMap result = unk.ProgressiveDecodeBackground(map);
                Assert.NotNull(map);
            }
        }

        [Fact()]
        public void ReadDataTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            BM44Chunk unk = new BM44Chunk(readerMock.Object, null, null, null, 1024);
            Assert.Equal<ChunkType>(ChunkType.BM44, unk.ChunkType);
            Assert.Equal(ChunkType.BM44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            unk.ReadData(reader);
            Assert.Equal<long>(2048, reader.Position);
        }

    }
}
