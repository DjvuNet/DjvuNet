using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using DjvuNet.DjvuLibre;
using DjvuNet.Wavelet;
using DjvuNet.Graphics;
using Xunit;
using DjvuNet.Tests;
using System.Threading.Tasks;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;

namespace DjvuNet.DjvuLibre.Compatibility.Tests
{
    public class PigeonTransformTests
    {
        public const int Seed = 42;

        // Realistic buffer sizes: a typical 300 DPI page can be ~3000x5000.
        // The background IW44 layer is typically subsampled, yielding ~850x1100.
        // Tripling the pixel count of an 850x1100 buffer yields roughly 1472x1905.
        // The color transform runs on the entire image grid at once.
        public const int DefaultWidth = 1472;
        public const int DefaultHeight = 1905;

        [Fact]
        public unsafe void YCbCr2RgbNativeParityRandomData()
        {
            int width = DefaultWidth;
            int height = DefaultHeight;
            int rowSize = width; // rowSize in GPixels
            int totalPixels = height * rowSize;
            int totalBytes = totalPixels * 3; // 3 bytes per pixel

            byte[] nativeBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            sbyte[] scalarBuffer = GC.AllocateUninitializedArray<sbyte>(totalBytes);
            sbyte[] unifiedBuffer = GC.AllocateUninitializedArray<sbyte>(totalBytes);

            // Generate some random test data to represent Y, Cb, Cr
            Random rnd = new Random(Seed);
            for (int i = 0; i < totalBytes; i++)
            {
                byte val = (byte)rnd.Next(256);
                nativeBuffer[i] = val;
                scalarBuffer[i] = unchecked((sbyte)val);
                unifiedBuffer[i] = unchecked((sbyte)val);
            }

            // 1. Process via Scalar C#
            fixed (sbyte* ptr = scalarBuffer)
            {
                // Multiply width by 3 to simulate legacy parameter usage
                InterWaveTransform.YCbCr2Rgb((Pixel*)ptr, width, height);
            }

            // 2. Process via Unified AVX2 C#
            fixed (sbyte* ptr = unifiedBuffer)
            {
                int rowSizeInBytes = width * sizeof(Pixel);
                InterWaveTransform.YCbCr2Rgb((Pixel*)ptr, width, height, rowSizeInBytes);
            }

            // 3. Process via Native C++ DjVuLibre
            IntPtr nativePtr = IntPtr.Zero;
            try
            {
                nativePtr = DjvuMarshal.AllocHGlobal((uint)totalBytes);
                Marshal.Copy(nativeBuffer, 0, nativePtr, totalBytes);
                bool success = NativeMethods.YCbCrToRgb(nativePtr, width, height, rowSize);
                Assert.True(success);
                Marshal.Copy(nativePtr, nativeBuffer, 0, totalBytes);
            }
            finally
            {
                DjvuMarshal.FreeHGlobal(nativePtr);
            }

            // 4. Compute Diffs and Assert
            double diffSc, diffUn, diffScUn;

            fixed (sbyte* pSc = scalarBuffer)
            fixed (sbyte* pUn = unifiedBuffer)
            fixed (byte* pNt = nativeBuffer)
            {
                int strideBytes = rowSize * sizeof(Pixel);
                diffSc = Util.ImageBinaryDiff((byte*)pSc, pNt, width, height, strideBytes);
                diffUn = Util.ImageBinaryDiff((byte*)pUn, pNt, width, height, strideBytes);
                diffScUn = Util.ImageBinaryDiff((byte*)pSc, (byte*)pUn, width, height, strideBytes);
            }

            bool hasError = diffSc > 0 || diffUn > 0 || diffScUn > 0;

            Assert.True(!hasError,
                $"Parity Mismatch (Contiguous):\n" +
                $"C# Scalar vs Native C++    -> Diff: {diffSc:F4}\n" +
                $"C# Unified vs Native C++   -> Diff: {diffUn:F4}\n" +
                $"C# Scalar vs C# Unified    -> Diff: {diffScUn:F4}");
        }

        [Fact]
        public unsafe void YCbCr2RgbNativeParityWithStride()
        {
            int width = DefaultWidth;
            int height = DefaultHeight;

            int paddingPixels = 17;
            int rowSize = width + paddingPixels; // Input stride (in pixels)

            int totalInPixels = height * rowSize;
            int totalInBytes = totalInPixels * 3; // 3 bytes per pixel

            byte[] nativeBuffer = GC.AllocateUninitializedArray<byte>(totalInBytes);
            sbyte[] scalarBuffer = GC.AllocateUninitializedArray<sbyte>(totalInBytes);
            sbyte[] unifiedBuffer = GC.AllocateUninitializedArray<sbyte>(totalInBytes);

            Random rnd = new Random(Seed);
            for (int i = 0; i < totalInBytes; i++)
            {
                byte val = (byte)rnd.Next(256);
                nativeBuffer[i] = val;
                scalarBuffer[i] = unchecked((sbyte)val);
                unifiedBuffer[i] = unchecked((sbyte)val);
            }

            // 1. Process via Scalar C#
            fixed (sbyte* ptr = scalarBuffer)
            {
                int rowSizeInBytes = rowSize * sizeof(Pixel);
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)ptr, width, height, rowSizeInBytes);
            }

            // 2. Process via Unified AVX2 C#
            fixed (sbyte* ptr = unifiedBuffer)
            {
                int rowSizeInBytes = rowSize * sizeof(Pixel);
                InterWaveTransform.YCbCr2Rgb((Pixel*)ptr, width, height, rowSizeInBytes);
            }

