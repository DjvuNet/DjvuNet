using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using DjvuNet.Graphics;
using DjvuNet.Tests;

namespace DjvuNet.Benchmarks
{
    [Config(typeof(BitmapRleConfig))] // Inherit the Scalar / Vector128 / AVX2 / AVX512 Jobs
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
    public class BitmapBlitSubSampleBenchmark : IThroughputBenchmark
    {
        // Test every single valid subsample factor (2 through 15)
        [Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)]
        public int SubSample { get; set; }

        // bad width for factor 4 and 8 Avx2/Vector128
        // 2038;
        // good width for factor 4 and 8 Avx2 and Vector128
        // 1931;
        // bad width for factor 3
        // 1976;
        public int SourceWidth { get; set; } = 2038;                                        
        public int SourceHeight { get; set; } = 791;

        // 1 530 589 = (1931 + 4) * 791 + 4
        public int PixelCount => (SourceWidth + Border) * SourceHeight + Border;
        public int Border { get; set; } = 4;
        public int TargetWidth { get; set; } = 2571;
        public int TargetHeight { get; set; } = 1391;

        private Bitmap _source;
        private Bitmap _target;

        public long GetBytesPerOperation(BenchmarkCase benchmarkCase)
        {
            // Returns the total number of *source* pixels processed (1 byte per pixel)
            return (long)PixelCount;
        }

        [GlobalSetup]
        public void Setup()
        {
            // 1. Arrange 
            _target = new Bitmap();
            Util.PrepareTestBitmap(ref _target, Util.SharedTargetBuffer, TargetWidth, TargetHeight, Border);

            _source = new Bitmap();
            Util.PrepareTestBitmap(ref _source, Util.SharedSourceBuffer, SourceWidth, SourceHeight, Border);
        }

        [Benchmark]
        public void BlitSubSampled()
        {
            // Benchmark the exact execution path analyzed across all Tiers
            _target.Blit(ref _source, 0, 0, SubSample);
        }
    }
}
