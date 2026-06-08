using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Xunit;
using DjvuNet.Tests;
using DjvuNet.DataChunks;
using DjvuNet.Wavelet;
using DjvuNet.DjvuLibre;

namespace DjvuNet.DjvuLibre.Compatibility.Tests
{
    public class Doc42Fixture : SingleDocFixture { public Doc42Fixture() : base(42) { } }
    public class Doc75Fixture : SingleDocFixture { public Doc75Fixture() : base(75) { } }

    public class IW44Compatibility_Doc42 : IClassFixture<Doc42Fixture>
    {
        private readonly Doc42Fixture _fixture;
        public IW44Compatibility_Doc42(Doc42Fixture fixture) { _fixture = fixture; }

        public static IEnumerable<object[]> PageIndices()
        {
            int pageCount = Util.GetTestDocumentPageCount(42);
            for (int i = 0; i < pageCount; i++) yield return new object[] { i };
        }

        [Theory]
        [MemberData(nameof(PageIndices))]
        public void VerifyEntropyDecodedBlocks(int pageIndex)
        {
            IW44CompatibilityTests.RunEntropyTestOnPage(42, _fixture.Document, pageIndex);
        }
    }

    public class IW44Compatibility_Doc75 : IClassFixture<Doc75Fixture>
    {
        private readonly Doc75Fixture _fixture;
        public IW44Compatibility_Doc75(Doc75Fixture fixture) { _fixture = fixture; }

        public static IEnumerable<object[]> PageIndices()
        {
            int pageCount = Util.GetTestDocumentPageCount(75);
            for (int i = 0; i < pageCount; i++) yield return new object[] { i };
        }

        [Theory]
        [MemberData(nameof(PageIndices))]
        public void VerifyEntropyDecodedBlocks(int pageIndex)
        {
            IW44CompatibilityTests.RunEntropyTestOnPage(75, _fixture.Document, pageIndex);
        }
    }

    public partial class IW44CompatibilityTests
    {
        internal static void RunEntropyTestOnPage(int fileIndex, DjvuDocument doc, int pIdx)
        {
            Assert.NotNull(doc);
            Assert.True(doc.Pages.Count > pIdx);

            var page = (DjvuPage)doc.Pages[pIdx];
            Assert.NotNull(page);

            var bg44Chunks = page.PageForm.Children
                .Where(x => x.ChunkType == ChunkType.BG44)
                .Cast<BG44Chunk>()
                .ToList();

            if (bg44Chunks.Count == 0)
            {
                foreach (var chunk in page.PageForm.Children)
                {
                    if (chunk is DjvuFormElement form)
                    {
                        bg44Chunks.AddRange(form.Children
                            .Where(x => x.ChunkType == ChunkType.BG44)
                            .Cast<BG44Chunk>());
                    }
                }
            }

            if (bg44Chunks.Count == 0) return; // Skip pages without background wavelets

            int totalDiffCount = 0;
            int totalBlocksWithErrors = 0;
            int maxPreviewLines = 256;
            StringBuilder errorLog = new StringBuilder();

            IntPtr nativeHandle = IntPtr.Zero;
            IntPtr nativeBlockBuffer = IntPtr.Zero;

            try
            {
                InterWavePixelMapDecoder mapDecoder = null;

                for (int chunkIdx = 0; chunkIdx < bg44Chunks.Count; chunkIdx++)
                {
                    var bg44 = bg44Chunks[chunkIdx];
                    byte[] rawChunkBytes;
                    using (var memoryReader = bg44.Reader.CloneReaderToMemory(bg44.DataOffset, bg44.Length))
                    {
                        rawChunkBytes = memoryReader.ReadBytes((int)bg44.Length);
                    }

                    bg44.Initialize();
                    if (mapDecoder == null)
                    {
                        mapDecoder = new InterWavePixelMapDecoder();
                        bg44.ProgressiveDecodeBackground(mapDecoder);
                    }
                    else
                    {
                        bg44.ProgressiveDecodeBackground(mapDecoder);
                    }

                    unsafe
                    {
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
                                Assert.True(decodeResult, $"Native DecodeIW44Chunk failed on chunk {chunkIdx}");
                            }
                        }
                    }
                }

