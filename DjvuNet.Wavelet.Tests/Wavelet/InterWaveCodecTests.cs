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
            InterWaveCodec codec = new InterWaveCodec();
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
            Assert.NotNull(codec._CtxMant);
            Assert.NotNull(codec._CtxRoot);

            Assert.NotNull(codec._CtxStart);
            Assert.Equal(32, codec._CtxStart.Length);

            foreach (var s in codec._CtxStart)
                Assert.NotNull(s);

            Assert.NotNull(codec._CtxBucket);
            Assert.Equal(10, codec._CtxBucket.Length);

            foreach (var b in codec._CtxBucket)
            {
                Assert.NotNull(b);
                Assert.Equal(8, b.Length);

                foreach (var c in b)
                    Assert.NotNull(c);
            }
        }

        [Fact()]
        public void CodeSliceTest001()
        {
            InterWaveCodec codec = new InterWaveCodec();
            codec._CurrentBitPlane = -1;
            Assert.Equal(0, codec.CodeSlice(null));
        }

        [Fact()]
        public void CodeSliceTest002()
        {
            InterWaveCodec codec = new InterWaveCodec();
            ZPCodec coder = new ZPCodec();

            Assert.Equal(1, codec.CodeSlice(coder));
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void DecodeBucketsTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void InitTest()
        {
            InterWaveMap map = new InterWaveMap(32, 32);
            InterWaveCodec codec = new InterWaveCodec();
            var test = codec.Init(map);

            Assert.NotNull(test);
            Assert.Same(codec, test);

        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void IsNullSliceTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Time consuming benchmark test"), Trait("Category", "Skip")]
        [Trait("Category", "Benchmark")]
        public void NextQuantBenchmarkTest()
        {
            InterWaveMap map = new InterWaveMap(32, 32);
            InterWaveCodec codec = new InterWaveCodec();
            var test = codec.Init(map);

            for (int i = 0; i < 500000000; i++)
                codec.NextQuant();
        }

        [Fact(Skip = "Time consuming benchmark test"), Trait("Category", "Skip")]
        [Trait("Category", "Benchmark")]
        public void NextQuantFastBenchmarkTest()
        {
            InterWaveMap map = new InterWaveMap(32, 32);
            InterWaveCodec codec = new InterWaveCodec();
            var test = codec.Init(map);

            for (int i = 0; i < 500000000; i++)
                codec.NextQuantFast();
        }
    }
}