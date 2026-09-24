using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using DjvuNet.DjvuLibre;
using DjvuNet.Graphics;
using DjvuNet.Tests;
using DjvuNet.Wavelet;
using Xunit;

namespace DjvuNet.DjvuLibre.Compatibility.Tests
{
    /// <summary>
    /// TODO: consolidate common initialization teardown logic across these tests.
    /// </summary>
    public class PigeonHybridTransformTests
    {
        public const int Seed = 42;
        public const int DefaultWidth = 1472;
        public const int DefaultHeight = 1905;

        [Fact]
        public unsafe void Rgb2YCbCrHybridVector_NativeParityRandomData()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            int width = DefaultWidth;
            int height = DefaultHeight;
            int rowSize = width;
            int outRowSize = width;
            int totalPixels = height * rowSize;
            int totalBytes = totalPixels * 3;

            byte[] nativeRgbBuffer = new byte[totalBytes];
            byte[] managedRgbBuffer = new byte[totalBytes];

            Random rnd = new Random(Seed);
            for (int i = 0; i < totalBytes; i++)
            {
                byte val = (byte)rnd.Next(256);
                nativeRgbBuffer[i] = val;
                managedRgbBuffer[i] = val;
            }

            sbyte[] hybridOutY = new sbyte[totalPixels];
            sbyte[] hybridOutCb = new sbyte[totalPixels];
            sbyte[] hybridOutCr = new sbyte[totalPixels];

            byte[] nativeOutY = new byte[totalPixels];
            byte[] nativeOutCb = new byte[totalPixels];
            byte[] nativeOutCr = new byte[totalPixels];

            // 1. Process via Hybrid AVX2 C#
            fixed (byte* ptr = managedRgbBuffer)
            fixed (sbyte* outY = hybridOutY, outCb = hybridOutCb, outCr = hybridOutCr)
            {
                int inRowBytes = rowSize * sizeof(Pixel);
                InterWaveSimd.Rgb2YCbCrHybridVector((Pixel*)ptr, width, height, inRowBytes, outY, outCb, outCr, outRowSize);
            }

