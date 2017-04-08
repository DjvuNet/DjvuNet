using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using DPixelMap = DjvuNet.Graphics.PixelMap;
using Xunit;

namespace DjvuNet.Tests
{

    public class DjvuPageTests
    {

        public static IEnumerable<object[]> TextContentData
        {
            get
            {
                var retVal = new List<object[]>();

                for(int i = 1; i < 77; i++)
                {
                    string pageText = "";
                    string path = null;
                    DjvuLibre.DjvuDocumentInfo info = null;

                    try
                    {
                        path = Util.GetTestFilePath(i);
                        info = DjvuLibre.DjvuDocumentInfo.CreateDjvuDocumentInfo(path);
                        pageText = info.GetPageText(info.PageCount - 1);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        info?.Dispose();
                    }

                    retVal.Add(new object[]
                    {
                        path,
                        Util.GetTestDocumentPageCount(i),
                        info.PageCount - 1,
                        pageText
                    });
                }

                return retVal;
            }
        }


        [Theory]
        [MemberData("TextContentData")]
        public void Text_Theory001(string filePath, int pageCount, int testPage, string expectedValue)
        {
            using (DjvuDocument document = new DjvuDocument(filePath))
            {
                try
                {
                    Util.VerifyDjvuDocument(pageCount, document);
                    var page = document.Pages[testPage];
                    TestPageText(expectedValue, page);
                }
                catch (Exception ex)
                {
                    throw new AggregateException(
                        $"Text_Theory001 error with test file {filePath}\nExpected page count:" + 
                        $" {pageCount}\nResult page count: {document.Pages.Length}", ex);
                }
            }
        }

        private static void TestPageText(string expectedValue, DjvuPage page)
        {
            var text = page.Text;
            if (expectedValue != null)
            {
                Assert.NotNull(text);
                Assert.IsType<string>(text);
                Assert.True(text.Contains(expectedValue),
                    $"Test text and page text does not match.\n Expected:\n\n{expectedValue}\n\nActual:\n\n{text}\n\n");
            }
            else
                Assert.Null(text);
        }

        [Fact]
        public void DjvuPage_Text001()
        {
            string expectedValue = "Can Ann fan the lad?";
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(1, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.Pages[8];

                var text = page.Text;
                Assert.NotNull(text);
                Assert.IsType<string>(text);
                Assert.True(text.Contains(expectedValue));
            }
        }

        [Fact]
        public void DjvuPage_Text003()
        {
            string expectedValue = "This book grew out of a graduate course on 3-manifolds taught at Emory";
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(3, out pageCount))
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
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(10, out pageCount))
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
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
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
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(3, out pageCount))
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
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(10, out pageCount))
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
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
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
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(3, out pageCount))
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
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(10, out pageCount))
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
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(1, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

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
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(1, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

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
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(10, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

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
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(10, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                var image = page.Image;
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
            }
        }

        [Fact]
        public void DjvuPage_InvertImage001()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(10, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                var image = page.Image;
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);

            }
        }

        [Fact]
        public void DjvuPage_GetPixelMap001()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(10, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle();
                var image = page.GetPixelMap(rect, 1, 2.2, null);

                VerifyPixelMap(image);
            }
        }

        [Fact]
        public void DjvuPage_GetPixelMap002()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
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
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(3, out pageCount))
            {
                int count = 0;
                try
                {
                    Util.VerifyDjvuDocument(pageCount, document);

                    foreach (DjvuPage page in document.Pages)
                    {
                        if (count > 0)
                            break;

                        var jb2img = page.ForegroundJB2Image;
                        Assert.NotNull(jb2img);

                        Graphics.Rectangle rect = new Graphics.Rectangle(0, 0, page.Width, page.Height);
                        Graphics.PixelMap map = new Graphics.PixelMap();
                        DPixelMap result = null;

                        result = page.GetPixelMap(rect, 1, 2.2, map);

                        VerifyPixelMap(result);

                        Graphics.Rectangle rect12 = new Graphics.Rectangle(0, 0, page.Width / 12, page.Height / 12);
                        Graphics.PixelMap map2 = new Graphics.PixelMap();
                        result = null;

                        result = page.GetPixelMap(rect12, 12, 2.2, map2);

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


        [Fact(/*Skip = "DjvuNet bug prevents clean pass and test is to general to track the bug."*/)]
        public void DjvuPage_Preload001()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(1, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.Pages[11];
                page.Preload();
                var image = page.Image;
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
            }
        }
    }
}
