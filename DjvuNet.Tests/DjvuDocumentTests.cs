using DjvuNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Moq;
using Xunit;
using DjvuNet.DataChunks;
using System.Linq;

namespace DjvuNet.Tests
{
    public class DjvuDocumentTests
    {

        public static IEnumerable<object[]> DjvuArtifacts
        {
            get
            {
                int maxTestNumber = 77;
                string filePathTempl = Util.GetTestFilePathTemplate();

                List<object[]> retVal = new List<object[]>
                {
                };

                GenerateTestFilesPaths(maxTestNumber, filePathTempl, retVal);

                return retVal;
            }
        }

        public static IEnumerable<object[]> DjvuArtifactsWithErrors
        {
            get
            {
                int maxTestNumber = 75;
                string filePathTempl = Util.GetTestFilePathTemplate();

                List<object[]> retVal = new List<object[]>
                {
                     new object[] { null, 62},
                     new object[] { null, 107},
                     new object[] { null, 300},
                     new object[] { null, 494},
                     new object[] { null, 286},
                     new object[] { null, 348},
                     new object[] { null, 186},
                     new object[] { null, 427},
                     new object[] { null, 274},
                     new object[] { null, 223},
                     new object[] { null, 154},
                     new object[] { null, 239},
                     new object[] { null, 9},
                     new object[] { null, 20},
                     new object[] { null, 40},
                     new object[] { null, 30},
                     new object[] { null, 12},
                     new object[] { null, 7},
                     new object[] { null, 28},
                     new object[] { null, 5},
                     new object[] { null, 12},
                     new object[] { null, 10},
                     new object[] { null, 3},
                     new object[] { null, 3},
                     new object[] { null, 9},
                     new object[] { null, 146},
                     new object[] { null, 173},
                     new object[] { null, 267},
                     new object[] { null, 323},
                     new object[] { null, 1},
                };

                GenerateTestFilesPaths(maxTestNumber, filePathTempl, retVal);

                return retVal;
            }
        }

        public static void GenerateTestFilesPaths(int maxTestNumber, string filePathTempl, List<object[]> retVal)
        {
            int i = 1;

            for (; i <= maxTestNumber; i++)
            {
                int j = i + 1;
                retVal.Add(
                    new object[] {
                        Util.GetTestFilePath(i),
                        Util.GetTestDocumentPageCount(i) });
            }
        }

        [Theory]
        [MemberData(nameof(DjvuArtifacts))]
        public void IsDjvuDocument_String_Theory(string filePath, int pageCount)
        {
            Assert.True(DjvuDocument.IsDjvuDocument(filePath));
        }

