using System;
using System.Drawing;
using Xunit;
using DjvuNet.Graphics;
using TUtil = DjvuNet.Tests.Util;
using DjvuNet.Errors;
using System.Diagnostics;

namespace DjvuNet.Graphics.Tests
{

    public class PixelMapTests
    {

        int shdWidth = 1920 * 2;
        int shdHeight = 1080 * 2;
        int shdBytesPerPixel = 4;
        int testCount = 1;

        public static sbyte[] GetRandomData(int width, int height, int bytesPerPixel)
        {
            long length = width * height * bytesPerPixel;
            sbyte[] data = new sbyte[length];
            Random rnd = new Random();
            for (int i = 0; i < data.Length; i++)
            {
                byte number = (byte)rnd.Next(256);
                data[i] = unchecked((sbyte)number);
            }

            return data;
        }

        public static void WritePixelMap(int width, int height, IPixelMap bmp)
        {
            string formatIndex = "x4";
            string formatPixel = "x2";
            var pix = bmp.CreateGPixelReference(0);
            var colorNum = pix.ColorNumber;

            for (int i = (height - 1); i >= 0; i--)
            {
                Console.Write($"{(i * width).ToString(formatIndex)}  ");
                for (int k = 0; k < width; k++)
                {
                    int rowOffset = i * width;
                    int columnOffset = k ;
                    pix.SetOffset(rowOffset + columnOffset);
                    Console.Write($"{pix.Blue.ToString(formatPixel)}");
                    Console.Write($"{pix.Green.ToString(formatPixel)}");
                    Console.Write($"{pix.Red.ToString(formatPixel)}  ");

                }

                Console.WriteLine();
            }
            Console.WriteLine();
        }

        public static IPixelMap CreateVerifyPixelMap()
        {
            var map = new PixelMap();
            Assert.Equal(3, map.BytesPerPixel);
            Assert.False(map.IsRampNeeded);
            Assert.Equal(0, map.Width);
            Assert.Equal(0, map.Height);
            return map;
        }

        public static IPixelMap CreateInitVerifyPixelMap(int width, int height, IPixel color)
        {
            IPixelMap map = CreateVerifyPixelMap();
            map.Init(height, width, color);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);
            Assert.Equal<IPixel>(color, map.CreateGPixelReference(width / 2).ToPixel());

            var pix = map.CreateGPixelReference(width / 2);
            Assert.True(color.Equals(pix.ToPixel()));
            return map;
        }


        [Fact]
        public void GetColorCorrection001()
        {
            int[] correctionTable = PixelMap.GetGammaCorrection(1.2);
            Assert.NotNull(correctionTable);
            Assert.Equal(correctionTable.Length, 256);
        }

        [Fact]
        public void GetColorCorrection002()
        {
            Assert.Throws<DjvuArgumentOutOfRangeException>("gamma", () => PixelMap.GetGammaCorrection(0.099));
        }

        [Fact]
        public void GetColorCorrection003()
        {
            Assert.Throws<DjvuArgumentOutOfRangeException>("gamma", () => PixelMap.GetGammaCorrection(10.01));
        }

        [Fact]
        public void GetColorCorrection004()
        {
            int[] correction = PixelMap.GetGammaCorrection(1.0000);
            Assert.Same(PixelMap.IdentityGammaCorr, correction);
        }

        [Fact]
        public void GetColorCorrection005()
        {
            double gamma = 2.200000000000;
            int[] correction = PixelMap.GetGammaCorrection(gamma);

            int[] correction2 = PixelMap.GetGammaCorrection(gamma);
            Assert.Same(correction, correction2);
            Assert.Same(PixelMap.CachedGammaTable, correction);
        }

        [Fact()]
        public void PixelMapTest()
        {
            PixelMap map = new PixelMap();
            Assert.Equal(3, map.BytesPerPixel);
            Assert.False(map.IsRampNeeded);
            Assert.Equal(0, map.Width);
            Assert.Equal(0, map.Height);
        }

        [Fact()]
        public void AttenuateTest001()
        {
            int width = 512;
            int height = 512;
            Pixel color = Pixel.RedPixel;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(-1);
            bmp.Grays = 127;

            var map = CreateInitVerifyPixelMap(width, height, color);
            map.Attenuate(bmp, 0, 0);

            Assert.Equal(width, bmp.Width);
            Assert.Equal(height, bmp.Height);
            Assert.Equal(255, bmp.GetByteAt(256));
        }

