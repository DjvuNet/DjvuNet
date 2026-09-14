using Xunit;
using DjvuNet.Wavelet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using DjvuNet;
using DjvuNet.DataChunks;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.Tests;

namespace DjvuNet.Wavelet.Tests
{
    public class InterWavePixelMapTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(-1)]
        [InlineData(255)]
        public void CrCbDelay_Theory(int delay)
        {
            var map = new InterWavePixelMap();
            map.CrCbDelay = delay;
            Assert.Equal(delay, map.CrCbDelay);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CrCbHalf_Theory(bool half)
        {
            var map = new InterWavePixelMap();
            map.CrCbHalf = half;
            Assert.Equal(half, map.CrCbHalf);
        }

        [Fact]
        public void Close_ResetsInternalFields()
        {
            var map = new InterWavePixelMap();
            map.Close();
            Assert.Equal(0, map.Bytes);
            Assert.Equal(0, map.Slices);
            Assert.Equal(0, map.Serial);
        }

        [Fact]
        public void GetPixelMap_NullYMap_ReturnsNull()
        {
            var map = new InterWavePixelMap();
            Assert.Null(map.GetPixelMap());
        }

        [Fact]
        public void GetPixelMap_Dimensions_Throws()
        {
            var map = new InterWavePixelMap();
            map._YMap = new InterWaveMap(30000, 30000); // 900M area is valid for map, but 2.7B pixels exceeds Array.MaxLength
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => map.GetPixelMap());
            Assert.Contains("invalid Data buffer size", ex.Message);
        }

        [Fact]
        public void GetPixelMap_Rect_NullYMap_ReturnsNull()
        {
            var map = new InterWavePixelMap();
            var rect = new Rectangle(0, 0, 100, 100);
            Assert.Null(map.GetPixelMap(1, rect, null));
        }

        [Fact]
        public void GetPixelMap_Rect_Throws()
        {
            var map = new InterWavePixelMap();
            map._YMap = new InterWaveMap(100, 100);
            var rect = new Rectangle(0, 0, 30000, 30000); // Exceeds Array.MaxLength when multiplied by 3
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => map.GetPixelMap(1, rect, null));
            Assert.Contains("invalid Data buffer size", ex.Message);
        }

        public static TheoryData<string, int> BG44PackData
        {
            get
            {
                var data = new TheoryData<string, int>();
                string dataDir = Path.Combine(Util.RepoRoot, "artifacts", "data");
                string[] prefixes = { "test001C_P01", "test002C_P01", "test042C_P01", "test075C_P01" };
                
                foreach (string prefix in prefixes)
                {
                    int chunkCount = Directory.GetFiles(dataDir, $"{prefix}_*.bg44").Length;
                    if (chunkCount > 0)
                    {
                        data.Add(prefix, chunkCount);
                    }
                }
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(BG44PackData))]
        public void GetPixelMap_BG44_SuccessPath(string prefix, int chunkCount)
        {
            string dataDir = Path.Combine(Util.RepoRoot, "artifacts", "data");
            var map = new InterWavePixelMapDecoder();

            for (int i = 0; i < chunkCount; i++)
            {
                string file = Path.Combine(dataDir, $"{prefix}_{i}.bg44");
                using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (IDjvuReader reader = new DjvuReader(fs))
                {
                    BG44Chunk chunk = new BG44Chunk(reader, null, null, "BG44", fs.Length);
                    chunk.Initialize();
                    chunk.ProgressiveDecodeBackground(map);
                }
            }

            PixelMap pixelMap = map.GetPixelMap();
            Assert.NotNull(pixelMap);
            Assert.Equal(map.Width, pixelMap.Width);
            Assert.Equal(map.Height, pixelMap.Height);
            Assert.NotNull(pixelMap.Data);
            
            var rect = new Rectangle(0, 0, map.Width / 2, map.Height / 2);
            PixelMap rectPixelMap = map.GetPixelMap(1, rect, null);
            Assert.NotNull(rectPixelMap);
            Assert.Equal(rect.Width, rectPixelMap.Width);
            Assert.Equal(rect.Height, rectPixelMap.Height);
            Assert.NotNull(rectPixelMap.Data);
        }

        public static TheoryData<string> FG44TestData
        {
            get
            {
                return new TheoryData<string>
                {
                    "test001C_P01.fg44",
                    "test030C_P01.fg44",
                    "test056C_P01.fg44",
                    "test062C_P01.fg44",
                    "test076C_P01.fg44"
                };
            }
        }

        [Theory]
        [MemberData(nameof(FG44TestData))]
        public void GetPixelMap_FG44_SuccessPath(string fileName)
        {
            string file = Path.Combine(Util.RepoRoot, "artifacts", "data", fileName);
            using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (IDjvuReader reader = new DjvuReader(fs))
            {
                FG44Chunk chunk = new FG44Chunk(reader, null, null, "FG44", fs.Length);
                chunk.Initialize();
                var map = (InterWavePixelMapDecoder)chunk.ForegroundImage;

                PixelMap pixelMap = map.GetPixelMap();
                Assert.NotNull(pixelMap);
                Assert.Equal(map.Width, pixelMap.Width);
                Assert.Equal(map.Height, pixelMap.Height);
            }
        }

        [Fact()]
        public void ImageDataTest()
        {
            InterWavePixelMap map = new InterWavePixelMap();
            Assert.True(map.ImageData);
        }

    }
}