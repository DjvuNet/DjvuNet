using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Formats.Tar;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.Tests;
using DjvuNet.DjvuLibre;
using Bitmap = System.Drawing.Bitmap;
using Rectangle = System.Drawing.Rectangle;

namespace DjvuNet.Benchmarks
{
    public enum DjvuNetBenchmarkType
    {
        ForwardRgbToYCbCr,
        ReverseYCbCrToRgb,
        ImageBinaryDiff
    }

    public abstract class DjvuNetBenchmarkBase : IThroughputBenchmark
    {
        protected const int InvocationCount = 10;
        protected const int MaxWidth = 5448;
        protected const int MaxHeight = 3686;
        protected const int MaxPixels = MaxWidth * MaxHeight;

        public unsafe virtual long GetBytesPerOperation(BenchmarkCase benchmarkCase)
        {
            // Default payload: 1x MaxPixels array at the exact unmanaged struct size.
            return MaxPixels * sizeof(Pixel); 
        }

        protected IntPtr[] _nativePixelBuffers;
        protected IntPtr[] _nativePlanarBuffers;
        protected IntPtr _masterBackupBuffer;
        
        protected byte[] _managedBuffer1;
        protected byte[] _managedBuffer2;

        public abstract int PixelCount { get; set; }
        protected abstract DjvuNetBenchmarkType BenchmarkType { get; }

        public int ImageWidth => PixelCount switch
        {
            1024 => 32,
            4096 => 64,
            9216 => 96,
            16384 => 128,
            36864 => 192,
            65536 => 256,
            262144 => 512,
            1048576 => 1024,
            2096704 => 1448,
            4194304 => 2048,
            _ => MaxWidth
        };

        public int ImageHeight => PixelCount switch
        {
            1024 => 32,
            4096 => 64,
            9216 => 96,
            16384 => 128,
            36864 => 192,
            65536 => 256,
            262144 => 512,
            1048576 => 1024,
            2096704 => 1448,
            4194304 => 2048,
            _ => MaxHeight
        };

        public int ImageRatio => MaxPixels / PixelCount;

        [ParamsSource(nameof(ThreadCountValues))]
        public virtual int ThreadCount { get; set; }