        [Fact()]
        public void AttenuateTest002()
        {
            int width = 512;
            int height = 512;
            sbyte bColor = 127;
            Pixel color = Pixel.RedPixel;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(bColor);
            bmp.Grays = 256;

            var map = CreateInitVerifyPixelMap(width, height, color);
            map.Attenuate(bmp, 16, 16);

            Assert.Equal(width, bmp.Width);
            Assert.Equal(height, bmp.Height);
            Assert.Equal(bColor, bmp.GetByteAt(256));
        }

        [Fact()]
        public void AttenuateTest003()
        {
            int width = 512;
            int height = 512;
            sbyte bColor = 127;
            Pixel color = Pixel.RedPixel;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(bColor);
            bmp.Grays = 256;

            var map = CreateInitVerifyPixelMap(width, height, color);
            map.Attenuate(bmp, -512, 16);

            Assert.Equal(width, bmp.Width);
            Assert.Equal(height, bmp.Height);
            Assert.Equal(bColor, bmp.GetByteAt(256));
        }

        [Fact()]
        public void AttenuateTest004()
        {
            int width = 512;
            int height = 512;
            sbyte bColor = 127;
            Pixel color = Pixel.RedPixel;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(bColor);
            bmp.Grays = 256;

            var map = CreateInitVerifyPixelMap(width, height, color);
            map.Attenuate(bmp, 16, -512);

            Assert.Equal(width, bmp.Width);
            Assert.Equal(height, bmp.Height);
            Assert.Equal(bColor, bmp.GetByteAt(256));
        }

        [Fact()]
        public void BlitTest01()
        {
            int width = 512;
            int height = 512;
            Pixel color = Pixel.RedPixel;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(0);

            var map = CreateInitVerifyPixelMap(width, height, color);

            map.Blit(bmp, 256, 1, Pixel.BlackPixel);

            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            var pix = map.CreateGPixelReference(128);
            Assert.True(color.Equals(pix.ToPixel()));

            var pix2 = map.CreateGPixelReference(384);
            Assert.True(color.Equals(pix2.ToPixel()));

            var pix3 = map.CreateGPixelReference(512 * 3 + 384);
            Assert.True(color.Equals(pix3.ToPixel()));
        }

        [Fact()]
        public void BlitTest02()
        {
            int width = 512;
            int height = 512;
            Pixel color = Pixel.RedPixel;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(0);

            var map = CreateInitVerifyPixelMap(width, height, Pixel.BlackPixel);

            map.Blit(bmp, 256, 1, null);
        }

        [Fact()]
        public void BlitTest03()
        {
            int width = 512;
            int height = 512;
            Pixel color = Pixel.RedPixel;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(0);

            var map = CreateInitVerifyPixelMap(2 * width, 2 * height, color);

            map.Blit(bmp, 256, 1, Pixel.WhitePixel);
        }

        [Fact()]
        public void BlitTest04()
        {
            int width = 512;
            int height = 512;
            Pixel color = Pixel.RedPixel;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(0);

            var map = CreateInitVerifyPixelMap(2 * width, 2 * height, color);

            map.Blit(bmp, -512, 1, Pixel.WhitePixel);
        }

        [Fact()]
        public void BlitTest05()
        {
            int width = 512;
            int height = 512;
            Pixel color = Pixel.RedPixel;

            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0);
            bmp.Fill(0);

            var map = CreateInitVerifyPixelMap(2 * width, 2 * height, color);

