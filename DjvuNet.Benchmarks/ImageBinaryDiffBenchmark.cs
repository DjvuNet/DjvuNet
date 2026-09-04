using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using DjvuNet.Graphics;
using DjvuNet.Tests;
using BenchmarkDotNet.Running;

namespace DjvuNet.Benchmarks
{
    [Config(typeof(CustomParallelConfig))]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
    public class ImageBinaryDiffBenchmark : DjvuNetBenchmarkBase
    {
        [Params(1024, 4096, 9216, 16384, 36864, 65536, 262144, 1048576, 2096704,  4194304, MaxPixels)]
        public override int PixelCount { get; set; }

        public override IEnumerable<int> ThreadCountValues => new[] { 2 , 4, 6, Environment.ProcessorCount };

        protected override DjvuNetBenchmarkType BenchmarkType => DjvuNetBenchmarkType.ImageBinaryDiff;

        public override long GetBytesPerOperation(BenchmarkCase benchmarkCase)
        {
            // Diff reads from both Buffer 1 and Buffer 2 simultaneously (2x memory bandwidth)
            int pixelSizeBits = 24; 
            long bytesPerImage = MaxPixels * (pixelSizeBits / 8);
            return bytesPerImage * 2; 
        }

        // Use a local constant to increase workload specifically for this fast benchmark
        private const int DiffInvocationCount = 5;

        [Benchmark(Baseline = true, OperationsPerInvoke = DiffInvocationCount)]
        public unsafe void Scalar()
        {
            int stride = ImageWidth * sizeof(Pixel);

            fixed (byte* pinnedBase1 = _managedBuffer1)
            fixed (byte* pinnedBase2 = _managedBuffer2)
            {
                for (int i = 0; i < DiffInvocationCount; i++)
                {
                    for (int j = 0; j < ImageRatio; j++)
                    {
                        GetIterationPointersImageBinaryDiff(j, stride, pinnedBase1, pinnedBase2, out byte* p1, out byte* p2);

                        Util.ImageBinaryDiffScalar(
                            p1,
                            p2,
                            (uint)ImageWidth,
                            (uint)ImageHeight,
                            stride
                        );
                    }
                }
            }
        }

        [Benchmark(OperationsPerInvoke = DiffInvocationCount)]
        public unsafe void Vector256()
        {
            if (!Avx2.IsSupported) return;

            int stride = ImageWidth * sizeof(Pixel);

            fixed (byte* pinnedBase1 = _managedBuffer1)
            fixed (byte* pinnedBase2 = _managedBuffer2)
            {
                for (int i = 0; i < DiffInvocationCount; i++)
                {
                    for (int j = 0; j < ImageRatio; j++)
                    {
                        GetIterationPointersImageBinaryDiff(j, stride, pinnedBase1, pinnedBase2, out byte* p1, out byte* p2);

                        Util.ImageDiffVector256(p1, p2, (uint)ImageWidth, (uint)ImageHeight, stride);
                    }
                }
            }
        }

        [Benchmark(OperationsPerInvoke = DiffInvocationCount)]
        public unsafe void Vector128()
        {
            if (!System.Runtime.Intrinsics.Vector128.IsHardwareAccelerated) return;

            int stride = ImageWidth * sizeof(Pixel);

            fixed (byte* pinnedBase1 = _managedBuffer1)
            fixed (byte* pinnedBase2 = _managedBuffer2)
            {
                for (int i = 0; i < DiffInvocationCount; i++)
                {
                    for (int j = 0; j < ImageRatio; j++)
                    {
                        GetIterationPointersImageBinaryDiff(j, stride, pinnedBase1, pinnedBase2, out byte* p1, out byte* p2);

                        Util.ImageDiffVector128(p1, p2, (uint)ImageWidth, (uint)ImageHeight, stride);
                    }
                }
            }
        }

        [Benchmark(OperationsPerInvoke = DiffInvocationCount)]
        public unsafe void Parallel256()
        {
            if (!Avx2.IsSupported) return;

            int stride = ImageWidth * sizeof(Pixel);

            fixed (byte* pinnedBase1 = _managedBuffer1)
            fixed (byte* pinnedBase2 = _managedBuffer2)
            {
                for (int i = 0; i < DiffInvocationCount; i++)
                {
                    for (int j = 0; j < ImageRatio; j++)
                    {
                        GetIterationPointersImageBinaryDiff(j, stride, pinnedBase1, pinnedBase2, out byte* p1, out byte* p2);

                        Util.ImageDiffParallel256(p1, p2, (uint)ImageWidth, (uint)ImageHeight, stride, _options);
                    }
                }
            }
        }

        [Benchmark(OperationsPerInvoke = DiffInvocationCount)]
        public unsafe void Parallel128()
        {
            if (!System.Runtime.Intrinsics.Vector128.IsHardwareAccelerated) return;

            int stride = ImageWidth * sizeof(Pixel);

            fixed (byte* pinnedBase1 = _managedBuffer1)
            fixed (byte* pinnedBase2 = _managedBuffer2)
            {
                for (int i = 0; i < DiffInvocationCount; i++)
                {
                    for (int j = 0; j < ImageRatio; j++)
                    {
                        GetIterationPointersImageBinaryDiff(j, stride, pinnedBase1, pinnedBase2, out byte* p1, out byte* p2);

                        Util.ImageDiffParallel128(p1, p2, (uint)ImageWidth, (uint)ImageHeight, stride, _options);
                    }
                }
            }
        }
    }
}