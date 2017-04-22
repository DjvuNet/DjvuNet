using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using DPixelMap = DjvuNet.Graphics.PixelMap;
using Xunit;
using System.IO;
using System.Drawing.Imaging;
using DjvuNet.Tests.Xunit;

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


        [DjvuTheory]
        [MemberData(nameof(TextContentData))]
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
                        $" {pageCount}\nResult page count: {document.Pages.Count}", ex);
                }

                //if (filePath.EndsWith("031C.djvu"))
                //    DumpPageNodes(filePath, document);
            }
        }

        private static void DumpPageNodes(string filePath, DjvuDocument document)
        {
            try
            {
                string path = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                path = Path.Combine(path, "data");
                var page = document.Pages[0];
                var children = page.PageForm.Children;
                Dictionary<string, int> dict = new Dictionary<string, int>();
                foreach (IDjvuNode node in children)
                {
                    if (dict.ContainsKey(node.Name))
                        continue;
                    else
                    {
                        dict.Add(node.Name, 1);
                        string file = Path.Combine(path, fileName + "_P01." + node.Name.ToLower());
                        using (FileStream fs = File.Create(file))
                        using (BinaryWriter writer = new BinaryWriter(fs))
                        {
                            byte[] buffer = node.ChunkData;
                            writer.Write(buffer, 0, buffer.Length);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private static void TestPageText(string expectedValue, IDjvuPage page)
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
        public void Text001()
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
        public void Text003()
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
        public void BuildImage001()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(1, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                using (var image = page.BuildImage())
                {
                    Assert.NotNull(image);
                    Assert.IsType<Bitmap>(image);
                    //image.Save(Path.Combine(Util.RepoRoot, "artifacts", "data", "dumps", "test001CBuildImagen.png"));

                }
            }
        }

        [Fact]
        public void BuildImage002()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                using (var image = page.BuildImage())
                {
                    Assert.NotNull(image);
                    Assert.IsType<Bitmap>(image);
                    //image.Save(Path.Combine(Util.RepoRoot, "artifacts", "data", "dumps", "test002CBuildImagen.png"));

                }
            }
        }

        [Fact]
        public void BuildImage003()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(3, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;
                var testImagePath = Path.Combine(Util.RepoRoot, "artifacts", "data", "test003C.png");

                using (var image = page.BuildImage())
                using (Bitmap testImage = new Bitmap(testImagePath))
                {
                    Assert.NotNull(image);
                    Assert.IsType<Bitmap>(image);
                    Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

                    BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    BitmapData testData = testImage.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                    bool result = Util.CompareImages(data, testData);

                    image.UnlockBits(data);
                    testImage.UnlockBits(testData);

                    //image.Save(Path.Combine(Util.RepoRoot, "artifacts", "data", "dumps", "test003CBuildImagen.png"));

                    //Assert.True(result);
                }
            }
        }

        [Fact]
        public void BuildPageImage001()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(1, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                using (var image = page.BuildPageImage())
                {
                    Assert.NotNull(image);
                    Assert.IsType<Bitmap>(image);
                    //image.Save(Path.Combine(Util.RepoRoot, "artifacts", "data", "dumps", "test001CBuildPageImagen.png"));

                }
            }
        }

        [Fact]
        public void BuildPageImage002()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                using (var image = page.BuildPageImage())
                {
                    Assert.NotNull(image);
                    Assert.IsType<Bitmap>(image);
                    //image.Save(Path.Combine(Util.RepoRoot, "artifacts", "data", "dumps", "test002CBuildPageImagen.png"));

                }
            }
        }

        [Fact]
        public void BuildPageImage003()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(3, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                IDjvuPage page = document.FirstPage;
                var testImagePath = Path.Combine(Util.RepoRoot, "artifacts", "data", "test003C.png");

                using (var image = page.BuildPageImage())
                using (Bitmap testImage = new Bitmap(testImagePath))
                {
                    Assert.NotNull(image);
                    Assert.IsType<Bitmap>(image);
                    Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

                    BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    BitmapData testData = testImage.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                    bool result = Util.CompareImages(data, testData);

                    image.UnlockBits(data);
                    testImage.UnlockBits(testData);

                    //image.Save(Path.Combine(Util.RepoRoot, "artifacts", "data", "dumps", "test003CBuildPageImagen.png"));

                    //Assert.True(result);
                }
            }
        }

        [Fact]
        public void GetForegroundImage003()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(3, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                DjvuPage page = document.FirstPage as DjvuPage;
                Assert.NotNull(page);

                using (var image = page.GetForegroundImage(1))
                {
                    Assert.NotNull(image);
                    Assert.IsType<Bitmap>(image);
                    //image.Save(Path.Combine(Util.RepoRoot, "artifacts", "data", "dumps", "test003CFn.png"));
                }
            }

        }

        [Fact]
        public void GetBackgroundImage003()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(3, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                DjvuPage page = document.FirstPage as DjvuPage;
                Assert.NotNull(page);

                using (var image = page.GetBackgroundImage(1))
                {
                    Assert.NotNull(image);
                    Assert.IsType<Bitmap>(image);
                    //image.Save(Path.Combine(Util.RepoRoot, "artifacts", "data", "dumps", "test003CBn.png"));
                }
            }
        }

        [Fact]
        public void GetTextImage003()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(3, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                DjvuPage page = document.FirstPage as DjvuPage;
                Assert.NotNull(page);

                using (var image = page.GetTextImage(1))
                {
                    Assert.NotNull(image);
                    Assert.IsType<Bitmap>(image);
                    //image.Save(Path.Combine(Util.RepoRoot, "artifacts", "data", "dumps", "test003CMn.png"));
                }
            }

        }

        [Fact]
        public void ExtractThumbnailImage001()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(10, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                using (var image = page.ExtractThumbnailImage())
                {
                    Assert.NotNull(image);
                    Assert.IsType<Bitmap>(image);
                }
            }
        }

        [Fact]
        public void GetBgPixmap001()
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
        public void GetBitmapList001()
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
        public void GetTextForLocation001()
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
        public void Image001()
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
        public void InvertImage001()
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
        public void Image003()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(3, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                IDjvuPage page = document.FirstPage;
                var testImagePath = Path.Combine(Util.RepoRoot, "artifacts", "data", "test003C.png");

                using (var image = page.Image)
                using (Bitmap testImage = new Bitmap(testImagePath))
                {
                    Assert.NotNull(image);
                    Assert.IsType<Bitmap>(image);
                    Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

                    BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    BitmapData testData = testImage.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                    bool result = Util.CompareImages(data, testData);

                    image.UnlockBits(data);
                    testImage.UnlockBits(testData);
                    //image.Save(Path.Combine(Util.RepoRoot, "artifacts", "data", "dumps", "test003CImage003n.png"));

                    //Assert.True(result);
                }
            }
        }

        [Fact]
        public void GetPixelMap001()
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
        public void GetPixelMap002()
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
        public void GetPixelMap003()
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


        [Fact()]
        public void Preload001()
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