            // 3. Process via Native C++ DjVuLibre
            IntPtr nativePtr = IntPtr.Zero;
            try
            {
                nativePtr = DjvuMarshal.AllocHGlobal((uint)totalInBytes);
                Marshal.Copy(nativeBuffer, 0, nativePtr, totalInBytes);

                bool success = NativeMethods.YCbCrToRgb(nativePtr, width, height, rowSize);
                Assert.True(success);

                Marshal.Copy(nativePtr, nativeBuffer, 0, totalInBytes);
            }
            finally
            {
                if (nativePtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativePtr);
            }

            // 4. Assert Byte-for-Byte Parity (ignoring padding)
            double diffScNt, diffUnNt, diffScUn;

            fixed (sbyte* pSc = scalarBuffer)
            fixed (sbyte* pUn = unifiedBuffer)
            fixed (byte* pNt = nativeBuffer)
            {
                int strideBytes = rowSize * 3; // 3 bytes per pixel
                diffScNt = Util.ImageBinaryDiff((byte*)pSc, pNt, width, height, strideBytes);
                diffUnNt = Util.ImageBinaryDiff((byte*)pUn, pNt, width, height, strideBytes);
                diffScUn = Util.ImageBinaryDiff((byte*)pSc, (byte*)pUn, width, height, strideBytes);
            }

            bool scalarVsNativePassed = diffScNt == 0.0;
            bool scalarVsUnifiedPassed = diffScUn == 0.0;
            bool unifiedVsNativePassed = diffUnNt == 0.0;

