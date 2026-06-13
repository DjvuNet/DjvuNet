using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using DjvuNet.Graphics;
using GdiRect = System.Drawing.Rectangle;

namespace DjvuNet.Graphics.Tests
{
    public class RectangleTests
    {
        private const int Min = int.MinValue;
        private const int Max = int.MaxValue;

        [Theory]
        // --- Standard Valid Inputs ---
        [InlineData(10, 20, 50, 60, 10, 20, 60, 80)]
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0)]
        [InlineData(-10, -20, 50, 60, -10, -20, 40, 40)]

        // --- Near MinValue Valid ---
        [InlineData(Min, Min, 10, 10, Min, Min, Min + 10, Min + 10)]
        [InlineData(Min + 5, Min + 5, 10, 10, Min + 5, Min + 5, Min + 15, Min + 15)]

        // --- Near MaxValue Valid ---
        // Just touching MaxValue
        [InlineData(Max - 10, Max - 10, 10, 10, Max - 10, Max - 10, Max, Max)]
        [InlineData(Max - 1, Max - 1, 1, 1, Max - 1, Max - 1, Max, Max)]

        // --- Overflowing MaxValue (Clamps to Empty, so XMax == XMin) ---
        // Barely overflowing MaxValue
        [InlineData(Max - 5, Max - 5, 10, 10, Max - 5, Max - 5, Max - 5, Max - 5)]
        [InlineData(Max - 1, Max - 1, 2, 2, Max - 1, Max - 1, Max - 1, Max - 1)]
        // Exactly at MaxValue
        [InlineData(Max, Max, 10, 10, Max, Max, Max, Max)]
        [InlineData(Max, Max, 0, 0, Max, Max, Max, Max)]
        public void Constructor_SetsCoordinatesCorrectly(
            int xMinInput, int yMinInput, int widthInput, int heightInput,
            int expectedXMin, int expectedYMin, int expectedXMax, int expectedYMax)
        {
            // Act
            var rect = new Rectangle(xMinInput, yMinInput, widthInput, heightInput);

            // Assert
            Assert.Equal(expectedXMin, rect.XMin);
            Assert.Equal(expectedYMin, rect.YMin);
            Assert.Equal(expectedXMax, rect.XMax);
            Assert.Equal(expectedYMax, rect.YMax);
        }

        [Theory]
        [InlineData(10, 20, 50, 60, 50)]
        [InlineData(0, 0, 0, 0, 0)]
        [InlineData(-10, -20, 50, 60, 50)]
        public void Width_CalculatedCorrectly(int x, int y, int w, int h, int expectedWidth)
        {
            var rect = new Rectangle(x, y, w, h);
            Assert.Equal(expectedWidth, rect.Width);
        }

        [Theory]
        [InlineData(10, 20, 50, 60, 60)]
        [InlineData(0, 0, 0, 0, 0)]
        [InlineData(-10, -20, 50, 60, 60)]
        public void Height_CalculatedCorrectly(int x, int y, int w, int h, int expectedHeight)
        {
            var rect = new Rectangle(x, y, w, h);
            Assert.Equal(expectedHeight, rect.Height);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, true)]       // Default / Zero width & height
        [InlineData(10, 10, 0, 50, true)]    // Zero width
        [InlineData(10, 10, 50, 0, true)]    // Zero height
        [InlineData(10, 10, 1, 1, false)]    // Valid 1x1
        [InlineData(-10, -10, 50, 50, false)] // Valid crossing origin
        public void Empty_EvaluatesCorrectly(int x, int y, int w, int h, bool expectedEmpty)
        {
            var rect = new Rectangle(x, y, w, h);
            Assert.Equal(expectedEmpty, rect.Empty);
        }

        [Theory]
        [InlineData(0, 0, 10, 10, 100L)]
        [InlineData(-5, -5, 10, 10, 100L)]
        [InlineData(0, 0, 0, 0, 0L)]
        [InlineData(10, 10, 0, 50, 0L)]     // Zero width
        // Extreme bounds: The maximum width/height is bounded by Int32.MaxValue (2,147,483,647).
        // 2,147,483,647 * 2,147,483,647 = 4,611,686,014,132,420,609, which safely fits inside a signed Int64 (long).
        [InlineData(-1000000000, -1000000000, Max, Max, 4611686014132420609L)] 
        public void Area_CalculatesCorrectly(int x, int y, int w, int h, long expectedArea)
        {
            var rect = new Rectangle(x, y, w, h);
            Assert.Equal(expectedArea, rect.Area);
        }

        [Fact]
        public void Equality_EmptyRectangles_AreAlwaysEqual()
        {
            var r1 = new Rectangle(0, 0, 0, 0);     // Empty at origin
            var r2 = new Rectangle(100, 100, 0, 0); // Empty at different location

            Assert.True(r1.Empty);
            Assert.True(r2.Empty);

            // Test all unified comparators
            Assert.True(r1.Equals(r2));
            Assert.True(r1.Equals((object)r2));
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void Equality_ValidRectangles_CompareCoordinates()
        {
            var r1 = new Rectangle(10, 10, 50, 50);
            var r2 = new Rectangle(10, 10, 50, 50);
            var r3 = new Rectangle(11, 10, 50, 50);

            Assert.True(r1 == r2);
            Assert.False(r1 == r3);
            Assert.True(r1 != r3);
        }

        [Fact]
        public void Equality_OneEmptyOneValid_NotEqual()
        {
            var empty = new Rectangle(0, 0, 0, 0);
            var valid = new Rectangle(0, 0, 10, 10);

            Assert.False(empty == valid);
            Assert.False(empty.Equals(valid));
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 100, 100, 0, 0)] // Origin vs Inverted
        [InlineData(10, 10, 0, 50, 10, 10, 50, 0)] // Zero Width vs Zero Height
        [InlineData(-50, -50, 0, 0, 999, 999, 0, 0)] // Negative origin vs Large origin inverted
        public void GetHashCode_EmptyRectangles_HaveIdenticalHashes(
            int x1, int y1, int w1, int h1,
            int x2, int y2, int w2, int h2)
        {
            var r1 = new Rectangle(x1, y1, w1, h1);
            var r2 = new Rectangle(x2, y2, w2, h2);

            Assert.True(r1.Empty);
            Assert.True(r2.Empty);

            // Contract: If Equals() is true, GetHashCode() MUST be equal.
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
        }

        [Theory]
        [InlineData(0, 0, 50, 50, 10, 10, 50, 50)]     // Same dimensions, shifted origin
        [InlineData(-10, -10, 50, 50, 10, 10, 50, 50)] // Same dimensions, mirrored origin
        [InlineData(0, 0, 10, 20, 0, 0, 20, 10)]       // Same origin, swapped dimensions
        [InlineData(10, 20, 30, 40, 20, 10, 40, 30)]   // Completely different mapping yielding same derived properties
        public void GetHashCode_DifferentCoordinates_HaveDifferentHashes(
            int x1, int y1, int w1, int h1,
            int x2, int y2, int w2, int h2)
        {
            var r1 = new Rectangle(x1, y1, w1, h1);
            var r2 = new Rectangle(x2, y2, w2, h2);

            Assert.False(r1.Empty);
            Assert.False(r2.Empty);

            // Fixes the specific bug reported in GH #149 where derived dimensions caused collisions.
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
        }

        [Theory]
        [InlineData(10, 20, 30, 40, true)]  
        [InlineData(-5, -5, 100, 100, true)] 
        [InlineData(0, 0, 0, 0, true)]      
        [InlineData(100, 100, 0, 0, true)]  
        [InlineData(10, 10, 0, 50, true)]   
        [InlineData(0, 0, 30, 40, false)]
        [InlineData(10, 20, 40, 30, false)]
        [InlineData(20, 10, 40, 30, false)] 
        public void Hashtable_Contains_IdentifiesKeyPresenceCorrectly(int x, int y, int w, int h, bool expectedToExist)
        {
            var hashTable = new Hashtable
            {
                { new Rectangle(10, 20, 30, 40), "BaseRect1" },
                { new Rectangle(-5, -5, 100, 100), "BaseRect2" },
                { new Rectangle(), "EmptyRect" }
            };
            
            var testRect = new Rectangle(x, y, w, h);
            
            // Contains invokes boxed GetHashCode() and Equals(object)
            Assert.Equal(expectedToExist, hashTable.Contains(testRect));
        }

        [Theory]
        [InlineData(10, 20, 30, 40)]  
        [InlineData(-5, -5, 100, 100)] 
        [InlineData(0, 0, 0, 0)]      
        [InlineData(100, 100, 0, 0)]  
        [InlineData(10, 10, 0, 50)]   
        public void Hashtable_Add_DuplicateKey_ThrowsArgumentException(int x, int y, int w, int h)
        {
            var hashTable = new Hashtable
            {
                { new Rectangle(10, 20, 30, 40), "BaseRect1" },
                { new Rectangle(-5, -5, 100, 100), "BaseRect2" },
                { new Rectangle(), "EmptyRect" }
            };
            
            var testRect = new Rectangle(x, y, w, h);
            
            var ex = Assert.Throws<ArgumentException>(() => hashTable.Add(testRect, "Duplicate"));
            Assert.Contains("Item has already been added", ex.Message);
        }

        [Theory]
        [InlineData(10, 20, 30, 40, true)]  
        [InlineData(-5, -5, 100, 100, true)] 
        [InlineData(0, 0, 0, 0, true)]      
        [InlineData(100, 100, 0, 0, true)]  
        [InlineData(10, 10, 0, 50, true)]   
        [InlineData(0, 0, 30, 40, false)]
        [InlineData(10, 20, 40, 30, false)]
        [InlineData(20, 10, 40, 30, false)] 
        public void Dictionary_ContainsKey_IdentifiesKeyPresenceCorrectly(int x, int y, int w, int h, bool expectedToExist)
        {
            var dictionary = new Dictionary<Rectangle, Rectangle>
            {
                { new Rectangle(10, 20, 30, 40), new Rectangle(10, 20, 30, 40) },
                { new Rectangle(-5, -5, 100, 100), new Rectangle(-5, -5, 100, 100) },
                { new Rectangle(), new Rectangle() }
            };
            
            var testRect = new Rectangle(x, y, w, h);
            
            // ContainsKey invokes strongly-typed GetHashCode() and IEquatable<T>.Equals(T)
            Assert.Equal(expectedToExist, dictionary.ContainsKey(testRect));
        }

        [Theory]
        [InlineData(10, 20, 30, 40)]  
        [InlineData(-5, -5, 100, 100)] 
        [InlineData(0, 0, 0, 0)]      
        [InlineData(100, 100, 0, 0)]  
        [InlineData(10, 10, 0, 50)]   
        public void Dictionary_Add_DuplicateKey_ThrowsArgumentException(int x, int y, int w, int h)
        {
            var dictionary = new Dictionary<Rectangle, Rectangle>
            {
                { new Rectangle(10, 20, 30, 40), new Rectangle(10, 20, 30, 40) },
                { new Rectangle(-5, -5, 100, 100), new Rectangle(-5, -5, 100, 100) },
                { new Rectangle(), new Rectangle() }
            };
            
            var testRect = new Rectangle(x, y, w, h);
            
            var ex = Assert.Throws<ArgumentException>(() => dictionary.Add(testRect, testRect));
            Assert.Contains("An item with the same key has already been added", ex.Message);
        }

        [Fact]
        public void ImplicitCast_ToGDIRectangle_MapsCoordinatesCorrectly()
        {
            // DjVu Cartesian: origin 10,20. Width 50, Height 60.
            // XMin = 10, YMin = 20, XMax = 60, YMax = 80.
            var rect = new Rectangle(10, 20, 50, 60);

            GdiRect gdiRect = rect;

            // GDI+ top-left origin should map to DjVu XMin and YMin.
            Assert.Equal(10, gdiRect.X);
            Assert.Equal(20, gdiRect.Y);
            Assert.Equal(50, gdiRect.Width);
            Assert.Equal(60, gdiRect.Height);
        }

        [Fact]
        public void ImplicitCast_EmptyRectangle_YieldsEmptyGDI()
        {
            var rect = new Rectangle(100, 100, 0, 0); // Mathematically empty
            GdiRect gdiRect = rect;

            Assert.True(gdiRect.IsEmpty);
        }

        [Theory]
        [InlineData(10, 20, 50, 60, 5, 5, 15, 25, 65, 85)] // Translate by +5,+5
        [InlineData(10, 20, 50, 60, -10, -20, 0, 0, 50, 60)] // Translate to origin
        public void Translate_ValidInputs_ShiftsCoordinatesCorrectly(
            int x, int y, int w, int h, int dx, int dy,
            int expectedXMin, int expectedYMin, int expectedXMax, int expectedYMax)
        {
            var rect = new Rectangle(x, y, w, h);
            rect.Translate(dx, dy);

            Assert.Equal(expectedXMin, rect.XMin);
            Assert.Equal(expectedYMin, rect.YMin);
            Assert.Equal(expectedXMax, rect.XMax);
            Assert.Equal(expectedYMax, rect.YMax);
        }

        [Theory]
        [InlineData(Max, Max, 0, 0, 1, 0)] // XMin + 1 = overflow
        [InlineData(Min, Min, 0, 0, -1, 0)] // XMin - 1 = underflow
        [InlineData(0, Max, 0, 0, 0, 1)] // YMin + 1 = overflow
        [InlineData(0, Min, 0, 0, 0, -1)] // YMin - 1 = underflow
        public void Translate_Overflow_ThrowsDjvuArgumentOutOfRangeException(
            int x, int y, int w, int h, int dx, int dy)
        {
            var rect = new Rectangle(x, y, w, h);
            var ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => rect.Translate(dx, dy));
            Assert.Contains("caused an overflow/underflow of the internal Int32 coordinate boundaries", ex.Message);
        }

        [Theory]
        [InlineData(10, 20, 50, 60, 5, 5, 5, 15, 65, 85)] // Inflate by 5 (grows outward)
        [InlineData(10, 20, 50, 60, -5, -5, 15, 25, 55, 75)] // Deflate by 5 (shrinks inward)
        public void Inflate_ValidInputs_ChangesCoordinatesCorrectly(
            int x, int y, int w, int h, int dx, int dy,
            int expectedXMin, int expectedYMin, int expectedXMax, int expectedYMax)
        {
            var rect = new Rectangle(x, y, w, h);
            rect.Inflate(dx, dy);

            Assert.Equal(expectedXMin, rect.XMin);
            Assert.Equal(expectedYMin, rect.YMin);
            Assert.Equal(expectedXMax, rect.XMax);
            Assert.Equal(expectedYMax, rect.YMax);
        }

        [Theory]
        [InlineData(Min + 1, Min + 1, 10, 10, 2, 0)] // XMin - 2 = underflow (XMin -= dx)
        [InlineData(Max - 10, Max - 10, 5, 5, 6, 0)] // XMax + 6 = overflow (XMax += dx)
        [InlineData(Min + 1, Min + 1, 10, 10, 0, 2)] // YMin - 2 = underflow
        [InlineData(Max - 10, Max - 10, 5, 5, 0, 6)] // YMax + 6 = overflow
        public void Inflate_Overflow_ThrowsDjvuArgumentOutOfRangeException(
            int x, int y, int w, int h, int dx, int dy)
        {
            var rect = new Rectangle(x, y, w, h);
            var ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => rect.Inflate(dx, dy));
            Assert.Contains("caused an overflow/underflow of the internal Int32 coordinate boundaries", ex.Message);
        }
        [Theory]
        // Base rectangle: XMin=10, YMin=10, Width=10, Height=10 => XMax=20, YMax=20
        [InlineData(10, 10, true)]   // Exactly on XMin, YMin (Inclusive -> IN)
        [InlineData(15, 15, true)]   // Dead center (IN)
        [InlineData(20, 10, false)]  // On XMax boundary (Exclusive -> OUT)
        [InlineData(10, 20, false)]  // On YMax boundary (Exclusive -> OUT)
        [InlineData(20, 20, false)]  // On XMax and YMax boundary (Exclusive -> OUT)
        [InlineData(19, 19, true)]   // Just inside XMax and YMax (IN)
        [InlineData(9, 10, false)]   // Just outside XMin (OUT)
        [InlineData(10, 9, false)]   // Just outside YMin (OUT)
        public void ContainsPoint_ExclusiveBoundaries_EvaluatesCorrectly(int testX, int testY, bool expected)
        {
            var r = new Rectangle(10, 10, 10, 10); // XMax = 20, YMax = 20
            Assert.Equal(expected, r.Contains(testX, testY));
        }

        [Theory]
        [InlineData(10, 10, 50, 50, 10, 10, true)] // Point exactly on XMin, YMin (Bottom-Left edge)
        [InlineData(10, 10, 50, 50, 60, 60, false)] // Point exactly on XMax, YMax (Top-Right edge - Exclusive)
        [InlineData(10, 10, 50, 50, 9, 10, false)] // Just outside left edge
        [InlineData(10, 10, 50, 50, 61, 60, false)] // Just outside right edge
        [InlineData(10, 10, 50, 50, 10, 9, false)] // Just outside bottom edge
        [InlineData(10, 10, 50, 50, 60, 61, false)] // Just outside top edge
        public void ContainsPoint_BoundaryCases_EvaluatesCorrectly(int x, int y, int w, int h, int testX, int testY, bool expected)
        {
            var r = new Rectangle(x, y, w, h);
            Assert.Equal(expected, r.Contains(testX, testY));
        }

        [Theory]
        // R1 is the "Container". R2 is the "Target".
        // --- Standard Containment ---
        [InlineData(10, 10, 50, 50, 20, 20, 10, 10, true)]   // Fully inside
        [InlineData(10, 10, 50, 50, 10, 10, 50, 50, true)]   // Identical (Self-containment)
        
        // --- Boundary Touching (Must be True) ---
        [InlineData(10, 10, 50, 50, 10, 10, 10, 10, true)]   // Touching bottom-left exactly
        [InlineData(10, 10, 50, 50, 50, 50, 10, 10, true)]   // Touching top-right exactly
        [InlineData(10, 10, 50, 50, 10, 20, 50, 10, true)]   // Spanning full width exactly
        [InlineData(10, 10, 50, 50, 20, 10, 10, 50, true)]   // Spanning full height exactly

        // --- Boundary Exceeding (Must be False) ---
        [InlineData(10, 10, 50, 50, 20, 20, 41, 10, false)]  // Exceeds XMax by 1
        [InlineData(10, 10, 50, 50, 20, 20, 10, 41, false)]  // Exceeds YMax by 1
        [InlineData(10, 10, 50, 50, 9, 20, 10, 10, false)]   // Precedes XMin by 1
        [InlineData(10, 10, 50, 50, 20, 9, 10, 10, false)]   // Precedes YMin by 1
        
        // --- Disjoint / Overlapping ---
        [InlineData(10, 10, 50, 50, 100, 100, 10, 10, false)] // Completely disjoint
        [InlineData(10, 10, 50, 50, 50, 50, 50, 50, false)]   // Overlapping corner but not contained
        [InlineData(10, 10, 10, 10, 0, 0, 50, 50, false)]     // Inverse containment (R2 contains R1)

        // --- Empty Rectangle Edge Cases ---
        [InlineData(10, 10, 50, 50, 0, 0, 0, 0, true)]        // C++ Quirk: Valid Container contains Empty Target
        [InlineData(0, 0, 0, 0, 10, 10, 50, 50, false)]       // Empty Container cannot contain Valid Target
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0, true)]            // Empty Container contains Empty Target (Identical state)
        [InlineData(0, 0, 0, 0, 100, 100, 0, 0, true)]        // C++ Quirk: Empty Container DOES contain Empty Target at a different offset
        public void ContainsRectangle_EvaluatesCorrectly(
            int xMin, int yMin, int width, int height,
            int testXMin, int testYMin, int testWidth, int testHeight,
            bool expected)
        {
            var rect = new Rectangle(xMin, yMin, width, height);
            var testRect = new Rectangle(testXMin, testYMin, testWidth, testHeight);
            
            Assert.Equal(expected, rect.Contains(testRect));
        }

        [Theory]
        [InlineData(0, 0, 10, 10, 20, 20, 10, 10, true)]  // Disjoint (No overlap, produces empty intersection)
        [InlineData(0, 0, 10, 10, 5, 5, 10, 10, false)] // Overlapping (Intersection is valid 5x5 rectangle)
        [InlineData(0, 0, 10, 10, 2, 2, 5, 5, false)] // R2 fully contained within R1
        [InlineData(0, 0, 10, 10, 10, 10, 10, 10, true)]  // Touching at exactly one point (produces mathematically empty rect)
        [InlineData(0, 0, 10, 10, 10, 0, 10, 10, true)]  // Touching along exactly one edge (produces mathematically empty rect)
        public void Intersect_EdgeCases_EvaluatesEmptinessCorrectly(
            int x1, int y1, int w1, int h1,
            int x2, int y2, int w2, int h2,
            bool expectEmpty)
        {
            var r1 = new Rectangle(x1, y1, w1, h1);
            var r2 = new Rectangle(x2, y2, w2, h2);
            var target = new Rectangle();
            target.Intersect(r1, r2);
            Assert.Equal(expectEmpty, target.Empty);
        }

        [Fact]
        public void Intersect_GeometricallyEmpty_CoordinatesAreZeroed()
        {
            // Two completely disjoint rectangles
            var r1 = new Rectangle(0, 0, 10, 10);
            var r2 = new Rectangle(20, 20, 10, 10);
            
            var target = new Rectangle();
            target.Intersect(r1, r2);

            Assert.True(target.Empty);
            
            // This assertion expects strict parity with DjVuLibre C++ behavior
            // where native GRect::intersect calls clear() (zeroing coordinates) if the result is empty.
            Assert.Equal(0, target.XMin);
            Assert.Equal(0, target.YMin);
            Assert.Equal(0, target.XMax);
            Assert.Equal(0, target.YMax);
        }

        [Theory]
        [InlineData(0, 0, 10, 10, 20, 20, 10, 10, 0, 0, 30, 30)] // Hull of disjoint rectangles spans the entire space
        [InlineData(0, 0, 10, 10, 5, 5, 10, 10, 0, 0, 15, 15)] // Hull of overlapping rectangles
        [InlineData(0, 0, 20, 20, 5, 5, 10, 10, 0, 0, 20, 20)] // Hull where R1 fully contains R2 equals R1
        public void Recthull_EdgeCases_SpansCorrectBoundaries(
            int x1, int y1, int w1, int h1,
            int x2, int y2, int w2, int h2,
            int expX, int expY, int expW, int expH)
        {
            var r1 = new Rectangle(x1, y1, w1, h1);
            var r2 = new Rectangle(x2, y2, w2, h2);
            var target = new Rectangle();
            target.Recthull(r1, r2);

            var expected = new Rectangle(expX, expY, expW, expH);
            Assert.Equal(expected, target);
        }

        [Fact]
        public void Recthull_WithEmptyRectangle_ReturnsValidRectangle()
        {
            var emptyRect = new Rectangle(100, 100, 0, 0); // Mathematically empty, offset origin
            var validRect = new Rectangle(0, 0, 20, 20);

            var target1 = new Rectangle();
            target1.Recthull(emptyRect, validRect); // R1 empty, R2 valid

            var target2 = new Rectangle();
            target2.Recthull(validRect, emptyRect); // R1 valid, R2 empty

            Assert.Equal(validRect, target1);
            Assert.Equal(validRect, target2);
        }

        [Theory]
        // 1. Negative dimensions (Invalid parameters)
        [InlineData(0, 0, -10, 10)]
        [InlineData(0, 0, 10, -10)]
        [InlineData(0, 0, -1, -1)]

        // 2. Positive origin + Dimension pushing XMax exactly 1 past Int32.MaxValue
        [InlineData(Max - 100, 0, 101, 10)]
        [InlineData(1, 0, Max, 10)]
        [InlineData(Max, 0, 1, 10)] // Origin exactly at Max, width 1 overflows

        // 3. Positive origin + Dimension pushing YMax exactly 1 past Int32.MaxValue
        [InlineData(0, Max - 100, 10, 101)]
        [InlineData(0, 1, 10, Max)]
        [InlineData(0, Max, 10, 1)] // Origin exactly at Max, height 1 overflows

        // 4. Both axes overflowing simultaneously
        [InlineData(Max / 2, Max / 2, (Max / 2) + 2, (Max / 2) + 2)]
        public void Constructor_InvalidDimensions_ClampsToEmpty(int x, int y, int w, int h)
        {
            var rect = new Rectangle(x, y, w, h);

            Assert.True(rect.Empty, $"Failed to clamp invalid dimensions. State: {rect}");
            Assert.Equal(0L, rect.Area);
            Assert.Equal(x, rect.XMin); // Origin must be preserved
            Assert.Equal(y, rect.YMin);
            Assert.Equal(x, rect.XMax); // Clamped
            Assert.Equal(y, rect.YMax); // Clamped
        }

        [Theory]
        // 1. Maximum valid absolute size from 0,0
        [InlineData(0, 0, Max, Max)]
        // 2. Safely crossing the Y-axis (Negative to Positive X)
        [InlineData(-100, 0, 200, 10)] // Spans from -100 to +100
        // 3. Safely crossing the X-axis (Negative to Positive Y)
        [InlineData(0, -100, 10, 200)] // Spans from -100 to +100
        // 4. Safely crossing both axes from the deep negative quadrant
        [InlineData(-500000, -500000, 1000000, 1000000)]
        // 5. Negative origin pushing exactly to the edge of the 32-bit positive space
        [InlineData(-1, -1, Max, Max)]
        public void Constructor_ValidBoundaryDimensions_InstantiatesCorrectly(int x, int y, int w, int h)
        {
            var rect = new Rectangle(x, y, w, h);

            Assert.False(rect.Empty);
            Assert.Equal(x, rect.XMin);
            Assert.Equal(y, rect.YMin);
            Assert.Equal(x + (int)w, rect.XMax);
            Assert.Equal(y + (int)h, rect.YMax);
        }

        [Fact]
        public void Constructor_AbsoluteLimits_ValidatesSafely()
        {
            // The largest mathematically possible valid rectangle in this architecture:
            // Spans from the absolute bottom-left to just below the origin.
            // (Cannot span to positive space because Width cannot exceed Int32.MaxValue)
            var maxValidRect = new Rectangle(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue);

            Assert.False(maxValidRect.Empty);
            Assert.Equal(int.MinValue, maxValidRect.XMin);
            Assert.Equal(-1, maxValidRect.XMax); // MinValue + MaxValue = -1

            // Expected Area = Int32.MaxValue * Int32.MaxValue 
            long expectedArea = (long)int.MaxValue * (long)int.MaxValue;
            Assert.Equal(expectedArea, maxValidRect.Area);
        }

        [Theory]
        // 1. Astronomical positive spans exceeding Int32
        [InlineData(Min, Max, Min, Max, 0, 0, true)] // X spans 4.2B, Y spans 4.2B -> clamped to 0, structurally Empty
        [InlineData(-10, Max, 0, 10, 0, 0, true)]    // X spans past Int32 -> Empty == true, therefore Width AND Height clamp to 0

        // 2. Inverted coordinates (Empty state)
        [InlineData(100, 50, 0, 10, 0, 0, true)]      // XMin > XMax -> Empty
        [InlineData(0, 10, 50, 20, 0, 0, true)]       // YMin > YMax -> Empty

        // 3. Exactly mathematically Empty (0 span)
        [InlineData(10, 10, 10, 10, 0, 0, true)]

        // 4. Valid mutations near the boundary
        [InlineData(Max - 100, Max, Min, Min + 50, 100, 50, false)]
        public void Properties_MutatedBounds_ClampSafely(
            int xMin, int xMax, int yMin, int yMax,
            int expectedWidth, int expectedHeight, bool expectEmpty)
        {
            var rect = new Rectangle();

            // Raw mutation bypassing constructor safety
            rect.XMin = xMin;
            rect.XMax = xMax;
            rect.YMin = yMin;
            rect.YMax = yMax;

            Assert.Equal(expectEmpty, rect.Empty);
            Assert.Equal(expectedWidth, rect.Width);
            Assert.Equal(expectedHeight, rect.Height);
        }

        [Theory]
        // Translate pushing XMax over the edge
        [InlineData(Max - 10, Max, 0, 10, 1, 0)]
        // Translate pushing YMax over the edge
        [InlineData(0, 10, Max - 10, Max, 0, 1)]
        // Translate pushing XMin under the edge
        [InlineData(Min, Min + 10, 0, 10, -1, 0)]
        // Translate pushing YMin under the edge
        [InlineData(0, 10, Min, Min + 10, 0, -1)]
        public void Combined_MutatedBounds_TranslateThrowsCorrectly(
            int xMin, int xMax, int yMin, int yMax, int dx, int dy)
        {
            var rect = new Rectangle();
            rect.XMin = xMin;
            rect.XMax = xMax;
            rect.YMin = yMin;
            rect.YMax = yMax;

            Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => rect.Translate(dx, dy));
        }

        [Theory]
        // Inflate pushing XMax and YMax over the edge
        [InlineData(Max - 5, Max - 1, Max - 5, Max - 1, 2, 2)]
        // Inflate pushing XMin and YMin under the edge
        [InlineData(Min + 1, Min + 5, Min + 1, Min + 5, 2, 2)]
        // Asymmetrical Inflate (XMax grows over Max, YMin shrinks under Min)
        [InlineData(Max - 5, Max - 1, Min + 1, Min + 5, 2, 2)]
        public void Combined_MutatedBounds_InflateThrowsCorrectly(
            int xMin, int xMax, int yMin, int yMax, int dx, int dy)
        {
            var rect = new Rectangle();
            rect.XMin = xMin;
            rect.XMax = xMax;
            rect.YMin = yMin;
            rect.YMax = yMax;

            Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => rect.Inflate(dx, dy));
        }

        [Fact]
        public void Inflate_ExtremeDeflation_CausesMathematicalImplosion()
        {
            // A 10x10 rectangle
            var rect = new Rectangle(10, 10, 10, 10);

            // Deflate by -20 on all sides. 
            // This shrinks the 10x10 box so aggressively that the bounds cross each other.
            // Mathematical expectation: No exception is thrown because coordinates stay within Int32.
            // XMin (10) -= (-20) => 30. XMax (20) += (-20) => 0.
            rect.Inflate(-20, -20);

            // Because XMin (30) > XMax (0), the Empty property MUST evaluate to true,
            // proving that an imploded rectangle is structurally harmless to the graphics pipeline.
            Assert.True(rect.Empty);
            Assert.Equal(30, rect.XMin);
            Assert.Equal(0, rect.XMax);
            Assert.Equal(30, rect.YMin);
            Assert.Equal(0, rect.YMax);
        }

        [Theory]
        // 1. X-Axis Overflow only (Span = 4,000,000,000 > Int32.MaxValue)
        [InlineData(-2000000000, 0, 2000000000, 10)]
        // 2. Y-Axis Overflow only (Span = 4,000,000,000 > Int32.MaxValue)
        [InlineData(0, -2000000000, 10, 2000000000)]
        // 3. Both Axes Overflow simultaneously
        [InlineData(-2000000000, -2000000000, 2000000000, 2000000000)]
        // 4. Exact Int32.MaxValue boundary + 1 on X-Axis (Span = 2,147,483,648)
        [InlineData(0, 0, int.MaxValue, 10, Skip = "Cannot initialize array directly with int.MaxValue + 1 if cast is unchecked, simulating memory instead")]
        public void Empty_GeometricDistanceOverflow_EvaluatesToTrue(int xMin, int yMin, int xMax, int yMax)
        {
            // We simulate a corrupted struct loaded from a binary stream (e.g. invalid DjVu chunk).
            // Layout: XMin, YMin, XMax, YMax
            int[] rawData = new int[] 
            { 
                xMin, 
                yMin, 
                xMax, 
                yMax 
            };

            Rectangle rect;
            
            // Cast the raw memory array into the struct to bypass constructor safety,
            // simulating a binary read from disk.
            unsafe
            {
                fixed (int* ptr = rawData)
                {
                    rect = *(Rectangle*)ptr;
                }
            }

            // The struct mathematically spans > Int32.MaxValue, violating Cartesian boundaries.
            // Width and Height gracefully clamp to 0. Empty MUST follow suit to maintain invariants.
            Assert.Equal(0, rect.Width);
            Assert.Equal(0, rect.Height);
            Assert.True(rect.Empty); 
        }

        [Theory]
        [InlineData(10, 20, 50, 60, 100, 100, 10, 20, 110, 120)] // Grow
        [InlineData(10, 20, 50, 60, 10, 10, 10, 20, 20, 30)] // Shrink
        [InlineData(10, 20, 50, 60, 0, 0, 10, 20, 10, 20)] // To Empty
        public void Resize_ValidInputs_ChangesDimensionsCorrectly(
            int x, int y, int w, int h, int newW, int newH,
            int expectedXMin, int expectedYMin, int expectedXMax, int expectedYMax)
        {
            var rect = new Rectangle(x, y, w, h);
            rect.Resize(newW, newH);

            Assert.Equal(expectedXMin, rect.XMin);
            Assert.Equal(expectedYMin, rect.YMin);
            Assert.Equal(expectedXMax, rect.XMax);
            Assert.Equal(expectedYMax, rect.YMax);
        }

        [Theory]
        [InlineData(Max - 100, 0, 10, 10, 101, 10, "width")] // Geometric overflow (XMax > Int32.MaxValue)
        [InlineData(0, Max - 100, 10, 10, 10, 101, "height")] // Geometric overflow (YMax > Int32.MaxValue)
        [InlineData(0, 0, 10, 10, -5, 10, "width")] // Negative width
        [InlineData(0, 0, 10, 10, 10, -5, "height")] // Negative height
        [InlineData(0, 0, 10, 10, -5, -5, "width, height")] // Both negative
        public void Resize_InvalidDimensions_IdentifiesCorrectParameter(
            int x, int y, int w, int h, int newW, int newH, string expectedParamName)
        {
            var rect = new Rectangle(x, y, w, h);
            var ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => rect.Resize(newW, newH));

            Assert.Equal(expectedParamName, ex.ParamName);
            Assert.True(ex.Message.Contains("invalid") || ex.Message.Contains("caused an overflow"),
                $"Exception message did not match expected patterns. Actual: {ex.Message}");
        }

        [Theory]
        [InlineData(Max - 100, 0, 10, 10, 101, 10)] // Geometric overflow (XMax > Int32.MaxValue)
        [InlineData(0, Max - 100, 10, 10, 10, 101)] // Geometric overflow (YMax > Int32.MaxValue)
        [InlineData(0, 0, 10, 10, -5, 10)] // Negative width
        [InlineData(0, 0, 10, 10, 10, -5)] // Negative height
        [InlineData(0, 0, 10, 10, -5, -5)] // Both negative
        public void Resize_InvalidDimensions_MaintainsStrongExceptionGuarantee(
            int x, int y, int w, int h, int newW, int newH)
        {
            var rect = new Rectangle(x, y, w, h);
            
            // Act - Expected to throw
            Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => rect.Resize(newW, newH));

            // Exception Safety Guarantee: The original struct state must be preserved
            Assert.False(rect.Empty);
            Assert.Equal(x, rect.XMin); 
            Assert.Equal(y, rect.YMin);
            Assert.Equal(x + w, rect.XMax); 
            Assert.Equal(y + h, rect.YMax); 
        }

        [Theory]
        [InlineData(10, 20, 50, 60, 0, 50, 10, 20, 10, 70)] // Resize to Zero Width (X collapses, Y grows)
        [InlineData(10, 20, 50, 60, 50, 0, 10, 20, 60, 20)] // Resize to Zero Height (X grows, Y collapses)
        public void Resize_ToZeroDimension_CreatesMathematicallyEmptyWithoutThrowing(
            int x, int y, int w, int h, int newW, int newH,
            int expectedXMin, int expectedYMin, int expectedXMax, int expectedYMax)
        {
            var rect = new Rectangle(x, y, w, h);
            rect.Resize(newW, newH);

            Assert.True(rect.Empty);
            Assert.Equal(expectedXMin, rect.XMin);
            Assert.Equal(expectedYMin, rect.YMin);
            Assert.Equal(expectedXMax, rect.XMax);
            Assert.Equal(expectedYMax, rect.YMax);
        }

        [Theory]
        // Exact MaxValue boundary
        [InlineData(0, 0, 10, 10, Max, Max, 0, 0, Max, Max)]
        // Deep negative origin spanning to 0
        [InlineData(-2000000000, -2000000000, 10, 10, 2000000000, 2000000000, -2000000000, -2000000000, 0, 0)]
        // MinValue origin spanning maximum possible absolute width
        [InlineData(Min, Min, 10, 10, Max, Max, Min, Min, Min + Max, Min + Max)]
        public void Resize_ExtremeValidBounds_ResizesSafelyWithoutThrowing(
            int x, int y, int w, int h, int newW, int newH,
            int expectedXMin, int expectedYMin, int expectedXMax, int expectedYMax)
        {
            var rect = new Rectangle(x, y, w, h);
            rect.Resize(newW, newH);

            Assert.False(rect.Empty);
            Assert.Equal(expectedXMin, rect.XMin);
            Assert.Equal(expectedYMin, rect.YMin);
            Assert.Equal(expectedXMax, rect.XMax);
            Assert.Equal(expectedYMax, rect.YMax);
        }

        [Fact]
        public void Translate_OverflowException_MaintainsAtomicity()
        {
            var rect = new Rectangle(0, 0, 10, 10);
            
            // Act: Translate by X = int.MaxValue. 
            // _XMin (0) + int.MaxValue = int.MaxValue (Succeeds, mutating state)
            // _XMax (10) + int.MaxValue = Overflow (Throws)
            var ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => rect.Translate(int.MaxValue, 0));

            // Assert: The struct MUST remain exactly as it was before the call.
            Assert.Equal(0, rect.XMin);
            Assert.Equal(0, rect.YMin);
            Assert.Equal(10, rect.XMax);
            Assert.Equal(10, rect.YMax);
        }

        [Fact]
        public void Inflate_OverflowException_MaintainsAtomicity()
        {
            var rect = new Rectangle(0, 0, 10, 10);
            
            // Act: Inflate by Y = int.MaxValue.
            // _XMin, _XMax succeed.
            // _YMin (0) - int.MaxValue = -int.MaxValue (Succeeds, mutating state)
            // _YMax (10) + int.MaxValue = Overflow (Throws)
            var ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => rect.Inflate(0, int.MaxValue));

            // Assert: The struct MUST remain exactly as it was before the call.
            Assert.Equal(0, rect.XMin);
            Assert.Equal(0, rect.YMin);
            Assert.Equal(10, rect.XMax);
            Assert.Equal(10, rect.YMax);
        }

        #region Scale Tests

        [Theory]
        [InlineData(10, 20, 50, 60, 2.0f, 20, 40, 120, 160)] // Scale uniformly by 2
        [InlineData(10, 20, 50, 60, 0.5f, 5, 10, 30, 40)]   // Scale uniformly by 0.5
        [InlineData(10, 20, 50, 60, 0.0f, 0, 0, 0, 0)]      // Scale to zero
        [InlineData(10, 20, 50, 60, 1.0f, 10, 20, 60, 80)]  // Identity scale
        [InlineData(-10, -20, 50, 60, 2.0f, -20, -40, 80, 80)] // Scale with negative coordinates
        [InlineData(5, 5, 10, 10, 1.5f, 7, 7, 22, 22)]      // Scale with truncation (7.5 -> 7, 22.5 -> 22)
        public void Scale_UniformValidInputs_ChangesCoordinatesCorrectly(
            int x, int y, int w, int h, float factor,
            int expectedXMin, int expectedYMin, int expectedXMax, int expectedYMax)
        {
            var rect = new Rectangle(x, y, w, h);
            rect.Scale(factor);

            Assert.Equal(expectedXMin, rect.XMin);
            Assert.Equal(expectedYMin, rect.YMin);
            Assert.Equal(expectedXMax, rect.XMax);
            Assert.Equal(expectedYMax, rect.YMax);
        }

        [Theory]
        [InlineData(10, 20, 50, 60, 2.0f, 0.5f, 20, 10, 120, 40)] // Scale non-uniformly
        [InlineData(10, 20, 50, 60, 1.0f, 3.0f, 10, 60, 60, 240)] // Keep X, scale Y
        [InlineData(10, 20, 50, 60, 0.0f, 1.0f, 0, 20, 0, 80)]    // Zero X scale
        public void Scale_NonUniformValidInputs_ChangesCoordinatesCorrectly(
            int x, int y, int w, int h, float xfactor, float yfactor,
            int expectedXMin, int expectedYMin, int expectedXMax, int expectedYMax)
        {
            var rect = new Rectangle(x, y, w, h);
            rect.Scale(xfactor, yfactor);

            Assert.Equal(expectedXMin, rect.XMin);
            Assert.Equal(expectedYMin, rect.YMin);
            Assert.Equal(expectedXMax, rect.XMax);
            Assert.Equal(expectedYMax, rect.YMax);
        }

        [Theory]
        [InlineData(-1.0f, 1.0f)]
        [InlineData(1.0f, -1.0f)]
        [InlineData(-2.0f, -2.0f)]
        public void Scale_NegativeFactors_ThrowsDjvuArgumentOutOfRangeException(float xfactor, float yfactor)
        {
            var rect = new Rectangle(10, 10, 50, 50);
            var ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => rect.Scale(xfactor, yfactor));
            Assert.Contains("Scale factors cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(float.NaN, 1.0f)]
        [InlineData(1.0f, float.NaN)]
        [InlineData(float.PositiveInfinity, 1.0f)]
        [InlineData(1.0f, float.PositiveInfinity)]
        public void Scale_NonFiniteFactors_ThrowsDjvuArgumentOutOfRangeException(float xfactor, float yfactor)
        {
            var rect = new Rectangle(10, 10, 50, 50);
            var ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => rect.Scale(xfactor, yfactor));
            Assert.Contains("Scale factors must be finite numbers", ex.Message);
        }

        [Theory]
        [InlineData(Max / 2, 0, 100, 100, 3.0f, 1.0f, "overflow/underflow of the internal Int32 coordinate boundaries")]
        [InlineData(0, Max / 2, 100, 100, 1.0f, 3.0f, "overflow/underflow of the internal Int32 coordinate boundaries")]
        [InlineData(Min / 2, 0, 100, 100, 3.0f, 1.0f, "overflow/underflow of the internal Int32 coordinate boundaries")]
        [InlineData(-1000000000, 0, 2000000000, 10, 2.0f, 1.0f, "dimensions exceeding Int32.MaxValue")]
        [InlineData(0, -1000000000, 10, 2000000000, 1.0f, 2.0f, "dimensions exceeding Int32.MaxValue")]
        public void Scale_Overflow_ThrowsDjvuArgumentOutOfRangeException(
            int x, int y, int w, int h, float xfactor, float yfactor, string expectedMessageFragment)
        {
            var rect = new Rectangle();
            
            // Bypass constructor safety constraints using properties directly 
            // since we need an initially valid rectangle to test mutation overflow
            rect.XMin = x;
            rect.YMin = y;
            rect.XMax = x + w;
            rect.YMax = y + h;

            var ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => rect.Scale(xfactor, yfactor));
            Assert.Contains(expectedMessageFragment, ex.Message);
        }

        [Fact]
        public void Scale_OverflowException_MaintainsAtomicity()
        {
            var rect = new Rectangle(10, 20, 50, 60);
            
            Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => rect.Scale(float.MaxValue));

            // Assert: The struct MUST remain exactly as it was before the call.
            Assert.Equal(10, rect.XMin);
            Assert.Equal(20, rect.YMin);
            Assert.Equal(60, rect.XMax);
            Assert.Equal(80, rect.YMax);
        }

        #endregion Scale Tests

        [Theory]
        [InlineData(10, 20, 50, 60, 100, 200, 100, 200, 150, 260)] // Valid positive move
        [InlineData(10, 20, 50, 60, -100, -200, -100, -200, -50, -140)] // Valid negative move
        [InlineData(0, 0, 10, 10, Max - 10, Max - 10, Max - 10, Max - 10, Max, Max)] // Valid edge push
        public void MoveTo_ValidInputs_ShiftsCoordinatesCorrectly(
            int x, int y, int w, int h, int newXMin, int newYMin,
            int expectedXMin, int expectedYMin, int expectedXMax, int expectedYMax)
        {
            var rect = new Rectangle(x, y, w, h);
            rect.MoveTo(newXMin, newYMin);

            Assert.Equal(expectedXMin, rect.XMin);
            Assert.Equal(expectedYMin, rect.YMin);
            Assert.Equal(expectedXMax, rect.XMax);
            Assert.Equal(expectedYMax, rect.YMax);
        }

        [Theory]
        // The delta calculation itself overflows an Int32 (e.g., from Max to Min)
        [InlineData(Max, Max, 0, 0, Min, Min, "xMin, yMin", "translation delta that exceeds Int32.MaxValue or Int32.MinValue")]
        [InlineData(Min, Min, 0, 0, Max, Min, "xMin", "translation delta that exceeds Int32.MaxValue or Int32.MinValue")]
        // Delta overflows Int32 boundaries on Y-Axis ONLY (Y moves from Min to Max)
        [InlineData(0, Min, 0, 0, 0, Max, "yMin", "translation delta that exceeds Int32.MaxValue or Int32.MinValue")]
        // Asymmetrical Delta Overflow (X underflows < Min, Y overflows > Max)
        [InlineData(Max, Min, 0, 0, Min, Max, "xMin, yMin", "translation delta that exceeds Int32.MaxValue or Int32.MinValue")]
        // The delta is a valid Int32, but the resulting Translate() call pushes XMax/YMax over the physical bounds
        [InlineData(Max - 15, Max - 15, 10, 10, Max - 2, Max - 2, "", "caused an overflow/underflow")]
        public void MoveTo_Overflow_ThrowsDjvuArgumentOutOfRangeException(
            int x, int y, int w, int h, int newXMin, int newYMin, string expectedParam, string expectedMessageFragment)
        {
            var rect = new Rectangle(x, y, w, h);
            var ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() => rect.MoveTo(newXMin, newYMin));

            Assert.Contains(expectedMessageFragment, ex.Message);
            if (!string.IsNullOrEmpty(expectedParam))
            {
                Assert.Equal(expectedParam, ex.ParamName);
            }
        }
    }
}
