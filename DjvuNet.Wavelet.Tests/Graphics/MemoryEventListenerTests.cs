using System;
using System.IO;
using System.Linq;
using Xunit;
using DjvuNet.Diagnostics;
using DjvuNet.Graphics;
using DjvuNet.Tests;
using DjvuNet.Diagnostics.Tests;

namespace DjvuNet.Graphics.Tests
{
    public class MemoryEventListenerTests
    {
        [Theory]
        [InlineData(100, 100, 0)]
        [InlineData(256, 256, 8)]
        [InlineData(1920, 1080, 0)]
        public void CapturesEtwEvents(int width, int height, int border)
        {
            string csvOutput;
            using (var sw = new StringWriter())
            {
                using (var listener = new MemoryEventListener(sw))
                {
                    // 1. Initialize Bitmap
                    var bmp = new Bitmap();
                    bmp.Init(height, width, border); // Triggers OnBitmapDataAllocated
                    
                    for(int i = 0; i < bmp.Data.Length; i++) 
                        bmp.Data[i] = (sbyte)(i % 2);

                    // 2. Compress the Bitmap
                    bmp.Compress(); // Triggers multiple OnRleDataAllocated

                    // 3. Decompress the Bitmap
                    bmp.Decompress(); // Triggers OnBitmapDataAllocated inside DecodeRle

                    // 4. Clone
                    var cloned = bmp.Duplicate(); // Triggers OnRleDataAllocated for clone
                }
                csvOutput = sw.ToString();
            }

            //Console.WriteLine("================ ETW TELEMETRY DUMP ================");
            //Console.WriteLine(csvOutput);
            //Console.WriteLine("====================================================");

            string[] lines = csvOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            
            Assert.True(lines.Length >= 4, "CSV should contain a header and multiple allocation events.");
            Assert.Equal("Type,Width,Height,SizeBytes,Border,Uninitialized,CallSite", lines[0]);
            
            long expectedStride = width + border;
            long expectedPixels = height * expectedStride + border;
            
            Assert.Contains(lines, l => l.StartsWith($"Bitmap,{width},{height},{expectedPixels},{border},"));
            Assert.Contains(lines, l => l.StartsWith("RleData,0,0,") && !l.Contains("RleData,0,0,0,"));
            
            var bitmapEvents = lines.Where(l => l.StartsWith("Bitmap,")).ToList();
            Assert.True(bitmapEvents.Count >= 2, "There should be at least two Bitmap allocations (Init and Decompress)");
        }

        //[Theory]
        //[InlineData("test023Cmask.png")]
        //[InlineData("test075Cmask.png")]
        //public unsafe void CapturesEtwEvents_RealWorldMasks(string maskName)
        //{
        //    string csvOutput;
        //    string path = Path.Combine(Util.RepoRoot, "artifacts", "data", maskName);

        //    using (var sw = new StringWriter())
        //    {
        //        using (var listener = new MemoryEtwListener(sw))
        //        {
        //            using (var img = new System.Drawing.Bitmap(path))
        //            {
        //                var bmp = new Bitmap();
        //                bmp.Init(img.Height, img.Width, 0);

        //                System.Drawing.Imaging.BitmapData bmpData = img.LockBits(
        //                    new System.Drawing.Rectangle(0, 0, img.Width, img.Height),
        //                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
        //                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        //                try
        //                {
        //                    byte* pSrcBase = (byte*)bmpData.Scan0;
        //                    int stride = bmpData.Stride;
        //                    int i = 0;
        //                    for (int y = 0; y < img.Height; y++)
        //                    {
        //                        byte* pRow = pSrcBase + (y * stride);
        //                        for (int x = 0; x < img.Width; x++)
        //                        {
        //                            bmp.Data[i++] = (pRow[x * 4 + 2] < 128) ? (sbyte)1 : (sbyte)0;
        //                        }
        //                    }
        //                }
        //                finally
        //                {
        //                    img.UnlockBits(bmpData);
        //                }

        //                // Profile compression!
        //                bmp.Compress();
        //                bmp.Decompress();
        //            }
        //        }
        //        csvOutput = sw.ToString();
        //    }

        //    //Console.WriteLine($"================ ETW PROFILE: {maskName} ================");
        //    //Console.WriteLine(csvOutput);
        //    //Console.WriteLine("=========================================================");
        //}
    }
}
