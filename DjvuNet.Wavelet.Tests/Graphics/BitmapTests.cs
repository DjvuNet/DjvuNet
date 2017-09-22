using Xunit;
using DjvuNet.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Tests;
using DjvuNet.Errors;

namespace DjvuNet.Graphics.Tests
{
    public class BitmapTests
    {
        [Fact()]
        public void BitmapTest001()
        {
            Bitmap bmp = new Bitmap();
            Assert.NotNull(bmp);
            Assert.Equal(1, bmp.BytesPerPixel);
            Assert.True(bmp.IsRampNeeded);
            Assert.Equal(0, bmp.Width);
            Assert.Equal(0, bmp.Height);
            Assert.Null(bmp.Data);
        }

        [Fact()]
        public void BitmapTest002()
        {
            int width = 128;
            int height = 128;
            int border = 0;
            Bitmap bmp = new Bitmap(height, width, border);
            Assert.NotNull(bmp);
            Assert.Equal(1, bmp.BytesPerPixel);
            Assert.True(bmp.IsRampNeeded);
            Assert.Equal(width, bmp.Width);
            Assert.Equal(height, bmp.Height);
            Assert.Equal((width + border) * height, bmp.Data.Length);
            Assert.Equal(height, bmp.Rows);
        }

        [Fact()]
        public void BitmapTest003()
        {
            int width = 128;
            int height = 128;
            int border = 0;
            Bitmap bmp = new Bitmap(height, width, border);
            Assert.NotNull(bmp);
            Assert.Equal(1, bmp.BytesPerPixel);
            Assert.True(bmp.IsRampNeeded);
            Assert.Equal(width, bmp.Width);
            Assert.Equal(height, bmp.Height);
            Assert.Equal((width + border) * height, bmp.Data.Length);

            Bitmap test = new Bitmap(bmp);
            Assert.Equal(1, test.BytesPerPixel);
            Assert.True(test.IsRampNeeded);
            Assert.Equal(width, test.Width);
            Assert.Equal(height, test.Height);
            Assert.Equal((width + border) * height, test.Data.Length);
            Assert.Equal(height, test.Rows);
        }

        public static IBitmap CreateVerifyBitmap()
        {
            Bitmap bmp = new Bitmap();
            Assert.NotNull(bmp);
            Assert.Equal(1, bmp.BytesPerPixel);
            Assert.True(bmp.IsRampNeeded);
            Assert.Equal(0, bmp.Width);
            Assert.Equal(0, bmp.Height);
            Assert.Null(bmp.Data);
            return bmp;
        }

        public static IBitmap CreateInitVerifyBitmap(int width, int height, int border)
        {
            IBitmap bmp = CreateVerifyBitmap();
            bmp.Init(height, width, border);
            Assert.Equal(width, bmp.Width);
            Assert.Equal(height, bmp.Height);
            Assert.Equal(border, bmp.Border);
            return bmp;
        }

        public static IBitmap CreateIntiFillVerifyBitmap(int width, int height, int border, sbyte color)
        {
            var bmp = CreateInitVerifyBitmap(width, height, border);
            bmp.Fill(color);
            Assert.Equal(unchecked((byte)color), bmp.GetByteAt(width * height / 2));
            return bmp;
        }

        public static void WriteBitmap(int width, int height, IBitmap bmp)
        {
            string format = "x4";
            for (int i = (height - 1); i >= 0; i--)
            {
                Console.Write($"{(i * width).ToString(format)}  ");
                for (int k = 0; k < width; k++)
                {
                    Console.Write($"{bmp.GetByteAt(i * width + k).ToString("x2")} ");
                }

                Console.WriteLine();
            }
            Console.WriteLine();
        }

        [Fact()]
        public void DuplicateTest()
        {
            int width = 128;
            int height = 128;
            int border = 0;
            sbyte color = -1;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);
            var bmp2 = bmp.Duplicate();

            Assert.NotSame(bmp, bmp2);

            Assert.Equal(width, bmp2.Width);
            Assert.Equal(height, bmp2.Height);
            Assert.Equal(border, bmp2.Border);
            Assert.Equal(unchecked((byte)color), bmp.GetByteAt(width / 2));
            Assert.Equal(bmp.GetByteAt(width/2), bmp2.GetByteAt(width/2));
        }

