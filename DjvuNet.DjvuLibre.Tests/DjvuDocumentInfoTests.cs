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
    public class DjvuDocumentInfoTests
    {
        [Fact(), Trait("Category", "DjvuLibre")]
        public void CreateDjvuDocumentInfoTest001()
        {
            using (DjvuDocumentInfo document = 
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(62, pageCount);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void CreateDjvuDocumentInfoTest002()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(300, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DisposeTest()
        {
            DjvuDocumentInfo document = null;
            try
            {
                document = DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1));
                Assert.NotNull(document);
                Assert.True(document.PageCount > 0);
            }
            finally
            {
                if (document != null)
                {
                    document.Dispose();
                    Assert.True(document.Disposed);
                }
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetDjvuPageInfoTest001()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(300, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                var info = document.GetPageInfo(0);
                Assert.NotNull(info);
                Assert.IsType<PageInfo>(info);
                Assert.Equal<int>(25, info.Version);
                Assert.Equal<int>(600, info.Dpi);
                Assert.Equal<int>(6054, info.Height);
                Assert.Equal<int>(3916, info.Width);
                Assert.Equal<int>(0, info.Rotation);
            }
        }
    }
}