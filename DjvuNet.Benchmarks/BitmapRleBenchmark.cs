using System;
using System.IO;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using DjvuNet.Graphics;
using DjvuNet.Tests;
using System.Collections.Concurrent;

using SysBitmap = System.Drawing.Bitmap;
using SysRectangle = System.Drawing.Rectangle;
using SysBitmapData = System.Drawing.Imaging.BitmapData;
using SysImageLockMode = System.Drawing.Imaging.ImageLockMode;
using SysPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace DjvuNet.Benchmarks
{
    // Custom Config using Out-Of-Process Jobs to force the JIT to respect CPU flag environment variables
    public class BitmapRleConfig : StandardConfig
    {
        public BitmapRleConfig() : base(false)
        {

            // 1.Baseline: Scalar(Disable all hardware intrinsics)
            AddJob(Job.Default
                .WithGcServer(true)
                .WithId("1. Scalar")
                .WithEnvironmentVariable("DOTNET_EnableHWIntrinsic", "0")
                .AsBaseline());

            // 2. Vector128: (SSE / SSE4.1) by disabling AVX and higher
            AddJob(Job.Default
                .WithGcServer(true)
                .WithId("2. Vector128")
                .WithEnvironmentVariable("DOTNET_EnableAVX", "0"));

            // 3. AVX2: Disable AVX-512 to restrict pipeline to 256-bit
            AddJob(Job.Default
                .WithGcServer(true)
                .WithId("3. AVX2")
                .WithEnvironmentVariable("DOTNET_EnableAVX512", "0"));

            // 4. AVX512: Unrestricted (Maximum Hardware Capabilities)
            AddJob(Job.Default
                .WithGcServer(true)
                .WithId("4. AVX512"));
        }
    }

    [Config(typeof(BitmapRleConfig))]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
    public class BitmapRleBenchmark : IThroughputBenchmark, ICompressionRatioBenchmark
    {
        // Thread-safe cache to prevent redundant disk I/O during report generation
        private static readonly ConcurrentDictionary<string, long> _byteSizeCache 
            = new ConcurrentDictionary<string, long>();

        private static readonly ConcurrentDictionary<string, double> _ratioCache 
            = new ConcurrentDictionary<string, double>();

        public long GetBytesPerOperation(BenchmarkCase benchmarkCase)
        {
            string maskName = benchmarkCase.Parameters["MaskName"] as string;
            
            return _byteSizeCache.GetOrAdd(maskName, name => 
            {
                string path = Path.Combine(Util.RepoRoot, "artifacts", "data", name);
                using (var img = new SysBitmap(path))
                {
                    return (long)img.Width * img.Height; // 1 byte per pixel
                }
            });
        }

        public double GetCompressionRatio(BenchmarkCase benchmarkCase)
        {
            string maskName = benchmarkCase.Parameters["MaskName"] as string;
            return _ratioCache.GetOrAdd(maskName, name => 
            {
                var bench = new BitmapRleBenchmark { MaskName = name };
                bench.Setup();
                
                // Compress the template that actually contains the image data!
                bench._compressTemplate.Compress();
                
                double uncompressed = bench.PixelCount;
                double compressed = bench._compressTemplate._RleData.Length;
                
                return uncompressed / compressed;
            });
        }

        [Params("test075Cmask.png", "test023Cmask.png")] 
        public string MaskName { get; set; }

        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }
        public int PixelCount => ImageWidth * ImageHeight;

        private sbyte[] _decodedData;
        private byte[] _rleRuns;
        private byte[] _encodedRunBuffer;
        private byte[] _rawRowBuffer;
        private Bitmap _compressTemplate;
        private Bitmap _decompressTemplate;
        private sbyte[] _preallocatedData;

        [GlobalSetup]
        public unsafe void Setup()
        {
            string path = Path.Combine(Util.RepoRoot, "artifacts", "data", MaskName);
            using (var img = new SysBitmap(path))
            {
                ImageWidth = img.Width;
                ImageHeight = img.Height;

                _decodedData = new sbyte[PixelCount + 4];
                _encodedRunBuffer = new byte[PixelCount + 1024]; 
                _rawRowBuffer = new byte[PixelCount];

                // Efficiently read pixels into a flat array via direct buffer access
                SysBitmapData bmpData = img.LockBits(
                    new SysRectangle(0, 0, ImageWidth, ImageHeight),
                    SysImageLockMode.ReadOnly,
                    SysPixelFormat.Format32bppArgb);

                try
                {
                    byte* pSrcBase = (byte*)bmpData.Scan0;
                    int stride = bmpData.Stride;
                    int i = 0;
                    for (int y = 0; y < ImageHeight; y++)
                    {
                        byte* pRow = pSrcBase + (y * stride);
                        for (int x = 0; x < ImageWidth; x++)
                        {
                            // Format32bppArgb stores as BGRA (Blue, Green, Red, Alpha)
                            // We use Red channel (offset 2) to determine black vs white
                            _rawRowBuffer[i++] = (pRow[x * 4 + 2] < 128) ? (byte)1 : (byte)0;
                        }
                    }
                }
                finally
                {
                    img.UnlockBits(bmpData);
                }

                // Pre-encode real RLE runs using our exact implementation
                _rleRuns = new byte[PixelCount * 2 + 1024];
                Bitmap bmp = new Bitmap();
                fixed (byte* pDest = _rleRuns)
                fixed (byte* pSrc = _rawRowBuffer)
                {
                    byte* ptr = pDest;
                    for (int y = 0; y < ImageHeight; y++)
                    {
                        bmp.AppendLine(ref ptr, pSrc + (y * ImageWidth), ImageWidth, false);
                    }
                }

                _compressTemplate = new Bitmap(ImageHeight, ImageWidth);
                for (int y = 0; y < ImageHeight; y++)
                {
                    ReadOnlySpan<byte> srcRow = new ReadOnlySpan<byte>(_rawRowBuffer, y * ImageWidth, ImageWidth);
                    Span<sbyte> destRow = new Span<sbyte>(_compressTemplate.GetRow(y), ImageWidth);
                    MemoryMarshal.Cast<byte, sbyte>(srcRow).CopyTo(destRow);
                }

                _decompressTemplate = new Bitmap(ImageHeight, ImageWidth);
                _decompressTemplate.Compress(); // Compressing clears Data, preparing it for Decompress
                
                long npixels = ImageHeight * (ImageWidth + _decompressTemplate.Border) + _decompressTemplate.Border;
                _preallocatedData = GC.AllocateUninitializedArray<sbyte>((int)npixels, pinned: true);
            }
        }

        [Benchmark]
        public unsafe void DecodeRleCore()
        {
            fixed (byte* pRuns = _rleRuns)
            {
                Bitmap.DecodeRleCore(pRuns, _rleRuns.Length, _decodedData, 0, ImageHeight, ImageWidth, ImageWidth);
            }
        }

        [Benchmark]
        public unsafe void AppendLine()
        {
            Bitmap bmp = new Bitmap();
            fixed (byte* pData = _encodedRunBuffer)
            fixed (byte* pRow = _rawRowBuffer)
            {
                byte* ptr = pData;
                for (int y = 0; y < ImageHeight; y++)
                {
                    bmp.AppendLine(ref ptr, pRow + (y * ImageWidth), ImageWidth, false);
                }
            }
        }

        [Benchmark]
        public void Decompress()
        {
            Bitmap bmp = _decompressTemplate;
            bmp._RleData = _rleRuns;
            bmp._Data = _preallocatedData;
            bmp.Decompress(forceOverwrite: true);
        }

        [Benchmark]
        public void Compress()
        {
            Bitmap bmp = _compressTemplate;
            bmp.Compress();
        }
    }
}
