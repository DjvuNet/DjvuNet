using Xunit;
using DjvuNet.Wavelet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Compression;

namespace DjvuNet.Wavelet.Tests
{
    public class InterWaveCodecTests
    {
        [Fact()]
        public void InterWaveCodecTest()
        {
            InterWaveMap map = new InterWaveMap();
            InterWaveCodec codec = new InterWaveCodec(map);
            Assert.NotNull(codec._QuantHigh);
            Assert.Equal(10, codec._QuantHigh.Length);
            Assert.NotNull(codec._QuantLow);
            Assert.Equal(16, codec._QuantLow.Length);
            Assert.NotNull(codec._CoefficientState);
            Assert.Equal(256, codec._CoefficientState.Length);
            Assert.NotNull(codec._BucketState);
            Assert.Equal(16, codec._BucketState.Length);
            Assert.Equal(0, codec._CurrentBand);
            Assert.Equal(1, codec._CurrentBitPlane);
            Assert.NotNull(codec._CtxStart);
            Assert.Equal(32, codec._CtxStart.Length);

            Assert.NotNull(codec._CtxBucket);
            Assert.Equal(10, codec._CtxBucket.Length);

            foreach (var b in codec._CtxBucket)
            {
                Assert.NotNull(b);
                Assert.Equal(8, b.Length);
            }
        }


    }
}
