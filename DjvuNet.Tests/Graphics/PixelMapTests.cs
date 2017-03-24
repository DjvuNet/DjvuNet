using System;
using System.Drawing;
using Xunit;
using DjvuNet.Graphics;

namespace DjvuNet.Graphics.Tests
{

    public class PixelMapTests
    {
        [Fact]
        public void PixelMap_GotColorCorrection001()
        {
            int pageCount1 = 223;
            int pageCount2 = 62;
            using (DjvuDocument document1 = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            using (DjvuDocument document2 = new DjvuDocument("..\\..\\..\\artifacts\\test001.djvu"))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount1, document1);
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount2, document2);

                var page1 = document1.FirstPage;
                var page2 = document2.FirstPage;

                Rectangle rect1 = new Rectangle(0, 0, page1.Width, page1.Height);
                Rectangle rect2 = new Rectangle(500, 500, 500, 500);

                PixelMap map = new PixelMap();
                var result1 = page1.GetPixelMap(rect1, 1, 2.2, map);
                Assert.NotNull(result1);
                Assert.IsType<PixelMap>(result1);
                Assert.Equal<int>(3, result1.BytesPerPixel);
                Assert.Equal<int>(0, result1.BlueOffset);
                Assert.Equal<int>(1, result1.GreenOffset);
                Assert.Equal<int>(2, result1.RedOffset);

                PixelMap mapInit = new PixelMap();
                PixelMap mapInit2 = mapInit.Init(result1, rect1);

                Bitmap bitmap = null;
                bitmap = page1.GetBitmap(rect1, 1, 1, null);
                Assert.NotNull(bitmap);

                result1.Attenuate(bitmap, 0, 0);

                result1.Blit(bitmap, 0, 0, Pixel.WhitePixel);

                result1.Stencil(bitmap, result1, 1, 1, rect2, 1.0);

                var jb2img = page2.ForegroundJB2Image;
                Assert.NotNull(jb2img);

                PixelMap mapStencil = new PixelMap();
                Rectangle rectStencil = new Rectangle(0, 3000, 0, 3000);
                var mapStencil2 = page2.GetPixelMap(rectStencil, 1, 1.0, mapStencil);
                Assert.NotNull(mapStencil2);
                Assert.IsType<PixelMap>(mapStencil2);

                using (var image = result1.ToImage(System.Drawing.RotateFlipType.RotateNoneFlipNone))
                {
                    Assert.NotNull(image);
                    Assert.IsType<System.Drawing.Bitmap>(image);
                    int[] correctionTable = PixelMap.GetColorCorrection(1.2);

                    result1.Downsample(result1, 2, rect2);

                    PixelMap map2 = new PixelMap();
                    var result2 = page1.GetPixelMap(rect1, 1, 2.2, map2);

                    Rectangle rect3 = new Rectangle(100, 100, 100, 100);
                    result2.Downsample43(result1, rect3);

                    var map3 = result2.Translate(10, 10, result1);
                    Assert.NotNull(map3);

                    PixelMap map4 = new PixelMap();
                    var map5 = map4.Init(map3);

                    PixelMap map6 = new PixelMap();
                    var map7 = map6.Init(map3, rect3);

                    map2.ApplyGammaCorrection(1.2);
                    map2.ApplyGammaCorrection(3.2);

                }
            }
        }

        [Fact(Skip = "Not implemented")]
        public void PixelMapTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void GetColorCorrectionTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void AttenuateTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void BlitTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void ApplyGammaCorrectionTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void DownsampleTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void Downsample43Test()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void FillTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void InitTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void InitTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void InitTest2()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void StencilTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void TranslateTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented")]
        public void InitTest3()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}
