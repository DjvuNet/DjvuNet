using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using DjvuNet.Graphics;
using DjvuNet.DjvuLibre;
using DjvuNet.Wavelet;
using DjvuNet.Benchmarks.Core;

namespace DjvuNet.Wavelet.Benchmarks
{
    [Config(typeof(CustomParallelConfig))]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
    public class YCbCrParallelSimdBenchmark : DjvuNetBenchmarkBase
    {
        [Params(1024, 4096, 9216, 16384, 36864, 65536, 262144, 1048576, 2096704, 4194304, MaxPixels)]
        public override int PixelCount { get; set; }

        public override IEnumerable<int> ThreadCountValues => new[] { 2, 4, 6, 12 };

        protected override DjvuNetBenchmarkType BenchmarkType => DjvuNetBenchmarkType.ForwardRgbToYCbCr;

        [Benchmark(Baseline = true, OperationsPerInvoke = InvocationCount)]
        public unsafe void HybridSingleThread()
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
                    InterWaveSimd.Rgb2YCbCrHybridVector(pIn, width, height, inRowSizeInBytes, pY, pCb, pCr, width);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = InvocationCount)]
        public unsafe void HybridMultiThread_ParallelFor()
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
                    InterWaveSimd.Rgb2YCbCrParallelHybridVector(pIn, width, height, inRowSizeInBytes, pY, pCb, pCr, width, _options);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = InvocationCount)]
        public unsafe void MemoryBandwidthParallelVector128()
        {
            int ratio = ImageRatio;
            int width = ImageWidth;
            int height = ImageHeight;
            int outRowSizeInBytes = width;
            int rowSizeInBytes = width * sizeof(Pixel);

            for (int i = 0; i < InvocationCount; i++)
            {
                for (int j = 0; j < ratio; j++)
                {
                    GetIterationPointersRgb2YCbCr(i, j, out Pixel* pIn, out sbyte* pY, out sbyte* pCb, out sbyte* pCr);

                    Parallel.For(0, height, _options, y =>
                    {
                        long inOffset = (long)y * rowSizeInBytes;
                        long outOffset = (long)y * outRowSizeInBytes;

                        Pixel* pInRow = (Pixel*)((byte*)pIn + inOffset);
                        sbyte* pYRow = pY + outOffset;
                        sbyte* pCbRow = pCb + outOffset;
                        sbyte* pCrRow = pCr + outOffset;

                        var dummyData = Vector128<sbyte>.Zero;

                        int x = 0;
                        while (x <= width - 16)
                        {
                            byte* readPtr = (byte*)pInRow;
                            var vec0 = Vector128.Load(readPtr);
                            var vec1 = Vector128.Load(readPtr + 16);
                            var vec2 = Vector128.Load(readPtr + 32);

                            dummyData.AsByte().Store((byte*)pYRow);
                            dummyData.AsByte().Store((byte*)pCbRow);
                            dummyData.AsByte().Store((byte*)pCrRow);

                            pInRow += 16;
                            pYRow += 16;
                            pCbRow += 16;
                            pCrRow += 16;
                            x += 16;
                        }
                    });
                }
            }
        }

        [Benchmark(OperationsPerInvoke = InvocationCount)]
        public unsafe void MemoryBandwidthParallelVector256()
        {
            if (!Avx2.IsSupported) return;

            int ratio = ImageRatio;
            int width = ImageWidth;
            int height = ImageHeight;
            int outRowSizeInBytes = width;
            int rowSizeInBytes = width * sizeof(Pixel);

            for (int i = 0; i < InvocationCount; i++)
            {
                for (int j = 0; j < ratio; j++)
                {
                    GetIterationPointersRgb2YCbCr(i, j, out Pixel* pIn, out sbyte* pY, out sbyte* pCb, out sbyte* pCr);

                    Parallel.For(0, height, _options, y =>
                    {
                        long inOffset = (long)y * rowSizeInBytes;
                        long outOffset = (long)y * outRowSizeInBytes;

                        Pixel* pInRow = (Pixel*)((byte*)pIn + inOffset);
                        sbyte* pYRow = pY + outOffset;
                        sbyte* pCbRow = pCb + outOffset;
                        sbyte* pCrRow = pCr + outOffset;

                        var dummyData = Vector256<sbyte>.Zero;

                        int x = 0;
                        while (x <= width - 32)
                        {
                            byte* readPtr = (byte*)pInRow;
                            var vecA = Vector256.Load(readPtr);
                            var vecF = Vector256.Load(readPtr + 32);
                            var vecB = Vector256.Load(readPtr + 64);

                            dummyData.AsByte().Store((byte*)pYRow);
                            dummyData.AsByte().Store((byte*)pCbRow);
                            dummyData.AsByte().Store((byte*)pCrRow);

                            pInRow += 32;
                            pYRow += 32;
                            pCbRow += 32;
                            pCrRow += 32;
                            x += 32;
                        }
                    });
                }
            }
        }
    }
}