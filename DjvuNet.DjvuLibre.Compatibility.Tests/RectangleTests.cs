using System;
using Xunit;
using DjvuNet.Graphics;
using DjvuNet.DjvuLibre;

namespace DjvuNet.DjvuLibre.Compatibility.Tests
{
    public class RectangleTests
    {
        [Theory]
        // --- Nominal Cases ---
        [InlineData(10, 10, 50, 50, 20, 20, 10, 10)] // Valid rectangle containing another valid rectangle
        [InlineData(10, 10, 50, 50, 0, 0, 0, 0)]     // Valid rectangle containing empty rectangle at origin (Validates the C++ Quirk)
        [InlineData(0, 0, 0, 0, 100, 100, 0, 0)]     // Empty rectangle at origin containing empty rectangle at offset (Validates the C++ Quirk)
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0)]         // Exact same empty rectangle
        [InlineData(10, 10, 50, 50, 10, 10, 50, 50)] // Valid rectangle containing identical valid rectangle

        // --- Boundary Condition Cases ---
        [InlineData(-1000000, -1000000, 2000000, 2000000, -5, -5, 10, 10)] // Deep negative origin, massive dimension containing a small test rect
        [InlineData(0, 0, int.MaxValue, int.MaxValue, 1000, 1000, 500, 500)] // Max valid coordinate spanning to Int32.MaxValue (Max=2147483647)
        [InlineData(-100, -100, 200, 200, -50, -50, 100, 100)] // Massive origin-spanning container holding smaller origin-spanning target (IN)
        [InlineData(-10, -10, 20, 20, -10, -10, 21, 20)]       // Target exceeds XMax by 1 across the origin (OUT)
        [InlineData(int.MaxValue - 100, int.MaxValue - 100, 100, 100, int.MaxValue - 50, int.MaxValue - 50, 40, 40)] // Extreme Positive (IN)
        [InlineData(int.MaxValue - 100, int.MaxValue - 100, 100, 100, int.MaxValue - 10, int.MaxValue - 10, 10, 10)] // Extreme Positive touching boundary exactly (IN)
        [InlineData(int.MinValue, int.MinValue, 100, 100, int.MinValue + 10, int.MinValue + 10, 10, 10)] // Extreme Negative (IN)

        // --- Exact Int32 Boundary Touching ---
        [InlineData(int.MinValue, int.MinValue, 1, 1, int.MinValue, int.MinValue, 1, 1)] // 1x1 exactly at MinValue
        [InlineData(int.MaxValue - 1, int.MaxValue - 1, 1, 1, int.MaxValue - 1, int.MaxValue - 1, 1, 1)] // 1x1 exactly ending at MaxValue
        [InlineData(int.MinValue, int.MinValue, 0, 0, int.MinValue, int.MinValue, 0, 0)] // Empty exactly at MinValue
        [InlineData(int.MaxValue, int.MaxValue, 0, 0, int.MaxValue, int.MaxValue, 0, 0)] // Empty exactly at MaxValue

        // --- 1D and Single-Pixel Containment ---
        [InlineData(10, 10, 1, 50, 10, 20, 1, 10)] // Vertical line contains smaller vertical line (IN)
        [InlineData(10, 10, 1, 50, 10, 30, 1, 1)]  // Vertical line contains single pixel (IN)
        [InlineData(10, 10, 1, 1, 10, 10, 1, 1)]   // Single pixel contains itself (IN)
        [InlineData(10, 10, 1, 1, 0, 0, 0, 0)]     // Single pixel contains empty rect at origin (IN - C++ Quirk)
        [InlineData(10, 10, 1, 1, 10, 10, 2, 1)]   // Single pixel cannot contain horizontal line (OUT)
        public void ContainsTests(
            int xMin, int yMin, int width, int height,
            int testXMin, int testYMin, int testWidth, int testHeight)
        {
            var managedRect = new Rectangle(xMin, yMin, width, height);
            var managedTestRect = new Rectangle(testXMin, testYMin, testWidth, testHeight);

            var nativeRect = new Rectangle(xMin, yMin, width, height);
            var nativeTestRect = new Rectangle(testXMin, testYMin, testWidth, testHeight);

            bool managedContainsResult = managedRect.Contains(managedTestRect);
            bool nativeContainsResult = NativeMethods.GRectContainsRect(ref nativeRect, ref nativeTestRect);

            Assert.Equal(nativeContainsResult, managedContainsResult);
        }

        [Theory]
        // --- Nominal Cases ---
        [InlineData(0, 0, 0, 0, 100, 100, 0, 0)]     // Different empty origins
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0)]         // Same empty
        [InlineData(10, 10, 50, 50, 10, 10, 50, 50)] // Same valid
        [InlineData(10, 10, 50, 50, 20, 20, 10, 10)] // Different valid

        // --- Boundary Condition Cases ---
        [InlineData(-2000000000, -2000000000, 1000, 1000, -2000000000, -2000000000, 1000, 1000)] // Deep negative origin equality
        [InlineData(10, 10, int.MaxValue - 10, int.MaxValue - 10, 10, 10, int.MaxValue - 10, int.MaxValue - 10)] // Max Int32 equality
        public void EqualsTests(
            int xMin, int yMin, int width, int height,
            int otherXMin, int otherYMin, int otherWidth, int otherHeight)
        {
            var managedRect = new Rectangle(xMin, yMin, width, height);
            var managedOther = new Rectangle(otherXMin, otherYMin, otherWidth, otherHeight);

            var nativeRect = new Rectangle(xMin, yMin, width, height);
            var nativeOther = new Rectangle(otherXMin, otherYMin, otherWidth, otherHeight);

            bool managedEqualsResult = managedRect.Equals(managedOther);
            bool nativeEqualsResult = NativeMethods.GRectEquals(ref nativeRect, ref nativeOther);

            Assert.Equal(nativeEqualsResult, managedEqualsResult);
        }

        [Theory]
        // --- Nominal Cases ---
        [InlineData(10, 20, 50, 60, 2.0f)]
        [InlineData(10, 20, 50, 60, 0.5f)]
        [InlineData(-10, -20, 50, 60, 2.0f)]

        // --- Boundary Condition Cases ---
        [InlineData(0, 0, 1000000000, 1000000000, 2.0f)] // Scale near MaxValue (1 billion * 2 = 2 billion, safely within 2.14B Int32 limit)
        [InlineData(-1000000000, -1000000000, 100, 100, 2.0f)] // Scale deep negative coordinates

        // --- Single Pixel / 1D Scaling ---
        [InlineData(10, 10, 1, 1, 2.0f)]   // 1x1 scales to 2x2
        [InlineData(10, 10, 1, 1, 0.5f)]   // 1x1 mathematically truncates to 0x0 (Collapse to Empty)
        [InlineData(10, 10, 1, 50, 2.0f)]  // Vertical line thickens to 2x100
        [InlineData(10, 10, 1, 50, 0.5f)]  // Vertical line collapses to 0x25 (Empty)
        [InlineData(10, 10, 50, 1, 0.5f)]  // Horizontal line collapses to 25x0 (Empty)
        public void Scale_UniformFactorTests(
            int xMin, int yMin, int width, int height, float uniformScaleFactor)
        {
            var managedRect = new Rectangle(xMin, yMin, width, height);
            var nativeRect = new Rectangle(xMin, yMin, width, height);

            managedRect.Scale(uniformScaleFactor);
            NativeMethods.GRectScale(ref nativeRect, uniformScaleFactor);

            Assert.Equal(nativeRect, managedRect);
        }

        [Theory]
        // --- Nominal Cases ---
        [InlineData(10, 20, 50, 60, 2.0f, 0.5f)]
        [InlineData(10, 20, 50, 60, 1.0f, 3.0f)]
        [InlineData(10, 20, 50, 60, 0.0f, 1.0f)]

        // --- Boundary Condition Cases ---
        [InlineData(0, 0, 1000000000, 1000000000, 2.0f, 1.0f)] // Massive X scale, Y unchanged
        [InlineData(0, 0, 1000000000, 1000000000, 1.0f, 2.0f)] // Massive Y scale, X unchanged
        [InlineData(-1000000000, -1000000000, 100, 100, 2.0f, 2.0f)] // Deep negative XY scale
        public void Scale_NonUniformFactorsTests(
            int xMin, int yMin, int width, int height, float scaleFactorX, float scaleFactorY)
        {
            var managedRect = new Rectangle(xMin, yMin, width, height);
            var nativeRect = new Rectangle(xMin, yMin, width, height);

            managedRect.Scale(scaleFactorX, scaleFactorY);
            NativeMethods.GRectScale(ref nativeRect, scaleFactorX, scaleFactorY);

            Assert.Equal(nativeRect, managedRect);
        }

        [Theory]
        // --- Nominal Cases ---
        [InlineData(0, 0, 0, 0)]       // Empty at origin
        [InlineData(10, 10, 0, 50)]    // Zero width
        [InlineData(10, 10, 50, 0)]    // Zero height
        [InlineData(10, 10, 1, 1)]     // Valid 1x1
        [InlineData(-10, -10, 50, 50)] // Valid crossing origin

        // --- Boundary Condition Cases ---
        [InlineData(int.MinValue, int.MinValue, 0, 0)] // Empty at extreme negative origin
        [InlineData(int.MaxValue, int.MaxValue, 0, 0)] // Empty at extreme positive origin
        public void IsEmptyTests(int xMin, int yMin, int width, int height)
        {
            var rect = new Rectangle(xMin, yMin, width, height);

            bool managedEmpty = rect.Empty;
            bool nativeEmpty = NativeMethods.IsEmptyGRect(ref rect);

            Assert.Equal(nativeEmpty, managedEmpty);
        }

        [Theory]
        // --- Nominal Cases ---
        [InlineData(10, 10, 50, 50, 10, 10)] // On XMin, YMin (Inclusive -> IN)
        [InlineData(10, 10, 50, 50, 59, 59)] // Just inside XMax and YMax (IN)
        [InlineData(10, 10, 50, 50, 60, 60)] // On XMax, YMax (Exclusive -> OUT)
        [InlineData(10, 10, 50, 50, 9, 10)]  // Just outside XMin (OUT)

        // --- Origin-Spanning Boundary Probes (-10 to +10) ---
        [InlineData(-10, -10, 20, 20, -10, -10)] // Bottom-Left Corner (IN)
        [InlineData(-10, -10, 20, 20, 9, 9)]     // Top-Right Inner Corner (IN)
        [InlineData(-10, -10, 20, 20, 10, 10)]   // Top-Right Exclusive Edge (OUT)
        [InlineData(-10, -10, 20, 20, -11, -10)] // Just outside Left Edge (OUT)

        // --- Extreme Negative Boundary Probes (Near int.MinValue) ---
        [InlineData(int.MinValue, int.MinValue, 10, 10, int.MinValue, int.MinValue)] // Bottom-Left Corner (IN)
        [InlineData(int.MinValue, int.MinValue, 10, 10, int.MinValue + 9, int.MinValue + 9)] // Top-Right Inner Corner (IN)
        [InlineData(int.MinValue, int.MinValue, 10, 10, int.MinValue + 10, int.MinValue + 10)] // Top-Right Exclusive Edge (OUT)

        // --- Extreme Positive Boundary Probes (Near int.MaxValue) ---
        [InlineData(int.MaxValue - 10, int.MaxValue - 10, 10, 10, int.MaxValue - 10, int.MaxValue - 10)] // Bottom-Left Corner (IN)
        [InlineData(int.MaxValue - 10, int.MaxValue - 10, 10, 10, int.MaxValue - 1, int.MaxValue - 1)] // Top-Right Inner Corner (IN)
        [InlineData(int.MaxValue - 10, int.MaxValue - 10, 10, 10, int.MaxValue, int.MaxValue)] // Top-Right Exclusive Edge (OUT)
        [InlineData(int.MaxValue - 10, int.MaxValue - 10, 10, 10, int.MaxValue - 11, int.MaxValue - 10)] // Just outside Left Edge (OUT)

        // --- Exact Int32 Boundary Touching ---
        [InlineData(int.MinValue, int.MinValue, 1, 1, int.MinValue, int.MinValue)] // Point IN
        [InlineData(int.MinValue, int.MinValue, 1, 1, int.MinValue + 1, int.MinValue)] // Point OUT
        [InlineData(int.MaxValue - 1, int.MaxValue - 1, 1, 1, int.MaxValue - 1, int.MaxValue - 1)] // Point IN
        [InlineData(int.MaxValue - 1, int.MaxValue - 1, 1, 1, int.MaxValue, int.MaxValue)] // Point OUT (XMax)

        // --- 1D and Single-Pixel Probes ---
        [InlineData(10, 10, 1, 1, 10, 10)] // The ONLY valid point (IN)
        [InlineData(10, 10, 1, 1, 11, 10)] // XMax boundary (OUT)
        [InlineData(10, 10, 1, 1, 10, 11)] // YMax boundary (OUT)
        [InlineData(10, 10, 1, 50, 10, 30)] // Point strictly on the vertical line (IN)
        [InlineData(10, 10, 1, 50, 11, 30)] // Deviation by 1 pixel horizontally (OUT)
        [InlineData(10, 10, 50, 1, 30, 10)] // Point strictly on the horizontal line (IN)
        [InlineData(10, 10, 50, 1, 30, 11)] // Deviation by 1 pixel vertically (OUT)
        public void ContainsPointTests(
            int xMin, int yMin, int width, int height, int testX, int testY)
        {
            var rect = new Rectangle(xMin, yMin, width, height);

            bool managedContains = rect.Contains(testX, testY);
            bool nativeContains = NativeMethods.GRectContainsPoint(ref rect, testX, testY);

            Assert.Equal(nativeContains, managedContains);
        }

        [Theory]
        // --- Nominal Cases ---
        [InlineData(0, 0, 10, 10, 20, 20, 10, 10)] // Disjoint
        [InlineData(0, 0, 10, 10, 5, 5, 10, 10)]   // Overlapping
        [InlineData(0, 0, 10, 10, 2, 2, 5, 5)]     // Fully contained
        [InlineData(0, 0, 10, 10, 10, 10, 10, 10)] // Touching at corner

        // --- Boundary Condition Cases ---
        [InlineData(-1000000000, -1000000000, 2000000000, 2000000000, -500000000, -500000000, 1000000000, 1000000000)] // Intersect massive bounds
        [InlineData(-1000000000, -1000000000, 10, 10, 1000000000, 1000000000, 10, 10)] // Intersect two distant coordinates (empty result)

        // --- Orthogonal Line Intersections ---
        [InlineData(10, 0, 1, 50, 0, 25, 50, 1)]  // Vertical line intersects Horizontal line at exactly (10, 25)
        [InlineData(10, 10, 1, 50, 10, 60, 1, 50)] // Two parallel 1D lines that touch end-to-end (Intersection is empty)

        // --- Exact Int32 Boundary Touching ---
        [InlineData(int.MinValue, int.MinValue, 10, 10, int.MinValue, int.MinValue, 5, 5)] // Intersecting at MinValue
        [InlineData(int.MaxValue - 10, int.MaxValue - 10, 10, 10, int.MaxValue - 5, int.MaxValue - 5, 5, 5)] // Intersecting at MaxValue

        // --- Origin-Spanning Boundaries ---
        [InlineData(-50, -50, 100, 100, -10, -10, 20, 20)] // Origin-spanning overlap
        public void IntersectTests(
            int leftXMin, int leftYMin, int leftWidth, int leftHeight,
            int rightXMin, int rightYMin, int rightWidth, int rightHeight)
        {
            var managedLeftRect = new Rectangle(leftXMin, leftYMin, leftWidth, leftHeight);
            var managedRightRect = new Rectangle(rightXMin, rightYMin, rightWidth, rightHeight);

            var nativeLeftRect = new Rectangle(leftXMin, leftYMin, leftWidth, leftHeight);
            var nativeRightRect = new Rectangle(rightXMin, rightYMin, rightWidth, rightHeight);

            var managedTarget = new Rectangle();
            managedTarget.Intersect(managedLeftRect, managedRightRect);

            NativeMethods.GRectIntersect(out Rectangle nativeTarget, ref nativeLeftRect, ref nativeRightRect);

            Assert.Equal(nativeTarget, managedTarget);
        }

        [Fact]
        public void Intersect_DisjointRectangles_StrictCoordinateParity()
        {
            var managedLeftRect = new Rectangle(0, 0, 10, 10);
            var managedRightRect = new Rectangle(20, 20, 10, 10);

            var nativeLeftRect = new Rectangle(0, 0, 10, 10);
            var nativeRightRect = new Rectangle(20, 20, 10, 10);

            var managedTarget = new Rectangle();
            managedTarget.Intersect(managedLeftRect, managedRightRect);

            NativeMethods.GRectIntersect(out Rectangle nativeTarget, ref nativeLeftRect, ref nativeRightRect);

            // Assert that the native code cleared the coordinates to 0
            Assert.True(nativeTarget.XMin == 0 && nativeTarget.YMin == 0 && nativeTarget.XMax == 0 && nativeTarget.YMax == 0,
                $"Native C++ GRect::intersect failed to clear coordinates. State: {nativeTarget}");

            // Assert strict parity between managed and native states
            Assert.True(nativeTarget.XMin == managedTarget.XMin && 
                        nativeTarget.YMin == managedTarget.YMin && 
                        nativeTarget.XMax == managedTarget.XMax && 
                        nativeTarget.YMax == managedTarget.YMax,
                $"Native parity divergence detected!\nNative GRect: {nativeTarget}\nManaged Rectangle: {managedTarget}");
        }

        [Theory]
        // --- Nominal Cases ---
        [InlineData(0, 0, 10, 10, 20, 20, 10, 10)] // Disjoint
        [InlineData(0, 0, 10, 10, 5, 5, 10, 10)]   // Overlapping
        [InlineData(0, 0, 20, 20, 5, 5, 10, 10)]   // Fully contained
        [InlineData(100, 100, 0, 0, 0, 0, 20, 20)] // Rect1 mathematically empty

        // --- Boundary Condition Cases ---
        [InlineData(-1000000000, -1000000000, 10, 10, 1000000000, 1000000000, 10, 10)] // Hull of two extremely distant coordinates
        [InlineData(0, 0, int.MaxValue / 2, int.MaxValue / 2, int.MaxValue / 2, int.MaxValue / 2, int.MaxValue / 2, int.MaxValue / 2)] // Hull spanning up to Int32.MaxValue

        // --- 1D and Single-Pixel Hulls ---
        [InlineData(10, 10, 1, 1, 20, 20, 1, 1)] // Hull of pixel at (10,10) and pixel at (20,20) = Rectangle from (10,10) to (21,21)

        // --- Exact Int32 Boundary Touching ---
        [InlineData(int.MinValue, int.MinValue, 10, 10, int.MinValue + 10, int.MinValue + 10, 10, 10)] // Hull near MinValue
        [InlineData(int.MaxValue - 20, int.MaxValue - 20, 10, 10, int.MaxValue - 10, int.MaxValue - 10, 10, 10)] // Hull near MaxValue
        [InlineData(0, 0, 0, 0, int.MaxValue, int.MaxValue, 0, 0)] // Hull from 0,0 to MaxValue,MaxValue

        // --- Origin-Spanning Boundaries ---
        [InlineData(-10, -10, 5, 5, 5, 5, 5, 5)] // Hull spanning across the origin
        public void RecthullTests(
            int leftXMin, int leftYMin, int leftWidth, int leftHeight,
            int rightXMin, int rightYMin, int rightWidth, int rightHeight)
        {
            var managedLeftRect = new Rectangle(leftXMin, leftYMin, leftWidth, leftHeight);
            var managedRightRect = new Rectangle(rightXMin, rightYMin, rightWidth, rightHeight);

            var nativeLeftRect = new Rectangle(leftXMin, leftYMin, leftWidth, leftHeight);
            var nativeRightRect = new Rectangle(rightXMin, rightYMin, rightWidth, rightHeight);

            var managedTarget = new Rectangle();
            managedTarget.Recthull(managedLeftRect, managedRightRect);

            NativeMethods.GRectRecthull(out Rectangle nativeTarget, ref nativeLeftRect, ref nativeRightRect);

            Assert.Equal(nativeTarget, managedTarget);
        }

        [Theory]
        // --- Nominal Cases ---
        [InlineData(10, 20, 50, 60, 5, 5)]   // Inflate (grow)
        [InlineData(10, 20, 50, 60, -5, -5)] // Deflate (shrink)
        [InlineData(10, 10, 10, 10, -20, -20)] // Implode (shrink past zero to empty)

        // --- Boundary Condition Cases ---
        [InlineData(0, 0, 10, 10, 1000000000, 1000000000)] // Massive inflation just shy of overflow
        [InlineData(-1000000000, -1000000000, 2000000000, 2000000000, -500000000, -500000000)] // Massive deflation of a massive rectangle

        // --- Exact Int32 Boundary Touching ---
        [InlineData(int.MaxValue - 10, int.MaxValue - 10, 10, 10, -5, -5)] // Deflate near MaxValue
        [InlineData(int.MinValue + 10, int.MinValue + 10, 10, 10, -5, -5)] // Deflate near MinValue
        [InlineData(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue, -int.MaxValue/2, -int.MaxValue/2)] // Massive deflate
        [InlineData(0, 0, 0, 0, int.MaxValue / 2, int.MaxValue / 2)] // Massive inflate from zero
        public void InflateTests(
            int xMin, int yMin, int width, int height, int dx, int dy)
        {
            var managedRect = new Rectangle(xMin, yMin, width, height);
            var nativeRect = new Rectangle(xMin, yMin, width, height);

            managedRect.Inflate(dx, dy);
            NativeMethods.GRectInflate(ref nativeRect, dx, dy);

            Assert.Equal(nativeRect, managedRect);
        }

        [Theory]
        // --- Nominal Cases ---
        [InlineData(10, 20, 50, 60, 5, 5)]     // Translate positive
        [InlineData(10, 20, 50, 60, -10, -20)] // Translate negative (to origin)

        // --- Boundary Condition Cases ---
        [InlineData(-1000000000, -1000000000, 10, 10, 2000000000, 2000000000)] // Massive translation pushing from deep negative to deep positive
        [InlineData(1000000000, 1000000000, 10, 10, -2000000000, -2000000000)] // Massive translation pushing from deep positive to deep negative

        // --- Exact Int32 Boundary Touching ---
        [InlineData(0, 0, 10, 10, int.MaxValue - 10, int.MaxValue - 10)] // Translate to exactly MaxValue
        [InlineData(0, 0, 10, 10, int.MinValue, int.MinValue)] // Translate to exactly MinValue
        [InlineData(int.MinValue, int.MinValue, 10, 10, int.MaxValue, int.MaxValue)] // Translate from MinValue by MaxValue -> ends at -1
        public void TranslateTests(
            int xMin, int yMin, int width, int height, int dx, int dy)
        {
            var managedRect = new Rectangle(xMin, yMin, width, height);
            var nativeRect = new Rectangle(xMin, yMin, width, height);

            managedRect.Translate(dx, dy);
            NativeMethods.GRectTranslate(ref nativeRect, dx, dy);

            Assert.Equal(nativeRect, managedRect);
        }
    }
}