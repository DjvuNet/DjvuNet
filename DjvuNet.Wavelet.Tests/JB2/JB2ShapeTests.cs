using Xunit;
using DjvuNet.JB2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.JB2.Tests
{
    public class JB2ShapeTests
    {
        [Fact]
        public void JB2Shape_DefaultConstructor_InitializesEmpty()
        {
            var shape = new JB2Shape();
            Assert.Equal(0, shape.Parent);
            Assert.Equal(0, shape.UserData);
            Assert.Null(shape.Bitmap);
        }

        [Fact]
        public void JB2Shape_ParentConstructor_InitializesWithParentAndBitmap()
        {
            var shape = new JB2Shape(5);
            Assert.Equal(5, shape.Parent);
            Assert.NotNull(shape.Bitmap);
            Assert.IsType<DjvuNet.Graphics.Bitmap>(shape.Bitmap);
        }

        [Fact]
        public void JB2Shape_Init_SetsParentAndCreatesBitmap()
        {
            var shape = new JB2Shape();
            var result = shape.Init(10);
            
            Assert.Same(shape, result);
            Assert.Equal(10, shape.Parent);
            Assert.NotNull(shape.Bitmap);
            Assert.IsType<DjvuNet.Graphics.Bitmap>(shape.Bitmap);
        }

        [Fact]
        public void JB2Shape_Duplicate_CreatesDeepCopyOfBitmap()
        {
            var shape = new JB2Shape(1);
            shape.UserData = 999;
            // Initialize the bitmap so Duplicate() has something to copy
            shape.Bitmap.Init(10, 10, 0); 
            
            var duplicate = shape.Duplicate();

            Assert.NotSame(shape, duplicate);
            Assert.Equal(shape.Parent, duplicate.Parent);
            Assert.Equal(shape.UserData, duplicate.UserData);
            
            Assert.NotNull(duplicate.Bitmap);
            Assert.NotSame(shape.Bitmap, duplicate.Bitmap); // Bitmap must be deeply cloned
        }

        [Fact]
        public void JB2Shape_Duplicate_HandlesNullBitmap()
        {
            var shape = new JB2Shape(); // Default constructor leaves Bitmap null
            shape.Parent = 2;
            shape.UserData = 123;

            var duplicate = shape.Duplicate();

            Assert.NotSame(shape, duplicate);
            Assert.Equal(shape.Parent, duplicate.Parent);
            Assert.Equal(shape.UserData, duplicate.UserData);
            Assert.Null(duplicate.Bitmap);
        }
    }
}