            Assert.True(scalarVsNativePassed && scalarVsUnifiedPassed && unifiedVsNativePassed,
                $"Parity Mismatch (With Stride):\n" +
                $"C# Scalar vs Native C++    -> Diff: {diffScNt:F4}\n" +
                $"C# Unified vs Native C++   -> Diff: {diffUnNt:F4}\n" +
                $"C# Scalar vs C# Unified    -> Diff: {diffScUn:F4}\n");
        }

        [Fact]
        public unsafe void Rgb2YCbCrNativeParityRandomData()
        {
            int width = 1248;
            int height = 1024;
            int rowSize = width; // rowSize in GPixels
            int outRowSize = width;
            int totalPixels = height * rowSize;
            int totalBytes = totalPixels * 3; // 3 bytes per pixel

            byte[] nativeRgbBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            sbyte[] managedRgbBuffer = GC.AllocateUninitializedArray<sbyte>(totalBytes);

            // Generate some random test data to represent RGB
            Random rnd = new Random(Seed);
            for (int i = 0; i < totalBytes; i++)
            {
                byte val = (byte)rnd.Next(256);
                nativeRgbBuffer[i] = val;
                managedRgbBuffer[i] = unchecked((sbyte)val);
            }

            sbyte[] managedOutY = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] managedOutCb = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] managedOutCr = GC.AllocateUninitializedArray<sbyte>(totalPixels);

            byte[] nativeOutY = GC.AllocateUninitializedArray<byte>(totalPixels);
            byte[] nativeOutCb = GC.AllocateUninitializedArray<byte>(totalPixels);
            byte[] nativeOutCr = GC.AllocateUninitializedArray<byte>(totalPixels);

            // 1. Process via Managed C#
            fixed (sbyte* ptr = managedRgbBuffer)
            fixed (sbyte* outY = managedOutY)
            fixed (sbyte* outCb = managedOutCb)
            fixed (sbyte* outCr = managedOutCr)
            {
                // Rgb2YCbCr sets up LUTs and extracts all 3 planes
                int inRowBytes = rowSize * sizeof(Pixel);
                InterWaveTransform.Rgb2YCbCr((Pixel*)ptr, width, height, inRowBytes, outY, outCb, outCr, outRowSize);
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
            fixed (sbyte* pY = managedOutY)
            fixed (sbyte* pCb = managedOutCb)
            fixed (sbyte* pCr = managedOutCr)
            fixed (byte* pNtY = nativeOutY)
            fixed (byte* pNtCb = nativeOutCb)
            fixed (byte* pNtCr = nativeOutCr)
            {
                double diffY = Util.ImageBinaryDiff((byte*)pY, pNtY, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit);
                double diffCb = Util.ImageBinaryDiff((byte*)pCb, pNtCb, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit);
                double diffCr = Util.ImageBinaryDiff((byte*)pCr, pNtCr, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit);

                Assert.Equal(0.0, diffY);
                Assert.Equal(0.0, diffCb);
                Assert.Equal(0.0, diffCr);
            }
        }

        [Fact]
        public unsafe void Rgb2YCbCrNativeParityWithStride()
        {
            int width = DefaultWidth;
            int height = DefaultHeight;

            // Inject padding to force rowSize > width.
            // This tests that the implementation respects pointer arithmetic
            // rather than flattening the 2D array into 1D.
            int paddingPixels = 17;
            int rowSize = width + paddingPixels; // Input stride (in pixels)
            int outRowSize = width + 5;          // Output stride (in bytes)

            int totalInPixels = height * rowSize;
            int totalInBytes = totalInPixels * 3; // 3 bytes per pixel for RGB

            int totalOutPixels = height * outRowSize;

            byte[] nativeRgbBuffer = GC.AllocateUninitializedArray<byte>(totalInBytes);
            sbyte[] managedRgbBuffer = GC.AllocateUninitializedArray<sbyte>(totalInBytes);

            // Generate synthetic random test data for the entire buffer (including padding)
            Random rnd = new Random(Seed);
            for (int i = 0; i < totalInBytes; i++)
            {
                byte val = (byte)rnd.Next(256);
                nativeRgbBuffer[i] = val;
                managedRgbBuffer[i] = unchecked((sbyte)val);
            }

            sbyte[] managedOutY = GC.AllocateUninitializedArray<sbyte>(totalOutPixels);
            sbyte[] managedOutCb = GC.AllocateUninitializedArray<sbyte>(totalOutPixels);
            sbyte[] managedOutCr = GC.AllocateUninitializedArray<sbyte>(totalOutPixels);

            byte[] nativeOutY = GC.AllocateUninitializedArray<byte>(totalOutPixels);
            byte[] nativeOutCb = GC.AllocateUninitializedArray<byte>(totalOutPixels);
            byte[] nativeOutCr = GC.AllocateUninitializedArray<byte>(totalOutPixels);

            // 1. Process via Managed C#
            fixed (sbyte* ptr = managedRgbBuffer)
            fixed (sbyte* outY = managedOutY)
            fixed (sbyte* outCb = managedOutCb)
            fixed (sbyte* outCr = managedOutCr)
            {
                int inRowBytes = rowSize * sizeof(Pixel);
                InterWaveTransform.Rgb2YCbCr((Pixel*)ptr, width, height, inRowBytes, outY, outCb, outCr, outRowSize);
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

                Marshal.Copy(nativeRgbBuffer, 0, nativeRgbPtr, totalInBytes);

                // Call C++ reference implementation
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

            // 3. Assert Byte-for-Byte Parity
            // We ignore the padding bytes at the end of each row by providing outRowSize as stride.
            fixed (sbyte* pY = managedOutY)
            fixed (sbyte* pCb = managedOutCb)
            fixed (sbyte* pCr = managedOutCr)
            fixed (byte* pNtY = nativeOutY)
            fixed (byte* pNtCb = nativeOutCb)
            fixed (byte* pNtCr = nativeOutCr)
            {
                double diffY = Util.ImageBinaryDiff((byte*)pY, pNtY, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit);
                double diffCb = Util.ImageBinaryDiff((byte*)pCb, pNtCb, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit);
                double diffCr = Util.ImageBinaryDiff((byte*)pCr, pNtCr, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit);

                if (diffY != 0.0 || diffCb != 0.0 || diffCr != 0.0)
                {
                    Console.WriteLine(
                        $"Parity Mismatch (With Stride):\n" +
                        $"C# Managed vs Native C++    -> Diff Y: {diffY:F17}, Diff Cb: {diffCb:F17}, Diff Cr: {diffCr:F17}\n" +
                        $"Note: The legacy scalar method is expected to fail parity on padded buffers, but the unified method should match exactly.");
                }

                Assert.Equal(0.0, diffY);
                Assert.Equal(0.0, diffCb);
                Assert.Equal(0.0, diffCr);
            }
        }

        [Fact]
        public unsafe void Rgb2YCbCrUnifiedNativeParityRandomData()
        {
            int width = DefaultWidth;
            int height = DefaultHeight;
            int rowSize = width; // rowSize in GPixels
            int outRowSize = width;
            int totalPixels = height * rowSize;
            int totalBytes = totalPixels * 3; // 3 bytes per pixel

            byte[] nativeRgbBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            sbyte[] managedRgbBuffer = GC.AllocateUninitializedArray<sbyte>(totalBytes);

            Random rnd = new Random(Seed);
            for (int i = 0; i < totalBytes; i++)
            {
                byte val = (byte)rnd.Next(256);
                nativeRgbBuffer[i] = val;
                managedRgbBuffer[i] = unchecked((sbyte)val);
            }

            sbyte[] scalarOutY = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] scalarOutCb = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] scalarOutCr = GC.AllocateUninitializedArray<sbyte>(totalPixels);

            sbyte[] unifiedOutY = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] unifiedOutCb = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] unifiedOutCr = GC.AllocateUninitializedArray<sbyte>(totalPixels);

            byte[] nativeOutY = GC.AllocateUninitializedArray<byte>(totalPixels);
            byte[] nativeOutCb = GC.AllocateUninitializedArray<byte>(totalPixels);
            byte[] nativeOutCr = GC.AllocateUninitializedArray<byte>(totalPixels);

            // 1. Process via Scalar C#
            fixed (sbyte* ptr = managedRgbBuffer)
            fixed (sbyte* outY = scalarOutY, outCb = scalarOutCb, outCr = scalarOutCr)
            {
                InterWaveTransform.Rgb2YCbCrScalar((Pixel*)ptr, width, height, rowSize * sizeof(Pixel), outY, outCb, outCr, outRowSize);
            }

            // 2. Process via Unified AVX2 C#
            fixed (sbyte* ptr = managedRgbBuffer)
            fixed (sbyte* outY = unifiedOutY, outCb = unifiedOutCb, outCr = unifiedOutCr)
            {
                int inRowBytes = rowSize * sizeof(Pixel);
                InterWaveTransform.Rgb2YCbCr((Pixel*)ptr, width, height, inRowBytes, outY, outCb, outCr, outRowSize);
            }

            // 3. Process via Native C++ DjVuLibre
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

                // Zero out native buffers to prevent garbage padding mismatches
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

            // 4. Assert Byte-for-Byte Parity
            fixed (sbyte* pScY = scalarOutY)
            fixed (sbyte* pScCb = scalarOutCb)
            fixed (sbyte* pScCr = scalarOutCr)
            fixed (sbyte* pUnY = unifiedOutY)
            fixed (sbyte* pUnCb = unifiedOutCb)
            fixed (sbyte* pUnCr = unifiedOutCr)
            fixed (byte* pNtY = nativeOutY)
            fixed (byte* pNtCb = nativeOutCb)
            fixed (byte* pNtCr = nativeOutCr)
            {
                // Native vs Unified
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnY, pNtY, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnCb, pNtCb, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnCr, pNtCr, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit));

                // Scalar vs Unified
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pScY, (byte*)pUnY, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pScCb, (byte*)pUnCb, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pScCr, (byte*)pUnCr, width, height, width, 0, PixelSize._8bpp, ChannelSize._8bit));
            }
        }

        [Fact]
        public unsafe void Rgb2YCbCrUnifiedNativeParityRandomDataDiagnostic()
        {
            int width = DefaultWidth;
            int height = DefaultHeight;
            int rowSize = width; // rowSize in GPixels
            int outRowSize = width;
            int totalPixels = height * rowSize;
            int totalBytes = totalPixels * 3; // 3 bytes per pixel

            byte[] nativeRgbBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            sbyte[] managedRgbBuffer = GC.AllocateUninitializedArray<sbyte>(totalBytes);

            Random rnd = new Random(Seed);
            for (int i = 0; i < totalBytes; i++)
            {
                byte val = (byte)rnd.Next(256);
                nativeRgbBuffer[i] = val;
                managedRgbBuffer[i] = unchecked((sbyte)val);
            }

            sbyte[] unifiedOutY = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] unifiedOutCb = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] unifiedOutCr = GC.AllocateUninitializedArray<sbyte>(totalPixels);

            byte[] nativeOutY = GC.AllocateUninitializedArray<byte>(totalPixels);
            byte[] nativeOutCb = GC.AllocateUninitializedArray<byte>(totalPixels);
            byte[] nativeOutCr = GC.AllocateUninitializedArray<byte>(totalPixels);

            // 2. Process via Unified AVX2 C#
            fixed (sbyte* ptr = managedRgbBuffer)
            fixed (sbyte* outY = unifiedOutY, outCb = unifiedOutCb, outCr = unifiedOutCr)
            {
                int inRowBytes = rowSize * sizeof(Pixel);
                InterWaveTransform.Rgb2YCbCr((Pixel*)ptr, width, height, inRowBytes, outY, outCb, outCr, outRowSize);
            }

            // 3. Process via Native C++ DjVuLibre
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

                // Zero out native buffers
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

            // 4. Assert Byte-for-Byte Parity with Diagnostic Logging
            int diffCount = 0;
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < totalPixels; i++)
            {
                sbyte nY = unchecked((sbyte)nativeOutY[i]);
                sbyte nCb = unchecked((sbyte)nativeOutCb[i]);
                sbyte nCr = unchecked((sbyte)nativeOutCr[i]);

                if (unifiedOutY[i] != nY || unifiedOutCb[i] != nCb || unifiedOutCr[i] != nCr)
                {
                    int b = nativeRgbBuffer[i * 3];
                    int g = nativeRgbBuffer[i * 3 + 1];
                    int r = nativeRgbBuffer[i * 3 + 2];

                    if (diffCount < 256)
                    {
                        sb.AppendLine($"Diff at {i} (B:{b}, G:{g}, R:{r}) - Native(Y:{nY}, Cb:{nCb}, Cr:{nCr}) vs Unified(Y:{unifiedOutY[i]}, Cb:{unifiedOutCb[i]}, Cr:{unifiedOutCr[i]})");
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
        public unsafe void Rgb2YCbCrUnifiedNativeParityWithStride()
        {
            int width = DefaultWidth;
            int height = DefaultHeight;

            int paddingPixels = 17;
            int rowSize = width + paddingPixels; // Input stride (in pixels)
            int outRowSize = width + 5;          // Output stride (in bytes)

            int totalInPixels = height * rowSize;
            int totalInBytes = totalInPixels * 3; // 3 bytes per pixel for RGB

            int totalOutPixels = height * outRowSize;

            byte[] nativeRgbBuffer = GC.AllocateUninitializedArray<byte>(totalInBytes);
            sbyte[] managedRgbBuffer = GC.AllocateUninitializedArray<sbyte>(totalInBytes);

            Random rnd = new Random(Seed);
            for (int i = 0; i < totalInBytes; i++)
            {
                byte val = (byte)rnd.Next(256);
                nativeRgbBuffer[i] = val;
                managedRgbBuffer[i] = unchecked((sbyte)val);
            }

            sbyte[] scalarOutY = GC.AllocateUninitializedArray<sbyte>(totalOutPixels);
            sbyte[] scalarOutCb = GC.AllocateUninitializedArray<sbyte>(totalOutPixels);
            sbyte[] scalarOutCr = GC.AllocateUninitializedArray<sbyte>(totalOutPixels);

            sbyte[] unifiedOutY = GC.AllocateUninitializedArray<sbyte>(totalOutPixels);
            sbyte[] unifiedOutCb = GC.AllocateUninitializedArray<sbyte>(totalOutPixels);
            sbyte[] unifiedOutCr = GC.AllocateUninitializedArray<sbyte>(totalOutPixels);

            byte[] nativeOutY = GC.AllocateUninitializedArray<byte>(totalOutPixels);
            byte[] nativeOutCb = GC.AllocateUninitializedArray<byte>(totalOutPixels);
            byte[] nativeOutCr = GC.AllocateUninitializedArray<byte>(totalOutPixels);

            // 1. Process via Scalar C#
            fixed (sbyte* ptr = managedRgbBuffer)
            fixed (sbyte* outY = scalarOutY, outCb = scalarOutCb, outCr = scalarOutCr)
            {
                InterWaveTransform.Rgb2YCbCrScalar((Pixel*)ptr, width, height, rowSize * sizeof(Pixel), outY, outCb, outCr, outRowSize);
            }

            // 2. Process via Unified AVX2 C#
            fixed (sbyte* ptr = managedRgbBuffer)
            fixed (sbyte* outY = unifiedOutY, outCb = unifiedOutCb, outCr = unifiedOutCr)
            {
                int inRowBytes = rowSize * sizeof(Pixel);
                InterWaveTransform.Rgb2YCbCr((Pixel*)ptr, width, height, inRowBytes, outY, outCb, outCr, outRowSize);
            }

            // 3. Process via Native C++ DjVuLibre
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

                // Zero out native buffers to prevent garbage padding mismatches
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

            // 4. Assert Byte-for-Byte Parity (ignoring padding)
            fixed (sbyte* pScY = scalarOutY)
            fixed (sbyte* pScCb = scalarOutCb)
            fixed (sbyte* pScCr = scalarOutCr)
            fixed (sbyte* pUnY = unifiedOutY)
            fixed (sbyte* pUnCb = unifiedOutCb)
            fixed (sbyte* pUnCr = unifiedOutCr)
            fixed (byte* pNtY = nativeOutY)
            fixed (byte* pNtCb = nativeOutCb)
            fixed (byte* pNtCr = nativeOutCr)
            {
                // Native vs Unified (Must match exactly)
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnY, pNtY, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnCb, pNtCb, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff((byte*)pUnCr, pNtCr, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit));

                // Scalar vs Unified (Must fail because Scalar ignores stride)
                double diffScY = Util.ImageBinaryDiff((byte*)pScY, (byte*)pUnY, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit);
                double diffScCb = Util.ImageBinaryDiff((byte*)pScCb, (byte*)pUnCb, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit);
                double diffScCr = Util.ImageBinaryDiff((byte*)pScCr, (byte*)pUnCr, width, height, outRowSize, 0, PixelSize._8bpp, ChannelSize._8bit);

                bool scalarFailed = diffScY > 0.0 || diffScCb > 0.0 || diffScCr > 0.0;
                Assert.False(scalarFailed, "Expected the new scalar method to pass parity on padded buffers.");
            }
        }

        [Fact]
        public unsafe void Rgb2YCbCrBruteForceParity()
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

            sbyte[] unifiedY = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] unifiedCb = GC.AllocateUninitializedArray<sbyte>(totalPixels);
            sbyte[] unifiedCr = GC.AllocateUninitializedArray<sbyte>(totalPixels);

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
            fixed (sbyte* pY = unifiedY)
            fixed (sbyte* pCb = unifiedCb)
            fixed (sbyte* pCr = unifiedCr)
            {
                InterWaveTransform.Rgb2YCbCr((Pixel*)pIn, width, height, inRowSize, pY, pCb, pCr, width);
            }

            int diffCount = 0;
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < totalPixels; i++)
            {
                sbyte nY = unchecked((sbyte)nativeY[i]);
                sbyte nCb = unchecked((sbyte)nativeCb[i]);
                sbyte nCr = unchecked((sbyte)nativeCr[i]);

                if (unifiedY[i] != nY || unifiedCb[i] != nCb || unifiedCr[i] != nCr)
                {
                    int b = rgbBuffer[i * 3];
                    int g = rgbBuffer[i * 3 + 1];
                    int r = rgbBuffer[i * 3 + 2];

                    if (diffCount < 256)
                    {
                        sb.AppendLine($"Diff at {i} (B:{b}, G:{g}, R:{r}) - Native(Y:{nY}, Cb:{nCb}, Cr:{nCr}) vs Vector128(Y:{unifiedY[i]}, Cb:{unifiedCb[i]}, Cr:{unifiedCr[i]})");
                    }
                    diffCount++;
                }
            }

            if (diffCount > 0)
            {
                Console.WriteLine($"Total Diffs: {diffCount}");
                Assert.True(diffCount == 0, $"Found diffs in Rgb2YCbCr conversion.\n{sb.ToString()}");
            }
        }
        [Fact]
        public unsafe void YCbCr2RgbParallelVector256_NativeParityRandomData()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");

            int width = 1248;
            int height = 1024;
            int rowSizeInBytes = width * 3;
            int totalBytes = height * rowSizeInBytes;

            byte[] managedBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] nativeBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);

            new Random(12345).NextBytes(managedBuffer);
            Buffer.BlockCopy(managedBuffer, 0, nativeBuffer, 0, totalBytes);

            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            fixed (byte* pOutM = managedBuffer, pOutN = nativeBuffer)
            {
                IntPtr nativePtr = DjvuMarshal.AllocHGlobal((uint)totalBytes);
                try
                {
                    Marshal.Copy(nativeBuffer, 0, nativePtr, totalBytes);

                    InterWaveSimd.YCbCr2RgbParallelVector256((Pixel*)pOutM, width, height, rowSizeInBytes, options);

                    bool success = NativeMethods.YCbCrToRgb(nativePtr, width, height, width);
                    Assert.True(success);

                    Marshal.Copy(nativePtr, nativeBuffer, 0, totalBytes);
                }
                finally
                {
                    if (nativePtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativePtr);
                }

                Assert.Equal(0.0, Util.ImageBinaryDiff(pOutN, pOutM, width, height, rowSizeInBytes));
            }
        }

        [Fact]
        public unsafe void YCbCr2RgbParallelVector256_NativeParityWithStride()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");

            int width = 120;
            int height = 16;
            int rowSizeInPixels = width + 1; // 1 pixel of padding (3 bytes)
            int rowSizeInBytes = rowSizeInPixels * 3;
            int totalBytes = height * rowSizeInBytes;

            byte[] managedBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] nativeBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);

            new Random(12345).NextBytes(managedBuffer);
            Buffer.BlockCopy(managedBuffer, 0, nativeBuffer, 0, totalBytes);

            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            fixed (byte* pOutM = managedBuffer, pOutN = nativeBuffer)
            {
                IntPtr nativePtr = DjvuMarshal.AllocHGlobal((uint)totalBytes);
                try
                {
                    Marshal.Copy(nativeBuffer, 0, nativePtr, totalBytes);

                    InterWaveSimd.YCbCr2RgbParallelVector256((Pixel*)pOutM, width, height, rowSizeInBytes, options);

                    bool success = NativeMethods.YCbCrToRgb(nativePtr, width, height, rowSizeInPixels);
                    Assert.True(success);

                    Marshal.Copy(nativePtr, nativeBuffer, 0, totalBytes);
                }
                finally
                {
                    if (nativePtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativePtr);
                }

                Assert.Equal(0.0, Util.ImageBinaryDiff(pOutN, pOutM, width, height, rowSizeInBytes));
            }
        }
        [Fact]
        public unsafe void YCbCr2RgbParallelVector256_BruteForceParity()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");

            int width = 4096;
            int height = 4096;
            int rowSizeInBytes = width * 3;
            int totalBytes = height * rowSizeInBytes;

            byte[] managedBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] nativeBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] backupBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);

            fixed (byte* pBackup = backupBuffer)
            {
                Pixel* pGenerator = (Pixel*)pBackup;
                for (int y = sbyte.MinValue; y <= sbyte.MaxValue; y++)
                {
                    for (int cb = sbyte.MinValue; cb <= sbyte.MaxValue; cb++)
                    {
                        for (int cr = sbyte.MinValue; cr <= sbyte.MaxValue; cr++)
                        {
                            pGenerator->Blue = (sbyte)y;
                            pGenerator->Green = (sbyte)cb;
                            pGenerator->Red = (sbyte)cr;
                            pGenerator++;
                        }
                    }
                }
            }

            Buffer.BlockCopy(backupBuffer, 0, managedBuffer, 0, totalBytes);
            Buffer.BlockCopy(backupBuffer, 0, nativeBuffer, 0, totalBytes);

            var options = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            fixed (byte* pOutM = managedBuffer)
            {
                IntPtr nativePtr = DjvuMarshal.AllocHGlobal((uint)totalBytes);
                try
                {
                    Marshal.Copy(nativeBuffer, 0, nativePtr, totalBytes);

                    InterWaveSimd.YCbCr2RgbParallelVector256((Pixel*)pOutM, width, height, rowSizeInBytes, options);

                    bool success = NativeMethods.YCbCrToRgb(nativePtr, width, height, width);
                    Assert.True(success);

                    Marshal.Copy(nativePtr, nativeBuffer, 0, totalBytes);
                }
                finally
                {
                    DjvuMarshal.FreeHGlobal(nativePtr);
                }
            }

            int diffCount = 0;
            var sb = new System.Text.StringBuilder();

            fixed (byte* pNative = nativeBuffer, pManaged = managedBuffer, pBackup = backupBuffer)
            {
                Pixel* pN = (Pixel*)pNative;
                Pixel* pM = (Pixel*)pManaged;
                Pixel* pIn = (Pixel*)pBackup;
                int totalPixels = width * height;

                for (int i = 0; i < totalPixels; i++, pN++, pM++, pIn++)
                {
                    if (pM->Blue != pN->Blue || pM->Green != pN->Green || pM->Red != pN->Red)
                    {
                        if (diffCount < 256)
                        {
                            sb.AppendLine($"Diff at {i} (Y:{pIn->Blue}, Cb:{pIn->Green}, Cr:{pIn->Red}) - Native(B:{pN->Blue}, G:{pN->Green}, R:{pN->Red}) vs Vector256(B:{pM->Blue}, G:{pM->Green}, R:{pM->Red})");
                        }
                        diffCount++;
                    }
                }
            }

            if (diffCount > 0)
            {
                Console.WriteLine($"Total Diffs: {diffCount}");
                Assert.True(diffCount == 0, $"Found diffs in YCbCr2Rgb conversion.\n{sb.ToString()}");
            }
        }
        [Fact]
        public unsafe void YCbCr2RgbVector256_NativeParityRandomData()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");

            int width = 1248;
            int height = 1024;
            int rowSizeInBytes = width * 3;
            int totalBytes = height * rowSizeInBytes;

            byte[] managedBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] nativeBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);

            new Random(12345).NextBytes(managedBuffer);
            Buffer.BlockCopy(managedBuffer, 0, nativeBuffer, 0, totalBytes);

            fixed (byte* pOutM = managedBuffer, pOutN = nativeBuffer)
            {
                IntPtr nativePtr = DjvuMarshal.AllocHGlobal((uint)totalBytes);
                try
                {
                    Marshal.Copy(nativeBuffer, 0, nativePtr, totalBytes);

                    InterWaveSimd.YCbCr2RgbVector256((Pixel*)pOutM, width, height, rowSizeInBytes);

                    bool success = NativeMethods.YCbCrToRgb(nativePtr, width, height, width);
                    Assert.True(success);

                    Marshal.Copy(nativePtr, nativeBuffer, 0, totalBytes);
                }
                finally
                {
                    if (nativePtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativePtr);
                }

                Assert.Equal(0.0, Util.ImageBinaryDiff(pOutN, pOutM, width, height, rowSizeInBytes));
            }
        }

        [Fact]
        public unsafe void YCbCr2RgbVector256_NativeParityWithStride()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");

            int width = 120;
            int height = 16;
            int rowSizeInPixels = width + 1; // 1 pixel of padding (3 bytes)
            int rowSizeInBytes = rowSizeInPixels * 3;
            int totalBytes = height * rowSizeInBytes;

            byte[] managedBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] nativeBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);

            new Random(12345).NextBytes(managedBuffer);
            Buffer.BlockCopy(managedBuffer, 0, nativeBuffer, 0, totalBytes);

            fixed (byte* pOutM = managedBuffer, pOutN = nativeBuffer)
            {
                IntPtr nativePtr = DjvuMarshal.AllocHGlobal((uint)totalBytes);
                try
                {
                    Marshal.Copy(nativeBuffer, 0, nativePtr, totalBytes);

                    InterWaveSimd.YCbCr2RgbVector256((Pixel*)pOutM, width, height, rowSizeInBytes);

                    bool success = NativeMethods.YCbCrToRgb(nativePtr, width, height, rowSizeInPixels);
                    Assert.True(success);

                    Marshal.Copy(nativePtr, nativeBuffer, 0, totalBytes);
                }
                finally
                {
                    if (nativePtr != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativePtr);
                }

                Assert.Equal(0.0, Util.ImageBinaryDiff(pOutN, pOutM, width, height, rowSizeInBytes));
            }
        }

        [Fact]
        public unsafe void YCbCr2RgbVector256_BruteForceParity()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");

            int width = 4096;
            int height = 4096;
            int rowSizeInBytes = width * 3;
            int totalBytes = height * rowSizeInBytes;

            byte[] managedBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] nativeBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] backupBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);

            fixed (byte* pBackup = backupBuffer)
            {
                Pixel* pGenerator = (Pixel*)pBackup;
                for (int y = sbyte.MinValue; y <= sbyte.MaxValue; y++)
                {
                    for (int cb = sbyte.MinValue; cb <= sbyte.MaxValue; cb++)
                    {
                        for (int cr = sbyte.MinValue; cr <= sbyte.MaxValue; cr++)
                        {
                            pGenerator->Blue = (sbyte)y;
                            pGenerator->Green = (sbyte)cb;
                            pGenerator->Red = (sbyte)cr;
                            pGenerator++;
                        }
                    }
                }
            }

            Buffer.BlockCopy(backupBuffer, 0, managedBuffer, 0, totalBytes);
            Buffer.BlockCopy(backupBuffer, 0, nativeBuffer, 0, totalBytes);

            fixed (byte* pOutM = managedBuffer)
            {
                IntPtr nativePtr = DjvuMarshal.AllocHGlobal((uint)totalBytes);
                try
                {
                    Marshal.Copy(nativeBuffer, 0, nativePtr, totalBytes);

                    InterWaveSimd.YCbCr2RgbVector256((Pixel*)pOutM, width, height, rowSizeInBytes);

                    bool success = NativeMethods.YCbCrToRgb(nativePtr, width, height, width);
                    Assert.True(success);

                    Marshal.Copy(nativePtr, nativeBuffer, 0, totalBytes);
                }
                finally
                {
                    DjvuMarshal.FreeHGlobal(nativePtr);
                }
            }

            int diffCount = 0;
            var sb = new System.Text.StringBuilder();

            fixed (byte* pNative = nativeBuffer, pManaged = managedBuffer, pBackup = backupBuffer)
            {
                Pixel* pN = (Pixel*)pNative;
                Pixel* pM = (Pixel*)pManaged;
                Pixel* pIn = (Pixel*)pBackup;
                int totalPixels = width * height;

                for (int i = 0; i < totalPixels; i++, pN++, pM++, pIn++)
                {
                    if (pM->Blue != pN->Blue || pM->Green != pN->Green || pM->Red != pN->Red)
                    {
                        if (diffCount < 256)
                        {
                            sb.AppendLine($"Diff at {i} (Y:{pIn->Blue}, Cb:{pIn->Green}, Cr:{pIn->Red}) - Native(B:{pN->Blue}, G:{pN->Green}, R:{pN->Red}) vs Vector256(B:{pM->Blue}, G:{pM->Green}, R:{pM->Red})");
                        }
                        diffCount++;
                    }
                }
            }

            if (diffCount > 0)
            {
                Console.WriteLine($"Total Diffs: {diffCount}");
                Assert.True(diffCount == 0, $"Found diffs in YCbCr2Rgb conversion.\n{sb.ToString()}");
            }
        }
        [Fact]
        public unsafe void YCbCr2RgbScalar_Fused_BruteForceParity()
        {
            int width = 4096;
            int height = 4096;
            int rowSizeInBytes = width * 3;
            int totalBytes = height * rowSizeInBytes;

            short[] arrY = GC.AllocateUninitializedArray<short>(width * height);
            short[] arrCb = GC.AllocateUninitializedArray<short>(width * height);
            short[] arrCr = GC.AllocateUninitializedArray<short>(width * height);
            
            byte[] managedBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] backupBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] nativeBuffer = GC.AllocateUninitializedArray<byte>(totalBytes);

            fixed (byte* pBackup = backupBuffer)
            fixed (short* pY = arrY, pCb = arrCb, pCr = arrCr)
            {
                Pixel* pGenerator = (Pixel*)pBackup;
                short* pGenY = pY, pGenCb = pCb, pGenCr = pCr;
                for (int y = sbyte.MinValue; y <= sbyte.MaxValue; y++)
                {
                    for (int cb = sbyte.MinValue; cb <= sbyte.MaxValue; cb++)
                    {
                        for (int cr = sbyte.MinValue; cr <= sbyte.MaxValue; cr++)
                        {
                            pGenerator->Blue = (sbyte)y; pGenerator->Green = (sbyte)cb; pGenerator->Red = (sbyte)cr; pGenerator++;
                            *pGenY++ = (short)(y << 6); *pGenCb++ = (short)(cb << 6); *pGenCr++ = (short)(cr << 6);
                        }
                    }
                }
            }

            Buffer.BlockCopy(backupBuffer, 0, managedBuffer, 0, totalBytes);
            Buffer.BlockCopy(backupBuffer, 0, nativeBuffer, 0, totalBytes);

            fixed (byte* pOutM = managedBuffer)
            fixed (byte* pNative = nativeBuffer)
            fixed (short* pY = arrY, pCb = arrCb, pCr = arrCr)
            {
                InterWaveTransform.YCbCr2RgbScalar(pY, pCb, pCr, (sbyte*)pOutM, width, height, rowSizeInBytes, width);

                bool success = NativeMethods.YCbCrToRgb((IntPtr)pNative, width, height, width);
                Assert.True(success);

                double diff = Util.ImageBinaryDiff(pNative, pOutM, width, height, rowSizeInBytes);
                if (diff > 0.0)
                {
                    Util.DumpImageMismatch(pNative, pOutM, totalBytes, width, 0, "FusedScalarBruteForce");
                    Assert.Fail($"Found diffs in Fused YCbCr2Rgb conversion. Mismatches dumped to log. Diff Score: {diff}");
                }
            }
        }

        private unsafe delegate int FusedSimdKernel(short* pY, short* pCb, short* pCr, sbyte* pImg8, int height, int width, int srcStride, int rowSizeInBytes, int startX);

        private unsafe void RunFusedBruteForceParity(FusedSimdKernel simdKernel)
        {
            string testName = Xunit.TestContext.Current.Test.TestDisplayName;
            int width = 4096;
            int height = 4096;
            int rowSizeInBytes = width * 3;
            int totalBytes = height * rowSizeInBytes;

            short[] arrY = GC.AllocateUninitializedArray<short>(width * height);
            short[] arrCb = GC.AllocateUninitializedArray<short>(width * height);
            short[] arrCr = GC.AllocateUninitializedArray<short>(width * height);
            
            sbyte[] managedBuffer = GC.AllocateUninitializedArray<sbyte>(totalBytes);
            sbyte[] backupBuffer = GC.AllocateUninitializedArray<sbyte>(totalBytes);
            sbyte[] nativeBuffer = GC.AllocateUninitializedArray<sbyte>(totalBytes);

            fixed (sbyte* pBackup = backupBuffer)
            fixed (short* pY = arrY, pCb = arrCb, pCr = arrCr)
            {
                Pixel* pGenerator = (Pixel*)pBackup;
                short* pGenY = pY, pGenCb = pCb, pGenCr = pCr;
                for (int y = sbyte.MinValue; y <= sbyte.MaxValue; y++)
                {
                    for (int cb = sbyte.MinValue; cb <= sbyte.MaxValue; cb++)
                    {
                        for (int cr = sbyte.MinValue; cr <= sbyte.MaxValue; cr++)
                        {
                            pGenerator->Blue = (sbyte)y; pGenerator->Green = (sbyte)cb; pGenerator->Red = (sbyte)cr; pGenerator++;
                            *pGenY++ = (short)(y << 6); *pGenCb++ = (short)(cb << 6); *pGenCr++ = (short)(cr << 6);
                        }
                    }
                }
            }

            Buffer.BlockCopy(backupBuffer, 0, managedBuffer, 0, totalBytes);
            Buffer.BlockCopy(backupBuffer, 0, nativeBuffer, 0, totalBytes);

            fixed (sbyte* pOutM = managedBuffer)
            fixed (sbyte* pNative = nativeBuffer)
            fixed (short* pY = arrY, pCb = arrCb, pCr = arrCr)
            {
                simdKernel(pY, pCb, pCr, pOutM, height, width, width, rowSizeInBytes, 0);

                bool success = NativeMethods.YCbCrToRgb((IntPtr)pNative, width, height, width);
                Assert.True(success);

                double diff = Util.ImageBinaryDiff((byte*)pNative, (byte*)pOutM, width, height, rowSizeInBytes);
                if (diff > 0.0)
                {
                    Util.DumpImageMismatch((byte*)pNative, (byte*)pOutM, totalBytes, width, 0, testName);
                    Assert.Fail($"Found diffs in {testName} fused YCbCr2Rgb conversion. Diff Score: {diff}");
                }
            }
        }

        [Fact]
        public unsafe void YCbCr2RgbVector128_Fused_BruteForceParity()
        {
            if (!Ssse3.IsSupported && !AdvSimd.Arm64.IsSupported)
                Assert.Skip("Vector128 (SSSE3 or ARM64 AdvSimd) hardware acceleration is not supported on this architecture.");
            
            RunFusedBruteForceParity(InterWaveSimd.YCbCr2RgbVector128);
        }

        [Fact]
        public unsafe void YCbCr2RgbVector256_Fused_BruteForceParity()
        {
            if (!Avx2.IsSupported)
                Assert.Skip("Vector256 (AVX2) hardware acceleration is not supported on this architecture.");
            
            RunFusedBruteForceParity(InterWaveSimd.YCbCr2RgbVector256);
        }

        [Fact]
        public unsafe void YCbCr2RgbVector512_Fused_BruteForceParity()
        {
            if (!Avx512F.IsSupported || !Avx512BW.IsSupported)
                Assert.Skip("Vector512 (AVX-512 F/BW) hardware acceleration is not supported on this architecture.");
            
            RunFusedBruteForceParity(InterWaveSimd.YCbCr2RgbVector512);
        }
    }
}