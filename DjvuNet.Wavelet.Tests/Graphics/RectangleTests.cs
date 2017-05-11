using System;
using System.Collections.Generic;
using Xunit;
using DjvuNet.Graphics;

namespace DjvuNet.Graphics.Tests
{

    public class RectangleTests
    {

        [Fact]
        public void Clear001()
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
        public void ContainsTest001()
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

        [Fact()]
        public void ContainsTest002()
        {
            int width = 7;
            int height = 5;

            Rectangle rect1 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Rectangle rect2 = new Rectangle
            {
                Left = 0,
                Top = 0
            };

            Assert.False(rect2.Contains(rect1));
        }

        [Fact]
        public void ContainsXY001()
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
        public void Duplicate001()
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
        public void Empty001()
        {
            var target1 = new Rectangle(0, 0, 0, 0);
            Assert.True(target1.Empty);

            var target2 = new Rectangle(1, 100, 0, 0);
            Assert.True(target2.Empty);

            var target3 = new Rectangle(1, 100, 1, 10);
            Assert.False(target3.Empty);
        }

        [Fact]
        public void Equals001()
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
        public void Equals002()
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
        public void Equals003()
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

        [Fact()]
        public void AreaTest001()
        {
            Rectangle rect = new Rectangle
            {
                Left = 0,
                Bottom = 0,
                Right = 100,
                Top = 1000,
            };

            Assert.Equal(100 * 1000, rect.Area);
        }

        [Fact()]
        public void AreaTest002()
        {
            Rectangle rect = new Rectangle
            {
                Left = -11,
                Bottom = -200,
                Right = 100,
                Top = 1000,
            };

            Assert.Equal((1000 + 200) * (11 + 100), rect.Area);
        }

        [Fact()]
        public void OpEqualityTest()
        {
            int width = 7;
            int height = 5;

            Rectangle rect1 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Rectangle rect2 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Assert.True(rect1 == rect2);
        }

        [Fact()]
        public void OpInequalityTest()
        {
            int width = 7;
            int height = 5;

            Rectangle rect1 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Rectangle rect2 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Assert.False(rect1 != rect2);
        }

        [Fact()]
        public void EqualsObjectTest001()
        {
            int width = 7;
            int height = 5;

            Rectangle rect1 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Rectangle rect2 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Assert.True(rect1.Equals((object) rect2));
        }

        [Fact()]
        public void EqualsObjectTest002()
        {
            int width = 7;
            int height = 5;

            Rectangle rect1 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Rectangle rect2 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Assert.False(rect1.Equals((object)null));
        }

        [Fact()]
        public void GetHashCodeTest()
        {
            Dictionary<Rectangle, int> dict = new Dictionary<Rectangle, int>();
            for(int i = 0; i < 100; i++)
            {
                dict.Add(
                    new Rectangle
                    {
                        Top = (i * 2 + 1) * 2,
                        Right = (i * 3 + 2) * 3,
                        Left = 0,
                        Bottom = 0,
                    }, i);
            }
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

        [Fact()]
        public void ProcessEmptyRectangle001()
        {
            int width = 7;
            int height = 5;

            Rectangle rect1 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Rectangle rect2 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Rectangle rectEmpty = new Rectangle
            {
            };

            Assert.True(rect1.ProcessEmptyRect(rect2, rectEmpty));
        }

        [Fact()]
        public void ProcessEmptyRectangle002()
        {
            int width = 7;
            int height = 5;

            Rectangle rect1 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Rectangle rect2 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Rectangle rectEmpty = new Rectangle
            {
            };

            Assert.True(rect1.ProcessEmptyRect(rectEmpty, rect2));
        }

        [Fact()]
        public void ProcessEmptyRectangle003()
        {
            int width = 7;
            int height = 5;

            Rectangle rect1 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Rectangle rect2 = new Rectangle
            {
                Left = width,
                Top = height
            };

            Rectangle rectEmpty = new Rectangle
            {
            };

            Assert.False(rect1.ProcessEmptyRect(rectEmpty, rectEmpty));
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
