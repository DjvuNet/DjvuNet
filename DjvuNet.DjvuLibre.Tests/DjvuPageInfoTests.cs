using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.DjvuLibre;
using DjvuNet.Tests;
using Xunit;

namespace DjvuNet.DjvuLibre.Tests
{
    public class DjvuPageInfoTests
    {
        [Fact(), Trait("Category", "DjvuLibre")]
        public void DjvuPageInfoTest()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(300, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DisposeTest()
        {
            DjvuPageInfo page = null;
            try
            {
                using (DjvuDocumentInfo document =
                    DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
                {
                    Assert.NotNull(document);

                    int pageCount = document.PageCount;
                    Assert.Equal<int>(300, pageCount);

                    DocumentType type = document.DocumentType;
                    Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                    page = new DjvuPageInfo(document, 0);
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);
                }
            }
            finally
            {
                if (page != null)
                {
                    page.Dispose();
                    Assert.True(page.Disposed);
                    page = null;
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetPageTypeTest()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
            {
                Assert.NotNull(document);

                int pageCount = document.PageCount;
                Assert.Equal<int>(300, pageCount);

                DocumentType type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                using (DjvuPageInfo page = new DjvuPageInfo(document, 0))
                {
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);

                    PageType pageType = page.GetPageType();
                }

                using (DjvuPageInfo page = new DjvuPageInfo(document, 1))
                {
                    Assert.NotNull(page);
                    Assert.IsType<DjvuPageInfo>(page);

                    PageType pageType = page.GetPageType();
                }
            }
        }
    }
}