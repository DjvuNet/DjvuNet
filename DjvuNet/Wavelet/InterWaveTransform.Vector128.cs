using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
using DjvuNet.Graphics;
using System.Threading.Tasks;
using DjvuNet.Errors;

namespace DjvuNet.Wavelet
{
    public static partial class InterWaveSimd
    {
        /// <summary>
        /// Vector128 (SSSE3 / ARM64 NEON) implementation of the deinterlace phase of the RGB to YCbCr (Pigeon) transform.
        /// Deinterlace phase is tested independently from other processing phases for both correctness and performance
        /// to eliminate any bottlenecks and ensure optimal codegen. Deinterlace implementation will rearrange any three
        /// interlaced vectors of the interlaced types into three single type data vectors so obviously its not restricted to BGR format.
        /// Further opimizations are possible based on the analysis of jit disasm data but were not attempted yet.
        /// This phase is not directly comparable to the Vector256 phase as resulting data need one more stage of processing
        /// before they can be used by Vector256 transform stage.
        /// </summary>
        internal static unsafe void DeinterlaceBgrVector128(
            Pixel* inputPointer,
            out Vector128<byte> blue, out Vector128<byte> green, out Vector128<byte> red)
        {
            byte* bytePointer = (byte*)inputPointer;
            var vec0 = Vector128.Load(bytePointer);
            var vec1 = Vector128.Load(bytePointer + 16);
            var vec2 = Vector128.Load(bytePointer + 32);

            if (AdvSimd.Arm64.IsSupported)
            {
                var blueMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                var blueMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 2, 5, 8, 11, 14, 255, 255, 255, 255, 255);
                var blueMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 1, 4, 7, 10, 13);

