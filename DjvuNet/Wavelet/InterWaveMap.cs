using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using DjvuNet.Errors;
using DjvuNet.Graphics;

namespace DjvuNet.Wavelet
{
    /// <summary>
    /// This class represents structured wavelet data.
    /// </summary>
    public class InterWaveMap : IInterWaveMap
    {
        #region Public Properties

        /// <summary>
        /// Image height in blocks
        /// </summary>
        public int BlockHeight
        {
            get;
            internal set;
        }

        /// <summary>
        /// Array of image blocks
        /// </summary>
        public InterWaveBlock[] Blocks
        {
            get;
            internal set;
        }

        /// <summary>
        /// Width of image in blocks
        /// </summary>
        public int BlockWidth
        {
            get;
            internal set;
        }

        /// <summary>
        /// Image height
        /// </summary>
        public int Height
        {
            get;
            internal set;
        }

        /// <summary>
        /// Image width
        /// </summary>
        public int Width
        {
            get;
            internal set;
        }

        #endregion Public Properties

        #region Public Fields

        /// <summary>
        /// Gets or sets the Nb value
        /// </summary>
        public int BlockNumber;

        /// <summary>
        /// Gets or sets the top value
        /// </summary>
        public int Top;

        #endregion Public Fields

        #region Constructors

        /// <summary> Creates a new Map object.</summary>
        public InterWaveMap()
        {
        }

        public InterWaveMap(int width, int height)
        {
            if (width <= 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width must be greater than zero.");
            }
            if (height <= 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height must be greater than zero.");
            }

            Width = width;
            Height = height;

            // Align dimensions to the 32x32 macroblock grid
            BlockWidth = ((width + 0x20) - 1) & unchecked((int)0xffffffe0);
            BlockHeight = ((height + 0x20) - 1) & unchecked((int)0xffffffe0);

            // Calculate total pixels, detecting integer overflow from malicious dimensions
            long totalPixels = (long)BlockWidth * (long)BlockHeight;
            if (totalPixels > Array.MaxLength || totalPixels <= 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), $"{width}x{height}",
                    "Image dimensions result in an invalid buffer size (less than zero or exceeding Array.MaxLength).");
            }

            long totalBlocks = totalPixels / 1024;
            BlockNumber = (int)totalBlocks;
            Blocks = new InterWaveBlock[BlockNumber];

            for (int i = 0; i < Blocks.Length; i++)
            {
                Blocks[i] = new InterWaveBlock();
            }
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Duplicate InterWaveMap
        /// </summary>
        /// <returns></returns>
        public InterWaveMap Duplicate()
        {
            InterWaveMap retval = null;

            try
            {
                retval = new InterWaveMap
                {
                    BlockHeight = BlockHeight,
                    Blocks = (InterWaveBlock[])Blocks.Clone(),
                    BlockWidth = BlockWidth,
                    Height = Height,
                    Width = Width,
                    BlockNumber = BlockNumber,
                    Top = Top
                };

                //IWBlock[] blocks = (IWBlock[])this.Blocks.Clone();
                //((IWMap)retval).Blocks = blocks;

                for (int i = 0; i < BlockNumber; i++)
                {
                    retval.Blocks[i] = Blocks[i].Duplicate();
                }
            }
            catch
            {
            }

            return retval;
        }



        /// <summary>
        /// Returns bucket count.
        /// </summary>
        /// <returns></returns>
        public int GetBucketCount()
        {
            int buckets = 0;

            for (int blockno = 0; blockno < BlockNumber; blockno++)
            {
                for (int buckno = 0; buckno < 64; buckno++)
                {
                    if (Blocks[blockno].GetBlock(buckno) != null)
                    {
                        buckets++;
                    }
                }
            }

            return buckets;
        }

