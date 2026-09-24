using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using DjvuNet.Errors;
using DjvuNet.Graphics;

namespace DjvuNet.Wavelet
{
    public static partial class InterWaveTransform
    {
        /// <summary>
        /// Convenience overload for contiguous input buffers. Calculates the input row stride in bytes as (width * sizeof(Pixel))
        /// and delegates to the primary byte-stride implementation.
        /// </summary>
        /// <param name="pPixBuff">Pointer to the input BGR pixel buffer.</param>
        /// <param name="width">The visible width of the image in pixels.</param>
        /// <param name="height">The visible height of the image in pixels.</param>
        /// <param name="pOutY">Pointer to the output Y (luminance) buffer.</param>
        /// <param name="pOutCb">Pointer to the output Cb (blue chrominance) buffer.</param>
        /// <param name="pOutCr">Pointer to the output Cr (red chrominance) buffer.</param>
        /// <param name="outRowSizeInBytes">The exact stride of the output planar buffers in bytes.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Rgb2YCbCr(Pixel* pPixBuff, int width, int height, sbyte* pOutY, sbyte* pOutCb, sbyte* pOutCr, int outRowSizeInBytes)
        {
            int inRowBytes = width * sizeof(Pixel);
            Rgb2YCbCr(pPixBuff, width, height, inRowBytes, pOutY, pOutCb, pOutCr, outRowSizeInBytes);
        }

        /// <summary>
        /// EXPLICIT ARCHITECTURE NOTE (SIMD Waterfall Pattern):
        /// Do not add 'else' blocks to these hardware checks. This is a deliberate "SIMD Waterfall".
        /// The waterfall only falls through if the initial width is smaller than the vector size.
        /// Otherwise, each tier processes the bulk of the data and handles its own unaligned tail 
        /// internally using an overlapping tail-shift strategy, returning the full width processed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void YCbCr2Rgb(short* pY, short* pCb, short* pCr, sbyte* pImg8, int width, int height, int rowSize, int blockWidth)
        {
            int processedWidth = 0;
            if (Avx512F.IsSupported && Avx512BW.IsSupported)
            {
                processedWidth = InterWaveSimd.YCbCr2RgbVector512(pY, pCb, pCr, pImg8, height, width, blockWidth, rowSize, 0);
            }
            
            if (Avx2.IsSupported && width - processedWidth >= 32)
            {
                processedWidth = InterWaveSimd.YCbCr2RgbVector256(pY, pCb, pCr, pImg8, height, width, blockWidth, rowSize, processedWidth);
            }
            
            if ((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported) && width - processedWidth >= 16)
            {
                processedWidth = InterWaveSimd.YCbCr2RgbVector128(pY, pCb, pCr, pImg8, height, width, blockWidth, rowSize, processedWidth);
            }

            if (processedWidth < width)
            {
                InterWaveTransform.YCbCr2RgbScalar(pY, pCb, pCr, pImg8, width, height, rowSize, blockWidth, processedWidth);
            }
        }

        /// <summary>
        /// See YCbCr2Rgb for documentation on the deliberate SIMD Waterfall routing pattern.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void YGray2Rgb(short* pY, sbyte* pImg8, int width, int height, int rowSize, int blockWidth)
        {
            int processedWidth = 0;
            if (Avx512F.IsSupported && Avx512BW.IsSupported)
            {
                processedWidth = InterWaveSimd.YGray2PixelMapVector512(pY, pImg8, height, width, blockWidth, rowSize, 0);
            }
            
            if (Avx2.IsSupported && width - processedWidth >= 32)
            {
                processedWidth = InterWaveSimd.YGray2PixelMapVector256(pY, pImg8, height, width, blockWidth, rowSize, processedWidth);
            }

            if ((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported) && width - processedWidth >= 16)
            {
                processedWidth = InterWaveSimd.YGray2PixelMapVector128(pY, pImg8, height, width, blockWidth, rowSize, processedWidth);
            }
            
            for (int y = 0, pidx = 0, ridx = 0; y < height; y++, pidx += blockWidth, ridx += rowSize)
            {
                int pixidx = ridx + (processedWidth * 3);
                for (int x = processedWidth; x < width; x++, pixidx += 3)
                {
                    int yVal = (pY[pidx + x] + 32) >> 6;
                    yVal = yVal < -128 ? -128 : (yVal > 127 ? 127 : yVal);
                    pImg8[pixidx] = pImg8[pixidx + 1] = pImg8[pixidx + 2] = (sbyte)(127 - yVal);
                }
            }
        }

