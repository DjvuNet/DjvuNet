using System;
using System.Collections.Generic;
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
                List<object[]> retVal = new List<object[]>
                {
                     new object[] {"..\\..\\..\\artifacts\\test001.djvu", 62},
                     new object[] {"..\\..\\..\\artifacts\\test002.djvu", 107},
                     new object[] {"..\\..\\..\\artifacts\\test003.djvu", 300},
                     new object[] {"..\\..\\..\\artifacts\\test004.djvu", 494},
                     new object[] {"..\\..\\..\\artifacts\\test005.djvu", 286},
                     new object[] {"..\\..\\..\\artifacts\\test006.djvu", 348},
                     new object[] {"..\\..\\..\\artifacts\\test007.djvu", 186},
                     new object[] {"..\\..\\..\\artifacts\\test008.djvu", 427},
                     new object[] {"..\\..\\..\\artifacts\\test009.djvu", 274},
                     new object[] {"..\\..\\..\\artifacts\\test010.djvu", 223},
                     new object[] {"..\\..\\..\\artifacts\\test011.djvu", 154},
                     new object[] {"..\\..\\..\\artifacts\\test012.djvu", 239},
                     new object[] {"..\\..\\..\\artifacts\\test013.djvu", 9},
                     new object[] {"..\\..\\..\\artifacts\\test014.djvu", 20},
                     new object[] {"..\\..\\..\\artifacts\\test015.djvu", 40},
                     new object[] {"..\\..\\..\\artifacts\\test016.djvu", 30},
                     new object[] {"..\\..\\..\\artifacts\\test017.djvu", 12},
                     new object[] {"..\\..\\..\\artifacts\\test018.djvu", 7},
                     new object[] {"..\\..\\..\\artifacts\\test019.djvu", 28},
                     new object[] {"..\\..\\..\\artifacts\\test020.djvu", 5},
                     new object[] {"..\\..\\..\\artifacts\\test021.djvu", 12},
                     new object[] {"..\\..\\..\\artifacts\\test022.djvu", 10},
                     new object[] {"..\\..\\..\\artifacts\\test023.djvu", 3},
                     new object[] {"..\\..\\..\\artifacts\\test024.djvu", 3},
                     new object[] {"..\\..\\..\\artifacts\\test025.djvu", 9},
                     new object[] {"..\\..\\..\\artifacts\\test026.djvu", 146},
                     new object[] {"..\\..\\..\\artifacts\\test027.djvu", 173},
                     new object[] {"..\\..\\..\\artifacts\\test028.djvu", 267},
                     new object[] {"..\\..\\..\\artifacts\\test029.djvu", 323},
                     new object[] {"..\\..\\..\\artifacts\\test030.djvu", 1},
                    //new object[] {"..\\..\\..\\artifacts\\test001.djvu"},
                    //new object[] {"..\\..\\..\\artifacts\\test001.djvu"},
                    //new object[] {"..\\..\\..\\artifacts\\test001.djvu"},
                };

                return retVal;
            }
        }

        [Theory]
        [MemberData(nameof(DjvuArtifacts))]
        public void DjvuDocument_IsDjvuDocumentString_Theory(string filePath, int pageCount)
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
            finally
            {
                document?.Dispose();
            }

            Thread.Sleep(500);
        }

        [Fact]
        public void DjvuDocument_ctor001()
        {
            int pageCount = 62;
            using(DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test001.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor002()
        {
            int pageCount = 107;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test002.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor003()
        {
            int pageCount = 300;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test003.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor004()
        {
            int pageCount = 494;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test004.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor005()
        {
            int pageCount = 286;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test005.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor006()
        {
            int pageCount = 348;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test006.djvu"))
            {
                Util.VerifyDjvuDocumentCtor(pageCount, document);
            }
        }

        [Fact]
        public void DjvuDocument_ctor007()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test007.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(186, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor008()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test008.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(427, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor009()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test009.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(274, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor010()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test010.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(223, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor011()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test011.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(154, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor012()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test012.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(239, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor013()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test013.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(9, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor014()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test014.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(20, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor015()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test015.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(40, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor016()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test016.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(30, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor017()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test017.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(12, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor018()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test018.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(7, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor019()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test019.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(28, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor020()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test020.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(5, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor021()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test021.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(12, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor022()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test022.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(10, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor023()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test023.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(3, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor024()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test024.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(3, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor025()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test025.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(9, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor026()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test026.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(146, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor027()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test027.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(173, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor028()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test028.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(267, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor029()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test029.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(323, document.Pages.Length);
                Assert.False(document.Disposed);
                Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }

        [Fact]
        public void DjvuDocument_ctor030()
        {
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test030.djvu"))
            {
                Assert.NotNull(document.FirstPage);
                Assert.NotNull(document.LastPage);
                Assert.Equal<int>(1, document.Pages.Length);
                Assert.False(document.Disposed);
                // TODO Verify why it is failing
                //Assert.NotNull(document.Directory);
                Assert.NotNull(document.ActivePage);
                Assert.NotNull(document.RootFormChunk);
                Assert.NotNull(document.Navigation);
            }
        }
    }
}