        /// <summary>
        /// Extracted internal method to build the unified spatial data array from the sparse blocks.
        /// This provides the structural boundary tested prior to SIMD unified memory refactoring.
        /// 
        /// TODO: This logic is mathematically identical to the extracted MapLiftBlock method.
        /// It is temporarily left intact utilizing Array.Copy until we apply SIMD optimizations to the lifting phase.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal short[] BuildUnifiedData()
        {
            short[] data16 = new short[BlockWidth * BlockHeight];
            short[] liftblock = new short[1024];
            int pidx = 0;
            InterWaveBlock[] blocks = Blocks;
            int blockidx = 0;
            int ppidx = 0;

            for (int i = 0; i < BlockHeight; i += 32, pidx += (32 * BlockWidth))
            {
                for (int j = 0; j < BlockWidth; j += 32)
                {
                    // when passed to WriteLiftBlock liftblock should contain zeros only
                    blocks[blockidx].WriteLiftBlock(liftblock, 0, 64);
                    blockidx++;

                    ppidx = pidx + j;

                    for (int ii = 0, p1idx = 0; ii++ < 32; p1idx += 32, ppidx += BlockWidth)
                    {
                        Array.Copy(liftblock, p1idx, data16, ppidx, 32);
                    }
                }
            }
            return data16;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe short[] DecodeToSpatial(bool fast)
        {
            short[] data16 = BuildUnifiedData();

            fixed (short* pData = data16)
            {
                if (fast)
                {
                    InterWaveTransform.Backward(pData, Width, Height, BlockWidth, 32, 2);
                    UpsampleFast2x2(pData, 0, BlockHeight, 0, BlockWidth, BlockWidth);
                }
                else
                {
                    InterWaveTransform.Backward(pData, Width, Height, BlockWidth, 32, 1);
                }
            }

            return data16;
        }

        public unsafe void ImageScalar(int index, sbyte[] img8, int rowSize, int pixelSeparation, bool fast)
        {
            short[] data16 = DecodeToSpatial(fast);

            fixed (short* pData = data16)
            fixed (sbyte* pImg8 = img8)
            {
                UnpackPixelsScalar(pData, pImg8, Height, Width, index, rowSize, pixelSeparation, BlockWidth);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void UnpackPixelsAvx2(short* pData, sbyte* pImg8, int height, int targetWidth, int index, int rowSize, int pixelDist, int dataw)
        {
            Vector256<short> v32 = Vector256.Create((short)32);
            int width32 = targetWidth & ~31;
            int width16 = targetWidth & ~15;
            sbyte* temp32 = stackalloc sbyte[32];

            // Shuffle masks for interleaving bytes
            Vector128<sbyte> shuffle1Mask128 = Vector128.Create((ReadOnlySpan<sbyte>)[0, -1, -1, 1, -1, -1, 2, -1, -1, 3, -1, -1, 4, -1, -1, 5]);
            Vector128<sbyte> shuffle2Mask128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, -1, 6, -1, -1, 7, -1, -1, 8, -1, -1, 9, -1, -1, 10, -1]);
            Vector128<sbyte> shuffle3Mask128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, 11, -1, -1, 12, -1, -1, 13, -1, -1, 14, -1, -1, 15, -1, -1]);

