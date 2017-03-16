using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DjvuNet.Graphics;

namespace DjvuNet.Graphics.Tests
{
    [TestClass]
    public class PixelMapTests
    {
        [TestMethod]
        public void PixelMap_GotColorCorrection001()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            using (DjvuDocument document2 = new DjvuDocument("..\\..\\..\\artifacts\\test001.djvu"))
            {
                Assert.IsNotNull(document.FirstPage);
                Assert.IsNotNull(document.LastPage);
                Assert.AreEqual<int>(223, document.Pages.Length);

                var page = document.FirstPage;
                var page2 = document2.FirstPage;

                Rectangle rect = new Rectangle(2000, 2000, 2000, 2000);
                Rectangle rect2 = new Rectangle(500, 500, 500, 500);

                PixelMap map = new PixelMap();
                var result = page.GetPixelMap(rect, 0, 2.2, map);
                Assert.IsNotNull(result);
                Assert.IsInstanceOfType(result, typeof(DjvuNet.Graphics.PixelMap));
                Assert.AreEqual<int>(3, result.BytesPerPixel);
                Assert.AreEqual<int>(0, result.BlueOffset);
                Assert.AreEqual<int>(1, result.GreenOffset);
                Assert.AreEqual<int>(2, result.RedOffset);

                PixelMap mapInit = new PixelMap();
                PixelMap mapInit2 = mapInit.Init(result, rect);

                Bitmap bitmap = null;
                bitmap = page.GetBitmap(rect, 1, 1, null);
                Assert.IsNotNull(bitmap);

                result.Attenuate(bitmap, 0, 0);

                result.Blit(bitmap, 0, 0, new Pixel(0, 0, 0));

                result.Stencil(bitmap, result, 1, 1, rect2, 1.0);

                var jb2img = page2.ForegroundJB2Image;
                Assert.IsNotNull(jb2img);

                PixelMap mapStencil = new PixelMap();
                Rectangle rectStencil = new Rectangle(0, 3000, 0, 3000);
                var mapStencil2 =  page2.GetPixelMap(rectStencil, 0, 1.0, mapStencil);
                Assert.IsNotNull(mapStencil2);
                Assert.IsInstanceOfType(mapStencil2, typeof(DjvuNet.Graphics.PixelMap));

                using (var image = result.ToImage(System.Drawing.RotateFlipType.RotateNoneFlipNone))
                {
                    Assert.IsNotNull(image);
                    Assert.IsInstanceOfType(image, typeof(System.Drawing.Bitmap));
                    int[] correctionTable = PixelMap.GetColorCorrection(1.2);

                    result.Downsample(result, 2, rect2);

                    PixelMap map2 = new PixelMap();
                    var result2 = page.GetPixelMap(rect, 0, 2.2, map2);

                    Rectangle rect3 = new Rectangle(100, 100, 100, 100);
                    result2.Downsample43(result, rect3);

                    var map3 = result2.Translate(10, 10, result);
                    Assert.IsNotNull(map3);

                    PixelMap map4 = new PixelMap();
                    var map5 = map4.Init(map3);

                    PixelMap map6 = new PixelMap();
                    var map7 = map6.Init(map3, rect3);

                    map2.ApplyGammaCorrection(1.2);
                    map2.ApplyGammaCorrection(3.2);




                }
            }
        }
    }
}
