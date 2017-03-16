using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DjvuNet.Tests
{
    [TestClass]
    public class DjvuPageTests
    {
        [TestMethod]
        public void DjvuPage_Text001()
        {
            string expectedValue = "charged particle is a diverging wave. I argue that this condition is best thought of as";
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(223, document.Pages.Length);

                var page = document.Pages[31];

                var text = page.Text;
                Assert.IsNotNull(text);
                Assert.IsInstanceOfType(text, typeof(string));
                Assert.IsTrue(text.Contains(expectedValue));
            }
        }

        [TestMethod]
        public void DjvuPage_BuildImage001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(223, document.Pages.Length);

                var page = document.FirstPage;

                var image = page.BuildImage();
                Assert.IsNotNull(image);
                Assert.IsInstanceOfType(image, typeof(Bitmap));
            }
        }

        [TestMethod]
        public void DjvuPage_BuildPageImage001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(223, document.Pages.Length);

                var page = document.FirstPage;

                var image = page.BuildPageImage();
                Assert.IsNotNull(image);
                Assert.IsInstanceOfType(image, typeof(Bitmap));
            }
        }

        [TestMethod]
        public void DjvuPage_ExtractThumbnailImage001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(223, document.Pages.Length);

                var page = document.FirstPage;

                var image = page.ExtractThumbnailImage();
                Assert.IsNotNull(image);
                Assert.IsInstanceOfType(image, typeof(Bitmap));
            }
        }

        [TestMethod]
        public void DjvuPage_GetBgPixmap001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test001.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(62, document.Pages.Length);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle(1000, 1500, 1000, 1500);
                Graphics.PixelMap map = new Graphics.PixelMap();
                var result = page.GetBgPixmap(rect, 1, 2.2, map);
                Assert.IsNotNull(result);

                Assert.IsInstanceOfType(result, typeof(Graphics.PixelMap));
                Assert.AreEqual<int>(3, result.BytesPerPixel);
                Assert.AreEqual<int>(0, result.BlueOffset);
                Assert.AreEqual<int>(1, result.GreenOffset);
                Assert.AreEqual<int>(2, result.RedOffset);
            }
        }

        [TestMethod]
        public void DjvuPage_GetTextForLocation001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(223, document.Pages.Length);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle(3108, 5042, 3108, 5042);
                var result = page.GetTextForLocation(rect);
                Assert.IsNotNull(result);
                Assert.IsInstanceOfType(result, typeof(string));
            }
        }

        [TestMethod]
        public void DjvuPage_Image001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(223, document.Pages.Length);

                var page = document.FirstPage;

                var image = page.Image;
                Assert.IsNotNull(image);
                Assert.IsInstanceOfType(image, typeof(Bitmap));
            }
        }

        [TestMethod]
        public void DjvuPage_InvertImage001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(223, document.Pages.Length);

                var page = document.FirstPage;

                var image = page.Image;
                Assert.IsNotNull(image);
                Assert.IsInstanceOfType(image, typeof(Bitmap));

            }
        }

        [TestMethod]
        public void DjvuPage_PixelMap001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(223, document.Pages.Length);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle();
                var image = page.GetPixelMap(rect, 0, 2.2, null);
                Assert.IsNotNull(image);
                Assert.IsInstanceOfType(image, typeof(Graphics.PixelMap));
                Assert.AreEqual<int>(3, image.BytesPerPixel);
                Assert.AreEqual<int>(0, image.BlueOffset);
                Assert.AreEqual<int>(1, image.GreenOffset);
                Assert.AreEqual<int>(2, image.RedOffset);
            }
        }

        [TestMethod]
        public void DjvuPage_PixelMap002()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(223, document.Pages.Length);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle();
                Graphics.PixelMap map = new Graphics.PixelMap();
                var image = page.GetPixelMap(rect, 0, 2.2, map);
                Assert.IsNotNull(image);
                Assert.IsInstanceOfType(image, typeof(Graphics.PixelMap));
                Assert.AreEqual<int>(3, image.BytesPerPixel);
                Assert.AreEqual<int>(0, image.BlueOffset);
                Assert.AreEqual<int>(1, image.GreenOffset);
                Assert.AreEqual<int>(2, image.RedOffset);
            }
        }

        [TestMethod]
        public void DjvuPage_PixelMap003()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(223, document.Pages.Length);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle(1000, 1000, 1000, 1000);
                Graphics.PixelMap map = new Graphics.PixelMap();
                var result = page.GetPixelMap(rect, 0, 2.2, map);
                Assert.IsNotNull(result);
                Assert.IsInstanceOfType(result, typeof(Graphics.PixelMap));
                Assert.AreEqual<int>(3, result.BytesPerPixel);
                Assert.AreEqual<int>(0, result.BlueOffset);
                Assert.AreEqual<int>(1, result.GreenOffset);
                Assert.AreEqual<int>(2, result.RedOffset);
            }
        }

        [TestMethod][Ignore]
        public void DjvuPage_Preload001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test001.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(62, document.Pages.Length);

                var page = document.Pages[11];
                page.Preload();
                var image = page.Image;
                Assert.IsNotNull(image);
                Assert.IsInstanceOfType(image, typeof(Bitmap));
            }
        }
    }
}