        /// <summary>
        /// Calculates the optimal number of threads for parallel SIMD execution based on empirical throughput curves.
        /// Returns 1 to indicate the caller should bypass the TPL and fall back to a single-threaded scalar loop.
        /// </summary>
        /// <remarks>
        /// ARCHITECTURAL RATIONALE:
        /// This step-function matrix is derived from extensive benchmarking of the TPL (Task Parallel Library) overhead
        /// versus SIMD throughput. Below is the full empirical data demonstrating the exact threshold crossings
        /// where scaling threads becomes profitable, measured in Time per Operation (Real).
        ///
        /// | Image Size |   V128 (1T) |  V128 (2T) |  V128 (4T) |  V128 (6T) | V128 (12T) |   V256 (1T) |  V256 (2T) |  V256 (4T) |  V256 (6T) | V256 (12T) |
        /// |-----------:|------------:|-----------:|-----------:|-----------:|-----------:|------------:|-----------:|-----------:|-----------:|-----------:|
        /// |      1,024 |   *1.81 µs* |    2.59 µs |    4.12 µs |    4.80 µs |    6.16 µs |   *0.55 µs* |    1.82 µs |    2.85 µs |    3.49 µs |    4.60 µs |
        /// |      4,096 |   *7.15 µs* |    5.83 µs |    7.16 µs |    7.90 µs |    9.29 µs |   *2.13 µs* |    3.55 µs |    4.94 µs |    5.57 µs |    6.87 µs |
        /// |      9,216 |   16.00 µs  | *10.60 µs* | *10.60 µs* |   11.00 µs |   13.10 µs |   *4.88 µs* |    5.06 µs |    6.85 µs |    7.68 µs |    9.14 µs |
        /// |     16,384 |   28.50 µs  |   19.00 µs | *13.50 µs* |   13.80 µs |   16.60 µs |    8.59 µs  |  *7.05 µs* |    8.67 µs |    9.45 µs |   11.90 µs |
        /// |     36,864 |   63.50 µs  |   38.10 µs |   24.80 µs | *23.40 µs* |   25.20 µs |   18.90 µs  |   14.20 µs | *12.00 µs* |   13.10 µs |   16.20 µs |
        /// |     65,536 |  113.20 µs  |   61.70 µs |   37.00 µs | *33.00 µs* |   35.60 µs |   34.30 µs  |   23.10 µs | *16.80 µs* |   18.80 µs |   22.60 µs |
        /// |    262,144 |  455.00 µs  |  237.60 µs |  127.70 µs | *92.60 µs* |   98.30 µs |  139.70 µs  |   76.50 µs | *53.00 µs* |   54.30 µs |   59.90 µs |
        /// |  1,048,576 | 1813.00 µs  |  935.10 µs |  491.60 µs |  345.80 µs |*323.40 µs* |  567.40 µs  |  300.40 µs |*203.80 µs* |  209.40 µs |  223.50 µs |
        /// |  2,096,704 | 3645.00 µs  | 1860.00 µs |  964.40 µs |  673.10 µs |*601.00 µs* | 1118.00 µs  |  592.80 µs |  429.00 µs |*426.70 µs* |  450.40 µs |
        /// |  4,194,304 | 7262.00 µs  | 3752.00 µs | 1919.00 µs | 1316.00 µs |*1183.00 µs*| 2249.00 µs  | 1319.00 µs |  985.20 µs |*968.40 µs* | 1027.00 µs |
        /// | 20,081,328 |34860.00 µs  |17694.00 µs | 9124.00 µs | 6387.00 µs |*6151.00 µs*|10588.00 µs  | 6128.00 µs |*5888.00 µs*| 6081.00 µs | 6158.00 µs |
        ///
        /// 1. Vector256 (AVX2): The pipeline executes extremely fast, meaning TPL synchronization overhead dominates on
        ///    smaller payloads. AVX2 requires roughly 12,000 pixels before 2 threads beat a single thread, and ~24,000
        ///    pixels before 4 threads become optimal. Furthermore, benchmark data (e.g., processing a 20 Megapixel image)
        ///    shows absolute memory bus saturation at 4 to 6 threads. Throwing 12 threads at AVX2 actively harms
        ///    performance due to cache thrashing. Therefore, AVX2 parallelism is strictly capped at 6.
        ///
        /// 2. Vector128 (SSSE3/AdvSimd): This pipeline executes slower, meaning the TPL overhead is amortized much earlier
        ///    (breaking even at ~2,000 pixels for 2 threads). Because the data ingestion rate per core is lower, the
        ///    memory bus can sustain more active threads before saturating, allowing positive scaling up to 12 threads
        ///    on large images (1M+ pixels).
        ///
        /// MEMORY TOPOLOGY DISCLAIMER:
        /// The saturation caps (6 for AVX2, 12 for Vector128) are calibrated against standard dual-channel (2-channel)
        /// memory configurations typical of consumer hardware. We did not benchmark quad-channel or octa-channel
        /// memory configurations, as DjvuNet is not expected to be primarily utilized on enterprise server-grade
        /// or workstation hardware (e.g., EPYC, Threadripper, Xeon). This missing optimization for high-bandwidth
        /// server topologies can be addressed in future iterations if required.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetThreadCountForRgb2YCbCr(long pixelCount)
        {
            int targetThreads = 1;
            int maxAvailableThreads = Environment.ProcessorCount;

            if (Avx2.IsSupported)
            {
                // AVX2 Optimization Matrix
                // Saturates memory bus quickly; capped at 6. TPL overhead is heavy due to very fast execution time.
                if (pixelCount >= 2_000_000)
                    targetThreads = 6;
                else if (pixelCount >= 24_000)
                    targetThreads = 4;
                else if (pixelCount >= 12_000)
                    targetThreads = 2;
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                // Vector128 (SSSE3/AdvSimd) Optimization Matrix
                // Processes slower; requires less payload per thread to overcome TPL overhead,
                // and memory bus saturates later (capped at 12).
                if (pixelCount >= 1_000_000)
                    targetThreads = 12;
                else if (pixelCount >= 24_000)
                    targetThreads = 6;
                else if (pixelCount >= 9_216)
                    targetThreads = 4;
                else if (pixelCount >= 2_000)
                    targetThreads = 2;
            }

            return Math.Min(targetThreads, maxAvailableThreads);
        }

