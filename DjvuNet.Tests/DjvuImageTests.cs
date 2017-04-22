using Xunit;
using DjvuNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using System.Drawing;

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
            Assert.Throws<ArgumentNullException>("page", () => new DjvuImage(null));
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
    }
}