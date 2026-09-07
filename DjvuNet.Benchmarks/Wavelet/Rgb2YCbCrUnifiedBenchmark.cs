using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using DjvuNet.Graphics;
using DjvuNet.Wavelet;

using DjvuNet.Benchmarks.Core;

namespace DjvuNet.Wavelet.Benchmarks
{
    [Config(typeof(CustomParallelConfig))]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
    public class Rgb2YCbCrUnifiedBenchmark : DjvuNetBenchmarkBase
    {
        [Params(1024, 4096, 9216, 16384, 36864, 65536, 262144, 1048576, 2096704, 4194304, MaxPixels)]
        public override int PixelCount { get; set; }

        protected override DjvuNetBenchmarkType BenchmarkType => DjvuNetBenchmarkType.ForwardRgbToYCbCr;

        [Benchmark(Baseline = true, OperationsPerInvoke = InvocationCount)]
        public unsafe void Unified_Rgb2YCbCr()
        {
            int ratio = ImageRatio;
            int width = ImageWidth;
            int height = ImageHeight;
            int inRowSizeInBytes = width * sizeof(Pixel);

            for (int i = 0; i < InvocationCount; i++)
            {
                for (int j = 0; j < ratio; j++)
                {
                    GetIterationPointersRgb2YCbCr(i, j, out Pixel* pIn, out sbyte* pY, out sbyte* pCb, out sbyte* pCr);
                    InterWaveTransform.Rgb2YCbCr(pIn, width, height, inRowSizeInBytes, pY, pCb, pCr, width);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = InvocationCount)]
        public unsafe void Unified_YCbCr2Rgb()
        {
            int ratio = ImageRatio;
            int width = ImageWidth;
            int height = ImageHeight;
            int inRowSizeInBytes = width * sizeof(Pixel);

            for (int i = 0; i < InvocationCount; i++)
            {
                for (int j = 0; j < ratio; j++)
                {
                    GetIterationPointersYCbCr2Rgb(i, j, out Pixel* pIn);
                    InterWaveTransform.YCbCr2Rgb(pIn, width, height, inRowSizeInBytes);
                }
            }
        }
    }
}
