using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DjvuNet;
using DjvuNet.Compression;
using DjvuNet.DataChunks;
using DjvuNet.DjvuLibre;
using DjvuNet.Graphics;
using DjvuNet.JB2;
using DjvuNet.Tests;
using Xunit;

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
            skipChunks: new string[]
            {
                "test001C_P19.sjbz", "test001C_P21.sjbz", "test001C_P22.sjbz", "test001C_P23.sjbz", "test001C_P29.sjbz",
                "test002C_P08.sjbz", "test002C_P09.sjbz", "test003C_P04.sjbz", "test003C_P14.sjbz", "test003C_P20.sjbz",
                "test003C_P21.sjbz", "test003C_P34.sjbz", "test003C_P38.sjbz", "test003C_P59.sjbz", "test003C_P63.sjbz",
                "test003C_P68.sjbz", "test003C_P92.sjbz", "test003C_P93.sjbz", "test003C_P94.sjbz", "test003C_P98.sjbz",
                "test004C_P09.sjbz", "test008C_P10.sjbz", "test024C_P13.sjbz", "test027C_P06.sjbz", "test031C_P05.sjbz",
                "test031C_P07.sjbz", "test036C_P02.sjbz", "test036C_P03.sjbz", "test036C_P08.sjbz", "test036C_P09.sjbz",
                "test037C_P02.sjbz", "test038C_P11.sjbz", "test039C_P12.sjbz", "test039C_P17.sjbz", "test040C_P12.sjbz",
                "test040C_P17.sjbz", "test040C_P23.sjbz", "test040C_P24.sjbz", "test040C_P25.sjbz", "test040C_P26.sjbz",
                "test040C_P28.sjbz", "test040C_P33.sjbz", "test040C_P34.sjbz", "test040C_P36.sjbz", "test040C_P37.sjbz",
                "test040C_P38.sjbz", "test040C_P39.sjbz", "test040C_P40.sjbz", "test040C_P44.sjbz", "test040C_P45.sjbz",
                "test040C_P47.sjbz", "test040C_P51.sjbz", "test040C_P54.sjbz", "test045C_P04.sjbz", "test045C_P08.sjbz",
                "test045C_P11.sjbz", "test045C_P12.sjbz", "test045C_P13.sjbz", "test045C_P14.sjbz", "test045C_P15.sjbz",
                "test045C_P16.sjbz", "test045C_P18.sjbz", "test045C_P21.sjbz", "test045C_P25.sjbz", "test045C_P28.sjbz",
                "test045C_P32.sjbz", "test045C_P34.sjbz", "test045C_P37.sjbz", "test045C_P39.sjbz", "test045C_P46.sjbz",
                "test045C_P51.sjbz", "test045C_P54.sjbz", "test045C_P57.sjbz", "test045C_P59.sjbz", "test045C_P62.sjbz",
                "test045C_P64.sjbz", "test045C_P66.sjbz", "test045C_P68.sjbz", "test045C_P69.sjbz", "test045C_P70.sjbz",
                "test045C_P73.sjbz", "test045C_P76.sjbz", "test045C_P77.sjbz", "test046C_P02.sjbz", "test046C_P04.sjbz",
                "test046C_P11.sjbz", "test046C_P16.sjbz", "test048C_P08.sjbz", "test050C_P04.sjbz", "test050C_P07.sjbz",
                "test050C_P08.sjbz", "test050C_P09.sjbz", "test050C_P10.sjbz", "test050C_P12.sjbz", "test050C_P13.sjbz",
                "test050C_P15.sjbz", "test050C_P16.sjbz", "test050C_P17.sjbz", "test050C_P18.sjbz", "test050C_P20.sjbz",
                "test051C_P01.sjbz", "test051C_P05.sjbz", "test051C_P11.sjbz", "test051C_P13.sjbz", "test052C_P03.sjbz",
                "test052C_P19.sjbz", "test053C_P03.sjbz", "test053C_P09.sjbz", "test053C_P12.sjbz", "test053C_P14.sjbz",
                "test056C_P30.sjbz", "test057C_P08.sjbz", "test057C_P12.sjbz", "test057C_P18.sjbz", "test057C_P20.sjbz",
                "test057C_P22.sjbz", "test059C_P03.sjbz", "test059C_P05.sjbz", "test059C_P07.sjbz", "test059C_P11.sjbz",
                "test059C_P21.sjbz", "test059C_P25.sjbz", "test059C_P34.sjbz", "test059C_P59.sjbz", "test059C_P61.sjbz",
                "test059C_P63.sjbz", "test059C_P68.sjbz", "test059C_P72.sjbz", "test059C_P73.sjbz", "test059C_P74.sjbz",
                "test059C_P79.sjbz", "test059C_P81.sjbz", "test061C_P04.sjbz", "test061C_P07.sjbz", "test061C_P09.sjbz",
                "test061C_P10.sjbz", "test061C_P12.sjbz", "test061C_P14.sjbz", "test061C_P16.sjbz", "test061C_P19.sjbz",
                "test061C_P21.sjbz", "test061C_P22.sjbz", "test061C_P23.sjbz", "test061C_P24.sjbz", "test061C_P26.sjbz",
                "test061C_P28.sjbz", "test061C_P33.sjbz", "test061C_P38.sjbz", "test061C_P40.sjbz", "test061C_P43.sjbz",
                "test061C_P45.sjbz", "test061C_P47.sjbz", "test061C_P49.sjbz", "test061C_P51.sjbz", "test061C_P55.sjbz",
                "test061C_P57.sjbz", "test061C_P59.sjbz", "test061C_P61.sjbz", "test061C_P63.sjbz", "test061C_P65.sjbz",
                "test061C_P68.sjbz", "test061C_P69.sjbz", "test061C_P71.sjbz", "test061C_P73.sjbz", "test061C_P74.sjbz",
                "test061C_P75.sjbz", "test061C_P76.sjbz", "test061C_P78.sjbz", "test061C_P80.sjbz", "test061C_P81.sjbz",
                "test061C_P83.sjbz", "test061C_P85.sjbz", "test061C_P89.sjbz", "test061C_P91.sjbz", "test061C_P93.sjbz",
                "test061C_P94.sjbz", "test061C_P95.sjbz", "test061C_P98.sjbz", "test061C_P99.sjbz", "test061C_P101.sjbz",
                "test061C_P103.sjbz", "test061C_P105.sjbz", "test061C_P107.sjbz", "test061C_P109.sjbz", "test061C_P111.sjbz",
                "test061C_P113.sjbz", "test061C_P114.sjbz", "test061C_P116.sjbz", "test061C_P117.sjbz", "test061C_P122.sjbz",
                "test061C_P123.sjbz", "test061C_P126.sjbz", "test062C_P18.sjbz", "test062C_P30.sjbz", "test068C_P02.sjbz",
                "test068C_P13.sjbz", "test070C_P03.sjbz", "test070C_P06.sjbz", "test070C_P24.sjbz", "test070C_P26.sjbz",
                "test071C_P09.sjbz", "test071C_P13.sjbz", "test071C_P25.sjbz", "test072C_P08.sjbz", "test072C_P14.sjbz",
                "test072C_P15.sjbz", "test072C_P33.sjbz", "test074C_P01.sjbz",
            },
            coverage: TestCoverage.UniqueOnly
        );

        [Theory]
        [MemberData(nameof(JB2ImageTestData))]
        //[InlineData("extracted\\test071C_D171878.djbz", "extracted\\test071C_P09.sjbz")]
        public unsafe void DecodeTest(string djbzFileName, string sjbzFileName)
        {

            string sjbzFilePath = Path.Combine(Util.ArtifactsDataPath, sjbzFileName);
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

                // 2. Extract Native Bitmap matching C#'s

                bool getBmpInfo = NativeMethods.GetDjvuJb2ImageBitmap(nativeImage, 4, out int nWidth, out int nHeight, out int nRowSize, out int nBorder, IntPtr.Zero, 0);
                Assert.True(getBmpInfo, "Native GetDjvuJb2ImageBitmap dimension query failed.");

                Assert.True(nWidth > 0, "Native decoded image has 0 width.");
                Assert.True(nHeight > 0, "Native decoded image has 0 height.");

                int bufferSize = Util.CalculateBufferSize(nHeight, nRowSize, nBorder);
                byte[] nativeBuffer = new byte[bufferSize];

                bool getBmpData = false;
                fixed (byte* pNative = nativeBuffer)
                {
                    getBmpData = NativeMethods.GetDjvuJb2ImageBitmap(nativeImage, 4, out nWidth, out nHeight, out nRowSize, out nBorder, (IntPtr)pNative, nativeBuffer.Length);
                }
                Assert.True(getBmpData, "Native GetDjvuJb2ImageBitmap extraction failed.");

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

                Bitmap managedBitmap = jb2Image.GetBitmap();

                Assert.Equal(managedBitmap.Width, nWidth);
                Assert.Equal(managedBitmap.Height, nHeight);
                Assert.Equal(managedBitmap.BytesPerRow, nRowSize);
                Assert.Equal(managedBitmap.Border, nBorder);

                    // 4. Structural Binary Equivalence Verification (1 byte per pixel for 8bpp mask)
                    // ImageBinaryDiff inherently skips memory padding since strides match.
                    unsafe
                    {
                        fixed (byte* pNative = nativeBuffer)
                        fixed (sbyte* pManagedData = managedBitmap.Data)
                        {
                            double diff = Util.ImageBinaryDiff(pNative, ((byte*)pManagedData + managedBitmap.Border), nWidth, nHeight, nRowSize, 8);

                            if (diff > 0.0)
                            {
                                lock (_fixture.Diffs)
                                {
                                    _fixture.Diffs[sjbzFileName] = (djbzFileName, sjbzFileName, diff, nWidth, nHeight, nBorder, nRowSize);
                                }
                            }

                            string msg = $"JB2Image compatibility mask match with Width: {nWidth}, Height: {nHeight}, Border: {nBorder}, BytePerRow {nRowSize}, Diff ratio: {diff:F4}";
                            Assert.True(diff == 0.0, msg);
                            // Console.WriteLine(msg);
                        }
                    }
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
