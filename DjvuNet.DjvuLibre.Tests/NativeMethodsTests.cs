using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using DjvuNet.DjvuLibre;
using DjvuNet.Errors;
using DjvuNet.Tests;
using Xunit;

namespace DjvuNet.DjvuLibre.Tests
{
    public class NativeMethodsTests
    {
        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(1, 0, 0)]
        [InlineData(0, 1, 0)]
        [InlineData(0, 0, 1)]
        [InlineData(1, 1, 1)]
        public void Jb2EncodingOptions_SetAndGet_StateRoundtrip(int all, int shared, int marks)
        {
            // 1. Arrange & Act: Set the new state securely within the context lock
            using (new Jb2EncodingContext(all, shared, marks))
            {
                // 2. Act: Retrieve the state
                int containsAll, containsShared, containsMarks;
                bool getResult = NativeMethods.GetJb2EncodingOptions(out containsAll, out containsShared, out containsMarks);

                // 3. Assert: Ensure the getter returned true and values match the context
                Assert.True(getResult, "GetJb2EncodingOptions failed to retrieve options settings.");
                Assert.Equal(all, containsAll);
                Assert.Equal(shared, containsShared);
                Assert.Equal(marks, containsMarks);
            }

            // 4. Act: Retrieve the state after encoding context was restored
            int vAll, vShared, vMarks;
            bool vResult = NativeMethods.GetJb2EncodingOptions(out vAll, out vShared, out vMarks);

            // 5. Assert: Ensure the getter returned true and values match the default context
            Assert.True(vResult, "GetJb2EncodingOptions failed to retrieve options settings.");
            Assert.Equal(1, vAll);
            Assert.Equal(0, vShared);
            Assert.Equal(0, vMarks);
        }

        public static IEnumerable<object[]> JB2ImageTestData => Util.GetJB2ImageTestData(
            skipDocs: new int[] { },
            skipChunks: new string[] { },
            TestCoverage.UniqueOnly,
            2
        );

        [Theory]
        [MemberData(nameof(JB2ImageTestData))]
        public void EncodeDjvuJb2ImageToChunk_RoundTrip(string djbzFileName, string sjbzFileName)
        {
            string sjbzFilePath = Path.Combine(Util.ArtifactsDataPath, sjbzFileName);
            byte[] sjbzPayload = File.ReadAllBytes(sjbzFilePath);

            byte[] djbzPayload = null;
            if (!string.IsNullOrEmpty(djbzFileName))
            {
                string djbzFilePath = Path.Combine(Util.ArtifactsDataPath, djbzFileName);
                djbzPayload = File.ReadAllBytes(djbzFilePath);
            }

            IntPtr nativeImage = IntPtr.Zero;
            IntPtr outData = IntPtr.Zero;
            int outSize = 0;

            try
            {
                // 1. Decode into native JB2Image AST (Oracle)
                unsafe
                {
                    if (djbzPayload == null)
                    {
                        fixed (byte* pSjbz = sjbzPayload)
                        {
                            bool imgResult = NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pSjbz, sjbzPayload.Length, IntPtr.Zero, 0, out nativeImage);
                            Assert.True(imgResult, "Native CreateDjvuJb2ImageFromChunk failed.");
                        }
                    }
                    else
                    {
                        fixed (byte* pSjbz = sjbzPayload)
                        fixed (byte* pDjbz = djbzPayload)
                        {
                            bool imgResult = NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pSjbz, sjbzPayload.Length, (IntPtr)pDjbz, djbzPayload.Length, out nativeImage);
                            Assert.True(imgResult, "Native CreateDjvuJb2ImageFromChunk failed.");
                        }
                    }
                }

                // 2. Encode native JB2Image AST back to chunk using new unlocked code
                bool encodeResult = NativeMethods.EncodeDjvuJb2ImageToChunk(nativeImage, out outData, out outSize);

                Assert.True(encodeResult, "EncodeDjvuJb2ImageToChunk failed to return true.");
                Assert.NotEqual(IntPtr.Zero, outData);
                Assert.True(outSize > 0, "Encoded chunk size should be greater than 0.");

