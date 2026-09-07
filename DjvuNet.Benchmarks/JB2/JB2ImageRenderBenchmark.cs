using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DjvuNet.DjvuLibre;
using DjvuNet.Graphics;
using DjvuNet.Benchmarks;
using DjvuNet.Tests;

using DjvuNet.Benchmarks.Core;

namespace DjvuNet.JB2.Benchmarks
{
    [Config(typeof(ShortSimdMatrixConfig))]
    public class JB2ImageRenderBenchmark : JB2ImageBenchmarkBase, IThroughputBenchmark
    {
        private static readonly ConcurrentDictionary<string, long> _bytesCache = new ConcurrentDictionary<string, long>();

        [Params("test032C_P01", "test033C_P01", "test033C_P25", "test049C_P01", "test053C_P02", "test074C_P01")]
        public override string TestFile { get; set; }

        [Params(1, 2, 4, 8)]
        public int SubSample { get; set; }

        public long GetBytesPerOperation(BenchmarkCase benchmarkCase)
        {
            string testFile = benchmarkCase.Parameters["TestFile"] as string;
            int subSample = (int)benchmarkCase.Parameters["SubSample"];

            return _bytesCache.GetOrAdd($"{testFile}_{subSample}", key => 
            {
                using (var bench = new JB2ImageRenderBenchmark { TestFile = testFile, SubSample = subSample })
                {
                    string sjbzPath = System.IO.Path.Combine(Util.ArtifactsDataPath, "extracted", $"{testFile}.sjbz");
                    byte[] sjbzPayload = System.IO.File.ReadAllBytes(sjbzPath);
                    
                    var decodedImage = new JB2Image();
                    using (var ms = new System.IO.MemoryStream(sjbzPayload))
                    using (var reader = new DjvuReader(ms))
                    {
                        decodedImage.DecodeGeometry(reader);
                    }
                    
                    int width = decodedImage.Width;
                    int height = decodedImage.Height;
                    int align = 4;

                    int swidth = ((width + subSample) - 1) / subSample;
                    int sheight = ((height + subSample) - 1) / subSample;
                    int border = (((swidth + align) - 1) & ~(align - 1)) - swidth;

                    long bytes = (long)sheight * (swidth + border);

                    return bytes;
                }
            });
        }

        [Benchmark]
        public Bitmap RenderJB2Image()
        {
            return _decodedImage.GetBitmap(SubSample);
        }

        [Benchmark(Baseline = true)]
        public void NativeRenderJB2Image()
        {
            GCHandle bufferHandle = default;
            try
            {
                bufferHandle = GCHandle.Alloc(_nativeRenderBuffer, GCHandleType.Pinned);
                NativeMethods.GetDjvuJb2ImageBitmap(
                    _nativeImageHandle, SubSample, 4,
                    out int width, out int height, out int rowsize, out int border,
                    bufferHandle.AddrOfPinnedObject(), _nativeRenderBuffer.Length);
            }
            finally
            {
                if (bufferHandle.IsAllocated)
                    bufferHandle.Free();
            }
        }
    }
}