        [Theory]
        [MemberData(nameof(DjvuArtifacts))]
        public void ctor_Theory(string filePath, int pageCount)
        {
            DjvuDocument document = null;
            try
            {
                document = new DjvuDocument(filePath);
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
            catch (Exception error)
            {
                Util.FailOnException(error, $"\nDjvuDocument_ctor failed. File: {filePath}, Page count: {pageCount}");
            }
            finally
            {
                document?.Dispose();
            }

            Thread.Yield();
            Thread.Sleep(500);
        }

        [Fact]
        public void ctor001()
        {
            int pageCount = Util.GetTestDocumentPageCount(1);
            using (DjvuDocument document = Util.GetTestDocument(1, out pageCount))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor002()
        {
            int pageCount = Util.GetTestDocumentPageCount(2);
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor003()
        {
            int pageCount = Util.GetTestDocumentPageCount(3);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test003C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor004()
        {
            int pageCount = Util.GetTestDocumentPageCount(4);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test004C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor005()
        {
            int pageCount = Util.GetTestDocumentPageCount(5);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test005C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor006()
        {
            int pageCount = Util.GetTestDocumentPageCount(6);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test006C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor007()
        {
            int pageCount = Util.GetTestDocumentPageCount(7);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test007C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor008()
        {
            int pageCount = Util.GetTestDocumentPageCount(8);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test008C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor009()
        {
            int pageCount = Util.GetTestDocumentPageCount(9);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test009C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor010()
        {
            int pageCount = Util.GetTestDocumentPageCount(10);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test010C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor011()
        {
            int pageCount = Util.GetTestDocumentPageCount(11);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test011C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor012()
        {
            int pageCount = Util.GetTestDocumentPageCount(12);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test012C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor013()
        {
            int pageCount = Util.GetTestDocumentPageCount(13);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test013C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor014()
        {
            int pageCount = Util.GetTestDocumentPageCount(14);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test014C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor015()
        {
            int pageCount = Util.GetTestDocumentPageCount(15);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test015C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor016()
        {
            int pageCount = Util.GetTestDocumentPageCount(16);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test016C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor017()
        {
            int pageCount = Util.GetTestDocumentPageCount(17);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test017C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor018()
        {
            int pageCount = Util.GetTestDocumentPageCount(18);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test018C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor019()
        {
            int pageCount = Util.GetTestDocumentPageCount(19);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test019C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor020()
        {
            int pageCount = Util.GetTestDocumentPageCount(20);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test020C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor021()
        {
            int pageCount = Util.GetTestDocumentPageCount(21);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test021C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor022()
        {
            int pageCount = Util.GetTestDocumentPageCount(22);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test022C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor023()
        {
            int pageCount = Util.GetTestDocumentPageCount(23);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test023C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor024()
        {
            int pageCount = Util.GetTestDocumentPageCount(24);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test024C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor025()
        {
            int pageCount = Util.GetTestDocumentPageCount(25);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test025C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor026()
        {
            int pageCount = Util.GetTestDocumentPageCount(26);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test026C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor027()
        {
            int pageCount = Util.GetTestDocumentPageCount(27);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test027C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor028()
        {
            int pageCount = Util.GetTestDocumentPageCount(28);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test028C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor029()
        {
            int pageCount = Util.GetTestDocumentPageCount(29);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test029C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor030()
        {
            int pageCount = Util.GetTestDocumentPageCount(30);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test030C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void IsDjvuDocument_String001()
        {
            string errorPath = "xyzzyx";
            Assert.Throws<FileNotFoundException>(() => DjvuDocument.IsDjvuDocument(errorPath));
        }

        [Fact]
        public void IsDjvuDocument_String002()
        {
            string errorPath = " ";
            Assert.Throws<ArgumentException>("filePath", () => DjvuDocument.IsDjvuDocument(errorPath));
        }

        [Fact]
        public void IsDjvuDocument_String003()
        {
            string errorPath = null;
            Assert.Throws<ArgumentNullException>("filePath", () => DjvuDocument.IsDjvuDocument(errorPath));
        }

        [Fact]
        public void IsDjvuDocument_String004()
        {
            string filePath = Path.GetTempFileName() + "1.djvu";
            FileStream fs = null;
            try
            {
                try
                {
                    fs = File.Create(filePath);
                    fs.Write(DjvuDocument.MagicBuffer, 0, DjvuDocument.MagicBuffer.Length);
                    fs.Write(DjvuDocument.MagicBuffer, 0, DjvuDocument.MagicBuffer.Length);
                    fs.WriteByte(0x4e);
                    fs.Flush();
                }
                finally
                {
                    fs.Close();
                }

                bool result = DjvuDocument.IsDjvuDocument(filePath);
                Assert.True(result);
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Fact]
        public void IsDjvuDocument_String005()
        {
            string filePath = Path.GetTempFileName() + "2.djvu";
            FileStream fs = null;
            try
            {
                try
                {
                    fs = File.Create(filePath);
                    byte[] buffer = new byte[DjvuDocument.MagicBuffer.Length];
                    Buffer.BlockCopy(DjvuDocument.MagicBuffer, 0, buffer, 0, buffer.Length);
                    Array.Reverse(buffer);
                    fs.Write(buffer, 0, buffer.Length);
                    fs.Write(buffer, 0, buffer.Length);
                    fs.WriteByte(0x4e);
                    fs.Flush();
                }
                finally
                {
                    fs.Close();
                }

                bool result = DjvuDocument.IsDjvuDocument(filePath);
                Assert.False(result);
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Fact]
        public void IsDjvuDocument_Stream001()
        {
            Stream stream = null;
            Assert.Throws<ArgumentNullException>("stream", () => DjvuDocument.IsDjvuDocument(stream));
        }

        [Fact]
        public void IsDjvuDocument_Stream002()
        {
            StreamMock stream = new StreamMock();
            Assert.Throws<ArgumentException>("stream", () => DjvuDocument.IsDjvuDocument(stream));

        }

        [Fact]
        public void IsDjvuDocument_Stream003()
        {
            byte[] buffer = DjvuDocument.MagicBuffer;
            byte[] target = new byte[buffer.Length * 3];
            Buffer.BlockCopy(buffer, 0, target, 0, buffer.Length);
            using (MemoryStream stream = new MemoryStream(target))
            {
                bool result = DjvuDocument.IsDjvuDocument(stream);
                Assert.True(result);
            }
        }

        [Fact]
        public void IsDjvuDocument_Stream004()
        {
            byte[] buffer = DjvuDocument.MagicBuffer;
            byte[] target = new byte[buffer.Length * 3];
            Buffer.BlockCopy(buffer, 0, target, 0, buffer.Length);
            target[2] = (byte)(target[2] - 0x01);
            using (MemoryStream stream = new MemoryStream(target))
            {
                bool result = DjvuDocument.IsDjvuDocument(stream);
                Assert.False(result);
            }
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void DjvuDocumentTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void DjvuDocumentTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void DjvuDocumentTest2()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void DisposeTest()
        {
            DjvuDocument document = null;
            int pageCount = 0;

            try
            {
                document = Util.GetTestDocument(2, out pageCount);
            }
            finally
            {
                document?.Dispose();
                Assert.True(document.IsDisposed);
                Assert.Empty(document.Pages);
            }
        }

        [Fact()]
        public void LoadTest()
        {
            DjvuDocument document = null;
            document = new DjvuDocument();
            string testFile = Util.GetTestFilePath(2);
            try
            {
                document.Load(testFile);
                Assert.NotNull(document.Pages);
                Assert.NotEmpty(document.Pages);
            }
            finally
            {
                document?.Dispose();
            }
        }

        [Fact()]
        public void GetRootFormChildrenTest001()
        {
            int pageCount = 0;
            using(DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                DirmChunk node = document.GetRootFormChildren<DirmChunk>().FirstOrDefault();
                Assert.NotNull(node);
                Assert.IsType<DirmChunk>(node);
            }
        }

        [Fact()]
        public void GetRootFormChildrenTest002()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var nodes = document.GetRootFormChildren<DjvuChunk>().ToList();
                Assert.NotNull(nodes);
                Assert.Equal<int>(pageCount, nodes.Count);
            }
        }
    }
}
