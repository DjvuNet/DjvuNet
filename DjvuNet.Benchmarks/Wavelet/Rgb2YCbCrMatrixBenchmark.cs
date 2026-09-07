using System;
using BenchmarkDotNet.Attributes;
using DjvuNet.Graphics;
using DjvuNet.Wavelet;
using DjvuNet.DjvuLibre;
using DjvuNet.Benchmarks.Core;

namespace DjvuNet.Wavelet.Benchmarks
{
    [Config(typeof(StandardConfig))]
    public class Rgb2YCbCrMatrixBenchmark : DjvuNetBenchmarkBase
    {
        [Params(1024, 4096, 9216, 16384, 36864, 65536, 262144, 1048576, 2096704, 4194304, MaxPixels)]
        public override int PixelCount { get; set; }

        protected override DjvuNetBenchmarkType BenchmarkType => DjvuNetBenchmarkType.ForwardRgbToYCbCr;

        [Benchmark(Baseline = true, OperationsPerInvoke = InvocationCount)]
        public unsafe void Native()
        {
            for (int i = 0; i < InvocationCount; i++)
            {
                for (int r = 0; r < ImageRatio; r++)
                {
                    GetIterationPointersRgb2YCbCr(i, r, out Pixel* pRgb, out sbyte* pY, out sbyte* pCb, out sbyte* pCr);
                    NativeMethods.RgbToYCbCr((IntPtr)pRgb, ImageWidth, ImageHeight, ImageWidth * 3, (IntPtr)pY, (IntPtr)pCb, (IntPtr)pCr, ImageWidth);
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
                    GetIterationPointersRgb2YCbCr(i, r, out Pixel* pRgb, out sbyte* pY, out sbyte* pCb, out sbyte* pCr);
                    InterWaveTransform.Rgb2YCbCrScalar(pRgb, ImageWidth, ImageHeight, ImageWidth * 3, pY, pCb, pCr, ImageWidth);
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
                    GetIterationPointersRgb2YCbCr(i, r, out Pixel* pRgb, out sbyte* pY, out sbyte* pCb, out sbyte* pCr);
                    InterWaveTransform.Rgb2YCbCr(pRgb, ImageWidth, ImageHeight, ImageWidth * 3, pY, pCb, pCr, ImageWidth);
                }
            }
        }
    }
}