                // 3. Extract the generated chunk memory
                byte[] encodedChunk = new byte[outSize];
                Marshal.Copy(outData, encodedChunk, 0, outSize);

                // 4. Verify Success: Compare the hashes of both binary payloads
                using (var sha = SHA256.Create())
                {
                    string originalHash = BitConverter.ToString(sha.ComputeHash(sjbzPayload)).Replace("-", "").ToLowerInvariant();
                    string newHash = BitConverter.ToString(sha.ComputeHash(encodedChunk)).Replace("-", "").ToLowerInvariant();

                    Assert.Equal(originalHash, newHash);
                }
            }
            finally
            {
                // 5. Secure deallocation of memory across the unmanaged boundary
                if (outData != IntPtr.Zero)
                {
                    DjvuMarshal.FreeHGlobal(outData);
                }

                if (nativeImage != IntPtr.Zero)
                {
                    NativeMethods.FreeDjvuJb2Image(nativeImage);
                }
            }
        }

        public static IEnumerable<object[]> UniqueDjbzJB2ImageTestData => Util.GetJB2ImageTestData(
            null,
            new string[] {
                    "test005C_P02.sjbz", "test012C_P11.sjbz", "test016C_P04.sjbz", "test016C_P11.sjbz", "test024C_P10.sjbz",
                    "test027C_P07.sjbz", "test032C_P02.sjbz", "test032C_P08.sjbz", "test073C_P02.sjbz"
            },
            TestCoverage.DjbzNotNull,
            3
        );

        public static IEnumerable<object[]> ExtractedRareVariantTestData => Util.GetExtractedRareVariantPayloads(coverage: TestCoverage.UniqueOnly, encoderCoverage: JB2EncoderTestCoverage.AllVariants, step: 10);

        [Theory]
        [MemberData(nameof(UniqueDjbzJB2ImageTestData))]
        public void EncodeDjvuJb2ImageToChunk_SharedPath(string djbzFileName, string sjbzFileName)
        {
            byte[] sjbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, sjbzFileName));
            byte[] djbzPayload = null;

            if (!string.IsNullOrEmpty(djbzFileName))
            {
                djbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName));
            }

            IntPtr nativeImage = IntPtr.Zero;

            try
            {
                // 1. Decode into native JB2Image AST (Oracle)
                unsafe
                {
                    if (djbzPayload != null)
                    {
                        fixed (byte* pSjbz = sjbzPayload)
                        fixed (byte* pDjbz = djbzPayload)
                        {
                            bool imgResult = NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pSjbz, sjbzPayload.Length, (IntPtr)pDjbz, djbzPayload.Length, out nativeImage);
                            Assert.True(imgResult, "Native CreateDjvuJb2ImageFromChunk failed.");
                        }
                    }
                    else
                    {
                        fixed (byte* pSjbz = sjbzPayload)
                        {
                            bool imgResult = NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pSjbz, sjbzPayload.Length, IntPtr.Zero, 0, out nativeImage);
                            Assert.True(imgResult, "Native CreateDjvuJb2ImageFromChunk failed.");
                        }
                    }
                }

                // 2. Encode with Path 1: (1, 0, 0) - All
                byte[] payload1;
                using (new Jb2EncodingContext(1, 0, 0)) { payload1 = EncodeToMemory(nativeImage); }

                // 3. Encode with Path 2: (0, 1, 0) - Shared
                byte[] payload2;
                using (new Jb2EncodingContext(0, 1, 0)) { payload2 = EncodeToMemory(nativeImage); }

                // 5. Compare Hashes against the original page chunk
                using (var sha = SHA256.Create())
                {
                    string originalHash = BitConverter.ToString(sha.ComputeHash(sjbzPayload)).Replace("-", "").ToLowerInvariant();
                    string hash1 = BitConverter.ToString(sha.ComputeHash(payload1)).Replace("-", "").ToLowerInvariant();
                    string hash2 = BitConverter.ToString(sha.ComputeHash(payload2)).Replace("-", "").ToLowerInvariant();

                    bool match1 = hash1 == originalHash;
                    bool match2 = hash2 == originalHash;

                    // string baseName = Path.GetFileNameWithoutExtension(sjbzFileName);
                    // string ext = Path.GetExtension(sjbzFileName);
                    // string outDir = Path.Combine(Util.ArtifactsDataPath, "extracted");

                    // if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

                    // Save diverging payloads to artifacts for C# Managed Codec integration testing
                    // if (!match2) File.WriteAllBytes(Path.Combine(outDir, $"{baseName}_shared{ext}"), payload2);
                    // if (!match3) File.WriteAllBytes(Path.Combine(outDir, $"{baseName}_marks{ext}"), payload3);
                    // if (!match4) File.WriteAllBytes(Path.Combine(outDir, $"{baseName}_allzero{ext}"), payload4);

                    int matchCount = (match1 ? 1 : 0) + (match2 ? 1 : 0);

                    Assert.True(matchCount == 1, $"Match count was {matchCount}. Matches: Path1(Default)={match1}, Path2(Shared)={match2}");
                }
            }
            finally
            {
                if (nativeImage != IntPtr.Zero)
                    NativeMethods.FreeDjvuJb2Image(nativeImage);
            }
        }

        [Theory]
        [MemberData(nameof(Util.ZeroEncodingJB2ImageTestData), MemberType = typeof(Util))]
        public void EncodeDjvuJb2ImageToChunk_WithAllZero(string djbzFileName, string sjbzFileName)
        {
            byte[] sjbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, sjbzFileName));
            byte[] djbzPayload = djbzFileName != null ? File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName)) : null;

            IntPtr nativeImage = IntPtr.Zero;

            try
            {
                unsafe
                {
                    if (djbzPayload != null)
                    {
                        fixed (byte* pSjbz = sjbzPayload)
                        fixed (byte* pDjbz = djbzPayload)
                        {
                            Assert.True(NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pSjbz, sjbzPayload.Length, (IntPtr)pDjbz, djbzPayload.Length, out nativeImage));
                        }
                    }
                    else
                    {
                        fixed (byte* pSjbz = sjbzPayload)
                        {
                            Assert.True(NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pSjbz, sjbzPayload.Length, IntPtr.Zero, 0, out nativeImage));
                        }
                    }
                }

                byte[] payloadAllZero;
                using (new Jb2EncodingContext(0, 0, 0)) { payloadAllZero = EncodeToMemory(nativeImage); }

                // Compare buffers using SIMD accelerated ImageBinaryDiff
                unsafe
                {
                    int compareLength = Math.Min(sjbzPayload.Length, payloadAllZero.Length);
                    fixed (byte* pOrig = sjbzPayload)
                    fixed (byte* pAllZero = payloadAllZero)
                    {
                        double diff = Util.ImageBinaryDiff(pOrig, pAllZero, compareLength, 1, compareLength, 8, 8);
                        string msg = $"Bitstreams identical - diff: {diff:F4}. Original size {sjbzPayload.Length}, AllZero size {payloadAllZero.Length}";
                        bool isIdentical = diff == 0.0 && sjbzPayload.Length == payloadAllZero.Length;
                        if (isIdentical)
                            Console.WriteLine(msg);
                        Assert.False(isIdentical, msg);
                    }
                }
            }
            finally
            {
                if (nativeImage != IntPtr.Zero)
                    NativeMethods.FreeDjvuJb2Image(nativeImage);
            }
        }

        [Theory]
        [InlineData("extracted/test002C_D1868.djbz", "extracted/test002C_P02.sjbz")] // Page with Shared Dict
        [InlineData(null, "extracted/test053C_P02.sjbz")] // Pure Standalone Page (no shared dict)
        public void EncodeDjvuJb2ImageToChunk_RoundTrip_NativeDecoder(string djbzFileName, string sjbzFileName)
        {
            byte[] sjbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, sjbzFileName));
            byte[] djbzPayload = djbzFileName != null ? File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName)) : null;

            IntPtr nativeImage = IntPtr.Zero;

            try
            {
                unsafe
                {
                    if (djbzPayload != null)
                    {
                        fixed (byte* pSjbz = sjbzPayload)
                        fixed (byte* pDjbz = djbzPayload)
                        {
                            Assert.True(NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pSjbz, sjbzPayload.Length, (IntPtr)pDjbz, djbzPayload.Length, out nativeImage));
                        }
                    }
                    else
                    {
                        fixed (byte* pSjbz = sjbzPayload)
                        {
                            Assert.True(NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pSjbz, sjbzPayload.Length, IntPtr.Zero, 0, out nativeImage));
                        }
                    }
                }

                bool getBmpInfo = NativeMethods.GetDjvuJb2ImageBitmap(nativeImage, 4, out int origWidth, out int origHeight, out int origRowSize, out int origBorder, IntPtr.Zero, 0);
                Assert.True(getBmpInfo);

                byte[] payloadAll;
                using (new Jb2EncodingContext(1, 0, 0)) { payloadAll = EncodeToMemory(nativeImage); }

                byte[] payloadShared;
                using (new Jb2EncodingContext(0, 1, 0)) { payloadShared = EncodeToMemory(nativeImage); }

                byte[] payloadMarks;
                using (new Jb2EncodingContext(0, 0, 1)) { payloadMarks = EncodeToMemory(nativeImage); }

                byte[] payloadAllZero;
                using (new Jb2EncodingContext(0, 0, 0)) { payloadAllZero = EncodeToMemory(nativeImage); }

                bool payloadAllMatched = TestNativePayloadAgainstReference(payloadAll, djbzPayload, origWidth, origHeight, origRowSize);
                bool payloadSharedMatched = TestNativePayloadAgainstReference(payloadShared, djbzPayload, origWidth, origHeight, origRowSize);
                bool payloadMarksMatched = TestNativePayloadAgainstReference(payloadMarks, djbzPayload, origWidth, origHeight, origRowSize);
                bool payloadAllZeroMatched = TestNativePayloadAgainstReference(payloadAllZero, djbzPayload, origWidth, origHeight, origRowSize);

                int matchCount = (payloadAllMatched ? 1 : 0) + (payloadSharedMatched ? 1 : 0) + (payloadMarksMatched ? 1 : 0) + (payloadAllZeroMatched ? 1 : 0);

                Assert.True(matchCount == 4, $"Expected exactly 4 payloads to successfully roundtrip through the Native pipeline. Matches: Default={payloadAllMatched}, Shared={payloadSharedMatched}, Marks={payloadMarksMatched}, AllZero={payloadAllZeroMatched}");
            }
            finally
            {
                if (nativeImage != IntPtr.Zero)
                    NativeMethods.FreeDjvuJb2Image(nativeImage);
            }
        }

        [Theory]
        [MemberData(nameof(ExtractedRareVariantTestData))]
        public void NativeDecoder_ParsesGeneratedRareVariants(string variantFile, string originalSjbzPath, string djbzPath)
        {
            byte[] variantPayload = File.ReadAllBytes(variantFile);
            byte[] originalPayload = File.ReadAllBytes(originalSjbzPath);
            byte[] djbzPayload = djbzPath != null ? File.ReadAllBytes(djbzPath) : null;

            IntPtr nativeOriginal = IntPtr.Zero;
            IntPtr nativeVariant = IntPtr.Zero;

            try
            {
                unsafe
                {
                    fixed (byte* pOrig = originalPayload, pVar = variantPayload, pDjbz = djbzPayload)
                    {
                        IntPtr ptrDjbz = djbzPayload != null ? (IntPtr)pDjbz : IntPtr.Zero;
                        int lenDjbz = djbzPayload != null ? djbzPayload.Length : 0;

                        Assert.True(NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pOrig, originalPayload.Length, ptrDjbz, lenDjbz, out nativeOriginal), $"Failed to decode original {Path.GetFileName(originalSjbzPath)}");
                        Assert.True(NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pVar, variantPayload.Length, ptrDjbz, lenDjbz, out nativeVariant), $"Failed to decode variant {Path.GetFileName(variantFile)}");
                    }
                }

                Assert.True(NativeMethods.GetDjvuJb2ImageBitmap(nativeOriginal, 4, out int origW, out int origH, out int origRow, out int origBorder, IntPtr.Zero, 0));
                Assert.True(NativeMethods.GetDjvuJb2ImageBitmap(nativeVariant, 4, out int varW, out int varH, out int varRow, out int varBorder, IntPtr.Zero, 0));

                Assert.Equal(origW, varW);
                Assert.Equal(origH, varH);
                Assert.Equal(origRow, varRow);

                long pixelCount = (long)origRow * origH;
                if (pixelCount > int.MaxValue)
                    DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(origH), origH, "Image dimensions cause memory boundary calculation to exceed maximum integer size.");
                
                byte[] origPixels = new byte[(int)pixelCount];
                byte[] varPixels = new byte[(int)pixelCount];

                double diff = -1.0;
                unsafe
                {
                    fixed(byte* pOrigPix = origPixels, pVarPix = varPixels)
                    {
                        Assert.True(NativeMethods.GetDjvuJb2ImageBitmap(nativeOriginal, 4, out _, out _, out _, out _, (IntPtr)pOrigPix, origPixels.Length));
                        Assert.True(NativeMethods.GetDjvuJb2ImageBitmap(nativeVariant, 4, out _, out _, out _, out _, (IntPtr)pVarPix, varPixels.Length));

                        diff = Util.ImageBinaryDiff(pOrigPix, pVarPix, origW, origH, origRow, 8, 8);
                    }
                }

                Assert.True(diff == 0.0, $"Rendered parity mismatch! Diff: {diff}. Variant: {Path.GetFileName(variantFile)} vs Reference: {Path.GetFileName(originalSjbzPath)}");
            }
            finally
            {
                if (nativeOriginal != IntPtr.Zero) NativeMethods.FreeDjvuJb2Image(nativeOriginal);
                if (nativeVariant != IntPtr.Zero) NativeMethods.FreeDjvuJb2Image(nativeVariant);
            }
        }

        private bool TestNativePayloadAgainstReference(byte[] payload, byte[] djbzPayload, int expectedWidth, int expectedHeight, int expectedRowSize)
        {
            IntPtr nativeImage = IntPtr.Zero;
            try
            {
                bool success = false;
                unsafe
                {
                    fixed (byte* pPayload = payload)
                    {
                        if (djbzPayload != null)
                        {
                            fixed (byte* pDjbz = djbzPayload)
                            {
                                success = NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pPayload, payload.Length, (IntPtr)pDjbz, djbzPayload.Length, out nativeImage);
                            }
                        }
                        else
                        {
                            success = NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pPayload, payload.Length, IntPtr.Zero, 0, out nativeImage);
                        }
                    }
                }

                if (!success || nativeImage == IntPtr.Zero)
                    return false;

                bool getBmpInfo = NativeMethods.GetDjvuJb2ImageBitmap(nativeImage, 4, out int w, out int h, out int rowSize, out int border, IntPtr.Zero, 0);
                return getBmpInfo && w == expectedWidth && h == expectedHeight && rowSize == expectedRowSize;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (nativeImage != IntPtr.Zero)
                    NativeMethods.FreeDjvuJb2Image(nativeImage);
            }
        }

        public static IEnumerable<object[]> FullJB2DictionaryTestData => Util.GetJB2DictionaryTestData();

        [Theory]
        [MemberData(nameof(FullJB2DictionaryTestData))]
        public void EncodeDjvuJb2DictToChunk_RoundTrip(string djbzFileName)
        {
            string djbzFilePath = Path.Combine(Util.ArtifactsDataPath, djbzFileName);
            byte[] djbzPayload = File.ReadAllBytes(djbzFilePath);

            IntPtr nativeDict = IntPtr.Zero;
            IntPtr outData = IntPtr.Zero;
            int outSize = 0;
            int shapeCount = 0;

            try
            {
                // 1. Decode into native JB2Dict AST
                unsafe
                {
                    fixed (byte* pDjbz = djbzPayload)
                    {
                        bool dictResult = NativeMethods.CreateDjvuJb2DictFromChunk((IntPtr)pDjbz, djbzPayload.Length, out shapeCount, out nativeDict);
                        Assert.True(dictResult, "Native CreateDjvuJb2DictFromChunk failed.");
                    }
                }

                // 2. Encode native JB2Dict AST back to chunk
                bool encodeResult = NativeMethods.EncodeDjvuJb2DictToChunk(nativeDict, out outData, out outSize);

                Assert.True(encodeResult, "EncodeDjvuJb2DictToChunk failed to return true.");
                Assert.NotEqual(IntPtr.Zero, outData);
                Assert.True(outSize > 0, "Encoded chunk size should be greater than 0.");

                // 3. Extract the generated chunk memory
                byte[] encodedChunk = new byte[outSize];
                Marshal.Copy(outData, encodedChunk, 0, outSize);

                // 4. Verify Success: Compare the hashes of both binary payloads
                using (var sha = SHA256.Create())
                {
                    string originalHash = BitConverter.ToString(sha.ComputeHash(djbzPayload)).Replace("-", "").ToLowerInvariant();
                    string newHash = BitConverter.ToString(sha.ComputeHash(encodedChunk)).Replace("-", "").ToLowerInvariant();

                    Assert.Equal(originalHash, newHash);
                }
            }
            finally
            {
                // 5. Secure deallocation of memory across the unmanaged boundary
                if (outData != IntPtr.Zero)
                {
                    DjvuMarshal.FreeHGlobal(outData);
                }

                if (nativeDict != IntPtr.Zero)
                {
                    NativeMethods.FreeDjvuJb2Dict(nativeDict);
                }
            }
        }

        private byte[] EncodeToMemory(IntPtr nativeImage)
        {
            IntPtr outData = IntPtr.Zero;
            int outSize = 0;
            try
            {
                bool encodeResult = NativeMethods.EncodeDjvuJb2ImageToChunk(nativeImage, out outData, out outSize);
                Assert.True(encodeResult);
                byte[] chunk = new byte[outSize];
                Marshal.Copy(outData, chunk, 0, outSize);
                return chunk;
            }
            finally
            {
                if (outData != IntPtr.Zero)
                    DjvuMarshal.FreeHGlobal(outData);
            }
        }

        [Theory]
        [InlineData("extracted/test002C_D1868.djbz", "extracted/test002C_P02.sjbz")]
        [InlineData("extracted/test002C_D1868.djbz", "extracted/test002C_P03.sjbz")]
        [InlineData("extracted/test003C_D1030.djbz", "extracted/test003C_P07.sjbz")]
        [InlineData("extracted/test003C_D1030.djbz", "extracted/test003C_P09.sjbz")]
        [InlineData("extracted/test003C_D76090.djbz", "extracted/test003C_P11.sjbz")]
        public unsafe void Encode_JB2Image_EncodesNonMarkData(string djbzFileName, string sjbzFileName)
        {
            string sjbzPath = Path.Combine(Util.ArtifactsDataPath, sjbzFileName);
            byte[] sjbzData = File.ReadAllBytes(sjbzPath);
            byte[] djbzData = djbzFileName != null ? File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName)) : null;

            IntPtr ptrJB2Img = IntPtr.Zero;
            IntPtr outDataAllZero = IntPtr.Zero;
            IntPtr outDataNonMark = IntPtr.Zero;
            try
            {
                fixed (byte* pSjbz = sjbzData)
                {
                    // 1. Create native JB2Image via DjVuLibre P/Invoke
                    bool result = default;
                    if (djbzData != null)
                    {
                        fixed (byte* pDjbz = djbzData)
                        {
                            result = NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pSjbz, sjbzData.Length, (IntPtr)pDjbz, djbzData.Length, out ptrJB2Img);
                        }
                        Assert.True(result, $"NativeMethods.CreateDjvuJb2ImageFromChunk failed. Djbz file{djbzFileName}, Sjbz file:{sjbzFileName}");
                        Assert.NotEqual(IntPtr.Zero, ptrJB2Img);
                    }
                    else
                    {
                        result = NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pSjbz, sjbzData.Length, IntPtr.Zero, 0, out ptrJB2Img);
                        Assert.True(result, $"NativeMethods.CreateDjvuJb2ImageFromChunk failed. Sjbz file:{sjbzFileName}");
                        Assert.NotEqual(IntPtr.Zero, ptrJB2Img);
                    }

                }

                // Get shapes count and inherited shapes count first
                // This call will return false but will return required data as well
                // All other data are invalid
                int shapeCount = 0;
                int inheritedShapeCount = 0;
                int origParent = int.MinValue;
                NativeMethods.GetDjvuJb2ShapeParent(ptrJB2Img, 0, out shapeCount, out inheritedShapeCount, out origParent);

                // Get original shape parent for last shape using getter
                // This call has to return true to indicate valid data retrieved
                int lastShape = shapeCount - 1;
                bool getResult = NativeMethods.GetDjvuJb2ShapeParent(ptrJB2Img, lastShape, out shapeCount, out inheritedShapeCount, out origParent);
                Assert.True(getResult, "NativeMethods.GetDjvuJb2ShapeParent failed.");

                using (var ctx = new Jb2EncodingContext(0, 0, 0))
                {
                    // 2. Encode AllZero (baseline bitstream)
                    bool encAllZero = NativeMethods.EncodeDjvuJb2ImageToChunk(ptrJB2Img, out outDataAllZero, out int outSizeAllZero);
                    Assert.True(encAllZero);

                    // 3. Mutate AST: Set last shape parent to -2 to ensure it's evaluated as a NON_MARK_DATA
                    bool mutResult = NativeMethods.SetDjvuJb2ShapeParent(ptrJB2Img, lastShape, -2);
                    Assert.True(mutResult, "NativeMethods.SetDjvuJb2ShapeParent failed.");

                    // 4. Encode mutated (NON_MARK_DATA bitstream)
                    bool encMutated = NativeMethods.EncodeDjvuJb2ImageToChunk(ptrJB2Img, out outDataNonMark, out int outSizeNonMark);
                    Assert.True(encMutated);

                    byte[] allZeroChunk = new byte[outSizeAllZero];
                    Marshal.Copy(outDataAllZero, allZeroChunk, 0, outSizeAllZero);

                    byte[] nonMarkChunk = new byte[outSizeNonMark];
                    Marshal.Copy(outDataNonMark, nonMarkChunk, 0, outSizeNonMark);

                    // 5. Write out the NonMarkData chunk to the artifact path
                    string nonMarkFileName = Path.GetFileNameWithoutExtension(sjbzFileName) + "_nonmark" + Path.GetExtension(sjbzFileName);
                    string nonMarkPath = Path.Combine(Util.ArtifactsDataPath, "extracted", nonMarkFileName);
                    File.WriteAllBytes(nonMarkPath, nonMarkChunk);

                    // 6. Compare buffers with Util.ImageBinaryDiff
                    // If diff == 0.0 AND sizes match, the bitstreams are functionally identical
                    int compareLength = Math.Min(outSizeAllZero, outSizeNonMark);
                    fixed (byte* pAllZero = allZeroChunk)
                    fixed (byte* pNonMark = nonMarkChunk)
                    {
                        double diff = Util.ImageBinaryDiff(pAllZero, pNonMark, compareLength, 1, compareLength, 8, 8);
                        string msg = $"Bitstreams - diff: {diff:F4}. NON_MARK_DATA record type was encoded: {diff != 0.0 || outSizeAllZero != outSizeNonMark} - shape count {shapeCount}," +
                                        $" inherited shape count {inheritedShapeCount}. AllZero size {outSizeAllZero}, NonMark size {outSizeNonMark}";
                        Assert.False(diff == 0.0 && outSizeAllZero == outSizeNonMark, msg);
                        Console.WriteLine(msg);
                    }
                }
            }
            finally
            {
                if (outDataAllZero != IntPtr.Zero)
                    DjvuMarshal.FreeHGlobal(outDataAllZero);
                if (outDataNonMark != IntPtr.Zero)
                    DjvuMarshal.FreeHGlobal(outDataNonMark);
                if (ptrJB2Img != IntPtr.Zero)
                    NativeMethods.FreeDjvuJb2Image(ptrJB2Img);
            }
        }
    }
}
