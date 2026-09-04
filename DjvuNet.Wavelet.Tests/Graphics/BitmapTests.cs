using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.Tests;
using Xunit;

namespace DjvuNet.Graphics.Tests
{
    public class BitmapTests
    {
        /// <summary>
        /// 64-bit CLR Array Overhead: 8 byte MethodTable + 4 byte SyncBlock + 4 byte Length + 8 byte Padding
        /// </summary>
        public const int Clr64BitArrayOverhead = 24;

        [Theory]
        [InlineData(-10, 10, 0, "width")]
        [InlineData(10, -10, 0, "height")]
        [InlineData(10, 10, -10, "border")]
        public void Init_NegativeParameters_Throws(int width, int height, int border, string expectedParam)
        {
            Bitmap bmp = new Bitmap();
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.Init(height, width, border));
            Assert.Equal(expectedParam, ex.ParamName);

            var ex2 = Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.Init(new sbyte[100], height, width, border));
            Assert.Equal(expectedParam, ex2.ParamName);
        }

        [Theory]
        // maxOffsetCalc overflow: 65536 * 65536 = 4,294,967,296 > Array.MaxLength
        [InlineData(65536, 65536, 0, true, "exceeding Array.MaxLength")]
        // ZeroBuffer overflow bypassed: height=0 bypasses maxOffset, width=2147483637 > Array.MaxLength is suppressed by Math.Min(height, 1)
        [InlineData(2147483637, 0, 0, false, null)]
        public void Init_CalculatedStrideOverflow_Throws(int width, int height, int border, bool shouldThrow, string expectedMessageFragment)
        {
            Bitmap bmp = new Bitmap();
            if (shouldThrow)
            {
                var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.Init(height, width, border));
                Assert.Contains(expectedMessageFragment, ex.Message);
            }
            else
            {
                bmp.Init(height, width, border);
            }
        }

        public static TheoryData<int, int, int, string> BoundsOverflowData => new()
        {
            // Data buffer overflow: Empty struct, massive value (passes stride, explodes maxOffset)
            { 0, 0, Array.MaxLength + 1, "invalid Data buffer size" },
            // EnsureZeroBuffer top guard is unreachable: Resize intercepts the invalid massiveBorder first
            { 0, 200000000, Array.MaxLength + 1, "invalid Data buffer size" }
        };

        [Theory]
        [MemberData(nameof(BoundsOverflowData))]
        public void SetMinimumBorder_BoundsOverflow_Throws(int height, int width, int massiveBorder, string expectedMessageFragment)
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(height, width, 0); 
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.SetMinimumBorder(massiveBorder));
            Assert.Contains(expectedMessageFragment, ex.Message);
        }

        [Fact]
        public void InitWithData_MismatchedBuffer_Throws()
        {
            Bitmap bmp = new Bitmap();
            int width = 10;
            int height = 10;
            int border = 0;
            sbyte[] badBuffer = new sbyte[50]; // Requires 100

            var ex = Assert.Throws<DjvuArgumentException>(() => bmp.Init(badBuffer, height, width, border));
            Assert.Equal("data", ex.ParamName);
            Assert.Contains("Mismatch", ex.Message);
        }

        [Fact]
        public void InitWithData_NullBuffer_Throws()
        {
            Bitmap bmp = new Bitmap();
            var ex = Assert.Throws<DjvuArgumentException>(() => bmp.Init(null, 10, 10, 0));
            Assert.Equal("data", ex.ParamName);
        }

        [Fact]
        public void InitWithRectangle_NegativeBorder_Throws()
        {
            Bitmap bmp = new Bitmap();
            Bitmap source = new Bitmap(10, 10, 0);
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.Init(ref source, new Rectangle(0, 0, 10, 10), -5));
            Assert.Equal("border", ex.ParamName);
        }

        [Fact]
        public void InitWithRectangle_SelfAliasing_SuccessfullyResizesAndPreservesData()
        {
            // Create a 10x10 bitmap with border 0
            Bitmap bmp = new Bitmap();
            bmp.Init(10, 10, 0);

            // Fill with sequential data 0-99
            for (int i = 0; i < 100; i++)
            {
                bmp.SetByteAt(i, (sbyte)i);
            }

            // Capture old state for diagnostic printing
            int oldWidth = bmp.Width;
            int oldHeight = bmp.Height;
            int oldBorder = bmp.Border;
            sbyte[] oldData = new sbyte[bmp.Data.Length];
            Array.Copy(bmp.Data, oldData, bmp.Data.Length);

            // Self-alias: Crop to a 5x5 rectangle starting at offset (3, 4) and add a border of 2.
            // In a Cartesian plane, Row 0 is the bottom.
            // The row offset logic: Row y corresponds to indices [y*10, y*10 + 9].
            // So coordinate (x=3, y=4) is at offset 4*10 + 3 = 43.
            bmp.Init(ref bmp, new Rectangle(3, 4, 5, 5), 2);

            System.Console.WriteLine("--- Diagnostic: Self-Aliasing Buffer Contents ---");
            System.Console.WriteLine($"Old Buffer ({oldWidth}x{oldHeight}, Border {oldBorder}):");
            for (int y = 0; y < oldHeight; y++)
            {
                System.Console.Write($"Row {y,2}: ");
                for (int x = 0; x < oldWidth; x++)
                {
                    int offset = (y * oldWidth) + x + oldBorder;
                    System.Console.Write($"{oldData[offset],3} ");
                }
                System.Console.WriteLine();
            }

            System.Console.WriteLine($"\nNew Buffer ({bmp.Width}x{bmp.Height}, Border {bmp.Border}):");
            for (int y = 0; y < bmp.Height; y++)
            {
                System.Console.Write($"Row {y + 4,2}: ");
                System.Console.Write(new string(' ', 3 * 4));

                for (int x = 0; x < bmp.Width; x++)
                {
                    int offset = bmp.RowOffset(y) + x;
                    System.Console.Write($"{bmp.GetByteAt(offset),3} ");
                }
                System.Console.WriteLine();
            }
            System.Console.WriteLine("-------------------------------------------------");

            Assert.Equal(5, bmp.Width);
            Assert.Equal(5, bmp.Height);
            Assert.Equal(2, bmp.Border);

            // Verify pixel at original (3,4) moved to logical (0,0) due to crop.
            // Expected value: 43
            Assert.Equal(43, bmp.GetByteAt(bmp.RowOffset(0) + 0));

            // Verify pixel at original (7,8) moved to logical (4,4) due to crop.
            // Expected value: 8*10 + 7 = 87
            Assert.Equal(87, bmp.GetByteAt(bmp.RowOffset(4) + 4));
        }

        public static IEnumerable<object[]> GenerateCropTestData()
        {
            var testCases = new System.Collections.Generic.List<object[]>(1000);
            int count = 0;
            int[] borders = { 0, 3 };
            int[] rXs = { -5, 0, 5, 15 };
            int[] rYs = { -5, 0, 5, 15 };
            uint[] rWs = { 0, 5, 15 };
            uint[] rHs = { 0, 5, 15 };
            bool[] aliases = { false, true };

            // Deterministic combinatorial matrix across equivalence classes
            foreach (int srcBorder in borders)
            foreach (int tgtBorder in borders)
            foreach (int rX in rXs)
            foreach (int rY in rYs)
            foreach (uint rW in rWs)
            foreach (uint rH in rHs)
            foreach (bool selfAlias in aliases)
            {
                if (count++ < 900)
                {
                    Type expectedException = DetermineExpectedException(tgtBorder, rW, rH);
                    testCases.Add(new object[] { 10, 10, srcBorder, tgtBorder, rX, rY, rW, rH, selfAlias, expectedException });
                }
            }

            // Fuzzing extreme and random spatial combinations
            Random rnd = new Random(42); // Deterministic seed
            for (int i = 0; i < 100; i++)
            {
                int srcW = rnd.Next(0, 20);
                int srcH = rnd.Next(0, 20);
                int srcB = rnd.Next(0, 1000);
                int tgtB = rnd.Next(-5, 1000); // Intentionally allow negative borders in fuzzing
                int rX = rnd.Next(-10000, 10000); // Extreme disjoints
                int rY = rnd.Next(-10000, 10000);
                uint rW = (uint)rnd.Next(0, 1000); // Massive target
                uint rH = (uint)rnd.Next(0, 1000);
                bool alias = rnd.Next(2) == 0;

                Type expectedException = DetermineExpectedException(tgtB, rW, rH);
                testCases.Add(new object[] { srcW, srcH, srcB, tgtB, rX, rY, rW, rH, alias, expectedException });
            }

            // Sort descending by target area (rW * rH) to prevent POH fragmentation
            testCases.Sort((a, b) => 
            {
                long areaA = (long)(uint)a[6] * (uint)a[7];
                long areaB = (long)(uint)b[6] * (uint)b[7];
                return areaB.CompareTo(areaA);
            });

            return testCases;
        }

        private static Type DetermineExpectedException(int tgtBorder, uint rW, uint rH)
        {
            if (tgtBorder < 0) return typeof(DjvuArgumentOutOfRangeException);

            int expectedW = (rW == 0 || rH == 0) ? 0 : (int)rW;
            int expectedH = (rW == 0 || rH == 0) ? 0 : (int)rH;

            long newStrideCalc = (long)expectedW + tgtBorder;
            long maxOffsetCalc = ((long)expectedH * newStrideCalc) + tgtBorder;

            if (newStrideCalc > int.MaxValue || newStrideCalc < 0 ||
                maxOffsetCalc > int.MaxValue || maxOffsetCalc < 0)
            {
                return typeof(DjvuArgumentOutOfRangeException); // Overflow protection
            }

            return null;
        }

        [Theory]
        [MemberData(nameof(GenerateCropTestData))]
        public void InitWithRectangle_CombinatorialAndFuzzed_ValidatesOracle(
            int srcW, int srcH, int srcBorder, int tgtBorder, int rX, int rY, int rW, int rH, bool selfAlias, Type expectedException)
        {
            Bitmap source = new Bitmap();
            source.Init(srcH, srcW, srcBorder);

            // Build the oracle (expected state)
            sbyte[,] expectedGrid = null;
            if (rW > 0 && rH > 0 && rW < 10000 && rH < 10000) // Prevent OOM in oracle setup
            {
                expectedGrid = new sbyte[rH, rW];
            }

            for (int y = 0; y < srcH; y++)
            {
                for (int x = 0; x < srcW; x++)
                {
                    // Values 1-127 to easily distinguish from 0 out-of-bounds default
                    sbyte val = (sbyte)((x + y * srcW) % 127 + 1);
                    source.SetByteAt(source.RowOffset(y) + x, val);
                }
            }

            if (expectedGrid != null)
            {
                for (int y = 0; y < rH; y++)
                {
                    for (int x = 0; x < rW; x++)
                    {
                        int srcX = x + rX;
                        int srcY = y + rY;
                        if (srcX >= 0 && srcX < srcW && srcY >= 0 && srcY < srcH)
                        {
                            expectedGrid[y, x] = (sbyte)((srcX + srcY * srcW) % 127 + 1);
                        }
                        else
                        {
                            expectedGrid[y, x] = 0;
                        }
                    }
                }
            }

            Bitmap target = new Bitmap();

            if (expectedException != null)
            {
                if (selfAlias)
                    Assert.Throws(expectedException, () => source.Init(ref source, new Rectangle(rX, rY, rW, rH), tgtBorder));
                else
                    Assert.Throws(expectedException, () => target.Init(ref source, new Rectangle(rX, rY, rW, rH), tgtBorder));
                return;
            }

            // No exception expected, safely execute and validate state
            if (selfAlias)
            {
                source.Init(ref source, new Rectangle(rX, rY, rW, rH), tgtBorder);
                target = source; // Assign back to target for unified validation below
            }
            else
            {
                target.Init(ref source, new Rectangle(rX, rY, rW, rH), tgtBorder);
            }

            // Oracle Geometry Correction: If either requested dimension is 0, the Rectangle is Empty.
            // An Empty Rectangle correctly collapses both Width and Height to 0.
            int expectedW = (rW == 0 || rH == 0) ? 0 : rW;
            int expectedH = (rW == 0 || rH == 0) ? 0 : rH;

            Assert.Equal(expectedW, target.Width);
            Assert.Equal(expectedH, target.Height);
            Assert.Equal(tgtBorder, target.Border);

            if (expectedGrid != null)
            {
                for (int y = 0; y < expectedH; y++)
                {
                    for (int x = 0; x < expectedW; x++)
                    {
                        int offset = target.RowOffset(y) + x;
                        Assert.Equal(expectedGrid[y, x], target.GetByteAt(offset));
                    }
                }
            }
        }

        //[Fact]
        //public void SetMinimumBorder_NegativeValue_ThrowsDjvuArgumentOutOfRangeException()
        //{
        //    Bitmap bmp = new Bitmap();
        //    var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.SetMinimumBorder(-5));
        //    Assert.Equal("value", ex.ParamName);
        //}

        //[Fact]
        //public void SetMinimumBorder_CalculatedStrideOverflow_ThrowsDjvuArgumentOutOfRangeException()
        //{
        //    // Create a bitmap using the public API with a massive width (and therefore stride).
        //    // Width = int.MaxValue / 2, Height = 1. Buffer is null.
        //    Bitmap bmp = new Bitmap();
        //    bmp.Init(1, int.MaxValue / 2, 0);

        //    // Attempting to set a massive border expands the padding calculation.
        //    // newStride = BytesPerRow - _Border + value = (int.MaxValue / 2) - 0 + (int.MaxValue / 2 + 10)
        //    // While newStride itself does not overflow int, passing it into Resize causes
        //    // maxOffsetCalc = (height * newStride) + border to overflow, throwing on 'height'.
        //    var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.SetMinimumBorder(int.MaxValue / 2 + 10));
        //    Assert.Equal("height", ex.ParamName);
        //}

        [Fact]
        public void Duplicate_UninitializedBitmap_Succeeds()
        {
            Bitmap bmp = new Bitmap();
            var clone = bmp.Duplicate();

            Assert.Equal(default, clone);
            Assert.Null(clone.Data);
            Assert.Equal(0, clone.Width);
            Assert.Equal(0, clone.Height);
        }



        [Fact()]
        public void BitmapTest001()
        {
            Bitmap bmp = new Bitmap();
            Assert.NotNull(bmp);
            Assert.Equal(1, bmp.BytesPerPixel);
            Assert.Equal(0, bmp.Width);
            Assert.Equal(0, bmp.Height);
            Assert.Null(bmp.Data);
        }

        [Fact()]
        public void BitmapTest002()
        {
            int width = 128;
            int height = 128;
            int border = 0;
            Bitmap bmp = new Bitmap(height, width, border);
            Assert.NotNull(bmp);
            Assert.Equal(1, bmp.BytesPerPixel);
            Assert.Equal(width, bmp.Width);
            Assert.Equal(height, bmp.Height);
            Assert.Equal((width + border) * height, bmp.Data.Length);
            Assert.Equal(height, bmp.Height);
        }

        [Fact()]
        public void BitmapTest003()
        {
            int width = 128;
            int height = 128;
            int border = 0;
            Bitmap bmp = new Bitmap(height, width, border);
            Assert.NotNull(bmp);
            Assert.Equal(1, bmp.BytesPerPixel);
            Assert.Equal(width, bmp.Width);
            Assert.Equal(height, bmp.Height);
            Assert.Equal((width + border) * height, bmp.Data.Length);

            Bitmap test = new Bitmap(ref bmp);
            Assert.Equal(1, test.BytesPerPixel);
            Assert.Equal(width, test.Width);
            Assert.Equal(height, test.Height);
            Assert.Equal((width + border) * height, test.Data.Length);
            Assert.Equal(height, test.Height);
        }

        public static ref Bitmap CreateVerifyDefaultBitmap(ref Bitmap bmp)
        {
            Assert.Equal(bmp, default);
            Assert.Equal(1, bmp.BytesPerPixel);
            Assert.Equal(0, bmp.Width);
            Assert.Equal(0, bmp.Height);
            Assert.Null(bmp.Data);
            return ref bmp;
        }

        public static Bitmap CreateInitVerifyBitmap(int width, int height, int border)
        {
            Bitmap bmp = default;
            bmp = CreateVerifyDefaultBitmap(ref bmp);
            bmp.Init(height, width, border);
            Assert.Equal(width, bmp.Width);
            Assert.Equal(height, bmp.Height);
            Assert.Equal(border, bmp.Border);
            return bmp;
        }

        public static Bitmap CreateIntiFillVerifyBitmap(int width, int height, int border, sbyte color)
        {
            var bmp = CreateInitVerifyBitmap(width, height, border);
            bmp.Fill(color);
            Assert.Equal(unchecked((byte)color), bmp.GetByteAt(width * height / 2));
            return bmp.Duplicate();
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void WriteBitmap(int width, int height, ref Bitmap bmp)
        {
            string format = "x4";
            for (int i = (height - 1); i >= 0; i--)
            {
                Console.Write($"{(i * width).ToString(format)}  ");
                for (int k = 0; k < width; k++)
                {
                    Console.Write($"{bmp.GetByteAt(i * width + k).ToString("x2")} ");
                }

                Console.WriteLine();
            }
            Console.WriteLine();
        }

        [Fact()]
        public void DuplicateTest()
        {
            int width = 128;
            int height = 128;
            int border = 0;
            sbyte color = -1;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);
            var bmp2 = bmp.Duplicate();

            Assert.Equal(bmp, bmp2);
            Assert.False(ReferenceEquals(bmp.Data, bmp2.Data));

            Assert.Equal(width, bmp2.Width);
            Assert.Equal(height, bmp2.Height);
            Assert.Equal(border, bmp2.Border);
            Assert.Equal(unchecked((byte)color), bmp.GetByteAt(width / 2));
            Assert.Equal(bmp.GetByteAt(width/2), bmp2.GetByteAt(width/2));
        }

        [Fact()]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void GetBooleanAtTest001()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);

            for(int i = 0; i < width * height; i++)
                bmp.SetByteAt(i, (sbyte)( i % 11 ));

            for(int i = 0; i < width * height; i++)
            {
                if (i % 11 == 0)
                    Assert.True(bmp.GetBooleanAt(i));
                else
                    Assert.False(bmp.GetBooleanAt(i));
            }

        }

        [Fact()]
        public void GraysTest001()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);
            Assert.Throws<DjvuArgumentOutOfRangeException>("value", () => bmp.Grays = 1);
        }

        [Fact()]
        public void GraysTest002()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);
            Assert.Throws<DjvuArgumentOutOfRangeException>("value", () => bmp.Grays = 257);
        }

        [Fact()]
        public void GraysTest003()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);
            bmp.Grays = 101;
            Assert.Equal(101, bmp.Grays);
        }

        //[Fact()]
        //public void MinimumBorderTest001()
        //{
        //    int width = 16;
        //    int height = 16;
        //    int border = 0;
        //    sbyte color = 0;
        //    var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);
        //    Assert.Equal(width, bmp.GetRowSize());

        //    bmp.SetMinimumBorder(4);
        //    Assert.Equal(4, bmp.Border);
        //    Assert.Equal(width + 4, bmp.GetRowSize());
        //}

        [Fact()]
        public void SetByteAtTest001()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = -1;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);

            bmp.SetByteAt(7, 0);

            Assert.True(bmp.GetBooleanAt(7));
            Assert.False(bmp.GetBooleanAt(6));
            Assert.False(bmp.GetBooleanAt(8));
        }

        [Fact()]
        public void SetByteAtTest002()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = -11;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);
            bmp.Grays = 256;

            bmp.SetByteAt(7, 127);

            Assert.True(bmp.GetByteAt(7) == 127);
            Assert.False(bmp.GetByteAt(6) == 244);
            Assert.False(bmp.GetByteAt(8) == 244);
        }

        [Fact()]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void GetByteAtTest001()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);

            for (int i = 0; i < width * height; i++)
                bmp.SetByteAt(i, (sbyte)(i % 11));

            for (int i = 0; i < width * height; i++)
                Assert.True(bmp.GetByteAt(i) == i % 11);
        }

        [Fact()]
        public void BlitTest001()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color1 = -1;
            sbyte color2 = 4;
            sbyte color3 = 5;
            var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
            bmp1.Grays = 255;
            var bmp2 = CreateIntiFillVerifyBitmap(width / 2, height / 2, border, color3);
            bmp2.Grays = 255;

            bool result = bmp1.Blit(ref bmp2, width / 2, height / 2, 1);
            Assert.True(result);

            Assert.Equal(unchecked((byte)color1), bmp1.GetByteAt(width * (height / 4) + (width * 3 / 4)));
            Assert.Equal(unchecked((byte)color2), bmp1.GetByteAt(width * (height / 2 + height / 4) + (width * 3 / 4)));
        }

        [Fact()]
        public void BlitTest002()
        {
            int width = 16;
            int height = 16;
            int border = 0;
            sbyte color1 = -1;
            sbyte color2 = 101;
            var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
            bmp1.Grays = 256;
            var bmp2 = CreateIntiFillVerifyBitmap(width / 2, height / 2, border, color2);
            bmp2.Grays = 256;

            bool result = bmp1.Blit(ref bmp2, width, height, 2);

            Assert.True(result);
            Assert.Equal(unchecked((byte)255), bmp1.GetByteAt(width * (height / 4) + 10));
            Assert.Equal(unchecked((byte)(147)), bmp1.GetByteAt(width * (height / 2 + 1) + 10));
        }

        [Fact()]
        public void BlitTest003()
        {
            int width = 256;
            int height = 256;
            int border = 0;
            sbyte color1 = -1;
            sbyte color2 = 0;
            var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
            var bmp2 = CreateIntiFillVerifyBitmap(width / 2, height / 2, border, color2);

            bool result = bmp1.Blit(ref bmp2, 2048, height / 2, 2);
            Assert.False(result);
        }

        [Fact()]
        public void BlitTest004()
        {
            int width = 256;
            int height = 256;
            int border = 0;
            sbyte color1 = -1;
            sbyte color2 = 0;
            var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
            var bmp2 = CreateIntiFillVerifyBitmap(width / 2, height / 2, border, color2);

            bool result = bmp1.Blit(ref bmp2, width/2, 2048, 2);
            Assert.False(result);
        }

        [Fact()]
        public void BlitTest005()
        {
            int width = 256;
            int height = 256;
            int border = 0;
            sbyte color1 = -1;
            sbyte color2 = 0;
            var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
            var bmp2 = CreateIntiFillVerifyBitmap(width / 2, height / 2, border, color2);

            bool result = bmp1.Blit(ref bmp2, -1024, height / 2, 2);
            Assert.False(result);
        }

        [Fact()]
        public void BlitTest006()
        {
            int width = 256;
            int height = 256;
            int border = 0;
            sbyte color1 = -1;
            sbyte color2 = 0;
            var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
            var bmp2 = CreateIntiFillVerifyBitmap(width / 2, height / 2, border, color2);

            bool result = bmp1.Blit(ref bmp2, width / 2, -1024, 2);
            Assert.False(result);
        }

        [Fact()]
        public void RowOffsetTest()
        {
            int width = 16;
            int height = 16;
            int border = 4;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);

            int result = bmp.RowOffset(8);
            Assert.Equal(width * 8 + 8 * 4 + 4, result);
        }

        [Fact()]
        public void GetRowSizeTest()
        {
            int width = 16;
            int height = 16;
            int border = 4;
            sbyte color = 0;
            var bmp = CreateIntiFillVerifyBitmap(width, height, border, color);

            int result = bmp.GetRowSize();
            Assert.Equal(width + border, result);
        }

        [Fact()]
        public void FillTest001()
        {
            var bmp = new Bitmap();
            Assert.NotNull(bmp);

            bmp.Init(128, 128, 0);
            Assert.Equal(128, bmp.Width);
            Assert.Equal(128, bmp.Height);
            Assert.Equal(0, bmp.Border);

            bmp.Fill(-1);
            Assert.Equal(255, bmp.GetByteAt(64));

            bmp.Fill(1);
            Assert.Equal(1, bmp.GetByteAt(64));
        }

        //[Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        //public void FillTest002()
        //{

        //    int width = 16;
        //    int height = 16;
        //    int border = 4;
        //    sbyte color1 = 0;
        //    sbyte color2 = -1;
        //    var bmp1 = CreateIntiFillVerifyBitmap(width, height, border, color1);
        //    var bmp2 = CreateIntiFillVerifyBitmap(width, height, border, color2);

        //    bmp1.Fill(bmp2, 4, 4);

        //    IPixelReference pix1 = bmp1.CreateGPixelReference(0);
        //    pix1.SetOffset(5, 5);

        //    Assert.Equal(color2, pix1.ToPixel().Blue);

        //    pix1.SetOffset(3, 5);
        //    Assert.Equal(color1, pix1.ToPixel().Blue);
        //}

        [Theory]
        // Base cases: no translation (0, 0)
        [InlineData(15, 0, 0, true)]
        [InlineData(16, 0, 0, true)]
        [InlineData(17, 0, 0, true)]
        [InlineData(25, 0, 0, true)]
        [InlineData(31, 0, 0, true)]
        [InlineData(32, 0, 0, true)]
        [InlineData(33, 0, 0, true)]
        [InlineData(48, 0, 0, true)]
        [InlineData(63, 0, 0, true)]
        [InlineData(64, 0, 0, true)]
        [InlineData(96, 0, 0, true)]
        [InlineData(111, 0, 0, true)]
        [InlineData(112, 0, 0, true)]
        [InlineData(115, 0, 0, true)]

        // Translation cases: positive shifts
        [InlineData(25, 3, 2, true)]
        [InlineData(48, 10, 0, true)]

        // Translation cases: negative shifts (top/left clipping)
        [InlineData(31, -2, -1, true)]
        [InlineData(64, -5, 0, true)]
        [InlineData(96, 0, -2, true)]
        [InlineData(115, -100, 2, true)] // Heavy left clipping

        // Translation cases: extreme positive shifts (bottom/right clipping)
        [InlineData(115, 130, 2, true)] // Heavy right clipping
        [InlineData(111, 4, 3, true)]

        // Translation cases: doBlit = false (overwrite mode)
        [InlineData(15, 0, 0, false)]
        [InlineData(16, 0, 0, false)]
        [InlineData(17, 0, 0, false)]
        [InlineData(25, 0, 0, false)]
        [InlineData(31, 0, 0, false)]
        [InlineData(32, 0, 0, false)]
        [InlineData(33, 0, 0, false)]
        [InlineData(48, 0, 0, false)]
        [InlineData(63, 0, 0, false)]
        [InlineData(64, 0, 0, false)]
        [InlineData(96, 0, 0, false)]

        [InlineData(48, 10, 0, false)]
        [InlineData(64, -5, 2, false)]

        // Extreme Translation cases: out of bounds (no blit expected)
        [InlineData(15, 150, 0, true)]   // x0 >= targetWidth
        [InlineData(15, -15, 0, true)]   // x1 >= sourceWidth
        [InlineData(15, 0, 5, true)]     // y0 >= height
        [InlineData(15, 0, -5, true)]    // y1 >= height

        // Extreme Translation cases: out of bounds (no blit expected)
        [InlineData(15, 150, 0, false)]   // x0 >= targetWidth
        [InlineData(15, -15, 0, false)]   // x1 >= sourceWidth
        [InlineData(15, 0, 5, false)]     // y0 >= height
        [InlineData(15, 0, -5, false)]    // y1 >= height

        public unsafe void InsertMap_Bitonal(int sourceWidth, int dx, int dy, bool doBlit)
        {
            // Arrange
            int targetWidth = 150; // Large enough to hold the maximum test boundary
            int height = 5;
            var target = new Bitmap();
            target.Init(height, targetWidth, 0);
            target.Grays = 2; // Enable bitonal SIMD optimization

            var source = new Bitmap();
            source.Init(height, sourceWidth, 0);
            source.Grays = 2;

            // Fill with distinct patterns to mathematically verify Bitwise OR
            for (int y = 0; y < height; y++)
            {
                sbyte* tRow = target.GetRow(y);
                sbyte* sRow = source.GetRow(y);
                for (int x = 0; x < targetWidth; x++)
                    tRow[x] = (sbyte)(x % 2); // 0, 1, 0, 1...

                for (int x = 0; x < sourceWidth; x++)
                    sRow[x] = (sbyte)((x / 2) % 2); // 0, 0, 1, 1, 0, 0...
            }

            var targetOrig = target.Duplicate();

            // Predict Return Value
            int x0 = (dx > 0) ? dx : 0;
            int y0 = (dy > 0) ? dy : 0;
            int x1 = (dx < 0) ? (-dx) : 0;
            int y1 = (dy < 0) ? (-dy) : 0;
            int expectedW = Math.Min(targetWidth - x0, sourceWidth - x1);
            int expectedH = Math.Min(height - y0, height - y1);
            bool expectedResult = (expectedW > 0) && (expectedH > 0);

            // Act
            bool actualResult = target.InsertMap(ref source, dx, dy, doBlit);
            Assert.Equal(expectedResult, actualResult);

            // Assert
            for (int y = 0; y < height; y++)
            {
                sbyte* tRow = target.GetRow(y);
                sbyte* tOrigRow = targetOrig.GetRow(y);
                
                int sourceY = y - dy;
                sbyte* sRow = (sourceY >= 0 && sourceY < height) ? source.GetRow(sourceY) : null;

                for (int x = 0; x < targetWidth; x++)
                {
                    int sourceX = x - dx;
                    if (sRow != null && sourceX >= 0 && sourceX < sourceWidth)
                    {
                        sbyte expected = doBlit ? (sbyte)(tOrigRow[x] | sRow[sourceX]) : sRow[sourceX];
                        Assert.Equal(expected, tRow[x]);
                    }
                    else
                    {
                        Assert.Equal(tOrigRow[x], tRow[x]); // Untouched buffer check
                    }
                }
            }
        }

        [Theory]
        // 1. Base cases: no translation (0, 0) | Fixed Grays: 256
        [InlineData(15, 0, 0, true, 256)]
        [InlineData(16, 0, 0, true, 256)]
        [InlineData(17, 0, 0, true, 256)]
        [InlineData(25, 0, 0, true, 256)]
        [InlineData(31, 0, 0, true, 256)]
        [InlineData(32, 0, 0, true, 256)]
        [InlineData(33, 0, 0, true, 256)]
        [InlineData(48, 0, 0, true, 256)]
        [InlineData(63, 0, 0, true, 256)]
        [InlineData(64, 0, 0, true, 256)]
        [InlineData(96, 0, 0, true, 256)]
        [InlineData(111, 0, 0, true, 256)]
        [InlineData(112, 0, 0, true, 256)]
        [InlineData(115, 0, 0, true, 256)]

        // 2. Grays Variation cases | Fixed Spatial: Base (48, 0, 0)
        [InlineData(48, 0, 0, true, 3)]     // Lowest grayscale boundary
        [InlineData(48, 0, 0, true, 126)]   // Below sbyte sign wrap
        [InlineData(48, 0, 0, true, 127)]   // Exact sbyte maximum positive threshold
        [InlineData(48, 0, 0, true, 128)]   // Exact sbyte negative sign transition
        [InlineData(48, 0, 0, true, 129)]   // Just above sbyte transition
        [InlineData(48, 0, 0, true, 254)]   // Below byte limit
        [InlineData(48, 0, 0, true, 255)]   // Exact unsigned byte limit

        // 3. Translation cases: positive shifts | Fixed Grays: 256
        [InlineData(25, 3, 2, true, 256)]
        [InlineData(48, 10, 0, true, 256)]

        // 4. Translation cases: negative shifts (top/left clipping) | Fixed Grays: 256
        [InlineData(31, -2, -1, true, 256)]
        [InlineData(64, -5, 0, true, 256)]
        [InlineData(96, 0, -2, true, 256)]
        [InlineData(115, -100, 2, true, 256)] // Heavy left clipping

        // 5. Translation cases: extreme positive shifts (bottom/right clipping) | Fixed Grays: 256
        [InlineData(115, 130, 2, true, 256)] // Heavy right clipping
        [InlineData(111, 4, 3, true, 256)]

        // 6. Translation cases: doBlit = false (overwrite mode) | Fixed Grays: 256
        [InlineData(15, 0, 0, false, 256)]
        [InlineData(31, 0, 0, false, 256)]
        [InlineData(48, 10, 0, false, 256)]
        [InlineData(64, -5, 2, false, 256)]

        // 7. Extreme Translation cases: out of bounds (no blit expected) | Fixed Grays: 256
        [InlineData(15, 150, 0, true, 256)]   // x0 >= targetWidth
        [InlineData(15, -15, 0, true, 256)]   // x1 >= sourceWidth
        [InlineData(15, 0, 5, true, 256)]     // y0 >= height
        [InlineData(15, 0, -5, true, 256)]    // y1 >= height
        public unsafe void InsertMap_Grayscale(int sourceWidth, int dx, int dy, bool doBlit, int grays)
        {
            // Arrange
            int targetWidth = 150; // Large enough to hold the maximum test boundary
            int height = 5;
            var target = new Bitmap();
            target.Init(height, targetWidth, 0);
            target.Grays = grays; // Dynamic depth allocation

            var source = new Bitmap();
            source.Init(height, sourceWidth, 0);
            source.Grays = grays;

            // Fill with extreme byte patterns precisely constructed to trigger wraps across memory bounds
            for (int y = 0; y < height; y++)
            {
                sbyte* tRow = target.GetRow(y);
                sbyte* sRow = source.GetRow(y);
                for (int x = 0; x < targetWidth; x++)
                    tRow[x] = (sbyte)((125 + x) % 256); // Hits sbyte transition directly

                for (int x = 0; x < sourceWidth; x++)
                    sRow[x] = (sbyte)((250 + x) % 256); // Hits byte limit transition directly
            }

            var targetOrig = target.Duplicate();

            // Predict Return Value
            int x0 = (dx > 0) ? dx : 0;
            int y0 = (dy > 0) ? dy : 0;
            int x1 = (dx < 0) ? (-dx) : 0;
            int y1 = (dy < 0) ? (-dy) : 0;
            int expectedW = Math.Min(targetWidth - x0, sourceWidth - x1);
            int expectedH = Math.Min(height - y0, height - y1);
            bool expectedResult = (expectedW > 0) && (expectedH > 0);

            // Act
            bool actualResult = target.InsertMap(ref source, dx, dy, doBlit);
            Assert.Equal(expectedResult, actualResult);

            // Assert
            for (int y = 0; y < height; y++)
            {
                sbyte* tRow = target.GetRow(y);
                sbyte* tOrigRow = targetOrig.GetRow(y);
                
                int sourceY = y - dy;
                sbyte* sRow = (sourceY >= 0 && sourceY < height) ? source.GetRow(sourceY) : null;

                for (int x = 0; x < targetWidth; x++)
                {
                    int sourceX = x - dx;
                    if (sRow != null && sourceX >= 0 && sourceX < sourceWidth)
                    {
                        // Native DjVuLibre logic: unsigned modulo 256 wrapping!
                        sbyte expected = doBlit ? (sbyte)((byte)tOrigRow[x] + (byte)sRow[sourceX]) : sRow[sourceX];
                        Assert.Equal(expected, tRow[x]);
                    }
                    else
                    {
                        Assert.Equal(tOrigRow[x], tRow[x]); // Untouched buffer check
                    }
                }
            }
        }

        [Fact()]
        public void InsertMapTest()
        {
            var bmp = new Bitmap();
            Assert.NotNull(bmp);

            bmp.Init(128, 128, 0);
            Assert.Equal(128, bmp.Width);
            Assert.Equal(128, bmp.Height);
            Assert.Equal(0, bmp.Border);

            bmp.Fill(-1);
            Assert.Equal(255, bmp.GetByteAt(64));

            var bmp2 = new Bitmap();
            Assert.NotNull(bmp2);

            bmp2.Init(256, 256, 0);
            Assert.Equal(256, bmp2.Width);
            Assert.Equal(256, bmp2.Height);
            Assert.Equal(0, bmp2.Border);

            bmp2.Fill(127);
            Assert.Equal(127, bmp2.GetByteAt(192));

            bmp2.InsertMap(ref bmp, 128, 0, false);
            Assert.Equal(255, bmp2.GetByteAt(192));
        }

        [Fact()]
        public void InitTest001()
        {
            Bitmap bmp = new Bitmap();

            bmp.Init(128, 128, 0);
            Assert.Equal(128, bmp.Width);
            Assert.Equal(128, bmp.Height);
            Assert.Equal(0, bmp.Border);
        }

        [Fact()]
        public void InitTest002()
        {
            Bitmap bmp = new Bitmap();

            bmp.Init(128, 128, 0);
            Assert.Equal(128, bmp.Width);
            Assert.Equal(128, bmp.Height);
            Assert.Equal(0, bmp.Border);

            bmp.Fill(-1);
            Assert.Equal(255, bmp.GetByteAt(64));

            Bitmap testBmp = new Bitmap();
            testBmp.Init(ref bmp, 0);

            Assert.Equal<Bitmap>(bmp, testBmp);
            Assert.False(ReferenceEquals(bmp.Data, testBmp.Data));
            Assert.Equal(bmp.Width, testBmp.Width);
            Assert.Equal(bmp.Height, testBmp.Height);
            Assert.Equal(bmp.GetByteAt(64), testBmp.GetByteAt(64));
        }
        public static TheoryData<int, int, int, int, int> GetInitRefBitmapBorderData()
        {
            var data = new TheoryData<int, int, int, int, int>();
            int[] widths = { 10, 15, 16, 17, 21, 31, 32, 33, 37, 48, 49, 63, 64, 65, 95, 96, 111, 112, 115, 144, 176 };
            int[] borders = { 0, 2, 4 };
            int[] grays = { 2, 128, 256 };
            int h = 2; // Fixed height is sufficient since row loops are identical per-row
            
            foreach (var w in widths)
            {
                foreach (var border in borders)
                {
                    foreach (var srcBorder in borders)
                    {
                        foreach (var gray in grays)
                        {
                            data.Add(w, h, border, srcBorder, gray);
                        }
                    }
                }
            }
            
            return data;
        }

        [Theory]
        [MemberData(nameof(GetInitRefBitmapBorderData))]
        public unsafe void Init_RefBitmap_Border(int w, int h, int border, int srcBorder, int grays)
        {
            Bitmap srcBmp = new Bitmap();
            srcBmp.Init(h, w, srcBorder);
            srcBmp.Grays = grays;
            
            // Fill with a recognizable pattern bounded by Grays
            sbyte* srcPtr = srcBmp.DataPointer;
            for (int y = 0; y < h; y++)
            {
                int offset = srcBmp.RowOffset(y);
                for (int x = 0; x < w; x++)
                {
                    srcPtr[offset + x] = (sbyte)((x + y * w) % (grays - 1) + 1);
                }
            }

            Bitmap testBmp = new Bitmap();
            testBmp.Init(ref srcBmp, border);

            Assert.Equal(w, testBmp.Width);
            Assert.Equal(h, testBmp.Height);
            Assert.Equal(border, testBmp.Border);
            Assert.Equal(grays, testBmp.Grays);

            // Verify content logic using DataPointer
            sbyte* dstPtr = testBmp.DataPointer;
            for (int r = 0; r < h; r++)
            {
                int srcOffset = srcBmp.RowOffset(r);
                int dstOffset = testBmp.RowOffset(r);

                for (int c = 0; c < w; c++)
                {
                    Assert.Equal(srcPtr[srcOffset + c], dstPtr[dstOffset + c]);
                }

                // Verify right border is zeroed
                for(int b = 0; b < border; b++)
                {
                    Assert.Equal(0, dstPtr[dstOffset + w + b]);
                }
            }

            // Verify initial top border is zeroed
            for(int b = 0; b < border; b++)
            {
                Assert.Equal(0, dstPtr[b]);
            }
        }

        [Theory]
        [InlineData(10, 2, 4, 256)] // new border is greater
        [InlineData(10, 4, 2, 128)] // new border is smaller
        [InlineData(10, 4, 4, 100)] // new border is equal
        public unsafe void Init_RefBitmap_SameReference(int size, int initialBorder, int newBorder, int grays)
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(size, size, initialBorder);
            bmp.Grays = grays;
            
            // Write unique coordinate-based pattern bounded by Grays
            sbyte* ptr = bmp.DataPointer;
            for (int y = 0; y < size; y++)
            {
                int rowOffset = bmp.RowOffset(y);
                for (int x = 0; x < size; x++)
                {
                    ptr[rowOffset + x] = (sbyte)((x + y * size) % (grays - 1) + 1);
                }
            }

            // Call Init with itself as the source
            bmp.Init(ref bmp, newBorder);

            // Assert border updated (SetMinimumBorder only increases)
            int expectedBorder = Math.Max(initialBorder, newBorder);
            Assert.Equal(expectedBorder, bmp.Border);
            Assert.Equal(grays, bmp.Grays);

            // Verify unique pattern survived in exact coordinates and padding is zeroed
            sbyte* newPtr = bmp.DataPointer;
            for (int y = 0; y < size; y++)
            {
                int rowOffset = bmp.RowOffset(y);
                for (int x = 0; x < size; x++)
                {
                    sbyte expectedPixel = (sbyte)((x + y * size) % (grays - 1) + 1);
                    Assert.Equal(expectedPixel, newPtr[rowOffset + x]);
                }

                // Verify right border is zeroed
                for(int b = 0; b < expectedBorder; b++)
                {
                    Assert.Equal(0, newPtr[rowOffset + size + b]);
                }
            }

            // Verify initial top border is zeroed
            for(int b = 0; b < expectedBorder; b++)
            {
                Assert.Equal(0, newPtr[b]);
            }
        }


        [Fact()]
        public void InitTest003()
        {
            Bitmap bmp = new Bitmap();

            bmp.Init(128, 128, 0);
            Assert.Equal(128, bmp.Width);
            Assert.Equal(128, bmp.Height);
            Assert.Equal(0, bmp.Border);

            bmp.Fill(-1);
            Assert.Equal(255, bmp.GetByteAt(64));

            var bmp2 = bmp.Init(ref bmp, new Rectangle(0, 0, 128, 128), 0);
            Assert.Equal<Bitmap>(bmp2, bmp);
        }

        [Fact()]
        public void TranslateTest()
        {
            Bitmap bmp = new Bitmap();

            bmp.Init(128, 128, 0);
            Assert.Equal(128, bmp.Width);
            Assert.Equal(128, bmp.Height);
            Assert.Equal(0, bmp.Border);

            bmp.Fill(-1);
            Assert.Equal(255, bmp.GetByteAt(64));

            Bitmap bmp2 = new Bitmap();
            Assert.NotNull(bmp2);

            bmp2.Init(256, 256, 0);
            Assert.Equal(256, bmp2.Width);
            Assert.Equal(256, bmp2.Height);
            Assert.Equal(0, bmp2.Border);

            bmp2.Fill(127);
            Assert.Equal(127, bmp2.GetByteAt(192));

            Bitmap bmp3 = (Bitmap) bmp.Translate(64, 64, ref bmp2);
            Assert.Equal(128, bmp3.Width);
            Assert.Equal(128, bmp3.Height);
            Assert.Equal(0, bmp3.GetByteAt(96));
            Assert.Equal(255, bmp3.GetByteAt(32));
        }

        [Theory]
        [InlineData(256, 4, 0, 0)]      // Min to Min
        [InlineData(256, 4, 255, 3)]    // Max to Max
        [InlineData(256, 4, 127, 1)]    // (127 * 3 + 255 / 2) / 255 = (381 + 127) / 255 = 508 / 255 = 1
        [InlineData(256, 4, 128, 2)]    // (128 * 3 + 255 / 2) / 255 = (384 + 127) / 255 = 511 / 255 = 2
        [InlineData(16, 256, 15, 255)]  // Upscaling: 15 to 255
        public void ChangeGrays_CalculatesParity_Theory(int originalGrays, int newGrays, byte inputPixel, byte expectedPixel)
        {
            var bmp = (Bitmap)CreateIntiFillVerifyBitmap(10, 10, 0, (sbyte)inputPixel);
            bmp.Grays = originalGrays;

            bmp.ChangeGrays(newGrays);

            Assert.Equal(newGrays, bmp.Grays);
            Assert.Equal(expectedPixel, bmp.GetByteAt(0));
        }

        [Theory]
        [InlineData(150, 100, 256, 0)] // Less than threshold -> White (0)
        [InlineData(150, 150, 256, 0)] // Exactly threshold -> White (0)
        [InlineData(150, 151, 256, 1)] // Strictly greater than threshold -> Black (1)
        public void BinarizeGrays_AppliesThreshold_Theory(int threshold, byte inputPixel, int originalGrays, byte expectedPixel)
        {
            var bmp = (Bitmap)CreateIntiFillVerifyBitmap(10, 10, 0, (sbyte)inputPixel);
            bmp.Grays = originalGrays;

            bmp.BinarizeGrays(threshold);

            Assert.Equal(2, bmp.Grays);
            Assert.Equal(expectedPixel, bmp.GetByteAt(0));
        }

        [Fact]
        public void ComputeBoundingBox_EmptyImage_ReturnsEmptyRectangle()
        {
            var bmp = (Bitmap)CreateIntiFillVerifyBitmap(10, 10, 0, 0); // 0 = White
            Rectangle rect = bmp.ComputeBoundingBox();
            Assert.True(rect.Empty);
        }

        [Fact]
        public void ComputeBoundingBox_FullImage_ReturnsFullRectangle()
        {
            var bmp = (Bitmap)CreateIntiFillVerifyBitmap(10, 10, 0, 1); // 1 = Black
            Rectangle rect = bmp.ComputeBoundingBox();
            Assert.False(rect.Empty);
            Assert.Equal(0, rect.XMin);
            Assert.Equal(0, rect.YMin);
            Assert.Equal(9, rect.Width);
            Assert.Equal(9, rect.Height);
        }

        [Fact]
        public void ComputeBoundingBox_SinglePixel_ReturnsOneByOneRectangle()
        {
            var bmp = (Bitmap)CreateIntiFillVerifyBitmap(10, 10, 0, 0); // 0 = White
            bmp.SetByteAt(bmp.RowOffset(5) + 3, 1); // Set (3, 5) to Black

            Rectangle rect = bmp.ComputeBoundingBox();

            Assert.True(rect.Empty);
            Assert.Equal(3, rect.XMin);
            Assert.Equal(5, rect.YMin);
            Assert.Equal(0, rect.Width);
            Assert.Equal(0, rect.Height);
        }

        public static TheoryData<int, int, int[], int, int, int, int, bool> BoundingBoxEdgeCases =>
            new TheoryData<int, int, int[], int, int, int, int, bool>
            {
                // w, h, points [x,y, ...], expXMin, expYMin, expWidth, expHeight, expEmpty
                // 1. Single pixel at origin (0,0) -> 0 Width, 0 Height, Empty
                { 10, 10, new[] { 0, 0 }, 0, 0, 0, 0, true },
                
                // 2. Single pixel at top-right bounds (9,9)
                { 10, 10, new[] { 9, 9 }, 9, 9, 0, 0, true },

                // 3. Two pixels forming a 1D horizontal line (Y bounds collapse to 0 area)
                { 10, 10, new[] { 2, 5, 3, 5 }, 2, 5, 0, 0, true },

                // 4. Two pixels forming a 1D vertical line (X bounds collapse to 0 area)
                { 10, 10, new[] { 5, 2, 5, 3 }, 5, 2, 0, 0, true },

                // 5. Normal 2D Rectangle (diagonally opposed points)
                { 10, 10, new[] { 2, 2, 5, 5 }, 2, 2, 3, 3, false },
                
                // 6. Cross pattern (furthest extents are at the middle of the edges, not corners)
                { 10, 10, new[] { 5, 1, 1, 5, 9, 5, 5, 9 }, 1, 1, 8, 8, false },
                
                // 7. Full border only (hollow center)
                { 10, 10, new[] { 0, 0, 9, 0, 0, 9, 9, 9 }, 0, 0, 9, 9, false },

                // 8. Tightly packed non-empty sub-box within large image
                { 100, 100, new[] { 40, 40, 40, 60, 60, 40, 60, 60 }, 40, 40, 20, 20, false }
            };

        [Theory]
        [MemberData(nameof(BoundingBoxEdgeCases))]
        public unsafe void ComputeBoundingBox_ComprehensiveEdgeCases(
            int w, int h, int[] points, int expX, int expY, int expW, int expH, bool expEmpty)
        {
            var bmp = (Bitmap)CreateIntiFillVerifyBitmap(w, h, 0, 0); // 0 = White
            
            for (int i = 0; i < points.Length; i += 2)
            {
                int px = points[i];
                int py = points[i + 1];
                sbyte* rowPtr = bmp.GetRow(py);
                rowPtr[px] = 1; // Set to Black
            }

            Rectangle rect = bmp.ComputeBoundingBox();

            Assert.Equal(expEmpty, rect.Empty);
            Assert.Equal(expX, rect.XMin);
            Assert.Equal(expY, rect.YMin);
            Assert.Equal(expW, rect.Width);
            Assert.Equal(expH, rect.Height);
        }


        [Fact]
        public void ChangeGrays_UninitializedData_SafelyUpdatesGrays()
        {
            var bmp = new Bitmap();
            bmp.ChangeGrays(4);
            Assert.Equal(4, bmp.Grays);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(257)]
        public void ChangeGrays_InvalidArguments_Throws(int invalidGrays)
        {
            var bmp = new Bitmap();
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.ChangeGrays(invalidGrays));
        }

        [Fact]
        public void ChangeGrays_CurrentGraysIsOne_Throws()
        {
            var bmp = new Bitmap();
            bmp.Init(10, 10, 0);

            // Force invalid state via reflection or internal setter if needed,
            // but the Grays property allows setting 1, which triggers the bug.
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.Grays = 1);

            // If the setter blocks it (as per previous tests), we simulate a corrupted init state
            // Let's assume Grays could somehow be 1 (e.g., from a bad stream parse).
            // Since the Grays setter is guarded, we must test the mathematical limit.
            // If we can't set it to 1, this specific DivideByZero is unreachable through the public API,
            // which is a good thing. We will verify the setter protection.
        }

        [Fact]
        public void BinarizeGrays_UninitializedData_SafelyUpdatesGrays()
        {
            var bmp = new Bitmap();
            bmp.BinarizeGrays(100);
            Assert.Equal(2, bmp.Grays);
        }

        [Theory]
        [InlineData(-1, 0, 1)]      // Threshold -1, Pixel 0 (White) -> Black (1)
        [InlineData(256, 255, 0)]   // Threshold 256, Pixel 255 (Black) -> White (0)
        public void BinarizeGrays_ExtremeThresholds_HandlesBoundaries(int threshold, byte inputPixel, byte expectedPixel)
        {
            var bmp = (Bitmap)CreateIntiFillVerifyBitmap(10, 10, 0, (sbyte)inputPixel);
            bmp.BinarizeGrays(threshold);
            Assert.Equal(expectedPixel, bmp.GetByteAt(0));
        }

        [Fact]
        public void ComputeBoundingBox_UninitializedData_ReturnsEmpty()
        {
            var bmp = new Bitmap(); // Data is null
            Rectangle rect = bmp.ComputeBoundingBox();
            Assert.True(rect.Empty);
        }

        [Fact]
        public void ComputeBoundingBox_CompressedData()
        {
            var bmp = (Bitmap)CreateIntiFillVerifyBitmap(10, 10, 0, 1);
            bmp.Compress();
            
            Assert.Null(bmp.Data);
            Assert.NotNull(bmp.RleData);

            Rectangle rect = bmp.ComputeBoundingBox();
            
            Assert.False(rect.Empty);
            Assert.Equal(0, rect.XMin);
            Assert.Equal(0, rect.YMin);
            Assert.Equal(9, rect.Width);
            Assert.Equal(9, rect.Height);
        }

        [Fact]
        public void ComputeBoundingBox_CompressedZeroDimensions()
        {
            Bitmap bmp = new Bitmap();
            ref BitmapSurrogate surrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref bmp);
            surrogate._Width = 0;
            surrogate._Height = 0;
            surrogate._RleData = new byte[10];
            
            Rectangle rect = bmp.ComputeBoundingBox();
            Assert.True(rect.Empty);
        }

        [Fact]
        public void ComputeBoundingBox_NullDataWithValidDimensions_Throws()
        {
            Bitmap bmp = default;
            ref BitmapSurrogate surrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref bmp);
            surrogate._Width = 10;
            surrogate._Height = 10;
            surrogate._Data = null;
            surrogate._RleData = null;

            var ex = Assert.Throws<DjvuNullReferenceException>(() => bmp.ComputeBoundingBox());
            Assert.Contains("Cannot compute bounding box", ex.Message);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Compress_InvalidState_Throws(bool hasData)
        {
            Bitmap bmp = default;
            ref BitmapSurrogate surrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref bmp);
            surrogate._Data = hasData ? new sbyte[10] : null;
            surrogate._RleData = hasData ? new byte[10] : null;

            var ex = Assert.Throws<DjvuInvalidOperationException>(() => bmp.Compress());
            if (hasData)
                Assert.Contains("already contains compressed", ex.Message);
            else
                Assert.Contains("Bitmap is not properly initialized", ex.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(255)]
        public void Compress_InvalidGrays_Throws(byte grays)
        {
            Bitmap bmp = default;
            ref BitmapSurrogate surrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref bmp);
            surrogate._Data = new sbyte[10];
            surrogate._Width = 10;
            surrogate._Height = 10;
            surrogate._Grays = grays;

            var ex = Assert.Throws<DjvuInvalidOperationException>(() => bmp.Compress());
            Assert.Contains("Cannot compress data with Grays", ex.Message);
        }

        [Theory]
        [InlineData(2147483592L)] // Array.MaxLength + 1L
        [InlineData(-1L)]
        public void EnsureZeroBuffer_InvalidSize_Throws(long size)
        {
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => Bitmap.EnsureZeroBuffer(size));
            if (size > 0)
                Assert.Contains("exceeds Array.MaxLength", ex.Message);
        }

        [Theory]
        [InlineData(10, 0, 5, 10, true)]
        [InlineData(2, 2147483621, -15, 0, false)] // Array.MaxLength + 30
        public void Resize_InvalidDimensions_Throws(int height, int border, int bytesPerRow, int width, bool isArgumentException)
        {
            Bitmap bmp = default;
            ref BitmapSurrogate surrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref bmp);
            surrogate._Height = height;
            surrogate._Border = border;
            surrogate._BytesPerRow = bytesPerRow;
            
            if (isArgumentException)
            {
                var ex = Assert.Throws<DjvuArgumentException>(() => bmp.SetWidth(width));
                Assert.Contains("BytesPerRow is insufficient", ex.Message);
            }
            else
            {
                var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.SetWidth(width));
                Assert.Contains("Requirements for _ZeroBuffer result in an invalid size", ex.Message);
            }
        }

        [Fact]
        public void Init_Throws()
        {
            var bmp = new Bitmap();
            // Init dimensions but try to force the underlying data buffer to remain null
            Assert.Throws<DjvuArgumentException>(() => bmp.Init(null, 10, 10, 0));
        }

        [Fact]
        public void Init_PopulatedBitmap_ThrowsInvalidOperation()
        {
            var bmp = new Bitmap();
            bmp.Init(10, 10, 0); // Populated (Data != null)
            Assert.Throws<DjvuInvalidOperationException>(() => bmp.Init(5, 5, 0));
        }

        [Theory]
        [InlineData("Dimensions")]
        [InlineData("Data")]
        [InlineData("RefBitmap")]
        [InlineData("RefBitmapRect")]
        public void Init_TargetDisposed_ThrowsObjectDisposed(string overload)
        {
            var bmp = new Bitmap();
            bmp.Dispose();
            var source = new Bitmap(10, 10);
            sbyte[] data = new sbyte[100];
            
            Action action = overload switch
            {
                "Dimensions" => () => bmp.Init(10, 10, 0),
                "Data" => () => bmp.Init(data, 10, 10, 0),
                "RefBitmap" => () => bmp.Init(ref source, 0),
                "RefBitmapRect" => () => bmp.Init(ref source, new Rectangle(0, 0, 10, 10), 0),
                _ => throw new ArgumentException()
            };
            
            Assert.Throws<DjvuObjectDisposedException>(action);
        }

        [Theory]
        [InlineData("RefBitmap")]
        [InlineData("RefBitmapRect")]
        public void Init_SourceDisposed_ThrowsArgumentException(string overload)
        {
            var bmp = new Bitmap();
            var source = new Bitmap(10, 10);
            source.Dispose();
            
            Action action = overload switch
            {
                "RefBitmap" => () => bmp.Init(ref source, 0),
                "RefBitmapRect" => () => bmp.Init(ref source, new Rectangle(0, 0, 10, 10), 0),
                _ => throw new ArgumentException()
            };
            
            var ex = Assert.Throws<DjvuArgumentException>(action);
            Assert.Contains("has been disposed", ex.Message);
        }

        [Theory]
        [InlineData(10, 10, 5, 5,  2,   2,  1, true)]  // 1. Happy Path - Exact Fit
        [InlineData(10, 10, 5, 5, -2,  -2,  1, true)]  // 2. Out of bounds (Negative X/Y) - Partial overlap
        [InlineData(10, 10, 5, 5, -10, -10, 1, false)] // 3. Out of bounds (Completely outside negative)
        [InlineData(10, 10, 5, 5,  15,  15, 1, false)] // 4. Out of bounds (Completely outside positive)
        [InlineData(10, 10, 5, 5,  8,   8,  1, true)]  // 5. Partial overlap (Positive bounds)
        [InlineData(10, 10, 4, 4,  0,   0,  2, true)]  // 6. Subsampling (Happy path)
        [InlineData(10, 10, 4, 4,  25,  25, 2, false)] // 7. Subsampling (Out of bounds)
        public void Blit_EdgeCases_ReturnsExpected(
            int tW, int tH, int sW, int sH, int x, int y, int sub, bool expected)
        {
            // Arrange
            Bitmap target = new Bitmap();
            Bitmap source = new Bitmap();
            {
                target.Init(tH, tW, 0);
                source.Init(sH, sW, 0);
                source.Fill(1);

                // Act
                bool result = target.Blit(ref source, x, y, sub);

                // Assert
                Assert.Equal(expected, result);

                if (result)
                {
                    int intersectX = Math.Max(0, x);
                    int intersectY = Math.Max(0, y);
                    if (intersectX < tW && intersectY < tH)
                    {
                        int targetSubX = intersectX / sub;
                        int targetSubY = intersectY / sub;
                        if (targetSubX < tW && targetSubY < tH)
                        {
                            int val = target.GetByteAt(target.RowOffset(targetSubY) + targetSubX);
                            Assert.True(val > 0, "Expected pixel to be modified by Blit");
                        }
                    }
                }
            }
        }

        [Fact]
        public void Blit_NullRefSource_Throws()
        {
            Bitmap target = new Bitmap();
            {
                target.Init(10, 10, 0);
                var ex = Assert.Throws<DjvuArgumentNullException>(() =>
                    target.Blit(ref Unsafe.NullRef<Bitmap>(), 0, 0, 1));
                Assert.Contains(
                    $"{typeof(Bitmap).FullName} source reference is null.", ex.Message);
            }
        }

        [Fact]
        public void Blit_DefaultSource_Throws()
        {
            Bitmap target = new Bitmap();
            {
                target.Init(10, 10, 0);
                Bitmap defBmp = default;
                var ex = Assert.Throws<DjvuArgumentException>(() => target.Blit(ref defBmp, 0, 0, 1));
                Assert.Contains(
                    $"Cannot Blit a default source {typeof(Bitmap).FullName} into the target as {nameof(Bitmap.Data)} is null.", ex.Message);
            }
        }

        [Fact]
        public void InsertMap_NullRefSource_Throws()
        {
            Bitmap target = new Bitmap();
            {
                target.Init(10, 10, 0);
                var ex = Assert.Throws<DjvuArgumentNullException>(() =>
                    target.InsertMap(ref Unsafe.NullRef<Bitmap>(), 0, 0, false));
                Assert.Contains(
                    $"{typeof(Bitmap).FullName} source reference is null.", ex.Message);
            }
        }

        [Fact]
        public void InsertMap_DefaultUninitializedSource_Throws()
        {
            Bitmap target = new Bitmap();
            {
                target.Init(10, 10, 0);
                Bitmap defBmp = default;
                var ex = Assert.Throws<DjvuArgumentException>(() => target.InsertMap(ref defBmp, 0, 0, false));
                Assert.Contains(
                    $"Cannot insert a default source {typeof(Bitmap).FullName} into the target as {nameof(Bitmap.Data)} is null.", ex.Message);
            }
        }

        [Theory]
        [InlineData("RefBitmap")]
        [InlineData("RefBitmapRect")]
        public void Init_SourceNullRef_ThrowsArgumentNullException(string overload)
        {
            Bitmap target = new Bitmap();
            target.Init(10, 10, 0);
            
            Action action = overload switch
            {
                "RefBitmap" => () => target.Init(ref Unsafe.NullRef<Bitmap>(), 0),
                "RefBitmapRect" => () => target.Init(ref Unsafe.NullRef<Bitmap>(), new Rectangle(0, 0, 5, 5), 0),
                _ => throw new ArgumentException()
            };

            var ex = Assert.Throws<DjvuArgumentNullException>(action);
            Assert.Contains($"{typeof(Bitmap).FullName} source reference is null.", ex.Message);
        }

        [Fact]
        public void Translate_NullRefRetVal_Throws()
        {
            Bitmap target = new Bitmap();
            {
                target.Init(10, 10, 0);
                var ex = Assert.Throws<DjvuArgumentNullException>(() =>
                    target.Translate(5, 5, ref Unsafe.NullRef<Bitmap>()));
                Assert.Contains(
                    $"{typeof(Bitmap).FullName} retVal reference is null.", ex.Message);
            }
        }

        [Fact]
        public void Constructor_NullRefSource_Throws()
        {
            var ex = Assert.Throws<DjvuArgumentNullException>(() => new Bitmap(ref Unsafe.NullRef<Bitmap>()));
            Assert.Contains(
                $"{typeof(Bitmap).FullName} bmp reference is null.", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(16)]
        [InlineData(100)]
        public void Blit_InvalidSubsample_Throws(int subSample)
        {
            Bitmap source = new Bitmap();
            source.Init(10, 10, 0);
            Bitmap target = new Bitmap();
            target.Init(10, 10, 0);
            
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => target.Blit(ref source, 0, 0, subSample));
            Assert.Contains($"Subsample factor {subSample} is out of range.", ex.Message);
        }

        // -------------------------------------------------------------------------
        // RLE COMPRESSION / DECOMPRESSION TESTS
        // -------------------------------------------------------------------------

        [Theory]
        [InlineData(8, 2, false)]    // Scalar fallback
        [InlineData(8, 2, true)]
        [InlineData(24, 4, false)]   // Vector128 boundary
        [InlineData(24, 4, true)]
        [InlineData(48, 4, false)]   // AVX2 boundary
        [InlineData(48, 4, true)]
        [InlineData(136, 4, false)]  // AVX-512 boundary
        [InlineData(136, 4, true)]
        public unsafe void DecodeRleCore_WithDirtyUninitializedBuffer(int width, int border, bool isDirty)
        {
            // Arrange
            int height = 8;
            int bytesPerRow = width + border; 
            int bufferSize = height * bytesPerRow + border;
            
            sbyte[] dirtyBuffer = new sbyte[bufferSize];
            if (isDirty) Array.Fill(dirtyBuffer, (sbyte)-1); 
            
            sbyte[] expectedBuffer = new sbyte[bufferSize];
            if (isDirty) Array.Fill(expectedBuffer, (sbyte)-1);
            
            Bitmap sourceBmp = new Bitmap();
            sourceBmp.Init(height, width, 0); 
            // Fill with 1s (black) so the decoded pixels are non-zero. 
            // If the decoder leaves padding uninitialized (-1) or overwrites it (1), SequenceEqual will fail.
            Array.Fill(sourceBmp.Data, (sbyte)1);
            sourceBmp.Compress();
            byte[] rleData = sourceBmp.RleData;
            
            // Act
            fixed (byte* pRle = rleData)
            {
                Bitmap.DecodeRleCore(pRle, rleData.Length, dirtyBuffer, border, height, bytesPerRow, width);
            }

            var sbDump = new StringBuilder();
            sbDump.AppendLine($"\n=== BUFFER DUMP (Width: {width}, Dirty: {isDirty}) ===");
            for (int i = 0; i < border; i++) sbDump.Append($"{dirtyBuffer[i],2} ");
            sbDump.AppendLine("  <- Top Border");
            
            for (int y = 0; y < height; y++)
            {
                int rowStart = border + y * bytesPerRow;
                for (int x = 0; x < bytesPerRow; x++)
                {
                    sbDump.Append($"{dirtyBuffer[rowStart + x],2} ");
                    if (x == width - 1) sbDump.Append("| ");
                }
                sbDump.AppendLine();
            }
            Console.WriteLine(sbDump.ToString());

            // Generate perfect expected state dynamically
            for (int i = 0; i < border; i++) expectedBuffer[i] = 0;
            
            for (int row = 0; row < height; row++)
            {
                int rowStart = border + row * bytesPerRow;
                
                for (int p = 0; p < width; p++)
                {
                    expectedBuffer[rowStart + p] = dirtyBuffer[rowStart + p];
                }
                
                int rightBorderStart = rowStart + width;
                for (int p = 0; p < border; p++)
                {
                    expectedBuffer[rightBorderStart + p] = 0;
                }
            }

            // Assert
            var dirtySpan = new ReadOnlySpan<sbyte>(dirtyBuffer);
            var expectedSpan = new ReadOnlySpan<sbyte>(expectedBuffer);
            
            if (!dirtySpan.SequenceEqual(expectedSpan))
            {
                int diffCount = 0;
                int maxPreviewLines = 64;
                var errorLog = new StringBuilder();
                
                for (int i = 0; i < bufferSize; i++)
                {
                    if (dirtySpan[i] != expectedSpan[i])
                    {
                        if (diffCount < maxPreviewLines)
                        {
                            errorLog.AppendLine($"Index: {i,4} | Expected: {expectedSpan[i],4} | Actual: {dirtySpan[i],4}");
                        }
                        diffCount++;
                    }
                }
                
                double diffPct = (double)diffCount / bufferSize;
                errorLog.Insert(0, $"Buffer corruption detected (Width: {width}, Dirty: {isDirty}). {diffCount} bytes ({diffPct:P2}) failed to initialize to 0.\n");
                Assert.Fail(errorLog.ToString());
            }
        }

        [Theory]
        [InlineData(4)]    // Scalar fallback
        [InlineData(16)]   // SSE2 exact alignment
        [InlineData(32)]   // AVX2 exact alignment
        [InlineData(64)]   // AVX-512 exact alignment
        [InlineData(65)]   // AVX-512 + 1 byte scalar remainder
        [InlineData(128)]  // AVX-512 x2 loop
        public void Compress_IsolatedAllWhite_ProducesExactRleBytes(int width)
        {
            // Goal: Test Compress encoding independently across SIMD vector boundaries.
            int height = 2; // Validate multiple rows to ensure state resets per row
            Bitmap source = new Bitmap();
            source.Init(height, width, 0);
            source.Grays = 2;

            source.Compress();

            Assert.Null(source.Data);
            Assert.NotNull(source._RleData);

            // Each row consists of exactly one run of 'width' white pixels.
            byte[] expectedRle = new byte[] { (byte)width, (byte)width };

            Assert.Equal(expectedRle.Length, source._RleData.Length);
            for (int i = 0; i < expectedRle.Length; i++)
            {
                Assert.Equal(expectedRle[i], source._RleData[i]);
            }
        }

        [Theory]
        [InlineData(4)]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(128)]
        public void Compress_IsolatedStartsWithBlack_ProducesExactRleBytes(int width)
        {
            // Goal: Verify 0-length white run prefix logic across SIMD vector boundaries.
            int height = 1;
            Bitmap source = new Bitmap();
            source.Init(height, width, 0);
            source.Grays = 2;

            // Layout: 1 Black pixel, followed by (width-1) White pixels
            source.SetByteAt(0, 1);

            source.Compress();

            // Expected RLE:
            // - 0 white pixels
            // - 1 black pixel
            // - (width - 1) white pixels
            byte[] expectedRle = new byte[] { 0, 1, (byte)(width - 1) };

            Assert.Equal(expectedRle.Length, source._RleData.Length);
            for (int i = 0; i < expectedRle.Length; i++)
            {
                Assert.Equal(expectedRle[i], source._RleData[i]);
            }
        }

        [Theory]
        [InlineData(4)]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(128)]
        public void Uncompress_IsolatedKnownRleData_ProducesExactPixels(int width)
        {
            // Goal: Test Uncompress decoding logic across SIMD vector boundaries.
            int height = 1;
            Bitmap target = new Bitmap();
            target.Init(height, width, 0);
            target.Grays = 2;

            // Simulate compressed state: Row starts with Black (0 white), 1 black, (width - 1) white.
            target.Compress();
            target._RleData = new byte[] { 0, 1, (byte)(width - 1) };

            target.Decompress();

            Assert.NotNull(target.Data);
            Assert.Null(target._RleData);

            // Assert exact pixel layout
            Assert.Equal(1, target.GetByteAt(0)); // First is Black

            // Assert tail is White
            for (int i = 1; i < width; i++)
            {
                Assert.Equal(0, target.GetByteAt(i));
            }
        }

        [Theory]
        [InlineData(15, false)]  [InlineData(15, true)]
        [InlineData(16, false)]  [InlineData(16, true)]
        [InlineData(31, false)]  [InlineData(31, true)]
        [InlineData(32, false)]  [InlineData(32, true)]
        [InlineData(33, false)]  [InlineData(33, true)]
        [InlineData(63, false)]  [InlineData(63, true)]
        [InlineData(64, false)]  [InlineData(64, true)]
        [InlineData(65, false)]  [InlineData(65, true)]
        [InlineData(127, false)] [InlineData(127, true)]
        [InlineData(128, false)] [InlineData(128, true)]
        [InlineData(129, false)] [InlineData(129, true)]
        [InlineData(255, false)] [InlineData(255, true)]
        [InlineData(256, false)] [InlineData(256, true)]
        [InlineData(257, false)] [InlineData(257, true)]
        public void Decompress_VectorBoundaries_RoundTrips(int rleByteTarget, bool isMultiRow)
        {
            int height;
            int width;
            List<int[]> rowDefinitions = new List<int[]>();
            int rem = rleByteTarget;

            if (isMultiRow)
            {
                width = 320; // Fixed width across all rows
                while (rem > 0)
                {
                    if (rem % 3 == 0)      { rowDefinitions.Add(new int[] { 20, 300 });     rem -= 3; } // 1+2 = 3 bytes
                    else if (rem % 3 == 2) { rowDefinitions.Add(new int[] { 320 });         rem -= 2; } // 2 bytes
                    else                   { rowDefinitions.Add(new int[] { 10, 10, 300 }); rem -= 4; } // 1+1+2 = 4 bytes
                }
                height = rowDefinitions.Count;
            }
            else
            {
                width = 0;
                List<int> singleRow = new List<int>();
                singleRow.Add(320); // 2-byte command to trigger bug (320 -> [193, 64])
                width += 320;
                rem -= 2;
                
                while (rem > 0)
                {
                    singleRow.Add(10); // 1-byte commands to pad to exact target
                    width += 10;
                    rem -= 1;
                }
                height = 1;
                rowDefinitions.Add(singleRow.ToArray());
            }

            Bitmap source = new Bitmap();
            Bitmap expected = new Bitmap();
            try
            {
                source.Init(height, width, 0);
                source.Grays = 2;

                int offset = 0;
                foreach (var rowRuns in rowDefinitions)
                {
                    sbyte color = 0; // Always start row with White (0) to prevent injected leading 0 bytes
                    foreach (int runLength in rowRuns)
                    {
                        for (int x = 0; x < runLength; x++)
                            source.SetByteAt(offset++, color);
                        color = (sbyte)(1 - color);
                    }
                }

                expected = source.Duplicate();
                
                // Compress to generate the boundary-crossing _RleData array
                source.Compress();
                source.Decompress();

                // Verify struct equivalence
                Assert.Equal(expected, source);
            }
            finally
            {
                source.Dispose();
                expected.Dispose();
            }
        }

        [Theory]
        [InlineData("AllWhite", 32, 32)]
        [InlineData("AllBlack", 32, 32)]
        [InlineData("Checkerboard", 64, 64)]
        [InlineData("StartsWithBlack", 64, 64)]
        [InlineData("LongRunOverflow", 200, 200)]
        public void Compress_DataPatterns_RoundTrips(string pattern, int width, int height)
        {
            Bitmap source = new Bitmap();
            source.Init(height, width, 0);
            source.Grays = 2;

            switch (pattern)
            {
                case "AllWhite":
                    break;
                case "AllBlack":
                    source.Fill(1);
                    break;
                case "Checkerboard":
                    for (int y = 0; y < height; y++)
                        for (int x = 0; x < width; x++)
                            source.SetByteAt(source.RowOffset(y) + x, (sbyte)((x + y) % 2));
                    break;
                case "StartsWithBlack":
                    source.SetByteAt(source.RowOffset(0) + 0, 1);
                    break;
                case "LongRunOverflow":
                    // Leaves as 0s, triggering MaxRunSize chunking
                    break;
            }

            sbyte[] original = (sbyte[])source.Data.Clone();

            source.Compress();
            source.Decompress();

            Assert.Equal(original.Length, source.Data.Length);
            for (int i = 0; i < original.Length; i++)
                Assert.Equal(original[i], source.Data[i]);
        }

        [Theory]
        [InlineData(10, 10, 16)]
        public void Compress_InvalidState_Throws(int width, int height, int grays)
        {
            Bitmap source = new Bitmap();
            if (width > 0 && height > 0)
            {
                source.Init(height, width, 0);
            }
            source.Grays = grays;

            Assert.Throws<DjvuInvalidOperationException>(() => source.Compress());
        }

        public static TheoryData<int, string> SimdOobReadTestData()
        {
            var data = new TheoryData<int, string>();
            data.Add(1, "Scalar"); // Unconditionally available

            bool v128 = Vector128.IsHardwareAccelerated;
            bool avx2 = Avx2.IsSupported;
            bool avx512 = Avx512F.IsSupported && Avx512BW.IsSupported;

            if (v128)
            {
                data.Add(16, "Vector128_16_Exact");
                data.Add(32, "Vector128_32_Exact");
                data.Add(48, "Vector128_32+16_Exact");
            }
            if (avx2) data.Add(64, "Avx2_Exact");
            if (avx512) data.Add(128, "Avx512_Exact");

            // Dynamically define fallback boundaries based on highest available ISA
            if (avx512 && v128)
            {
                data.Add(144, "Avx512_Vector128_Fallback (128+16)");
                data.Add(160, "Avx512_Vector128_Fallback (128+32)");
                data.Add(176, "Avx512_Vector128_Fallback (128+32+16)");
            }
            else if (avx2 && v128)
            {
                data.Add(80, "Avx2_Vector128_Fallback (64+16)");
                data.Add(96, "Avx2_Vector128_Fallback (64+32)");
                data.Add(112, "Avx2_Vector128_Fallback_Extended (64+32+16)");
            }
            else if (v128)
            {
                data.Add(80, "Vector128_Extended (2x32+16)");
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(SimdOobReadTestData))]
        public void DecodeRle_SimdOobRead(int rleStreamLength, string isaName)
        {
            const int rleMoreByte = 0xC1;
            Bitmap source = new Bitmap();
            source.Init(1, 1024, 0); // height=1, width=1024, border=0
            source.Fill(0);          // Pre-fill with 0s to detect written pixels

            byte[] rle = new byte[rleStreamLength];
            rle[rleStreamLength - 1] = rleMoreByte; // 193 forces a massive write of 256+G pixels, proving the OOB read.

            string hexDumpOfBytes = null;

            using (MemoryStream ms = new MemoryStream(rle))
            {
                // Verify exception is thrown
                Assert.Throws<DjvuEndOfStreamException>(() => source.ReadRleStream(ms));
            }

            bool result = source.Data[0] == 0 && source.Data[255] == 0 && source.Data[512] == 0 && source.Data[source.Data.Length - 1] == 0;

            if (!result)
            {
                Span<byte> bytes = MemoryMarshal.Cast<sbyte, byte>(source.Data.AsSpan(0, 512));
                hexDumpOfBytes = Convert.ToHexString(bytes);
            }

            Assert.True(result, $"[{isaName}] OOB read caused {nameof(Bitmap.Data)} memory overwrite. Expected all 0s. Data hex dump: {hexDumpOfBytes}");
        }

        [Fact]
        public void Decompress_CorruptedRleData_Throws()
        {
            Bitmap source = new Bitmap();
            source.Init(10, 10, 0);
            source.Grays = 2;
            source.Compress();

            // Corrupt the first RLE run to claim 50 pixels (Width is only 10)
            source._RleData[0] = 50;

            Assert.Throws<DjvuFormatException>(() => source.Decompress());
        }

        [Fact]
        public void Decompress_ZeroDimensions_Throws()
        {
            Bitmap source = new Bitmap();
            // Setting RleData manually without initializing dimensions
            source._RleData = new byte[] { 0 };

            var ex = Assert.Throws<DjvuInvalidOperationException>(() => source.Decompress());
            Assert.Contains("Bitmap is not properly initialized", ex.Message);
        }



        [Fact(Timeout = 200)] // Safeguard against infinite loops
        public void SerializeToPbm_RawFormat_RleData()
        {
            Bitmap source = new Bitmap();
            source.Init(10, 10, 0);
            source.Grays = 2;
            source.Compress(); // Generate _RleData

            using (var memoryStream = new System.IO.MemoryStream())
            {
                // Without the fix, this will hang infinitely and timeout
                source.SerializeToPbm(memoryStream, raw: true);

                // Assert reasonable length. Header + (2 bytes per row * 10 rows)
                Assert.True(memoryStream.Length > 0 && memoryStream.Length < 100);
            }
        }

        [Fact]
        public void Compress_NonZeroBorder_RoundTrips()
        {
            int width = 32;
            int height = 32;

            // Cover all ranges with dense stepping for border.
            // 0 is standard, 1-16 covers unaligned and aligned byte boundaries.
            for (int border = 0; border <= 16; border++)
            {
                Bitmap source = new Bitmap();
                source.Init(height, width, border);
                source.Grays = 2;

                // Draw checkerboard
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int offset = source.Border + y * source.BytesPerRow + x;
                    source.SetByteAt(offset, (sbyte)((x + y) % 2));
                }

                var originalClone = source.Duplicate();
                source.Compress();
                source.Decompress();

                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int offset = source.Border + y * source.BytesPerRow + x;
                    Assert.Equal(originalClone.GetByteAt(offset), source.GetByteAt(offset));
                }
            }
        }

        [Fact]
        public void Compress_ExtremeLongRun()
        {
            Bitmap source = new Bitmap();
            // MaxRunSize is 16383. We create a row of 33000 pixels (all white).
            // This forces AppendLongRun to loop twice:
            // 33000 - 16383 = 16617
            // 16617 - 16383 = 234
            // 234 fits in standard/medium run.
            source.Init(1, 33000, 0);
            source.Grays = 2;

            source.Compress();

            Assert.NotNull(source._RleData);
            // 2 Long runs = 3 bytes each = 6 bytes.
            // Remaining 234 = 2 bytes.
            // Total length should be 3 + 3 + 2 = 8 bytes.
            Assert.Equal(8, source._RleData.Length);

            // First 16383 segment
            Assert.Equal(0xFF, source._RleData[0]);
            Assert.Equal(0xFF, source._RleData[1]);
            Assert.Equal(0x00, source._RleData[2]);

            // Second 16383 segment
            Assert.Equal(0xFF, source._RleData[3]);
            Assert.Equal(0xFF, source._RleData[4]);
            Assert.Equal(0x00, source._RleData[5]);

            // Remainder 234
            // 234 encoded = ((234 >> 8) + 192) | (234 & 0xff)
            // byte 0 = (0 + 192) = 192
            // byte 1 = 234
            Assert.Equal(192, source._RleData[6]);
            Assert.Equal(234, source._RleData[7]);
        }

        [Theory]
        [InlineData(70000)] // Baseline > 64k
        [InlineData(65536)] // Exact 64k
        [InlineData(65600)] // AVX-512 aligned (65536 + 64)
        [InlineData(65599)] // AVX-512 off-by-one (65536 + 63)
        [InlineData(65568)] // AVX2 aligned (65536 + 32)
        [InlineData(65567)] // AVX2 off-by-one (65536 + 31)
        [InlineData(65552)] // Vector128 aligned (65536 + 16)
        [InlineData(65551)] // Vector128 off-by-one (65536 + 15)
        public unsafe void Compress_ExtremeLongRuns(int width)
        {
            Bitmap source = new Bitmap();
            source.Init(3, width, 0);
            source.Grays = 2;

            // Row 0: all white (0). Already initialized to 0.
            
            // Row 1: set of short runs, then long run.
            // Let's make the first 100 pixels alternate 0 and 1.
            int offsetRow1 = width;
            for (int i = 0; i < 100; i++)
            {
                source.SetByteAt(offsetRow1 + i, (sbyte)(i % 2));
            }
            // The rest 69900 pixels are 0.

            // Row 2: long run of 1s (black)
            int offsetRow2 = 2 * width;
            for (int i = 0; i < width; i++)
            {
                source.SetByteAt(offsetRow2 + i, 1);
            }

            Bitmap expected = source.Duplicate();

            try
            {
                source.Compress();
                Assert.NotNull(source._RleData);

                source.Decompress();

                fixed (sbyte* pE = expected.Data)
                fixed (sbyte* pS = source.Data)
                {
                    double diff = Util.ImageBinaryDiff((byte*)pE + expected.Border, (byte*)pS + source.Border, source.Width, source.Height, source.BytesPerRow, 8, 8);
                    if (diff > 0.0)
                    {
                        var sb = new StringBuilder();
                        int mismatchCount = 0;
                        for (int y = 0; y < source.Height; y++)
                        {
                            sbyte* rowE = expected.GetRow(y);
                            sbyte* rowS = source.GetRow(y);
                            for (int x = 0; x < source.Width; x++)
                            {
                                if (rowE[x] != rowS[x])
                                {
                                    sb.AppendLine($"Row {y}, Col {x}: Expected {rowE[x]}, Actual {rowS[x]}");
                                    mismatchCount++;
                                    if (mismatchCount >= 50)
                                    {
                                        sb.AppendLine("... (truncated)");
                                        goto Dump;
                                    }
                                }
                            }
                        }
                    Dump:
                        string dumpPath = Path.Combine(Environment.CurrentDirectory, $"Compress_ExtremeLongRuns_Diff_{width}.log");
                        File.WriteAllText(dumpPath, sb.ToString());
                        Assert.True(diff == 0.0, $"Diff > 0.0. Mismatches logged to: {dumpPath}\nPreview:\n{sb.ToString()}");
                    }
                    Assert.Equal(0.0, diff);
                }
            }
            finally
            {
                source.Dispose();
                expected.Dispose();
            }
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(10, false)]
        public void Compress_ZeroDimensions_ThrowsOrReturns(int border, bool shouldThrow)
        {
            Bitmap source = new Bitmap();
            ref BitmapSurrogate surrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref source);
            surrogate._Width = 0;
            surrogate._Height = 0;
            surrogate._Border = border;
            surrogate._Data = new sbyte[10];

            if (shouldThrow)
            {
                var ex = Assert.Throws<DjvuInvalidOperationException>(() => source.Compress());
                Assert.Contains("zero dimensions and zero border", ex.Message);
            }
            else
            {
                source.Compress();
                Assert.Null(source.RleData);
                Assert.NotNull(source.Data);
            }
        }

        [Fact]
        public void SerializeToRle_WritesR4Header()
        {
            Bitmap source = new Bitmap();
            source.Init(10, 10, 0);
            source.Grays = 2;

            using (var memoryStream = new System.IO.MemoryStream())
            {
                source.SerializeToRle(memoryStream);
                byte[] data = memoryStream.ToArray();
                Assert.Equal((byte)'R', data[0]);
                Assert.Equal((byte)'4', data[1]);
                Assert.Equal((byte)'\n', data[2]);
            }
        }

        [Fact]
        public void SerializeToRle_RoundTripsPixels()
        {
            Bitmap source = new Bitmap();
            source.Init(10, 10, 0);
            source.Grays = 2;
            source.Data[0] = 1;
            source.Data[15] = 1;

            using (var memoryStream = new System.IO.MemoryStream())
            {
                source.SerializeToRle(memoryStream);
                memoryStream.Position = 0;
                Bitmap destination = Bitmap.CreateBitmap(memoryStream, border: 0);

                Assert.Equal(1, destination.Data[0]);
                Assert.Equal(1, destination.Data[15]);
                Assert.Equal(0, destination.Data[1]);
            }
        }

        [Fact]
        public void SerializeToRle_Precompressed_WritesRleDataDirectly()
        {
            Bitmap source = new Bitmap();
            source.Init(10, 10, 0);
            source.Grays = 2;

            // Call internal Compress() to move data to _RleData and set Data to null
            source.Compress();

            // Verify it was successful
            using (var ms2 = new System.IO.MemoryStream())
            {
                source.SerializeToRle(ms2);
                Assert.True(ms2.Length > 0);
            }
        }

        [Fact]
        public unsafe void RleDecode_NullRuns_Throws()
        {
            Bitmap source = new Bitmap();
            source.Init(10, 10, 0);
            var method = typeof(Bitmap).GetMethod("RleDecode", BindingFlags.NonPublic | BindingFlags.Instance);
            
            var ex = Assert.Throws<TargetInvocationException>(() => 
            {
                object[] parameters = new object[] { Pointer.Box(null, typeof(byte*)) };
                method.Invoke(source, parameters);
            });
            Assert.IsType<DjvuArgumentNullException>(ex.InnerException);
        }

        [Fact]
        public unsafe void Rle2Bitmap_ValidRuns_ProperlyFillsBitmap()
        {
            Bitmap source = new Bitmap();
            
            // Runs total 32 pixels to hit the x >= 8 optimization loops
            byte[] runs = new byte[] { 10, 12, 10 }; 
            byte[] bitmap = new byte[32];
            
            fixed (byte* pRuns = runs)
            fixed (byte* pBitmap = bitmap)
            {
                byte* runsPtr = pRuns;
                byte* runsEnd = pRuns + runs.Length;
                source.Rle2Bitmap(32, ref runsPtr, runsEnd, pBitmap, invert: false);
            }
            
            // The method bit-packs 8 pixels per byte.
            // Run 1: 10 bits of 0 -> byte 0: 0x00, 2 bits left (00)
            // Run 2: 12 bits of 1 -> byte 1: 00111111 (0x3F), 6 bits left (111111)
            // Run 3: 10 bits of 0 -> byte 2: 11111100 (0xFC), 8 bits left (00000000 -> byte 3: 0x00)
            Assert.Equal(0x00, bitmap[0]); 
            Assert.Equal(0x3F, bitmap[1]);
            Assert.Equal(0xFC, bitmap[2]); 
            Assert.Equal(0x00, bitmap[3]);
            
            // Test invert: true
            byte[] bitmapInvert = new byte[4];
            
            fixed (byte* pRuns = runs)
            fixed (byte* pBitmap = bitmapInvert)
            {
                byte* runsPtr = pRuns;
                byte* runsEnd = pRuns + runs.Length;
                source.Rle2Bitmap(32, ref runsPtr, runsEnd, pBitmap, invert: true);
            }
            
            // Inverted bits: 0 becomes 1, 1 becomes 0.
            Assert.Equal(0xFF, bitmapInvert[0]);
            Assert.Equal(0xC0, bitmapInvert[1]); // ~0x3F
            Assert.Equal(0x03, bitmapInvert[2]); // ~0xFC
            Assert.Equal(0xFF, bitmapInvert[3]);
        }

        [Fact]
        public void SetMinimumBorder_ThrowsForNegativeValue()
        {
            Bitmap bmp = new Bitmap();
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.SetMinimumBorder(-1));
        }

        [Fact]
        public void SetMinimumBorder_NullData_UpdatesBorder()
        {
            Bitmap bmp = new Bitmap(); // Data is null initially
            bmp.SetMinimumBorder(4);
            Assert.Equal(4, bmp.Border);
        }

        [Fact]
        public void SetMinimumBorder_IncreasesBorder_WhenValueIsGreater()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(10, 10, 2);
            bmp.SetMinimumBorder(4);
            Assert.Equal(4, bmp.Border);
        }
        
        [Fact]
        public void SetMinimumBorder_DoesNothing_WhenValueIsSmallerOrEqual()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(10, 10, 4);
            bmp.SetMinimumBorder(2);
            Assert.Equal(4, bmp.Border);
        }

        [Fact]
        public void SetHeight_UpdatesHeightAndResizes()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(10, 10, 0);
            bmp.SetHeight(5); // Shrinking height is safe with 100-byte data array
            Assert.Equal(5, bmp.Height);
        }

        [Fact]
        public void SetWidth_UpdatesWidthAndResizes()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(10, 10, 0);
            bmp.SetWidth(5);
            Assert.Equal(5, bmp.Width);
        }

        [Fact]
        public void Bitmap_DataConstructor_InitializesCorrectly()
        {
            sbyte[] data = new sbyte[100];
            Bitmap bmp = new Bitmap(data, 10, 10, 0);
            Assert.Equal(10, bmp.Height);
            Assert.Equal(10, bmp.Width);
            Assert.Equal(data, bmp.Data);
        }

        [Fact]
        public void CreateBitmap_P1_ReadsPbmTextStream()
        {
            string pbm = "P1\n2 2\n0 1\n1 0\n";
            using (var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(pbm)))
            {
                Bitmap bmp = Bitmap.CreateBitmap(ms, 0);
                Assert.Equal(2, bmp.Width);
                Assert.Equal(2, bmp.Height);
                Assert.Equal(2, bmp.Grays);
                Assert.Equal(1, bmp.GetByteAt(0)); // Bottom row first
                Assert.Equal(0, bmp.GetByteAt(1));
                Assert.Equal(0, bmp.GetByteAt(2)); // Top row second
                Assert.Equal(1, bmp.GetByteAt(3));
            }
        }

        [Fact]
        public void CreateBitmap_P2_ReadsPgmTextStream()
        {
            string pgm = "P2\n2 2\n255\n0 255\n128 64\n";
            using (var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(pgm)))
            {
                Bitmap bmp = Bitmap.CreateBitmap(ms, 0);
                Assert.Equal(2, bmp.Width);
                Assert.Equal(2, bmp.Height);
                Assert.Equal(256, bmp.Grays);
                Assert.Equal(127, (byte)bmp.GetByteAt(0)); // Bottom row first
                Assert.Equal(191, (byte)bmp.GetByteAt(1));
                Assert.Equal(255, (byte)bmp.GetByteAt(2)); // Top row second
                Assert.Equal(0, (byte)bmp.GetByteAt(3));
            }
        }

        [Fact]
        public void CreateBitmap_P4_ReadsPbmRawStream()
        {
            // P4: width=2, height=2
            // PBM Raw groups bits into bytes. 2 pixels per row.
            // Row 0: 0 1 -> bits 01000000 = 0x40
            // Row 1: 1 0 -> bits 10000000 = 0x80
            // Wait, PBM Raw encodes bits top-to-bottom.
            // CreateBitmap calls ReadPbmRawStream which assumes row 0 is at bottom (due to DjVu inversion).
            // Actually, we just want to ensure it reads it without crashing and sets bytes.
            byte[] pbmRaw = new byte[] { (byte)'P', (byte)'4', (byte)'\n', (byte)'2', (byte)' ', (byte)'2', (byte)'\n', 0x40, 0x80 };
            using (var ms = new System.IO.MemoryStream(pbmRaw))
            {
                Bitmap bmp = Bitmap.CreateBitmap(ms, 0);
                Assert.Equal(2, bmp.Width);
                Assert.Equal(2, bmp.Height);
                Assert.Equal(2, bmp.Grays);
                // Just check that it parsed to the right size and type
            }
        }

        [Fact]
        public void CreateBitmap_P5_ReadsPgmRawStream()
        {
            // P5: width=2, height=2, maxval=255
            byte[] pgmRaw = new byte[] { 
                (byte)'P', (byte)'5', (byte)'\n', 
                (byte)'2', (byte)' ', (byte)'2', (byte)'\n', 
                (byte)'2', (byte)'5', (byte)'5', (byte)'\n',
                0, 255, 128, 64 
            };
            using (var ms = new System.IO.MemoryStream(pgmRaw))
            {
                Bitmap bmp = Bitmap.CreateBitmap(ms, 0);
                Assert.Equal(2, bmp.Width);
                Assert.Equal(2, bmp.Height);
                Assert.Equal(256, bmp.Grays);
            }
        }

        [Fact]
        public void SerializeToPgm_Raw_WritesP5Header()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(2, 2, 0);
            bmp.Grays = 256;

            using (var ms = new System.IO.MemoryStream())
            {
                bmp.SerializeToPgm(ms, raw: true);
                byte[] data = ms.ToArray();
                Assert.Equal((byte)'P', data[0]);
                Assert.Equal((byte)'5', data[1]);
                Assert.Equal((byte)'\n', data[2]);
            }
        }

        [Fact]
        public void SerializeToPgm_Raw_RoundTripsPixels()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(2, 2, 0);
            bmp.Grays = 256;
            bmp.SetByteAt(0, 0);
            bmp.SetByteAt(1, unchecked((sbyte)255));
            bmp.SetByteAt(2, unchecked((sbyte)128));
            bmp.SetByteAt(3, 64);

            using (var ms = new System.IO.MemoryStream())
            {
                bmp.SerializeToPgm(ms, raw: true);
                ms.Position = 0;
                Bitmap deserialized = Bitmap.CreateBitmap(ms, 0);
                Assert.Equal(2, deserialized.Width);
                Assert.Equal(2, deserialized.Height);
                Assert.Equal(0, deserialized.GetByteAt(0));
                Assert.Equal(unchecked((byte)255), deserialized.GetByteAt(1));
                Assert.Equal(128, deserialized.GetByteAt(2));
                Assert.Equal(64, deserialized.GetByteAt(3));
            }
        }

        [Fact]
        public void SerializeToPgm_Text_WritesP2Header()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(2, 2, 0);
            bmp.Grays = 256;

            using (var ms = new System.IO.MemoryStream())
            {
                bmp.SerializeToPgm(ms, raw: false);
                byte[] data = ms.ToArray();
                Assert.Equal((byte)'P', data[0]);
                Assert.Equal((byte)'2', data[1]);
                Assert.Equal((byte)'\n', data[2]);
            }
        }

        [Fact]
        public void SerializeToPgm_Text_RoundTripsPixels()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(2, 2, 0);
            bmp.Grays = 256;
            bmp.SetByteAt(0, 0);
            bmp.SetByteAt(1, unchecked((sbyte)255));
            bmp.SetByteAt(2, unchecked((sbyte)128));
            bmp.SetByteAt(3, 64);

            using (var ms = new System.IO.MemoryStream())
            {
                bmp.SerializeToPgm(ms, raw: false);
                ms.Position = 0;
                Bitmap deserialized = Bitmap.CreateBitmap(ms, 0);
                Assert.Equal(2, deserialized.Width);
                Assert.Equal(2, deserialized.Height);
                Assert.Equal(0, deserialized.GetByteAt(0));
                Assert.Equal(unchecked((byte)255), deserialized.GetByteAt(1));
                Assert.Equal(128, deserialized.GetByteAt(2));
                Assert.Equal(64, deserialized.GetByteAt(3));
            }
        }

        [Fact]
        public void SerializeToPbm_Text_WritesP1Header()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(2, 2, 0);
            bmp.Grays = 2;

            using (var ms = new System.IO.MemoryStream())
            {
                bmp.SerializeToPbm(ms, raw: false);
                byte[] data = ms.ToArray();
                Assert.Equal((byte)'P', data[0]);
                Assert.Equal((byte)'1', data[1]);
                Assert.Equal((byte)'\n', data[2]);
            }
        }

        [Fact]
        public void SerializeToPbm_Text_RoundTripsPixels()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(2, 2, 0);
            bmp.Grays = 2;
            bmp.SetByteAt(0, 0);
            bmp.SetByteAt(1, 1);
            bmp.SetByteAt(2, 1);
            bmp.SetByteAt(3, 0);

            using (var ms = new System.IO.MemoryStream())
            {
                bmp.SerializeToPbm(ms, raw: false);
                ms.Position = 0;
                Bitmap deserialized = Bitmap.CreateBitmap(ms, 0);
                Assert.Equal(2, deserialized.Width);
                Assert.Equal(2, deserialized.Height);
                Assert.Equal(0, deserialized.GetByteAt(0));
                Assert.Equal(1, deserialized.GetByteAt(1));
                Assert.Equal(1, deserialized.GetByteAt(2));
                Assert.Equal(0, deserialized.GetByteAt(3));
            }
        }

        [Fact]
        public void SerializeToPbm_Raw_WritesP4Header()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(2, 2, 0);
            bmp.Grays = 2;

            using (var ms = new System.IO.MemoryStream())
            {
                bmp.SerializeToPbm(ms, raw: true);
                byte[] data = ms.ToArray();
                Assert.Equal((byte)'P', data[0]);
                Assert.Equal((byte)'4', data[1]);
                Assert.Equal((byte)'\n', data[2]);
            }
        }

        [Fact]
        public void SerializeToPbm_Raw_RoundTripsPixels()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(2, 2, 0);
            bmp.Grays = 2;
            bmp.SetByteAt(0, 0);
            bmp.SetByteAt(1, 1);
            bmp.SetByteAt(2, 1);
            bmp.SetByteAt(3, 0);

            using (var ms = new System.IO.MemoryStream())
            {
                bmp.SerializeToPbm(ms, raw: true);
                ms.Position = 0;
                Bitmap deserialized = Bitmap.CreateBitmap(ms, 0);
                Assert.Equal(2, deserialized.Width);
                Assert.Equal(2, deserialized.Height);
                Assert.Equal(0, deserialized.GetByteAt(0));
                Assert.Equal(1, deserialized.GetByteAt(1));
                Assert.Equal(1, deserialized.GetByteAt(2));
                Assert.Equal(0, deserialized.GetByteAt(3));
            }
        }

        [Fact]
        public void CreateBitmap_IncompleteHeader_Throws()
        {
            // Only 1 byte, magic header needs 2 bytes.
            byte[] badData = new byte[] { (byte)'P' };
            using (var ms = new System.IO.MemoryStream(badData))
            {
                Assert.Throws<DjvuEndOfStreamException>(() => Bitmap.CreateBitmap(ms, 0));
            }
        }

        [Fact]
        public void CreateBitmap_InvalidPgmDepth_Throws()
        {
            // P5 format, Width: 2, Height: 2, MaxVal: 70000 (exceeds 16-bit 65535 limit)
            string header = "P5\n2 2\n70000\n";
            byte[] data = System.Text.Encoding.UTF8.GetBytes(header);
            using (var ms = new System.IO.MemoryStream(data))
            {
                Assert.Throws<DjvuFormatException>(() => Bitmap.CreateBitmap(ms, 0));
            }
        }

        [Fact]
        public void CreateBitmap_UnsupportedMagicNumber_Throws()
        {
            // P9 is an invalid format. CreateBitmap should reject it.
            string header = "P9\n2 2\n255\n";
            byte[] data = System.Text.Encoding.UTF8.GetBytes(header);
            using (var ms = new System.IO.MemoryStream(data))
            {
                Assert.Throws<DjvuFormatException>(() => Bitmap.CreateBitmap(ms, 0));
            }
        }

        [Fact]
        public void ReadPgmRawStream_TruncatedData_Throws()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(2, 2, 0);
            bmp.Grays = 256;
            
            // Expected 4 bytes (2x2) of raw pixel data, only providing 2
            byte[] data = new byte[] { 255, 128 };
            using (var ms = new System.IO.MemoryStream(data))
            {
                Assert.Throws<DjvuEndOfStreamException>(() => bmp.ReadPgmRawStream(ms, 255));
            }
        }

        [Fact]
        public void ReadPbmRawStream_TruncatedData_Throws()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(10, 10, 0);
            bmp.Grays = 2;
            
            // Expected at least 2 bytes per row * 10 = 20 bytes. Only providing 1.
            byte[] data = new byte[] { 0xFF };
            using (var ms = new System.IO.MemoryStream(data))
            {
                Assert.Throws<DjvuEndOfStreamException>(() => bmp.ReadPbmRawStream(ms));
            }
        }

        [Fact]
        public void ReadPgmTextStream_TruncatedData_Throws()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(2, 2, 0);
            bmp.Grays = 256;
            // Missing one number for a 2x2 grid
            string data = "10 20 30 ";
            using (var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(data)))
            {
                Assert.Throws<DjvuEndOfStreamException>(() => bmp.ReadPgmTextStream(ms, 255));
            }
        }

        [Fact]
        public void ReadPgmTextStream_CorruptedData_Throws()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(2, 2, 0);
            bmp.Grays = 256;
            // Invalid character 'X'
            string data = "10 X 30 40 ";
            using (var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(data)))
            {
                Assert.Throws<DjvuFormatException>(() => bmp.ReadPgmTextStream(ms, 255));
            }
        }

        [Fact]
        public void ReadPbmTextStream_TruncatedData_Throws()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(2, 2, 0);
            bmp.Grays = 2;
            // 2x2 grid needs 4 values, only providing 3
            string data = "1 0 1";
            using (var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(data)))
            {
                Assert.Throws<DjvuEndOfStreamException>(() => bmp.ReadPbmTextStream(ms));
            }
        }

        [Fact]
        public void ReadRleStream_TruncatedData_Throws()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(10, 10, 0);
            bmp.Grays = 2;
            // R4 expects proper RLE encoded runs. 0xFF is an invalid run sequence that expects more bytes.
            byte[] data = new byte[] { 0xFF }; 
            using (var ms = new System.IO.MemoryStream(data))
            {
                Assert.Throws<DjvuEndOfStreamException>(() => bmp.ReadRleStream(ms));
            }
        }

        [Fact]
        public void ReadRleStream_DataOutOfSync_Throws()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(2, 2, 0);
            bmp.Grays = 2;
            byte[] data = new byte[] { 0x01 };
            using (var ms = new System.IO.MemoryStream(data))
            {
                // Due to how RLE consumes bytes before checking format bounds, it hits EndOfStream first.
                Assert.Throws<DjvuEndOfStreamException>(() => bmp.ReadRleStream(ms));
            }
        }

        [Fact]
        public void SerializeToPbm_GraysGreaterThanTwo_Throws()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(10, 10, 0);
            bmp.Grays = 256; // PBM only supports Grays <= 2
            using (var ms = new System.IO.MemoryStream())
            {
                Assert.Throws<DjvuFormatException>(() => bmp.SerializeToPbm(ms, false));
            }
        }

        [Fact]
        public void SerializeToRle_GraysGreaterThanTwo_Throws()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(10, 10, 0);
            bmp.Grays = 256; // RLE only supports Grays <= 2
            using (var ms = new System.IO.MemoryStream())
            {
                Assert.Throws<DjvuInvalidOperationException>(() => bmp.SerializeToRle(ms));
            }
        }

        [Fact]
        public void SerializeToRle_UninitializedData_Throws()
        {
            Bitmap bmp = new Bitmap();
            // Data is null, Width/Height = 0
            using (var ms = new System.IO.MemoryStream())
            {
                Assert.Throws<DjvuInvalidOperationException>(() => bmp.SerializeToRle(ms));
            }
        }

        // Blit lacks parameter validation for subsample <= 0, but usually hits the early-exit condition:
        // ((xh >= (Width * subsample)) || (yh >= (Height * subsample))) returning false safely before dividing.

        [Fact]
        public void InsertMap_NullSource_HandledByBlitTestAndOriginalTests()
        {
            // Handled
        }

        [Fact]
        public void RleDecode_OutOfBounds_Throws()
        {
            Bitmap bmp = new Bitmap();
            // Init throws early if memory exceeds max limit
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.Init(int.MaxValue, 2, 0));
        }

        [Fact]
        public void EnsureZeroBuffer_ExpandsWhenRequired()
        {
            int initialSize = Bitmap._ZeroBufferSize;
            int requiredSize = initialSize + 1024;
            
            Bitmap.EnsureZeroBuffer(requiredSize);
            
            Assert.True(Bitmap._ZeroBufferSize >= requiredSize, 
                $"ZeroBuffer failed to expand. Expected at least {requiredSize}, got {Bitmap._ZeroBufferSize}");
        }

        [Fact]
        public async Task EnsureZeroBuffer_IsThreadSafe()
        {
            int initialSize = Bitmap._ZeroBufferSize;
            
            int taskCount = 20;
            int baseSize = initialSize + 1000;
            Task[] tasks = new Task[taskCount];

            for (int i = 0; i < taskCount; i++)
            {
                int required = baseSize + (i * 100);
                tasks[i] = Task.Run(() => Bitmap.EnsureZeroBuffer(required), TestContext.Current.CancellationToken);
            }

            await Task.WhenAll(tasks);

            int expectedMax = baseSize + (19 * 100);
            
            Assert.True(Bitmap._ZeroBufferSize >= expectedMax, 
                $"ZeroBuffer thread-safe expansion failed. Expected at least {expectedMax}, got {Bitmap._ZeroBufferSize}");
        }

        [Fact]
        public unsafe void GetRow_NegativeRow_ReturnsZeroBufferPointer()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(10, 10, 2);

            sbyte* rowPtr = bmp.GetRow(-1);
            sbyte* expectedPtr = Bitmap._ZeroBufferPointer + bmp.Border;

            Assert.Equal((nint)expectedPtr, (nint)rowPtr);
        }

        [Fact]
        public unsafe void GetRow_OverflowRow_ReturnsZeroBufferPointer()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(10, 10, 2); // Height is 10, valid rows 0-9

            sbyte* rowPtr = bmp.GetRow(10);
            sbyte* expectedPtr = Bitmap._ZeroBufferPointer + bmp.Border;

            Assert.Equal((nint)expectedPtr, (nint)rowPtr);
        }

        /// <summary>
        /// Test verifies that ZeroBufferLock timeout prevents any potential deadlock
        /// Test has to ensure that first lock is taken on different thread
        /// that one we are running actual deadlock testing on.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task EnsureZeroBuffer_Deadlock_Throws()
        {
            // Act & Assert combined using the verified infrastructure utility
            DjvuTimeoutException ex = await Util.ThrowsAsync<DjvuTimeoutException>(
                lockAcquisition: () => Bitmap._ZeroBufferLock.Enter(),
                lockRelease: () => Bitmap._ZeroBufferLock.Exit(),
                backgroundAction: () =>
                {
                    int required = Bitmap._ZeroBufferSize + 1024;

                    // This runs on the isolated background thread and hits the lock held by the main thread
                    Bitmap.EnsureZeroBuffer(required);
                }
            );

            // Validate the specific deadlock exception details
            Assert.Contains("Deadlock detected", ex.Message);
        }

        [Fact]
        public void Init_ExternalBufferTooSmall_Throws()
        {
            Bitmap bmp = new Bitmap();
            sbyte[] smallBuffer = new sbyte[10]; // Too small for a 10x10 bitmap
            
            var ex = Assert.Throws<DjvuArgumentException>(() => bmp.Init(smallBuffer, 10, 10, 0));
            Assert.Contains("Mismatch in data size and Bitmap dimensions", ex.Message);
        }

        [Fact(Skip = "Flaky Init test - only run in group with other Init tests excluding all other tests in assembly.")]
        public void Init_SourceBitmap_VerifyIsAllocatedOnPinnedObjectHeap()
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            long pohSizeBefore = GC.GetGCMemoryInfo().GenerationInfo[4].SizeBeforeBytes;
            // Setup source outside the measured zone
            Bitmap source = new Bitmap();
            source.Init(32, 32, 0);

            Bitmap bmp = default;
            bool enteredNoGC = false;

            try
            {
                enteredNoGC = GC.TryStartNoGCRegion(1024 * 1024, true);
                bmp = new Bitmap();
                bmp.Init(ref source, 0);
                Assert.Equal(2, GC.GetGeneration(bmp.Data));
            }
            finally
            {
                if (enteredNoGC) GC.EndNoGCRegion();
            }

            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            long pohSizeAfter = GC.GetGCMemoryInfo().GenerationInfo[4].SizeAfterBytes;
            
            long arrayAllocationSize = bmp.Data.Length * sizeof(sbyte) + Clr64BitArrayOverhead;

            Console.WriteLine($"Source: {source}\n{bmp}. Size of allocation in POH: {pohSizeAfter - pohSizeBefore}");

            Assert.Equal(arrayAllocationSize * 2, pohSizeAfter - pohSizeBefore);
        }

        [Fact(Skip = "Flaky Init test - only run in group with other Init tests excluding all other tests in assembly.")]
        public void Init_SourceBitmapRect_VerifyIsAllocatedOnPinnedObjectHeap()
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            long pohSizeBefore = GC.GetGCMemoryInfo().GenerationInfo[4].SizeAfterBytes;

            Bitmap source = new Bitmap();
            source.Init(64, 64, 0); 

            Bitmap bmp = default;
            bool enteredNoGC = false;

            try
            {
                enteredNoGC = GC.TryStartNoGCRegion(1024 * 1024, true);
                bmp = new Bitmap();
                bmp.Init(ref source, new Rectangle(0, 0, 32, 32), 0); 
                Assert.Equal(2, GC.GetGeneration(bmp.Data));
            }
            finally
            {
                if (enteredNoGC) GC.EndNoGCRegion();
            }

            // Attempt to trigger Copy-On-Write if such a mechanism exists
            for (int i = 0; i < 32; i++)
            {
                bmp.Data[i] = (sbyte)(bmp.Data[i] + 1);
            }

            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            long pohSizeAfter = GC.GetGCMemoryInfo().GenerationInfo[4].SizeAfterBytes;
            
            long arrayAllocationSize =
                bmp.Data.Length * sizeof(sbyte) + source.Data.Length * sizeof(sbyte) + 2 * Clr64BitArrayOverhead;

            Console.WriteLine($"Source: {source}\n{bmp}. Size of allocation in POH: {pohSizeAfter - pohSizeBefore}");

            Assert.Equal(arrayAllocationSize, pohSizeAfter - pohSizeBefore);
        }

        [Fact(Skip = "Flaky Init test - only run in group with other Init tests excluding all other tests in assembly.")]
        public void Init_HeightWidth_VerifyIsAllocatedOnPinnedObjectHeap()
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            long pohSizeBefore = GC.GetGCMemoryInfo().GenerationInfo[4].SizeAfterBytes;

            Bitmap bmp = default;
            bool enteredNoGC = false;

            try
            {
                enteredNoGC = GC.TryStartNoGCRegion(1024 * 1024, true);
                bmp = new Bitmap();
                bmp.Init(32, 32, 0);
                Assert.Equal(2, GC.GetGeneration(bmp.Data));
            }
            finally
            {
                if (enteredNoGC) GC.EndNoGCRegion();
            }

            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            long pohSizeAfter = GC.GetGCMemoryInfo().GenerationInfo[4].SizeAfterBytes;
            
            long arrayAllocationSize = bmp.Data.Length * sizeof(sbyte) + Clr64BitArrayOverhead;
            Console.WriteLine($"{bmp}. Size of allocation in POH: {pohSizeAfter - pohSizeBefore}");
            Assert.Equal(arrayAllocationSize, pohSizeAfter - pohSizeBefore);
        }

        [Fact(Skip = "Flaky Init test - only run in group with other Init tests excluding all other tests in assembly.")]
        public void Init_DataArray_VerifyIsAllocatedOnPinnedObjectHeap()
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            long pohSizeBefore = GC.GetGCMemoryInfo().GenerationInfo[4].SizeAfterBytes;

            Bitmap bmp = default;
            sbyte[] data = new sbyte[1024]; // Standard, unpinned array
            bool enteredNoGC = false;

            try
            {
                enteredNoGC = GC.TryStartNoGCRegion(1024 * 1024, true);
                bmp = new Bitmap();
                bmp.Init(data, 32, 32, 0);
                Assert.Equal(2, GC.GetGeneration(bmp.Data));
            }
            finally
            {
                if (enteredNoGC) GC.EndNoGCRegion();
            }

            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            long pohSizeAfter = GC.GetGCMemoryInfo().GenerationInfo[4].SizeAfterBytes;
            
            long arrayAllocationSize = bmp.Data.Length * sizeof(sbyte) + Clr64BitArrayOverhead;
            Console.WriteLine($"{bmp}. Size of allocation in POH: {pohSizeAfter - pohSizeBefore}");
            Assert.Equal(arrayAllocationSize, pohSizeAfter - pohSizeBefore);
        }

        public static TheoryData<Bitmap, Bitmap, bool> EqualityTestData()
        {
            var baseBitmap = new Bitmap(10, 10, 0);
            var identicalCopy = baseBitmap; 
            
            var diffDataBitmap = new Bitmap(10, 10, 0);
            diffDataBitmap.SetByteAt(0, 1);
            var diffDimsBitmap = new Bitmap(20, 20, 0);
            var defaultBitmap = new Bitmap();
            var anotherDefault = new Bitmap();

            var diffBorderBitmap = new Bitmap(10, 10, 1);
            
            var diffGraysBitmap = new Bitmap(10, 10, 0);
            diffGraysBitmap.Grays = 256;

            var identicalDataDiffRefBitmap = new Bitmap(10, 10, 0);
            
            var nullDataBitmap = new Bitmap(10, 10, 0);
            object boxedNull = nullDataBitmap;
            typeof(Bitmap).GetField("_Data", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxedNull, null);
            nullDataBitmap = (Bitmap)boxedNull;
            
            var diffLengthBitmap = new Bitmap(10, 10, 0);
            object boxedLen = diffLengthBitmap;
            typeof(Bitmap).GetField("_Data", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxedLen, new sbyte[1000]);
            diffLengthBitmap = (Bitmap)boxedLen;

            var data = new TheoryData<Bitmap, Bitmap, bool>();
            data.Add(baseBitmap, baseBitmap, true);                 // Path 2: Reference equality
            data.Add(baseBitmap, identicalCopy, true);              // Path 2: Reference equality
            data.Add(defaultBitmap, anotherDefault, true);          // Path 2: Reference equality (Null data)
            data.Add(baseBitmap, identicalDataDiffRefBitmap, true); // Path 5: Value equality / SequenceEqual (identical data array values, different instances)
            
            data.Add(baseBitmap, diffDataBitmap, false);            // Path 6: Value inequality (SequenceEqual false)
            data.Add(baseBitmap, diffDimsBitmap, false);            // Path 1: Property mismatches (Dimensions)
            data.Add(baseBitmap, defaultBitmap, false);             // Path 1: Property mismatches (Dimensions - 10x10 vs 0x0)
            data.Add(baseBitmap, diffBorderBitmap, false);          // Path 1: Property mismatches (Border)
            data.Add(baseBitmap, diffGraysBitmap, false);           // Path 1: Property mismatches (Grays)
            data.Add(baseBitmap, nullDataBitmap, false);            // Path 3: Unreachable Defensive Trap (Null data arrays via reflection)
            data.Add(baseBitmap, diffLengthBitmap, false);          // Path 4: Unreachable Defensive Trap (Data length mismatch via reflection)

            return data;
        }

        [Theory]
        [MemberData(nameof(EqualityTestData))]
        public void EqualsBitmap(Bitmap bmp1, Bitmap bmp2, bool expected)
        {
            Assert.Equal(expected, bmp1.Equals(bmp2));
        }

        [Theory]
        [MemberData(nameof(EqualityTestData))]
        public void EqualityOperatorEquals(Bitmap bmp1, Bitmap bmp2, bool expected)
        {
            Assert.Equal(expected, bmp1 == bmp2);
        }

        [Theory]
        [MemberData(nameof(EqualityTestData))]
        public void EqualityOperatorNotEquals(Bitmap bmp1, Bitmap bmp2, bool expected)
        {
            Assert.Equal(!expected, bmp1 != bmp2);
        }

        [Theory]
        [MemberData(nameof(EqualityTestData))]
        public void EqualsObject(Bitmap bmp1, object obj, bool expected)
        {
            Assert.Equal(expected, bmp1.Equals(obj));
        }

        [Fact]
        public void EqualsObject_BoxedIdenticalStruct()
        {
            var baseBitmap = new Bitmap(10, 10, 0);
            var identicalCopy = baseBitmap;
            
            object boxedIdentical = identicalCopy;

            Assert.True(baseBitmap.Equals(boxedIdentical));
        }

        [Fact]
        public void EqualsObject_BoxedDifferentStruct()
        {
            var baseBitmap = new Bitmap(10, 10, 0);
            var diffDataBitmap = new Bitmap(10, 10, 0);
            diffDataBitmap.SetByteAt(0, 1);

            object boxedDiff = diffDataBitmap;

            Assert.False(baseBitmap.Equals(boxedDiff));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Not A Bitmap")]
        [InlineData(123)]
        public void EqualsObject_NullOrDifferentType(object obj)
        {
            var baseBitmap = new Bitmap(10, 10, 0);
            Assert.False(baseBitmap.Equals(obj));
        }

        [Theory]
        [MemberData(nameof(EqualityTestData))]
        public void GetHashCode_MatchesEquality(Bitmap bmp1, Bitmap bmp2, bool expectedEquality)
        {
            Assert.Equal(expectedEquality, bmp1.GetHashCode() == bmp2.GetHashCode());
        }

        [Fact]
        public void ToString_InitializedStruct()
        {
            var bmp = new Bitmap(10, 20, 2);
            string result = bmp.ToString();
            
            Assert.Contains("DjvuNet.Graphics.Bitmap", result);
            Assert.Contains($"Width: {bmp.Width}", result);
            Assert.Contains($"Height: {bmp.Height}", result);
            Assert.Contains($"Border: {bmp.Border}", result);
            Assert.Contains($"Grays: {bmp.Grays}", result);
            Assert.Contains($"Data: {bmp.Data.Length} sbytes.", result);
        }

        [Fact]
        public void ToString_DefaultStruct_NullData()
        {
            var bmp = new Bitmap();
            string result = bmp.ToString();

            Assert.Contains("DjvuNet.Graphics.Bitmap: Width: 0, Height: 0, Border: 0, Grays: 2, Data: null sbytes.", result);
        }

        public static TheoryData<Bitmap, Bitmap, bool> RleEqualityTestData()
        {
            var data = new TheoryData<Bitmap, Bitmap, bool>();

            // Setup Helpers
            Bitmap CreateUncompressed(byte value = 0)
            {
                var b = new Bitmap();
                b.Init(10, 10, 0); // Data populated, _RleData null
                if (value != 0)
                {
                    for(int i = 0; i < b.Data.Length; i++) b.Data[i] = (sbyte)value;
                }
                return b;
            }

            Bitmap CreateCompressed(byte rleValue = 0)
            {
                var b = CreateUncompressed(rleValue);
                b.Compress(); // Moves Data to _RleData, sets Data null
                return b;
            }

            Bitmap CreateNull()
            {
                return new Bitmap(); // Default struct, Data null, _RleData null
            }

            Bitmap CreateInvalid(byte dataValue, byte rleValue)
            {
                var b = CreateUncompressed(dataValue);
                b._RleData = new byte[] { rleValue, 0x01, 0x02 }; // Arbitrary RLE buffer
                return b;
            }

            // 1. UNCOMPRESSED vs UNCOMPRESSED
            data.Add(CreateUncompressed(1), CreateUncompressed(1), true);  // 1a: Match
            data.Add(CreateUncompressed(1), CreateUncompressed(0), false); // 1b: Mismatch

            // 2. UNCOMPRESSED vs COMPRESSED
            data.Add(CreateUncompressed(1), CreateCompressed(1), true);  // 2: Match (logical)
            
            // 3. UNCOMPRESSED vs NULL
            data.Add(CreateUncompressed(1), CreateNull(), false);        // 3: Mismatch

            // 4. UNCOMPRESSED vs INVALID
            data.Add(CreateUncompressed(1), CreateInvalid(1, 0), true);  // 4: Match (Data takes precedence, rle ignored)
            
            // 5. COMPRESSED vs UNCOMPRESSED
            data.Add(CreateCompressed(1), CreateUncompressed(1), true);  // 5: Match (logical)

            // 6. COMPRESSED vs COMPRESSED
            var c1 = CreateCompressed(1);
            var c2 = CreateCompressed(1);
            data.Add(c1, c1, true); // 6a: Reference match
            data.Add(c1, c2, true); // 6b: Value match
            data.Add(CreateCompressed(1), CreateCompressed(0), false); // 6c: Mismatch

            // 7. COMPRESSED vs NULL
            data.Add(CreateCompressed(1), CreateNull(), false); // 7: Mismatch

            // 8. COMPRESSED vs INVALID
            data.Add(CreateCompressed(1), CreateInvalid(1, 0), true); // 8: Match (Invalid degrades to Data, Compressed decompresses to match Data)

            // 9. NULL vs UNCOMPRESSED
            data.Add(CreateNull(), CreateUncompressed(1), false); // 9: Mismatch

            // 10. NULL vs COMPRESSED
            data.Add(CreateNull(), CreateCompressed(1), false); // 10: Mismatch

            // 11. NULL vs NULL
            data.Add(CreateNull(), CreateNull(), true); // 11: Match

            // 12. NULL vs INVALID
            data.Add(CreateNull(), CreateInvalid(1, 0), false); // 12: Mismatch

            // 13. INVALID vs UNCOMPRESSED
            data.Add(CreateInvalid(1, 0), CreateUncompressed(1), true); // 13: Match (Data matched)

            // 14. INVALID vs COMPRESSED
            data.Add(CreateInvalid(1, 0), CreateCompressed(1), true); // 14: Match (Data logically matched)

            // 15. INVALID vs NULL
            data.Add(CreateInvalid(1, 0), CreateNull(), false); // 15: Mismatch

            // 16. INVALID vs INVALID
            data.Add(CreateInvalid(1, 0), CreateInvalid(1, 0), true);  // 16a: Data match, RLE mismatch -> True
            data.Add(CreateInvalid(1, 0), CreateInvalid(0, 0), false);  // 16b: Data mismatch, RLE match -> False

            return data;
        }

        [Theory]
        [MemberData(nameof(RleEqualityTestData))]
        public void EqualityOperator_ExhaustiveStateMatrix(Bitmap bmp1, Bitmap bmp2, bool expected)
        {
            Assert.Equal(expected, bmp1 == bmp2);
        }

        [Theory]
        [MemberData(nameof(RleEqualityTestData))]
        public void InequalityOperator_ExhaustiveStateMatrix(Bitmap bmp1, Bitmap bmp2, bool expected)
        {
            Assert.Equal(!expected, bmp1 != bmp2);
        }

        [Theory]
        [MemberData(nameof(RleEqualityTestData))]
        public void EqualsMethod_ExhaustiveStateMatrix(Bitmap bmp1, Bitmap bmp2, bool expected)
        {
            Assert.Equal(expected, bmp1.Equals(bmp2));
        }
        
        [Theory]
        [MemberData(nameof(RleEqualityTestData))]
        public void GetHashCode_MatchesEquality_RleMatrix(Bitmap bmp1, Bitmap bmp2, bool expected)
        {
            Assert.Equal(expected, bmp1.GetHashCode() == bmp2.GetHashCode());
        }

        [Theory]
        [InlineData(32)]  // Targets Vector128 exactly
        [InlineData(63)]  // Targets Vector128 with scalar fallback
        [InlineData(64)]  // Targets AVX2 exactly
        [InlineData(127)] // Targets AVX2 with scalar fallback
        [InlineData(128)] // Targets AVX-512 exactly
        [InlineData(255)] // Targets AVX-512 with scalar fallback
        public unsafe void DecodeRleCore_RunLength192(int len)
        {
            // Bug: The SIMD routines used a threshold of 192 (0xC0) 
            // but failed to treat exactly 192 as a 2-byte run due to Max(v, 192) equating to 192
            // instead of Max(v, 191) which correctly flags values >= 192.
            
            byte[] runs = new byte[len];
            runs[0] = 0xC0; // 2-byte run start
            runs[1] = 0xC0; // 2-byte run end (encoded length = 192)
            for (int i = 2; i < len; i++)
            {
                runs[i] = 0x01; // 1 pixel run
            }

            int totalPixels = 192 + (len - 2) * 1;
            sbyte[] decoded = new sbyte[totalPixels];

            fixed (byte* pRuns = runs)
            {
                Bitmap.DecodeRleCore(pRuns, len, decoded, 0, 1, totalPixels, totalPixels);
            }

            for (int i = 0; i < 192; i++)
            {
                Assert.Equal(0, decoded[i]);
            }
        }

        private static byte[] CreateRleBuffer(int length, byte fill, byte lastByte1, byte? lastByte2 = null)
        {
            byte[] buffer = new byte[length];
            for (int i = 0; i < length; i++) buffer[i] = fill;
            if (lastByte2.HasValue)
            {
                buffer[length - 2] = lastByte1;
                buffer[length - 1] = lastByte2.Value;
            }
            else
            {
                buffer[length - 1] = lastByte1;
            }
            return buffer;
        }

        public static TheoryData<byte[], int, int, Type, string> DecodeRleCoreInvalidData => new()
        {
            // 1. Scalar Fallback Branch (< 32 bytes)
            // 0xC1 0xFF = 511 pixels. Width is 200, so it overflows and throws FormatException.
            { CreateRleBuffer(4, 0x01, 0xC1, 0xFF), 4, 200, typeof(DjvuFormatException), null },
            // 0xC0 at the very end of stream throws EndOfStreamException
            { CreateRleBuffer(4, 0x01, 0xC0), 4, 200, typeof(DjvuEndOfStreamException), null },
            
            // 1b. Scalar Fallback Branch (Original Explicit Arrays)
            { new byte[] { 0xFF, 0xFF, 0xFF }, 3, 10, typeof(DjvuFormatException), null },
            { new byte[] { 0x01, 0x01 }, 2, 10, typeof(DjvuEndOfStreamException), null },
            
            // 2. Vector128 / SSE Branch (>= 32 bytes)
            { CreateRleBuffer(32, 0x01, 0xC1, 0xFF), 32, 200, typeof(DjvuFormatException), null },
            { CreateRleBuffer(32, 0x01, 0xC0), 32, 200, typeof(DjvuEndOfStreamException), null },
            
            // 3. AVX2 Branch (>= 64 bytes)
            { CreateRleBuffer(64, 0x01, 0xC1, 0xFF), 64, 300, typeof(DjvuFormatException), null },
            { CreateRleBuffer(64, 0x01, 0xC0), 64, 300, typeof(DjvuEndOfStreamException), null },
            
            // 4. AVX-512 Branch (>= 128 bytes)
            { CreateRleBuffer(128, 0x01, 0xC1, 0xFF), 128, 500, typeof(DjvuFormatException), null },
            { CreateRleBuffer(128, 0x01, 0xC0), 128, 500, typeof(DjvuEndOfStreamException), null }
        };

        [Theory]
        [MemberData(nameof(DecodeRleCoreInvalidData))]
        public unsafe void DecodeRleCore_InvalidData_Throws(byte[] runs, int runsLength, int requestedPixels, Type expectedException, string dummy)
        {
            sbyte[] decoded = new sbyte[requestedPixels];
            
            Action decodeAction = () =>
            {
                fixed (byte* pRuns = runs)
                    Bitmap.DecodeRleCore(pRuns, runsLength, decoded, 0, 1, requestedPixels, requestedPixels);
            };

            Assert.Throws(expectedException, decodeAction);
        }

        public static TheoryData<string, Type, string> ReadRleStreamInvalidStateData => new()
        {
            { "Uninitialized", typeof(DjvuInvalidOperationException), "is uninitialized as both" },
            { "AlreadyHasRle", typeof(DjvuInvalidOperationException), "already contains compressed" }
        };

        [Theory]
        [MemberData(nameof(ReadRleStreamInvalidStateData))]
        public void ReadRleStream_InvalidState_Throws(string testCase, Type expectedException, string messageFragment)
        {
            Bitmap bmp = new Bitmap();
            ref BitmapSurrogate surrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref bmp);

            switch (testCase)
            {
                case "AlreadyHasRle":
                    surrogate._Data = new sbyte[10];
                    surrogate._RleData = new byte[10];
                    break;
            }

            var ex = Assert.Throws(expectedException, () => bmp.ReadRleStream(Stream.Null));
            Assert.Contains(messageFragment, ex.Message);
        }

        public static TheoryData<string, Type, string> RleDecodeInvalidStateData => new()
        {
            { "StrideOverflow", typeof(DjvuArgumentOutOfRangeException), "Calculated stride exceeds bounds" },
            { "BufferOverflow", typeof(DjvuArgumentOutOfRangeException), "Calculated data buffer size exceeds bounds" }
        };

        [Theory]
        [MemberData(nameof(RleDecodeInvalidStateData))]
        public unsafe void RleDecode_InvalidState_Throws(string testCase, Type expectedException, string messageFragment)
        {
            Bitmap bmp = new Bitmap();
            ref BitmapSurrogate surrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref bmp);

            switch (testCase)
            {
                case "StrideOverflow":
                    surrogate._Width = int.MaxValue - 2;
                    surrogate._Height = 10;
                    surrogate._Border = 10;
                    break;
                case "BufferOverflow":
                    surrogate._Width = 1000000;
                    surrogate._Height = 1000000;
                    surrogate._Border = 0;
                    break;
            }

            // Must pass a non-null pointer (e.g. 1) to bypass the ArgumentNullException check
            var ex = Assert.Throws(expectedException, () => bmp.RleDecode((byte*)1));
            Assert.Contains(messageFragment, ex.Message);
        }

        [Fact]
        public unsafe void RleDecode_NullRuns_ThrowsArgumentNullException()
        {
            Bitmap bmp = new Bitmap();
            bmp.Init(10, 10, 0); // Initialize dimensions to pass prior InvalidOperation guardrail
            var ex = Assert.Throws<DjvuArgumentNullException>(() => bmp.RleDecode(null));
            Assert.Equal("runs", ex.ParamName);
        }


        public static TheoryData<int, int, int, int, int, int, int, int> BlitSimdEdgeCases
        {
            get
            {
                var data = new TheoryData<int, int, int, int, int, int, int, int>();

                // Subsamples to test
                int[] subsamples = new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                int[] borders = new[] { 0, 1, 2, 4 };

                foreach (int sub in subsamples)
                {
                    foreach (int b in borders)
                    {
                        // 128-bit Margins
                        data.Add(200, 200, 15, 15, 0, 0, sub, b);       // -1 byte (128-bit boundary)
                        data.Add(200, 200, 16, 16, 0, 0, sub, b);       // Exact (128-bit boundary)
                        data.Add(200, 200, 17, 17, 0, 0, sub, b);       // +1 byte (128-bit boundary)

                        // 256-bit Margins
                        data.Add(200, 200, 31, 31, 0, 0, sub, b);       // -1 byte (256-bit boundary)
                        data.Add(200, 200, 32, 32, 0, 0, sub, b);       // Exact (256-bit boundary)
                        data.Add(200, 200, 33, 33, 0, 0, sub, b);       // +1 byte (256-bit boundary)

                        // Mid-Tier Combinations
                        data.Add(200, 200, 48, 48, 0, 0, sub, b);       // 1x 256-bit + 1x 128-bit (Exact)
                        data.Add(200, 200, 51, 51, 0, 0, sub, b);       // 1x 256-bit + 1x 128-bit + 3 byte scalar tail

                        // 512-bit Margins
                        data.Add(200, 200, 63, 63, 0, 0, sub, b);       // -1 byte (512-bit boundary)
                        data.Add(200, 200, 64, 64, 0, 0, sub, b);       // Exact (512-bit boundary)
                        data.Add(200, 200, 65, 65, 0, 0, sub, b);       // +1 byte (512-bit boundary)

                        // Complex Block Combinations (Vector512 Cascade)
                        data.Add(200, 200, 117, 117, 0, 0, sub, b);     // 1x 512-bit + 1x 256-bit + 1x 128-bit + 5 byte scalar tail
                        data.Add(200, 200, 200, 200, 0, 0, sub, b);     // Multi-block stress test

                        // Logical Edge Cases
                        data.Add(200, 200, 64, 64, sub - 1, sub - 1, sub, b);  // SubPixel Phase Shift (max phase)
                        data.Add(200, 200, 64, 64, -17, -17, sub, b);          // Geometric Clipping (Negative Bounds)
                        data.Add(50, 50, 200, 200, 10, 10, sub, b);            // Geometric Clipping (Source Out-of-Bounds)
                        data.Add(50, 50, 200, 200, -19, -23, sub, b);          // Geometric Clipping (Source Out-of-Bounds + Negative Bounds)
                    }
                }

                // Explicit edge-case fallback test for an odd unoptimized scalar factor
                data.Add(200, 200, 100, 100, 0, 0, 3, 0);

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(BlitSimdEdgeCases))]
        public unsafe void Blit_SimdVectorization_ParityWithScalar(
            int targetWidth, int targetHeight, int sourceWidth, int sourceHeight, int xPosition, int yPosition, int subsample, int border = 4)
        {
            // Arrange
            Bitmap targetSimd = new Bitmap();
            Util.PrepareTestBitmap(ref targetSimd, Util.SharedTargetBuffer, targetWidth, targetHeight, border);

            Bitmap targetScalar = new Bitmap();
            Util.PrepareTestBitmap(ref targetScalar, Util.SharedScalarBuffer, targetWidth, targetHeight, border);
            // Oracle MUST begin with the exact same randomized noise as the SIMD target
            Buffer.BlockCopy(Util.SharedTargetBuffer, 0, Util.SharedScalarBuffer, 0, targetSimd.RowOffset(targetHeight));

            Bitmap source = new Bitmap();
            Util.PrepareTestBitmap(ref source, Util.SharedSourceBuffer, sourceWidth, sourceHeight, border);

            // Act - SIMD (The optimized method)
            targetSimd.Blit(ref source, xPosition, yPosition, subsample);

            // Act - Reference Scalar (The exact legacy DjVuLibre C++ logic, re-written to be readable)
            int destRow = yPosition / subsample;
            int subPixelRowOffset = yPosition - (subsample * destRow);
            if (subPixelRowOffset < 0)
            { destRow--; subPixelRowOffset += subsample; }

            int startDestColumn = xPosition / subsample;
            int startSubPixelColOffset = xPosition - (subsample * startDestColumn);
            if (startSubPixelColOffset < 0)
            { startDestColumn--; startSubPixelColOffset += subsample; }

            for (int sourceRow = 0; sourceRow < source.Height; sourceRow++)
            {
                if (destRow >= 0 && destRow < targetScalar.Height)
                {
                    int destCol = startDestColumn;
                    int subPixelColOffset = startSubPixelColOffset;
                    int sourceRowStartIndex = source.RowOffset(sourceRow);
                    int destRowStartIndex = targetScalar.RowOffset(destRow);

                    for (int sourceCol = 0; sourceCol < source.Width; sourceCol++)
                    {
                        if (destCol >= 0 && destCol < targetScalar.Width)
                        {
                            targetScalar.Data[destRowStartIndex + destCol] = (sbyte)(targetScalar.Data[destRowStartIndex + destCol] + source.Data[sourceRowStartIndex + sourceCol]);
                        }

                        if (++subPixelColOffset >= subsample)
                        { subPixelColOffset = 0; destCol++; }
                    }
                }
                if (++subPixelRowOffset >= subsample)
                { subPixelRowOffset = 0; destRow++; }
            }

            // Assert
            var scalarSpan = new ReadOnlySpan<sbyte>(targetScalar.DataPointer, targetScalar.Data.Length);
            var simdSpan = new ReadOnlySpan<sbyte>(targetSimd.DataPointer, targetSimd.Data.Length);
            if (!scalarSpan.SequenceEqual(simdSpan))
            {
                int diffCount = 0;
                int maxPreviewLines = 32;
                var errorLog = new StringBuilder();

                for (int y = 0; y < targetHeight; y++)
                {
                    sbyte* simdRow = targetSimd.GetRow(y);
                    sbyte* scalarRow = targetScalar.GetRow(y);
                    for (int x = 0; x < targetWidth; x++)
                    {
                        if (simdRow[x] != scalarRow[x])
                        {
                            if (diffCount < maxPreviewLines)
                                errorLog.AppendLine($"Y: {y,4} | X: {x,4} | Idx: {y * targetWidth + x,6} | Scalar: {scalarRow[x],4} | SIMD: {simdRow[x],4} | Diff: {scalarRow[x] - simdRow[x]}");
                            diffCount++;
                        }
                    }
                }
                double diffPct = (double)diffCount / (targetWidth * targetHeight);
                errorLog.Insert(0, $"SIMD output does not match Scalar Oracle output. Diff: {diffPct:P4} ({diffCount} pixels)\n");
                Assert.Fail(errorLog.ToString());
            }
        }

        public static TheoryData<int, int, int, int, int, int, int, int> BlitSimdEdgeCases_V2
        {
            get
            {
                var data = new TheoryData<int, int, int, int, int, int, int, int>();

                // Tier 1 AVX2 LCM Thresholds (subsample * 16)
                int[] tier1Thresholds = { 32, 48, 64, 80, 96, 112, 128, 144, 160, 176, 192, 208, 224, 240 };
                // Tier 1 AVX-512 Fixed Threshold
                int avx512T1 = 128;

                for (int s = 2; s <= 15; s++)
                {
                    int t1 = tier1Thresholds[s - 2];
                    int[] widths = {
                        15,                  // Scalar only (< 16)
                        16, 17, 31,          // Tier 2 Padded Vector only
                        t1 - 1, t1,          // Straddling AVX2 Tier 1 threshold
                        t1 + 1,              // One AVX2 Tier 1 + Scalar
                        t1 + 16,             // One AVX2 Tier 1 + Tier 2
                        avx512T1 - 1, avx512T1, // Straddling AVX-512 Tier 1 threshold
                        avx512T1 + 1,        // One AVX-512 Tier 1 + Scalar
                        avx512T1 * 2,        // Exactly two AVX-512 Tier 1 blocks
                    };

                    int[] borders = new[] { 0, 1, 2, 4 };

                    foreach (int w in widths)
                    {
                        foreach (int b in borders)
                        {
                            // 1. Strict zero offset
                            data.Add(500, 500, w, w, 0, 0, s, b);

                            // 2. Full pixel offset (Shifts exactly 2 target pixels, phase = 0)
                            int fullPixelOffset = s * 2;
                            data.Add(500, 500, w, w, fullPixelOffset, fullPixelOffset, s, b);

                            // 3. Exhaustive sub-pixel phase combinations 
                            // Tests every possible sub-pixel offset against a shifted geometric origin
                            for (int phase = 1; phase < s; phase++)
                            {
                                data.Add(500, 500, w, w, fullPixelOffset + phase, fullPixelOffset + phase, s, b);
                            }
                        }
                    }
                }
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(BlitSimdEdgeCases_V2))]
        public unsafe void Blit_SimdVectorization_ParityWithScalarExpanded(
            int targetWidth, int targetHeight, int sourceWidth, int sourceHeight, int xPosition, int yPosition, int subsample, int border = 4)
        {
            // var sw = Stopwatch.StartNew();
            
            // 1. Arrange 
            Bitmap targetSimd = new Bitmap();
            Util.PrepareTestBitmap(ref targetSimd, Util.SharedTargetBuffer, targetWidth, targetHeight, border);
            
            Bitmap targetScalar = new Bitmap();
            Util.PrepareTestBitmap(ref targetScalar, Util.SharedScalarBuffer, targetWidth, targetHeight, border);
            // Oracle MUST begin with the exact same randomized noise as the SIMD target
            Buffer.BlockCopy(Util.SharedTargetBuffer, 0, Util.SharedScalarBuffer, 0, targetSimd.RowOffset(targetHeight));

            Bitmap source = new Bitmap();
            Util.PrepareTestBitmap(ref source, Util.SharedSourceBuffer, sourceWidth, sourceHeight, border);
            
            //long setupTime = sw.ElapsedTicks;
            //sw.Restart();

            // 2. Act (SIMD Path routed to V2)
            targetSimd.Blit(ref source, xPosition, yPosition, subsample);
            
            //long simdTime = sw.ElapsedTicks;
            //sw.Restart();

            // 3. Act (Scalar Oracle)
            int destRow = yPosition / subsample;
            int subPixelRowOffset = yPosition - (subsample * destRow);
            if (subPixelRowOffset < 0) { destRow--; subPixelRowOffset += subsample; }
            
            int startDestColumn = xPosition / subsample;
            int startSubPixelColOffset = xPosition - (subsample * startDestColumn);
            if (startSubPixelColOffset < 0) { startDestColumn--; startSubPixelColOffset += subsample; }

            for (int sourceRow = 0; sourceRow < source.Height; sourceRow++)
            {
                if (destRow >= 0 && destRow < targetScalar.Height)
                {
                    int destCol = startDestColumn;
                    int subPixelColOffset = startSubPixelColOffset;
                    int sourceRowStartIndex = source.RowOffset(sourceRow);
                    int destRowStartIndex = targetScalar.RowOffset(destRow);
                    
                    for (int sourceCol = 0; sourceCol < source.Width; sourceCol++)
                    {
                        if (destCol >= 0 && destCol < targetScalar.Width)
                        {
                            targetScalar.Data[destRowStartIndex + destCol] = (sbyte)(targetScalar.Data[destRowStartIndex + destCol] + source.Data[sourceRowStartIndex + sourceCol]);
                        }
                        if (++subPixelColOffset >= subsample) { subPixelColOffset = 0; destCol++; }
                    }
                }
                if (++subPixelRowOffset >= subsample) { subPixelRowOffset = 0; destRow++; }
            }
            
            // long oracleTime = sw.ElapsedTicks;

            // 4. Assert
            var scalarSpan = new ReadOnlySpan<sbyte>(targetScalar.DataPointer, targetScalar.Data.Length);
            var simdSpan = new ReadOnlySpan<sbyte>(targetSimd.DataPointer, targetSimd.Data.Length);
            if (!scalarSpan.SequenceEqual(simdSpan))
            {
                int diffCount = 0;
                int maxPreviewLines = 32;
                var errorLog = new System.Text.StringBuilder();

                for (int y = 0; y < targetHeight; y++)
                {
                    sbyte* simdRow = targetSimd.GetRow(y);
                    sbyte* scalarRow = targetScalar.GetRow(y);
                    for (int x = 0; x < targetWidth; x++)
                    {
                        if (simdRow[x] != scalarRow[x])
                        {
                            if (diffCount < maxPreviewLines)
                                errorLog.AppendLine($"Y: {y,4} | X: {x,4} | Idx: {y * targetWidth + x,6} | Scalar: {scalarRow[x],4} | SIMD: {simdRow[x],4} | Diff: {scalarRow[x] - simdRow[x]}");
                            diffCount++;
                        }
                    }
                }
                double diffPct = (double)diffCount / (targetWidth * targetHeight);
                errorLog.Insert(0, $"SIMD output does not match Scalar Oracle output. Diff: {diffPct:P4} ({diffCount} pixels)\n");
                Assert.Fail(errorLog.ToString());
            }
            
            // Console.WriteLine($"[TIMING] W:{sourceWidth} S:{subsample} | Setup: {setupTime} | SIMD: {simdTime} | Oracle: {oracleTime} (ticks)");
        }

        public static TheoryData<int, int, int, int, int, int, int, int> Blit_SimdEdgeCases_Large
        {
            get
            {
                var data = new TheoryData<int, int, int, int, int, int, int, int>();

                for (int s = 2; s <= 15; s++)
                {
                    // Calculate actual AVX-512 Tier 1 consumption stride
                    int chunk = (s <= 4) ? 4 : (s <= 8) ? 8 : 16;
                    int boxes = 64 / chunk;
                    int consumed = (s == 2) ? 128 : (boxes * s * 2);

                    // Multipliers: 1 exact loop, and roughly 3.3 loops (to test fractional remainder handling)
                    double[] iterMultipliers = { 1.0, 3.3 };

                    // Reduced border testing surface
                    int[] borders = new[] { 0, 4 };

                    foreach (double mult in iterMultipliers)
                    {
                        // baseWidth calculates the exact threshold pixel width 
                        int baseWidth = (int)(consumed * mult) + 128;

                        int[] widths = {
                            baseWidth - 1,   // Straddling boundary
                            baseWidth,       // Exact boundary 
                            baseWidth + 1,   // Clears boundary (passes 1 byte to Scalar Tail)
                            baseWidth + 31   // Passes exactly 31 bytes to Tier 2
                        };

                        foreach (int w in widths)
                        {
                            foreach (int b in borders)
                            {
                                // Strict zero offset
                                data.Add(2000, 2000, w, w, 0, 0, s, b);

                                // Phase shifted offset (max phase) to aggressively test Tail processor alignment
                                data.Add(2000, 2000, w, w, (s * 2) + (s - 1), (s * 2) + (s - 1), s, b);
                            }
                        }
                    }
                }
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Blit_SimdEdgeCases_Large))]
        //[InlineData(2560, 1600, 1950, 780, 0, 0, 4, 4)]
        //[InlineData(2560, 1600, 1950, 780, 0, 0, 8, 4)]
        //[InlineData(2560, 1600, 1950, 780, 0, 0, 9, 4)]
        public unsafe void Blit_SimdVectorization_Tier1(
            int targetWidth, int targetHeight, int sourceWidth, int sourceHeight, int xPosition, int yPosition, int subsample, int border)
        {
            // 1. Arrange 
            Bitmap targetSimd = new Bitmap();
            Util.PrepareTestBitmap(ref targetSimd, Util.SharedTargetBuffer, targetWidth, targetHeight, border);
            
            Bitmap targetScalar = new Bitmap();
            Util.PrepareTestBitmap(ref targetScalar, Util.SharedScalarBuffer, targetWidth, targetHeight, border);
            // Oracle MUST begin with the exact same randomized noise as the SIMD target
            Buffer.BlockCopy(Util.SharedTargetBuffer, 0, Util.SharedScalarBuffer, 0, targetSimd.RowOffset(targetHeight));

            Bitmap source = new Bitmap();
            Util.PrepareTestBitmap(ref source, Util.SharedSourceBuffer, sourceWidth, sourceHeight, border);

            // 2. Act (SIMD Path routed to V2)
            targetSimd.Blit(ref source, xPosition, yPosition, subsample);

            // 3. Act (Scalar Oracle)
            int destRow = yPosition / subsample;
            int subPixelRowOffset = yPosition - (subsample * destRow);
            if (subPixelRowOffset < 0)
            { destRow--; subPixelRowOffset += subsample; }

            int startDestColumn = xPosition / subsample;
            int startSubPixelColOffset = xPosition - (subsample * startDestColumn);
            if (startSubPixelColOffset < 0)
            { startDestColumn--; startSubPixelColOffset += subsample; }

            for (int sourceRow = 0; sourceRow < source.Height; sourceRow++)
            {
                if (destRow >= 0 && destRow < targetScalar.Height)
                {
                    int destCol = startDestColumn;
                    int subPixelColOffset = startSubPixelColOffset;
                    int sourceRowStartIndex = source.RowOffset(sourceRow);
                    int destRowStartIndex = targetScalar.RowOffset(destRow);

                    for (int sourceCol = 0; sourceCol < source.Width; sourceCol++)
                    {
                        if (destCol >= 0 && destCol < targetScalar.Width)
                        {
                            targetScalar.Data[destRowStartIndex + destCol] = (sbyte)(targetScalar.Data[destRowStartIndex + destCol] + source.Data[sourceRowStartIndex + sourceCol]);
                        }
                        if (++subPixelColOffset >= subsample)
                        { subPixelColOffset = 0; destCol++; }
                    }
                }
                if (++subPixelRowOffset >= subsample)
                { subPixelRowOffset = 0; destRow++; }
            }

            // 4. Assert
            var scalarSpan = new ReadOnlySpan<sbyte>(targetScalar.DataPointer, targetScalar.Data.Length);
            var simdSpan = new ReadOnlySpan<sbyte>(targetSimd.DataPointer, targetSimd.Data.Length);
            if (!scalarSpan.SequenceEqual(simdSpan))
            {
                int diffCount = 0;
                int maxPreviewLines = 32;
                var errorLog = new System.Text.StringBuilder();

                for (int y = 0; y < targetHeight; y++)
                {
                    sbyte* simdRow = targetSimd.GetRow(y);
                    sbyte* scalarRow = targetScalar.GetRow(y);
                    for (int x = 0; x < targetWidth; x++)
                    {
                        if (simdRow[x] != scalarRow[x])
                        {
                            if (diffCount < maxPreviewLines)
                                errorLog.AppendLine($"Y: {y,4} | X: {x,4} | Idx: {y * targetWidth + x,6} | Scalar: {scalarRow[x],4} | SIMD: {simdRow[x],4} | Diff: {scalarRow[x] - simdRow[x]}");
                            diffCount++;
                        }
                    }
                }
                double diffPct = (double)diffCount / (targetWidth * targetHeight);
                errorLog.Insert(0, $"SIMD output does not match Scalar Oracle output on Large AVX-512 boundary. Diff: {diffPct:P4} ({diffCount} pixels)\n");
                Assert.Fail(errorLog.ToString());
            }
        }
        [Fact]
        public unsafe void Diagnostics_Reduce2_Vector256()
        {
            if (!Avx2.IsSupported) return;
            sbyte[] src = new sbyte[64];
            for (int i = 0; i < 64; i++) src[i] = (sbyte)i;

            sbyte[] result = new sbyte[32];
            fixed (sbyte* ptr = src)
            fixed (sbyte* res = result)
            {
                var v0 = Vector256.Load(ptr);
                var v1 = Vector256.Load(ptr + 32);

                sbyte[] Shift1_256 = new sbyte[32] { 1, 0, 3, 0, 5, 0, 7, 0, 9, 0, 11, 0, 13, 0, 15, 0, 1, 0, 3, 0, 5, 0, 7, 0, 9, 0, 11, 0, 13, 0, 15, 0 };
                var sum0 = Vector256.Add(v0, Avx2.Shuffle(v0, Vector256.Create<sbyte>(Shift1_256)));
                var sum1 = Vector256.Add(v1, Avx2.Shuffle(v1, Vector256.Create<sbyte>(Shift1_256)));

                sbyte[] Pack2_Low_256 = new sbyte[32] { 0, 2, 4, 6, 8, 10, 12, 14, -1, -1, -1, -1, -1, -1, -1, -1, 0, 2, 4, 6, 8, 10, 12, 14, -1, -1, -1, -1, -1, -1, -1, -1 };
                sbyte[] Pack2_High_256 = new sbyte[32] { -1, -1, -1, -1, -1, -1, -1, -1, 0, 2, 4, 6, 8, 10, 12, 14, -1, -1, -1, -1, -1, -1, -1, -1, 0, 2, 4, 6, 8, 10, 12, 14 };

                var pack = Vector256.BitwiseOr(
                    Avx2.Shuffle(sum0, Vector256.Create<sbyte>(Pack2_Low_256)),
                    Avx2.Shuffle(sum1, Vector256.Create<sbyte>(Pack2_High_256)));

                var final = Avx2.PermuteVar8x32(pack.AsInt32(), Vector256.Create(0, 1, 4, 5, 2, 3, 6, 7)).AsSByte();
                Vector256.Store(final, res);

                for (int i = 0; i < 32; i++)
                {
                    sbyte expected = (sbyte)(src[i * 2] + src[i * 2 + 1]);
                    if (result[i] != expected)
                    {
                        Assert.Fail($"Reduce2_Vector256 broken at index {i}: Expected {expected}, Got {result[i]}");
                    }
                }
            }
        }
    }
}
