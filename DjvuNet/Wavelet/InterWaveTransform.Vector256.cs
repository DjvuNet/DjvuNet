using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using DjvuNet.Errors;
using DjvuNet.Graphics;

namespace DjvuNet.Wavelet
{
    public static partial class InterWaveSimd
    {
        /// <summary>
        /// Vector256 (AVX2) implementation of the deinterlace phase of the RGB to YCbCr (Pigeon) transform.
        /// Deinterlace phase is tested independently from other processing phases for both correctness and performance
        /// to eliminate any bottlenecks and ensure optimal codegen. Deinterlace implementation will rearrange any three
        /// interlaced vectors of the interlaced types into three single type data vectors so obviously its not restricted to BGR format.
        /// Further optimizations are possible based on the analysis of jit disasm data but were not attempted yet.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void DeinterlaceBgrVector256(
            Pixel* inputPointer,
            out Vector256<short> lumaEven, out Vector256<short> blueEven, out Vector256<short> redEven,
            out Vector256<short> lumaOdd, out Vector256<short> blueOdd, out Vector256<short> redOdd)
        {
            if (!Avx2.IsSupported)
            {
                throw new PlatformNotSupportedException("AVX2 is not supported on this platform.");
            }

            byte* bytePointer = (byte*)inputPointer;
            var vecA = Vector256.Load(bytePointer);
            var vecF = Vector256.Load(bytePointer + 32);
            var vecB = Vector256.Load(bytePointer + 64);

            var vecC = vecA;
            vecA = Avx2.InsertVector128(vecF, vecA.GetLower(), 0);
            vecC = Avx2.InsertVector128(vecC, vecB.GetLower(), 0);
            vecB = Avx2.InsertVector128(vecB, vecF.GetLower(), 0);
            vecF = Avx2.Permute2x128(vecC, vecC, 1);

            var vecG = vecA;
            vecA = Avx2.ShiftLeftLogical128BitLane(vecA, 8);
            vecG = Avx2.ShiftRightLogical128BitLane(vecG, 8);
            vecA = Avx2.UnpackHigh(vecA, vecF);
            vecF = Avx2.ShiftLeftLogical128BitLane(vecF, 8);
            vecG = Avx2.UnpackLow(vecG, vecB);
            vecF = Avx2.UnpackHigh(vecF, vecB);

            var vecD = vecA;
            vecA = Avx2.ShiftLeftLogical128BitLane(vecA, 8);
            vecD = Avx2.ShiftRightLogical128BitLane(vecD, 8);
            vecA = Avx2.UnpackHigh(vecA, vecG);
            vecG = Avx2.ShiftLeftLogical128BitLane(vecG, 8);
            vecD = Avx2.UnpackLow(vecD, vecF);
            vecG = Avx2.UnpackHigh(vecG, vecF);

            var vecE = vecA;
            vecA = Avx2.ShiftLeftLogical128BitLane(vecA, 8);
            vecE = Avx2.ShiftRightLogical128BitLane(vecE, 8);
            vecA = Avx2.UnpackHigh(vecA, vecD);
            vecD = Avx2.ShiftLeftLogical128BitLane(vecD, 8);
            vecE = Avx2.UnpackLow(vecE, vecG);
            vecD = Avx2.UnpackHigh(vecD, vecG);

            var vecH = Vector256<byte>.Zero;

            vecC = vecA;
            vecA = Avx2.UnpackLow(vecA, vecH);
            vecC = Avx2.UnpackHigh(vecC, vecH);

            vecB = vecE;
            vecE = Avx2.UnpackLow(vecE, vecH);
            vecB = Avx2.UnpackHigh(vecB, vecH);

            vecF = vecD;
            vecD = Avx2.UnpackLow(vecD, vecH);
            vecF = Avx2.UnpackHigh(vecF, vecH);

            lumaEven = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(vecA.AsInt16(), 8), 8);
            blueEven = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(vecC.AsInt16(), 8), 8);
            redEven = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(vecE.AsInt16(), 8), 8);
            lumaOdd = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(vecB.AsInt16(), 8), 8);
            blueOdd = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(vecD.AsInt16(), 8), 8);
            redOdd = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(vecF.AsInt16(), 8), 8);
        }

        /// <summary>
        /// Vector256 (AVX2) implementation of the deinterlace phase of the RGB to YCbCr (Pigeon) transform
        /// processing the whole image in single pass to simulate real world operation. Deinterlace phase is tested
        /// independently from other processing phases for both correctness and performance to eliminate any bottlenecks
        /// and ensure optimal codegen. Deinterlace implementation will rearrange any three interlaced vectors of the interlaced
        /// types into three single type data vectors so obviously its not restricted to BGR format.
        /// Further optimizations are possible based on the analysis of jit disasm data but were not attempted yet.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void DeinterlaceBgrVector256FullImage(
            Pixel* inputPointer, int width, int height, int rowSizeInBytes,
            out Vector256<short> lumaEven, out Vector256<short> blueEven, out Vector256<short> redEven,
            out Vector256<short> lumaOdd, out Vector256<short> blueOdd, out Vector256<short> redOdd)
        {

            if (!Avx2.IsSupported)
            {
                throw new PlatformNotSupportedException("AVX2 is not supported on this platform.");
            }

            lumaEven = Vector256<short>.Zero; blueEven = Vector256<short>.Zero; redEven = Vector256<short>.Zero;
            lumaOdd = Vector256<short>.Zero; blueOdd = Vector256<short>.Zero; redOdd = Vector256<short>.Zero;

            for (int y = 0; y < height; y++)
            {
                Pixel* pIn = (Pixel*)((byte*)inputPointer + ((long)y * rowSizeInBytes));
                int x = 0;
                while (x <= width - 32)
                {
                    byte* bytePointer = (byte*)pIn;
                    var vecA = Vector256.Load(bytePointer);
                    var vecF = Vector256.Load(bytePointer + 32);
                    var vecB = Vector256.Load(bytePointer + 64);

                    var vecC = vecA;
                    vecA = Avx2.InsertVector128(vecF, vecA.GetLower(), 0);
                    vecC = Avx2.InsertVector128(vecC, vecB.GetLower(), 0);
                    vecB = Avx2.InsertVector128(vecB, vecF.GetLower(), 0);
                    vecF = Avx2.Permute2x128(vecC, vecC, 1);

                    var vecG = vecA;
                    vecA = Avx2.ShiftLeftLogical128BitLane(vecA, 8);
                    vecG = Avx2.ShiftRightLogical128BitLane(vecG, 8);
                    vecA = Avx2.UnpackHigh(vecA, vecF);
                    vecF = Avx2.ShiftLeftLogical128BitLane(vecF, 8);
                    vecG = Avx2.UnpackLow(vecG, vecB);
                    vecF = Avx2.UnpackHigh(vecF, vecB);

                    var vecD = vecA;
                    vecA = Avx2.ShiftLeftLogical128BitLane(vecA, 8);
                    vecD = Avx2.ShiftRightLogical128BitLane(vecD, 8);
                    vecA = Avx2.UnpackHigh(vecA, vecG);
                    vecG = Avx2.ShiftLeftLogical128BitLane(vecG, 8);
                    vecD = Avx2.UnpackLow(vecD, vecF);
                    vecG = Avx2.UnpackHigh(vecG, vecF);

                    var vecE = vecA;
                    vecA = Avx2.ShiftLeftLogical128BitLane(vecA, 8);
                    vecE = Avx2.ShiftRightLogical128BitLane(vecE, 8);
                    vecA = Avx2.UnpackHigh(vecA, vecD);
                    vecD = Avx2.ShiftLeftLogical128BitLane(vecD, 8);
                    vecE = Avx2.UnpackLow(vecE, vecG);
                    vecD = Avx2.UnpackHigh(vecD, vecG);

                    var vecH = Vector256<byte>.Zero;

                    vecC = vecA;
                    vecA = Avx2.UnpackLow(vecA, vecH);
                    vecC = Avx2.UnpackHigh(vecC, vecH);

                    vecB = vecE;
                    vecE = Avx2.UnpackLow(vecE, vecH);
                    vecB = Avx2.UnpackHigh(vecB, vecH);

                    vecF = vecD;
                    vecD = Avx2.UnpackLow(vecD, vecH);
                    vecF = Avx2.UnpackHigh(vecF, vecH);

                    lumaEven = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(vecA.AsInt16(), 8), 8);
                    blueEven = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(vecC.AsInt16(), 8), 8);
                    redEven = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(vecE.AsInt16(), 8), 8);
                    lumaOdd = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(vecB.AsInt16(), 8), 8);
                    blueOdd = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(vecD.AsInt16(), 8), 8);
                    redOdd = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(vecF.AsInt16(), 8), 8);

                    pIn += 32;
                    x += 32;
                }
            }
        }

        /// <summary>
        /// Vector256 (AVX2) implementation of the YCbCr (Pigeon) to RGB transform
        /// performed on deinterlaced pixel data and processing 32 pixels in single pass. Used to test correctness
        /// and performance of this phase independently from the deinterlace and interlace stages.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void TransformYCbCrToRgbVector256(
            Vector256<short> lumaEven, Vector256<short> blueEven, Vector256<short> redEven,
            Vector256<short> lumaOdd, Vector256<short> blueOdd, Vector256<short> redOdd,
            out Vector256<byte> blueEvenOut, out Vector256<byte> blueOddOut,
            out Vector256<byte> greenEvenOut, out Vector256<byte> greenOddOut,
            out Vector256<byte> redEvenOut, out Vector256<byte> redOddOut)
        {
            if (!Avx2.IsSupported)
            {
                throw new PlatformNotSupportedException("AVX2 is not supported on this platform.");
            }

            var vec128 = Vector256.Create((short)128);
            var vecZero = Vector256.Create((short)0);
            var vec255 = Vector256.Create((short)255);

            var temp1Even = Avx2.ShiftRightArithmetic(blueEven, 2);
            var redShift1Even = Avx2.ShiftRightArithmetic(redEven, 1);
            var temp2Even = Avx2.Add(redEven, redShift1Even);
            var luma128Even = Avx2.Add(lumaEven, vec128);
            var temp3Even = Avx2.Subtract(luma128Even, temp1Even);

            var tempRedEven = Avx2.Add(luma128Even, temp2Even);
            var temp2Shift1Even = Avx2.ShiftRightArithmetic(temp2Even, 1);
            var blueShift1Even = Avx2.ShiftLeftLogical(blueEven, 1);

            var tempGreenEven = Avx2.Subtract(temp3Even, temp2Shift1Even);
            var tempBlueEven = Avx2.Add(temp3Even, blueShift1Even);

            tempRedEven = Avx2.Max(vecZero, Avx2.Min(vec255, tempRedEven));
            tempGreenEven = Avx2.Max(vecZero, Avx2.Min(vec255, tempGreenEven));
            tempBlueEven = Avx2.Max(vecZero, Avx2.Min(vec255, tempBlueEven));

            var temp1Odd = Avx2.ShiftRightArithmetic(blueOdd, 2);
            var redShift1Odd = Avx2.ShiftRightArithmetic(redOdd, 1);
            var temp2Odd = Avx2.Add(redOdd, redShift1Odd);
            var luma128Odd = Avx2.Add(lumaOdd, vec128);
            var temp3Odd = Avx2.Subtract(luma128Odd, temp1Odd);

            var tempRedOdd = Avx2.Add(luma128Odd, temp2Odd);
            var temp2Shift1Odd = Avx2.ShiftRightArithmetic(temp2Odd, 1);
            var blueShift1Odd = Avx2.ShiftLeftLogical(blueOdd, 1);

            var tempGreenOdd = Avx2.Subtract(temp3Odd, temp2Shift1Odd);
            var tempBlueOdd = Avx2.Add(temp3Odd, blueShift1Odd);

            tempRedOdd = Avx2.Max(vecZero, Avx2.Min(vec255, tempRedOdd));
            tempGreenOdd = Avx2.Max(vecZero, Avx2.Min(vec255, tempGreenOdd));
            tempBlueOdd = Avx2.Max(vecZero, Avx2.Min(vec255, tempBlueOdd));

            blueEvenOut = Avx2.PackUnsignedSaturate(tempBlueEven, tempBlueEven);
            blueOddOut = Avx2.PackUnsignedSaturate(tempBlueOdd, tempBlueOdd);
            greenEvenOut = Avx2.PackUnsignedSaturate(tempGreenEven, tempGreenEven);
            greenOddOut = Avx2.PackUnsignedSaturate(tempGreenOdd, tempGreenOdd);
            redEvenOut = Avx2.PackUnsignedSaturate(tempRedEven, tempRedEven);
            redOddOut = Avx2.PackUnsignedSaturate(tempRedOdd, tempRedOdd);
        }

        /// <summary>
        /// Single step Vector256 (AVX2) implementation of the RGB to YCbCr (Pigeon) transform
        /// performed on deinterlaced pixel data and processing 32 pixels in single pass. Used to test correctness
        /// and performance of this phase independently from the deinterlace and interlace stages.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void TransformRgbToYCbCrVector256(
            Vector256<short> blueEven, Vector256<short> greenEven, Vector256<short> redEven,
            Vector256<short> blueOdd, Vector256<short> greenOdd, Vector256<short> redOdd,
            out Vector256<sbyte> lumaEvenOut, out Vector256<sbyte> chromaBlueEvenOut, out Vector256<sbyte> chromaRedEvenOut,
            out Vector256<sbyte> lumaOddOut, out Vector256<sbyte> chromaBlueOddOut, out Vector256<sbyte> chromaRedOddOut)
        {
            if (!Avx2.IsSupported)
            {
                throw new PlatformNotSupportedException("AVX2 is not supported on this platform.");
            }

            var vec128 = Vector256.Create((int)128);
            var vec32768 = Vector256.Create((int)32768);
            var vecZero16 = Vector256<short>.Zero;

            short coeffYRed = 19946, coeffYGreen1 = 19946, coeffYGreen2 = 19945, coeffYBlue = 5698;
            short coeffCrRed = 30393, coeffCrGreen = -26594, coeffCrBlue = -3799;
            short coeffCbRed = -11397, coeffCbGreen = -22795, coeffCbBlue1 = 17096, coeffCbBlue2 = 17096;

            var vecCoeffYRedGreen = Vector256.Create(coeffYRed, coeffYGreen1, coeffYRed, coeffYGreen1, coeffYRed, coeffYGreen1, coeffYRed, coeffYGreen1, coeffYRed, coeffYGreen1, coeffYRed, coeffYGreen1, coeffYRed, coeffYGreen1, coeffYRed, coeffYGreen1);
            var vecCoeffYBlueGreen = Vector256.Create(coeffYBlue, coeffYGreen2, coeffYBlue, coeffYGreen2, coeffYBlue, coeffYGreen2, coeffYBlue, coeffYGreen2, coeffYBlue, coeffYGreen2, coeffYBlue, coeffYGreen2, coeffYBlue, coeffYGreen2, coeffYBlue, coeffYGreen2);
            var vecCoeffCrRedGreen = Vector256.Create(coeffCrRed, coeffCrGreen, coeffCrRed, coeffCrGreen, coeffCrRed, coeffCrGreen, coeffCrRed, coeffCrGreen, coeffCrRed, coeffCrGreen, coeffCrRed, coeffCrGreen, coeffCrRed, coeffCrGreen, coeffCrRed, coeffCrGreen);
            var vecCoeffCrBlueZero = Vector256.Create(coeffCrBlue, (short)0, coeffCrBlue, (short)0, coeffCrBlue, (short)0, coeffCrBlue, (short)0, coeffCrBlue, (short)0, coeffCrBlue, (short)0, coeffCrBlue, (short)0, coeffCrBlue, (short)0);
            var vecCoeffCbRedGreen = Vector256.Create(coeffCbRed, coeffCbGreen, coeffCbRed, coeffCbGreen, coeffCbRed, coeffCbGreen, coeffCbRed, coeffCbGreen, coeffCbRed, coeffCbGreen, coeffCbRed, coeffCbGreen, coeffCbRed, coeffCbGreen, coeffCbRed, coeffCbGreen);
            var vecCoeffCbBlueBlue = Vector256.Create(coeffCbBlue1, coeffCbBlue2, coeffCbBlue1, coeffCbBlue2, coeffCbBlue1, coeffCbBlue2, coeffCbBlue1, coeffCbBlue2, coeffCbBlue1, coeffCbBlue2, coeffCbBlue1, coeffCbBlue2, coeffCbBlue1, coeffCbBlue2, coeffCbBlue1, coeffCbBlue2);

            // EVEN 16 PIXELS
            var vecRedGreenLowEven = Avx2.UnpackLow(redEven, greenEven);
            var vecBlueGreenLowEven = Avx2.UnpackLow(blueEven, greenEven);
            var vecBlueZeroLowEven = Avx2.UnpackLow(blueEven, vecZero16);
            var vecBlueBlueLowEven = Avx2.UnpackLow(blueEven, blueEven);

            var vecY32LowEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowEven, vecCoeffYRedGreen), Avx2.MultiplyAddAdjacent(vecBlueGreenLowEven, vecCoeffYBlueGreen)), vec32768), 16);
            var vecCr32LowEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowEven, vecCoeffCrRedGreen), Avx2.MultiplyAddAdjacent(vecBlueZeroLowEven, vecCoeffCrBlueZero)), vec32768), 16);
            var vecCb32LowEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowEven, vecCoeffCbRedGreen), Avx2.MultiplyAddAdjacent(vecBlueBlueLowEven, vecCoeffCbBlueBlue)), vec32768), 16);

            vecY32LowEven = Avx2.Subtract(vecY32LowEven, vec128);

            var vecRedGreenHighEven = Avx2.UnpackHigh(redEven, greenEven);
            var vecBlueGreenHighEven = Avx2.UnpackHigh(blueEven, greenEven);
            var vecBlueZeroHighEven = Avx2.UnpackHigh(blueEven, vecZero16);
            var vecBlueBlueHighEven = Avx2.UnpackHigh(blueEven, blueEven);

            var vecY32HighEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighEven, vecCoeffYRedGreen), Avx2.MultiplyAddAdjacent(vecBlueGreenHighEven, vecCoeffYBlueGreen)), vec32768), 16);
            var vecCr32HighEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighEven, vecCoeffCrRedGreen), Avx2.MultiplyAddAdjacent(vecBlueZeroHighEven, vecCoeffCrBlueZero)), vec32768), 16);
            var vecCb32HighEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighEven, vecCoeffCbRedGreen), Avx2.MultiplyAddAdjacent(vecBlueBlueHighEven, vecCoeffCbBlueBlue)), vec32768), 16);

            vecY32HighEven = Avx2.Subtract(vecY32HighEven, vec128);

            var vecY16Even = Avx2.PackSignedSaturate(vecY32LowEven, vecY32HighEven);
            var vecCr16Even = Avx2.PackSignedSaturate(vecCr32LowEven, vecCr32HighEven);
            var vecCb16Even = Avx2.PackSignedSaturate(vecCb32LowEven, vecCb32HighEven);

            // ODD 16 PIXELS
            var vecRedGreenLowOdd = Avx2.UnpackLow(redOdd, greenOdd);
            var vecBlueGreenLowOdd = Avx2.UnpackLow(blueOdd, greenOdd);
            var vecBlueZeroLowOdd = Avx2.UnpackLow(blueOdd, vecZero16);
            var vecBlueBlueLowOdd = Avx2.UnpackLow(blueOdd, blueOdd);

            var vecY32LowOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowOdd, vecCoeffYRedGreen), Avx2.MultiplyAddAdjacent(vecBlueGreenLowOdd, vecCoeffYBlueGreen)), vec32768), 16);
            var vecCr32LowOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowOdd, vecCoeffCrRedGreen), Avx2.MultiplyAddAdjacent(vecBlueZeroLowOdd, vecCoeffCrBlueZero)), vec32768), 16);
            var vecCb32LowOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowOdd, vecCoeffCbRedGreen), Avx2.MultiplyAddAdjacent(vecBlueBlueLowOdd, vecCoeffCbBlueBlue)), vec32768), 16);

            vecY32LowOdd = Avx2.Subtract(vecY32LowOdd, vec128);

            var vecRedGreenHighOdd = Avx2.UnpackHigh(redOdd, greenOdd);
            var vecBlueGreenHighOdd = Avx2.UnpackHigh(blueOdd, greenOdd);
            var vecBlueZeroHighOdd = Avx2.UnpackHigh(blueOdd, vecZero16);
            var vecBlueBlueHighOdd = Avx2.UnpackHigh(blueOdd, blueOdd);

            var vecY32HighOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighOdd, vecCoeffYRedGreen), Avx2.MultiplyAddAdjacent(vecBlueGreenHighOdd, vecCoeffYBlueGreen)), vec32768), 16);
            var vecCr32HighOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighOdd, vecCoeffCrRedGreen), Avx2.MultiplyAddAdjacent(vecBlueZeroHighOdd, vecCoeffCrBlueZero)), vec32768), 16);
            var vecCb32HighOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighOdd, vecCoeffCbRedGreen), Avx2.MultiplyAddAdjacent(vecBlueBlueHighOdd, vecCoeffCbBlueBlue)), vec32768), 16);

            vecY32HighOdd = Avx2.Subtract(vecY32HighOdd, vec128);

            var vecY16Odd = Avx2.PackSignedSaturate(vecY32LowOdd, vecY32HighOdd);
            var vecCr16Odd = Avx2.PackSignedSaturate(vecCr32LowOdd, vecCr32HighOdd);
            var vecCb16Odd = Avx2.PackSignedSaturate(vecCb32LowOdd, vecCb32HighOdd);

            // FINAL PACK
            lumaEvenOut = Avx2.PackSignedSaturate(Avx2.UnpackLow(vecY16Even, vecY16Odd), Avx2.UnpackHigh(vecY16Even, vecY16Odd)).AsSByte();
            chromaBlueEvenOut = Avx2.PackSignedSaturate(Avx2.UnpackLow(vecCb16Even, vecCb16Odd), Avx2.UnpackHigh(vecCb16Even, vecCb16Odd)).AsSByte();
            chromaRedEvenOut = Avx2.PackSignedSaturate(Avx2.UnpackLow(vecCr16Even, vecCr16Odd), Avx2.UnpackHigh(vecCr16Even, vecCr16Odd)).AsSByte();

            lumaOddOut = Vector256<sbyte>.Zero;
            chromaBlueOddOut = Vector256<sbyte>.Zero;
            chromaRedOddOut = Vector256<sbyte>.Zero;
        }

        /// <summary>
        /// Vector256 (AVX2) implementation of the interlace phase of the RGB to YCbCr (Pigeon) transform.
        /// Interlace phase is tested independently for both correctness and performance to eliminate any bottlenecks
        /// and ensure optimal codegen. Interlace implementation will rearrange any three single type data vectors
        /// into three interlaced vectors of the interlaced types so obviously its not restricted to BGR format.
        /// Further optimizations are possible based on the analysis of jit disasm data but were not attempted yet.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void InterlaceBgrVector256(
            Vector256<byte> vecA, Vector256<byte> vecB,
            Vector256<byte> vecC, Vector256<byte> vecD,
            Vector256<byte> vecE, Vector256<byte> vecF,
            out Vector256<byte> outA, out Vector256<byte> outD, out Vector256<byte> outF)
        {
            if (!Avx2.IsSupported)
            {
                throw new PlatformNotSupportedException("AVX2 is not supported on this platform.");
            }

            vecA = Avx2.UnpackLow(vecA, vecC);
            vecE = Avx2.UnpackLow(vecE, vecB);
            vecD = Avx2.UnpackLow(vecD, vecF);

            var vecH = Avx2.ShiftRightLogical128BitLane(vecA, 2);
            var vecG = Avx2.UnpackHigh(vecA.AsInt16(), vecE.AsInt16()).AsByte();
            vecA = Avx2.UnpackLow(vecA.AsInt16(), vecE.AsInt16()).AsByte();

            vecE = Avx2.ShiftRightLogical128BitLane(vecE, 2);
            vecB = Avx2.ShiftRightLogical128BitLane(vecD, 2);
            vecC = Avx2.UnpackHigh(vecD.AsInt16(), vecH.AsInt16()).AsByte();
            vecD = Avx2.UnpackLow(vecD.AsInt16(), vecH.AsInt16()).AsByte();

            vecF = Avx2.UnpackHigh(vecE.AsInt16(), vecB.AsInt16()).AsByte();
            vecE = Avx2.UnpackLow(vecE.AsInt16(), vecB.AsInt16()).AsByte();

            vecH = Avx2.Shuffle(vecA.AsInt32(), 0x4E).AsByte();
            vecA = Avx2.UnpackLow(vecA.AsInt32(), vecD.AsInt32()).AsByte();
            vecD = Avx2.UnpackHigh(vecD.AsInt32(), vecE.AsInt32()).AsByte();
            vecE = Avx2.UnpackLow(vecE.AsInt32(), vecH.AsInt32()).AsByte();

            vecH = Avx2.Shuffle(vecG.AsInt32(), 0x4E).AsByte();
            vecG = Avx2.UnpackLow(vecG.AsInt32(), vecC.AsInt32()).AsByte();
            vecC = Avx2.UnpackHigh(vecC.AsInt32(), vecF.AsInt32()).AsByte();
            vecF = Avx2.UnpackLow(vecF.AsInt32(), vecH.AsInt32()).AsByte();

            vecH = Avx2.UnpackLow(vecA.AsInt64(), vecE.AsInt64()).AsByte();
            vecG = Avx2.UnpackLow(vecD.AsInt64(), vecG.AsInt64()).AsByte();
            vecC = Avx2.UnpackLow(vecF.AsInt64(), vecC.AsInt64()).AsByte();

            outA = Avx2.Permute2x128(vecH, vecG, 0x20);
            outD = Avx2.Permute2x128(vecC, vecH, 0x30);
            outF = Avx2.Permute2x128(vecG, vecC, 0x31);
        }

        /// <summary>
        /// Vector256 (AVX2) implementation of the RGB to YCbCr transform.
        /// This implementation aggressively inlines all stages (deinterlace, math, interlace) to eliminate
        /// function call overhead, achieving maximum throughput despite heavy register pressure.
        /// It serves as the primary high-performance target for modern x64 CPUs and was heavily benchmarked
        /// against other existing implementations. The Rgb2YCbCr method selects this path if AVX2 is available,
        /// image width is >= 32 pixels and threshold for multithreaded processing was not reached.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe void Rgb2YCbCrVector256(Pixel* pPixBuff, int width, int height, int rowSizeInBytes, sbyte* pOutY, sbyte* pOutCb, sbyte* pOutCr, int outRowSizeInBytes)
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
                    nameof(width), width, "Width must be greater than zero.");
            }
            if (height <= 0)
            {
                throw new DjvuArgumentOutOfRangeException(
                    nameof(height), height, "Height must be greater than zero.");
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

            if (!Avx2.IsSupported)
            {
                throw new PlatformNotSupportedException("AVX2 is not supported on this platform.");
            }
            else if (width < 32)
            {
                throw new DjvuArgumentOutOfRangeException(
                    nameof(width),
                    width,
                    "Width must be at least 32 pixels for AVX2 Vector256 processing.");
            }

            long inPadBytes = rowSizeInBytes - ((long)width * sizeof(Pixel));
            int outPadBytes = outRowSizeInBytes - width;

            int vectorBound = width - 32;
            int tailShift = (32 - (width % 32)) % 32;
            int tailShiftBytes = tailShift * sizeof(Pixel);

            var v128 = Vector256.Create((int)128);
            var v32768 = Vector256.Create((int)32768);
            var vZero16 = Vector256<short>.Zero;
            var vZero8 = Vector256<byte>.Zero;

            short cY_R = 19946, cY_G1 = 19946, cY_G2 = 19945, cY_B = 5698;
            short cCr_R = 30393, cCr_G = -26594, cCr_B = -3799;
            short cCb_R = -11397, cCb_G = -22795, cCb_B1 = 17096, cCb_B2 = 17096;

            var vCoeffY_RG = Vector256.Create(cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1);
            var vCoeffY_BG = Vector256.Create(cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2);
            var vCoeffCr_RG = Vector256.Create(cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G);
            var vCoeffCr_BZ = Vector256.Create(cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0);
            var vCoeffCb_RG = Vector256.Create(cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G);
            var vCoeffCb_BB = Vector256.Create(cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2);

            for (int y = 0; y < height; y++)
            {
                int x = 0;
                while (x < width)
                {
                    int shift = (x > vectorBound) ? tailShift : 0;
                    int shiftBytes = (x > vectorBound) ? tailShiftBytes : 0;

                    byte* readPtr = (byte*)pIn - shiftBytes;
                    var ymmA = Vector256.Load(readPtr);
                    var ymmF = Vector256.Load(readPtr + 32);
                    var ymmB = Vector256.Load(readPtr + 64);

                    // 2. UNIFIED MATH BLOCK
                    var ymmC = ymmA;
                    ymmA = Avx2.InsertVector128(ymmF, ymmA.GetLower(), 0);
                    ymmC = Avx2.InsertVector128(ymmC, ymmB.GetLower(), 0);
                    ymmB = Avx2.InsertVector128(ymmB, ymmF.GetLower(), 0);
                    ymmF = Avx2.Permute2x128(ymmC, ymmC, 1);

                    var ymmG = ymmA;
                    ymmA = Avx2.ShiftLeftLogical128BitLane(ymmA, 8);
                    ymmG = Avx2.ShiftRightLogical128BitLane(ymmG, 8);
                    ymmA = Avx2.UnpackHigh(ymmA, ymmF);
                    ymmF = Avx2.ShiftLeftLogical128BitLane(ymmF, 8);
                    ymmG = Avx2.UnpackLow(ymmG, ymmB);
                    ymmF = Avx2.UnpackHigh(ymmF, ymmB);

                    var ymmD = ymmA;
                    ymmA = Avx2.ShiftLeftLogical128BitLane(ymmA, 8);
                    ymmD = Avx2.ShiftRightLogical128BitLane(ymmD, 8);
                    ymmA = Avx2.UnpackHigh(ymmA, ymmG);
                    ymmG = Avx2.ShiftLeftLogical128BitLane(ymmG, 8);
                    ymmD = Avx2.UnpackLow(ymmD, ymmF);
                    ymmG = Avx2.UnpackHigh(ymmG, ymmF);

                    var ymmE = ymmA;
                    ymmA = Avx2.ShiftLeftLogical128BitLane(ymmA, 8);
                    ymmE = Avx2.ShiftRightLogical128BitLane(ymmE, 8);
                    ymmA = Avx2.UnpackHigh(ymmA, ymmD);
                    ymmD = Avx2.ShiftLeftLogical128BitLane(ymmD, 8);
                    ymmE = Avx2.UnpackLow(ymmE, ymmG);
                    ymmD = Avx2.UnpackHigh(ymmD, ymmG);

                    ymmC = ymmA;
                    ymmA = Avx2.UnpackLow(ymmA, vZero8);
                    ymmC = Avx2.UnpackHigh(ymmC, vZero8);

                    ymmB = ymmE;
                    ymmE = Avx2.UnpackLow(ymmE, vZero8);
                    ymmB = Avx2.UnpackHigh(ymmB, vZero8);

                    ymmF = ymmD;
                    ymmD = Avx2.UnpackLow(ymmD, vZero8);
                    ymmF = Avx2.UnpackHigh(ymmF, vZero8);

                    var b_e = ymmA.AsInt16();
                    var g_e = ymmC.AsInt16();
                    var r_e = ymmE.AsInt16();
                    var b_o  = ymmB.AsInt16();
                    var g_o  = ymmD.AsInt16();
                    var r_o  = ymmF.AsInt16();

                    // MATH: EVEN 16 PIXELS
                    var vRG_L = Avx2.UnpackLow(r_e, g_e);
                    var vBG_L = Avx2.UnpackLow(b_e, g_e);
                    var vBZ_L = Avx2.UnpackLow(b_e, vZero16);
                    var vBB_L = Avx2.UnpackLow(b_e, b_e);

                    var vY32_L = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_L, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vBG_L, vCoeffY_BG)), v32768), 16);
                    var vCr32_L = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_L, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vBZ_L, vCoeffCr_BZ)), v32768), 16);
                    var vCb32_L = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_L, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vBB_L, vCoeffCb_BB)), v32768), 16);

                    vY32_L = Avx2.Subtract(vY32_L, v128);

                    var vRG_H = Avx2.UnpackHigh(r_e, g_e);
                    var vBG_H = Avx2.UnpackHigh(b_e, g_e);
                    var vBZ_H = Avx2.UnpackHigh(b_e, vZero16);
                    var vBB_H = Avx2.UnpackHigh(b_e, b_e);

                    var vY32_H = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_H, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vBG_H, vCoeffY_BG)), v32768), 16);
                    var vCr32_H = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_H, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vBZ_H, vCoeffCr_BZ)), v32768), 16);
                    var vCb32_H = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_H, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vBB_H, vCoeffCb_BB)), v32768), 16);

                    vY32_H = Avx2.Subtract(vY32_H, v128);

                    var vY16_E = Avx2.PackSignedSaturate(vY32_L, vY32_H);
                    var vCr16_E = Avx2.PackSignedSaturate(vCr32_L, vCr32_H);
                    var vCb16_E = Avx2.PackSignedSaturate(vCb32_L, vCb32_H);

                    // MATH: ODD 16 PIXELS
                    var vRG_L_o = Avx2.UnpackLow(r_o, g_o);
                    var vBG_L_o = Avx2.UnpackLow(b_o, g_o);
                    var vBZ_L_o = Avx2.UnpackLow(b_o, vZero16);
                    var vBB_L_o = Avx2.UnpackLow(b_o, b_o);

                    var vY32_L_o = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_L_o, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vBG_L_o, vCoeffY_BG)), v32768), 16);
                    var vCr32_L_o = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_L_o, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vBZ_L_o, vCoeffCr_BZ)), v32768), 16);
                    var vCb32_L_o = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_L_o, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vBB_L_o, vCoeffCb_BB)), v32768), 16);

                    vY32_L_o = Avx2.Subtract(vY32_L_o, v128);

                    var vRG_H_o = Avx2.UnpackHigh(r_o, g_o);
                    var vBG_H_o = Avx2.UnpackHigh(b_o, g_o);
                    var vBZ_H_o = Avx2.UnpackHigh(b_o, vZero16);
                    var vBB_H_o = Avx2.UnpackHigh(b_o, b_o);

                    var vY32_H_o = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_H_o, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vBG_H_o, vCoeffY_BG)), v32768), 16);
                    var vCr32_H_o = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_H_o, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vBZ_H_o, vCoeffCr_BZ)), v32768), 16);
                    var vCb32_H_o = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_H_o, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vBB_H_o, vCoeffCb_BB)), v32768), 16);

                    vY32_H_o = Avx2.Subtract(vY32_H_o, v128);

                    var vY16_O = Avx2.PackSignedSaturate(vY32_L_o, vY32_H_o);
                    var vCr16_O = Avx2.PackSignedSaturate(vCr32_L_o, vCr32_H_o);
                    var vCb16_O = Avx2.PackSignedSaturate(vCb32_L_o, vCb32_H_o);

                    // FINAL PACK & STORE
                    var outYVec = Avx2.PackSignedSaturate(Avx2.UnpackLow(vY16_E, vY16_O), Avx2.UnpackHigh(vY16_E, vY16_O)).AsSByte();
                    var outCbVec = Avx2.PackSignedSaturate(Avx2.UnpackLow(vCb16_E, vCb16_O), Avx2.UnpackHigh(vCb16_E, vCb16_O)).AsSByte();
                    var outCrVec = Avx2.PackSignedSaturate(Avx2.UnpackLow(vCr16_E, vCr16_O), Avx2.UnpackHigh(vCr16_E, vCr16_O)).AsSByte();

                    outYVec.Store(pY - shift);
                    outCbVec.Store(pCb - shift);
                    outCrVec.Store(pCr - shift);

                    int advance = 32 - shift;
                    pIn += advance;
                    pY += advance;
                    pCb += advance;
                    pCr += advance;
                    x += advance;
                }

                pIn = (Pixel*)((byte*)pIn + inPadBytes);
                pY += outPadBytes;
                pCb += outPadBytes;
                pCr += outPadBytes;
            }
        }

        /// <summary>
        /// Multithreaded Vector256 (AVX2) implementation of the RGB to YCbCr (Pigeon) transform.
        /// Wraps the AVX2 pipeline in a <see cref="System.Threading.Tasks.Parallel.For"/> loop.
        /// This method is selected by the Rgb2YCbCr method when AVX2 is supported, image width is >= 32 pixels
        /// and threshold for multithreaded processing was reached.
        /// </summary>
        internal static unsafe void Rgb2YCbCrParallelVector256(Pixel* pPixBuff, int width, int height, int rowSizeInBytes, sbyte* pOutY, sbyte* pOutCb, sbyte* pOutCr, int outRowSizeInBytes, ParallelOptions options)
        {
            if (pPixBuff == null) throw new DjvuArgumentNullException(nameof(pPixBuff));
            if (pOutY == null) throw new DjvuArgumentNullException(nameof(pOutY));
            if (pOutCb == null) throw new DjvuArgumentNullException(nameof(pOutCb));
            if (pOutCr == null) throw new DjvuArgumentNullException(nameof(pOutCr));
            if (width <= 0) throw new DjvuArgumentOutOfRangeException(nameof(width), width, "Width must be greater than zero.");
            if (height <= 0) throw new DjvuArgumentOutOfRangeException(nameof(height), height, "Height must be greater than zero.");
            if (rowSizeInBytes < (long)width * sizeof(Pixel)) throw new DjvuArgumentOutOfRangeException(nameof(rowSizeInBytes), rowSizeInBytes, "Input row size invalid.");
            if (outRowSizeInBytes < width) throw new DjvuArgumentOutOfRangeException(nameof(outRowSizeInBytes), outRowSizeInBytes, "Output row size invalid.");

            byte* startIn = (byte*)pPixBuff;
            byte* endIn = startIn + ((long)height * rowSizeInBytes);
            byte* startY = (byte*)pOutY;
            byte* endY = startY + ((long)height * outRowSizeInBytes);
            byte* startCb = (byte*)pOutCb;
            byte* endCb = startCb + ((long)height * outRowSizeInBytes);
            byte* startCr = (byte*)pOutCr;
            byte* endCr = startCr + ((long)height * outRowSizeInBytes);
            if ((startIn < endY && endIn > startY) || (startIn < endCb && endIn > startCb) || (startIn < endCr && endIn > startCr))
                throw new DjvuInvalidOperationException("Source and destination buffers must be distinct and non-overlapping.");
            if ((startY < endCb && endY > startCb) || (startY < endCr && endY > startCr) || (startCb < endCr && endCb > startCr))
                throw new DjvuInvalidOperationException("Destination planar buffers (Y, Cb, Cr) must be distinct and not overlap.");

            if (!Avx2.IsSupported)
            {
                throw new PlatformNotSupportedException("AVX2 is not supported on this platform.");
            }
            else if (width < 32)
            {
                throw new DjvuArgumentOutOfRangeException(
                    nameof(width),
                    width,
                    "Width must be at least 32 pixels for AVX2 Vector256 processing.");
            }

            int vectorBound = width - 32;
            int tailShift = (32 - (width % 32)) % 32;
            int tailShiftBytes = tailShift * sizeof(Pixel);

            Parallel.For(0, height, options, y =>
            {
                long inOffset = (long)y * rowSizeInBytes;
                long outOffset = (long)y * outRowSizeInBytes;

                Pixel* pInRow = (Pixel*)((byte*)pPixBuff + inOffset);
                sbyte* pYRow = pOutY + outOffset;
                sbyte* pCbRow = pOutCb + outOffset;
                sbyte* pCrRow = pOutCr + outOffset;

                var v128 = Vector256.Create((int)128);
                var v32768 = Vector256.Create((int)32768);
                var vZero16 = Vector256<short>.Zero;
                var vZero8 = Vector256<byte>.Zero;

                short cY_R = 19946, cY_G1 = 19946, cY_G2 = 19945, cY_B = 5698;
                short cCr_R = 30393, cCr_G = -26594, cCr_B = -3799;
                short cCb_R = -11397, cCb_G = -22795, cCb_B1 = 17096, cCb_B2 = 17096;

                var vCoeffY_RG = Vector256.Create(cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1);
                var vCoeffY_BG = Vector256.Create(cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2);
                var vCoeffCr_RG = Vector256.Create(cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G);
                var vCoeffCr_BZ = Vector256.Create(cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0);
                var vCoeffCb_RG = Vector256.Create(cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G);
                var vCoeffCb_BB = Vector256.Create(cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2);

                int x = 0;
                while (x < width)
                {
                    int shift = (x > vectorBound) ? tailShift : 0;
                    int shiftBytes = (x > vectorBound) ? tailShiftBytes : 0;

                    byte* readPtr = (byte*)pInRow - shiftBytes;
                    var ymmA = Vector256.Load(readPtr);
                    var ymmF = Vector256.Load(readPtr + 32);
                    var ymmB = Vector256.Load(readPtr + 64);

                    var ymmC = ymmA;
                    ymmA = Avx2.InsertVector128(ymmF, ymmA.GetLower(), 0);
                    ymmC = Avx2.InsertVector128(ymmC, ymmB.GetLower(), 0);
                    ymmB = Avx2.InsertVector128(ymmB, ymmF.GetLower(), 0);
                    ymmF = Avx2.Permute2x128(ymmC, ymmC, 1);

                    var ymmG = ymmA;
                    ymmA = Avx2.ShiftLeftLogical128BitLane(ymmA, 8);
                    ymmG = Avx2.ShiftRightLogical128BitLane(ymmG, 8);
                    ymmA = Avx2.UnpackHigh(ymmA, ymmF);
                    ymmF = Avx2.ShiftLeftLogical128BitLane(ymmF, 8);
                    ymmG = Avx2.UnpackLow(ymmG, ymmB);
                    ymmF = Avx2.UnpackHigh(ymmF, ymmB);

                    var ymmD = ymmA;
                    ymmA = Avx2.ShiftLeftLogical128BitLane(ymmA, 8);
                    ymmD = Avx2.ShiftRightLogical128BitLane(ymmD, 8);
                    ymmA = Avx2.UnpackHigh(ymmA, ymmG);
                    ymmG = Avx2.ShiftLeftLogical128BitLane(ymmG, 8);
                    ymmD = Avx2.UnpackLow(ymmD, ymmF);
                    ymmG = Avx2.UnpackHigh(ymmG, ymmF);

                    var ymmE = ymmA;
                    ymmA = Avx2.ShiftLeftLogical128BitLane(ymmA, 8);
                    ymmE = Avx2.ShiftRightLogical128BitLane(ymmE, 8);
                    ymmA = Avx2.UnpackHigh(ymmA, ymmD);
                    ymmD = Avx2.ShiftLeftLogical128BitLane(ymmD, 8);
                    ymmE = Avx2.UnpackLow(ymmE, ymmG);
                    ymmD = Avx2.UnpackHigh(ymmD, ymmG);

                    ymmC = ymmA;
                    ymmA = Avx2.UnpackLow(ymmA, vZero8);
                    ymmC = Avx2.UnpackHigh(ymmC, vZero8);

                    ymmB = ymmE;
                    ymmE = Avx2.UnpackLow(ymmE, vZero8);
                    ymmB = Avx2.UnpackHigh(ymmB, vZero8);

                    ymmF = ymmD;
                    ymmD = Avx2.UnpackLow(ymmD, vZero8);
                    ymmF = Avx2.UnpackHigh(ymmF, vZero8);

                    var b_e = ymmA.AsInt16();
                    var g_e = ymmC.AsInt16();
                    var r_e = ymmE.AsInt16();
                    var b_o  = ymmB.AsInt16();
                    var g_o  = ymmD.AsInt16();
                    var r_o  = ymmF.AsInt16();

                    var vRG_L = Avx2.UnpackLow(r_e, g_e);
                    var vBG_L = Avx2.UnpackLow(b_e, g_e);
                    var vBZ_L = Avx2.UnpackLow(b_e, vZero16);
                    var vBB_L = Avx2.UnpackLow(b_e, b_e);

                    var vY32_L = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_L, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vBG_L, vCoeffY_BG)), v32768), 16);
                    var vCr32_L = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_L, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vBZ_L, vCoeffCr_BZ)), v32768), 16);
                    var vCb32_L = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_L, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vBB_L, vCoeffCb_BB)), v32768), 16);

                    vY32_L = Avx2.Subtract(vY32_L, v128);

                    var vRG_H = Avx2.UnpackHigh(r_e, g_e);
                    var vBG_H = Avx2.UnpackHigh(b_e, g_e);
                    var vBZ_H = Avx2.UnpackHigh(b_e, vZero16);
                    var vBB_H = Avx2.UnpackHigh(b_e, b_e);

                    var vY32_H = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_H, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vBG_H, vCoeffY_BG)), v32768), 16);
                    var vCr32_H = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_H, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vBZ_H, vCoeffCr_BZ)), v32768), 16);
                    var vCb32_H = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_H, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vBB_H, vCoeffCb_BB)), v32768), 16);

                    vY32_H = Avx2.Subtract(vY32_H, v128);

                    var vY16_E = Avx2.PackSignedSaturate(vY32_L, vY32_H);
                    var vCr16_E = Avx2.PackSignedSaturate(vCr32_L, vCr32_H);
                    var vCb16_E = Avx2.PackSignedSaturate(vCb32_L, vCb32_H);

                    var vRG_L_o = Avx2.UnpackLow(r_o, g_o);
                    var vBG_L_o = Avx2.UnpackLow(b_o, g_o);
                    var vBZ_L_o = Avx2.UnpackLow(b_o, vZero16);
                    var vBB_L_o = Avx2.UnpackLow(b_o, b_o);

                    var vY32_L_o = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_L_o, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vBG_L_o, vCoeffY_BG)), v32768), 16);
                    var vCr32_L_o = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_L_o, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vBZ_L_o, vCoeffCr_BZ)), v32768), 16);
                    var vCb32_L_o = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_L_o, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vBB_L_o, vCoeffCb_BB)), v32768), 16);

                    vY32_L_o = Avx2.Subtract(vY32_L_o, v128);

                    var vRG_H_o = Avx2.UnpackHigh(r_o, g_o);
                    var vBG_H_o = Avx2.UnpackHigh(b_o, g_o);
                    var vBZ_H_o = Avx2.UnpackHigh(b_o, vZero16);
                    var vBB_H_o = Avx2.UnpackHigh(b_o, b_o);

                    var vY32_H_o = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_H_o, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vBG_H_o, vCoeffY_BG)), v32768), 16);
                    var vCr32_H_o = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_H_o, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vBZ_H_o, vCoeffCr_BZ)), v32768), 16);
                    var vCb32_H_o = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vRG_H_o, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vBB_H_o, vCoeffCb_BB)), v32768), 16);

                    vY32_H_o = Avx2.Subtract(vY32_H_o, v128);

                    var vY16_O = Avx2.PackSignedSaturate(vY32_L_o, vY32_H_o);
                    var vCr16_O = Avx2.PackSignedSaturate(vCr32_L_o, vCr32_H_o);
                    var vCb16_O = Avx2.PackSignedSaturate(vCb32_L_o, vCb32_H_o);

                    var outYVec = Avx2.PackSignedSaturate(Avx2.UnpackLow(vY16_E, vY16_O), Avx2.UnpackHigh(vY16_E, vY16_O));
                    var outCbVec = Avx2.PackSignedSaturate(Avx2.UnpackLow(vCb16_E, vCb16_O), Avx2.UnpackHigh(vCb16_E, vCb16_O));
                    var outCrVec = Avx2.PackSignedSaturate(Avx2.UnpackLow(vCr16_E, vCr16_O), Avx2.UnpackHigh(vCr16_E, vCr16_O));

                    outYVec.Store(pYRow - shift);
                    outCbVec.Store(pCbRow - shift);
                    outCrVec.Store(pCrRow - shift);

                    int advance = 32 - shift;
                    pInRow += advance;
                    pYRow += advance;
                    pCbRow += advance;
                    pCrRow += advance;
                    x += advance;
                }
            });
        }

        /// <summary>
        /// Hybrid Vector256/Vector128 implementation of the RGB to YCbCr (Pigeon) transform.
        /// This implementation performs the deinterlace step using Vector128 and SSSE3 intrinsics,
        /// which testing has shown to be more efficient than pure AVX2 deinterlacing.
        /// From a cold start, this hybrid approach is roughly 5% faster even on 60 MB images.
        /// However, it becomes slower by about 10% after longer sustained execution on larger images,
        /// which is the normal operational profile when processing multi-layered DjVu documents.
        /// </summary>
        internal static unsafe void Rgb2YCbCrHybridVector(Pixel* pPixBuff, int width, int height, int rowSizeInBytes, sbyte* pOutY, sbyte* pOutCb, sbyte* pOutCr, int outRowSizeInBytes)
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

            if (!Avx2.IsSupported)
            {
                throw new PlatformNotSupportedException("AVX2 is not supported on this platform.");
            }
            else if (width < 32)
            {
                throw new DjvuArgumentOutOfRangeException(
                    nameof(width),
                    width,
                    "Width must be at least 32 pixels for AVX2 Vector256 processing.");
            }

            long inPadBytes = rowSizeInBytes - ((long)width * sizeof(Pixel));
            int outPadBytes = outRowSizeInBytes - width;

            int vectorBound = width - 32;
            int tailShift = (32 - (width % 32)) % 32;
            int tailShiftBytes = tailShift * sizeof(Pixel);

            var v128 = Vector256.Create((int)128);
            var v32768 = Vector256.Create((int)32768);
            var vZero16 = Vector256<short>.Zero;
            var vZero8 = Vector256<byte>.Zero;

            short cY_R = 19946, cY_G1 = 19946, cY_G2 = 19945, cY_B = 5698;
            short cCr_R = 30393, cCr_G = -26594, cCr_B = -3799;
            short cCb_R = -11397, cCb_G = -22795, cCb_B1 = 17096, cCb_B2 = 17096;

            var vCoeffY_RG = Vector256.Create(cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1);
            var vCoeffY_BG = Vector256.Create(cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2);
            var vCoeffCr_RG = Vector256.Create(cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G);
            var vCoeffCr_BZ = Vector256.Create(cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0);
            var vCoeffCb_RG = Vector256.Create(cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G);
            var vCoeffCb_BB = Vector256.Create(cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2);

            var blueMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
            var blueMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14, 128, 128, 128, 128, 128);
            var blueMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 1, 4, 7, 10, 13);
            var greenMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
            var greenMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128);
            var greenMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14);
            var redMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
            var redMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128);
            var redMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15);

            for (int y = 0; y < height; y++)
            {
                int x = 0;
                while (x < width)
                {
                    int shift = (x > vectorBound) ? tailShift : 0;
                    int shiftBytes = (x > vectorBound) ? tailShiftBytes : 0;

                    byte* readPtr = (byte*)pIn - shiftBytes;

                    var vec0 = Vector128.Load(readPtr);
                    var vec1 = Vector128.Load(readPtr + 16);
                    var vec2 = Vector128.Load(readPtr + 32);
                    var vec0B = Vector128.Load(readPtr + 48);
                    var vec1B = Vector128.Load(readPtr + 64);
                    var vec2B = Vector128.Load(readPtr + 80);

                    var blue128A = Sse2.Or(Ssse3.Shuffle(vec0, blueMask0), Sse2.Or(Ssse3.Shuffle(vec1, blueMask1), Ssse3.Shuffle(vec2, blueMask2)));
                    var green128A = Sse2.Or(Ssse3.Shuffle(vec0, greenMask0), Sse2.Or(Ssse3.Shuffle(vec1, greenMask1), Ssse3.Shuffle(vec2, greenMask2)));
                    var red128A = Sse2.Or(Ssse3.Shuffle(vec0, redMask0), Sse2.Or(Ssse3.Shuffle(vec1, redMask1), Ssse3.Shuffle(vec2, redMask2)));

                    var blue128B = Sse2.Or(Ssse3.Shuffle(vec0B, blueMask0), Sse2.Or(Ssse3.Shuffle(vec1B, blueMask1), Ssse3.Shuffle(vec2B, blueMask2)));
                    var green128B = Sse2.Or(Ssse3.Shuffle(vec0B, greenMask0), Sse2.Or(Ssse3.Shuffle(vec1B, greenMask1), Ssse3.Shuffle(vec2B, greenMask2)));
                    var red128B = Sse2.Or(Ssse3.Shuffle(vec0B, redMask0), Sse2.Or(Ssse3.Shuffle(vec1B, redMask1), Ssse3.Shuffle(vec2B, redMask2)));

                    var blueEven = Vector256.Create(Vector128.WidenLower(blue128A).AsInt16(), Vector128.WidenUpper(blue128A).AsInt16());
                    var greenEven = Vector256.Create(Vector128.WidenLower(green128A).AsInt16(), Vector128.WidenUpper(green128A).AsInt16());
                    var redEven = Vector256.Create(Vector128.WidenLower(red128A).AsInt16(), Vector128.WidenUpper(red128A).AsInt16());

                    var blueOdd = Vector256.Create(Vector128.WidenLower(blue128B).AsInt16(), Vector128.WidenUpper(blue128B).AsInt16());
                    var greenOdd = Vector256.Create(Vector128.WidenLower(green128B).AsInt16(), Vector128.WidenUpper(green128B).AsInt16());
                    var redOdd = Vector256.Create(Vector128.WidenLower(red128B).AsInt16(), Vector128.WidenUpper(red128B).AsInt16());

                    // We already widened from unsigned byte to signed short using WidenLower.
                    // Now feed directly into Math (we don't need lumaEven as it was only generated by the Vector256 deinterlace bug, we use greenEven instead)
                    var vecRedGreenLowEven = Avx2.UnpackLow(redEven, greenEven);
                    var vecBlueGreenLowEven = Avx2.UnpackLow(blueEven, greenEven);
                    var vecBlueZeroLowEven = Avx2.UnpackLow(blueEven, vZero16);
                    var vecBlueBlueLowEven = Avx2.UnpackLow(blueEven, blueEven);

                    var vecY32LowEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowEven, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vecBlueGreenLowEven, vCoeffY_BG)), v32768), 16);
                    var vecCr32LowEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowEven, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vecBlueZeroLowEven, vCoeffCr_BZ)), v32768), 16);
                    var vecCb32LowEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowEven, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vecBlueBlueLowEven, vCoeffCb_BB)), v32768), 16);

                    vecY32LowEven = Avx2.Subtract(vecY32LowEven, v128);

                    var vecRedGreenHighEven = Avx2.UnpackHigh(redEven, greenEven);
                    var vecBlueGreenHighEven = Avx2.UnpackHigh(blueEven, greenEven);
                    var vecBlueZeroHighEven = Avx2.UnpackHigh(blueEven, vZero16);
                    var vecBlueBlueHighEven = Avx2.UnpackHigh(blueEven, blueEven);

                    var vecY32HighEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighEven, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vecBlueGreenHighEven, vCoeffY_BG)), v32768), 16);
                    var vecCr32HighEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighEven, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vecBlueZeroHighEven, vCoeffCr_BZ)), v32768), 16);
                    var vecCb32HighEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighEven, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vecBlueBlueHighEven, vCoeffCb_BB)), v32768), 16);

                    vecY32HighEven = Avx2.Subtract(vecY32HighEven, v128);

                    var vecY16Even = Avx2.PackSignedSaturate(vecY32LowEven, vecY32HighEven);
                    var vecCr16Even = Avx2.PackSignedSaturate(vecCr32LowEven, vecCr32HighEven);
                    var vecCb16Even = Avx2.PackSignedSaturate(vecCb32LowEven, vecCb32HighEven);

                    // MATH ODD
                    var vecRedGreenLowOdd = Avx2.UnpackLow(redOdd, greenOdd);
                    var vecBlueGreenLowOdd = Avx2.UnpackLow(blueOdd, greenOdd);
                    var vecBlueZeroLowOdd = Avx2.UnpackLow(blueOdd, vZero16);
                    var vecBlueBlueLowOdd = Avx2.UnpackLow(blueOdd, blueOdd);

                    var vecY32LowOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowOdd, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vecBlueGreenLowOdd, vCoeffY_BG)), v32768), 16);
                    var vecCr32LowOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowOdd, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vecBlueZeroLowOdd, vCoeffCr_BZ)), v32768), 16);
                    var vecCb32LowOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowOdd, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vecBlueBlueLowOdd, vCoeffCb_BB)), v32768), 16);

                    vecY32LowOdd = Avx2.Subtract(vecY32LowOdd, v128);

                    var vecRedGreenHighOdd = Avx2.UnpackHigh(redOdd, greenOdd);
                    var vecBlueGreenHighOdd = Avx2.UnpackHigh(blueOdd, greenOdd);
                    var vecBlueZeroHighOdd = Avx2.UnpackHigh(blueOdd, vZero16);
                    var vecBlueBlueHighOdd = Avx2.UnpackHigh(blueOdd, blueOdd);

                    var vecY32HighOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighOdd, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vecBlueGreenHighOdd, vCoeffY_BG)), v32768), 16);
                    var vecCr32HighOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighOdd, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vecBlueZeroHighOdd, vCoeffCr_BZ)), v32768), 16);
                    var vecCb32HighOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighOdd, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vecBlueBlueHighOdd, vCoeffCb_BB)), v32768), 16);

                    vecY32HighOdd = Avx2.Subtract(vecY32HighOdd, v128);

                    var vecY16Odd = Avx2.PackSignedSaturate(vecY32LowOdd, vecY32HighOdd);
                    var vecCr16Odd = Avx2.PackSignedSaturate(vecCr32LowOdd, vecCr32HighOdd);
                    var vecCb16Odd = Avx2.PackSignedSaturate(vecCb32LowOdd, vecCb32HighOdd);

                    // FINAL PACK & STORE
                    Vector256<sbyte> outYVec = Avx2.Permute4x64(Avx2.PackSignedSaturate(vecY16Even, vecY16Odd).AsInt64(), 0xD8).AsSByte();
                    Vector256<sbyte> outCbVec = Avx2.Permute4x64(Avx2.PackSignedSaturate(vecCb16Even, vecCb16Odd).AsInt64(), 0xD8).AsSByte();
                    Vector256<sbyte> outCrVec = Avx2.Permute4x64(Avx2.PackSignedSaturate(vecCr16Even, vecCr16Odd).AsInt64(), 0xD8).AsSByte();

                    sbyte* writeY = pY - shift;
                    sbyte* writeCb = pCb - shift;
                    sbyte* writeCr = pCr - shift;

                    outYVec.AsByte().Store((byte*)writeY);
                    outCbVec.AsByte().Store((byte*)writeCb);
                    outCrVec.AsByte().Store((byte*)writeCr);

                    int advance = 32 - shift;
                    pIn += advance;
                    pY += advance;
                    pCb += advance;
                    pCr += advance;
                    x += advance;
                }

                pIn = (Pixel*)((byte*)pIn + inPadBytes);
                pY += outPadBytes;
                pCb += outPadBytes;
                pCr += outPadBytes;
            }
        }

        /// <summary>
        /// Multithreaded hybrid Vector256/Vector128 implementation of the RGB to YCbCr (Pigeon) transform.
        /// Wraps the hybrid pipeline in a <see cref="System.Threading.Tasks.Parallel.For"/> loop.
        /// This implementation performs the deinterlace step using Vector128 and SSSE3 intrinsics,
        /// which testing has shown to be more efficient than pure AVX2 deinterlacing.
        /// From a cold start, this hybrid approach is roughly 5% faster even on 60 MB images.
        /// However, it becomes slower by about 10% after longer sustained execution on larger images,
        /// which is the normal operational profile when processing multi-layered DjVu documents.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe void Rgb2YCbCrParallelHybridVector(Pixel* pPixBuff, int width, int height, int rowSizeInBytes, sbyte* pOutY, sbyte* pOutCb, sbyte* pOutCr, int outRowSizeInBytes, ParallelOptions options)
        {
            if (pPixBuff == null) throw new DjvuArgumentNullException(nameof(pPixBuff));
            if (pOutY == null) throw new DjvuArgumentNullException(nameof(pOutY));
            if (pOutCb == null) throw new DjvuArgumentNullException(nameof(pOutCb));
            if (pOutCr == null) throw new DjvuArgumentNullException(nameof(pOutCr));
            if (width <= 0) throw new DjvuArgumentOutOfRangeException(nameof(width), width, "Width must be greater than zero.");
            if (height <= 0) throw new DjvuArgumentOutOfRangeException(nameof(height), height, "Height must be greater than zero.");

            if (!Avx2.IsSupported)
            {
                throw new PlatformNotSupportedException("AVX2 is not supported on this platform.");
            }
            else if (width < 32)
            {
                throw new DjvuArgumentOutOfRangeException(
                    nameof(width),
                    width,
                    "Width must be at least 32 pixels for AVX2 Vector256 processing.");
            }

            long inPadBytes = rowSizeInBytes - ((long)width * sizeof(Pixel));
            int outPadBytes = outRowSizeInBytes - width;

            int vectorBound = width - 32;
            int tailShift = (32 - (width % 32)) % 32;
            int tailShiftBytes = tailShift * sizeof(Pixel);

            Parallel.For(0, height, options, y =>
            {
                long inOffset = (long)y * rowSizeInBytes;
                long outOffset = (long)y * outRowSizeInBytes;

                Pixel* pInRow = (Pixel*)((byte*)pPixBuff + inOffset);
                sbyte* pYRow = pOutY + outOffset;
                sbyte* pCbRow = pOutCb + outOffset;
                sbyte* pCrRow = pOutCr + outOffset;

                var v128 = Vector256.Create((int)128);
                var v32768 = Vector256.Create((int)32768);
                var vZero16 = Vector256<short>.Zero;

                short cY_R = 19946, cY_G1 = 19946, cY_G2 = 19945, cY_B = 5698;
                short cCr_R = 30393, cCr_G = -26594, cCr_B = -3799;
                short cCb_R = -11397, cCb_G = -22795, cCb_B1 = 17096, cCb_B2 = 17096;

                var vCoeffY_RG = Vector256.Create(cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1, cY_R, cY_G1);
                var vCoeffY_BG = Vector256.Create(cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2, cY_B, cY_G2);
                var vCoeffCr_RG = Vector256.Create(cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G, cCr_R, cCr_G);
                var vCoeffCr_BZ = Vector256.Create(cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0, cCr_B, (short)0);
                var vCoeffCb_RG = Vector256.Create(cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G, cCb_R, cCb_G);
                var vCoeffCb_BB = Vector256.Create(cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2, cCb_B1, cCb_B2);

                var blueMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                var blueMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14, 128, 128, 128, 128, 128);
                var blueMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 1, 4, 7, 10, 13);
                var greenMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                var greenMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128);
                var greenMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14);
                var redMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                var redMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128);
                var redMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15);

                int x = 0;
                while (x < width)
                {
                    int shift = (x > vectorBound) ? tailShift : 0;
                    int shiftBytes = (x > vectorBound) ? tailShiftBytes : 0;

                    byte* readPtr = (byte*)pInRow - shiftBytes;

                    var vec0 = Vector128.Load(readPtr);
                    var vec1 = Vector128.Load(readPtr + 16);
                    var vec2 = Vector128.Load(readPtr + 32);
                    var vec0B = Vector128.Load(readPtr + 48);
                    var vec1B = Vector128.Load(readPtr + 64);
                    var vec2B = Vector128.Load(readPtr + 80);

                    var blue128A = Sse2.Or(Ssse3.Shuffle(vec0, blueMask0), Sse2.Or(Ssse3.Shuffle(vec1, blueMask1), Ssse3.Shuffle(vec2, blueMask2)));
                    var green128A = Sse2.Or(Ssse3.Shuffle(vec0, greenMask0), Sse2.Or(Ssse3.Shuffle(vec1, greenMask1), Ssse3.Shuffle(vec2, greenMask2)));
                    var red128A = Sse2.Or(Ssse3.Shuffle(vec0, redMask0), Sse2.Or(Ssse3.Shuffle(vec1, redMask1), Ssse3.Shuffle(vec2, redMask2)));

                    var blue128B = Sse2.Or(Ssse3.Shuffle(vec0B, blueMask0), Sse2.Or(Ssse3.Shuffle(vec1B, blueMask1), Ssse3.Shuffle(vec2B, blueMask2)));
                    var green128B = Sse2.Or(Ssse3.Shuffle(vec0B, greenMask0), Sse2.Or(Ssse3.Shuffle(vec1B, greenMask1), Ssse3.Shuffle(vec2B, greenMask2)));
                    var red128B = Sse2.Or(Ssse3.Shuffle(vec0B, redMask0), Sse2.Or(Ssse3.Shuffle(vec1B, redMask1), Ssse3.Shuffle(vec2B, redMask2)));

                    var blueEven = Vector256.Create(Vector128.WidenLower(blue128A).AsInt16(), Vector128.WidenUpper(blue128A).AsInt16());
                    var greenEven = Vector256.Create(Vector128.WidenLower(green128A).AsInt16(), Vector128.WidenUpper(green128A).AsInt16());
                    var redEven = Vector256.Create(Vector128.WidenLower(red128A).AsInt16(), Vector128.WidenUpper(red128A).AsInt16());

                    var blueOdd = Vector256.Create(Vector128.WidenLower(blue128B).AsInt16(), Vector128.WidenUpper(blue128B).AsInt16());
                    var greenOdd = Vector256.Create(Vector128.WidenLower(green128B).AsInt16(), Vector128.WidenUpper(green128B).AsInt16());
                    var redOdd = Vector256.Create(Vector128.WidenLower(red128B).AsInt16(), Vector128.WidenUpper(red128B).AsInt16());

                    var vecRedGreenLowEven = Avx2.UnpackLow(redEven, greenEven);
                    var vecBlueGreenLowEven = Avx2.UnpackLow(blueEven, greenEven);
                    var vecBlueZeroLowEven = Avx2.UnpackLow(blueEven, vZero16);
                    var vecBlueBlueLowEven = Avx2.UnpackLow(blueEven, blueEven);

                    var vecY32LowEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowEven, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vecBlueGreenLowEven, vCoeffY_BG)), v32768), 16);
                    var vecCr32LowEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowEven, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vecBlueZeroLowEven, vCoeffCr_BZ)), v32768), 16);
                    var vecCb32LowEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowEven, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vecBlueBlueLowEven, vCoeffCb_BB)), v32768), 16);

                    vecY32LowEven = Avx2.Subtract(vecY32LowEven, v128);

                    var vecRedGreenHighEven = Avx2.UnpackHigh(redEven, greenEven);
                    var vecBlueGreenHighEven = Avx2.UnpackHigh(blueEven, greenEven);
                    var vecBlueZeroHighEven = Avx2.UnpackHigh(blueEven, vZero16);
                    var vecBlueBlueHighEven = Avx2.UnpackHigh(blueEven, blueEven);

                    var vecY32HighEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighEven, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vecBlueGreenHighEven, vCoeffY_BG)), v32768), 16);
                    var vecCr32HighEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighEven, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vecBlueZeroHighEven, vCoeffCr_BZ)), v32768), 16);
                    var vecCb32HighEven = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighEven, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vecBlueBlueHighEven, vCoeffCb_BB)), v32768), 16);

                    vecY32HighEven = Avx2.Subtract(vecY32HighEven, v128);

                    var vecY16Even = Avx2.PackSignedSaturate(vecY32LowEven, vecY32HighEven);
                    var vecCr16Even = Avx2.PackSignedSaturate(vecCr32LowEven, vecCr32HighEven);
                    var vecCb16Even = Avx2.PackSignedSaturate(vecCb32LowEven, vecCb32HighEven);

                    var vecRedGreenLowOdd = Avx2.UnpackLow(redOdd, greenOdd);
                    var vecBlueGreenLowOdd = Avx2.UnpackLow(blueOdd, greenOdd);
                    var vecBlueZeroLowOdd = Avx2.UnpackLow(blueOdd, vZero16);
                    var vecBlueBlueLowOdd = Avx2.UnpackLow(blueOdd, blueOdd);

                    var vecY32LowOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowOdd, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vecBlueGreenLowOdd, vCoeffY_BG)), v32768), 16);
                    var vecCr32LowOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowOdd, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vecBlueZeroLowOdd, vCoeffCr_BZ)), v32768), 16);
                    var vecCb32LowOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenLowOdd, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vecBlueBlueLowOdd, vCoeffCb_BB)), v32768), 16);

                    vecY32LowOdd = Avx2.Subtract(vecY32LowOdd, v128);

                    var vecRedGreenHighOdd = Avx2.UnpackHigh(redOdd, greenOdd);
                    var vecBlueGreenHighOdd = Avx2.UnpackHigh(blueOdd, greenOdd);
                    var vecBlueZeroHighOdd = Avx2.UnpackHigh(blueOdd, vZero16);
                    var vecBlueBlueHighOdd = Avx2.UnpackHigh(blueOdd, blueOdd);

                    var vecY32HighOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighOdd, vCoeffY_RG), Avx2.MultiplyAddAdjacent(vecBlueGreenHighOdd, vCoeffY_BG)), v32768), 16);
                    var vecCr32HighOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighOdd, vCoeffCr_RG), Avx2.MultiplyAddAdjacent(vecBlueZeroHighOdd, vCoeffCr_BZ)), v32768), 16);
                    var vecCb32HighOdd = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.Add(Avx2.MultiplyAddAdjacent(vecRedGreenHighOdd, vCoeffCb_RG), Avx2.MultiplyAddAdjacent(vecBlueBlueHighOdd, vCoeffCb_BB)), v32768), 16);

                    vecY32HighOdd = Avx2.Subtract(vecY32HighOdd, v128);

                    var vecY16Odd = Avx2.PackSignedSaturate(vecY32LowOdd, vecY32HighOdd);
                    var vecCr16Odd = Avx2.PackSignedSaturate(vecCr32LowOdd, vecCr32HighOdd);
                    var vecCb16Odd = Avx2.PackSignedSaturate(vecCb32LowOdd, vecCb32HighOdd);

                    Vector256<sbyte> outYVec = Avx2.Permute4x64(Avx2.PackSignedSaturate(vecY16Even, vecY16Odd).AsInt64(), 0xD8).AsSByte();
                    Vector256<sbyte> outCbVec = Avx2.Permute4x64(Avx2.PackSignedSaturate(vecCb16Even, vecCb16Odd).AsInt64(), 0xD8).AsSByte();
                    Vector256<sbyte> outCrVec = Avx2.Permute4x64(Avx2.PackSignedSaturate(vecCr16Even, vecCr16Odd).AsInt64(), 0xD8).AsSByte();

                    sbyte* writeY = pYRow - shift;
                    sbyte* writeCb = pCbRow - shift;
                    sbyte* writeCr = pCrRow - shift;

                    outYVec.AsByte().Store((byte*)writeY);
                    outCbVec.AsByte().Store((byte*)writeCb);
                    outCrVec.AsByte().Store((byte*)writeCr);

                    int advance = 32 - shift;
                    pInRow += advance;
                    pYRow += advance;
                    pCbRow += advance;
                    pCrRow += advance;
                    x += advance;
                }
            });
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe void YCbCr2RgbVector256(Pixel* pixelBuffer, int width, int height, int rowSizeInBytes)
        {
            if (!Avx2.IsSupported)
            {
                throw new PlatformNotSupportedException("AVX2 is not supported on this platform.");
            }
            else if (width < 32)
            {
                throw new DjvuArgumentOutOfRangeException(nameof(width), width, "Width must be at least 32 pixels for Vector256 (AVX2) processing.");
            }

            Pixel* ptr = pixelBuffer;
            var vec128 = Vector256.Create((short)128);
            var vec0   = Vector256.Create((short)0);
            var vec255 = Vector256.Create((short)255);
            var vecZero8 = Vector256<byte>.Zero;

            int vectorBound = width - 32;
            int tailShift = (32 - (width % 32)) % 32;
            int tailShiftBytes = tailShift * sizeof(Pixel);

            for (int y = 0; y < height; y++)
            {
                byte* rowBase = (byte*)ptr;
                byte* pByte = rowBase;

                var ymmA = Vector256.Load(pByte);
                var ymmF = Vector256.Load(pByte + 32);
                var ymmB = Vector256.Load(pByte + 64);

                int x = 0;
                while (x < width)
                {
                    int nextX = x + 32;
                    int nextShiftBytes = (nextX > vectorBound) ? tailShiftBytes : 0;
                    byte* nextPtr = (nextX >= width) ? rowBase : (pByte + 96 - nextShiftBytes);

                    var nextYmmA = Vector256.Load(nextPtr);
                    var nextYmmF = Vector256.Load(nextPtr + 32);
                    var nextYmmB = Vector256.Load(nextPtr + 64);

                    var ymmC = ymmA;
                    ymmA = Avx2.InsertVector128(ymmF, ymmA.GetLower(), 0);
                    ymmC = Avx2.InsertVector128(ymmC, ymmB.GetLower(), 0);
                    ymmB = Avx2.InsertVector128(ymmB, ymmF.GetLower(), 0);
                    ymmF = Avx2.Permute2x128(ymmC, ymmC, 1);

                    var ymmG = ymmA;
                    ymmA = Avx2.ShiftLeftLogical128BitLane(ymmA, 8);
                    ymmG = Avx2.ShiftRightLogical128BitLane(ymmG, 8);
                    ymmA = Avx2.UnpackHigh(ymmA, ymmF);
                    ymmF = Avx2.ShiftLeftLogical128BitLane(ymmF, 8);
                    ymmG = Avx2.UnpackLow(ymmG, ymmB);
                    ymmF = Avx2.UnpackHigh(ymmF, ymmB);

                    var ymmD = ymmA;
                    ymmA = Avx2.ShiftLeftLogical128BitLane(ymmA, 8);
                    ymmD = Avx2.ShiftRightLogical128BitLane(ymmD, 8);
                    ymmA = Avx2.UnpackHigh(ymmA, ymmG);
                    ymmG = Avx2.ShiftLeftLogical128BitLane(ymmG, 8);
                    ymmD = Avx2.UnpackLow(ymmD, ymmF);
                    ymmG = Avx2.UnpackHigh(ymmG, ymmF);

                    var ymmE = ymmA;
                    ymmA = Avx2.ShiftLeftLogical128BitLane(ymmA, 8);
                    ymmE = Avx2.ShiftRightLogical128BitLane(ymmE, 8);
                    ymmA = Avx2.UnpackHigh(ymmA, ymmD);
                    ymmD = Avx2.ShiftLeftLogical128BitLane(ymmD, 8);
                    ymmE = Avx2.UnpackLow(ymmE, ymmG);
                    ymmD = Avx2.UnpackHigh(ymmD, ymmG);

                    var ymmH = vecZero8;

                    ymmC = ymmA;
                    ymmA = Avx2.UnpackLow(ymmA, ymmH);
                    ymmC = Avx2.UnpackHigh(ymmC, ymmH);

                    ymmB = ymmE;
                    ymmE = Avx2.UnpackLow(ymmE, ymmH);
                    ymmB = Avx2.UnpackHigh(ymmB, ymmH);

                    ymmF = ymmD;
                    ymmD = Avx2.UnpackLow(ymmD, ymmH);
                    ymmF = Avx2.UnpackHigh(ymmF, ymmH);

                    var y_even = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(ymmA.AsInt16(), 8), 8);
                    var b_even = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(ymmC.AsInt16(), 8), 8);
                    var r_even = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(ymmE.AsInt16(), 8), 8);
                    var y_odd  = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(ymmB.AsInt16(), 8), 8);
                    var b_odd  = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(ymmD.AsInt16(), 8), 8);
                    var r_odd  = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(ymmF.AsInt16(), 8), 8);

                    var tr_even = vec0;
                    var tg_even = vec0;
                    var tb_even = vec0;
                    {
                        var t1_even    = Avx2.ShiftRightArithmetic(b_even, 2);
                        var r_sh1_even = Avx2.ShiftRightArithmetic(r_even, 1);
                        var t2_even    = Avx2.Add(r_even, r_sh1_even);
                        var y_128_even = Avx2.Add(y_even, vec128);
                        var t3_even    = Avx2.Subtract(y_128_even, t1_even);

                        tr_even = Avx2.Add(y_128_even, t2_even);
                        var t2_sh1_even = Avx2.ShiftRightArithmetic(t2_even, 1);
                        var b_sh1_even  = Avx2.ShiftLeftLogical(b_even, 1);

                        tg_even = Avx2.Subtract(t3_even, t2_sh1_even);
                        tb_even = Avx2.Add(t3_even, b_sh1_even);

                        tr_even = Avx2.Max(vec0, Avx2.Min(vec255, tr_even));
                        tg_even = Avx2.Max(vec0, Avx2.Min(vec255, tg_even));
                        tb_even = Avx2.Max(vec0, Avx2.Min(vec255, tb_even));
                    }

                    var tr_odd = vec0;
                    var tg_odd = vec0;
                    var tb_odd = vec0;
                    {
                        var t1_odd    = Avx2.ShiftRightArithmetic(b_odd, 2);
                        var r_sh1_odd = Avx2.ShiftRightArithmetic(r_odd, 1);
                        var t2_odd    = Avx2.Add(r_odd, r_sh1_odd);
                        var y_128_odd = Avx2.Add(y_odd, vec128);
                        var t3_odd    = Avx2.Subtract(y_128_odd, t1_odd);

                        tr_odd = Avx2.Add(y_128_odd, t2_odd);
                        var t2_sh1_odd = Avx2.ShiftRightArithmetic(t2_odd, 1);
                        var b_sh1_odd  = Avx2.ShiftLeftLogical(b_odd, 1);

                        tg_odd = Avx2.Subtract(t3_odd, t2_sh1_odd);
                        tb_odd = Avx2.Add(t3_odd, b_sh1_odd);

                        tr_odd = Avx2.Max(vec0, Avx2.Min(vec255, tr_odd));
                        tg_odd = Avx2.Max(vec0, Avx2.Min(vec255, tg_odd));
                        tb_odd = Avx2.Max(vec0, Avx2.Min(vec255, tb_odd));
                    }

                    ymmA = Avx2.PackUnsignedSaturate(tb_even, tb_even);
                    ymmB = Avx2.PackUnsignedSaturate(tb_odd, tb_odd);
                    ymmC = Avx2.PackUnsignedSaturate(tg_even, tg_even);
                    ymmD = Avx2.PackUnsignedSaturate(tg_odd, tg_odd);
                    ymmE = Avx2.PackUnsignedSaturate(tr_even, tr_even);
                    ymmF = Avx2.PackUnsignedSaturate(tr_odd, tr_odd);

                    ymmA = Avx2.UnpackLow(ymmA, ymmC);
                    ymmE = Avx2.UnpackLow(ymmE, ymmB);
                    ymmD = Avx2.UnpackLow(ymmD, ymmF);

                    ymmH = Avx2.ShiftRightLogical128BitLane(ymmA, 2);
                    ymmG = Avx2.UnpackHigh(ymmA.AsInt16(), ymmE.AsInt16()).AsByte();
                    ymmA = Avx2.UnpackLow(ymmA.AsInt16(), ymmE.AsInt16()).AsByte();

                    ymmE = Avx2.ShiftRightLogical128BitLane(ymmE, 2);
                    ymmB = Avx2.ShiftRightLogical128BitLane(ymmD, 2);
                    ymmC = Avx2.UnpackHigh(ymmD.AsInt16(), ymmH.AsInt16()).AsByte();
                    ymmD = Avx2.UnpackLow(ymmD.AsInt16(), ymmH.AsInt16()).AsByte();

                    ymmF = Avx2.UnpackHigh(ymmE.AsInt16(), ymmB.AsInt16()).AsByte();
                    ymmE = Avx2.UnpackLow(ymmE.AsInt16(), ymmB.AsInt16()).AsByte();

                    ymmH = Avx2.Shuffle(ymmA.AsInt32(), 0x4E).AsByte();
                    ymmA = Avx2.UnpackLow(ymmA.AsInt32(), ymmD.AsInt32()).AsByte();
                    ymmD = Avx2.UnpackHigh(ymmD.AsInt32(), ymmE.AsInt32()).AsByte();
                    ymmE = Avx2.UnpackLow(ymmE.AsInt32(), ymmH.AsInt32()).AsByte();

                    ymmH = Avx2.Shuffle(ymmG.AsInt32(), 0x4E).AsByte();
                    ymmG = Avx2.UnpackLow(ymmG.AsInt32(), ymmC.AsInt32()).AsByte();
                    ymmC = Avx2.UnpackHigh(ymmC.AsInt32(), ymmF.AsInt32()).AsByte();
                    ymmF = Avx2.UnpackLow(ymmF.AsInt32(), ymmH.AsInt32()).AsByte();

                    ymmH = Avx2.UnpackLow(ymmA.AsInt64(), ymmE.AsInt64()).AsByte();
                    ymmG = Avx2.UnpackLow(ymmD.AsInt64(), ymmG.AsInt64()).AsByte();
                    ymmC = Avx2.UnpackLow(ymmF.AsInt64(), ymmC.AsInt64()).AsByte();

                    ymmA = Avx2.Permute2x128(ymmH, ymmG, 0x20);
                    ymmD = Avx2.Permute2x128(ymmC, ymmH, 0x30);
                    ymmF = Avx2.Permute2x128(ymmG, ymmC, 0x31);

                    int currentShiftBytes = (x > vectorBound) ? tailShiftBytes : 0;
                    byte* storePtr = pByte - currentShiftBytes;

                    ymmA.Store(storePtr);
                    ymmD.Store(storePtr + 32);
                    ymmF.Store(storePtr + 64);

                    ymmA = nextYmmA;
                    ymmF = nextYmmF;
                    ymmB = nextYmmB;

                    pByte += 96;
                    x += 32;
                }

                ptr = (Pixel*)(rowBase + rowSizeInBytes);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe void YCbCr2RgbParallelVector256(Pixel* pixelBuffer, int width, int height, int rowSizeInBytes, ParallelOptions options)
        {
            if (!Avx2.IsSupported)
            {
                throw new PlatformNotSupportedException("AVX2 is not supported on this platform.");
            }
            else if (width < 32)
            {
                throw new DjvuArgumentOutOfRangeException(nameof(width), width, "Width must be at least 32 pixels for Vector256 (AVX2) processing.");
            }

            int vectorBound = width - 32;
            int tailShift = (32 - (width % 32)) % 32;
            int tailShiftBytes = tailShift * sizeof(Pixel);

            Parallel.For(0, height, options, y =>
            {
                var vec128 = Vector256.Create((short)128);
                var vec0   = Vector256.Create((short)0);
                var vec255 = Vector256.Create((short)255);
                var vecZero8 = Vector256<byte>.Zero;

                byte* rowBase = (byte*)pixelBuffer + ((long)y * rowSizeInBytes);
                byte* pByte = rowBase;

                var ymmA = Vector256.Load(pByte);
                var ymmF = Vector256.Load(pByte + 32);
                var ymmB = Vector256.Load(pByte + 64);

                int x = 0;
                while (x < width)
                {
                    int nextX = x + 32;
                    int nextShiftBytes = (nextX > vectorBound) ? tailShiftBytes : 0;
                    byte* nextPtr = (nextX >= width) ? rowBase : (pByte + 96 - nextShiftBytes);

                    var nextYmmA = Vector256.Load(nextPtr);
                    var nextYmmF = Vector256.Load(nextPtr + 32);
                    var nextYmmB = Vector256.Load(nextPtr + 64);

                    var ymmC = ymmA;
                    ymmA = Avx2.InsertVector128(ymmF, ymmA.GetLower(), 0);
                    ymmC = Avx2.InsertVector128(ymmC, ymmB.GetLower(), 0);
                    ymmB = Avx2.InsertVector128(ymmB, ymmF.GetLower(), 0);
                    ymmF = Avx2.Permute2x128(ymmC, ymmC, 1);

                    var ymmG = ymmA;
                    ymmA = Avx2.ShiftLeftLogical128BitLane(ymmA, 8);
                    ymmG = Avx2.ShiftRightLogical128BitLane(ymmG, 8);
                    ymmA = Avx2.UnpackHigh(ymmA, ymmF);
                    ymmF = Avx2.ShiftLeftLogical128BitLane(ymmF, 8);
                    ymmG = Avx2.UnpackLow(ymmG, ymmB);
                    ymmF = Avx2.UnpackHigh(ymmF, ymmB);

                    var ymmD = ymmA;
                    ymmA = Avx2.ShiftLeftLogical128BitLane(ymmA, 8);
                    ymmD = Avx2.ShiftRightLogical128BitLane(ymmD, 8);
                    ymmA = Avx2.UnpackHigh(ymmA, ymmG);
                    ymmG = Avx2.ShiftLeftLogical128BitLane(ymmG, 8);
                    ymmD = Avx2.UnpackLow(ymmD, ymmF);
                    ymmG = Avx2.UnpackHigh(ymmG, ymmF);

                    var ymmE = ymmA;
                    ymmA = Avx2.ShiftLeftLogical128BitLane(ymmA, 8);
                    ymmE = Avx2.ShiftRightLogical128BitLane(ymmE, 8);
                    ymmA = Avx2.UnpackHigh(ymmA, ymmD);
                    ymmD = Avx2.ShiftLeftLogical128BitLane(ymmD, 8);
                    ymmE = Avx2.UnpackLow(ymmE, ymmG);
                    ymmD = Avx2.UnpackHigh(ymmD, ymmG);

                    var ymmH = vecZero8;

                    ymmC = ymmA;
                    ymmA = Avx2.UnpackLow(ymmA, ymmH);
                    ymmC = Avx2.UnpackHigh(ymmC, ymmH);

                    ymmB = ymmE;
                    ymmE = Avx2.UnpackLow(ymmE, ymmH);
                    ymmB = Avx2.UnpackHigh(ymmB, ymmH);

                    ymmF = ymmD;
                    ymmD = Avx2.UnpackLow(ymmD, ymmH);
                    ymmF = Avx2.UnpackHigh(ymmF, ymmH);

                    var y_even = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(ymmA.AsInt16(), 8), 8);
                    var b_even = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(ymmC.AsInt16(), 8), 8);
                    var r_even = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(ymmE.AsInt16(), 8), 8);
                    var y_odd  = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(ymmB.AsInt16(), 8), 8);
                    var b_odd  = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(ymmD.AsInt16(), 8), 8);
                    var r_odd  = Avx2.ShiftRightArithmetic(Avx2.ShiftLeftLogical(ymmF.AsInt16(), 8), 8);

                    var tr_even = vec0;
                    var tg_even = vec0;
                    var tb_even = vec0;
                    {
                        var t1_even    = Avx2.ShiftRightArithmetic(b_even, 2);
                        var r_sh1_even = Avx2.ShiftRightArithmetic(r_even, 1);
                        var t2_even    = Avx2.Add(r_even, r_sh1_even);
                        var y_128_even = Avx2.Add(y_even, vec128);
                        var t3_even    = Avx2.Subtract(y_128_even, t1_even);

                        tr_even = Avx2.Add(y_128_even, t2_even);
                        var t2_sh1_even = Avx2.ShiftRightArithmetic(t2_even, 1);
                        var b_sh1_even  = Avx2.ShiftLeftLogical(b_even, 1);

                        tg_even = Avx2.Subtract(t3_even, t2_sh1_even);
                        tb_even = Avx2.Add(t3_even, b_sh1_even);

                        tr_even = Avx2.Max(vec0, Avx2.Min(vec255, tr_even));
                        tg_even = Avx2.Max(vec0, Avx2.Min(vec255, tg_even));
                        tb_even = Avx2.Max(vec0, Avx2.Min(vec255, tb_even));
                    }

                    var tr_odd = vec0;
                    var tg_odd = vec0;
                    var tb_odd = vec0;
                    {
                        var t1_odd    = Avx2.ShiftRightArithmetic(b_odd, 2);
                        var r_sh1_odd = Avx2.ShiftRightArithmetic(r_odd, 1);
                        var t2_odd    = Avx2.Add(r_odd, r_sh1_odd);
                        var y_128_odd = Avx2.Add(y_odd, vec128);
                        var t3_odd    = Avx2.Subtract(y_128_odd, t1_odd);

                        tr_odd = Avx2.Add(y_128_odd, t2_odd);
                        var t2_sh1_odd = Avx2.ShiftRightArithmetic(t2_odd, 1);
                        var b_sh1_odd  = Avx2.ShiftLeftLogical(b_odd, 1);

                        tg_odd = Avx2.Subtract(t3_odd, t2_sh1_odd);
                        tb_odd = Avx2.Add(t3_odd, b_sh1_odd);

                        tr_odd = Avx2.Max(vec0, Avx2.Min(vec255, tr_odd));
                        tg_odd = Avx2.Max(vec0, Avx2.Min(vec255, tg_odd));
                        tb_odd = Avx2.Max(vec0, Avx2.Min(vec255, tb_odd));
                    }

                    ymmA = Avx2.PackUnsignedSaturate(tb_even, tb_even);
                    ymmB = Avx2.PackUnsignedSaturate(tb_odd, tb_odd);
                    ymmC = Avx2.PackUnsignedSaturate(tg_even, tg_even);
                    ymmD = Avx2.PackUnsignedSaturate(tg_odd, tg_odd);
                    ymmE = Avx2.PackUnsignedSaturate(tr_even, tr_even);
                    ymmF = Avx2.PackUnsignedSaturate(tr_odd, tr_odd);

                    ymmA = Avx2.UnpackLow(ymmA, ymmC);
                    ymmE = Avx2.UnpackLow(ymmE, ymmB);
                    ymmD = Avx2.UnpackLow(ymmD, ymmF);

                    ymmH = Avx2.ShiftRightLogical128BitLane(ymmA, 2);
                    ymmG = Avx2.UnpackHigh(ymmA.AsInt16(), ymmE.AsInt16()).AsByte();
                    ymmA = Avx2.UnpackLow(ymmA.AsInt16(), ymmE.AsInt16()).AsByte();

                    ymmE = Avx2.ShiftRightLogical128BitLane(ymmE, 2);
                    ymmB = Avx2.ShiftRightLogical128BitLane(ymmD, 2);
                    ymmC = Avx2.UnpackHigh(ymmD.AsInt16(), ymmH.AsInt16()).AsByte();
                    ymmD = Avx2.UnpackLow(ymmD.AsInt16(), ymmH.AsInt16()).AsByte();

                    ymmF = Avx2.UnpackHigh(ymmE.AsInt16(), ymmB.AsInt16()).AsByte();
                    ymmE = Avx2.UnpackLow(ymmE.AsInt16(), ymmB.AsInt16()).AsByte();

                    ymmH = Avx2.Shuffle(ymmA.AsInt32(), 0x4E).AsByte();
                    ymmA = Avx2.UnpackLow(ymmA.AsInt32(), ymmD.AsInt32()).AsByte();
                    ymmD = Avx2.UnpackHigh(ymmD.AsInt32(), ymmE.AsInt32()).AsByte();
                    ymmE = Avx2.UnpackLow(ymmE.AsInt32(), ymmH.AsInt32()).AsByte();

                    ymmH = Avx2.Shuffle(ymmG.AsInt32(), 0x4E).AsByte();
                    ymmG = Avx2.UnpackLow(ymmG.AsInt32(), ymmC.AsInt32()).AsByte();
                    ymmC = Avx2.UnpackHigh(ymmC.AsInt32(), ymmF.AsInt32()).AsByte();
                    ymmF = Avx2.UnpackLow(ymmF.AsInt32(), ymmH.AsInt32()).AsByte();

                    ymmH = Avx2.UnpackLow(ymmA.AsInt64(), ymmE.AsInt64()).AsByte();
                    ymmG = Avx2.UnpackLow(ymmD.AsInt64(), ymmG.AsInt64()).AsByte();
                    ymmC = Avx2.UnpackLow(ymmF.AsInt64(), ymmC.AsInt64()).AsByte();

                    ymmA = Avx2.Permute2x128(ymmH, ymmG, 0x20);
                    ymmD = Avx2.Permute2x128(ymmC, ymmH, 0x30);
                    ymmF = Avx2.Permute2x128(ymmG, ymmC, 0x31);

                    int currentShiftBytes = (x > vectorBound) ? tailShiftBytes : 0;
                    byte* storePtr = pByte - currentShiftBytes;

                    ymmA.Store(storePtr);
                    ymmD.Store(storePtr + 32);
                    ymmF.Store(storePtr + 64);

                    ymmA = nextYmmA;
                    ymmF = nextYmmF;
                    ymmB = nextYmmB;

                    pByte += 96;
                    x += 32;
                }
            });
        }

        internal static unsafe int YCbCr2RgbVector256(short* pY, short* pCb, short* pCr, sbyte* pImg8, int height, int width, int srcStride, int rowSizeInBytes, int startX = 0)
        {
            if (Avx2.IsSupported)
            {
                Vector256<short> v32 = Vector256.Create((short)32);
                Vector256<short> v128 = Vector256.Create((short)128);
                Vector256<short> v127 = Vector256.Create((short)127);
                Vector256<short> vNeg128 = Vector256.Create((short)-128);
                
                if (width - startX < 32) return startX;

                int vectorBound = width - 32;
                int tailShift = (32 - ((width - startX) % 32)) % 32;

                // .rdata allocated 256-bit masks (duplicated 128-bit lanes)
                Vector256<byte> maskB1 = Vector256.Create((ReadOnlySpan<byte>)[0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128, 5, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128, 5]);
                Vector256<byte> maskG1 = Vector256.Create((ReadOnlySpan<byte>)[128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128, 128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128]);
                Vector256<byte> maskR1 = Vector256.Create((ReadOnlySpan<byte>)[128, 128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128, 128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128]);

                Vector256<byte> maskB2 = Vector256.Create((ReadOnlySpan<byte>)[128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10, 128, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10, 128]);
                Vector256<byte> maskG2 = Vector256.Create((ReadOnlySpan<byte>)[5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10, 5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10]);
                Vector256<byte> maskR2 = Vector256.Create((ReadOnlySpan<byte>)[128, 5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 128, 5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128]);

                Vector256<byte> maskB3 = Vector256.Create((ReadOnlySpan<byte>)[128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128, 128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128, 128]);
                Vector256<byte> maskG3 = Vector256.Create((ReadOnlySpan<byte>)[128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128, 128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128]);
                Vector256<byte> maskR3 = Vector256.Create((ReadOnlySpan<byte>)[10, 128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 10, 128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15]);

                for (int y = 0, pidx = 0, ridx = 0; y < height; y++, pidx += srcStride, ridx += rowSizeInBytes)
                {
                    int x = startX;

                    while (x < width)
                    {
                        int shift = (x > vectorBound) ? tailShift : 0;
                        int j = x - shift;
                        int pixidx = ridx + (j * 3);
                        // Block 1 (Pixels 0-15)
                        Vector256<short> y1 = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.LoadVector256(pY + pidx + j), v32), 6);
                        Vector256<short> cb1 = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.LoadVector256(pCb + pidx + j), v32), 6);
                        Vector256<short> cr1 = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.LoadVector256(pCr + pidx + j), v32), 6);

                        y1 = Avx2.Max(Avx2.Min(y1, v127), vNeg128);
                        cb1 = Avx2.Max(Avx2.Min(cb1, v127), vNeg128);
                        cr1 = Avx2.Max(Avx2.Min(cr1, v127), vNeg128);

                        // Block 2 (Pixels 16-31)
                        Vector256<short> y2 = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.LoadVector256(pY + pidx + j + 16), v32), 6);
                        Vector256<short> cb2 = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.LoadVector256(pCb + pidx + j + 16), v32), 6);
                        Vector256<short> cr2 = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.LoadVector256(pCr + pidx + j + 16), v32), 6);

                        y2 = Avx2.Max(Avx2.Min(y2, v127), vNeg128);
                        cb2 = Avx2.Max(Avx2.Min(cb2, v127), vNeg128);
                        cr2 = Avx2.Max(Avx2.Min(cr2, v127), vNeg128);

                        // Transform Block 1
                        Vector256<short> cbShift1 = Avx2.ShiftRightArithmetic(cb1, 2);
                        Vector256<short> crShift1 = Avx2.ShiftRightArithmetic(cr1, 1);
                        Vector256<short> crAdd1 = Avx2.Add(cr1, crShift1);
                        Vector256<short> yAdd1 = Avx2.Add(y1, v128);
                        Vector256<short> ySub1 = Avx2.Subtract(yAdd1, cbShift1);

                        Vector256<short> r1 = Avx2.Add(yAdd1, crAdd1);
                        Vector256<short> g1 = Avx2.Subtract(ySub1, Avx2.ShiftRightArithmetic(crAdd1, 1));
                        Vector256<short> b1 = Avx2.Add(ySub1, Avx2.Add(cb1, cb1));

                        // Transform Block 2
                        Vector256<short> cbShift2 = Avx2.ShiftRightArithmetic(cb2, 2);
                        Vector256<short> crShift2 = Avx2.ShiftRightArithmetic(cr2, 1);
                        Vector256<short> crAdd2 = Avx2.Add(cr2, crShift2);
                        Vector256<short> yAdd2 = Avx2.Add(y2, v128);
                        Vector256<short> ySub2 = Avx2.Subtract(yAdd2, cbShift2);

                        Vector256<short> r2 = Avx2.Add(yAdd2, crAdd2);
                        Vector256<short> g2 = Avx2.Subtract(ySub2, Avx2.ShiftRightArithmetic(crAdd2, 1));
                        Vector256<short> b2 = Avx2.Add(ySub2, Avx2.Add(cb2, cb2));

                        // Saturate 16-bit to 8-bit. AVX2 splits lanes during packing.
                        Vector256<byte> vecR = Avx2.PackUnsignedSaturate(r1, r2);
                        Vector256<byte> vecG = Avx2.PackUnsignedSaturate(g1, g2);
                        Vector256<byte> vecB = Avx2.PackUnsignedSaturate(b1, b2);

                        // Untangle the 128-bit lanes (0xD8 = 11_01_10_00) to linearize the bytes.
                        Vector256<byte> vecR_ordered = Avx2.Permute4x64(vecR.AsInt64(), 0xD8).AsByte();
                        Vector256<byte> vecG_ordered = Avx2.Permute4x64(vecG.AsInt64(), 0xD8).AsByte();
                        Vector256<byte> vecB_ordered = Avx2.Permute4x64(vecB.AsInt64(), 0xD8).AsByte();

                        // Shuffle and interleave across both lanes simultaneously
                        Vector256<byte> out1_4 = Avx2.Or(Avx2.Or(Avx2.Shuffle(vecB_ordered, maskB1), Avx2.Shuffle(vecG_ordered, maskG1)), Avx2.Shuffle(vecR_ordered, maskR1));
                        Vector256<byte> out2_5 = Avx2.Or(Avx2.Or(Avx2.Shuffle(vecB_ordered, maskB2), Avx2.Shuffle(vecG_ordered, maskG2)), Avx2.Shuffle(vecR_ordered, maskR2));
                        Vector256<byte> out3_6 = Avx2.Or(Avx2.Or(Avx2.Shuffle(vecB_ordered, maskB3), Avx2.Shuffle(vecG_ordered, maskG3)), Avx2.Shuffle(vecR_ordered, maskR3));

                        // Extract 128-bit chunks and route them to their proper memory offsets
                        Sse2.Store((byte*)pImg8 + pixidx, out1_4.GetLower());
                        Sse2.Store((byte*)pImg8 + pixidx + 48, out1_4.GetUpper());

                        Sse2.Store((byte*)pImg8 + pixidx + 16, out2_5.GetLower());
                        Sse2.Store((byte*)pImg8 + pixidx + 64, out2_5.GetUpper());

                        Sse2.Store((byte*)pImg8 + pixidx + 32, out3_6.GetLower());
                        Sse2.Store((byte*)pImg8 + pixidx + 80, out3_6.GetUpper());

                        x += 32 - shift;
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
        internal static unsafe int YGray2PixelMapVector256(short* pY, sbyte* pImg8, int height, int width, int srcStride, int rowSizeInBytes, int startX)
        {
            if (Avx2.IsSupported)
            {
                if (width - startX < 32) return startX;

                int vectorBound = width - 32;
                int tailShift = (32 - ((width - startX) % 32)) % 32;

                Vector256<short> v32 = Vector256.Create((short)32);
                Vector256<sbyte> v127 = Vector256.Create((sbyte)127);

                Vector256<byte> mask1 = Vector256.Create((ReadOnlySpan<byte>)[
                    0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5,
                    5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10]);

                Vector256<byte> mask2 = Vector256.Create((ReadOnlySpan<byte>)[
                    10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15,
                    0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5]);

                Vector256<byte> mask3 = Vector256.Create((ReadOnlySpan<byte>)[
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
                        Vector256<short> y1 = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.LoadVector256(pY + pidx + j), v32), 6);
                        Vector256<short> y2 = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.LoadVector256(pY + pidx + j + 16), v32), 6);

                        Vector256<sbyte> narrowed = Avx2.PackSignedSaturate(y1, y2);
                        narrowed = Avx2.Permute4x64(narrowed.AsInt64(), 0xD8).AsSByte();
                        Vector256<sbyte> gray = Avx2.Subtract(v127, narrowed);

                        Vector256<sbyte> grayLow = Avx2.Permute2x128(gray.AsInt64(), gray.AsInt64(), 0x00).AsSByte();
                        Vector256<sbyte> grayHigh = Avx2.Permute2x128(gray.AsInt64(), gray.AsInt64(), 0x11).AsSByte();

                        Vector256<byte> out1 = Avx2.Shuffle(grayLow.AsByte(), mask1);
                        Vector256<byte> out2 = Avx2.Shuffle(gray.AsByte(), mask2);
                        Vector256<byte> out3 = Avx2.Shuffle(grayHigh.AsByte(), mask3);

                        Avx2.Store((byte*)pImg8 + pixidx, out1);
                        Avx2.Store((byte*)pImg8 + pixidx + 32, out2);
                        Avx2.Store((byte*)pImg8 + pixidx + 64, out3);

                        x += 32 - shift;
                    }
                }
                return width;
            }
            return 0;
        }
    }
}
