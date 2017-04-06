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

            //_TestDocumentData.Add(1, 62);
            //_TestDocumentData.Add(2, 11);
            //_TestDocumentData.Add(3, 300);
            //_TestDocumentData.Add(4, 494);
            //_TestDocumentData.Add(5, 286);
            //_TestDocumentData.Add(6, 348);
            //_TestDocumentData.Add(7, 186);
            //_TestDocumentData.Add(8, 427);
            //_TestDocumentData.Add(9, 274);
            //_TestDocumentData.Add(10, 17);
            //_TestDocumentData.Add(11, 154);
            //_TestDocumentData.Add(12, 239);
            //_TestDocumentData.Add(13, 9);
            //_TestDocumentData.Add(14, 20);
            //_TestDocumentData.Add(15, 40);
            //_TestDocumentData.Add(16, 30);
            //_TestDocumentData.Add(17, 12);
            //_TestDocumentData.Add(18, 7);
            //_TestDocumentData.Add(19, 28);
            //_TestDocumentData.Add(20, 5);
            //_TestDocumentData.Add(21, 12);
            //_TestDocumentData.Add(22, 10);
            //_TestDocumentData.Add(23, 3);
            //_TestDocumentData.Add(24, 3);
            //_TestDocumentData.Add(25, 9);
            //_TestDocumentData.Add(26, 146);
            //_TestDocumentData.Add(27, 173);
            //_TestDocumentData.Add(28, 267);
            //_TestDocumentData.Add(29, 323);
            //_TestDocumentData.Add(30, 1);
            //_TestDocumentData.Add(31, 15);
            //_TestDocumentData.Add(32, 9);
            //_TestDocumentData.Add(33, 31);
            //_TestDocumentData.Add(34, 4);
            //_TestDocumentData.Add(35, 15);
            //_TestDocumentData.Add(36, 10);
            //_TestDocumentData.Add(37, 17);
            //_TestDocumentData.Add(38, 28);
            //_TestDocumentData.Add(39, 36);
            //_TestDocumentData.Add(40, 57);
            //_TestDocumentData.Add(41, 14);
            //_TestDocumentData.Add(42, 21);
            //_TestDocumentData.Add(43, 5);
            //_TestDocumentData.Add(44, 10);
            //_TestDocumentData.Add(45, 78);
            //_TestDocumentData.Add(46, 59);
            //_TestDocumentData.Add(47, 26);
            //_TestDocumentData.Add(48, 52);
            //_TestDocumentData.Add(49, 19);
            //_TestDocumentData.Add(50, 21);
            //_TestDocumentData.Add(51, 32);
            //_TestDocumentData.Add(52, 26);
            //_TestDocumentData.Add(53, 21);
            //_TestDocumentData.Add(54, 176);
            //_TestDocumentData.Add(55, 26);
            //_TestDocumentData.Add(56, 37);
            //_TestDocumentData.Add(57, 124);
            //_TestDocumentData.Add(58, 122);
            //_TestDocumentData.Add(59, 94);
            //_TestDocumentData.Add(60, 28);
            //_TestDocumentData.Add(61, 127);
            //_TestDocumentData.Add(62, 30);
            //_TestDocumentData.Add(63, 27);
            //_TestDocumentData.Add(64, 1);
            //_TestDocumentData.Add(65, 37);
            //_TestDocumentData.Add(66, 16);
            //_TestDocumentData.Add(67, 15);
            //_TestDocumentData.Add(68, 17);
            //_TestDocumentData.Add(69, 15);
            //_TestDocumentData.Add(70, 28);
            //_TestDocumentData.Add(71, 29);
            //_TestDocumentData.Add(72, 36);
            //_TestDocumentData.Add(73, 18);
            //_TestDocumentData.Add(74, 50);
            //_TestDocumentData.Add(75, 11);
            //_TestDocumentData.Add(76, 18);

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
