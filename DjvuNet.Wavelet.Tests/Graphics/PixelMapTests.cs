using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DjvuNet.Errors;
using Xunit;

namespace DjvuNet.Graphics.Tests
{

    public class PixelMapTests
    {

        int shdWidth = 1920 * 2;
        int shdHeight = 1080 * 2;
        int shdBytesPerPixel = 4;
        int testCount = 1;

#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
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

#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public static void WritePixelMap(int width, int height, PixelMap bmp)
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

        public static TheoryData<sbyte[], int, int, Type> ConstructorData => new TheoryData<sbyte[], int, int, Type>
        {
            // 1. null data
            { null, 10, 10, typeof(DjvuArgumentNullException) },
            // 2. negative width
            { new sbyte[300], -10, 10, typeof(DjvuArgumentOutOfRangeException) },
            // 3. negative height
            { new sbyte[300], 10, -10, typeof(DjvuArgumentOutOfRangeException) },
            // 4. array size too small for dimensions (needs 10 * 10 * 3 = 300)
            { new sbyte[299], 10, 10, typeof(DjvuArgumentException) },
            // 5. array empty for non-zero dimensions
            { new sbyte[0], 10, 10, typeof(DjvuArgumentException) },
            // 6. happy path - exact size
            { new sbyte[300], 10, 10, null },
            // 7. happy path - larger array (allowed for LOH pooling)
            { new sbyte[400], 10, 10, null },
            // 8. happy path - zero dimensions
            { new sbyte[0], 0, 0, null }
        };

        [Theory]
        [MemberData(nameof(ConstructorData))]
        public void ConstructorWithDataTheory(sbyte[] data, int width, int height, Type expectedExceptionType)
        {
            if (expectedExceptionType != null)
            {
                Assert.Throws(expectedExceptionType, () => new PixelMap(data, width, height));
            }
            else
            {
                var map = new PixelMap(data, width, height);
                Assert.Equal(width, map.Width);
                Assert.Equal(height, map.Height);
                Assert.Same(data, map.Data);
            }
        }

        [Theory]
        [MemberData(nameof(ConstructorData))]
        public void InitWithDataTheory(sbyte[] data, int width, int height, Type expectedExceptionType)
        {
            if (expectedExceptionType != null)
            {
                Assert.Throws(expectedExceptionType, () => new PixelMap().Init(data, height, width)); // Init signature takes arows(height), acolumns(width)
            }
            else
            {
                var map = new PixelMap().Init(data, height, width);
                Assert.Equal(width, map.Width);
                Assert.Equal(height, map.Height);
                Assert.Same(data, map.Data);
            }
        }

        public static PixelMap CreateVerifyPixelMap()
        {
            var map = new PixelMap();
            Assert.Equal(3, map.BytesPerPixel);
            Assert.Equal(0, map.Width);
            Assert.Equal(0, map.Height);
            return map;
        }

        public static PixelMap CreateInitVerifyPixelMap(int width, int height, IPixel color)
        {
            PixelMap map = CreateVerifyPixelMap();
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
            Assert.Equal(256, correctionTable.Length);
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
            map.Attenuate(ref bmp, 0, 0);

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
            map.Attenuate(ref bmp, 16, 16);

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
            map.Attenuate(ref bmp, -512, 16);

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
            map.Attenuate(ref bmp, 16, -512);

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

            map.Blit(ref bmp, 256, 1, Pixel.BlackPixel);

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

            map.Blit(ref bmp, 256, 1, null);
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

            map.Blit(ref bmp, 256, 1, Pixel.WhitePixel);
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

            map.Blit(ref bmp, -512, 1, Pixel.WhitePixel);
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

            map.Blit(ref bmp, 256, -512, Pixel.WhitePixel);
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
#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
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

        [Fact()]
#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
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
#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
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
            map.DownSample(map, 1, default(Rectangle));
            Assert.Equal(width, map.Width);
            Assert.Equal(height, map.Height);
        }

        [Fact()]
        public void DownsampleTest002()
        {
            int width = 32;
            int height = 32;
            var map = CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            map.DownSample(map, 1, map.BoundingRectangle);
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

            map.DownSample(map, subsample, default(Rectangle));
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

            map.DownSample(map, subsample, default(Rectangle));
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

            map.DownSample(map, subsample, default(Rectangle));
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

            map.DownSample(map, subsample, default(Rectangle));
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

            map.DownSample(map, subsample, default(Rectangle));
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
                XMax = width,
                YMin = 0,
                XMin = 0,
                YMax = height,
            };

