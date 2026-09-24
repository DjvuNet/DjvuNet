using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using DjvuNet.Graphics;

namespace DjvuNet.Wavelet
{
    public partial class InterWaveSimd
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int YCbCr2RgbVector512(short* pY, short* pCb, short* pCr, sbyte* pImg8, int height, int width, int srcStride, int rowSizeInBytes, int startX)
        {
            if (Avx512F.IsSupported && Avx512BW.IsSupported)
            {
                if (width - startX < 64) return startX;

                int vectorBound = width - 64;
                int tailShift = (64 - ((width - startX) % 64)) % 64;

                Vector512<short> v32 = Vector512.Create((short)32);
                Vector512<short> v128 = Vector512.Create((short)128);
                Vector512<short> v127 = Vector512.Create((short)127);
                Vector512<short> vNeg128 = Vector512.Create((short)-128);

                Vector512<long> permuteMask = Vector512.Create((ReadOnlySpan<long>)[0L, 2L, 4L, 6L, 1L, 3L, 5L, 7L]);

                Vector512<byte> maskB1 = Vector512.Create((ReadOnlySpan<byte>)[
                    0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128, 5,
                    0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128, 5,
                    0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128, 5,
                    0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128, 5]);

                Vector512<byte> maskG1 = Vector512.Create((ReadOnlySpan<byte>)[
                    128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128,
                    128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128,
                    128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128,
                    128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128]);

                Vector512<byte> maskR1 = Vector512.Create((ReadOnlySpan<byte>)[
                    128, 128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128,
                    128, 128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128,
                    128, 128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128,
                    128, 128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128]);

                Vector512<byte> maskB2 = Vector512.Create((ReadOnlySpan<byte>)[
                    128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10, 128,
                    128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10, 128,
                    128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10, 128,
                    128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10, 128]);

                Vector512<byte> maskG2 = Vector512.Create((ReadOnlySpan<byte>)[
                    5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10,
                    5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10,
                    5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10,
                    5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10]);

                Vector512<byte> maskR2 = Vector512.Create((ReadOnlySpan<byte>)[
                    128, 5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128,
                    128, 5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128,
                    128, 5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128,
                    128, 5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128]);

                Vector512<byte> maskB3 = Vector512.Create((ReadOnlySpan<byte>)[
                    128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128, 128,
                    128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128, 128,
                    128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128, 128,
                    128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128, 128]);

                Vector512<byte> maskG3 = Vector512.Create((ReadOnlySpan<byte>)[
                    128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128,
                    128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128,
                    128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128,
                    128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128]);

                Vector512<byte> maskR3 = Vector512.Create((ReadOnlySpan<byte>)[
                    10, 128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15,
                    10, 128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15,
                    10, 128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15,
                    10, 128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15]);

                for (int y = 0, pidx = 0, ridx = 0; y < height; y++, pidx += srcStride, ridx += rowSizeInBytes)
                {
                    int x = startX;

                    while (x < width)
                    {
                        int shift = (x > vectorBound) ? tailShift : 0;
                        int j = x - shift;
                        int pixidx = ridx + (j * 3);
                        // Block 1 (Pixels 0-31)
                        Vector512<short> y1 = Avx512BW.ShiftRightArithmetic(Avx512BW.Add(Avx512F.LoadVector512(pY + pidx + j), v32), 6);
                        Vector512<short> cb1 = Avx512BW.ShiftRightArithmetic(Avx512BW.Add(Avx512F.LoadVector512(pCb + pidx + j), v32), 6);
                        Vector512<short> cr1 = Avx512BW.ShiftRightArithmetic(Avx512BW.Add(Avx512F.LoadVector512(pCr + pidx + j), v32), 6);

                        y1 = Avx512BW.Max(Avx512BW.Min(y1, v127), vNeg128);
                        cb1 = Avx512BW.Max(Avx512BW.Min(cb1, v127), vNeg128);
                        cr1 = Avx512BW.Max(Avx512BW.Min(cr1, v127), vNeg128);

                        // Block 2 (Pixels 32-63)
                        Vector512<short> y2 = Avx512BW.ShiftRightArithmetic(Avx512BW.Add(Avx512F.LoadVector512(pY + pidx + j + 32), v32), 6);
                        Vector512<short> cb2 = Avx512BW.ShiftRightArithmetic(Avx512BW.Add(Avx512F.LoadVector512(pCb + pidx + j + 32), v32), 6);
                        Vector512<short> cr2 = Avx512BW.ShiftRightArithmetic(Avx512BW.Add(Avx512F.LoadVector512(pCr + pidx + j + 32), v32), 6);

                        y2 = Avx512BW.Max(Avx512BW.Min(y2, v127), vNeg128);
                        cb2 = Avx512BW.Max(Avx512BW.Min(cb2, v127), vNeg128);
                        cr2 = Avx512BW.Max(Avx512BW.Min(cr2, v127), vNeg128);

                        // Transform Block 1
                        Vector512<short> cbShift1 = Avx512BW.ShiftRightArithmetic(cb1, 2);
                        Vector512<short> crShift1 = Avx512BW.ShiftRightArithmetic(cr1, 1);
                        Vector512<short> crAdd1 = Avx512BW.Add(cr1, crShift1);
                        Vector512<short> yAdd1 = Avx512BW.Add(y1, v128);
                        Vector512<short> ySub1 = Avx512BW.Subtract(yAdd1, cbShift1);

                        Vector512<short> r1 = Avx512BW.Add(yAdd1, crAdd1);
                        Vector512<short> g1 = Avx512BW.Subtract(ySub1, Avx512BW.ShiftRightArithmetic(crAdd1, 1));
                        Vector512<short> b1 = Avx512BW.Add(ySub1, Avx512BW.Add(cb1, cb1));

                        // Transform Block 2
                        Vector512<short> cbShift2 = Avx512BW.ShiftRightArithmetic(cb2, 2);
                        Vector512<short> crShift2 = Avx512BW.ShiftRightArithmetic(cr2, 1);
                        Vector512<short> crAdd2 = Avx512BW.Add(cr2, crShift2);
                        Vector512<short> yAdd2 = Avx512BW.Add(y2, v128);
                        Vector512<short> ySub2 = Avx512BW.Subtract(yAdd2, cbShift2);

                        Vector512<short> r2 = Avx512BW.Add(yAdd2, crAdd2);
                        Vector512<short> g2 = Avx512BW.Subtract(ySub2, Avx512BW.ShiftRightArithmetic(crAdd2, 1));
                        Vector512<short> b2 = Avx512BW.Add(ySub2, Avx512BW.Add(cb2, cb2));

                        // Saturate 16-bit to 8-bit. Avx512BW splits lanes during packing.
                        Vector512<byte> vecR = Avx512BW.PackUnsignedSaturate(r1, r2);
                        Vector512<byte> vecG = Avx512BW.PackUnsignedSaturate(g1, g2);
                        Vector512<byte> vecB = Avx512BW.PackUnsignedSaturate(b1, b2);

                        // Untangle the 512-bit lanes (64-bit block granularity) to linearize the bytes.
                        Vector512<byte> vecR_ordered = Avx512F.PermuteVar8x64(vecR.AsInt64(), permuteMask).AsByte();
                        Vector512<byte> vecG_ordered = Avx512F.PermuteVar8x64(vecG.AsInt64(), permuteMask).AsByte();
                        Vector512<byte> vecB_ordered = Avx512F.PermuteVar8x64(vecB.AsInt64(), permuteMask).AsByte();

                        // Shuffle and interleave across all 4 lanes simultaneously
                        Vector512<byte> out1 = Avx512BW.Or(Avx512BW.Or(Avx512BW.Shuffle(vecB_ordered, maskB1), Avx512BW.Shuffle(vecG_ordered, maskG1)), Avx512BW.Shuffle(vecR_ordered, maskR1));
                        Vector512<byte> out2 = Avx512BW.Or(Avx512BW.Or(Avx512BW.Shuffle(vecB_ordered, maskB2), Avx512BW.Shuffle(vecG_ordered, maskG2)), Avx512BW.Shuffle(vecR_ordered, maskR2));
                        Vector512<byte> out3 = Avx512BW.Or(Avx512BW.Or(Avx512BW.Shuffle(vecB_ordered, maskB3), Avx512BW.Shuffle(vecG_ordered, maskG3)), Avx512BW.Shuffle(vecR_ordered, maskR3));

                        // Extract 128-bit chunks and route them to their proper memory offsets
                        Vector256<byte> out1Low = out1.GetLower();
                        Vector256<byte> out1High = out1.GetUpper();
                        Sse2.Store((byte*)pImg8 + pixidx, out1Low.GetLower());
                        Sse2.Store((byte*)pImg8 + pixidx + 48, out1Low.GetUpper());
                        Sse2.Store((byte*)pImg8 + pixidx + 96, out1High.GetLower());
                        Sse2.Store((byte*)pImg8 + pixidx + 144, out1High.GetUpper());

                        Vector256<byte> out2Low = out2.GetLower();
                        Vector256<byte> out2High = out2.GetUpper();
                        Sse2.Store((byte*)pImg8 + pixidx + 16, out2Low.GetLower());
                        Sse2.Store((byte*)pImg8 + pixidx + 64, out2Low.GetUpper());
                        Sse2.Store((byte*)pImg8 + pixidx + 112, out2High.GetLower());
                        Sse2.Store((byte*)pImg8 + pixidx + 160, out2High.GetUpper());

                        Vector256<byte> out3Low = out3.GetLower();
                        Vector256<byte> out3High = out3.GetUpper();
                        Sse2.Store((byte*)pImg8 + pixidx + 32, out3Low.GetLower());
                        Sse2.Store((byte*)pImg8 + pixidx + 80, out3Low.GetUpper());
                        Sse2.Store((byte*)pImg8 + pixidx + 128, out3High.GetLower());
                        Sse2.Store((byte*)pImg8 + pixidx + 176, out3High.GetUpper());

                        x += 64 - shift;
                    }
                }
                return width;
            }
            else
            {
                return 0;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int YGray2PixelMapVector512(short* pY, sbyte* pImg8, int height, int width, int srcStride, int rowSizeInBytes, int startX)
        {
            if (Avx512F.IsSupported && Avx512BW.IsSupported)
            {
                if (width - startX < 64) return startX;

                int vectorBound = width - 64;
                int tailShift = (64 - ((width - startX) % 64)) % 64;

                Vector512<short> v32 = Vector512.Create((short)32);
                Vector512<sbyte> v127 = Vector512.Create((sbyte)127);

                Vector512<long> permuteOrder = Vector512.Create((ReadOnlySpan<long>)[0L, 2L, 4L, 6L, 1L, 3L, 5L, 7L]);

                Vector512<long> permute1 = Vector512.Create((ReadOnlySpan<long>)[0L, 1L, 0L, 1L, 0L, 1L, 2L, 3L]);
                Vector512<long> permute2 = Vector512.Create((ReadOnlySpan<long>)[2L, 3L, 2L, 3L, 4L, 5L, 4L, 5L]);
                Vector512<long> permute3 = Vector512.Create((ReadOnlySpan<long>)[4L, 5L, 6L, 7L, 6L, 7L, 6L, 7L]);

                Vector512<byte> mask1 = Vector512.Create((ReadOnlySpan<byte>)[
                    0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5,
                    5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10,
                    10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15,
                    0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5]);

                Vector512<byte> mask2 = Vector512.Create((ReadOnlySpan<byte>)[
                    5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10,
                    10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15,
                    0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5,
                    5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10]);

                Vector512<byte> mask3 = Vector512.Create((ReadOnlySpan<byte>)[
                    10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15,
                    0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5,
                    5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10,
                    10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15]);

                for (int y = 0, pidx = 0, ridx = 0; y < height; y++, pidx += srcStride, ridx += rowSizeInBytes)
                {
                    int x = startX;

                    while (x < width)
                    {
                        int shift = (x > vectorBound) ? tailShift : 0;
                        int j = x - shift;
                        int pixidx = ridx + (j * 3);
                        Vector512<short> y1 = Avx512BW.ShiftRightArithmetic(Avx512BW.Add(Avx512F.LoadVector512(pY + pidx + j), v32), 6);
                        Vector512<short> y2 = Avx512BW.ShiftRightArithmetic(Avx512BW.Add(Avx512F.LoadVector512(pY + pidx + j + 32), v32), 6);

                        Vector512<sbyte> narrowed = Avx512BW.PackSignedSaturate(y1, y2);
                        narrowed = Avx512F.PermuteVar8x64(narrowed.AsInt64(), permuteOrder).AsSByte();
                        Vector512<sbyte> gray = Avx512BW.Subtract(v127, narrowed);

                        Vector512<sbyte> gray1 = Avx512F.PermuteVar8x64(gray.AsInt64(), permute1).AsSByte();
                        Vector512<sbyte> gray2 = Avx512F.PermuteVar8x64(gray.AsInt64(), permute2).AsSByte();
                        Vector512<sbyte> gray3 = Avx512F.PermuteVar8x64(gray.AsInt64(), permute3).AsSByte();

                        Vector512<byte> out1 = Avx512BW.Shuffle(gray1.AsByte(), mask1);
                        Vector512<byte> out2 = Avx512BW.Shuffle(gray2.AsByte(), mask2);
                        Vector512<byte> out3 = Avx512BW.Shuffle(gray3.AsByte(), mask3);

                        Avx512F.Store((byte*)pImg8 + pixidx, out1);
                        Avx512F.Store((byte*)pImg8 + pixidx + 64, out2);
                        Avx512F.Store((byte*)pImg8 + pixidx + 128, out3);

                        x += 64 - shift;
                    }
                }
                return width;
            }
            return 0;
        }
    }
}
