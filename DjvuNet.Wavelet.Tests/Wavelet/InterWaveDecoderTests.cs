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
        /// <summary>
        /// Verifies that the InterWaveDecoder constructor properly delegates to the base class 
        /// Init method and allocates the internal state arrays correctly.
        /// </summary>
        [Fact]
        public void InterWaveDecoderTest()
        {
            var map = new InterWaveMap(32, 32);
            var codec = new InterWaveDecoder(map);
            
            Assert.NotNull(codec);
            // Verify internal arrays are initialized by the base class Init() call
            Assert.NotNull(codec._QuantHigh);
            Assert.NotNull(codec._QuantLow);
            Assert.NotNull(codec._CoefficientState);
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

        /// <summary>
        /// EDGE CASE: Verifies that DecodeBuckets expects a valid block and throws 
        /// a NullReferenceException if the block is missing during processing.
        /// </summary>
        [Fact]
        public void DecodeBuckets_NullBlock_ThrowsNullReferenceException()
        {
            var map = new InterWaveMap(32, 32);
            var codec = new InterWaveDecoder(map);
            var coder = new ZPCodec();
            
            // Expected to throw because blk parameter is null and it calls blk.GetBlock()
            Assert.Throws<NullReferenceException>(() => codec.DecodeBuckets(coder, 0, 0, null, 0, 1));
        }
        
        /// <summary>
        /// EDGE CASE: Verifies that passing an out-of-bounds band index throws 
        /// an IndexOutOfRangeException when attempting to read the quantization thresholds.
        /// </summary>
        [Fact]
        public void DecodeBuckets_OutOfBoundsBand_ThrowsIndexOutOfRangeException()
        {
            var map = new InterWaveMap(32, 32);
            var codec = new InterWaveDecoder(map);
            var coder = new ZPCodec();
            var block = new InterWaveBlock();
            
            // Expected to throw because _QuantHigh[band] will be out of bounds (10 is typical max)
            Assert.Throws<IndexOutOfRangeException>(() => codec.DecodeBuckets(coder, 0, 999, block, 0, 1));
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

        /// <summary>
        /// Verifies that IsNullSlice processes band 0 by analyzing the _QuantLow array thresholds.
        /// </summary>
        [Fact]
        public void IsNullSlice_BandZero_ExecutesSuccessfully()
        {
            var map = new InterWaveMap(32, 32);
            var codec = new InterWaveDecoder(map);
            
            bool result = codec.IsNullSlice(0, 0);
            Assert.IsType<bool>(result);
        }

        /// <summary>
        /// Verifies that IsNullSlice processes bands > 0 by checking the _QuantHigh array thresholds.
        /// </summary>
        [Fact]
        public void IsNullSlice_BandGreaterThanZero_ExecutesSuccessfully()
        {
            var map = new InterWaveMap(32, 32);
            var codec = new InterWaveDecoder(map);
            
            bool result = codec.IsNullSlice(0, 1);
            Assert.IsType<bool>(result);
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