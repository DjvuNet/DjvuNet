using System;
using System.IO;
using System.IO.Compression;
using System.Formats.Tar;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.Wavelet;
using DjvuNet.Tests;

namespace DjvuNet.Wavelet.Tests
{
    public class InterWaveTransformUnifiedTests
    {
        private const string TestImageContinuous = "TitanIR-5448x3686-24bpp-YCbCr.bin.tar.gz";
        private const string TestImagePadded = "TitanIR-padded-5447x3686-24bpp-YCbCr.bin.tar.gz";

        private string GetArtifactPath(string fileName)
        {
            return Path.Combine(Util.ArtifactsPath, fileName);
        }

        private byte[] ReadGzBinary(string archivePath, int totalBytes)
        {
            using (FileStream fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read))
            using (GZipStream gzip = new GZipStream(fs, CompressionMode.Decompress))
            using (TarReader tar = new TarReader(gzip))
            {
                TarEntry entry = tar.GetNextEntry();
                if (entry == null || entry.DataStream == null)
                    throw new InvalidDataException("No valid file found in the tar.gz archive.");

                byte[] rawBytes = new byte[totalBytes];
                int totalRead = 0;
                int bytesRead;

                while (totalRead < totalBytes && (bytesRead = entry.DataStream.Read(rawBytes, totalRead, totalBytes - totalRead)) > 0)
                {
                    totalRead += bytesRead;
                }

                if (totalRead != totalBytes)
                    throw new InvalidDataException($"Failed to read complete artifact from tar.gz. Expected {totalBytes}, got {totalRead}");

                return rawBytes;
            }
        }

        [Fact]
        public unsafe void YCbCr2Rgb_MatchesScalar_RealData_Continuous()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            string imagePath = GetArtifactPath(TestImageContinuous);
            Assert.True(File.Exists(imagePath), $"Test artifact not found at: {imagePath}");

            int width = 5448;
            int height = 3686;
            int rowSizeInBytes = width * 3;
            int totalBytes = rowSizeInBytes * height;

            byte[] sourceData = ReadGzBinary(imagePath, totalBytes);
            Assert.Equal(totalBytes, sourceData.Length);

            byte[] bufferScalar = new byte[totalBytes];
            byte[] bufferUnified = new byte[totalBytes];
            Buffer.BlockCopy(sourceData, 0, bufferScalar, 0, totalBytes);
            Buffer.BlockCopy(sourceData, 0, bufferUnified, 0, totalBytes);

