using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DjvuNet.Graphics;
using DjvuNet.Wavelet;
using BenchmarkDotNet.Configs;

using DjvuNet.Benchmarks.Core;

namespace DjvuNet.Wavelet.Benchmarks
{
    [Config(typeof(CustomParallelConfig))]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
    public class YCbCr2RgbParallelSimdBenchmark : DjvuNetBenchmarkBase
    {
        [Params(1024, 4096, 9216, 16384, 36864, 65536, 262144, 1048576, 2096704, 4194304, MaxPixels)]
        public override int PixelCount { get; set; }

        protected override DjvuNetBenchmarkType BenchmarkType => DjvuNetBenchmarkType.ReverseYCbCrToRgb;

        [ParamsSource(nameof(ThreadCountValues))]
        public override int ThreadCount { get; set; }
        public override IEnumerable<int> ThreadCountValues => new[] { 1, 2, 4, 6, Environment.ProcessorCount };

        [IterationSetup]
        public override unsafe void IterationSetup()
        {
            base.IterationSetup();
            _options = new ParallelOptions { MaxDegreeOfParallelism = ThreadCount };
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = InvocationCount)]
        public unsafe void Vector256SingleThread()
        {
            if (!Avx2.IsSupported) return;
            int ratio = ImageRatio;
            int offsetPixels = PixelCount;
            int width = ImageWidth;
            int height = ImageHeight;
            int rowSizeInBytes = width * sizeof(Pixel);

            for (int i = 0; i < InvocationCount; i++)
            {
                Pixel* basePointer = (Pixel*)_nativePixelBuffers[i].ToPointer();

                for (int j = 0; j < ratio; j++)
                {
                    int pixelOffset = j * offsetPixels;
                    InterWaveSimd.YCbCr2RgbVector256(basePointer + pixelOffset, width, height, rowSizeInBytes);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = InvocationCount)]
        public unsafe void Vector256Parallel()
        {
            if (!Avx2.IsSupported) return;
            int ratio = ImageRatio;
            int offsetPixels = PixelCount;
            int width = ImageWidth;
            int height = ImageHeight;
            int rowSizeInBytes = width * sizeof(Pixel);

            for (int i = 0; i < InvocationCount; i++)
            {
                Pixel* basePointer = (Pixel*)_nativePixelBuffers[i].ToPointer();

                for (int j = 0; j < ratio; j++)
                {
                    int pixelOffset = j * offsetPixels;
                    InterWaveSimd.YCbCr2RgbParallelVector256(basePointer + pixelOffset, width, height, rowSizeInBytes, _options);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = InvocationCount)]
        public unsafe void Vector128SingleThread()
        {
            if (!Ssse3.IsSupported && !AdvSimd.IsSupported) return;
            int ratio = ImageRatio;
            int offsetPixels = PixelCount;
            int width = ImageWidth;
            int height = ImageHeight;
            int rowSizeInBytes = width * sizeof(Pixel);

            for (int i = 0; i < InvocationCount; i++)
            {
                Pixel* basePointer = (Pixel*)_nativePixelBuffers[i].ToPointer();

                for (int j = 0; j < ratio; j++)
                {
                    int pixelOffset = j * offsetPixels;
                    InterWaveSimd.YCbCr2RgbVector128(basePointer + pixelOffset, width, height, rowSizeInBytes);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = InvocationCount)]
        public unsafe void Vector128Parallel()
        {
            if (!Ssse3.IsSupported && !AdvSimd.IsSupported) return;
            int ratio = ImageRatio;
            int offsetPixels = PixelCount;
            int width = ImageWidth;
            int height = ImageHeight;
            int rowSizeInBytes = width * sizeof(Pixel);

            for (int i = 0; i < InvocationCount; i++)
            {
                Pixel* basePointer = (Pixel*)_nativePixelBuffers[i].ToPointer();

                for (int j = 0; j < ratio; j++)
                {
                    int pixelOffset = j * offsetPixels;
                    InterWaveSimd.YCbCr2RgbParallelVector128(basePointer + pixelOffset, width, height, rowSizeInBytes, _options);
                }
            }
        }
    }
}