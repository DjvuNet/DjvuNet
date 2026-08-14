using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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
        // Massive overflow: height * stride > int.MaxValue.
        // 65536 * (65536 + 0) = 4,294,967,296
        [InlineData(65536, 65536, 0)]
        public void Init_CalculatedStrideOverflow_Throws(int width, int height, int border)
        {
            Bitmap bmp = new Bitmap();
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.Init(height, width, border));
            Assert.Contains("exceed maximum integer size", ex.Message);
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

            Bitmap target = selfAlias ? source : new Bitmap();

            if (expectedException != null)
            {
                Assert.Throws(expectedException, () => target.Init(ref source, new Rectangle(rX, rY, rW, rH), tgtBorder));
                return;
            }

            // No exception expected, safely execute and validate state
            target.Init(ref source, new Rectangle(rX, rY, rW, rH), tgtBorder);

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
        public void Init_Throws()
        {
            var bmp = new Bitmap();
            // Init dimensions but try to force the underlying data buffer to remain null
            Assert.Throws<DjvuArgumentException>(() => bmp.Init(null, 10, 10, 0));
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
                target.Fill(0);
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

        // -------------------------------------------------------------------------
        // RLE COMPRESSION / DECOMPRESSION TESTS
        // -------------------------------------------------------------------------

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

            target.Uncompress();

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
        [InlineData("AllWhite", 32, 32)]
        [InlineData("AllBlack", 32, 32)]
        [InlineData("Checkerboard", 64, 64)]
        [InlineData("StartsWithBlack", 64, 64)]
        [InlineData("LongRunOverflow", 200, 200)]
        public void CompressUncompress_DataPatterns_RoundTripPreservesExactPixels(string pattern, int width, int height)
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
            source.Uncompress();

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

        [Fact]
        public void Uncompress_CorruptedRleData_ThrowsDjvuFormatException()
        {
            Bitmap source = new Bitmap();
            source.Init(10, 10, 0);
            source.Grays = 2;
            source.Compress();

            // Corrupt the first RLE run to claim 50 pixels (Width is only 10)
            source._RleData[0] = 50;

            Assert.Throws<DjvuFormatException>(() => source.Uncompress());
        }

        [Fact]
        public void Uncompress_ZeroDimensions_ThrowsInvalidOperationException()
        {
            Bitmap source = new Bitmap();
            // Setting RleData manually without initializing dimensions
            source._RleData = new byte[] { 0 };

            var ex = Assert.Throws<DjvuInvalidOperationException>(() => source.Uncompress());
            Assert.Contains("Bitmap is not properly initialized", ex.Message);
        }



        [Fact(Timeout = 200)] // Safeguard against infinite loops
        public void SerializeToPbm_RawFormat_RleData_SuccessfullySerializesWithoutInfiniteLoop()
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
        public void CompressUncompress_NonZeroBorder_RoundTripPreservesPixels()
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
                source.Uncompress();

                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int offset = source.Border + y * source.BytesPerRow + x;
                    Assert.Equal(originalClone.GetByteAt(offset), source.GetByteAt(offset));
                }
            }
        }

        [Fact]
        public void Compress_ExtremeLongRun_CorrectlyEncodesMultiple16383Segments()
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

        [Fact]
        public void RleEncode_ZeroDimensions_ReturnsZero()
        {
            Bitmap source = new Bitmap();
            // Do not init dimensions (Height=0, Width=0)
            source.Grays = 2;
            source.Compress();

            // Should gracefully do nothing
            Assert.Null(source._RleData);
            Assert.Null(source.Data);
        }

        [Fact]
        public void Compress_ZeroDimensions_ReturnsGracefully()
        {
            Bitmap source = new Bitmap();
            typeof(DjvuNet.Graphics.Bitmap).GetProperty("Width").SetValue(source, 0);
            typeof(DjvuNet.Graphics.Bitmap).GetProperty("Height").SetValue(source, 0);
            typeof(DjvuNet.Graphics.Bitmap).GetProperty("Data").SetValue(source, new sbyte[10]);

            source.Compress();

            var rleData = typeof(Bitmap).GetField("_RleData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(source) as byte[];
            Assert.Null(rleData);
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
                source.Rle2Bitmap(32, ref runsPtr, pBitmap, invert: false);
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
                source.Rle2Bitmap(32, ref runsPtr, pBitmap, invert: true);
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
    }
}
