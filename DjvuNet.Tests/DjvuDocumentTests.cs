﻿using System;
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
                int maxTestNumber = 75;
                string filePathTempl = Util.GetTestFilePathTemplate();

                List<object[]> retVal = new List<object[]>
                {
                     new object[] { null, 62},
                     new object[] { null, 11},
                     new object[] { null, 300},
                     new object[] { null, 494},
                     new object[] { null, 286},
                     new object[] { null, 348},
                     new object[] { null, 186},
                     new object[] { null, 427},
                     new object[] { null, 274},
                     new object[] { null, 17},
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
            int i = 0;
            for (; i < retVal.Count; i++)
                retVal[i][0] = String.Format(filePathTempl, (i + 1));

            for (; i < maxTestNumber; i++)
                retVal.Add(
                    new object[] { String.Format(filePathTempl, (i + 1)), 0});
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
            string path = Util.RepoRoot;
            int pageCount = 62;
            using(DjvuDocument document = Util.GetTestDocument(1, out pageCount))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor002()
        {
            int pageCount = 11;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor003()
        {
            int pageCount = 300;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test003C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor004()
        {
            int pageCount = 494;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test004C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor005()
        {
            int pageCount = 286;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test005C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor006()
        {
            int pageCount = 348;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test006C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor007()
        {
            int pageCount = 186;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test007C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor008()
        {
            int pageCount = 427;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test008C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor009()
        {
            int pageCount = 274;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test009C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor010()
        {
            int pageCount = 17;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test010C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor011()
        {
            int pageCount = 154;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test011C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor012()
        {
            int pageCount = 239;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test012C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor013()
        {
            int pageCount = 9;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test013C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor014()
        {
            int pageCount = 20;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test014C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor015()
        {
            int pageCount = 40;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test015C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor016()
        {
            int pageCount = 30;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test016C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor017()
        {
            int pageCount = 12;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test017C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor018()
        {
            int pageCount = 7;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test018C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor019()
        {
            int pageCount = 28;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test019C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor020()
        {
            int pageCount = 5;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test020C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor021()
        {
            int pageCount = 12;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test021C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor022()
        {
            int pageCount = 10;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test022C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor023()
        {
            int pageCount = 3;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test023C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor024()
        {
            int pageCount = 3;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test024C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor025()
        {
            int pageCount = 9;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test025C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor026()
        {
            int pageCount = 146;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test026C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor027()
        {
            int pageCount = 173;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test027C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor028()
        {
            int pageCount = 267;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test028C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor029()
        {
            int pageCount = 323;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test029C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor030()
        {
            int pageCount = 1;
            using (DjvuDocument document = new DjvuDocument($"{Util.RepoRoot}artifacts\\test030C.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }
    }
}