                var csharpMap = (InterWaveMap)mapDecoder._YMap;

                bool result = NativeMethods.GetIW44MapInfo(nativeHandle, 0, out int bw, out int bh, out int nb);
                Assert.True(result, $"Native GetIW44MapInfo failed on page {pIdx}.");
                Assert.Equal(csharpMap.BlockWidth, bw);
                Assert.Equal(csharpMap.BlockHeight, bh);
                Assert.Equal(csharpMap.BlockNumber, nb);

                nativeBlockBuffer = DjvuMarshal.AllocHGlobal((uint)(1024 * sizeof(short)));

                for (int i = 0; i < nb; i++)
                {
                    var csharpBlock = csharpMap.Blocks[i];
                    short[] csCoeff = new short[1024];
                    csharpBlock.WriteLiftBlock(csCoeff, 0, 64);

                    bool blockResult = NativeMethods.GetIW44BlockData(nativeHandle, 0, i, nativeBlockBuffer, 1024);
                    if (!blockResult)
                    {
                        errorLog.AppendLine($"Native GetIW44BlockData failed to retrieve block {i} on page {pIdx}.");
                        totalDiffCount++;
                        totalBlocksWithErrors++;
                        continue;
                    }

                    unsafe
                    {
                        fixed (short* pCs = csCoeff)
                        {
                            // Treat 1024 shorts as a 1024x1 pixel row, stride is width in bytes
                            double diff = Util.ImageBinaryDiff((byte*)pCs, (byte*)nativeBlockBuffer, 1024, 1, 2048, 16, 8);
                            if (diff > 0.0)
                            {
                                totalBlocksWithErrors++;
                                totalDiffCount++; // Mark block as failed
                            }
                        }
                    }

                    /* --- Uncomment for deep scalar debugging ---
                    short[] cppCoeff = new short[1024];
                    Marshal.Copy(nativeBlockBuffer, cppCoeff, 0, 1024);

                    int blockDiffCount = 0;
                    for (int c = 0; c < 1024; c++)
                    {
                        if (csCoeff[c] != cppCoeff[c])
                        {
                            errorLog.AppendLine($"Page {pIdx,3} | Block {i,4} | Offset {c,4} | C#: {csCoeff[c],6} | C++: {cppCoeff[c],6} | Diff: {csCoeff[c] - cppCoeff[c]}");
                            blockDiffCount++;
                            totalDiffCount++;
                        }
                    }

                    if (blockDiffCount > 0)
                    {
                        totalBlocksWithErrors++;
                        errorLog.AppendLine($"--- End of Page {pIdx,3} Block {i,4} | Block Errors: {blockDiffCount} ---");
                    }
                    */
                }

                if (totalDiffCount > 0)
                {
                    string dumpPath = Path.Combine(Environment.CurrentDirectory, $"IW44_Diff_Report_Doc{fileIndex}_Page{pIdx}.log");
                    File.WriteAllText(dumpPath, errorLog.ToString());

                    string[] allLines = errorLog.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    string preview = string.Join(Environment.NewLine, allLines.Take(maxPreviewLines));

                    Assert.True(totalDiffCount == 0,
                        $"Entropy block parity failed for Doc {fileIndex} Page {pIdx}! Total Mismatches: {totalDiffCount} across {totalBlocksWithErrors} blocks.\n" +
                        $"Full report dumped to: {dumpPath}\n" +
                        $"--- Preview (First {maxPreviewLines} lines) ---\n{preview}\n...");
                }
            }
            finally
            {
                if (nativeHandle != IntPtr.Zero) NativeMethods.FreeIW44Image(nativeHandle);
                if (nativeBlockBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(nativeBlockBuffer);
            }
        }


