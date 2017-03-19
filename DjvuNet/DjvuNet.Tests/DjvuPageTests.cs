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
                return new List<object[]>
                {
                     new object[] {"..\\..\\..\\artifacts\\test001.djvu", 62, 9, "Can Ann fan the lad?" },
                     new object[] {"..\\..\\..\\artifacts\\test002.djvu", 107, 2, "The famous Riemann Hypothesis, asserting that all of the non-trivial zeros of zeta are on"},
                     new object[] {"..\\..\\..\\artifacts\\test003.djvu", 300, 37, "We have discussed manifolds from several points of view. The key idea,"},
                     new object[] {"..\\..\\..\\artifacts\\test004.djvu", 494, 485, "77. Yaglom, I.M., Geometric Transformations II, Mathematical Association of America,"},
                     new object[] {"..\\..\\..\\artifacts\\test005.djvu", 286, 10, "Because of these divisions in the human population, anyone"},
                     new object[] {"..\\..\\..\\artifacts\\test006.djvu", 348, 211, "property of the exponential density, as the following intuitive argument"},
                     new object[] {"..\\..\\..\\artifacts\\test007.djvu", 186, 71, "In view of (3.2.8), the final assertion when j is recurrent comes down to"},
                     new object[] {"..\\..\\..\\artifacts\\test008.djvu", 427, 5, "ISBN 978-1-107-10012-1 Hardback"},
                     new object[] {"..\\..\\..\\artifacts\\test009.djvu", 274, 250, "a perfect mirror - something that does not exist in nature? Could the approach"},
                     new object[] {"..\\..\\..\\artifacts\\test010.djvu", 223, 32, "charged particle is a diverging wave. I argue that this condition is best thought of as"},
                     new object[] {"..\\..\\..\\artifacts\\test011.djvu", 154, 39, "Definition 3.14 (Diameter). The diameter of a topology is the maximal number of links"},
                     new object[] {"..\\..\\..\\artifacts\\test012.djvu", 239, 2, "Second printing of the third edition with ISBN 3-540-64611-6, published as softcover edition"},
                     new object[] {"..\\..\\..\\artifacts\\test013.djvu", 9, 9, "DUNKL, С. Р. 1962. Romberg quadrature to prescribed accuracy. SHARE File 7090-1481,"},
                     new object[] {"..\\..\\..\\artifacts\\test013.djvu", 9, 9, "DUNKL, C. P. AND RAMIREZ, D. E. 1994. Computing hyperelliptic integrals for surface measure of"},
                     new object[] {"..\\..\\..\\artifacts\\test014.djvu", 20, 1, "algebra system Maple [16], [4] and the Maple packages FPS [9], [7], gfun [19], hsum"},
                     new object[] {"..\\..\\..\\artifacts\\test015.djvu", 40, 15, "so that p (5) = 7. The generating function in this case"},
                     // new object[] {"..\\..\\..\\artifacts\\test016.djvu", 30}, - text without OCR
                     new object[] {"..\\..\\..\\artifacts\\test017.djvu", 12, 1, "1. Introduction. The Riemann zeta function f(s) is the analytic function of s ="},
                     new object[] {"..\\..\\..\\artifacts\\test018.djvu", 7, 3, "3.1 1—1 correspondence proof"},
                     new object[] {"..\\..\\..\\artifacts\\test018.djvu", 7, 3, "Figure 3: The Riemann surface of W(z)"},
                     new object[] {"..\\..\\..\\artifacts\\test019.djvu", 28, 1, "MA 02142-1493 USA; F17J53-2889; journals-orders@mit.edu, journals-info@mit.edu."},
                     new object[] {"..\\..\\..\\artifacts\\test020.djvu", 5, 5, "implementation independent: the average number of main"},
                     //new object[] {"..\\..\\..\\artifacts\\test021.djvu", 12, 1, "Algebraic Algorithms using p-adic Constructions"}, // Crashes test execution engine
                     new object[] {"..\\..\\..\\artifacts\\test022.djvu", 10, 7, "and prime p is again provided by the EZGCD Algor-"},
                     new object[] {"..\\..\\..\\artifacts\\test023.djvu", 3, 2, "• Part 3 is split again into 4 parts recursively and its"},
                     new object[] {"..\\..\\..\\artifacts\\test023.djvu", 3, 2, "Part 3 is split again into 4 parts recursively and its"},
                     new object[] {"..\\..\\..\\artifacts\\test024.djvu", 3, 1, "are analog to S-polynomials of Buchberger and cntical pairs"},
                     new object[] {"..\\..\\..\\artifacts\\test025.djvu", 9, 2, "time with the sparse representation. Theorem 1 states that the existence of an integer root"},
                     new object[] {"..\\..\\..\\artifacts\\test026.djvu", 146, 93, "One interesting example of a flexible Lie-admissible algebra is the so-"},
                     new object[] {"..\\..\\..\\artifacts\\test027.djvu", 173, 70, "It is worth noting that if η € N is a value of the mapping F and if m € Μ"},
                     new object[] {"..\\..\\..\\artifacts\\test027.djvu", 173, 70, "It is not difficult to see that Theorem 3.5.1 is also true when the domain W"},
                     new object[] {"..\\..\\..\\artifacts\\test028.djvu", 267, 38, "global solution (see Snyman and Fatti, 1987; Groenwold and Snyman,"},
                     new object[] {"..\\..\\..\\artifacts\\test029.djvu", 323, 215, "Proof. For any large integer r, we can apply the solvability theorem (8.3.6)"},
                     //new object[] {"..\\..\\..\\artifacts\\test030.djvu", 1}, - technical drawing without OCR
                    //new object[] {"..\\..\\..\\artifacts\\test001.djvu"},
                    //new object[] {"..\\..\\..\\artifacts\\test001.djvu"},
                    //new object[] {"..\\..\\..\\artifacts\\test001.djvu"},
                };
            }
        }


        [Theory]
        [MemberData("TextContentData")]
        public void DjvuPage_Text_Theory001(string filePath, int pageCount, int testPage, string expectedValue)
        {
            using (DjvuDocument document = new DjvuDocument(filePath))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.Pages[testPage - 1];

                TestPageText(expectedValue, page);
            }

            Thread.Yield();
            Thread.Sleep(500);
        }

        private static void TestPageText(string expectedValue, DjvuPage page)
        {
            var text = page.Text;
            Assert.NotNull(text);
            Assert.IsType<string>(text);
            Assert.True(text.Contains(expectedValue), 
                $"Test text and page text does not match.\n Expected:\n\n{expectedValue}\n\nActual:\n\n{text}\n\n");
        }

        [Fact]
        public void DjvuPage_Text001()
        {
            int pageCount = 62;
            string expectedValue = "Can Ann fan the lad?";
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test001.djvu"))
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


        [Fact(/*Skip = "DjvuNet bug prevents clean pass and test is to general to track the bug."*/)]
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