        /// <summary>
        /// Calculates the optimal number of threads for parallel SIMD execution of the YCbCr2Rgb inverse transform.
        /// Returns 1 to indicate the caller should bypass the TPL and fall back to a single-threaded loop.
        /// </summary>
        /// <remarks>
        /// ARCHITECTURAL RATIONALE:
        /// This step-function matrix is derived from extensive benchmarking of the TPL (Task Parallel Library) overhead
        /// versus SIMD throughput. Below is the full empirical data demonstrating the exact threshold crossings
        /// where scaling threads becomes profitable, measured in Time per Operation (Real).
        ///
        /// | Image Size |   V128 (1T) |  V128 (2T) |  V128 (4T) |  V128 (6T) | V128 (12T) |   V256 (1T) |  V256 (2T) |  V256 (4T) |  V256 (6T) | V256 (12T) |
        /// |-----------:|------------:|-----------:|-----------:|-----------:|-----------:|------------:|-----------:|-----------:|-----------:|-----------:|
        /// |      1,024 |   *0.52 µs* |    1.74 µs |    2.74 µs |    3.20 µs |    4.26 µs |   *0.55 µs* |    1.78 µs |    2.71 µs |    3.10 µs |    4.05 µs |
        /// |      4,096 |   *2.02 µs* |    3.09 µs |    4.29 µs |    5.09 µs |    6.09 µs |   *2.20 µs* |    3.43 µs |    4.30 µs |    5.10 µs |    6.08 µs |
        /// |      9,216 |    4.63 µs  |  *4.45 µs* |    5.79 µs |    6.77 µs |    8.31 µs |    4.91 µs  |  *4.79 µs* |    5.73 µs |    6.68 µs |    8.15 µs |
        /// |     16,384 |    8.08 µs  |  *6.59 µs* |    6.97 µs |    8.04 µs |   10.40 µs |    8.72 µs  |  *6.77 µs* |    7.01 µs |    8.14 µs |   10.29 µs |
        /// |     36,864 |   22.64 µs  |   13.12 µs | *10.77 µs* |   11.54 µs |   13.89 µs |   19.60 µs  |   12.84 µs | *10.76 µs* |   11.52 µs |   13.58 µs |
        /// |     65,536 |   40.02 µs  |   21.24 µs | *15.98 µs* |   16.36 µs |   19.74 µs |   34.78 µs  |   22.66 µs | *16.11 µs* |   16.53 µs |   19.48 µs |
        /// |    262,144 |  132.08 µs  |   74.21 µs | *53.06 µs* |   53.90 µs |   57.14 µs |  139.65 µs  |   77.32 µs | *53.45 µs* |   53.49 µs |   56.53 µs |
        /// |  1,048,576 |  528.43 µs  |  276.37 µs |*204.07 µs* |  206.81 µs |  214.44 µs |  558.86 µs  |  294.91 µs |*203.82 µs* |  205.09 µs |  215.21 µs |
        /// |  2,096,704 | 1035.50 µs  |  536.39 µs |*401.37 µs* |  406.31 µs |  422.04 µs | 1114.50 µs  |  587.87 µs |*401.12 µs* |  410.04 µs |  420.35 µs |
        /// |  4,194,304 | 2097.30 µs  | 1069.40 µs |*792.48 µs* |  797.62 µs |  823.14 µs | 2224.20 µs  | 1165.80 µs |*804.03 µs* |  798.43 µs |  816.77 µs |
        /// | 20,081,328 | 9865.70 µs  | 5034.10 µs |*3805.40 µs*| 3853.00 µs | 3925.70 µs | 10564.80 µs | 5494.20 µs |*3801.60 µs*| 3845.40 µs | 3931.90 µs |
        ///
        /// 1. Vector256 (AVX2): The inverse transform (planar to interleaved) applies immense pressure on the memory bus,
        ///    saturating much earlier than the forward transform. TPL overhead is amortized at roughly 9,000 pixels.
        ///    Due to the high cost of writing interleaved BGR data to memory, the bus completely saturates at 4 threads.
        ///    Pushing to 6 or 12 threads causes L3 cache thrashing and active performance regressions.
        ///
        /// 2. Vector128 (SSSE3/AdvSimd): The write-bound nature of the inverse transform flattens the advantage of 
        ///    slower data ingestion seen in the forward transform. It shares the exact same memory saturation cap (4) 
        ///    and TPL crossover (~9,000) as AVX2.
        ///
        /// MEMORY TOPOLOGY DISCLAIMER:
        /// The saturation caps (4 for both AVX2 and Vector128) are calibrated against standard dual-channel (2-channel)
        /// memory configurations typical of consumer hardware. We did not benchmark quad-channel or octa-channel
        /// memory configurations, as DjvuNet is not expected to be primarily utilized on enterprise server-grade
        /// or workstation hardware (e.g., EPYC, Threadripper, Xeon). This missing optimization for high-bandwidth
        /// server topologies can be addressed in future iterations if required.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetThreadCountForYCbCr2Rgb(long pixelCount)
        {
            int targetThreads = 1;
            int maxAvailableThreads = Environment.ProcessorCount;

            if (Avx2.IsSupported)
            {
                // AVX2 Optimization Matrix (Inverse Transform)
                // Writing interleaved pixels violently saturates the memory bus; strictly capped at 4.
                // TPL overhead is amortized at ~9,000 pixels.
                if (pixelCount >= 36_000)
                    targetThreads = 4;
                else if (pixelCount >= 9_000)
                    targetThreads = 2;
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                // Vector128 (SSSE3/AdvSimd) Optimization Matrix (Inverse Transform)
                // The write-bound nature of the inverse transform flattens the advantage of slower data ingestion.
                // It shares the exact same memory saturation cap (4) and TPL crossover (~9,000) as AVX2.
                if (pixelCount >= 36_000)
                    targetThreads = 4;
                else if (pixelCount >= 9_000)
                    targetThreads = 2;
            }

            return Math.Min(targetThreads, maxAvailableThreads);
        }