        [Fact(Skip = "C++ backend preemptively runs spatial filters on load, destroying entropy state parity. Use isolated chunk tests instead.")]
        public void VerifyEntropyDecodedBlocks_test042C_02()
        {
            int testFileIndex = 42;
            string filePath = Util.GetTestFilePath(testFileIndex);

            // 1. Native C++ Decoding via existing wrappers
            using (DjvuDocumentInfo nativeDoc = DjvuDocumentInfo.CreateDjvuDocumentInfo(filePath))
            using (DjvuPageInfo nativePage = new DjvuPageInfo(nativeDoc, 0))
            {
                // Note: We MUST NOT call RenderPage() here. 
                // Rendering triggers spatial wavelet filters in C++ which destructively modifies 
                // the coefficients, causing massive mismatches against the raw C# entropy data.

                // 2. Managed C# Decoding
                using (DjvuDocument csDoc = Util.GetTestDocument(testFileIndex, out int pageCount))
                {
                    DjvuPage csPage = (DjvuPage)csDoc.Pages[0];
                    var csMap = (InterWaveMap)((InterWavePixelMapDecoder)csPage.BackgroundIWPixelMap)._YMap;

                    IntPtr nativeBlockBuffer = IntPtr.Zero;
                    int diffCount = 0;
                    int maxPreviewLines = 256;
                    StringBuilder errorLog = new StringBuilder();

                    try
                    {
                        nativeBlockBuffer = DjvuMarshal.AllocHGlobal((uint)(1024 * sizeof(short)));
                        // 3. Compare all blocks in the map
                        int blocksWithErrors = 0;

                        for (int i = 0; i < csMap.BlockNumber; i++)
                        {
                            var csharpBlock = csMap.Blocks[i];
                            short[] csCoeff = new short[1024];
                            csharpBlock.WriteLiftBlock(csCoeff, 0, 64);

                            bool blockResult = NativeMethods.GetPageIW44BlockData(nativePage.Page, 0, i, nativeBlockBuffer, 1024);
                            if (!blockResult)
                            {
                                errorLog.AppendLine($"Native GetPageIW44BlockData failed to retrieve block {i}.");
                                diffCount++;
                                blocksWithErrors++;
                                continue;
                            }

                            short[] cppCoeff = new short[1024];
                            Marshal.Copy(nativeBlockBuffer, cppCoeff, 0, 1024);

                            int blockDiffCount = 0;
                            for (int c = 0; c < 1024; c++)
                            {
                                if (csCoeff[c] != cppCoeff[c])
                                {
                                    errorLog.AppendLine($"Block {i,4} | Offset {c,4} | C#: {csCoeff[c],6} | C++: {cppCoeff[c],6} | Diff: {csCoeff[c] - cppCoeff[c]}");
                                    blockDiffCount++;
                                    diffCount++;
                                }
                            }

                            if (blockDiffCount > 0)
                            {
                                blocksWithErrors++;
                                errorLog.AppendLine($"--- End of Block {i,4} | Block Errors: {blockDiffCount} ---");
                            }
                        }

                        if (diffCount > 0)
                        {
                            string dumpPath = Path.Combine(Environment.CurrentDirectory, "IW44_Diff_Report_02.log");
                            File.WriteAllText(dumpPath, errorLog.ToString());

                            string[] allLines = errorLog.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                            string preview = string.Join(Environment.NewLine, allLines.Take(maxPreviewLines));

                            Assert.True(diffCount == 0,
                                $"Entropy block parity failed! Total Mismatches: {diffCount} across {blocksWithErrors} blocks.\n" +
                                $"Full report dumped to: {dumpPath}\n" +
                                $"--- Preview (First {maxPreviewLines} lines) ---\n{preview}\n...");
                        }
                    }
                    finally
                    {
                        if (nativeBlockBuffer != IntPtr.Zero)
                            DjvuMarshal.FreeHGlobal(nativeBlockBuffer);
                    }
                }
            }
        }

