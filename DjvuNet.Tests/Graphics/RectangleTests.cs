using System;
using Xunit;
using DjvuNet.Graphics;

namespace DjvuNet.Graphics.Tests
{

    public class RectangleTests
    {

        [Fact]
        public void Rectangle_Clear001()
        {
            var target1 = new Rectangle(0, 0, 0, 0);
            Assert.True(target1.Empty);
            target1.Clear();
            Assert.True(target1.Empty);

            var target2 = new Rectangle(1, 100, 0, 0);
            Assert.True(target2.Empty);
            target2.Clear();
            Assert.True(target2.Empty);
            Assert.Equal<int>(0, target2.Right);
            Assert.Equal<int>(0, target2.Bottom);

            var target3 = new Rectangle(1, 100, 1, 10);
            Assert.False(target3.Empty);
            target3.Clear();
            Assert.True(target3.Empty);
            Assert.Equal<int>(0, target3.Right);
            Assert.Equal<int>(0, target3.Bottom);
            Assert.Equal<int>(0, target3.Width);
            Assert.Equal<int>(0, target3.Height);
        }

        [Fact]
        public void Rectangle_ContainsRect001()
        {
            var target1 = new Rectangle(0, 0, 0, 0);
            Assert.True(target1.Empty);

            var target2 = new Rectangle(1, 100, 0, 0);
            Assert.True(target2.Empty);

            Assert.False(target1.Contains(target2), "Empty rectangle does not contain empty rectangle - case 1");
            Assert.False(target2.Contains(target1), "Empty rectangle does not contain empty rectangle - case 2");

            var target3 = new Rectangle(1, 100, 1, 10);
            Assert.False(target3.Empty);

            Assert.False(target3.Contains(target1), "Non empty rectangle contains empty rectangle - case 1");
            Assert.False(target3.Contains(target2), "Non empty rectangle contains empty rectangle - case 2");
            Assert.True(target3.Contains(target3), "Rectangle does contain itself");

            var target4 = new Rectangle(-100, -200, 1000, 2000);
            Assert.False(target4.Empty);

            Assert.True(target4.Contains(target1));
            Assert.True(target4.Contains(target2));
            Assert.True(target4.Contains(target3));
        }

        [Fact]
        public void Rectangle_ContainsXY001()
        {
            var target1 = new Rectangle(0, 0, 0, 0);
            Assert.True(target1.Empty);

            Assert.True(target1.Contains(0, 0), "0,0,0,0 contains 0,0");
            Assert.False(target1.Contains(-100, 0), "0,0,0,0 not contains -100,0");
            Assert.False(target1.Contains(0, 1), "0, 0, 0, 0 not contains 0,1");

            var target2 = new Rectangle(1, 100, 0, 0);
            Assert.True(target2.Empty);

            Assert.True(target2.Contains(1, 100), "1,100,0,0 contains 1,100");
            Assert.False(target2.Contains(0, 100), "1,100,0,0 not contains 0,100");
            Assert.False(target2.Contains(1, -100), "1,100,0,0 not contains 1,-100");

            var target3 = new Rectangle(1, 100, 1, 10);
            Assert.False(target3.Empty);

            Assert.True(target3.Contains(1, 100), "1,100,1,10 contains 1,100");
            Assert.True(target3.Contains(2, 110), "1,100,1,10 contains 2,100");
            Assert.False(target3.Contains(1, 11), "1,100,1,10 not contains 1,11");
            Assert.True(target3.Contains(2, 109), "1,100,1,10 contains 2,109");
            Assert.False(target3.Contains(0, 105), "1,100,1,10 not contains 0,105");
            Assert.False(target3.Contains(2, 95), "1,100,1,10 not contains 2,95");

            var target4 = new Rectangle(-100, -200, 1000, 2000);
            Assert.False(target4.Empty);

            Assert.True(target4.Contains(-100, -200), "-100,-200,1000,2000 contains -100,-200");
            Assert.True(target4.Contains(900, 1800), "-100,-200,1000,2000 contains 900,1800");
            Assert.True(target4.Contains(0, 0), "-100,-200,1000,2000 contains 0,0");
            Assert.False(target4.Contains(1000, 2000), "-100,-200,1000,2000 not contains 1000,2000");
            Assert.False(target4.Contains(-1000, -2000), "-100,-200,1000,2000 not contains -1000,-2000");
        }

