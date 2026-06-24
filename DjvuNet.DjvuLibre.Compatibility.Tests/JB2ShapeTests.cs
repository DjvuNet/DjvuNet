using System;
using System.IO;
using System.Runtime.InteropServices;
using DjvuNet;
using DjvuNet.JB2;
using DjvuNet.DjvuLibre;
using DjvuNet.Graphics;
using Xunit;
using DjvuNet.Tests;
using System.Collections.Generic;

namespace DjvuNet.DjvuLibre.Compatibility.Tests
{
    public class JB2ShapeTests
    {
        [Theory]
        [MemberData(nameof(Util.ForegroundImageSourceDocs), MemberType = typeof(Util))]
        public void JB2ShapeDictionary_Parity_Theory(int index)
        {
            string filePath = Util.GetTestFilePath(index);
            Assert.True(File.Exists(filePath), $"Test file not found: {filePath}");

            // 1. Load Managed Document
            using (DjvuDocument doc = Util.GetTestDocument(index, out int pageCount))
            // 2. Load Native Document
            using (DjvuDocumentInfo nativeDocInfo = DjvuDocumentInfo.CreateDjvuDocumentInfo(filePath))
            {
                int totalShapes = 0;
                int processedShapes = 0;

                for (int p = 0; p < Math.Min(pageCount, 2); p++)
                {
                    int shapesProcessed = 0;
                    int nativeShapeCount = 0;
                    IDjvuPage managedPage = doc.Pages[p];
                    JB2Image managedJb2 = managedPage.ForegroundJB2Image;

                    // Skip pages without JB2 foreground data
                    if (managedJb2 == null)
                        continue;

                    using (DjvuPageInfo nativePageInfo = new DjvuPageInfo(nativeDocInfo, p))
                    {
                        IntPtr nativePage = nativePageInfo.Page;

                        // Retrieve native shape count
                        bool success = NativeMethods.GetDjvuPageJb2ShapeCount(nativePage, out nativeShapeCount);
                        Assert.True(success, $"Failed to retrieve native JB2 shape count for page {p} in doc {index}");

                        // Parity Check 1: Dictionary Size
                        Assert.Equal(nativeShapeCount, managedJb2.ShapeCount);

                        Dictionary<int, string> shapeDiffs = new(nativeShapeCount);

                        for (int s = 0; s < nativeShapeCount; s++)
                        {
                            // Probe native shape for dimensions
                            success = NativeMethods.GetDjvuPageJb2Shape(nativePage, s, out int nWidth, out int nHeight, out int nRowSize, null);
                            Assert.True(success, $"Failed to probe native shape {s}");

                            JB2Shape managedShape = managedJb2.GetShape(s);
                            Bitmap managedBitmap = (Bitmap)managedShape.Bitmap;

                            // Handle empty shapes (e.g., whitespace blits or structural nodes)
                            if (nWidth == 0 && nHeight == 0)
                            {
                                Assert.Null(managedBitmap);
                                continue;
                            }

                            Assert.NotNull(managedBitmap);

                            // Parity Check 2: Shape Dimensions
                            Assert.Equal(nWidth, managedBitmap.Width);
                            Assert.Equal(nHeight, managedBitmap.Height);
                            Assert.Equal(nRowSize, managedBitmap.BytesPerRow);

                            // Extract native pixels
                            int bufferSize = Util.CalculateBufferSize(nHeight, nRowSize);
                            byte[] nativePixels = new byte[bufferSize];
                            success = NativeMethods.GetDjvuPageJb2Shape(nativePage, s, out _, out _, out _, nativePixels);
                            Assert.True(success, $"Failed to extract native shape pixels {s}");

                            // Extract managed pixels safely (sbyte[] to byte[])
                            byte[] managedPixels = new byte[nHeight * nRowSize];
                            Buffer.BlockCopy(managedBitmap.Data, 4, managedPixels, 0, managedPixels.Length);

                            // Parity Check 3: Absolute Pixel Equality
                            Assert.Equal(nativePixels.Length, managedPixels.Length);

                            unsafe
                            {
                                fixed (byte* pNative = nativePixels)
                                fixed (byte* pManaged = managedPixels)
                                {
                                    // JB2 bitmaps are 1-bit per pixel conceptually, but decoded to bytes. 
                                    // We use 8 bits for pixelSize and channelSize.
                                    double diff = Util.ImageBinaryDiff(
                                        pManaged,
                                        pNative,
                                        nWidth,
                                        nHeight,
                                        nRowSize,
                                        8,
                                        8);

                                    if (diff != 0.0 && shapeDiffs.Count < 100)
                                    {
                                        shapeDiffs.Add(s, $"JB2 Shape Dictionary mismatch on shape {s} out of {nativeShapeCount}, page {p}, doc 23, dimensions w: {nWidth} h: {nHeight}, pixel count {nWidth * nHeight}, Diff: {diff}");
                                        if (shapeDiffs.Count >= 10)
                                            break;
                                    }
                                }
                            }

                            shapesProcessed++;
                        }
                        if (shapeDiffs.Count > 0)
                        {
                            foreach (int key in shapeDiffs.Keys)
                                Console.WriteLine(shapeDiffs[key]);
                        }
                        Assert.True(shapeDiffs.Count == 0);
                    }

                    //Console.WriteLine($"Processed {p + 1} pages out of {pageCount} and processed {shapesProcessed} out of {nativeShapeCount} JB2Shapes");
                    processedShapes += shapesProcessed;
                    totalShapes += nativeShapeCount;
                }

                Console.WriteLine($"Processed {processedShapes} out of total {totalShapes} JB2Shapes");
            }
        }

