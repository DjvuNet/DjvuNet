using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;
using DjvuNet.Errors;
using DjvuNet.Configuration;

namespace DjvuNet.Graphics
{
    /// <summary>
    /// This class represents 24 bit color image maps.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Coordinate System:</b> PixelMap operates on a Cartesian coordinate system where the Y-axis 
    /// increases from bottom to top. The coordinate (0, 0) represents the bottom-left corner of the image. 
    /// This aligns with the standard DjVu format and mathematics, contrasting with the top-down (top-left) 
    /// orientation common in GDI+ and Windows rendering pipelines.
    /// </para>
    /// <para>
    /// <b>Architectural Limits:</b> Maximum image dimensions are constrained by the .NET <see cref="Array.MaxLength"/> 
    /// limit (2,147,483,591 bytes) for the contiguous 1D sbyte array backing the map. Since each pixel 
    /// consumes 3 bytes (24bpp), the maximum supported theoretical resolution is bounded such that 
    /// (Width * Height * 3) &lt;= Array.MaxLength, yielding a maximum area of roughly 715,827,863 pixels.
    /// </para>
    /// </remarks>
    public sealed class PixelMap
    {
        public sbyte[] Data { get; internal set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public const int BytesPerPixel = 3;
        public const int BlueOffset = 0;
        public const int GreenOffset = 1;
        public const int RedOffset = 2;
        public const bool IsRampNeeded = false;

        internal void SetWidth(int width)
        {
            if (width < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width cannot be negative.");
            }
            Width = width;
        }

        internal void SetHeight(int height)
        {
            if (height < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height cannot be negative.");
            }
            Height = height;
        }

        #region Private Members

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static unsafe Pixel* GenerateGrayRamp(int grays, Pixel* ramp)
        {
            ramp[0] = Pixel.WhitePixel;
            int color = 0xff0000;
            int gmax = (grays > 1) ? (grays - 1) : 1;
            int i = 1;
            if (gmax > 1)
            {
                int delta = color / gmax;
                do
                {
                    color -= delta;
                    sbyte c = (sbyte)(color >> 16);
                    ramp[i++] = new Pixel(c, c, c);
                } while (i < gmax);
            }

            while (i < 256)
            {
                ramp[i++] = Pixel.BlackPixel;
            }
            return ramp;
        }

        /// <summary>
        /// Used to represent division as multiplication.
        /// </summary>
        private static readonly int[] _invmap = new int[256];

        /// <summary>
        /// Identity color correction table.
        /// </summary>
        internal static readonly int[] IdentityGammaCorr = new int[256];

        /// <summary>
        /// Cached color correction table.
        /// </summary>
        internal static int[] CachedGammaTable = new int[256];

        /// <summary>
        /// The color correction subsample for the cached color table.
        /// </summary>
        internal static double CachedGamma = -1D;

        /// <summary>
        /// Used for attenuation
        /// </summary>
        private static readonly Object[] _multiplierRefArray = new Object[256];

        private static Lock _syncLock = new();

        #endregion Private Members

        #region Public Properties

        #endregion Public Properties

        #region Constructors

        static PixelMap()
        {


            for (int i = 1; i < _invmap.Length; i++)
            {
                _invmap[i] = 0x10000 / i;
            }

            for (int i = 0; i < IdentityGammaCorr.Length; i++)
            {
                IdentityGammaCorr[i] = i;
            }
        }

        /// <summary> Creates a new PixelMap object.</summary>
        public PixelMap()
        {
        }

        public PixelMap(sbyte[] data, int width, int height)
        {
            if (data == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(data), "Data array cannot be null when initializing PixelMap.");
            }

            SetWidth(width);
            SetHeight(height);

            long expectedSize = (long)width * height * 3;
            if (data.Length < expectedSize)
            {
                DjvuExceptionUtil.ThrowArgument($"Data array size {data.Length} is insufficient for a {width}x{height} pixel map. Expected at least {expectedSize} elements.", nameof(data));
            }

            Data = data;
        }

        #endregion Constructors

        #region Public Methods

        public IPixel GetPixelAt(int row, int column)
        {
            return CreateGPixelReference(row, column).ToPixel();
        }

        /// <summary>
        /// Fill the array with color correction constants.
        /// </summary>
        /// <param name="gamma">
        /// Color correction subsample
        /// </param>
        /// <returns>
        /// The new color correction table
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static int[] GetGammaCorrection(double gamma)
        {
            lock (_syncLock)
            {
                if ((gamma < 0.10000000000000001D) || (gamma > 10D))
                {
                    DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(gamma), $"Gamma out of range: {gamma}");
                }

                int[] retval;
                if ((gamma < 1.0009999999999999D) && (gamma > 0.999D))
                {
                    retval = IdentityGammaCorr;
                }
                else
                {
                    if (!(gamma > (CachedGamma - 0.000000001000000001D) && gamma < (CachedGamma + 0.000000001000000001D)))
                    {
                        CachedGammaTable = new int[256];
                        for (int i = 0; i < CachedGammaTable.Length; i++)
                        {
                            double x = i / 255D;

                            if (DjvuOptions.BEZIERGAMMA)
                            {
                                double t = (Math.Sqrt(1.0D + (((gamma * gamma) - 1.0D) * x)) - 1.0D) / (gamma - 1.0D);
                                x = ((((1.0D - gamma) * t) + (2D * gamma)) * t) / (gamma + 1.0D);
                            }
                            else
                            {
                                x = Math.Pow(x, 1.0D / gamma);
                            }

                            CachedGammaTable[i] = (int)Math.Floor((255D * x) + 0.5D);
                        }
                        CachedGamma = gamma;
                    }
                    retval = CachedGammaTable;
                }
                return retval;
            }
        }