        [Fact]
        public void Rectangle_Duplicate001()
        {
            var target1 = new Rectangle(0, 0, 0, 0);
            Assert.True(target1.Empty);
            var result1 = target1.Duplicate();
            Assert.True(result1.Empty);
            Assert.True(result1.Equals(target1));

            var target2 = new Rectangle(1, 100, 0, 0);
            Assert.True(target2.Empty);
            var result2 = target2.Duplicate();
            Assert.True(result2.Empty);
            Assert.Equal<int>(target2.Right, result2.Right);
            Assert.Equal<int>(target2.Bottom, result2.Bottom);

            var target3 = new Rectangle(1, 100, 1, 10);
            Assert.False(target3.Empty);
            var result3 = target3.Duplicate();
            Assert.True(result3.Equals(target3));
            Assert.Equal<int>(target3.Right, result3.Right);
            Assert.Equal<int>(target3.Bottom, result3.Bottom);
            Assert.Equal<int>(target3.Width, result3.Width);
            Assert.Equal<int>(target3.Height, result3.Height);
        }

        [Fact]
        public void Rectangle_Empty001()
        {
            var target1 = new Rectangle(0, 0, 0, 0);
            Assert.True(target1.Empty);

            var target2 = new Rectangle(1, 100, 0, 0);
            Assert.True(target2.Empty);

            var target3 = new Rectangle(1, 100, 1, 10);
            Assert.False(target3.Empty);
        }

        [Fact]
        public void Rectangle_Equals001()
        {
            Rectangle rect1 = null;
            Rectangle rect2 = null;

            var target1 = new Rectangle(0, 0, 0, 0);

            Assert.False(target1.Equals(rect1));
            Assert.False(target1.Equals(rect2));

            rect2 = new Rectangle();

            Assert.True(target1.Equals(rect2));
            Assert.False(rect2.Equals(rect1));
        }

        [Fact]
        public void Rectangle_Equals002()
        {
            Rectangle rect1 = null;
            Rectangle rect2 = null;

            var target1 = new Rectangle(0, 0, 1000, 1000);

            Assert.False(target1.Equals(rect1));
            Assert.False(target1.Equals(rect2));

            rect1 = new Rectangle(0, 0, 1000, 1000);
            rect2 = new Rectangle(10, 10, 1000, 1000);

            Assert.False(target1.Equals(rect2), "Rectangle after translation is not equal to original rectangle.");
            Assert.False(rect2.Equals(rect1));
            Assert.True(target1.Equals(rect1));
        }

        [Fact]
        public void Rectangle_Equals003()
        {
            Rectangle rect1 = null;
            Rectangle rect2 = null;

            var target1 = new Rectangle(0, 0, 1000, 1000);

            Assert.False(target1.Equals(rect1));
            Assert.False(target1.Equals(rect2));

            rect1 = new Rectangle(0, 0, 1001, 1001);
            rect2 = new Rectangle(10, 10, 1000, 1000);

            Assert.False(target1.Equals(rect2), "Rectangle after translation is not equal to original rectangle.");
            Assert.False(rect2.Equals(rect1));
            Assert.False(target1.Equals(rect1), "Bigger rectangle is not equal to original one.");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void RectangleTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void RectangleTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void DuplicateTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ClearTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ContainsTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ContainsTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void EqualsTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetHashCodeTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void InflateTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void IntersectTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void RecthullTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void TranslateTest()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}
