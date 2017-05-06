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

namespace DjvuNet.DataChunks.Tests
{
    public class PM44ChunkTests
    {
        [Fact()]
        public void PM44ChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            PM44Chunk unk = new PM44Chunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.PM44, unk.ChunkType);
            Assert.Equal<string>(ChunkType.PM44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [Fact()]
        public void DecodeImageTest()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test001C_P01.fg44");
            using (DjvuReader reader = new DjvuReader(file))
            {
                PM44Chunk unk = new PM44Chunk(reader, null, null, null, reader.Length);
                Assert.Equal<ChunkType>(ChunkType.PM44, unk.ChunkType);
                Assert.Equal<string>(ChunkType.PM44.ToString(), unk.Name);
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
            using (DjvuReader reader = new DjvuReader(file))
            {
                PM44Chunk unk = new PM44Chunk(reader, null, null, null, reader.Length);
                Assert.Equal<ChunkType>(ChunkType.PM44, unk.ChunkType);
                Assert.Equal<string>(ChunkType.PM44.ToString(), unk.Name);
                Assert.Equal<long>(0, unk.DataOffset);
                Assert.Equal<long>(reader.Length, unk.Length);

                IInterWavePixelMap map = unk.Image;
                Assert.NotNull(map);
            }
        }

        [Fact()]
        public void ProgressiveDecodeBackgroundTest()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test001C_P01.fg44");
            using (DjvuReader reader = new DjvuReader(file))
            {
                PM44Chunk unk = new PM44Chunk(reader, null, null, null, reader.Length);
                Assert.Equal<ChunkType>(ChunkType.PM44, unk.ChunkType);
                Assert.Equal<string>(ChunkType.PM44.ToString(), unk.Name);
                Assert.Equal<long>(0, unk.DataOffset);
                Assert.Equal<long>(reader.Length, unk.Length);

                InterWavePixelMap map = new InterWavePixelMap();
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

            PM44Chunk unk = new PM44Chunk(readerMock.Object, null, null, null, 1024);
            Assert.Equal<ChunkType>(ChunkType.PM44, unk.ChunkType);
            Assert.Equal<string>(ChunkType.PM44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            unk.ReadData(reader);
            Assert.Equal<long>(2048, reader.Position);
        }
    }
}