            // 2. Process via Native C++ DjVuLibre
            IntPtr nativeRgbPtr = IntPtr.Zero;
            IntPtr nativeYPtr = IntPtr.Zero;
            IntPtr nativeCbPtr = IntPtr.Zero;
            IntPtr nativeCrPtr = IntPtr.Zero;
            try
            {
                nativeRgbPtr = DjvuMarshal.AllocHGlobal((uint)totalBytes);
                nativeYPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);
                nativeCbPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);
                nativeCrPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);

                new Span<byte>(nativeYPtr.ToPointer(), totalPixels).Clear();
                new Span<byte>(nativeCbPtr.ToPointer(), totalPixels).Clear();
                new Span<byte>(nativeCrPtr.ToPointer(), totalPixels).Clear();

                Marshal.Copy(nativeRgbBuffer, 0, nativeRgbPtr, totalBytes);

                Assert.True(NativeMethods.RgbToY(nativeRgbPtr, width, height, rowSize, nativeYPtr, outRowSize));
                Assert.True(NativeMethods.RgbToCb(nativeRgbPtr, width, height, rowSize, nativeCbPtr, outRowSize));
                Assert.True(NativeMethods.RgbToCr(nativeRgbPtr, width, height, rowSize, nativeCrPtr, outRowSize));

                Marshal.Copy(nativeYPtr, nativeOutY, 0, totalPixels);
                Marshal.Copy(nativeCbPtr, nativeOutCb, 0, totalPixels);
                Marshal.Copy(nativeCrPtr, nativeOutCr, 0, totalPixels);
            }
            finally
            {
                if (nativeRgbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeRgbPtr);
                if (nativeYPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeYPtr);
                if (nativeCbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCbPtr);
                if (nativeCrPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCrPtr);
            }

            // 3. Assert Byte-for-Byte Parity
            fixed (sbyte* pUnY = hybridOutY, pUnCb = hybridOutCb, pUnCr = hybridOutCr)
            fixed (byte* pNtY = nativeOutY, pNtCb = nativeOutCb, pNtCr = nativeOutCr)
            {
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnY, pNtY, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnCb, pNtCb, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnCr, pNtCr, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit));
            }
        }

        [Fact]
        public unsafe void Rgb2YCbCrParallelHybridVector_NativeParityRandomData()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            int width = DefaultWidth;
            int height = DefaultHeight;
            int rowSize = width;
            int outRowSize = width;
            int totalPixels = height * rowSize;
            int totalBytes = totalPixels * 3;

            byte[] nativeRgbBuffer = new byte[totalBytes];
            byte[] managedRgbBuffer = new byte[totalBytes];

            Random rnd = new Random(Seed);
            for (int i = 0; i < totalBytes; i++)
            {
                byte val = (byte)rnd.Next(256);
                nativeRgbBuffer[i] = val;
                managedRgbBuffer[i] = val;
            }

            sbyte[] hybridOutY = new sbyte[totalPixels];
            sbyte[] hybridOutCb = new sbyte[totalPixels];
            sbyte[] hybridOutCr = new sbyte[totalPixels];

            byte[] nativeOutY = new byte[totalPixels];
            byte[] nativeOutCb = new byte[totalPixels];
            byte[] nativeOutCr = new byte[totalPixels];

            // 1. Process via Parallel Hybrid AVX2 C#
            fixed (byte* ptr = managedRgbBuffer)
            fixed (sbyte* outY = hybridOutY, outCb = hybridOutCb, outCr = hybridOutCr)
            {
                int inRowBytes = rowSize * sizeof(Pixel);
                var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };
                InterWaveSimd.Rgb2YCbCrParallelHybridVector((Pixel*)ptr, width, height, inRowBytes, outY, outCb, outCr, outRowSize, options);
            }

            // 2. Process via Native C++ DjVuLibre
            IntPtr nativeRgbPtr = IntPtr.Zero;
            IntPtr nativeYPtr = IntPtr.Zero;
            IntPtr nativeCbPtr = IntPtr.Zero;
            IntPtr nativeCrPtr = IntPtr.Zero;
            try
            {
                nativeRgbPtr = DjvuMarshal.AllocHGlobal((uint)totalBytes);
                nativeYPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);
                nativeCbPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);
                nativeCrPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);

                new Span<byte>(nativeYPtr.ToPointer(), totalPixels).Clear();
                new Span<byte>(nativeCbPtr.ToPointer(), totalPixels).Clear();
                new Span<byte>(nativeCrPtr.ToPointer(), totalPixels).Clear();

                Marshal.Copy(nativeRgbBuffer, 0, nativeRgbPtr, totalBytes);

                Assert.True(NativeMethods.RgbToY(nativeRgbPtr, width, height, rowSize, nativeYPtr, outRowSize));
                Assert.True(NativeMethods.RgbToCb(nativeRgbPtr, width, height, rowSize, nativeCbPtr, outRowSize));
                Assert.True(NativeMethods.RgbToCr(nativeRgbPtr, width, height, rowSize, nativeCrPtr, outRowSize));

                Marshal.Copy(nativeYPtr, nativeOutY, 0, totalPixels);
                Marshal.Copy(nativeCbPtr, nativeOutCb, 0, totalPixels);
                Marshal.Copy(nativeCrPtr, nativeOutCr, 0, totalPixels);
            }
            finally
            {
                if (nativeRgbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeRgbPtr);
                if (nativeYPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeYPtr);
                if (nativeCbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCbPtr);
                if (nativeCrPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCrPtr);
            }

            // 3. Assert Byte-for-Byte Parity
            fixed (sbyte* pUnY = hybridOutY, pUnCb = hybridOutCb, pUnCr = hybridOutCr)
            fixed (byte* pNtY = nativeOutY, pNtCb = nativeOutCb, pNtCr = nativeOutCr)
            {
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnY, pNtY, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnCb, pNtCb, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnCr, pNtCr, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit));
            }
        }

        [Fact]
        public unsafe void Rgb2YCbCrHybridVector_NativeParityWithStride()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            int width = DefaultWidth;
            int height = DefaultHeight;
            int paddingPixels = 17;
            int rowSize = width + paddingPixels; // Input stride
            int outRowSize = width + 5;          // Output stride

            int totalInPixels = height * rowSize;
            int totalInBytes = totalInPixels * 3;
            int totalOutPixels = height * outRowSize;

            byte[] nativeRgbBuffer = new byte[totalInBytes];
            byte[] managedRgbBuffer = new byte[totalInBytes];

            Random rnd = new Random(Seed);
            for (int i = 0; i < totalInBytes; i++)
            {
                byte val = (byte)rnd.Next(256);
                nativeRgbBuffer[i] = val;
                managedRgbBuffer[i] = val;
            }

            sbyte[] hybridOutY = new sbyte[totalOutPixels];
            sbyte[] hybridOutCb = new sbyte[totalOutPixels];
            sbyte[] hybridOutCr = new sbyte[totalOutPixels];

            byte[] nativeOutY = new byte[totalOutPixels];
            byte[] nativeOutCb = new byte[totalOutPixels];
            byte[] nativeOutCr = new byte[totalOutPixels];

            // 1. Process via Hybrid AVX2 C#
            fixed (byte* ptr = managedRgbBuffer)
            fixed (sbyte* outY = hybridOutY, outCb = hybridOutCb, outCr = hybridOutCr)
            {
                int inRowBytes = rowSize * sizeof(Pixel);
                InterWaveSimd.Rgb2YCbCrHybridVector((Pixel*)ptr, width, height, inRowBytes, outY, outCb, outCr, outRowSize);
            }

            // 2. Process via Native C++ DjVuLibre
            IntPtr nativeRgbPtr = IntPtr.Zero;
            IntPtr nativeYPtr = IntPtr.Zero;
            IntPtr nativeCbPtr = IntPtr.Zero;
            IntPtr nativeCrPtr = IntPtr.Zero;
            try
            {
                nativeRgbPtr = DjvuMarshal.AllocHGlobal((uint)totalInBytes);
                nativeYPtr = DjvuMarshal.AllocHGlobal((uint)totalOutPixels);
                nativeCbPtr = DjvuMarshal.AllocHGlobal((uint)totalOutPixels);
                nativeCrPtr = DjvuMarshal.AllocHGlobal((uint)totalOutPixels);

                new Span<byte>(nativeYPtr.ToPointer(), totalOutPixels).Clear();
                new Span<byte>(nativeCbPtr.ToPointer(), totalOutPixels).Clear();
                new Span<byte>(nativeCrPtr.ToPointer(), totalOutPixels).Clear();

                Marshal.Copy(nativeRgbBuffer, 0, nativeRgbPtr, totalInBytes);

                Assert.True(NativeMethods.RgbToY(nativeRgbPtr, width, height, rowSize, nativeYPtr, outRowSize));
                Assert.True(NativeMethods.RgbToCb(nativeRgbPtr, width, height, rowSize, nativeCbPtr, outRowSize));
                Assert.True(NativeMethods.RgbToCr(nativeRgbPtr, width, height, rowSize, nativeCrPtr, outRowSize));

                Marshal.Copy(nativeYPtr, nativeOutY, 0, totalOutPixels);
                Marshal.Copy(nativeCbPtr, nativeOutCb, 0, totalOutPixels);
                Marshal.Copy(nativeCrPtr, nativeOutCr, 0, totalOutPixels);
            }
            finally
            {
                if (nativeRgbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeRgbPtr);
                if (nativeYPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeYPtr);
                if (nativeCbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCbPtr);
                if (nativeCrPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCrPtr);
            }

            // 3. Assert Byte-for-Byte Parity (ignoring padding)
            fixed (sbyte* pUnY = hybridOutY, pUnCb = hybridOutCb, pUnCr = hybridOutCr)
            fixed (byte* pNtY = nativeOutY, pNtCb = nativeOutCb, pNtCr = nativeOutCr)
            {
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnY, pNtY, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnCb, pNtCb, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnCr, pNtCr, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit));
            }
        }

        [Fact]
        public unsafe void Rgb2YCbCrParallelHybridVector_NativeParityWithStride()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            int width = DefaultWidth;
            int height = DefaultHeight;
            int paddingPixels = 17;
            int rowSize = width + paddingPixels; // Input stride
            int outRowSize = width + 5;          // Output stride

            int totalInPixels = height * rowSize;
            int totalInBytes = totalInPixels * 3;
            int totalOutPixels = height * outRowSize;

            byte[] nativeRgbBuffer = new byte[totalInBytes];
            byte[] managedRgbBuffer = new byte[totalInBytes];

            Random rnd = new Random(Seed);
            for (int i = 0; i < totalInBytes; i++)
            {
                byte val = (byte)rnd.Next(256);
                nativeRgbBuffer[i] = val;
                managedRgbBuffer[i] = val;
            }

            sbyte[] hybridOutY = new sbyte[totalOutPixels];
            sbyte[] hybridOutCb = new sbyte[totalOutPixels];
            sbyte[] hybridOutCr = new sbyte[totalOutPixels];

            byte[] nativeOutY = new byte[totalOutPixels];
            byte[] nativeOutCb = new byte[totalOutPixels];
            byte[] nativeOutCr = new byte[totalOutPixels];

            // 1. Process via Parallel Hybrid AVX2 C#
            fixed (byte* ptr = managedRgbBuffer)
            fixed (sbyte* outY = hybridOutY, outCb = hybridOutCb, outCr = hybridOutCr)
            {
                int inRowBytes = rowSize * sizeof(Pixel);
                var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };
                InterWaveSimd.Rgb2YCbCrParallelHybridVector((Pixel*)ptr, width, height, inRowBytes, outY, outCb, outCr, outRowSize, options);
            }

            // 2. Process via Native C++ DjVuLibre
            IntPtr nativeRgbPtr = IntPtr.Zero;
            IntPtr nativeYPtr = IntPtr.Zero;
            IntPtr nativeCbPtr = IntPtr.Zero;
            IntPtr nativeCrPtr = IntPtr.Zero;
            try
            {
                nativeRgbPtr = DjvuMarshal.AllocHGlobal((uint)totalInBytes);
                nativeYPtr = DjvuMarshal.AllocHGlobal((uint)totalOutPixels);
                nativeCbPtr = DjvuMarshal.AllocHGlobal((uint)totalOutPixels);
                nativeCrPtr = DjvuMarshal.AllocHGlobal((uint)totalOutPixels);

                new Span<byte>(nativeYPtr.ToPointer(), totalOutPixels).Clear();
                new Span<byte>(nativeCbPtr.ToPointer(), totalOutPixels).Clear();
                new Span<byte>(nativeCrPtr.ToPointer(), totalOutPixels).Clear();

                Marshal.Copy(nativeRgbBuffer, 0, nativeRgbPtr, totalInBytes);

                Assert.True(NativeMethods.RgbToY(nativeRgbPtr, width, height, rowSize, nativeYPtr, outRowSize));
                Assert.True(NativeMethods.RgbToCb(nativeRgbPtr, width, height, rowSize, nativeCbPtr, outRowSize));
                Assert.True(NativeMethods.RgbToCr(nativeRgbPtr, width, height, rowSize, nativeCrPtr, outRowSize));

                Marshal.Copy(nativeYPtr, nativeOutY, 0, totalOutPixels);
                Marshal.Copy(nativeCbPtr, nativeOutCb, 0, totalOutPixels);
                Marshal.Copy(nativeCrPtr, nativeOutCr, 0, totalOutPixels);
            }
            finally
            {
                if (nativeRgbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeRgbPtr);
                if (nativeYPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeYPtr);
                if (nativeCbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCbPtr);
                if (nativeCrPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCrPtr);
            }

            // 3. Assert Byte-for-Byte Parity (ignoring padding)
            fixed (sbyte* pUnY = hybridOutY, pUnCb = hybridOutCb, pUnCr = hybridOutCr)
            fixed (byte* pNtY = nativeOutY, pNtCb = nativeOutCb, pNtCr = nativeOutCr)
            {
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnY, pNtY, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnCb, pNtCb, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnCr, pNtCr, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit));
            }
        }
        [Fact]
        public unsafe void Rgb2YCbCrHybridVector_BruteForceParity()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            int width = 4096;
            int height = 4096;
            int rowSize = width;
            int totalPixels = width * height;
            int inRowSize = width * 3;
            int totalInBytes = totalPixels * 3;

            byte[] rgbBuffer = new byte[totalInBytes];
            int idx = 0;
            for (int r = 0; r < 256; r++)
            {
                for (int g = 0; g < 256; g++)
                {
                    for (int b = 0; b < 256; b++)
                    {
                        rgbBuffer[idx++] = (byte)b;
                        rgbBuffer[idx++] = (byte)g;
                        rgbBuffer[idx++] = (byte)r;
                    }
                }
            }

            sbyte[] hybridY = new sbyte[totalPixels];
            sbyte[] hybridCb = new sbyte[totalPixels];
            sbyte[] hybridCr = new sbyte[totalPixels];

            byte[] nativeY = new byte[totalPixels];
            byte[] nativeCb = new byte[totalPixels];
            byte[] nativeCr = new byte[totalPixels];

            IntPtr nativeRgbPtr = IntPtr.Zero;
            IntPtr nativeYPtr = IntPtr.Zero;
            IntPtr nativeCbPtr = IntPtr.Zero;
            IntPtr nativeCrPtr = IntPtr.Zero;

            try
            {
                nativeRgbPtr = DjvuMarshal.AllocHGlobal((uint)totalInBytes);
                nativeYPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);
                nativeCbPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);
                nativeCrPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);

                Marshal.Copy(rgbBuffer, 0, nativeRgbPtr, totalInBytes);

                Assert.True(NativeMethods.RgbToY(nativeRgbPtr, width, height, width, nativeYPtr, width));
                Assert.True(NativeMethods.RgbToCb(nativeRgbPtr, width, height, width, nativeCbPtr, width));
                Assert.True(NativeMethods.RgbToCr(nativeRgbPtr, width, height, width, nativeCrPtr, width));

                Marshal.Copy(nativeYPtr, nativeY, 0, totalPixels);
                Marshal.Copy(nativeCbPtr, nativeCb, 0, totalPixels);
                Marshal.Copy(nativeCrPtr, nativeCr, 0, totalPixels);
            }
            finally
            {
                if (nativeRgbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeRgbPtr);
                if (nativeYPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeYPtr);
                if (nativeCbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCbPtr);
                if (nativeCrPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCrPtr);
            }

            fixed (byte* pIn = rgbBuffer)
            fixed (sbyte* pY = hybridY)
            fixed (sbyte* pCb = hybridCb)
            fixed (sbyte* pCr = hybridCr)
            {
                InterWaveSimd.Rgb2YCbCrHybridVector((Pixel*)pIn, width, height, inRowSize, pY, pCb, pCr, width);
            }

            int diffCount = 0;
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < totalPixels; i++)
            {
                sbyte nY = unchecked((sbyte)nativeY[i]);
                sbyte nCb = unchecked((sbyte)nativeCb[i]);
                sbyte nCr = unchecked((sbyte)nativeCr[i]);

                if (hybridY[i] != nY || hybridCb[i] != nCb || hybridCr[i] != nCr)
                {
                    int b = rgbBuffer[i * 3];
                    int g = rgbBuffer[i * 3 + 1];
                    int r = rgbBuffer[i * 3 + 2];

                    if (diffCount < 256)
                    {
                        sb.AppendLine($"Diff at {i} (B:{b}, G:{g}, R:{r}) - Native(Y:{nY}, Cb:{nCb}, Cr:{nCr}) vs Hybrid(Y:{hybridY[i]}, Cb:{hybridCb[i]}, Cr:{hybridCr[i]})");
                    }
                    diffCount++;
                }
            }

            if (diffCount > 0)
            {
                Console.WriteLine($"Total Diffs: {diffCount}");
                Assert.Fail($"Found diffs in Rgb2YCbCr conversion.\n{sb.ToString()}");
            }
        }

        [Fact]
        public unsafe void Rgb2YCbCrParallelHybridVector_BruteForceParity()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            int width = 4096;
            int height = 4096;
            int rowSize = width;
            int totalPixels = width * height;
            int inRowSize = width * 3;
            int totalInBytes = totalPixels * 3;

            byte[] rgbBuffer = GC.AllocateUninitializedArray<byte>(totalInBytes);
            int idx = 0;
            for (int r = 0; r < 256; r++)
            {
                for (int g = 0; g < 256; g++)
                {
                    for (int b = 0; b < 256; b++)
                    {
                        rgbBuffer[idx++] = (byte)b;
                        rgbBuffer[idx++] = (byte)g;
                        rgbBuffer[idx++] = (byte)r;
                    }
                }
            }

            sbyte[] hybridY = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] hybridCb = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] hybridCr = GC.AllocateUninitializedArray<sbyte>(totalPixels);

            byte[] nativeY = GC.AllocateUninitializedArray<byte>(totalPixels);
            byte[] nativeCb = GC.AllocateUninitializedArray<byte>(totalPixels);
            byte[] nativeCr = GC.AllocateUninitializedArray<byte>(totalPixels);

            IntPtr nativeRgbPtr = IntPtr.Zero;
            IntPtr nativeYPtr = IntPtr.Zero;
            IntPtr nativeCbPtr = IntPtr.Zero;
            IntPtr nativeCrPtr = IntPtr.Zero;

            try
            {
                nativeRgbPtr = DjvuMarshal.AllocHGlobal((uint)totalInBytes);
                nativeYPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);
                nativeCbPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);
                nativeCrPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);

                Marshal.Copy(rgbBuffer, 0, nativeRgbPtr, totalInBytes);

                Assert.True(NativeMethods.RgbToY(nativeRgbPtr, width, height, width, nativeYPtr, width));
                Assert.True(NativeMethods.RgbToCb(nativeRgbPtr, width, height, width, nativeCbPtr, width));
                Assert.True(NativeMethods.RgbToCr(nativeRgbPtr, width, height, width, nativeCrPtr, width));

                Marshal.Copy(nativeYPtr, nativeY, 0, totalPixels);
                Marshal.Copy(nativeCbPtr, nativeCb, 0, totalPixels);
                Marshal.Copy(nativeCrPtr, nativeCr, 0, totalPixels);
            }
            finally
            {
                if (nativeRgbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeRgbPtr);
                if (nativeYPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeYPtr);
                if (nativeCbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCbPtr);
                if (nativeCrPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCrPtr);
            }

            fixed (byte* pIn = rgbBuffer)
            fixed (sbyte* pY = hybridY)
            fixed (sbyte* pCb = hybridCb)
            fixed (sbyte* pCr = hybridCr)
            {
                var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };
                InterWaveSimd.Rgb2YCbCrParallelHybridVector((Pixel*)pIn, width, height, inRowSize, pY, pCb, pCr, width, options);
            }

            int diffCount = 0;
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < totalPixels; i++)
            {
                sbyte nY = unchecked((sbyte)nativeY[i]);
                sbyte nCb = unchecked((sbyte)nativeCb[i]);
                sbyte nCr = unchecked((sbyte)nativeCr[i]);

                if (hybridY[i] != nY || hybridCb[i] != nCb || hybridCr[i] != nCr)
                {
                    int b = rgbBuffer[i * 3];
                    int g = rgbBuffer[i * 3 + 1];
                    int r = rgbBuffer[i * 3 + 2];

                    if (diffCount < 256)
                    {
                        sb.AppendLine($"Diff at {i} (B:{b}, G:{g}, R:{r}) - Native(Y:{nY}, Cb:{nCb}, Cr:{nCr}) vs Hybrid(Y:{hybridY[i]}, Cb:{hybridCb[i]}, Cr:{hybridCr[i]})");
                    }
                    diffCount++;
                }
            }

            if (diffCount > 0)
            {
                Console.WriteLine($"Total Diffs: {diffCount}");
                Assert.Fail($"Found diffs in Rgb2YCbCr conversion.\n{sb.ToString()}");
            }
        }
        [Fact]
        public unsafe void Rgb2YCbCrVector128_BruteForceParity()
        {
            if (!Vector128.IsHardwareAccelerated)
            {
                Assert.Skip("Vector128 hardware acceleration is not supported on this architecture.");
            }

            int width = 4096;
            int height = 4096;
            int rowSize = width;
            int totalPixels = width * height;
            int inRowSize = width * 3;
            int totalInBytes = totalPixels * 3;

            byte[] rgbBuffer = GC.AllocateUninitializedArray<byte>(totalInBytes);
            int idx = 0;
            for (int r = 0; r < 256; r++)
            {
                for (int g = 0; g < 256; g++)
                {
                    for (int b = 0; b < 256; b++)
                    {
                        rgbBuffer[idx++] = (byte)b;
                        rgbBuffer[idx++] = (byte)g;
                        rgbBuffer[idx++] = (byte)r;
                    }
                }
            }

            sbyte[] vecY = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] vecCb = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] vecCr = GC.AllocateUninitializedArray<sbyte>(totalPixels);

            byte[] nativeY = GC.AllocateUninitializedArray<byte>(totalPixels);
            byte[] nativeCb = GC.AllocateUninitializedArray<byte>(totalPixels);
            byte[] nativeCr = GC.AllocateUninitializedArray<byte>(totalPixels);

            IntPtr nativeRgbPtr = IntPtr.Zero;
            IntPtr nativeYPtr = IntPtr.Zero;
            IntPtr nativeCbPtr = IntPtr.Zero;
            IntPtr nativeCrPtr = IntPtr.Zero;

            try
            {
                nativeRgbPtr = DjvuMarshal.AllocHGlobal((uint)totalInBytes);
                nativeYPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);
                nativeCbPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);
                nativeCrPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);

                Marshal.Copy(rgbBuffer, 0, nativeRgbPtr, totalInBytes);

                Assert.True(NativeMethods.RgbToY(nativeRgbPtr, width, height, width, nativeYPtr, width));
                Assert.True(NativeMethods.RgbToCb(nativeRgbPtr, width, height, width, nativeCbPtr, width));
                Assert.True(NativeMethods.RgbToCr(nativeRgbPtr, width, height, width, nativeCrPtr, width));

                Marshal.Copy(nativeYPtr, nativeY, 0, totalPixels);
                Marshal.Copy(nativeCbPtr, nativeCb, 0, totalPixels);
                Marshal.Copy(nativeCrPtr, nativeCr, 0, totalPixels);
            }
            finally
            {
                if (nativeRgbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeRgbPtr);
                if (nativeYPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeYPtr);
                if (nativeCbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCbPtr);
                if (nativeCrPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCrPtr);
            }

            fixed (byte* pIn = rgbBuffer)
            fixed (sbyte* pY = vecY)
            fixed (sbyte* pCb = vecCb)
            fixed (sbyte* pCr = vecCr)
            {
                InterWaveSimd.Rgb2YCbCrVector128((Pixel*)pIn, width, height, inRowSize, pY, pCb, pCr, width);
            }

            int diffCount = 0;
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < totalPixels; i++)
            {
                sbyte nY = unchecked((sbyte)nativeY[i]);
                sbyte nCb = unchecked((sbyte)nativeCb[i]);
                sbyte nCr = unchecked((sbyte)nativeCr[i]);

                if (vecY[i] != nY || vecCb[i] != nCb || vecCr[i] != nCr)
                {
                    int b = rgbBuffer[i * 3];
                    int g = rgbBuffer[i * 3 + 1];
                    int r = rgbBuffer[i * 3 + 2];

                    if (diffCount < 256)
                    {
                        sb.AppendLine($"Diff at {i} (B:{b}, G:{g}, R:{r}) - Native(Y:{nY}, Cb:{nCb}, Cr:{nCr}) vs Vector128(Y:{vecY[i]}, Cb:{vecCb[i]}, Cr:{vecCr[i]})");
                    }
                    diffCount++;
                }
            }

            if (diffCount > 0)
            {
                Console.WriteLine($"Total Diffs: {diffCount}");
                Assert.Fail($"Found diffs in Rgb2YCbCr conversion.\n{sb.ToString()}");
            }
        }

        [Fact]
        public unsafe void Rgb2YCbCrVector256_BruteForceParity()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            int width = 4096;
            int height = 4096;
            int rowSize = width;
            int totalPixels = width * height;
            int inRowSize = width * 3;
            int totalInBytes = totalPixels * 3;

            byte[] rgbBuffer = GC.AllocateUninitializedArray<byte>(totalInBytes);
            int idx = 0;
            for (int r = 0; r < 256; r++)
            {
                for (int g = 0; g < 256; g++)
                {
                    for (int b = 0; b < 256; b++)
                    {
                        rgbBuffer[idx++] = (byte)b;
                        rgbBuffer[idx++] = (byte)g;
                        rgbBuffer[idx++] = (byte)r;
                    }
                }
            }

            sbyte[] vecY = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] vecCb = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] vecCr = GC.AllocateUninitializedArray<sbyte>(totalPixels);

            byte[] nativeY = GC.AllocateUninitializedArray<byte>(totalPixels);
            byte[] nativeCb = GC.AllocateUninitializedArray<byte>(totalPixels);
            byte[] nativeCr = GC.AllocateUninitializedArray<byte>(totalPixels);

            IntPtr nativeRgbPtr = IntPtr.Zero;
            IntPtr nativeYPtr = IntPtr.Zero;
            IntPtr nativeCbPtr = IntPtr.Zero;
            IntPtr nativeCrPtr = IntPtr.Zero;

            try
            {
                nativeRgbPtr = DjvuMarshal.AllocHGlobal((uint)totalInBytes);
                nativeYPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);
                nativeCbPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);
                nativeCrPtr = DjvuMarshal.AllocHGlobal((uint)totalPixels);

                Marshal.Copy(rgbBuffer, 0, nativeRgbPtr, totalInBytes);

                Assert.True(NativeMethods.RgbToY(nativeRgbPtr, width, height, width, nativeYPtr, width));
                Assert.True(NativeMethods.RgbToCb(nativeRgbPtr, width, height, width, nativeCbPtr, width));
                Assert.True(NativeMethods.RgbToCr(nativeRgbPtr, width, height, width, nativeCrPtr, width));

                Marshal.Copy(nativeYPtr, nativeY, 0, totalPixels);
                Marshal.Copy(nativeCbPtr, nativeCb, 0, totalPixels);
                Marshal.Copy(nativeCrPtr, nativeCr, 0, totalPixels);
            }
            finally
            {
                if (nativeRgbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeRgbPtr);
                if (nativeYPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeYPtr);
                if (nativeCbPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCbPtr);
                if (nativeCrPtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeCrPtr);
            }

            fixed (byte* pIn = rgbBuffer)
            fixed (sbyte* pY = vecY)
            fixed (sbyte* pCb = vecCb)
            fixed (sbyte* pCr = vecCr)
            {
                InterWaveSimd.Rgb2YCbCrVector256((Pixel*)pIn, width, height, inRowSize, pY, pCb, pCr, width);
            }

            int diffCount = 0;
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < totalPixels; i++)
            {
                sbyte nY = unchecked((sbyte)nativeY[i]);
                sbyte nCb = unchecked((sbyte)nativeCb[i]);
                sbyte nCr = unchecked((sbyte)nativeCr[i]);

                if (vecY[i] != nY || vecCb[i] != nCb || vecCr[i] != nCr)
                {
                    int b = rgbBuffer[i * 3];
                    int g = rgbBuffer[i * 3 + 1];
                    int r = rgbBuffer[i * 3 + 2];

                    if (diffCount < 256)
                    {
                        sb.AppendLine($"Diff at {i} (B:{b}, G:{g}, R:{r}) - Native(Y:{nY}, Cb:{nCb}, Cr:{nCr}) vs Vector(Y:{vecY[i]}, Cb:{vecCb[i]}, Cr:{vecCr[i]})");
                    }
                    diffCount++;
                }
            }

            if (diffCount > 0)
            {
                Console.WriteLine($"Total Diffs: {diffCount}");
                Assert.Fail($"Found diffs in Rgb2YCbCr conversion.\n{sb.ToString()}");
            }
        }
    }
}