        private unsafe void AssertFilterParity(string filterName, int width, int height, int rowSize, int scale, short[] csResult, short[] cppResult)
        {
            fixed (short* pCs = csResult)
            fixed (short* pCpp = cppResult)
            {
                // We use rowSize * 2 for byte stride because array is linear memory
                double diff = Util.ImageBinaryDiff((byte*)pCs, (byte*)pCpp, width, height, rowSize * 2, 16, 8);
                
                if (diff > 0.0)
                {
                    int diffCount = 1; // Mark as failed
                    
                    // --- Uncomment for deep scalar debugging ---
                    /*
                    diffCount = 0;
                    int maxPreviewLines = 256;
                    StringBuilder errorLog = new StringBuilder();
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * rowSize + x;
                            if (csResult[idx] != cppResult[idx])
                            {
                                if (diffCount < maxPreviewLines)
                                    errorLog.AppendLine($"Y: {y,4} | X: {x,4} | Idx: {idx,6} | C#: {csResult[idx],6} | C++: {cppResult[idx],6} | Diff: {csResult[idx] - cppResult[idx]}");
                                diffCount++;
                            }
                        }
                    }
                    string dumpPath = Path.Combine(Environment.CurrentDirectory, $"IW44_Diff_{filterName}_{width}x{height}_s{scale}.log");
                    File.WriteAllText(dumpPath, errorLog.ToString());
                    Assert.True(diffCount == 0, $"{filterName}(w:{width}, h:{height}, rowSize:{rowSize}, scale:{scale}) failed with {diffCount} mismatches.\nFull report: {dumpPath}\nPreview:\n{errorLog}...");
                    */
                    
                    Assert.True(diff == 0.0, $"{filterName}(w:{width}, h:{height}, rowSize:{rowSize}, scale:{scale}) failed with diff ratio {diff}.");
                }
            }
        }

        public static IEnumerable<object[]> SpatialFilterTestParameters()
        {
            yield return new object[] { 32, 32, 32, 1 };     // Single block, scale 1
            yield return new object[] { 128, 128, 128, 1 };  // 4x4 blocks, scale 1
            yield return new object[] { 128, 128, 128, 2 };  // 4x4 blocks, scale 2
            yield return new object[] { 128, 128, 128, 16 }; // 4x4 blocks, max scale
            yield return new object[] { 128, 32, 128, 1 };   // Horizontal stripe
            yield return new object[] { 32, 128, 32, 1 };    // Vertical stripe
            yield return new object[] { 77, 99, 100, 1 };    // Odd boundaries with row padding
            yield return new object[] { 77, 99, 100, 4 };    // Odd boundaries scaled
        }

