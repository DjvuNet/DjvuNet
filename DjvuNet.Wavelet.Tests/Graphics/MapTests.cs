using Xunit;
using DjvuNet.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;

namespace DjvuNet.Graphics.Tests
{
    public class MapTests
    {
        [Fact()]
        public void MapTest()
        {
            Mock<Map> mapMock = new Mock<Map> { CallBase = true };
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void FillTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void FillRgbPixelsTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void TranslateTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void RowOffsetTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetRowSizeTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void CreateGPixelReferenceTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void CreateGPixelReferenceTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void PixelRampTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Skip due to AVE in GDI+ - Graphics"), Trait("Category", "Skip")]
        public void ToImageTest001()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            map1.BytesPerPixel = 1;
            using (System.Drawing.Bitmap bmp = map1.ToImage())
            {
                Assert.NotNull(bmp);
                Assert.IsType<System.Drawing.Bitmap>(bmp);
                Assert.Equal(width, bmp.Width);
                Assert.Equal(height, bmp.Height);
            }
        }

        [Fact(Skip = "Skip due to AVE in GDI+ - Graphics"), Trait("Category", "Skip")]
        public void ToImageTest002()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            map1.BytesPerPixel = 2;
            using (System.Drawing.Bitmap bmp = map1.ToImage())
            {
                Assert.NotNull(bmp);
                Assert.IsType<System.Drawing.Bitmap>(bmp);
                Assert.Equal(width, bmp.Width);
                Assert.Equal(height, bmp.Height);
            }
        }

        [Fact()]
        public void ToImageTest008()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            map1.BytesPerPixel = 8;
            using (System.Drawing.Bitmap bmp = map1.ToImage())
            {
                Assert.NotNull(bmp);
                Assert.IsType<System.Drawing.Bitmap>(bmp);
                Assert.Equal(width, bmp.Width);
                Assert.Equal(height, bmp.Height);
            }
        }

        [Fact()]
        public void ToImageTest006()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            map1.BytesPerPixel = 6;
            using (System.Drawing.Bitmap bmp = map1.ToImage())
            {
                Assert.NotNull(bmp);
                Assert.IsType<System.Drawing.Bitmap>(bmp);
                Assert.Equal(width, bmp.Width);
                Assert.Equal(height, bmp.Height);
            }
        }

        [Fact()]
        public void ToImageTest003()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            map1.BytesPerPixel = 3;
            using (System.Drawing.Bitmap bmp = map1.ToImage())
            {
                Assert.NotNull(bmp);
                Assert.IsType<System.Drawing.Bitmap>(bmp);
                Assert.Equal(width, bmp.Width);
                Assert.Equal(height, bmp.Height);
            }
        }

        [Fact()]
        public void ToImageTest004()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            map1.BytesPerPixel = 4;
            using (System.Drawing.Bitmap bmp = map1.ToImage())
            {
                Assert.NotNull(bmp);
                Assert.IsType<System.Drawing.Bitmap>(bmp);
                Assert.Equal(width, bmp.Width);
                Assert.Equal(height, bmp.Height);
            }
        }

        [Fact()]
        public void ToImageTest005()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            map1.BytesPerPixel = 5;
            Assert.Throws<DjvuFormatException>(() => map1.ToImage());
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void CopyDataToBitmapTest()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}
