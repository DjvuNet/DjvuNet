using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;
using DjvuNet.Graphics;
using DjvuNet.DjvuLibre;
using DjvuNet.DataChunks;
using DjvuNet.Tests;

namespace DjvuNet.DjvuLibre.Compatibility.Tests
{
    public class PagePixelMapTests
    {
        [Theory]
        [InlineData(23)]
        [InlineData(33)]
        [InlineData(75)]
        public unsafe void VerifyPageBackgroundData(int fileIndex)
        {
            string filePath = Util.GetTestFilePath(fileIndex);
            
            int nWidth, nHeight, rawSize;
            byte[] nativeBuffer;
            
            using (DjvuDocument managedDoc = Util.GetTestDocument(fileIndex, out int pageCount))
            using (DjvuDocumentInfo nativeDoc = DjvuDocumentInfo.CreateDjvuDocumentInfo(filePath))
            using (DjvuPageInfo nativePage = new DjvuPageInfo(nativeDoc, 0))
            {
                DjvuPage managedPage = (DjvuPage)managedDoc.Pages[0];
                Graphics.Rectangle managedRect = new Graphics.Rectangle(0, 0, managedPage.Width, managedPage.Height);
                PixelMap managedMap = (PixelMap)managedPage.GetBackgroundPixelMap(managedRect, 1, 2.2, null);
                
                DjvuRectangle pageRect = new DjvuRectangle { X = 0, Y = 0, Width = (uint)nativePage.Width, Height = (uint)nativePage.Height };
                DjvuRectangle renderRect = new DjvuRectangle { X = 0, Y = 0, Width = (uint)managedMap.Width, Height = (uint)managedMap.Height };

                bool res1 = NativeMethods.GetPageBackgroundData(nativePage.Page, ref renderRect, ref pageRect, out nWidth, out nHeight, out rawSize, IntPtr.Zero, 0);
                Assert.True(res1);
                
                nativeBuffer = new byte[rawSize];
                fixed (byte* pNative = nativeBuffer)
                {
                    bool res2 = NativeMethods.GetPageBackgroundData(nativePage.Page, ref renderRect, ref pageRect, out nWidth, out nHeight, out rawSize, (IntPtr)pNative, rawSize);
                    Assert.True(res2);
                }
                
                if (nWidth != managedMap.Width || nHeight != managedMap.Height)
                {
                    Console.WriteLine($"Dimension Mismatch: Native is {nWidth}x{nHeight}, Managed is {managedMap.Width}x{managedMap.Height}");
                    Assert.Fail("Dimension mismatch stops testing early.");
                }
                
                fixed (byte* pNative = nativeBuffer)
                fixed (sbyte* pManaged = managedMap.Data)
                {
                    int stride = managedMap.Width * PixelMap.BytesPerPixel;
                    double diff = Util.ImageBinaryDiffCore(pNative, (byte*)pManaged, (uint)nWidth, (uint)nHeight, stride, 24, 8);
                    
                    if (diff != 0.0)
                    {
                        Util.DumpImageMismatchDetails(pNative, (byte*)pManaged, managedMap.Data.Length, managedMap.Width, fileIndex, "Background");
                        Console.WriteLine($"Background Diff: {diff}");
                    }
                    
                    Assert.True(diff == 0.0, $"Background PixelMap Binary Mismatch. Diff: {diff}");
                }
            }
        }