        /// <summary>
        /// Attenuate the specified bitmap.
        /// </summary>
        /// <param name="target">
        /// Bitmap to attenuate
        /// </param>
        /// <param name="xPos">
        /// horizontal position
        /// </param>
        /// <param name="Ppos">
        /// vertical position
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Attenuate(ref Bitmap target, int xPos, int yPos)
        {
            if (Unsafe.IsNullRef(ref target))
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(target), $"{typeof(Bitmap).FullName} target reference is null.");
            }

            // Compute number of rows and columns
            int rows = yPos + target.Height;

            if (rows > Height)
            {
                rows = Height;
            }

            if (yPos > 0)
            {
                rows -= yPos;
            }

            int columns = xPos + target.Width;

            if (columns > Width)
            {
                columns = Width;
            }

            if (xPos > 0)
            {
                columns -= xPos;
            }

            if ((rows <= 0) || (columns <= 0))
            {
                return;
            }

            // Precompute multiplier map
            int maxGray = target.Grays - 1;
            int[] multiplier = GetMultiplier(maxGray);

            // Compute starting point
            int src = target.RowOffset((yPos < 0) ? (-yPos) : 0) - ((xPos < 0) ? xPos : 0);
            int dst = RowOffset((yPos > 0) ? yPos : 0) + ((xPos > 0) ? xPos : 0);

            IPixelReference dstPixel = CreateGPixelReference(0);

            // Loop over rows
            for (int y = 0; y < rows; y++)
            {
                // Loop over columns
                dstPixel.SetOffset(dst);

                for (int x = 0; x < columns; dstPixel.IncOffset())
                {
                    int srcpix = target.GetByteAt(src + (x++));

                    // Perform pixel operation
                    if (srcpix > 0)
                    {
                        if (srcpix >= maxGray)
                        {
                            dstPixel.SetGray(0);
                        }
                        else
                        {
                            int level = multiplier[srcpix];
                            dstPixel.SetBGR((dstPixel.Blue * level) >> 16, (dstPixel.Green * level) >> 16,
                                            (dstPixel.Red * level) >> 16);
                        }
                    }
                }

                // Next line
                dst += GetRowSize();
                src += target.GetRowSize();
            }
        }

        /// <summary>
        /// Insert the specified bitmap with the specified color.
        /// </summary>
        /// <param name="bitmap">
        /// bitmap to insert
        /// </param>
        /// <param name="xPos">
        /// horizontal position
        /// </param>
        /// <param name="yPos">
        /// vertical position
        /// </param>
        /// <param name="color">
        /// color to insert bitmap with
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe void Blit(ref Bitmap bitmap, int xPos, int yPos, Pixel color)
        {
            if (Unsafe.IsNullRef(ref bitmap))
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(bitmap), $"{typeof(Bitmap).FullName} bm reference is null.");
            }

            // Compute number of rows and columns
            int rowCount = yPos + bitmap.Height;

            if (rowCount > Height)
            {
                rowCount = Height;
            }

            if (yPos > 0)
            {
                rowCount -= yPos;
            }

            int columnCount = xPos + bitmap.Width;

            if (columnCount > Width)
            {
                columnCount = Width;
            }

            if (xPos > 0)
            {
                columnCount -= xPos;
            }

            if ((rowCount <= 0) || (columnCount <= 0))
            {
                return;
            }

            // Precompute multiplier map
            int maxGray = bitmap.Grays - 1;
            int[] multiplier = new int[maxGray];

            for (int i = 0; i < maxGray; i++)
            {
                multiplier[i] = 0x10000 - ((i << 16) / maxGray);
            }

            // Cache target color
            int targetRed = (byte)color.Red;
            int targetGreen = (byte)color.Green;
            int targetBlue = (byte)color.Blue;

            // Compute starting point
            int bitmapStartY = (yPos < 0) ? (-yPos) : 0;
            int bitmapStartX = (xPos < 0) ? (-xPos) : 0;
            int dstOffset = ((yPos > 0) ? RowOffset(yPos) : 0) + ((xPos > 0) ? xPos : 0);
            int dstRowIncrement = GetRowSize();

            // Precompute target color vectors and shuffle masks for AVX2 (32 pixels) and SSE4.1 (16 pixels)
            Vector256<byte> target0Vec256 = Vector256<byte>.Zero;
            Vector256<byte> target1Vec256 = Vector256<byte>.Zero;
            Vector256<byte> target2Vec256 = Vector256<byte>.Zero;
            Vector256<byte> maxGrayVec256 = Vector256.Create((byte)maxGray);
            Vector256<byte> shuffleMask0Vec256 = Vector256<byte>.Zero;
            Vector256<byte> shuffleMask1Vec256 = Vector256<byte>.Zero;
            Vector256<byte> shuffleMask2Vec256 = Vector256<byte>.Zero;

            Vector128<byte> target0Vec = Vector128<byte>.Zero;
            Vector128<byte> target1Vec = Vector128<byte>.Zero;
            Vector128<byte> target2Vec = Vector128<byte>.Zero;
            Vector128<byte> maxGrayVec = Vector128.Create((byte)maxGray);
            Vector128<byte> shuffleMask0Vec = Vector128<byte>.Zero;
            Vector128<byte> shuffleMask1Vec = Vector128<byte>.Zero;
            Vector128<byte> shuffleMask2Vec = Vector128<byte>.Zero;

            if (Avx2.IsSupported)
            {
                byte* tmpTargets = stackalloc byte[96];
                for (int i = 0; i < 96; i += 3)
                {
                    tmpTargets[i] = (byte)targetBlue;
                    tmpTargets[i + 1] = (byte)targetGreen;
                    tmpTargets[i + 2] = (byte)targetRed;
                }
                target0Vec256 = Avx.LoadVector256(tmpTargets);
                target1Vec256 = Avx.LoadVector256(tmpTargets + 32);
                target2Vec256 = Avx.LoadVector256(tmpTargets + 64);

                Vector128<byte> smA = Vector128.Create((byte)0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5);
                Vector128<byte> smB = Vector128.Create((byte)5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10);
                Vector128<byte> smC = Vector128.Create((byte)10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15);
                
                shuffleMask0Vec256 = Vector256.Create(smA, smB);
                shuffleMask1Vec256 = Vector256.Create(smC, smA);
                shuffleMask2Vec256 = Vector256.Create(smB, smC);

                target0Vec = target0Vec256.GetLower();
                target1Vec = target0Vec256.GetUpper();
                target2Vec = target1Vec256.GetLower();
                shuffleMask0Vec = smA;
                shuffleMask1Vec = smB;
                shuffleMask2Vec = smC;
            }
            else if (Sse41.IsSupported)
            {
                byte* tmpTargets = stackalloc byte[48];
                for (int i = 0; i < 48; i += 3)
                {
                    tmpTargets[i] = (byte)targetBlue;
                    tmpTargets[i + 1] = (byte)targetGreen;
                    tmpTargets[i + 2] = (byte)targetRed;
                }
                target0Vec = Sse2.LoadVector128(tmpTargets);
                target1Vec = Sse2.LoadVector128(tmpTargets + 16);
                target2Vec = Sse2.LoadVector128(tmpTargets + 32);

                shuffleMask0Vec = Vector128.Create((byte)0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5);
                shuffleMask1Vec = Vector128.Create((byte)5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10);
                shuffleMask2Vec = Vector128.Create((byte)10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15);
            }

            fixed (sbyte* pData = Data)
            {
                // Loop over rows
                for (int y = 0; y < rowCount; y++)
                {
                    sbyte* sourcePtr = bitmap.GetRow(bitmapStartY + y) + bitmapStartX;
                    int currentDst = dstOffset;
                    int x = 0;

                    if (Avx2.IsSupported)
                    {
                        for (; x <= columnCount - 32; x += 32)
                        {
                            Vector256<byte> srcAlpha = Avx.LoadVector256((byte*)sourcePtr + x);
                            
                            // 1. ALL-ZERO BLOCK
                            if (Avx2.MoveMask(Avx2.CompareEqual(srcAlpha, Vector256<byte>.Zero)) == -1)
                            {
                                currentDst += 32;
                                continue;
                            }

                            sbyte* pixelPtr = pData + (currentDst * BytesPerPixel);
                            Vector256<byte> isMax = Avx2.CompareEqual(srcAlpha, maxGrayVec256);

                            // 2. ALL-OPAQUE BLOCK
                            if (Avx2.MoveMask(isMax) == -1)
                            {
                                Avx.Store((byte*)pixelPtr, target0Vec256);
                                Avx.Store((byte*)pixelPtr + 32, target1Vec256);
                                Avx.Store((byte*)pixelPtr + 64, target2Vec256);
                                currentDst += 32;
                                continue;
                            }

                            // 3. MIXED BITONAL EDGE
                            Vector256<byte> isZero = Avx2.CompareEqual(srcAlpha, Vector256<byte>.Zero);
                            if (Avx2.MoveMask(Avx2.Or(isZero, isMax)) == -1)
                            {
                                Vector256<byte> srcDupLower = Avx2.Permute4x64(isMax.AsInt64(), 0b_01_00_01_00).AsByte();
                                Vector256<byte> srcDupUpper = Avx2.Permute4x64(isMax.AsInt64(), 0b_11_10_11_10).AsByte();

                                Vector256<byte> alpha0 = Avx2.Shuffle(srcDupLower, shuffleMask0Vec256);
                                Vector256<byte> alpha1 = Avx2.Shuffle(isMax, shuffleMask1Vec256);
                                Vector256<byte> alpha2 = Avx2.Shuffle(srcDupUpper, shuffleMask2Vec256);

                                Vector256<byte> dst0 = Avx.LoadVector256((byte*)pixelPtr);
                                Vector256<byte> dst1 = Avx.LoadVector256((byte*)pixelPtr + 32);
                                Vector256<byte> dst2 = Avx.LoadVector256((byte*)pixelPtr + 64);

                                Avx.Store((byte*)pixelPtr, Avx2.BlendVariable(dst0, target0Vec256, alpha0));
                                Avx.Store((byte*)pixelPtr + 32, Avx2.BlendVariable(dst1, target1Vec256, alpha1));
                                Avx.Store((byte*)pixelPtr + 64, Avx2.BlendVariable(dst2, target2Vec256, alpha2));
                                
                                currentDst += 32;
                                continue;
                            }

                            // 4. FRACTIONAL ANTI-ALIASED FALLBACK
                            for (int i = 0; i < 32; i++)
                            {
                                int sourcePixel = (byte)sourcePtr[x + i];
                                if (sourcePixel != 0)
                                {
                                    sbyte* p = pixelPtr + (i * BytesPerPixel);
                                    if (sourcePixel >= maxGray)
                                    {
                                        p[BlueOffset] = unchecked((sbyte)targetBlue);
                                        p[GreenOffset] = unchecked((sbyte)targetGreen);
                                        p[RedOffset] = unchecked((sbyte)targetRed);
                                    }
                                    else
                                    {
                                        int level0 = multiplier[sourcePixel];
                                        int level1 = 0x10000 - level0;
                                        p[BlueOffset] = unchecked((sbyte)((((byte)p[BlueOffset] * level0) + (targetBlue * level1)) >> 16));
                                        p[GreenOffset] = unchecked((sbyte)((((byte)p[GreenOffset] * level0) + (targetGreen * level1)) >> 16));
                                        p[RedOffset] = unchecked((sbyte)((((byte)p[RedOffset] * level0) + (targetRed * level1)) >> 16));
                                    }
                                }
                            }
                            currentDst += 32;
                        }
                    }

                    if (Sse41.IsSupported)
                    {
                        for (; x <= columnCount - 16; x += 16)
                        {
                            Vector128<byte> srcAlpha = Sse2.LoadVector128((byte*)sourcePtr + x);
                            
                            // 1. ALL-ZERO BLOCK: Instant Skip
                            if (Sse2.MoveMask(Sse2.CompareEqual(srcAlpha, Vector128<byte>.Zero)) == 0xFFFF)
                            {
                                currentDst += 16;
                                continue;
                            }

                            sbyte* pixelPtr = pData + (currentDst * BytesPerPixel);
                            Vector128<byte> isMax = Sse2.CompareEqual(srcAlpha, maxGrayVec);

                            // 2. ALL-OPAQUE BLOCK: Instant Overwrite
                            if (Sse2.MoveMask(isMax) == 0xFFFF)
                            {
                                Sse2.Store((byte*)pixelPtr, target0Vec);
                                Sse2.Store((byte*)pixelPtr + 16, target1Vec);
                                Sse2.Store((byte*)pixelPtr + 32, target2Vec);
                                currentDst += 16;
                                continue;
                            }

                            // 3. MIXED BITONAL EDGE: SSE4.1 Blend Variable
                            Vector128<byte> isZero = Sse2.CompareEqual(srcAlpha, Vector128<byte>.Zero);
                            if (Sse2.MoveMask(Sse2.Or(isZero, isMax)) == 0xFFFF)
                            {
                                Vector128<byte> alpha0 = Ssse3.Shuffle(isMax, shuffleMask0Vec);
                                Vector128<byte> alpha1 = Ssse3.Shuffle(isMax, shuffleMask1Vec);
                                Vector128<byte> alpha2 = Ssse3.Shuffle(isMax, shuffleMask2Vec);

                                Vector128<byte> dst0 = Sse2.LoadVector128((byte*)pixelPtr);
                                Vector128<byte> dst1 = Sse2.LoadVector128((byte*)pixelPtr + 16);
                                Vector128<byte> dst2 = Sse2.LoadVector128((byte*)pixelPtr + 32);

                                Sse2.Store((byte*)pixelPtr, Sse41.BlendVariable(dst0, target0Vec, alpha0));
                                Sse2.Store((byte*)pixelPtr + 16, Sse41.BlendVariable(dst1, target1Vec, alpha1));
                                Sse2.Store((byte*)pixelPtr + 32, Sse41.BlendVariable(dst2, target2Vec, alpha2));
                                
                                currentDst += 16;
                                continue;
                            }

                            // 4. FRACTIONAL ANTI-ALIASED FALLBACK
                            for (int i = 0; i < 16; i++)
                            {
                                int sourcePixel = (byte)sourcePtr[x + i];
                                if (sourcePixel != 0)
                                {
                                    sbyte* p = pixelPtr + (i * BytesPerPixel);
                                    if (sourcePixel >= maxGray)
                                    {
                                        p[BlueOffset] = unchecked((sbyte)targetBlue);
                                        p[GreenOffset] = unchecked((sbyte)targetGreen);
                                        p[RedOffset] = unchecked((sbyte)targetRed);
                                    }
                                    else
                                    {
                                        int level0 = multiplier[sourcePixel];
                                        int level1 = 0x10000 - level0;
                                        p[BlueOffset] = unchecked((sbyte)((((byte)p[BlueOffset] * level0) + (targetBlue * level1)) >> 16));
                                        p[GreenOffset] = unchecked((sbyte)((((byte)p[GreenOffset] * level0) + (targetGreen * level1)) >> 16));
                                        p[RedOffset] = unchecked((sbyte)((((byte)p[RedOffset] * level0) + (targetRed * level1)) >> 16));
                                    }
                                }
                            }
                            currentDst += 16;
                        }
                    }

                    // 5. REMAINDER LOOP
                    for (; x < columnCount; x++)
                    {
                        int sourcePixel = (byte)sourcePtr[x];
                        if (sourcePixel != 0)
                        {
                            sbyte* pixelPtr = pData + (currentDst * BytesPerPixel);
                            if (sourcePixel >= maxGray)
                            {
                                pixelPtr[BlueOffset] = unchecked((sbyte)targetBlue);
                                pixelPtr[GreenOffset] = unchecked((sbyte)targetGreen);
                                pixelPtr[RedOffset] = unchecked((sbyte)targetRed);
                            }
                            else
                            {
                                int level0 = multiplier[sourcePixel];
                                int level1 = 0x10000 - level0;
                                pixelPtr[BlueOffset] = unchecked((sbyte)((((byte)pixelPtr[BlueOffset] * level0) + (targetBlue * level1)) >> 16));
                                pixelPtr[GreenOffset] = unchecked((sbyte)((((byte)pixelPtr[GreenOffset] * level0) + (targetGreen * level1)) >> 16));
                                pixelPtr[RedOffset] = unchecked((sbyte)((((byte)pixelPtr[RedOffset] * level0) + (targetRed * level1)) >> 16));
                            }
                        }
                        currentDst++;
                    }

                    // Next line
                    dstOffset += dstRowIncrement;
                }
            }
        }

        /// <summary>
        /// Correct the colors of the <see cref="DjvuNet.Graphics.PixelMap"/> instance with a gamma subsample normalized to 1.0 for no correction.
        /// </summary>
        /// <param name="gamma">
        /// Color gamma correction
        /// </param>
        public void ApplyGammaCorrection(double gamma)
        {
            ApplyGamma(gamma, Data);
        }

        /// <summary>
        /// Apply gamma correction to passed image data.
        /// </summary>
        /// <param name="gamma"></param>
        /// <param name="data"></param>
        public static void ApplyGamma(double gamma, sbyte[] data)
        {
            if ((gamma > 0.999D) && (gamma < 1.0009999999999999D))
            {
                return;
            }

            int[] gtable = GetGammaCorrection(gamma);

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (sbyte)gtable[unchecked((byte)data[i])];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe void ApplyGammaFastMT(double gamma, sbyte[] data)
        {
            if ((gamma > 0.999D) && (gamma < 1.0009999999999999D))
            {
                return;
            }

            int[] gtable = GetGammaCorrection(gamma);

            GCHandle hData = default(GCHandle);
            GCHandle hGTable = default(GCHandle);

            hData = GCHandle.Alloc(data, GCHandleType.Pinned);
            hGTable = GCHandle.Alloc(gtable, GCHandleType.Pinned);
            byte* pData = (byte*)hData.AddrOfPinnedObject();
            int* gammaLUT = (int*)hGTable.AddrOfPinnedObject();
            int dataLength = data.Length;
            int reminderLength = data.Length % 48;

            // Parallel.For on 4 cores is 70% slower than single threaded
            // on one core - extremely inefficient loop call design
            int prllReminder = 0;
            int procCount = Environment.ProcessorCount;
            int part = Math.DivRem(dataLength, procCount, out prllReminder);

            TaskFactory tskFactory = new TaskFactory();
            List<Task> tasks = new List<Task>();
            for (int k = 0; k < procCount; k++)
            {
                tasks.Add(null);
            }

            for (int k = 0; k < procCount; k++)
            {
                int index = part * k;
                tasks[k] = tskFactory.StartNew(() =>
                {
                    int i = index;
                    int partEnd = index + part - 48;
                    if ((k + 1) == procCount)
                    {
                        partEnd += prllReminder;
                    }

                    for (; i < partEnd;)
                    {
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];

                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];

                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];

                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];

                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];

                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                        pData[i] = (byte)gammaLUT[pData[i++]];
                    }
                });
            }

            Task.WaitAll(tasks.ToArray());

            if (hData.IsAllocated)
            {
                hData.Free();
            }

            if (hGTable.IsAllocated)
            {
                hGTable.Free();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe void ApplyGammaFastST(double gamma, sbyte[] data)
        {
            if ((gamma > 0.999D) && (gamma < 1.0009999999999999D))
            {
                return;
            }

            int[] gtable = GetGammaCorrection(gamma);
            GCHandle hData = default(GCHandle);
            GCHandle hGTable = default(GCHandle);

            hData = GCHandle.Alloc(data, GCHandleType.Pinned);
            hGTable = GCHandle.Alloc(gtable, GCHandleType.Pinned);
            byte* pData = (byte*)hData.AddrOfPinnedObject();
            int* gammaLUT = (int*)hGTable.AddrOfPinnedObject();

            int dataLength = data.Length;
            int reminderLength = data.Length % 48;
            dataLength -= reminderLength;

            for (int i = 0; i < dataLength;)
            {
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];

                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];

                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];

                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];

                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];

                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
            }

            for (int i = dataLength; i < dataLength + reminderLength; i++)
            {
                pData[i] = (byte)gammaLUT[pData[i]];
            }
            if (hData.IsAllocated)
            {
                hData.Free();
            }

            if (hGTable.IsAllocated)
            {
                hGTable.Free();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe void ApplyGamma(byte* pData, int dataLengthRem, int dataLength, int* gammaLUT)
        {
            for (int i = 0; i < dataLength;)
            {
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];

                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];

                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];

                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];

                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];

                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
                pData[i] = (byte)gammaLUT[pData[i++]];
            }

            for (int i = dataLength; i < dataLengthRem; i++)
            {
                pData[i] = (byte)gammaLUT[pData[i]];
            }
        }

        public Rectangle BoundingRectangle
        {
            get
            {
                return new Rectangle
                {
                    XMax = Width,
                    YMin = 0,
                    XMin = 0,
                    YMax = Height
                };
            }
        }

        /// <summary>
        /// Fill this image from another source at reduced resolution.  Pixel
        /// averaging will be used.
        /// </summary>
        /// <param name="src">
        /// Image source to reduce
        /// </param>
        /// <param name="subsample">
        /// Subsample value
        /// </param>
        /// <param name="targetRect">
        /// Target bounds
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void DownSample(PixelMap src, int subsample, Rectangle targetRect)
        {
            Utilities.Verify.SubsampleRange(subsample);

            if (src == this && ((targetRect.Empty || targetRect == BoundingRectangle) && subsample == 1))
            {
                return;
            }

            Rectangle rect = BoundingRectangle;

            if (!targetRect.Empty)
            {
                if ((targetRect.XMin < rect.XMin) || (targetRect.YMin < rect.YMin) ||
                    (targetRect.XMax > rect.XMax) || (targetRect.YMax > rect.YMax))
                {
                    DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(targetRect),
                        $"Specified rectangle overflows destination PixelMap {nameof(BoundingRectangle)}");
                }

                rect = targetRect;
            }
            else
            {
                int width =  (((src.Width + subsample) - 1) / subsample);
                int height =  (((src.Height + subsample) - 1) / subsample);
                rect = new Rectangle(0, 0, width, height);
            }

            Init(rect.Height, rect.Width);

            int sy = rect.YMin * subsample;
            int sxz = rect.XMin * subsample;
            int sidx = src.RowOffset(sy);
            int didx = 0;

            IPixelReference sptr = src.CreateGPixelReference(0);
            IPixelReference dptr = CreateGPixelReference(0);

            for (int y = 0; y < Height; y++)
            {
                int sx = sxz;

                for (int x = Width; x-- > 0; dptr.IncOffset())
                {
                    int r = 0;
                    int g = 0;
                    int b = 0;
                    int s = 0;
                    int kidx = sidx;
                    int lsy = sy + subsample;

                    if (lsy > src.Height)
                    {
                        lsy = src.Height;
                    }

                    int lsx = sx + subsample;

                    if (lsx > src.Width)
                    {
                        lsx = src.Width;
                    }

                    for (int rsy = sy; rsy < lsy; rsy++)
                    {
                        sptr.SetOffset(kidx + sx);
                        for (int rsx = lsx - sx; rsx-- > 0; sptr.IncOffset())
                        {
                            r += sptr.Red;
                            g += sptr.Green;
                            b += sptr.Blue;
                            s++;
                        }

                        kidx += src.GetRowSize();
                    }

                    if (s >= _invmap.Length)
                    {
                        dptr.SetBGR(b / s, g / s, r / s);
                    }
                    else
                    {
                        dptr.SetBGR(((b * _invmap[s]) + 32768) >> 16, ((g * _invmap[s]) + 32768) >> 16,
                                   ((r * _invmap[s]) + 32768) >> 16);
                    }

                    sx += subsample;
                }

                sy += subsample;
                sidx += src.RowOffset(subsample);
                dptr.SetOffset(didx += GetRowSize());
            }
        }

        /// <summary>
        /// Fill this image from another source at reduced resolution of 4 vertical
        /// pixels to 3.  An extrapolating pixel averaging algorithm is used.
        /// </summary>
        /// <param name="src">
        /// Image map to reduce
        /// </param>
        /// <param name="targetRect">
        /// Target bounds
        /// </param>
        /// <throws>
        /// <see cref="DjvuNet.Errors.DjvuArgumentOutOfRangeException"/> if the target rectangle is out of bounds
        /// </throws>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void DownSample43(PixelMap src, Rectangle targetRect)
        {
            int srcWidth = src.Width;
            int srcHeight = src.Height;
            int destWidth = (int)Math.Ceiling(srcWidth * 0.75D);
            int destHeight = (int)Math.Ceiling(srcHeight * 0.75D);
            Rectangle rect = new Rectangle(0, 0, destWidth, destHeight);

            if (!targetRect.Empty)
            {
                if ((targetRect.XMin < rect.XMin) || (targetRect.YMin < rect.YMin) || (targetRect.XMax > rect.XMax) || (targetRect.YMax > rect.YMax))
                {
                    DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(targetRect), "Rectangle out of bounds" + "pdr=(" + targetRect.XMin + "," + targetRect.YMin + "," +
                                                targetRect.XMax + "," + targetRect.YMax + "),rect=(" + rect.XMin + "," + rect.YMin +
                                                "," + rect.XMax + "," + rect.YMax + ")");
                }

                rect = targetRect;
                destWidth = rect.Width;
                destHeight = rect.Height;
            }

            Init(destHeight, destWidth);

            int sy = rect.YMin / 3;
            int dy = rect.YMin - (3 * sy);

            //    if(dy < 0)
            //    {
            //      sy--;
            //      dy += 3;
            //    }

            int sxz = rect.XMin / 3;
            int dxz = rect.XMin - (3 * sxz);

            if (dxz < 0)   // always false what can be trivially derived from proceeding 2 loc
            {
                sxz--;
                dxz += 3;
            }

            sxz *= 4;
            sy *= 4;

            var spix0 = src.CreateGPixelReference(0);
            var spix1 = src.CreateGPixelReference(0);
            var spix2 = src.CreateGPixelReference(0);
            var spix3 = src.CreateGPixelReference(0);
            var dpix0 = CreateGPixelReference(0);
            var dpix1 = CreateGPixelReference(0);
            var dpix2 = CreateGPixelReference(0);
            while (dy < destHeight)
            {
                spix0.SetOffset(sy++, sxz);

                if (sy >= srcHeight)
                {
                    sy--;
                }

                spix1.SetOffset(sy++, sxz);

                if (sy >= srcHeight)
                {
                    sy--;
                }

                spix2.SetOffset(sy++, sxz);

                if (sy >= srcHeight)
                {
                    sy--;
                }

                spix3.SetOffset(sy++, sxz);

                dpix0.SetOffset((dy < 0) ? 0 : dy, dxz);

                if (++dy >= destHeight)
                {
                    dy--;
                }

                dpix1.SetOffset((dy < 0) ? 0 : dy, dxz);

                if (++dy >= destHeight)
                {
                    dy--;
                }

                dpix2.SetOffset(dy++, dxz);
                int dx = dxz;
                int sx = sxz;

                IPixelReference pix0 = spix0;
                IPixelReference pix1 = spix1;
                IPixelReference pix2 = spix2;
                IPixelReference pix3 = spix3;
                while (dx < destWidth)
                {
                    int s00b = pix0.Blue;
                    int s00g = pix0.Green;
                    int s00r = pix0.Red;
                    int s01b = pix1.Blue;
                    int s01g = pix1.Green;
                    int s01r = pix1.Red;
                    int s02b = pix2.Blue;
                    int s02g = pix2.Green;
                    int s02r = pix2.Red;
                    int s03b = pix3.Blue;
                    int s03g = pix3.Green;
                    int s03r = pix3.Red;

                    if (++sx < srcWidth)
                    {
                        spix0.IncOffset();
                        spix1.IncOffset();
                        spix2.IncOffset();
                        spix3.IncOffset();
                        pix0 = spix0;
                        pix1 = spix1;
                        pix2 = spix2;
                        pix3 = spix3;
                    }

                    int s10b = pix0.Blue;
                    int s10g = pix0.Green;
                    int s10r = pix0.Red;
                    int s11b = pix1.Blue;
                    int s11g = pix1.Green;
                    int s11r = pix1.Red;
                    int s12b = pix2.Blue;
                    int s12g = pix2.Green;
                    int s12r = pix2.Red;
                    int s13b = pix3.Blue;
                    int s13g = pix3.Green;
                    int s13r = pix3.Red;

                    if (++sx < srcWidth)
                    {
                        spix0.IncOffset();
                        spix1.IncOffset();
                        spix2.IncOffset();
                        spix3.IncOffset();
                        pix0 = spix0;
                        pix1 = spix1;
                        pix2 = spix2;
                        pix3 = spix3;
                    }

                    int s20b = pix0.Blue;
                    int s20g = pix0.Green;
                    int s20r = pix0.Red;
                    int s21b = pix1.Blue;
                    int s21g = pix1.Green;
                    int s21r = pix1.Red;
                    int s22b = pix2.Blue;
                    int s22g = pix2.Green;
                    int s22r = pix2.Red;
                    int s23b = pix3.Blue;
                    int s23g = pix3.Green;
                    int s23r = pix3.Red;

                    if (++sx < srcWidth)
                    {
                        spix0.IncOffset();
                        spix1.IncOffset();
                        spix2.IncOffset();
                        spix3.IncOffset();
                        pix0 = spix0;
                        pix1 = spix1;
                        pix2 = spix2;
                        pix3 = spix3;
                    }

                    int s30b = pix0.Blue;
                    int s30g = pix0.Green;
                    int s30r = pix0.Red;
                    int s31b = pix1.Blue;
                    int s31g = pix1.Green;
                    int s31r = pix1.Red;
                    int s32b = pix2.Blue;
                    int s32g = pix2.Green;
                    int s32r = pix2.Red;
                    int s33b = pix3.Blue;
                    int s33g = pix3.Green;
                    int s33r = pix3.Red;

                    if (++sx < srcWidth)
                    {
                        spix0.IncOffset();
                        spix1.IncOffset();
                        spix2.IncOffset();
                        spix3.IncOffset();
                        pix0 = spix0;
                        pix1 = spix1;
                        pix2 = spix2;
                        pix3 = spix3;
                    }

                    dpix0.Blue = (sbyte)(((11 * s00b) + (2 * (s01b + s10b)) + s11b + 8) >> 4);
                    dpix0.Green = (sbyte)(((11 * s00g) + (2 * (s01g + s10g)) + s11g + 8) >> 4);
                    dpix0.Red = (sbyte)(((11 * s00r) + (2 * (s01r + s10r)) + s11r + 8) >> 4);
                    dpix1.Blue = (sbyte)(((7 * (s01b + s02b)) + s11b + s12b + 8) >> 4);
                    dpix1.Green = (sbyte)(((7 * (s01g + s02g)) + s11g + s12g + 8) >> 4);
                    dpix1.Red = (sbyte)(((7 * (s01r + s02r)) + s11r + s12r + 8) >> 4);
                    dpix2.Blue = (sbyte)(((11 * s03b) + (2 * (s02b + s13b)) + s12b + 8) >> 4);
                    dpix2.Green = (sbyte)(((11 * s03g) + (2 * (s02g + s13g)) + s12g + 8) >> 4);
                    dpix2.Red = (sbyte)(((11 * s03r) + (2 * (s02r + s13r)) + s12r + 8) >> 4);

                    if (++dx < destWidth)
                    {
                        dpix0.IncOffset();
                        dpix1.IncOffset();
                        dpix2.IncOffset();
                    }

                    dpix0.Blue = (sbyte)(((7 * (s10b + s20b)) + s11b + s21b + 8) >> 4);
                    dpix0.Green = (sbyte)(((7 * (s10g + s20g)) + s11g + s21g + 8) >> 4);
                    dpix0.Red = (sbyte)(((7 * (s10r + s20r)) + s11r + s21r + 8) >> 4);
                    dpix1.Blue = (sbyte)((s12b + s22b + s11b + s21b + 2) >> 2);
                    dpix1.Green = (sbyte)((s12g + s22g + s11g + s21g + 2) >> 2);
                    dpix1.Red = (sbyte)((s12r + s22r + s11r + s21r + 2) >> 2);
                    dpix2.Blue = (sbyte)(((7 * (s13b + s23b)) + s12b + s22b + 8) >> 4);
                    dpix2.Green = (sbyte)(((7 * (s13g + s23g)) + s12g + s22g + 8) >> 4);
                    dpix2.Red = (sbyte)(((7 * (s13r + s23r)) + s12r + s22r + 8) >> 4);

                    if (++dx < destWidth)
                    {
                        dpix0.IncOffset();
                        dpix1.IncOffset();
                        dpix2.IncOffset();
                    }

                    dpix0.Blue = (sbyte)(((11 * s30b) + (2 * (s31b + s20b)) + s21b + 8) >> 4);
                    dpix0.Green = (sbyte)(((11 * s30g) + (2 * (s31g + s20g)) + s21g + 8) >> 4);
                    dpix0.Red = (sbyte)(((11 * s30r) + (2 * (s31r + s20r)) + s21r + 8) >> 4);
                    dpix1.Blue = (sbyte)(((7 * (s31b + s32b)) + s21b + s22b + 8) >> 4);
                    dpix1.Green = (sbyte)(((7 * (s31g + s32g)) + s21g + s22g + 8) >> 4);
                    dpix1.Red = (sbyte)(((7 * (s31r + s32r)) + s21r + s22r + 8) >> 4);
                    dpix2.Blue = (sbyte)(((11 * s33b) + (2 * (s32b + s23b)) + s22b + 8) >> 4);
                    dpix2.Green = (sbyte)(((11 * s33g) + (2 * (s32g + s23g)) + s22g + 8) >> 4);
                    dpix2.Red = (sbyte)(((11 * s33r) + (2 * (s32r + s23r)) + s22r + 8) >> 4);

                    if (++dx < destWidth)
                    {
                        dpix0.IncOffset();
                        dpix1.IncOffset();
                        dpix2.IncOffset();
                    }
                }
            }
        }

        /// <summary>
        /// Insert the reference map at the specified location.
        /// </summary>
        /// <param name="source">
        /// Map to insert
        /// </param>
        /// <param name="dx">
        /// Horizontal position to insert at
        /// </param>
        /// <param name="dy">
        /// Vertical position to insert at
        /// </param>
        public unsafe void Fill(ref Bitmap source, int dx, int dy)
        {
            if (Unsafe.IsNullRef(ref source))
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(source), $"{typeof(Bitmap).FullName} source reference is null.");
            }

            if (source == default)
                DjvuExceptionUtil.ThrowArgument( 
                    "The source Bitmap cannot be default. Please provide a valid and initialized Bitmap instance.", nameof(source));

            if (source.Width < 0 || source.Height < 0)
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(source), 
                    $"The source Bitmap has invalid dimensions (Width: {source.Width}, Height: {source.Height}). Dimensions cannot be negative.");
            
            // Circuit breaker for empty sources
            if (source.Width == 0 || source.Height == 0)
                return;

            if (source.Data == null)
                DjvuExceptionUtil.ThrowInvalidOperation(
                    $"The source Bitmap.Data array is null - probably uninitialized. Current state - Width: {source.Width}, Height: {source.Height}, Data: {source.Data}.");

            int x0 = (dx > 0) ? dx : 0;
            int y0 = (dy > 0) ? dy : 0;
            int x1 = (dx < 0) ? (-dx) : 0;
            int y1 = (dy < 0) ? (-dy) : 0;
            int w0 = Width - x0;
            int w1 = source.Width - x1;
            int w = (w0 < w1) ? w0 : w1;
            int h0 = Height - y0;
            int h1 = source.Height - y1;
            int h = (h0 < h1) ? h0 : h1;

            if ((w > 0) && (h > 0))
            {
                if (source.Grays == 2)
                {
                    FillBitonal(source.Data, this.Data, w, h, source.BytesPerRow, Width * BytesPerPixel, source.RowOffset(y1) + x1, (this.RowOffset(y0) + x0) * BytesPerPixel);
                }
                else
                {
                    Pixel* localRamp = stackalloc Pixel[256];
                    Pixel* ramp = GenerateGrayRamp(source.Grays, localRamp);
                    int bpp = BytesPerPixel;
                    sbyte[] srcData = source.Data;
                    sbyte[] dstData = this.Data;

                    int srcStride = source.BytesPerRow;
                    int dstStride = Width * bpp;

                    int srcIdx = source.RowOffset(y1) + x1;
                    int dstIdx = (this.RowOffset(y0) + x0) * bpp;

                    do
                    {
                        int sIdx = srcIdx;
                        int dIdx = dstIdx;

                        for (int i = 0; i < w; i++)
                        {
                            Pixel p = ramp[(byte)srcData[sIdx++]];
                            dstData[dIdx++] = p.Blue;
                            dstData[dIdx++] = p.Green;
                            dstData[dIdx++] = p.Red;
                        }
                        
                        srcIdx += srcStride;
                        dstIdx += dstStride;
                    } while (--h > 0);
                }
            }
        }

        /// <summary>
        /// Insert the reference map at the specified location.
        /// </summary>
        public void Fill(PixelMap source, int dx, int dy)
        {
            if (source == null)
                DjvuExceptionUtil.ThrowArgumentNull(nameof(source), 
                    "The source PixelMap cannot be null. Please provide a valid PixelMap instance.");

            if (source.Width < 0 || source.Height < 0)
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(source), 
                    $"The source PixelMap has invalid dimensions (Width: {source.Width}, Height: {source.Height}). Dimensions cannot be negative.");
            
            // Circuit breaker for empty sources
            if (source.Width == 0 || source.Height == 0)
                return;

            if (source.Data == null)
                DjvuExceptionUtil.ThrowInvalidOperation(
                    $"The source PixelMap.Data array is null - probably uninitialized. Current state - Width: {source.Width}, Height: {source.Height}, Data: {source.Data}.");

            int x0 = (dx > 0) ? dx : 0;
            int y0 = (dy > 0) ? dy : 0;
            int x1 = (dx < 0) ? (-dx) : 0;
            int y1 = (dy < 0) ? (-dy) : 0;
            int w0 = Width - x0;
            int w1 = source.Width - x1;
            int w = (w0 < w1) ? w0 : w1;
            int h0 = Height - y0;
            int h1 = source.Height - y1;
            int h = (h0 < h1) ? h0 : h1;

            if ((w > 0) && (h > 0))
            {
                var pixel = CreateGPixelReference(0);
                var refPixel = source.CreateGPixelReference(0);

                do
                {
                    pixel.SetOffset(y0++, x0);
                    refPixel.SetOffset(y1++, x1);
                    pixel.SetPixels(refPixel, w);
                } while (--h > 0);
            }
        }

        /// <summary>
        /// Initialize this PixelMap to the specified size and fill in the specified color.
        /// </summary>
        /// <param name="height">
        /// Number of rows
        /// </param>
        /// <param name="width">
        /// Number of columns
        /// </param>
        /// <param name="color">
        /// Fill color
        /// </param>
        /// <returns>
        /// The initialized PixelMap
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitUninitialized(int height, int width)
        {
            if ((height != Height) || (width != Width))
            {
                SetHeight(height);
                SetWidth(width);
            }

            long npix = (long)Height * Width;

            if (npix > 0)
            {
                long totalBytes = npix * BytesPerPixel;

                // Guard against > Array.MaxLength and < 0
                if (totalBytes > Array.MaxLength || totalBytes < 0)
                {
                    DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height,
                        $"Image dimensions result in an invalid {nameof(Data)} buffer size (less than zero or exceeding Array.MaxLength).");
                }

                if (Data == null || Data.Length < totalBytes)
                {
                    Data = GC.AllocateUninitializedArray<sbyte>((int)totalBytes, pinned: false);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PixelMap Init(int height, int width)
        {
            if (height != Height || width != Width)
            {
                SetHeight(height);
                SetWidth(width);
            }

            long npix = (long)Height * Width;

            if (npix > 0)
            {
                long totalBytes = npix * 3;
                
                if (totalBytes > Array.MaxLength || totalBytes < 0)
                {
                    DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height,
                        $"Image dimensions result in an invalid {nameof(Data)} buffer size (less than zero or exceeding Array.MaxLength).");
                }

                // TODO (Optimization Opportunity): Consider replacing this array allocation with ArrayPool<sbyte>.Shared
                if (Data == null || Data.Length < totalBytes)
                {
                    Data = GC.AllocateArray<sbyte>((int)totalBytes, pinned: false);
                }
                else
                {
                    Unsafe.InitBlockUnaligned(
                        ref Unsafe.As<sbyte, byte>(ref MemoryMarshal.GetArrayDataReference(Data)),
                        0,
                        (uint)totalBytes);
                }
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe PixelMap Init(int height, int width, Pixel color)
        {
            if (color.Blue == 0 && color.Green == 0 && color.Red == 0)
            {
                return Init(height, width);
            }

            // Note: InitUninitialized serves as the primary bounds-check guard for image dimensions. 
            // If the Array.MaxLength and 64-bit overflow checks are ever removed from it, they MUST
            // be restored in this method
            InitUninitialized(height, width);

            long npix = (long)Height * Width;

            if (npix > 0)
            {
                long totalBytes = npix * 3;

                ref byte dataRef = ref Unsafe.As<sbyte, byte>(ref MemoryMarshal.GetArrayDataReference(Data));

                sbyte b = color.Blue;
                sbyte g = color.Green;
                sbyte r = color.Red;

                if (b == g && g == r)
                {
                    Unsafe.InitBlockUnaligned(ref dataRef, (byte)b, (uint)totalBytes);
                }
                else
                {
                    nuint offset = 0;

                    if (Vector512.IsHardwareAccelerated && totalBytes >= 192)
                    {
                        byte* pPattern = stackalloc byte[192];
                        for (int i = 0; i < 192;) { pPattern[i++] = (byte)b; pPattern[i++] = (byte)g; pPattern[i++] = (byte)r; }
                        
                        Vector512<byte> v0 = Vector512.Load(pPattern);
                        Vector512<byte> v1 = Vector512.Load(pPattern + 64);
                        Vector512<byte> v2 = Vector512.Load(pPattern + 128);

                        nuint limit = (nuint)totalBytes - 191;
                        while (offset < limit)
                        {
                            Vector512.StoreUnsafe(v0, ref dataRef, offset);
                            Vector512.StoreUnsafe(v1, ref dataRef, offset + 64);
                            Vector512.StoreUnsafe(v2, ref dataRef, offset + 128);
                            offset += 192;
                        }
                        
                        nuint rem = (nuint)totalBytes - offset;
                        if (rem > 0)
                        {
                            if (rem <= 64)
                            {
                                Vector512.StoreUnsafe(v2, ref dataRef, (nuint)totalBytes - 64);
                            }
                            else if (rem <= 128)
                            {
                                Vector512.StoreUnsafe(v1, ref dataRef, (nuint)totalBytes - 128);
                                Vector512.StoreUnsafe(v2, ref dataRef, (nuint)totalBytes - 64);
                            }
                            else
                            {
                                Vector512.StoreUnsafe(v0, ref dataRef, (nuint)totalBytes - 192);
                                Vector512.StoreUnsafe(v1, ref dataRef, (nuint)totalBytes - 128);
                                Vector512.StoreUnsafe(v2, ref dataRef, (nuint)totalBytes - 64);
                            }
                            offset = (nuint)totalBytes;
                        }
                    }
                    else if (Vector256.IsHardwareAccelerated && totalBytes >= 96)
                    {
                        byte* pPattern = stackalloc byte[96];
                        for (int i = 0; i < 96;) { pPattern[i++] = (byte)b; pPattern[i++] = (byte)g; pPattern[i++] = (byte)r; }
                        
                        Vector256<byte> v0 = Vector256.Load(pPattern);
                        Vector256<byte> v1 = Vector256.Load(pPattern + 32);
                        Vector256<byte> v2 = Vector256.Load(pPattern + 64);

                        nuint limit = (nuint)totalBytes - 95;
                        while (offset < limit)
                        {
                            Vector256.StoreUnsafe(v0, ref dataRef, offset);
                            Vector256.StoreUnsafe(v1, ref dataRef, offset + (nuint)32);
                            Vector256.StoreUnsafe(v2, ref dataRef, offset + (nuint)64);
                            offset += 96;
                        }
                        
                        nuint rem = (nuint)totalBytes - offset;
                        if (rem > 0)
                        {
                            if (rem <= 32)
                            {
                                Vector256.StoreUnsafe(v2, ref dataRef, (nuint)totalBytes - 32);
                            }
                            else if (rem <= 64)
                            {
                                Vector256.StoreUnsafe(v1, ref dataRef, (nuint)totalBytes - 64);
                                Vector256.StoreUnsafe(v2, ref dataRef, (nuint)totalBytes - 32);
                            }
                            else
                            {
                                Vector256.StoreUnsafe(v0, ref dataRef, (nuint)totalBytes - 96);
                                Vector256.StoreUnsafe(v1, ref dataRef, (nuint)totalBytes - 64);
                                Vector256.StoreUnsafe(v2, ref dataRef, (nuint)totalBytes - 32);
                            }
                            offset = (nuint)totalBytes;
                        }
                    }
                    else if (Vector128.IsHardwareAccelerated && totalBytes >= 48)
                    {
                        byte* pPattern = stackalloc byte[48];
                        for (int i = 0; i < 48;) { pPattern[i++] = (byte)b; pPattern[i++] = (byte)g; pPattern[i++] = (byte)r; }
                        
                        Vector128<byte> v0 = Vector128.Load(pPattern);
                        Vector128<byte> v1 = Vector128.Load(pPattern + 16);
                        Vector128<byte> v2 = Vector128.Load(pPattern + 32);

                        nuint limit = (nuint)totalBytes - 47;
                        while (offset < limit)
                        {
                            Vector128.StoreUnsafe(v0, ref dataRef, offset);
                            Vector128.StoreUnsafe(v1, ref dataRef, offset + (nuint)16);
                            Vector128.StoreUnsafe(v2, ref dataRef, offset + (nuint)32);
                            offset += 48;
                        }
                        
                        nuint rem = (nuint)totalBytes - offset;
                        if (rem > 0)
                        {
                            if (rem <= 16)
                            {
                                Vector128.StoreUnsafe(v2, ref dataRef, (nuint)totalBytes - 16);
                            }
                            else if (rem <= 32)
                            {
                                Vector128.StoreUnsafe(v1, ref dataRef, (nuint)totalBytes - 32);
                                Vector128.StoreUnsafe(v2, ref dataRef, (nuint)totalBytes - 16);
                            }
                            else
                            {
                                Vector128.StoreUnsafe(v0, ref dataRef, (nuint)totalBytes - 48);
                                Vector128.StoreUnsafe(v1, ref dataRef, (nuint)totalBytes - 32);
                                Vector128.StoreUnsafe(v2, ref dataRef, (nuint)totalBytes - 16);
                            }
                            offset = (nuint)totalBytes;
                        }
                    }

                    while (offset < (nuint)totalBytes)
                    {
                        Unsafe.Add(ref dataRef, offset++) = (byte)b;
                        Unsafe.Add(ref dataRef, offset++) = (byte)g;
                        Unsafe.Add(ref dataRef, offset++) = (byte)r;
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Initialize this PixelMap from a segment of another PixelMap.
        /// </summary>
        /// <param name="source">
        /// PixelMap to initialize from
        /// </param>
        /// <param name="rect">
        /// Bounding Rectangle to initialize from
        /// </param>
        /// <returns>
        /// The initialized PixelMap
        /// </returns>
        public PixelMap Init(PixelMap source, Rectangle rect)
        {
            if (source == null)
                DjvuExceptionUtil.ThrowArgumentNull(nameof(source), 
                    "The source PixelMap cannot be null. Please provide a valid PixelMap instance.");

            if (source.Width < 0 || source.Height < 0)
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(source), 
                    $"The source PixelMap has invalid dimensions (Width: {source.Width}, Height: {source.Height}). Dimensions cannot be negative.");
            
            // Circuit breaker for empty sources
            if (source.Width == 0 || source.Height == 0)
                return Init(rect.Height, rect.Width);

            if (source.Data == null)
                DjvuExceptionUtil.ThrowInvalidOperation(
                    $"The source PixelMap.Data array is null - probably uninitialized. Current state - Width: {source.Width}, Height: {source.Height}, Data: {source.Data}.");

            InitUninitialized(rect.Height, rect.Width);

            Rectangle rect2 = new Rectangle(0, 0, source.Width, source.Height);
            rect2.Intersect(rect2, rect);
            rect2.Translate(-rect.XMin, -rect.YMin);

            if (!rect2.Empty)
            {
                var pixel = CreateGPixelReference(0);
                var refPixel = source.CreateGPixelReference(0);

                for (int y = rect2.YMin; y < rect2.YMax; y++)
                {
                    pixel.SetOffset(y, rect2.XMin);
                    refPixel.SetOffset(y + rect.YMin, rect.XMin + rect2.XMin);

                    for (int x = rect2.XMax - rect2.XMin; x-- > 0; pixel.IncOffset(), refPixel.IncOffset())
                    {
                        pixel.CopyFrom(refPixel);
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Initialize this PixelMap from Bitmap.
        /// </summary>
        /// <param name="source">
        /// Bitmap to initialize from
        /// </param>
        /// <returns>
        /// The initialized PixelMap
        /// </returns>
        public unsafe PixelMap Init(ref Bitmap source)
        {
            if (Unsafe.IsNullRef(ref source))
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(source), $"{typeof(Bitmap).FullName} source reference is null.");
            }

            // TODO: When RLE memory compression is enabled we should check for: source.RleData == null
            if (source.Data == null && source.Height == 0 && source.Width == 0 && source.Border == 0)
                DjvuExceptionUtil.ThrowArgument( 
                    "The source Bitmap cannot be default instance. Please provide a valid, initialized Bitmap instance.",
                    nameof(source));

            if (source.Width < 0 || source.Height < 0)
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(source), 
                    $"The source Bitmap has invalid dimensions (Width: {source.Width}, Height: {source.Height}). Dimensions cannot be negative.");

            // Circuit breaker for empty sources
            if (source.Width == 0 || source.Height == 0)
                return Init(0, 0);

            // TODO: When RLE memory compression is enabled we should check for: source.RleData == null
            if (source.Data == null)
                DjvuExceptionUtil.ThrowInvalidOperation(
                    $"The source Bitmap.Data array is null - probably uninitialized. Current state - Width: {source.Width}, Height: {source.Height}, Data: {source.Data}.");

            InitUninitialized(source.Height, source.Width);

            if ((Height > 0) && (Width > 0))
            {
                if (source.Grays == 2)
                {
                    FillBitonal(source.Data, this.Data, Width, Height, source.BytesPerRow, Width * BytesPerPixel, source.RowOffset(0), 0);
                }
                else
                {
                    Pixel* localRamp = stackalloc Pixel[256];
                    Pixel* ramp = GenerateGrayRamp(source.Grays, localRamp);
                    
                    int w = Width;
                    int h = Height;
                    int bpp = BytesPerPixel;

                    sbyte[] srcData = source.Data;
                    sbyte[] dstData = this.Data;

                    int srcStride = source.BytesPerRow;
                    int dstStride = w * bpp;

                    int srcIdx = source.RowOffset(0);
                    int dstIdx = 0;

                    for (int y = 0; y < h; y++)
                    {
                        int sIdx = srcIdx;
                        int dIdx = dstIdx;
                        for (int x = 0; x < w; x++)
                        {
                            Pixel p = ramp[(byte)srcData[sIdx++]];
                            dstData[dIdx++] = p.Blue;
                            dstData[dIdx++] = p.Green;
                            dstData[dIdx++] = p.Red;
                        }
                        srcIdx += srcStride;
                        dstIdx += dstStride;
                    }
                }
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void FillBitonal(sbyte[] srcData, sbyte[] dstData, int width, int height, int srcStride, int dstStride, int srcIdx, int dstIdx)
        {
            ReadOnlySpan<sbyte> srcSpan = srcData;
            Span<sbyte> dstSpan = dstData;

            if (Avx512Vbmi.IsSupported && width >= 64)
            {
                ReadOnlySpan<byte> b0 = [ 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6, 7, 7, 7,
                    8, 8, 8, 9, 9, 9, 10, 10, 10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15,
                    16, 16, 16, 17, 17, 17, 18, 18, 18, 19, 19, 19, 20, 20, 20, 21 ];

                ReadOnlySpan<byte> b1 = [ 21, 21, 22, 22, 22, 23, 23, 23, 24, 24, 24, 25, 25, 25, 26, 26, 26,
                    27, 27, 27, 28, 28, 28, 29, 29, 29, 30, 30, 30, 31, 31, 31, 32, 32, 32, 33, 33, 33, 34, 34, 34,
                    35, 35, 35, 36, 36, 36, 37, 37, 37, 38, 38, 38, 39, 39, 39, 40, 40, 40, 41, 41, 41, 42, 42 ];

                ReadOnlySpan<byte> b2 = [ 42, 43, 43, 43, 44, 44, 44, 45, 45, 45, 46, 46, 46, 47, 47, 47, 48, 48, 48,
                    49, 49, 49, 50, 50, 50, 51, 51, 51, 52, 52, 52, 53, 53, 53, 54, 54, 54, 55, 55, 55, 56, 56, 56,
                    57, 57, 57, 58, 58, 58, 59, 59, 59, 60, 60, 60, 61, 61, 61, 62, 62, 62, 63, 63, 63 ];
                
                var idx0 = Vector512.Create<byte>(b0);
                var idx1 = Vector512.Create<byte>(b1);
                var idx2 = Vector512.Create<byte>(b2);

                int vEnd = width - 64;
                for (int y = 0; y < height; y++)
                {
                    ReadOnlySpan<sbyte> srcRow = srcSpan.Slice(srcIdx, width);
                    Span<sbyte> dstRow = dstSpan.Slice(dstIdx, width * 3);
                    int x = 0;
                    
                    for (; x <= vEnd; x += 64)
                    {
                        var vSrc = Vector512.Create(srcRow.Slice(x));
                        var vMask = Vector512.Equals(vSrc, Vector512<sbyte>.Zero).AsByte();

                        Avx512Vbmi.PermuteVar64x8(vMask, idx0).AsSByte().CopyTo(dstRow.Slice(x * 3));
                        Avx512Vbmi.PermuteVar64x8(vMask, idx1).AsSByte().CopyTo(dstRow.Slice(x * 3 + 64));
                        Avx512Vbmi.PermuteVar64x8(vMask, idx2).AsSByte().CopyTo(dstRow.Slice(x * 3 + 128));
                    }
                    
                    if (x < width)
                    {
                        x = width - 64;
                        var vSrc = Vector512.Create(srcRow.Slice(x));
                        var vMask = Vector512.Equals(vSrc, Vector512<sbyte>.Zero).AsByte();

                        Avx512Vbmi.PermuteVar64x8(vMask, idx0).AsSByte().CopyTo(dstRow.Slice(x * 3));
                        Avx512Vbmi.PermuteVar64x8(vMask, idx1).AsSByte().CopyTo(dstRow.Slice(x * 3 + 64));
                        Avx512Vbmi.PermuteVar64x8(vMask, idx2).AsSByte().CopyTo(dstRow.Slice(x * 3 + 128));
                    }
                    srcIdx += srcStride;
                    dstIdx += dstStride;
                }
            }
            else if (Avx2.IsSupported && width >= 32)
            {
                ReadOnlySpan<byte> b0 = [ 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10 ];
                ReadOnlySpan<byte> b1 = [ 10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15, 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5 ];
                ReadOnlySpan<byte> b2 = [ 5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10, 10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15 ];
                
                var idx0 = Vector256.Create<byte>(b0);
                var idx1 = Vector256.Create<byte>(b1);
                var idx2 = Vector256.Create<byte>(b2);

                int vEnd = width - 32;
                for (int y = 0; y < height; y++)
                {
                    ReadOnlySpan<sbyte> srcRow = srcSpan.Slice(srcIdx, width);
                    Span<sbyte> dstRow = dstSpan.Slice(dstIdx, width * 3);
                    int x = 0;

                    for (; x <= vEnd; x += 32)
                    {
                        var vSrc = Vector256.Create(srcRow.Slice(x));
                        var vMask = Vector256.Equals(vSrc, Vector256<sbyte>.Zero).AsByte();

                        var src0 = Avx2.Permute2x128(vMask.AsInt64(), vMask.AsInt64(), 0x00).AsByte();
                        var src1 = vMask;
                        var src2 = Avx2.Permute2x128(vMask.AsInt64(), vMask.AsInt64(), 0x11).AsByte();

                        Avx2.Shuffle(src0, idx0).AsSByte().CopyTo(dstRow.Slice(x * 3));
                        Avx2.Shuffle(src1, idx1).AsSByte().CopyTo(dstRow.Slice(x * 3 + 32));
                        Avx2.Shuffle(src2, idx2).AsSByte().CopyTo(dstRow.Slice(x * 3 + 64));
                    }
                    
                    if (x < width)
                    {
                        x = width - 32;
                        var vSrc = Vector256.Create(srcRow.Slice(x));
                        var vMask = Vector256.Equals(vSrc, Vector256<sbyte>.Zero).AsByte();

                        var src0 = Avx2.Permute2x128(vMask.AsInt64(), vMask.AsInt64(), 0x00).AsByte();
                        var src1 = vMask;
                        var src2 = Avx2.Permute2x128(vMask.AsInt64(), vMask.AsInt64(), 0x11).AsByte();

                        Avx2.Shuffle(src0, idx0).AsSByte().CopyTo(dstRow.Slice(x * 3));
                        Avx2.Shuffle(src1, idx1).AsSByte().CopyTo(dstRow.Slice(x * 3 + 32));
                        Avx2.Shuffle(src2, idx2).AsSByte().CopyTo(dstRow.Slice(x * 3 + 64));
                    }
                    srcIdx += srcStride;
                    dstIdx += dstStride;
                }
            }
            else if (Vector128.IsHardwareAccelerated && width >= 16)
            {
                ReadOnlySpan<byte> b0 = [ 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5 ];
                ReadOnlySpan<byte> b1 = [ 5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10 ];
                ReadOnlySpan<byte> b2 = [ 10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15 ];
                
                var idx0 = Vector128.Create<byte>(b0);
                var idx1 = Vector128.Create<byte>(b1);
                var idx2 = Vector128.Create<byte>(b2);

                int vEnd = width - 16;
                for (int y = 0; y < height; y++)
                {
                    ReadOnlySpan<sbyte> srcRow = srcSpan.Slice(srcIdx, width);
                    Span<sbyte> dstRow = dstSpan.Slice(dstIdx, width * 3);
                    int x = 0;

                    for (; x <= vEnd; x += 16)
                    {
                        var vSrc = Vector128.Create(srcRow.Slice(x));
                        var vMask = Vector128.Equals(vSrc, Vector128<sbyte>.Zero).AsByte();

                        Vector128.Shuffle(vMask, idx0).AsSByte().CopyTo(dstRow.Slice(x * 3));
                        Vector128.Shuffle(vMask, idx1).AsSByte().CopyTo(dstRow.Slice(x * 3 + 16));
                        Vector128.Shuffle(vMask, idx2).AsSByte().CopyTo(dstRow.Slice(x * 3 + 32));
                    }
                    
                    if (x < width)
                    {
                        x = width - 16;
                        var vSrc = Vector128.Create(srcRow.Slice(x));
                        var vMask = Vector128.Equals(vSrc, Vector128<sbyte>.Zero).AsByte();

                        Vector128.Shuffle(vMask, idx0).AsSByte().CopyTo(dstRow.Slice(x * 3));
                        Vector128.Shuffle(vMask, idx1).AsSByte().CopyTo(dstRow.Slice(x * 3 + 16));
                        Vector128.Shuffle(vMask, idx2).AsSByte().CopyTo(dstRow.Slice(x * 3 + 32));
                    }
                    srcIdx += srcStride;
                    dstIdx += dstStride;
                }
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    ReadOnlySpan<sbyte> srcRow = srcSpan.Slice(srcIdx, width);
                    Span<sbyte> dstRow = dstSpan.Slice(dstIdx, width * 3);
                    for (int x = 0; x < width; x++)
                    {
                        sbyte color = (srcRow[x] == 0) ? (sbyte)-1 : (sbyte)0;
                        dstRow[x * 3]     = color;
                        dstRow[x * 3 + 1] = color;
                        dstRow[x * 3 + 2] = color;
                    }
                    srcIdx += srcStride;
                    dstIdx += dstStride;
                }
            }
        }

        /// <summary>
        /// Initialize this PixelMap from PixelMap.
        /// </summary>
        /// <param name="source">
        /// PixelMap to initialize from
        /// </param>
        /// <returns>
        /// The initialized PixelMap
        /// </returns>
        public PixelMap Init(PixelMap source)
        {
            if (source == null)
                DjvuExceptionUtil.ThrowArgumentNull(nameof(source), 
                    "The source PixelMap cannot be null. Please provide a valid PixelMap instance.");

            if (source.Width < 0 || source.Height < 0)
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(source), 
                    $"The source PixelMap has invalid dimensions (Width: {source.Width}, Height: {source.Height}). Dimensions cannot be negative.");

            // Circuit breaker for empty sources
            if (source.Width == 0 || source.Height == 0)
                return Init(0, 0);

            if (source.Data == null)
                DjvuExceptionUtil.ThrowInvalidOperation(
                    $"The source PixelMap.Data array is null - probably uninitialized. Current state - Width: {source.Width}, Height: {source.Height}, Data: {source.Data}.");

            InitUninitialized(source.Height, source.Width);

            if ((Height > 0) && (Width > 0))
            {
                Buffer.BlockCopy(source.Data, 0, this.Data, 0, Width * Height * BytesPerPixel);
            }

            return this;
        }


        /// <summary>
        /// Fills an array of pixels from the specified values.
        /// </summary>
        /// <param name="x">
        /// The x-coordinate of the bottom-left corner of the region of pixels
        /// </param>
        /// <param name="y">
        /// The y-coordinate of the bottom-left corner of the region of pixels
        /// </param>
        /// <param name="width">
        /// The width of the region of pixels
        /// </param>
        /// <param name="height">
        /// The height of the region of pixels
        /// </param>
        /// <param name="pixels">
        /// The array of pixels
        /// </param>
        /// <param name="offset">
        /// The offset into the pixel array
        /// </param>
        /// <param name="scanSize">
        /// The distance from one row of pixels to the next in the array
        /// </param>
        /// <remarks>
        /// See <see cref="Map"/> class remarks for architectural limits regarding maximum dimensions.
        /// </remarks>
        public void FillRgbPixels(int x, int y, int width, int height, int[] pixels, int offset, int scanSize)
        {
            if (pixels == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(pixels));
            }

            // Reference: DjVuLibre explicitly throws on negative dimensions via (unsigned short) casting
            // but explicitly supports 0 via (npix > 0) allocation guards. (See GPixmap.cpp / GBitmap.cpp).
            if (width < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width cannot be negative.");
            }

            if (height < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height cannot be negative.");
            }

            if (x < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(x), x, "X coordinate cannot be negative.");
            }

            if (y < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(y), y, "Y coordinate cannot be negative.");
            }

            long right = (long)x + width;
            if (right > Width || right > int.MaxValue)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Region exceeds horizontal bounds.");
            }

            long bottom = (long)y + height;
            if (bottom > Height || bottom > int.MaxValue)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Region exceeds vertical bounds.");
            }

            if (offset < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(offset), offset, "Offset cannot be negative.");
            }

            if (scanSize < width)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(scanSize), scanSize, "Scansize cannot be smaller than width.");
            }

            // Calculate required buffer space to prevent buffer over-writes in the nested PixelReference loops.
            long requiredSpace = (height > 0) ? (long)offset + ((long)(height - 1) * scanSize) + width : offset;
            if (requiredSpace > pixels.Length)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"Destination buffer too small. Required: {requiredSpace}, Actual: {pixels.Length}.");
            }

            new PixelReference(this, 0).FillRgbPixels(x, y, width, height, pixels, offset, scanSize);
        }

        /// <summary>
        /// Create a PixelReference (a pixel iterator) that refers to this map
        /// starting at the specified offset.
        /// </summary>
        /// <param name="offset">
        /// Position of the first pixel to reference
        /// </param>
        /// <returns>
        /// The newly created PixelReference
        /// </returns>
        public IPixelReference CreateGPixelReference(int offset)
        {
            return new PixelReference(this, offset);
        }

        /// <summary>
        /// Create a PixelReference (a pixel iterator) that refers to this map
        /// starting at the specified position.
        /// </summary>
        /// <param name="row">initial vertical position
        /// </param>
        /// <param name="column">initial horizontal position
        ///
        /// </param>
        /// <returns> the newly created PixelReference
        /// </returns>
        public IPixelReference CreateGPixelReference(int row, int column)
        {
            return new PixelReference(this, row, column);
        }

        /// <summary>
        /// Query the getRowSize.
        /// </summary>
        /// <returns>
        /// Row size in bytes.
        /// </returns>
        public int GetRowSize()
        {
            return Width;
        }

        /// <summary> Query the start offset of a row.
        ///
        /// </summary>
        /// <param name="row">the row to query
        ///
        /// </param>
        /// <returns> the offset to the pixel data
        /// </returns>
        public int RowOffset(int row)
        {
            return row * GetRowSize();
        }

        /// <summary>
        /// Convert the pixel to 24 bit color.
        /// </summary>
        /// <returns>
        /// The converted pixel
        /// </returns>
        public IPixel PixelRamp(IPixelReference pixel)
        {
            return pixel.ToPixel();
        }

        /// <summary>
        /// Draw the foreground layer onto this background image.
        /// 
        /// Architectural Note: 
        /// This implementation significantly diverges from and optimizes the DjVuLibre C++ reference (GPixmap::stencil).
        /// The C++ stencil hot loop strictly supports integer upsampling (via the supersample parameter). To achieve 
        /// fractional downsampling, the C++ architecture bypasses its stencil loop entirely, allocates a new image buffer, 
        /// performs a full pre-warping geometric scaling pass, and then executes an integer upsample blit.
        /// 
        /// In DjvuNet, we introduced the subSample parameter and integrated a Bresenham-style step-and-fraction 
        /// precalculated advancement algorithm directly into the hot loop. This perfectly guarantees mathematical 
        /// algorithmic identity with C++ upsampling when subSample=1, while safely achieving dynamic fractional 
        /// downsampling on-the-fly without requiring any intermediate buffer allocations or pre-warping scaling passes.
        /// </summary>
        /// <param name="mask">
        /// the mask layer
        /// </param>
        /// <param name="foregroundMap">
        /// the foreground colors
        /// </param>
        /// <param name="superSample">
        /// rate to upsample the foreground colors
        /// </param>
        /// <param name="subSample">
        /// rate to subsample the foreground colors
        /// </param>
        /// <param name="bounds">
        /// the target rectangle
        /// </param>
        /// <param name="gamma">
        /// color correction factor
        /// </param>
        /// <throws>
        /// <see cref="DjvuNet.Errors.DjvuArgumentOutOfRangeException"/>  if the specified bounds are not contained in the page
        /// </throws>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe void Stencil(ref Bitmap mask, PixelMap foregroundMap, int superSample,
        int subSample, Rectangle bounds, double gamma)
        {
            if (Unsafe.IsNullRef(ref mask))
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(mask), $"{typeof(Bitmap).FullName} mask reference is null.");
            }

            if (foregroundMap == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(foregroundMap), $"{typeof(PixelMap).FullName} foregroundMap reference is null.");
            }

            // Check arguments
            int width = (((foregroundMap.Width * superSample) + subSample - 1) / subSample);
            int height = (((foregroundMap.Height * superSample) + subSample - 1) / subSample);
            Rectangle rect = new Rectangle(0, 0, width, height);

            if (!bounds.Empty)
            {
                if ((bounds.XMin < rect.XMin) || (bounds.YMin < rect.YMin) || (bounds.XMax > rect.XMax) ||
                    (bounds.YMax > rect.YMax))
                {
                    DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(bounds),
                        "Rectangle out of bounds" + "bounds=(" + bounds.XMin + "," + bounds.YMin +
                                                "," + bounds.XMax + "," + bounds.YMax + "),rect=(" + rect.XMin + "," +
                                                rect.YMin + "," + rect.XMax + "," + rect.YMax + ")");
                }

                rect = bounds;
            }

            // Compute number of rows
            int xrows = Height;

            if (mask.Height < xrows)
            {
                xrows = mask.Height;
            }

            if (rect.Height < xrows)
            {
                xrows = rect.Height;
            }

            // Compute number of columns
            int xcolumns = Width;

            if (mask.Width < xcolumns)
            {
                xcolumns = mask.Width;
            }

            if (rect.Width < xcolumns)
            {
                xcolumns = rect.Width;
            }

            // Precompute multiplier map
            int maxGray = mask.Grays - 1;
            int* multiplier = stackalloc int[maxGray];

            for (int i = 1; i < maxGray; i++)
            {
                multiplier[i] = (0x10000 * i) / maxGray;
            }

            // Prepare color correction table
            int[] gtable = GetGammaCorrection(gamma);

            int fgy = (rect.YMin * subSample) / superSample;
            int fgy1Scaled = rect.YMin * subSample - superSample * fgy;

            if (fgy1Scaled < 0)
            {
                fgy--;
                fgy1Scaled += superSample;
            }

            int fgxz = (rect.XMin * subSample) / superSample;
            int fgx1zScaled = rect.XMin * subSample - superSample * fgxz;

            if (fgx1zScaled < 0)
            {
                fgxz--;
                fgx1zScaled += superSample;
            }

            // Bresenham-style optimization to extract division from the hot loop while 
            // safely handling extreme downsampling ratios (subSample > superSample).
            // This maintains exactly one IF branch per pixel iteration, identical to the 
            // C++ DjVuLibre implementation when upsampling, but correctly handles fractional stepping.
            int stepAdvanceX = (subSample / superSample) * BytesPerPixel;
            int fracAdvanceX = subSample % superSample;
            int stepAdvanceY = subSample / superSample;
            int fracAdvanceY = subSample % superSample;

            fixed (sbyte* pData = Data)
            fixed (sbyte* pFgData = foregroundMap.Data)
            fixed (int* gTableLocation = gtable)
            {
                // Loop over rows
                for (int y = 0; y < xrows; y++)
                {
                    int fgx1Scaled = fgx1zScaled;
                    
                    int dstRowOffset = RowOffset(y);
                    int fgRowOffset = foregroundMap.RowOffset(fgy);
                    sbyte* maskRow = mask.GetRow(y);

                    sbyte* dstPixel = pData + (dstRowOffset * BytesPerPixel);
                    sbyte* fgPixel = pFgData + ((fgRowOffset + fgxz) * BytesPerPixel);

                    for (int x = 0; x < xcolumns; x++)
                    {
                        int srcPixel = (byte)maskRow[x];

                        // Perform pixel operation
                        if (srcPixel > 0)
                        {
                            byte fgB = (byte)fgPixel[BlueOffset];
                            byte fgG = (byte)fgPixel[GreenOffset];
                            byte fgR = (byte)fgPixel[RedOffset];

                            if (srcPixel >= maxGray)
                            {
                                dstPixel[BlueOffset] = unchecked((sbyte)gTableLocation[fgB]);
                                dstPixel[GreenOffset] = unchecked((sbyte)gTableLocation[fgG]);
                                dstPixel[RedOffset] = unchecked((sbyte)gTableLocation[fgR]);
                            }
                            else
                            {
                                int level = multiplier[srcPixel];
                                
                                byte dstB = (byte)dstPixel[BlueOffset];
                                byte dstG = (byte)dstPixel[GreenOffset];
                                byte dstR = (byte)dstPixel[RedOffset];

                                // Use exact C++ algebraic truncation to prevent off-by-1 byte rounding errors
                                dstPixel[BlueOffset] = unchecked((sbyte)((int)dstB - ((((int)dstB - gTableLocation[fgB]) * level) >> 16)));
                                dstPixel[GreenOffset] = unchecked((sbyte)((int)dstG - ((((int)dstG - gTableLocation[fgG]) * level) >> 16)));
                                dstPixel[RedOffset] = unchecked((sbyte)((int)dstR - ((((int)dstR - gTableLocation[fgR]) * level) >> 16)));
                            }
                        }

                        dstPixel += BytesPerPixel;

                        // Next column
                        fgPixel += stepAdvanceX;
                        fgx1Scaled += fracAdvanceX;
                        if (fgx1Scaled >= superSample)
                        {
                            fgx1Scaled -= superSample;
                            fgPixel += BytesPerPixel;
                        }
                    }

                    // Next line
                    fgy += stepAdvanceY;
                    fgy1Scaled += fracAdvanceY;
                    if (fgy1Scaled >= superSample)
                    {
                        fgy1Scaled -= superSample;
                        fgy++;
                    }
                }
            }
        }

        /// <summary>
        /// Copy this image with a translated origin.
        /// </summary>
        /// <param name="dx">
        /// horizontal distance to translate
        /// </param>
        /// <param name="dy">
        /// vertical distance to translate
        /// </param>
        /// <param name="retVal">
        /// an old image to try and reuse for the return value
        /// </param>
        /// <returns> the translated image
        /// </returns>
        public PixelMap Translate(int dx, int dy, PixelMap retVal)
        {
            if (retVal == null || retVal.Width != Width || retVal.Height != Height)
            {
                retVal = new PixelMap().Init(Height, Width);
            }

            retVal.Fill(this, -dx, -dy);

            return retVal;
        }

        /// <summary>
        /// Initialize this PixelMap with a preallocated buffer.
        /// </summary>
        /// <param name="data">
        /// buffer to use
        /// </param>
        /// <param name="rows">
        /// number of rows
        /// </param>
        /// <param name="columns">
        /// number of columns
        /// </param>
        /// <returns> the initialized PixelMap
        /// </returns>
        public PixelMap Init(sbyte[] data, int rows, int columns)
        {
            if (data == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(data), "Data array cannot be null when initializing PixelMap.");
            }

            SetHeight(rows);
            SetWidth(columns);

            long expectedSize = (long)columns * rows * 3;
            if (data.Length < expectedSize)
            {
                DjvuExceptionUtil.ThrowArgument($"Data array size {data.Length} is insufficient for a {columns}x{rows} pixel map. Expected at least {expectedSize} elements.", nameof(data));
            }

            this.Data = data;

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe PixelMap Init(IDjvuReader reader)
        {
            // Read header
            bool raw = false;
            bool grey = false;
            ushort magic = reader.ReadUInt16BigEndian();
            Bitmap bm = default;
            switch (magic)
            {
                case (('P' << 8) + '2'):
                    grey = true;
                    break;
                case (('P' << 8) + '3'):
                    break;
                case (('P' << 8) + '5'):
                    raw = grey = true;
                    break;
                case (('P' << 8) + '6'):
                    raw = true;
                    break;
                case ('P' << 8) + '1':
                case ('P' << 8) + '4':
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    bm = Bitmap.CreateBitmap(reader.BaseStream);
                    Init(ref bm);
                    return this;
                default:
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    //JPEGDecoder::decode(bs, *this);
                    return this;

            }

            // Read image size
            char lookahead = '\n';
            int bytesperrow = 0;
            int bytespercomp = 1;
            uint acolumns = ParserUtil.ReadInteger(ref lookahead, reader.BaseStream);
            uint arows = ParserUtil.ReadInteger(ref lookahead, reader.BaseStream);
            uint maxval = ParserUtil.ReadInteger(ref lookahead, reader.BaseStream);

            if (maxval > 65535)
            {
                DjvuExceptionUtil.ThrowFormatException("Cannot read PPM data with depth greater than 48 bits.");
            }

            if (maxval > 255)
            {
                bytespercomp = 2;
            }

            Init((int)arows, (int)acolumns, Pixel.BlackPixel);

            // Prepare ramp
            byte[] ramp = null;
            int maxbin = 1 << (8 * bytespercomp);
            Array.Resize(ref ramp, maxbin - 1);

            for (int i = 0; i < maxbin; i++)
            {
                ramp[i] = (byte) (i < maxval ? (255 * i + maxval / 2) / maxval : 255);
            }

            fixed (byte* pramp = ramp)
            fixed (sbyte* pData = Data)
            {
                sbyte* bramp = (sbyte*)pramp;
                // Read image data
                if (raw && grey)
                {
                    bytesperrow = Width * bytespercomp;
                    byte[] line = new  byte[bytesperrow];

                    for (int y = Height - 1; y >= 0; y--)
                    {
                        fixed (byte* pg = line)
                        {
                            byte* g = pg;
                            Pixel* p = (Pixel*)pData + y * bytesperrow;
                            if (reader.Read(line, 0, bytesperrow) < bytesperrow)
                            {
                                DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream");
                            }

                            if (bytespercomp <= 1)
                            {
                                for (int x = 0; x < Width; x += 1, g += 1)
                                {
                                    p[x].Red = p[x].Green = p[x].Blue = bramp[g[0]];
                                }
                            }
                            else
                            {
                                for (int x = 0; x < Width; x += 1, g += 2)
                                {
                                    p[x].Red = p[x].Green = p[x].Blue = bramp[g[0] * 256 + g[1]];
                                }
                            }
                        }
                    }
                }
                else if (raw)
                {
                    bytesperrow = Width * bytespercomp * 3;
                    byte[] line = new byte[bytesperrow];

                    for (int y = Height - 1; y >= 0; y--)
                    {
                        Pixel* p = (Pixel*)pData + y * bytesperrow;
                        fixed (byte* prgb = line)
                        {
                            byte* rgb = prgb;
                            if (reader.Read(line, 0, bytesperrow) < bytesperrow)
                            {
                                DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream");
                            }

                            if (bytespercomp <= 1)
                            {
                                for (int x = 0; x < Width; x += 1, rgb += 3)
                                {
                                    p[x].Red = bramp[rgb[0]];
                                    p[x].Green = bramp[rgb[1]];
                                    p[x].Blue = bramp[rgb[2]];
                                }
                            }
                            else
                            {
                                for (int x = 0; x < Width; x += 1, rgb += 6)
                                {
                                    p[x].Red = bramp[rgb[0] * 256 + rgb[1]];
                                    p[x].Green = bramp[rgb[2] * 256 + rgb[3]];
                                    p[x].Blue = bramp[rgb[4] * 256 + rgb[5]];
                                }
                            }
                        }
                    }
                }
                else
                {
                    bytesperrow = Width * bytespercomp * 3;

                    for (int y = Height - 1; y >= 0; y--)
                    {
                        Pixel* p = (Pixel*)pData + y * bytesperrow;

                        for (int x = 0; x < Width; x++)
                        {
                            if (grey)
                            {
                                p[x].Green = p[x].Blue = p[x].Red = bramp[(int)ParserUtil.ReadInteger(ref lookahead, reader.BaseStream)];
                            }
                            else
                            {
                                p[x].Red = bramp[(int)ParserUtil.ReadInteger(ref lookahead, reader.BaseStream)];
                                p[x].Green = bramp[(int)ParserUtil.ReadInteger(ref lookahead, reader.BaseStream)];
                                p[x].Blue = bramp[(int)ParserUtil.ReadInteger(ref lookahead, reader.BaseStream)];
                            }
                        }
                    }
                }
            }

            return this;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Creates or retrieves a cached multiplier array to use when attenuating.
        /// </summary>
        /// <returns>
        /// Attenuation array
        /// </returns>
        internal static int[] GetMultiplier(int maxgray)
        {
            int[] retval = (int[])_multiplierRefArray[maxgray];
            if (retval == null)
            {
                retval = new int[maxgray];

                for (int i = 0; i < maxgray; i++)
                {
                    retval[i] = 0x10000 - ((i << 16) / maxgray);
                }

                _multiplierRefArray[maxgray] = retval;
            }
            return retval;
        }

        #endregion Protected Methods

        #region Object Overrides

        public override string ToString()
        {
            return $"{base.ToString()}: Width: {Width}, Height: {Height}, Data: {(Data == null ? "null" : Data.Length.ToString())} sbytes.";
        }

        #endregion Object Overrides
    }
}