        [Fact]
        public void JB2ShapeDictionary_Parity_023()
        {
            string filePath = Util.GetTestFilePath(23);
            Assert.True(File.Exists(filePath), $"Test file not found: {filePath}");

            // 1. Load Managed Document test_023C.djvu - PLoS Biology Article
            using (DjvuDocument doc = Util.GetTestDocument(23, out int pageCount))
            // 2. Load Native Document
            using (DjvuDocumentInfo nativeDocInfo = DjvuDocumentInfo.CreateDjvuDocumentInfo(filePath))
            {
                int totalShapes = 0;
                int processedShapes = 0;
                for (int p = 0; p < Math.Min(pageCount, 2); p++)
                {
                    int shapesProcessed = 0;
                    int nativeShapeCount = 0;
                    IDjvuPage managedPage = doc.Pages[p];
                    JB2Image managedJb2 = managedPage.ForegroundJB2Image;

                    // Skip pages without JB2 foreground data
                    if (managedJb2 == null)
                        continue;

                    using (DjvuPageInfo nativePageInfo = new DjvuPageInfo(nativeDocInfo, p))
                    {
                        IntPtr nativePage = nativePageInfo.Page;

                        // Retrieve native shape count
                        bool success = NativeMethods.GetDjvuPageJb2ShapeCount(nativePage, out nativeShapeCount);
                        Assert.True(success, $"Failed to retrieve native JB2 shape count for page {p} in doc 23");

                        // Parity Check 1: Dictionary Size
                        Assert.Equal(nativeShapeCount, managedJb2.ShapeCount);

                        Dictionary<int, string> shapeDiffs = new(nativeShapeCount);

                        for (int s = 0; s < nativeShapeCount; s++)
                        {
                            // Probe native shape for dimensions
                            success = NativeMethods.GetDjvuPageJb2Shape(nativePage, s, out int nWidth, out int nHeight, out int nRowSize, null);
                            Assert.True(success, $"Failed to probe native shape {s}");

                            JB2Shape managedShape = managedJb2.GetShape(s);
                            Bitmap managedBitmap = (Bitmap)managedShape.Bitmap;

                            // Handle empty shapes (e.g., whitespace blits or structural nodes)
                            if (nWidth == 0 && nHeight == 0)
                            {
                                Assert.Null(managedBitmap);
                                continue;
                            }

                            Assert.NotNull(managedBitmap);

                            // Parity Check 2: Shape Dimensions
                            Assert.Equal(nWidth, managedBitmap.Width);
                            Assert.Equal(nHeight, managedBitmap.Height);
                            Assert.Equal(nRowSize, managedBitmap.BytesPerRow);

                            // Extract native pixels
                            int bufferSize = Util.CalculateBufferSize(nHeight, nRowSize);
                            byte[] nativePixels = new byte[bufferSize];
                            success = NativeMethods.GetDjvuPageJb2Shape(nativePage, s, out _, out _, out _, nativePixels);
                            Assert.True(success, $"Failed to extract native shape pixels {s}");

                            // Extract managed pixels safely (sbyte[] to byte[])
                            byte[] managedPixels = new byte[nHeight * nRowSize];
                            Buffer.BlockCopy(managedBitmap.Data, 4, managedPixels, 0, managedPixels.Length);

                            unsafe
                            {
                                fixed (byte* pNative = nativePixels)
                                fixed (byte* pManaged = managedPixels)
                                {
                                    byte* pNativePtr = pNative;
                                    byte* pManagedPtr = pManaged;

                                    // JB2 bitmaps are 1-bit per pixel conceptually, but decoded to bytes. 
                                    // We use 8 bits for pixelSize and channelSize.
                                    double diff = Util.ImageBinaryDiff(
                                        pManaged,
                                        pNative,
                                        nWidth,
                                        nHeight,
                                        nRowSize,
                                        8,
                                        8);

                                    //for(int h = 0; h < nHeight; h++, pNativePtr += nRowSize, pManagedPtr += nRowSize)
                                    //{
                                    //    for (int w = 0; w < nWidth; w++)
                                    //    {
                                    //        if (w == 0)
                                    //            Console.Write("ImgN:");
                                    //        Console.Write($" {pNativePtr[w]:X}");
                                    //    }
                                    //    //Console.WriteLine();
                                    //    for (int w = 0; w < nWidth; w++)
                                    //    {
                                    //        if (w == 0)
                                    //            Console.Write(" ImgM:");
                                    //        Console.Write($" {pManagedPtr[w]:X}");
                                    //    }
                                    //    Console.WriteLine();
                                    //}

                                    if (diff != 0.0 && shapeDiffs.Count < 100)
                                    {
                                        shapeDiffs.Add(s, $"JB2 Shape Dictionary mismatch on shape {s} out of {nativeShapeCount}, page {p}, doc 23, dimensions w: {nWidth} h: {nHeight}, pixel count {nWidth * nHeight}, Diff: {diff}");
                                        if (shapeDiffs.Count >= 10)
                                            break;
                                    }
                                }
                            }

                            shapesProcessed++;
                        }
                        if (shapeDiffs.Count > 0)
                        {
                            foreach (int key in shapeDiffs.Keys)
                                Console.WriteLine(shapeDiffs[key]);
                        }
                        Assert.True(shapeDiffs.Count == 0);
                    }

                    Console.WriteLine($"Processed {p+1} pages out of {pageCount} and processed {shapesProcessed} out of {nativeShapeCount} JB2Shapes");
                    processedShapes += shapesProcessed;
                    totalShapes += nativeShapeCount;
                }

                Console.WriteLine($"Processed {processedShapes} out of total {totalShapes} JB2Shapes");
            }
        }
    }
}
