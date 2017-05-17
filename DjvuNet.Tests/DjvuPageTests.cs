using System;
using System.Collections.Generic;
using System.Drawing;
using DPixelMap = DjvuNet.Graphics.PixelMap;
using Xunit;
using System.IO;
using System.Drawing.Imaging;
using DjvuNet.Tests.Xunit;
using Moq;
using DjvuNet.DataChunks;
using DjvuNet.Errors;

namespace DjvuNet.Tests
{

    public class DjvuPageTests
    {

        public static IEnumerable<object[]> TextContentData
        {
            get
            {
                var retVal = new List<object[]>();

                for(int i = 1; i <= 77; i++)
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

        [Fact]
        public void DjvuPageTest001()
        {
            using (MemoryStream stream = new MemoryStream())
            using (DjvuReader reader = new DjvuReader(stream))
            {
                ThumChunk form = new ThumChunk(reader, null, null, "THUM", 0);
                Assert.Throws<DjvuFormatException>(() => new DjvuPage(1, null, null, null, null, form));
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
                //DumpPageNodes(filePath, document);
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
                        dict[node.Name] += 1;
                    else
                    {
                        dict.Add(node.Name, 0);
                    }
                    string nameExt = node.Name == "BG44" || node.Name == "Incl" ? $"_P01_{dict[node.Name]}." : "_P01.";
                    string file = Path.Combine(path, fileName + nameExt + node.Name.ToLower());
                    using (FileStream fs = File.Create(file))
                    using (BinaryWriter writer = new BinaryWriter(fs))
                    {
                        byte[] buffer = node.ChunkData;
                        writer.Write(buffer, 0, buffer.Length);
                    }
                }
            }
            catch
            {
            }
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
        public void BuildImage002i()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;
                page.IsInverted = true;
                using (var image = page.BuildImage())
                {
                    Assert.NotNull(image);
                    Assert.IsType<Bitmap>(image);
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
        public void BuildPageImage002i()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);
                var page = document.FirstPage;
                page.IsInverted = true;
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
        public void ClearImage002()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);
                var page = document.FirstPage;
                var image = page.Image;
                var format = image.RawFormat;
                Assert.NotNull(image);
                page.ClearImage();
                Assert.Throws<ArgumentException>(() => image.RawFormat);
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
#if !_APPVEYOR
                    string file = Path.Combine(Util.ArtifactsDataPath, "dumps", "test003CFn.png");
                    using (FileStream stream = new FileStream(file, FileMode.Create))
                        image.Save(stream, ImageFormat.Png);
#endif
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
#if !_APPVEYOR
                    string file = Path.Combine(Util.ArtifactsDataPath, "dumps", "test003CBn.png");
                    using (FileStream stream = new FileStream(file, FileMode.Create))
                        image.Save(stream, ImageFormat.Png);
#endif
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
#if !_APPVEYOR
                    string file = Path.Combine(Util.ArtifactsDataPath, "dumps", "test003CMn.png");
                    using (FileStream stream = new FileStream(file, FileMode.Create))
                        image.Save(stream, ImageFormat.Png);
#endif
                }
            }

        }

        [Fact]
        public void ExtractThumbnailImage010()
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
        public void ExtractThumbnailImage058()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(58, out pageCount))
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

                Graphics.Rectangle rect = new Graphics.Rectangle(0, 0, 1000, 1500);
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

        public static IEnumerable<object[]> SubsampleTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                for (int i = 1; i <= 12; i++)
                    retVal.Add(new object[] { i });

                return retVal;
            }
        }

        [DjvuTheory]
        [MemberData(nameof(SubsampleTestData))]
        public void GetBgPixmap_Theory(int subsample)
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle(0, 0, page.Width/subsample, page.Height/subsample);
                Graphics.PixelMap map = new Graphics.PixelMap();
                var result = page.GetBgPixmap(rect, subsample, 2.2, map);
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
                var image = page.GetBitmapList(rect, 1, 1, null);
                    //new List<int>(new int[] { 1, 2, 3, 4, 5 }));
                Assert.NotNull(image);

                var image2 = page.GetBitmapList(rect, 1, 1, new List<int>(new int[] { 1, 2, 3, 4, 5 }));
                Assert.NotNull(image2);
            }
        }

        [Fact]
        public void GetBitmapList002()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(1, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                var rect = new Graphics.Rectangle(0, 0, 0, 0);
                var image = page.GetBitmapList(rect, 1, 1, null);
                Assert.NotNull(image);

            }
        }

        public static IEnumerable<object[]> PixelSizeTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { "Format16bppRgb555", 135173 });
                retVal.Add(new object[] { "Format16bppRgb565", 135174 });
                retVal.Add(new object[] { "Format24bppRgb", 137224 });
                retVal.Add(new object[] { "Format32bppRgb", 139273 });
                retVal.Add(new object[] { "Format8bppIndexed", 198659 });
                retVal.Add(new object[] { "Format16bppArgb1555", 397319 });
                retVal.Add(new object[] { "Format32bppPArgb", 925707 });
                retVal.Add(new object[] { "Format16bppGrayScale", 1052676 });
                retVal.Add(new object[] { "Format48bppRgb", 1060876 });
                retVal.Add(new object[] { "Format64bppPArgb", 1851406 });
                retVal.Add(new object[] { "Canonical", 2097152 });
                retVal.Add(new object[] { "Format32bppArgb", 2498570 });
                retVal.Add(new object[] { "Format64bppArgb", 3424269 });

                return retVal;
            }
        }

        public static IEnumerable<object[]> PixelSizeTestThrows
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { "Undefined", 0 });
                retVal.Add(new object[] { "DontCare", 0 });
                retVal.Add(new object[] { "Max", 15 });
                retVal.Add(new object[] { "Indexed", 65536 });
                retVal.Add(new object[] { "Gdi", 131072 });
                retVal.Add(new object[] { "Format1bppIndexed", 196865 });
                retVal.Add(new object[] { "Format4bppIndexed", 197634 });
                retVal.Add(new object[] { "Alpha", 262144 });
                retVal.Add(new object[] { "PAlpha", 524288 });
                retVal.Add(new object[] { "Extended", 1048576 });

                return retVal;
            }
        }
 
        [DjvuTheory]
        [MemberData(nameof(PixelSizeTestData))]
        public void GetPixelSize_Theory(string name, int format)
        {
            int size = DjvuPage.GetPixelSize((PixelFormat)format);
        }

        [DjvuTheory]
        [MemberData(nameof(PixelSizeTestThrows))]
        public void GetPixelSize_Theory2(string name, int format)
        {
            Assert.Throws<DjvuFormatException>(() => DjvuPage.GetPixelSize((PixelFormat)format));
        }

        [Fact]
        public void GetTextForLocation010()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(10, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle(0, 0, page.Width, page.Height);
                var result = page.GetTextForLocation(rect);
                Assert.NotNull(result);
                Assert.IsType<string>(result);
            }
        }

        [Fact]
        public void GetTextForLocation002()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(10, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle(page.Width/2, 0, page.Width/2, page.Height);
                var result = page.GetTextForLocation(rect);
                Assert.NotNull(result);
                Assert.IsType<string>(result);
            }
        }

        [Fact]
        public void Image010()
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
        public void Image001()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(1, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                var image = page.Image;
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
            }
        }

        [Fact]
        public void InvertImage010()
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
                        Graphics.IPixelMap result = null;

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

        private void VerifyPixelMap(DjvuNet.Graphics.IPixelMap result)
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

        [Fact()]
        public void BuildBitmap001()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);
                IDjvuPage page = document.Pages[0];
                Graphics.Rectangle rect = new Graphics.Rectangle(100, 0, 100, 100);
                var bitmap = page.BuildBitmap(rect, 1, 1, null);
                Assert.NotNull(bitmap);
            }
        }

        [Fact()]
        public void InvertImageTest001()
        {
            using (System.Drawing.Bitmap bitmap = new Bitmap(128, 128))
            {
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(0xed, 0xed, 0xed)))
                        g.FillRectangle(brush, 0, 0, bitmap.Width, bitmap.Height);

                }

                Bitmap bitmap2 = null;
                Bitmap bitmap3 = null;
                Bitmap bitmapInv = null;
                Bitmap bitmap2Inv = null;

                try
                {
                    bitmap2 = (Bitmap)bitmap.Clone();
                    bitmap3 = (Bitmap)bitmap.Clone();

                    bitmapInv = DjvuPage.InvertImage(bitmap);
                    bitmap2Inv = Util.InvertColor(bitmap2);

                    bool result = Util.CompareImages(bitmapInv, bitmap2Inv);
                    Assert.True(result);

                    result = Util.CompareImages(bitmap3, bitmapInv);
                    Assert.False(result);
                }
                finally
                {
                    bitmap2?.Dispose();
                    bitmap3?.Dispose();
                    bitmapInv?.Dispose();
                    bitmap2Inv?.Dispose();
                }
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

                Bitmap newImage = DjvuPage.ResizeImage(bitmap, 2 * bitmap.Width, 2 * bitmap.Height);
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
            Assert.Throws<DjvuArgumentNullException>("srcImage", () => DjvuPage.ResizeImage(null, 128, 128));
        }

        [Fact()]
        public void ResizeImage003()
        {
            using (System.Drawing.Bitmap bitmap = new Bitmap(128, 128))
            {
                Assert.Throws<DjvuArgumentException>(() => DjvuPage.ResizeImage(bitmap, -128, 128));
            }
        }

        [Fact()]
        public void ResizeImage004()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(4, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);
                IDjvuPage page = document.ActivePage;
                using (Bitmap image = page.ResizeImage(64, 64))
                {
                    Assert.NotNull(image);
                    Assert.Equal(64, image.Width);
                    Assert.Equal(64, image.Height);
                }
            }
        }
    }
}
