using System;
using System.Collections.Generic;
using DjvuNet.Graphics;
using Xunit;

namespace DjvuNet.JB2.Tests
{
    public class JB2ShapeTests
    {
        [Fact]
        public void DefaultConstructor_InitializesEmpty()
        {
            var shape = new JB2Shape();
            Assert.Equal(0, shape.Parent);
            Assert.Equal(0, shape.UserData);
            Assert.Equal<Bitmap>(default, shape.Bitmap);
        }

        [Fact]
        public void ParentConstructor_InitializesWithParentAndBitmap()
        {
            var shape = new JB2Shape(5);
            Assert.Equal(5, shape.Parent);
            Assert.NotNull(shape.Bitmap);
            Assert.IsType<Bitmap>(shape.Bitmap);
        }

        [Fact]
        public void Init_SetsParentAndCreatesBitmap()
        {
            var shape = new JB2Shape();
            var result = shape.Init(10);
            
            Assert.Equal<JB2Shape>(shape, result);
            Assert.Equal(10, shape.Parent);
            Assert.NotNull(shape.Bitmap);
            Assert.IsType<Bitmap>(shape.Bitmap);
        }

        [Fact]
        public void Duplicate_CreatesDeepCopyOfBitmap()
        {
            var shape = new JB2Shape(1);
            shape.UserData = 999;
            // Initialize the bitmap so Duplicate() has something to copy
            shape.Bitmap.Init(10, 10, 0); 
            
            var duplicate = shape.Duplicate();

            Assert.Equal<JB2Shape>(shape, duplicate);
            Assert.Equal<sbyte[]>(shape.Bitmap.Data, duplicate.Bitmap.Data);
            Assert.Equal(shape.Parent, duplicate.Parent);
            Assert.Equal(shape.UserData, duplicate.UserData);
            
            Assert.Equal<Bitmap>(shape.Bitmap, duplicate.Bitmap); // Bitmap must be deeply cloned
        }

        [Fact]
        public void Duplicate_HandlesDefaultBitmap()
        {
            var shape = new JB2Shape(); // Default constructor leaves default Bitmap
            shape.Parent = 2;
            shape.UserData = 123;

            var duplicate = shape.Duplicate();

            Assert.Equal<JB2Shape>(shape, duplicate);
            Assert.Equal(shape.Parent, duplicate.Parent);
            Assert.Equal(shape.UserData, duplicate.UserData);
            Assert.Equal<Bitmap>(default, duplicate.Bitmap);
        }

        public static TheoryData<JB2Shape, JB2Shape, bool> EqualityTestData()
        {
            var baseBitmap = new Bitmap(10, 10, 0);
            var baseShape = new JB2Shape { Parent = 1, UserData = 100, Bitmap = baseBitmap };
            
            var diffParent = new JB2Shape { Parent = 2, UserData = 100, Bitmap = baseBitmap };
            var diffUserData = new JB2Shape { Parent = 1, UserData = 200, Bitmap = baseBitmap };
            var diffBitmap = new JB2Shape { Parent = 1, UserData = 100, Bitmap = new Bitmap(20, 20, 0) };
            var identicalShape = new JB2Shape { Parent = 1, UserData = 100, Bitmap = baseBitmap };
            var defaultShape = new JB2Shape();
            var anotherDefault = new JB2Shape();

            var data = new TheoryData<JB2Shape, JB2Shape, bool>();
            // [shape1, shape2, expectedEquality]
            data.Add(baseShape, baseShape, true);
            data.Add(baseShape, identicalShape, true);
            data.Add(defaultShape, anotherDefault, true);
            
            data.Add(baseShape, diffParent, false);
            data.Add(baseShape, diffUserData, false);
            data.Add(baseShape, diffBitmap, false);
            data.Add(baseShape, defaultShape, false);

            return data;
        }

        [Theory]
        [MemberData(nameof(EqualityTestData))]
        public void EqualsJB2Shape(JB2Shape shape1, JB2Shape shape2, bool expected)
        {
            Assert.Equal(expected, shape1.Equals(shape2));
        }

        [Theory]
        [MemberData(nameof(EqualityTestData))]
        public void EqualityOperatorEquals(JB2Shape shape1, JB2Shape shape2, bool expected)
        {
            Assert.Equal(expected, shape1 == shape2);
        }

        [Theory]
        [MemberData(nameof(EqualityTestData))]
        public void EqualityOperatorNotEquals(JB2Shape shape1, JB2Shape shape2, bool expected)
        {
            Assert.Equal(!expected, shape1 != shape2);
        }

        [Theory]
        [MemberData(nameof(EqualityTestData))]
        public void EqualsObject(JB2Shape shape1, object obj, bool expected)
        {
            // Note: 'obj' parameter inherently receives a boxed JB2Shape from EqualityTestData
            Assert.Equal(expected, shape1.Equals(obj));
        }

        [Fact]
        public void EqualsObject_BoxedIdenticalStruct_ReturnsTrue()
        {
            var shape1 = new JB2Shape { Parent = 1, UserData = 100 };
            var identicalShape = new JB2Shape { Parent = 1, UserData = 100 };
            
            object boxedIdentical = identicalShape;

            Assert.True(shape1.Equals(boxedIdentical));
        }

        [Fact]
        public void EqualsObject_BoxedDifferentStruct_ReturnsFalse()
        {
            var shape1 = new JB2Shape { Parent = 1, UserData = 100 };
            var diffShape = new JB2Shape { Parent = 2, UserData = 100 };

            object boxedDiff = diffShape;

            Assert.False(shape1.Equals(boxedDiff));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Not A Shape")]
        [InlineData(123)]
        public void EqualsObject_NullOrDifferentType_ReturnsFalse(object obj)
        {
            var shape = new JB2Shape { Parent = 1, UserData = 100 };
            Assert.False(shape.Equals(obj));
        }

        [Theory]
        [MemberData(nameof(EqualityTestData))]
        public void GetHashCode_MatchesEquality(JB2Shape shape1, JB2Shape shape2, bool expectedEquality)
        {
            Assert.Equal(expectedEquality, shape1.GetHashCode() == shape2.GetHashCode());
        }
    }
}
