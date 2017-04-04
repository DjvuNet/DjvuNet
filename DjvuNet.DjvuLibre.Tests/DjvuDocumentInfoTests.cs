using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DjvuNet.DjvuLibre;
using DjvuNet.Tests;
using Newtonsoft.Json;
using Xunit;


namespace DjvuNet.DjvuLibre.Tests
{
    public class DjvuDocumentInfoTests : SynchronizedBase
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
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void CreateDjvuDocumentInfoTest003()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(300, pageCount);
                int fileCount = document.FileCount;
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void CreateDjvuDocumentInfoTest030()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(30)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(1, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.SinglePage, type);
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
        public void GetPageInfoTest003()
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

        [Fact(), Trait("Category", "DjvuLibre")]
        public void GetFileInfoTest003()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
            {
                Assert.NotNull(document);
                int fileCount = document.FileCount;
                Assert.Equal<int>(330, fileCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                var info = document.GetFileInfo(0);
                Assert.NotNull(info);
                Assert.IsType<DjvuFileInfo>(info);
                Assert.Equal<int>('I', info.Type);
                Assert.Equal<int>(3364, info.Size);
                Assert.True(info.PageNumber < 0);
                Assert.Equal<string>("Schultens_Страница_005_1L.djbz", info.Title);
                Assert.Equal<string>("Schultens_Страница_005_1L.djbz", info.Name);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData001()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
            {
                Assert.NotNull(document);
                int fileCount = document.FileCount;
                Assert.Equal<int>(62, fileCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "  FORM:DJVU [11889] \n    INFO [10]         DjVu 2215x3639, v25, 400 dpi, gamma=2.2\n    CIDa [36] \n    Sjbz [5398]       JB2 bilevel data\n    FG44 [1910]       IW4 data #1, 100 slices, v1.2 (color), 185x304\n    BG44 [1119]       IW4 data #1, 74 slices, v1.2 (color), 739x1213\n    BG44 [797]        IW4 data #2, 10 slices\n    BG44 [263]        IW4 data #3, 4 slices\n    BG44 [1878]       IW4 data #4, 9 slices\n    TXTz [399]        Hidden text (text, etc.)\n";

                var data = document.DumpPageData(1, false);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData001Json()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
            {
                Assert.NotNull(document);
                int fileCount = document.FileCount;
                Assert.Equal<int>(62, fileCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVU\", \"Size\": 11889, \"Children\": [\n            { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 2215, \"Height\": 3639, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2 },\n            { \"ID\": \"CIDa\", \"Size\": 36 },\n            { \"ID\": \"Sjbz\", \"Size\": 5398, \"Description\": \"JB2 bilevel data\" },\n            { \"ID\": \"FG44\", \"Size\": 1910, \"Description\": \"IW4 data #1\", \"Slices\": 100, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 185, \"Height\": 304 },\n            { \"ID\": \"BG44\", \"Size\": 1119, \"Description\": \"IW4 data #1\", \"Slices\": 74, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 739, \"Height\": 1213 },\n            { \"ID\": \"BG44\", \"Size\": 797, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n            { \"ID\": \"BG44\", \"Size\": 263, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n            { \"ID\": \"BG44\", \"Size\": 1878, \"Description\": \"IW4 data #4\", \"Slices\": 9 },\n            { \"ID\": \"TXTz\", \"Size\": 399, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(1, true);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData001JsonDefault()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
            {
                Assert.NotNull(document);
                int fileCount = document.FileCount;
                Assert.Equal<int>(62, fileCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVU\", \"Size\": 11889, \"Children\": [\n            { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 2215, \"Height\": 3639, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2 },\n            { \"ID\": \"CIDa\", \"Size\": 36 },\n            { \"ID\": \"Sjbz\", \"Size\": 5398, \"Description\": \"JB2 bilevel data\" },\n            { \"ID\": \"FG44\", \"Size\": 1910, \"Description\": \"IW4 data #1\", \"Slices\": 100, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 185, \"Height\": 304 },\n            { \"ID\": \"BG44\", \"Size\": 1119, \"Description\": \"IW4 data #1\", \"Slices\": 74, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 739, \"Height\": 1213 },\n            { \"ID\": \"BG44\", \"Size\": 797, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n            { \"ID\": \"BG44\", \"Size\": 263, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n            { \"ID\": \"BG44\", \"Size\": 1878, \"Description\": \"IW4 data #4\", \"Slices\": 9 },\n            { \"ID\": \"TXTz\", \"Size\": 399, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(1);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData001JsonParser()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
            {
                Assert.NotNull(document);
                int fileCount = document.FileCount;
                Assert.Equal<int>(62, fileCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                var data = document.DumpPageData(1);
                Assert.NotNull(data);
                object json = JsonConvert.DeserializeObject(data);
                Assert.NotNull(json);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData002()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(2)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(13, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "  FORM:DJVU [24134] \n    INFO [10]         DjVu 2480x3260, v21, 300 dpi, gamma=2.2\n    INCL [12]         Indirection chunk --> {dict0013.iff}\n    Sjbz [13542]      JB2 bilevel data\n    FGbz [85]         JB2 colors data, v0, 8 colors\n    BG44 [1119]       IW4 data #1, 72 slices, v1.2 (color), 827x1087\n    BG44 [311]        IW4 data #2, 11 slices\n    BG44 [892]        IW4 data #3, 10 slices\n    BG44 [791]        IW4 data #4, 10 slices\n    ANTz [117]        Page annotation (hyperlinks, etc.)\n    TXTz [7166]       Hidden text (text, etc.)\n";

                var data = document.DumpPageData(0, false);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData002Json()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(2)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(13, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVU\", \"Size\": 24134, \"Children\": [\n            { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 2480, \"Height\": 3260, \"Version\": 21, \"Dpi\": 300, \"Gamma\": 2.2 },\n            { \"ID\": \"INCL\", \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"{dict0013.iff}\" },\n            { \"ID\": \"Sjbz\", \"Size\": 13542, \"Description\": \"JB2 bilevel data\" },\n            { \"ID\": \"FGbz\", \"Size\": 85, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 8 },\n            { \"ID\": \"BG44\", \"Size\": 1119, \"Description\": \"IW4 data #1\", \"Slices\": 72, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 827, \"Height\": 1087 },\n            { \"ID\": \"BG44\", \"Size\": 311, \"Description\": \"IW4 data #2\", \"Slices\": 11 },\n            { \"ID\": \"BG44\", \"Size\": 892, \"Description\": \"IW4 data #3\", \"Slices\": 10 },\n            { \"ID\": \"BG44\", \"Size\": 791, \"Description\": \"IW4 data #4\", \"Slices\": 10 },\n            { \"ID\": \"ANTz\", \"Size\": 117, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"ID\": \"TXTz\", \"Size\": 7166, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(0, true);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData002JsonDefault()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(2)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(13, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVU\", \"Size\": 24134, \"Children\": [\n            { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 2480, \"Height\": 3260, \"Version\": 21, \"Dpi\": 300, \"Gamma\": 2.2 },\n            { \"ID\": \"INCL\", \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"{dict0013.iff}\" },\n            { \"ID\": \"Sjbz\", \"Size\": 13542, \"Description\": \"JB2 bilevel data\" },\n            { \"ID\": \"FGbz\", \"Size\": 85, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 8 },\n            { \"ID\": \"BG44\", \"Size\": 1119, \"Description\": \"IW4 data #1\", \"Slices\": 72, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 827, \"Height\": 1087 },\n            { \"ID\": \"BG44\", \"Size\": 311, \"Description\": \"IW4 data #2\", \"Slices\": 11 },\n            { \"ID\": \"BG44\", \"Size\": 892, \"Description\": \"IW4 data #3\", \"Slices\": 10 },\n            { \"ID\": \"BG44\", \"Size\": 791, \"Description\": \"IW4 data #4\", \"Slices\": 10 },\n            { \"ID\": \"ANTz\", \"Size\": 117, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"ID\": \"TXTz\", \"Size\": 7166, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(0);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData002JsonParse()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(2)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(13, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                var data = document.DumpPageData(0);
                Assert.NotNull(data);
                object json = JsonConvert.DeserializeObject(data);
                Assert.NotNull(json);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData003()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(300, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "  FORM:DJVU [27048] \n    INFO [10]         DjVu 3916x6054, v25, 600 dpi, gamma=2.2\n    INCL [38]         Indirection chunk --> {Schultens_Страница_005_1L.djbz}\n    Sjbz [5941]       JB2 bilevel data\n    FGbz [110]        JB2 colors data, v0, 14 colors\n    BG44 [6577]       IW4 data #1, 75 slices, v1.2 (color), 979x1514\n    BG44 [4057]       IW4 data #2, 10 slices\n    BG44 [3433]       IW4 data #3, 4 slices\n    BG44 [6315]       IW4 data #4, 8 slices\n    TXTz [486]        Hidden text (text, etc.)\n";

                var data = document.DumpPageData(0, false);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData003Json()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(300, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVU\", \"Size\": 27048, \"Children\": [\n            { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 3916, \"Height\": 6054, \"Version\": 25, \"Dpi\": 600, \"Gamma\": 2.2 },\n            { \"ID\": \"INCL\", \"Size\": 38, \"Description\": \"Indirection chunk\", \"Name\": \"{Schultens_Страница_005_1L.djbz}\" },\n            { \"ID\": \"Sjbz\", \"Size\": 5941, \"Description\": \"JB2 bilevel data\" },\n            { \"ID\": \"FGbz\", \"Size\": 110, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 14 },\n            { \"ID\": \"BG44\", \"Size\": 6577, \"Description\": \"IW4 data #1\", \"Slices\": 75, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 979, \"Height\": 1514 },\n            { \"ID\": \"BG44\", \"Size\": 4057, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n            { \"ID\": \"BG44\", \"Size\": 3433, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n            { \"ID\": \"BG44\", \"Size\": 6315, \"Description\": \"IW4 data #4\", \"Slices\": 8 },\n            { \"ID\": \"TXTz\", \"Size\": 486, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(0, true);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData003JsonDefault()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(300, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVU\", \"Size\": 27048, \"Children\": [\n            { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 3916, \"Height\": 6054, \"Version\": 25, \"Dpi\": 600, \"Gamma\": 2.2 },\n            { \"ID\": \"INCL\", \"Size\": 38, \"Description\": \"Indirection chunk\", \"Name\": \"{Schultens_Страница_005_1L.djbz}\" },\n            { \"ID\": \"Sjbz\", \"Size\": 5941, \"Description\": \"JB2 bilevel data\" },\n            { \"ID\": \"FGbz\", \"Size\": 110, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 14 },\n            { \"ID\": \"BG44\", \"Size\": 6577, \"Description\": \"IW4 data #1\", \"Slices\": 75, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 979, \"Height\": 1514 },\n            { \"ID\": \"BG44\", \"Size\": 4057, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n            { \"ID\": \"BG44\", \"Size\": 3433, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n            { \"ID\": \"BG44\", \"Size\": 6315, \"Description\": \"IW4 data #4\", \"Slices\": 8 },\n            { \"ID\": \"TXTz\", \"Size\": 486, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(0);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData003JsonParse()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(3)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(300, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                var data = document.DumpPageData(0);
                Assert.NotNull(data);
                object json = JsonConvert.DeserializeObject(data);
                Assert.NotNull(json);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData017()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(17)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(13, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "  FORM:DJVU [18203] \n    INFO [10]         DjVu 4961x7016, v21, 600 dpi, gamma=2.2\n    INCL [12]         Indirection chunk --> {dict0013.iff}\n    Sjbz [10077]      JB2 bilevel data\n    FGbz [63]         JB2 colors data, v0, 3 colors\n    BG44 [283]        IW4 data #1, 97 slices, v1.2 (b&w), 414x585\n    ANTz [399]        Page annotation (hyperlinks, etc.)\n    TXTz [7295]       Hidden text (text, etc.)\n";

                var data = document.DumpPageData(1, false);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData017Json()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(17)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(13, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVU\", \"Size\": 18203, \"Children\": [\n            { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 4961, \"Height\": 7016, \"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2 },\n            { \"ID\": \"INCL\", \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"{dict0013.iff}\" },\n            { \"ID\": \"Sjbz\", \"Size\": 10077, \"Description\": \"JB2 bilevel data\" },\n            { \"ID\": \"FGbz\", \"Size\": 63, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 3 },\n            { \"ID\": \"BG44\", \"Size\": 283, \"Description\": \"IW4 data #1\", \"Slices\": 97, \"Version\": 1.2, \"Color\": \"False\", \"Width\": 414, \"Height\": 585 },\n            { \"ID\": \"ANTz\", \"Size\": 399, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"ID\": \"TXTz\", \"Size\": 7295, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(1, true);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData017JsonDefault()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(17)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(13, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVU\", \"Size\": 18203, \"Children\": [\n            { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 4961, \"Height\": 7016, \"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2 },\n            { \"ID\": \"INCL\", \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"{dict0013.iff}\" },\n            { \"ID\": \"Sjbz\", \"Size\": 10077, \"Description\": \"JB2 bilevel data\" },\n            { \"ID\": \"FGbz\", \"Size\": 63, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 3 },\n            { \"ID\": \"BG44\", \"Size\": 283, \"Description\": \"IW4 data #1\", \"Slices\": 97, \"Version\": 1.2, \"Color\": \"False\", \"Width\": 414, \"Height\": 585 },\n            { \"ID\": \"ANTz\", \"Size\": 399, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"ID\": \"TXTz\", \"Size\": 7295, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(1);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData017JsonParse()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(17)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(13, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                var data = document.DumpPageData(1, true);
                Assert.NotNull(data);
                object json = JsonConvert.DeserializeObject(data);
                Assert.NotNull(json);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData023()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(23)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(33, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = 
                    "  FORM:DJVU [188579] \n" + 
                    "    INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2\n" + 
                    "    INCL [12]         Indirection chunk --> {dict0020.iff}\n" + 
                    "    Sjbz [19225]      JB2 bilevel data\n" + 
                    "    FGbz [1873]       JB2 colors data, v0, 255 colors\n" + 
                    "    BG44 [35097]      IW4 data #1, 72 slices, v1.2 (color), 1700x2200\n" + 
                    "    BG44 [32409]      IW4 data #2, 11 slices\n" + 
                    "    BG44 [40554]      IW4 data #3, 10 slices\n" + 
                    "    BG44 [55043]      IW4 data #4, 10 slices\n" + 
                    "    ANTz [186]        Page annotation (hyperlinks, etc.)\n" + 
                    "    TXTz [4081]       Hidden text (text, etc.)\n";

                var data = document.DumpPageData(2, false);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData023Json()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(23)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(33, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVU\", \"Size\": 188579, \"Children\": [\n            { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 5100, \"Height\": 6600, \"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2 },\n            { \"ID\": \"INCL\", \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"{dict0020.iff}\" },\n            { \"ID\": \"Sjbz\", \"Size\": 19225, \"Description\": \"JB2 bilevel data\" },\n            { \"ID\": \"FGbz\", \"Size\": 1873, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 255 },\n            { \"ID\": \"BG44\", \"Size\": 35097, \"Description\": \"IW4 data #1\", \"Slices\": 72, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 1700, \"Height\": 2200 },\n            { \"ID\": \"BG44\", \"Size\": 32409, \"Description\": \"IW4 data #2\", \"Slices\": 11 },\n            { \"ID\": \"BG44\", \"Size\": 40554, \"Description\": \"IW4 data #3\", \"Slices\": 10 },\n            { \"ID\": \"BG44\", \"Size\": 55043, \"Description\": \"IW4 data #4\", \"Slices\": 10 },\n            { \"ID\": \"ANTz\", \"Size\": 186, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"ID\": \"TXTz\", \"Size\": 4081, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(2, true);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data); 
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData023JsonDefault()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(23)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(33, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVU\", \"Size\": 188579, \"Children\": [\n            { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 5100, \"Height\": 6600, \"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2 },\n            { \"ID\": \"INCL\", \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"{dict0020.iff}\" },\n            { \"ID\": \"Sjbz\", \"Size\": 19225, \"Description\": \"JB2 bilevel data\" },\n            { \"ID\": \"FGbz\", \"Size\": 1873, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 255 },\n            { \"ID\": \"BG44\", \"Size\": 35097, \"Description\": \"IW4 data #1\", \"Slices\": 72, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 1700, \"Height\": 2200 },\n            { \"ID\": \"BG44\", \"Size\": 32409, \"Description\": \"IW4 data #2\", \"Slices\": 11 },\n            { \"ID\": \"BG44\", \"Size\": 40554, \"Description\": \"IW4 data #3\", \"Slices\": 10 },\n            { \"ID\": \"BG44\", \"Size\": 55043, \"Description\": \"IW4 data #4\", \"Slices\": 10 },\n            { \"ID\": \"ANTz\", \"Size\": 186, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"ID\": \"TXTz\", \"Size\": 4081, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(2);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData023JsonParse()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(23)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(33, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);
                var data = document.DumpPageData(2, true);
                Assert.NotNull(data);

                object json = JsonConvert.DeserializeObject(data);
                Assert.NotNull(json);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData001()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(62, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expectedStart = "  FORM:DJVM [2925001] \n    DIRM [603]        Document directory (bundled, 62 files 62 pages)\n    FORM:DJVU [25223] {mcguff1steclprimer01mcguf_0005.djvu} [P1]\n      INFO [10]         DjVu 2260x3669, v25, 400 dpi, gamma=2.2\n      CIDa [36] \n      Sjbz [10327]      JB2 bilevel data\n      FG44 [2056]       IW4 data #1, 100 slices, v1.2 (color), 189x306\n      BG44 [2773]       IW4 data #1, 74 slices, v1.2 (color), 754x1223\n      BG44 [2894]       IW4 data #2, 10 slices\n      BG44 [2009]       IW4 data #3, 4 slices\n      BG44 [4643]       IW4 data #4, 9 slices\n      TXTz [395]        Hidden text (text, etc.)\n    FORM:DJVU [11889] {mcguff1steclprimer01mcguf_0006.djvu} [P2]\n      INFO [10]         DjVu 2215x3639, v25, 400 dpi, gamma=2.2\n      CIDa [36] \n      Sjbz [5398]       JB2 bilevel data\n      FG44 [1910]       IW4 data #1, 100 slices, v1.2 (color), 185x304\n      BG44 [1119]       IW4 data #1, 74 slices, v1.2 (color), 739x1213\n      BG44 [797]        IW4 data #2, 10 slices\n      BG44 [263]   ";
                string expectedEnd = " INFO [10]         DjVu 2215x3669, v25, 400 dpi, gamma=2.2\n      CIDa [36] \n      Sjbz [5672]       JB2 bilevel data\n      FG44 [1867]       IW4 data #1, 100 slices, v1.2 (color), 185x306\n      BG44 [1213]       IW4 data #1, 74 slices, v1.2 (color), 739x1223\n      BG44 [898]        IW4 data #2, 10 slices\n      BG44 [295]        IW4 data #3, 4 slices\n      BG44 [1974]       IW4 data #4, 9 slices\n      TXTz [485]        Hidden text (text, etc.)\n    FORM:DJVU [13130] {mcguff1steclprimer01mcguf_0064.djvu} [P60]\n      INFO [10]         DjVu 2238x3639, v25, 400 dpi, gamma=2.2\n      CIDa [36] \n      Sjbz [6997]       JB2 bilevel data\n      FG44 [1961]       IW4 data #1, 100 slices, v1.2 (color), 187x304\n      BG44 [999]        IW4 data #1, 74 slices, v1.2 (color), 746x1213\n      BG44 [661]        IW4 data #2, 10 slices\n      BG44 [194]        IW4 data #3, 4 slices\n      BG44 [1482]       IW4 data #4, 9 slices\n      TXTz [710]        Hidden text (text, etc.)\n    FORM:DJVU [13966] {mcguff1steclprimer01mcguf_0065.djvu} [P61]\n      INFO [10]         DjVu 2215x3669, v25, 400 dpi, gamma=2.2\n      CIDa [36] \n      Sjbz [7227]       JB2 bilevel data\n      FG44 [2112]       IW4 data #1, 100 slices, v1.2 (color), 185x306\n      BG44 [1001]       IW4 data #1, 74 slices, v1.2 (color), 739x1223\n      BG44 [845]        IW4 data #2, 10 slices\n      BG44 [139]        IW4 data #3, 4 slices\n      BG44 [1897]       IW4 data #4, 9 slices\n      TXTz [618]        Hidden text (text, etc.)\n    FORM:DJVU [14833] {mcguff1steclprimer01mcguf_0066.djvu} [P62]\n      INFO [10]         DjVu 2238x3639, v25, 400 dpi, gamma=2.2\n      CIDa [36] \n      Sjbz [10293]      JB2 bilevel data\n      FG44 [1421]       IW4 data #1, 100 slices, v1.2 (color), 187x304\n      BG44 [929]        IW4 data #1, 74 slices, v1.2 (color), 746x1213\n      BG44 [363]        IW4 data #2, 10 slices\n      BG44 [16]         IW4 data #3, 4 slices\n      BG44 [886]        IW4 data #4, 9 slices\n      TXTz [799]        Hidden text (text, etc.)\n";
                var data = document.DumpDocumentData(false);
                Assert.NotNull(data);
                Assert.StartsWith(expectedStart, data);
                Assert.EndsWith(expectedEnd, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData001Json()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(62, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);
                string expectedStart = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVM\", \"Size\": 2925001, \"Children\": [\n            { \"ID\": \"DIRM\", \"Size\": 603, \"Description\": \"Document directory\", \"DocumentType\": \"bundled\", \"FileCount\": 62, \"PageCount\": 62 },\n            { \"ID\": \"FORM:DJVU\", \"Size\": 25223, \"Children\": [\n                    { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 2260, \"Height\": 3669, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2 },\n                    { \"ID\": \"CIDa\", \"Size\": 36 },\n                    { \"ID\": \"Sjbz\", \"Size\": 10327, \"Description\": \"JB2 bilevel data\" },\n                    { \"ID\": \"FG44\", \"Size\": 2056, \"Description\": \"IW4 data #1\", \"Slices\": 100, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 189, \"Height\": 306 },\n                    { \"ID\": \"BG44\", \"Size\": 2773, \"Description\": \"IW4 data #1\", \"Slices\": 74, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 754, \"Height\": 1223 },\n                    { \"ID\": \"BG44\", \"Size\": 2894, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n                    { \"ID\": \"BG44\", \"Size\": 2009,";
                string expectedEnd = "3\", \"Slices\": 4 },\n                    { \"ID\": \"BG44\", \"Size\": 1897, \"Description\": \"IW4 data #4\", \"Slices\": 9 },\n                    { \"ID\": \"TXTz\", \"Size\": 618, \"Description\": \"Hidden text (text, etc.)\" }\n                ]\n            },\n            { \"ID\": \"FORM:DJVU\", \"Size\": 14833, \"Children\": [\n                    { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 2238, \"Height\": 3639, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2 },\n                    { \"ID\": \"CIDa\", \"Size\": 36 },\n                    { \"ID\": \"Sjbz\", \"Size\": 10293, \"Description\": \"JB2 bilevel data\" },\n                    { \"ID\": \"FG44\", \"Size\": 1421, \"Description\": \"IW4 data #1\", \"Slices\": 100, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 187, \"Height\": 304 },\n                    { \"ID\": \"BG44\", \"Size\": 929, \"Description\": \"IW4 data #1\", \"Slices\": 74, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 746, \"Height\": 1213 },\n                    { \"ID\": \"BG44\", \"Size\": 363, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n                    { \"ID\": \"BG44\", \"Size\": 16, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n                    { \"ID\": \"BG44\", \"Size\": 886, \"Description\": \"IW4 data #4\", \"Slices\": 9 },\n                    { \"ID\": \"TXTz\", \"Size\": 799, \"Description\": \"Hidden text (text, etc.)\" }\n                ]\n            }\n        ]\n    }\n}\n";
                var data = document.DumpDocumentData(true);      
                Assert.NotNull(data);
                Assert.StartsWith(expectedStart, data);
                Assert.EndsWith(expectedEnd, data);   
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData001JsonDefault()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(62, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expectedStart = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVM\", \"Size\": 2925001, \"Children\": [\n            { \"ID\": \"DIRM\", \"Size\": 603, \"Description\": \"Document directory\", \"DocumentType\": \"bundled\", \"FileCount\": 62, \"PageCount\": 62 },\n            { \"ID\": \"FORM:DJVU\", \"Size\": 25223, \"Children\": [\n                    { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 2260, \"Height\": 3669, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2 },\n                    { \"ID\": \"CIDa\", \"Size\": 36 },\n                    { \"ID\": \"Sjbz\", \"Size\": 10327, \"Description\": \"JB2 bilevel data\" },\n                    { \"ID\": \"FG44\", \"Size\": 2056, \"Description\": \"IW4 data #1\", \"Slices\": 100, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 189, \"Height\": 306 },\n                    { \"ID\": \"BG44\", \"Size\": 2773, \"Description\": \"IW4 data #1\", \"Slices\": 74, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 754, \"Height\": 1223 },\n                    { \"ID\": \"BG44\", \"Size\": 2894, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n                    { \"ID\": \"BG44\", \"Size\": 2009,";
                string expectedEnd = "3\", \"Slices\": 4 },\n                    { \"ID\": \"BG44\", \"Size\": 1897, \"Description\": \"IW4 data #4\", \"Slices\": 9 },\n                    { \"ID\": \"TXTz\", \"Size\": 618, \"Description\": \"Hidden text (text, etc.)\" }\n                ]\n            },\n            { \"ID\": \"FORM:DJVU\", \"Size\": 14833, \"Children\": [\n                    { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 2238, \"Height\": 3639, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2 },\n                    { \"ID\": \"CIDa\", \"Size\": 36 },\n                    { \"ID\": \"Sjbz\", \"Size\": 10293, \"Description\": \"JB2 bilevel data\" },\n                    { \"ID\": \"FG44\", \"Size\": 1421, \"Description\": \"IW4 data #1\", \"Slices\": 100, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 187, \"Height\": 304 },\n                    { \"ID\": \"BG44\", \"Size\": 929, \"Description\": \"IW4 data #1\", \"Slices\": 74, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 746, \"Height\": 1213 },\n                    { \"ID\": \"BG44\", \"Size\": 363, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n                    { \"ID\": \"BG44\", \"Size\": 16, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n                    { \"ID\": \"BG44\", \"Size\": 886, \"Description\": \"IW4 data #4\", \"Slices\": 9 },\n                    { \"ID\": \"TXTz\", \"Size\": 799, \"Description\": \"Hidden text (text, etc.)\" }\n                ]\n            }\n        ]\n    }\n}\n";
                var data = document.DumpDocumentData();
                Assert.NotNull(data);
                Assert.StartsWith(expectedStart, data);
                Assert.EndsWith(expectedEnd, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData001JsonParse()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(1)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(62, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);
                var data = document.DumpDocumentData();
                Assert.NotNull(data);
                object json = JsonConvert.DeserializeObject(data);
                Assert.NotNull(json);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData023()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(23)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(33, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expectedStart = "  FORM:DJVM [2217878] \n    DIRM [348]        Document directory (bundled, 35 files 33 pages)\n    FORM:DJVI [12511] {dict0020.iff} [I]\n      Djbz [12499]      JB2 shared dictionary\n    FORM:DJVU [43535] {p0001.djvu} [P1]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2\n      INCL [12]         Indirection chunk --> {dict0020.iff}\n      Sjbz [29260]      JB2 bilevel data\n      FGbz [2013]       JB2 colors data, v0, 256 colors\n      BG44 [3264]       IW4 data #1, 72 slices, v1.2 (color), 1700x2200\n      BG44 [983]        IW4 data #2, 11 slices\n      BG44 [1345]       IW4 data #3, 10 slices\n      BG44 [1787]       IW4 data #4, 10 slices\n      ANTz [161]        Page annotation (hyperlinks, etc.)\n      TXTz [4611]       Hidden text (text, etc.)\n    FORM:DJVU [31328] {p0002.djvu} [P2]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2\n      INCL [12]         Indirection chunk --> {dict0020.iff}\n      Sjbz [19232]      JB2 bilevel data\n      FGbz [1907]       JB2 colors ";
                string expectedEnd = "     DjVu 5100x6600, v21, 600 dpi, gamma=2.2\n      INCL [12]         Indirection chunk --> {dict0033.iff}\n      Sjbz [20238]      JB2 bilevel data\n      FGbz [1886]       JB2 colors data, v0, 255 colors\n      BG44 [288]        IW4 data #1, 97 slices, v1.2 (b&w), 425x550\n      ANTz [584]        Page annotation (hyperlinks, etc.)\n      TXTz [4576]       Hidden text (text, etc.)\n    FORM:DJVU [29378] {p0032.djvu} [P32]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2\n      INCL [12]         Indirection chunk --> {dict0033.iff}\n      Sjbz [21197]      JB2 bilevel data\n      FGbz [1905]       JB2 colors data, v0, 255 colors\n      BG44 [288]        IW4 data #1, 97 slices, v1.2 (b&w), 425x550\n      ANTz [794]        Page annotation (hyperlinks, etc.)\n      TXTz [5110]       Hidden text (text, etc.)\n    FORM:DJVU [22258] {p0033.djvu} [P33]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2\n      INCL [12]         Indirection chunk --> {dict0033.iff}\n      Sjbz [16865]      JB2 bilevel data\n      FGbz [1875]       JB2 colors data, v0, 255 colors\n      BG44 [288]        IW4 data #1, 97 slices, v1.2 (b&w), 425x550\n      ANTz [389]        Page annotation (hyperlinks, etc.)\n      TXTz [2756]       Hidden text (text, etc.)\n";
                var data = document.DumpDocumentData(false);
                Assert.NotNull(data);
                Assert.StartsWith(expectedStart, data);
                Assert.EndsWith(expectedEnd, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData023Json()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(23)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(33, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expectedStart = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVM\", \"Size\": 2217878, \"Children\": [\n            { \"ID\": \"DIRM\", \"Size\": 348, \"Description\": \"Document directory\", \"DocumentType\": \"bundled\", \"FileCount\": 35, \"PageCount\": 33 },\n            { \"ID\": \"FORM:DJVI\", \"Size\": 12511, \"Children\": [\n                    { \"ID\": \"Djbz\", \"Size\": 12499, \"Description\": \"JB2 shared dictionary\" }\n                ]\n            },\n            { \"ID\": \"FORM:DJVU\", \"Size\": 43535, \"Children\": [\n                    { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 5100, \"Height\": 6600, \"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2 },\n                    { \"ID\": \"INCL\", \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"{dict0020.iff}\" },\n                    { \"ID\": \"Sjbz\", \"Size\": 29260, \"Description\": \"JB2 bilevel data\" },\n                    { \"ID\": \"FGbz\", \"Size\": 2013, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 256 },\n        ";
                string expectedEnd = "\"JB2 colors data\", \"Version\": 0, \"Colors\": 255 },\n                    { \"ID\": \"BG44\", \"Size\": 288, \"Description\": \"IW4 data #1\", \"Slices\": 97, \"Version\": 1.2, \"Color\": \"False\", \"Width\": 425, \"Height\": 550 },\n                    { \"ID\": \"ANTz\", \"Size\": 389, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n                    { \"ID\": \"TXTz\", \"Size\": 2756, \"Description\": \"Hidden text (text, etc.)\" }\n                ]\n            }\n        ]\n    }\n}\n";
                var data = document.DumpDocumentData(true);
                Assert.NotNull(data);
                Assert.StartsWith(expectedStart, data);
                Assert.EndsWith(expectedEnd, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData023JsonParse()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(23)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(33, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);
                var data = document.DumpDocumentData(true);

                object json = JsonConvert.DeserializeObject(data);
                Assert.NotNull(json);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData030Json()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(30)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(1, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.SinglePage, type);

                string expected = "{ \"DjvuData\":\n    { \"ID\": \"FORM:DJVU\", \"Size\": 146736, \"Children\": [\n            { \"ID\": \"INFO\", \"Size\": 10, \"Width\": 6518, \"Height\": 9399, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2 },\n            { \"ID\": \"CIDa\", \"Size\": 36 },\n            { \"ID\": \"Sjbz\", \"Size\": 84753, \"Description\": \"JB2 bilevel data\" },\n            { \"ID\": \"FG44\", \"Size\": 31845, \"Description\": \"IW4 data #1\", \"Slices\": 100, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 544, \"Height\": 784 },\n            { \"ID\": \"BG44\", \"Size\": 7015, \"Description\": \"IW4 data #1\", \"Slices\": 74, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 1630, \"Height\": 2350 },\n            { \"ID\": \"BG44\", \"Size\": 6576, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n            { \"ID\": \"BG44\", \"Size\": 1245, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n            { \"ID\": \"BG44\", \"Size\": 15184, \"Description\": \"IW4 data #4\", \"Slices\": 9 }\n        ]\n    }\n}\n";
                var data = document.DumpDocumentData(true);
                Assert.NotNull(data);
                Assert.Equal<string>(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentDataAndParse030()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(30)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(1, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.SinglePage, type);
                var data = document.DumpDocumentData(true);

                object json = JsonConvert.DeserializeObject(data);
                Assert.NotNull(json);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DocumentAnnotation002()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(2)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(13, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);
                string annotation = document.GetDocumentAnnotation();

                Assert.Null(annotation);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DocumentAnnotation077()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(77)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(21, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);
                string annotation = document.GetDocumentAnnotation();

                Assert.NotNull(annotation);
                Assert.Equal<string>("eLife", annotation);
            }
        }
    }
}