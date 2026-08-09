using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using DjvuNet.Tests;
using Xunit;

namespace DjvuNet.Shared.Tests
{
    public class ImageBinaryDiffTests
    {
        [Fact]
        public unsafe void ImageBinaryDiff_RawPointers_WithPadding_CalculatesCorrectly()
        {
            // We want > 128 bytes per row to hit the AVX2 loop (Avx2.LoadDquVector256)
            // 24bpp = 3 bytes per pixel.
            // Width 130 * 3 = 390 bytes per row.
            // We add 2 bytes of padding, so Stride = 392 bytes.
            int width = 130;
            int height = 3;
            int pixelSize = 24; // 24 bpp (3 bytes/pixel)
            int stride = 392;
            int totalBytes = height * stride;

            byte[] buffer1 = new byte[totalBytes];
            byte[] buffer2 = new byte[totalBytes];

            // Initialize buffers with identical data
            for (int i = 0; i < totalBytes; i++)
            {
                byte val = (byte)(i % 256);
                buffer1[i] = val;
                buffer2[i] = val;
            }

            // Introduce 1 intentional difference in the visible area of Row 1
            // Row 1 offset = 1 * stride = 392.
            int diffIndex1 = 392 + 50;
            buffer2[diffIndex1] = (byte)((buffer1[diffIndex1] + 10) % 256);

            // Introduce 1 intentional difference in the visible area of Row 2
            int diffIndex2 = (2 * stride) + 120;
            buffer2[diffIndex2] = (byte)((buffer1[diffIndex2] + 20) % 256);

            // Introduce a difference in the PADDING area of Row 0.
            // Visible bytes end at index 389. Indices 390 and 391 are padding.
            buffer2[390] = 255;

            // Calculate expected difference
            // ImageBinaryDiff calculates the average absolute difference per channel per pixel across the whole image.
            // Total visible pixels = 130 * 3 = 390
            // Total channels = 390 * 3 = 1170.
            // Max channel value = 255.
            // Diff 1 = 10. Diff 2 = 20.
            // Expected result = (10 + 20) / (width * height * (pixelSize/channelSize) * 255)
            double expectedDiff = (10.0 + 20.0) / (130 * 3 * 3 * 255.0);

            fixed (byte* ptr1 = buffer1)
            fixed (byte* ptr2 = buffer2)
            {
                // The method should ignore the difference in the padding byte (index 390)
                double actualDiff = Util.ImageBinaryDiff(ptr1, ptr2, width, height, stride, pixelSize);

                // AVX2 uses floats internally for some calculations before casting to double,
                // so we assert with a small precision tolerance.
                Assert.Equal(expectedDiff, actualDiff, 5);
            }
        }