        [Fact()]
        public void GetBooleanAtTest001()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);

            for(int i = 0; i < width * height; i++)
                bmp.SetByteAt(i, (sbyte)( i % 11 ));

            for(int i = 0; i < width * height; i++)
            {
                if (i % 11 == 0)
                    Assert.True(bmp.GetBooleanAt(i));
                else
                    Assert.False(bmp.GetBooleanAt(i));
            }

        }

        [Fact()]
        public void GraysTest001()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);
            Assert.Throws<DjvuArgumentOutOfRangeException>("value", () => bmp.Grays = 1);
        }

        [Fact()]
        public void GraysTest002()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);
            Assert.Throws<DjvuArgumentOutOfRangeException>("value", () => bmp.Grays = 257);
        }

        [Fact()]
        public void GraysTest003()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);
            bmp.Grays = 101;
            Assert.Equal(101, bmp.Grays);
        }

        [Fact()]
        public void MinimumBorderTest001()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);
            Assert.Equal(width, bmp.GetRowSize());

            bmp.MinimumBorder = 4;
            Assert.Equal(4, bmp.Border);
            Assert.Equal(width + 4, bmp.GetRowSize());
        }

        [Fact()]
        public void SetByteAtTest001()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = -1;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);

            bmp.SetByteAt(7, 0);

            Assert.True(bmp.GetBooleanAt(7));
            Assert.False(bmp.GetBooleanAt(6));
            Assert.False(bmp.GetBooleanAt(8));
        }

        [Fact()]
        public void SetByteAtTest002()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = -11;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);
            bmp.Grays = 256;

            bmp.SetByteAt(7, 127);

            Assert.True(bmp.GetByteAt(7) == 127);
            Assert.False(bmp.GetByteAt(6) == 244);
            Assert.False(bmp.GetByteAt(8) == 244);
        }

        [Fact()]
        public void GetByteAtTest001()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);

            for (int i = 0; i < width * height; i++)
                bmp.SetByteAt(i, (sbyte)(i % 11));

            for (int i = 0; i < width * height; i++)
                Assert.True(bmp.GetByteAt(i) == i % 11);
        }

        [Fact()]
        public void BlitTest001()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color1 = -1;
            sbyte color2 = 4;
            sbyte color3 = 5;
            var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
            bmp1.Grays = 255;
            var bmp2 = CreateIntiFillVerifyBitmap(width / 2, height / 2, border, color3);
            bmp2.Grays = 255;

            bool result = bmp1.Blit(bmp2, width / 2, height / 2, 1);
            Assert.True(result);

            Assert.Equal(unchecked((byte)color1), bmp1.GetByteAt(width * (height / 4) + (width * 3 / 4)));
            Assert.Equal(unchecked((byte)color2), bmp1.GetByteAt(width * (height / 2 + height / 4) + (width * 3 / 4)));
        }

        [Fact()]
        public void BlitTest002()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color1 = -1;
            sbyte color2 = 101;
            var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
            bmp1.Grays = 256;
            var bmp2 = CreateIntiFillVerifyBitmap(width / 2, height / 2, border, color2);
            bmp2.Grays = 256;

            bool result = bmp1.Blit(bmp2, width, height, 2);

            Assert.True(result);
            Assert.Equal(unchecked((byte)255), bmp1.GetByteAt(width * (height / 4) + 10));
            Assert.Equal(unchecked((byte)(147)), bmp1.GetByteAt(width * (height / 2 + 1) + 10));
        }

        [Fact()]
        public void BlitTest003()
        {
            int width = 256;
            int height = 256;
            int border = 0;
            sbyte color1 = -1;
            sbyte color2 = 0;
            var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
            var bmp2 = CreateIntiFillVerifyBitmap(width / 2, height / 2, border, color2);

            bool result = bmp1.Blit(bmp2, 2048, height / 2, 2);
            Assert.False(result);
        }

        [Fact()]
        public void BlitTest004()
        {
            int width = 256;
            int height = 256;
            int border = 0;
            sbyte color1 = -1;
            sbyte color2 = 0;
            var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
            var bmp2 = CreateIntiFillVerifyBitmap(width / 2, height / 2, border, color2);

            bool result = bmp1.Blit(bmp2, width/2, 2048, 2);
            Assert.False(result);
        }

        [Fact()]
        public void BlitTest005()
        {
            int width = 256;
            int height = 256;
            int border = 0;
            sbyte color1 = -1;
            sbyte color2 = 0;
            var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
            var bmp2 = CreateIntiFillVerifyBitmap(width / 2, height / 2, border, color2);

            bool result = bmp1.Blit(bmp2, -1024, height / 2, 2);
            Assert.False(result);
        }

        [Fact()]
        public void BlitTest006()
        {
            int width = 256;
            int height = 256;
            int border = 0;
            sbyte color1 = -1;
            sbyte color2 = 0;
            var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
            var bmp2 = CreateIntiFillVerifyBitmap(width / 2, height / 2, border, color2);

            bool result = bmp1.Blit(bmp2, width / 2, -1024, 2);
            Assert.False(result);
        }

        [Fact()]
        public void RowOffsetTest()
        {
            int width = 16;
            int height = 16;
            int border = 4;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);

            int result = bmp.RowOffset(8);
            Assert.Equal(width * 8 + 8 * 4 + 4, result);
        }

        [Fact()]
        public void GetRowSizeTest()
        {
            int width = 16;
            int height = 16;
            int border = 4;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);

            int result = bmp.GetRowSize();
            Assert.Equal(width + border, result);
        }

        [Fact()]
        public void FillTest001()
        {
            var bmp = new Bitmap();
            Assert.NotNull(bmp);
            Assert.True(bmp.IsRampNeeded);

            bmp.Init(128, 128, 0);
            Assert.Equal(128, bmp.Width);
            Assert.Equal(128, bmp.Height);
            Assert.Equal(0, bmp.Border);

            bmp.Fill(-1);
            Assert.Equal(255, bmp.GetByteAt(64));

            bmp.Fill(1);
            Assert.Equal(1, bmp.GetByteAt(64));
        }

        [Fact()]
        public void FillTest002()
        {

            int width = 16;
            int height = 16;
            int border = 4;
            sbyte color1 = 0;
            sbyte color2 = -1;
            var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
            var bmp2 = CreateIntiFillVerifyBitmap(width, height, border, color2);

            bmp1.Fill(bmp2, 4, 4);

            IPixelReference pix1 = bmp1.CreateGPixelReference(0);
            pix1.SetOffset(5, 5);

            Assert.Equal(color2, pix1.ToPixel().Blue);

            pix1.SetOffset(3, 5);
            Assert.Equal(color1, pix1.ToPixel().Blue);
        }

        [Fact()]
        public void InsertMapTest()
        {
            var bmp = new Bitmap();
            Assert.NotNull(bmp);
            Assert.True(bmp.IsRampNeeded);

            bmp.Init(128, 128, 0);
            Assert.Equal(128, bmp.Width);
            Assert.Equal(128, bmp.Height);
            Assert.Equal(0, bmp.Border);

            bmp.Fill(-1);
            Assert.Equal(255, bmp.GetByteAt(64));

            var bmp2 = new Bitmap();
            Assert.NotNull(bmp2);
            Assert.True(bmp2.IsRampNeeded);

            bmp2.Init(256, 256, 0);
            Assert.Equal(256, bmp2.Width);
            Assert.Equal(256, bmp2.Height);
            Assert.Equal(0, bmp2.Border);

            bmp2.Fill(127);
            Assert.Equal(127, bmp2.GetByteAt(192));

            bmp2.InsertMap(bmp, 128, 0, false);
            Assert.Equal(255, bmp2.GetByteAt(192));
        }

        [Fact()]
        public void InitTest001()
        {
            Bitmap bmp = new Bitmap();
            Assert.NotNull(bmp);
            Assert.True(bmp.IsRampNeeded);

            bmp.Init(128, 128, 0);
            Assert.Equal(128, bmp.Width);
            Assert.Equal(128, bmp.Height);
            Assert.Equal(0, bmp.Border);
        }

        [Fact()]
        public void InitTest002()
        {
            Bitmap bmp = new Bitmap();
            Assert.NotNull(bmp);
            Assert.True(bmp.IsRampNeeded);

            bmp.Init(128, 128, 0);
            Assert.Equal(128, bmp.Width);
            Assert.Equal(128, bmp.Height);
            Assert.Equal(0, bmp.Border);

            bmp.Fill(-1);
            Assert.Equal(255, bmp.GetByteAt(64));

            Bitmap testBmp = new Bitmap();
            testBmp.Init(bmp, 0);

            Assert.NotSame(bmp, testBmp);
            Assert.Equal(bmp.Width, testBmp.Width);
            Assert.Equal(bmp.Height, testBmp.Height);
            Assert.Equal(bmp.GetByteAt(64), testBmp.GetByteAt(64));
        }

        [Fact()]
        public void InitTest003()
        {
            Bitmap bmp = new Bitmap();
            Assert.NotNull(bmp);
            Assert.True(bmp.IsRampNeeded);

            bmp.Init(128, 128, 0);
            Assert.Equal(128, bmp.Width);
            Assert.Equal(128, bmp.Height);
            Assert.Equal(0, bmp.Border);

            bmp.Fill(-1);
            Assert.Equal(255, bmp.GetByteAt(64));

            var bmp2 = bmp.Init(bmp, new Rectangle(0, 0, 128, 128), 0);
            Assert.Same(bmp2, bmp);
        }

        [Fact()]
        public void TranslateTest()
        {
            Bitmap bmp = new Bitmap();
            Assert.NotNull(bmp);
            Assert.True(bmp.IsRampNeeded);

            bmp.Init(128, 128, 0);
            Assert.Equal(128, bmp.Width);
            Assert.Equal(128, bmp.Height);
            Assert.Equal(0, bmp.Border);

            bmp.Fill(-1);
            Assert.Equal(255, bmp.GetByteAt(64));

            Bitmap bmp2 = new Bitmap();
            Assert.NotNull(bmp2);
            Assert.True(bmp2.IsRampNeeded);

            bmp2.Init(256, 256, 0);
            Assert.Equal(256, bmp2.Width);
            Assert.Equal(256, bmp2.Height);
            Assert.Equal(0, bmp2.Border);

            bmp2.Fill(127);
            Assert.Equal(127, bmp2.GetByteAt(192));

            Bitmap bmp3 = (Bitmap) bmp.Translate(64, 64, bmp2);
            Assert.Equal(128, bmp3.Width);
            Assert.Equal(128, bmp3.Height);
            Assert.Equal(0, bmp3.GetByteAt(96));
            Assert.Equal(255, bmp3.GetByteAt(32));
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void PixelRampTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ComputeBoundingBoxTest()
        {

        }
    }
}
