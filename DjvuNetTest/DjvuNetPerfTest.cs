using System;
using System.Collections.Generic;
using System.Text;
using DjvuNet;
using DjvuNet.Graphics;
using DjvuNet.Tests;
using Xunit;

namespace DjvuNetTest
{
    public class DjvuNetPerfTest
    {
        //[Fact]
        //public void BuildPageImage023()
        //{
        //    int pageCount = 0;
        //    using (DjvuDocument document = Util.GetTestDocument(23, out pageCount))
        //    {
        //        Util.VerifyDjvuDocument(pageCount, document);

        //        IDjvuPage page = document.FirstPage;

        //        DjvuImage djvuImage = page.Image as DjvuImage;
        //        using (Bitmap image = djvuImage.BuildImage())
        //        {
        //            Assert.NotNull(image);
        //            Assert.IsType<Bitmap>(image);
        //        }
        //    }
        //}

        [Fact]
        public void BuildImage023()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(23, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                IDjvuPage page = document.FirstPage;

                DjvuImage djvuImage = page.Image as DjvuImage;

                Rectangle rect = new Rectangle(0, 0, page.Width, page.Height);
                PixelMap map = page.GetPixelMap(rect, 1, 2.2, null);

                Assert.NotNull(map);
                Assert.IsType<PixelMap>(map);
            }
        }

        //[Fact]
        //public void BuildImage075()
        //{
        //    int pageCount = 0;
        //    using (DjvuDocument document = Util.GetTestDocument(75, out pageCount))
        //    {
        //        Util.VerifyDjvuDocument(pageCount, document);

        //        IDjvuPage page = document.FirstPage;

        //        DjvuImage djvuImage = page.Image as DjvuImage;

        //        Rectangle rect = new Rectangle(0, 0, page.Width, page.Height);
        //        PixelMap map = page.GetPixelMap(rect, 1, 2.2, null);

        //        Assert.NotNull(map);
        //        Assert.IsType<PixelMap>(map);
        //    }
        //}
    }
}