        /// <summary>
        /// Core hardware-accelerated Color Space Conversion from BGR to YCbCr with explicit byte-stride support.
        /// Dynamically routes execution to optimal SIMD implementations (AVX2, SSSE3/AdvSimd) or falls back to
        /// scalar processing based on hardware support and image dimensions.
        /// </summary>
        /// <param name="pPixBuff">Pointer to the input BGR pixel buffer.</param>
        /// <param name="width">The visible width of the image in pixels.</param>
        /// <param name="height">The visible height of the image in pixels.</param>
        /// <param name="rowSizeInBytes">The exact stride of the input BGR buffer in bytes, including any padding.</param>
        /// <param name="pOutY">Pointer to the output Y (luminance) buffer.</param>
        /// <param name="pOutCb">Pointer to the output Cb (blue chrominance) buffer.</param>
        /// <param name="pOutCr">Pointer to the output Cr (red chrominance) buffer.</param>
        /// <param name="outRowSizeInBytes">The exact stride of the output planar buffers in bytes, including any padding.</param>
        /// <exception cref="DjvuArgumentNullException">Thrown if any of the pointer arguments are null.</exception>
        /// <exception cref="DjvuArgumentOutOfRangeException">Thrown if width or height is less than or equal to zero, or if stride dimensions are mathematically invalid for the given width.</exception>
        /// <exception cref="DjvuInvalidOperationException">Thrown if the input buffer overlaps with any of the output buffers.</exception>
        /// <remarks>
        /// ARCHITECTURAL NOTE: This implementation intentionally diverges from the native DjVuLibre C++ behavior.
        ///
        /// The native C++ library assumes the input stride is always a perfect multiple of the Pixel struct size (3 bytes),
        /// relying on simple GPixel pointer arithmetic. This creates a brittle constraint that fails if the input buffer
        /// utilizes arbitrary byte-aligned padding (e.g., 4-byte boundaries common in Windows GDI+ Bitmaps).
        ///
        /// To ensure DjvuNet is robust in all interop scenarios, this method explicitly defines the input
        /// stride (`rowSizeInBytes`) strictly in bytes. It relies on the out-of-place nature of the transform
        /// to utilize a highly optimized pointer-shift technique for processing row tails, ensuring memory safety
        /// against any arbitrary OS boundary without duplicating math logic.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe void Rgb2YCbCr(
            Pixel* pPixBuff,
            int width,
            int height,
            int rowSizeInBytes,
            sbyte* pOutY,
            sbyte* pOutCb,
            sbyte* pOutCr,
            int outRowSizeInBytes)
        {
            if (pPixBuff == null)
            {
                throw new DjvuArgumentNullException(nameof(pPixBuff));
            }
            if (pOutY == null)
            {
                throw new DjvuArgumentNullException(nameof(pOutY));
            }
            if (pOutCb == null)
            {
                throw new DjvuArgumentNullException(nameof(pOutCb));
            }
            if (pOutCr == null)
            {
                throw new DjvuArgumentNullException(nameof(pOutCr));
            }
            if (width <= 0)
            {
                throw new DjvuArgumentOutOfRangeException(
                    nameof(width),
                    width,
                    "Width must be greater than zero.");
            }
            if (height <= 0)
            {
                throw new DjvuArgumentOutOfRangeException(
                    nameof(height),
                    height,
                    "Height must be greater than zero.");
            }
            if (rowSizeInBytes < (long)width * sizeof(Pixel))
            {
                throw new DjvuArgumentOutOfRangeException(
                    nameof(rowSizeInBytes),
                    rowSizeInBytes,
                    $"Input row size ({rowSizeInBytes}) must be at least width * sizeof(Pixel) ({(long)width * sizeof(Pixel)}).");
            }
            if (outRowSizeInBytes < width)
            {
                throw new DjvuArgumentOutOfRangeException(
                    nameof(outRowSizeInBytes),
                    outRowSizeInBytes,
                    $"Output row size ({outRowSizeInBytes}) must be at least width ({width}).");
            }

            Pixel* pIn = pPixBuff;
            sbyte* pY = pOutY;
            sbyte* pCb = pOutCb;
            sbyte* pCr = pOutCr;

            // SAFETY CHECK: Ensure input buffer does not overlap with output buffers.
            // If they overlap, the pointer-shift tail optimization will cause read-after-write corruption.
            byte* startIn = (byte*)pPixBuff;
            byte* endIn = startIn + ((long)height * rowSizeInBytes);

            byte* startY = (byte*)pOutY;
            byte* endY = startY + ((long)height * outRowSizeInBytes);
            byte* startCb = (byte*)pOutCb;
            byte* endCb = startCb + ((long)height * outRowSizeInBytes);
            byte* startCr = (byte*)pOutCr;
            byte* endCr = startCr + ((long)height * outRowSizeInBytes);
            if ((startIn < endY && endIn > startY) ||
                (startIn < endCb && endIn > startCb) ||
                (startIn < endCr && endIn > startCr))
            {
                throw new DjvuInvalidOperationException("Source and destination buffers must be distinct and non-overlapping in Rgb2YCbCr out-of-place transform.");
            }

            if ((startY < endCb && endY > startCb) ||
                (startY < endCr && endY > startCr) ||
                (startCb < endCr && endCb > startCr))
            {
                throw new DjvuInvalidOperationException("Destination planar buffers (Y, Cb, Cr) must be distinct and not overlap.");
            }

            long pixelCount = (long)width * height;

            if (Avx2.IsSupported)
            {
                if (width >= 32)
                {
                    int optimalThreads = GetThreadCountForRgb2YCbCr(pixelCount);
                    if (optimalThreads > 1)
                    {
                        var options = new ParallelOptions { MaxDegreeOfParallelism = optimalThreads };
                        InterWaveSimd.Rgb2YCbCrParallelVector256(pIn, width, height, rowSizeInBytes, pY, pCb, pCr, outRowSizeInBytes, options);
                    }
                    else
                    {
                        InterWaveSimd.Rgb2YCbCrVector256(pIn, width, height, rowSizeInBytes, pY, pCb, pCr, outRowSizeInBytes);
                    }
                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                if (width >= 16)
                {
                    int optimalThreads = GetThreadCountForRgb2YCbCr(pixelCount);
                    if (optimalThreads > 1)
                    {
                        var options = new ParallelOptions { MaxDegreeOfParallelism = optimalThreads };
                        InterWaveSimd.Rgb2YCbCrParallelVector128(pIn, width, height, rowSizeInBytes, pY, pCb, pCr, outRowSizeInBytes, options);
                    }
                    else
                    {
                        InterWaveSimd.Rgb2YCbCrVector128(pIn, width, height, rowSizeInBytes, pY, pCb, pCr, outRowSizeInBytes);
                    }
                    return;
                }
            }

            // Scalar Fallback
            EnsureLutsInitialized();
            InterWaveTransform.Rgb2YCbCrScalar(pPixBuff, width, height, rowSizeInBytes, pOutY, pOutCb, pOutCr, outRowSizeInBytes);
        }

        /// <summary>
        /// Convenience overload for contiguous input buffers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void YCbCr2Rgb(Pixel* pPixBuff, int width, int height)
        {
            int rowSizeInBytes = width * sizeof(Pixel);
            YCbCr2Rgb(pPixBuff, width, height, rowSizeInBytes);
        }

        /// <summary>
        /// Next-generation unified AVX2 loop for IN-PLACE YCbCr to RGB conversion.
        /// Architecture: Branchless Read-Ahead Pipeline.
        /// Overlapping tails cause read-after-write corruption in in-place transforms. To avoid this
        /// without branching or code duplication, the loop loads the NEXT memory chunk at the very beginning
        /// of the CURRENT iteration. This buffers the overlapping tail bytes *before* the current iteration's
        /// store operation overwrites them, guaranteeing perfect memory safety via pure CMOV hardware instructions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe void YCbCr2Rgb(Pixel* pPixBuff, int width, int height, int rowSizeInBytes)
        {
            if (pPixBuff == null)
            {
                throw new DjvuArgumentNullException(nameof(pPixBuff));
            }
            if (width <= 0)
            {
                throw new DjvuArgumentOutOfRangeException(
                    nameof(width),
                    width,
                    "Width must be greater than zero.");
            }
            if (height <= 0)
            {
                throw new DjvuArgumentOutOfRangeException(
                    nameof(height),
                    height,
                    "Height must be greater than zero.");
            }
            if (rowSizeInBytes < (long)width * sizeof(Pixel))
            {
                throw new DjvuArgumentOutOfRangeException(
                    nameof(rowSizeInBytes),
                    rowSizeInBytes,
                    $"Row size ({rowSizeInBytes}) must be at least width * sizeof(Pixel) ({(long)width * sizeof(Pixel)}).");
            }

            long pixelCount = (long)width * height;

            if (Avx2.IsSupported)
            {
                if (width >= 32)
                {
                    int optimalThreads = GetThreadCountForYCbCr2Rgb(pixelCount);
                    if (optimalThreads > 1)
                    {
                        var options = new ParallelOptions { MaxDegreeOfParallelism = optimalThreads };
                        InterWaveSimd.YCbCr2RgbParallelVector256(pPixBuff, width, height, rowSizeInBytes, options);
                    }
                    else
                    {
                        InterWaveSimd.YCbCr2RgbVector256(pPixBuff, width, height, rowSizeInBytes);
                    }
                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                if (width >= 16)
                {
                    int optimalThreads = GetThreadCountForYCbCr2Rgb(pixelCount);
                    if (optimalThreads > 1)
                    {
                        var options = new ParallelOptions { MaxDegreeOfParallelism = optimalThreads };
                        InterWaveSimd.YCbCr2RgbParallelVector128(pPixBuff, width, height, rowSizeInBytes, options);
                    }
                    else
                    {
                        InterWaveSimd.YCbCr2RgbVector128(pPixBuff, width, height, rowSizeInBytes);
                    }
                    return;
                }
            }

            // Scalar Fallback
            InterWaveTransform.YCbCr2RgbScalar(pPixBuff, width, height, rowSizeInBytes);
        }
    }
}
