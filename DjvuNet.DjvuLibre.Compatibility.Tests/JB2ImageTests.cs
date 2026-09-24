using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using DjvuNet;
using DjvuNet.Compression;
using DjvuNet.DataChunks;
using DjvuNet.DjvuLibre;
using DjvuNet.Extensions;
using DjvuNet.Graphics;
using DjvuNet.JB2;
using DjvuNet.Tests;
using Microsoft.Testing.Platform.Extensions.Messages;
using Xunit;
using SysBitmap = System.Drawing.Bitmap;

namespace DjvuNet.DjvuLibre.Compatibility.Tests
{
    /// <summary>
    /// A test fixture for managing the lifecycle of JB2 image tests, including caching diffs and cleaning up native resources.
    /// </summary>
    public class JB2ImageTestsFixture : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Compares SJBZ filenames to ensure a deterministic sorting order for chunks during tests.
        /// </summary>
        private class SjbzNameComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                string[] partsX = x.Split(new[] { "_P" }, StringSplitOptions.None);
                string[] partsY = y.Split(new[] { "_P" }, StringSplitOptions.None);

                if (partsX.Length == 2 && partsY.Length == 2)
                {
                    int cmp = string.CompareOrdinal(partsX[0], partsY[0]);
                    if (cmp != 0) return cmp;

                    string numXStr = partsX[1].Replace(".sjbz", "");
                    string numYStr = partsY[1].Replace(".sjbz", "");

                    if (int.TryParse(numXStr, out int numX) && int.TryParse(numYStr, out int numY))
                    {
                        return numX.CompareTo(numY);
                    }
                }