            map.Blit(bmp, 256, -512, Pixel.WhitePixel);
        }

        [Fact()]
        public void ApplyGammaCorrectionTest001()
        {
            double g = 2.90000000;
            var map = CreateInitVerifyPixelMap(256, 256, Pixel.GreenPixel);
            var pixRef = map.CreateGPixelReference(0, 128);
            var pix = pixRef.ToPixel();
            map.ApplyGammaCorrection(g);
            var pixAfterGamma = pixRef.ToPixel();
            int[] gammaTable = PixelMap.GetGammaCorrection(g);

            Assert.Equal<byte>(unchecked((byte)pixAfterGamma.Blue), (byte) gammaTable[unchecked((byte)pix.Blue)]);
            Assert.Equal<byte>(unchecked((byte)pixAfterGamma.Green), (byte)gammaTable[unchecked((byte)pix.Green)]);
            Assert.Equal<byte>(unchecked((byte)pixAfterGamma.Red), (byte)gammaTable[unchecked((byte)pix.Red)]);
        }

        [Fact()]
        public void ApplyGammaCorrectionTest002()
        {
            var map = CreateInitVerifyPixelMap(256, 256, Pixel.GreenPixel);
            var pixRef = map.CreateGPixelReference(0, 128);
            var pix = pixRef.ToPixel();
            map.ApplyGammaCorrection(1.00000000);
            var pixAfterGamma = pixRef.ToPixel();
            Assert.Equal<IPixel>(pix, pixAfterGamma);
        }

        [Fact()]
        public void BenchmarkApplyGammaCorrection()
        {
            sbyte[] data = GetRandomData(shdWidth, shdHeight, shdBytesPerPixel);
            
            long ticks = 0;
            Stopwatch watch = new Stopwatch();

            for (int i = 0; i < testCount; i++)
            {
                sbyte[] testData = new sbyte[data.Length];
                Buffer.BlockCopy(data, 0, testData, 0, data.Length);

                watch.Restart();
                PixelMap.ApplyGamma(2.2, testData);
                watch.Stop();
                ticks += watch.ElapsedMilliseconds;
            }

            Console.WriteLine($"ApplyGammaCorrection ms per call\t\t{((double)ticks / testCount).ToString("0#.000")}");

        }

        [Fact(Skip = "Fails in AppVeyor"), Trait("Category", "Skip")]
        public void BenchmarkApplyGammaCorrectionFastMT()
        {
            sbyte[] data = GetRandomData(shdWidth, shdHeight, shdBytesPerPixel);
            long ticks = 0;
            Stopwatch watch = new Stopwatch();

            for (int i = 0; i < testCount; i++)
            {
                sbyte[] testData = new sbyte[data.Length];
                Buffer.BlockCopy(data, 0, testData, 0, data.Length);

                watch.Restart();
                PixelMap.ApplyGammaFastMT(2.2, testData);
                watch.Stop();
                ticks += watch.ElapsedMilliseconds;
            }

            Console.WriteLine($"ApplyGammaCorrectionFastMT ms per call\t\t{((double)ticks / testCount).ToString("0#.000")}");
        }

        [Fact()]
        public void BenchmarkApplyGammaCorrectionFastST()
        {
            sbyte[] data = GetRandomData(shdWidth, shdHeight, shdBytesPerPixel);
            long ticks = 0;
            Stopwatch watch = new Stopwatch();

            for (int i = 0; i < testCount; i++)
            {
                sbyte[] testData = new sbyte[data.Length];
                Buffer.BlockCopy(data, 0, testData, 0, data.Length);

                watch.Restart();
                PixelMap.ApplyGammaFastST(2.2, testData);
                watch.Stop();
                ticks += watch.ElapsedMilliseconds;
                //GC.Collect();
            }

            Console.WriteLine($"ApplyGammaCorrectionFastST ms per call\t\t{((double)ticks/ testCount).ToString("0#.000")}");
        }

        [Fact()]
        public void DownsampleTest001()
        {
            int width = 32;
            int height = 32;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            map.Downsample(map, 1, null);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);
        }

        [Fact()]
        public void DownsampleTest002()
        {
            int width = 32;
            int height = 32;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            map.Downsample(map, 1, map.BoundingRectangle);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);
        }

        [Fact()]
        public void DownsampleTest003()
        {
            int width = 32;
            int height = 32;
            int subsample = 2;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            map.Downsample(map, subsample, null);
            Assert.Equal(width/subsample, map.Width);
            Assert.Equal(height/subsample, map.Height);
        }

        [Fact()]
        public void DownsampleTest004()
        {
            int width = 128;
            int height = 128;
            int subsample = 4;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            map.Downsample(map, subsample, null);
            Assert.Equal(width / subsample, map.Width);
            Assert.Equal(height / subsample, map.Height);
        }

        [Fact()]
        public void DownsampleTest005()
        {
            int width = 160;
            int height = 160;
            int subsample = 8;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            map.Downsample(map, subsample, null);
            Assert.Equal(width / subsample, map.Width);
            Assert.Equal(height / subsample, map.Height);
        }

        [Fact()]
        public void DownsampleTest006()
        {
            int width = 512;
            int height = 512;
            int subsample = 12;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            map.Downsample(map, subsample, null);
            Assert.Equal(Math.Round((double)width / subsample, 0), map.Width);
            Assert.Equal(Math.Round((double)height / subsample, 0), map.Height);
        }

        [Fact()]
        public void DownsampleTest007()
        {
            int width = 512;
            int height = 512;
            int subsample = 11;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            map.Downsample(map, subsample, null);
            Assert.Equal(Math.Round((double)width / subsample, 0), map.Width);
            Assert.Equal(Math.Round((double)height / subsample, 0), map.Height);
        }

        [Fact()]
        public void DownsampleTest008()
        {
            int width = 110;
            int height = 110;
            int subsample = 11;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            Rectangle rect = new Rectangle
            {
                Left = 0,
                Bottom = 0,
                Right = width,
                Top = height,
            };

            map.Downsample(map, subsample, rect);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);
        }

        [Fact()]
        public void DownsampleTest009()
        {
            int width = 32;
            int height = 32;
            int subsample = 4;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            Rectangle rect = new Rectangle
            {
                Left = 0,
                Bottom = 0,
                Right = width / 2,
                Top = height,
            };

            Assert.Throws<DjvuArgumentOutOfRangeException>("targetRect", () => map.Downsample(map, subsample, rect));
        }

        [Fact()]
        public void DownsampleTest010()
        {
            int width = 32;
            int height = 32;
            int subsample = 4;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            Rectangle rect = new Rectangle
            {
                Left = 0,
                Bottom = -32,
                Right = width,
                Top = height,
            };

            Assert.Throws<DjvuArgumentOutOfRangeException>("targetRect", () => map.Downsample(map, subsample, rect));
        }

        [Fact()]
        public void DownsampleTest011()
        {
            int width = 32;
            int height = 32;
            int subsample = 4;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            Rectangle rect = new Rectangle
            {
                Left = 32,
                Bottom = 0,
                Right = width,
                Top = height,
            };

            Assert.Throws<DjvuArgumentOutOfRangeException>("targetRect", () => map.Downsample(map, subsample, rect));
        }

        [Fact()]
        public void DownsampleTest012()
        {
            int width = 32;
            int height = 32;
            int subsample = 4;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            Rectangle rect = new Rectangle
            {
                Left = 0,
                Bottom = 0,
                Right = width,
                Top = height * 2,
            };

            Assert.Throws<DjvuArgumentOutOfRangeException>("targetRect", () => map.Downsample(map, subsample, rect));
        }

        [Fact()]
        public void DownsampleTest014()
        {
            int width = 512;
            int height = 512;
            int subsample = 11;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            map.IsRampNeeded = true;
            map.Downsample(map, subsample, null);
            Assert.Equal(Math.Round((double)width / subsample, 0), map.Width);
            Assert.Equal(Math.Round((double)height / subsample, 0), map.Height);
        }


        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void Downsample43Test001()
        {
            var map = CreateInitVerifyPixelMap(512, 512, Pixel.BluePixel);
            var map2 = CreateInitVerifyPixelMap(1024, 1024, Pixel.GreenPixel);
            map.Downsample43(map2, map.BoundingRectangle);
            Assert.Equal(map.Width, 512);
            Assert.Equal(map.Height, 512);
            Assert.Equal(map.CreateGPixelReference(256).ToPixel(), Pixel.GreenPixel);
        }

        [Fact()]
        public void Downsample43Test002()
        {
            var map = CreateInitVerifyPixelMap(512, 512, Pixel.BluePixel);
            var map2 = CreateInitVerifyPixelMap(1024, 1024, Pixel.GreenPixel);
            Assert.Throws<DjvuArgumentOutOfRangeException>("targetRect", 
                () => map.Downsample43(map2, new Rectangle { Left = 0, Bottom = 0, Top = -100, Right = -2048}));
        }

        [Fact]
        public void FillTest001()
        {
            int width = 16;
            int height = 16;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.GreenPixel);

            var pix = map.CreateGPixelReference(7);
            Assert.Equal<IPixel>(Pixel.GreenPixel, pix.ToPixel());

            var map2 = CreateInitVerifyPixelMap(width, height, Pixel.BluePixel);

            var pix2 = map2.CreateGPixelReference(7);
            Assert.Equal<IPixel>(Pixel.BluePixel, pix2.ToPixel());

            map.Fill(map2, 8, 8);

            var pix3 = map.CreateGPixelReference(0);
            pix3.SetOffset(8, 12);            
            Assert.Equal(pix3.ToPixel().ToString(), Pixel.BluePixel.ToString());

            var pix4 = map.CreateGPixelReference(0);
            pix4.SetOffset(4, 12);
            Assert.Equal(Pixel.GreenPixel.ToString(), pix4.ToPixel().ToString());

        }

        [Fact]
        public void FillTest002()
        {
            int width = 16;
            int height = 16;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.GreenPixel);

            var pix = map.CreateGPixelReference(7);
            Assert.Equal<IPixel>(Pixel.GreenPixel, pix.ToPixel());

            var map2 = CreateInitVerifyPixelMap(width, height, Pixel.BluePixel);

            var pix2 = map2.CreateGPixelReference(7);
            Assert.Equal<IPixel>(Pixel.BluePixel, pix2.ToPixel());

            map.IsRampNeeded = true;
            map.Fill(map2, 8, 8);

            var pix3 = map.CreateGPixelReference(0);
            pix3.SetOffset(8, 12);
            Assert.Equal(pix3.ToPixel().ToString(), Pixel.BluePixel.ToString());

            var pix4 = map.CreateGPixelReference(0);
            pix4.SetOffset(4, 12);
            Assert.Equal(Pixel.GreenPixel.ToString(), pix4.ToPixel().ToString());

        }

        [Fact()]
        public void InitTest001()
        {
            int width = 256;
            int height = 256;
            Pixel color = Pixel.GreenPixel;

            var map = CreateVerifyPixelMap();
            map.Init(height, width, color);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            var pix = map.CreateGPixelReference(width / 2);
            Assert.True(color.Equals(pix.ToPixel()));
        }

        [Fact()]
        public void InitTest002()
        {
            int width = 256;
            int height = 256;
            Pixel color = Pixel.GreenPixel;

            var map = CreateVerifyPixelMap();
            var source = CreateInitVerifyPixelMap(width, height, color);
            map.Init(source);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            var pix = map.CreateGPixelReference(width / 2);
            Assert.True(color.Equals(pix.ToPixel()));
        }

        [Fact()]
        public void InitTest002a()
        {
            int width = 256;
            int height = 256;
            Pixel color = Pixel.GreenPixel;

            var map = CreateVerifyPixelMap();
            var source = CreateInitVerifyPixelMap(width, height, color);
            map.IsRampNeeded = true;
            map.Init(source);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            var pix = map.CreateGPixelReference(width / 2);
            Assert.True(color.Equals(pix.ToPixel()));
        }

        [Fact()]
        public void InitTest003()
        {
            int width = 256;
            int height = 256;
            Pixel color = Pixel.GreenPixel;

            var map = CreateVerifyPixelMap();
            var source = CreateInitVerifyPixelMap(width, height, color);

            int rectWidth = 100;
            int right = 101;
            int rectHeight = 102;

            map.Init(source, new Rectangle(right, 0, rectWidth, rectHeight));
            Assert.Equal(rectWidth, map.Width);
            Assert.Equal(rectHeight, map.Height);

            var pix = map.CreateGPixelReference(width / 2);
            Assert.True(color.Equals(pix.ToPixel()));
        }

        [Fact()]
        public void InitTest004()
        {
            int width = 256;
            int height = 256;
            Pixel color = Pixel.GreenPixel;

            var map = CreateVerifyPixelMap();
            var source = CreateInitVerifyPixelMap(width, height, color);

            int rectWidth = 100;
            int right = 101;
            int rectHeight = 102;

            map.IsRampNeeded = true;
            map.Init(source, new Rectangle(right, 0, rectWidth, rectHeight));
            Assert.Equal(rectWidth, map.Width);
            Assert.Equal(rectHeight, map.Height);

            var pix = map.CreateGPixelReference(width / 2);
            Assert.True(color.Equals(pix.ToPixel()));
        }

        [Fact()]
        public void InitTest005()
        {
            int width = 256;
            int height = 256;
            Pixel color = Pixel.GreenPixel;

            var map = CreateVerifyPixelMap();
            var source = CreateInitVerifyPixelMap(width, height, color);
            map.Init(source.Data, height, width);
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);

            var pix = map.CreateGPixelReference(width / 2);
            Assert.True(color.Equals(pix.ToPixel()));
        }

        [Fact()]
        public void StencilTest001()
        {
            int width = 128;
            int height = 128;
            Pixel color = Pixel.WhitePixel;
            Rectangle rect = new Rectangle { Left = -10, Bottom = -10, Right = 200, Top = 200 };

            IBitmap bmp = BitmapTests.CreateIntiFillVerifyBitmap(width, height, 0, -1);
            IPixelMap map = CreateInitVerifyPixelMap(width, height, color);
            IPixelMap map2 = CreateInitVerifyPixelMap(width, height, Pixel.BlackPixel);
            Assert.Throws<DjvuArgumentOutOfRangeException>("bounds", () => map.Stencil(bmp, map2, 1, 1, rect, 2.2));
        }

        [Fact()]
        public void StencilTest002()
        {
            int width = 128;
            int height = 128;
            Pixel color = new Pixel(107, 125, 93);
            Pixel color2 = new Pixel(-77, -77, -77);
            Rectangle rect = new Rectangle { Left = 100, Bottom = 0, Right = 0, Top = 100 };

            IBitmap bmp = BitmapTests.CreateIntiFillVerifyBitmap(width, height, 0, 127);
            bmp.Grays = 256;
            IPixelMap map = CreateInitVerifyPixelMap(width, height, color);
            IPixelMap map2 = CreateInitVerifyPixelMap(width, height, color2);
            map.Stencil(bmp, map2, 1, 1, rect, 2.2);
        }

        [Fact()]
        public void StencilTest003()
        {
            int width = 128;
            int height = 128;
            Pixel color = new Pixel(107, 125, 93);
            Pixel color2 = new Pixel(-77, -77, -77);
            Rectangle rect = new Rectangle { Left = 100, Bottom = 0, Right = 0, Top = 100 };

            IBitmap bmp = BitmapTests.CreateIntiFillVerifyBitmap(width/2, height/2, 0, 127);
            bmp.Grays = 256;
            IPixelMap map = CreateInitVerifyPixelMap(width, height, color);
            IPixelMap map2 = CreateInitVerifyPixelMap(width, height, color2);
            map.Stencil(bmp, map2, 1, 1, rect, 2.2);
        }

        [Fact()]
        public void TranslateTest001()
        {
            int width = 12;
            int height = 12;
            Pixel color = Pixel.GreenPixel;
            Pixel color2 = Pixel.RedPixel;

            var map = CreateInitVerifyPixelMap(width, height, color2);
            var source = CreateInitVerifyPixelMap(width, height, color);

            var translMap = (PixelMap) source.Translate(6, 6, map);

            Assert.Same(map, translMap);
            var pix = translMap.CreateGPixelReference(0);
            pix.SetOffset(7, 7);
            Assert.Equal(color2, pix.ToPixel());
            pix.SetOffset(5, 5);
            Assert.Equal(color, pix.ToPixel());
        }

        [Fact()]
        public void TranslateTest002()
        {
            int width = 12;
            int height = 12;
            Pixel color = Pixel.GreenPixel;
            Pixel color2 = Pixel.RedPixel;

            var map = CreateInitVerifyPixelMap(width/2, height/2, color2);
            var source = CreateInitVerifyPixelMap(width, height, color);

            var translMap = (PixelMap)source.Translate(6, 6, map);

            Assert.NotSame(source, translMap);
            var pix = translMap.CreateGPixelReference(0);
            pix.SetOffset(7, 7);
            Assert.Equal(Pixel.BlackPixel, pix.ToPixel());
            pix.SetOffset(5, 5);
            Assert.Equal(Pixel.GreenPixel, pix.ToPixel());
        }

    }
}
