using System;
using System.Collections.Generic;
using System.Drawing;
using DjvuNet;
using DjvuNet.Errors;
using Moq;
using Xunit;

namespace DjvuNet.Tests
{
    public class DjvuImageTests
    {
        [Fact()]
        public void DjvuImageTest001()
        {
            DjvuImage image = new DjvuImage();
            Assert.Null(image.Page);
            Assert.Null(image.Document);
        }

        [Fact()]
        public void DjvuImageTest002()
        {
            Assert.Throws<DjvuArgumentNullException>("page", () => new DjvuImage(null));
        }

        [Fact()]
        public void DjvuImageTest003()
        {
            Mock<IDjvuPage> pageMock = new Mock<IDjvuPage>();
            Mock<IDjvuDocument> documentMock = new Mock<IDjvuDocument>();
            pageMock.Setup(x => x.Document).Returns(documentMock.Object);

            DjvuImage image = new DjvuImage(pageMock.Object);
            Assert.Same(pageMock.Object, image.Page);
            Assert.Same(documentMock.Object, image.Document);
        }

        [Fact()]
        public void CreateBlankImageTest()
        {
            Mock<IDjvuPage> pageMock = new Mock<IDjvuPage>();
            Mock<IDjvuDocument> documentMock = new Mock<IDjvuDocument>();
            pageMock.Setup(x => x.Document).Returns(documentMock.Object);
            pageMock.Setup(x => x.Width).Returns(100);
            pageMock.Setup(x => x.Height).Returns(200);
            IDjvuPage page = pageMock.Object;

            DjvuImage image = new DjvuImage(page);
            Assert.Same(pageMock.Object, image.Page);
            Assert.Same(documentMock.Object, image.Document);

            using(Bitmap bitmap = image.CreateBlankImage(Brushes.White))
            {
                Assert.Equal<int>(page.Width, bitmap.Width);
                Assert.Equal<int>(page.Height, bitmap.Height);
                Color c = bitmap.GetPixel(0, 0);
                Assert.Equal<byte>(0xff, c.A);
                Assert.Equal<byte>(0xff, c.B);
                Assert.Equal<byte>(0xff, c.G);
                Assert.Equal<byte>(0xff, c.R);
            }
        }

        [Fact()]
        public void CreateBlankImageTest1()
        {
            using (Bitmap bitmap = DjvuImage.CreateBlankImage(Brushes.White, 100, 200))
            {
                Assert.Equal<int>(100, bitmap.Width);
                Assert.Equal<int>(200, bitmap.Height);
                Color c = bitmap.GetPixel(0, 0);
                Assert.Equal<byte>(0xff, c.A);
                Assert.Equal<byte>(0xff, c.B);
                Assert.Equal<byte>(0xff, c.G);
                Assert.Equal<byte>(0xff, c.R);
            }
        }

        [Fact()]
        public void ResizeImage001()
        {
            int wh = 128;
            using (System.Drawing.Bitmap bitmap = new Bitmap(wh, wh))
            {
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(0xed, 0xed, 0xed)))
                        g.FillRectangle(brush, 0, 0, bitmap.Width, bitmap.Height);

                }

                Bitmap newImage = DjvuImage.ResizeImage(bitmap, 2 * bitmap.Width, 2 * bitmap.Height);
                Assert.Equal(2 * wh, newImage.Width);
                Assert.Equal(2 * wh, newImage.Height);
                Color c = newImage.GetPixel(newImage.Width / 2, newImage.Height / 2);
                Assert.Equal(0xed, c.R);
                Assert.Equal(0xed, c.G);
                Assert.Equal(0xed, c.B);
            }
        }

        [Fact()]
        public void ResizeImage002()
        {
            Assert.Throws<DjvuArgumentNullException>("srcImage", () => DjvuImage.ResizeImage(null, 128, 128));
        }

        [Fact()]
        public void ResizeImage003()
        {
            using (System.Drawing.Bitmap bitmap = new Bitmap(128, 128))
            {
                Assert.Throws<DjvuArgumentException>(() => DjvuImage.ResizeImage(bitmap, -128, 128));
            }
        }

    }
}