        [Theory]
        [InlineData(23)]
        [InlineData(33)]
        [InlineData(75)]
        public unsafe void VerifyPageForegroundData(int fileIndex)
        {
            string filePath = Util.GetTestFilePath(fileIndex);
            
            int nWidth, nHeight, rawSize;
            byte[] nativeBuffer;
            
            using (DjvuDocument managedDoc = Util.GetTestDocument(fileIndex, out int pageCount))
            using (DjvuDocumentInfo nativeDoc = DjvuDocumentInfo.CreateDjvuDocumentInfo(filePath))
            using (DjvuPageInfo nativePage = new DjvuPageInfo(nativeDoc, 0))
            {
                DjvuPage managedPage = (DjvuPage)managedDoc.Pages[0];
                DjvuImage managedImage = (DjvuImage)managedPage.Image;
                PixelMap managedMap = managedImage.GetForegroundMap();
                Assert.NotNull(managedMap);
                
                DjvuRectangle pageRect = new DjvuRectangle { X = 0, Y = 0, Width = (uint)nativePage.Width, Height = (uint)nativePage.Height };
                DjvuRectangle renderRect = new DjvuRectangle { X = 0, Y = 0, Width = (uint)managedMap.Width, Height = (uint)managedMap.Height };

                bool res1 = NativeMethods.GetPageForegroundData(nativePage.Page, ref renderRect, ref pageRect, out nWidth, out nHeight, out rawSize, IntPtr.Zero, 0);
                Assert.True(res1);
                
                nativeBuffer = new byte[rawSize];
                fixed (byte* pNative = nativeBuffer)
                {
                    bool res2 = NativeMethods.GetPageForegroundData(nativePage.Page, ref renderRect, ref pageRect, out nWidth, out nHeight, out rawSize, (IntPtr)pNative, rawSize);
                    Assert.True(res2);
                }
                
                Assert.Equal(nWidth, managedMap.Width);
                Assert.Equal(nHeight, managedMap.Height);
                
                fixed (byte* pNative = nativeBuffer)
                fixed (sbyte* pManaged = managedMap.Data)
                {
                    int stride = managedMap.Width * PixelMap.BytesPerPixel;
                    double diff = Util.ImageBinaryDiffCore(pNative, (byte*)pManaged, (uint)nWidth, (uint)nHeight, stride, 24, 8);
                    
                    if (diff != 0.0)
                    {
                        Util.DumpImageMismatchDetails(pNative, (byte*)pManaged, managedMap.Data.Length, managedMap.Width, fileIndex, "Foreground");
                        Console.WriteLine($"Foreground Diff: {diff}");
                    }
                    
                    Assert.True(diff == 0.0, $"Foreground PixelMap Binary Mismatch. Diff: {diff}");
                }
            }
        }

