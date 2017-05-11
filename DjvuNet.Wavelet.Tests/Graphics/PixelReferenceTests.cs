using Xunit;
using DjvuNet.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Graphics.Tests
{
    public class PixelReferenceTests
    {

        [Fact()]
        public void EqualsPixelReference()
        {
            int width = 16;
            int height = 16;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            IPixelMap map2 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            IPixelMap map3 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.BlackPixel);

            PixelReference pix1 = (PixelReference)map1.CreateGPixelReference(0);
            PixelReference pix2 = (PixelReference)map2.CreateGPixelReference(0);
            PixelReference pix3 = (PixelReference)map3.CreateGPixelReference(0);

            Assert.True(pix1.Equals(pix2));
            Assert.False(pix1.Equals(pix3));
            Assert.False(pix1.Equals((PixelReference)null));
        }

        [Fact()]
        public void EqualsIPixel()
        {
            int width = 16;
            int height = 16;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            IPixelMap map2 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            IPixelMap map3 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.BlackPixel);

            var pix1 = map1.CreateGPixelReference(0);
            var pix2 = map2.CreateGPixelReference(0);
            var pix3 = map3.CreateGPixelReference(0);

            Assert.True(pix1.Equals(pix2.ToPixel()));
            Assert.False(pix1.Equals(pix3.ToPixel()));
            Assert.False(pix1.Equals((IPixel)null));
        }

        [Fact()]
        public void EqualsIPixelReference()
        {
            int width = 16;
            int height = 16;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            IPixelMap map2 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            IPixelMap map3 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.BlackPixel);

            IPixelReference pix1 = map1.CreateGPixelReference(0);
            IPixelReference pix2 = map2.CreateGPixelReference(0);
            IPixelReference pix3 = map3.CreateGPixelReference(0);

            Assert.True(pix1.Equals(pix2));
            Assert.False(pix1.Equals(pix3));
            Assert.False(pix1.Equals((IPixelReference)null));
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void PixelReferenceTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void PixelReferenceTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void SetPixelsTest001()
        {
            int width = 16;
            int height = 16;
            Pixel color = Pixel.BluePixel;
            sbyte scolor = 127;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(scolor);

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            IPixelMap map2 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.GreenPixel);
            IPixelMap map3 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.RedPixel);

            var pix1 = map1.CreateGPixelReference(0);
            var pix2 = map2.CreateGPixelReference(0);
            var pix3 = map3.CreateGPixelReference(0);
            var pix4 = bmp.CreateGPixelReference(0);

            Assert.Equal(scolor, bmp.GetByteAt(8));

            pix4.SetPixels(pix1, 16);
            Assert.Equal(255, bmp.GetByteAt(8));
            Assert.Equal(scolor, bmp.GetByteAt(24));

            pix4.SetPixels(pix2, 16);
            Assert.Equal(0, bmp.GetByteAt(24));
            Assert.Equal(scolor, bmp.GetByteAt(40));

            pix4.SetPixels(pix3, 16);
            Assert.Equal(0, bmp.GetByteAt(40));
            Assert.Equal(scolor, bmp.GetByteAt(56));
        }

        [Fact()]
        public void SetPixelsTest002()
        {
            int width = 16;
            int height = 16;
            Pixel color = Pixel.BlackPixel;
            Pixel spixel = new Pixel(127, 127, 127);
            sbyte scolor = 127;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(scolor);

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.BlackPixel);
            IPixelMap map2 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.GreenPixel);
            IPixelMap map3 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.RedPixel);

            var pix1 = map1.CreateGPixelReference(0);
            var pix2 = map2.CreateGPixelReference(0);
            var pix3 = map3.CreateGPixelReference(0);
            var pix4 = bmp.CreateGPixelReference(0);

            Assert.Equal(color, map1.GetPixelAt(0, 8));

            pix1.SetPixels(pix2, 16);
            Assert.Equal(Pixel.GreenPixel, map1.GetPixelAt(0, 8));
            Assert.Equal(color, map1.GetPixelAt(1, 8));

            pix1.SetPixels(pix3, 16);
            Assert.Equal(Pixel.RedPixel, map1.GetPixelAt(1, 8));
            Assert.Equal(color, map1.GetPixelAt(2, 8));

            pix1.SetPixels(pix4, 16);
            Assert.Equal(spixel, map1.GetPixelAt(2, 8));
            Assert.Equal(color, map1.GetPixelAt(3, 8));
        }

        [Fact()]
        public void SetOffsetTest001()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.RedPixel;

            var map = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            var pix = map.CreateGPixelReference(0);

            for (int i = 0; i < width * height; i++)
            {
                pix.SetOffset(i);
                pix.Blue = unchecked((sbyte) (i % 256));
            }

            for (int i = 0; i < width * height; i++)
            {
                pix.SetOffset(i);
                Assert.Equal(pix.Blue, unchecked((sbyte)(i % 256)));
                Assert.Equal(pix.Green, 0);
                Assert.Equal(pix.Red, -1);
            }
        }

        [Fact()]
        public void SetOffsetTest002()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.RedPixel;

            var map = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            var pix = map.CreateGPixelReference(0);

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    pix.SetOffset(i, j);
                    pix.Blue = unchecked((sbyte)((i*width + j) % 256));
                }
            }

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    pix.SetOffset(i, j);
                    Assert.Equal(pix.Blue, unchecked((sbyte)((i * width + j) % 256)));
                    Assert.Equal(pix.Green, 0);
                    Assert.Equal(pix.Red, -1);
                }
            }
        }

        [Fact()]
        public void Ycc2RgbTest001()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.RedPixel;

            var map = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            map.IsRampNeeded = true;
            var pix = map.CreateGPixelReference(0);
            Assert.Throws<DjvuFormatException>(() => pix.Ycc2Rgb(width * height));
        }

        [Fact()]
        public void Ycc2RgbTest002()
        {
            int width = 32;
            int height = 32;
            sbyte color = -1;

            var map = BitmapTests.CreateIntiFillVerifyBitmap(width, height, 0, color);
            var pix = map.CreateGPixelReference(0);
            Assert.Throws<DjvuFormatException>(() => pix.Ycc2Rgb(width * height));
        }

        [Fact()]
        public void SetBGRTest001()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.RedPixel;

            var map = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);

            PixelReference pix = (PixelReference)map.CreateGPixelReference(0);
            pix.SetOffset(10, 16);

            pix.SetBGR(0xafcfdf);
            Pixel testColor = new Pixel(0xafcfdf);
            Assert.True((Pixel)pix.ToPixel() == testColor);
            pix.SetOffset(10, 15);
            Assert.False((Pixel)pix.ToPixel() == testColor);
            pix.SetOffset(10, 17);
            Assert.False((Pixel)pix.ToPixel() == testColor);
        }

        [Fact()]
        public void DuplicateTest001()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.RedPixel;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(-70);

            PixelReference pix = (PixelReference)bmp.CreateGPixelReference(0);
            pix.SetOffset(10, 16);

            var pix2 = (PixelReference) pix.Duplicate();

            Assert.Equal(pix.Offset, pix2.Offset);
            Assert.Same(pix.Parent, pix2.Parent);
            Assert.Equal(pix.ColorNumber, pix2.ColorNumber);
            Assert.True(pix == pix2);
            pix.SetGray(90);
            pix.IncOffset();
            Assert.True(pix != pix2);
            pix2.IncOffset();
            Assert.True(pix == pix2);
            pix2.SetBGR(0x00ffffff);
            Assert.True((Pixel)pix.ToPixel() == Pixel.WhitePixel);
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void FillRgbPixelsTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void IncOffsetTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void IncOffsetTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void BlueOffsetTest()
        {
            int width = 256;
            int height = 256;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            PixelReference pix = (PixelReference) map1.CreateGPixelReference(0);

            Assert.Equal(0, pix.BlueOffset);
            pix.BlueOffset = 1;
            Assert.Equal(1, pix.BlueOffset);
        }

        [Fact()]
        public void GreenOffsetTest()
        {
            int width = 256;
            int height = 256;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            PixelReference pix = (PixelReference)map1.CreateGPixelReference(0);

            Assert.Equal(1, pix.GreenOffset);
            pix.GreenOffset = 0;
            Assert.Equal(0, pix.GreenOffset);
        }

        [Fact()]
        public void RedOffsetTest()
        {
            int width = 256;
            int height = 256;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            PixelReference pix = (PixelReference)map1.CreateGPixelReference(0);

            Assert.Equal(2, pix.RedOffset);
            pix.RedOffset = 0;
            Assert.Equal(0, pix.RedOffset);
        }

        [Fact()]
        public void ParentTest()
        {
            int width = 256;
            int height = 256;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            IPixelMap map2 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            PixelReference pix = (PixelReference)map1.CreateGPixelReference(0);

            Assert.Same(pix.Parent, map1);
            pix.Parent = map2;
            Assert.NotSame(map1, map2);
            Assert.NotSame(pix.Parent, map1);
            Assert.Same(pix.Parent, map2);
        }

        [Fact()]
        public void OpEqualityTest001()
        {
            int width = 16;
            int height = 16;
            Pixel color = Pixel.BlackPixel;
            Pixel spixel = new Pixel(127, 127, 127);
            sbyte scolor = 127;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(scolor);

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.BlackPixel);
            IPixelMap map2 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.GreenPixel);
            IPixelMap map3 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.RedPixel);

            PixelReference pix1 = (PixelReference) map1.CreateGPixelReference(0);
            PixelReference pix2 = (PixelReference) map2.CreateGPixelReference(0);
            PixelReference pix3 = (PixelReference) map3.CreateGPixelReference(0);
            PixelReference pix4 = (PixelReference) bmp.CreateGPixelReference(0);
            PixelReference pix11 = (PixelReference) pix1.Duplicate();
            PixelReference pix21 = (PixelReference) pix2.Duplicate();
            PixelReference pix31 = (PixelReference) pix3.Duplicate();

            Assert.False(pix1 == pix2);
            Assert.True(pix1 == pix11);
            Assert.False(pix3 == pix2);
            Assert.True(pix2 == pix21);
            Assert.False(pix3 == pix1);
            Assert.True(pix3 == pix31);
            Assert.False(pix3 == (PixelReference) null);
            Assert.False((PixelReference)null == pix3);
        }

        [Fact()]
        public void OpInequalityTest001()
        {
            int width = 16;
            int height = 16;
            Pixel color = Pixel.BlackPixel;
            Pixel spixel = new Pixel(127, 127, 127);
            sbyte scolor = 127;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(scolor);

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.BlackPixel);
            IPixelMap map2 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.GreenPixel);
            IPixelMap map3 = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.RedPixel);

            PixelReference pix1 = (PixelReference)map1.CreateGPixelReference(0);
            PixelReference pix2 = (PixelReference)map2.CreateGPixelReference(0);
            PixelReference pix3 = (PixelReference)map3.CreateGPixelReference(0);
            PixelReference pix4 = (PixelReference)bmp.CreateGPixelReference(0);
            PixelReference pix11 = (PixelReference)pix1.Duplicate();
            PixelReference pix21 = (PixelReference)pix2.Duplicate();
            PixelReference pix31 = (PixelReference)pix3.Duplicate();

            Assert.True(pix1 != pix2);
            Assert.False(pix1 != pix11);
            Assert.True(pix3 != pix2);
            Assert.False(pix2 != pix21);
            Assert.True(pix3 != pix1);
            Assert.False(pix3 != pix31);
            Assert.True(pix3 != (PixelReference)null);
            Assert.True((PixelReference)null != pix3);
        }

        [Fact]
        public void OffsetTest001()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            PixelReference pix = (PixelReference)map1.CreateGPixelReference(0);
            pix.Offset = 3 * width;
            Assert.Equal(3 * width, pix.Offset);
        }
    }
}