using Xunit;
using DjvuNet.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Errors;

namespace DjvuNet.Graphics.Tests
{
    public class PixelMapScalerTests
    {
        private static PixelMapScaler CreateVerifyPixelMapScaler(int srcWidth, int srcHeight, int targetWidth, int targetHeight)
        {
            PixelMapScaler scaler = new PixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);
            Assert.Null(scaler._PixelMap1);
            Assert.Null(scaler._PixelMap2);
            Assert.Null(scaler._HCoord);
            Assert.Null(scaler._VCoord);
            Assert.Equal(-1, scaler._L1);
            Assert.Equal(-1, scaler._L2);
            Assert.Equal(srcWidth, scaler._SrcWidth);
            Assert.Equal(srcHeight, scaler._SrcHeight);
            Assert.Equal(targetWidth, scaler._DestWidth);
            Assert.Equal(targetHeight, scaler._DestHeight);
            return scaler;
        }

        [Fact()]
        public void PixelMapScalerTest001()
        {
            PixelMapScaler scaler = new PixelMapScaler();
            Assert.Null(scaler._PixelMap1);
            Assert.Null(scaler._PixelMap2);
            Assert.Null(scaler._HCoord);
            Assert.Null(scaler._VCoord);
            Assert.Equal(-1, scaler._L1);
            Assert.Equal(-1, scaler._L2);
        }

