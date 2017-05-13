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
    public class InterWaveDecoderTests
    {
        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void InterWaveDecoderTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void CodeSliceTest001()
        {
            var map = new InterWaveMap();
            var codec = new InterWaveDecoder(map);
            codec._CurrentBitPlane = -1;
            Assert.Equal(0, codec.CodeSlice(null));
        }

        [Fact()]
        public void CodeSliceTest002()
        {
            var map = new InterWaveMap();
            var codec = new InterWaveDecoder(map);
            var coder = new ZPCodec();

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
            var map = new InterWaveMap();
            var codec = new InterWaveDecoder(map);
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
            var codec = new InterWaveDecoder(map);

            for (int i = 0; i < 500000000; i++)
                codec.NextQuant();
        }

        [Fact(Skip = "Time consuming benchmark test"), Trait("Category", "Skip")]
        [Trait("Category", "Benchmark")]
        public void NextQuantFastBenchmarkTest()
        {
            InterWaveMap map = new InterWaveMap(32, 32);
            var codec = new InterWaveCodec(map);

            for (int i = 0; i < 500000000; i++)
                codec.NextQuantFast();
        }
    }
}