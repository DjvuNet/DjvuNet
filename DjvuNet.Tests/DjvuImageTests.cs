using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using DjvuNet.Errors;
using DjvuNet.Tests.Xunit;
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


        public static IEnumerable<object[]> PixelSizeTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { "Format8bppIndexed", 198659 });
                retVal.Add(new object[] { "Format16bppRgb555", 135173 });
                retVal.Add(new object[] { "Format16bppRgb565", 135174 });
                retVal.Add(new object[] { "Format16bppGrayScale", 1052676 });
                retVal.Add(new object[] { "Format16bppArgb1555", 397319 });
                retVal.Add(new object[] { "Format24bppRgb", 137224 });
                retVal.Add(new object[] { "Format32bppRgb", 139273 });
                retVal.Add(new object[] { "Format32bppPArgb", 925707 });
                retVal.Add(new object[] { "Format32bppArgb", 2498570 });
                retVal.Add(new object[] { "Format48bppRgb", 1060876 });
                retVal.Add(new object[] { "Format64bppPArgb", 1851406 });
                retVal.Add(new object[] { "Format64bppArgb", 3424269 });
                retVal.Add(new object[] { "Canonical", 2097152 });

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
            int size = DjvuImage.GetPixelSize((PixelFormat)format);
        }

        [DjvuTheory]
        [MemberData(nameof(PixelSizeTestThrows))]
        public void GetPixelSize_Theory2(string name, int format)
        {
            Assert.Throws<DjvuFormatException>(() => DjvuImage.GetPixelSize((PixelFormat)format));
        }

        [Fact()]
        public void CachePageImagesTest()
        {
            int pageCount;
            using (DjvuDocument doc = Util.GetTestDocument(5, out pageCount))
            {
                doc.CachePageImages(doc.Pages);
                var cached = doc.Pages.Where(p => p.Image.IsPageImageCached).ToList();
                Assert.NotNull(cached);
                // TODO verify logic after method reimplementation
                Assert.Equal(0, cached.Count);
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
                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.BuildImage())
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

                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.BuildImage())
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
                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.BuildImage())
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

                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.BuildImage())
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

        [Fact()]
        public void BuildPageImageTest000()
        {
            string file = Path.Combine(Util.ArtifactsPath, "test077C.djvu");
            using (DjvuDocument doc = new DjvuDocument(file))
            {
                var page = (DjvuPage)doc.Pages[0];
                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.BuildPageImage())
                {
                    page.IsInverted = true;
                    using (Bitmap imageInv = djvuImage.BuildPageImage())
                    {
                        Assert.NotNull(image);
                        Assert.NotNull(imageInv);
                        Assert.NotSame(image, imageInv);
                        Color pix = image.GetPixel(0, 0);
                        Color pixInv = imageInv.GetPixel(0, 0);
                        Assert.Equal(pix.R, 255 - pixInv.R);
                        Assert.Equal(pix.G, 255 - pixInv.G);
                        Assert.Equal(pix.B, 255 - pixInv.B);
                    }
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

                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.BuildImage())
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

                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.BuildImage())
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
                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.BuildImage())
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

                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.BuildImage())
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
                var djvuImage = page.Image as DjvuImage;
                Assert.NotNull(djvuImage);
                var image = djvuImage.Image;

                Assert.NotNull(image);
                var format = image.RawFormat;

                page.Image.ClearImage();
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

                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.GetForegroundImage(1))
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

                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.GetBackgroundImage(1))
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

                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.GetTextImage(1))
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

                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.ExtractThumbnailImage())
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

                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.ExtractThumbnailImage())
                {
                    Assert.NotNull(image);
                    Assert.IsType<Bitmap>(image);
                }
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
                var testImagePath = Path.Combine(Util.ArtifactsDataPath, "test003C.png");

                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var image = djvuImage.Image)
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

        [Fact()]
        public void Preload001()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(1, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.Pages[11];
                page.Image.Preload();
                var image = ((DjvuImage)page.Image).Image;
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
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

                    bitmapInv = DjvuImage.InvertImage(bitmap);
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
        public void ResizeImage0011()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(4, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);
                IDjvuPage page = document.ActivePage;
                DjvuImage djvuImage = page.Image as DjvuImage;
                using (Bitmap image = djvuImage.ResizeImage(64, 64))
                {
                    Assert.NotNull(image);
                    Assert.Equal(64, image.Width);
                    Assert.Equal(64, image.Height);
                }
            }
        }

        [Fact()]
        public void InvertColorTest001()
        {
            int color = 0x00ffffff;
            int result = DjvuImage.InvertColor(color);
            Assert.Equal(0x00000000, result);
        }

        [Fact()]
        public void InvertColorTest002()
        {
            int color = 0x00000000;
            int result = DjvuImage.InvertColor(color);
            Assert.Equal(0x00ffffff, result);
        }

        [Fact()]
        public void InvertColorTest003()
        {
            int color = 0x00f0f0f0;
            int result = DjvuImage.InvertColor(color);
            Assert.Equal(0x000f0f0f, result);
        }

        [Fact()]
        public void GetBackgroundImage078()
        {
            string file = Path.Combine(Util.ArtifactsPath, "test078C.djvu");
            using (DjvuDocument doc = new DjvuDocument(file))
            {
                var page = (DjvuPage)doc.Pages[0];
                page.Height = 128;
                page.Width = 128;
                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var bitmap = djvuImage.GetBackgroundImage(1))
                {
                    Assert.NotNull(bitmap);
                    Assert.NotEqual(0, page.Width);
                    Assert.Equal(page.Width, bitmap.Width);
                    Assert.Equal(page.Height, bitmap.Height);
                    var pixel = bitmap.GetPixel(0, 0);
                    Assert.Equal(255, pixel.R);
                    Assert.Equal(255, pixel.G);
                    Assert.Equal(255, pixel.B);
                }
            }
        }

        [Fact()]
        public void GetBackgroundImage000()
        {
            var page = new DjvuPage();
            page.Height = 128;
            page.Width = 128;
            DjvuImage djvuImage = page.Image as DjvuImage;
            using (var bitmap = djvuImage.GetBackgroundImage(1))
            {
                Assert.NotNull(bitmap);
                Assert.NotEqual(0, page.Width);
                Assert.Equal(page.Width, bitmap.Width);
                Assert.Equal(page.Height, bitmap.Height);
                var pixel = bitmap.GetPixel(0, 0);
                Assert.Equal(255, pixel.R);
                Assert.Equal(255, pixel.G);
                Assert.Equal(255, pixel.B);
            }
        }


        [Fact()]
        public void GetForegroundImage078()
        {
            string file = Path.Combine(Util.ArtifactsPath, "test078C.djvu");
            using (DjvuDocument doc = new DjvuDocument(file))
            {
                var page = (DjvuPage)doc.Pages[0];
                page.Height = 128;
                page.Width = 128;
                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var bitmap = djvuImage.GetForegroundImage(1))
                {
                    Assert.NotNull(bitmap);
                    Assert.NotEqual(0, page.Width);
                    Assert.Equal(page.Width, bitmap.Width);
                    Assert.Equal(page.Height, bitmap.Height);
                    var pixel = bitmap.GetPixel(0, 0);
                    Assert.Equal(0, pixel.R);
                    Assert.Equal(0, pixel.G);
                    Assert.Equal(0, pixel.B);
                }
            }
        }

        [Fact()]
        public void GetForegroundImage000()
        {
            var page = new DjvuPage();
            page.Height = 128;
            page.Width = 128;
            DjvuImage djvuImage = page.Image as DjvuImage;
            using (var bitmap = djvuImage.GetForegroundImage(1))
            {
                Assert.NotNull(bitmap);
                Assert.NotEqual(0, page.Width);
                Assert.Equal(page.Width, bitmap.Width);
                Assert.Equal(page.Height, bitmap.Height);
                var pixel = bitmap.GetPixel(0, 0);
                Assert.Equal(0, pixel.R);
                Assert.Equal(0, pixel.G);
                Assert.Equal(0, pixel.B);
            }
        }

        [Fact()]
        public void GetTextImage078()
        {
            string file = Path.Combine(Util.ArtifactsPath, "test078C.djvu");
            using (DjvuDocument doc = new DjvuDocument(file))
            {
                var page = (DjvuPage)doc.Pages[0];
                page.Height = 128;
                page.Width = 128;
                DjvuImage djvuImage = page.Image as DjvuImage;
                using (var bitmap = djvuImage.GetTextImage(1))
                {
                    Assert.NotNull(bitmap);
                    Assert.NotEqual(0, page.Width);
                    Assert.Equal(page.Width, bitmap.Width);
                    Assert.Equal(page.Height, bitmap.Height);
                    var pixel = bitmap.GetPixel(0, 0);
                    Assert.Equal(0, pixel.R);
                    Assert.Equal(0, pixel.G);
                    Assert.Equal(0, pixel.B);
                }
            }
        }

        [Fact()]
        public void GetTextImage000()
        {
            var page = new DjvuPage();
            page.Height = 128;
            page.Width = 128;
            DjvuImage djvuImage = page.Image as DjvuImage;
            using (var bitmap = djvuImage.GetTextImage(1))
            {
                Assert.NotNull(bitmap);
                Assert.NotEqual(0, page.Width);
                Assert.Equal(page.Width, bitmap.Width);
                Assert.Equal(page.Height, bitmap.Height);
                var pixel = bitmap.GetPixel(0, 0);
                Assert.Equal(0, pixel.R);
                Assert.Equal(0, pixel.G);
                Assert.Equal(0, pixel.B);
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
                Assert.IsType<DjvuImage>(image);
                Assert.NotNull(image.Image);
                Assert.IsType<Bitmap>(image.Image);
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
                Assert.IsType<DjvuImage>(image);
                Assert.NotNull(image.Image);
                Assert.IsType<Bitmap>(image.Image);
            }
        }
    }
}
