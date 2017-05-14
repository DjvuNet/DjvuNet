using Xunit;
using DjvuNet.Wavelet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Graphics.Tests;
using DjvuNet.Graphics;
using System.IO;
using DjvuNet.Tests;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using DjvuNet.Errors;
using DjvuNet.DataChunks;

namespace DjvuNet.Wavelet.Tests
{
    public class InterWavePixelMapEncoderTests
    {

        public static IPixelMap PixelMapFromBitmap(System.Drawing.Bitmap bmp)
        {
            var pixMap = PixelMapTests.CreateInitVerifyPixelMap(bmp.Width, bmp.Height, Pixel.WhitePixel);
            BitmapData data = null;

            try
            {
                data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                unsafe
                {
                    fixed (sbyte* dest = pixMap.Data)
                        NativeMethods.MoveMemory(dest, (void*)data.Scan0, pixMap.Data.Length);
                }
            }
            catch(Exception ex)
            {
                throw new DjvuAggregateException(
                    $"Error with bitmap. Width: {bmp.Width}, Height: {bmp.Height}, PixelFormat: {bmp.PixelFormat}", ex);
            }
            finally
            {
                if (data != null)
                    bmp.UnlockBits(data);
            }
            return pixMap;
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void EncodeChunkTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void EncodeImageTest001()
        {
            string file = Path.Combine(Util.ArtifactsPath, "nasa001B.jpg");
            string outFile = Path.Combine(Util.ArtifactsDataPath, "nasa001B.jpg.djvu");
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(file))
            {
                int width = bmp.Width;
                int height = bmp.Height;
                var pixMap = PixelMapFromBitmap(bmp);

                var map = new InterWavePixelMapEncoder();
                map._CrCbDelay = 0;
                TestVerifyEncoderInitialization(pixMap, map);

                int nchunks = 4;
                int[] slices = new int[] { 74, 15, 11, 7 };

                InterWaveEncoderSettings[] settings = new InterWaveEncoderSettings[nchunks];
                for (int i = 0; i < nchunks; i++)
                {
                    settings[i] = new InterWaveEncoderSettings
                    {
                        Slices = slices[i]
                    };
                }

                DjvuFormElement form = null;
                using (MemoryStream stream = new MemoryStream())
                using (IDjvuWriter writer = new DjvuWriter(stream))
                    form = map.EncodeImage(writer, nchunks, settings);

                using (IDjvuWriter writer = new DjvuWriter(outFile))
                    form.WriteData(writer);
            }
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void CloseEncoderTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        private static void TestVerifyEncoderInitialization(IPixelMap pixMap, InterWavePixelMapEncoder map)
        {
            Assert.Null(map._YEncoder);
            Assert.Null(map._YMap);
            Assert.Null(map._CbEncoder);
            Assert.Null(map._CbMap);
            Assert.Null(map._CrEncoder);
            Assert.Null(map._CrMap);

            map.InitializeEncoder(pixMap);

            Assert.Null(map._YEncoder);
            Assert.NotNull(map._YMap);
            Assert.Null(map._CbEncoder);
            Assert.NotNull(map._CbMap);
            Assert.Null(map._CrEncoder);
            Assert.NotNull(map._CrMap);
        }

        [Fact()]
        public void InitializeEncoderTest001()
        {
            string file = Path.Combine(Util.ArtifactsPath, "nasa001B.thmb.jpg");
            using(System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(file))
            {
                int width = bmp.Width;
                int height = bmp.Height;
                var pixMap = PixelMapFromBitmap(bmp);

                var map = new InterWavePixelMapEncoder();
                TestVerifyEncoderInitialization(pixMap, map);
            }
        }

        [Fact()]
        public void InitializeEncoderTest002()
        {
            string file = Path.Combine(Util.ArtifactsPath, "nasa001B.jpg");
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(file))
            {
                int width = bmp.Width;
                int height = bmp.Height;
                var pixMap = PixelMapFromBitmap(bmp);

                var map = new InterWavePixelMapEncoder();
                TestVerifyEncoderInitialization(pixMap, map);
            }
        }

        [Fact()]
        public void InitializeEncoderTest003()
        {
            string file = Path.Combine(Util.ArtifactsPath, "metr001B.jpg");
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(file))
            {
                int width = bmp.Width;
                int height = bmp.Height;
                var pixMap = PixelMapFromBitmap(bmp);

                var map = new InterWavePixelMapEncoder();
                TestVerifyEncoderInitialization(pixMap, map);
            }
        }
    }
}