        [Fact()]
        public void PixelMapScalerTest002()
        {
            int srcWidth = 16;
            int srcHeight = 17;
            int targetWidth = 18;
            int targetHeight = 19;

            PixelMapScaler scaler = new PixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);
            Assert.Null(scaler._PixelMap1);
            Assert.Null(scaler._PixelMap2);
            Assert.Null(scaler._HCoord);
            Assert.Null(scaler._VCoord);
            Assert.Equal(-1, scaler._L1);
            Assert.Equal(-1, scaler._L2);
            Assert.Equal(srcWidth, scaler._SrcWidth);
            Assert.Equal(srcHeight, scaler._SrcHeight);
            Assert.Equal(targetWidth, scaler._DestWidth);
            Assert.Equal(targetHeight, scaler._DestHeight);
            scaler.SetDestSize(0, 0);
        }

        [Fact()]
        public void SetDestSizeTest()
        {
            PixelMapScaler scaler = new PixelMapScaler();
            scaler._HCoord = new int[1];
            scaler._VCoord = new int[1];

            Assert.Null(scaler._PixelMap1);
            Assert.Null(scaler._PixelMap2);
            Assert.NotNull(scaler._HCoord);
            Assert.NotNull(scaler._VCoord);
            Assert.Equal(-1, scaler._L1);
            Assert.Equal(-1, scaler._L2);
            Assert.Equal(0, scaler._DestWidth);
            Assert.Equal(0, scaler._DestHeight);
            Assert.Equal(0, scaler._SrcWidth);
            Assert.Equal(0, scaler._SrcHeight);

            scaler.SetDestSize(15, 17);
            Assert.Equal(15, scaler._DestWidth);
            Assert.Equal(17, scaler._DestHeight);
            Assert.Null(scaler._HCoord);
            Assert.Null(scaler._VCoord);
            Assert.Equal(0, scaler._SrcWidth);
            Assert.Equal(0, scaler._SrcHeight);
        }

        [Fact()]
        public void SetSrcSizeTest()
        {
            PixelMapScaler scaler = new PixelMapScaler();
            scaler._HCoord = new int[1];
            scaler._VCoord = new int[1];

            Assert.Null(scaler._PixelMap1);
            Assert.Null(scaler._PixelMap2);
            Assert.NotNull(scaler._HCoord);
            Assert.NotNull(scaler._VCoord);
            Assert.Equal(-1, scaler._L1);
            Assert.Equal(-1, scaler._L2);
            Assert.Equal(0, scaler._DestWidth);
            Assert.Equal(0, scaler._DestHeight);
            Assert.Equal(0, scaler._SrcWidth);
            Assert.Equal(0, scaler._SrcHeight);

            scaler.SetSrcSize(15, 17);
            Assert.Equal(0, scaler._DestWidth);
            Assert.Equal(0, scaler._DestHeight);
            Assert.Null(scaler._HCoord);
            Assert.Null(scaler._VCoord);
            Assert.Equal(15, scaler._SrcWidth);
            Assert.Equal(17, scaler._SrcHeight);
        }

        [Fact()]
        public void SetHorzRatioTest001()
        {
            int srcWidth = 0;
            int srcHeight = 17;
            int targetWidth = 18;
            int targetHeight = 19;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Assert.Throws<DjvuInvalidOperationException>(() => sclr.SetHorzRatio(1, 1));
        }

        [Fact()]
        public void SetHorzRatioTest002()
        {
            int srcWidth = 16;
            int srcHeight = 0;
            int targetWidth = 18;
            int targetHeight = 19;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Assert.Throws<DjvuInvalidOperationException>(() => sclr.SetHorzRatio(1, 1));
        }

        [Fact()]
        public void SetHorzRatioTest003()
        {
            int srcWidth = 16;
            int srcHeight = 17;
            int targetWidth = 0;
            int targetHeight = 19;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Assert.Throws<DjvuInvalidOperationException>(() => sclr.SetHorzRatio(1, 1));
        }

        [Fact()]
        public void SetHorzRatioTest004()
        {
            int srcWidth = 16;
            int srcHeight = 17;
            int targetWidth = 18;
            int targetHeight = 0;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Assert.Throws<DjvuInvalidOperationException>(() => sclr.SetHorzRatio(1, 1));
        }

        [Fact()]
        public void SetHorzRatioTest005()
        {
            int srcWidth = 16;
            int srcHeight = 17;
            int targetWidth = 18;
            int targetHeight = 19;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Assert.Throws<DjvuArgumentOutOfRangeException>(() => sclr.SetHorzRatio(0, 9));
        }

        [Fact()]
        public void SetHorzRatioTest006()
        {
            int srcWidth = 16;
            int srcHeight = 17;
            int targetWidth = 18;
            int targetHeight = 19;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Assert.Throws<DjvuArgumentOutOfRangeException>(() => sclr.SetHorzRatio(16, 0));
        }

        [Fact()]
        public void SetHorzRatioTest007()
        {
            int srcWidth = 1280;
            int srcHeight = 720;
            int targetWidth = 1980;
            int targetHeight = 1080;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            sclr.SetHorzRatio(1280, 1980);
        }

        [Fact()]
        public void SetVertRatioTest001()
        {
            int srcWidth = 0;
            int srcHeight = 17;
            int targetWidth = 18;
            int targetHeight = 19;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Assert.Throws<DjvuInvalidOperationException>(() => sclr.SetVertRatio(1, 1));
        }

        [Fact()]
        public void SetVertRatioTest002()
        {
            int srcWidth = 16;
            int srcHeight = 0;
            int targetWidth = 18;
            int targetHeight = 19;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Assert.Throws<DjvuInvalidOperationException>(() => sclr.SetVertRatio(1, 1));
        }

        [Fact()]
        public void SetVertRatioTest003()
        {
            int srcWidth = 16;
            int srcHeight = 17;
            int targetWidth = 0;
            int targetHeight = 19;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Assert.Throws<DjvuInvalidOperationException>(() => sclr.SetVertRatio(1, 1));
        }

        [Fact()]
        public void SetVertRatioTest004()
        {
            int srcWidth = 16;
            int srcHeight = 17;
            int targetWidth = 18;
            int targetHeight = 0;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Assert.Throws<DjvuInvalidOperationException>(() => sclr.SetVertRatio(1, 1));
        }

        [Fact()]
        public void SetVertRatioTest005()
        {
            int srcWidth = 16;
            int srcHeight = 17;
            int targetWidth = 18;
            int targetHeight = 19;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Assert.Throws<DjvuArgumentOutOfRangeException>(() => sclr.SetVertRatio(0, 9));
        }

        [Fact()]
        public void SetVertRatioTest006()
        {
            int srcWidth = 16;
            int srcHeight = 17;
            int targetWidth = 18;
            int targetHeight = 19;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Assert.Throws<DjvuArgumentOutOfRangeException>(() => sclr.SetVertRatio(16, 0));
        }

        [Fact()]
        public void SetVertRatioTest007()
        {
            int srcWidth = 1280;
            int srcHeight = 720;
            int targetWidth = 1980;
            int targetHeight = 1080;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            sclr.SetVertRatio(720, 1080);
        }

        [Fact()]
        public void CreateRectanglesTest001()
        {
            int srcWidth = 1280;
            int srcHeight = 720;
            int targetWidth = 1980;
            int targetHeight = 1080;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle des = new Rectangle
            {
                XMin = -1,
                XMax = 1980,
                YMin = 0,
                YMax = 1080
            };

            Rectangle red = new Rectangle
            {
                
            };

            Assert.Throws<DjvuArgumentOutOfRangeException>(() => sclr.CreateRectangles(des, red));
        }

        [Fact()]
        public void CreateRectanglesTest002()
        {
            int srcWidth = 1280;
            int srcHeight = 720;
            int targetWidth = 1980;
            int targetHeight = 1080;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle des = new Rectangle
            {
                XMin = 0,
                XMax = 1981,
                YMin = 0,
                YMax = 1080
            };

            Rectangle red = new Rectangle
            {

            };

            Assert.Throws<DjvuArgumentOutOfRangeException>(() => sclr.CreateRectangles(des, red));
        }

        [Fact()]
        public void CreateRectanglesTest003()
        {
            int srcWidth = 1280;
            int srcHeight = 720;
            int targetWidth = 1980;
            int targetHeight = 1080;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle des = new Rectangle
            {
                XMin = 0,
                XMax = 1980,
                YMin = -1,
                YMax = 1080
            };

            Rectangle red = new Rectangle
            {

            };

            Assert.Throws<DjvuArgumentOutOfRangeException>(() => sclr.CreateRectangles(des, red));
        }

        [Fact()]
        public void CreateRectanglesTest004()
        {
            int srcWidth = 1280;
            int srcHeight = 720;
            int targetWidth = 1980;
            int targetHeight = 1080;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle des = new Rectangle
            {
                XMin = 0,
                XMax = 1980,
                YMin = 0,
                YMax = 1081
            };

            Rectangle red = new Rectangle
            {

            };

            Assert.Throws<DjvuArgumentOutOfRangeException>(() => sclr.CreateRectangles(des, red));
        }

        [Fact()]
        public void CreateRectanglesTest005()
        {
            int srcWidth = 1280;
            int srcHeight = 720;
            int targetWidth = 1980;
            int targetHeight = 1080;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle des = new Rectangle
            {
                XMin = 0,
                XMax = 1980,
                YMin = 0,
                YMax = 1080
            };

            Rectangle red = new Rectangle
            {
            };

            sclr.CreateRectangles(des, red);
        }

        [Fact()]
        public void CreateRectanglesTest006()
        {
            int srcWidth = 1280;
            int srcHeight = 720;
            int targetWidth = 1980;
            int targetHeight = 1080;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle des = new Rectangle
            {
                XMin = 0,
                XMax = 1980,
                YMin = 0,
                YMax = 1080
            };

            Rectangle red = new Rectangle
            {
                XMin = 0,
                XMax = 1980,
                YMin = 0,
                YMax = 1080
            };

            sclr.CreateRectangles(des, red);
        }

        [Fact()]
        public void ScaleTest001()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 32;
            int targetHeight = 18;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = 47,
                Bottom = 0,
                Top = 27
            };

            Rectangle targetRect = new Rectangle
            {
                Right = 32,
                Left = 0,
                Bottom = 0,
                Top = 18
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            Assert.Throws<DjvuArgumentException>(() => sclr.Scale(srcRect, srcMap, targetRect, destMap));
        }

        [Fact()]
        public void ScaleTest002()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 32;
            int targetHeight = 18;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = 48,
                Bottom = 0,
                Top = 23
            };

            Rectangle targetRect = new Rectangle
            {
                Right = 0,
                Left = 32,
                Bottom = 0,
                Top = 18
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            Assert.Throws<DjvuArgumentException>(() => sclr.Scale(srcRect, srcMap, targetRect, destMap));
        }

        [Fact()]
        public void ScaleTest003()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 32;
            int targetHeight = 18;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = 48,
                Bottom = 0,
                Top = 27
            };

            srcRect.XMin += 1;

            Rectangle targetRect = new Rectangle
            {
                Right = 0,
                Left = 32,
                Bottom = 0,
                Top = 18
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            Assert.Throws<DjvuArgumentException>(() => sclr.Scale(srcRect, srcMap, targetRect, destMap));
        }

        [Fact()]
        public void ScaleTest004()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 32;
            int targetHeight = 18;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = 48,
                Bottom = 0,
                Top = 27
            };

            srcRect.YMin += 1;

            Rectangle targetRect = new Rectangle
            {
                Right = 0,
                Left = 32,
                Bottom = 0,
                Top = 18
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            Assert.Throws<DjvuArgumentException>(() => sclr.Scale(srcRect, srcMap, targetRect, destMap));
        }

        [Fact()]
        public void ScaleTest005()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 32;
            int targetHeight = 18;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = 48,
                Bottom = 0,
                Top = 27
            };

            srcRect.XMax -= 1;

            Rectangle targetRect = new Rectangle
            {
                Right = 0,
                Left = 32,
                Bottom = 0,
                Top = 18
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            Assert.Throws<DjvuArgumentException>(() => sclr.Scale(srcRect, srcMap, targetRect, destMap));
        }

        [Fact()]
        public void ScaleTest006()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 32;
            int targetHeight = 18;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = 48,
                Bottom = 0,
                Top = 27
            };

            srcRect.YMax -= 1;

            Rectangle targetRect = new Rectangle
            {
                Right = 0,
                Left = 32,
                Bottom = 0,
                Top = 18
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            Assert.Throws<DjvuArgumentException>(() => sclr.Scale(srcRect, srcMap, targetRect, destMap));
        }

        [Fact()]
        public void ScaleTest007()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 32;
            int targetHeight = 18;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = 48,
                Bottom = 0,
                Top = 27
            };

            Rectangle targetRect = new Rectangle
            {
                Right = 0,
                Left = 32,
                Bottom = 0,
                Top = 18
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            sclr.SetHorzRatio(32, 48);
            sclr.SetVertRatio(18, 27);

            sclr.Scale(srcRect, srcMap, targetRect, destMap);
        }

        [Fact()]
        public void ScaleTest008()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 16;
            int targetHeight = 9;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = srcWidth,
                Bottom = 0,
                Top = srcHeight
            };

            Rectangle targetRect = new Rectangle
            {
                Right = 0,
                Left = targetWidth,
                Bottom = 0,
                Top = targetHeight
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            sclr.SetHorzRatio(targetWidth, srcWidth);
            sclr.SetVertRatio(targetHeight, srcHeight);

            sclr.Scale(srcRect, srcMap, targetRect, destMap);
        }

        [Fact()]
        public void ScaleTest009()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 16;
            int targetHeight = 9;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 1,
                Left = srcWidth + 1,
                Bottom = 0,
                Top = srcHeight
            };

            Rectangle targetRect = new Rectangle
            {
                Right = 0,
                Left = targetWidth,
                Bottom = 0,
                Top = targetHeight
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            Assert.Throws<DjvuArgumentException>(() => sclr.Scale(srcRect, srcMap, targetRect, destMap));
        }

        [Fact()]
        public void ScaleTest010()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 16;
            int targetHeight = 9;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = srcWidth,
                Bottom = 1,
                Top = srcHeight + 1
            };

            Rectangle targetRect = new Rectangle
            {
                Right = 0,
                Left = targetWidth,
                Bottom = 0,
                Top = targetHeight
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            Assert.Throws<DjvuArgumentException>(() => sclr.Scale(srcRect, srcMap, targetRect, destMap));
        }

        [Fact()]
        public void ScaleTest011()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 16;
            int targetHeight = 9;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = -1,
                Left = srcWidth - 1,
                Bottom = 0,
                Top = srcHeight
            };

            Rectangle targetRect = new Rectangle
            {
                Right = 0,
                Left = targetWidth,
                Bottom = 0,
                Top = targetHeight
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            Assert.Throws<DjvuArgumentException>(() => sclr.Scale(srcRect, srcMap, targetRect, destMap));
        }

        [Fact()]
        public void ScaleTest012()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 16;
            int targetHeight = 9;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = srcWidth,
                Bottom = -1,
                Top = srcHeight - 1
            };

            Rectangle targetRect = new Rectangle
            {
                Right = 0,
                Left = targetWidth,
                Bottom = 0,
                Top = targetHeight
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            Assert.Throws<DjvuArgumentException>(() => sclr.Scale(srcRect, srcMap, targetRect, destMap));
        }

        [Fact()]
        public void GetLineTest001()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 16;
            int targetHeight = 9;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = srcWidth,
                Bottom = 0,
                Top = srcHeight
            };

            Rectangle redRect = new Rectangle
            {
                Right = 0,
                Left = targetWidth,
                Bottom = 0,
                Top = targetHeight
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            sclr._PixelMap1 = srcMap;
            sclr._PixelMap2 = destMap;

            int fy = -1;

            var line = sclr.GetLine(fy, redRect, srcRect, srcMap);
            Assert.NotNull(line);
        }

        [Fact()]
        public void GetLineTest002()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 16;
            int targetHeight = 9;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = srcWidth,
                Bottom = 0,
                Top = srcHeight
            };

            Rectangle redRect = new Rectangle
            {
                Right = 0,
                Left = targetWidth,
                Bottom = 0,
                Top = targetHeight
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            sclr._PixelMap1 = srcMap;
            sclr._PixelMap2 = destMap;

            int fy = 128;

            var line = sclr.GetLine(fy, redRect, srcRect, srcMap);
            Assert.NotNull(line);
        }

        [Fact()]
        public void GetLineTest003()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 16;
            int targetHeight = 9;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = srcWidth,
                Bottom = 0,
                Top = srcHeight
            };

            Rectangle redRect = new Rectangle
            {
                Right = 0,
                Left = targetWidth,
                Bottom = 0,
                Top = targetHeight
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            sclr._PixelMap1 = srcMap;
            sclr._PixelMap2 = destMap;

            int fy = sclr._L1;

            var line = sclr.GetLine(fy, redRect, srcRect, srcMap);
            Assert.NotNull(line);
        }

        [Fact()]
        public void GetLineTest004()
        {
            int srcWidth = 48;
            int srcHeight = 27;
            int targetWidth = 16;
            int targetHeight = 9;

            var sclr = CreateVerifyPixelMapScaler(srcWidth, srcHeight, targetWidth, targetHeight);

            Rectangle srcRect = new Rectangle
            {
                Right = 0,
                Left = srcWidth,
                Bottom = 0,
                Top = srcHeight
            };

            Rectangle redRect = new Rectangle
            {
                Right = 0,
                Left = targetWidth,
                Bottom = 0,
                Top = targetHeight
            };

            IPixelMap srcMap = PixelMapTests.CreateInitVerifyPixelMap(srcWidth, srcHeight, Pixel.RedPixel);
            IPixelMap destMap = PixelMapTests.CreateInitVerifyPixelMap(targetWidth, targetHeight, Pixel.WhitePixel);

            sclr._PixelMap1 = srcMap;
            sclr._PixelMap2 = destMap;

            int fy = sclr._L2;

            var line = sclr.GetLine(fy, redRect, srcRect, srcMap);
            Assert.NotNull(line);
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetRequiredRectTest()
        {
            Assert.True(false, "This test needs an implementation");
        }


    }
}