using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;

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
                     //new object[] { null, 62},
                     //new object[] { null, 11},
                     //new object[] { null, 300},
                     //new object[] { null, 494},
                     //new object[] { null, 286},
                     //new object[] { null, 348},
                     //new object[] { null, 186},
                     //new object[] { null, 427},
                     //new object[] { null, 274},
                     //new object[] { null, 17},
                     //new object[] { null, 154},
                     //new object[] { null, 239},
                     //new object[] { null, 9},
                     //new object[] { null, 20},
                     //new object[] { null, 40},
                     //new object[] { null, 30},
                     //new object[] { null, 12},
                     //new object[] { null, 7},
                     //new object[] { null, 28},
                     //new object[] { null, 5},
                     //new object[] { null, 12},
                     //new object[] { null, 10},
                     //new object[] { null, 3},
                     //new object[] { null, 3},
                     //new object[] { null, 9},
                     //new object[] { null, 146},
                     //new object[] { null, 173},
                     //new object[] { null, 267},
                     //new object[] { null, 323},
                     //new object[] { null, 1},
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
            //for (; i < retVal.Count; i++)
            //    retVal[i][0] = String.Format(filePathTempl, (i + 1));

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
        public void DjvuDocument_IsDjvuDocument_String_Theory(string filePath, int pageCount)
        {
            Assert.True(DjvuDocument.IsDjvuDocument(filePath));
        }

        [Theory]
        [MemberData(nameof(DjvuArtifacts))]
        public void DjvuDocument_ctor_Theory(string filePath, int pageCount)
        {
            DjvuDocument document = null;
            try
            {
                document = new DjvuDocument(filePath);
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
            catch(Exception error)
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
        public void DjvuDocument_ctor001()
        {
            int pageCount = Util.GetTestDocumentPageCount(1);
            using(DjvuDocument document = Util.GetTestDocument(1, out pageCount))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor002()
        {
            int pageCount = Util.GetTestDocumentPageCount(2);
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor003()
        {
            int pageCount = Util.GetTestDocumentPageCount(3);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test003C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor004()
        {
            int pageCount = Util.GetTestDocumentPageCount(4);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test004C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor005()
        {
            int pageCount = Util.GetTestDocumentPageCount(5);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test005C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor006()
        {
            int pageCount = Util.GetTestDocumentPageCount(6);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test006C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor007()
        {
            int pageCount = Util.GetTestDocumentPageCount(7);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test007C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor008()
        {
            int pageCount = Util.GetTestDocumentPageCount(8);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test008C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor009()
        {
            int pageCount = Util.GetTestDocumentPageCount(9);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test009C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor010()
        {
            int pageCount = Util.GetTestDocumentPageCount(10);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test010C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor011()
        {
            int pageCount = Util.GetTestDocumentPageCount(11);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test011C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor012()
        {
            int pageCount = Util.GetTestDocumentPageCount(12);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test012C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor013()
        {
            int pageCount = Util.GetTestDocumentPageCount(13);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test013C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor014()
        {
            int pageCount = Util.GetTestDocumentPageCount(14);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test014C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor015()
        {
            int pageCount = Util.GetTestDocumentPageCount(15);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test015C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor016()
        {
            int pageCount = Util.GetTestDocumentPageCount(16);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test016C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor017()
        {
            int pageCount = Util.GetTestDocumentPageCount(17);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test017C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor018()
        {
            int pageCount = Util.GetTestDocumentPageCount(18);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test018C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor019()
        {
            int pageCount = Util.GetTestDocumentPageCount(19);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test019C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor020()
        {
            int pageCount = Util.GetTestDocumentPageCount(20);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test020C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor021()
        {
            int pageCount = Util.GetTestDocumentPageCount(21);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test021C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor022()
        {
            int pageCount = Util.GetTestDocumentPageCount(22);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test022C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor023()
        {
            int pageCount = Util.GetTestDocumentPageCount(23);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test023C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor024()
        {
            int pageCount = Util.GetTestDocumentPageCount(24);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test024C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor025()
        {
            int pageCount = Util.GetTestDocumentPageCount(25);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test025C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor026()
        {
            int pageCount = Util.GetTestDocumentPageCount(26);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test026C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor027()
        {
            int pageCount = Util.GetTestDocumentPageCount(27);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test027C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor028()
        {
            int pageCount = Util.GetTestDocumentPageCount(28);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test028C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor029()
        {
            int pageCount = Util.GetTestDocumentPageCount(29);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test029C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor030()
        {
            int pageCount = Util.GetTestDocumentPageCount(30);
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test030C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }
    }
}