        [Fact]
        public void ImageBinaryDiff_BitmapData_WithPadding_CalculatesCorrectly()
        {
            int width = 130;
            int height = 3;

            // Create two identical bitmaps
            using (Bitmap bmp1 = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            using (Bitmap bmp2 = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            {
                // Bitmaps automatically pad strides to 4-byte boundaries.
                // 130 * 3 = 390 bytes. Padded to nearest multiple of 4 = 392 bytes.

                // Introduce differences
                bmp1.SetPixel(10, 1, Color.FromArgb(100, 100, 100));
                bmp2.SetPixel(10, 1, Color.FromArgb(110, 100, 100)); // R diff = 10

                bmp1.SetPixel(40, 2, Color.FromArgb(50, 50, 50));
                bmp2.SetPixel(40, 2, Color.FromArgb(50, 70, 50)); // G diff = 20

                double expectedDiff = (10.0 + 20.0) / (width * height * 3 * 255.0);

                Rectangle rect = new Rectangle(0, 0, width, height);
                BitmapData data1 = null;
                BitmapData data2 = null;

                try
                {
                    data1 = bmp1.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    data2 = bmp2.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                    double actualDiff = Util.ImageBinaryDiff(data1, data2);
                    Assert.Equal(expectedDiff, actualDiff, 5);
                }
                finally
                {
                    if (data1 != null) bmp1.UnlockBits(data1);
                    if (data2 != null) bmp2.UnlockBits(data2);
                }
            }
        }

        [Fact]
        public unsafe void ImageBinaryDiffCore_ReproduceAvx2PointerDesynchronization()
        {
            // A width that triggers widthBytesAvx2L = 128, oneVectorRounds = 0, widthBytesAvx2SRem = 2
            int width = 130;
            int height = 3; // Ensure multiple rows are processed

            // Add synthetic padding to simulate GDI+ alignment or similar strides
            int stride = width + 2;
            int totalBytes = stride * height;

            byte[] img1 = new byte[totalBytes];
            byte[] img2 = new byte[totalBytes];

            // Initialize arrays to be completely identical
            for (int i = 0; i < totalBytes; i++)
            {
                img1[i] = 100;
                img2[i] = 100;
            }

            // Poison ONLY the tail bytes (indexes 128 and 129) of the SECOND row.
            // Row 2 starts at index `stride * 1`.
            int row2TailIndex1 = (stride * 1) + 128;
            int row2TailIndex2 = (stride * 1) + 129;

            img1[row2TailIndex1] = 200; img1[row2TailIndex2] = 200;
            img2[row2TailIndex1] = 50;  img2[row2TailIndex2] = 50;  // Different!

            fixed (byte* ptr1 = img1)
            fixed (byte* ptr2 = img2)
            {
                // channelSize = 1, pixelSize = 8 (1 byte per pixel)
                double diff = Util.ImageBinaryDiffCore(ptr1, ptr2, (uint)width, (uint)height, stride, 8, 8);

                Console.WriteLine($"\nTested AVX2 Diff: {diff}");
                for (int y = 0; y < height; y++)
                {
                    int rowStart = y * stride;
                    int chunkStart = rowStart + width - 16;
                    if (chunkStart < rowStart) chunkStart = rowStart;

                    Console.WriteLine($"\n--- Hex Dump Row {y}: Last 16 bytes + Padding ---");

                    Console.Write("Img1: ");
                    for (int i = chunkStart; i < rowStart + stride; i++)
                    {
                        if (i == rowStart + width) Console.Write("| Padding: ");
                        Console.Write($"{img1[i]:X2} ");
                    }
                    Console.WriteLine();

                    Console.Write("Img2: ");
                    for (int i = chunkStart; i < rowStart + stride; i++)
                    {
                        if (i == rowStart + width) Console.Write("| Padding: ");
                        Console.Write($"{img2[i]:X2} ");
                    }
                    Console.WriteLine();
                }

                // If the bug exists, diff will be 0.0 because it skipped the tail bytes on every row.
                // We assert it should NOT be 0, proving the bug exists and forced a failure.
                Assert.True(diff > 0.0, "AVX2 pointer desynchronization caused tail bytes to be skipped during binary diff comparison providing false positive similarity.");
            }
        }

        [Theory]
        [InlineData(16)] // Exact vector boundary (Vector128)
        [InlineData(17)] // 1 byte remainder (SIMD processes 0-15, overlapping shift would re-process byte 1)
        [InlineData(31)] // Just under 2 vectors
        [InlineData(32)] // Exactly 2 vectors (AVX2 exact boundary)
        [InlineData(33)] // AVX2 + 1 byte remainder (AVX2 processes 0-31, overlapping shift would re-process byte 1)
        public unsafe void ImageBinaryDiff_DetectsTailShiftDoubleCounting(int width)
        {
            int height = 1;
            int stride = width + 5;
            int totalBytes = stride * height;

            byte[] img1 = new byte[totalBytes];
            byte[] img2 = new byte[totalBytes];

            for (int i = 0; i < totalBytes; i++)
            {
                img1[i] = 100;
                img2[i] = 100;
            }

            // Inject the difference strictly WITHIN the region that would be overlapped
            // if a tail shift occurred without masking.
            int vectorSize = (width >= 32 && Avx2.IsSupported) ? 32 : 16;
            int tailShift = (vectorSize - (width % vectorSize)) % vectorSize;

            // If tailShift is 0, just test the last byte. Otherwise test the byte right before the new bytes.
            int overlapIndex = tailShift == 0 ? width - 1 : width - tailShift - 1;

            img1[overlapIndex] = 150;
            img2[overlapIndex] = 100;

            double expectedRawDiff = 50.0;

            fixed (byte* ptr1 = img1)
            fixed (byte* ptr2 = img2)
            {
                double maxChannelValue = 255.0;
                double expectedFinalDiff = expectedRawDiff / (width * height * maxChannelValue);

                double actualDiff = Util.ImageBinaryDiffCore(ptr1, ptr2, (uint)width, (uint)height, stride, 8, 8);

                Assert.Equal(expectedFinalDiff, actualDiff);
            }
        }

        [Fact]
        public unsafe void ImageBinaryDiffScalar_ReproducePaddingOverreadBug()
        {
            // Test specifically targets the 1-byte planar format bug (channelSize = 1, pixelSize = 8)
            int width = 10;
            int height = 3;
            int stride = width; // Contiguous visible data
            int totalPixels = height * stride;
            int pixelSizeInBits = 8; // 1 byte per pixel

            // Allocate extra space to hold "padding" bytes at the very end
            int allocationSize = totalPixels + 2;

            byte[] img1 = new byte[allocationSize];
            byte[] img2 = new byte[allocationSize];

            // Initialize visible pixels to be completely identical
            for (int i = 0; i < totalPixels; i++)
            {
                img1[i] = 100;
                img2[i] = 100;
            }

            // Poison the memory AFTER the visible pixels (simulating uninitialized padding)
            img1[totalPixels] = 255;
            img1[totalPixels + 1] = 128;

            img2[totalPixels] = 128;
            img2[totalPixels + 1] = 255; // Different!

            fixed (byte* p1 = img1)
            fixed (byte* p2 = img2)
            {
                // widthBytes = 10, pixelSizeInBits = 8 (1 byte per pixel)
                double diff = Util.ImageBinaryDiffScalar(p1, p2, (uint)width, (uint)height, stride, pixelSizeInBits);

                Console.WriteLine($"\nTested Scalar 8-bpp Diff: {diff}");
                Console.WriteLine($"\n--- Hex Dump: Last 16 bytes + Poisoned Padding ---");

                Console.Write("Img1: ");
                for (int i = totalPixels - 16; i < allocationSize; i++)
                {
                    if (i == totalPixels) Console.Write("| Padding: ");
                    Console.Write($"{img1[i]:X2} ");
                }
                Console.WriteLine();

                Console.Write("Img2: ");
                for (int i = totalPixels - 16; i < allocationSize; i++)
                {
                    if (i == totalPixels) Console.Write("| Padding: ");
                    Console.Write($"{img2[i]:X2} ");
                }
                Console.WriteLine();

                // If the bug exists (hardcoded 3-byte read), it will read the poisoned memory
                // when processing the final pixel, resulting in a difference > 0.
                Assert.True(diff == 0.0, $"Expected zero difference between identical buffers, but got {diff}. "
                    + " This indicates the scalar fallback logic overread into the padding bytes.");
            }
        }

        [Fact]
        public unsafe void ImageDiffVector256_MatchesScalar_RealData()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");

            using (var bmp = new Bitmap(Path.Combine(Util.ArtifactsPath, "TitanIR-24bgr.png")))
            {
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    byte* scan0 = (byte*)data.Scan0.ToPointer();
                    int stride = data.Stride;
                    uint width = (uint)bmp.Width;
                    uint height = (uint)bmp.Height;

                    // Duplicate the image to memory to diff against itself
                    byte[] copy = new byte[stride * height];
                    Marshal.Copy(data.Scan0, copy, 0, copy.Length);
                    fixed (byte* copyPtr = copy)
                    {
                        double scalarDiff = Util.ImageBinaryDiffScalar(scan0, copyPtr, width, height, stride, 24);
                        double vectorDiff = Util.ImageDiffVector256(scan0, copyPtr, width, height, stride);

                        Assert.Equal(scalarDiff, vectorDiff, 10);
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        [Fact]
        public unsafe void ImageDiffVector128_MatchesScalar_RealData()
        {
            if (!Vector128.IsHardwareAccelerated)
                Assert.Skip("Vector128 hardware acceleration is not supported on this CPU.");

            using (var bmp = new Bitmap(Path.Combine(Util.ArtifactsPath, "TitanIR-24bgr.png")))
            {
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    byte* scan0 = (byte*)data.Scan0.ToPointer();
                    int stride = data.Stride;
                    uint width = (uint)bmp.Width;
                    uint height = (uint)bmp.Height;

                    byte[] copy = new byte[stride * height];
                    Marshal.Copy(data.Scan0, copy, 0, copy.Length);
                    fixed (byte* copyPtr = copy)
                    {
                        double scalarDiff = Util.ImageBinaryDiffScalar(scan0, copyPtr, width, height, stride, 24);
                        double vectorDiff = Util.ImageDiffVector128(scan0, copyPtr, width, height, stride);

                        Assert.Equal(scalarDiff, vectorDiff, 10);
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        [Fact]
        public unsafe void ImageDiffParallel256_MatchesScalar_RealData_Padded()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");

            using (var bmp = new Bitmap(Path.Combine(Util.ArtifactsPath, "TitanIR-24bgr-padded.png")))
            {
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    byte* scan0 = (byte*)data.Scan0.ToPointer();
                    int stride = data.Stride;
                    uint widthBytes = (uint)(bmp.Width * 3);
                    uint width = (uint)bmp.Width;
                    uint height = (uint)bmp.Height;

                    // Duplicate the image to memory to poison the padding
                    byte[] copy = new byte[stride * height];
                    Marshal.Copy(data.Scan0, copy, 0, copy.Length);

                    int paddingBytes = stride - (int)widthBytes;
                    if (paddingBytes > 0)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            int rowOffset = y * stride;
                            for (int p = 0; p < paddingBytes; p++)
                            {
                                copy[rowOffset + widthBytes + p] = (byte)((y * 13 + p * 17) & 0xFF); // Poison padding with unique pattern
                            }
                        }
                    }

                    fixed (byte* copyPtr = copy)
                    {
                        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                        double scalarDiff = Util.ImageBinaryDiffScalar(scan0, copyPtr, width, height, stride, 24);
                        double parallelDiff = Util.ImageDiffParallel256(scan0, copyPtr, width, height, stride, options);

                        Assert.Equal(0.0, scalarDiff);
                        Assert.Equal(0.0, parallelDiff);
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        [Fact]
        public unsafe void ImageDiffParallel128_MatchesScalar_RealData_Padded()
        {
            if (!Vector128.IsHardwareAccelerated)
                Assert.Skip("Vector128 hardware acceleration is not supported on this CPU.");

            using (var bmp = new Bitmap(Path.Combine(Util.ArtifactsPath, "TitanIR-24bgr-padded.png")))
            {
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    byte* scan0 = (byte*)data.Scan0.ToPointer();
                    int stride = data.Stride;
                    uint widthBytes = (uint)(bmp.Width * 3);
                    uint width = (uint)bmp.Width;
                    uint height = (uint)bmp.Height;

                    // Duplicate the image to memory to poison the padding
                    byte[] copy = new byte[stride * height];
                    Marshal.Copy(data.Scan0, copy, 0, copy.Length);

                    int paddingBytes = stride - (int)widthBytes;
                    if (paddingBytes > 0)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            int rowOffset = y * stride;
                            for (int p = 0; p < paddingBytes; p++)
                            {
                                copy[rowOffset + widthBytes + p] = (byte)((y * 13 + p * 17) & 0xFF); // Poison padding with unique pattern
                            }
                        }
                    }

                    fixed (byte* copyPtr = copy)
                    {
                        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                        double scalarDiff = Util.ImageBinaryDiffScalar(scan0, copyPtr, width, height, stride, 24);
                        double parallelDiff = Util.ImageDiffParallel128(scan0, copyPtr, width, height, stride, options);

                        Assert.Equal(0.0, scalarDiff);
                        Assert.Equal(0.0, parallelDiff);
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        private unsafe (byte[] img1, byte[] img2) CreateExactRatioBuffers()
        {
            // width=12 (36 widthBytes), height=2, stride=36. Total=72 bytes.
            byte[] img1 = new byte[72];
            byte[] img2 = new byte[72];
            for (int i = 0; i < 36; i++) img2[i] = 127;
            for (int i = 36; i < 72; i++) img2[i] = 128;
            return (img1, img2);
        }

        [Fact]
        public unsafe void ImageBinaryDiffScalar_KnownRatio_CalculatesExactFraction()
        {
            var buffers = CreateExactRatioBuffers();
            fixed (byte* p1 = buffers.img1)
            fixed (byte* p2 = buffers.img2)
            {
                Assert.Equal(0.5, Util.ImageBinaryDiffScalar(p1, p2, 12, 2, 36, 24));
            }
        }

        [Fact]
        public unsafe void ImageDiffVector256_KnownRatio_CalculatesExactFraction()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            var buffers = CreateExactRatioBuffers();
            fixed (byte* p1 = buffers.img1)
            fixed (byte* p2 = buffers.img2)
            {
                Assert.Equal(0.5, Util.ImageDiffVector256(p1, p2, 12, 2, 36));
            }
        }

        [Fact]
        public unsafe void ImageDiffVector128_KnownRatio_CalculatesExactFraction()
        {
            if (!Vector128.IsHardwareAccelerated) Assert.Skip("Vector128 hardware acceleration is not supported on this CPU.");
            var buffers = CreateExactRatioBuffers();
            fixed (byte* p1 = buffers.img1)
            fixed (byte* p2 = buffers.img2)
            {
                Assert.Equal(0.5, Util.ImageDiffVector128(p1, p2, 12, 2, 36));
            }
        }

        [Fact]
        public unsafe void ImageDiffParallel256_KnownRatio_CalculatesExactFraction()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");
            var buffers = CreateExactRatioBuffers();
            fixed (byte* p1 = buffers.img1)
            fixed (byte* p2 = buffers.img2)
            {
                var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                Assert.Equal(0.5, Util.ImageDiffParallel256(p1, p2, 12, 2, 36, options));
            }
        }

        [Fact]
        public unsafe void ImageDiffParallel128_KnownRatio_CalculatesExactFraction()
        {
            if (!Vector128.IsHardwareAccelerated) Assert.Skip("Vector128 hardware acceleration is not supported on this CPU.");
            var buffers = CreateExactRatioBuffers();
            fixed (byte* p1 = buffers.img1)
            fixed (byte* p2 = buffers.img2)
            {
                var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                Assert.Equal(0.5, Util.ImageDiffParallel128(p1, p2, 12, 2, 36, options));
            }
        }
        [Fact]
        public unsafe void ImageBinaryDiffCore_KnownRatio_CalculatesExactFraction()
        {
            var buffers = CreateExactRatioBuffers();
            fixed (byte* p1 = buffers.img1)
            fixed (byte* p2 = buffers.img2)
            {
                Assert.Equal(0.5, Util.ImageBinaryDiffCore(p1, p2, 12, 2, 36, 24, 8));
            }
        }

        [Theory]
        [InlineData("Vector256", 1)]
        [InlineData("Vector256", 2)]
        [InlineData("Vector128", 1)]
        [InlineData("Vector128", 2)]
        [InlineData("Parallel256", 1)]
        [InlineData("Parallel256", 2)]
        [InlineData("Parallel128", 1)]
        [InlineData("Parallel128", 2)]
        [InlineData("Core", 1)]
        [InlineData("Core", 2)]
        [InlineData("Scalar", 1)]
        [InlineData("Scalar", 2)]
        public unsafe void ImageBinaryDiff_InputValidation_NullPointers_ThrowsDjvuArgumentNullException(string method, int nullIndex)
        {
            byte[] validBuffer = new byte[100];
            fixed (byte* pValid = validBuffer)
            {
                byte* p1 = nullIndex == 1 ? null : pValid;
                byte* p2 = nullIndex == 2 ? null : pValid;
                var options = new ParallelOptions();

                DjvuNet.Errors.DjvuArgumentNullException ex = null;

                if (method == "Vector256")
                {
                    if (!Avx2.IsSupported) Assert.Skip("AVX2 not supported on this CPU.");
                    ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentNullException>(() => Util.ImageDiffVector256(p1, p2, 12, 2, 36));
                }
                else if (method == "Vector128")
                {
                    if (!Vector128.IsHardwareAccelerated) Assert.Skip("Vector128 not supported on this CPU.");
                    ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentNullException>(() => Util.ImageDiffVector128(p1, p2, 12, 2, 36));
                }
                else if (method == "Parallel256")
                {
                    if (!Avx2.IsSupported) Assert.Skip("AVX2 not supported on this CPU.");
                    ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentNullException>(() => Util.ImageDiffParallel256(p1, p2, 12, 2, 36, options));
                }
                else if (method == "Parallel128")
                {
                    if (!Vector128.IsHardwareAccelerated) Assert.Skip("Vector128 not supported on this CPU.");
                    ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentNullException>(() => Util.ImageDiffParallel128(p1, p2, 12, 2, 36, options));
                }
                else if (method == "Core")
                {
                    ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentNullException>(() => Util.ImageBinaryDiffCore(p1, p2, 12, 2, 36, 24, 8));
                }
                else if (method == "Scalar")
                {
                    ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentNullException>(() => Util.ImageBinaryDiffScalar(p1, p2, 12, 2, 36));
                }

                string expectedParam = nullIndex == 1 ? "scan0_1" : "scan0_2";
                Assert.Equal(expectedParam, ex.ParamName);
                Assert.Contains("cannot be null", ex.Message);
            }
        }

        [Theory]
        [InlineData("Vector256", 10)] // 30 bytes < 32 bytes required for AVX2
        [InlineData("Parallel256", 10)]
        [InlineData("Vector128", 5)]  // 15 bytes < 16 bytes required for Vector128
        [InlineData("Parallel128", 5)]
        public unsafe void ImageBinaryDiff_InputValidation_InvalidWidth_ThrowsDjvuArgumentOutOfRangeException(string method, int width)
        {
            IntPtr pValidPtr = IntPtr.Zero;
            try
            {
                pValidPtr = Marshal.AllocHGlobal(100);
                byte* pValid = (byte*)pValidPtr;

                var options = new ParallelOptions();
                DjvuNet.Errors.DjvuArgumentOutOfRangeException ex = null;
                ulong expectedWidthBytes = (ulong)width * 3; // default pixelSize=24 (3 bytes)

                if (method == "Vector256")
                {
                    if (!Avx2.IsSupported) Assert.Skip("AVX2 not supported on this CPU.");
                    ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => Util.ImageDiffVector256(pValid, pValid, (uint)width, 2, 36));
                }
                else if (method == "Parallel256")
                {
                    if (!Avx2.IsSupported) Assert.Skip("AVX2 not supported on this CPU.");
                    ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => Util.ImageDiffParallel256(pValid, pValid, (uint)width, 2, 36, options));
                }
                else if (method == "Vector128")
                {
                    if (!Vector128.IsHardwareAccelerated) Assert.Skip("Vector128 not supported on this CPU.");
                    ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => Util.ImageDiffVector128(pValid, pValid, (uint)width, 2, 36));
                }
                else if (method == "Parallel128")
                {
                    if (!Vector128.IsHardwareAccelerated) Assert.Skip("Vector128 not supported on this CPU.");
                    ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => Util.ImageDiffParallel128(pValid, pValid, (uint)width, 2, 36, options));
                }

                Assert.Equal("width", ex.ParamName);
                Assert.Equal(expectedWidthBytes, (ulong)ex.ActualValue);
                Assert.Contains("Buffer width must be at least", ex.Message);
            }
            finally
            {
                if (pValidPtr != IntPtr.Zero) Marshal.FreeHGlobal(pValidPtr);
            }
        }

        [Theory]
        [InlineData(6, 10, false)]   // 18 bytes: Exact Vector128 + 2
        [InlineData(10, 10, false)]  // 30 bytes: Vector128 * 1.8
        [InlineData(11, 10, false)]  // 33 bytes: > 2x Vector128
        [InlineData(15, 10, true)]   // 45 bytes: padded
        public unsafe void ImageDiffVector128_BoundaryEdgeCases_MatchesScalar(int width, int height, bool isPadded)
        {
            if (!Vector128.IsHardwareAccelerated) Assert.Skip("Vector128 not supported on this CPU.");

            int pixelSize = 24;
            int channelSize = 8;
            uint widthBytes = (uint)width * (uint)(pixelSize / 8);
            int stride = isPadded ? (int)((widthBytes + 3) & ~3) : (int)widthBytes;
            int totalBytes = stride * height;

            byte[] buffer1 = new byte[totalBytes];
            byte[] buffer2 = new byte[totalBytes];

            for (uint i = 0; i < totalBytes; i++)
            {
                buffer1[i] = (byte)(i % 251);
                buffer2[i] = (i % 7 == 0) ? (byte)((buffer1[i] + 5) % 255) : buffer1[i];
            }

            fixed (byte* p1 = buffer1)
            fixed (byte* p2 = buffer2)
            {
                double expectedScalar = Util.ImageBinaryDiffScalar(p1, p2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                double actualV128 = Util.ImageDiffVector128(p1, p2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                Assert.Equal(expectedScalar, actualV128, 7);
            }
        }

        [Theory]
        [InlineData(11, 10, false)]  // 33 bytes: Vector256 + 1 byte
        [InlineData(16, 10, false)]  // 48 bytes: Vector256 * 1.5
        [InlineData(21, 10, false)]  // 63 bytes: Pre-2x Vector256
        [InlineData(22, 10, false)]  // 66 bytes: 2x Vector256 + 2 bytes
        [InlineData(23, 10, true)]   // 69 bytes: Odd size, padded
        public unsafe void ImageDiffVector256_BoundaryEdgeCases_MatchesScalar(int width, int height, bool isPadded)
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 not supported on this CPU.");

            int pixelSize = 24;
            int channelSize = 8;
            uint widthBytes = (uint)width * (uint)(pixelSize / 8);
            int stride = isPadded ? (int)((widthBytes + 3) & ~3) : (int)widthBytes;
            int totalBytes = stride * height;

            byte[] buffer1 = new byte[totalBytes];
            byte[] buffer2 = new byte[totalBytes];

            for (uint i = 0; i < totalBytes; i++)
            {
                buffer1[i] = (byte)(i % 251);
                buffer2[i] = (i % 7 == 0) ? (byte)((buffer1[i] + 5) % 255) : buffer1[i];
            }

            fixed (byte* p1 = buffer1)
            fixed (byte* p2 = buffer2)
            {
                double expectedScalar = Util.ImageBinaryDiffScalar(p1, p2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                double actualV256 = Util.ImageDiffVector256(p1, p2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                Assert.Equal(expectedScalar, actualV256, 7);
            }
        }

        [Theory]
        [InlineData(5, 10, false)]     // 15 bytes: Sub-Vector128 -> scalar routing
        [InlineData(10, 10, false)]    // 30 bytes: Sub-Vector256 -> V128 routing
        [InlineData(342, 100, false)]  // ~102K bytes -> Single thread routing
        [InlineData(342, 100, true)]   // Padded single thread routing
        [InlineData(1024, 100, false)] // ~307K bytes -> Multi thread routing
        [InlineData(1024, 100, true)]  // Padded multi thread routing
        public unsafe void ImageBinaryDiffCore_RoutingEdgeCases_MatchesScalar(int width, int height, bool isPadded)
        {
            int pixelSize = 24;
            int channelSize = 8;
            uint widthBytes = (uint)width * (uint)(pixelSize / 8);
            int stride = isPadded ? (int)((widthBytes + 3) & ~3) : (int)widthBytes;
            int totalBytes = stride * height;

            byte[] buffer1 = new byte[totalBytes];
            byte[] buffer2 = new byte[totalBytes];

            for (uint i = 0; i < totalBytes; i++)
            {
                buffer1[i] = (byte)(i % 251);
                buffer2[i] = (i % 7 == 0) ? (byte)((buffer1[i] + 5) % 255) : buffer1[i];
            }

            fixed (byte* p1 = buffer1)
            fixed (byte* p2 = buffer2)
            {
                double expectedScalar = Util.ImageBinaryDiffScalar(p1, p2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                double actualCore = Util.ImageBinaryDiffCore(p1, p2, (uint)width, (uint)height, stride, pixelSize, channelSize);
                Assert.Equal(expectedScalar, actualCore, 7);
            }
        }
    }
}
