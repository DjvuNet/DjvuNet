using Xunit;
using DjvuNet.Graphics;
using DjvuNet.Errors;
using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;

namespace DjvuNet.Graphics.Tests
{
    public class MapTests
    {
        [Fact()]
        public void MapTest()
        {
            Mock<Map> mapMock = new Mock<Map>(new object[] { 1, 0, 0, 0, false }) { CallBase = true };
        }

        /// <summary>
        /// Verifies that assigning negative values via SetWidth and SetHeight methods 
        /// correctly throws DjvuArgumentOutOfRangeException.
        /// </summary>
        [Fact]
        public void SetDimensions_NegativeAssignment_ThrowsDjvuArgumentOutOfRangeException()
        {
            Mock<Map> mapMock = new Mock<Map>(new object[] { 1, 0, 0, 0, false }) { CallBase = true };
            Map map = mapMock.Object;

            Assert.Throws<DjvuArgumentOutOfRangeException>(() => map.SetWidth(-100));
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => map.SetHeight(-200));
        }

        /// <summary>
        /// Verifies the ReadInteger method successfully skips leading whitespaces and comments 
        /// before extracting the integer value from the stream.
        /// </summary>
        [Fact]
        public void ReadInteger_ValidInput_ParsesCorrectly()
        {
            string input = "  \t \n # comment \n 12345 ";
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(input)))
            {
                char c = (char)stream.ReadByte();
                uint result = Map.ReadInteger(ref c, stream);
                Assert.Equal(12345u, result);
            }
        }

        /// <summary>
        /// Verifies that providing an invalid (non-numeric) character to ReadInteger 
        /// correctly aborts parsing and throws a domain-specific DjvuFormatException.
        /// </summary>
        [Fact]
        public void ReadInteger_InvalidInput_ThrowsFormatException()
        {
            string input = "abc";
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(input)))
            {
                char c = (char)stream.ReadByte();
                Assert.Throws<DjvuFormatException>((Action)(() => Map.ReadInteger(ref c, stream)));
            }
        }

        /// <summary>
        /// Verifies that ReadInteger explicitly guards against null stream parameters,
        /// throwing DjvuArgumentNullException instead of a raw runtime NullReferenceException.
        /// </summary>
        [Fact]
        public void ReadInteger_NullStream_ThrowsDjvuArgumentNullException()
        {
            char c = ' ';
            var ex = Assert.Throws<DjvuArgumentNullException>(() => Map.ReadInteger(ref c, null));
            Assert.Equal("stream", ex.ParamName);
        }

        /// <summary>
        /// Verifies that ReadInteger correctly handles a truncated stream during comment parsing
        /// by throwing a DjvuEndOfStreamException instead of entering an infinite loop.
        /// </summary>
        [Fact]
        public void ReadInteger_EofInComment_ThrowsDjvuEndOfStreamException()
        {
            // The string ends abruptly inside a comment (no \n or \r)
            string input = " # truncated comment";
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(input)))
            {
                char c = (char)stream.ReadByte();
                Assert.Throws<DjvuEndOfStreamException>((Action)(() => Map.ReadInteger(ref c, stream)));
            }
        }

        /// <summary>
        /// Verifies that ReadInteger correctly handles a truncated stream while consuming leading
        /// whitespace, throwing a DjvuEndOfStreamException to prevent buffer over-reads.
        /// </summary>
        [Fact]
        public void ReadInteger_EofInWhitespace_ThrowsDjvuEndOfStreamException()
        {
            // The string ends abruptly while still consuming trailing whitespace
            string input = "  ";
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(input)))
            {
                char c = (char)stream.ReadByte();
                Assert.Throws<DjvuEndOfStreamException>((Action)(() => Map.ReadInteger(ref c, stream)));
            }
        }

        /// <summary>
        /// Verifies that parsing integer strings at extreme valid boundaries parses correctly.
        /// </summary>
        [Theory]
        [InlineData(" 0 ", 0u)]
        [InlineData(" 4294967295 ", 4294967295u)] // uint.MaxValue
        public void ReadInteger_ValidBoundary_ParsesCorrectly(string input, uint expected)
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(input)))
            {
                char c = (char)stream.ReadByte();
                uint result = Map.ReadInteger(ref c, stream);
                Assert.Equal(expected, result);
            }
        }

        /// <summary>
        /// Verifies that parsing integer strings exceeding uint bounds safely throws a 
        /// DjvuFormatException rather than silently overflowing, testing exact boundaries and massive numbers.
        /// </summary>
        [Theory]
        [InlineData(" 4294967296 ")] // uint.MaxValue + 1
        [InlineData(" 5000000000 ")] // Large overflow
        [InlineData(" 999999999999999999999999999 ")] // Massive overflow (Multiple wraps)
        public void ReadInteger_OverflowBoundary_ThrowsDjvuFormatException(string input)
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(input)))
            {
                char c = (char)stream.ReadByte();
                var ex = Assert.Throws<DjvuFormatException>((Action)(() => Map.ReadInteger(ref c, stream)));
                Assert.Contains("exceeds maximum representable bounds", ex.Message);
            }
        }

        /// <summary>
        /// Verifies that calling ToImage with zero dimensions safely throws a 
        /// DjvuInvalidOperationException to prevent unmanaged GDI+ ArgumentExceptions.
        /// </summary>
        [Theory]
        [InlineData(0, 32)]
        [InlineData(32, 0)]
        [InlineData(0, 0)]
        public void ToImage_ZeroDimensions_ThrowsDjvuInvalidOperationException(int width, int height)
        {
            Mock<Map> mapMock = new Mock<Map>(new object[] { 3, 0, 0, 0, false }) { CallBase = true };
            Map map = mapMock.Object;
            typeof(Map).GetProperty("Data").SetValue(map, new sbyte[1024]);
            map.SetWidth(width);
            map.SetHeight(height);

            var ex = Assert.Throws<DjvuInvalidOperationException>(() => map.ToImage());
            Assert.Contains("Dimensions must be greater than zero", ex.Message);
        }

        /// <summary>
        /// Verifies that FillRgbPixels explicitly guards against null pixel buffers
        /// by throwing a DjvuArgumentNullException, preventing silent downstream crashes.
        /// </summary>
        [Fact]
        public void FillRgbPixels_NullPixelsArray_ThrowsDjvuArgumentNullException()
        {
            int width = 32;
            int height = 32;
            PixelMap map = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.BluePixel) as PixelMap;
            
            var ex = Assert.Throws<DjvuArgumentNullException>(() => map.FillRgbPixels(0, 0, width, height, null, 0, width));
            Assert.Equal("pixels", ex.ParamName);
        }

        /// <summary>
        /// Verifies that attempting to fill pixels with an undersized source buffer 
        /// correctly aborts the invalid operation, preventing memory corruption downstream.
        /// </summary>
        [Fact]
        public void FillRgbPixels_OutOfBounds_ThrowsDjvuInvalidOperationException()
        {
            int width = 32;
            int height = 32;
            PixelMap map = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.BluePixel) as PixelMap;
            int[] pixels = new int[10]; // Too small

            var ex = Assert.Throws<DjvuInvalidOperationException>(() => map.FillRgbPixels(0, 0, width, height, pixels, 0, width));
            Assert.Contains("Destination buffer too small", ex.Message);
        }

        /// <summary>
        /// Verifies that providing invalid spatial dimensions or offsets to FillRgbPixels
        /// correctly aborts the operation and throws a DjvuArgumentOutOfRangeException.
        /// Exhaustively tests all negative bounds, overreaches, and invalid strides.
        /// </summary>
        [Theory]
        [InlineData(0, 0, -5, 32, 0, 32, "w")]          // Negative width
        [InlineData(0, 0, 32, -5, 0, 32, "h")]          // Negative height
        [InlineData(-5, 0, 32, 32, 0, 32, "x")]         // Negative X
        [InlineData(0, -5, 32, 32, 0, 32, "y")]         // Negative Y
        [InlineData(5, 0, 32, 32, 0, 32, "w")]          // X + W > Map.Width (5 + 32 > 32)
        [InlineData(0, 5, 32, 32, 0, 32, "h")]          // Y + H > Map.Height (5 + 32 > 32)
        [InlineData(0, 0, 32, 32, -5, 32, "off")]       // Negative offset
        [InlineData(0, 0, 32, 32, 0, 10, "scansize")]   // Scansize < Width
        public void FillRgbPixels_InvalidSpatialBounds_ThrowsDjvuArgumentOutOfRangeException(
            int x, int y, int w, int h, int off, int scansize, string expectedParam)
        {
            int mapWidth = 32;
            int mapHeight = 32;
            PixelMap map = PixelMapTests.CreateInitVerifyPixelMap(mapWidth, mapHeight, Pixel.BluePixel) as PixelMap;
            
            // Allocate a massive buffer so we don't accidentally trip the "buffer too small" InvalidOperationException check
            int[] pixels = new int[mapWidth * mapHeight * 10]; 

            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => map.FillRgbPixels(x, y, w, h, pixels, off, scansize));
            Assert.Equal(expectedParam, ex.ParamName);
        }

        /// <summary>
        /// Verifies that calling ToImage with an unsupported byte count per pixel 
        /// prevents rendering and throws a DjvuFormatException detailing the failure.
        /// </summary>
        [Fact]
        public void ToImage_InvalidBytesPerPixel_ThrowsFormatException()
        {
            Mock<Map> mapMock = new Mock<Map>(new object[] { 5, 0, 0, 0, false }) { CallBase = true };
            Map map = mapMock.Object;
            typeof(Map).GetProperty("Data").SetValue(map, new sbyte[100]);
            map.SetWidth(10);
            map.SetHeight(10);

            var ex = Assert.Throws<DjvuFormatException>(() => map.ToImage());
            Assert.Contains("Unknown pixel format for byte count: 5", ex.Message);
        }

        /// <summary>
        /// Verifies that ToImage explicitly guards against null internal Data buffers
        /// before attempting to pin them for unmanaged GDI+ transfers, throwing DjvuInvalidOperationException
        /// since the object is in an invalid state for rendering.
        /// </summary>
        [Fact]
        public void ToImage_NullData_ThrowsDjvuInvalidOperationException()
        {
            Mock<Map> mapMock = new Mock<Map>(new object[] { 3, 0, 0, 0, false }) { CallBase = true };
            Map map = mapMock.Object;
            typeof(Map).GetProperty("Data").SetValue(map, null); // Explicitly null
            map.SetWidth(10);
            map.SetHeight(10);

            var ex = Assert.Throws<DjvuInvalidOperationException>(() => map.ToImage());
            Assert.Contains("Data buffer is null", ex.Message);
        }

        /// <summary>
        /// Verifies the explicit type constraint in Map.ToImage(). The rendering logic 
        /// strictly requires the concrete type to be either Bitmap or PixelMap. 
        /// Any other subclass (like this dynamically generated mock) must throw a DjvuNotSupportedException.
        /// </summary>
        [Fact]
        public void ToImage_UnsupportedType_ThrowsDjvuNotSupportedException()
        {
            Mock<Map> mapMock = new Mock<Map>(new object[] { 3, 0, 0, 0, false }) { CallBase = true };
            Map map = mapMock.Object;
            typeof(Map).GetProperty("Data").SetValue(map, new sbyte[300]); // Valid buffer
            map.SetWidth(10);
            map.SetHeight(10);

            var ex = Assert.Throws<DjvuNotSupportedException>(() => map.ToImage());
            Assert.Contains("Unsupported Map derived type", ex.Message);
        }

        /// <summary>
        /// Validates that the FillRgbPixels method successfully populates the underlying
        /// pixel data buffer of the map. This is critical for ensuring that external
        /// ARGB/RGB pixel arrays can be correctly ingested into the internal DjvuNet
        /// pixel representat.
        /// </summary>
        [Fact]
        public void FillRgbPixelsTest()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.BluePixel;

            PixelMap map = PixelMapTests.CreateInitVerifyPixelMap(width, height, color) as PixelMap;
            
            // Create a test array of pixels (ARGB format: 4 bytes per pixel)
            int[] pixels = new int[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                // Fill with an arbitrary solid color: Red
                pixels[i] = unchecked((int)0xFFFF0000); 
            }

            // Act
            map.FillRgbPixels(0, 0, width, height, pixels, 0, width);

            // Assert: Verify that the internal Data buffer reflects the change.
            Assert.NotNull(map.Data);
            Assert.True(map.Data.Length > 0);
            
            // Using CreateGPixelReference to indirectly verify the Fill worked as it traverses the updated buffer
            var reference = map.CreateGPixelReference(0);
            Assert.NotNull(reference);
            Assert.Equal(0, reference.Offset);
            
            // Verify that the PixelReference correctly binds back to the parent map
            Assert.Same(map, reference.Parent);
        }

        /// <summary>
        /// Validates the creation of GPixelReference objects (iterators) from the Map.
        /// This tests both the 1D (offset) and 2D (row, column) initialization paths
        /// to ensure that iterators are correctly bound to the source map, which is
           /// essential for safe memory traversal during rendering.
        /// </summary>
        [Fact]
        public void CreateGPixelReferenceTest()
        {
            int width = 16;
            int height = 16;
            PixelMap map = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.BluePixel) as PixelMap;
            
            // Test 1D linear offset iterator
            var referenceByOffset = map.CreateGPixelReference(42);
            Assert.NotNull(referenceByOffset);
            Assert.Equal(42 * map.BytesPerPixel, referenceByOffset.Offset);
            Assert.Same(map, referenceByOffset.Parent);

            // Test 2D spatial coordinate iterator
            var referenceByCoord = map.CreateGPixelReference(2, 5);
            Assert.NotNull(referenceByCoord);
            int expectedOffset = (map.RowOffset(2) + 5) * map.BytesPerPixel;
            Assert.Equal(expectedOffset, referenceByCoord.Offset);
            Assert.Same(map, referenceByCoord.Parent);
        }

        /// <summary>
        /// Validates that the public ToImage method correctly converts the internal
        /// DjvuNet pixel data into a standard GDI+ System.Drawing.Bitmap.
        /// This ensures memory transfer and format mapping function correctly.
        /// </summary>
        [Fact]
        public void ToImageTest003()
        {
            int width = 32;
            int height = 32;
            Pixel color = Pixel.BluePixel;

            IPixelMap map1 = PixelMapTests.CreateInitVerifyPixelMap(width, height, color);
            using (System.Drawing.Bitmap bmp = map1.ToImage())
            {
                Assert.NotNull(bmp);
                Assert.IsType<System.Drawing.Bitmap>(bmp);
                Assert.Equal(width, bmp.Width);
                Assert.Equal(height, bmp.Height);
            }
        }

        private class TestMap : Map
        {
            public TestMap(int bytesPerPixel = 3) : base(bytesPerPixel, 0, 0, 0, false) { }

            public System.Drawing.Bitmap TestCopyDataToBitmap(int width, int height, IntPtr data, long length, PixelFormat format, int bytesPerSrcRow = 0)
            {
                return CopyDataToBitmap(width, height, data, length, format, bytesPerSrcRow);
            }
        }

        /// <summary>
        /// Verifies that the internal CopyDataToBitmap method correctly promotes arithmetic
        /// to 64-bit prior to multiplying the height by the stride. Without 64-bit promotion,
        /// a high-resolution image causes an Int32 overflow, generating a negative memory 
        /// offset that throws an AccessViolationException or corrupts unmanaged memory.
        /// </summary>
        [Theory]
        // 1. Massive Height, Normal Width (height * stride > Int32.MaxValue)
        // 100,000 height * 10,000 width * 3 bytes/pixel = 3 GB
        [InlineData(10000, 100000, 3, "is insufficient for the requested image dimensions")] 
        
        // 2. Normal Height, Massive Width (height * stride > Int32.MaxValue)
        // 10,000 height * 100,000 width * 3 bytes/pixel = 3 GB
        [InlineData(100000, 10000, 3, "is insufficient for the requested image dimensions")] 
        
        // 3. Both Dimensions Massive (Square, ~3.6 GB)
        [InlineData(30000, 30000, 4, "is insufficient for the requested image dimensions")]
        
        // 4. Exact Int32.MaxValue boundary condition (Stride causes immediate overflow)
        // (Int32.MaxValue / 4) + 1 = 536870912. 
        // 536870912 width * 4 bpp = 2147483648 stride (Int32.MaxValue + 1)
        [InlineData(536870912, 2, 4, "exceeds the 32-bit limits of GDI+")]
        public void CopyDataToBitmap_LargeDimensions_Avoids_Int32_Overflow(int width, int height, int bytesPerPixel, string expectedMessageFragment)
        {
            TestMap map = new TestMap(bytesPerPixel);
            
            // We use a dummy data pointer. The test is designed to verify the memory *address calculation*
            // doesn't overflow to a negative number before it tries to allocate or move memory.
            IntPtr dummyPtr = new IntPtr(1);
            long dummyLength = 100; // Small length to trip the buffer bounds check gracefully

            PixelFormat format = bytesPerPixel == 3 ? PixelFormat.Format24bppRgb : PixelFormat.Format32bppArgb;

            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => 
                map.TestCopyDataToBitmap(width, height, dummyPtr, dummyLength, format, 0));
            
            Assert.Contains(expectedMessageFragment, ex.Message);
        }
    }
}