            fixed (byte* ptrScalar = bufferScalar)
            fixed (byte* ptrUnified = bufferUnified)
            {
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)ptrScalar, width, height, rowSizeInBytes);

                InterWaveTransform.YCbCr2Rgb((Pixel*)ptrUnified, width, height, rowSizeInBytes);

                double diff = Util.ImageBinaryDiff(ptrScalar, ptrUnified, width, height, rowSizeInBytes, 0, PixelSize._24bpp, ChannelSize._8bit);
                Assert.Equal(0.0, diff);
            }
        }

        [Fact]
        public unsafe void YCbCr2Rgb_MatchesScalar_RealData_Padded()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            string imagePath = GetArtifactPath(TestImagePadded);
            Assert.True(File.Exists(imagePath), $"Test artifact not found at: {imagePath}");

            int width = 5447;
            int height = 3686;
            int rowSizeInBytes = (width * 3 + 3) & ~3; // 16344 bytes
            int totalBytes = rowSizeInBytes * height;

            byte[] sourceData = ReadGzBinary(imagePath, totalBytes);
            Assert.Equal(totalBytes, sourceData.Length);

            byte[] bufferScalar = new byte[totalBytes];
            byte[] bufferUnified = new byte[totalBytes];
            Buffer.BlockCopy(sourceData, 0, bufferScalar, 0, totalBytes);
            Buffer.BlockCopy(sourceData, 0, bufferUnified, 0, totalBytes);

            fixed (byte* ptrScalar = bufferScalar)
            fixed (byte* ptrUnified = bufferUnified)
            {
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)ptrScalar, width, height, rowSizeInBytes);

                InterWaveTransform.YCbCr2Rgb((Pixel*)ptrUnified, width, height, rowSizeInBytes);

                double diff = Util.ImageBinaryDiff(ptrScalar, ptrUnified, width, height, rowSizeInBytes, 0, PixelSize._24bpp, ChannelSize._8bit);

                // Assert that the scalar method matches the rowSizeInBytes-safe method
                Assert.Equal(0.0, diff);
            }
        }

        [Fact]
        public unsafe void YCbCr2Rgb_MatchesScalar_SmallSequential()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            int width = 50;
            int height = 3;
            int rowSizeInBytes = (width * 3 + 3) & ~3; // 152 bytes
            int totalBytes = rowSizeInBytes * height;

            byte[] bufferScalar = new byte[totalBytes];
            byte[] bufferUnified = new byte[totalBytes];

            // Fill with sequential bytes to easily track permutations
            for (uint i = 0; i < totalBytes; i++)
            {
                bufferScalar[i] = (byte)(i % 251);
                bufferUnified[i] = bufferScalar[i];
            }

            fixed (byte* ptrScalar = bufferScalar)
            fixed (byte* ptrUnified = bufferUnified)
            {
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)ptrScalar, width, height, rowSizeInBytes);

                InterWaveTransform.YCbCr2Rgb((Pixel*)ptrUnified, width, height, rowSizeInBytes);

                double diff = Util.ImageBinaryDiff(ptrScalar, ptrUnified, width, height, rowSizeInBytes, 0, PixelSize._24bpp, ChannelSize._8bit);
                Assert.Equal(0.0, diff);
            }
        }

        [Fact]
        public unsafe void Rgb2YCbCr_InputValidation_NullPointers_ThrowsDjvuArgumentNullException()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            IntPtr pIn = IntPtr.Zero, pY = IntPtr.Zero, pCb = IntPtr.Zero, pCr = IntPtr.Zero;
            try
            {
                pIn = Marshal.AllocHGlobal(300);
                pY = Marshal.AllocHGlobal(100);
                pCb = Marshal.AllocHGlobal(100);
                pCr = Marshal.AllocHGlobal(100);
                var exPix = Assert.Throws<DjvuArgumentNullException>(() => InterWaveTransform.Rgb2YCbCr(null, 10, 10, 30, (sbyte*)pY, (sbyte*)pCb, (sbyte*)pCr, 10));
                Assert.Equal("pPixBuff", exPix.ParamName);
                Assert.Contains("Value cannot be null.", exPix.Message);

                var exY = Assert.Throws<DjvuArgumentNullException>(() => InterWaveTransform.Rgb2YCbCr((Pixel*)pIn, 10, 10, 30, null, (sbyte*)pCb, (sbyte*)pCr, 10));
                Assert.Equal("pOutY", exY.ParamName);
                Assert.Contains("Value cannot be null.", exY.Message);

                var exCb = Assert.Throws<DjvuArgumentNullException>(() => InterWaveTransform.Rgb2YCbCr((Pixel*)pIn, 10, 10, 30, (sbyte*)pY, null, (sbyte*)pCr, 10));
                Assert.Equal("pOutCb", exCb.ParamName);
                Assert.Contains("Value cannot be null.", exCb.Message);

                var exCr = Assert.Throws<DjvuArgumentNullException>(() => InterWaveTransform.Rgb2YCbCr((Pixel*)pIn, 10, 10, 30, (sbyte*)pY, (sbyte*)pCb, null, 10));
                Assert.Equal("pOutCr", exCr.ParamName);
                Assert.Contains("Value cannot be null.", exCr.Message);
            }
            finally
            {
                if (pIn != IntPtr.Zero) Marshal.FreeHGlobal(pIn);
                if (pY != IntPtr.Zero) Marshal.FreeHGlobal(pY);
                if (pCb != IntPtr.Zero) Marshal.FreeHGlobal(pCb);
                if (pCr != IntPtr.Zero) Marshal.FreeHGlobal(pCr);
            }
        }

        [Fact]
        public unsafe void YCbCr2Rgb_InputValidation_NullPointers_ThrowsDjvuArgumentNullException()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            var ex = Assert.Throws<DjvuArgumentNullException>(() => InterWaveTransform.YCbCr2Rgb(null, 10, 10, 30));
            Assert.Equal("pPixBuff", ex.ParamName);
            Assert.Contains("Value cannot be null.", ex.Message);
        }

        [Fact]
        public unsafe void Rgb2YCbCr_InputValidation_InvalidDimensions_ThrowsDjvuArgumentOutOfRangeException()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            IntPtr pIn = IntPtr.Zero, pY = IntPtr.Zero, pCb = IntPtr.Zero, pCr = IntPtr.Zero;
            try
            {
                pIn = Marshal.AllocHGlobal(300);
                pY = Marshal.AllocHGlobal(100);
                pCb = Marshal.AllocHGlobal(100);
                pCr = Marshal.AllocHGlobal(100);
                var exW = Assert.Throws<DjvuArgumentOutOfRangeException>(() => InterWaveTransform.Rgb2YCbCr((Pixel*)pIn, 0, 10, 30, (sbyte*)pY, (sbyte*)pCb, (sbyte*)pCr, 10));
                Assert.Equal("width", exW.ParamName);
                Assert.Equal(0, exW.ActualValue);
                Assert.Contains("Width must be greater than zero.", exW.Message);
                Assert.Contains("Actual value was 0.", exW.Message);

                var exH = Assert.Throws<DjvuArgumentOutOfRangeException>(() => InterWaveTransform.Rgb2YCbCr((Pixel*)pIn, 10, -1, 30, (sbyte*)pY, (sbyte*)pCb, (sbyte*)pCr, 10));
                Assert.Equal("height", exH.ParamName);
                Assert.Equal(-1, exH.ActualValue);
                Assert.Contains("Height must be greater than zero.", exH.Message);
                Assert.Contains("Actual value was -1.", exH.Message);
            }
            finally
            {
                if (pIn != IntPtr.Zero) Marshal.FreeHGlobal(pIn);
                if (pY != IntPtr.Zero) Marshal.FreeHGlobal(pY);
                if (pCb != IntPtr.Zero) Marshal.FreeHGlobal(pCb);
                if (pCr != IntPtr.Zero) Marshal.FreeHGlobal(pCr);
            }
        }

        [Fact]
        public unsafe void YCbCr2Rgb_InputValidation_InvalidDimensions_ThrowsDjvuArgumentOutOfRangeException()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            IntPtr pIn = IntPtr.Zero;
            try
            {
                pIn = Marshal.AllocHGlobal(300);
                var exW = Assert.Throws<DjvuArgumentOutOfRangeException>(() => InterWaveTransform.YCbCr2Rgb((Pixel*)pIn, 0, 10, 30));
                Assert.Equal("width", exW.ParamName);
                Assert.Equal(0, exW.ActualValue);
                Assert.Contains("Width must be greater than zero.", exW.Message);
                Assert.Contains("Actual value was 0.", exW.Message);

                var exH = Assert.Throws<DjvuArgumentOutOfRangeException>(() => InterWaveTransform.YCbCr2Rgb((Pixel*)pIn, 10, -1, 30));
                Assert.Equal("height", exH.ParamName);
                Assert.Equal(-1, exH.ActualValue);
                Assert.Contains("Height must be greater than zero.", exH.Message);
                Assert.Contains("Actual value was -1.", exH.Message);
            }
            finally
            {
                if (pIn != IntPtr.Zero) Marshal.FreeHGlobal(pIn);
            }
        }

        [Fact]
        public unsafe void Rgb2YCbCr_InputValidation_InvalidStride_ThrowsDjvuArgumentOutOfRangeException()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            IntPtr pIn = IntPtr.Zero, pY = IntPtr.Zero, pCb = IntPtr.Zero, pCr = IntPtr.Zero;
            try
            {
                pIn = Marshal.AllocHGlobal(300);
                pY = Marshal.AllocHGlobal(100);
                pCb = Marshal.AllocHGlobal(100);
                pCr = Marshal.AllocHGlobal(100);
                var exIn = Assert.Throws<DjvuArgumentOutOfRangeException>(() => InterWaveTransform.Rgb2YCbCr((Pixel*)pIn, 10, 10, 29, (sbyte*)pY, (sbyte*)pCb, (sbyte*)pCr, 10));
                Assert.Equal("rowSizeInBytes", exIn.ParamName);
                Assert.Equal(29, exIn.ActualValue);
                Assert.Contains("Input row size (29) must be at least width * sizeof(Pixel) (30).", exIn.Message);
                Assert.Contains("Actual value was 29.", exIn.Message);

                var exOut = Assert.Throws<DjvuArgumentOutOfRangeException>(() => InterWaveTransform.Rgb2YCbCr((Pixel*)pIn, 10, 10, 30, (sbyte*)pY, (sbyte*)pCb, (sbyte*)pCr, 9));
                Assert.Equal("outRowSizeInBytes", exOut.ParamName);
                Assert.Equal(9, exOut.ActualValue);
                Assert.Contains("Output row size (9) must be at least width (10).", exOut.Message);
                Assert.Contains("Actual value was 9.", exOut.Message);
            }
            finally
            {
                if (pIn != IntPtr.Zero) Marshal.FreeHGlobal(pIn);
                if (pY != IntPtr.Zero) Marshal.FreeHGlobal(pY);
                if (pCb != IntPtr.Zero) Marshal.FreeHGlobal(pCb);
                if (pCr != IntPtr.Zero) Marshal.FreeHGlobal(pCr);
            }
        }

        [Fact]
        public unsafe void YCbCr2Rgb_InputValidation_InvalidStride_ThrowsDjvuArgumentOutOfRangeException()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            IntPtr pIn = IntPtr.Zero;
            try
            {
                pIn = Marshal.AllocHGlobal(300);
                var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => InterWaveTransform.YCbCr2Rgb((Pixel*)pIn, 10, 10, 29));
                Assert.Equal("rowSizeInBytes", ex.ParamName);
                Assert.Equal(29, ex.ActualValue);
                Assert.Contains("Row size (29) must be at least width * sizeof(Pixel) (30).", ex.Message);
                Assert.Contains("Actual value was 29.", ex.Message);
            }
            finally
            {
                if (pIn != IntPtr.Zero) Marshal.FreeHGlobal(pIn);
            }
        }
        [Fact]
        public unsafe void Rgb2YCbCr_InputValidation_OverlappingInputOutput_ThrowsDjvuInvalidOperationException()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            IntPtr pShared = IntPtr.Zero, pSafe1 = IntPtr.Zero, pSafe2 = IntPtr.Zero;
            try
            {
                pShared = Marshal.AllocHGlobal(400);
                pSafe1 = Marshal.AllocHGlobal(100);
                pSafe2 = Marshal.AllocHGlobal(100);

                var ex1 = Assert.Throws<DjvuInvalidOperationException>(() =>
                    InterWaveTransform.Rgb2YCbCr((Pixel*)pShared, 10, 10, 30, (sbyte*)pShared, (sbyte*)pSafe1, (sbyte*)pSafe2, 10));
                Assert.Contains("Source and destination buffers must be distinct", ex1.Message);

                var ex2 = Assert.Throws<DjvuInvalidOperationException>(() =>
                    InterWaveTransform.Rgb2YCbCr((Pixel*)pShared, 10, 10, 30, (sbyte*)pSafe1, (sbyte*)pShared, (sbyte*)pSafe2, 10));

                var ex3 = Assert.Throws<DjvuInvalidOperationException>(() =>
                    InterWaveTransform.Rgb2YCbCr((Pixel*)pShared, 10, 10, 30, (sbyte*)pSafe1, (sbyte*)pSafe2, (sbyte*)pShared, 10));
            }
            finally
            {
                if (pShared != IntPtr.Zero) Marshal.FreeHGlobal(pShared);
                if (pSafe1 != IntPtr.Zero) Marshal.FreeHGlobal(pSafe1);
                if (pSafe2 != IntPtr.Zero) Marshal.FreeHGlobal(pSafe2);
            }
        }

        [Fact]
        public unsafe void Rgb2YCbCr_InputValidation_OverlappingOutputBuffers_ThrowsDjvuInvalidOperationException()
        {
            if (!Avx2.IsSupported)
            {
                Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            }

            IntPtr pIn = IntPtr.Zero, pSharedOut = IntPtr.Zero, pSafeOut = IntPtr.Zero;
            try
            {
                pIn = Marshal.AllocHGlobal(300);
                pSharedOut = Marshal.AllocHGlobal(200);
                pSafeOut = Marshal.AllocHGlobal(100);

                var ex1 = Assert.Throws<DjvuInvalidOperationException>(() =>
                    InterWaveTransform.Rgb2YCbCr((Pixel*)pIn, 10, 10, 30, (sbyte*)pSharedOut, (sbyte*)pSharedOut, (sbyte*)pSafeOut, 10));
                Assert.Contains("Destination planar buffers (Y, Cb, Cr) must be distinct and not overlap.", ex1.Message);

                var ex2 = Assert.Throws<DjvuInvalidOperationException>(() =>
                    InterWaveTransform.Rgb2YCbCr((Pixel*)pIn, 10, 10, 30, (sbyte*)pSharedOut, (sbyte*)pSafeOut, (sbyte*)pSharedOut, 10));

                var ex3 = Assert.Throws<DjvuInvalidOperationException>(() =>
                    InterWaveTransform.Rgb2YCbCr((Pixel*)pIn, 10, 10, 30, (sbyte*)pSafeOut, (sbyte*)pSharedOut, (sbyte*)pSharedOut, 10));
            }
            finally
            {
                if (pIn != IntPtr.Zero) Marshal.FreeHGlobal(pIn);
                if (pSharedOut != IntPtr.Zero) Marshal.FreeHGlobal(pSharedOut);
                if (pSafeOut != IntPtr.Zero) Marshal.FreeHGlobal(pSafeOut);
            }
        }

        [Theory]
        [InlineData(15, 100, false)] // Sub-Vector128 (forces scalar)
        [InlineData(16, 100, false)] // Exact Vector128
        [InlineData(17, 100, false)] // Vector128 + 1 (tests tail logic)
        [InlineData(23, 100, true)]  // Odd size, padded
        [InlineData(29, 100, false)] // Pre-Vector256
        [InlineData(31, 100, false)] // Pre-Vector256
        [InlineData(32, 100, false)] // Exact Vector256
        [InlineData(33, 100, false)] // Vector256 + 1 (tests tail logic)
        [InlineData(47, 100, true)]  // Odd size, padded
        [InlineData(48, 100, false)] // Vector256 * 1.5
        [InlineData(49, 100, false)] // Vector256 * 1.5 + 1
        [InlineData(63, 100, false)] // Pre-2x Vector256
        [InlineData(64, 100, false)] // Exact 2x Vector256
        [InlineData(65, 100, true)]  // 2x Vector256 + 1, padded
        public unsafe void YCbCr2Rgb_VectorWidthEdgeCases_MatchesScalar(int width, int height, bool isPadded)
        {
            if (!Avx2.IsSupported && !Vector128.IsHardwareAccelerated)
                Assert.Skip("SIMD hardware acceleration is not supported on this CPU.");

            int baseRowSize = width * 3;
            int rowSizeInBytes = isPadded ? ((baseRowSize + 3) & ~3) : baseRowSize;
            int totalBytes = rowSizeInBytes * height;

            byte[] bufferScalar = new byte[totalBytes];
            byte[] bufferUnified = new byte[totalBytes];

            for (uint i = 0; i < totalBytes; i++)
            {
                bufferScalar[i] = (byte)(i % 251);
                bufferUnified[i] = bufferScalar[i];
            }

            fixed (byte* ptrScalar = bufferScalar)
            fixed (byte* ptrUnified = bufferUnified)
            {
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)ptrScalar, width, height, rowSizeInBytes);
                InterWaveTransform.YCbCr2Rgb((Pixel*)ptrUnified, width, height, rowSizeInBytes);
                Assert.Equal(0.0, Util.ImageBinaryDiff(ptrScalar, ptrUnified, width, height, rowSizeInBytes, 0, PixelSize._24bpp, ChannelSize._8bit));
            }
        }

        [Theory]
        [InlineData(15, 100, false)]
        [InlineData(16, 100, false)]
        [InlineData(17, 100, false)]
        [InlineData(23, 100, true)]
        [InlineData(29, 100, false)]
        [InlineData(31, 100, false)]
        [InlineData(32, 100, false)]
        [InlineData(33, 100, false)]
        [InlineData(47, 100, true)]
        [InlineData(48, 100, false)]
        [InlineData(49, 100, false)]
        [InlineData(63, 100, false)]
        [InlineData(64, 100, false)]
        [InlineData(65, 100, true)]
        public unsafe void Rgb2YCbCr_VectorWidthEdgeCases_MatchesScalar(int width, int height, bool isPadded)
        {
            if (!Avx2.IsSupported && !Vector128.IsHardwareAccelerated)
                Assert.Skip("SIMD hardware acceleration is not supported on this CPU.");

            int baseInRowSize = width * 3;
            int rowSizeInBytes = isPadded ? ((baseInRowSize + 3) & ~3) : baseInRowSize;
            int totalInBytes = rowSizeInBytes * height;

            int outRowSizeInBytes = isPadded ? ((width + 3) & ~3) : width;
            int totalOutBytes = outRowSizeInBytes * height;

            byte[] bufferIn = new byte[totalInBytes];
            for (uint i = 0; i < totalInBytes; i++) bufferIn[i] = (byte)(i % 251);

            byte[] yScalar = new byte[totalOutBytes];
            byte[] cbScalar = new byte[totalOutBytes];
            byte[] crScalar = new byte[totalOutBytes];

            byte[] yUnified = new byte[totalOutBytes];
            byte[] cbUnified = new byte[totalOutBytes];
            byte[] crUnified = new byte[totalOutBytes];

            fixed (byte* pIn = bufferIn)
            fixed (byte* pYS = yScalar, pCbS = cbScalar, pCrS = crScalar)
            fixed (byte* pYU = yUnified, pCbU = cbUnified, pCrU = crUnified)
            {
                InterWaveTransform.Rgb2YCbCrScalar((Pixel*)pIn, width, height, rowSizeInBytes, (sbyte*)pYS, (sbyte*)pCbS, (sbyte*)pCrS, outRowSizeInBytes);
                InterWaveTransform.Rgb2YCbCr((Pixel*)pIn, width, height, rowSizeInBytes, (sbyte*)pYU, (sbyte*)pCbU, (sbyte*)pCrU, outRowSizeInBytes);

                Assert.Equal(0.0, Util.ImageBinaryDiff(pYS, pYU, width, height, outRowSizeInBytes, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff(pCbS, pCbU, width, height, outRowSizeInBytes, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff(pCrS, pCrU, width, height, outRowSizeInBytes, 0, PixelSize._8bpp, ChannelSize._8bit));
            }
        }

        // TPL Boundaries (Height = 100 to ensure rows can be distributed to threads)
        // YCbCr2Rgb Boundaries: 9000 (2T), 36000 (4T)
        [Theory]
        [InlineData(89, 100, false)]  // 8900 pixels: Strictly 1 Thread (V256/V128)
        [InlineData(90, 100, false)]  // 9000 pixels: Exact 2 Thread boundary (V256/V128)
        [InlineData(91, 100, true)]   // 9100 pixels: Padded 2 Thread boundary
        [InlineData(359, 100, false)] // 35900 pixels: Just below 4 Thread boundary
        [InlineData(360, 100, false)] // 36000 pixels: Exact 4 Thread ceiling
        [InlineData(361, 100, true)]  // 36100 pixels: Padded 4 Thread ceiling
        public unsafe void YCbCr2Rgb_ParallelRoutingBoundaries_MatchesScalar(int width, int height, bool isPadded)
        {
            if (!Avx2.IsSupported && !Vector128.IsHardwareAccelerated)
                Assert.Skip("SIMD hardware acceleration is not supported on this CPU.");

            int baseRowSize = width * 3;
            int rowSizeInBytes = isPadded ? ((baseRowSize + 3) & ~3) : baseRowSize;
            int totalBytes = rowSizeInBytes * height;

            byte[] bufferScalar = new byte[totalBytes];
            byte[] bufferUnified = new byte[totalBytes];

            for (uint i = 0; i < totalBytes; i++)
            {
                bufferScalar[i] = (byte)(i % 251);
                bufferUnified[i] = bufferScalar[i];
            }

            fixed (byte* ptrScalar = bufferScalar)
            fixed (byte* ptrUnified = bufferUnified)
            {
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)ptrScalar, width, height, rowSizeInBytes);
                InterWaveTransform.YCbCr2Rgb((Pixel*)ptrUnified, width, height, rowSizeInBytes);
                Assert.Equal(0.0, Util.ImageBinaryDiff(ptrScalar, ptrUnified, width, height, rowSizeInBytes, 0, PixelSize._24bpp, ChannelSize._8bit));
            }
        }

        // Rgb2YCbCr Boundaries:
        // V128: 2000 (2T), 9216 (4T), 24000 (6T), 1M (12T)
        // V256: 12000 (2T), 24000 (4T), 2M (6T)
        [Theory]
        [InlineData(19, 100, false)]  // 1900 px: 1T
        [InlineData(20, 100, false)]  // 2000 px: V128 2T boundary
        [InlineData(92, 100, true)]   // 9200 px: near V128 4T boundary
        [InlineData(119, 100, false)] // 11900 px: V256 1T
        [InlineData(120, 100, false)] // 12000 px: V256 2T boundary
        [InlineData(239, 100, true)]  // 23900 px: V256 2T / V128 4T
        [InlineData(240, 100, false)] // 24000 px: V256 4T / V128 6T boundary
        public unsafe void Rgb2YCbCr_ParallelRoutingBoundaries_MatchesScalar(int width, int height, bool isPadded)
        {
            if (!Avx2.IsSupported && !Vector128.IsHardwareAccelerated)
                Assert.Skip("SIMD hardware acceleration is not supported on this CPU.");

            int baseInRowSize = width * 3;
            int rowSizeInBytes = isPadded ? ((baseInRowSize + 3) & ~3) : baseInRowSize;
            int totalInBytes = rowSizeInBytes * height;

            int outRowSizeInBytes = isPadded ? ((width + 3) & ~3) : width;
            int totalOutBytes = outRowSizeInBytes * height;

            byte[] bufferIn = new byte[totalInBytes];
            for (uint i = 0; i < totalInBytes; i++) bufferIn[i] = (byte)(i % 251);

            byte[] yScalar = new byte[totalOutBytes];
            byte[] cbScalar = new byte[totalOutBytes];
            byte[] crScalar = new byte[totalOutBytes];

            byte[] yUnified = new byte[totalOutBytes];
            byte[] cbUnified = new byte[totalOutBytes];
            byte[] crUnified = new byte[totalOutBytes];

            fixed (byte* pIn = bufferIn)
            fixed (byte* pYS = yScalar, pCbS = cbScalar, pCrS = crScalar)
            fixed (byte* pYU = yUnified, pCbU = cbUnified, pCrU = crUnified)
            {
                InterWaveTransform.Rgb2YCbCrScalar((Pixel*)pIn, width, height, rowSizeInBytes, (sbyte*)pYS, (sbyte*)pCbS, (sbyte*)pCrS, outRowSizeInBytes);
                InterWaveTransform.Rgb2YCbCr((Pixel*)pIn, width, height, rowSizeInBytes, (sbyte*)pYU, (sbyte*)pCbU, (sbyte*)pCrU, outRowSizeInBytes);

                Assert.Equal(0.0, Util.ImageBinaryDiff(pYS, pYU, width, height, outRowSizeInBytes, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff(pCbS, pCbU, width, height, outRowSizeInBytes, 0, PixelSize._8bpp, ChannelSize._8bit));
                Assert.Equal(0.0, Util.ImageBinaryDiff(pCrS, pCrU, width, height, outRowSizeInBytes, 0, PixelSize._8bpp, ChannelSize._8bit));
            }
        }
    }
}