        [Theory]
        [InlineData(23)]
        [InlineData(33)]
        [InlineData(75)]
        public unsafe void VerifyPageImageData(int fileIndex)
        {
            string filePath = Util.GetTestFilePath(fileIndex);
            
            int nWidth, nHeight, rawSize;
            byte[] nativeBuffer;
            
            using (DjvuDocument managedDoc = Util.GetTestDocument(fileIndex, out int pageCount))
            using (DjvuDocumentInfo nativeDoc = DjvuDocumentInfo.CreateDjvuDocumentInfo(filePath))
            using (DjvuPageInfo nativePage = new DjvuPageInfo(nativeDoc, 0))
            {
                DjvuPage managedPage = (DjvuPage)managedDoc.Pages[0];
                Graphics.Rectangle managedRect = new Graphics.Rectangle(0, 0, managedPage.Width, managedPage.Height);
                PixelMap managedMap = managedPage.GetPixelMap(managedRect, 1, 0.0, null);
                Assert.NotNull(managedMap);
                
                DjvuRectangle pageRect = new DjvuRectangle { X = 0, Y = 0, Width = (uint)nativePage.Width, Height = (uint)nativePage.Height };
                DjvuRectangle renderRect = new DjvuRectangle { X = 0, Y = 0, Width = (uint)managedMap.Width, Height = (uint)managedMap.Height };

                bool res1 = NativeMethods.GetPageImageData(nativePage.Page, ref renderRect, ref pageRect, out nWidth, out nHeight, out rawSize, IntPtr.Zero, 0);
                Assert.True(res1);
                
                nativeBuffer = new byte[rawSize];
                fixed (byte* pNative = nativeBuffer)
                {
                    bool res2 = NativeMethods.GetPageImageData(nativePage.Page, ref renderRect, ref pageRect, out nWidth, out nHeight, out rawSize, (IntPtr)pNative, rawSize);
                    Assert.True(res2);
                }
                
                Assert.Equal(nWidth, managedMap.Width);
                Assert.Equal(nHeight, managedMap.Height);
                
                fixed (byte* pNative = nativeBuffer)
                fixed (sbyte* pManaged = managedMap.Data)
                {
                    int stride = managedMap.Width * PixelMap.BytesPerPixel;
                    double diff = Util.ImageBinaryDiffCore(pNative, (byte*)pManaged, (uint)nWidth, (uint)nHeight, stride, 24, 8);
                    
                    if (diff != 0.0)
                    {
                        Console.WriteLine($"Composite PixelMap Binary Mismatch for test0{fileIndex}C.djvu. Diff: {diff}");
                    }
                    
                    Assert.True(diff == 0.0, $"Composite PixelMap Binary Mismatch. Diff: {diff}");
                }
            }
        }
        [Theory]
        [InlineData(23)]
        [InlineData(33)]
        [InlineData(75)]
        public unsafe void VerifyIW44RawPixmapData(int fileIndex)
        {
            using (DjvuDocument doc = Util.GetTestDocument(fileIndex, out int _))
            {
                DjvuPage page = (DjvuPage)doc.Pages[0];
                List<BG44Chunk> bg44Chunks = new List<BG44Chunk>();

                // 1. Gather all BG44 chunks from the page form
                foreach (var chunk in page.PageForm.Children)
                {
                    if (chunk.ChunkType == ChunkType.BG44)
                    {
                        bg44Chunks.Add((BG44Chunk)chunk);
                    }
                    else if (chunk is DjvuFormElement form)
                    {
                        foreach (var subChunk in form.Children)
                        {
                            if (subChunk.ChunkType == ChunkType.BG44)
                            {
                                bg44Chunks.Add((BG44Chunk)subChunk);
                            }
                        }
                    }
                }

                if (bg44Chunks.Count == 0) return;

                IntPtr nativeHandle = IntPtr.Zero;
                IntPtr rawBuffer = IntPtr.Zero;
                
                try
                {
                    // 2. Decode chunks natively
                    for (int chunkIdx = 0; chunkIdx < bg44Chunks.Count; chunkIdx++)
                    {
                        BG44Chunk bg44 = bg44Chunks[chunkIdx];
                        byte[] rawChunkBytes;
                        using (var memoryReader = bg44.Reader.CloneReaderToMemory(bg44.DataOffset, bg44.Length))
                        {
                            rawChunkBytes = memoryReader.ReadBytes((int)bg44.Length);
                        }

                        fixed (byte* pChunk = rawChunkBytes)
                        {
                            if (nativeHandle == IntPtr.Zero)
                            {
                                nativeHandle = NativeMethods.CreateIW44ImageFromChunk((IntPtr)pChunk, rawChunkBytes.Length, 1);
                                Assert.NotEqual(IntPtr.Zero, nativeHandle);
                            }
                            else
                            {
                                bool decodeResult = NativeMethods.DecodeIW44Chunk(nativeHandle, (IntPtr)pChunk, rawChunkBytes.Length);
                                Assert.True(decodeResult);
                            }
                        }
                    }

                    // 3. Native Extraction
                    NativeMethods.GetIW44RawPixelMap(nativeHandle, 1, IntPtr.Zero, IntPtr.Zero, 0, out int width, out int height);
                    int bufferSize = width * height * PixelMap.BytesPerPixel;
                    rawBuffer = DjvuMarshal.AllocHGlobal((uint)bufferSize);
                    NativeMethods.GetIW44RawPixelMap(nativeHandle, 1, IntPtr.Zero, rawBuffer, bufferSize, out int _, out int _);

                    // 4. C# Extraction
                    PixelMap csMap = page.BackgroundIWPixelMap.GetPixelMap();
                    
                    // 5. Binary Comparison with Dump Logic
                    fixed (sbyte* pCs = csMap.Data)
                    {
                        byte* pNative = (byte*)rawBuffer;
                        double diff = Util.ImageBinaryDiffCore(pNative, (byte*)pCs, (uint)width, (uint)height, width * PixelMap.BytesPerPixel, 24, 8);
                        
                        if (diff != 0.0)
                        {
                            Util.DumpImageMismatchDetails(pNative, (byte*)pCs, bufferSize, width, fileIndex, "Raw IW44");
                            Console.WriteLine($"Raw IW44 Diff: {diff}");
                        }
                        
                        Assert.True(diff == 0.0, $"Raw IW44 PixelMap Binary Mismatch. Diff: {diff}");
                    }
                }
                finally
                {
                    if (rawBuffer != IntPtr.Zero) 
                    {
                        DjvuMarshal.FreeHGlobal(rawBuffer);
                    }
                    
                    if (nativeHandle != IntPtr.Zero) 
                    {
                        NativeMethods.FreeIW44Image(nativeHandle);
                    }
                }
            }
        }

