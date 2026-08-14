using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using DjvuNet.Errors;
using DjvuNet.Extensions;
using DjvuNet.Graphics;
using Bitmap = DjvuNet.Graphics.Bitmap;
using GdiBitmap = System.Drawing.Bitmap;

namespace DjvuNet.Extensions.Tests
{
    public class ExtensionMethodsTests
    {
        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void OrientRectangleTest()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void OrientRectangleTest1()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact]
        public void ToImage_Bitmap_NullData_Throws()
        {
            Bitmap bmp = default(Bitmap); 
            
            var ex = Assert.Throws<DjvuInvalidOperationException>(() => bmp.ToImage());
            Assert.Contains("buffer is null", ex.Message);
        }

        [Fact]
        public void ToImage_Bitmap_ZeroDimensions_Throws()
        {
            Bitmap bmp = new Bitmap(0, 0, 0); 
            
            var ex = Assert.Throws<DjvuInvalidOperationException>(() => bmp.ToImage());
            Assert.Contains("Dimensions must be greater than zero", ex.Message);
        }

        [Fact]
        public void ToImage_Bitmap_InsufficientBuffer_Throws()
        {
            Bitmap originalBmp = new Bitmap(10, 10, 0);
            
            object boxedBmp = originalBmp;
            PropertyInfo dataProp = typeof(Bitmap).GetProperty("Data", BindingFlags.Instance | BindingFlags.Public);
            dataProp.SetValue(boxedBmp, new sbyte[5]); 
            
            Bitmap bmp = (Bitmap)boxedBmp;

            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => bmp.ToImage());
            Assert.Contains("insufficient for the requested image dimensions", ex.Message);
        }

        [Fact]
        public void ToImage_Bitmap_BiLevel()
        {
            Bitmap bmp = new Bitmap(10, 10, 0);
            bmp.Grays = 2;

            using (GdiBitmap image = bmp.ToImage())
            {
                Assert.NotNull(image);
                Assert.Equal(PixelFormat.Format8bppIndexed, image.PixelFormat);
                Assert.Equal(10, image.Width);
                Assert.Equal(10, image.Height);
                
                ColorPalette palette = image.Palette;
                Assert.Equal(Color.FromArgb(255, 0, 0, 0), palette.Entries[0]);
                Assert.Equal(Color.FromArgb(255, 255, 255, 255), palette.Entries[1]);
            }
        }

        [Fact]
        public void ToImage_Bitmap_Grayscale()
        {
            Bitmap bmp = new Bitmap(10, 10, 0);
            bmp.Grays = 256; 

            using (GdiBitmap image = bmp.ToImage())
            {
                Assert.NotNull(image);
                Assert.Equal(PixelFormat.Format8bppIndexed, image.PixelFormat);
                
                ColorPalette palette = image.Palette;
                Assert.Equal(Color.FromArgb(255, 255, 255, 255), palette.Entries[0]);
                Assert.Equal(Color.FromArgb(255, 127, 127, 127), palette.Entries[128]);
                Assert.Equal(Color.FromArgb(255, 0, 0, 0), palette.Entries[255]);
            }
        }

        [Fact]
        public void ToImage_PixelMap_NullInstance_Throws()
        {
            PixelMap pixelMap = null;
            var ex = Assert.Throws<DjvuArgumentNullException>(() => pixelMap.ToImage());
            Assert.Equal("pixmp", ex.ParamName);
        }

        [Fact]
        public void ToImage_PixelMap_NullData_Throws()
        {
            var pixelMap = new PixelMap(); 
            var ex = Assert.Throws<DjvuInvalidOperationException>(() => pixelMap.ToImage());
            Assert.Contains("buffer is null", ex.Message);
        }

        [Fact]
        public void ToImage_PixelMap_ZeroDimensions_Throws()
        {
            var pixelMap = new PixelMap();
            typeof(PixelMap).GetProperty("Data", BindingFlags.Instance | BindingFlags.Public)
                .SetValue(pixelMap, new sbyte[10]);
            
            var ex = Assert.Throws<DjvuInvalidOperationException>(() => pixelMap.ToImage());
            Assert.Contains("Dimensions must be greater than zero", ex.Message);
        }

        [Fact]
        public void ToImage_PixelMap_InsufficientBuffer_Throws()
        {
            sbyte[] validData = new sbyte[10 * 10 * 3]; 
            var pixelMap = new PixelMap(validData, 10, 10);
            
            typeof(PixelMap).GetProperty("Data", BindingFlags.Instance | BindingFlags.Public)
                .SetValue(pixelMap, new sbyte[5]);
            
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => pixelMap.ToImage());
            Assert.Contains("insufficient for the requested image dimensions", ex.Message);
        }

        [Fact]
        public void ToImage_PixelMap_ValidData()
        {
            sbyte[] data = new sbyte[10 * 10 * 3];
            var pixelMap = new PixelMap(data, 10, 10);

            using (GdiBitmap image = pixelMap.ToImage())
            {
                Assert.NotNull(image);
                Assert.Equal(PixelFormat.Format24bppRgb, image.PixelFormat);
                Assert.Equal(10, image.Width);
                Assert.Equal(10, image.Height);
            }
        }
    }
}
