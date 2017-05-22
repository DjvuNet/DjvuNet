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
using DjvuNet.Tests.Xunit;

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

        public static IEnumerable<object[]> EncodeImageTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { "nasa001C.png" });
                retVal.Add(new object[] { "nasa002C.png" });
                retVal.Add(new object[] { "nasa003C.png" });
                retVal.Add(new object[] { "nasa004C.png" });
                retVal.Add(new object[] { "nasa005C.png" });

                return retVal;
            }
        }

        [Fact()]
        public void EncodeChunkTest()
        {
            ;
        }

        [DjvuTheory]
        [MemberData(nameof(EncodeImageTestData))]
        public void EncodeImage_Theory(string fileName)
        {
            string file = Path.Combine(Util.ArtifactsPath, fileName);
            string outFile = Path.Combine(Util.ArtifactsDataPath, fileName + ".djvu");
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(file))
            {
                bmp.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipX);
                int width = bmp.Width;
                int height = bmp.Height;
                var pixMap = PixelMapFromBitmap(bmp);

                var map = new InterWavePixelMapEncoder();
                map.InitializeEncoder(pixMap, null, YCrCbMode.Full);

                int nchunks = 4;
                int[] slices = new int[] { 74, 90, 98, 103 };
                //float[] decibel = new float[] { 5.0f, 10.0f, 15.0f, 20.0f };

                InterWaveEncoderSettings[] settings = new InterWaveEncoderSettings[nchunks];
                if (fileName != "")
                {
                    for (int i = 0; i < nchunks; i++)
                    {
                        settings[i] = new InterWaveEncoderSettings
                        {
                            Slices = slices[i]
                        };
                    }
                }
                //else
                //{
                //    for (int i = 0; i < nchunks; i++)
                //    {
                //        settings[i] = new InterWaveEncoderSettings
                //        {
                //            Decibels = decibel[i]
                //        };
                //    }
                //}

                DjvuFormElement form = null;
                using (MemoryStream stream = new MemoryStream())
                using (IDjvuWriter writer = new DjvuWriter(stream))
                    form = map.EncodeImage(writer, nchunks, settings);

                using (IDjvuWriter writer = new DjvuWriter(outFile))
                    form.WriteData(writer);

                using (DjvuDocument doc = new DjvuDocument(outFile))
                {
                    IDjvuPage page = doc.Pages[0];
                    PM44Form pageForm = (PM44Form) page.PageForm;
                    Assert.Equal(nchunks, pageForm.Children.Count);

                    Assert.NotNull(form);
                    Assert.Equal(nchunks, form.Children.Count);
                    Assert.IsType<PM44Form>(form);
                    for (int i = 0; i < form.Children.Count; i++)
                    {
                        var c = form.Children[i];
                        Assert.NotNull(c);
                        Assert.IsType<PM44Chunk>(c);
                        PM44Chunk chunk = (PM44Chunk)c;
                        Assert.NotNull(chunk.ChunkData);
                        if (chunk.ChunkData.Length >= 2)
                        {
                            Assert.Equal(i, chunk.ChunkData[0]);
                            Assert.Equal(slices[i] - (i == 0 ? 0 : slices[i - 1]), chunk.ChunkData[1]);
                            if (i == 0)
                            {
                                Assert.Equal(1, chunk.ChunkData[2]);
                                Assert.Equal(2, chunk.ChunkData[3]);

                                int widthTest = chunk.ChunkData[4] << 8;
                                widthTest |= chunk.ChunkData[5];
                                Assert.Equal(bmp.Width, widthTest);

                                int heightTest = chunk.ChunkData[6] << 8;
                                heightTest |= chunk.ChunkData[7];
                                Assert.Equal(bmp.Height, heightTest);
                            }
                        }
                    }
                }
            }
        }

        [Fact()]
        public void EncodeImageTest001()
        {
            string file = Path.Combine(Util.ArtifactsPath, "cmhi001C.png");
            string outFile = Path.Combine(Util.ArtifactsDataPath, "cmhi001C.png.djvu");
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(file))
            {
                bmp.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipX);
                int width = bmp.Width;
                int height = bmp.Height;
                var pixMap = PixelMapFromBitmap(bmp);

                var map = new InterWavePixelMapEncoder();
                map._CrCbDelay = 0;
                map.InitializeEncoder(pixMap, null, YCrCbMode.Full);

                int nchunks = 4;
                float[] decibel = new float[] { 25.0f, 25.2f, 25.2647f, 25.7f };

                InterWaveEncoderSettings[] settings = new InterWaveEncoderSettings[nchunks];
                for (int i = 0; i < nchunks; i++)
                {
                    settings[i] = new InterWaveEncoderSettings
                    {
                        Decibels = decibel[i]
                    };
                }

                map._dBFrac = 10.0f;

                DjvuFormElement form = null;
                using (MemoryStream stream = new MemoryStream())
                using (IDjvuWriter writer = new DjvuWriter(stream))
                    form = map.EncodeImage(writer, nchunks, settings);

                using (IDjvuWriter writer = new DjvuWriter(outFile))
                    form.WriteData(writer);

                Assert.NotNull(form);
                Assert.Equal(nchunks, form.Children.Count);
                Assert.IsType<PM44Form>(form);
                for (int i = 0; i < form.Children.Count; i++)
                {
                    var c = form.Children[i];
                    Assert.NotNull(c);
                    Assert.IsType<PM44Chunk>(c);
                    PM44Chunk chunk = (PM44Chunk)c;
                    Assert.NotNull(chunk.ChunkData);
                    if (chunk.ChunkData.Length >= 2)
                    {
                        Assert.Equal(i, chunk.ChunkData[0]);
                        if (i == 0)
                        {
                            Assert.Equal(1, chunk.ChunkData[2]);
                            Assert.Equal(2, chunk.ChunkData[3]);

                            int widthTest = chunk.ChunkData[4] << 8;
                            widthTest |= chunk.ChunkData[5];
                            Assert.Equal(bmp.Width, widthTest);

                            int heightTest = chunk.ChunkData[6] << 8;
                            heightTest |= chunk.ChunkData[7];
                            Assert.Equal(bmp.Height, heightTest);
                        }
                    }
                }



                //using (DjvuDocument doc = new DjvuDocument(outFile))
                //{
                //    IDjvuPage page = doc.Pages[0];
                //    IDjvuElement pageForm = page.PageForm;
                //    Assert.Equal(nchunks, pageForm.Children.Count);
                //}
            }
        }

        [Fact()]
        public void EncodeImageTest002()
        {
            string file = Path.Combine(Util.ArtifactsPath, "cmhi002C.png");
            string outFile = Path.Combine(Util.ArtifactsDataPath, "cmhi002C.png.djvu");
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(file))
            {
                bmp.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipX);
                int width = bmp.Width;
                int height = bmp.Height;
                var pixMap = PixelMapFromBitmap(bmp);

                var map = new InterWavePixelMapEncoder();
                map._CrCbDelay = 0;
                map.InitializeEncoder(pixMap, null, YCrCbMode.Full);

                int nchunks = 4;
                int[] slices = new int[] { 111, 152, 184, 200 };

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

                Assert.NotNull(form);
                Assert.Equal(nchunks, form.Children.Count);
                Assert.IsType<PM44Form>(form);
                for (int i = 0; i < form.Children.Count; i++)
                {
                    var c = form.Children[i];
                    Assert.NotNull(c);
                    Assert.IsType<PM44Chunk>(c);
                    PM44Chunk chunk = (PM44Chunk)c;
                    Assert.NotNull(chunk.ChunkData);
                    if (chunk.ChunkData.Length >= 2)
                    {
                        Assert.Equal(i, chunk.ChunkData[0]);
                        Assert.Equal(slices[i] - (i == 0 ? 0 : slices[i - 1]), chunk.ChunkData[1]);
                        if (i == 0)
                        {
                            Assert.Equal(1, chunk.ChunkData[2]);
                            Assert.Equal(2, chunk.ChunkData[3]);

                            int widthTest = chunk.ChunkData[4] << 8;
                            widthTest |= chunk.ChunkData[5];
                            Assert.Equal(bmp.Width, widthTest);

                            int heightTest = chunk.ChunkData[6] << 8;
                            heightTest |= chunk.ChunkData[7];
                            Assert.Equal(bmp.Height, heightTest);
                        }
                    }
                }


                //using (DjvuDocument doc = new DjvuDocument(outFile))
                //{
                //    IDjvuPage page = doc.Pages[0];
                //    IDjvuElement pageForm = page.PageForm;
                //    Assert.Equal(nchunks, pageForm.Children.Count);
                //}
            }
        }

        [Fact()]
        public void EncodeImageTest003()
        {
            string file = Path.Combine(Util.ArtifactsPath, "block001C.png");
            string outFile = Path.Combine(Util.ArtifactsDataPath, "block001C.png.djvu");
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(file))
            {
                bmp.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipX);
                int width = bmp.Width;
                int height = bmp.Height;
                var pixMap = PixelMapFromBitmap(bmp);

                var map = new InterWavePixelMapEncoder();
                map._CrCbDelay = 0;
                map.InitializeEncoder(pixMap, null, YCrCbMode.Full);

                int nchunks = 4;
                int[] slices = new int[] { 111, 152, 184, 200 };

                InterWaveEncoderSettings[] settings = new InterWaveEncoderSettings[nchunks];
                for (int i = 0; i < nchunks; i++)
                {
                    settings[i] = new InterWaveEncoderSettings
                    {
                        Slices = slices[i]
                    };
                }

                map._dBFrac = 10.0f;

                DjvuFormElement form = null;
                using (MemoryStream stream = new MemoryStream())
                using (IDjvuWriter writer = new DjvuWriter(stream))
                    form = map.EncodeImage(writer, nchunks, settings);

                using (IDjvuWriter writer = new DjvuWriter(outFile))
                    form.WriteData(writer);

                Assert.NotNull(form);
                Assert.Equal(nchunks, form.Children.Count);
                Assert.IsType<PM44Form>(form);
                for (int i = 0; i < form.Children.Count; i++)
                {
                    var c = form.Children[i];
                    Assert.NotNull(c);
                    Assert.IsType<PM44Chunk>(c);
                    PM44Chunk chunk = (PM44Chunk)c;
                    Assert.NotNull(chunk.ChunkData);
                    if (chunk.ChunkData.Length >= 2)
                    {
                        Assert.Equal(i, chunk.ChunkData[0]);
                        if (i == 0)
                        {
                            Assert.Equal(1, chunk.ChunkData[2]);
                            Assert.Equal(2, chunk.ChunkData[3]);

                            int widthTest = chunk.ChunkData[4] << 8;
                            widthTest |= chunk.ChunkData[5];
                            Assert.Equal(bmp.Width, widthTest);

                            int heightTest = chunk.ChunkData[6] << 8;
                            heightTest |= chunk.ChunkData[7];
                            Assert.Equal(bmp.Height, heightTest);
                        }
                    }
                }



                //using (DjvuDocument doc = new DjvuDocument(outFile))
                //{
                //    IDjvuPage page = doc.Pages[0];
                //    IDjvuElement pageForm = page.PageForm;
                //    Assert.Equal(nchunks, pageForm.Children.Count);
                //}
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
            string file = Path.Combine(Util.ArtifactsPath, "nasa001C.png");
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
            string file = Path.Combine(Util.ArtifactsPath, "nasa004C.png");
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
            string file = Path.Combine(Util.ArtifactsPath, "block001C.png");
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
        public void InitializeEncoderTest004()
        {
            string file = Path.Combine(Util.ArtifactsPath, "cmhi005C.png");
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