using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.DataChunks;
using DjvuNet.Graphics;
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
            Assert.Equal<string>(ChunkType.BG44.ToString(), unk.Name);
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
                IWPixelMap map = new IWPixelMap();
                IWPixelMap result = unk.ProgressiveDecodeBackground(map);
                Assert.Same(result, map);
                using (System.Drawing.Bitmap bitmap = map.GetPixelMap().ToImage())
                {
                    string path = Path.Combine(
                        Util.RepoRoot, "artifacts", "data", "dumps",
                        Path.GetFileNameWithoutExtension(file) + "_bg44.png"); 
                    bitmap.Save(path);
                }
            }
        }

        [Fact]
        public void ImageDecodeTest035()
        {
            string file = Path.Combine(Util.RepoRoot, "artifacts", "data", "test035C_P01_0.bg44");
            using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (IDjvuReader reader = new DjvuReader(fs))
            {
                BG44Chunk unk = new BG44Chunk(reader, null, null, "BG44", fs.Length);
                unk.Initialize();
                IWPixelMap map = new IWPixelMap();
                IWPixelMap result = unk.ProgressiveDecodeBackground(map);
                PixelMap pixMap = result.GetPixelMap();
                using(var bmp = pixMap.ToImage())
                {
                    var format = bmp.PixelFormat;
                }
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
            Assert.Equal<string>(ChunkType.BG44.ToString(), unk.Name);
            Assert.Equal<long>(1024, unk.DataOffset);

            unk.ReadData(reader);
            Assert.Equal<long>(2048, reader.Position);
        }
    }
}