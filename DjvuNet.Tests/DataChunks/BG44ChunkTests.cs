using System.Collections.Generic;
using System.IO;
using DjvuNet.Tests;
using DjvuNet.Tests.Xunit;
using DjvuNet.Wavelet;
using Moq;
using Xunit;

namespace DjvuNet.DataChunks.Tests
{
    public class BG44ChunkTests
    {

        public static IEnumerable<object[]> BG44TestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                string dataDir = Path.Combine(Util.RepoRoot, "artifacts", "data");
                DirectoryInfo directory = new DirectoryInfo(dataDir);
                FileInfo[] files = directory.GetFiles("*_0.bg44");

                foreach (FileInfo f in files)
                    retVal.Add(new object[] { f.FullName, f.Length});

                return retVal;
            }
        }

        [Fact()]
        public void BG44ChunkTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup(x => x.Position).Returns(1024);

            BG44Chunk unk = new BG44Chunk(readerMock.Object, null, null, null, 0);
            Assert.Equal<ChunkType>(ChunkType.BG44, unk.ChunkType);
            Assert.Equal(ChunkType.BG44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);
        }

        [DjvuTheory]
        [MemberData(nameof(BG44TestData))]
        public void ProgressiveDecodeBackground_Theory(string file, long length)
        {
            using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (IDjvuReader reader = new DjvuReader(fs))
            {
                BG44Chunk unk = new BG44Chunk(reader, null, null, "BG44", length);
                unk.Initialize();
                var map = new InterWavePixelMapDecoder();
                var result = unk.ProgressiveDecodeBackground(map);
                Assert.Same(result, map);
#if !_APPVEYOR
                string dumpsDir = Path.Combine(Util.ArtifactsDataPath, "dumps");
                if (!System.IO.Directory.Exists(dumpsDir))
                    System.IO.Directory.CreateDirectory(dumpsDir);
                using (System.Drawing.Bitmap image = map.GetPixelMap().ToImage())
                {

                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string outFile = Path.Combine(dumpsDir, fileName + "_bg44.png");
                    using (FileStream stream = new FileStream(outFile, FileMode.Create))
                        image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

                }
#endif
            }
        }

        [Fact]
        public void ImageTest035()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test035C_P01_0.bg44");
            using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (IDjvuReader reader = new DjvuReader(fs))
            {
                BG44Chunk unk = new BG44Chunk(reader, null, null, "BG44", fs.Length);
                unk.Initialize();
                var map = new InterWavePixelMapDecoder();
                IInterWavePixelMap result = unk.BackgroundImage;
                var pixMap = result.GetPixelMap();
                unk.BackgroundImage = map;
                Assert.NotSame(result, unk.BackgroundImage);
                Assert.Same(map, unk.BackgroundImage);
            }
        }

        [Fact()]
        public void ReadDataTest()
        {
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.SetupProperty<long>(x => x.Position);
            IDjvuReader reader = readerMock.Object;
            reader.Position = 1024;

            BG44Chunk unk = new BG44Chunk(readerMock.Object, null, null, null, 1024);
            Assert.Equal<ChunkType>(ChunkType.BG44, unk.ChunkType);
            Assert.Equal(ChunkType.BG44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            unk.ReadData(reader);
            Assert.Equal<long>(2048, reader.Position);
        }
    }
}
