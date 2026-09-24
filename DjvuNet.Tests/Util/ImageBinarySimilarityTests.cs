using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using Xunit;
using SysBitmap = System.Drawing.Bitmap;
using DjvuBitmap = DjvuNet.Graphics.Bitmap;

namespace DjvuNet.Tests
{
    public class ImageBinarySimilarityTests
    {
        private static SysBitmap CreateTestBitmap(int width, int height, int stride, PixelFormat format, IntPtr scan0)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DjvuExceptionUtil.ThrowNotSupported("Unmanaged scan0 Bitmap instantiation is fundamentally broken in upstream libgdiplus (CWE-125 and pixel bounds bugs).");
            }
            return new SysBitmap(width, height, stride, format, scan0);
        }

        [Theory]
        [InlineData(128, 128, -384)] // Negative Stride (Bottom-Up memory)
        [InlineData(128, 128, 384)]  // Positive Stride (Top-Down memory)
        public void SysBitmap_PixelMap(int width, int height, int stride)
        {
            int absStride = Math.Abs(stride);
            int imageBytes = absStride * height;
            
            // Allocate 3x the required buffer to create safe OOB canary zones
            byte[] backingBuffer = new byte[imageBytes * 3];
            
            // Fill entire buffer with canary values (max diff)
            Array.Fill(backingBuffer, (byte)255);
            
            // Zero out the actual active image region (the middle chunk)
            int imageOffset = imageBytes;
            Array.Fill(backingBuffer, (byte)0, imageOffset, imageBytes);
            
            // Create a matching zeroed-out Cartesian PixelMap
            var map = new PixelMap();
            map.Init(height, width, Pixel.BlackPixel);
            
            unsafe
            {
                fixed (byte* pBuffer = backingBuffer)
                {
                    // Scan0 must point to the visual Top row.
                    // For negative stride, the Top row is located at the highest valid address in the chunk.
                    byte* scan0 = pBuffer + imageOffset;
                    if (stride < 0) 
                    {
                        scan0 += imageBytes - absStride; 
                    }
                    
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Assert.Throws<DjvuNotSupportedException>(() => CreateTestBitmap(width, height, stride, PixelFormat.Format24bppRgb, (IntPtr)scan0));
                        return;
                    }

                    using (var sysBitmap = CreateTestBitmap(width, height, stride, PixelFormat.Format24bppRgb, (IntPtr)scan0))
                    {
                        // If OOB occurs, it reads 255 canary bytes instead of 0 image bytes, failing the similarity check.
                        bool isSimilar = Util.ImageBinarySimilarity(sysBitmap, map, 0.0);
                        
                        if (!isSimilar)
                        {
                            StringBuilder sb = new StringBuilder(1024);
                            fixed (sbyte* pMap = map.Data)
                            {
                                // Provide diagnostic dump of the exact canary memory read
                                Util.DumpImageMismatchCore(
                                    scan0, (byte*)pMap,
                                    width, height,
                                    stride, map.Width * 3,
                                    0, 0,
                                    PixelSize._24bpp, ChannelSize._8bit, sb);
                            }
                            
                            Assert.True(isSimilar, "ImageBinarySimilarity failed. The engine read out-of-bounds canary data.\n" + sb.ToString());
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData(128, 128, -128)]
        [InlineData(128, 128, 128)]
        public void SysBitmap_Bitmap(int width, int height, int stride)
        {
            int absStride = Math.Abs(stride);
            int imageBytes = absStride * height;
            
            byte[] backingBuffer = new byte[imageBytes * 3];
            Array.Fill(backingBuffer, (byte)255);
            
            int imageOffset = imageBytes;
            Array.Fill(backingBuffer, (byte)0, imageOffset, imageBytes);
            
            int border = 2;
            var map = new DjvuBitmap();
            map.Init(height, width, border); 
            
            unsafe
            {
                fixed (byte* pBuffer = backingBuffer)
                {
                    byte* scan0 = pBuffer + imageOffset;
                    if (stride < 0) 
                    {
                        scan0 += imageBytes - absStride; 
                    }
                    
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Assert.Throws<DjvuNotSupportedException>(() => CreateTestBitmap(width, height, stride, PixelFormat.Format8bppIndexed, (IntPtr)scan0));
                        return;
                    }

                    using (var sysBitmap = CreateTestBitmap(width, height, stride, PixelFormat.Format8bppIndexed, (IntPtr)scan0))
                    {
                        bool isSimilar = Util.ImageBinarySimilarity(sysBitmap, ref map, 0.0);
                        
                        if (!isSimilar)
                        {
                            StringBuilder sb = new StringBuilder(1024);
                            byte* pMap = (byte*)map.GetRow(0);
                            
                            Util.DumpImageMismatchCore(
                                scan0, pMap,
                                width, height,
                                stride, map.BytesPerRow, 
                                0, 0,
                                PixelSize._8bpp, ChannelSize._8bit, sb);
                                
                            Assert.True(isSimilar, "ImageBinarySimilarity failed. The engine read out-of-bounds canary data.\n" + sb.ToString());
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData(128, 128, 384, 384, false)] // Happy path
        [InlineData(128, 128, -384, 384, false)] // Mixed layouts
        [InlineData(128, 128, 384, -384, false)]
        [InlineData(128, 128, -384, -384, false)] // Both bottom-up
        [InlineData(128, 128, 384, 384, true)]  // Failure path (injected diff)
        [InlineData(128, 128, -384, 384, true)]
        public void SysBitmap_SysBitmap(int width, int height, int stride1, int stride2, bool injectDiff)
        {
            int absStride1 = Math.Abs(stride1);
            int absStride2 = Math.Abs(stride2);
            int imageBytes1 = absStride1 * height;
            int imageBytes2 = absStride2 * height;
            
            byte[] backing1 = new byte[imageBytes1 * 3];
            byte[] backing2 = new byte[imageBytes2 * 3];
            
            Array.Fill(backing1, (byte)255);
            Array.Fill(backing2, (byte)255);
            
            Array.Fill(backing1, (byte)0, imageBytes1, imageBytes1);
            Array.Fill(backing2, (byte)0, imageBytes2, imageBytes2);
            
            if (injectDiff)
            {
                // Inject a difference inside the active image area
                backing2[imageBytes2 + (height / 2 * absStride2) + (width / 2)] = 128;
            }
            
            unsafe
            {
                fixed (byte* p1 = backing1) 
                fixed (byte* p2 = backing2)
                {
                    byte* scan1 = p1 + imageBytes1;
                    if (stride1 < 0) scan1 += imageBytes1 - absStride1;
                    
                    byte* scan2 = p2 + imageBytes2;
                    if (stride2 < 0) scan2 += imageBytes2 - absStride2;
                    
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Assert.Throws<DjvuNotSupportedException>(() => 
                        {
                            using (var bmp1 = CreateTestBitmap(width, height, stride1, PixelFormat.Format24bppRgb, (IntPtr)scan1))
                            using (var bmp2 = CreateTestBitmap(width, height, stride2, PixelFormat.Format24bppRgb, (IntPtr)scan2))
                            {
                            }
                        });
                        return;
                    }

                    using (var bmp1 = CreateTestBitmap(width, height, stride1, PixelFormat.Format24bppRgb, (IntPtr)scan1))
                    using (var bmp2 = CreateTestBitmap(width, height, stride2, PixelFormat.Format24bppRgb, (IntPtr)scan2))
                    {
                        bool isSimilar = Util.ImageBinarySimilarity(bmp1, bmp2, 0.0);
                        if (injectDiff) Assert.False(isSimilar, "Expected to fail due to injected difference.");
                        else Assert.True(isSimilar, "SysBitmap_SysBitmap comparison read OOB canary data.");
                    }
                }
            }
        }

        [Fact]
        public void SysBitmap_SysBitmap_FormatMismatch()
        {
            using (var bmp1 = new SysBitmap(128, 128, PixelFormat.Format24bppRgb))
            using (var bmp2 = new SysBitmap(64, 64, PixelFormat.Format24bppRgb))
            {
                Assert.False(Util.ImageBinarySimilarity(bmp1, bmp2, 0.0));
            }
        }

        [Theory]
        [InlineData(128, 128, 0, false)] // Happy path, 0 border
        [InlineData(128, 128, 2, false)] // Happy path, 2 padding border
        [InlineData(128, 128, 0, true)]  // Failure path (injected diff)
        [InlineData(128, 128, 2, true)]
        public void DjvuBitmap_DjvuBitmap(int width, int height, int border, bool injectDiff)
        {
            var map1 = new DjvuBitmap(); map1.Init(height, width, border);
            var map2 = new DjvuBitmap(); map2.Init(height, width, border);
            
            if (injectDiff) map2.Data[map2.RowOffset(height / 2) + (width / 2)] = 127;
            
            var result = Util.ImageBinarySimilarity(ref map1, ref map2, 0.0);
            
            if (injectDiff)
            {
                Assert.False(result.Item1, "Expected to fail due to injected difference.");
                Assert.True(result.Item2 > 0.0, "Difference calculation should be > 0.0");
            }
            else
            {
                Assert.True(result.Item1, "Comparison failed on identical maps.");
                Assert.Equal(0.0, result.Item2);
            }
        }

        [Fact]
        public void DjvuBitmap_DjvuBitmap_FormatMismatch()
        {
            var map1 = new DjvuBitmap(); map1.Init(128, 128, 0);
            var map2 = new DjvuBitmap(); map2.Init(64, 64, 0);
            
            var result = Util.ImageBinarySimilarity(ref map1, ref map2, 0.0);
            Assert.False(result.Item1);
            Assert.True(double.IsNaN(result.Item2)); // Expected behavior on mismatch
        }
        [Fact]
        public void ImageBinarySimilarity_NegativeStride_ThrowsOnNonWindows()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Skip("Test is strictly for verifying non-Windows exception behavior.");
            }

            // Mock BitmapData to directly test the engine guard without libgdiplus interference
            var badData = new BitmapData { Stride = -384, Width = 128, Height = 128, PixelFormat = PixelFormat.Format24bppRgb };
            var goodData = new BitmapData { Stride = 384, Width = 128, Height = 128, PixelFormat = PixelFormat.Format24bppRgb };

            Assert.Throws<DjvuNotSupportedException>(() => 
                Util.ImageBinarySimilarity(badData, goodData));
        }
    }
}
