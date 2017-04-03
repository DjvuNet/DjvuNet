using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DjvuNet.Tests
{
    public static partial class Util
    {
        public static DjvuDocument GetTestDocument(int index, out int pageCount)
        {
            pageCount = 0;

            if (index < 1)
                return null;

            pageCount = GetTestDocumentPageCount(index);
            return new DjvuDocument(GetTestFilePath(index));
        }

        public static void VerifyDjvuDocumentCtor(int pageCount, DjvuDocument document)
        {
            VerifyDjvuDocument(pageCount, document);
            Assert.False(document.Disposed);
            if (pageCount > 1)
                Assert.NotNull(document.Directory);
            Assert.NotNull(document.ActivePage);
            Assert.NotNull(document.RootForm);
            Assert.NotNull(document.Navigation);
        }

        public static void VerifyDjvuDocument(int pageCount, DjvuDocument document)
        {
            Assert.NotNull(document.FirstPage);
            Assert.NotNull(document.LastPage);
            if (pageCount > 0)
                Assert.Equal<int>(pageCount, document.Pages.Length);
        }

    }
}
