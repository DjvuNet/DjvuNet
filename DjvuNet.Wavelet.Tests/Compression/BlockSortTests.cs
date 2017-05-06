using Xunit;
using DjvuNet.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Tests.Xunit;
using DjvuNet.Tests;
using System.IO;

namespace DjvuNet.Compression.Tests
{
    public class BlockSortTests
    {
        public static IEnumerable<object[]> StringTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                var data = Util.TestUnicodeStrings;
                foreach (String s in data)
                    retVal.Add(new object[] { s });

                return retVal;
            }
        }

        public static IEnumerable<object[]> StringEncodingTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                Encoding[] encodings = new Encoding[]
                {
                    new UTF7Encoding(false),
                    new UTF7Encoding(true),
                    new UTF8Encoding(false),
                    new UTF8Encoding(true),
                    new UnicodeEncoding(false, false),
                    //new UnicodeEncoding(true, false),
                    new UnicodeEncoding(false, true),
                    //new UnicodeEncoding(true, true),
                    new UTF32Encoding(false, false),
                    //new UTF32Encoding(true, false),
                    new UTF32Encoding(false, true),
                    //new UTF32Encoding(true, true)
                };

                var data = StringTestData;

                foreach (Encoding e in encodings)
                {
                    foreach (object[] o in data)
                    {
                        retVal.Add(new object[] { e, o[0] });
                    }
                }

                return retVal;
            }
        }

        public static IEnumerable<object[]> StringEncodingCrashTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                Encoding[] encodings = new Encoding[]
                {
                    new UnicodeEncoding(true, false),
                    new UnicodeEncoding(true, true),
                    new UTF32Encoding(true, false),
                    new UTF32Encoding(true, true)
                };

                var data = StringTestData;

                foreach (Encoding e in encodings)
                {
                    foreach (object[] o in data)
                    {
                        string testStr = (string) o[0];

                        if (testStr.StartsWith("免去于革") || testStr.StartsWith("על-פי ")
                            || testStr.StartsWith("ホテル近く") || testStr.StartsWith("Konservatiivipuolueen"))
                            continue;

                        retVal.Add(new object[] { e, testStr });
                    }
                }

                return retVal;
            }
        }

        public static IEnumerable<object[]> BlockSortValidateData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { Path.Combine(Util.ArtifactsDataPath, "DjvuNet.pdb"), Path.Combine(Util.ArtifactsDataPath, "DjvuNet.pdb.bsr") });
                retVal.Add(new object[] { Path.Combine(Util.ArtifactsPath, "test001C.json"), Path.Combine(Util.ArtifactsDataPath, "test001C.json.bsr") });
                retVal.Add(new object[] { Path.Combine(Util.ArtifactsPath, "test003C.json"), Path.Combine(Util.ArtifactsDataPath, "test003C.json.bsr") });
                retVal.Add(new object[] { Path.Combine(Util.ArtifactsPath, "test023C.json"), Path.Combine(Util.ArtifactsDataPath, "test023C.json.bsr") });
                retVal.Add(new object[] { Path.Combine(Util.ArtifactsPath, "test038C.json"), Path.Combine(Util.ArtifactsDataPath, "test038C.json.bsr") });
                retVal.Add(new object[] { Path.Combine(Util.ArtifactsPath, "test039C.json"), Path.Combine(Util.ArtifactsDataPath, "test039C.json.bsr") });
                retVal.Add(new object[] { Path.Combine(Util.ArtifactsPath, "test040C.json"), Path.Combine(Util.ArtifactsDataPath, "test040C.json.bsr") });
                retVal.Add(new object[] { Path.Combine(Util.ArtifactsPath, "test043C.json"), Path.Combine(Util.ArtifactsDataPath, "test043C.json.bsr") });
                retVal.Add(new object[] { Path.Combine(Util.ArtifactsPath, "test045C.json"), Path.Combine(Util.ArtifactsDataPath, "test045C.json.bsr") });
                retVal.Add(new object[] { Path.Combine(Util.ArtifactsPath, "test058C.json"), Path.Combine(Util.ArtifactsDataPath, "test058C.json.bsr") });
                retVal.Add(new object[] { Path.Combine(Util.ArtifactsPath, "test063C.json"), Path.Combine(Util.ArtifactsDataPath, "test063C.json.bsr") });
                retVal.Add(new object[] { Path.Combine(Util.ArtifactsPath, "test070C.json"), Path.Combine(Util.ArtifactsDataPath, "test070C.json.bsr") });
                retVal.Add(new object[] { Path.Combine(Util.ArtifactsPath, "test075C.json"), Path.Combine(Util.ArtifactsDataPath, "test075C.json.bsr") });
                retVal.Add(new object[] { Path.Combine(Util.ArtifactsPath, "test077C.json"), Path.Combine(Util.ArtifactsDataPath, "test077C.json.bsr") });

                return retVal;
            }
        }

        [Fact()]
        public void BlockSortTest001()
        {
            BlockSort bSort = new BlockSort();
            int markerpos = 0;
            Assert.Throws<InvalidOperationException>(() => bSort.Sort(ref markerpos));
        }

        [Fact()]
        public void BlockSortTest002()
        {
            Assert.Throws<ArgumentOutOfRangeException>("psize", () => new BlockSort(new byte[1], 0));
        }

        [Fact()]
        public void BlockSortTest003()
        {
            Assert.Throws<ArgumentOutOfRangeException>("psize", () => new BlockSort(new byte[1], 0x1000000));
        }

        [Fact()]
        public void BlockSortTest004()
        {
            Assert.Throws<ArgumentNullException>("pdata", () => new BlockSort(null, 1));
        }

        [Fact()]
        public void BlockSortTest005()
        {
            Assert.Throws<ArgumentOutOfRangeException>("pdata", () => new BlockSort(new byte[0], 10));
        }

        [Fact()]
        public void BlockSortTest006()
        {
            BlockSort bSort = new BlockSort(new byte[10], 10);
        }

        [Fact()]
        public void BlockSortTest007()
        {
            BlockSort bSort = new BlockSort(new byte[] { 5 }, 1);
            int markerpos = 1;
            bSort.Sort(ref markerpos);
        }

        [Fact()]
        public void BlockSortDataTest001()
        {
            int markpos = 0;
            Assert.Throws<ArgumentOutOfRangeException>("psize", () => BlockSort.BlockSortData(new byte[1], 10, ref markpos));
        }

        [Fact()]
        public void BlockSortDataTest002()
        {
            int markpos = 0;
            Assert.Throws<InvalidOperationException>(() => BlockSort.BlockSortData(new byte[] { 1, 2, 3, 4, 5 }, 5, ref markpos));
        }

        [Fact()]
        public void BlockSortDataTest003()
        {
            int markpos = 0;
            byte[] buffer = new byte[] { 41, 32, 43, 44, 45, 46, 47, 38, 39, 10, 20, 10, 20, 234, 0 };
            BlockSort.BlockSortData(buffer, buffer.Length, ref markpos);
        }

        [Fact()]
        public void BlockSortDataTest005()
        {
            int markpos = 13;
            byte[] buffer = Util.ReadFileToEnd(Path.Combine(Util.RepoRoot, "artifacts", "data", "testhello.obz"));
            byte[] data = new byte[1024];
            Buffer.BlockCopy(buffer, 0, data, 0, buffer.Length);
            BlockSort.BlockSortData(data, buffer.Length + 1, ref markpos);

            byte[] expected = new byte[] { 0x0a, 0x0d, 0x20, 0x21, 0x6f, 0x7a, 0x00, 0x20, 0x48, 0x65, 0x6c, 0x6c, 0x7a, 0x62 };
            for (int i = 0; i < expected.Length; i++)
                Assert.Equal<byte>(expected[i], data[i]);
        }

        [DjvuTheory]
        [MemberData(nameof(StringTestData))]
        public void BlockSortData_Theory01(string testStr)
        {
            UTF8Encoding enc = new UTF8Encoding(false);
            byte[] buffer = new byte[enc.GetByteCount(testStr) + 1];
            byte[] byteStr = enc.GetBytes(testStr);
            Buffer.BlockCopy(byteStr, 0, buffer, 0, byteStr.Length);
            int markpos = byteStr.Length;

            BlockSort.BlockSortData(buffer, buffer.Length, ref markpos);
        }
 
        [DjvuTheory]
        [MemberData(nameof(StringEncodingTestData))]
        public void BlockSortData_Theory02(Encoding enc, string testStr)
        {
            // TODO Find reason for failing tests with Big Endian multibyte
            // text encodings - except for some Chinese samples 
            // (probably not enough 0x00s as these may be 4 byte unicode characters)

            byte[] buffer;
            int markpos;
            PrepareTestData(enc, testStr, out buffer, out markpos);

            BlockSort.BlockSortData(buffer, buffer.Length, ref markpos);
        }

        [DjvuTheory, Trait("Category", "BugTrack")]
        [MemberData(nameof(StringEncodingCrashTestData))]
        public void BlockSortData_Theory03(Encoding enc, string testStr)
        {

            byte[] buffer;
            int markpos;
            PrepareTestData(enc, testStr, out buffer, out markpos);

            Assert.Throws<IndexOutOfRangeException>(
                () => BlockSort.BlockSortData(buffer, buffer.Length, ref markpos));
        }

        private static void PrepareTestData(Encoding enc, string testStr, out byte[] buffer, out int markpos)
        {
            buffer = new byte[enc.GetByteCount(testStr) + 1];
            byte[] byteStr = enc.GetBytes(testStr);
            Buffer.BlockCopy(byteStr, 0, buffer, 0, byteStr.Length);
            markpos = byteStr.Length;
        }

        [DjvuTheory]
        [MemberData(nameof(BlockSortValidateData))]
        public void BlockSortValidate_Theory(string source, string expected)
        {
            byte[] sourceData = Util.ReadFileToEnd(source);
            byte[] expectedData = Util.ReadFileToEnd(expected);
            byte[] buffer = new byte[sourceData.Length + 1];
            Buffer.BlockCopy(sourceData, 0, buffer, 0, sourceData.Length);
            int markpos = sourceData.Length;
            BlockSort.BlockSortData(buffer, buffer.Length, ref markpos);

            for (int i = 0; i < expectedData.Length; i++)
                Assert.Equal<uint>(expectedData[i], buffer[i]);
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void SortTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ValueSwapTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GTTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GTDTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void RankSortTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void Pivot3rTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void QuickSort3rTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void Pivot3dTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void QuickSort3dTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void RadixSort16Test()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void RadixSort8Test()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}