        [Theory]
        [InlineData(56)]
        [InlineData(62)]
        public unsafe void VerifyIW44RawPixmapData_Foreground(int fileIndex)
        {
            using (DjvuDocument doc = Util.GetTestDocument(fileIndex, out int _))
            {
                DjvuPage page = (DjvuPage)doc.Pages[0];
                List<FG44Chunk> fg44Chunks = new List<FG44Chunk>();

                // 1. Gather all FG44 chunks from the page form
                foreach (var chunk in page.PageForm.Children)
                {
                    if (chunk.ChunkType == ChunkType.FG44)
                    {
                        fg44Chunks.Add((FG44Chunk)chunk);
                    }
                    else if (chunk is DjvuFormElement form)
                    {
                        foreach (var subChunk in form.Children)
                        {
                            if (subChunk.ChunkType == ChunkType.FG44)
                            {
                                fg44Chunks.Add((FG44Chunk)subChunk);
                            }
                        }
                    }
                }

                if (fg44Chunks.Count == 0) return;

                IntPtr nativeHandle = IntPtr.Zero;
                IntPtr rawBuffer = IntPtr.Zero;
                
                try
                {
                    // 2. Decode chunks natively
                    for (int chunkIdx = 0; chunkIdx < fg44Chunks.Count; chunkIdx++)
                    {
                        FG44Chunk fg44 = fg44Chunks[chunkIdx];
                        byte[] rawChunkBytes;
                        
                        using (var memoryReader = 
                            fg44.Reader.CloneReaderToMemory(fg44.DataOffset, fg44.Length))
                        {
                            rawChunkBytes = memoryReader.ReadBytes((int)fg44.Length);
                        }

                        fixed (byte* pChunk = rawChunkBytes)
                        {
                            if (nativeHandle == IntPtr.Zero)
                            {
                                nativeHandle = NativeMethods.CreateIW44ImageFromChunk(
                                    (IntPtr)pChunk, rawChunkBytes.Length, 1);
                                    
                                Assert.NotEqual(IntPtr.Zero, nativeHandle);
                            }
                            else
                            {
                                bool decodeResult = NativeMethods.DecodeIW44Chunk(
                                    nativeHandle, (IntPtr)pChunk, rawChunkBytes.Length);
                                    
                                Assert.True(decodeResult);
                            }
                        }
                    }

                    // 3. Native Extraction
                    NativeMethods.GetIW44RawPixelMap(
                        nativeHandle, 1, IntPtr.Zero, IntPtr.Zero, 0, 
                        out int width, out int height);
                        
                    int bufferSize = width * height * PixelMap.BytesPerPixel;
                    rawBuffer = DjvuMarshal.AllocHGlobal((uint)bufferSize);
                    
                    NativeMethods.GetIW44RawPixelMap(
                        nativeHandle, 1, IntPtr.Zero, rawBuffer, bufferSize, 
                        out int _, out int _);

                    // 4. C# Extraction (using multiresolution bounding box to match C++)
                    PixelMap csMap = page.ForegroundIWPixelMap.GetPixelMap(1, new Graphics.Rectangle(0, 0, width, height), null);
                    
                    // 5. Binary Comparison with Dump Logic
                    fixed (sbyte* pCs = csMap.Data)
                    {
                        byte* pNative = (byte*)rawBuffer;
                        int stride = width * PixelMap.BytesPerPixel;
                        
                        double diff = Util.ImageBinaryDiffCore(
                            pNative, (byte*)pCs, (uint)width, (uint)height, stride, 24, 8);

                        double threshold = 0.0;
                        
                        if (diff > threshold)
                        {
                            Util.DumpImageMismatchDetails(
                                pNative, (byte*)pCs, bufferSize, width, fileIndex, "Raw FG44");
                                
                            Console.WriteLine($"Raw FG44 Diff: {diff}");
                        }
                        
                        Assert.True(diff == threshold, 
                            $"Raw FG44 PixelMap Binary Mismatch. Diff: {diff}");
                    }
                }
                finally
                {
                    if (rawBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(rawBuffer);
                    if (nativeHandle != IntPtr.Zero) NativeMethods.FreeIW44Image(nativeHandle);
                }
            }
        }

        [Theory]
        [InlineData(56)]
        [InlineData(62)]
        public unsafe void VerifyIW44RawPixmapData_Foreground_Linear(int fileIndex)
        {
            using (DjvuDocument doc = Util.GetTestDocument(fileIndex, out int _))
            {
                DjvuPage page = (DjvuPage)doc.Pages[0];
                List<FG44Chunk> fg44Chunks = new List<FG44Chunk>();

                // 1. Gather all FG44 chunks from the page form
                foreach (var chunk in page.PageForm.Children)
                {
                    if (chunk.ChunkType == ChunkType.FG44)
                    {
                        fg44Chunks.Add((FG44Chunk)chunk);
                    }
                    else if (chunk is DjvuFormElement form)
                    {
                        foreach (var subChunk in form.Children)
                        {
                            if (subChunk.ChunkType == ChunkType.FG44)
                            {
                                fg44Chunks.Add((FG44Chunk)subChunk);
                            }
                        }
                    }
                }

                if (fg44Chunks.Count == 0) return;

                IntPtr nativeHandle = IntPtr.Zero;
                IntPtr rawBuffer = IntPtr.Zero;
                
                try
                {
                    // 2. Decode chunks natively
                    for (int chunkIdx = 0; chunkIdx < fg44Chunks.Count; chunkIdx++)
                    {
                        FG44Chunk fg44 = fg44Chunks[chunkIdx];
                        byte[] rawChunkBytes;
                        
                        using (var memoryReader = 
                            fg44.Reader.CloneReaderToMemory(fg44.DataOffset, fg44.Length))
                        {
                            rawChunkBytes = memoryReader.ReadBytes((int)fg44.Length);
                        }

                        fixed (byte* pChunk = rawChunkBytes)
                        {
                            if (nativeHandle == IntPtr.Zero)
                            {
                                nativeHandle = NativeMethods.CreateIW44ImageFromChunk(
                                    (IntPtr)pChunk, rawChunkBytes.Length, 1);
                                    
                                Assert.NotEqual(IntPtr.Zero, nativeHandle);
                            }
                            else
                            {
                                bool decodeResult = NativeMethods.DecodeIW44Chunk(
                                    nativeHandle, (IntPtr)pChunk, rawChunkBytes.Length);
                                    
                                Assert.True(decodeResult);
                            }
                        }
                    }

                    // 3. Native Extraction (Linear Decode)
                    NativeMethods.GetIW44RawPixelMapLinear(
                        nativeHandle, IntPtr.Zero, 0, 
                        out int width, out int height);
                        
                    int bufferSize = width * height * PixelMap.BytesPerPixel;
                    rawBuffer = DjvuMarshal.AllocHGlobal((uint)bufferSize);
                    
                    NativeMethods.GetIW44RawPixelMapLinear(
                        nativeHandle, rawBuffer, bufferSize, 
                        out int _, out int _);

                    // 4. C# Extraction (Full Decode)
                    PixelMap csMap = page.ForegroundIWPixelMap.GetPixelMap();
                    
                    // 5. Binary Comparison with Dump Logic
                    fixed (sbyte* pCs = csMap.Data)
                    {
                        byte* pNative = (byte*)rawBuffer;
                        int stride = width * PixelMap.BytesPerPixel;
                        
                        double diff = Util.ImageBinaryDiffCore(
                            pNative, (byte*)pCs, (uint)width, (uint)height, stride, 24, 8);

                        double threshold = 0.0;

                        if (diff > threshold)
                        {
                            Util.DumpImageMismatchDetails(
                                pNative, (byte*)pCs, bufferSize, width, fileIndex, "Raw FG44 Linear");
                                
                            Console.WriteLine($"Raw FG44 Linear Diff: {diff}");
                        }
                        
                        Assert.True(diff == threshold, 
                            $"Raw FG44 Linear PixelMap Binary Mismatch. Diff: {diff}");
                    }
                }
                finally
                {
                    if (rawBuffer != IntPtr.Zero) 
                    {
                        DjvuMarshal.FreeHGlobal(rawBuffer);
                    }
                    if (nativeHandle != IntPtr.Zero) 
                    {
                        NativeMethods.FreeIW44Image(nativeHandle);
                    }
                }
            }
        }
    }
}
