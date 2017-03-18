using System;
using System.Collections.Generic;
using System.Drawing;
using DPixelMap = DjvuNet.Graphics.PixelMap;
using Xunit;

namespace DjvuNet.Tests
{

    public class DjvuPageTests
    {
        [Fact]
        public void DjvuPage_Text001()
        {
            int pageCount = 223;
            string expectedValue = "charged particle is a diverging wave. I argue that this condition is best thought of as";
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.Pages[31];

                var text = page.Text;
                Assert.NotNull(text);
                Assert.IsType<string>(text);
                Assert.True(text.Contains(expectedValue));
            }
        }

        [Fact]
        public void DjvuPage_Text003()
        {
            int pageCount = 300;
            string expectedValue = "This book grew out of a graduate course on 3-manifolds taught at Emory";
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test003.djvu"))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.Pages[10];

                var text = page.Text;
                Assert.NotNull(text);
                Assert.IsType<string>(text);
                Assert.True(text.Contains(expectedValue));
            }
        }

        [Fact]
        public void DjvuPage_BuildImage001()
        {
            int pageCount = 223;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                var image = page.BuildImage();
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
            }
        }

        [Fact]
        public void DjvuPage_BuildImage002()
        {
            int pageCount = 107;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test002.djvu"))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                var image = page.BuildImage();
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
            }
        }

        [Fact]
        public void DjvuPage_BuildImage003()
        {
            int pageCount = 300;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test003.djvu"))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                var image = page.BuildImage();
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
            }
        }

        [Fact]
        public void DjvuPage_BuildPageImage001()
        {
            int pageCount = 223;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                var image = page.BuildPageImage();
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
            }
        }

        [Fact]
        public void DjvuPage_BuildPageImage002()
        {
            int pageCount = 107;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test002.djvu"))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                var image = page.BuildPageImage();
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
            }
        }

        [Fact]
        public void DjvuPage_BuildPageImage003()
        {
            int pageCount = 300;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test003.djvu"))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                var image = page.BuildPageImage();
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
            }
        }

        [Fact]
        public void DjvuPage_ExtractThumbnailImage001()
        {
            int pageCount = 223;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                var image = page.ExtractThumbnailImage();
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
            }
        }

        [Fact]
        public void DjvuPage_GetBgPixmap001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test001.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(62, document.Pages.Length);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle(1000, 1500, 1000, 1500);
                Graphics.PixelMap map = new Graphics.PixelMap();
                var result = page.GetBgPixmap(rect, 1, 2.2, map);
                Assert.NotNull(result);

                Assert.IsType<Graphics.PixelMap>(result);
                Assert.Equal<int>(3, result.BytesPerPixel);
                Assert.Equal<int>(0, result.BlueOffset);
                Assert.Equal<int>(1, result.GreenOffset);
                Assert.Equal<int>(2, result.RedOffset);
            }
        }

        [Fact]
        public void DjvuPage_GetBitmapList001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test001.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(62, document.Pages.Length);

                var page = document.FirstPage;

                DjvuNet.Graphics.Rectangle rect = new Graphics.Rectangle(0, 0, page.Width/12, page.Height/12);
                // By setting subsample 1, align 0 one gets AccessViolationException 
                // subsample 0, align 0 - 10 gives ArgumentException
                // subsample 12, align 0 gives NullReferenceException
                // subsample 1 - 12, align 1, components list 1 - 5 or null is OK
                DjvuNet.Graphics.Bitmap image = page.GetBitmapList(rect, 1, 1, null);
                    //new List<int>(new int[] { 1, 2, 3, 4, 5 }));
                Assert.NotNull(image);

                DjvuNet.Graphics.Bitmap image2 = page.GetBitmapList(rect, 1, 1,
                new List<int>(new int[] { 1, 2, 3, 4, 5 }));
                Assert.NotNull(image2);
            }
        }

        [Fact]
        public void DjvuPage_GetTextForLocation001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(223, document.Pages.Length);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle(3108, 5042, 3108, 5042);
                var result = page.GetTextForLocation(rect);
                Assert.NotNull(result);
                Assert.IsType<string>(result);
            }
        }

        [Fact]
        public void DjvuPage_Image001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(223, document.Pages.Length);

                var page = document.FirstPage;

                var image = page.Image;
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
            }
        }

        [Fact]
        public void DjvuPage_InvertImage001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(223, document.Pages.Length);

                var page = document.FirstPage;

                var image = page.Image;
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);

            }
        }

        [Fact]
        public void DjvuPage_GetPixelMap001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(223, document.Pages.Length);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle();
                var image = page.GetPixelMap(rect, 1, 2.2, null);

                VerifyPixelMap(image);
            }
        }

        [Fact]
        public void DjvuPage_GetPixelMap002()
        {
            int pageCount = 107;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test002.djvu"))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle();
                Graphics.PixelMap map = new Graphics.PixelMap();
                var image = page.GetPixelMap(rect, 1, 2.2, map);

                VerifyPixelMap(image);
            }
        }

        [Fact]
        public void DjvuPage_GetPixelMap003()
        {
            int pageCount = 300;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test003.djvu"))
            {
                int count = 0;
                try
                {
                    Util.VerifyDjvuDocument(pageCount, document);

                    foreach (DjvuPage page in document.Pages)
                    {
                        if (count > 5)
                            break;

                        var jb2img = page.ForegroundJB2Image;
                        Assert.NotNull(jb2img);

                        Graphics.Rectangle rect = new Graphics.Rectangle(0, 0, page.Width, page.Height);
                        Graphics.PixelMap map = new Graphics.PixelMap();
                        DPixelMap result = null;

                        try
                        {
                            result = page.GetPixelMap(rect, 1, 2.2, map);
                        }
                        catch (Exception ex)
                        {
                            Util.FailOnException(ex, $"1. Exception while calling DjvuPage.GetPixelMap:\n");
                        }

                        VerifyPixelMap(result);

                        Graphics.Rectangle rect12 = new Graphics.Rectangle(0, 0, page.Width / 12, page.Height / 12);
                        Graphics.PixelMap map2 = new Graphics.PixelMap();
                        result = null;

                        try
                        {
                            result = page.GetPixelMap(rect12, 12, 2.2, map2);
                        }
                        catch (Exception ex)
                        {
                            Util.FailOnException(ex, $"2. Exception while calling DjvuPage.GetPixelMap:\n");
                        }

                        VerifyPixelMap(result);

                        count++;
                    }
                }
                catch (Exception err)
                {
                    Util.FailOnException(err, "Error on DjvuPage.GetPixelMap call cycle count {0}", count);
                }
            }
        }

        private void VerifyPixelMap(DPixelMap result)
        {
            Assert.NotNull(result);
            Assert.IsType<DPixelMap>(result);
            Assert.Equal<int>(3, result.BytesPerPixel);
            Assert.Equal<int>(0, result.BlueOffset);
            Assert.Equal<int>(1, result.GreenOffset);
            Assert.Equal<int>(2, result.RedOffset);
        }


        [Fact(Skip = "DjvuNet bug prevents clean pass and test is to general to track the bug.")]
        public void DjvuPage_Preload001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test001.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(62, document.Pages.Length);

                var page = document.Pages[11];
                page.Preload();
                var image = page.Image;
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
            }
        }
    }
}
