using Xunit;
using DjvuNet.Wavelet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Graphics;
using System.Runtime.InteropServices;
using DjvuNet.Graphics.Tests;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DjvuNet.Wavelet.Tests
{
    public class InterWaveTransformTests
    {
        int width = 4160;
        int height = 2160;
        int bytesPerPixel = 3;
        int testsToSkip = 0;
        int testCount = 1;

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ForwardTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void BackwardTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Test bug or code bug?"), Trait("Category", "Skip")]
        public unsafe void Rgb2YTest()
        {
            Pixel3B[] pixBuffer = new Pixel3B[256];
            for (int i = 0; i < pixBuffer.Length; i++)
                pixBuffer[i].SetGray((byte)i);

            GCHandle hPix = GCHandle.Alloc(pixBuffer, GCHandleType.Pinned);
            byte* pByteBuff = (byte*)hPix.AddrOfPinnedObject();

            int[] gammaLUT = PixelMap.GetGammaCorrection(2.2);
            GCHandle hGamma = GCHandle.Alloc(gammaLUT, GCHandleType.Pinned);
            int* pGammaLUT = (int*)hGamma.AddrOfPinnedObject();

            int dataLength = pixBuffer.Length * 3;
            dataLength -= dataLength % 48;

            PixelMap.ApllyGamma(pByteBuff, pixBuffer.Length * 3, dataLength, pGammaLUT);

            hPix.Free();
            hGamma.Free();


            sbyte[] outBuff = new sbyte[256];

            unsafe
            {
                fixed (Pixel3B* p = pixBuffer)
                fixed(sbyte* pOutBuff = outBuff)
                {
                    Pixel* pBuff = (Pixel*) p;
                    sbyte* pOut = pOutBuff;
                    byte* pB = (byte*)p;
                    InterWaveTransform.Rgb2Y(pBuff, 16, 16, 16 * 3, pOut, 16);


                }
            }

            int outLength = outBuff.Length - 1;
            for (int i = 0; i < outLength; i++)
            {
                //Console.WriteLine(unchecked((byte)outBuff[i]));
                if (i > 0)
                {
                    if (i < 94)
                        Assert.True(unchecked((byte)outBuff[i]) <= unchecked((byte)outBuff[i + 1]));
                    Assert.NotEqual(0, unchecked((byte)outBuff[i]));
                }
            }
        }

        [Fact(Skip = "Test bug or code bug?"), Trait("Category", "Skip")]
        public void Rgb2CbTest()
        {
            Pixel[] pixBuffer = new Pixel[256];
            for (int i = 0; i < pixBuffer.Length; i++)
                pixBuffer[i].SetGray((sbyte)(i - 128));

            sbyte[] outBuff = new sbyte[256];

            unsafe
            {
                fixed (Pixel* p = pixBuffer)
                fixed (sbyte* pOutBuff = outBuff)
                {
                    Pixel* pBuff = p;
                    sbyte* pOut = pOutBuff;
                    InterWaveTransform.Rgb2Cb(pBuff, 16, 16, 16 * 3, pOut, 16);
                }
            }

            int outLength = outBuff.Length - 1;
            for (int i = 0; i < outLength; i++)
            {
                //Console.WriteLine($"i: {i}, Y: {unchecked((byte)outBuff[i])}");
                if (i > 0)
                {
                    if (i < 94)
                        Assert.True(unchecked((byte)outBuff[i]) <= unchecked((byte)outBuff[i + 1]));
                    Assert.NotEqual(0, unchecked((byte)outBuff[i]));
                }
            }
        }

        [Fact(Skip = "Test bug or code bug?"), Trait("Category", "Skip")]
        public void Rgb2CrTest()
        {
            Pixel[] pixBuffer = new Pixel[256];
            for (int i = 0; i < pixBuffer.Length; i++)
                pixBuffer[i].SetGray((sbyte)(i - 128));

            sbyte[] outBuff = new sbyte[256];

            unsafe
            {
                fixed (Pixel* p = pixBuffer)
                fixed (sbyte* pOutBuff = outBuff)
                {
                    Pixel* pBuff = p;
                    sbyte* pOut = pOutBuff;
                    InterWaveTransform.Rgb2Y(pBuff, 16, 16, 16 * 3, pOut, 16);
                }
            }

            int outLength = outBuff.Length - 1;
            for (int i = 0; i < outLength; i++)
            {
                if (i > 0)
                {
                    if (i < 94)
                        Assert.True(unchecked((byte)outBuff[i]) <= unchecked((byte)outBuff[i + 1]));
                    Assert.NotEqual(0, unchecked((byte)outBuff[i]));
                }
            }
        }

        [Fact()]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Rgb2YCbCrFragmentedTest()
        {
            sbyte[] data = PixelMapTests.GetRandomData(width, height, bytesPerPixel);

            long time = 0;
            Stopwatch watch = new Stopwatch();

            sbyte[] outY = new sbyte[width * height];
            sbyte[] outCb = new sbyte[width * height];
            sbyte[] outCr = new sbyte[width * height];

            for (int i = 0; i < testCount; i++)
            {
                watch.Restart();
                unsafe
                {
                    GCHandle hData = GCHandle.Alloc(data, GCHandleType.Pinned);
                    GCHandle hOutY = GCHandle.Alloc(outY, GCHandleType.Pinned);
                    GCHandle hOutCb = GCHandle.Alloc(outCb, GCHandleType.Pinned);
                    GCHandle hOutCr = GCHandle.Alloc(outCr, GCHandleType.Pinned);
                    sbyte* pY = (sbyte*) hOutY.AddrOfPinnedObject();
                    sbyte* pCb = (sbyte*) hOutCb.AddrOfPinnedObject();
                    sbyte* pCr = (sbyte*) hOutCr.AddrOfPinnedObject();
                    Pixel* pPix = (Pixel*)hData.AddrOfPinnedObject();

                    InterWaveTransform.Rgb2Y(pPix, width, height, width * bytesPerPixel, pY, width);
                    InterWaveTransform.Rgb2Cb(pPix, width, height, width * bytesPerPixel, pCb, width);
                    InterWaveTransform.Rgb2Cr(pPix, width, height, width * bytesPerPixel, pCr, width);

                    hData.Free();
                    hOutY.Free();
                    hOutCb.Free();
                    hOutCr.Free();
                }
                watch.Stop();
                if (i >= testsToSkip)
                    time += watch.ElapsedMilliseconds;
            }

            Console.WriteLine($"Fragmented Rgb2YCbCr conversion time ms {((double)time / testCount).ToString("0#.000")}");
        }

        [Fact()]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Rgb2YCbCrOptimizedTest()
        {

            sbyte[] data = PixelMapTests.GetRandomData(width, height, bytesPerPixel);

            long time = 0;
            Stopwatch watch = new Stopwatch();

            for (int i = 0; i < testCount + testsToSkip; i++)
            {
                watch.Restart();
                sbyte[] outY = new sbyte[width * height];
                sbyte[] outCb = new sbyte[width * height];
                sbyte[] outCr = new sbyte[width * height];

                unsafe
                {
                    fixed (sbyte* pData = data)
                    fixed (sbyte* pOutY = outY)
                    fixed (sbyte* pOutCb = outCb)
                    fixed (sbyte* pOutCr = outCr)
                    {
                        Pixel* pPix = (Pixel*)pData;
                        InterWaveTransform.Rgb2YCbCr(pPix, width, height, width * bytesPerPixel, pOutY, pOutCb, pOutCr, width);
                    }
                }
                watch.Stop();
                if (i >= testsToSkip)
                    time += watch.ElapsedMilliseconds;
            }

            Console.WriteLine($"Optimized Rgb2YCbCr conversion time ms {((double)time / testCount).ToString("0#.000")}");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void FilterFvTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void FilterFhTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void FilterBvTest()
        {

        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void FilterBhTest()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}
