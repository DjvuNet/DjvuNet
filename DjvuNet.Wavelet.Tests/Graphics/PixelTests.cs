using Xunit;
using DjvuNet.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace DjvuNet.Graphics.Tests
{
    public class PixelTests
    {
        [Fact()]
        public void PixelTest001()
        {
            Pixel pix = new Pixel();
            Assert.Equal(0, pix.Blue);
            Assert.Equal(0, pix.Green);
            Assert.Equal(0, pix.Red);
        }

        [Fact()]
        public void PixelTest002()
        {
            Pixel pix = new Pixel(0x00ddbbaa);
            Assert.Equal("dd", pix.Blue.ToString("x2"));
            Assert.Equal("bb", pix.Green.ToString("x2"));
            Assert.Equal("aa", pix.Red.ToString("x2"));
        }

        [Fact()]
        public void PixelTest003()
        {
            Pixel pix = new Pixel(47, 89, 111);
            Assert.Equal(47, pix.Blue);
            Assert.Equal(89, pix.Green);
            Assert.Equal(111, pix.Red);
        }

        [Fact()]
        public void PixelTest004()
        {
            Pixel pix = new Pixel(47, 89);
            Assert.Equal(47, pix.Blue);
            Assert.Equal(89, pix.Green);
            Assert.Equal(0, pix.Red);
        }

        [Fact()]
        public void PixelTest005()
        {
            Pixel pix = new Pixel((sbyte)47);
            Assert.Equal(47, pix.Blue);
            Assert.Equal(0, pix.Green);
            Assert.Equal(0, pix.Red);
        }

        [Fact()]
        public void PixelTest006()
        {
            Pixel pix = new Pixel(-1, -1, -1);
            Assert.Equal("ff", pix.Blue.ToString("x2"));
            Assert.Equal("ff", pix.Green.ToString("x2"));
            Assert.Equal("ff", pix.Red.ToString("x2"));
        }

        [Fact()]
        public void PixelTest007()
        {
            Pixel pix = new Pixel(-128, -128, -128);
            Assert.Equal("80", pix.Blue.ToString("x2"));
            Assert.Equal("80", pix.Green.ToString("x2"));
            Assert.Equal("80", pix.Red.ToString("x2"));
        }


        [Fact()]
        public void DuplicateTest()
        {
            Pixel pix1 = new Pixel(-1, 0, -1);
            Pixel pix2 = (Pixel) pix1.Duplicate();
            Assert.Equal<Pixel>(pix1, pix2);
            Assert.NotSame(pix1, pix2);
        }

        [Fact()]
        public void ToStringTest()
        {
            Pixel pix = new Pixel(-112, 54, -125);
            string result = pix.ToString();
        }

        [Fact()]
        public void SizeOfPixelTest()
        {
            int size = Marshal.SizeOf<Pixel>();
            Assert.Equal(3, size);
        }

        [Fact()]
        public void SetBGRTest()
        {
            Pixel pix1 = new Pixel(-1, 0, -1);
            Assert.Equal(-1, pix1.Blue);
            Assert.Equal(0, pix1.Green);
            Assert.Equal(-1, pix1.Red);

            pix1.SetBGR(121, 125, 127);
            Assert.Equal(121, pix1.Blue);
            Assert.Equal(125, pix1.Green);
            Assert.Equal(127, pix1.Red);
        }

        [Fact()]
        public void EqualsTest001()
        {
            Pixel pix1 = new Pixel(-1, 0, -1);
            Pixel pix2 = (Pixel)pix1.Duplicate();
            Assert.True(pix1.Equals((IPixel)pix2));

            Pixel pix3 = new Pixel(-51);
            Assert.False(pix1.Equals((IPixel) pix3));
        }

        [Fact()]
        public void EqualsTest002()
        {
            Pixel pix1 = new Pixel(-1, 0, -1);
            Pixel pix2 = (Pixel)pix1.Duplicate();
            Assert.True(pix1.Equals((object)pix2));
            Assert.False(pix1.Equals((object)null));

            Pixel pix3 = new Pixel(-51);
            Assert.False(pix1.Equals((object)pix3));
        }

        [Fact()]
        public void OpInequality001()
        {
            Pixel pix1 = new Pixel(-1, 0, -1);
            Pixel pix2 = (Pixel)pix1.Duplicate();
            Assert.False(pix1 != pix2);
        }

        [Fact()]
        public void OpInequality002()
        {
            Pixel pix1 = new Pixel(-1, 0, -1);
            Pixel pix2 = new Pixel(-1, 0, -1);
            Assert.False(pix1 != pix2);
        }

        [Fact()]
        public void OpInequality003()
        {
            Pixel pix1 = new Pixel(-1, 0, -1);
            Pixel pix2 = new Pixel(-1, 0, 1);
            Assert.True(pix1 != pix2);
        }

        [Fact()]
        public void SetGrayTest()
        {
            Pixel pix1 = new Pixel(-1, 0, -1);
            Assert.Equal(-1, pix1.Blue);
            Assert.Equal(0, pix1.Green);
            Assert.Equal(-1, pix1.Red);

            pix1.SetGray(-100);
            Assert.Equal(-100, pix1.Blue);
            Assert.Equal(-100, pix1.Green);
            Assert.Equal(-100, pix1.Red);
        }

        [Fact()]
        public void GetHashCodeTest()
        {
            Pixel pix1 = new Pixel(-1, 0, 1);
            string hash = pix1.GetHashCode().ToString("x8");
            Assert.Equal("ff0100ff", hash);
        }

        [Fact()]
        public void CopyFromTest()
        {
            Pixel pix1 = new Pixel(-1, 0, 1);
            Assert.Equal(-1, pix1.Blue);
            Assert.Equal(0, pix1.Green);
            Assert.Equal(1, pix1.Red);

            Pixel pix2 = new Pixel(-51, -51, -51);
            Assert.Equal(-51, pix2.Blue);
            Assert.Equal(-51, pix2.Green);
            Assert.Equal(-51, pix2.Red);

            pix2.CopyFrom(pix1);
            Assert.Equal(-1, pix2.Blue);
            Assert.Equal(0, pix2.Green);
            Assert.Equal(1, pix2.Red);
        }

        [Fact()]
        public void BlueTest()
        {
            Pixel pix1 = new Pixel(-1, 0, 1);
            Assert.Equal(-1, pix1.Blue);
            Assert.Equal(0, pix1.Green);
            Assert.Equal(1, pix1.Red);

            pix1.Blue = 127;
            Assert.Equal(127, pix1.Blue);

            pix1.Blue = 0;
            Assert.Equal(0, pix1.Blue);
        }

        [Fact()]
        public void GreenTest()
        {
            Pixel pix1 = new Pixel(-1, 1, 3);
            Assert.Equal(-1, pix1.Blue);
            Assert.Equal(1, pix1.Green);
            Assert.Equal(3, pix1.Red);

            pix1.Green = 127;
            Assert.Equal(127, pix1.Green);

            pix1.Green = 0;
            Assert.Equal(0, pix1.Green);
        }

        [Fact()]
        public void RedTest()
        {
            Pixel pix1 = new Pixel(-1, 0, 3);
            Assert.Equal(-1, pix1.Blue);
            Assert.Equal(0, pix1.Green);
            Assert.Equal(3, pix1.Red);

            pix1.Red = 127;
            Assert.Equal(127, pix1.Red);

            pix1.Red = 0;
            Assert.Equal(0, pix1.Red);
        }

        [Fact()]
        public void DefaultValueInArrayTest()
        {
            Pixel[] buffer = new Pixel[128];
            for(int i = 0; i < buffer.Length; i++)
            {
                Pixel ipix = buffer[i];
                Assert.Equal<Pixel>(Pixel.BlackPixel, ipix);
            }
        }
    }
}