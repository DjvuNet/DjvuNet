using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using DjvuNet.DjvuLibre;
using Xunit;

namespace DjvuNet.Tests
{
    public static partial class Util
    {
        private static SortedDictionary<int, Tuple<int, int, DocumentType, string> > _TestDocumentData;

        static Util()
        {
            _TestDocumentData = new SortedDictionary<int, Tuple< int, int, DocumentType, string>> ();

            for(int i = 1; i <= 77; i++)
            {
                using (DjvuNet.DjvuLibre.DjvuDocumentInfo docInfo = 
                    DjvuLibre.DjvuDocumentInfo.CreateDjvuDocumentInfo(GetTestFilePath(i)))
                {
                    var docData = Tuple.Create<int, int, DocumentType, string>(
                        docInfo.PageCount, docInfo.FileCount, docInfo.DocumentType, null /* docInfo.DumpDocumentData(true)*/);
                    _TestDocumentData.Add(i, docData);

                }
            }

        }

        public static int GetTestDocumentPageCount(int index)
        {
            return _TestDocumentData[index].Item1;
        }

        public static int GetTestDocumentFileCount(int index)
        {
            return _TestDocumentData[index].Item2;
        }

        public static DocumentType GetTestDocumentType(int index)
        {
            return _TestDocumentData[index].Item3;
        }

        public static string GetTestDocumentJsonDump(int index)
        {
            return _TestDocumentData[index].Item4;
        }

        public static void FailOnException(Exception ex, string message, params object[] data)
        {
            string info = $"\nTest Failed -> Unexpected Exception: " + 
                $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")}\n\n";

            if (data != null && data.Length > 0)
                info += (String.Format(message, data) + "\n" + ex.ToString());
            else
                info += (message + "\n" + ex.ToString());

            Assert.True(false, info);
        }

        public static string GetTestFilePathTemplate()
        {
            char dirSep = Path.DirectorySeparatorChar;
            string filePathTempl = $"artifacts{dirSep}test{{0:00#}}C.djvu";
            string rootDir = Util.RepoRoot;
            filePathTempl = Path.Combine(rootDir, filePathTempl);
            return filePathTempl;
        }

        public static string GetTestFilePath(int index)
        {
            string filePathTempl = GetTestFilePathTemplate();
            string filePath = String.Format(filePathTempl, index);
            return filePath;
        }
    }
}