                return string.CompareOrdinal(x, y);
            }
        }

        public SortedDictionary<string, (string DjbzFileName, string SjbzFileName, double Diff, int Width, int Height, int Border, int RowSize)> Diffs { get; }
            = new SortedDictionary<string, (string, string, double, int, int, int, int)>(new SjbzNameComparer());

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                List<(string DjbzFileName, string SjbzFileName, double Diff, int Width, int Height, int Border, int RowSize)> diffsToDump = Diffs.Values.Where(d => d.Diff > 0.0).ToList();
                if (diffsToDump.Count > 0)
                {
                    string basePath = Path.Combine(Util.ArtifactsDataPath, "extracted");
                    string pathReport = Path.Combine(basePath, "failing_tests_chunks.txt");
                    string pathInlineData = Path.Combine(basePath, "failing_tests_inlinedata.txt");

                    using (FileStream fs = File.Open(pathReport, FileMode.Create))
                    using (StreamWriter bw = new StreamWriter(fs, Encoding.UTF8))
                    using (FileStream fsInline = File.Open(pathInlineData, FileMode.Create))
                    using (StreamWriter bwInline = new StreamWriter(fsInline, Encoding.UTF8))
                    {
                        bw.WriteLine($"Found {diffsToDump.Count} mismatches:");
                        foreach ((string DjbzFileName, string SjbzFileName, double Diff, int Width, int Height, int Border, int RowSize) d in diffsToDump)
                        {
                            string djbz = d.DjbzFileName == null ? "null" : $"\"{d.DjbzFileName}\"";
                            string sjbz = $"\"{Path.GetFileName(d.SjbzFileName)}\"";
                            bw.WriteLine($"Diff: {d.Diff:E4} | Width: {d.Width}, Height: {d.Height}, Border: {d.Border}, RowSize: {d.RowSize} | Chunks: {djbz}, {sjbz}");

                            string djbzInline = d.DjbzFileName == null ? "null" : $"\"{d.DjbzFileName.Replace("\\", "\\\\")}\"";
                            string sjbzInline = $"\"{d.SjbzFileName.Replace("\\", "\\\\")}\"";
                            bwInline.WriteLine($"[InlineData({djbzInline}, {sjbzInline})]");
                        }

                        bw.WriteLine("\nC# Array format for exclusions:");
                        for (int i = 0; i < diffsToDump.Count; i++)
                        {
                            bw.Write($"\"{Path.GetFileName(diffsToDump[i].SjbzFileName)}\", ");
                            if ((i + 1) % 5 == 0) bw.WriteLine();
                        }
                        bw.WriteLine();
                    }
                }
            }

            _disposed = true;
        }
    }

    public class JB2ImageTests : IClassFixture<JB2ImageTestsFixture>
    {
        private readonly JB2ImageTestsFixture _fixture;

        public JB2ImageTests(JB2ImageTestsFixture fixture)
        {
            _fixture = fixture;
        }

        public static IEnumerable<object[]> JB2ImageTestData => Util.GetJB2ImageTestData(
            skipDocs: new int[] { },
            skipChunks: new string[] { },
            coverage: TestCoverage.UniqueOnly
        );

        [Theory]
        [InlineData("extracted/test003C_D453132.djbz", "extracted/test003C_P53.sjbz")]
        [InlineData("extracted/test003C_D453132.djbz", "extracted/test003C_P54.sjbz")]
        public void Decode_Tokens6And8(string djbzFileName, string sjbzFileName)
        {
            DecodeInternal(djbzFileName, sjbzFileName);
        }

        [Theory]
        [InlineData("extracted/test002C_D1868.djbz", "extracted/test002C_P02_nonmark.sjbz")]
        [InlineData("extracted/test002C_D1868.djbz", "extracted/test002C_P03_nonmark.sjbz")]
        [InlineData("extracted/test003C_D1030.djbz", "extracted/test003C_P07_nonmark.sjbz")]
        [InlineData("extracted/test003C_D1030.djbz", "extracted/test003C_P09_nonmark.sjbz")]
        [InlineData("extracted/test003C_D76090.djbz", "extracted/test003C_P11_nonmark.sjbz")]
        public void Decode_AllZero_NonMarkData(string djbzFileName, string sjbzFileName)
        {
            DecodeInternal(djbzFileName, sjbzFileName);
        }

        [Theory]
        [MemberData(nameof(JB2ImageTestData))]
        public unsafe void DecodeTest(string djbzFileName, string sjbzFileName)
        {
            DecodeInternal(djbzFileName, sjbzFileName);
        }

        private unsafe void DecodeInternal(string djbzFileName, string sjbzFileName)
        {

            string sjbzFilePath = Path.Combine(Util.ArtifactsDataPath, sjbzFileName);
            byte[] djbzData = djbzFileName != null ? File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName)) : null;
            byte[] sjbzData = File.ReadAllBytes(sjbzFilePath);

            byte[] djbzPayload = djbzData;
            byte[] sjbzPayload = sjbzData;

            // 3. Managed Decoding
            JB2Dictionary djbzDict = null;
            if (djbzPayload != null)
            {
                djbzDict = new JB2Dictionary();
                using (var ms = new MemoryStream(djbzPayload))
                using (var reader = new DjvuReader(ms))
                {
                    djbzDict.Decode(reader);
                }
            }

            var jb2Image = new JB2Image();

            using (var ms = new MemoryStream(sjbzPayload))
            using (var reader = new DjvuReader(ms))
            {
                jb2Image.Decode(reader, djbzDict);
            }

            Assert.True(jb2Image.ShapeCount > 0, "Failed to decode JB2 data from chunks.");

            Bitmap decoded = jb2Image.GetBitmap(1, 4);

            Bitmap oracle = default(Bitmap);

            using (FileStream oracleStream = File.OpenRead(sjbzFilePath.Replace(".sjbz", ".rle")))
            {
                oracle = Bitmap.CreateBitmap(oracleStream, decoded.Border);
            }

            Assert.NotNull(oracle.Data);

            Assert.True(oracle.Width == decoded.Width, $"Width values differ: {oracle.Width} != {decoded.Width}");
            Assert.True(oracle.Height == decoded.Height, $"Width values differ: {oracle.Height} != {decoded.Height}");
            Assert.True(oracle.Border == decoded.Border, $"Border values differ: oracle Bitmap {oracle.Border} != decoded Bitmap {decoded.Border}");
            Assert.True(oracle.BytesPerRow == decoded.BytesPerRow, $"BytesPerRow values differ: {oracle.BytesPerRow} != {decoded.BytesPerRow}");

            (bool result, double diff) = Util.ImageBinarySimilarity(ref oracle, ref decoded);

            if (!result)
            {
                lock (_fixture.Diffs)
                {
                    _fixture.Diffs[sjbzFileName] = (djbzFileName, sjbzFileName, diff, oracle.Width, oracle.Height, oracle.Border, oracle.BytesPerRow);
                }

                Util.DumpImageMismatch(ref oracle, ref decoded, 0);
            }

            string msg = $"JB2Image compatibility mask match with Width: {oracle.Width}, Height: {oracle.Height}, Border: {oracle.Border}, BytePerRow {oracle.BytesPerRow}, Diff ratio: {diff:F10}";
            Assert.True(diff == 0.0, msg);
        }

        public static IEnumerable<object[]> JB2ImageAllData => Util.GetJB2ImageTestData(
                    skipDocs: new int[] { },
                    skipChunks: new string[]
                    {}, coverage: TestCoverage.All);


        [Theory(Skip = "Dumps RLE encoded GBitmap Masks using function NativeMethods.GetJb2ImageRleMask")]
        [MemberData(nameof(JB2ImageAllData))]
        [InlineData("extracted/test002C_D1868.djbz", "extracted/test002C_P02_nonmark.sjbz")]
        [InlineData("extracted/test002C_D1868.djbz", "extracted/test002C_P03_nonmark.sjbz")]
        [InlineData("extracted/test003C_D1030.djbz", "extracted/test003C_P07_nonmark.sjbz")]
        [InlineData("extracted/test003C_D1030.djbz", "extracted/test003C_P09_nonmark.sjbz")]
        [InlineData("extracted/test003C_D76090.djbz", "extracted/test003C_P11_nonmark.sjbz")]
        public unsafe void ExtractNativeMaskBitmapRle(string djbzFileName, string sjbzFileName)
        {
            string sjbzFilePath = Path.Combine(Util.ArtifactsDataPath, sjbzFileName);
            string rleFileName = Path.GetFileNameWithoutExtension(sjbzFilePath);
            string rleFilePath = Path.Combine(Util.ArtifactsDataPath, "extracted", rleFileName + ".rle");

            if (File.Exists(rleFilePath))
                return;

            byte[] djbzData = djbzFileName != null ? File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName)) : null;
            byte[] sjbzData = File.ReadAllBytes(sjbzFilePath);

            byte[] djbzPayload = djbzData;
            byte[] sjbzPayload = sjbzData;

            IntPtr nativeDict = IntPtr.Zero;
            IntPtr nativeImage = IntPtr.Zero;
            IntPtr nativeBitmap = IntPtr.Zero;

            try
            {
                // 1. Native Decoding (Oracle)
                unsafe
                {
                    if (djbzPayload == null)
                    {
                        fixed (byte* pSjbz = sjbzPayload)
                        {
                            bool imgResult = NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pSjbz, sjbzPayload.Length, IntPtr.Zero, 0, out nativeImage);

                            if (!imgResult)
                            {
                                string nativeError = NativeMethods.GetLastError();
                                Assert.Fail($"Native CreateDjvuJb2ImageFromChunk failed. Native Error: {nativeError}");
                            }
                        }
                    }
                    else
                    {
                        fixed (byte* pSjbz = sjbzPayload)
                        fixed (byte* pDjbz = djbzPayload)
                        {
                            bool imgResult = NativeMethods.CreateDjvuJb2ImageFromChunk((IntPtr)pSjbz, sjbzPayload.Length, (IntPtr)pDjbz, djbzPayload.Length, out nativeImage);

                            if (!imgResult)
                            {
                                string nativeError = NativeMethods.GetLastError();
                                Assert.Fail($"Native CreateDjvuJb2ImageFromChunk failed. Native Error: {nativeError}");
                            }
                        }
                    }


                }

                // We have JB2Image handle and we can extract GBitmap data and geometry in RLE format

                int rleMaskSize = 0;

                // 1. Get the RLE buffer size from the native JB2Image
                NativeMethods.GetJb2ImageRleMask(nativeImage, 1, 1, IntPtr.Zero, 0, out rleMaskSize);

                // 2. Create buffer to hold RLE compressed data
                byte[] rleBuffer = GC.AllocateUninitializedArray<byte>(rleMaskSize, true);
                byte* rleBufferPtr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(rleBuffer));
                NativeMethods.GetJb2ImageRleMask(nativeImage, 1, 1, (nint)rleBufferPtr, rleMaskSize, out _);

                using (FileStream fs = File.OpenWrite(rleFilePath))
                {
                    fs.Write(rleBuffer, 0, rleBuffer.Length);
                    fs.Flush();
                }

                Assert.True(File.Exists(rleFilePath));
                Console.WriteLine($"Written {rleFileName}");
               
            }
            finally
            {
                if (nativeImage != IntPtr.Zero)
                    NativeMethods.FreeDjvuJb2Image(nativeImage);
            }
        }

        private static byte[] StripChunkHeader(byte[] data)
        {
            if (data == null || data.Length < 8) return data;

            // Check if it starts with "Djbz" or "Sjbz"
            if ((data[0] == 'D' || data[0] == 'S') && data[1] == 'j' && data[2] == 'b' && data[3] == 'z')
            {
                byte[] payload = new byte[data.Length - 8];
                Buffer.BlockCopy(data, 8, payload, 0, payload.Length);
                return payload;
            }
            return data;
        }
    }
}
