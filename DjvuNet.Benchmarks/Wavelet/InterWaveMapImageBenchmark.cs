using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using DjvuNet.Wavelet;
using DjvuNet.Tests;
using DjvuNet.Benchmarks.Core;

namespace DjvuNet.Benchmarks.Wavelet
{
    [Config(typeof(SimdMatrixConfig))]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
    public class InterWaveMapImageBenchmark : IThroughputBenchmark
    {
        private InterWavePixelMapDecoder _decoder;
        private InterWaveMap _yMap;
        private sbyte[] _img8;
        private int _rowSize;

        public long GetBytesPerOperation(BenchmarkCase benchmarkCase)
        {
            // Measure the input buffer (16-bit array) since it is larger than the 8-bit output
            return (long)_yMap.BlockHeight * _yMap.BlockWidth * 2;
        }

        [GlobalSetup]
        public void Setup()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C_P01_0.bg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                _decoder = new InterWavePixelMapDecoder();
                _decoder.Decode(reader);
            }

            _yMap = _decoder._YMap;
            _rowSize = _yMap.Width * 3;
            _img8 = new sbyte[_yMap.Height * _rowSize];
        }

        [Benchmark]
        public void Image()
        {
            // Execute the inner clamping loop path (fast = true)
            _yMap.Image(0, _img8, _rowSize, 3, true);
        }
    }
}
