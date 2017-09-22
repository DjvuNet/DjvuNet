using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Moq;
using Xunit;
using DjvuNet.DataChunks;
using System.Linq;
using DjvuNet.Tests.Xunit;
using System.ComponentModel;
using DjvuNet.Errors;

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
                int maxTestNumber = 77;
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
                    new object[]
                    {
                        Util.GetTestFilePath(i),
                        Util.GetTestDocumentPageCount(i)
                    });
            }
        }

        [DjvuTheory]
        [MemberData(nameof(DjvuArtifacts))]
        public void IsDjvuDocument_String_Theory(string filePath, int pageCount)
        {
            Assert.True(DjvuDocument.IsDjvuDocument(filePath));
        }

        [DjvuTheory]
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
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test003C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor004()
        {
            int pageCount = Util.GetTestDocumentPageCount(4);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test004C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor005()
        {
            int pageCount = Util.GetTestDocumentPageCount(5);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test005C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor006()
        {
            int pageCount = Util.GetTestDocumentPageCount(6);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test006C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor007()
        {
            int pageCount = Util.GetTestDocumentPageCount(7);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test007C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor008()
        {
            int pageCount = Util.GetTestDocumentPageCount(8);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test008C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor009()
        {
            int pageCount = Util.GetTestDocumentPageCount(9);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test009C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor010()
        {
            int pageCount = Util.GetTestDocumentPageCount(10);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test010C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor011()
        {
            int pageCount = Util.GetTestDocumentPageCount(11);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test011C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor012()
        {
            int pageCount = Util.GetTestDocumentPageCount(12);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test012C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor013()
        {
            int pageCount = Util.GetTestDocumentPageCount(13);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test013C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor014()
        {
            int pageCount = Util.GetTestDocumentPageCount(14);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test014C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor015()
        {
            int pageCount = Util.GetTestDocumentPageCount(15);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test015C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor016()
        {
            int pageCount = Util.GetTestDocumentPageCount(16);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test016C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor017()
        {
            int pageCount = Util.GetTestDocumentPageCount(17);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test017C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor018()
        {
            int pageCount = Util.GetTestDocumentPageCount(18);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test018C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor019()
        {
            int pageCount = Util.GetTestDocumentPageCount(19);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test019C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor020()
        {
            int pageCount = Util.GetTestDocumentPageCount(20);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test020C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor021()
        {
            int pageCount = Util.GetTestDocumentPageCount(21);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test021C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor022()
        {
            int pageCount = Util.GetTestDocumentPageCount(22);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test022C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor023()
        {
            int pageCount = Util.GetTestDocumentPageCount(23);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test023C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor024()
        {
            int pageCount = Util.GetTestDocumentPageCount(24);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test024C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor025()
        {
            int pageCount = Util.GetTestDocumentPageCount(25);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test025C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor026()
        {
            int pageCount = Util.GetTestDocumentPageCount(26);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test026C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor027()
        {
            int pageCount = Util.GetTestDocumentPageCount(27);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test027C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor028()
        {
            int pageCount = Util.GetTestDocumentPageCount(28);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test028C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor029()
        {
            int pageCount = Util.GetTestDocumentPageCount(29);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test029C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor030()
        {
            int pageCount = Util.GetTestDocumentPageCount(30);
            using (DjvuDocument document = new DjvuDocument(Path.Combine(Util.ArtifactsPath, "test030C.djvu")))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void ctor077()
        {
            int pageCount = Util.GetTestDocumentPageCount(77);
            string file = Util.GetTestFilePath(77);
            using (DjvuDocument document = new DjvuDocument(file, 77))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
                Assert.Equal(77, document.Identifier);
                Assert.False(String.IsNullOrWhiteSpace(document.Name));
                Assert.StartsWith(file, document.Location);
                Assert.False(document.IsInverted);
                Assert.NotNull(document.NextPage);
                // TODO change list logic - if first page is current
                // than previous page should be null
                //Assert.Null(document.PreviousPage);
            }
        }

        [Fact]
        public void Properties003()
        {
            int pageCount = Util.GetTestDocumentPageCount(3);
            string file = Util.GetTestFilePath(3);
            int identifier = 3;
            using (DjvuDocument document = new DjvuDocument(file, identifier))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
                Assert.Equal(identifier, document.Identifier);
                Assert.False(String.IsNullOrWhiteSpace(document.Name));
                Assert.StartsWith(file, document.Location);
                Assert.False(document.IsInverted);
                Assert.NotNull(document.NextPage);

                // TODO change list logic - if first page is current
                // than previous page should be null
                //Assert.Null(document.PreviousPage);

                string testLocation = "http:://web.site";

                document.IsInverted = true;
                Assert.True(document.IsInverted);

                document.Location = testLocation;
                Assert.Equal(testLocation, document.Location);

                document.Identifier = int.MinValue;
                Assert.Equal(int.MinValue, document.Identifier);

                var includes = document.Includes;
                Assert.NotNull(includes);
                Assert.Equal(11, includes.Count);
            }
        }

        [Fact]
        public void Properties023()
        {
            int pageCount = Util.GetTestDocumentPageCount(23);
            string file = Util.GetTestFilePath(23);
            int identifier = 23;
            using (DjvuDocument document = new DjvuDocument(file, identifier))
            {
                var includes = document.Includes;
                Assert.NotNull(includes);
                Assert.Equal(2, includes.Count);

                Util.VerifyDjvuDocumentCtor(pageCount, document);
                Assert.Equal(identifier, document.Identifier);
                Assert.False(String.IsNullOrWhiteSpace(document.Name));
                Assert.StartsWith(file, document.Location);
                Assert.False(document.IsInverted);
                Assert.NotNull(document.NextPage);

                // TODO change list logic - if first page is current
                // than previous page should be null
                //Assert.Null(document.PreviousPage);

                string testLocation = "http:://web.site";

                document.IsInverted = true;
                Assert.True(document.IsInverted);

                document.Location = testLocation;
                Assert.Equal(testLocation, document.Location);

                document.Identifier = int.MinValue;
                Assert.Equal(int.MinValue, document.Identifier);

                string testName = "Test Name";
                document.Name = testName;
                Assert.Equal(testName, document.Name);
            }
        }

        [Fact]
        public void IsDjvuDocument_String001()
        {
            string errorPath = "xyzzyx";
            Assert.Throws<DjvuFileNotFoundException>(() => DjvuDocument.IsDjvuDocument(errorPath));
        }

        [Fact]
        public void IsDjvuDocument_String002()
        {
            string errorPath = " ";
            Assert.Throws<DjvuArgumentException>("filePath", () => DjvuDocument.IsDjvuDocument(errorPath));
        }

        [Fact]
        public void IsDjvuDocument_String003()
        {
            string errorPath = null;
            Assert.Throws<DjvuArgumentNullException>("filePath", () => DjvuDocument.IsDjvuDocument(errorPath));
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
        public void IsDjvuDocument_String006()
        {
            string errorPath = Path.GetTempFileName();
            using (FileStream stream = new FileStream(errorPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.Throws<DjvuAggregateException>(() => DjvuDocument.IsDjvuDocument(errorPath));
            }
        }

        [Fact]
        public void IsDjvuDocument_Stream001()
        {
            Stream stream = null;
            Assert.Throws<DjvuArgumentNullException>("stream", () => DjvuDocument.IsDjvuDocument(stream));
        }

        [Fact]
        public void IsDjvuDocument_Stream002()
        {
            StreamMock stream = new StreamMock();
            Assert.Throws<DjvuArgumentException>("stream", () => DjvuDocument.IsDjvuDocument(stream));

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

        [Fact()]
        public void IsDjvuDocument_Stream005()
        {
            using (MemoryStream stream = new MemoryStream(new byte[4]))
            {
                Assert.Equal(4, stream.Length);
                Assert.False(DjvuDocument.IsDjvuDocument(stream));
            }
        }

        [Fact()]
        public void IsDjvuDocument_Stream006()
        {
            using (MemoryStream stream = new MemoryStream(DjvuDocument.MagicBuffer))
            {
                Assert.Equal(8, stream.Length);
                Assert.False(DjvuDocument.IsDjvuDocument(stream));
            }
        }

        [Fact()]
        public void IsDjvuDocument_Stream007()
        {
            byte[] buffer = new byte[] { 0x41, 0x54, 0x26, 0x54, 0x46, 0x4f, 0x52, 0x4d, 0x41, 0x54, 0x26, 0x54, 0x46, 0x4f, 0x52, 0x4d };
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                Assert.Equal(16, stream.Length);
                stream.Position = 10;
                Assert.True(DjvuDocument.IsDjvuDocument(stream));
            }
        }

        [Fact()]
        public void LoadTest004()
        {
            string file = Util.GetTestFilePath(4);
            using(DjvuDocument document = new DjvuDocument())
            {
                int hash = file.GetHashCode();
                document.Load(file, hash);
                Assert.Equal(hash, document.Identifier);
            }
        }

        [Fact()]
        public void LoadTest027()
        {
            string file = Util.GetTestFilePath(27);
            using (DjvuDocument document = new DjvuDocument())
            {
                int hash = file.GetHashCode();
                document.Load(file, hash);
                Assert.Equal(hash, document.Identifier);
            }
        }

        [Fact()]
        public void LoadTest039()
        {
            string file = Util.GetTestFilePath(39);
            using (DjvuDocument document = new DjvuDocument())
            {
                int hash = file.GetHashCode();
                document.Load(file, hash);
                Assert.Equal(hash, document.Identifier);
            }
        }

        [Fact()]
        public void LoadTest078()
        {
            string file = Path.Combine(Util.ArtifactsPath, "test078C.djvu");
            using (DjvuDocument document = new DjvuDocument())
            {
                int hash = file.GetHashCode();
                document.Load(file, hash);
                Assert.Equal(hash, document.Identifier);
                IDjvuPage page = document.ActivePage;

                PM44Chunk pmChunk = page.PageForm.Children[0] as PM44Chunk;
                Assert.IsType<PM44Chunk>(pmChunk);

                var img = pmChunk.Image;
                Assert.NotNull(img);

                var pixMap = new Wavelet.InterWavePixelMapDecoder();
                pixMap = pmChunk.ProgressiveDecodeBackground(pixMap) as Wavelet.InterWavePixelMapDecoder;
                Assert.NotNull(pixMap);

                pmChunk = page.PageForm.Children[1] as PM44Chunk;
                Assert.NotNull(pmChunk);

                var pixMap2 = new Wavelet.InterWavePixelMapDecoder();
                Assert.Throws<DjvuFormatException>(() => pmChunk.ProgressiveDecodeBackground(pixMap2));

                // This time call will not throw
                pmChunk.ProgressiveDecodeBackground(pixMap);
            }
        }

        [Fact()]
        public void DisposeTest001()
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
        public void DisposeTest002()
        {
            DjvuDocument document = null;
            int pageCount = 0;

            try
            {
                document = Util.GetTestDocument(2, out pageCount);
            }
            finally
            {
                document.Dispose();
                Assert.True(document.IsDisposed);
                Assert.Empty(document.Pages);
                // Test call to Dispose does not throw
                document.Dispose();
            }
        }

        [Fact()]
        public void LoadTest002()
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

        [Fact()]
        public void BuildPageList001()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
                readerMock.SetupProperty(x => x.Position);
                IDjvuReader reader = readerMock.Object;
                reader.Position = 0;
                document.RootForm = new ThumChunk(reader, null, document, "THUM", 0);
                Assert.Throws<DjvuFormatException>(() => document.BuildPageList());
            }
        }

        [Fact()]
        public void CheckDjvuHeader001()
        {
            byte[] buffer = new byte[] { 0x01, 0x07, 0x31, 0x67, 0xdf, 0xe4, 0x5f, 0x61 };
            using (MemoryStream stream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                DjvuDocument doc = new DjvuDocument();
                Assert.Throws<DjvuFormatException>(() => doc.CheckDjvuHeader(reader));
            }
        }

        [Fact()]
        public void CheckDjvuHeader002()
        {
            byte[] buffer = new byte[] { 0x41, 0x54, 0x26, 0x54, 0x46, 0x4f, 0x52, 0x4d };
            using (MemoryStream stream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                DjvuDocument doc = new DjvuDocument();
                doc.CheckDjvuHeader(reader);
            }
        }

        [Fact()]
        public void GetRootFormChildren001()
        {
            int pageCount = 0;
            using(DjvuDocument doc = Util.GetTestDocument(30, out pageCount))
            {
                DjvuChunk form = doc.GetRootFormChildren<DjvuChunk>().FirstOrDefault();
                Assert.NotNull(form);
                Assert.IsType<DjvuChunk>(form);
            }
        }

        [Fact()]
        public void OnPropertyChanged001()
        {
            int pageCount = 0;
            using (DjvuDocument doc = Util.GetTestDocument(30, out pageCount))
            {
                bool called = false;
                bool propertyMatched = false;
                doc.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
                {
                    called = true;
                    if (e.PropertyName == "Identifier")
                        propertyMatched = true;
                };
                doc.Identifier = doc.GetHashCode();

                Assert.True(called);
                Assert.True(propertyMatched);
            }
        }

        [Fact()]
        public void ActiveNextPreviousPageTest001()
        {
            int pageCount;
            using (DjvuDocument doc = Util.GetTestDocument(2, out pageCount))
            {
                doc.ActivePage = doc.Pages[7];
                Assert.Same(doc.PreviousPage, doc.Pages[6]);
                Assert.Same(doc.NextPage, doc.Pages[8]);
                Assert.Same(doc.ActivePage, doc.Pages[7]);
            }
        }

        [Fact()]
        public void CachePageImagesTest()
        {
            int pageCount;
            using (DjvuDocument doc = Util.GetTestDocument(5, out pageCount))
            {
                doc.CachePageImages(doc.Pages);
                var cached = doc.Pages.Where(p => p.IsPageImageCached == true).ToList();
                Assert.NotNull(cached);
                // TODO verify logic after method reimplementation
                Assert.Equal(0, cached.Count);
            }
        }

        [Fact()]
        public void DirectoryTest001()
        {
            int pageCount;
            using (DjvuDocument doc = Util.GetTestDocument(30, out pageCount))
            {
                Assert.Null(doc.Directory);
                Assert.IsType<DjvuChunk>(doc.RootForm);
            }
        }

        [Fact()]
        public void DirectoryTest002()
        {
            int pageCount;
            using (DjvuDocument doc = Util.GetTestDocument(5, out pageCount))
            {
                Assert.NotNull(doc.Directory);
                Assert.IsType<DjvmChunk>(doc.RootForm);
            }
        }
    }
}
