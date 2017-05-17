using Xunit;
using DjvuNet.Wavelet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Graphics;

namespace DjvuNet.Wavelet.Tests
{
    public class InterWaveTransformTests
    {
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

        [Fact()]
        public void Rgb2YTest()
        {
            Pixel3B[] pixBuffer = new Pixel3B[256];
            for (int i = 0; i < pixBuffer.Length; i++)
                pixBuffer[i].SetGray((byte)i);

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

                    //for (int i = 0; i < pixBuffer.Length * 3; i++)
                    //    Console.WriteLine(pB[i]);

                    //Console.WriteLine("---------------------------------------");
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

        [Fact()]
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

        [Fact()]
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

        [Fact()]
        public void FilterBhTest()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}