            // Clear masks for zeroing target bytes before logical OR
            Vector128<sbyte> clear1Mask128 = Vector128.Create((ReadOnlySpan<sbyte>)[0, -1, -1, 0, -1, -1, 0, -1, -1, 0, -1, -1, 0, -1, -1, 0]);
            Vector128<sbyte> clear2Mask128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, -1, 0, -1, -1, 0, -1, -1, 0, -1, -1, 0, -1, -1, 0, -1]);
            Vector128<sbyte> clear3Mask128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, 0, -1, -1, 0, -1, -1, 0, -1, -1, 0, -1, -1, 0, -1, -1]);

            for (int i = 0, pidx = 0, ridx = index;
                 i++ < height;
                 ridx += rowSize, pidx += dataw)
            {
                int j = 0;
                int pixidx = ridx;

                for (; j < width32; j += 32)
                {
                    int readOffset = pidx + j;
                    Vector256<short> v1 = Avx.LoadVector256(pData + readOffset);
                    Vector256<short> v2 = Avx.LoadVector256(pData + readOffset + 16);

                    v1 = Avx2.Add(v1, v32);
                    v2 = Avx2.Add(v2, v32);

                    v1 = Avx2.ShiftRightArithmetic(v1, 6);
                    v2 = Avx2.ShiftRightArithmetic(v2, 6);

                    Vector256<sbyte> packed = Avx2.PackSignedSaturate(v1, v2);
                    Vector256<sbyte> narrowed = Avx2.Permute4x64(packed.AsInt64(), 0xD8).AsSByte();

                    if (pixelDist == 1)
                    {
                        Avx.Store(pImg8 + pixidx, narrowed);
                        pixidx += 32;
                    }
                    else if (pixelDist == 3)
                    {
                        Vector128<sbyte> yLow = narrowed.GetLower();
                        Vector128<sbyte> m1 = Sse2.LoadVector128(pImg8 + pixidx);
                        Vector128<sbyte> m2 = Sse2.LoadVector128(pImg8 + pixidx + 16);
                        Vector128<sbyte> m3 = Sse2.LoadVector128(pImg8 + pixidx + 32);

                        m1 = Sse2.Or(Sse2.And(m1, clear1Mask128), Ssse3.Shuffle(yLow, shuffle1Mask128));
                        m2 = Sse2.Or(Sse2.And(m2, clear2Mask128), Ssse3.Shuffle(yLow, shuffle2Mask128));
                        m3 = Sse2.Or(Sse2.And(m3, clear3Mask128), Ssse3.Shuffle(yLow, shuffle3Mask128));

                        Sse2.Store(pImg8 + pixidx, m1);
                        Sse2.Store(pImg8 + pixidx + 16, m2);
                        Sse2.Store(pImg8 + pixidx + 32, m3);

                        Vector128<sbyte> yHigh = narrowed.GetUpper();
                        m1 = Sse2.LoadVector128(pImg8 + pixidx + 48);
                        m2 = Sse2.LoadVector128(pImg8 + pixidx + 64);
                        m3 = Sse2.LoadVector128(pImg8 + pixidx + 80);

                        m1 = Sse2.Or(Sse2.And(m1, clear1Mask128), Ssse3.Shuffle(yHigh, shuffle1Mask128));
                        m2 = Sse2.Or(Sse2.And(m2, clear2Mask128), Ssse3.Shuffle(yHigh, shuffle2Mask128));
                        m3 = Sse2.Or(Sse2.And(m3, clear3Mask128), Ssse3.Shuffle(yHigh, shuffle3Mask128));

                        Sse2.Store(pImg8 + pixidx + 48, m1);
                        Sse2.Store(pImg8 + pixidx + 64, m2);
                        Sse2.Store(pImg8 + pixidx + 80, m3);

                        pixidx += 96;
                    }
                    else
                    {
                        narrowed.Store(temp32);
                        for (int k = 0; k < 32; k++)
                        {
                            pImg8[pixidx] = temp32[k];
                            pixidx += pixelDist;
                        }
                    }
                }

                if (j < width16)
                {
                    int readOffset = pidx + j;
                    Vector128<short> v1 = Sse2.LoadVector128(pData + readOffset);
                    Vector128<short> v2 = Sse2.LoadVector128(pData + readOffset + 8);

                    v1 = Sse2.ShiftRightArithmetic(Sse2.Add(v1, v32.GetLower()), 6);
                    v2 = Sse2.ShiftRightArithmetic(Sse2.Add(v2, v32.GetLower()), 6);

                    Vector128<sbyte> narrowed = Sse2.PackSignedSaturate(v1, v2);

                    if (pixelDist == 1)
                    {
                        Sse2.Store(pImg8 + pixidx, narrowed);
                        pixidx += 16;
                    }
                    else if (pixelDist == 3)
                    {
                        Vector128<sbyte> m1 = Sse2.LoadVector128(pImg8 + pixidx);
                        Vector128<sbyte> m2 = Sse2.LoadVector128(pImg8 + pixidx + 16);
                        Vector128<sbyte> m3 = Sse2.LoadVector128(pImg8 + pixidx + 32);

                        m1 = Sse2.Or(Sse2.And(m1, clear1Mask128), Ssse3.Shuffle(narrowed, shuffle1Mask128));
                        m2 = Sse2.Or(Sse2.And(m2, clear2Mask128), Ssse3.Shuffle(narrowed, shuffle2Mask128));
                        m3 = Sse2.Or(Sse2.And(m3, clear3Mask128), Ssse3.Shuffle(narrowed, shuffle3Mask128));

                        Sse2.Store(pImg8 + pixidx, m1);
                        Sse2.Store(pImg8 + pixidx + 16, m2);
                        Sse2.Store(pImg8 + pixidx + 32, m3);

                        pixidx += 48;
                    }
                    else
                    {
                        narrowed.Store(temp32);
                        for (int k = 0; k < 16; k++)
                        {
                            pImg8[pixidx] = temp32[k];
                            pixidx += pixelDist;
                        }
                    }
                    j += 16;
                }

                UnpackRowScalar(pData + pidx, pImg8, targetWidth, j, pixidx, pixelDist);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void UnpackPixelsSsse3(short* pData, sbyte* pImg8, int height, int targetWidth, int index, int rowSize, int pixelDist, int dataw)
        {
            Vector128<short> v32 = Vector128.Create((short)32);
            int width16 = targetWidth & ~15;
            sbyte* temp16 = stackalloc sbyte[16];

            // Shuffle masks for interleaving bytes
            Vector128<sbyte> shuffle1Mask128 = Vector128.Create((ReadOnlySpan<sbyte>)[0, -1, -1, 1, -1, -1, 2, -1, -1, 3, -1, -1, 4, -1, -1, 5]);
            Vector128<sbyte> shuffle2Mask128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, -1, 6, -1, -1, 7, -1, -1, 8, -1, -1, 9, -1, -1, 10, -1]);
            Vector128<sbyte> shuffle3Mask128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, 11, -1, -1, 12, -1, -1, 13, -1, -1, 14, -1, -1, 15, -1, -1]);

            // Clear masks for zeroing target bytes before logical OR
            Vector128<sbyte> clear1Mask128 = Vector128.Create((ReadOnlySpan<sbyte>)[0, -1, -1, 0, -1, -1, 0, -1, -1, 0, -1, -1, 0, -1, -1, 0]);
            Vector128<sbyte> clear2Mask128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, -1, 0, -1, -1, 0, -1, -1, 0, -1, -1, 0, -1, -1, 0, -1]);
            Vector128<sbyte> clear3Mask128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, 0, -1, -1, 0, -1, -1, 0, -1, -1, 0, -1, -1, 0, -1, -1]);

            for (int i = 0, pidx = 0, ridx = index;
                 i++ < height;
                 ridx += rowSize, pidx += dataw)
            {
                int j = 0;
                int pixidx = ridx;

                for (; j < width16; j += 16)
                {
                    int readOffset = pidx + j;
                    Vector128<short> v1 = Vector128.Load(pData + readOffset);
                    Vector128<short> v2 = Vector128.Load(pData + readOffset + 8);

                    v1 = Vector128.Add(v1, v32);
                    v2 = Vector128.Add(v2, v32);

                    v1 = Vector128.ShiftRightArithmetic(v1, 6);
                    v2 = Vector128.ShiftRightArithmetic(v2, 6);

                    Vector128<sbyte> narrowed = Vector128.NarrowWithSaturation(v1, v2);

                    if (pixelDist == 1)
                    {
                        narrowed.Store(pImg8 + pixidx);
                        pixidx += 16;
                    }
                    else if (pixelDist == 3)
                    {
                        Vector128<sbyte> m1 = Sse2.LoadVector128(pImg8 + pixidx);
                        Vector128<sbyte> m2 = Sse2.LoadVector128(pImg8 + pixidx + 16);
                        Vector128<sbyte> m3 = Sse2.LoadVector128(pImg8 + pixidx + 32);

                        m1 = Sse2.Or(Sse2.And(m1, clear1Mask128), Ssse3.Shuffle(narrowed, shuffle1Mask128));
                        m2 = Sse2.Or(Sse2.And(m2, clear2Mask128), Ssse3.Shuffle(narrowed, shuffle2Mask128));
                        m3 = Sse2.Or(Sse2.And(m3, clear3Mask128), Ssse3.Shuffle(narrowed, shuffle3Mask128));

                        Sse2.Store(pImg8 + pixidx, m1);
                        Sse2.Store(pImg8 + pixidx + 16, m2);
                        Sse2.Store(pImg8 + pixidx + 32, m3);

                        pixidx += 48;
                    }
                    else
                    {
                        narrowed.Store(temp16);
                        for (int k = 0; k < 16; k++)
                        {
                            pImg8[pixidx] = temp16[k];
                            pixidx += pixelDist;
                        }
                    }
                }

                UnpackRowScalar(pData + pidx, pImg8, targetWidth, j, pixidx, pixelDist);
            }
        }

        /// <summary>
        /// Add docs
        /// </summary>
        /// <param name="index"></param>
        /// <param name="img8"></param>
        /// <param name="rowSize"></param>
        /// <param name="pixelSeparation"></param>
        /// <param name="fast"></param>
        public unsafe void Image(int index, sbyte[] img8, int rowSize, int pixelSeparation, bool fast)
        {
            short[] data16 = DecodeToSpatial(fast);

            fixed (short* pData = data16)
            fixed (sbyte* pImg8 = img8)
            {
                if (Avx2.IsSupported && Width >= 32)
                {
                    UnpackPixelsAvx2(pData, pImg8, Height, Width, index, rowSize, pixelSeparation, BlockWidth);
                }
                else if (Ssse3.IsSupported && Width >= 16)
                {
                    UnpackPixelsSsse3(pData, pImg8, Height, Width, index, rowSize, pixelSeparation, BlockWidth);
                }
                else
                {
                    UnpackPixelsScalar(pData, pImg8, Height, Width, index, rowSize, pixelSeparation, BlockWidth);
                }
            }
        }

        /// <summary>
        /// Add docs
        /// </summary>
        /// <param name="subSample"></param>
        /// <param name="rect"></param>
        /// <param name="index"></param>
        /// <param name="img8"></param>
        /// <param name="rowSize"></param>
        /// <param name="pixelDist"></param>
        /// <param name="fast"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe short[] DecodeToSpatial(int subSample, Rectangle rect, bool fast, out Rectangle nrect, out int dataw, out int targetWidth)
        {
            int nlevel = 0;

            while ((nlevel < 5) && ((32 >> nlevel) > subSample))
            {
                nlevel++;
            }

            int boxsize = 1 << nlevel;

            if (subSample != (32 >> nlevel))
            {
                throw new DjvuArgumentOutOfRangeException("Unsupported subsampling factor");
            }

            if (rect.Empty)
            {
                throw new DjvuArgumentException("Rectangle is empty", nameof(rect));
            }

            int width = (((Width + subSample) - 1) / subSample);
            int height = (((Height + subSample) - 1) / subSample);
            Rectangle irect = new Rectangle(0, 0, width, height);

            if ((rect.XMin < 0) || (rect.YMin < 0) || (rect.XMax > irect.XMax) || (rect.YMax > irect.YMax))
            {
                throw new DjvuArgumentException(
                    "Rectangle is out of bounds: " + rect.XMin + "," + rect.YMin +
                    "," + rect.XMax + "," + rect.YMax + "," + irect.XMax + "," + irect.YMax, nameof(rect));
            }

            Rectangle[] needed = new Rectangle[8];
            Rectangle[] recomp = new Rectangle[8];

            for (int i = 0; i < 8;)
            {
                needed[i] = new Rectangle();
                recomp[i++] = new Rectangle();
            }

            int r = 1;
            needed[nlevel] = rect;
            recomp[nlevel] = rect;

            for (int i = nlevel - 1; i >= 0; i--)
            {
                needed[i] = recomp[i + 1];
                needed[i].Inflate(3 * r, 3 * r);
                needed[i].Intersect(needed[i], irect);
                r += r;
                recomp[i].XMin = ((needed[i].XMin + r) - 1) & ~(r - 1);
                recomp[i].XMax = needed[i].XMax & ~(r - 1);
                recomp[i].YMin = ((needed[i].YMin + r) - 1) & ~(r - 1);
                recomp[i].YMax = needed[i].YMax & ~(r - 1);
            }

            Rectangle work = new Rectangle();
            work.XMin = needed[0].XMin & ~(boxsize - 1);
            work.YMin = needed[0].YMin & ~(boxsize - 1);
            work.XMax = ((needed[0].XMax - 1) & ~(boxsize - 1)) + boxsize;
            work.YMax = ((needed[0].YMax - 1) & ~(boxsize - 1)) + boxsize;

            dataw = work.Width;
            long dataSize = (long)dataw * work.Height;

            if (dataSize > Array.MaxLength || dataSize <= 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(rect), rect,
                    "Image dimensions result in an invalid buffer size (less than zero or exceeding Array.MaxLength).");
            }

            short[] data = new short[(int)dataSize];
            int blockWidth = BlockWidth >> 5;
            int lblock = ((work.YMin >> nlevel) * blockWidth) + (work.XMin >> nlevel);

            short[] liftBlock = new short[1024];

            for (int by = work.YMin, ldata = 0;
                 by < work.YMax;
                 by += boxsize, ldata += (dataw << nlevel), lblock += blockWidth)
            {
                for (int bx = work.XMin, bidx = lblock, rdata = ldata;
                     bx < work.XMax;
                     bx += boxsize, bidx++, rdata += boxsize)
                {
                    InterWaveBlock block = Blocks[bidx];
                    int mlevel = nlevel;

                    if ((nlevel > 2) &&
                        (((bx + 31) < needed[2].XMin) || (bx > needed[2].XMax) || ((by + 31) < needed[2].YMin) ||
                         (by > needed[2].YMax)))
                    {
                        mlevel = 2;
                    }

                    int bmax = ((1 << (mlevel + mlevel)) + 15) >> 4;

                    // liftBlock should contain zeros only
                    block.WriteLiftBlock(liftBlock, 0, bmax);

                    fixed (short* pData = data)
                    {
                        MapLiftBlock(liftBlock, pData, rdata, dataw, boxsize, nlevel, mlevel);
                    }
                }
            }

            r = boxsize;

            for (int i = 0; i < nlevel; i++)
            {
                Rectangle comp = needed[i];
                comp.XMin = comp.XMin & ~(r - 1);
                comp.YMin = comp.YMin & ~(r - 1);
                comp.Translate(-work.XMin, -work.YMin);

                if (fast && (i >= 4))
                {
                    fixed (short* pData = data)
                    {
                        UpsampleFast2x2(pData, comp.YMin, comp.YMax, comp.XMin, comp.XMax, dataw);
                    }

                    break;
                }

                fixed (short* pData = data)
                {
                    short* pOffset = pData + (comp.YMin * dataw) + comp.XMin;
                    InterWaveTransform.Backward(pOffset, comp.Width, comp.Height, dataw, r, r >> 1);
                }

                r >>= 1;
            }

            nrect = rect;
            nrect.Translate(-work.XMin, -work.YMin);
            targetWidth = nrect.XMax - nrect.XMin;
            
            return data;
        }

        public unsafe void ImageScalar(int subSample, Rectangle rect, int index, sbyte[] img8, int rowSize, int pixelDist, bool fast)
        {
            short[] data = DecodeToSpatial(subSample, rect, fast, out Rectangle nrect, out int dataw, out int targetWidth);

            fixed (short* pData = data)
            fixed (sbyte* pImg8 = img8)
            {
                UnpackPixelsScalar(pData + (nrect.YMin * dataw) + nrect.XMin, pImg8, nrect.Height, targetWidth, index, rowSize, pixelDist, dataw);
            }
        }

        /// <summary>
        /// Add docs
        /// </summary>
        /// <param name="subSample"></param>
        /// <param name="rect"></param>
        /// <param name="index"></param>
        /// <param name="img8"></param>
        /// <param name="rowSize"></param>
        /// <param name="pixelDist"></param>
        /// <param name="fast"></param>
        public unsafe void Image(int subSample, Rectangle rect, int index, sbyte[] img8, int rowSize, int pixelDist, bool fast)
        {
            short[] data = DecodeToSpatial(subSample, rect, fast, out Rectangle nrect, out int dataw, out int targetWidth);

            fixed (short* pData = data)
            fixed (sbyte* pImg8 = img8)
            {
                short* pOffset = pData + (nrect.YMin * dataw) + nrect.XMin;
                UnpackPixels(pOffset, pImg8, nrect.Height, targetWidth, index, rowSize, pixelDist, dataw);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void UnpackPixels(short* pData, sbyte* pImg8, int height, int targetWidth, int index, int rowSize, int pixelDist, int srcStride)
        {
            if (Avx2.IsSupported && targetWidth >= 32)
            {
                UnpackPixelsAvx2(pData, pImg8, height, targetWidth, index, rowSize, pixelDist, srcStride);
            }
            else if (Ssse3.IsSupported && targetWidth >= 16)
            {
                UnpackPixelsSsse3(pData, pImg8, height, targetWidth, index, rowSize, pixelDist, srcStride);
            }
            else
            {
                UnpackPixelsScalar(pData, pImg8, height, targetWidth, index, rowSize, pixelDist, srcStride);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void UnpackPixelsScalar(short* pData, sbyte* pImg8, int height, int width, int index, int rowSize, int pixelDist, int srcStride)
        {
            for (int i = 0, rowidx = index, pidx = 0; i < height; i++, rowidx += rowSize, pidx += srcStride)
            {
                UnpackRowScalar(pData + pidx, pImg8, width, 0, rowidx, pixelDist);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void UnpackRowScalar(short* pData, sbyte* pImg8, int width, int j, int pixidx, int pixelDist)
        {
            for (; j < width; j++, pixidx += pixelDist)
            {
                int x = (pData[j] + 32) >> 6;
                if (x < -128)
                    x = -128;
                else if (x > 127)
                    x = 127;
                pImg8[pixidx] = (sbyte)x;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void UpsampleFast2x2(short* pData, int yMin, int yMax, int xMin, int xMax, int stride)
        {
            for (int ii = yMin, pp = (yMin * stride); ii < yMax; ii += 2, pp += (stride + stride))
            {
                for (int jj = xMin; jj < xMax; jj += 2)
                {
                    pData[pp + jj + stride] = pData[pp + jj + stride + 1] = pData[pp + jj + 1] = pData[pp + jj];
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void MapLiftBlock(short[] liftBlock, short* pData, int destOffset, int destStride, int boxSize, int nLevel, int mLevel)
        {
            int ppinc = 1 << (nLevel - mLevel);
            int ppmod1 = destStride << (nLevel - mLevel);
            int ttmod0 = 32 >> mLevel;
            int ttmod1 = ttmod0 << 5;

            for (int ii = 0, tt = 0, pp = destOffset; ii < boxSize; ii += ppinc, pp += ppmod1, tt += (ttmod1 - 32))
            {
                for (int jj = 0; jj < boxSize; jj += ppinc, tt += ttmod0)
                {
                    pData[pp + jj] = liftBlock[tt];
                }
            }
        }

        #endregion Public Methods
    }
}
