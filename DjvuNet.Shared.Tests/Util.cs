using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace DjvuNet.Tests
{
    public static partial class Util
    {
        public static void FailOnException(Exception ex, string message, params object[] data)
        {
            string info = $"Test Failed -> Unexpected Exception: " + 
                $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")}\n\n";

            if (data != null && data.Length > 0)
                info += (String.Format(message, data) + "\n" + ex.ToString());
            else
                info += (message + "\n" + ex.ToString());

            Assert.True(false, info);
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

        public static string GetTestFilePathTemplate()
        {
            char dirSep = Path.DirectorySeparatorChar;
            string filePathTempl = $"artifacts{dirSep}test{{0:00#}}C.djvu";
            string rootDir = Util.RepoRoot;
            filePathTempl = Path.Combine(rootDir, filePathTempl);
            return filePathTempl;
        }

    }
}
