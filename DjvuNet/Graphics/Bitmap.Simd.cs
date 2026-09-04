using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Text;
using DjvuNet.Errors;

namespace DjvuNet.Graphics
{
    public unsafe partial struct Bitmap
    {
        private interface IFactor { int Value { get; } }
        private struct Factor15 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 15; } }
        private struct Factor14 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 14; } }
        private struct Factor13 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 13; } }
        private struct Factor12 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 12; } }
        private struct Factor11 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 11; } }
        private struct Factor10 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 10; } }
        private struct Factor9 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 9; } }
        private struct Factor8 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 8; } }
        private struct Factor7 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 7; } }
        private struct Factor6 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 6; } }
        private struct Factor5 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 5; } }
        private struct Factor4 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 4; } }
        private struct Factor3 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 3; } }
        private struct Factor2 : IFactor { public int Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 2; } }

        /// <summary>
        /// Handles the edge-case pixel summation at the start of a subsampling row before SIMD boundaries are reached.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Performance Tradeoff:</b> This method abstracts away complex loop boundaries to keep the hot SIMD loops readable.
        /// However, returning a <see cref="ValueTuple{T1, T2}"/> forces RyuJIT to emit register-shuffling <c>mov</c> instructions at the method boundaries.
        /// Even with <see cref="MethodImplOptions.AggressiveInlining"/>, this struct unpacking can induce a marginal performance
        /// regression (e.g., ~1-2% bandwidth loss in AVX-512) due to pipeline disruption right before the hot loop. In the best case,
        /// it yields no performance change if the extra instructions happen to align perfectly with the CPU's instruction cache blocks.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ValueTuple<int, int> SubSamplePrologue(int srcWidth, int dstWidth, int subSample, int initialSrcCount, sbyte* srcPtr, sbyte* dstPtr, int srcCol, int dstCol)
        {
            // --- INLINED PROLOGUE ---
            if (dstCol >= 0 && dstCol < dstWidth)
            {
                int sum = dstPtr[dstCol];
                for (; srcCol < initialSrcCount; srcCol++)
                {
                    sum += srcPtr[srcCol];
                }
                dstPtr[dstCol] = (sbyte)sum;
            }
            else
            {
                srcCol = initialSrcCount;
            }
            dstCol++;

            while (dstCol < 0 && srcCol < srcWidth)
            {
                srcCol += subSample;
                dstCol++;
            }

            if (srcCol > srcWidth)
            {
                srcCol = srcWidth;
            }

            return (srcCol, dstCol);
            // --- END PROLOGUE ---
        }

        /// <summary>
        /// Executes the core scalar fallback loop for the remainder of a subsampling row.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Performance Tradeoff:</b> This method abstracts away scalar loops to keep the hot SIMD paths readable.
        /// However, returning a <see cref="ValueTuple{T1, T2}"/> forces RyuJIT to emit register-shuffling <c>mov</c> instructions at the method boundaries.
        /// Even with <see cref="MethodImplOptions.AggressiveInlining"/>, this struct unpacking can induce a marginal performance
        /// regression (e.g., ~1-2% bandwidth loss in AVX-512) due to pipeline disruption right after the hot loop. In the best case,
        /// it yields no performance change if the extra instructions happen to align perfectly with the CPU's instruction cache blocks.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ValueTuple<int, int> SubSampleEpilogue(int srcWidth, int dstWidth, int subSample, sbyte* srcPtr, sbyte* dstPtr, int srcCol, int dstCol)
        {
            // --- INLINED EPILOGUE ---
            while (srcCol <= srcWidth - subSample)
            {
                if (dstCol >= 0 && dstCol < dstWidth)
                {
                    int limit = srcCol + subSample;
                    int sum = dstPtr[dstCol];
                    for (; srcCol < limit; srcCol++)
                    {
                        sum += srcPtr[srcCol];
                    }
                    dstPtr[dstCol] = (sbyte)sum;
                }
                else
                {
                    srcCol += subSample;
                    dstCol++;
                    continue;
                }
                dstCol++;
            }

            if (srcCol < srcWidth)
            {
                if (dstCol >= 0 && dstCol < dstWidth)
                {
                    int sum = dstPtr[dstCol];
                    for (; srcCol < srcWidth; srcCol++)
                    {
                        sum += srcPtr[srcCol];
                    }
                    dstPtr[dstCol] = (sbyte)sum;
                }
            }
            return (srcCol, dstCol);
            // --- END EPILOGUE ---
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        private bool BlitSubSample3<TFactor>(
            ref Bitmap source, int startDestRow, int subPixelRowOffset,
            int startDestColumn, int subPixelColumnOffset)
            where TFactor : struct, IFactor
        {
            sbyte* srcBase = source.DataPointer;
            sbyte* dstBase = this.DataPointer;

            int srcWidth = source.Width;
            int dstWidth = this.Width;
            int subSample = default(TFactor).Value;

            // The value '3' represents the hard coded subSample factor for this specific method.
            // Hard coding this rather than passing a variable allows RyuJIT to constant-fold
            // the math and eliminate dead branches in the inlined prologue/epilogue.
            int initialSrcCount = subSample - subPixelColumnOffset;
            if (initialSrcCount > srcWidth)
                initialSrcCount = srcWidth;

            if (Avx512Vbmi.IsSupported && Avx512BW.IsSupported && Avx512F.IsSupported)
            {
                Vector512<sbyte> byte0MaskOne512 = Vector512.Create<sbyte>((ReadOnlySpan<sbyte>)
                    [0, 3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45, 48, 51, 54, 57, 60, 63, 66, 69, 72, 75, 78, 81, 84, 87, 90, 93, 96, 99, 102, 105,
                    108, 111, 114, 117, 120, 123, 126, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127]);

                Vector512<sbyte> byte0MaskTwo512 = Vector512.Create<sbyte>((ReadOnlySpan<sbyte>)
                    [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37,
                    38, 39, 40, 41, 42, 65, 68, 71, 74, 77, 80, 83, 86, 89, 92, 95, 98, 101, 104, 107, 110, 113, 116, 119, 122, 125 ]);

                Vector512<sbyte> byte1MaskOne512 = Vector512.Create<sbyte>((ReadOnlySpan<sbyte>)
                    [1, 4, 7, 10, 13, 16, 19, 22, 25, 28, 31, 34, 37, 40, 43, 46, 49, 52, 55, 58, 61, 64, 67, 70, 73, 76, 79, 82, 85, 88, 91, 94, 97, 100, 103, 106,
                    109, 112, 115, 118, 121, 124, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127 ]);

                Vector512<sbyte> byte1MaskTwo512 = Vector512.Create<sbyte>((ReadOnlySpan<sbyte>)
                    [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
                    39, 40, 41, 42, 66, 69, 72, 75, 78, 81, 84, 87, 90, 93, 96, 99, 102, 105, 108, 111, 114, 117, 120, 123, 126 ]);

                Vector512<sbyte> byte2MaskOne512 = Vector512.Create<sbyte>((ReadOnlySpan<sbyte>)
                    [2, 5, 8, 11, 14, 17, 20, 23, 26, 29, 32, 35, 38, 41, 44, 47, 50, 53, 56, 59, 62, 65, 68, 71, 74, 77, 80, 83, 86, 89, 92, 95, 98, 101, 104, 107,
                    110, 113, 116, 119, 122, 125, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127 ]);

                Vector512<sbyte> byte2MaskTwo512 = Vector512.Create<sbyte>((ReadOnlySpan<sbyte>)
                    [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
                    39, 40, 41, 64, 67, 70, 73, 76, 79, 82, 85, 88, 91, 94, 97, 100, 103, 106, 109, 112, 115, 118, 121, 124, 127 ]);

                Vector512<sbyte> byte0Mask64Read512 = Vector512.Create((ReadOnlySpan<sbyte>)[
                    0, 3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45, 48, 51, 54, 57, 60, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63,
                    63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63]);
                Vector512<sbyte> byte1Mask64Read512 = Vector512.Create((ReadOnlySpan<sbyte>)[
                    1, 4, 7, 10, 13, 16, 19, 22, 25, 28, 31, 34, 37, 40, 43, 46, 49, 52, 55, 58, 61, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63,
                    63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63]);
                Vector512<sbyte> byte2Mask64Read512 = Vector512.Create((ReadOnlySpan<sbyte>)[
                    2, 5, 8, 11, 14, 17, 20, 23, 26, 29, 32, 35, 38, 41, 44, 47, 50, 53, 56, 59, 62, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63,
                    63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63]);

                Vector256<sbyte> byte0Mask32Read256 = Vector256.Create((ReadOnlySpan<sbyte>)[
                    0, 3, 6, 9, 12, 15, 18, 21, 24, 27,
                    31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31]);
                Vector256<sbyte> byte1Mask32Read256 = Vector256.Create((ReadOnlySpan<sbyte>)[
                    1, 4, 7, 10, 13, 16, 19, 22, 25, 28,
                    31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31]);
                Vector256<sbyte> byte2Mask32Read256 = Vector256.Create((ReadOnlySpan<sbyte>)[
                    2, 5, 8, 11, 14, 17, 20, 23, 26, 29,
                    31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31, 31]);

                int safeSimdWidth512T1 = srcWidth + source.Border - 192;

                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;
                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);

                        int srcCol = 0;
                        int dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                        while (srcCol <= safeSimdWidth512T1 && dstCol + 64 <= dstWidth)
                        {
                            var dst512 = Vector512.Load(dstPtr + dstCol);

                            Vector512<sbyte> v0 = Vector512.Load(srcPtr + srcCol);
                            Vector512<sbyte> v1 = Vector512.Load(srcPtr + srcCol + 64);
                            Vector512<sbyte> v2 = Vector512.Load(srcPtr + srcCol + 128);

                            Vector512<sbyte> byte0Temp512 = Avx512Vbmi.PermuteVar64x8x2(v0, byte0MaskOne512, v1);
                            Vector512<sbyte> byte1Temp512 = Avx512Vbmi.PermuteVar64x8x2(v0, byte1MaskOne512, v1);
                            Vector512<sbyte> byte2Temp512 = Avx512Vbmi.PermuteVar64x8x2(v0, byte2MaskOne512, v1);

                            Vector512<sbyte> byte0 = Avx512Vbmi.PermuteVar64x8x2(byte0Temp512, byte0MaskTwo512, v2);
                            Vector512<sbyte> byte1 = Avx512Vbmi.PermuteVar64x8x2(byte1Temp512, byte1MaskTwo512, v2);
                            Vector512<sbyte> byte2 = Avx512Vbmi.PermuteVar64x8x2(byte2Temp512, byte2MaskTwo512, v2);

                            Vector512<sbyte> sumByte01 = Vector512.Add(byte0, byte1);
                            Vector512<sbyte> final = Vector512.Add(sumByte01, byte2);

                            Vector512.Store(Vector512.Add(dst512, final), dstPtr + dstCol);

                            srcCol += 192;
                            dstCol += 64;
                        }

                        // Tier 2A Cascade (64-byte read, 63 source pixels consumed, 21 output pixels)
                        int safeSimdWidth512T2A = srcWidth + source.Border - 64;
                        while (srcCol <= safeSimdWidth512T2A && dstCol + 21 <= dstWidth)
                        {
                            Vector128<sbyte> dst16 = Vector128.Load(dstPtr + dstCol);
                            Vector128<sbyte> dst4 = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol + 16)).AsSByte();

                            Vector512<sbyte> src = Vector512.Load(srcPtr + srcCol);
                            Vector512<sbyte> byte0 = Avx512Vbmi.PermuteVar64x8(src, byte0Mask64Read512);
                            Vector512<sbyte> byte1 = Avx512Vbmi.PermuteVar64x8(src, byte1Mask64Read512);
                            Vector512<sbyte> byte2 = Avx512Vbmi.PermuteVar64x8(src, byte2Mask64Read512);

                            Vector512<sbyte> final = Vector512.Add(Vector512.Add(byte0, byte1), byte2);

                            Vector128<sbyte> v16 = final.GetLower().GetLower();
                            Vector128.Store(Vector128.Add(dst16, v16), dstPtr + dstCol);

                            Vector128<sbyte> vNext = final.GetLower().GetUpper();
                            Unsafe.WriteUnaligned<int>(dstPtr + dstCol + 16, Vector128.Add(dst4, vNext).AsInt32().ToScalar());
                            dstPtr[dstCol + 20] += vNext.GetElement(4);

                            srcCol += 63;
                            dstCol += 21;
                        }

                        // Tier 2 Cascade (32-byte read, 30 source pixels consumed, 10 output pixels)
                        if (Avx512Vbmi.VL.IsSupported)
                        {
                            int safeSimdWidth256 = srcWidth + source.Border - 32;
                            while (srcCol <= safeSimdWidth256 && dstCol + 10 <= dstWidth)
                            {
                                Vector128<sbyte> dst8 = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(dstPtr + dstCol)).AsSByte();
                                Vector128<sbyte> dst2 = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<short>(dstPtr + dstCol + 8)).AsSByte();

                                Vector256<sbyte> src = Vector256.Load(srcPtr + srcCol);
                                Vector256<sbyte> byte0 = Avx512Vbmi.VL.PermuteVar32x8(src, byte0Mask32Read256);
                                Vector256<sbyte> byte1 = Avx512Vbmi.VL.PermuteVar32x8(src, byte1Mask32Read256);
                                Vector256<sbyte> byte2 = Avx512Vbmi.VL.PermuteVar32x8(src, byte2Mask32Read256);

                                Vector256<sbyte> final = Vector256.Add(Vector256.Add(byte0, byte1), byte2);
                                Vector128<sbyte> v10 = final.GetLower();

                                Unsafe.WriteUnaligned<long>(dstPtr + dstCol, Vector128.Add(dst8, v10).AsInt64().ToScalar());

                                Vector128<sbyte> shifted = Sse2.ShiftRightLogical128BitLane(v10.AsByte(), 8).AsSByte();
                                Unsafe.WriteUnaligned<short>(dstPtr + dstCol + 8, Vector128.Add(dst2, shifted).AsInt16().ToScalar());

                                srcCol += 30;
                                dstCol += 10;
                            }
                        }

                        // Tier 3: 12 bytes -> 4 pixels cascade
                        int safeSimdWidth12T3 = srcWidth + source.Border - 16;
                        if (srcCol <= safeSimdWidth12T3)
                        {
                            Vector128<sbyte> t3_mask0 = Vector128.Create((ReadOnlySpan<sbyte>)[0, 3, 6, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                            Vector128<sbyte> t3_mask1 = Vector128.Create((ReadOnlySpan<sbyte>)[1, 4, 7, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                            Vector128<sbyte> t3_mask2 = Vector128.Create((ReadOnlySpan<sbyte>)[2, 5, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);

                            while (srcCol <= safeSimdWidth12T3 && dstCol + 4 <= dstWidth)
                            {
                                Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsSByte();

                                Vector128<sbyte> v0 = Vector128.Load(srcPtr + srcCol);
                                Vector128<sbyte> byte0 = Ssse3.Shuffle(v0, t3_mask0);
                                Vector128<sbyte> byte1 = Ssse3.Shuffle(v0, t3_mask1);
                                Vector128<sbyte> byte2 = Ssse3.Shuffle(v0, t3_mask2);

                                Vector128<sbyte> final0 = Vector128.Add(Vector128.Add(byte0, byte1), byte2);

                                Unsafe.WriteUnaligned<int>(dstPtr + dstCol, Vector128.Add(dstVec, final0).AsInt32().ToScalar());

                                srcCol += 12;
                                dstCol += 4;
                            }
                        }

                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }

                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }
            else if (Avx2.IsSupported)
            {
                Vector256<sbyte> byte0MaskOne256 = Vector256.Create((ReadOnlySpan<sbyte>)
                    [0, 3, 6, 9, 12, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 3, 6, 9, 12, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                Vector256<sbyte> byte0MaskTwo256 = Vector256.Create((ReadOnlySpan<sbyte>)
                    [-1, -1, -1, -1, -1, -1, 2, 5, 8, 11, 14, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 2, 5, 8, 11, 14, -1, -1, -1, -1, -1]);
                Vector256<sbyte> byte0MaskThree256 = Vector256.Create((ReadOnlySpan<sbyte>)
                    [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 4, 7, 10, 13, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 4, 7, 10, 13]);
                Vector256<sbyte> byte1MaskOne256 = Vector256.Create((ReadOnlySpan<sbyte>)
                    [1, 4, 7, 10, 13, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 4, 7, 10, 13, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                Vector256<sbyte> byte1MaskTwo256 = Vector256.Create((ReadOnlySpan<sbyte>)
                    [-1, -1, -1, -1, -1, 0, 3, 6, 9, 12, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 3, 6, 9, 12, 15, -1, -1, -1, -1, -1]);
                Vector256<sbyte> byte1MaskThree256 = Vector256.Create((ReadOnlySpan<sbyte>)
                    [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 2, 5, 8, 11, 14, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 2, 5, 8, 11, 14]);
                Vector256<sbyte> byte2MaskOne256 = Vector256.Create((ReadOnlySpan<sbyte>)
                    [2, 5, 8, 11, 14, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 2, 5, 8, 11, 14, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                Vector256<sbyte> byte2MaskTwo256 = Vector256.Create((ReadOnlySpan<sbyte>)
                    [-1, -1, -1, -1, -1, 1, 4, 7, 10, 13, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 4, 7, 10, 13, -1, -1, -1, -1, -1, -1]);
                Vector256<sbyte> byte2MaskThree256 = Vector256.Create((ReadOnlySpan<sbyte>)
                    [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 3, 6, 9, 12, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 3, 6, 9, 12, 15]);

                int safeSimdWidthAvx2 = srcWidth + source.Border - 96;

                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;
                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);

                        int srcCol = 0;
                        int dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                        while (srcCol <= safeSimdWidthAvx2 && dstCol + 32 <= dstWidth)
                        {
                            Vector256<sbyte> dst256 = Vector256.Load(dstPtr + dstCol);

                            Vector256<sbyte> L0 = Vector256.Load(srcPtr + srcCol);
                            Vector256<sbyte> L1 = Vector256.Load(srcPtr + srcCol + 32);
                            Vector256<sbyte> L2 = Vector256.Load(srcPtr + srcCol + 64);

                            // Rearrange 128-bit lanes so Lower = Iteration 1, Upper = Iteration 2
                            Vector256<sbyte> v0 = Avx2.Permute2x128(L0, L1, 0x30);
                            Vector256<sbyte> v1 = Avx2.Permute2x128(L0, L2, 0x21);
                            Vector256<sbyte> v2 = Avx2.Permute2x128(L1, L2, 0x30);

                            Vector256<sbyte> byte0 = Avx2.Shuffle(v0, byte0MaskOne256);
                            Vector256<sbyte> byte1 = Avx2.Shuffle(v0, byte1MaskOne256);
                            Vector256<sbyte> byte2 = Avx2.Shuffle(v0, byte2MaskOne256);

                            byte0 = Vector256.BitwiseOr(byte0, Avx2.Shuffle(v1, byte0MaskTwo256));
                            byte1 = Vector256.BitwiseOr(byte1, Avx2.Shuffle(v1, byte1MaskTwo256));
                            byte2 = Vector256.BitwiseOr(byte2, Avx2.Shuffle(v1, byte2MaskTwo256));

                            byte0 = Vector256.BitwiseOr(byte0, Avx2.Shuffle(v2, byte0MaskThree256));
                            byte1 = Vector256.BitwiseOr(byte1, Avx2.Shuffle(v2, byte1MaskThree256));
                            byte2 = Vector256.BitwiseOr(byte2, Avx2.Shuffle(v2, byte2MaskThree256));

                            Vector256<sbyte> final = Vector256.Add(Vector256.Add(byte0, byte1), byte2);

                            Vector256.Store(Vector256.Add(dst256, final), dstPtr + dstCol);
                            srcCol += 96;
                            dstCol += 32;
                        }

                        // Tier 2: 32 bytes -> 10 pixels cascade (Strictly ONE Vector256.Load)
                        int safeSimdWidth32T2 = srcWidth + source.Border - 32;
                        if (srcCol <= safeSimdWidth32T2)
                        {
                            Vector256<sbyte> t2_mask0_v0 = Vector256.Create((ReadOnlySpan<sbyte>)[
                                0, 3, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                                8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                            Vector256<sbyte> t2_mask0_v1 = Vector256.Create((ReadOnlySpan<sbyte>)[
                                -1, -1, -1, 1, 4, 7, 10, 13, -1, -1, -1, -1, -1, -1, -1, -1,
                                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);

                            Vector256<sbyte> t2_mask1_v0 = Vector256.Create((ReadOnlySpan<sbyte>)[
                                1, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                                9, 12, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                            Vector256<sbyte> t2_mask1_v1 = Vector256.Create((ReadOnlySpan<sbyte>)[
                                -1, -1, -1, 2, 5, 8, 11, 14, -1, -1, -1, -1, -1, -1, -1, -1,
                                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);

                            Vector256<sbyte> t2_mask2_v0 = Vector256.Create((ReadOnlySpan<sbyte>)[
                                2, 5, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                                10, 13, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                            Vector256<sbyte> t2_mask2_v1 = Vector256.Create((ReadOnlySpan<sbyte>)[
                                -1, -1, -1, 3, 6, 9, 12, 15, -1, -1, -1, -1, -1, -1, -1, -1,
                                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);

                            while (srcCol <= safeSimdWidth32T2 && dstCol + 10 <= dstWidth)
                            {
                                Vector128<sbyte> dst8Vec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(dstPtr + dstCol)).AsSByte();
                                Vector128<sbyte> dst2Vec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<short>(dstPtr + dstCol + 8)).AsSByte();

                                Vector256<sbyte> v0 = Vector256.Load(srcPtr + srcCol);
                                // Shift vector by 8 bytes across the 128-bit lane boundary to rescue boundary-straddling pixels
                                Vector256<sbyte> v1 = Avx2.Permute4x64(v0.AsInt64(), 0x39).AsSByte();

                                Vector256<sbyte> byte0 = Vector256.BitwiseOr(Avx2.Shuffle(v0, t2_mask0_v0), Avx2.Shuffle(v1, t2_mask0_v1));
                                Vector256<sbyte> byte1 = Vector256.BitwiseOr(Avx2.Shuffle(v0, t2_mask1_v0), Avx2.Shuffle(v1, t2_mask1_v1));
                                Vector256<sbyte> byte2 = Vector256.BitwiseOr(Avx2.Shuffle(v0, t2_mask2_v0), Avx2.Shuffle(v1, t2_mask2_v1));

                                Vector256<sbyte> final = Vector256.Add(Vector256.Add(byte0, byte1), byte2);

                                Unsafe.WriteUnaligned<long>(dstPtr + dstCol, Vector128.Add(dst8Vec, final.GetLower()).AsInt64().ToScalar());
                                Unsafe.WriteUnaligned<short>(dstPtr + dstCol + 8, Vector128.Add(dst2Vec, final.GetUpper()).AsInt16().ToScalar());

                                srcCol += 30;
                                dstCol += 10;
                            }
                        }

                        // Tier 3: 12 bytes -> 4 pixels cascade
                        int safeSimdWidth12T3 = srcWidth + source.Border - 16;
                        if (srcCol <= safeSimdWidth12T3)
                        {
                            Vector128<sbyte> t3_mask0 = Vector128.Create((ReadOnlySpan<sbyte>)[0, 3, 6, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                            Vector128<sbyte> t3_mask1 = Vector128.Create((ReadOnlySpan<sbyte>)[1, 4, 7, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                            Vector128<sbyte> t3_mask2 = Vector128.Create((ReadOnlySpan<sbyte>)[2, 5, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);

                            while (srcCol <= safeSimdWidth12T3 && dstCol + 4 <= dstWidth)
                            {
                                Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsSByte();

                                Vector128<sbyte> v0 = Vector128.Load(srcPtr + srcCol);
                                Vector128<sbyte> byte0 = Ssse3.Shuffle(v0, t3_mask0);
                                Vector128<sbyte> byte1 = Ssse3.Shuffle(v0, t3_mask1);
                                Vector128<sbyte> byte2 = Ssse3.Shuffle(v0, t3_mask2);

                                Vector128<sbyte> final0 = Vector128.Add(Vector128.Add(byte0, byte1), byte2);

                                Unsafe.WriteUnaligned<int>(dstPtr + dstCol, Vector128.Add(dstVec, final0).AsInt32().ToScalar());

                                srcCol += 12;
                                dstCol += 4;
                            }
                        }

                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }

                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }
            else if (Ssse3.IsSupported)
            {
                Vector128<sbyte> byte0MaskOne128 = Vector128.Create((ReadOnlySpan<sbyte>)[0, 3, 6, 9, 12, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                Vector128<sbyte> byte0MaskTwo128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, -1, -1, -1, -1, -1, 2, 5, 8, 11, 14, -1, -1, -1, -1, -1]);
                Vector128<sbyte> byte0MaskThree128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 4, 7, 10, 13]);
                Vector128<sbyte> byte1MaskOne128 = Vector128.Create((ReadOnlySpan<sbyte>)[1, 4, 7, 10, 13, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                Vector128<sbyte> byte1MaskTwo128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, -1, -1, -1, -1, 0, 3, 6, 9, 12, 15, -1, -1, -1, -1, -1]);
                Vector128<sbyte> byte1MaskThree128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 2, 5, 8, 11, 14]);
                Vector128<sbyte> byte2MaskOne128 = Vector128.Create((ReadOnlySpan<sbyte>)[2, 5, 8, 11, 14, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                Vector128<sbyte> byte2MaskTwo128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, -1, -1, -1, -1, 1, 4, 7, 10, 13, -1, -1, -1, -1, -1, -1]);
                Vector128<sbyte> byte2MaskThree128 = Vector128.Create((ReadOnlySpan<sbyte>)[-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 3, 6, 9, 12, 15]);

                int safeSimdWidth128 = srcWidth + source.Border - 48;

                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;
                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);

                        int srcCol = 0;
                        int dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                        while (srcCol <= safeSimdWidth128 && dstCol + 16 <= dstWidth)
                        {
                            var dst128 = Vector128.Load(dstPtr + dstCol);

                            Vector128<sbyte> v0 = Vector128.Load(srcPtr + srcCol);
                            Vector128<sbyte> v1 = Vector128.Load(srcPtr + srcCol + 16);
                            Vector128<sbyte> v2 = Vector128.Load(srcPtr + srcCol + 32);

                            Vector128<sbyte> byte0 = Ssse3.Shuffle(v0, byte0MaskOne128);
                            Vector128<sbyte> byte1 = Ssse3.Shuffle(v0, byte1MaskOne128);
                            Vector128<sbyte> byte2 = Ssse3.Shuffle(v0, byte2MaskOne128);

                            byte0 = Vector128.BitwiseOr(byte0, Ssse3.Shuffle(v1, byte0MaskTwo128));
                            byte1 = Vector128.BitwiseOr(byte1, Ssse3.Shuffle(v1, byte1MaskTwo128));
                            byte2 = Vector128.BitwiseOr(byte2, Ssse3.Shuffle(v1, byte2MaskTwo128));

                            byte0 = Vector128.BitwiseOr(byte0, Ssse3.Shuffle(v2, byte0MaskThree128));
                            byte1 = Vector128.BitwiseOr(byte1, Ssse3.Shuffle(v2, byte1MaskThree128));
                            byte2 = Vector128.BitwiseOr(byte2, Ssse3.Shuffle(v2, byte2MaskThree128));

                            Vector128<sbyte> final = Vector128.Add(Vector128.Add(byte0, byte1), byte2);
                            Vector128.Store(Vector128.Add(dst128, final), dstPtr + dstCol);

                            srcCol += 48;
                            dstCol += 16;
                        }

                        // Tier 3: 12 bytes -> 4 pixels cascade
                        int safeSimdWidth12T3 = srcWidth + source.Border - 16;
                        if (srcCol <= safeSimdWidth12T3)
                        {
                            Vector128<sbyte> t3_mask0 = Vector128.Create((ReadOnlySpan<sbyte>)[0, 3, 6, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                            Vector128<sbyte> t3_mask1 = Vector128.Create((ReadOnlySpan<sbyte>)[1, 4, 7, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);
                            Vector128<sbyte> t3_mask2 = Vector128.Create((ReadOnlySpan<sbyte>)[2, 5, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]);

                            while (srcCol <= safeSimdWidth12T3 && dstCol + 4 <= dstWidth)
                            {
                                Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsSByte();

                                Vector128<sbyte> v0 = Vector128.Load(srcPtr + srcCol);
                                Vector128<sbyte> byte0 = Ssse3.Shuffle(v0, t3_mask0);
                                Vector128<sbyte> byte1 = Ssse3.Shuffle(v0, t3_mask1);
                                Vector128<sbyte> byte2 = Ssse3.Shuffle(v0, t3_mask2);

                                Vector128<sbyte> final0 = Vector128.Add(Vector128.Add(byte0, byte1), byte2);

                                Unsafe.WriteUnaligned<int>(dstPtr + dstCol, Vector128.Add(dstVec, final0).AsInt32().ToScalar());

                                srcCol += 12;
                                dstCol += 4;
                            }
                        }

                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }

                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }
            else
            {
                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;
                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);

                        int srcCol = 0;
                        int dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }

                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        private bool BlitSubSampleDoubleWord<TFactor>(
            ref Bitmap source, int startDestRow, int subPixelRowOffset,
            int startDestColumn, int subPixelColumnOffset)
            where TFactor : struct, IFactor
        {
            int subSample = default(TFactor).Value;
            int srcWidth = source.Width;
            int dstWidth = this.Width;
            int srcBorder = source.Border;

            int initialSrcCount = subSample - subPixelColumnOffset;
            if (initialSrcCount > srcWidth)
                initialSrcCount = srcWidth;

            if (Avx512BW.IsSupported && Avx512F.IsSupported)
            {
                int safeSimdWidth128 = srcWidth + srcBorder - 128;
                int safeSimdWidth64 = srcWidth + srcBorder - 64;
                int safeSimdWidth32 = srcWidth + srcBorder - 32;

                Vector512<byte> ones8 = Vector512.Create((byte)1);
                Vector512<short> ones16 = Vector512.Create((short)1);

                if (subSample == 4)
                {
                    Vector256<byte> ones8_256 = Vector256.Create((byte)1);
                    Vector256<short> ones16_256 = Vector256.Create((short)1);

                    int dstRow = startDestRow;
                    int rowMod = subPixelRowOffset;
                    for (int srcY = 0; srcY < source.Height; srcY++)
                    {
                        if (dstRow >= 0 && dstRow < Height)
                        {
                            sbyte* srcPtr = source.GetRow(srcY);
                            sbyte* dstPtr = this.GetRow(dstRow);

                            int srcCol = 0;
                            int dstCol = startDestColumn;

                            (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                            // Tier 1: 128 bytes -> 32 pixels
                            while (srcCol <= safeSimdWidth128 && dstCol + 32 <= dstWidth)
                            {
                                Vector512<short> mulA = Avx512BW.MultiplyAddAdjacent(ones8, Vector512.Load(srcPtr + srcCol));
                                Vector512<short> mulB = Avx512BW.MultiplyAddAdjacent(ones8, Vector512.Load(srcPtr + srcCol + 64));

                                Vector512<int> sumA = Avx512BW.MultiplyAddAdjacent(mulA, ones16);
                                Vector512<int> sumB = Avx512BW.MultiplyAddAdjacent(mulB, ones16);

                                // vpmovsdb: Directly truncates 16 ints -> 16 sbytes sequentially (no interleaving!)
                                Vector128<sbyte> packA = Avx512F.ConvertToVector128SByteWithSaturation(sumA);
                                Vector128<sbyte> packB = Avx512F.ConvertToVector128SByteWithSaturation(sumB);

                                Vector256<sbyte> finalOut = Vector256.Create(packA, packB);

                                Vector256<sbyte> dstOut = Vector256.Load(dstPtr + dstCol);
                                Vector256.Store(Vector256.Add(dstOut, finalOut), dstPtr + dstCol);

                                srcCol += 128;
                                dstCol += 32;
                            }

                            // Tier 2A: 64 bytes -> 16 pixels cascade
                            while (srcCol <= safeSimdWidth64 && dstCol + 16 <= dstWidth)
                            {
                                Vector128<sbyte> dstOut = Vector128.Load(dstPtr + dstCol);
                                Vector512<short> mulA = Avx512BW.MultiplyAddAdjacent(ones8, Vector512.Load(srcPtr + srcCol));
                                Vector512<int> sumA = Avx512BW.MultiplyAddAdjacent(mulA, ones16);

                                Vector128<sbyte> finalOut = Avx512F.ConvertToVector128SByteWithSaturation(sumA);

                                Vector128.Store(Vector128.Add(dstOut, finalOut), dstPtr + dstCol);

                                srcCol += 64;
                                dstCol += 16;
                            }

                            // Tier 2: 32 bytes -> 8 pixels cascade
                            if (Avx512F.VL.IsSupported)
                            {
                                while (srcCol <= safeSimdWidth32 && dstCol + 8 <= dstWidth)
                                {
                                    Vector128<sbyte> dstOut = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(dstPtr + dstCol)).AsSByte();

                                    Vector256<short> mulA = Avx2.MultiplyAddAdjacent(ones8_256, Vector256.Load(srcPtr + srcCol));
                                    Vector256<int> sumA = Avx2.MultiplyAddAdjacent(mulA, ones16_256);

                                    // Translates to vpmovsdb xmm, ymm (perfectly extracts 8 sbytes sequentially)
                                    Vector128<sbyte> finalOut = Avx512F.VL.ConvertToVector128SByteWithSaturation(sumA);

                                    Unsafe.WriteUnaligned<long>(dstPtr + dstCol, Vector128.Add(dstOut, finalOut).AsInt64().ToScalar());

                                    srcCol += 32;
                                    dstCol += 8;
                                }
                            }

                            // Tier 3: 16 bytes -> 4 pixels cascade
                            if (Ssse3.IsSupported)
                            {
                                Vector128<byte> ones8_128 = Vector128.Create((byte)1);
                                Vector128<short> ones16_128 = Vector128.Create((short)1);
                                int safeSimdWidth16T3 = srcWidth + srcBorder - 16;
                                while (srcCol <= safeSimdWidth16T3 && dstCol + 4 <= dstWidth)
                                {
                                    Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsSByte();

                                    Vector128<sbyte> v0 = Vector128.Load(srcPtr + srcCol);
                                    Vector128<short> mul0 = Ssse3.MultiplyAddAdjacent(ones8_128, v0);
                                    Vector128<int> sum0 = Sse2.MultiplyAddAdjacent(mul0, ones16_128);
                                    Vector128<short> pack0 = Sse2.PackSignedSaturate(sum0, sum0);
                                    Vector128<sbyte> final0 = Sse2.PackSignedSaturate(pack0, pack0);

                                    Unsafe.WriteUnaligned<int>(dstPtr + dstCol, Vector128.Add(dstVec, final0).AsInt32().ToScalar());

                                    srcCol += 16;
                                    dstCol += 4;
                                }
                            }

                            (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                        }

                        rowMod++;
                        if (rowMod >= subSample)
                        {
                            rowMod = 0;
                            dstRow++;
                        }
                    }
                }
                else if (subSample == 2)
                {
                    Vector256<byte> ones8_256 = Vector256.Create((byte)1);

                    int dstRow = startDestRow;
                    int rowMod = subPixelRowOffset;
                    for (int srcY = 0; srcY < source.Height; srcY++)
                    {
                        if (dstRow >= 0 && dstRow < Height)
                        {
                            sbyte* srcPtr = source.GetRow(srcY);
                            sbyte* dstPtr = this.GetRow(dstRow);

                            int srcCol = 0;
                            int dstCol = startDestColumn;

                            (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                            // Tier 1: 128 bytes -> 64 pixels
                            while (srcCol <= safeSimdWidth128 && dstCol + 64 <= dstWidth)
                            {
                                Vector256<sbyte> dstOutA = Vector256.Load(dstPtr + dstCol);
                                Vector256<sbyte> dstOutB = Vector256.Load(dstPtr + dstCol + 32);

                                Vector512<short> mulA = Avx512BW.MultiplyAddAdjacent(ones8, Vector512.Load(srcPtr + srcCol));
                                Vector512<short> mulB = Avx512BW.MultiplyAddAdjacent(ones8, Vector512.Load(srcPtr + srcCol + 64));

                                // vpmovswb: Directly truncates 32 shorts -> 32 sbytes sequentially
                                Vector256<sbyte> packA = Avx512BW.ConvertToVector256SByteWithSaturation(mulA);
                                Vector256<sbyte> packB = Avx512BW.ConvertToVector256SByteWithSaturation(mulB);

                                Vector256.Store(Vector256.Add(dstOutA, packA), dstPtr + dstCol);
                                Vector256.Store(Vector256.Add(dstOutB, packB), dstPtr + dstCol + 32);

                                srcCol += 128;
                                dstCol += 64;
                            }

                            // Tier 2A: 64 bytes -> 32 pixels cascade
                            while (srcCol <= safeSimdWidth64 && dstCol + 32 <= dstWidth)
                            {
                                Vector256<sbyte> dstOut = Vector256.Load(dstPtr + dstCol);

                                Vector512<short> mulA = Avx512BW.MultiplyAddAdjacent(ones8, Vector512.Load(srcPtr + srcCol));
                                Vector256<sbyte> finalOut = Avx512BW.ConvertToVector256SByteWithSaturation(mulA);

                                Vector256.Store(Vector256.Add(dstOut, finalOut), dstPtr + dstCol);

                                srcCol += 64;
                                dstCol += 32;
                            }

                            // Tier 2: 32 bytes -> 16 pixels cascade
                            if (Avx512BW.VL.IsSupported)
                            {
                                while (srcCol <= safeSimdWidth32 && dstCol + 16 <= dstWidth)
                                {
                                    Vector128<sbyte> dstOut = Vector128.Load(dstPtr + dstCol);

                                    Vector256<short> mulA = Avx2.MultiplyAddAdjacent(ones8_256, Vector256.Load(srcPtr + srcCol));
                                    Vector128<sbyte> finalOut = Avx512BW.VL.ConvertToVector128SByteWithSaturation(mulA);

                                    Vector128.Store(Vector128.Add(dstOut, finalOut), dstPtr + dstCol);

                                    srcCol += 32;
                                    dstCol += 16;
                                }
                            }

                            // Tier 3: 16 bytes -> 8 pixels cascade
                            if (Ssse3.IsSupported)
                            {
                                Vector128<byte> ones8_128 = Vector128.Create((byte)1);
                                int safeSimdWidth16T3 = srcWidth + srcBorder - 16;
                                while (srcCol <= safeSimdWidth16T3 && dstCol + 8 <= dstWidth)
                                {
                                    Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(dstPtr + dstCol)).AsSByte();

                                    Vector128<sbyte> v0 = Vector128.Load(srcPtr + srcCol);
                                    Vector128<short> mul0 = Ssse3.MultiplyAddAdjacent(ones8_128, v0);
                                    Vector128<sbyte> final0 = Sse2.PackSignedSaturate(mul0, mul0);

                                    Unsafe.WriteUnaligned<long>(dstPtr + dstCol, Vector128.Add(dstVec, final0).AsInt64().ToScalar());

                                    srcCol += 16;
                                    dstCol += 8;
                                }
                            }

                            (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                        }

                        rowMod++;
                        if (rowMod >= subSample)
                        {
                            rowMod = 0;
                            dstRow++;
                        }
                    }
                }
            }
            else if (Avx2.IsSupported)
            {
                int safeSimdWidth128 = srcWidth + srcBorder - 128;

                Vector256<byte> ones8 = Vector256.Create((byte)1);
                Vector256<short> ones16 = Vector256.Create((short)1);

                if (subSample == 4)
                {
                    Vector256<int> permuteMask4 = Vector256.Create(0, 4, 1, 5, 2, 6, 3, 7);

                    int dstRow = startDestRow;
                    int rowMod = subPixelRowOffset;
                    for (int srcY = 0; srcY < source.Height; srcY++)
                    {
                        if (dstRow >= 0 && dstRow < Height)
                        {
                            sbyte* srcPtr = source.GetRow(srcY);
                            sbyte* dstPtr = this.GetRow(dstRow);

                            int srcCol = 0;
                            int dstCol = startDestColumn;

                            (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                            while (srcCol <= safeSimdWidth128 && dstCol + 32 <= dstWidth)
                            {
                                Vector256<sbyte> dst = Vector256.Load(dstPtr + dstCol);

                                Vector256<short> mul0 = Avx2.MultiplyAddAdjacent(ones8, Vector256.Load(srcPtr + srcCol));
                                Vector256<short> mul1 = Avx2.MultiplyAddAdjacent(ones8, Vector256.Load(srcPtr + srcCol + 32));
                                Vector256<short> mul2 = Avx2.MultiplyAddAdjacent(ones8, Vector256.Load(srcPtr + srcCol + 64));
                                Vector256<short> mul3 = Avx2.MultiplyAddAdjacent(ones8, Vector256.Load(srcPtr + srcCol + 96));

                                Vector256<int> sum0 = Avx2.MultiplyAddAdjacent(mul0, ones16);
                                Vector256<int> sum1 = Avx2.MultiplyAddAdjacent(mul1, ones16);
                                Vector256<int> sum2 = Avx2.MultiplyAddAdjacent(mul2, ones16);
                                Vector256<int> sum3 = Avx2.MultiplyAddAdjacent(mul3, ones16);

                                Vector256<short> pack0 = Avx2.PackSignedSaturate(sum0, sum1);
                                Vector256<short> pack1 = Avx2.PackSignedSaturate(sum2, sum3);

                                Vector256<sbyte> pack8 = Avx2.PackSignedSaturate(pack0, pack1);
                                Vector256<sbyte> final = Avx2.PermuteVar8x32(pack8.AsInt32(), permuteMask4).AsSByte();

                                Vector256.Store(Vector256.Add(dst, final), dstPtr + dstCol);

                                srcCol += 128;
                                dstCol += 32;
                            }

                            // Tier 2: 32 bytes -> 8 pixels cascade
                            int safeSimdWidth32T2 = srcWidth + srcBorder - 32;
                            while (srcCol <= safeSimdWidth32T2 && dstCol + 8 <= dstWidth)
                            {
                                Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(dstPtr + dstCol)).AsSByte();

                                Vector256<short> mul0 = Avx2.MultiplyAddAdjacent(ones8, Vector256.Load(srcPtr + srcCol));
                                Vector256<int> sum0 = Avx2.MultiplyAddAdjacent(mul0, ones16);

                                Vector256<short> pack0 = Avx2.PackSignedSaturate(sum0, sum0);
                                Vector256<sbyte> pack8 = Avx2.PackSignedSaturate(pack0, pack0);
                                Vector256<sbyte> final = Avx2.PermuteVar8x32(pack8.AsInt32(), permuteMask4).AsSByte();

                                Unsafe.WriteUnaligned<long>(dstPtr + dstCol, Vector128.Add(dstVec, final.GetLower()).AsInt64().ToScalar());

                                srcCol += 32;
                                dstCol += 8;
                            }

                            // Tier 3: 16 bytes -> 4 pixels cascade
                            if (Ssse3.IsSupported)
                            {
                                Vector128<byte> ones8_128 = Vector128.Create((byte)1);
                                Vector128<short> ones16_128 = Vector128.Create((short)1);
                                int safeSimdWidth16T3 = srcWidth + srcBorder - 16;
                                while (srcCol <= safeSimdWidth16T3 && dstCol + 4 <= dstWidth)
                                {
                                    Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsSByte();

                                    Vector128<sbyte> v0 = Vector128.Load(srcPtr + srcCol);
                                    Vector128<short> mul0 = Ssse3.MultiplyAddAdjacent(ones8_128, v0);
                                    Vector128<int> sum0 = Sse2.MultiplyAddAdjacent(mul0, ones16_128);
                                    Vector128<short> pack0 = Sse2.PackSignedSaturate(sum0, sum0);
                                    Vector128<sbyte> final0 = Sse2.PackSignedSaturate(pack0, pack0);

                                    Unsafe.WriteUnaligned<int>(dstPtr + dstCol, Vector128.Add(dstVec, final0).AsInt32().ToScalar());

                                    srcCol += 16;
                                    dstCol += 4;
                                }
                            }

                            // --- INLINED EPILOGUE ---
                            (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                        }

                        rowMod++;
                        if (rowMod >= subSample)
                        {
                            rowMod = 0;
                            dstRow++;
                        }
                    }
                }
                else if (subSample == 2)
                {
                    Vector256<int> permuteMask2 = Vector256.Create(0, 1, 4, 5, 2, 3, 6, 7);

                    int dstRow = startDestRow;
                    int rowMod = subPixelRowOffset;
                    for (int srcY = 0; srcY < source.Height; srcY++)
                    {
                        if (dstRow >= 0 && dstRow < Height)
                        {
                            sbyte* srcPtr = source.GetRow(srcY);
                            sbyte* dstPtr = this.GetRow(dstRow);

                            int srcCol = 0;
                            int dstCol = startDestColumn;

                            (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                            while (srcCol <= safeSimdWidth128 && dstCol + 64 <= dstWidth)
                            {
                                Vector256<sbyte> dst0 = Vector256.Load(dstPtr + dstCol);
                                Vector256<sbyte> dst1 = Vector256.Load(dstPtr + dstCol + 32);

                                Vector256<short> mul0 = Avx2.MultiplyAddAdjacent(ones8, Vector256.Load(srcPtr + srcCol));
                                Vector256<short> mul1 = Avx2.MultiplyAddAdjacent(ones8, Vector256.Load(srcPtr + srcCol + 32));
                                Vector256<short> mul2 = Avx2.MultiplyAddAdjacent(ones8, Vector256.Load(srcPtr + srcCol + 64));
                                Vector256<short> mul3 = Avx2.MultiplyAddAdjacent(ones8, Vector256.Load(srcPtr + srcCol + 96));

                                Vector256<sbyte> pack0 = Avx2.PackSignedSaturate(mul0, mul1);
                                Vector256<sbyte> final0 = Avx2.PermuteVar8x32(pack0.AsInt32(), permuteMask2).AsSByte();

                                Vector256<sbyte> pack1 = Avx2.PackSignedSaturate(mul2, mul3);
                                Vector256<sbyte> final1 = Avx2.PermuteVar8x32(pack1.AsInt32(), permuteMask2).AsSByte();

                                Vector256.Store(Vector256.Add(dst0, final0), dstPtr + dstCol);
                                Vector256.Store(Vector256.Add(dst1, final1), dstPtr + dstCol + 32);

                                srcCol += 128;
                                dstCol += 64;
                            }

                            // Tier 2: 32 bytes -> 16 pixels cascade
                            int safeSimdWidth32T2 = srcWidth + srcBorder - 32;
                            while (srcCol <= safeSimdWidth32T2 && dstCol + 16 <= dstWidth)
                            {
                                Vector128<sbyte> dstVec = Vector128.Load(dstPtr + dstCol);

                                Vector256<short> mul0 = Avx2.MultiplyAddAdjacent(ones8, Vector256.Load(srcPtr + srcCol));

                                Vector256<sbyte> pack0 = Avx2.PackSignedSaturate(mul0, mul0);
                                Vector256<sbyte> final0 = Avx2.PermuteVar8x32(pack0.AsInt32(), permuteMask2).AsSByte();

                                Vector128.Store(Vector128.Add(dstVec, final0.GetLower()), dstPtr + dstCol);

                                srcCol += 32;
                                dstCol += 16;
                            }

                            // Tier 3: 16 bytes -> 8 pixels cascade
                            if (Ssse3.IsSupported)
                            {
                                Vector128<byte> ones8_128 = Vector128.Create((byte)1);
                                int safeSimdWidth16T3 = srcWidth + srcBorder - 16;
                                while (srcCol <= safeSimdWidth16T3 && dstCol + 8 <= dstWidth)
                                {
                                    Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(dstPtr + dstCol)).AsSByte();

                                    Vector128<sbyte> v0 = Vector128.Load(srcPtr + srcCol);
                                    Vector128<short> mul0 = Ssse3.MultiplyAddAdjacent(ones8_128, v0);
                                    Vector128<sbyte> final0 = Sse2.PackSignedSaturate(mul0, mul0);

                                    Unsafe.WriteUnaligned<long>(dstPtr + dstCol, Vector128.Add(dstVec, final0).AsInt64().ToScalar());

                                    srcCol += 16;
                                    dstCol += 8;
                                }
                            }

                            (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                        }

                        rowMod++;
                        if (rowMod >= subSample)
                        {
                            rowMod = 0;
                            dstRow++;
                        }
                    }
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                int safeSimdWidth64 = srcWidth + srcBorder - 64;

                if (Ssse3.IsSupported)
                {
                    Vector128<byte> ones8 = Vector128.Create((byte)1);
                    Vector128<short> ones16 = Vector128.Create((short)1);

                    if (subSample == 4)
                    {
                        int dstRow = startDestRow;
                        int rowMod = subPixelRowOffset;
                        for (int srcY = 0; srcY < source.Height; srcY++)
                        {
                            if (dstRow >= 0 && dstRow < Height)
                            {
                                sbyte* srcPtr = source.GetRow(srcY);
                                sbyte* dstPtr = this.GetRow(dstRow);

                                int srcCol = 0;
                                int dstCol = startDestColumn;

                                (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                                while (srcCol <= safeSimdWidth64 && dstCol + 16 <= dstWidth)
                                {
                                    Vector128<sbyte> dst = Vector128.Load(dstPtr + dstCol);

                                    Vector128<short> mul0 = Ssse3.MultiplyAddAdjacent(ones8, Vector128.Load(srcPtr + srcCol));
                                    Vector128<short> mul1 = Ssse3.MultiplyAddAdjacent(ones8, Vector128.Load(srcPtr + srcCol + 16));
                                    Vector128<short> mul2 = Ssse3.MultiplyAddAdjacent(ones8, Vector128.Load(srcPtr + srcCol + 32));
                                    Vector128<short> mul3 = Ssse3.MultiplyAddAdjacent(ones8, Vector128.Load(srcPtr + srcCol + 48));

                                    Vector128<int> sum0 = Sse2.MultiplyAddAdjacent(mul0, ones16);
                                    Vector128<int> sum1 = Sse2.MultiplyAddAdjacent(mul1, ones16);
                                    Vector128<int> sum2 = Sse2.MultiplyAddAdjacent(mul2, ones16);
                                    Vector128<int> sum3 = Sse2.MultiplyAddAdjacent(mul3, ones16);

                                    Vector128<short> pack0 = Sse2.PackSignedSaturate(sum0, sum1);
                                    Vector128<short> pack1 = Sse2.PackSignedSaturate(sum2, sum3);

                                    Vector128<sbyte> final = Sse2.PackSignedSaturate(pack0, pack1);

                                    Vector128.Store(Vector128.Add(dst, final), dstPtr + dstCol);

                                    srcCol += 64;
                                    dstCol += 16;
                                }

                                // Tier 3: 16 bytes -> 4 pixels cascade
                                int safeSimdWidth16T3 = srcWidth + srcBorder - 16;
                                while (srcCol <= safeSimdWidth16T3 && dstCol + 4 <= dstWidth)
                                {
                                    Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsSByte();

                                    Vector128<sbyte> v0 = Vector128.Load(srcPtr + srcCol);
                                    Vector128<short> mul0 = Ssse3.MultiplyAddAdjacent(ones8, v0);
                                    Vector128<int> sum0 = Sse2.MultiplyAddAdjacent(mul0, ones16);
                                    Vector128<short> pack0 = Sse2.PackSignedSaturate(sum0, sum0);
                                    Vector128<sbyte> final0 = Sse2.PackSignedSaturate(pack0, pack0);

                                    Unsafe.WriteUnaligned<int>(dstPtr + dstCol, Vector128.Add(dstVec, final0).AsInt32().ToScalar());

                                    srcCol += 16;
                                    dstCol += 4;
                                }

                                (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                            }

                            rowMod++;
                            if (rowMod >= subSample)
                            {
                                rowMod = 0;
                                dstRow++;
                            }
                        }
                    }
                    else if (subSample == 2)
                    {
                        int dstRow = startDestRow;
                        int rowMod = subPixelRowOffset;
                        for (int srcY = 0; srcY < source.Height; srcY++)
                        {
                            if (dstRow >= 0 && dstRow < Height)
                            {
                                sbyte* srcPtr = source.GetRow(srcY);
                                sbyte* dstPtr = this.GetRow(dstRow);

                                int srcCol = 0;
                                int dstCol = startDestColumn;

                                (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                                while (srcCol <= safeSimdWidth64 && dstCol + 32 <= dstWidth)
                                {
                                    Vector128<sbyte> dst0 = Vector128.Load(dstPtr + dstCol);
                                    Vector128<sbyte> dst1 = Vector128.Load(dstPtr + dstCol + 16);

                                    Vector128<short> mul0 = Ssse3.MultiplyAddAdjacent(ones8, Vector128.Load(srcPtr + srcCol));
                                    Vector128<short> mul1 = Ssse3.MultiplyAddAdjacent(ones8, Vector128.Load(srcPtr + srcCol + 16));
                                    Vector128<short> mul2 = Ssse3.MultiplyAddAdjacent(ones8, Vector128.Load(srcPtr + srcCol + 32));
                                    Vector128<short> mul3 = Ssse3.MultiplyAddAdjacent(ones8, Vector128.Load(srcPtr + srcCol + 48));

                                    Vector128<sbyte> final0 = Sse2.PackSignedSaturate(mul0, mul1);
                                    Vector128<sbyte> final1 = Sse2.PackSignedSaturate(mul2, mul3);

                                    Vector128.Store(Vector128.Add(dst0, final0), dstPtr + dstCol);
                                    Vector128.Store(Vector128.Add(dst1, final1), dstPtr + dstCol + 16);

                                    srcCol += 64;
                                    dstCol += 32;
                                }

                                // Tier 3: 16 bytes -> 8 pixels cascade
                                int safeSimdWidth16T3 = srcWidth + srcBorder - 16;
                                while (srcCol <= safeSimdWidth16T3 && dstCol + 8 <= dstWidth)
                                {
                                    Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(dstPtr + dstCol)).AsSByte();

                                    Vector128<sbyte> v0 = Vector128.Load(srcPtr + srcCol);
                                    Vector128<short> mul0 = Ssse3.MultiplyAddAdjacent(ones8, v0);
                                    Vector128<sbyte> final0 = Sse2.PackSignedSaturate(mul0, mul0);

                                    Unsafe.WriteUnaligned<long>(dstPtr + dstCol, Vector128.Add(dstVec, final0).AsInt64().ToScalar());

                                    srcCol += 16;
                                    dstCol += 8;
                                }

                                (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                            }

                            rowMod++;
                            if (rowMod >= subSample)
                            {
                                rowMod = 0;
                                dstRow++;
                            }
                        }
                    }
                }
                else if (AdvSimd.IsSupported)
                {
                    if (subSample == 4)
                    {
                        int dstRow = startDestRow;
                        int rowMod = subPixelRowOffset;
                        for (int srcY = 0; srcY < source.Height; srcY++)
                        {
                            if (dstRow >= 0 && dstRow < Height)
                            {
                                sbyte* srcPtr = source.GetRow(srcY);
                                sbyte* dstPtr = this.GetRow(dstRow);

                                int srcCol = 0;
                                int dstCol = startDestColumn;

                                (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                                while (srcCol <= safeSimdWidth64 && dstCol + 16 <= dstWidth)
                                {
                                    Vector128<sbyte> dst = Vector128.Load(dstPtr + dstCol);

                                    Vector128<short> mul0 = AdvSimd.AddPairwiseWidening(Vector128.Load(srcPtr + srcCol));
                                    Vector128<short> mul1 = AdvSimd.AddPairwiseWidening(Vector128.Load(srcPtr + srcCol + 16));
                                    Vector128<short> mul2 = AdvSimd.AddPairwiseWidening(Vector128.Load(srcPtr + srcCol + 32));
                                    Vector128<short> mul3 = AdvSimd.AddPairwiseWidening(Vector128.Load(srcPtr + srcCol + 48));

                                    Vector128<int> sum0 = AdvSimd.AddPairwiseWidening(mul0);
                                    Vector128<int> sum1 = AdvSimd.AddPairwiseWidening(mul1);
                                    Vector128<int> sum2 = AdvSimd.AddPairwiseWidening(mul2);
                                    Vector128<int> sum3 = AdvSimd.AddPairwiseWidening(mul3);

                                    Vector64<short> pack0Low = AdvSimd.ExtractNarrowingSaturateLower(sum0);
                                    Vector128<short> pack0 = AdvSimd.ExtractNarrowingSaturateUpper(pack0Low, sum1);

                                    Vector64<short> pack1Low = AdvSimd.ExtractNarrowingSaturateLower(sum2);
                                    Vector128<short> pack1 = AdvSimd.ExtractNarrowingSaturateUpper(pack1Low, sum3);

                                    Vector64<sbyte> finalLow = AdvSimd.ExtractNarrowingSaturateLower(pack0);
                                    Vector128<sbyte> final = AdvSimd.ExtractNarrowingSaturateUpper(finalLow, pack1);

                                    Vector128.Store(AdvSimd.Add(dst, final), dstPtr + dstCol);

                                    srcCol += 64;
                                    dstCol += 16;
                                }

                                // Tier 3: 16 bytes -> 4 pixels cascade
                                int safeSimdWidth16T3 = srcWidth + srcBorder - 16;
                                while (srcCol <= safeSimdWidth16T3 && dstCol + 4 <= dstWidth)
                                {
                                    Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsSByte();

                                    Vector128<sbyte> v0 = Vector128.Load(srcPtr + srcCol);
                                    Vector128<short> mul0 = AdvSimd.AddPairwiseWidening(v0);
                                    Vector128<int> sum0 = AdvSimd.AddPairwiseWidening(mul0);

                                    Vector64<short> pack0Low = AdvSimd.ExtractNarrowingSaturateLower(sum0);
                                    Vector128<short> pack0 = AdvSimd.ExtractNarrowingSaturateUpper(pack0Low, sum0);

                                    Vector64<sbyte> final0Low = AdvSimd.ExtractNarrowingSaturateLower(pack0);
                                    Vector128<sbyte> final0 = AdvSimd.ExtractNarrowingSaturateUpper(final0Low, pack0);

                                    Unsafe.WriteUnaligned<int>(dstPtr + dstCol, Vector128.Add(dstVec, final0).AsInt32().ToScalar());

                                    srcCol += 16;
                                    dstCol += 4;
                                }

                                (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                            }

                            rowMod++;
                            if (rowMod >= subSample)
                            {
                                rowMod = 0;
                                dstRow++;
                            }
                        }
                    }
                    else if (subSample == 2)
                    {
                        int dstRow = startDestRow;
                        int rowMod = subPixelRowOffset;
                        for (int srcY = 0; srcY < source.Height; srcY++)
                        {
                            if (dstRow >= 0 && dstRow < Height)
                            {
                                sbyte* srcPtr = source.GetRow(srcY);
                                sbyte* dstPtr = this.GetRow(dstRow);

                                int srcCol = 0;
                                int dstCol = startDestColumn;

                                (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                                while (srcCol <= safeSimdWidth64 && dstCol + 32 <= dstWidth)
                                {
                                    Vector128<sbyte> dst0 = Vector128.Load(dstPtr + dstCol);
                                    Vector128<sbyte> dst1 = Vector128.Load(dstPtr + dstCol + 16);

                                    Vector128<short> mul0 = AdvSimd.AddPairwiseWidening(Vector128.Load(srcPtr + srcCol));
                                    Vector128<short> mul1 = AdvSimd.AddPairwiseWidening(Vector128.Load(srcPtr + srcCol + 16));
                                    Vector128<short> mul2 = AdvSimd.AddPairwiseWidening(Vector128.Load(srcPtr + srcCol + 32));
                                    Vector128<short> mul3 = AdvSimd.AddPairwiseWidening(Vector128.Load(srcPtr + srcCol + 48));

                                    Vector64<sbyte> final0Low = AdvSimd.ExtractNarrowingSaturateLower(mul0);
                                    Vector128<sbyte> final0 = AdvSimd.ExtractNarrowingSaturateUpper(final0Low, mul1);

                                    Vector64<sbyte> final1Low = AdvSimd.ExtractNarrowingSaturateLower(mul2);
                                    Vector128<sbyte> final1 = AdvSimd.ExtractNarrowingSaturateUpper(final1Low, mul3);

                                    Vector128.Store(AdvSimd.Add(dst0, final0), dstPtr + dstCol);
                                    Vector128.Store(AdvSimd.Add(dst1, final1), dstPtr + dstCol + 16);

                                    srcCol += 64;
                                    dstCol += 32;
                                }

                                // Tier 3: 16 bytes -> 8 pixels cascade
                                int safeSimdWidth16T3 = srcWidth + srcBorder - 16;
                                while (srcCol <= safeSimdWidth16T3 && dstCol + 8 <= dstWidth)
                                {
                                    Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(dstPtr + dstCol)).AsSByte();

                                    Vector128<sbyte> v0 = Vector128.Load(srcPtr + srcCol);
                                    Vector128<short> mul0 = AdvSimd.AddPairwiseWidening(v0);

                                    Vector64<sbyte> final0Low = AdvSimd.ExtractNarrowingSaturateLower(mul0);
                                    Vector128<sbyte> final0 = AdvSimd.ExtractNarrowingSaturateUpper(final0Low, mul0);

                                    Unsafe.WriteUnaligned<long>(dstPtr + dstCol, Vector128.Add(dstVec, final0).AsInt64().ToScalar());

                                    srcCol += 16;
                                    dstCol += 8;
                                }

                                (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                            }

                            rowMod++;
                            if (rowMod >= subSample)
                            {
                                rowMod = 0;
                                dstRow++;
                            }
                        }
                    }
                }
            }
            else
            {
                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;
                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);

                        int srcCol = 0;
                        int dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);
                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }

                    rowMod++;
                    if (rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private bool BlitSubSampleQuadWord<TFactor>(
            ref Bitmap source, int startDestRow, int subPixelRowOffset,
            int startDestColumn, int subPixelColumnOffset)
            where TFactor : struct, IFactor
        {
            int subSample = default(TFactor).Value;
            int srcWidth = source.Width;
            int dstWidth = this.Width;
            int srcBorder = source.Border;

            int initialSrcCount = subSample - subPixelColumnOffset;
            if (initialSrcCount > srcWidth)
                initialSrcCount = srcWidth;

            if (Avx512Vbmi.IsSupported && Avx512BW.IsSupported)
            {
                Vector512<byte> zero512 = Vector512<byte>.Zero;
                Vector512<sbyte> zeroSbyte = Vector512<sbyte>.Zero;
                Vector512<sbyte> vbmiExtractMask = Vector512<sbyte>.Zero;

                if (subSample == 7)
                {
                    vbmiExtractMask = Vector512.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 64,
                            7, 8, 9, 10, 11, 12, 13, 64,
                            14, 15, 16, 17, 18, 19, 20, 64,
                            21, 22, 23, 24, 25, 26, 27, 64,
                            28, 29, 30, 31, 32, 33, 34, 64,
                            35, 36, 37, 38, 39, 40, 41, 64,
                            42, 43, 44, 45, 46, 47, 48, 64,
                            49, 50, 51, 52, 53, 54, 55, 64
                    ]);
                }
                else if (subSample == 6)
                {
                    vbmiExtractMask = Vector512.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 64, 64,
                            6, 7, 8, 9, 10, 11, 64, 64,
                            12, 13, 14, 15, 16, 17, 64, 64,
                            18, 19, 20, 21, 22, 23, 64, 64,
                            24, 25, 26, 27, 28, 29, 64, 64,
                            30, 31, 32, 33, 34, 35, 64, 64,
                            36, 37, 38, 39, 40, 41, 64, 64,
                            42, 43, 44, 45, 46, 47, 64, 64
                    ]);
                }
                else if (subSample == 5)
                {
                    vbmiExtractMask = Vector512.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 64, 64, 64,
                            5, 6, 7, 8, 9, 64, 64, 64,
                            10, 11, 12, 13, 14, 64, 64, 64,
                            15, 16, 17, 18, 19, 64, 64, 64,
                            20, 21, 22, 23, 24, 64, 64, 64,
                            25, 26, 27, 28, 29, 64, 64, 64,
                            30, 31, 32, 33, 34, 64, 64, 64,
                            35, 36, 37, 38, 39, 64, 64, 64
                    ]);
                }

                Vector512<byte> permuteMask16 = Vector512.Create((ReadOnlySpan<byte>)[
                    0, 4, 16, 20, 32, 36, 48, 52,        // P0 - P7
                    8, 12, 24, 28, 40, 44, 56, 60,       // P8 - P15
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
                ]);

                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;

                int safeSimdWidth512 = srcWidth + srcBorder - (subSample * 8 + 64);

                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);
                        int srcCol = 0;
                        int dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                        if (subSample == 8)
                        {
                            // Tier 1: 128 bytes -> 16 pixels
                            while (srcCol <= safeSimdWidth512 && dstCol + 16 <= dstWidth)
                            {
                                Vector512<sbyte> v0 = Vector512.Load(srcPtr + srcCol);
                                Vector512<sbyte> v1 = Vector512.Load(srcPtr + srcCol + 64);

                                Vector512<ushort> sum0 = Avx512BW.SumAbsoluteDifferences(v0.AsByte(), zero512);
                                Vector512<ushort> sum1 = Avx512BW.SumAbsoluteDifferences(v1.AsByte(), zero512);

                                Vector512<byte> pack01 = Avx512BW.PackUnsignedSaturate(sum0.AsInt16(), sum1.AsInt16());
                                Vector512<byte> ordered01 = Avx512Vbmi.PermuteVar64x8x2(pack01, permuteMask16, zero512);

                                Vector128<sbyte> dstVec = Vector128.Load(dstPtr + dstCol);
                                Vector128.Store(Vector128.Add(dstVec, ordered01.GetLower().GetLower().AsSByte()), dstPtr + dstCol);

                                srcCol += 128;
                                dstCol += 16;
                            }

                            // Tier 2A: 64 bytes -> 8 pixels cascade
                            int safeSimdWidth512T2A = srcWidth + srcBorder - 64;
                            while (srcCol <= safeSimdWidth512T2A && dstCol + 8 <= dstWidth)
                            {
                                Vector512<sbyte> v0 = Vector512.Load(srcPtr + srcCol);
                                Vector512<ushort> sum0 = Avx512BW.SumAbsoluteDifferences(v0.AsByte(), zero512);
                                Vector512<byte> pack01 = Avx512BW.PackUnsignedSaturate(sum0.AsInt16(), sum0.AsInt16());
                                Vector512<byte> ordered01 = Avx512Vbmi.PermuteVar64x8x2(pack01, permuteMask16, zero512);

                                Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(dstPtr + dstCol)).AsSByte();
                                Unsafe.WriteUnaligned<long>(dstPtr + dstCol, Vector128.Add(dstVec, ordered01.GetLower().GetLower().AsSByte()).AsInt64().ToScalar());

                                srcCol += 64;
                                dstCol += 8;
                            }

                            // Tier 2: 32 bytes -> 4 pixels cascade
                            int safeSimdWidth256T2 = srcWidth + srcBorder - 32;
                            while (srcCol <= safeSimdWidth256T2 && dstCol + 4 <= dstWidth)
                            {
                                Vector512<sbyte> v0 = Vector256.Load(srcPtr + srcCol).ToVector512Unsafe();
                                Vector512<ushort> sum0 = Avx512BW.SumAbsoluteDifferences(v0.AsByte(), zero512);
                                Vector512<byte> pack01 = Avx512BW.PackUnsignedSaturate(sum0.AsInt16(), sum0.AsInt16());
                                Vector512<byte> ordered01 = Avx512Vbmi.PermuteVar64x8x2(pack01, permuteMask16, zero512);

                                Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsSByte();
                                Unsafe.WriteUnaligned<int>(dstPtr + dstCol, Vector128.Add(dstVec, ordered01.GetLower().GetLower().AsSByte()).AsInt32().ToScalar());

                                srcCol += 32;
                                dstCol += 4;
                            }
                        }
                        else
                        {
                            // Tier 1: subSample * 16 bytes -> 16 pixels
                            while (srcCol <= safeSimdWidth512 && dstCol + 16 <= dstWidth)
                            {
                                Vector512<sbyte> v0 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + srcCol), vbmiExtractMask, zeroSbyte);
                                Vector512<sbyte> v1 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + srcCol + subSample * 8), vbmiExtractMask, zeroSbyte);

                                Vector512<ushort> sum0 = Avx512BW.SumAbsoluteDifferences(v0.AsByte(), zero512);
                                Vector512<ushort> sum1 = Avx512BW.SumAbsoluteDifferences(v1.AsByte(), zero512);

                                Vector512<byte> pack01 = Avx512BW.PackUnsignedSaturate(sum0.AsInt16(), sum1.AsInt16());
                                Vector512<byte> ordered01 = Avx512Vbmi.PermuteVar64x8x2(pack01, permuteMask16, zero512);

                                Vector128<sbyte> dstVec = Vector128.Load(dstPtr + dstCol);
                                Vector128.Store(Vector128.Add(dstVec, ordered01.GetLower().GetLower().AsSByte()), dstPtr + dstCol);

                                srcCol += subSample * 16;
                                dstCol += 16;
                            }

                            // Tier 2A: subSample * 8 bytes -> 8 pixels cascade
                            int safeSimdWidth512T2A = srcWidth + srcBorder - 64;
                            while (srcCol <= safeSimdWidth512T2A && dstCol + 8 <= dstWidth)
                            {
                                Vector512<sbyte> v0 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + srcCol), vbmiExtractMask, zeroSbyte);
                                Vector512<ushort> sum0 = Avx512BW.SumAbsoluteDifferences(v0.AsByte(), zero512);
                                Vector512<byte> pack01 = Avx512BW.PackUnsignedSaturate(sum0.AsInt16(), sum0.AsInt16());
                                Vector512<byte> ordered01 = Avx512Vbmi.PermuteVar64x8x2(pack01, permuteMask16, zero512);

                                Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(dstPtr + dstCol)).AsSByte();
                                Unsafe.WriteUnaligned<long>(dstPtr + dstCol, Vector128.Add(dstVec, ordered01.GetLower().GetLower().AsSByte()).AsInt64().ToScalar());

                                srcCol += subSample * 8;
                                dstCol += 8;
                            }

                            // Tier 2: subSample * 4 bytes -> 4 pixels cascade
                            int safeSimdWidth256T2 = srcWidth + srcBorder - 32;
                            while (srcCol <= safeSimdWidth256T2 && dstCol + 4 <= dstWidth)
                            {
                                Vector512<sbyte> v0 = Avx512Vbmi.PermuteVar64x8x2(Vector256.Load(srcPtr + srcCol).ToVector512Unsafe(), vbmiExtractMask, zeroSbyte);
                                Vector512<ushort> sum0 = Avx512BW.SumAbsoluteDifferences(v0.AsByte(), zero512);
                                Vector512<byte> pack01 = Avx512BW.PackUnsignedSaturate(sum0.AsInt16(), sum0.AsInt16());
                                Vector512<byte> ordered01 = Avx512Vbmi.PermuteVar64x8x2(pack01, permuteMask16, zero512);

                                Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsSByte();
                                Unsafe.WriteUnaligned<int>(dstPtr + dstCol, Vector128.Add(dstVec, ordered01.GetLower().GetLower().AsSByte()).AsInt32().ToScalar());

                                srcCol += subSample * 4;
                                dstCol += 4;
                            }
                        }

                        // Tier 3: 2 bytes Vector128 fallback
                        int safeSimdWidth128T3 = srcWidth + srcBorder - 16;
                        if (srcCol <= safeSimdWidth128T3 && dstCol + 2 <= dstWidth)
                        {
                            Vector128<byte> zero128 = Vector128<byte>.Zero;
                            Vector128<byte> padMask = zero128;
                            if (subSample == 8)
                                padMask = Vector128.Create((ReadOnlySpan<byte>)[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15]);
                            else if (subSample == 7)
                                padMask = Vector128.Create((ReadOnlySpan<byte>)[0, 1, 2, 3, 4, 5, 6, 255, 7, 8, 9, 10, 11, 12, 13, 255]);
                            else if (subSample == 6)
                                padMask = Vector128.Create((ReadOnlySpan<byte>)[0, 1, 2, 3, 4, 5, 255, 255, 6, 7, 8, 9, 10, 11, 255, 255]);
                            else if (subSample == 5)
                                padMask = Vector128.Create((ReadOnlySpan<byte>)[0, 1, 2, 3, 4, 255, 255, 255, 5, 6, 7, 8, 9, 255, 255, 255]);

                            Vector128<byte> gatherBytes = Vector128.Create((ReadOnlySpan<byte>)[0, 4, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255]);

                            while (srcCol <= safeSimdWidth128T3 && dstCol + 2 <= dstWidth)
                            {
                                Vector128<byte> v0 = Vector128.Load(srcPtr + srcCol).AsByte();
                                v0 = Ssse3.Shuffle(v0, padMask);

                                Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0, zero128);
                                Vector128<byte> pack0 = Sse2.PackUnsignedSaturate(sum0.AsInt16(), sum0.AsInt16());
                                Vector128<byte> p0 = Ssse3.Shuffle(pack0, gatherBytes);

                                Vector128<byte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<short>(dstPtr + dstCol)).AsByte();
                                Vector128<byte> added = Vector128.Add(dstVec, p0);
                                Unsafe.WriteUnaligned<short>(dstPtr + dstCol, added.AsInt16().ToScalar());

                                srcCol += subSample * 2;
                                dstCol += 2;
                            }
                        }

                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }

                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }
            else if (Avx2.IsSupported)
            {
                Vector256<byte> zero256 = Vector256<byte>.Zero;
                Vector256<byte> alignMask = Vector256<byte>.Zero;

                if (subSample != 8)
                {
                    if (subSample == 7)
                    {
                        alignMask = Vector256.Create((ReadOnlySpan<byte>)[
                            0, 1, 2, 3, 4, 5, 6, 255, 7, 8, 9, 10, 11, 12, 13, 255,
                            0, 1, 2, 3, 4, 5, 6, 255, 7, 8, 9, 10, 11, 12, 13, 255
                        ]);
                    }
                    else if (subSample == 6)
                    {
                        alignMask = Vector256.Create((ReadOnlySpan<byte>)[
                            0, 1, 2, 3, 4, 5, 255, 255, 6, 7, 8, 9, 10, 11, 255, 255,
                            0, 1, 2, 3, 4, 5, 255, 255, 6, 7, 8, 9, 10, 11, 255, 255
                        ]);
                    }
                    else if (subSample == 5)
                    {
                        alignMask = Vector256.Create((ReadOnlySpan<byte>)[
                            0, 1, 2, 3, 4, 255, 255, 255, 5, 6, 7, 8, 9, 255, 255, 255,
                            0, 1, 2, 3, 4, 255, 255, 255, 5, 6, 7, 8, 9, 255, 255, 255
                        ]);
                    }
                }

                Vector256<byte> shuffleMask = Vector256.Create((ReadOnlySpan<byte>)[
                    0, 4, 8, 12, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                        0, 4, 8, 12, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255
                ]);
                Vector256<int> permuteMask = Vector256.Create(0, 4, 1, 5, 0, 0, 0, 0);

                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;

                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);
                        int srcCol = 0;
                        int dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                        int safeSimdWidth256 = srcWidth + srcBorder - (subSample * 14 + 16);

                        if (subSample == 8)
                        {
                            while (srcCol <= safeSimdWidth256 && dstCol + 16 <= dstWidth)
                            {
                                Vector256<ushort> sum0 = Avx2.SumAbsoluteDifferences(zero256, Vector256.Load(srcPtr + srcCol).AsByte());
                                Vector256<ushort> sum1 = Avx2.SumAbsoluteDifferences(zero256, Vector256.Load(srcPtr + srcCol + 32).AsByte());
                                Vector256<ushort> sum2 = Avx2.SumAbsoluteDifferences(zero256, Vector256.Load(srcPtr + srcCol + 64).AsByte());
                                Vector256<ushort> sum3 = Avx2.SumAbsoluteDifferences(zero256, Vector256.Load(srcPtr + srcCol + 96).AsByte());

                                Vector256<byte> packed16_0 = Avx2.PackUnsignedSaturate(sum0.AsInt16(), sum1.AsInt16());
                                Vector256<byte> packed16_1 = Avx2.PackUnsignedSaturate(sum2.AsInt16(), sum3.AsInt16());

                                Vector256<byte> packed8_0 = Avx2.Shuffle(packed16_0, shuffleMask);
                                Vector256<byte> packed8_1 = Avx2.Shuffle(packed16_1, shuffleMask);

                                Vector256<byte> swapped_0 = Avx2.Permute4x64(packed8_0.AsInt64(), 0x4E).AsByte();
                                Vector256<byte> P0_P7_256 = Avx2.UnpackLow(packed8_0.AsInt16(), swapped_0.AsInt16()).AsByte();

                                Vector256<byte> swapped_1 = Avx2.Permute4x64(packed8_1.AsInt64(), 0x4E).AsByte();
                                Vector256<byte> P8_P15_256 = Avx2.UnpackLow(packed8_1.AsInt16(), swapped_1.AsInt16()).AsByte();

                                Vector256<byte> packedSums256 = Avx2.UnpackLow(P0_P7_256.AsInt64(), P8_P15_256.AsInt64()).AsByte();
                                Vector128<sbyte> v16 = packedSums256.GetLower().AsSByte();

                                Vector128<sbyte> dst16 = Vector128.Load(dstPtr + dstCol);
                                Vector128.Store(Vector128.Add(dst16, v16), dstPtr + dstCol);

                                srcCol += 128;
                                dstCol += 16;
                            }

                            // Tier 2: 32 bytes -> 4 pixels cascade
                            int safeSimdWidth256T2 = srcWidth + srcBorder - 32;
                            while (srcCol <= safeSimdWidth256T2 && dstCol + 4 <= dstWidth)
                            {
                                Vector256<ushort> sum0 = Avx2.SumAbsoluteDifferences(zero256, Vector256.Load(srcPtr + srcCol).AsByte());

                                Vector256<byte> packed16_0 = Avx2.PackUnsignedSaturate(sum0.AsInt16(), sum0.AsInt16());
                                Vector256<byte> packed8_0 = Avx2.Shuffle(packed16_0, shuffleMask);
                                Vector256<byte> swapped_0 = Avx2.Permute4x64(packed8_0.AsInt64(), 0x4E).AsByte();
                                Vector256<byte> P0_P7_256 = Avx2.UnpackLow(packed8_0.AsInt16(), swapped_0.AsInt16()).AsByte();

                                Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsSByte();
                                Unsafe.WriteUnaligned<int>(dstPtr + dstCol, Vector128.Add(dstVec, P0_P7_256.GetLower().AsSByte()).AsInt32().ToScalar());

                                srcCol += 32;
                                dstCol += 4;
                            }
                        }
                        else
                        {
                            while (srcCol <= safeSimdWidth256 && dstCol + 16 <= dstWidth)
                            {
                                Vector256<sbyte> v0 = Avx2.Shuffle(Vector256.Create(
                                    Vector128.Load(srcPtr + srcCol),
                                    Vector128.Load(srcPtr + srcCol + subSample * 2)).AsByte(), alignMask).AsSByte();

                                Vector256<sbyte> v1 = Avx2.Shuffle(Vector256.Create(
                                    Vector128.Load(srcPtr + srcCol + subSample * 4),
                                    Vector128.Load(srcPtr + srcCol + subSample * 6)).AsByte(), alignMask).AsSByte();

                                Vector256<sbyte> v2 = Avx2.Shuffle(Vector256.Create(
                                    Vector128.Load(srcPtr + srcCol + subSample * 8),
                                    Vector128.Load(srcPtr + srcCol + subSample * 10)).AsByte(), alignMask).AsSByte();

                                Vector256<sbyte> v3 = Avx2.Shuffle(Vector256.Create(
                                    Vector128.Load(srcPtr + srcCol + subSample * 12),
                                    Vector128.Load(srcPtr + srcCol + subSample * 14)).AsByte(), alignMask).AsSByte();

                                Vector256<ushort> sum0 = Avx2.SumAbsoluteDifferences(v0.AsByte(), zero256);
                                Vector256<ushort> sum1 = Avx2.SumAbsoluteDifferences(v1.AsByte(), zero256);
                                Vector256<ushort> sum2 = Avx2.SumAbsoluteDifferences(v2.AsByte(), zero256);
                                Vector256<ushort> sum3 = Avx2.SumAbsoluteDifferences(v3.AsByte(), zero256);

                                Vector256<byte> pack01 = Avx2.PackUnsignedSaturate(sum0.AsInt16(), sum1.AsInt16());
                                Vector256<byte> pack23 = Avx2.PackUnsignedSaturate(sum2.AsInt16(), sum3.AsInt16());

                                Vector256<byte> ordered01 = Avx2.Permute4x64(pack01.AsInt64(), 0b_11_01_10_00).AsByte();
                                Vector256<byte> ordered23 = Avx2.Permute4x64(pack23.AsInt64(), 0b_11_01_10_00).AsByte();

                                Vector256<byte> shuf01 = Avx2.Shuffle(ordered01, shuffleMask);
                                Vector256<byte> shuf23 = Avx2.Shuffle(ordered23, shuffleMask);

                                Vector256<byte> shuf23_shifted = Avx2.ShiftLeftLogical128BitLane(shuf23, 4);
                                Vector256<byte> combined = Avx2.Or(shuf01, shuf23_shifted);

                                Vector128<sbyte> v16 = Avx2.PermuteVar8x32(combined.AsInt32(), permuteMask).GetLower().AsSByte();

                                Vector128<sbyte> dst16 = Vector128.Load(dstPtr + dstCol);
                                Vector128.Store(Vector128.Add(dst16, v16), dstPtr + dstCol);

                                srcCol += subSample * 16;
                                dstCol += 16;
                            }

                            // Tier 2: subSample * 4 bytes -> 4 pixels cascade
                            int safeSimdWidth256T2 = srcWidth + srcBorder - (subSample * 2 + 16);
                            while (srcCol <= safeSimdWidth256T2 && dstCol + 4 <= dstWidth)
                            {
                                Vector256<sbyte> v0 = Avx2.Shuffle(Vector256.Create(
                                    Vector128.Load(srcPtr + srcCol),
                                    Vector128.Load(srcPtr + srcCol + subSample * 2)).AsByte(), alignMask).AsSByte();

                                Vector256<ushort> sum0 = Avx2.SumAbsoluteDifferences(v0.AsByte(), zero256);
                                Vector256<byte> pack01 = Avx2.PackUnsignedSaturate(sum0.AsInt16(), sum0.AsInt16());
                                Vector256<byte> ordered01 = Avx2.Permute4x64(pack01.AsInt64(), 0b_11_01_10_00).AsByte();
                                Vector256<byte> shuf01 = Avx2.Shuffle(ordered01, shuffleMask);

                                Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsSByte();
                                Unsafe.WriteUnaligned<int>(dstPtr + dstCol, Vector128.Add(dstVec, shuf01.GetLower().AsSByte()).AsInt32().ToScalar());

                                srcCol += subSample * 4;
                                dstCol += 4;
                            }
                        }

                        // Tier 3: 2 bytes Vector128 fallback
                        int safeSimdWidth128T3 = srcWidth + srcBorder - 16;
                        if (srcCol <= safeSimdWidth128T3 && dstCol + 2 <= dstWidth)
                        {
                            Vector128<byte> zero128 = Vector128<byte>.Zero;
                            Vector128<byte> padMask = zero128;

                            if (subSample == 8)
                                padMask = Vector128.Create((ReadOnlySpan<byte>)[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15]);
                            else if (subSample == 7)
                                padMask = Vector128.Create((ReadOnlySpan<byte>)[0, 1, 2, 3, 4, 5, 6, 255, 7, 8, 9, 10, 11, 12, 13, 255]);
                            else if (subSample == 6)
                                padMask = Vector128.Create((ReadOnlySpan<byte>)[0, 1, 2, 3, 4, 5, 255, 255, 6, 7, 8, 9, 10, 11, 255, 255]);
                            else if (subSample == 5)
                                padMask = Vector128.Create((ReadOnlySpan<byte>)[0, 1, 2, 3, 4, 255, 255, 255, 5, 6, 7, 8, 9, 255, 255, 255]);

                            Vector128<byte> gatherBytes = Vector128.Create((ReadOnlySpan<byte>)[0, 4, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255]);

                            while (srcCol <= safeSimdWidth128T3 && dstCol + 2 <= dstWidth)
                            {
                                Vector128<byte> v0 = Vector128.Load(srcPtr + srcCol).AsByte();
                                v0 = Ssse3.Shuffle(v0, padMask);

                                Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0, zero128);
                                Vector128<byte> pack0 = Sse2.PackUnsignedSaturate(sum0.AsInt16(), sum0.AsInt16());
                                Vector128<byte> p0 = Ssse3.Shuffle(pack0, gatherBytes);

                                Vector128<byte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<short>(dstPtr + dstCol)).AsByte();
                                Vector128<byte> added = Vector128.Add(dstVec, p0);
                                Unsafe.WriteUnaligned<short>(dstPtr + dstCol, added.AsInt16().ToScalar());

                                srcCol += subSample * 2;
                                dstCol += 2;
                            }
                        }


                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }

                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }
            else if (Ssse3.IsSupported)
            {
                Vector128<byte> zero128 = Vector128<byte>.Zero;
                Vector128<byte> alignMask = Vector128<byte>.Zero;

                if (subSample != 8)
                {
                    if (subSample == 7)
                    {
                        alignMask = Vector128.Create((ReadOnlySpan<byte>)[
                            0, 1, 2, 3, 4, 5, 6, 255, 7, 8, 9, 10, 11, 12, 13, 255
                        ]);
                    }
                    else if (subSample == 6)
                    {
                        alignMask = Vector128.Create((ReadOnlySpan<byte>)[
                            0, 1, 2, 3, 4, 5, 255, 255, 6, 7, 8, 9, 10, 11, 255, 255
                        ]);
                    }
                    else if (subSample == 5)
                    {
                        alignMask = Vector128.Create((ReadOnlySpan<byte>)[
                            0, 1, 2, 3, 4, 255, 255, 255, 5, 6, 7, 8, 9, 255, 255, 255
                        ]);
                    }
                }

                Vector128<byte> packMask128 = Vector128.Create((ReadOnlySpan<sbyte>)[
                    0, 4, 8, 12, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]).AsByte();

                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;

                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);
                        int srcCol = 0;
                        int dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                        int safeSimdWidth128 = srcWidth + srcBorder - (subSample * 14 + 16);

                        if (subSample == 8)
                        {
                            while (srcCol <= safeSimdWidth128 && dstCol + 16 <= dstWidth)
                            {
                                Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(zero128, Vector128.Load(srcPtr + srcCol).AsByte());
                                Vector128<ushort> sum1 = Sse2.SumAbsoluteDifferences(zero128, Vector128.Load(srcPtr + srcCol + 16).AsByte());
                                Vector128<ushort> sum2 = Sse2.SumAbsoluteDifferences(zero128, Vector128.Load(srcPtr + srcCol + 32).AsByte());
                                Vector128<ushort> sum3 = Sse2.SumAbsoluteDifferences(zero128, Vector128.Load(srcPtr + srcCol + 48).AsByte());
                                Vector128<ushort> sum4 = Sse2.SumAbsoluteDifferences(zero128, Vector128.Load(srcPtr + srcCol + 64).AsByte());
                                Vector128<ushort> sum5 = Sse2.SumAbsoluteDifferences(zero128, Vector128.Load(srcPtr + srcCol + 80).AsByte());
                                Vector128<ushort> sum6 = Sse2.SumAbsoluteDifferences(zero128, Vector128.Load(srcPtr + srcCol + 96).AsByte());
                                Vector128<ushort> sum7 = Sse2.SumAbsoluteDifferences(zero128, Vector128.Load(srcPtr + srcCol + 112).AsByte());

                                Vector128<byte> packed16_0 = Sse2.PackUnsignedSaturate(sum0.AsInt16(), sum1.AsInt16());
                                Vector128<byte> packed16_1 = Sse2.PackUnsignedSaturate(sum2.AsInt16(), sum3.AsInt16());
                                Vector128<byte> packed16_2 = Sse2.PackUnsignedSaturate(sum4.AsInt16(), sum5.AsInt16());
                                Vector128<byte> packed16_3 = Sse2.PackUnsignedSaturate(sum6.AsInt16(), sum7.AsInt16());

                                Vector128<byte> packed8_0 = Ssse3.Shuffle(packed16_0, packMask128);
                                Vector128<byte> packed8_1 = Ssse3.Shuffle(packed16_1, packMask128);
                                Vector128<byte> packed8_2 = Ssse3.Shuffle(packed16_2, packMask128);
                                Vector128<byte> packed8_3 = Ssse3.Shuffle(packed16_3, packMask128);

                                Vector128<sbyte> half0 = Sse2.UnpackLow(packed8_0.AsInt32(), packed8_1.AsInt32()).AsSByte();
                                Vector128<sbyte> half1 = Sse2.UnpackLow(packed8_2.AsInt32(), packed8_3.AsInt32()).AsSByte();

                                Vector128<sbyte> packedSums = Sse2.UnpackLow(half0.AsInt64(), half1.AsInt64()).AsSByte();

                                Vector128<sbyte> dstVec = Vector128.Load(dstPtr + dstCol);
                                Vector128.Store(Vector128.Add(dstVec, packedSums), dstPtr + dstCol);

                                srcCol += 128;
                                dstCol += 16;
                            }
                        }
                        else
                        {
                            while (srcCol <= safeSimdWidth128 && dstCol + 16 <= dstWidth)
                            {
                                Vector128<sbyte> v0 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol).AsByte(), alignMask).AsSByte();
                                Vector128<sbyte> v1 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol + subSample * 2).AsByte(), alignMask).AsSByte();
                                Vector128<sbyte> v2 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol + subSample * 4).AsByte(), alignMask).AsSByte();
                                Vector128<sbyte> v3 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol + subSample * 6).AsByte(), alignMask).AsSByte();
                                Vector128<sbyte> v4 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol + subSample * 8).AsByte(), alignMask).AsSByte();
                                Vector128<sbyte> v5 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol + subSample * 10).AsByte(), alignMask).AsSByte();
                                Vector128<sbyte> v6 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol + subSample * 12).AsByte(), alignMask).AsSByte();
                                Vector128<sbyte> v7 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol + subSample * 14).AsByte(), alignMask).AsSByte();

                                Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0.AsByte(), zero128);
                                Vector128<ushort> sum1 = Sse2.SumAbsoluteDifferences(v1.AsByte(), zero128);
                                Vector128<ushort> sum2 = Sse2.SumAbsoluteDifferences(v2.AsByte(), zero128);
                                Vector128<ushort> sum3 = Sse2.SumAbsoluteDifferences(v3.AsByte(), zero128);
                                Vector128<ushort> sum4 = Sse2.SumAbsoluteDifferences(v4.AsByte(), zero128);
                                Vector128<ushort> sum5 = Sse2.SumAbsoluteDifferences(v5.AsByte(), zero128);
                                Vector128<ushort> sum6 = Sse2.SumAbsoluteDifferences(v6.AsByte(), zero128);
                                Vector128<ushort> sum7 = Sse2.SumAbsoluteDifferences(v7.AsByte(), zero128);

                                Vector128<byte> packed16_0 = Sse2.PackUnsignedSaturate(sum0.AsInt16(), sum1.AsInt16());
                                Vector128<byte> packed16_1 = Sse2.PackUnsignedSaturate(sum2.AsInt16(), sum3.AsInt16());
                                Vector128<byte> packed16_2 = Sse2.PackUnsignedSaturate(sum4.AsInt16(), sum5.AsInt16());
                                Vector128<byte> packed16_3 = Sse2.PackUnsignedSaturate(sum6.AsInt16(), sum7.AsInt16());

                                Vector128<byte> packed8_0 = Ssse3.Shuffle(packed16_0, packMask128);
                                Vector128<byte> packed8_1 = Ssse3.Shuffle(packed16_1, packMask128);
                                Vector128<byte> packed8_2 = Ssse3.Shuffle(packed16_2, packMask128);
                                Vector128<byte> packed8_3 = Ssse3.Shuffle(packed16_3, packMask128);

                                Vector128<sbyte> half0 = Sse2.UnpackLow(packed8_0.AsInt32(), packed8_1.AsInt32()).AsSByte();
                                Vector128<sbyte> half1 = Sse2.UnpackLow(packed8_2.AsInt32(), packed8_3.AsInt32()).AsSByte();

                                Vector128<sbyte> packedSums = Sse2.UnpackLow(half0.AsInt64(), half1.AsInt64()).AsSByte();

                                Vector128<sbyte> dstVec = Vector128.Load(dstPtr + dstCol);
                                Vector128.Store(Vector128.Add(dstVec, packedSums), dstPtr + dstCol);

                                srcCol += subSample * 16;
                                dstCol += 16;
                            }
                        }

                        // Tier 3: 2 bytes Vector128 fallback
                        int safeSimdWidth128T3 = srcWidth + srcBorder - 16;
                        if (srcCol <= safeSimdWidth128T3 && dstCol + 2 <= dstWidth)
                        {
                            Vector128<byte> padMask = zero128;

                            if (subSample == 8)
                                padMask = Vector128.Create((ReadOnlySpan<byte>)[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15]);
                            else if (subSample == 7)
                                padMask = Vector128.Create((ReadOnlySpan<byte>)[0, 1, 2, 3, 4, 5, 6, 255, 7, 8, 9, 10, 11, 12, 13, 255]);
                            else if (subSample == 6)
                                padMask = Vector128.Create((ReadOnlySpan<byte>)[0, 1, 2, 3, 4, 5, 255, 255, 6, 7, 8, 9, 10, 11, 255, 255]);
                            else if (subSample == 5)
                                padMask = Vector128.Create((ReadOnlySpan<byte>)[0, 1, 2, 3, 4, 255, 255, 255, 5, 6, 7, 8, 9, 255, 255, 255]);

                            Vector128<byte> gatherBytes = Vector128.Create((ReadOnlySpan<byte>)[0, 4, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255]);

                            while (srcCol <= safeSimdWidth128T3 && dstCol + 2 <= dstWidth)
                            {
                                Vector128<byte> v0 = Vector128.Load(srcPtr + srcCol).AsByte();
                                v0 = Ssse3.Shuffle(v0, padMask);

                                Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0, zero128);
                                Vector128<byte> pack0 = Sse2.PackUnsignedSaturate(sum0.AsInt16(), sum0.AsInt16());
                                Vector128<byte> p0 = Ssse3.Shuffle(pack0, gatherBytes);

                                Vector128<byte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<short>(dstPtr + dstCol)).AsByte();
                                Vector128<byte> added = Vector128.Add(dstVec, p0);
                                Unsafe.WriteUnaligned<short>(dstPtr + dstCol, added.AsInt16().ToScalar());

                                srcCol += subSample * 2;
                                dstCol += 2;
                            }
                        }


                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }

                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }
            else
            {
                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;

                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);
                        int srcCol = 0;
                        int dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);
                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }

                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        private bool BlitSubSample128Lane<TFactor>(
            ref Bitmap source, int startDestRow, int subPixelRowOffset,
            int startDestColumn, int subPixelColumnOffset)
            where TFactor : struct, IFactor
        {
            int subSample = default(TFactor).Value;
            int srcWidth = source.Width;
            int dstWidth = this.Width;
            int srcBorder = source.Border;

            int initialSrcCount = subSample - subPixelColumnOffset;
            if (initialSrcCount > srcWidth)
                initialSrcCount = srcWidth;

            if (Avx512Vbmi.IsSupported && Avx512BW.IsSupported)
            {
                Vector512<byte> zero512 = Vector512<byte>.Zero;
                Vector512<sbyte> vbmiExtractMask = zero512.AsSByte();

                if (subSample == 15)
                {
                    vbmiExtractMask = Vector512.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 64,
                            15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 64,
                            30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 64,
                            45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 64
                    ]);
                }
                else if (subSample == 14)
                {
                    vbmiExtractMask = Vector512.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 64, 64,
                            14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 64, 64,
                            28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 64, 64,
                            42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 64, 64
                    ]);
                }
                else if (subSample == 13)
                {
                    vbmiExtractMask = Vector512.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 64, 64, 64,
                            13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 64, 64, 64,
                            26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 64, 64, 64,
                            39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 64, 64, 64
                    ]);
                }
                else if (subSample == 12)
                {
                    vbmiExtractMask = Vector512.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 64, 64, 64, 64,
                            12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 64, 64, 64, 64,
                            24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 64, 64, 64, 64,
                            36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 64, 64, 64, 64
                    ]);
                }
                else if (subSample == 11)
                {
                    vbmiExtractMask = Vector512.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 64, 64, 64, 64, 64,
                            11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 64, 64, 64, 64, 64,
                            22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 64, 64, 64, 64, 64,
                            33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 64, 64, 64, 64, 64
                    ]);
                }
                else if (subSample == 10)
                {
                    vbmiExtractMask = Vector512.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 64, 64, 64, 64, 64, 64,
                            10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 64, 64, 64, 64, 64, 64,
                            20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 64, 64, 64, 64, 64, 64,
                            30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 64, 64, 64, 64, 64, 64
                    ]);
                }

                Vector512<byte> permuteMask = Vector512.Create((ReadOnlySpan<byte>)[
                    0, 16, 32, 48, 8, 24, 40, 56, 64, 80, 96, 112, 72, 88, 104, 120,
                        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
                ]);

                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;

                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);
                        int srcCol = 0;
                        int dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                        // Tier 1: 16 pixels
                        int safeSimdWidth512 = srcWidth + srcBorder - (subSample * 12 + 64);
                        while (srcCol <= safeSimdWidth512 && dstCol + 16 <= dstWidth)
                        {
                            Vector512<sbyte> v0 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + srcCol), vbmiExtractMask, zero512.AsSByte());
                            Vector512<sbyte> v1 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + srcCol + subSample * 4), vbmiExtractMask, zero512.AsSByte());
                            Vector512<sbyte> v2 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + srcCol + subSample * 8), vbmiExtractMask, zero512.AsSByte());
                            Vector512<sbyte> v3 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + srcCol + subSample * 12), vbmiExtractMask, zero512.AsSByte());

                            Vector512<ushort> sum0 = Avx512BW.SumAbsoluteDifferences(v0.AsByte(), zero512);
                            Vector512<ushort> sum1 = Avx512BW.SumAbsoluteDifferences(v1.AsByte(), zero512);
                            Vector512<ushort> sum2 = Avx512BW.SumAbsoluteDifferences(v2.AsByte(), zero512);
                            Vector512<ushort> sum3 = Avx512BW.SumAbsoluteDifferences(v3.AsByte(), zero512);

                            Vector512<ushort> total0 = Avx512BW.Add(sum0, Avx512BW.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
                            Vector512<ushort> total1 = Avx512BW.Add(sum1, Avx512BW.AlignRight(sum1.AsByte(), sum1.AsByte(), 8).AsUInt16());
                            Vector512<ushort> total2 = Avx512BW.Add(sum2, Avx512BW.AlignRight(sum2.AsByte(), sum2.AsByte(), 8).AsUInt16());
                            Vector512<ushort> total3 = Avx512BW.Add(sum3, Avx512BW.AlignRight(sum3.AsByte(), sum3.AsByte(), 8).AsUInt16());

                            Vector512<byte> pack01 = Avx512BW.PackUnsignedSaturate(total0.AsInt16(), total1.AsInt16());
                            Vector512<byte> pack23 = Avx512BW.PackUnsignedSaturate(total2.AsInt16(), total3.AsInt16());

                            Vector512<byte> ordered03 = Avx512Vbmi.PermuteVar64x8x2(pack01, permuteMask, pack23);

                            Vector128<sbyte> dstVec = Vector128.Load(dstPtr + dstCol);
                            Vector128<sbyte> final = Vector128.Add(dstVec, ordered03.GetLower().GetLower().AsSByte());

                            final.Store(dstPtr + dstCol);

                            srcCol += subSample * 16;
                            dstCol += 16;
                        }

                        //// Tier 2A: 8 pixels
                        int safeSimdWidth512T2A = srcWidth + srcBorder - (subSample * 4 + 64);
                        while (srcCol <= safeSimdWidth512T2A && dstCol + 8 <= dstWidth)
                        {
                            Vector512<sbyte> v0 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + srcCol), vbmiExtractMask, zero512.AsSByte());
                            Vector512<sbyte> v1 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + srcCol + subSample * 4), vbmiExtractMask, zero512.AsSByte());

                            Vector512<ushort> sum0 = Avx512BW.SumAbsoluteDifferences(v0.AsByte(), zero512);
                            Vector512<ushort> sum1 = Avx512BW.SumAbsoluteDifferences(v1.AsByte(), zero512);

                            Vector512<ushort> total0 = Avx512BW.Add(sum0, Avx512BW.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
                            Vector512<ushort> total1 = Avx512BW.Add(sum1, Avx512BW.AlignRight(sum1.AsByte(), sum1.AsByte(), 8).AsUInt16());

                            Vector512<byte> pack01 = Avx512BW.PackUnsignedSaturate(total0.AsInt16(), total1.AsInt16());

                            Vector512<byte> ordered01 = Avx512Vbmi.PermuteVar64x8x2(pack01, permuteMask, zero512);

                            Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(dstPtr + dstCol)).AsSByte();
                            Unsafe.WriteUnaligned<long>(dstPtr + dstCol, Vector128.Add(dstVec, ordered01.GetLower().GetLower().AsSByte()).AsInt64().ToScalar());

                            srcCol += subSample * 8;
                            dstCol += 8;
                        }

                        // Tier 2: 4 pixels
                        int safeSimdWidth512T2 = srcWidth + srcBorder - 64;
                        while (srcCol <= safeSimdWidth512T2 && dstCol + 4 <= dstWidth)
                        {
                            Vector512<sbyte> v0 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + srcCol), vbmiExtractMask, zero512.AsSByte());

                            Vector512<ushort> sum0 = Avx512BW.SumAbsoluteDifferences(v0.AsByte(), zero512);
                            Vector512<ushort> total0 = Avx512BW.Add(sum0, Avx512BW.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());

                            Vector512<byte> pack0 = Avx512BW.PackUnsignedSaturate(total0.AsInt16(), total0.AsInt16());
                            Vector512<byte> ordered0 = Avx512Vbmi.PermuteVar64x8x2(pack0, permuteMask, zero512);

                            Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsSByte();
                            Unsafe.WriteUnaligned<int>(dstPtr + dstCol, Vector128.Add(dstVec, ordered0.GetLower().GetLower().AsSByte()).AsInt32().ToScalar());

                            srcCol += subSample * 4;
                            dstCol += 4;
                        }

                        // Tier 3: 1 pixel (Vector128 fallback)
                        int safeSimdWidth128T3 = srcWidth + srcBorder - 16;
                        if (srcCol <= safeSimdWidth128T3 && dstCol + 1 <= dstWidth)
                        {
                            Vector128<byte> zero128 = Vector128<byte>.Zero;
                            Vector128<sbyte> padMask = zero128.AsSByte();
                            if (subSample == 15) padMask = Vector128.Create((ReadOnlySpan<sbyte>)[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, -1]);
                            else if (subSample == 14) padMask = Vector128.Create((ReadOnlySpan<sbyte>)[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, -1, -1]);
                            else if (subSample == 13) padMask = Vector128.Create((ReadOnlySpan<sbyte>)[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, -1, -1, -1]);
                            else if (subSample == 12) padMask = Vector128.Create((ReadOnlySpan<sbyte>)[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, -1, -1, -1, -1]);
                            else if (subSample == 11) padMask = Vector128.Create((ReadOnlySpan<sbyte>)[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, -1, -1, -1, -1, -1]);
                            else if (subSample == 10) padMask = Vector128.Create((ReadOnlySpan<sbyte>)[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, -1, -1, -1, -1, -1, -1]);

                            while (srcCol <= safeSimdWidth128T3 && dstCol + 1 <= dstWidth)
                            {
                                Vector128<sbyte> v0 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol), padMask);

                                Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0.AsByte(), zero128);
                                Vector128<ushort> total0 = Sse2.Add(sum0, Ssse3.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());

                                Vector128<byte> pack0 = Sse2.PackUnsignedSaturate(total0.AsInt16(), total0.AsInt16());

                                *(dstPtr + dstCol) += pack0.AsSByte().ToScalar();

                                srcCol += subSample;
                                dstCol += 1;
                            }
                        }

                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }

                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }
            else if (Avx2.IsSupported)
            {
                Vector256<byte> zero256 = Vector256<byte>.Zero;
                Vector128<sbyte> padMask = zero256.GetLower().AsSByte(); // Re-use zero register for init

                if (subSample == 15)
                {
                    padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, -1
                    ]);
                }
                else if (subSample == 14)
                {
                    padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, -1, -1
                    ]);
                }
                else if (subSample == 13)
                {
                    padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, -1, -1, -1
                    ]);
                }
                else if (subSample == 12)
                {
                    padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, -1, -1, -1, -1
                    ]);
                }
                else if (subSample == 11)
                {
                    padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, -1, -1, -1, -1, -1
                    ]);
                }
                else if (subSample == 10)
                {
                    padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, -1, -1, -1, -1, -1, -1
                    ]);
                }

                Vector256<sbyte> padMask256 = Vector256.Create(padMask, padMask);
                Vector256<int> gatherDwords = Vector256.Create((ReadOnlySpan<int>)[0, 4, 2, 6, 0, 0, 0, 0]);

                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;

                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);
                        int srcCol = 0;
                        int dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                        // Tier 1: 8 pixels
                        int safeSimdWidth256 = srcWidth + srcBorder - (subSample * 7 + 16);
                        while (srcCol <= safeSimdWidth256 && dstCol + 8 <= dstWidth)
                        {
                            Vector256<sbyte> v0 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + srcCol), Vector128.Load(srcPtr + srcCol + subSample)), padMask256);
                            Vector256<sbyte> v1 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + srcCol + subSample * 2), Vector128.Load(srcPtr + srcCol + subSample * 3)), padMask256);
                            Vector256<sbyte> v2 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + srcCol + subSample * 4), Vector128.Load(srcPtr + srcCol + subSample * 5)), padMask256);
                            Vector256<sbyte> v3 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + srcCol + subSample * 6), Vector128.Load(srcPtr + srcCol + subSample * 7)), padMask256);

                            Vector256<ushort> sum0 = Avx2.SumAbsoluteDifferences(v0.AsByte(), zero256);
                            Vector256<ushort> sum1 = Avx2.SumAbsoluteDifferences(v1.AsByte(), zero256);
                            Vector256<ushort> sum2 = Avx2.SumAbsoluteDifferences(v2.AsByte(), zero256);
                            Vector256<ushort> sum3 = Avx2.SumAbsoluteDifferences(v3.AsByte(), zero256);

                            Vector256<ushort> total0 = Avx2.Add(sum0, Avx2.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
                            Vector256<ushort> total1 = Avx2.Add(sum1, Avx2.AlignRight(sum1.AsByte(), sum1.AsByte(), 8).AsUInt16());
                            Vector256<ushort> total2 = Avx2.Add(sum2, Avx2.AlignRight(sum2.AsByte(), sum2.AsByte(), 8).AsUInt16());
                            Vector256<ushort> total3 = Avx2.Add(sum3, Avx2.AlignRight(sum3.AsByte(), sum3.AsByte(), 8).AsUInt16());

                            Vector256<byte> pack01 = Avx2.PackUnsignedSaturate(total0.AsInt16(), total1.AsInt16());
                            Vector256<byte> pack23 = Avx2.PackUnsignedSaturate(total2.AsInt16(), total3.AsInt16());

                            Vector128<int> p0123 = Avx2.PermuteVar8x32(pack01.AsInt32(), gatherDwords).GetLower();
                            Vector128<int> p4567 = Avx2.PermuteVar8x32(pack23.AsInt32(), gatherDwords).GetLower();

                            Vector128<ushort> p0_7 = Sse41.PackUnsignedSaturate(p0123, p4567);
                            Vector128<byte> final8 = Sse2.PackUnsignedSaturate(p0_7.AsInt16(), Vector128<short>.Zero);

                            Vector128<byte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(dstPtr + dstCol)).AsByte();
                            Vector128<byte> added = Vector128.Add(dstVec, final8);
                            Sse2.StoreScalar((long*)(dstPtr + dstCol), added.AsInt64());

                            srcCol += subSample * 8;
                            dstCol += 8;
                        }

                        // Tier 2A: 4 pixels
                        int safeSimdWidth256T2A = srcWidth + srcBorder - (subSample * 3 + 16);
                        while (srcCol <= safeSimdWidth256T2A && dstCol + 4 <= dstWidth)
                        {
                            Vector256<sbyte> v0 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + srcCol), Vector128.Load(srcPtr + srcCol + subSample)), padMask256);
                            Vector256<sbyte> v1 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + srcCol + subSample * 2), Vector128.Load(srcPtr + srcCol + subSample * 3)), padMask256);

                            Vector256<ushort> sum0 = Avx2.SumAbsoluteDifferences(v0.AsByte(), zero256);
                            Vector256<ushort> sum1 = Avx2.SumAbsoluteDifferences(v1.AsByte(), zero256);

                            Vector256<ushort> total0 = Avx2.Add(sum0, Avx2.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
                            Vector256<ushort> total1 = Avx2.Add(sum1, Avx2.AlignRight(sum1.AsByte(), sum1.AsByte(), 8).AsUInt16());

                            Vector256<byte> pack01 = Avx2.PackUnsignedSaturate(total0.AsInt16(), total1.AsInt16());

                            Vector128<int> p01 = Avx2.PermuteVar8x32(pack01.AsInt32(), gatherDwords).GetLower();
                            Vector128<ushort> p0_3 = Sse41.PackUnsignedSaturate(p01, Vector128<int>.Zero);
                            Vector128<byte> final4 = Sse2.PackUnsignedSaturate(p0_3.AsInt16(), Vector128<short>.Zero);

                            Vector128<sbyte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsSByte();
                            Unsafe.WriteUnaligned<int>(dstPtr + dstCol, Vector128.Add(dstVec, final4.AsSByte()).AsInt32().ToScalar());

                            srcCol += subSample * 4;
                            dstCol += 4;
                        }

                        // Tier 2B: 2 pixels (Vector128 fallback)
                        Vector128<byte> gatherBytes = Vector128.Create((ReadOnlySpan<byte>)[
                            0, 8, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255
                        ]);
                        Vector128<byte> zero128 = Vector128<byte>.Zero;

                        int safeSimdWidth128T2 = srcWidth + srcBorder - (subSample + 16);
                        while (srcCol <= safeSimdWidth128T2 && dstCol + 2 <= dstWidth)
                        {
                            Vector128<sbyte> v0 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol), padMask);
                            Vector128<sbyte> v1 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol + subSample), padMask);

                            Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0.AsByte(), zero128);
                            Vector128<ushort> sum1 = Sse2.SumAbsoluteDifferences(v1.AsByte(), zero128);

                            Vector128<ushort> total0 = Sse2.Add(sum0, Ssse3.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
                            Vector128<ushort> total1 = Sse2.Add(sum1, Ssse3.AlignRight(sum1.AsByte(), sum1.AsByte(), 8).AsUInt16());

                            Vector128<byte> pack01 = Sse2.PackUnsignedSaturate(total0.AsInt16(), total1.AsInt16());
                            Vector128<byte> p01 = Ssse3.Shuffle(pack01, gatherBytes);

                            Vector128<byte> dstVec2 = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<short>(dstPtr + dstCol)).AsByte();
                            Vector128<byte> added2 = Vector128.Add(dstVec2, p01);
                            Unsafe.WriteUnaligned<short>(dstPtr + dstCol, added2.AsInt16().ToScalar());

                            srcCol += subSample * 2;
                            dstCol += 2;
                        }

                        // Tier 3: 1 pixel (Vector128 fallback)
                        int safeSimdWidth128T3 = srcWidth + srcBorder - 16;
                        while (srcCol <= safeSimdWidth128T3 && dstCol + 1 <= dstWidth)
                        {
                            Vector128<sbyte> v0 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol), padMask);

                            Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0.AsByte(), zero128);
                            Vector128<ushort> total0 = Sse2.Add(sum0, Ssse3.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());

                            Vector128<byte> pack0 = Sse2.PackUnsignedSaturate(total0.AsInt16(), total0.AsInt16());

                            *(dstPtr + dstCol) += pack0.AsSByte().ToScalar();

                            srcCol += subSample;
                            dstCol += 1;
                        }

                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }

                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }
            else if (Ssse3.IsSupported)
            {
                Vector128<byte> zero128 = Vector128<byte>.Zero;
                Vector128<sbyte> padMask = zero128.AsSByte();

                if (subSample == 15)
                {
                    padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, -1
                    ]);
                }
                else if (subSample == 14)
                {
                    padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, -1, -1
                    ]);
                }
                else if (subSample == 13)
                {
                    padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, -1, -1, -1
                    ]);
                }
                else if (subSample == 12)
                {
                    padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, -1, -1, -1, -1
                    ]);
                }
                else if (subSample == 11)
                {
                    padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, -1, -1, -1, -1, -1
                    ]);
                }
                else if (subSample == 10)
                {
                    padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, -1, -1, -1, -1, -1, -1
                    ]);
                }

                Vector128<byte> gatherBytes = Vector128.Create((ReadOnlySpan<byte>)[
                    0, 8, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255
                ]);

                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;

                int safeSimdWidth128 = srcWidth + srcBorder - (subSample * 3 + 16);

                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);
                        int srcCol = 0;
                        int dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                        // Tier 1: 4 pixels
                        while (srcCol <= safeSimdWidth128 && dstCol + 4 <= dstWidth)
                        {
                            Vector128<sbyte> v0 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol), padMask);
                            Vector128<sbyte> v1 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol + subSample), padMask);
                            Vector128<sbyte> v2 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol + subSample * 2), padMask);
                            Vector128<sbyte> v3 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol + subSample * 3), padMask);

                            Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0.AsByte(), zero128);
                            Vector128<ushort> sum1 = Sse2.SumAbsoluteDifferences(v1.AsByte(), zero128);
                            Vector128<ushort> sum2 = Sse2.SumAbsoluteDifferences(v2.AsByte(), zero128);
                            Vector128<ushort> sum3 = Sse2.SumAbsoluteDifferences(v3.AsByte(), zero128);

                            Vector128<ushort> total0 = Sse2.Add(sum0, Ssse3.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
                            Vector128<ushort> total1 = Sse2.Add(sum1, Ssse3.AlignRight(sum1.AsByte(), sum1.AsByte(), 8).AsUInt16());
                            Vector128<ushort> total2 = Sse2.Add(sum2, Ssse3.AlignRight(sum2.AsByte(), sum2.AsByte(), 8).AsUInt16());
                            Vector128<ushort> total3 = Sse2.Add(sum3, Ssse3.AlignRight(sum3.AsByte(), sum3.AsByte(), 8).AsUInt16());

                            Vector128<byte> pack01 = Sse2.PackUnsignedSaturate(total0.AsInt16(), total1.AsInt16());
                            Vector128<byte> pack23 = Sse2.PackUnsignedSaturate(total2.AsInt16(), total3.AsInt16());

                            Vector128<byte> p01 = Ssse3.Shuffle(pack01, gatherBytes);
                            Vector128<byte> p23 = Ssse3.Shuffle(pack23, gatherBytes);

                            Vector128<byte> final4 = Sse2.UnpackLow(p01.AsInt16(), p23.AsInt16()).AsByte();

                            Vector128<byte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + dstCol)).AsByte();
                            Vector128<byte> added = Vector128.Add(dstVec, final4);
                            Sse2.StoreScalar((int*)(dstPtr + dstCol), added.AsInt32());

                            srcCol += subSample * 4;
                            dstCol += 4;
                        }

                        // Tier 2: 2 pixels
                        int safeSimdWidth128T2 = srcWidth + srcBorder - (subSample + 16);
                        while (srcCol <= safeSimdWidth128T2 && dstCol + 2 <= dstWidth)
                        {
                            Vector128<sbyte> v0 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol), padMask);
                            Vector128<sbyte> v1 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol + subSample), padMask);

                            Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0.AsByte(), zero128);
                            Vector128<ushort> sum1 = Sse2.SumAbsoluteDifferences(v1.AsByte(), zero128);

                            Vector128<ushort> total0 = Sse2.Add(sum0, Ssse3.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
                            Vector128<ushort> total1 = Sse2.Add(sum1, Ssse3.AlignRight(sum1.AsByte(), sum1.AsByte(), 8).AsUInt16());

                            Vector128<byte> pack01 = Sse2.PackUnsignedSaturate(total0.AsInt16(), total1.AsInt16());
                            Vector128<byte> p01 = Ssse3.Shuffle(pack01, gatherBytes);

                            Vector128<byte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<short>(dstPtr + dstCol)).AsByte();
                            Vector128<byte> added = Vector128.Add(dstVec, p01);
                            Unsafe.WriteUnaligned<short>(dstPtr + dstCol, added.AsInt16().ToScalar());

                            srcCol += subSample * 2;
                            dstCol += 2;
                        }

                        // Tier 3: 1 pixel
                        int safeSimdWidth128T3 = srcWidth + srcBorder - 16;
                        while (srcCol <= safeSimdWidth128T3 && dstCol + 1 <= dstWidth)
                        {
                            Vector128<sbyte> v0 = Ssse3.Shuffle(Vector128.Load(srcPtr + srcCol), padMask);

                            Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0.AsByte(), zero128);
                            Vector128<ushort> total0 = Sse2.Add(sum0, Ssse3.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());

                            Vector128<byte> pack0 = Sse2.PackUnsignedSaturate(total0.AsInt16(), total0.AsInt16());

                            *(dstPtr + dstCol) += pack0.AsSByte().ToScalar();

                            srcCol += subSample;
                            dstCol += 1;
                        }

                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }

                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }
            else
            {
                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;

                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);
                        int srcCol = 0;
                        int dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);
                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }

                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        private bool BlitSubSample9<TFactor>(
    ref Bitmap source, int startDestRow, int subPixelRowOffset,
    int startDestColumn, int subPixelColumnOffset)
    where TFactor : struct, IFactor
        {
            int subSample = default(TFactor).Value;
            int srcWidth = source.Width;
            int dstWidth = this.Width;
            int srcBorder = source.Border;

            int initialSrcCount = subSample - subPixelColumnOffset;
            if (initialSrcCount > srcWidth)
                initialSrcCount = srcWidth;

            if (Avx512Vbmi.IsSupported && Avx512BW.IsSupported)
            {
                Vector512<sbyte> vbmiExtractMask = Vector512.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 64, 64, 64, 64, 64, 64, 64,
                        9, 10, 11, 12, 13, 14, 15, 16, 17, 64, 64, 64, 64, 64, 64, 64,
                        18, 19, 20, 21, 22, 23, 24, 25, 26, 64, 64, 64, 64, 64, 64, 64,
                        27, 28, 29, 30, 31, 32, 33, 34, 35, 64, 64, 64, 64, 64, 64, 64]);

                Vector512<byte> permuteMask = Vector512.Create((ReadOnlySpan<byte>)[
                    0, 16, 32, 48, 8, 24, 40, 56, 64, 80, 96, 112, 72, 88, 104, 120, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);

                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;

                int safeSimdWidth512 = srcWidth + srcBorder - (subSample * 12 + 64);

                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);
                        int srcCol = 0, dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                        int sourceColumn = srcCol;
                        int destColumn = dstCol;

                        while (sourceColumn <= safeSimdWidth512 && destColumn + 16 <= dstWidth)
                        {
                            Vector512<sbyte> v0 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + sourceColumn), vbmiExtractMask, Vector512<sbyte>.Zero);
                            Vector512<sbyte> v1 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + sourceColumn + subSample * 4), vbmiExtractMask, Vector512<sbyte>.Zero);
                            Vector512<sbyte> v2 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + sourceColumn + subSample * 8), vbmiExtractMask, Vector512<sbyte>.Zero);
                            Vector512<sbyte> v3 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + sourceColumn + subSample * 12), vbmiExtractMask, Vector512<sbyte>.Zero);

                            Vector512<ushort> sum0 = Avx512BW.SumAbsoluteDifferences(v0.AsByte(), Vector512<byte>.Zero);
                            Vector512<ushort> sum1 = Avx512BW.SumAbsoluteDifferences(v1.AsByte(), Vector512<byte>.Zero);
                            Vector512<ushort> sum2 = Avx512BW.SumAbsoluteDifferences(v2.AsByte(), Vector512<byte>.Zero);
                            Vector512<ushort> sum3 = Avx512BW.SumAbsoluteDifferences(v3.AsByte(), Vector512<byte>.Zero);

                            Vector512<ushort> total0 = Avx512BW.Add(sum0, Avx512BW.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
                            Vector512<ushort> total1 = Avx512BW.Add(sum1, Avx512BW.AlignRight(sum1.AsByte(), sum1.AsByte(), 8).AsUInt16());
                            Vector512<ushort> total2 = Avx512BW.Add(sum2, Avx512BW.AlignRight(sum2.AsByte(), sum2.AsByte(), 8).AsUInt16());
                            Vector512<ushort> total3 = Avx512BW.Add(sum3, Avx512BW.AlignRight(sum3.AsByte(), sum3.AsByte(), 8).AsUInt16());

                            Vector512<byte> pack01 = Avx512BW.PackUnsignedSaturate(total0.AsInt16(), total1.AsInt16());
                            Vector512<byte> pack23 = Avx512BW.PackUnsignedSaturate(total2.AsInt16(), total3.AsInt16());

                            Vector512<byte> ordered03 = Avx512Vbmi.PermuteVar64x8x2(pack01, permuteMask, pack23);

                            Vector128<sbyte> dstVec = Vector128.Load(dstPtr + destColumn);
                            Vector128<sbyte> final = Vector128.Add(dstVec, ordered03.GetLower().GetLower().AsSByte());
                            final.Store(dstPtr + destColumn);

                            sourceColumn += subSample * 16;
                            destColumn += 16;
                        }

                        // AVX-512 Tier 2 (1x Vector512 load, 4 output pixels)
                        int safeSimdWidth512T2 = srcWidth + srcBorder - 64;
                        while (sourceColumn <= safeSimdWidth512T2 && destColumn + 4 <= dstWidth)
                        {
                            Vector512<sbyte> v0 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + sourceColumn), vbmiExtractMask, Vector512<sbyte>.Zero);
                            Vector512<ushort> sum0 = Avx512BW.SumAbsoluteDifferences(v0.AsByte(), Vector512<byte>.Zero);
                            Vector512<ushort> total0 = Avx512BW.Add(sum0, Avx512BW.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());

                            Vector512<byte> pack0 = Avx512BW.PackUnsignedSaturate(total0.AsInt16(), total0.AsInt16());
                            Vector512<byte> ordered0 = Avx512Vbmi.PermuteVar64x8x2(pack0, permuteMask, pack0);

                            // Utilize cross-platform Vector128 for load/add, and SSE2 StoreScalar (movss) to prevent GPR penalty
                            Vector128<byte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + destColumn)).AsByte();
                            Vector128<byte> vAdd = Vector128.Add(ordered0.GetLower().GetLower(), dstVec);
                            Sse2.StoreScalar((int*)(dstPtr + destColumn), vAdd.AsInt32());

                            sourceColumn += subSample * 4;
                            destColumn += 4;
                        }

                        // Tier 3 Vector128 (1 output pixel fallback)
                        int safeSimdWidth128T3 = srcWidth + srcBorder - 16;
                        if (sourceColumn <= safeSimdWidth128T3 && destColumn + 1 <= dstWidth)
                        {
                            Vector128<sbyte> padMask128 = Vector128.Create((ReadOnlySpan<sbyte>)[
                                0, 1, 2, 3, 4, 5, 6, 7, 8, -1, -1, -1, -1, -1, -1, -1]);
                            while (sourceColumn <= safeSimdWidth128T3 && destColumn + 1 <= dstWidth)
                            {
                                Vector128<sbyte> v0 = Ssse3.Shuffle(Vector128.Load(srcPtr + sourceColumn), padMask128);
                                Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0.AsByte(), Vector128<byte>.Zero);
                                Vector128<ushort> total0 = Sse2.Add(sum0, Ssse3.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
                                Vector128<byte> pack0 = Sse2.PackUnsignedSaturate(total0.AsInt16(), total0.AsInt16());
                                *(dstPtr + destColumn) += pack0.AsSByte().ToScalar();

                                sourceColumn += subSample;
                                destColumn += 1;
                            }
                        }

                        srcCol = sourceColumn;
                        dstCol = destColumn;

                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }
                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }
            else if (Avx2.IsSupported)
            {
                Vector256<byte> zero256 = Vector256<byte>.Zero;
                Vector128<sbyte> padMask = Vector128.Create((ReadOnlySpan<sbyte>)[0, 1, 2, 3, 4, 5, 6, 7, 8, -1, -1, -1, -1, -1, -1, -1]);
                Vector256<sbyte> padMask256 = Vector256.Create(padMask, padMask);
                Vector256<int> gatherDwords = Vector256.Create((ReadOnlySpan<int>)[0, 4, 2, 6, 0, 0, 0, 0]);

                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;

                int safeSimdWidth256 = srcWidth + srcBorder - (subSample * 7 + 16);

                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);
                        int srcCol = 0, dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                        int sourceColumn = srcCol;
                        int destColumn = dstCol;

                        while (sourceColumn <= safeSimdWidth256 && destColumn + 8 <= dstWidth)
                        {
                            Vector256<sbyte> v0 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + sourceColumn), Vector128.Load(srcPtr + sourceColumn + subSample)), padMask256);
                            Vector256<sbyte> v1 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + sourceColumn + subSample * 2), Vector128.Load(srcPtr + sourceColumn + subSample * 3)), padMask256);
                            Vector256<sbyte> v2 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + sourceColumn + subSample * 4), Vector128.Load(srcPtr + sourceColumn + subSample * 5)), padMask256);
                            Vector256<sbyte> v3 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + sourceColumn + subSample * 6), Vector128.Load(srcPtr + sourceColumn + subSample * 7)), padMask256);

                            Vector256<ushort> sum0 = Avx2.SumAbsoluteDifferences(v0.AsByte(), zero256);
                            Vector256<ushort> sum1 = Avx2.SumAbsoluteDifferences(v1.AsByte(), zero256);
                            Vector256<ushort> sum2 = Avx2.SumAbsoluteDifferences(v2.AsByte(), zero256);
                            Vector256<ushort> sum3 = Avx2.SumAbsoluteDifferences(v3.AsByte(), zero256);

                            Vector256<ushort> total0 = Avx2.Add(sum0, Avx2.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
                            Vector256<ushort> total1 = Avx2.Add(sum1, Avx2.AlignRight(sum1.AsByte(), sum1.AsByte(), 8).AsUInt16());
                            Vector256<ushort> total2 = Avx2.Add(sum2, Avx2.AlignRight(sum2.AsByte(), sum2.AsByte(), 8).AsUInt16());
                            Vector256<ushort> total3 = Avx2.Add(sum3, Avx2.AlignRight(sum3.AsByte(), sum3.AsByte(), 8).AsUInt16());

                            Vector256<byte> pack01 = Avx2.PackUnsignedSaturate(total0.AsInt16(), total1.AsInt16());
                            Vector256<byte> pack23 = Avx2.PackUnsignedSaturate(total2.AsInt16(), total3.AsInt16());

                            Vector128<int> p0123 = Avx2.PermuteVar8x32(pack01.AsInt32(), gatherDwords).GetLower();
                            Vector128<int> p4567 = Avx2.PermuteVar8x32(pack23.AsInt32(), gatherDwords).GetLower();

                            Vector128<ushort> p0_7 = Sse41.PackUnsignedSaturate(p0123, p4567);
                            Vector128<byte> final8 = Sse2.PackUnsignedSaturate(p0_7.AsInt16(), Vector128<short>.Zero);

                            Vector128<byte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(dstPtr + destColumn)).AsByte();
                            Vector128<byte> added = Vector128.Add(dstVec, final8);
                            Sse2.StoreScalar((long*)(dstPtr + destColumn), added.AsInt64());

                            sourceColumn += subSample * 8;
                            destColumn += 8;
                        }

                        // AVX2 Tier 2 (1x Vector256 load, 2 output pixels) - does not on average
                        // improve performance as code will always execute the same number
                        // of instructions and will only increase I-Cache pressure

                        // Tier 3 Vector128 (1 output pixel fallback)
                        int safeSimdWidth128T3 = srcWidth + srcBorder - 16;
                        if (sourceColumn <= safeSimdWidth128T3 && destColumn + 1 <= dstWidth)
                        {
                            Vector128<sbyte> padMask128 = Vector128.Create((ReadOnlySpan<sbyte>)[
                                0, 1, 2, 3, 4, 5, 6, 7, 8, -1, -1, -1, -1, -1, -1, -1]);
                            while (sourceColumn <= safeSimdWidth128T3 && destColumn + 1 <= dstWidth)
                            {
                                Vector128<sbyte> v0 = Ssse3.Shuffle(Vector128.Load(srcPtr + sourceColumn), padMask128);
                                Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0.AsByte(), Vector128<byte>.Zero);
                                Vector128<ushort> total0 = Sse2.Add(sum0, Ssse3.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
                                Vector128<byte> pack0 = Sse2.PackUnsignedSaturate(total0.AsInt16(), total0.AsInt16());
                                *(dstPtr + destColumn) += pack0.AsSByte().ToScalar();

                                sourceColumn += subSample;
                                destColumn += 1;
                            }
                        }

                        srcCol = sourceColumn;
                        dstCol = destColumn;

                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }
                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }
            else if (Ssse3.IsSupported)
            {
                Vector128<byte> zero128 = Vector128<byte>.Zero;
                Vector128<sbyte> padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, -1, -1, -1, -1, -1, -1, -1]);

                Vector128<byte> gatherBytes = Vector128.Create((ReadOnlySpan<byte>)[
                    0, 8, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255
                ]);

                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;

                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);
                        int srcCol = 0, dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);

                        int sourceColumn = srcCol;
                        int destColumn = dstCol;

                        int safeSimdWidth128 = srcWidth + srcBorder - (subSample * 3 + 16);
                        while (sourceColumn <= safeSimdWidth128 && destColumn + 4 <= dstWidth)
                        {
                            Vector128<sbyte> v0 = Ssse3.Shuffle(Vector128.Load(srcPtr + sourceColumn), padMask);
                            Vector128<sbyte> v1 = Ssse3.Shuffle(Vector128.Load(srcPtr + sourceColumn + subSample), padMask);
                            Vector128<sbyte> v2 = Ssse3.Shuffle(Vector128.Load(srcPtr + sourceColumn + subSample * 2), padMask);
                            Vector128<sbyte> v3 = Ssse3.Shuffle(Vector128.Load(srcPtr + sourceColumn + subSample * 3), padMask);

                            Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0.AsByte(), zero128);
                            Vector128<ushort> sum1 = Sse2.SumAbsoluteDifferences(v1.AsByte(), zero128);
                            Vector128<ushort> sum2 = Sse2.SumAbsoluteDifferences(v2.AsByte(), zero128);
                            Vector128<ushort> sum3 = Sse2.SumAbsoluteDifferences(v3.AsByte(), zero128);

                            Vector128<ushort> total0 = Sse2.Add(sum0, Ssse3.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
                            Vector128<ushort> total1 = Sse2.Add(sum1, Ssse3.AlignRight(sum1.AsByte(), sum1.AsByte(), 8).AsUInt16());
                            Vector128<ushort> total2 = Sse2.Add(sum2, Ssse3.AlignRight(sum2.AsByte(), sum2.AsByte(), 8).AsUInt16());
                            Vector128<ushort> total3 = Sse2.Add(sum3, Ssse3.AlignRight(sum3.AsByte(), sum3.AsByte(), 8).AsUInt16());

                            Vector128<byte> pack01 = Sse2.PackUnsignedSaturate(total0.AsInt16(), total1.AsInt16());
                            Vector128<byte> pack23 = Sse2.PackUnsignedSaturate(total2.AsInt16(), total3.AsInt16());

                            Vector128<byte> p01 = Ssse3.Shuffle(pack01, gatherBytes);
                            Vector128<byte> p23 = Ssse3.Shuffle(pack23, gatherBytes);

                            Vector128<byte> final4 = Sse2.UnpackLow(p01.AsInt16(), p23.AsInt16()).AsByte();

                            Vector128<byte> dstVec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<int>(dstPtr + destColumn)).AsByte();
                            Vector128<byte> added = Vector128.Add(dstVec, final4);
                            Sse2.StoreScalar((int*)(dstPtr + destColumn), added.AsInt32());

                            sourceColumn += subSample * 4;
                            destColumn += 4;
                        }

                        // Tier 3 Vector128 (1 output pixel fallback)
                        int safeSimdWidth128T3 = srcWidth + srcBorder - 16;
                        if (sourceColumn <= safeSimdWidth128T3 && destColumn + 1 <= dstWidth)
                        {
                            Vector128<sbyte> padMask128 = Vector128.Create((ReadOnlySpan<sbyte>)[
                                0, 1, 2, 3, 4, 5, 6, 7, 8, -1, -1, -1, -1, -1, -1, -1]);
                            while (sourceColumn <= safeSimdWidth128T3 && destColumn + 1 <= dstWidth)
                            {
                                Vector128<sbyte> v0 = Ssse3.Shuffle(Vector128.Load(srcPtr + sourceColumn), padMask128);
                                Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0.AsByte(), Vector128<byte>.Zero);
                                Vector128<ushort> total0 = Sse2.Add(sum0, Ssse3.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
                                Vector128<byte> pack0 = Sse2.PackUnsignedSaturate(total0.AsInt16(), total0.AsInt16());
                                *(dstPtr + destColumn) += pack0.AsSByte().ToScalar();

                                sourceColumn += subSample;
                                destColumn += 1;
                            }
                        }

                        srcCol = sourceColumn;
                        dstCol = destColumn;

                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }
                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }
            else
            {
                int dstRow = startDestRow;
                int rowMod = subPixelRowOffset;
                for (int srcY = 0; srcY < source.Height; srcY++)
                {
                    if (dstRow >= 0 && dstRow < Height)
                    {
                        sbyte* srcPtr = source.GetRow(srcY);
                        sbyte* dstPtr = this.GetRow(dstRow);
                        int srcCol = 0, dstCol = startDestColumn;

                        (srcCol, dstCol) = SubSamplePrologue(srcWidth, dstWidth, subSample, initialSrcCount, srcPtr, dstPtr, srcCol, dstCol);
                        (srcCol, dstCol) = SubSampleEpilogue(srcWidth, dstWidth, subSample, srcPtr, dstPtr, srcCol, dstCol);
                    }
                    if (++rowMod >= subSample)
                    {
                        rowMod = 0;
                        dstRow++;
                    }
                }
            }

            return true;
        }

        //[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        //private bool BlitSubSample9<TFactor>(
        //    ref Bitmap source, int startDestRow, int subPixelRowOffset,
        //    int startDestColumn, int subPixelColumnOffset)
        //    where TFactor : struct, IFactor
        //{
        //    int subSample = default(TFactor).Value;
        //    int srcWidth = source.Width;
        //    int dstWidth = this.Width;

        //    int initialSrcCount = subSample - subPixelColumnOffset;
        //    if (initialSrcCount > srcWidth)
        //        initialSrcCount = srcWidth;

        //    int dstRow = startDestRow;
        //    int rowMod = subPixelRowOffset;

        //    for (int srcY = 0; srcY < source.Height; srcY++)
        //    {
        //        if (dstRow >= 0 && dstRow < Height)
        //        {
        //            sbyte* srcPtr = source.GetRow(srcY);
        //            sbyte* dstPtr = this.GetRow(dstRow);
        //            int srcCol = 0;
        //            int dstCol = startDestColumn;

        //            if (dstCol >= 0 && dstCol < dstWidth)
        //            {
        //                int sum = dstPtr[dstCol];
        //                for (; srcCol < initialSrcCount; srcCol++)
        //                {
        //                    sum += srcPtr[srcCol];
        //                }
        //                dstPtr[dstCol] = (sbyte)sum;
        //            }
        //            else
        //            {
        //                srcCol = initialSrcCount;
        //            }
        //            dstCol++;

        //            // Rapidly advance over any remaining invisible left-edge columns
        //            while (dstCol < 0 && srcCol < srcWidth)
        //            {
        //                srcCol += subSample;
        //                dstCol++;
        //            }

        //            // Boundary Clamp: Prevent sourceColumn from overshooting srcWidth if the
        //            // entire image was clipped off the left edge.
        //            if (srcCol > srcWidth)
        //            {
        //                srcCol = srcWidth;
        //            }

        //            int sourceColumn = srcCol;
        //            int destColumn = dstCol;

        //            if (Avx512Vbmi.IsSupported && Avx512BW.IsSupported)
        //            {
        //                Vector512<sbyte> vbmiExtractMask = Vector512.Create((ReadOnlySpan<sbyte>)[
        //                        0, 1, 2, 3, 4, 5, 6, 7, 8, 64, 64, 64, 64, 64, 64, 64,
        //                        9, 10, 11, 12, 13, 14, 15, 16, 17, 64, 64, 64, 64, 64, 64, 64,
        //                        18, 19, 20, 21, 22, 23, 24, 25, 26, 64, 64, 64, 64, 64, 64, 64,
        //                        27, 28, 29, 30, 31, 32, 33, 34, 35, 64, 64, 64, 64, 64, 64, 64]);

        //                Vector512<byte> permuteMask = Vector512.Create((ReadOnlySpan<byte>)[
        //                    0, 16, 32, 48, 8, 24, 40, 56, 64, 80, 96, 112, 72, 88, 104, 120, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        //                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);

        //                int safeSimdWidth512 = srcWidth + source.Border - (subSample * 12 + 64);

        //                while (sourceColumn <= safeSimdWidth512 && destColumn + 16 <= dstWidth)
        //                {
        //                    Vector512<sbyte> v0 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + sourceColumn), vbmiExtractMask, Vector512<sbyte>.Zero);
        //                    Vector512<sbyte> v1 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + sourceColumn + subSample * 4), vbmiExtractMask, Vector512<sbyte>.Zero);
        //                    Vector512<sbyte> v2 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + sourceColumn + subSample * 8), vbmiExtractMask, Vector512<sbyte>.Zero);
        //                    Vector512<sbyte> v3 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + sourceColumn + subSample * 12), vbmiExtractMask, Vector512<sbyte>.Zero);

        //                    Vector512<ushort> sum0 = Avx512BW.SumAbsoluteDifferences(v0.AsByte(), Vector512<byte>.Zero);
        //                    Vector512<ushort> sum1 = Avx512BW.SumAbsoluteDifferences(v1.AsByte(), Vector512<byte>.Zero);
        //                    Vector512<ushort> sum2 = Avx512BW.SumAbsoluteDifferences(v2.AsByte(), Vector512<byte>.Zero);
        //                    Vector512<ushort> sum3 = Avx512BW.SumAbsoluteDifferences(v3.AsByte(), Vector512<byte>.Zero);

        //                    Vector512<ushort> total0 = Avx512BW.Add(sum0, Avx512BW.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
        //                    Vector512<ushort> total1 = Avx512BW.Add(sum1, Avx512BW.AlignRight(sum1.AsByte(), sum1.AsByte(), 8).AsUInt16());
        //                    Vector512<ushort> total2 = Avx512BW.Add(sum2, Avx512BW.AlignRight(sum2.AsByte(), sum2.AsByte(), 8).AsUInt16());
        //                    Vector512<ushort> total3 = Avx512BW.Add(sum3, Avx512BW.AlignRight(sum3.AsByte(), sum3.AsByte(), 8).AsUInt16());

        //                    Vector512<byte> pack01 = Avx512BW.PackUnsignedSaturate(total0.AsInt16(), total1.AsInt16());
        //                    Vector512<byte> pack23 = Avx512BW.PackUnsignedSaturate(total2.AsInt16(), total3.AsInt16());

        //                    Vector512<byte> ordered03 = Avx512Vbmi.PermuteVar64x8x2(pack01, permuteMask, pack23);

        //                    Vector128<sbyte> dstVec = Vector128.Load(dstPtr + destColumn);
        //                    Vector128<sbyte> final = Vector128.Add(dstVec, ordered03.GetLower().GetLower().AsSByte());

        //                    final.Store(dstPtr + destColumn);

        //                    sourceColumn += subSample * 16;
        //                    destColumn += 16;
        //                }

        //                // AVX-512 Tier 2 (1x Vector512 load, 4 output pixels)
        //                int safeSimdWidth512T2 = srcWidth + source.Border - 64;
        //                while (sourceColumn <= safeSimdWidth512T2 && destColumn + 4 <= dstWidth)
        //                {
        //                    Vector512<sbyte> v0 = Avx512Vbmi.PermuteVar64x8x2(Vector512.Load(srcPtr + sourceColumn), vbmiExtractMask, Vector512<sbyte>.Zero);
        //                    Vector512<ushort> sum0 = Avx512BW.SumAbsoluteDifferences(v0.AsByte(), Vector512<byte>.Zero);
        //                    Vector512<ushort> total0 = Avx512BW.Add(sum0, Avx512BW.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());

        //                    Vector512<byte> pack0 = Avx512BW.PackUnsignedSaturate(total0.AsInt16(), total0.AsInt16());
        //                    Vector512<byte> ordered0 = Avx512Vbmi.PermuteVar64x8x2(pack0, permuteMask, pack0);

        //                    // Utilize cross-platform Vector128 for load/add, and SSE2 StoreScalar (movss) to prevent GPR penalty
        //                    Vector128<byte> dstVec = Vector128.CreateScalarUnsafe(*(int*)(dstPtr + destColumn)).AsByte();
        //                    Vector128<byte> vAdd = Vector128.Add(ordered0.GetLower().GetLower(), dstVec);
        //                    Sse2.StoreScalar((int*)(dstPtr + destColumn), vAdd.AsInt32());

        //                    sourceColumn += subSample * 4;
        //                    destColumn += 4;
        //                }
        //            }

        //            else if (Avx2.IsSupported)
        //            {
        //                Vector256<byte> zero256 = Vector256<byte>.Zero;
        //                Vector128<sbyte> padMask = Vector128.Create((ReadOnlySpan<sbyte>)[0, 1, 2, 3, 4, 5, 6, 7, 8, -1, -1, -1, -1, -1, -1, -1]);

        //                Vector256<sbyte> padMask256 = Vector256.Create(padMask, padMask);

        //                Vector256<int> gatherDwords = Vector256.Create((ReadOnlySpan<int>)[0, 4, 2, 6, 0, 0, 0, 0]);

        //                int safeSimdWidth256 = srcWidth + source.Border - (subSample * 7 + 16);

        //                while (sourceColumn <= safeSimdWidth256 && destColumn + 8 <= dstWidth)
        //                {
        //                    Vector256<sbyte> v0 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + sourceColumn), Vector128.Load(srcPtr + sourceColumn + subSample)), padMask256);
        //                    Vector256<sbyte> v1 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + sourceColumn + subSample * 2), Vector128.Load(srcPtr + sourceColumn + subSample * 3)), padMask256);
        //                    Vector256<sbyte> v2 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + sourceColumn + subSample * 4), Vector128.Load(srcPtr + sourceColumn + subSample * 5)), padMask256);
        //                    Vector256<sbyte> v3 = Avx2.Shuffle(Vector256.Create(Vector128.Load(srcPtr + sourceColumn + subSample * 6), Vector128.Load(srcPtr + sourceColumn + subSample * 7)), padMask256);

        //                    Vector256<ushort> sum0 = Avx2.SumAbsoluteDifferences(v0.AsByte(), zero256);
        //                    Vector256<ushort> sum1 = Avx2.SumAbsoluteDifferences(v1.AsByte(), zero256);
        //                    Vector256<ushort> sum2 = Avx2.SumAbsoluteDifferences(v2.AsByte(), zero256);
        //                    Vector256<ushort> sum3 = Avx2.SumAbsoluteDifferences(v3.AsByte(), zero256);

        //                    Vector256<ushort> total0 = Avx2.Add(sum0, Avx2.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
        //                    Vector256<ushort> total1 = Avx2.Add(sum1, Avx2.AlignRight(sum1.AsByte(), sum1.AsByte(), 8).AsUInt16());
        //                    Vector256<ushort> total2 = Avx2.Add(sum2, Avx2.AlignRight(sum2.AsByte(), sum2.AsByte(), 8).AsUInt16());
        //                    Vector256<ushort> total3 = Avx2.Add(sum3, Avx2.AlignRight(sum3.AsByte(), sum3.AsByte(), 8).AsUInt16());

        //                    Vector256<byte> pack01 = Avx2.PackUnsignedSaturate(total0.AsInt16(), total1.AsInt16());
        //                    Vector256<byte> pack23 = Avx2.PackUnsignedSaturate(total2.AsInt16(), total3.AsInt16());

        //                    Vector128<int> p0123 = Avx2.PermuteVar8x32(pack01.AsInt32(), gatherDwords).GetLower();
        //                    Vector128<int> p4567 = Avx2.PermuteVar8x32(pack23.AsInt32(), gatherDwords).GetLower();

        //                    Vector128<ushort> p0_7 = Sse41.PackUnsignedSaturate(p0123, p4567);
        //                    Vector128<byte> final8 = Sse2.PackUnsignedSaturate(p0_7.AsInt16(), Vector128<short>.Zero);

        //                    Vector128<byte> dstVec = Vector128.CreateScalarUnsafe(*(long*)(dstPtr + destColumn)).AsByte();
        //                    Vector128<byte> added = Vector128.Add(dstVec, final8);
        //                    Sse2.StoreScalar((long*)(dstPtr + destColumn), added.AsInt64());

        //                    sourceColumn += subSample * 8;
        //                    destColumn += 8;
        //                }
        //            }

        //            if (Ssse3.IsSupported)
        //            {
        //                Vector128<byte> zero128 = Vector128<byte>.Zero;
        //                Vector128<sbyte> padMask = padMask = Vector128.Create((ReadOnlySpan<sbyte>)[
        //                        0, 1, 2, 3, 4, 5, 6, 7, 8, -1, -1, -1, -1, -1, -1, -1]);

        //                Vector128<byte> gatherBytes = Vector128.Create((ReadOnlySpan<byte>)[
        //                    0, 8, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255
        //                ]);

        //                int safeSimdWidth128 = srcWidth + source.Border - (subSample * 3 + 16);

        //                while (sourceColumn <= safeSimdWidth128 && destColumn + 4 <= dstWidth)
        //                {
        //                    Vector128<sbyte> v0 = Ssse3.Shuffle(Vector128.Load(srcPtr + sourceColumn), padMask);
        //                    Vector128<sbyte> v1 = Ssse3.Shuffle(Vector128.Load(srcPtr + sourceColumn + subSample), padMask);
        //                    Vector128<sbyte> v2 = Ssse3.Shuffle(Vector128.Load(srcPtr + sourceColumn + subSample * 2), padMask);
        //                    Vector128<sbyte> v3 = Ssse3.Shuffle(Vector128.Load(srcPtr + sourceColumn + subSample * 3), padMask);

        //                    Vector128<ushort> sum0 = Sse2.SumAbsoluteDifferences(v0.AsByte(), zero128);
        //                    Vector128<ushort> sum1 = Sse2.SumAbsoluteDifferences(v1.AsByte(), zero128);
        //                    Vector128<ushort> sum2 = Sse2.SumAbsoluteDifferences(v2.AsByte(), zero128);
        //                    Vector128<ushort> sum3 = Sse2.SumAbsoluteDifferences(v3.AsByte(), zero128);

        //                    Vector128<ushort> total0 = Sse2.Add(sum0, Ssse3.AlignRight(sum0.AsByte(), sum0.AsByte(), 8).AsUInt16());
        //                    Vector128<ushort> total1 = Sse2.Add(sum1, Ssse3.AlignRight(sum1.AsByte(), sum1.AsByte(), 8).AsUInt16());
        //                    Vector128<ushort> total2 = Sse2.Add(sum2, Ssse3.AlignRight(sum2.AsByte(), sum2.AsByte(), 8).AsUInt16());
        //                    Vector128<ushort> total3 = Sse2.Add(sum3, Ssse3.AlignRight(sum3.AsByte(), sum3.AsByte(), 8).AsUInt16());

        //                    Vector128<byte> pack01 = Sse2.PackUnsignedSaturate(total0.AsInt16(), total1.AsInt16());
        //                    Vector128<byte> pack23 = Sse2.PackUnsignedSaturate(total2.AsInt16(), total3.AsInt16());

        //                    Vector128<byte> p01 = Ssse3.Shuffle(pack01, gatherBytes);
        //                    Vector128<byte> p23 = Ssse3.Shuffle(pack23, gatherBytes);

        //                    Vector128<byte> final4 = Sse2.UnpackLow(p01.AsInt16(), p23.AsInt16()).AsByte();

        //                    Vector128<byte> dstVec = Vector128.CreateScalarUnsafe(*(int*)(dstPtr + destColumn)).AsByte();
        //                    Vector128<byte> added = Vector128.Add(dstVec, final4);
        //                    Sse2.StoreScalar((int*)(dstPtr + destColumn), added.AsInt32());

        //                    sourceColumn += subSample * 4;
        //                    destColumn += 4;
        //                }
        //            }

        //            srcCol = sourceColumn;
        //            dstCol = destColumn;

        //            while (srcCol <= srcWidth - subSample)
        //            {
        //                if (dstCol >= 0 && dstCol < dstWidth)
        //                {
        //                    int limit = srcCol + subSample;
        //                    int sum = dstPtr[dstCol];
        //                    for (; srcCol < limit; srcCol++)
        //                    {
        //                        sum += srcPtr[srcCol];
        //                    }
        //                    dstPtr[dstCol] = (sbyte)sum;
        //                }
        //                else
        //                {
        //                    srcCol += subSample;
        //                }
        //                dstCol++;
        //            }

        //            if (srcCol < srcWidth)
        //            {
        //                if (dstCol >= 0 && dstCol < dstWidth)
        //                {
        //                    int sum = dstPtr[dstCol];
        //                    for (; srcCol < srcWidth; srcCol++)
        //                    {
        //                        sum += srcPtr[srcCol];
        //                    }
        //                    dstPtr[dstCol] = (sbyte)sum;
        //                }
        //            }
        //        }

        //        rowMod++;
        //        if (rowMod == subSample)
        //        {
        //            rowMod = 0;
        //            dstRow++;
        //        }
        //    }
        //    return true;
        //}

        /// <summary>
        /// Insert the reference map at the specified location.
        /// </summary>
        /// <param name="source">
        /// map to insert
        /// </param>
        /// <param name="dx">
        /// horizontal position to insert at
        /// </param>
        /// <param name="dy">
        /// vertical position to insert at
        /// </param>
        /// <param name="doBlit">
        /// True if the gray scale values should be added
        /// </param>
        /// <returns>
        /// True if pixels are inserted
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool InsertMap(ref Bitmap source, int dx, int dy, bool doBlit)
        {
            if (Unsafe.IsNullRef(ref source))
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(source), $"{typeof(Bitmap).FullName} source reference is null.");
            }

            if (source == default)
            {
                DjvuExceptionUtil.ThrowArgument(
                    $"Cannot insert a default source {typeof(Bitmap).FullName} into the target as {nameof(source.Data)} is null.", nameof(source));
            }

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
                if (Grays == 2 && source.Grays == 2)
                {
                    // Path 1: Bitonal SIMD Fast Path
                    do
                    {
                        sbyte* dst = GetRow(y0++) + x0;
                        sbyte* src = source.GetRow(y1++) + x1;

                        if (doBlit)
                        {
                            int c = 0;
                            if (Vector256.IsHardwareAccelerated)
                            {
                                int limit128 = w - 128;
                                while (c <= limit128)
                                {
                                    Vector512.BitwiseOr(Vector512.Load(dst + c), Vector512.Load(src + c)).Store(dst + c);
                                    Vector512.BitwiseOr(Vector512.Load(dst + c + 64), Vector512.Load(src + c + 64)).Store(dst + c + 64);
                                    c += 128;
                                }
                            }
                            else if (Vector256.IsHardwareAccelerated)
                            {
                                int limit128 = w - 128;
                                while (c <= limit128)
                                {
                                    Vector256.BitwiseOr(Vector256.Load(dst + c), Vector256.Load(src + c)).Store(dst + c);
                                    Vector256.BitwiseOr(Vector256.Load(dst + c + 32), Vector256.Load(src + c + 32)).Store(dst + c + 32);
                                    Vector256.BitwiseOr(Vector256.Load(dst + c + 64), Vector256.Load(src + c + 64)).Store(dst + c + 64);
                                    Vector256.BitwiseOr(Vector256.Load(dst + c + 96), Vector256.Load(src + c + 96)).Store(dst + c + 96);
                                    c += 128;
                                }
                            }
                            else if (Vector128.IsHardwareAccelerated)
                            {
                                int limit64 = w - 64;
                                while (c <= limit64)
                                {
                                    Vector128.BitwiseOr(Vector128.Load(dst + c), Vector128.Load(src + c)).Store(dst + c);
                                    Vector128.BitwiseOr(Vector128.Load(dst + c + 16), Vector128.Load(src + c + 16)).Store(dst + c + 16);
                                    Vector128.BitwiseOr(Vector128.Load(dst + c + 32), Vector128.Load(src + c + 32)).Store(dst + c + 32);
                                    Vector128.BitwiseOr(Vector128.Load(dst + c + 48), Vector128.Load(src + c + 48)).Store(dst + c + 48);
                                    c += 64;
                                }
                            }

                            // We expect large population of Bitmaps with 32 pixel width
                            if (Vector256.IsHardwareAccelerated)
                            {
                                int limit32 = w - 32;
                                while (c <= limit32)
                                {
                                    Vector256.BitwiseOr(Vector256.Load(dst + c), Vector256.Load(src + c)).Store(dst + c);
                                    c += 32;
                                }
                            }

                            if (Vector128.IsHardwareAccelerated)
                            {
                                int limit16 = w - 16;
                                while (c <= limit16)
                                {
                                    Vector128.BitwiseOr(Vector128.Load(dst + c), Vector128.Load(src + c)).Store(dst + c);
                                    c += 16;
                                }
                            }

                            // Scalar tail
                            while (c < w)
                            {
                                dst[c] = (sbyte)(dst[c] | src[c]);
                                c++;
                            }
                        }
                        else
                        {
                            Unsafe.CopyBlockUnaligned(dst, src, (uint)w);
                        }
                    } while (--h > 0);
                }
                else
                {
                    // Path 2: Grayscale/Color Fallback
                    // Note: This intentionally uses standard modulo-256 wrapping addition,
                    // which mathematically mirrors the unsigned char wrapping in native DjvuLibre GBitmap::blit.
                    //byte gmax = (byte)(Grays - 1);

                    //Vector512<byte> vGmax512 = Vector512.Create(gmax);
                    //Vector256<byte> vGmax256 = Vector256.Create(gmax);
                    //Vector128<byte> vGmax128 = Vector128.Create(gmax);

                    do
                    {
                        byte* dst = (byte*)GetRow(y0++) + x0;
                        byte* src = (byte*)source.GetRow(y1++) + x1;

                        if (doBlit)
                        {
                            int c = 0;

                            if (Vector512.IsHardwareAccelerated)
                            {
                                int limit128 = w - 128;
                                while (c <= limit128)
                                {
                                    Vector512.Add(Vector512.Load(dst + c), Vector512.Load(src + c)).Store(dst + c);
                                    Vector512.Add(Vector512.Load(dst + c + 64), Vector512.Load(src + c + 64)).Store(dst + c + 64);
                                    c += 128;
                                }
                            }
                            else if (Vector256.IsHardwareAccelerated)
                            {
                                int limit128 = w - 128;
                                while (c <= limit128)
                                {
                                    Vector256.Add(Vector256.Load(dst + c), Vector256.Load(src + c)).Store(dst + c);
                                    Vector256.Add(Vector256.Load(dst + c + 32), Vector256.Load(src + c + 32)).Store(dst + c + 32);
                                    Vector256.Add(Vector256.Load(dst + c + 64), Vector256.Load(src + c + 64)).Store(dst + c + 64);
                                    Vector256.Add(Vector256.Load(dst + c + 96), Vector256.Load(src + c + 96)).Store(dst + c + 96);
                                    c += 128;
                                }
                            }
                            else if (Vector128.IsHardwareAccelerated)
                            {
                                int limit64 = w - 64;
                                while (c <= limit64)
                                {
                                    Vector128.Add(Vector128.Load(dst + c), Vector128.Load(src + c)).Store(dst + c);
                                    Vector128.Add(Vector128.Load(dst + c + 16), Vector128.Load(src + c + 16)).Store(dst + c + 16);
                                    Vector128.Add(Vector128.Load(dst + c + 32), Vector128.Load(src + c + 32)).Store(dst + c + 32);
                                    Vector128.Add(Vector128.Load(dst + c + 48), Vector128.Load(src + c + 48)).Store(dst + c + 48);
                                    c += 64;
                                }
                            }

                            if (Vector256.IsHardwareAccelerated)
                            {
                                int limit32 = w - 32;
                                while (c <= limit32)
                                {
                                    Vector256.Add(Vector256.Load(dst + c), Vector256.Load(src + c)).Store(dst + c);
                                    c += 32;
                                }
                            }

                            if (Vector128.IsHardwareAccelerated)
                            {
                                int limit16 = w - 16;
                                while (c <= limit16)
                                {
                                    Vector128.Add(Vector128.Load(dst + c), Vector128.Load(src + c)).Store(dst + c);
                                    c += 16;
                                }
                            }

                            // Scalar tail
                            while (c < w)
                            {
                                // int g = dst[c] + src[c];
                                // dst[c] = (g < Grays) ? (byte)g : gmax;
                                dst[c] = (byte)(dst[c] + src[c]);
                                c++;
                            }
                        }
                        else
                        {
                            Unsafe.CopyBlockUnaligned(dst, src, (uint)w);
                        }
                    } while (--h > 0);
                }
                return true;
            }

            return false;
        }

        #region RLE Decoding (SIMD Optimized)

        /// <summary>
        /// Decompresses the cached RLE data into the raw pixel array.
        /// State Changes: If successful, the internal <c>Data</c> buffer is allocated and populated with 8bpp pixels, and the <c>_RleData</c> cache is cleared (set to null) to free memory.
        /// </summary>
        /// <param name="forceOverwrite">
        /// Parameter Impact: If <c>false</c> (default), decompression only occurs if <c>Data</c> is null. If <c>true</c>, the existing <c>Data</c> buffer is forcibly overwritten, destroying any previously uncompressed pixel state.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Decompress(bool forceOverwrite = false)
        {
            // GMonitorLock lock (monitor()) ;
            if ((Data == null || forceOverwrite) && _RleData != null)
            {
                fixed (byte* rle = _RleData)
                {
                    RleDecode(rle);
                }
            }
        }

        /// <summary>
        /// Reads an RLE-encoded image from the stream and decodes it into the bitmap using SIMD operations.
        /// If the bitmap data buffer is not yet allocated (e.g., when calling on an empty struct),
        /// it will be allocated efficiently (uninitialized) based on the current dimensions.
        /// Note: The internal structural Init method natively validates dimensions, preventing security or stability risks.
        /// </summary>
        /// <param name="stream">The stream containing R4 RLE encoded data.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ReadRleStream(Stream stream)
        {
            if (IsDisposed)
            {
                DjvuExceptionUtil.ThrowObjectDisposed(typeof(Bitmap).FullName);
            }

            if (_Data == null && _RleData == null)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"{typeof(Bitmap).FullName} is uninitialized as both {nameof(Data)} and {nameof(RleData)} are null.");
            }

            if (_RleData != null)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"{typeof(Bitmap).FullName} already contains compressed {nameof(RleData)}.");
            }

            // Safely allocates if null, throws if poisoned (too small), and calls EnsureZeroBuffer
            Resize(_Width, _Height, _Border, _BytesPerRow, uninitialized: true);

            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                byte[] rleData = ms.ToArray();

                fixed (byte* rle = rleData)
                {
                    DecodeRleCore(rle, rleData.Length, _Data, _Border, _Height, _BytesPerRow, _Width);
                }
            }
        }

        /// <summary>
        /// Internal method to initialize the data buffer and invoke the core SIMD-accelerated RLE decoding pipeline.
        /// State Changes: Validates dimensions, allocates an uninitialized 8bpp <c>Data</c> array (if null) placed on the Pinned Object Heap, resizes internal striding, populates the <c>Data</c> buffer with decompressed pixels, and explicitly nullifies the <c>_RleData</c> reference.
        /// </summary>
        /// <param name="runs">Pointer to the start of the RLE encoded byte data. Parameter Impact: The source data is read-only and is not mutated.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal void RleDecode(byte* runs)
        {
            if (Width == 0 && Height > 0 && Border > 0)
            {
                long emptyStride = (long)Width + Border;
                long emptyPixels = Height * emptyStride + Border;

                Data = GC.AllocateArray<sbyte>((int)emptyPixels, pinned: true);
                _RleData = null;

                return;
            }

            // initialize pixel array
            if (Width == 0 || Height == 0)
            {
                DjvuExceptionUtil.ThrowInvalidOperation("Bitmap is not properly initialized.");
            }

            long newStrideCalc = (long)Width + Border;

            // This condition should be unreachable under normal circumstances because
            // the Init() and Resize() methods strictly validate memory boundaries beforehand.
            // If this throws, it indicates that encapsulation has been bypassed or a required
            // upstream check is missing.
            if (newStrideCalc > int.MaxValue || newStrideCalc < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(Width), Width, "Calculated stride exceeds bounds.");
            }

            if (runs == (byte*)0)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(runs));
            }

            long npixels = Height * newStrideCalc + Border;
            if (npixels > int.MaxValue || npixels < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(npixels), npixels, "Calculated data buffer size exceeds bounds.");
            }

            if (Data == null)
            {
                Data = GC.AllocateUninitializedArray<sbyte>((int)npixels, pinned: true);
            }

            Resize(Width, Height, Border, (int)newStrideCalc);

            DecodeRleCore(runs, _RleData.Length, Data, Border, Height, BytesPerRow, Width);

            _RleData = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DecodeRleTailPixels(byte* destination, int pixelCount, byte pixelValue)
        {
            if (pixelCount >= 16)
            {
                Vector128<byte> fillVector16 = Vector128.Create(pixelValue);
                fillVector16.Store(destination);
                fillVector16.Store(destination + pixelCount - 16);
            }
            else if (pixelCount >= 8)
            {
                ulong scalarValue = (pixelValue == 0) ? 0ul : 0x0101010101010101ul;
                *(ulong*)destination = scalarValue;
                *(ulong*)(destination + pixelCount - 8) = scalarValue;
            }
            else if (pixelCount >= 4)
            {
                uint scalarValue = (pixelValue == 0) ? 0u : 0x01010101u;
                *(uint*)destination = scalarValue;
                *(uint*)(destination + pixelCount - 4) = scalarValue;
            }
            else if (pixelCount >= 2)
            {
                ushort scalarValue = (ushort)((pixelValue == 0) ? 0 : 0x0101);
                *(ushort*)destination = scalarValue;
                *(ushort*)(destination + pixelCount - 2) = scalarValue;
            }
            else
            {
                *destination = pixelValue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ReadRunValue(byte* runs, int index, bool isDataMaximum, int streamEndBoundary, int rowIndex, int columnIndex)
        {
            int runValue = runs[index];
            return (runValue < RunOverflow) ?
                runValue :
                (!isDataMaximum || index != streamEndBoundary) ?
                    (((runValue & ~RunOverflow) << 8) | runs[index + 1]) :
                    DjvuExceptionUtil.ThrowEndOfStream<int>($"Unexpected end of stream at row {rowIndex}, column {columnIndex}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZeroMemoryAvx512(byte* ptr, int length)
        {
            int offset = 0;
            while (length - offset >= 64)
            {
                Vector512.Store(Vector512<byte>.Zero, ptr + offset);
                offset += 64;
            }
            while (length - offset >= 32)
            {
                Vector256.Store(Vector256<byte>.Zero, ptr + offset);
                offset += 32;
            }
            while (length - offset >= 16)
            {
                Vector128.Store(Vector128<byte>.Zero, ptr + offset);
                offset += 16;
            }
            while (length - offset >= 8)
            {
                Unsafe.WriteUnaligned<long>(ptr + offset, 0L);
                offset += 8;
            }
            while (offset < length) ptr[offset++] = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZeroMemoryAvx2(byte* ptr, int length)
        {
            int offset = 0;
            while (length - offset >= 32)
            {
                Vector256.Store(Vector256<byte>.Zero, ptr + offset);
                offset += 32;
            }
            while (length - offset >= 16)
            {
                Vector128.Store(Vector128<byte>.Zero, ptr + offset);
                offset += 16;
            }
            while (length - offset >= 8)
            {
                Unsafe.WriteUnaligned<long>(ptr + offset, 0L);
                offset += 8;
            }
            while (offset < length) ptr[offset++] = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZeroMemoryVector128(byte* ptr, int length)
        {
            int offset = 0;
            while (length - offset >= 16)
            {
                Vector128.Store(Vector128<byte>.Zero, ptr + offset);
                offset += 16;
            }
            while (length - offset >= 8)
            {
                Unsafe.WriteUnaligned<long>(ptr + offset, 0L);
                offset += 8;
            }
            while (offset < length) ptr[offset++] = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZeroMemoryScalar(byte* ptr, int length)
        {
            int offset = 0;
            while (length - offset >= 8)
            {
                Unsafe.WriteUnaligned<long>(ptr + offset, 0L);
                offset += 8;
            }
            while (offset < length) ptr[offset++] = 0;
        }

        /// <summary>
        /// Core RLE decoding loop. Utilizes dynamic hardware intrinsic switching (AVX-512, AVX2, SSSE3)
        /// to decompress run-length data into continuous bitmap memory space safely.
        /// State Changes: Mutates the <c>data</c> array parameter directly by clearing the border region and writing decompressed 8bpp pixels. Does not modify struct properties.
        /// </summary>
        /// <param name="runs">Pointer to the RLE data. Parameter Impact: Read-only, determines the decompression sequence.</param>
        /// <param name="runsLength">Length of the RLE data array in bytes.</param>
        /// <param name="data">The target managed array for the decompressed image. Parameter Impact: This array is mutated during execution.</param>
        /// <param name="border">Border size in bytes around the image data. Parameter Impact: Dictates the padding offset for the pixel mutation.</param>
        /// <param name="height">The height of the bitmap.</param>
        /// <param name="bytesPerRow">The number of bytes in each row including the border padding.</param>
        /// <param name="width">The width of the bitmap in pixels.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static void DecodeRleCore(byte* runs, int runsLength, sbyte[] data, int border, int height, int bytesPerRow, int width)
        {
            fixed (sbyte* pData = data)
            {
                if (border > 0)
                {
                    if (Avx512F.IsSupported && Avx512BW.IsSupported) ZeroMemoryAvx512((byte*)pData, border);
                    else if (Avx2.IsSupported) ZeroMemoryAvx2((byte*)pData, border);
                    else if (Vector128.IsHardwareAccelerated) ZeroMemoryVector128((byte*)pData, border);
                    else ZeroMemoryScalar((byte*)pData, border);
                }

                byte* rowStart = (byte*)pData + border;
                byte* runsEnd = runs + runsLength;

                int rowIndex = height - 1;
                byte* row = rowStart + rowIndex * bytesPerRow;
                int columnIndex = 0;
                byte pixelValue = 0;

                uint commandCarry = 0;
                ulong globalEvenCarry = 0;
                ulong globalOddCarry = 0;

                uint avx512loops = 0;
                uint avx2loops = 0;
                uint vector128loops = 0;

                if (Avx512F.IsSupported && Avx512BW.IsSupported)
                {
                    Vector512<byte> _indices512 = Vector512.Create((ReadOnlySpan<byte>)[
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                        16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
                        32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47,
                        48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63]);

                    Vector512<byte> vector512Value192 = Vector512.Create((byte)192);

                    ulong chainCarry = commandCarry;
                    ulong evenCarry = globalEvenCarry;
                    ulong oddCarry = globalOddCarry;

                    while (rowIndex >= 0 && runs + 128 <= runsEnd)
                    {
                        bool isDataMaximum = runs + 128 == runsEnd;

                        var vector64Low = Vector512.Load(runs);
                        var vector64High = Vector512.Load(runs + 64);

                        ulong maskLow = Vector512.GreaterThanOrEqual(vector64Low, vector512Value192).ExtractMostSignificantBits();
                        ulong maskHigh = Vector512.GreaterThanOrEqual(vector64High, vector512Value192).ExtractMostSignificantBits();

                        // ILP: Precompute independent shifts and inversions for both blocks simultaneously
                        ulong maskLowShifted = maskLow << 1;
                        ulong maskHighShifted = maskHigh << 1;

                        // Evaluate Block Low
                        ulong blockStartsMaskLow = maskLow & ~(maskLowShifted | chainCarry);
                        ulong sumEvenLow = maskLow + (blockStartsMaskLow & 0x5555555555555555ul) + evenCarry;
                        evenCarry = (sumEvenLow < maskLow) ? 1ul : 0ul;

                        ulong sumOddLow = maskLow + (blockStartsMaskLow & 0xAAAAAAAAAAAAAAAAul) + oddCarry;
                        oddCarry = (sumOddLow < maskLow) ? 1ul : 0ul;

                        ulong startsTwoByteMaskLow = maskLow & ~((sumEvenLow & 0x5555555555555555ul) | (sumOddLow & 0xAAAAAAAAAAAAAAAAul));
                        ulong startsLow = ~((startsTwoByteMaskLow << 1) | chainCarry);
                        chainCarry = startsTwoByteMaskLow >> 63;
                        evenCarry &= chainCarry;
                        oddCarry &= chainCarry;

                        // Evaluate Block High (Now consuming the resolved carries from Low)
                        ulong blockStartsMaskHigh = maskHigh & ~(maskHighShifted | chainCarry);
                        ulong sumEvenHigh = maskHigh + (blockStartsMaskHigh & 0x5555555555555555ul) + evenCarry;
                        evenCarry = (sumEvenHigh < maskHigh) ? 1ul : 0ul;

                        ulong sumOddHigh = maskHigh + (blockStartsMaskHigh & 0xAAAAAAAAAAAAAAAAul) + oddCarry;
                        oddCarry = (sumOddHigh < maskHigh) ? 1ul : 0ul;

                        ulong startsTwoByteMaskHigh = maskHigh & ~((sumEvenHigh & 0x5555555555555555ul) | (sumOddHigh & 0xAAAAAAAAAAAAAAAAul));
                        ulong startsHigh = ~((startsTwoByteMaskHigh << 1) | chainCarry);
                        chainCarry = startsTwoByteMaskHigh >> 63;
                        evenCarry &= chainCarry;
                        oddCarry &= chainCarry;

                        int offset = 0;
                        ulong activeStarts = startsLow;

                        while (true)
                        {
                            while (activeStarts != 0 && rowIndex >= 0)
                            {
                                int trailingZeros = BitOperations.TrailingZeroCount(activeStarts);
                                activeStarts &= activeStarts - 1;

                                int pixelCount = ReadRunValue(runs, trailingZeros + offset, isDataMaximum, 127, rowIndex, columnIndex);

                                if (columnIndex + pixelCount > width)
                                {
                                    int dumpStart = Math.Max(0, trailingZeros + offset - 64);
                                    int dumpLength = Math.Min((int)(runsEnd - runs) - dumpStart, 128);
                                    string dump = BitConverter.ToString(new ReadOnlySpan<byte>(runs + dumpStart, dumpLength).ToArray()).Replace("-", " ");
                                    DjvuExceptionUtil.ThrowFormatException($"Invalid RLE encoded data: Bitmap.Width: {width}, c: {columnIndex}, x: {pixelCount}, trailingZeros: {trailingZeros}, offset: {offset}, Rle.Length: {runsLength}, Remaining RLE: {(nuint)(runsEnd - runs)} " +
                                        $"CommandCarry: {chainCarry}, EvenCarry: {evenCarry}, OddCarry: {oddCarry}, Avx512Loops: {avx512loops}, Avx2loops: {avx2loops}, Vector128loops: {vector128loops}\nDump: {dump}");
                                }

                                if (columnIndex + 64 <= width)
                                {
                                    Vector512<byte> fillVector = Vector512.Create(pixelValue);
                                    if (pixelCount < 64)
                                    {
                                        fillVector.Store(row + columnIndex);
                                    }
                                    else
                                    {
                                        int remainingPixels = pixelCount;
                                        byte* destination = row + columnIndex;

                                        while (remainingPixels >= 256)
                                        {
                                            fillVector.Store(destination);
                                            fillVector.Store(destination + 64);
                                            fillVector.Store(destination + 128);
                                            fillVector.Store(destination + 192);
                                            destination += 256;
                                            remainingPixels -= 256;
                                        }

                                        while (remainingPixels >= 64)
                                        {
                                            fillVector.Store(destination);
                                            destination += 64;
                                            remainingPixels -= 64;
                                        }

                                        if (remainingPixels > 0)
                                        {
                                            fillVector.Store(row + columnIndex + pixelCount - 64);
                                        }
                                    }
                                    columnIndex += pixelCount;
                                }
                                else if (pixelCount > 0)
                                {
                                    Vector512<byte> fillVector = Vector512.Create(pixelValue);
                                    Vector512<byte> writeMask = Vector512.LessThan(_indices512, Vector512.Create((byte)pixelCount));
                                    Avx512BW.MaskStore(row + columnIndex, writeMask, fillVector);
                                    columnIndex += pixelCount;
                                }

                                pixelValue = (byte)unchecked(1 - pixelValue);

                                if (columnIndex >= width)
                                {
                                    if (border > 0)
                                    {
                                        ZeroMemoryAvx512(row + width, border);
                                    }
                                    columnIndex = 0;
                                    pixelValue = 0;
                                    row -= bytesPerRow;
                                    rowIndex -= 1;
                                }
                            }

                            if (offset == 64 || rowIndex < 0)
                            {
                                break;
                            }

                            activeStarts = startsHigh;
                            offset = 64;
                        }

                        runs += 128;
                        avx512loops++;
                    }
                    commandCarry = (uint)chainCarry;
                    globalEvenCarry = evenCarry;
                    globalOddCarry = oddCarry;
                }

                if (Avx2.IsSupported)
                {
                    Vector256<byte> vector256Value192 = Vector256.Create((byte)192);

                    uint chainCarry = commandCarry;
                    uint evenCarry = (uint)globalEvenCarry;
                    uint oddCarry = (uint)globalOddCarry;

                    while (rowIndex >= 0 && runs + 64 <= runsEnd)
                    {
                        bool isDataMaximum = runs + 64 == runsEnd;

                        var vector32Low = Vector256.Load(runs);
                        var vector32High = Vector256.Load(runs + 32);

                        uint maskLow = Vector256.GreaterThanOrEqual(vector32Low, vector256Value192).ExtractMostSignificantBits();
                        uint maskHigh = Vector256.GreaterThanOrEqual(vector32High, vector256Value192).ExtractMostSignificantBits();

                        uint maskLowShifted = maskLow << 1;
                        uint maskHighShifted = maskHigh << 1;

                        uint blockStartsMaskLow = maskLow & ~(maskLowShifted | chainCarry);
                        uint sumEvenLow = maskLow + (blockStartsMaskLow & 0x55555555u) + evenCarry;
                        evenCarry = (sumEvenLow < maskLow) ? 1u : 0u;

                        uint sumOddLow = maskLow + (blockStartsMaskLow & 0xAAAAAAAAu) + oddCarry;
                        oddCarry = (sumOddLow < maskLow) ? 1u : 0u;

                        uint startsTwoByteMaskLow = maskLow & ~((sumEvenLow & 0x55555555u) | (sumOddLow & 0xAAAAAAAAu));
                        uint startsLow = ~((startsTwoByteMaskLow << 1) | chainCarry);

                        chainCarry = startsTwoByteMaskLow >> 31;
                        evenCarry &= chainCarry;
                        oddCarry &= chainCarry;

                        uint blockStartsMaskHigh = maskHigh & ~(maskHighShifted | chainCarry);
                        uint sumEvenHigh = maskHigh + (blockStartsMaskHigh & 0x55555555u) + evenCarry;
                        evenCarry = (sumEvenHigh < maskHigh) ? 1u : 0u;

                        uint sumOddHigh = maskHigh + (blockStartsMaskHigh & 0xAAAAAAAAu) + oddCarry;
                        oddCarry = (sumOddHigh < maskHigh) ? 1u : 0u;

                        uint startsTwoByteMaskHigh = maskHigh & ~((sumEvenHigh & 0x55555555u) | (sumOddHigh & 0xAAAAAAAAu));
                        uint startsHigh = ~((startsTwoByteMaskHigh << 1) | chainCarry);

                        chainCarry = startsTwoByteMaskHigh >> 31;
                        evenCarry &= chainCarry;
                        oddCarry &= chainCarry;

                        int offset = 0;
                        uint activeStarts = startsLow;

                        while (true)
                        {
                            while (activeStarts != 0 && rowIndex >= 0)
                            {
                                int trailingZeros = BitOperations.TrailingZeroCount(activeStarts);
                                activeStarts &= activeStarts - 1;

                                int pixelCount = ReadRunValue(runs, trailingZeros + offset, isDataMaximum, 63, rowIndex, columnIndex);

                                if (columnIndex + pixelCount > width)
                                {
                                    int dumpStart = Math.Max(0, trailingZeros + offset - 64);
                                    int dumpLength = Math.Min((int)(runsEnd - runs) - dumpStart, 128);
                                    string dump = BitConverter.ToString(new ReadOnlySpan<byte>(runs + dumpStart, dumpLength).ToArray()).Replace("-", " ");
                                    DjvuExceptionUtil.ThrowFormatException($"Invalid RLE encoded data: Bitmap.Width: {width}, c: {columnIndex}, x: {pixelCount}, tz: {trailingZeros}, offset: {offset}, Rle.Length: {runsLength}, Remaining RLE: {(nuint)(runsEnd - runs)} " +
                                        $"CommandCarry: {chainCarry}, EvenCarry: {evenCarry}, OddCarry: {oddCarry}, Avx512Loops: {avx512loops}, Avx2loops: {avx2loops}, Vector128loops: {vector128loops}\nDump: {dump}");
                                }

                                if (columnIndex + 32 <= width)
                                {
                                    Vector256<byte> fillVector = Vector256.Create(pixelValue);
                                    if (pixelCount < 32)
                                    {
                                        fillVector.Store(row + columnIndex);
                                    }
                                    else
                                    {
                                        int remainingPixels = pixelCount;
                                        byte* destination = row + columnIndex;

                                        while (remainingPixels >= 128)
                                        {
                                            fillVector.Store(destination);
                                            fillVector.Store(destination + 32);
                                            fillVector.Store(destination + 64);
                                            fillVector.Store(destination + 96);
                                            destination += 128;
                                            remainingPixels -= 128;
                                        }

                                        while (remainingPixels >= 32)
                                        {
                                            fillVector.Store(destination);
                                            destination += 32;
                                            remainingPixels -= 32;
                                        }

                                        if (remainingPixels > 0)
                                        {
                                            fillVector.Store(row + columnIndex + pixelCount - 32);
                                        }
                                    }
                                    columnIndex += pixelCount;
                                }
                                else if (pixelCount > 0)
                                {
                                    DecodeRleTailPixels(row + columnIndex, pixelCount, pixelValue);
                                    columnIndex += pixelCount;
                                }

                                pixelValue = (byte)unchecked(1 - pixelValue);

                                if (columnIndex >= width)
                                {
                                    if (border > 0)
                                    {
                                        ZeroMemoryAvx2(row + width, border);
                                    }
                                    columnIndex = 0;
                                    pixelValue = 0;
                                    row -= bytesPerRow;
                                    rowIndex -= 1;
                                }
                            }

                            if (offset == 32 || rowIndex < 0)
                            {
                                break;
                            }

                            activeStarts = startsHigh;
                            offset = 32;
                        }

                        runs += 64;
                        avx2loops++;
                    }
                    commandCarry = chainCarry;
                    globalEvenCarry = evenCarry;
                    globalOddCarry = oddCarry;
                }

                if (Vector128.IsHardwareAccelerated)
                {
                    Vector128<byte> vectorZero128 = Vector128<byte>.Zero;
                    Vector128<byte> vectorOne128 = Vector128.Create((byte)1);
                    Vector128<byte> vector128Value192 = Vector128.Create((byte)192);

                    uint chainCarry = commandCarry;
                    uint evenCarry = (uint)globalEvenCarry;
                    uint oddCarry = (uint)globalOddCarry;

                    while (rowIndex >= 0 && runs + 32 <= runsEnd)
                    {
                        bool isDataMaximum = runs + 32 == runsEnd;

                        var vector16Low = Vector128.Load(runs);
                        var vector16High = Vector128.Load(runs + 16);

                        uint maskLow = Vector128.GreaterThanOrEqual(vector16Low, vector128Value192).ExtractMostSignificantBits();
                        uint maskHigh = Vector128.GreaterThanOrEqual(vector16High, vector128Value192).ExtractMostSignificantBits();
                        uint mask = maskLow | (maskHigh << 16);

                        uint maskShifted = mask << 1;

                        uint blockStartsMask = mask & ~(maskShifted | chainCarry);
                        uint sumEven = mask + (blockStartsMask & 0x55555555u) + evenCarry;
                        evenCarry = (sumEven < mask) ? 1u : 0u;

                        uint sumOdd = mask + (blockStartsMask & 0xAAAAAAAAu) + oddCarry;
                        oddCarry = (sumOdd < mask) ? 1u : 0u;

                        uint startsTwoByteMask = mask & ~((sumEven & 0x55555555u) | (sumOdd & 0xAAAAAAAAu));
                        uint starts = ~((startsTwoByteMask << 1) | chainCarry);

                        chainCarry = startsTwoByteMask >> 31;
                        evenCarry &= chainCarry;
                        oddCarry &= chainCarry;

                        while (starts != 0 && rowIndex >= 0)
                        {
                            int trailingZeros = BitOperations.TrailingZeroCount(starts);
                            starts &= starts - 1;

                            int pixelCount = ReadRunValue(runs, trailingZeros, isDataMaximum, 31, rowIndex, columnIndex);

                            if (columnIndex + pixelCount > width)
                            {
                                DjvuExceptionUtil.ThrowFormatException($"Invalid RLE encoded data: Bitmap.Width: {width}, c: {columnIndex}, x: {pixelCount}, Rle.Length: {runsLength}, Remaining RLE: {(nuint)(runsEnd - runs)} " +
                                    $"CommandCarry: {chainCarry}, EvenCarry: {evenCarry}, OddCarry: {oddCarry}, Avx512Loops: {avx512loops}, Avx2loops: {avx2loops}, Vector128loops: {vector128loops}");
                            }

                            if (columnIndex + 16 <= width)
                            {
                                Vector128<byte> fillVector = Vector128.Create(pixelValue);
                                if (pixelCount < 16)
                                {
                                    fillVector.Store(row + columnIndex);
                                }
                                else
                                {
                                    int remainingPixels = pixelCount;
                                    byte* destination = row + columnIndex;

                                    while (remainingPixels >= 64)
                                    {
                                        fillVector.Store(destination);
                                        fillVector.Store(destination + 16);
                                        fillVector.Store(destination + 32);
                                        fillVector.Store(destination + 48);
                                        destination += 64;
                                        remainingPixels -= 64;
                                    }

                                    while (remainingPixels >= 16)
                                    {
                                        fillVector.Store(destination);
                                        destination += 16;
                                        remainingPixels -= 16;
                                    }

                                    if (remainingPixels > 0)
                                    {
                                        fillVector.Store(row + columnIndex + pixelCount - 16);
                                    }
                                }
                                columnIndex += pixelCount;
                            }
                            else if (pixelCount > 0)
                            {
                                DecodeRleTailPixels(row + columnIndex, pixelCount, pixelValue);
                                columnIndex += pixelCount;
                            }

                            pixelValue = (byte)unchecked(1 - pixelValue);

                            if (columnIndex >= width)
                            {
                                    if (border > 0)
                                    {
                                        ZeroMemoryVector128(row + width, border);
                                    }
                                columnIndex = 0;
                                pixelValue = 0;
                                row -= bytesPerRow;
                                rowIndex -= 1;
                            }
                        }

                        runs += 32;
                        vector128loops++;
                    }

                    // Final 16-byte Vector128 fallback loop to prevent premature scalar degradation
                    while (rowIndex >= 0 && runs + 16 <= runsEnd)
                    {
                        bool isDataMaximum = runs + 16 == runsEnd;
                        Vector128<byte> vector16 = Vector128.Load(runs);

                        uint mask = Vector128.GreaterThanOrEqual(vector16, vector128Value192).ExtractMostSignificantBits();
                        uint maskShifted = mask << 1;
                        uint blockStartsMask = mask & ~(maskShifted | chainCarry);

                        // 16-bit arithmetic prevents wrap-around overflows against 32-bit registers
                        uint sumEven = (mask + (blockStartsMask & 0x5555u) + evenCarry) & 0xFFFFu;
                        evenCarry = (sumEven < mask) ? 1u : 0u;

                        uint sumOdd = (mask + (blockStartsMask & 0xAAAAu) + oddCarry) & 0xFFFFu;
                        oddCarry = (sumOdd < mask) ? 1u : 0u;

                        uint startsTwoByteMask = mask & ~((sumEven & 0x5555u) | (sumOdd & 0xAAAAu));
                        uint starts = (~((startsTwoByteMask << 1) | chainCarry)) & 0xFFFFu;

                        chainCarry = startsTwoByteMask >> 15;
                        evenCarry &= chainCarry;
                        oddCarry &= chainCarry;

                        while (starts != 0 && rowIndex >= 0)
                        {
                            int trailingZeros = BitOperations.TrailingZeroCount(starts);
                            starts &= starts - 1;

                            int pixelCount = ReadRunValue(runs, trailingZeros, isDataMaximum, 15, rowIndex, columnIndex);

                            if (columnIndex + pixelCount > width)
                            {
                                DjvuExceptionUtil.ThrowFormatException($"Invalid RLE encoded data: Bitmap.Width: {width}, c: {columnIndex}, x: {pixelCount}, Rle.Length: {runsLength}, Remaining RLE: {(nuint)(runsEnd - runs)} " +
                                    $"CommandCarry: {chainCarry}, EvenCarry: {evenCarry}, OddCarry: {oddCarry}, Avx512Loops: {avx512loops}, Avx2loops: {avx2loops}, Vector128loops: {vector128loops}");
                            }

                            if (columnIndex + 16 <= width)
                            {
                                Vector128<byte> fillVector = Vector128.Create(pixelValue);
                                if (pixelCount < 16)
                                {
                                    fillVector.Store(row + columnIndex);
                                }
                                else
                                {
                                    int remainingPixels = pixelCount;
                                    byte* destination = row + columnIndex;

                                    while (remainingPixels >= 64)
                                    {
                                        fillVector.Store(destination);
                                        fillVector.Store(destination + 16);
                                        fillVector.Store(destination + 32);
                                        fillVector.Store(destination + 48);
                                        destination += 64;
                                        remainingPixels -= 64;
                                    }

                                    while (remainingPixels >= 16)
                                    {
                                        fillVector.Store(destination);
                                        destination += 16;
                                        remainingPixels -= 16;
                                    }

                                    if (remainingPixels > 0)
                                    {
                                        fillVector.Store(row + columnIndex + pixelCount - 16);
                                    }
                                }
                                columnIndex += pixelCount;
                            }
                            else if (pixelCount > 0)
                            {
                                DecodeRleTailPixels(row + columnIndex, pixelCount, pixelValue);
                                columnIndex += pixelCount;
                            }

                            pixelValue = (byte)unchecked(1 - pixelValue);

                            if (columnIndex >= width)
                            {
                                if (border > 0)
                                {
                                    ZeroMemoryVector128(row + width, border);
                                }
                                columnIndex = 0;
                                pixelValue = 0;
                                row -= bytesPerRow;
                                rowIndex -= 1;
                            }
                        }
                        runs += 16;
                        vector128loops++;
                    }

                    commandCarry = chainCarry;
                    globalEvenCarry = evenCarry;
                    globalOddCarry = oddCarry;
                }

                // Bridge SIMD-to-Scalar boundary:
                // If the SIMD block ended on a 2-byte start, it already consumed
                // the first byte of this tail as a continuation. Skip it.
                runs += commandCarry;

                while (rowIndex >= 0 && runs < runsEnd)
                {
                    //if (runs[0] >= RunOverflow && runs + 1 >= runsEnd)
                    //    DjvuExceptionUtil.ThrowEndOfStream($"{typeof(Bitmap).FullName}: Unexpected end of RLE stream at row {rowIndex}.");

                    bool isDataMaximum = runs + 1 == runsEnd;
                    int x = ReadRun(ref runs, isDataMaximum);

                    if (columnIndex + x > width)
                        DjvuExceptionUtil.ThrowFormatException($"Invalid RLE encoded data: Bitmap.Width: {width}, c: {columnIndex}, x: {x}, Rle.Length: {runsLength}, Remaining RLE: {(nuint)(runsEnd - runs)}" +
                            $" CommandCarry: {commandCarry}, EvenCarry: {globalEvenCarry}, OddCarry: {globalOddCarry}, Avx512Loops: {avx512loops}, Avx2loops: {avx2loops}, Vector128loops: {vector128loops}");

                    while (x-- > 0)
                    {
                        row[columnIndex++] = pixelValue;
                    }

                    pixelValue = (byte)unchecked(1 - pixelValue);
                    if (columnIndex >= width)
                    {
                        if (border > 0) ZeroMemoryScalar(row + width, border);
                        columnIndex = 0;
                        pixelValue = 0;
                        row -= bytesPerRow;
                        rowIndex -= 1;
                    }
                }

                if (rowIndex >= 0)
                {
                    DjvuExceptionUtil.ThrowEndOfStream($"{typeof(Bitmap).FullName}: Unexpected end of RLE stream at row {rowIndex}.");
                }
            }
        }

        /// <summary>
        /// Reads a single run length from the RLE stream. A run length is encoded in 1 byte if less than 192 (RunOverflow),
        /// otherwise it spans 2 bytes.
        /// State Changes: Mutates the caller's pointer by advancing it 1 or 2 bytes depending on the read value.
        /// </summary>
        /// <param name="data">Reference to the pointer advancing through the RLE stream. Parameter Impact: The pointer is mutated (advanced) by this method.</param>
        /// <returns>The decoded run length in pixels.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ReadRun(ref byte* data, bool isDataMaximum)
        {
            int firstByte = *data++;
            return (firstByte >= RunOverflow) ?
                (isDataMaximum ?
                    DjvuExceptionUtil.ThrowEndOfStream<int>("Unexpected end of stream.") :
                    (((firstByte & ~RunOverflow) << 8) | (*data++))) :
                firstByte;
        }

        /// <summary>
        /// Decodes RLE data directly into a 1bpp (1 bit per pixel) bit-packed format for external PBM (Portable Bitmap) serialization.
        /// Note: The internal <c>Bitmap</c> format strictly uses 8bpp (ranging from 2 to 256 tones, usually bitonal). This method bypasses the 8bpp buffer allocation entirely for raw PBM export.
        /// State Changes: Sequentially mutates the memory pointed to by <c>bitmap</c> and advances the <c>runs</c> pointer.
        /// </summary>
        /// <param name="width">Width of the image row in pixels.</param>
        /// <param name="runs">Reference to the pointer tracking current position in the RLE stream. Parameter Impact: The pointer is mutated (advanced) by this method.</param>
        /// <param name="runsEnd">Pointer indicating the end boundary of the RLE stream.</param>
        /// <param name="bitmap">Pointer to the target 1bpp bit-packed output buffer. Parameter Impact: The underlying memory is mutated.</param>
        /// <param name="invert">If true, inverts the output bits (flips 0 and 1) written to the bitmap.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal void Rle2Bitmap(int width, ref byte* runs, byte* runsEnd, byte* bitmap, bool invert = false)
        {
            int obyte_def = invert ? 0xff : 0;
            int obyte_ndef = invert ? 0 : 0xff;
            int mask = 0x80, obyte = 0;

            for (int c = width; c > 0;)
            {
                if (runs >= runsEnd)
                    DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream.");

                bool isDataMaximum = runs + 1 == runsEnd;
                int x = ReadRun(ref runs, isDataMaximum);
                c -= x;

                while ((x--) > 0)
                {
                    if ((mask >>= 1) == 0)
                    {
                        *(bitmap++) = (byte)(obyte ^ obyte_def);
                        obyte = 0;
                        mask = 0x80;

                        for (; x >= 8; x -= 8)
                        {
                            *(bitmap++) = (byte)obyte_def;
                        }
                    }
                }

                if (c > 0)
                {
                    if (runs >= runsEnd)
                        DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream.");

                    isDataMaximum = runs + 1 == runsEnd;
                    x = ReadRun(ref runs, isDataMaximum);

                    c -= x;
                    while ((x--) > 0)
                    {
                        obyte |= mask;
                        if ((mask >>= 1) == 0)
                        {
                            *(bitmap++) = (byte)(obyte ^ obyte_def);
                            obyte = 0;
                            mask = 0x80;

                            for (; x > 8; x -= 8)
                            {
                                *(bitmap++) = (byte)obyte_ndef;
                            }
                        }
                    }
                }
            }

            if (mask != 0x80)
            {
                *(bitmap++) = (byte)(obyte ^ obyte_def);
            }
        }

        #endregion

        #region RLE Encoding (SIMD Optimized)


        /// <summary>
        /// Serializes Bitmap data using Run Length Encoding compression to RLE format.
        /// </summary>
        /// <param name="stream"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SerializeToRle(Stream stream)
        {
            // checks
            if (Width == 0 || Height == 0)
            {
                DjvuExceptionUtil.ThrowInvalidOperation("Bitmap is not properly initialized.");
            }

            //GMonitorLock lock (monitor()) ;
            if (Grays > 2)
            {
                DjvuExceptionUtil.ThrowInvalidOperation(
                    $"Only bi-level bitmaps can be saved in PBM format. Grays: {Grays}");
            }

            // header
            string head = $"R4\n{Width} {Height}\n";
            byte[] buffer = new UTF8Encoding(false).GetBytes(head);
            stream.Write(buffer, 0, buffer.Length);

            // body
            if (_RleData != null)
            {
                stream.Write(_RleData, 0, _RleData.Length);
            }
            else
            {
                byte[] gruns;
                long size = RleEncode(out gruns);
                if (gruns != null && size > 0)
                {
                    stream.Write(gruns, 0, gruns.Length);
                }
            }
        }

        /// <summary>
        /// Compresses the raw 8bpp bitmap pixel data into the internal RLE format cache.
        /// State Changes: Allocates a new byte array containing the RLE stream and assigns it to <c>_RleData</c>.
        /// Crucially, it sets the <c>Data</c> array (uncompressed 8bpp pixels) to null, destroying the uncompressed state to minimize memory footprint. Valid only for bi-level images.
        /// </summary>
        public void Compress()
        {
            if (IsDisposed)
            {
                DjvuExceptionUtil.ThrowObjectDisposed(typeof(Bitmap).FullName);
            }

            if (_Data == null && _RleData == null)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"{typeof(Bitmap).FullName} is not properly initialized.");
            }

            if (_RleData != null)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"{typeof(Bitmap).FullName} already contains compressed {nameof(RleData)}.");
            }

            if (_Width == 0 && _Height == 0 && _Border == 0)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"Cannot compress {typeof(Bitmap).FullName} with zero dimensions and zero border.");
            }

            if (Grays > 2)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"Cannot compress data with Grays: {Grays}");
            }

            //GMonitorLock lock (monitor()) ;
            if (Data != null)
            {
                byte[] grle;
                long rleLength = RleEncode(out grle);
                if (rleLength > 0)
                {
                    Data = null;
                    _RleData = grle;
                }
            }
        }

        /// <summary>
        /// Internal engine to scan the raw 8bpp pixel map and encode contiguous spans of identically colored pixels
        /// into RLE byte arrays using vectorized hardware acceleration (AVX-512, AVX2, SSSE3).
        /// State Changes: Allocates temporary scaling buffers and a final exact-size array for the RLE compressed data.
        /// </summary>
        /// <param name="gpruns">Out parameter returning the newly allocated array containing RLE compressed data. Parameter Impact: State is exposed to caller.</param>
        /// <returns>The total number of bytes written to the RLE array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal long RleEncode(out byte[] gpruns)
        {
            gpruns = null;

            if (Height == 0 || Width == 0)
            {
                return 0;
            }

            if (Data == null)
            {
                gpruns = _RleData;
                return _RleData != null ? _RleData.Length : 0;
            }

            // create run array
            long pos = 0;
            int maxpos = 1024 + Width + Height;
            byte[] runsBuff = GC.AllocateUninitializedArray<byte>(maxpos);

            // encode bitmap as rle
            {
                int n = Height - 1;
                byte* row = (byte*)GetRow(n);
                while (n >= 0)
                {
                    long required = pos + 2 + (2 * Width);
                    if (maxpos < required)
                    {
                        maxpos = (int)Math.Max((long)maxpos * 2, required + 1024);
                        var newRuns = GC.AllocateUninitializedArray<byte>(maxpos);
                        Buffer.BlockCopy(runsBuff, 0, newRuns, 0, (int)pos);
                        runsBuff = newRuns;
                    }

                    fixed (byte* runs = runsBuff)
                    {
                        byte* runs_pos = runs + pos;
                        byte* runs_pos_start = runs_pos;

                        AppendLine(ref runs_pos, row, Width);

                        pos += (int)(runs_pos - runs_pos_start);
                    }
                    row -= BytesPerRow;
                    n -= 1;
                }
            }
            // return result
            var finalRuns = GC.AllocateUninitializedArray<byte>((int)pos);
            Buffer.BlockCopy(runsBuff, 0, finalRuns, 0, (int)pos);
            gpruns = finalRuns;
            return pos;
        }

        /// <summary>
        /// Encodes runs larger than the 16383 format limit by chaining maximum-size segments
        /// separated by 0-length runs of the alternating color.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal void AppendLongRun(ref byte* data, int count)
        {
            while (count > MaxRunSize)
            {
                data[0] = data[1] = 0xff;
                data[2] = 0;
                data += 3;
                count -= MaxRunSize;
            }

            if (count < RunOverflow)
            {
                data[0] = (byte)count;
                data += 1;
            }
            else
            {
                data[0] = (byte)((count >> 8) + RunOverflow);
                data[1] = (byte)(count & 0xff);
                data += 2;
            }
        }

        /// <summary>
        /// Scans an entire image row utilizing advanced SIMD bitwise masks and hardware intrinsics
        /// to quickly tally pixel run lengths, writing the computed runs to the RLE stream.
        /// State Changes: Mutates the <c>data</c> pointer by advancing it as RLE lengths are written.
        /// </summary>
        /// <param name="data">Reference to the pointer indicating where to write the next RLE byte. Parameter Impact: The pointer is mutated (advanced).</param>
        /// <param name="row">Pointer to the start of the 8bpp pixel row to compress. Parameter Impact: Read-only.</param>
        /// <param name="rowLength">Length of the row in pixels.</param>
        /// <param name="invert">If true, inverts the baseline color logic, changing how sequences are identified.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal void AppendLine(ref byte* data, byte* row, int rowLength, bool invert = false)
        {
            byte* rowEnd = row + rowLength;
            int count = 0;
            int last_bit = invert ? 1 : 0;

            if (Vector512.IsHardwareAccelerated)
            {
                while (row + 128 <= rowEnd)
                {
                    var v1 = Vector512.Load(row);
                    var v2 = Vector512.Load(row + 64);

                    ulong M1 = ~Vector512.Equals(v1, Vector512<byte>.Zero).ExtractMostSignificantBits();
                    ulong M2 = ~Vector512.Equals(v2, Vector512<byte>.Zero).ExtractMostSignificantBits();

                    ulong M1_shl = M1 << 1;
                    ulong M2_shl = M2 << 1;

                    ulong t1 = M1 ^ (M1_shl | (uint)last_bit);
                    int last_bit2 = (int)(M1 >> 63);

                    ulong t2 = M2 ^ (M2_shl | (uint)last_bit2);
                    last_bit = (int)(M2 >> 63);

                    if ((t1 | t2) == 0)
                    {
                        count += 128;
                    }
                    else
                    {
                        int offset = 0;
                        while (t1 != 0)
                        {
                            int tz = BitOperations.TrailingZeroCount(t1);
                            count += (tz - offset);
                            AppendRun(ref data, count);
                            count = 0;
                            offset = tz;
                            t1 &= (t1 - 1);
                        }
                        count += (64 - offset);

                        offset = 0;
                        while (t2 != 0)
                        {
                            int tz = BitOperations.TrailingZeroCount(t2);
                            count += (tz - offset);
                            AppendRun(ref data, count);
                            count = 0;
                            offset = tz;
                            t2 &= (t2 - 1);
                        }
                        count += (64 - offset);
                    }
                    row += 128;
                }

                while (row + 64 <= rowEnd)
                {
                    var v = Vector512.Load(row);
                    ulong M = ~Vector512.Equals(v, Vector512<byte>.Zero).ExtractMostSignificantBits();
                    ulong transitions = M ^ ((M << 1) | (uint)last_bit);
                    last_bit = (int)(M >> 63);

                    if (transitions == 0)
                        count += 64;
                    else
                    {
                        int offset = 0;
                        while (transitions != 0)
                        {
                            int tz = BitOperations.TrailingZeroCount(transitions);
                            count += (tz - offset);
                            AppendRun(ref data, count);
                            count = 0;
                            offset = tz;
                            transitions &= (transitions - 1);
                        }
                        count += (64 - offset);
                    }
                    row += 64;
                }
            }

            if (Vector256.IsHardwareAccelerated)
            {
                while (row + 64 <= rowEnd)
                {
                    var v1 = Vector256.Load(row);
                    var v2 = Vector256.Load(row + 32);

                    uint M1 = ~Vector256.Equals(v1, Vector256<byte>.Zero).ExtractMostSignificantBits();
                    uint M2 = ~Vector256.Equals(v2, Vector256<byte>.Zero).ExtractMostSignificantBits();

                    ulong M = M1 | ((ulong)M2 << 32);
                    ulong transitions = M ^ ((M << 1) | (uint)last_bit);
                    last_bit = (int)(M >> 63);

                    if (transitions == 0)
                        count += 64;
                    else
                    {
                        int offset = 0;
                        while (transitions != 0)
                        {
                            int tz = BitOperations.TrailingZeroCount(transitions);
                            count += (tz - offset);
                            AppendRun(ref data, count);
                            count = 0;
                            offset = tz;
                            transitions &= (transitions - 1);
                        }
                        count += (64 - offset);
                    }
                    row += 64;
                }

                while (row + 32 <= rowEnd)
                {
                    var v = Vector256.Load(row);
                    uint M = ~Vector256.Equals(v, Vector256<byte>.Zero).ExtractMostSignificantBits();
                    uint transitions = M ^ ((M << 1) | (uint)last_bit);
                    last_bit = (int)(M >> 31);

                    if (transitions == 0)
                        count += 32;
                    else
                    {
                        int offset = 0;
                        while (transitions != 0)
                        {
                            int tz = BitOperations.TrailingZeroCount(transitions);
                            count += (tz - offset);
                            AppendRun(ref data, count);
                            count = 0;
                            offset = tz;
                            transitions &= (transitions - 1);
                        }
                        count += (32 - offset);
                    }
                    row += 32;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                while (row + 64 <= rowEnd)
                {
                    var v1 = Vector128.Load(row);
                    var v2 = Vector128.Load(row + 16);
                    var v3 = Vector128.Load(row + 32);
                    var v4 = Vector128.Load(row + 48);

                    uint M1 = (~Vector128.Equals(v1, Vector128<byte>.Zero).ExtractMostSignificantBits()) & 0xFFFFu;
                    uint M2 = (~Vector128.Equals(v2, Vector128<byte>.Zero).ExtractMostSignificantBits()) & 0xFFFFu;
                    uint M3 = (~Vector128.Equals(v3, Vector128<byte>.Zero).ExtractMostSignificantBits()) & 0xFFFFu;
                    uint M4 = (~Vector128.Equals(v4, Vector128<byte>.Zero).ExtractMostSignificantBits()) & 0xFFFFu;

                    ulong M = M1 | ((ulong)M2 << 16) | ((ulong)M3 << 32) | ((ulong)M4 << 48);
                    ulong transitions = M ^ ((M << 1) | (uint)last_bit);
                    last_bit = (int)(M >> 63);

                    if (transitions == 0)
                        count += 64;
                    else
                    {
                        int offset = 0;
                        while (transitions != 0)
                        {
                            int tz = BitOperations.TrailingZeroCount(transitions);
                            count += (tz - offset);
                            AppendRun(ref data, count);
                            count = 0;
                            offset = tz;
                            transitions &= (transitions - 1);
                        }
                        count += (64 - offset);
                    }
                    row += 64;
                }

                while (row + 16 <= rowEnd)
                {
                    var v = Vector128.Load(row);
                    uint M = (~Vector128.Equals(v, Vector128<byte>.Zero).ExtractMostSignificantBits()) & 0xFFFFu;
                    uint transitions = (M ^ ((M << 1) | (uint)last_bit)) & 0xFFFFu;
                    last_bit = (int)(M >> 15);

                    if (transitions == 0)
                        count += 16;
                    else
                    {
                        int offset = 0;
                        while (transitions != 0)
                        {
                            int tz = BitOperations.TrailingZeroCount(transitions);
                            count += (tz - offset);
                            AppendRun(ref data, count);
                            count = 0;
                            offset = tz;
                            transitions &= (transitions - 1);
                        }
                        count += (16 - offset);
                    }
                    row += 16;
                }
            }

            if (row < rowEnd)
            {
                byte p = (byte)last_bit;
                while (row < rowEnd)
                {
                    byte pixel = (*row == 0) ? (byte)0 : (byte)1;
                    if (pixel == p)
                    {
                        count++;
                    }
                    else
                    {
                        AppendRun(ref data, count);
                        count = 1;
                        p = pixel;
                    }
                    row++;
                }
            }

            AppendRun(ref data, count);
        }

        /// <summary>
        /// Appends a specific pixel run count to the RLE stream, automatically escalating to long-run
        /// representations if the count exceeds the standard 192 (RunOverflow) bounds.
        /// State Changes: Mutates the memory at the <c>data</c> pointer and advances the pointer by 1, 2, or more bytes.
        /// </summary>
        /// <param name="data">Reference to the current write pointer in the RLE stream. Parameter Impact: The pointer is mutated (advanced).</param>
        /// <param name="count">The number of pixels in this continuous run.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal void AppendRun(ref byte* data, int count)
        {
            if (count < RunOverflow)
            {
                data[0] = (byte)count;
                data += 1;
            }
            else if (count <= MaxRunSize)
            {
                data[0] = (byte)((count >> 8) + RunOverflow);
                data[1] = (byte)(count & 0xff);
                data += 2;
            }
            else
            {
                AppendLongRun(ref data, count);
            }
        }

        #endregion
    }
}
