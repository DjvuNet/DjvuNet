using System;
using BenchmarkDotNet.Attributes;
using DjvuNet.Graphics;
using DjvuNet.Wavelet;
using DjvuNet.DjvuLibre;
using DjvuNet.Benchmarks.Core;

namespace DjvuNet.Wavelet.Benchmarks
{
    [Config(typeof(StandardConfig))]
    public class YCbCr2RgbMatrixBenchmark : DjvuNetBenchmarkBase
    {
        [Params(1024, 4096, 9216, 16384, 36864, 65536, 262144, 1048576, 2096704, 4194304, MaxPixels)]
        public override int PixelCount { get; set; }

        protected override DjvuNetBenchmarkType BenchmarkType => DjvuNetBenchmarkType.ReverseYCbCrToRgb;

        [Benchmark(Baseline = true, OperationsPerInvoke = InvocationCount)]
        public unsafe void Native()
        {
            for (int i = 0; i < InvocationCount; i++)
            {
                for (int r = 0; r < ImageRatio; r++)
                {
                    GetIterationPointersYCbCr2Rgb(i, r, out Pixel* pOut);
                    NativeMethods.YCbCrToRgb((IntPtr)pOut, ImageWidth, ImageHeight, ImageWidth);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = InvocationCount)]
        public unsafe void Scalar()
        {
            for (int i = 0; i < InvocationCount; i++)
            {
                for (int r = 0; r < ImageRatio; r++)
                {
                    GetIterationPointersYCbCr2Rgb(i, r, out Pixel* pOut);
                    InterWaveTransform.YCbCr2RgbScalar(pOut, ImageWidth, ImageHeight, ImageWidth * 3);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = InvocationCount)]
        public unsafe void Simd()
        {
            for (int i = 0; i < InvocationCount; i++)
            {
                for (int r = 0; r < ImageRatio; r++)
                {
                    GetIterationPointersYCbCr2Rgb(i, r, out Pixel* pOut);
                    InterWaveTransform.YCbCr2Rgb(pOut, ImageWidth, ImageHeight, ImageWidth * 3);
                }
            }
        }
    }
}