                var greenMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                var greenMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 0, 3, 6, 9, 12, 15, 255, 255, 255, 255, 255);
                var greenMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 2, 5, 8, 11, 14);

                var redMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                var redMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 1, 4, 7, 10, 13, 255, 255, 255, 255, 255, 255);
                var redMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 3, 6, 9, 12, 15);

                blue = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec0, blueMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec1, blueMask1), AdvSimd.Arm64.VectorTableLookup(vec2, blueMask2)));
                green = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec0, greenMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec1, greenMask1), AdvSimd.Arm64.VectorTableLookup(vec2, greenMask2)));
                red = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec0, redMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec1, redMask1), AdvSimd.Arm64.VectorTableLookup(vec2, redMask2)));
            }
            else if (Ssse3.IsSupported)
            {
                var blueMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                var blueMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14, 128, 128, 128, 128, 128);
                var blueMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 1, 4, 7, 10, 13);

                var greenMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                var greenMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128);
                var greenMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14);

                var redMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                var redMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128);
                var redMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15);

                blue = Sse2.Or(Ssse3.Shuffle(vec0, blueMask0), Sse2.Or(Ssse3.Shuffle(vec1, blueMask1), Ssse3.Shuffle(vec2, blueMask2)));
                green = Sse2.Or(Ssse3.Shuffle(vec0, greenMask0), Sse2.Or(Ssse3.Shuffle(vec1, greenMask1), Ssse3.Shuffle(vec2, greenMask2)));
                red = Sse2.Or(Ssse3.Shuffle(vec0, redMask0), Sse2.Or(Ssse3.Shuffle(vec1, redMask1), Ssse3.Shuffle(vec2, redMask2)));
            }
            else
            {
                throw new PlatformNotSupportedException("Neither SSSE3 nor ARM64 NEON is supported on this platform.");
            }
        }

        /// <summary>
        /// Vector128 (SSSE3 / ARM64 NEON) implementation of the deinterlace phase of the RGB to YCbCr (Pigeon) transform
        /// processing the whole image in single pass to simulate real world operation. Deinterlace phase is tested
        /// independently from other processing phases for both correctness and performance to eliminate any bottlenecks
        /// and ensure optimal codegen. Deinterlace implementation will rearrange any three interlaced vectors of the interlaced
        /// types into three single type data vectors so obviously its not restricted to BGR format.
        /// Further opimizations are possible based on the analysis of jit disasm data but were not attempted yet.
        /// This phase is not directly comparable to the Vector256 phase as resulting data need one more stage of processing
        /// before they can be used by Vector256 transform stage.
        /// </summary>
        internal static unsafe void DeinterlaceBgrVector128FullImage(
            Pixel* inputPointer, int width, int height, int rowSizeInBytes,
            out Vector128<byte> blue, out Vector128<byte> green, out Vector128<byte> red)
        {
            blue = Vector128<byte>.Zero; green = Vector128<byte>.Zero; red = Vector128<byte>.Zero;

            for (int y = 0; y < height; y++)
            {
                Pixel* pIn = (Pixel*)((byte*)inputPointer + ((long)y * rowSizeInBytes));
                int x = 0;
                while (x <= width - 16)
                {
                    byte* bytePointer = (byte*)pIn;
                    var vec0 = Vector128.Load(bytePointer);
                    var vec1 = Vector128.Load(bytePointer + 16);
                    var vec2 = Vector128.Load(bytePointer + 32);

                    if (AdvSimd.Arm64.IsSupported)
                    {
                        var blueMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var blueMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 2, 5, 8, 11, 14, 255, 255, 255, 255, 255);
                        var blueMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 1, 4, 7, 10, 13);

                        var greenMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var greenMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 0, 3, 6, 9, 12, 15, 255, 255, 255, 255, 255);
                        var greenMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 2, 5, 8, 11, 14);

                        var redMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var redMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 1, 4, 7, 10, 13, 255, 255, 255, 255, 255, 255);
                        var redMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 3, 6, 9, 12, 15);

                        blue = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec0, blueMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec1, blueMask1), AdvSimd.Arm64.VectorTableLookup(vec2, blueMask2)));
                        green = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec0, greenMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec1, greenMask1), AdvSimd.Arm64.VectorTableLookup(vec2, greenMask2)));
                        red = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec0, redMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec1, redMask1), AdvSimd.Arm64.VectorTableLookup(vec2, redMask2)));
                    }
                    else if (Ssse3.IsSupported)
                    {
                        var blueMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var blueMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14, 128, 128, 128, 128, 128);
                        var blueMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 1, 4, 7, 10, 13);

                        var greenMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var greenMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128);
                        var greenMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14);

                        var redMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var redMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128);
                        var redMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15);

                        blue = Sse2.Or(Ssse3.Shuffle(vec0, blueMask0), Sse2.Or(Ssse3.Shuffle(vec1, blueMask1), Ssse3.Shuffle(vec2, blueMask2)));
                        green = Sse2.Or(Ssse3.Shuffle(vec0, greenMask0), Sse2.Or(Ssse3.Shuffle(vec1, greenMask1), Ssse3.Shuffle(vec2, greenMask2)));
                        red = Sse2.Or(Ssse3.Shuffle(vec0, redMask0), Sse2.Or(Ssse3.Shuffle(vec1, redMask1), Ssse3.Shuffle(vec2, redMask2)));
                    }
                    else
                    {
                        throw new PlatformNotSupportedException("Neither SSSE3 nor ARM64 NEON is supported on this platform.");
                    }

                    pIn += 16;
                    x += 16;
                }
            }
        }

        /// <summary>
        /// Vector128 (SSSE3 / ARM64 NEON) implementation of the interlace phase of the RGB to YCbCr (Pigeon) transform.
        /// Interlace phase is tested independently for both correctness and performance to eliminate any bottlenecks
        /// and ensure optimal codegen. Interlace implementation will rearrange any three single type data vectors
        /// into three interlaced vectors of the interlaced types so obviously its not restricted to BGR format.
        /// Further opimizations are possible based on the analysis of jit disasm data but were not attempted yet.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void InterlaceBgrVector128(
            Vector128<byte> blue, Vector128<byte> green, Vector128<byte> red,
            out Vector128<byte> vec0, out Vector128<byte> vec1, out Vector128<byte> vec2)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                var blueMask0 = Vector128.Create((byte)0, 255, 255, 1, 255, 255, 2, 255, 255, 3, 255, 255, 4, 255, 255, 5);
                var greenMask0 = Vector128.Create((byte)255, 0, 255, 255, 1, 255, 255, 2, 255, 255, 3, 255, 255, 4, 255, 255);
                var redMask0 = Vector128.Create((byte)255, 255, 0, 255, 255, 1, 255, 255, 2, 255, 255, 3, 255, 255, 4, 255);

                var blueMask1 = Vector128.Create((byte)255, 255, 6, 255, 255, 7, 255, 255, 8, 255, 255, 9, 255, 255, 10, 255);
                var greenMask1 = Vector128.Create((byte)5, 255, 255, 6, 255, 255, 7, 255, 255, 8, 255, 255, 9, 255, 255, 10);
                var redMask1 = Vector128.Create((byte)255, 5, 255, 255, 6, 255, 255, 7, 255, 255, 8, 255, 255, 9, 255, 255);

                var blueMask2 = Vector128.Create((byte)255, 11, 255, 255, 12, 255, 255, 13, 255, 255, 14, 255, 255, 15, 255, 255);
                var greenMask2 = Vector128.Create((byte)255, 255, 11, 255, 255, 12, 255, 255, 13, 255, 255, 14, 255, 255, 15, 255);
                var redMask2 = Vector128.Create((byte)10, 255, 255, 11, 255, 255, 12, 255, 255, 13, 255, 255, 14, 255, 255, 15);

                vec0 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(blue, blueMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(green, greenMask0), AdvSimd.Arm64.VectorTableLookup(red, redMask0)));
                vec1 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(blue, blueMask1), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(green, greenMask1), AdvSimd.Arm64.VectorTableLookup(red, redMask1)));
                vec2 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(blue, blueMask2), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(green, greenMask2), AdvSimd.Arm64.VectorTableLookup(red, redMask2)));
            }
            else if (Ssse3.IsSupported)
            {
                var blueMask0 = Vector128.Create((byte)0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128, 5);
                var greenMask0 = Vector128.Create((byte)128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128);
                var redMask0 = Vector128.Create((byte)128, 128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128);

                var blueMask1 = Vector128.Create((byte)128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10, 128);
                var greenMask1 = Vector128.Create((byte)5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10);
                var redMask1 = Vector128.Create((byte)128, 5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128);

                var blueMask2 = Vector128.Create((byte)128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128, 128);
                var greenMask2 = Vector128.Create((byte)128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128);
                var redMask2 = Vector128.Create((byte)10, 128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15);

                vec0 = Sse2.Or(Ssse3.Shuffle(blue, blueMask0), Sse2.Or(Ssse3.Shuffle(green, greenMask0), Ssse3.Shuffle(red, redMask0)));
                vec1 = Sse2.Or(Ssse3.Shuffle(blue, blueMask1), Sse2.Or(Ssse3.Shuffle(green, greenMask1), Ssse3.Shuffle(red, redMask1)));
                vec2 = Sse2.Or(Ssse3.Shuffle(blue, blueMask2), Sse2.Or(Ssse3.Shuffle(green, greenMask2), Ssse3.Shuffle(red, redMask2)));
            }
            else
            {
                throw new PlatformNotSupportedException("Neither SSSE3 nor ARM64 NEON is supported on this platform.");
            }
        }

        /// <summary>
        /// Vector128 (SSSE3 / ARM64 NEON) implementation of the YCbCr (Pigeon) to RGB transform
        /// performed on deinterlaced pixel data and processing 16 pixels in single pass. Used to test correctness
        /// and performance of this phase independently from the deinterlace and interlace stages.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void TransformYCbCrToRgbVector128(
            Vector128<sbyte> luma, Vector128<sbyte> chromaBlue, Vector128<sbyte> chromaRed,
            out Vector128<byte> blue, out Vector128<byte> green, out Vector128<byte> red)
        {
            if (!Vector128.IsHardwareAccelerated)
            {
                throw new PlatformNotSupportedException("Neither SSSE3 nor ARM64 NEON is supported on this platform.");
            }

            var vec128 = Vector128.Create((short)128);
            var vecZero = Vector128<short>.Zero;
            var vec255 = Vector128.Create((short)255);

            var lumaLow = Vector128.WidenLower(luma); var lumaHigh = Vector128.WidenUpper(luma);
            var chromaBlueLow = Vector128.WidenLower(chromaBlue); var chromaBlueHigh = Vector128.WidenUpper(chromaBlue);
            var chromaRedLow = Vector128.WidenLower(chromaRed); var chromaRedHigh = Vector128.WidenUpper(chromaRed);

            var temp1Low = Vector128.ShiftRightArithmetic(chromaBlueLow, 2);
            var crShift1Low = Vector128.ShiftRightArithmetic(chromaRedLow, 1);
            var temp2Low = Vector128.Add(chromaRedLow, crShift1Low);
            var luma128Low = Vector128.Add(lumaLow, vec128);
            var temp3Low = Vector128.Subtract(luma128Low, temp1Low);

            var redLow = Vector128.Add(luma128Low, temp2Low);
            var temp2Shift1Low = Vector128.ShiftRightArithmetic(temp2Low, 1);
            var cbShift1Low = Vector128.ShiftLeft(chromaBlueLow, 1);

            var greenLow = Vector128.Subtract(temp3Low, temp2Shift1Low);
            var blueLow = Vector128.Add(temp3Low, cbShift1Low);

            redLow = Vector128.Max(vecZero, Vector128.Min(vec255, redLow));
            greenLow = Vector128.Max(vecZero, Vector128.Min(vec255, greenLow));
            blueLow = Vector128.Max(vecZero, Vector128.Min(vec255, blueLow));

            var temp1High = Vector128.ShiftRightArithmetic(chromaBlueHigh, 2);
            var crShift1High = Vector128.ShiftRightArithmetic(chromaRedHigh, 1);
            var temp2High = Vector128.Add(chromaRedHigh, crShift1High);
            var luma128High = Vector128.Add(lumaHigh, vec128);
            var temp3High = Vector128.Subtract(luma128High, temp1High);

            var redHigh = Vector128.Add(luma128High, temp2High);
            var temp2Shift1High = Vector128.ShiftRightArithmetic(temp2High, 1);
            var cbShift1High = Vector128.ShiftLeft(chromaBlueHigh, 1);

            var greenHigh = Vector128.Subtract(temp3High, temp2Shift1High);
            var blueHigh = Vector128.Add(temp3High, cbShift1High);

            redHigh = Vector128.Max(vecZero, Vector128.Min(vec255, redHigh));
            greenHigh = Vector128.Max(vecZero, Vector128.Min(vec255, greenHigh));
            blueHigh = Vector128.Max(vecZero, Vector128.Min(vec255, blueHigh));

            blue = Vector128.Narrow(blueLow.AsUInt16(), blueHigh.AsUInt16());
            green = Vector128.Narrow(greenLow.AsUInt16(), greenHigh.AsUInt16());
            red = Vector128.Narrow(redLow.AsUInt16(), redHigh.AsUInt16());
        }

        /// <summary>
        /// Single step Vector128 (SSSE3 / ARM64 NEON) implementation of the RGB to YCbCr (Pigeon) transform
        /// performed on deinterlaced pixel data and processing 16 pixels in single pass. Used to test correctness
        /// and performance of this phase independently from the deinterlace and interlace stages.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void TransformRgbToYCbCrVector128(
            Vector128<byte> blue, Vector128<byte> green, Vector128<byte> red,
            out Vector128<sbyte> luma, out Vector128<sbyte> chromaBlue, out Vector128<sbyte> chromaRed)
        {
            if (!Vector128.IsHardwareAccelerated)
            {
                throw new PlatformNotSupportedException("Neither SSSE3 nor ARM64 NEON is supported on this platform.");
            }

            var coeffYRed = Vector128.Create(19946);
            var coeffYGreen = Vector128.Create(39891);
            var coeffYBlue = Vector128.Create(5698);
            var coeffCbRed = Vector128.Create(-11397);
            var coeffCbGreen = Vector128.Create(-22795);
            var coeffCbBlue = Vector128.Create(34192);
            var coeffCrRed = Vector128.Create(30393);
            var coeffCrGreen = Vector128.Create(-26594);
            var coeffCrBlue = Vector128.Create(-3799);

            var vec32768 = Vector128.Create(32768);
            var vec128 = Vector128.Create(128);

            var blueLow16 = Vector128.WidenLower(blue); var blueHigh16 = Vector128.WidenUpper(blue);
            var greenLow16 = Vector128.WidenLower(green); var greenHigh16 = Vector128.WidenUpper(green);
            var redLow16 = Vector128.WidenLower(red); var redHigh16 = Vector128.WidenUpper(red);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            (Vector128<int> yVec, Vector128<int> cbVec, Vector128<int> crVec) ProcessFourPixels(Vector128<ushort> b16, Vector128<ushort> g16, Vector128<ushort> r16, bool isUpper)
            {
                var b32 = isUpper ? Vector128.WidenUpper(b16).AsInt32() : Vector128.WidenLower(b16).AsInt32();
                var g32 = isUpper ? Vector128.WidenUpper(g16).AsInt32() : Vector128.WidenLower(g16).AsInt32();
                var r32 = isUpper ? Vector128.WidenUpper(r16).AsInt32() : Vector128.WidenLower(r16).AsInt32();

                var y32 = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32, coeffYRed), Vector128.Multiply(g32, coeffYGreen)), Vector128.Multiply(b32, coeffYBlue)), vec32768), 16);
                var cb32 = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32, coeffCbRed), Vector128.Multiply(g32, coeffCbGreen)), Vector128.Multiply(b32, coeffCbBlue)), vec32768), 16);
                var cr32 = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32, coeffCrRed), Vector128.Multiply(g32, coeffCrGreen)), Vector128.Multiply(b32, coeffCrBlue)), vec32768), 16);

                return (Vector128.Subtract(y32, vec128), cb32, cr32);
            }

            var (lumaLowLow, chromaBlueLowLow, chromaRedLowLow) = ProcessFourPixels(blueLow16, greenLow16, redLow16, false);
            var (lumaLowHigh, chromaBlueLowHigh, chromaRedLowHigh) = ProcessFourPixels(blueLow16, greenLow16, redLow16, true);
            var (lumaHighLow, chromaBlueHighLow, chromaRedHighLow) = ProcessFourPixels(blueHigh16, greenHigh16, redHigh16, false);
            var (lumaHighHigh, chromaBlueHighHigh, chromaRedHighHigh) = ProcessFourPixels(blueHigh16, greenHigh16, redHigh16, true);

            var lumaLow16 = Vector128.Narrow(lumaLowLow, lumaLowHigh); var lumaHigh16 = Vector128.Narrow(lumaHighLow, lumaHighHigh);
            var chromaBlueLow16 = Vector128.Narrow(chromaBlueLowLow, chromaBlueLowHigh); var chromaBlueHigh16 = Vector128.Narrow(chromaBlueHighLow, chromaBlueHighHigh);
            var chromaRedLow16 = Vector128.Narrow(chromaRedLowLow, chromaRedLowHigh); var chromaRedHigh16 = Vector128.Narrow(chromaRedHighLow, chromaRedHighHigh);

            var vecMinSbyte = Vector128.Create((short)-128);
            var vecMaxSbyte = Vector128.Create((short)127);

            // TODO: Verify if explicit Vector128 clamping is strictly necessary here.
            // This redundant clamping was introduced to fix legacy test errors, but
            // Vector128.Narrow should intrinsically map to saturating hardware instructions.
            lumaLow16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, lumaLow16));
            lumaHigh16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, lumaHigh16));

            chromaBlueLow16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, chromaBlueLow16));
            chromaBlueHigh16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, chromaBlueHigh16));

            chromaRedLow16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, chromaRedLow16));
            chromaRedHigh16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, chromaRedHigh16));

            luma = Vector128.Narrow(lumaLow16, lumaHigh16);
            chromaBlue = Vector128.Narrow(chromaBlueLow16, chromaBlueHigh16);
            chromaRed = Vector128.Narrow(chromaRedLow16, chromaRedHigh16);
        }

        /// <summary>
        /// Slow testing setup for verifying correctness of the RGB to YCbCr (Pigeon) transform phases implementations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe void Rgb2YCbCrVector128Composite(Pixel* pixelBuffer, int width, int height, int rowSizeInBytes, sbyte* outY, sbyte* outCb, sbyte* outCr, int outRowSizeInBytes)
        {
            if (!AdvSimd.Arm64.IsSupported && !Ssse3.IsSupported)
            {
                throw new PlatformNotSupportedException("Neither SSSE3 nor ARM64 NEON is supported on this platform.");
            }
            else if (width < 16)
            {
                throw new ArgumentException("Image width is too small for vectorized processing.", nameof(width));
            }

            Pixel* ptrIn = pixelBuffer;
            sbyte* ptrY = outY;
            sbyte* ptrCb = outCb;
            sbyte* ptrCr = outCr;

            long inPadBytes = rowSizeInBytes - ((long)width * sizeof(Pixel));
            int outPadBytes = outRowSizeInBytes - width;

            int vectorBound = width - 16;
            int tailShift = (16 - (width % 16)) % 16;
            int tailShiftBytes = tailShift * sizeof(Pixel);

            for (int y = 0; y < height; y++)
            {
                int x = 0;

                while (x < width)
                {
                    int shift = (x > vectorBound) ? tailShift : 0;
                    int shiftBytes = (x > vectorBound) ? tailShiftBytes : 0;

                    Pixel* readPtr = (Pixel*)((byte*)ptrIn - shiftBytes);

                    DeinterlaceBgrVector128(readPtr, out var blue, out var green, out var red);
                    TransformRgbToYCbCrVector128(blue, green, red, out var lumaVec, out var chromaBlueVec, out var chromaRedVec);

                    sbyte* writeY = ptrY - shift;
                    sbyte* writeCb = ptrCb - shift;
                    sbyte* writeCr = ptrCr - shift;

                    lumaVec.Store(writeY);
                    chromaBlueVec.Store(writeCb);
                    chromaRedVec.Store(writeCr);

                    int advance = 16 - shift;
                    ptrIn += advance;
                    ptrY += advance;
                    ptrCb += advance;
                    ptrCr += advance;
                    x += advance;
                }

                ptrIn = (Pixel*)((byte*)ptrIn + inPadBytes);
                ptrY += outPadBytes;
                ptrCb += outPadBytes;
                ptrCr += outPadBytes;
            }
        }

        /// <summary>
        /// Vector128 (SSSE3 / ARM64 NEON) implementation of the RGB to YCbCr (Pigeon)transform.
        /// This implementation all stages (deinterlace, math, interlace) are teasted independently
        /// for both correctness and performance to eliminate any bottlenecks and ensure optimal codegen.
        /// It was built to establish the highest possible performance ceiling for 128-bit wide registers.
        /// The unified router may select this implementation based on hardware support.
        /// Further opimizations are possible based on the analysis of jit disasm data but were not attempted yet.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe void Rgb2YCbCrVector128(Pixel* pixelBuffer, int width, int height, int rowSizeInBytes, sbyte* outY, sbyte* outCb, sbyte* outCr, int outRowSizeInBytes)
        {
            if (!AdvSimd.Arm64.IsSupported && !Ssse3.IsSupported)
            {
                throw new PlatformNotSupportedException("Neither SSSE3 nor ARM64 NEON is supported on this platform.");
            }
            else if (width < 16)
            {
                throw new ArgumentException("Image width is too small for vectorized processing.", nameof(width));
            }

            Pixel* ptrIn = pixelBuffer;
            sbyte* ptrY = outY;
            sbyte* ptrCb = outCb;
            sbyte* ptrCr = outCr;

            long inPadBytes = rowSizeInBytes - ((long)width * sizeof(Pixel));
            int outPadBytes = outRowSizeInBytes - width;

            int vectorBound = width - 16;
            int tailShift = (16 - (width % 16)) % 16;
            int tailShiftBytes = tailShift * sizeof(Pixel);

            var coeffYRed = Vector128.Create(19946);
            var coeffYGreen = Vector128.Create(39891);
            var coeffYBlue = Vector128.Create(5698);
            var coeffCbRed = Vector128.Create(-11397);
            var coeffCbGreen = Vector128.Create(-22795);
            var coeffCbBlue = Vector128.Create(34192);
            var coeffCrRed = Vector128.Create(30393);
            var coeffCrGreen = Vector128.Create(-26594);
            var coeffCrBlue = Vector128.Create(-3799);

            var vec32768 = Vector128.Create(32768);
            var vec128 = Vector128.Create(128);

            var vecMinSbyte = Vector128.Create((short)-128);
            var vecMaxSbyte = Vector128.Create((short)127);

            for (int y = 0; y < height; y++)
            {
                int x = 0;

                while (x < width)
                {
                    int shift = (x > vectorBound) ? tailShift : 0;
                    int shiftBytes = (x > vectorBound) ? tailShiftBytes : 0;

                    byte* readPtr = (byte*)ptrIn - shiftBytes;
                    var vec0 = Vector128.Load(readPtr);
                    var vec1 = Vector128.Load(readPtr + 16);
                    var vec2 = Vector128.Load(readPtr + 32);

                    Vector128<byte> blue, green, red;

                    if (AdvSimd.Arm64.IsSupported)
                    {
                        var blueMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var blueMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 2, 5, 8, 11, 14, 255, 255, 255, 255, 255);
                        var blueMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 1, 4, 7, 10, 13);

                        var greenMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var greenMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 0, 3, 6, 9, 12, 15, 255, 255, 255, 255, 255);
                        var greenMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 2, 5, 8, 11, 14);

                        var redMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var redMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 1, 4, 7, 10, 13, 255, 255, 255, 255, 255, 255);
                        var redMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 3, 6, 9, 12, 15);

                        blue = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec0, blueMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec1, blueMask1), AdvSimd.Arm64.VectorTableLookup(vec2, blueMask2)));
                        green = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec0, greenMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec1, greenMask1), AdvSimd.Arm64.VectorTableLookup(vec2, greenMask2)));
                        red = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec0, redMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec1, redMask1), AdvSimd.Arm64.VectorTableLookup(vec2, redMask2)));
                    }
                    else if (Ssse3.IsSupported)
                    {
                        var blueMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var blueMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14, 128, 128, 128, 128, 128);
                        var blueMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 1, 4, 7, 10, 13);

                        var greenMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var greenMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128);
                        var greenMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14);

                        var redMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var redMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128);
                        var redMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15);

                        blue = Sse2.Or(Ssse3.Shuffle(vec0, blueMask0), Sse2.Or(Ssse3.Shuffle(vec1, blueMask1), Ssse3.Shuffle(vec2, blueMask2)));
                        green = Sse2.Or(Ssse3.Shuffle(vec0, greenMask0), Sse2.Or(Ssse3.Shuffle(vec1, greenMask1), Ssse3.Shuffle(vec2, greenMask2)));
                        red = Sse2.Or(Ssse3.Shuffle(vec0, redMask0), Sse2.Or(Ssse3.Shuffle(vec1, redMask1), Ssse3.Shuffle(vec2, redMask2)));
                    }
                    else
                    {
                        blue = Vector128.Create((byte)0);
                        green = Vector128.Create((byte)0);
                        red = Vector128.Create((byte)0);
                    }

                    var blueLow16 = Vector128.WidenLower(blue); var blueHigh16 = Vector128.WidenUpper(blue);
                    var greenLow16 = Vector128.WidenLower(green); var greenHigh16 = Vector128.WidenUpper(green);
                    var redLow16 = Vector128.WidenLower(red); var redHigh16 = Vector128.WidenUpper(red);

                    // Low Low
                    var b32_LL = Vector128.WidenLower(blueLow16).AsInt32();
                    var g32_LL = Vector128.WidenLower(greenLow16).AsInt32();
                    var r32_LL = Vector128.WidenLower(redLow16).AsInt32();

                    var y32_LL = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_LL, coeffYRed), Vector128.Multiply(g32_LL, coeffYGreen)), Vector128.Multiply(b32_LL, coeffYBlue)), vec32768), 16);
                    var cb32_LL = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_LL, coeffCbRed), Vector128.Multiply(g32_LL, coeffCbGreen)), Vector128.Multiply(b32_LL, coeffCbBlue)), vec32768), 16);
                    var cr32_LL = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_LL, coeffCrRed), Vector128.Multiply(g32_LL, coeffCrGreen)), Vector128.Multiply(b32_LL, coeffCrBlue)), vec32768), 16);
                    y32_LL = Vector128.Subtract(y32_LL, vec128);

                    // Low High
                    var b32_LH = Vector128.WidenUpper(blueLow16).AsInt32();
                    var g32_LH = Vector128.WidenUpper(greenLow16).AsInt32();
                    var r32_LH = Vector128.WidenUpper(redLow16).AsInt32();

                    var y32_LH = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_LH, coeffYRed), Vector128.Multiply(g32_LH, coeffYGreen)), Vector128.Multiply(b32_LH, coeffYBlue)), vec32768), 16);
                    var cb32_LH = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_LH, coeffCbRed), Vector128.Multiply(g32_LH, coeffCbGreen)), Vector128.Multiply(b32_LH, coeffCbBlue)), vec32768), 16);
                    var cr32_LH = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_LH, coeffCrRed), Vector128.Multiply(g32_LH, coeffCrGreen)), Vector128.Multiply(b32_LH, coeffCrBlue)), vec32768), 16);
                    y32_LH = Vector128.Subtract(y32_LH, vec128);

                    // High Low
                    var b32_HL = Vector128.WidenLower(blueHigh16).AsInt32();
                    var g32_HL = Vector128.WidenLower(greenHigh16).AsInt32();
                    var r32_HL = Vector128.WidenLower(redHigh16).AsInt32();

                    var y32_HL = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_HL, coeffYRed), Vector128.Multiply(g32_HL, coeffYGreen)), Vector128.Multiply(b32_HL, coeffYBlue)), vec32768), 16);
                    var cb32_HL = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_HL, coeffCbRed), Vector128.Multiply(g32_HL, coeffCbGreen)), Vector128.Multiply(b32_HL, coeffCbBlue)), vec32768), 16);
                    var cr32_HL = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_HL, coeffCrRed), Vector128.Multiply(g32_HL, coeffCrGreen)), Vector128.Multiply(b32_HL, coeffCrBlue)), vec32768), 16);
                    y32_HL = Vector128.Subtract(y32_HL, vec128);

                    // High High
                    var b32_HH = Vector128.WidenUpper(blueHigh16).AsInt32();
                    var g32_HH = Vector128.WidenUpper(greenHigh16).AsInt32();
                    var r32_HH = Vector128.WidenUpper(redHigh16).AsInt32();

                    var y32_HH = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_HH, coeffYRed), Vector128.Multiply(g32_HH, coeffYGreen)), Vector128.Multiply(b32_HH, coeffYBlue)), vec32768), 16);
                    var cb32_HH = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_HH, coeffCbRed), Vector128.Multiply(g32_HH, coeffCbGreen)), Vector128.Multiply(b32_HH, coeffCbBlue)), vec32768), 16);
                    var cr32_HH = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_HH, coeffCrRed), Vector128.Multiply(g32_HH, coeffCrGreen)), Vector128.Multiply(b32_HH, coeffCrBlue)), vec32768), 16);
                    y32_HH = Vector128.Subtract(y32_HH, vec128);

                    var lumaLow16 = Vector128.Narrow(y32_LL, y32_LH); var lumaHigh16 = Vector128.Narrow(y32_HL, y32_HH);
                    var chromaBlueLow16 = Vector128.Narrow(cb32_LL, cb32_LH); var chromaBlueHigh16 = Vector128.Narrow(cb32_HL, cb32_HH);
                    var chromaRedLow16 = Vector128.Narrow(cr32_LL, cr32_LH); var chromaRedHigh16 = Vector128.Narrow(cr32_HL, cr32_HH);

                    // TODO: Verify if explicit Vector128 clamping is strictly necessary here.
                    // This redundant clamping was introduced to fix legacy test errors, but
                    // Vector128.Narrow should intrinsically map to saturating hardware instructions.
                    lumaLow16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, lumaLow16));
                    lumaHigh16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, lumaHigh16));

                    chromaBlueLow16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, chromaBlueLow16));
                    chromaBlueHigh16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, chromaBlueHigh16));

                    chromaRedLow16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, chromaRedLow16));
                    chromaRedHigh16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, chromaRedHigh16));

                    var lumaVec = Vector128.Narrow(lumaLow16, lumaHigh16);
                    var chromaBlueVec = Vector128.Narrow(chromaBlueLow16, chromaBlueHigh16);
                    var chromaRedVec = Vector128.Narrow(chromaRedLow16, chromaRedHigh16);

                    sbyte* writeY = ptrY - shift;
                    sbyte* writeCb = ptrCb - shift;
                    sbyte* writeCr = ptrCr - shift;

                    lumaVec.Store(writeY);
                    chromaBlueVec.Store(writeCb);
                    chromaRedVec.Store(writeCr);

                    int advance = 16 - shift;
                    ptrIn += advance;
                    ptrY += advance;
                    ptrCb += advance;
                    ptrCr += advance;
                    x += advance;
                }

                ptrIn = (Pixel*)((byte*)ptrIn + inPadBytes);
                ptrY += outPadBytes;
                ptrCb += outPadBytes;
                ptrCr += outPadBytes;
            }
        }

        /// <summary>
        /// Multithreaded Vector128 implementation of the RGB to YCbCr transform.
        /// Wraps the inlined 128-bit vector pipeline in a <see cref="System.Threading.Tasks.Parallel.For"/>
        /// loop. This method was benchmarked against the single-threaded implementation to calculate the
        /// breakeven thresholds for parallel execution parametrization used by the unified router.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe void Rgb2YCbCrParallelVector128(Pixel* pixelBuffer, int width, int height, int rowSizeInBytes, sbyte* outY, sbyte* outCb, sbyte* outCr, int outRowSizeInBytes, ParallelOptions options)
        {
            if (!AdvSimd.Arm64.IsSupported && !Ssse3.IsSupported)
            {
                throw new PlatformNotSupportedException("Neither SSSE3 nor ARM64 NEON is supported on this platform.");
            }
            else if (width < 16)
            {
                throw new ArgumentException("Image width is too small for vectorized processing.", nameof(width));
            }

            int vectorBound = width - 16;
            int tailShift = (16 - (width % 16)) % 16;
            int tailShiftBytes = tailShift * sizeof(Pixel);

            Parallel.For(0, height, options, y =>
            {
                long inOffset = (long)y * rowSizeInBytes;
                long outOffset = (long)y * outRowSizeInBytes;

                Pixel* ptrInRow = (Pixel*)((byte*)pixelBuffer + inOffset);
                sbyte* ptrYRow = outY + outOffset;
                sbyte* ptrCbRow = outCb + outOffset;
                sbyte* ptrCrRow = outCr + outOffset;

                var coeffYRed = Vector128.Create(19946);
                var coeffYGreen = Vector128.Create(39891);
                var coeffYBlue = Vector128.Create(5698);
                var coeffCbRed = Vector128.Create(-11397);
                var coeffCbGreen = Vector128.Create(-22795);
                var coeffCbBlue = Vector128.Create(34192);
                var coeffCrRed = Vector128.Create(30393);
                var coeffCrGreen = Vector128.Create(-26594);
                var coeffCrBlue = Vector128.Create(-3799);

                var vec32768 = Vector128.Create(32768);
                var vec128 = Vector128.Create(128);

                var vecMinSbyte = Vector128.Create((short)-128);
                var vecMaxSbyte = Vector128.Create((short)127);

                int x = 0;
                while (x < width)
                {
                    int shift = (x > vectorBound) ? tailShift : 0;
                    int shiftBytes = (x > vectorBound) ? tailShiftBytes : 0;

                    byte* readPtr = (byte*)ptrInRow - shiftBytes;

                    var vec0 = Vector128.Load(readPtr);
                    var vec1 = Vector128.Load(readPtr + 16);
                    var vec2 = Vector128.Load(readPtr + 32);

                    Vector128<byte> blue, green, red;

                    if (AdvSimd.Arm64.IsSupported)
                    {
                        var blueMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var blueMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 2, 5, 8, 11, 14, 255, 255, 255, 255, 255);
                        var blueMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 1, 4, 7, 10, 13);

                        var greenMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var greenMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 0, 3, 6, 9, 12, 15, 255, 255, 255, 255, 255);
                        var greenMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 2, 5, 8, 11, 14);

                        var redMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var redMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 1, 4, 7, 10, 13, 255, 255, 255, 255, 255, 255);
                        var redMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 3, 6, 9, 12, 15);

                        blue = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec0, blueMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec1, blueMask1), AdvSimd.Arm64.VectorTableLookup(vec2, blueMask2)));
                        green = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec0, greenMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec1, greenMask1), AdvSimd.Arm64.VectorTableLookup(vec2, greenMask2)));
                        red = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec0, redMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vec1, redMask1), AdvSimd.Arm64.VectorTableLookup(vec2, redMask2)));
                    }
                    else if (Ssse3.IsSupported)
                    {
                        var blueMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var blueMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14, 128, 128, 128, 128, 128);
                        var blueMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 1, 4, 7, 10, 13);

                        var greenMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var greenMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128);
                        var greenMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14);

                        var redMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var redMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128);
                        var redMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15);

                        blue = Sse2.Or(Ssse3.Shuffle(vec0, blueMask0), Sse2.Or(Ssse3.Shuffle(vec1, blueMask1), Ssse3.Shuffle(vec2, blueMask2)));
                        green = Sse2.Or(Ssse3.Shuffle(vec0, greenMask0), Sse2.Or(Ssse3.Shuffle(vec1, greenMask1), Ssse3.Shuffle(vec2, greenMask2)));
                        red = Sse2.Or(Ssse3.Shuffle(vec0, redMask0), Sse2.Or(Ssse3.Shuffle(vec1, redMask1), Ssse3.Shuffle(vec2, redMask2)));
                    }
                    else
                    {
                        blue = Vector128<byte>.Zero; green = Vector128<byte>.Zero; red = Vector128<byte>.Zero;
                    }

                    var blueLow16 = Vector128.WidenLower(blue); var blueHigh16 = Vector128.WidenUpper(blue);
                    var greenLow16 = Vector128.WidenLower(green); var greenHigh16 = Vector128.WidenUpper(green);
                    var redLow16 = Vector128.WidenLower(red); var redHigh16 = Vector128.WidenUpper(red);

                    // Low Low
                    var b32_LL = Vector128.WidenLower(blueLow16).AsInt32();
                    var g32_LL = Vector128.WidenLower(greenLow16).AsInt32();
                    var r32_LL = Vector128.WidenLower(redLow16).AsInt32();

                    var y32_LL = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_LL, coeffYRed), Vector128.Multiply(g32_LL, coeffYGreen)), Vector128.Multiply(b32_LL, coeffYBlue)), vec32768), 16);
                    var cb32_LL = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_LL, coeffCbRed), Vector128.Multiply(g32_LL, coeffCbGreen)), Vector128.Multiply(b32_LL, coeffCbBlue)), vec32768), 16);
                    var cr32_LL = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_LL, coeffCrRed), Vector128.Multiply(g32_LL, coeffCrGreen)), Vector128.Multiply(b32_LL, coeffCrBlue)), vec32768), 16);
                    y32_LL = Vector128.Subtract(y32_LL, vec128);

                    // Low High
                    var b32_LH = Vector128.WidenUpper(blueLow16).AsInt32();
                    var g32_LH = Vector128.WidenUpper(greenLow16).AsInt32();
                    var r32_LH = Vector128.WidenUpper(redLow16).AsInt32();

                    var y32_LH = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_LH, coeffYRed), Vector128.Multiply(g32_LH, coeffYGreen)), Vector128.Multiply(b32_LH, coeffYBlue)), vec32768), 16);
                    var cb32_LH = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_LH, coeffCbRed), Vector128.Multiply(g32_LH, coeffCbGreen)), Vector128.Multiply(b32_LH, coeffCbBlue)), vec32768), 16);
                    var cr32_LH = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_LH, coeffCrRed), Vector128.Multiply(g32_LH, coeffCrGreen)), Vector128.Multiply(b32_LH, coeffCrBlue)), vec32768), 16);
                    y32_LH = Vector128.Subtract(y32_LH, vec128);

                    // High Low
                    var b32_HL = Vector128.WidenLower(blueHigh16).AsInt32();
                    var g32_HL = Vector128.WidenLower(greenHigh16).AsInt32();
                    var r32_HL = Vector128.WidenLower(redHigh16).AsInt32();

                    var y32_HL = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_HL, coeffYRed), Vector128.Multiply(g32_HL, coeffYGreen)), Vector128.Multiply(b32_HL, coeffYBlue)), vec32768), 16);
                    var cb32_HL = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_HL, coeffCbRed), Vector128.Multiply(g32_HL, coeffCbGreen)), Vector128.Multiply(b32_HL, coeffCbBlue)), vec32768), 16);
                    var cr32_HL = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_HL, coeffCrRed), Vector128.Multiply(g32_HL, coeffCrGreen)), Vector128.Multiply(b32_HL, coeffCrBlue)), vec32768), 16);
                    y32_HL = Vector128.Subtract(y32_HL, vec128);

                    // High High
                    var b32_HH = Vector128.WidenUpper(blueHigh16).AsInt32();
                    var g32_HH = Vector128.WidenUpper(greenHigh16).AsInt32();
                    var r32_HH = Vector128.WidenUpper(redHigh16).AsInt32();

                    var y32_HH = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_HH, coeffYRed), Vector128.Multiply(g32_HH, coeffYGreen)), Vector128.Multiply(b32_HH, coeffYBlue)), vec32768), 16);
                    var cb32_HH = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_HH, coeffCbRed), Vector128.Multiply(g32_HH, coeffCbGreen)), Vector128.Multiply(b32_HH, coeffCbBlue)), vec32768), 16);
                    var cr32_HH = Vector128.ShiftRightArithmetic(Vector128.Add(Vector128.Add(Vector128.Add(Vector128.Multiply(r32_HH, coeffCrRed), Vector128.Multiply(g32_HH, coeffCrGreen)), Vector128.Multiply(b32_HH, coeffCrBlue)), vec32768), 16);
                    y32_HH = Vector128.Subtract(y32_HH, vec128);

                    var lumaLow16 = Vector128.Narrow(y32_LL, y32_LH); var lumaHigh16 = Vector128.Narrow(y32_HL, y32_HH);
                    var chromaBlueLow16 = Vector128.Narrow(cb32_LL, cb32_LH); var chromaBlueHigh16 = Vector128.Narrow(cb32_HL, cb32_HH);
                    var chromaRedLow16 = Vector128.Narrow(cr32_LL, cr32_LH); var chromaRedHigh16 = Vector128.Narrow(cr32_HL, cr32_HH);

                    // TODO: Verify if explicit Vector128 clamping is strictly necessary here.
                    // This redundant clamping was introduced to fix legacy test errors, but
                    // Vector128.Narrow should intrinsically map to saturating hardware instructions.
                    lumaLow16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, lumaLow16));
                    lumaHigh16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, lumaHigh16));

                    chromaBlueLow16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, chromaBlueLow16));
                    chromaBlueHigh16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, chromaBlueHigh16));

                    chromaRedLow16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, chromaRedLow16));
                    chromaRedHigh16 = Vector128.Max(vecMinSbyte, Vector128.Min(vecMaxSbyte, chromaRedHigh16));

                    var lumaVec = Vector128.Narrow(lumaLow16, lumaHigh16);
                    var chromaBlueVec = Vector128.Narrow(chromaBlueLow16, chromaBlueHigh16);
                    var chromaRedVec = Vector128.Narrow(chromaRedLow16, chromaRedHigh16);

                    sbyte* writeY = ptrYRow - shift;
                    sbyte* writeCb = ptrCbRow - shift;
                    sbyte* writeCr = ptrCrRow - shift;

                    lumaVec.Store(writeY);
                    chromaBlueVec.Store(writeCb);
                    chromaRedVec.Store(writeCr);

                    int advance = 16 - shift;
                    ptrInRow += advance;
                    ptrYRow += advance;
                    ptrCbRow += advance;
                    ptrCrRow += advance;
                    x += advance;
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe void YCbCr2RgbVector128Composite(Pixel* pixelBuffer, int width, int height, int rowSizeInBytes)
        {
            if (!Vector128.IsHardwareAccelerated || width < 16) return;

            Pixel* ptr = pixelBuffer;
            long inPadBytes = rowSizeInBytes - ((long)width * sizeof(Pixel));

            int vectorBound = width - 16;
            int tailShift = (16 - (width % 16)) % 16;
            int tailShiftBytes = tailShift * sizeof(Pixel);

            for (int y = 0; y < height; y++)
            {
                byte* rowBase = (byte*)ptr;
                byte* pointerByte = rowBase;

                var vec0 = Vector128.Load(pointerByte);
                var vec1 = Vector128.Load(pointerByte + 16);
                var vec2 = Vector128.Load(pointerByte + 32);

                int x = 0;
                while (x < width)
                {
                    int nextX = x + 16;
                    int nextShiftBytes = (nextX > vectorBound) ? tailShiftBytes : 0;
                    byte* nextPtr = (nextX >= width) ? rowBase : (pointerByte + 48 - nextShiftBytes);

                    var nextVec0 = Vector128.Load(nextPtr);
                    var nextVec1 = Vector128.Load(nextPtr + 16);
                    var nextVec2 = Vector128.Load(nextPtr + 32);

                    DeinterlaceBgrVector128((Pixel*)(pointerByte - ((x > vectorBound) ? tailShiftBytes : 0)), out var lumaVec, out var chromaBlueVec, out var chromaRedVec);
                    TransformYCbCrToRgbVector128(lumaVec.AsSByte(), chromaBlueVec.AsSByte(), chromaRedVec.AsSByte(), out var blueOut, out var greenOut, out var redOut);
                    InterlaceBgrVector128(blueOut, greenOut, redOut, out var out0, out var out1, out var out2);

                    int currentShiftBytes = (x > vectorBound) ? tailShiftBytes : 0;
                    byte* storePtr = pointerByte - currentShiftBytes;

                    out0.Store(storePtr);
                    out1.Store(storePtr + 16);
                    out2.Store(storePtr + 32);

                    vec0 = nextVec0;
                    vec1 = nextVec1;
                    vec2 = nextVec2;

                    pointerByte += 48;
                    x += 16;
                }

                ptr = (Pixel*)(rowBase + rowSizeInBytes);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe void YCbCr2RgbVector128(Pixel* pixelBuffer, int width, int height, int rowSizeInBytes)
        {
            if (!Ssse3.IsSupported && !AdvSimd.IsSupported)
            {
                throw new PlatformNotSupportedException("Neither SSSE3 nor ARM64 NEON is supported on this platform.");
            }
            else if (width < 16)
            {
                throw new DjvuArgumentOutOfRangeException(nameof(width), width, "Width must be at least 16 pixels for Vector128 processing.");
            }

            Pixel* p = pixelBuffer;
            var v128 = Vector128.Create((short)128);
            var vZero = Vector128<short>.Zero;
            var v255 = Vector128.Create((short)255);
            var vZero8 = Vector128<byte>.Zero;

            int vectorBound = width - 16;
            int tailShift = (16 - (width % 16)) % 16;
            int tailShiftBytes = tailShift * sizeof(Pixel);

            for (int y = 0; y < height; y++)
            {
                byte* rowBase = (byte*)p;
                byte* pByte = rowBase;

                var xmmA = Vector128.Load(pByte);
                var xmmB = Vector128.Load(pByte + 16);
                var xmmC = Vector128.Load(pByte + 32);

                int x = 0;
                while (x < width)
                {
                    int nextX = x + 16;
                    int nextShiftBytes = (nextX > vectorBound) ? tailShiftBytes : 0;
                    byte* nextPtr = (nextX >= width) ? rowBase : (pByte + 48 - nextShiftBytes);

                    var nextXmmA = Vector128.Load(nextPtr);
                    var nextXmmB = Vector128.Load(nextPtr + 16);
                    var nextXmmC = Vector128.Load(nextPtr + 32);

                    Vector128<byte> xmmYin, xmmCbin, xmmCrin;

                    if (AdvSimd.Arm64.IsSupported)
                    {
                        var bMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var bMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 2, 5, 8, 11, 14, 255, 255, 255, 255, 255);
                        var bMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 1, 4, 7, 10, 13);
                        var gMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var gMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 0, 3, 6, 9, 12, 15, 255, 255, 255, 255, 255);
                        var gMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 2, 5, 8, 11, 14);
                        var rMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var rMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 1, 4, 7, 10, 13, 255, 255, 255, 255, 255, 255);
                        var rMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 3, 6, 9, 12, 15);

                        xmmYin = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmA, bMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmB, bMask1), AdvSimd.Arm64.VectorTableLookup(xmmC, bMask2)));
                        xmmCbin = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmA, gMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmB, gMask1), AdvSimd.Arm64.VectorTableLookup(xmmC, gMask2)));
                        xmmCrin = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmA, rMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmB, rMask1), AdvSimd.Arm64.VectorTableLookup(xmmC, rMask2)));
                    }
                    else
                    {
                        var bMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var bMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14, 128, 128, 128, 128, 128);
                        var bMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 1, 4, 7, 10, 13);
                        var gMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var gMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128);
                        var gMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14);
                        var rMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var rMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128);
                        var rMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15);

                        xmmYin = Sse2.Or(Ssse3.Shuffle(xmmA, bMask0), Sse2.Or(Ssse3.Shuffle(xmmB, bMask1), Ssse3.Shuffle(xmmC, bMask2)));
                        xmmCbin = Sse2.Or(Ssse3.Shuffle(xmmA, gMask0), Sse2.Or(Ssse3.Shuffle(xmmB, gMask1), Ssse3.Shuffle(xmmC, gMask2)));
                        xmmCrin = Sse2.Or(Ssse3.Shuffle(xmmA, rMask0), Sse2.Or(Ssse3.Shuffle(xmmB, rMask1), Ssse3.Shuffle(xmmC, rMask2)));
                    }

                    var xmmYSbyte = xmmYin.AsSByte();
                    var xmmCbSbyte = xmmCbin.AsSByte();
                    var xmmCrSbyte = xmmCrin.AsSByte();

                    var xmmYL = Vector128.WidenLower(xmmYSbyte);
                    var xmmYH = Vector128.WidenUpper(xmmYSbyte);
                    var xmmCbL = Vector128.WidenLower(xmmCbSbyte);
                    var xmmCbH = Vector128.WidenUpper(xmmCbSbyte);
                    var xmmCrL = Vector128.WidenLower(xmmCrSbyte);
                    var xmmCrH = Vector128.WidenUpper(xmmCrSbyte);

                    var xmmT1L = Vector128.ShiftRightArithmetic(xmmCbL, 2);
                    var xmmCrSh1L = Vector128.ShiftRightArithmetic(xmmCrL, 1);
                    var xmmT2L = Vector128.Add(xmmCrL, xmmCrSh1L);
                    var xmmY128L = Vector128.Add(xmmYL, v128);
                    var xmmT3L = Vector128.Subtract(xmmY128L, xmmT1L);

                    var xmmRL = Vector128.Add(xmmY128L, xmmT2L);
                    var xmmT2Sh1L = Vector128.ShiftRightArithmetic(xmmT2L, 1);
                    var xmmCbSh1L = Vector128.ShiftLeft(xmmCbL, 1);

                    var xmmGL = Vector128.Subtract(xmmT3L, xmmT2Sh1L);
                    var xmmBL = Vector128.Add(xmmT3L, xmmCbSh1L);

                    xmmRL = Vector128.Max(vZero, Vector128.Min(v255, xmmRL));
                    xmmGL = Vector128.Max(vZero, Vector128.Min(v255, xmmGL));
                    xmmBL = Vector128.Max(vZero, Vector128.Min(v255, xmmBL));

                    var xmmT1H = Vector128.ShiftRightArithmetic(xmmCbH, 2);
                    var xmmCrSh1H = Vector128.ShiftRightArithmetic(xmmCrH, 1);
                    var xmmT2H = Vector128.Add(xmmCrH, xmmCrSh1H);
                    var xmmY128H = Vector128.Add(xmmYH, v128);
                    var xmmT3H = Vector128.Subtract(xmmY128H, xmmT1H);

                    var xmmRH = Vector128.Add(xmmY128H, xmmT2H);
                    var xmmT2Sh1H = Vector128.ShiftRightArithmetic(xmmT2H, 1);
                    var xmmCbSh1H = Vector128.ShiftLeft(xmmCbH, 1);

                    var xmmGH = Vector128.Subtract(xmmT3H, xmmT2Sh1H);
                    var xmmBH = Vector128.Add(xmmT3H, xmmCbSh1H);

                    xmmRH = Vector128.Max(vZero, Vector128.Min(v255, xmmRH));
                    xmmGH = Vector128.Max(vZero, Vector128.Min(v255, xmmGH));
                    xmmBH = Vector128.Max(vZero, Vector128.Min(v255, xmmBH));

                    var xmmBOut = Vector128.Narrow(xmmBL.AsUInt16(), xmmBH.AsUInt16());
                    var xmmGOut = Vector128.Narrow(xmmGL.AsUInt16(), xmmGH.AsUInt16());
                    var xmmROut = Vector128.Narrow(xmmRL.AsUInt16(), xmmRH.AsUInt16());

                    Vector128<byte> xmmOut0, xmmOut1, xmmOut2;
                    if (AdvSimd.Arm64.IsSupported)
                    {
                        var bMask0 = Vector128.Create((byte)0, 255, 255, 1, 255, 255, 2, 255, 255, 3, 255, 255, 4, 255, 255, 5);
                        var gMask0 = Vector128.Create((byte)255, 0, 255, 255, 1, 255, 255, 2, 255, 255, 3, 255, 255, 4, 255, 255);
                        var rMask0 = Vector128.Create((byte)255, 255, 0, 255, 255, 1, 255, 255, 2, 255, 255, 3, 255, 255, 4, 255);
                        var bMask1 = Vector128.Create((byte)255, 255, 6, 255, 255, 7, 255, 255, 8, 255, 255, 9, 255, 255, 10, 255);
                        var gMask1 = Vector128.Create((byte)5, 255, 255, 6, 255, 255, 7, 255, 255, 8, 255, 255, 9, 255, 255, 10);
                        var rMask1 = Vector128.Create((byte)255, 5, 255, 255, 6, 255, 255, 7, 255, 255, 8, 255, 255, 9, 255, 255);
                        var bMask2 = Vector128.Create((byte)255, 11, 255, 255, 12, 255, 255, 13, 255, 255, 14, 255, 255, 15, 255, 255);
                        var gMask2 = Vector128.Create((byte)255, 255, 11, 255, 255, 12, 255, 255, 13, 255, 255, 14, 255, 255, 15, 255);
                        var rMask2 = Vector128.Create((byte)10, 255, 255, 11, 255, 255, 12, 255, 255, 13, 255, 255, 14, 255, 255, 15);

                        xmmOut0 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmBOut, bMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmGOut, gMask0), AdvSimd.Arm64.VectorTableLookup(xmmROut, rMask0)));
                        xmmOut1 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmBOut, bMask1), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmGOut, gMask1), AdvSimd.Arm64.VectorTableLookup(xmmROut, rMask1)));
                        xmmOut2 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmBOut, bMask2), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmGOut, gMask2), AdvSimd.Arm64.VectorTableLookup(xmmROut, rMask2)));
                    }
                    else
                    {
                        var bMask0 = Vector128.Create((byte)0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128, 5);
                        var gMask0 = Vector128.Create((byte)128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128);
                        var rMask0 = Vector128.Create((byte)128, 128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128);
                        var bMask1 = Vector128.Create((byte)128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10, 128);
                        var gMask1 = Vector128.Create((byte)5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10);
                        var rMask1 = Vector128.Create((byte)128, 5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128);
                        var bMask2 = Vector128.Create((byte)128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128, 128);
                        var gMask2 = Vector128.Create((byte)128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128);
                        var rMask2 = Vector128.Create((byte)10, 128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15);

                        xmmOut0 = Sse2.Or(Ssse3.Shuffle(xmmBOut, bMask0), Sse2.Or(Ssse3.Shuffle(xmmGOut, gMask0), Ssse3.Shuffle(xmmROut, rMask0)));
                        xmmOut1 = Sse2.Or(Ssse3.Shuffle(xmmBOut, bMask1), Sse2.Or(Ssse3.Shuffle(xmmGOut, gMask1), Ssse3.Shuffle(xmmROut, rMask1)));
                        xmmOut2 = Sse2.Or(Ssse3.Shuffle(xmmBOut, bMask2), Sse2.Or(Ssse3.Shuffle(xmmGOut, gMask2), Ssse3.Shuffle(xmmROut, rMask2)));
                    }

                    int currentShiftBytes = (x > vectorBound) ? tailShiftBytes : 0;
                    byte* storePtr = pByte - currentShiftBytes;

                    xmmOut0.Store(storePtr);
                    xmmOut1.Store(storePtr + 16);
                    xmmOut2.Store(storePtr + 32);

                    xmmA = nextXmmA;
                    xmmB = nextXmmB;
                    xmmC = nextXmmC;

                    pByte += 48;
                    x += 16;
                }

                p = (Pixel*)(rowBase + rowSizeInBytes);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe void YCbCr2RgbParallelVector128(Pixel* pixelBuffer, int width, int height, int rowSizeInBytes, ParallelOptions options)
        {
            if (!Ssse3.IsSupported && !AdvSimd.IsSupported)
            {
                throw new PlatformNotSupportedException("Neither SSSE3 nor ARM64 NEON is supported on this platform.");
            }
            else if (width < 16)
            {
                throw new DjvuArgumentOutOfRangeException(nameof(width), width, "Width must be at least 16 pixels for Vector128 processing.");
            }

            int vectorBound = width - 16;
            int tailShift = (16 - (width % 16)) % 16;
            int tailShiftBytes = tailShift * sizeof(Pixel);

            Parallel.For(0, height, options, y =>
            {
                var v128 = Vector128.Create((short)128);
                var vZero = Vector128<short>.Zero;
                var v255 = Vector128.Create((short)255);
                var vZero8 = Vector128<byte>.Zero;

                byte* rowBase = (byte*)pixelBuffer + ((long)y * rowSizeInBytes);
                byte* pByte = rowBase;

                var xmmA = Vector128.Load(pByte);
                var xmmB = Vector128.Load(pByte + 16);
                var xmmC = Vector128.Load(pByte + 32);

                int x = 0;
                while (x < width)
                {
                    int nextX = x + 16;
                    int nextShiftBytes = (nextX > vectorBound) ? tailShiftBytes : 0;
                    byte* nextPtr = (nextX >= width) ? rowBase : (pByte + 48 - nextShiftBytes);

                    var nextXmmA = Vector128.Load(nextPtr);
                    var nextXmmB = Vector128.Load(nextPtr + 16);
                    var nextXmmC = Vector128.Load(nextPtr + 32);

                    Vector128<byte> xmmYin, xmmCbin, xmmCrin;

                    if (AdvSimd.Arm64.IsSupported)
                    {
                        var bMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var bMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 2, 5, 8, 11, 14, 255, 255, 255, 255, 255);
                        var bMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 1, 4, 7, 10, 13);
                        var gMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var gMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 0, 3, 6, 9, 12, 15, 255, 255, 255, 255, 255);
                        var gMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 2, 5, 8, 11, 14);
                        var rMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255);
                        var rMask1 = Vector128.Create((byte)255, 255, 255, 255, 255, 1, 4, 7, 10, 13, 255, 255, 255, 255, 255, 255);
                        var rMask2 = Vector128.Create((byte)255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 3, 6, 9, 12, 15);

                        xmmYin = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmA, bMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmB, bMask1), AdvSimd.Arm64.VectorTableLookup(xmmC, bMask2)));
                        xmmCbin = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmA, gMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmB, gMask1), AdvSimd.Arm64.VectorTableLookup(xmmC, gMask2)));
                        xmmCrin = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmA, rMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmB, rMask1), AdvSimd.Arm64.VectorTableLookup(xmmC, rMask2)));
                    }
                    else
                    {
                        var bMask0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var bMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14, 128, 128, 128, 128, 128);
                        var bMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 1, 4, 7, 10, 13);
                        var gMask0 = Vector128.Create((byte)1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var gMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15, 128, 128, 128, 128, 128);
                        var gMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 2, 5, 8, 11, 14);
                        var rMask0 = Vector128.Create((byte)2, 5, 8, 11, 14, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128);
                        var rMask1 = Vector128.Create((byte)128, 128, 128, 128, 128, 1, 4, 7, 10, 13, 128, 128, 128, 128, 128, 128);
                        var rMask2 = Vector128.Create((byte)128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 0, 3, 6, 9, 12, 15);

                        xmmYin = Sse2.Or(Ssse3.Shuffle(xmmA, bMask0), Sse2.Or(Ssse3.Shuffle(xmmB, bMask1), Ssse3.Shuffle(xmmC, bMask2)));
                        xmmCbin = Sse2.Or(Ssse3.Shuffle(xmmA, gMask0), Sse2.Or(Ssse3.Shuffle(xmmB, gMask1), Ssse3.Shuffle(xmmC, gMask2)));
                        xmmCrin = Sse2.Or(Ssse3.Shuffle(xmmA, rMask0), Sse2.Or(Ssse3.Shuffle(xmmB, rMask1), Ssse3.Shuffle(xmmC, rMask2)));
                    }

                    var xmmYSbyte = xmmYin.AsSByte();
                    var xmmCbSbyte = xmmCbin.AsSByte();
                    var xmmCrSbyte = xmmCrin.AsSByte();

                    var xmmYL = Vector128.WidenLower(xmmYSbyte);
                    var xmmYH = Vector128.WidenUpper(xmmYSbyte);
                    var xmmCbL = Vector128.WidenLower(xmmCbSbyte);
                    var xmmCbH = Vector128.WidenUpper(xmmCbSbyte);
                    var xmmCrL = Vector128.WidenLower(xmmCrSbyte);
                    var xmmCrH = Vector128.WidenUpper(xmmCrSbyte);

                    var xmmT1L = Vector128.ShiftRightArithmetic(xmmCbL, 2);
                    var xmmCrSh1L = Vector128.ShiftRightArithmetic(xmmCrL, 1);
                    var xmmT2L = Vector128.Add(xmmCrL, xmmCrSh1L);
                    var xmmY128L = Vector128.Add(xmmYL, v128);
                    var xmmT3L = Vector128.Subtract(xmmY128L, xmmT1L);

                    var xmmRL = Vector128.Add(xmmY128L, xmmT2L);
                    var xmmT2Sh1L = Vector128.ShiftRightArithmetic(xmmT2L, 1);
                    var xmmCbSh1L = Vector128.ShiftLeft(xmmCbL, 1);

                    var xmmGL = Vector128.Subtract(xmmT3L, xmmT2Sh1L);
                    var xmmBL = Vector128.Add(xmmT3L, xmmCbSh1L);

                    xmmRL = Vector128.Max(vZero, Vector128.Min(v255, xmmRL));
                    xmmGL = Vector128.Max(vZero, Vector128.Min(v255, xmmGL));
                    xmmBL = Vector128.Max(vZero, Vector128.Min(v255, xmmBL));

                    var xmmT1H = Vector128.ShiftRightArithmetic(xmmCbH, 2);
                    var xmmCrSh1H = Vector128.ShiftRightArithmetic(xmmCrH, 1);
                    var xmmT2H = Vector128.Add(xmmCrH, xmmCrSh1H);
                    var xmmY128H = Vector128.Add(xmmYH, v128);
                    var xmmT3H = Vector128.Subtract(xmmY128H, xmmT1H);

                    var xmmRH = Vector128.Add(xmmY128H, xmmT2H);
                    var xmmT2Sh1H = Vector128.ShiftRightArithmetic(xmmT2H, 1);
                    var xmmCbSh1H = Vector128.ShiftLeft(xmmCbH, 1);

                    var xmmGH = Vector128.Subtract(xmmT3H, xmmT2Sh1H);
                    var xmmBH = Vector128.Add(xmmT3H, xmmCbSh1H);

                    xmmRH = Vector128.Max(vZero, Vector128.Min(v255, xmmRH));
                    xmmGH = Vector128.Max(vZero, Vector128.Min(v255, xmmGH));
                    xmmBH = Vector128.Max(vZero, Vector128.Min(v255, xmmBH));

                    var xmmBOut = Vector128.Narrow(xmmBL.AsUInt16(), xmmBH.AsUInt16());
                    var xmmGOut = Vector128.Narrow(xmmGL.AsUInt16(), xmmGH.AsUInt16());
                    var xmmROut = Vector128.Narrow(xmmRL.AsUInt16(), xmmRH.AsUInt16());

                    Vector128<byte> xmmOut0, xmmOut1, xmmOut2;
                    if (AdvSimd.Arm64.IsSupported)
                    {
                        var bMask0 = Vector128.Create((byte)0, 255, 255, 1, 255, 255, 2, 255, 255, 3, 255, 255, 4, 255, 255, 5);
                        var gMask0 = Vector128.Create((byte)255, 0, 255, 255, 1, 255, 255, 2, 255, 255, 3, 255, 255, 4, 255, 255);
                        var rMask0 = Vector128.Create((byte)255, 255, 0, 255, 255, 1, 255, 255, 2, 255, 255, 3, 255, 255, 4, 255);
                        var bMask1 = Vector128.Create((byte)255, 255, 6, 255, 255, 7, 255, 255, 8, 255, 255, 9, 255, 255, 10, 255);
                        var gMask1 = Vector128.Create((byte)5, 255, 255, 6, 255, 255, 7, 255, 255, 8, 255, 255, 9, 255, 255, 10);
                        var rMask1 = Vector128.Create((byte)255, 5, 255, 255, 6, 255, 255, 7, 255, 255, 8, 255, 255, 9, 255, 255);
                        var bMask2 = Vector128.Create((byte)255, 11, 255, 255, 12, 255, 255, 13, 255, 255, 14, 255, 255, 15, 255, 255);
                        var gMask2 = Vector128.Create((byte)255, 255, 11, 255, 255, 12, 255, 255, 13, 255, 255, 14, 255, 255, 15, 255);
                        var rMask2 = Vector128.Create((byte)10, 255, 255, 11, 255, 255, 12, 255, 255, 13, 255, 255, 14, 255, 255, 15);

                        xmmOut0 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmBOut, bMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmGOut, gMask0), AdvSimd.Arm64.VectorTableLookup(xmmROut, rMask0)));
                        xmmOut1 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmBOut, bMask1), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmGOut, gMask1), AdvSimd.Arm64.VectorTableLookup(xmmROut, rMask1)));
                        xmmOut2 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmBOut, bMask2), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmGOut, gMask2), AdvSimd.Arm64.VectorTableLookup(xmmROut, rMask2)));
                    }
                    else
                    {
                        var bMask0 = Vector128.Create((byte)0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128, 5);
                        var gMask0 = Vector128.Create((byte)128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128);
                        var rMask0 = Vector128.Create((byte)128, 128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128);
                        var bMask1 = Vector128.Create((byte)128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10, 128);
                        var gMask1 = Vector128.Create((byte)5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10);
                        var rMask1 = Vector128.Create((byte)128, 5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128);
                        var bMask2 = Vector128.Create((byte)128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128, 128);
                        var gMask2 = Vector128.Create((byte)128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128);
                        var rMask2 = Vector128.Create((byte)10, 128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15);

                        xmmOut0 = Sse2.Or(Ssse3.Shuffle(xmmBOut, bMask0), Sse2.Or(Ssse3.Shuffle(xmmGOut, gMask0), Ssse3.Shuffle(xmmROut, rMask0)));
                        xmmOut1 = Sse2.Or(Ssse3.Shuffle(xmmBOut, bMask1), Sse2.Or(Ssse3.Shuffle(xmmGOut, gMask1), Ssse3.Shuffle(xmmROut, rMask1)));
                        xmmOut2 = Sse2.Or(Ssse3.Shuffle(xmmBOut, bMask2), Sse2.Or(Ssse3.Shuffle(xmmGOut, gMask2), Ssse3.Shuffle(xmmROut, rMask2)));
                    }

                    int currentShiftBytes = (x > vectorBound) ? tailShiftBytes : 0;
                    byte* storePtr = pByte - currentShiftBytes;

                    xmmOut0.Store(storePtr);
                    xmmOut1.Store(storePtr + 16);
                    xmmOut2.Store(storePtr + 32);

                    xmmA = nextXmmA;
                    xmmB = nextXmmB;
                    xmmC = nextXmmC;

                    pByte += 48;
                    x += 16;
                }
            });
        }

        /// <summary>
        /// Vector128 (SSSE3) implementation of the fused Grayscale to PixelMap pipeline.
        /// Reads 16-bit spatial Y channel data, bounds it, converts to grayscale (127 - Y), 
        /// and natively interleaves it into a 24bpp BGR (PixelMap) memory layout.
        /// Returns the number of pixels successfully processed per row (width16).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int YGray2PixelMapVector128(short* pY, sbyte* pImg8, int height, int width, int srcStride, int rowSizeInBytes, int startX = 0)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                if (width - startX < 16) return startX;

                int vectorBound = width - 16;
                int tailShift = (16 - ((width - startX) % 16)) % 16;

                Vector128<short> v32 = Vector128.Create((short)32);
                Vector128<sbyte> v127 = Vector128.Create((sbyte)127);

                Vector128<byte> mask1 = Vector128.Create((ReadOnlySpan<byte>)[0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5]);
                Vector128<byte> mask2 = Vector128.Create((ReadOnlySpan<byte>)[5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10]);
                Vector128<byte> mask3 = Vector128.Create((ReadOnlySpan<byte>)[10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15]);

                for (int y = 0, pidx = 0, ridx = 0; y < height; y++, pidx += srcStride, ridx += rowSizeInBytes)
                {
                    int x = startX;

                    while (x < width)
                    {
                        int shift = (x > vectorBound) ? tailShift : 0;
                        int j = x - shift;
                        int pixidx = ridx + (j * 3);
                        Vector128<short> y1 = AdvSimd.ShiftRightArithmetic(AdvSimd.Add(AdvSimd.LoadVector128(pY + pidx + j), v32), 6);
                        Vector128<short> y2 = AdvSimd.ShiftRightArithmetic(AdvSimd.Add(AdvSimd.LoadVector128(pY + pidx + j + 8), v32), 6);

                        Vector128<sbyte> narrowedGray = AdvSimd.ExtractNarrowingSaturateUpper(AdvSimd.ExtractNarrowingSaturateLower(y1), y2);
                        Vector128<sbyte> gray = AdvSimd.Subtract(v127, narrowedGray);

                        AdvSimd.Store((byte*)pImg8 + pixidx, AdvSimd.Arm64.VectorTableLookup(gray.AsByte(), mask1));
                        AdvSimd.Store((byte*)pImg8 + pixidx + 16, AdvSimd.Arm64.VectorTableLookup(gray.AsByte(), mask2));
                        AdvSimd.Store((byte*)pImg8 + pixidx + 32, AdvSimd.Arm64.VectorTableLookup(gray.AsByte(), mask3));

                        x += 16 - shift;
                    }
                }

                return width;
            }
            else if (Ssse3.IsSupported)
            {
                if (width - startX < 16) return startX;

                int vectorBound = width - 16;
                int tailShift = (16 - ((width - startX) % 16)) % 16;

                Vector128<short> v32 = Vector128.Create((short)32);
                Vector128<sbyte> v127 = Vector128.Create((sbyte)127);

                Vector128<sbyte> mask1 = Vector128.Create((ReadOnlySpan<sbyte>)[0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5]);
                Vector128<sbyte> mask2 = Vector128.Create((ReadOnlySpan<sbyte>)[5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10]);
                Vector128<sbyte> mask3 = Vector128.Create((ReadOnlySpan<sbyte>)[10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15]);

                for (int y = 0, pidx = 0, ridx = 0; y < height; y++, pidx += srcStride, ridx += rowSizeInBytes)
                {
                    int x = startX;

                    while (x < width)
                    {
                        int shift = (x > vectorBound) ? tailShift : 0;
                        int j = x - shift;
                        int pixidx = ridx + (j * 3);
                        Vector128<short> y1 = Sse2.ShiftRightArithmetic(Sse2.Add(Sse2.LoadVector128(pY + pidx + j), v32), 6);
                        Vector128<short> y2 = Sse2.ShiftRightArithmetic(Sse2.Add(Sse2.LoadVector128(pY + pidx + j + 8), v32), 6);

                        Vector128<sbyte> narrowedGray = Sse2.PackSignedSaturate(y1, y2);
                        Vector128<sbyte> gray = Sse2.Subtract(v127, narrowedGray);

                        Sse2.Store(pImg8 + pixidx, Ssse3.Shuffle(gray, mask1));
                        Sse2.Store(pImg8 + pixidx + 16, Ssse3.Shuffle(gray, mask2));
                        Sse2.Store(pImg8 + pixidx + 32, Ssse3.Shuffle(gray, mask3));

                        x += 16 - shift;
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
        internal static unsafe int YCbCr2RgbVector128(short* pY, short* pCb, short* pCr, sbyte* pImg8, int height, int width, int srcStride, int rowSizeInBytes, int startX = 0)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                if (width - startX < 16) return startX;

                int vectorBound = width - 16;
                int tailShift = (16 - ((width - startX) % 16)) % 16;

                Vector128<short> v32 = Vector128.Create((short)32);
                Vector128<short> v128 = Vector128.Create((short)128);
                Vector128<short> v127 = Vector128.Create((short)127);
                Vector128<short> vNeg128 = Vector128.Create((short)-128);

                Vector128<byte> maskB1 = Vector128.Create((ReadOnlySpan<byte>)[0, 255, 255, 1, 255, 255, 2, 255, 255, 3, 255, 255, 4, 255, 255, 5]);
                Vector128<byte> maskG1 = Vector128.Create((ReadOnlySpan<byte>)[255, 0, 255, 255, 1, 255, 255, 2, 255, 255, 3, 255, 255, 4, 255, 255]);
                Vector128<byte> maskR1 = Vector128.Create((ReadOnlySpan<byte>)[255, 255, 0, 255, 255, 1, 255, 255, 2, 255, 255, 3, 255, 255, 4, 255]);

                Vector128<byte> maskB2 = Vector128.Create((ReadOnlySpan<byte>)[255, 255, 6, 255, 255, 7, 255, 255, 8, 255, 255, 9, 255, 255, 10, 255]);
                Vector128<byte> maskG2 = Vector128.Create((ReadOnlySpan<byte>)[5, 255, 255, 6, 255, 255, 7, 255, 255, 8, 255, 255, 9, 255, 255, 10]);
                Vector128<byte> maskR2 = Vector128.Create((ReadOnlySpan<byte>)[255, 5, 255, 255, 6, 255, 255, 7, 255, 255, 8, 255, 255, 9, 255, 255]);

                Vector128<byte> maskB3 = Vector128.Create((ReadOnlySpan<byte>)[255, 11, 255, 255, 12, 255, 255, 13, 255, 255, 14, 255, 255, 15, 255, 255]);
                Vector128<byte> maskG3 = Vector128.Create((ReadOnlySpan<byte>)[255, 255, 11, 255, 255, 12, 255, 255, 13, 255, 255, 14, 255, 255, 15, 255]);
                Vector128<byte> maskR3 = Vector128.Create((ReadOnlySpan<byte>)[10, 255, 255, 11, 255, 255, 12, 255, 255, 13, 255, 255, 14, 255, 255, 15]);

                for (int y = 0, pidx = 0, ridx = 0; y < height; y++, pidx += srcStride, ridx += rowSizeInBytes)
                {
                    int x = startX;

                    while (x < width)
                    {
                        int shift = (x > vectorBound) ? tailShift : 0;
                        int j = x - shift;
                        int pixidx = ridx + (j * 3);
                        Vector128<short> y1 = AdvSimd.ShiftRightArithmetic(AdvSimd.Add(AdvSimd.LoadVector128(pY + pidx + j), v32), 6);
                        Vector128<short> cb1 = AdvSimd.ShiftRightArithmetic(AdvSimd.Add(AdvSimd.LoadVector128(pCb + pidx + j), v32), 6);
                        Vector128<short> cr1 = AdvSimd.ShiftRightArithmetic(AdvSimd.Add(AdvSimd.LoadVector128(pCr + pidx + j), v32), 6);

                        y1 = AdvSimd.Max(AdvSimd.Min(y1, v127), vNeg128);
                        cb1 = AdvSimd.Max(AdvSimd.Min(cb1, v127), vNeg128);
                        cr1 = AdvSimd.Max(AdvSimd.Min(cr1, v127), vNeg128);

                        Vector128<short> y2 = AdvSimd.ShiftRightArithmetic(AdvSimd.Add(AdvSimd.LoadVector128(pY + pidx + j + 8), v32), 6);
                        Vector128<short> cb2 = AdvSimd.ShiftRightArithmetic(AdvSimd.Add(AdvSimd.LoadVector128(pCb + pidx + j + 8), v32), 6);
                        Vector128<short> cr2 = AdvSimd.ShiftRightArithmetic(AdvSimd.Add(AdvSimd.LoadVector128(pCr + pidx + j + 8), v32), 6);

                        y2 = AdvSimd.Max(AdvSimd.Min(y2, v127), vNeg128);
                        cb2 = AdvSimd.Max(AdvSimd.Min(cb2, v127), vNeg128);
                        cr2 = AdvSimd.Max(AdvSimd.Min(cr2, v127), vNeg128);

                        // Block 1
                        Vector128<short> cbShift1 = AdvSimd.ShiftRightArithmetic(cb1, 2);
                        Vector128<short> crShift1 = AdvSimd.ShiftRightArithmetic(cr1, 1);
                        Vector128<short> crAdd1 = AdvSimd.Add(cr1, crShift1);
                        Vector128<short> yAdd1 = AdvSimd.Add(y1, v128);
                        Vector128<short> ySub1 = AdvSimd.Subtract(yAdd1, cbShift1);

                        Vector128<short> r1 = AdvSimd.Add(yAdd1, crAdd1);
                        Vector128<short> g1 = AdvSimd.Subtract(ySub1, AdvSimd.ShiftRightArithmetic(crAdd1, 1));
                        Vector128<short> b1 = AdvSimd.Add(ySub1, AdvSimd.Add(cb1, cb1));

                        // Block 2
                        Vector128<short> cbShift2 = AdvSimd.ShiftRightArithmetic(cb2, 2);
                        Vector128<short> crShift2 = AdvSimd.ShiftRightArithmetic(cr2, 1);
                        Vector128<short> crAdd2 = AdvSimd.Add(cr2, crShift2);
                        Vector128<short> yAdd2 = AdvSimd.Add(y2, v128);
                        Vector128<short> ySub2 = AdvSimd.Subtract(yAdd2, cbShift2);

                        Vector128<short> r2 = AdvSimd.Add(yAdd2, crAdd2);
                        Vector128<short> g2 = AdvSimd.Subtract(ySub2, AdvSimd.ShiftRightArithmetic(crAdd2, 1));
                        Vector128<short> b2 = AdvSimd.Add(ySub2, AdvSimd.Add(cb2, cb2));

                        Vector128<byte> vecR = AdvSimd.ExtractNarrowingSaturateUnsignedUpper(AdvSimd.ExtractNarrowingSaturateUnsignedLower(r1), r2);
                        Vector128<byte> vecG = AdvSimd.ExtractNarrowingSaturateUnsignedUpper(AdvSimd.ExtractNarrowingSaturateUnsignedLower(g1), g2);
                        Vector128<byte> vecB = AdvSimd.ExtractNarrowingSaturateUnsignedUpper(AdvSimd.ExtractNarrowingSaturateUnsignedLower(b1), b2);

                        Vector128<byte> out1 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vecB, maskB1), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vecG, maskG1), AdvSimd.Arm64.VectorTableLookup(vecR, maskR1)));
                        Vector128<byte> out2 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vecB, maskB2), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vecG, maskG2), AdvSimd.Arm64.VectorTableLookup(vecR, maskR2)));
                        Vector128<byte> out3 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vecB, maskB3), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(vecG, maskG3), AdvSimd.Arm64.VectorTableLookup(vecR, maskR3)));

                        AdvSimd.Store((byte*)pImg8 + pixidx, out1);
                        AdvSimd.Store((byte*)pImg8 + pixidx + 16, out2);
                        AdvSimd.Store((byte*)pImg8 + pixidx + 32, out3);

                        x += 16 - shift;
                    }
                }
                return width;
            }
            else if (Ssse3.IsSupported)
            {
                if (width - startX < 16) return startX;

                int vectorBound = width - 16;
                int tailShift = (16 - ((width - startX) % 16)) % 16;

                Vector128<short> v32 = Vector128.Create((short)32);
                Vector128<short> v128 = Vector128.Create((short)128);
                Vector128<short> v127 = Vector128.Create((short)127);
                Vector128<short> vNeg128 = Vector128.Create((short)-128);

                Vector128<byte> maskB1 = Vector128.Create((ReadOnlySpan<byte>)[0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128, 5]);
                Vector128<byte> maskG1 = Vector128.Create((ReadOnlySpan<byte>)[128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128, 128]);
                Vector128<byte> maskR1 = Vector128.Create((ReadOnlySpan<byte>)[128, 128, 0, 128, 128, 1, 128, 128, 2, 128, 128, 3, 128, 128, 4, 128]);

                Vector128<byte> maskB2 = Vector128.Create((ReadOnlySpan<byte>)[128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10, 128]);
                Vector128<byte> maskG2 = Vector128.Create((ReadOnlySpan<byte>)[5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128, 10]);
                Vector128<byte> maskR2 = Vector128.Create((ReadOnlySpan<byte>)[128, 5, 128, 128, 6, 128, 128, 7, 128, 128, 8, 128, 128, 9, 128, 128]);

                Vector128<byte> maskB3 = Vector128.Create((ReadOnlySpan<byte>)[128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128, 128]);
                Vector128<byte> maskG3 = Vector128.Create((ReadOnlySpan<byte>)[128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15, 128]);
                Vector128<byte> maskR3 = Vector128.Create((ReadOnlySpan<byte>)[10, 128, 128, 11, 128, 128, 12, 128, 128, 13, 128, 128, 14, 128, 128, 15]);

                for (int y = 0, pidx = 0, ridx = 0; y < height; y++, pidx += srcStride, ridx += rowSizeInBytes)
                {
                    int x = startX;

                    while (x < width)
                    {
                        int shift = (x > vectorBound) ? tailShift : 0;
                        int j = x - shift;
                        int pixidx = ridx + (j * 3);
                        Vector128<short> y1 = Sse2.ShiftRightArithmetic(Sse2.Add(Sse2.LoadVector128(pY + pidx + j), v32), 6);
                        Vector128<short> cb1 = Sse2.ShiftRightArithmetic(Sse2.Add(Sse2.LoadVector128(pCb + pidx + j), v32), 6);
                        Vector128<short> cr1 = Sse2.ShiftRightArithmetic(Sse2.Add(Sse2.LoadVector128(pCr + pidx + j), v32), 6);

                        y1 = Sse2.Max(Sse2.Min(y1, v127), vNeg128);
                        cb1 = Sse2.Max(Sse2.Min(cb1, v127), vNeg128);
                        cr1 = Sse2.Max(Sse2.Min(cr1, v127), vNeg128);

                        Vector128<short> y2 = Sse2.ShiftRightArithmetic(Sse2.Add(Sse2.LoadVector128(pY + pidx + j + 8), v32), 6);
                        Vector128<short> cb2 = Sse2.ShiftRightArithmetic(Sse2.Add(Sse2.LoadVector128(pCb + pidx + j + 8), v32), 6);
                        Vector128<short> cr2 = Sse2.ShiftRightArithmetic(Sse2.Add(Sse2.LoadVector128(pCr + pidx + j + 8), v32), 6);

                        y2 = Sse2.Max(Sse2.Min(y2, v127), vNeg128);
                        cb2 = Sse2.Max(Sse2.Min(cb2, v127), vNeg128);
                        cr2 = Sse2.Max(Sse2.Min(cr2, v127), vNeg128);

                        // Block 1
                        Vector128<short> cbShift1 = Sse2.ShiftRightArithmetic(cb1, 2);
                        Vector128<short> crShift1 = Sse2.ShiftRightArithmetic(cr1, 1);
                        Vector128<short> crAdd1 = Sse2.Add(cr1, crShift1);
                        Vector128<short> yAdd1 = Sse2.Add(y1, v128);
                        Vector128<short> ySub1 = Sse2.Subtract(yAdd1, cbShift1);

                        Vector128<short> r1 = Sse2.Add(yAdd1, crAdd1);
                        Vector128<short> g1 = Sse2.Subtract(ySub1, Sse2.ShiftRightArithmetic(crAdd1, 1));
                        Vector128<short> b1 = Sse2.Add(ySub1, Sse2.Add(cb1, cb1));

                        // Block 2
                        Vector128<short> cbShift2 = Sse2.ShiftRightArithmetic(cb2, 2);
                        Vector128<short> crShift2 = Sse2.ShiftRightArithmetic(cr2, 1);
                        Vector128<short> crAdd2 = Sse2.Add(cr2, crShift2);
                        Vector128<short> yAdd2 = Sse2.Add(y2, v128);
                        Vector128<short> ySub2 = Sse2.Subtract(yAdd2, cbShift2);

                        Vector128<short> r2 = Sse2.Add(yAdd2, crAdd2);
                        Vector128<short> g2 = Sse2.Subtract(ySub2, Sse2.ShiftRightArithmetic(crAdd2, 1));
                        Vector128<short> b2 = Sse2.Add(ySub2, Sse2.Add(cb2, cb2));

                        // Saturate to bytes
                        Vector128<byte> vecR = Sse2.PackUnsignedSaturate(r1, r2);
                        Vector128<byte> vecG = Sse2.PackUnsignedSaturate(g1, g2);
                        Vector128<byte> vecB = Sse2.PackUnsignedSaturate(b1, b2);

                        // Interleave and store
                        Vector128<byte> out1 = Sse2.Or(Sse2.Or(Ssse3.Shuffle(vecB, maskB1), Ssse3.Shuffle(vecG, maskG1)), Ssse3.Shuffle(vecR, maskR1));
                        Vector128<byte> out2 = Sse2.Or(Sse2.Or(Ssse3.Shuffle(vecB, maskB2), Ssse3.Shuffle(vecG, maskG2)), Ssse3.Shuffle(vecR, maskR2));
                        Vector128<byte> out3 = Sse2.Or(Sse2.Or(Ssse3.Shuffle(vecB, maskB3), Ssse3.Shuffle(vecG, maskG3)), Ssse3.Shuffle(vecR, maskR3));

                        Sse2.Store((byte*)pImg8 + pixidx, out1);
                        Sse2.Store((byte*)pImg8 + pixidx + 16, out2);
                        Sse2.Store((byte*)pImg8 + pixidx + 32, out3);

                        x += 16 - shift;
                    }
                }
                return width;
            }
            else
            {
                return 0;
            }
        }
    }
}