        public virtual IEnumerable<int> ThreadCountValues => new[] { /*2, 4, 6, */ Environment.ProcessorCount };
        protected ParallelOptions _options;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected unsafe void GetIterationPointersYCbCr2Rgb(int invocationIndex, int ratioIndex, out Pixel* pOut)
        {
            Pixel* basePixelPointer = (Pixel*)_nativePixelBuffers[invocationIndex].ToPointer();
            int pixelOffset = ratioIndex * PixelCount;
            pOut = basePixelPointer + pixelOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected unsafe void GetIterationPointersRgb2YCbCr(int invocationIndex, int ratioIndex, out Pixel* pRgb, out sbyte* pY, out sbyte* pCb, out sbyte* pCr)
        {
            Pixel* basePixelPointer = (Pixel*)_nativePixelBuffers[invocationIndex].ToPointer();
            sbyte* basePlanarPointer = (sbyte*)_nativePlanarBuffers[invocationIndex].ToPointer();

            int pixelOffset = ratioIndex * PixelCount;
            int planarOffset = pixelOffset * 3; // 3 planes

            pRgb = basePixelPointer + pixelOffset;
            pY = basePlanarPointer + planarOffset;
            pCb = pY + PixelCount;
            pCr = pCb + PixelCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected unsafe void GetIterationPointersImageBinaryDiff(int ratioIndex, int stride, byte* pinnedBase1, byte* pinnedBase2, out byte* p1, out byte* p2)
        {
            int byteOffset = ratioIndex * ImageHeight * stride;
            p1 = pinnedBase1 + byteOffset;
            p2 = pinnedBase2 + byteOffset;
        }

        [GlobalSetup]
        public unsafe virtual void GlobalSetup()
        {
            long pixelBytes = MaxPixels * sizeof(Pixel);
            long planarBytes = MaxPixels * 3 * sizeof(sbyte);

            if (BenchmarkType == DjvuNetBenchmarkType.ImageBinaryDiff)
            {
                string imagePath = Path.Combine(Util.ArtifactsPath, "TitanIR-24bgr.png");
                if (!File.Exists(imagePath)) throw new FileNotFoundException($"Benchmark artifact not found: {imagePath}");

                using (var bmp = new Bitmap(imagePath))
                {
                    var rect = new Rectangle(0, 0, MaxWidth, MaxHeight);
                    BitmapData data = null;
                    try
                    {
                        data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                        int totalBytes = data.Stride * MaxHeight;

                        _managedBuffer1 = GC.AllocateUninitializedArray<byte>(totalBytes);
                        _managedBuffer2 = GC.AllocateUninitializedArray<byte>(totalBytes);

                        Marshal.Copy(data.Scan0, _managedBuffer1, 0, totalBytes);
                        Marshal.Copy(data.Scan0, _managedBuffer2, 0, totalBytes);
                    }
                    finally
                    {
                        if (data != null)
                        {
                            bmp.UnlockBits(data);
                        }
                    }
                }
                return; // Early exit for ImageBinaryDiff, skip unmanaged allocations below
            }

            _nativePixelBuffers = new IntPtr[InvocationCount];
            if (BenchmarkType == DjvuNetBenchmarkType.ForwardRgbToYCbCr)
            {
                _nativePlanarBuffers = new IntPtr[InvocationCount];
            }

            try
            {
                _masterBackupBuffer = DjvuMarshal.AllocHGlobal((uint)pixelBytes);

                if (BenchmarkType == DjvuNetBenchmarkType.ForwardRgbToYCbCr)
                {
                    string imagePath = Path.Combine(Util.ArtifactsPath, "TitanIR-24bgr.png");
                    if (!File.Exists(imagePath)) throw new FileNotFoundException($"Benchmark artifact not found: {imagePath}");

                    using (var bmp = new Bitmap(imagePath))
                    {
                        var rect = new Rectangle(0, 0, MaxWidth, MaxHeight);
                        BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                        try
                        {
                            Buffer.MemoryCopy(data.Scan0.ToPointer(), _masterBackupBuffer.ToPointer(), pixelBytes, pixelBytes);
                        }
                        finally
                        {
                            bmp.UnlockBits(data);
                        }
                    }
                }
                else if (BenchmarkType == DjvuNetBenchmarkType.ReverseYCbCrToRgb)
                {
                    string artifactPath = Path.Combine(Util.ArtifactsPath, "TitanIR-5448x3686-24bpp-YCbCr.bin.tar.gz");
                    if (!File.Exists(artifactPath)) throw new FileNotFoundException($"Benchmark artifact not found: {artifactPath}");

                    using (FileStream fs = new FileStream(artifactPath, FileMode.Open, FileAccess.Read))
                    using (GZipStream gzip = new GZipStream(fs, CompressionMode.Decompress))
                    using (TarReader tar = new TarReader(gzip))
                    {
                        TarEntry entry = tar.GetNextEntry();
                        if (entry == null || entry.DataStream == null)
                        {
                            throw new DjvuFormatException("No valid file found in the tar.gz archive.");
                        }

                        byte[] chunk = new byte[81920];
                        int bytesRead;
                        int offset = 0;
                        while ((bytesRead = entry.DataStream.Read(chunk, 0, chunk.Length)) > 0)
                        {
                            if (offset + bytesRead > pixelBytes)
                            {
                                throw new DjvuInvalidOperationException("Decompressed stream size exceeds the allocated unmanaged buffer size.");
                            }
                            Marshal.Copy(chunk, 0, _masterBackupBuffer + offset, bytesRead);
                            offset += bytesRead;
                        }
                    }
                }
                else
                {
                    throw new DjvuInvalidOperationException($"Invalid or unsupported DjvuNetBenchmarkType: {BenchmarkType}");
                }

                for (int i = 0; i < InvocationCount; i++)
                {
                    _nativePixelBuffers[i] = DjvuMarshal.AllocHGlobal((uint)pixelBytes);
                    if (BenchmarkType == DjvuNetBenchmarkType.ForwardRgbToYCbCr)
                    {
                        _nativePlanarBuffers[i] = DjvuMarshal.AllocHGlobal((uint)planarBytes);
                    }
                }
            }
            catch
            {
                GlobalCleanup();
                throw;
            }
        }

        [IterationSetup]
        public unsafe virtual void IterationSetup()
        {
            if (BenchmarkType != DjvuNetBenchmarkType.ImageBinaryDiff)
            {
                long pixelBytes = MaxPixels * sizeof(Pixel);
                for (int i = 0; i < InvocationCount; i++)
                {
                    Buffer.MemoryCopy(_masterBackupBuffer.ToPointer(), _nativePixelBuffers[i].ToPointer(), pixelBytes, pixelBytes);
                }
            }
            _options = new ParallelOptions { MaxDegreeOfParallelism = ThreadCount };
        }

        [GlobalCleanup]
        public virtual void GlobalCleanup()
        {
            if (_masterBackupBuffer != IntPtr.Zero)
            {
                DjvuMarshal.FreeHGlobal(_masterBackupBuffer);
                _masterBackupBuffer = IntPtr.Zero;
            }

            if (_nativePixelBuffers != null)
            {
                for (int i = 0; i < InvocationCount; i++)
                {
                    if (_nativePixelBuffers[i] != IntPtr.Zero)
                    {
                        DjvuMarshal.FreeHGlobal(_nativePixelBuffers[i]);
                        _nativePixelBuffers[i] = IntPtr.Zero;
                    }
                }
            }

            if (_nativePlanarBuffers != null)
            {
                for (int i = 0; i < InvocationCount; i++)
                {
                    if (_nativePlanarBuffers[i] != IntPtr.Zero)
                    {
                        DjvuMarshal.FreeHGlobal(_nativePlanarBuffers[i]);
                        _nativePlanarBuffers[i] = IntPtr.Zero;
                    }
                }
            }
        }
    }
}