        [Theory]
        [MemberData(nameof(SpatialFilterTestParameters))]
        public void VerifySpatialFilter_BackwardHorizontal(int width, int height, int rowSize, int scale)
        {
            int totalElements = height * rowSize;
            short[] sourceData = new short[totalElements];
            Random rng = new Random(width + height + scale);
            for (int i = 0; i < totalElements; i++) sourceData[i] = (short)rng.Next(short.MinValue, short.MaxValue);

            IntPtr cppBuffer = IntPtr.Zero;
            IntPtr csBuffer = IntPtr.Zero;

            try
            {
                cppBuffer = DjvuMarshal.AllocHGlobal((uint)(totalElements * sizeof(short)));
                csBuffer = DjvuMarshal.AllocHGlobal((uint)(totalElements * sizeof(short)));
                Marshal.Copy(sourceData, 0, cppBuffer, totalElements);
                Marshal.Copy(sourceData, 0, csBuffer, totalElements);

                bool nativeResult = NativeMethods.FilterBh(cppBuffer, width, height, rowSize, scale);
                Assert.True(nativeResult, "Native FilterBh failed.");

                unsafe { InterWaveTransform.FilterBh((short*)csBuffer, width, height, rowSize, scale); }

                short[] cppResult = new short[totalElements];
                short[] csResult = new short[totalElements];
                Marshal.Copy(cppBuffer, cppResult, 0, totalElements);
                Marshal.Copy(csBuffer, csResult, 0, totalElements);

                AssertFilterParity("FilterBh", width, height, rowSize, scale, csResult, cppResult);
            }
            finally
            {
                if (cppBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(cppBuffer);
                if (csBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(csBuffer);
            }
        }

        [Theory]
        [MemberData(nameof(SpatialFilterTestParameters))]
        public void VerifySpatialFilter_BackwardVertical(int width, int height, int rowSize, int scale)
        {
            int totalElements = height * rowSize;
            short[] sourceData = new short[totalElements];
            Random rng = new Random(width + height + scale);
            for (int i = 0; i < totalElements; i++) sourceData[i] = (short)rng.Next(short.MinValue, short.MaxValue);

            IntPtr cppBuffer = IntPtr.Zero;
            IntPtr csBuffer = IntPtr.Zero;

            try
            {
                cppBuffer = DjvuMarshal.AllocHGlobal((uint)(totalElements * sizeof(short)));
                csBuffer = DjvuMarshal.AllocHGlobal((uint)(totalElements * sizeof(short)));
                Marshal.Copy(sourceData, 0, cppBuffer, totalElements);
                Marshal.Copy(sourceData, 0, csBuffer, totalElements);

                bool nativeResult = NativeMethods.FilterBv(cppBuffer, width, height, rowSize, scale);
                Assert.True(nativeResult, "Native FilterBv failed.");

                unsafe { InterWaveTransform.FilterBv((short*)csBuffer, width, height, rowSize, scale); }

                short[] cppResult = new short[totalElements];
                short[] csResult = new short[totalElements];
                Marshal.Copy(cppBuffer, cppResult, 0, totalElements);
                Marshal.Copy(csBuffer, csResult, 0, totalElements);

                AssertFilterParity("FilterBv", width, height, rowSize, scale, csResult, cppResult);
            }
            finally
            {
                if (cppBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(cppBuffer);
                if (csBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(csBuffer);
            }
        }

        [Theory]
        [MemberData(nameof(SpatialFilterTestParameters))]
        public void VerifySpatialFilter_ForwardHorizontal(int width, int height, int rowSize, int scale)
        {
            int totalElements = height * rowSize;
            short[] sourceData = new short[totalElements];
            Random rng = new Random(width + height + scale);
            for (int i = 0; i < totalElements; i++) sourceData[i] = (short)rng.Next(short.MinValue, short.MaxValue);

            IntPtr cppBuffer = IntPtr.Zero;
            IntPtr csBuffer = IntPtr.Zero;

            try
            {
                cppBuffer = DjvuMarshal.AllocHGlobal((uint)(totalElements * sizeof(short)));
                csBuffer = DjvuMarshal.AllocHGlobal((uint)(totalElements * sizeof(short)));
                Marshal.Copy(sourceData, 0, cppBuffer, totalElements);
                Marshal.Copy(sourceData, 0, csBuffer, totalElements);

                bool nativeResult = NativeMethods.FilterFh(cppBuffer, width, height, rowSize, scale);
                Assert.True(nativeResult, "Native FilterFh failed.");

                unsafe { InterWaveTransform.FilterFh((short*)csBuffer, width, height, rowSize, scale); }

                short[] cppResult = new short[totalElements];
                short[] csResult = new short[totalElements];
                Marshal.Copy(cppBuffer, cppResult, 0, totalElements);
                Marshal.Copy(csBuffer, csResult, 0, totalElements);

                AssertFilterParity("FilterFh", width, height, rowSize, scale, csResult, cppResult);
            }
            finally
            {
                if (cppBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(cppBuffer);
                if (csBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(csBuffer);
            }
        }

        [Theory]
        [MemberData(nameof(SpatialFilterTestParameters))]
        public void VerifySpatialFilter_ForwardVertical(int width, int height, int rowSize, int scale)
        {
            int totalElements = height * rowSize;
            short[] sourceData = new short[totalElements];
            Random rng = new Random(width + height + scale);
            for (int i = 0; i < totalElements; i++) sourceData[i] = (short)rng.Next(short.MinValue, short.MaxValue);

            IntPtr cppBuffer = IntPtr.Zero;
            IntPtr csBuffer = IntPtr.Zero;

            try
            {
                cppBuffer = DjvuMarshal.AllocHGlobal((uint)(totalElements * sizeof(short)));
                csBuffer = DjvuMarshal.AllocHGlobal((uint)(totalElements * sizeof(short)));
                Marshal.Copy(sourceData, 0, cppBuffer, totalElements);
                Marshal.Copy(sourceData, 0, csBuffer, totalElements);

                bool nativeResult = NativeMethods.FilterFv(cppBuffer, width, height, rowSize, scale);
                Assert.True(nativeResult, "Native FilterFv failed.");

                unsafe { InterWaveTransform.FilterFv((short*)csBuffer, width, height, rowSize, scale); }

                short[] cppResult = new short[totalElements];
                short[] csResult = new short[totalElements];
                Marshal.Copy(cppBuffer, cppResult, 0, totalElements);
                Marshal.Copy(csBuffer, csResult, 0, totalElements);

                AssertFilterParity("FilterFv", width, height, rowSize, scale, csResult, cppResult);
            }
            finally
            {
                if (cppBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(cppBuffer);
                if (csBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(csBuffer);
            }
        }

        [Theory]
        [MemberData(nameof(SpatialFilterTestParameters))]
        public unsafe void VerifyHighLevelTransform_Forward(int width, int height, int rowSize, int scale)
        {
            int totalElements = height * rowSize;
            short[] sourceData = new short[totalElements];
            Random rng = new Random(width + height + scale);
            for (int i = 0; i < totalElements; i++) sourceData[i] = (short)rng.Next(short.MinValue, short.MaxValue);

            IntPtr cppBuffer = IntPtr.Zero;
            IntPtr csBuffer = IntPtr.Zero;

            try
            {
                cppBuffer = DjvuMarshal.AllocHGlobal((uint)(totalElements * sizeof(short)));
                csBuffer = DjvuMarshal.AllocHGlobal((uint)(totalElements * sizeof(short)));
                Marshal.Copy(sourceData, 0, cppBuffer, totalElements);
                Marshal.Copy(sourceData, 0, csBuffer, totalElements);

                bool nativeResult = NativeMethods.IW44TransformForward(cppBuffer, width, height, rowSize, 1, scale);
                Assert.True(nativeResult, "Native IW44TransformForward failed.");

                InterWaveTransform.Forward((short*)csBuffer, width, height, rowSize, 1, scale);

                short[] cppResult = new short[totalElements];
                short[] csResult = new short[totalElements];
                Marshal.Copy(cppBuffer, cppResult, 0, totalElements);
                Marshal.Copy(csBuffer, csResult, 0, totalElements);

                AssertFilterParity("TransformForward", width, height, rowSize, scale, csResult, cppResult);
            }
            finally
            {
                if (cppBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(cppBuffer);
                if (csBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(csBuffer);
            }
        }

        [Theory]
        [MemberData(nameof(SpatialFilterTestParameters))]
        public unsafe void VerifyHighLevelTransform_Backward(int width, int height, int rowSize, int scale)
        {
            int totalElements = height * rowSize;
            short[] sourceData = new short[totalElements];
            Random rng = new Random(width + height + scale);
            for (int i = 0; i < totalElements; i++) sourceData[i] = (short)rng.Next(short.MinValue, short.MaxValue);

            IntPtr cppBuffer = IntPtr.Zero;
            IntPtr csBuffer = IntPtr.Zero;

            try
            {
                cppBuffer = DjvuMarshal.AllocHGlobal((uint)(totalElements * sizeof(short)));
                csBuffer = DjvuMarshal.AllocHGlobal((uint)(totalElements * sizeof(short)));
                Marshal.Copy(sourceData, 0, cppBuffer, totalElements);
                Marshal.Copy(sourceData, 0, csBuffer, totalElements);

                bool nativeResult = NativeMethods.IW44TransformBackward(cppBuffer, width, height, rowSize, scale, 1);
                Assert.True(nativeResult, "Native IW44TransformBackward failed.");

                InterWaveTransform.Backward((short*)csBuffer, width, height, rowSize, scale, 1);

                short[] cppResult = new short[totalElements];
                short[] csResult = new short[totalElements];
                Marshal.Copy(cppBuffer, cppResult, 0, totalElements);
                Marshal.Copy(csBuffer, csResult, 0, totalElements);

                AssertFilterParity("TransformBackward", width, height, rowSize, scale, csResult, cppResult);
            }
            finally
            {
                if (cppBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(cppBuffer);
                if (csBuffer != IntPtr.Zero) DjvuMarshal.FreeHGlobal(csBuffer);
            }
        }
    }
}
