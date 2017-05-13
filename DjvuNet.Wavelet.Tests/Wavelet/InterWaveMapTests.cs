using Xunit;
using DjvuNet.Wavelet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DjvuNet.Tests;
using DjvuNet.Graphics;
using DjvuNet.Errors;

namespace DjvuNet.Wavelet.Tests
{
    public class InterWaveMapTests
    {
        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void InterWaveMapTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void DuplicateTest001()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C_P01_0.bg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                var map = new InterWavePixelMapDecoder();
                map.Decode(reader);
                Assert.NotNull(map._YMap);
                Assert.NotNull(map._YDecoder);
                Assert.NotNull(map._CbMap);
                Assert.NotNull(map._CbDecoder);
                Assert.NotNull(map._CrMap);
                Assert.NotNull(map._CrDecoder);

                var dyMap = map._YMap.Duplicate();
                Assert.NotNull(dyMap);
                Assert.Equal(map._YMap.Width, dyMap.Width);
                Assert.Equal(map._YMap.Height, dyMap.Height);
                Assert.Equal(map._YMap.Blocks.Length, dyMap.Blocks.Length);
            }
        }

        [Fact()]
        public void DuplicateTest002()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C_P01_0.bg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                var map = new InterWavePixelMapDecoder();
                map.Decode(reader);
                Assert.NotNull(map._YMap);
                Assert.NotNull(map._YDecoder);
                Assert.NotNull(map._CbMap);
                Assert.NotNull(map._CbDecoder);
                Assert.NotNull(map._CrMap);
                Assert.NotNull(map._CrDecoder);

                map._YMap.Blocks = null;
                var dyMap = map._YMap.Duplicate();
                Assert.Null(dyMap);
            }
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void BackwardTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void BackwardFilterTest001()
        {
            InterWaveMap map = new InterWaveMap();
            Assert.Throws<DjvuFormatException>(() => InterWaveMap.BackwardFilter(null, 0, 10, 16, 9, 0));
        }

        [Fact()]
        public void BackwardFilterTest002()
        {
            InterWaveMap map = new InterWaveMap();
            Assert.Throws<DjvuFormatException>(() => InterWaveMap.BackwardFilter(null, 0, 10, 16, 17, 0));
        }

        [Fact()]
        public void GetBucketCountTest()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C_P01_0.bg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                var map = new InterWavePixelMapDecoder();
                map.Decode(reader);
                Assert.NotNull(map._YMap);
                Assert.NotNull(map._YDecoder);
                Assert.NotNull(map._CbMap);
                Assert.NotNull(map._CbDecoder);
                Assert.NotNull(map._CrMap);
                Assert.NotNull(map._CrDecoder);

                int result = map._YMap.GetBucketCount();
                Assert.Equal(910, result);
            }
        }

        [Fact()]
        public void ImageTest001()
        {
            int width = 32;
            int height = 32;

            int subsample = 24;

            Rectangle rect = new Rectangle
            {
                Left = width,
                Top = height
            };

            InterWaveMap map = new InterWaveMap(width, height);
            Assert.Throws<DjvuArgumentOutOfRangeException>(
                () => map.Image(subsample, rect, 1, new sbyte[width * height], width, 1, true));
        }

        [Fact()]
        public void ImageTest002()
        {
            int width = 32;
            int height = 32;

            int subsample = 4;

            Rectangle rect = new Rectangle
            {
                Left = 0,
                Top = height
            };

            InterWaveMap map = new InterWaveMap(width, height);
            Assert.Throws<DjvuArgumentException>(
                () => map.Image(subsample, rect, 1, new sbyte[width * height], width, 1, true));
        }

        [Fact()]
        public void ImageTest003()
        {
            int width = 32;
            int height = 32;

            int subsample = 4;

            Rectangle rect = new Rectangle
            {
                Right = -3,
                Left = width - 3,
                Top = height
            };

            InterWaveMap map = new InterWaveMap(width, height);
            Assert.Throws<DjvuArgumentException>(
                () => map.Image(subsample, rect, 1, new sbyte[width * height], width, 1, true));
        }

        [Fact()]
        public void ImageTest004()
        {
            int width = 32;
            int height = 32;

            int subsample = 4;

            Rectangle rect = new Rectangle
            {
                Right = 0,
                Left = width,
                Bottom = -3,
                Top = height - 3
            };

            InterWaveMap map = new InterWaveMap(width, height);
            Assert.Throws<DjvuArgumentException>(
                () => map.Image(subsample, rect, 1, new sbyte[width * height], width, 1, true));
        }

        [Fact()]
        public void ImageTest005()
        {
            int width = 32;
            int height = 32;

            Rectangle rect = new Rectangle
            {
                Right = 0,
                Left = width,
                Bottom = 0,
                Top = height
            };

            InterWaveMap map = new InterWaveMap(width, height);
            map.Image(0, new sbyte[width * height], width, 1, true);
        }

        [Fact()]
        public void ImageTest006()
        {
            int width = 32;
            int height = 32;
            int subsample = 1;

            Rectangle rect = new Rectangle
            {
                Right = 0,
                Left = width,
                Bottom = 0,
                Top = height
            };

            InterWaveMap map = new InterWaveMap(width, height);
            map.Image(subsample, rect, 1, new sbyte[width * height * 3], width, 1, true);
        }

        [Fact()]
        public void ImageTest007()
        {
            int width = 32;
            int height = 32;
            int subsample = 1;

            Rectangle rect = new Rectangle
            {
                Right = 0,
                Left = width,
                Bottom = 0,
                Top = height
            };

            InterWaveMap map = new InterWaveMap(width, height);
            map.Image(subsample, rect, 1, new sbyte[width * height + 1], width, 1, true);
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void CreateTest()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}
