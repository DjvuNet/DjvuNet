using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using DjvuNet.DjvuLibre;
using DjvuNet.JB2;
using DjvuNet.Benchmarks;
using DjvuNet.Tests;
using DjvuNet.Benchmarks.Core;
using System.IO;

namespace DjvuNet.JB2.Benchmarks
{
    [Config(typeof(ShortSimdMatrixConfig))]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class JB2ImageCodecBenchmark : JB2ImageBenchmarkBase
    {
        [Params("test032C_P01", "test033C_P01", "test033C_P25", "test049C_P01")]
        public override string TestFile { get; set; }

        [BenchmarkCategory("Decode"), Benchmark]
        public JB2Image DecodeJB2Image()
        {
            var image = new JB2Image();
            using (var ms = new MemoryStream(_sjbzPayload))
            using (var reader = new DjvuReader(ms))
            {
                image.Decode(reader, _sharedDict);
            }
            return image;
        }

        [BenchmarkCategory("Encode"), Benchmark]
        public void EncodeJB2Image()
        {
            _encodeStream.Position = 0;
            _decodedImage.Encode(_writer, _sharedDict);
        }

        [BenchmarkCategory("Decode"), Benchmark(Baseline = true)]
        public void NativeDecodeJB2Image()
        {
            GCHandle sjbzHandle = default;
            GCHandle djbzHandle = default;
            IntPtr nativeImageHandle = IntPtr.Zero;
            try
            {
                sjbzHandle = GCHandle.Alloc(_sjbzPayload, GCHandleType.Pinned);
                
                if (_djbzPayload != null)
                {
                    djbzHandle = GCHandle.Alloc(_djbzPayload, GCHandleType.Pinned);
                }

                NativeMethods.CreateDjvuJb2ImageFromChunk(
                    sjbzHandle.AddrOfPinnedObject(), _sjbzPayload.Length,
                    djbzHandle.IsAllocated ? djbzHandle.AddrOfPinnedObject() : IntPtr.Zero,
                    _djbzPayload?.Length ?? 0,
                    out nativeImageHandle);
            }
            finally
            {
                if (sjbzHandle.IsAllocated) sjbzHandle.Free();
                if (djbzHandle.IsAllocated) djbzHandle.Free();
                if (nativeImageHandle != IntPtr.Zero) NativeMethods.FreeDjvuJb2Image(nativeImageHandle);
            }
        }

        [BenchmarkCategory("Encode"), Benchmark(Baseline = true)]
        public void NativeEncodeJB2Image()
        {
            NativeMethods.EncodeDjvuJb2ImageToChunk(
                _nativeImageHandle, out IntPtr outData, out int outSize);

            if (outData != IntPtr.Zero)
                DjvuMarshal.FreeHGlobal(outData);
        }
    }
}
