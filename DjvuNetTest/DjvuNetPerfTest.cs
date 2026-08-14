using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using DjvuNet;
using GBitmap = DjvuNet.Graphics.Bitmap;
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
        public void BuildMaskImage023()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(23, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                IDjvuPage page = document.FirstPage;

                DjvuImage djvuImage = page.Image as DjvuImage;
                Bitmap image = djvuImage.BuildPageImage();
                Assert.NotNull(image);
                Assert.IsType<Bitmap>(image);
            }
        }
    }
}
