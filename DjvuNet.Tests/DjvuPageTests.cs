using DjvuNet;
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
using System.Text;
using System.Runtime.CompilerServices;

namespace DjvuNet.Tests
{

    public class DjvuPageTests
    {

        public static IEnumerable<object[]> TextContentData
        {
            get
            {
                var retVal = new List<object[]>();
                UTF8Encoding utf8 = new UTF8Encoding(false);

                for (int i = 1; i <= 77; i++)
                {
                    string pageText = "";
                    string path = null;
                    StreamReader info = null;
                    int pageNo = 0;

                    try
                    {
                        path = Util.GetTestFilePath(i);
                        string textPath = Path.Combine(Util.ArtifactsPath, "content",
                            Path.GetFileNameWithoutExtension(path) + ".txt");
                        info = new StreamReader(textPath, utf8);
                        pageText = null;
                        string line = info.ReadLine();
                        while(line != null)
                        {
                            if (line.StartsWith("/*** Page"))
                            {
                                line = info.ReadLine();
                                if (!line.StartsWith("//*** End Page")
                                    && !String.IsNullOrWhiteSpace(line) && line.Length > 10)
                                {
                                    pageText = line.Substring(0, line.Length - 2);
                                    break;
                                }
                                pageNo++;
                            }

                            line = info.ReadLine();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        info?.Dispose();
                    }

                    if (!String.IsNullOrWhiteSpace(pageText))
                    {
                        retVal.Add(new object[]
                        {
                            path,
                            Util.GetTestDocumentPageCount(i),
                            pageNo,
                            pageText
                        });
                    }
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
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
                Assert.Contains(expectedValue, text);
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
                Assert.Contains(expectedValue, text);
            }
        }

        [Fact()]
        public void DisposeTest001()
        {
            DjvuPage page = new DjvuPage();
            page.Dispose();
            Assert.True(page.Disposed);
            // Ensure does not throw
            page.Dispose();
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
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void GetBgPixmap_Theory(int subsample)
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.FirstPage;

                Graphics.Rectangle rect = new Graphics.Rectangle(0, 0, page.Width / subsample, page.Height / subsample);
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

                DjvuNet.Graphics.Rectangle rect = new Graphics.Rectangle(0, 0, page.Width / 12, page.Height / 12);
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

                Graphics.Rectangle rect = new Graphics.Rectangle(page.Width / 2, 0, page.Width / 2, page.Height);
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
                Assert.IsType<DjvuImage>(image);
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
                Assert.IsType<DjvuImage>(image);
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
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
        public void GetTextForLocationTest001()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(30, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);
                IDjvuPage page = document.ActivePage;
                string result = page.GetTextForLocation(new Rectangle(0, 0, page.Width, page.Height));
                Assert.NotNull(result);
                Assert.Equal(String.Empty, result);
            }
        }

        [Fact()]
        public void GetTextForLocationTest002()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);
                IDjvuPage page = document.ActivePage;
                string result = page.GetTextForLocation(new Rectangle(0, 0, page.Width, page.Height));
                Assert.False(String.IsNullOrWhiteSpace(result));
            }
        }

        [Fact()]
        public void GetBgPixMap000()
        {
            var page = new DjvuPage();
            Assert.Null(page.GetBgPixmap(new Graphics.Rectangle(), 1, 2.2, null));
        }

        [Fact()]
        public void GetBgPixMap001()
        {
            var page = new DjvuPage();
            page.Height = 128;
            page.Width = 128;
            Assert.Null(page.GetBgPixmap(new Graphics.Rectangle(), 1, 2.2, null));
        }

        [Fact()]
        public void IsLegalBilevelTest001()
        {
            string file = Path.Combine(Util.ArtifactsPath, "test078C.djvu");
            using (DjvuDocument doc = new DjvuDocument(file))
            {
                var page = (DjvuPage)doc.Pages[0];
                Assert.False(page.IsLegalBilevel());
            }
        }

        [Fact()]
        public void IsLegalBilevelTest002()
        {
            var page = new DjvuPage();
            Assert.False(page.IsLegalBilevel());
        }

        [Fact()]
        public void IsLegalBilevelTest003()
        {
            var page = new DjvuPage();
            page.Height = 128;
            page.Width = 128;
            Assert.False(page.IsLegalBilevel());
        }

        [Fact()]
        public void IsLegalCompundTest001()
        {
            string file = Path.Combine(Util.ArtifactsPath, "test078C.djvu");
            using (DjvuDocument doc = new DjvuDocument(file))
            {
                var page = (DjvuPage)doc.Pages[0];
                Assert.False(page.IsLegalCompound());
            }
        }

        [Fact()]
        public void IsLegalCompundTest002()
        {
            var page = new DjvuPage();
            Assert.False(page.IsLegalCompound());
        }

        [Fact()]
        public void IsLegalCompundTest003()
        {
            var page = new DjvuPage();
            page.Height = 128;
            page.Width = 128;
            Assert.False(page.IsLegalCompound());
        }

        [Fact()]
        public void StencilTest001()
        {
            var page = new DjvuPage();
            Assert.False(page.Stencil(null, new Graphics.Rectangle(), 1, 2.2));
        }

        [Fact()]
        public void StencilTest002()
        {
            var page = new DjvuPage();
            page.Height = 128;
            page.Width = 128;
            Assert.False(page.Stencil(null, new Graphics.Rectangle(), 1, 2.2));
        }

    }
}