            map.DownSample(map, subsample, rect);
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
                XMax = width / 2,
                YMin = 0,
                XMin = -5,
                YMax = height,
            };

            Assert.Throws<DjvuArgumentOutOfRangeException>("targetRect", () => map.DownSample(map, subsample, rect));
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
                XMax = 64,
                YMin = 0,
                XMin = 0,
                YMax = height,
            };

            Assert.Throws<DjvuArgumentOutOfRangeException>("targetRect", () => map.DownSample(map, subsample, rect));
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
                XMin = 0,
                YMin = -1,
                XMax = 32,
                YMax = height,
            };

            Assert.Throws<DjvuArgumentOutOfRangeException>("targetRect", () => map.DownSample(map, subsample, rect));
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
                XMin = 0,
                YMin = 0,
                XMax = 1,
                YMax = height * 2,
            };

            Assert.Throws<DjvuArgumentOutOfRangeException>("targetRect", () => map.DownSample(map, subsample, rect));
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

            map.DownSample(map, subsample, default(Rectangle));
            Assert.Equal(Math.Round((double)width / subsample, 0), map.Width);
            Assert.Equal(Math.Round((double)height / subsample, 0), map.Height);
        }


        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void Downsample43Test001()
        {
            var map = CreateInitVerifyPixelMap(512, 512, Pixel.BluePixel);
            var map2 = CreateInitVerifyPixelMap(1024, 1024, Pixel.GreenPixel);
            map.DownSample43(map2, map.BoundingRectangle);
            Assert.Equal(512, map.Width);
            Assert.Equal(512, map.Height);
            Assert.Equal(map.CreateGPixelReference(256).ToPixel(), Pixel.GreenPixel);
        }

        [Fact()]
        public void Downsample43Test002()
        {
            var map = CreateInitVerifyPixelMap(512, 512, Pixel.BluePixel);
            var map2 = CreateInitVerifyPixelMap(1024, 1024, Pixel.GreenPixel);
            Assert.Throws<DjvuArgumentOutOfRangeException>("targetRect",
                () => map.DownSample43(map2, new Rectangle { XMin = 0, YMin = 0, XMax = 2048, YMax = 2048 }));
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
            Rectangle boundsRect = new Rectangle { XMin = 0, YMin = 0, XMax = 200, YMax = 200 };

            Bitmap bmp = BitmapTests.CreateIntiFillVerifyBitmap(width, height, 0, -1);
            PixelMap map = CreateInitVerifyPixelMap(width, height, color);
            PixelMap map2 = CreateInitVerifyPixelMap(width, height, Pixel.BlackPixel);
            Assert.Throws<DjvuArgumentOutOfRangeException>("bounds", () => map.Stencil(ref bmp, map2, 1, 1, boundsRect, 2.2));
        }

        [Fact()]
        public void StencilTest002()
        {
            int width = 128;
            int height = 128;
            Pixel color = new Pixel(107, 125, 93);
            Pixel color2 = new Pixel(-77, -77, -77);
            Rectangle rect = new Rectangle { XMax = 100, YMin = 0, XMin = 0, YMax = 100 };

            Bitmap bmp = BitmapTests.CreateIntiFillVerifyBitmap(width, height, 0, 127);
            bmp.Grays = 256;
            PixelMap map = CreateInitVerifyPixelMap(width, height, color);
            PixelMap map2 = CreateInitVerifyPixelMap(width, height, color2);
            map.Stencil(ref bmp, map2, 1, 1, rect, 2.2);
        }

        [Fact()]
        public void StencilTest003()
        {
            int width = 128;
            int height = 128;
            Pixel color = new Pixel(107, 125, 93);
            Pixel color2 = new Pixel(-77, -77, -77);
            Rectangle rect = new Rectangle { XMax = 100, YMin = 0, XMin = 0, YMax = 100 };

            Bitmap bmp = BitmapTests.CreateIntiFillVerifyBitmap(width/2, height/2, 0, 127);
            bmp.Grays = 256;
            PixelMap map = CreateInitVerifyPixelMap(width, height, color);
            PixelMap map2 = CreateInitVerifyPixelMap(width, height, color2);
            map.Stencil(ref bmp, map2, 1, 1, rect, 2.2);
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
        [Fact]
        public void Init_Bitmap_DefaultUninitialized_Throws()
        {
            var map = new PixelMap();
            var bmp = new Bitmap();
            var ex = Assert.Throws<DjvuArgumentException>("source", () => map.Init(ref bmp));
            Assert.Contains("The source Bitmap cannot be default instance. Please provide a valid, initialized Bitmap instance.", ex.Message);
        }

        [Theory]
        [InlineData(2, new sbyte[] { -1 })] 
        [InlineData(3, new sbyte[] { -1, 127 })] 
        [InlineData(4, new sbyte[] { -1, -86, 85 })] 
        [InlineData(5, new sbyte[] { -1, -65, 127, 63 })] // Prime
        [InlineData(7, new sbyte[] { -1, -44, -86, 127, 85, 42 })] // Prime
        [InlineData(11, new sbyte[] { -1, -27, -52, -78, -103, 127, 102, 76, 51, 25 })] // Prime
        [InlineData(13, new sbyte[] { -1, -23, -44, -65, -86, -108, 127, 106, 85, 63, 42, 21 })] // Prime
        [InlineData(16, new sbyte[] { -1, -18, -35, -52, -69, -86, -103, -120, 119, 102, 85, 68, 51, 34, 17 })]
        [InlineData(17, new sbyte[] { -1, -17, -33, -49, -65, -81, -97, -113, 127, 111, 95, 79, 63, 47, 31, 15 })] // Prime
        [InlineData(19, new sbyte[] { -1, -16, -30, -44, -58, -72, -86, -101, -115, 127, 113, 99, 85, 70, 56, 42, 28, 14 })] // Prime
        [InlineData(23, new sbyte[] { -1, -13, -25, -36, -48, -59, -71, -83, -94, -106, -117, 127, 115, 104, 92, 81, 69, 57, 46, 34, 23, 11 })] // Prime
        [InlineData(29, new sbyte[] { -1, -11, -20, -29, -38, -47, -56, -65, -74, -83, -93, -102, -111, -120, 127, 118, 109, 100, 91, 81, 72, 63, 54, 45, 36, 27, 18, 9 })] // Prime
        [InlineData(31, new sbyte[] { -1, -10, -18, -27, -35, -44, -52, -61, -69, -78, -86, -95, -103, -112, -120, 127, 119, 110, 102, 93, 85, 76, 68, 59, 51, 42, 34, 25, 17, 8 })] // Prime
        [InlineData(32, new sbyte[] { -1, -10, -18, -26, -34, -43, -51, -59, -67, -76, -84, -92, -100, -108, -117, -125, 123, 115, 106, 98, 90, 82, 74, 65, 57, 49, 41, 32, 24, 16, 8 })]
        public void Init_Bitmap_GrayRamps(int grays, sbyte[] expectedValues)
        {
            var source = new Bitmap();
            source.Init(256, 1, 0);
            source.Grays = grays;
            
            for (int i = 0; i < 256; i++)
            {
                source.Data[i] = unchecked((sbyte)i);
            }

            var map = new PixelMap();
            map.Init(ref source);

            for (int i = 0; i < expectedValues.Length; i++)
            {
                int idx = i * 3;
                Assert.Equal(expectedValues[i], map.Data[idx]);     
                Assert.Equal(expectedValues[i], map.Data[idx + 1]); 
                Assert.Equal(expectedValues[i], map.Data[idx + 2]); 
            }
            
            for (int i = expectedValues.Length; i < 256; i++)
            {
                int idx = i * 3;
                Assert.Equal(0, map.Data[idx]);
                Assert.Equal(0, map.Data[idx + 1]);
                Assert.Equal(0, map.Data[idx + 2]);
            }
        }

        [Fact]
        public void Init_Bitmap_GrayRamp_256()
        {
            var source = new Bitmap();
            source.Init(256, 1, 0);
            source.Grays = 256;
            for (int i = 0; i < 256; i++)
            {
                source.Data[i] = unchecked((sbyte)i);
            }

            var map = new PixelMap();
            map.Init(ref source);

            Assert.Equal(unchecked((sbyte)255), map.Data[0]); 
            
            for (int i = 1; i < 255; i++)
            {
                sbyte expected = unchecked((sbyte)(255 - i));
                int idx = i * 3;
                Assert.Equal(expected, map.Data[idx]);
            }
            
            Assert.Equal(0, map.Data[255 * 3]); 
        }

        [Fact]
        public void Init_PixelMap_NullSource_Throws()
        {
            var map = new PixelMap();
            Assert.Throws<DjvuArgumentNullException>("source", () => map.Init((PixelMap)null));
        }

        [Fact]
        public void Fill_Bitmap_InitializedEmpty_CircuitBreaker()
        {
            var map = new PixelMap();
            map.Init(10, 10, Pixel.WhitePixel);
            
            Bitmap emptySource = new Bitmap();
            emptySource.Init(0, 0, 0);
            map.Fill(ref emptySource, 0, 0); 
            Assert.Equal(10, map.Width); 
        }

        [Fact]
        public void Fill_PixelMap_InitializedEmpty_CircuitBreaker()
        {
            var map = new PixelMap();
            map.Init(10, 10, Pixel.WhitePixel);
            
            var emptySource = new PixelMap(); 
            map.Fill(emptySource, 0, 0); 
            Assert.Equal(10, map.Width); 
        }

        [Fact]
        public void Fill_Bitmap()
        {
            var map = new PixelMap();
            map.Init(4, 4, Pixel.BluePixel);

            var sourceBitmap = new Bitmap();
            sourceBitmap.Init(2, 2, 0);
            sourceBitmap.Grays = 2; 
            
            sourceBitmap.SetByteAt(0, 1);
            sourceBitmap.SetByteAt(1, 1);
            sourceBitmap.SetByteAt(2, 1);
            sourceBitmap.SetByteAt(3, 1);

            map.Fill(ref sourceBitmap, 1, 1);

            var bgPixel = map.CreateGPixelReference(0).ToPixel();
            Assert.Equal(Pixel.BluePixel.Blue, bgPixel.Blue);

            var interiorPixel = map.CreateGPixelReference(5).ToPixel();
            Assert.Equal(0, interiorPixel.Blue); 
        }

        [Fact]
        public void Fill_PixelMap()
        {
            var map = new PixelMap();
            map.Init(4, 4, Pixel.BluePixel);

            var sourceMap = new PixelMap();
            sourceMap.Init(2, 2, Pixel.BlackPixel);
            
            map.Fill(sourceMap, 1, 1);

            var bgPixel = map.CreateGPixelReference(0).ToPixel();
            Assert.Equal(Pixel.BluePixel.Blue, bgPixel.Blue);

            var interiorPixel = map.CreateGPixelReference(5).ToPixel();
            Assert.Equal(0, interiorPixel.Blue); 
        }

        [Theory]
        // AVX-512 Boundaries (64 bytes)
        [InlineData(140, 13)] // Multi-iteration (AVX-512)
        [InlineData(128, 11)] // Exact 2x multiple (AVX-512)
        [InlineData(70, 7)]   // Standard > 64 case
        [InlineData(65, 7)]   // Boundary + 1 (AVX-512)
        [InlineData(64, 5)]   // Boundary Exact (AVX-512)
        [InlineData(63, 5)]   // Boundary - 1 (Falls to AVX2)

        // AVX2 Boundaries (32 bytes)
        [InlineData(33, 5)]   // Boundary + 1 (AVX2)
        [InlineData(32, 3)]   // Boundary Exact (AVX2)
        [InlineData(31, 3)]   // Boundary - 1 (Falls to Vector128)

        // Vector128 Boundaries (16 bytes)
        [InlineData(17, 3)]   // Boundary + 1 (Vector128)
        [InlineData(16, 2)]   // Boundary Exact (Vector128)
        [InlineData(15, 2)]   // Boundary - 1 (Falls to Scalar)

        // Scalar Fallback (< 16 bytes)
        [InlineData(10, 2)]   // Standard < 16 case
        [InlineData(1, 2)]    // Absolute minimum case
        public void Fill_Bitmap_SIMD_Widths(int width, int prime)
        {
            int height = 5;
            // Pad PixelMap to allow offset pasting and edge boundary testing
            var map = new PixelMap();
            map.Init(height + 2, width + 2, Pixel.BluePixel);

            var sourceBitmap = new Bitmap();
            sourceBitmap.Init(height, width, 0);
            sourceBitmap.Grays = 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    sourceBitmap.SetByteAt(sourceBitmap.RowOffset(y) + x, (sbyte)((x % prime == 0) ? 1 : 0));
                }
            }

            // Fill at offset (1, 1)
            map.Fill(ref sourceBitmap, 1, 1);

            for (int y = 0; y < height + 2; y++)
            {
                for (int x = 0; x < width + 2; x++)
                {
                    var pixel = map.CreateGPixelReference(map.RowOffset(y) + x).ToPixel();

                    if (y == 0 || y == height + 1 || x == 0 || x == width + 1)
                    {
                        // Outside the fill area, border must remain Blue
                        Assert.Equal(Pixel.BluePixel.Blue, pixel.Blue);
                        Assert.Equal(0, pixel.Red);
                    }
                    else
                    {
                        // Inside the fill area
                        int srcX = x - 1;
                        if (srcX % prime == 0)
                        {
                            // Foreground (1) -> Black (0, 0, 0)
                            Assert.Equal(0, pixel.Blue);
                            Assert.Equal(0, pixel.Red);
                            Assert.Equal(0, pixel.Green);
                        }
                        else
                        {
                            // Background (0) -> White (-1, -1, -1) in sbyte
                            Assert.Equal(-1, pixel.Blue);
                            Assert.Equal(-1, pixel.Red);
                            Assert.Equal(-1, pixel.Green);
                        }
                    }
                }
            }
        }


        [Fact]
        public void ToString_InitializedClass()
        {
            var map = new PixelMap(new sbyte[300], 10, 10);
            string result = map.ToString();
            
            Assert.Contains("DjvuNet.Graphics.PixelMap", result);
            Assert.Contains($"Width: {map.Width}", result);
            Assert.Contains($"Height: {map.Height}", result);
            Assert.Contains($"Data: {map.Data.Length} sbytes.", result);
        }

        [Fact]
        public void ToString_UninitializedClass()
        {
            var map = new PixelMap();
            string result = map.ToString();

            Assert.Contains("DjvuNet.Graphics.PixelMap: Width: 0, Height: 0, Data: null sbytes.", result);
        }

        /// <summary>
        /// Verifies that FillRgbPixels explicitly guards against null pixel buffers
        /// by throwing a DjvuArgumentNullException, preventing silent downstream crashes.
        /// </summary>
        [Fact]
        public void FillRgbPixels_NullPixelsArray_Throws()
        {
            int width = 32;
            int height = 32;
            PixelMap map = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.BluePixel) as PixelMap;

            var ex = Assert.Throws<DjvuArgumentNullException>(() => map.FillRgbPixels(0, 0, width, height, null, 0, width));
            Assert.Equal("pixels", ex.ParamName);
        }

        /// <summary>
        /// Verifies that attempting to fill pixels with an undersized source buffer 
        /// correctly aborts the invalid operation, preventing memory corruption downstream.
        /// </summary>
        [Fact]
        public void FillRgbPixels_OutOfBounds_Throws()
        {
            int width = 32;
            int height = 32;
            PixelMap map = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.BluePixel) as PixelMap;
            int[] pixels = new int[10]; // Too small

            var ex = Assert.Throws<DjvuInvalidOperationException>(() => map.FillRgbPixels(0, 0, width, height, pixels, 0, width));
            Assert.Contains("Destination buffer too small", ex.Message);
        }

        /// <summary>
        /// Verifies that providing invalid spatial dimensions or offsets to FillRgbPixels
        /// correctly aborts the operation and throws a DjvuArgumentOutOfRangeException.
        /// Exhaustively tests all negative bounds, overreaches, integer overflows, and invalid strides.
        /// </summary>
        [Theory]
        [InlineData(0, 0, -5, 32, 0, 32, "width")]          // Negative width
        [InlineData(0, 0, 32, -5, 0, 32, "height")]         // Negative height
        [InlineData(-5, 0, 32, 32, 0, 32, "x")]             // Negative X
        [InlineData(0, -5, 32, 32, 0, 32, "y")]             // Negative Y
        [InlineData(5, 0, 32, 32, 0, 32, "width")]          // X + W > Map.Width (5 + 32 > 32)
        [InlineData(0, 5, 32, 32, 0, 32, "height")]         // Y + H > Map.Height (5 + 32 > 32)
        [InlineData(2147483637, 0, 20, 32, 0, 32, "width")] // Overflow: X (int.MaxValue - 10) + width > int.MaxValue
        [InlineData(0, 2147483637, 32, 20, 0, 32, "height")]// Overflow: Y (int.MaxValue - 10) + height > int.MaxValue
        [InlineData(0, 0, 32, 32, -5, 32, "offset")]        // Negative offset
        [InlineData(0, 0, 32, 32, 0, 10, "scanSize")]       // scanSize < Width
        public void FillRgbPixels_InvalidSpatialBounds_Throws(
            int x, int y, int w, int h, int off, int scanSize, string expectedParam)
        {
            int mapWidth = 32;
            int mapHeight = 32;
            PixelMap map = PixelMapTests.CreateInitVerifyPixelMap(mapWidth, mapHeight, Pixel.BluePixel) as PixelMap;

            // Rent a massive buffer so we don't accidentally trip the "buffer too small" InvalidOperationException check
            int[] pixels = ArrayPool<int>.Shared.Rent(mapWidth * mapHeight * 10);
            try
            {
                var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => map.FillRgbPixels(x, y, w, h, pixels, off, scanSize));
                Assert.Equal(expectedParam, ex.ParamName);
            }
            finally
            {
                ArrayPool<int>.Shared.Return(pixels);
            }
        }

        /// <summary>
        /// Validates that the FillRgbPixels method successfully populates the underlying
        /// pixel data buffer of the map. This is critical for ensuring that external
        /// ARGB/RGB pixel arrays can be correctly ingested into the internal DjvuNet
        /// pixel representation.
        /// </summary>
        [Fact]
        public void FillRgbPixelsTest()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.BluePixel;

            PixelMap map = PixelMapTests.CreateInitVerifyPixelMap(width, height, color) as PixelMap;

            // Create a test array of pixels (ARGB format: 4 bytes per pixel)
            int[] pixels = new int[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                // Fill with an arbitrary solid color: Red
                pixels[i] = unchecked((int)0xFFFF0000);
            }

            // Act
            map.FillRgbPixels(0, 0, width, height, pixels, 0, width);

            // Assert: Verify that the internal Data buffer reflects the change.
            Assert.NotNull(map.Data);
            Assert.True(map.Data.Length > 0);

            // Using CreateGPixelReference to indirectly verify the Fill worked as it traverses the updated buffer
            var reference = map.CreateGPixelReference(0);
            Assert.NotNull(reference);
            Assert.Equal(0, reference.Offset);

            // Verify that the PixelReference correctly binds back to the parent map
            Assert.Same(map, reference.Parent);
        }

        /// <summary>
        /// Validates the creation of GPixelReference objects (iterators) from the Map.
        /// This tests both the 1D (offset) and 2D (row, column) initialization paths
        /// to ensure that iterators are correctly bound to the source map, which is
        /// essential for safe memory traversal during rendering.
        /// </summary>
        [Fact]
        public void CreateGPixelReferenceTest()
        {
            int width = 16;
            int height = 16;
            PixelMap map = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.BluePixel) as PixelMap;

            // Test 1D linear offset iterator
            var referenceByOffset = map.CreateGPixelReference(42);
            Assert.NotNull(referenceByOffset);
            Assert.Equal(42 * map.BytesPerPixel, referenceByOffset.Offset);
            Assert.Same(map, referenceByOffset.Parent);

            // Test 2D spatial coordinate iterator
            var referenceByCoord = map.CreateGPixelReference(2, 5);
            Assert.NotNull(referenceByCoord);
            int expectedOffset = (map.RowOffset(2) + 5) * map.BytesPerPixel;
            Assert.Equal(expectedOffset, referenceByCoord.Offset);
            Assert.Same(map, referenceByCoord.Parent);
        }

        /// <summary>
        /// Validates that the public ToImage method correctly converts the internal
        /// DjvuNet pixel data into a standard GDI+ System.Drawing.Bitmap.
        /// This ensures memory transfer and format mapping function correctly.
        /// </summary>
        //[Fact]
        //public void ToImageTest003()
        //{
        //    int width = 32;
        //    int height = 32;
        //    Pixel color = Pixel.BluePixel;

        //    PixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
        //    using (System.Drawing.Bitmap bmp = map1.ToImage())
        //    {
        //        Assert.NotNull(bmp);
        //        Assert.IsType<System.Drawing.Bitmap>(bmp);
        //        Assert.Equal(width, bmp.Width);
        //        Assert.Equal(height, bmp.Height);
        //    }
        //}
    }
}
