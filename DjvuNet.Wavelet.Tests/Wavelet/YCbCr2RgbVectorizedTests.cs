using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Xunit;
using DjvuNet.Graphics;
using DjvuNet.Tests;

namespace DjvuNet.Wavelet.Tests
{
    public class YCbCr2RgbVectorizedTests
    {
        public const int Seed = 42;

        [Fact]
        public void InterlaceBgrVector128_PacksCorrectly()
        {
            if (!Vector128.IsHardwareAccelerated)
            {
                Assert.Skip("Vector128 hardware acceleration is not supported on this architecture.");
            }

            byte[] bBytes = new byte[16], gBytes = new byte[16], rBytes = new byte[16];

            for (int i = 0; i < 16; i++)
            {
                // Hex encoding: High nibble = Channel (1=B, 2=G, 3=R). Low nibble = Index
                bBytes[i] = (byte)(0x10 + i);
                gBytes[i] = (byte)(0x20 + i);
                rBytes[i] = (byte)(0x30 + i);
            }

            Vector128<byte> xmmB, xmmG, xmmR;
            unsafe
            {
                fixed (byte* pb = bBytes) fixed (byte* pg = gBytes) fixed (byte* pr = rBytes)
                {
                    xmmB = Vector128.Load(pb); xmmG = Vector128.Load(pg); xmmR = Vector128.Load(pr);
                }
            }

            Console.WriteLine("--- PLANAR INPUT VECTORS ---");
            Console.WriteLine($"B: {FormatVector(xmmB)}");
            Console.WriteLine($"G: {FormatVector(xmmG)}");
            Console.WriteLine($"R: {FormatVector(xmmR)}");
            Console.WriteLine("");

            // Act - Mirrored inline logic
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

                xmmOut0 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmB, bMask0), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmG, gMask0), AdvSimd.Arm64.VectorTableLookup(xmmR, rMask0)));
                xmmOut1 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmB, bMask1), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmG, gMask1), AdvSimd.Arm64.VectorTableLookup(xmmR, rMask1)));
                xmmOut2 = AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmB, bMask2), AdvSimd.Or(AdvSimd.Arm64.VectorTableLookup(xmmG, gMask2), AdvSimd.Arm64.VectorTableLookup(xmmR, rMask2)));
            }
            else if (Ssse3.IsSupported)
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

                xmmOut0 = Sse2.Or(Ssse3.Shuffle(xmmB, bMask0), Sse2.Or(Ssse3.Shuffle(xmmG, gMask0), Ssse3.Shuffle(xmmR, rMask0)));
                xmmOut1 = Sse2.Or(Ssse3.Shuffle(xmmB, bMask1), Sse2.Or(Ssse3.Shuffle(xmmG, gMask1), Ssse3.Shuffle(xmmR, rMask1)));
                xmmOut2 = Sse2.Or(Ssse3.Shuffle(xmmB, bMask2), Sse2.Or(Ssse3.Shuffle(xmmG, gMask2), Ssse3.Shuffle(xmmR, rMask2)));
            }
            else
            {
                xmmOut0 = Vector128<byte>.Zero; xmmOut1 = Vector128<byte>.Zero; xmmOut2 = Vector128<byte>.Zero;
            }

            Console.WriteLine("--- INTERLEAVED OUTPUT VECTORS ---");
            Console.WriteLine($"v0: {FormatVector(xmmOut0)}");
            Console.WriteLine($"v1: {FormatVector(xmmOut1)}");
            Console.WriteLine($"v2: {FormatVector(xmmOut2)}");

            // Assert exact byte placements based on hex logic
            byte[] expectedV0 = { 0x10, 0x20, 0x30, 0x11, 0x21, 0x31, 0x12, 0x22, 0x32, 0x13, 0x23, 0x33, 0x14, 0x24, 0x34, 0x15 };
            byte[] expectedV1 = { 0x25, 0x35, 0x16, 0x26, 0x36, 0x17, 0x27, 0x37, 0x18, 0x28, 0x38, 0x19, 0x29, 0x39, 0x1A, 0x2A };
            byte[] expectedV2 = { 0x3A, 0x1B, 0x2B, 0x3B, 0x1C, 0x2C, 0x3C, 0x1D, 0x2D, 0x3D, 0x1E, 0x2E, 0x3E, 0x1F, 0x2F, 0x3F };

            for (int i = 0; i < 16; i++)
            {
                Assert.Equal(expectedV0[i], xmmOut0.GetElement(i));
                Assert.Equal(expectedV1[i], xmmOut1.GetElement(i));
                Assert.Equal(expectedV2[i], xmmOut2.GetElement(i));
            }
        }

        private static string FormatVector(Vector128<byte> vec)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                sb.Append($"{vec.GetElement(i):X2} ");
            }
            return sb.ToString().TrimEnd();
        }

        [Fact]
        public void TransformYCbCrToRgbVector128_MathParity()
        {
            if (!Vector128.IsHardwareAccelerated)
            {
                Assert.Skip("Vector128 hardware acceleration is not supported on this architecture.");
            }

            // Generate an extreme test case: Y=127, Cb=-128, Cr=127
            var xmmYin = Vector128.Create((sbyte)127);
            var xmmCbin = Vector128.Create((sbyte)-128);
            var xmmCrin = Vector128.Create((sbyte)127);

            // Mirrored inline logic
            var v128 = Vector128.Create((short)128);
            var vZero = Vector128<short>.Zero;
            var v255 = Vector128.Create((short)255);

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

            var bOut = Vector128.Narrow(xmmBL.AsUInt16(), xmmBH.AsUInt16());
            var gOut = Vector128.Narrow(xmmGL.AsUInt16(), xmmGH.AsUInt16());
            var rOut = Vector128.Narrow(xmmRL.AsUInt16(), xmmRH.AsUInt16());

            // Scalar fallback expectation logic
            int t1 = -128 >> 2;
            int t2 = 127 + (127 >> 1);
            int t3 = 127 + 128 - t1;
            int tr = 127 + 128 + t2;
            int tg = t3 - (t2 >> 1);
            int tb = t3 + (-128 << 1);

            byte expectedR = (byte)Math.Max(0, Math.Min(255, tr));
            byte expectedG = (byte)Math.Max(0, Math.Min(255, tg));
            byte expectedB = (byte)Math.Max(0, Math.Min(255, tb));

            for (int i = 0; i < 16; i++)
            {
                Assert.Equal(expectedB, bOut.GetElement(i));
                Assert.Equal(expectedG, gOut.GetElement(i));
                Assert.Equal(expectedR, rOut.GetElement(i));
            }
        }

        [Fact]
        public unsafe void YCbCr2RgbVector128_LoopMatchesScalar()
        {
            if (!Vector128.IsHardwareAccelerated)
            {
                Assert.Skip("Vector128 hardware acceleration is not supported on this architecture.");
            }

            int width = 34; // Forces two full iterations + one 2-pixel shifted tail
            int height = 2;
            int rowSizeInBytes = width * 3;

            byte[] outVec = new byte[height * rowSizeInBytes];
            byte[] outScl = new byte[height * rowSizeInBytes];

            var rand = new Random(Seed);
            for (int i = 0; i < outVec.Length; i++)
            {
                byte val = (byte)rand.Next(256);
                outVec[i] = val;
                outScl[i] = val;
            }

            fixed (byte* pOutVec = outVec, pOutScl = outScl)
            {
                InterWaveTransform.YCbCr2Rgb((Pixel*)pOutVec, width, height, rowSizeInBytes);
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)pOutScl, width, height, rowSizeInBytes);

                AssertParity(pOutScl, pOutVec, outVec.Length, width, height, rowSizeInBytes);
            }
        }

        [Fact]
        public unsafe void Vector128MultiThreaded_MatchesScalar_RealData_Continuous()
        {
            if (!Ssse3.IsSupported && !AdvSimd.IsSupported) Assert.Skip("Vector128 hardware acceleration is not supported on this CPU.");

            int width = 1248;
            int height = 1024;
            int rowSizeInBytes = width * 3;
            int totalBytes = height * rowSizeInBytes;

            byte[] bufferVec = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] bufferScl = GC.AllocateUninitializedArray<byte>(totalBytes);

            new Random(Seed).NextBytes(bufferVec);
            Buffer.BlockCopy(bufferVec, 0, bufferScl, 0, totalBytes);

            var options = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 4 };

            fixed (byte* pVec = bufferVec, pScl = bufferScl)
            {
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)pScl, width, height, rowSizeInBytes);
                InterWaveSimd.YCbCr2RgbParallelVector128((Pixel*)pVec, width, height, rowSizeInBytes, options);

                AssertParity(pScl, pVec, totalBytes, width, height, rowSizeInBytes);
            }
        }

        [Fact]
        public unsafe void Vector128MultiThreaded_MatchesScalar_RealData_Padded()
        {
            if (!Ssse3.IsSupported && !AdvSimd.IsSupported) Assert.Skip("Vector128 hardware acceleration is not supported on this CPU.");

            int width = 1247;
            int height = 1024;
            int rowSizeInBytes = (width * 3 + 3) & ~3;
            int totalBytes = height * rowSizeInBytes;

            byte[] bufferVec = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] bufferScl = GC.AllocateUninitializedArray<byte>(totalBytes);

            new Random(Seed).NextBytes(bufferVec);
            Buffer.BlockCopy(bufferVec, 0, bufferScl, 0, totalBytes);

            var options = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 4 };

            fixed (byte* pVec = bufferVec, pScl = bufferScl)
            {
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)pScl, width, height, rowSizeInBytes);
                InterWaveSimd.YCbCr2RgbParallelVector128((Pixel*)pVec, width, height, rowSizeInBytes, options);

                AssertParity(pScl, pVec, totalBytes, width, height, rowSizeInBytes);
            }
        }
        [Fact]
        public unsafe void Vector256MultiThreaded_MatchesScalar_RealData_Continuous()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");

            int width = 1248;
            int height = 1024;
            int rowSizeInBytes = width * 3;
            int totalBytes = height * rowSizeInBytes;

            byte[] bufferVec = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] bufferScl = GC.AllocateUninitializedArray<byte>(totalBytes);

            new Random(Seed).NextBytes(bufferVec);
            Buffer.BlockCopy(bufferVec, 0, bufferScl, 0, totalBytes);

            var options = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 4 };

            fixed (byte* pVec = bufferVec, pScl = bufferScl)
            {
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)pScl, width, height, rowSizeInBytes);
                InterWaveSimd.YCbCr2RgbParallelVector256((Pixel*)pVec, width, height, rowSizeInBytes, options);

                AssertParity(pScl, pVec, totalBytes, width, height, rowSizeInBytes);
            }
        }

        [Fact]
        public unsafe void Vector256MultiThreaded_MatchesScalar_RealData_Padded()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");

            int width = 1247;
            int height = 1024;
            int rowSizeInBytes = (width * 3 + 3) & ~3;
            int totalBytes = height * rowSizeInBytes;

            byte[] bufferVec = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] bufferScl = GC.AllocateUninitializedArray<byte>(totalBytes);

            new Random(Seed).NextBytes(bufferVec);
            Buffer.BlockCopy(bufferVec, 0, bufferScl, 0, totalBytes);

            var options = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 4 };

            fixed (byte* pVec = bufferVec, pScl = bufferScl)
            {
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)pScl, width, height, rowSizeInBytes);
                InterWaveSimd.YCbCr2RgbParallelVector256((Pixel*)pVec, width, height, rowSizeInBytes, options);

                AssertParity(pScl, pVec, totalBytes, width, height, rowSizeInBytes);
            }
        }
        [Fact]
        public unsafe void Vector128SingleThread_MatchesScalar_RealData_Continuous()
        {
            if (!Ssse3.IsSupported && !AdvSimd.IsSupported) Assert.Skip("Vector128 hardware acceleration is not supported on this CPU.");

            int width = 1248;
            int height = 1024;
            int rowSizeInBytes = width * 3;
            int totalBytes = height * rowSizeInBytes;

            byte[] bufferVec = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] bufferScl = GC.AllocateUninitializedArray<byte>(totalBytes);

            new Random(Seed).NextBytes(bufferVec);
            Buffer.BlockCopy(bufferVec, 0, bufferScl, 0, totalBytes);

            fixed (byte* pVec = bufferVec, pScl = bufferScl)
            {
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)pScl, width, height, rowSizeInBytes);
                InterWaveSimd.YCbCr2RgbVector128((Pixel*)pVec, width, height, rowSizeInBytes);

                AssertParity(pScl, pVec, totalBytes, width, height, rowSizeInBytes);
            }
        }

        [Fact]
        public unsafe void Vector128SingleThread_MatchesScalar_RealData_Padded()
        {
            if (!Ssse3.IsSupported && !AdvSimd.IsSupported) Assert.Skip("Vector128 hardware acceleration is not supported on this CPU.");

            int width = 1247;
            int height = 1024;
            int rowSizeInBytes = (width * 3 + 3) & ~3;
            int totalBytes = height * rowSizeInBytes;

            byte[] bufferVec = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] bufferScl = GC.AllocateUninitializedArray<byte>(totalBytes);

            new Random(Seed).NextBytes(bufferVec);
            Buffer.BlockCopy(bufferVec, 0, bufferScl, 0, totalBytes);

            fixed (byte* pVec = bufferVec, pScl = bufferScl)
            {
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)pScl, width, height, rowSizeInBytes);
                InterWaveSimd.YCbCr2RgbVector128((Pixel*)pVec, width, height, rowSizeInBytes);

                AssertParity(pScl, pVec, totalBytes, width, height, rowSizeInBytes);
            }
        }

        [Fact]
        public unsafe void Vector256SingleThread_MatchesScalar_RealData_Continuous()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");

            int width = 1248;
            int height = 1024;
            int rowSizeInBytes = width * 3;
            int totalBytes = height * rowSizeInBytes;

            byte[] bufferVec = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] bufferScl = GC.AllocateUninitializedArray<byte>(totalBytes);

            new Random(Seed).NextBytes(bufferVec);
            Buffer.BlockCopy(bufferVec, 0, bufferScl, 0, totalBytes);

            fixed (byte* pVec = bufferVec, pScl = bufferScl)
            {
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)pScl, width, height, rowSizeInBytes);
                InterWaveSimd.YCbCr2RgbVector256((Pixel*)pVec, width, height, rowSizeInBytes);

                AssertParity(pScl, pVec, totalBytes, width, height, rowSizeInBytes);
            }
        }

        [Fact]
        public unsafe void Vector256SingleThread_MatchesScalar_RealData_Padded()
        {
            if (!Avx2.IsSupported) Assert.Skip("AVX2 hardware acceleration is not supported on this CPU.");

            int width = 1247;
            int height = 1024;
            int rowSizeInBytes = (width * 3 + 3) & ~3;
            int totalBytes = height * rowSizeInBytes;

            byte[] bufferVec = GC.AllocateUninitializedArray<byte>(totalBytes);
            byte[] bufferScl = GC.AllocateUninitializedArray<byte>(totalBytes);

            new Random(Seed).NextBytes(bufferVec);
            Buffer.BlockCopy(bufferVec, 0, bufferScl, 0, totalBytes);

            fixed (byte* pVec = bufferVec, pScl = bufferScl)
            {
                InterWaveTransform.YCbCr2RgbScalar((Pixel*)pScl, width, height, rowSizeInBytes);
                InterWaveSimd.YCbCr2RgbVector256((Pixel*)pVec, width, height, rowSizeInBytes);

                AssertParity(pScl, pVec, totalBytes, width, height, rowSizeInBytes);
            }
        }

        private unsafe void AssertParity(byte* pScl, byte* pVec, int totalBytes, int width, int height, int rowSizeInBytes)
        {
            string testName = Xunit.TestContext.Current.Test.TestDisplayName;
            double diff = Util.ImageBinaryDiff(pScl, pVec, width, height, rowSizeInBytes);
            if (diff > 0.0)
            {
                Util.DumpImageMismatch(pScl, pVec, totalBytes, width, 0, testName);
                Assert.Fail($"Image mismatch in {testName}. Diff score: {diff}");
            }
        }
    }
}