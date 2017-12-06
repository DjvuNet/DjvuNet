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
                Assert.Equal<int>(31, pageCount);
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
                Assert.Equal<int>(101, pageCount);
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
                Assert.Equal<int>(101, pageCount);
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
                Assert.Equal<int>(112, fileCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                var info = document.GetFileInfo(0);
                Assert.NotNull(info);
                Assert.IsType<DjvuFileInfo>(info);
                Assert.Equal<int>('I', info.Type);
                Assert.Equal<int>(3364, info.Size);
                Assert.True(info.PageNumber < 0);
                Assert.Equal("Schultens_Страница_005_1L.djbz", info.Title);
                Assert.Equal("Schultens_Страница_005_1L.djbz", info.Name);
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
                Assert.Equal<int>(31, fileCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "  FORM:DJVU [11894] \n    INFO [10]         DjVu 2215x3639, v25, 400 dpi, gamma=2.2, orientation=0\n    CIDa [36] \n    Sjbz [5398]       JB2 bilevel data\n    FG44 [1910]       IW4 data #1, 100 slices, v1.2 (color), 185x304\n    BG44 [1119]       IW4 data #1, 74 slices, v1.2 (color), 739x1213\n    BG44 [797]        IW4 data #2, 10 slices\n    BG44 [263]        IW4 data #3, 4 slices\n    BG44 [1878]       IW4 data #4, 9 slices\n    TXTz [375]        Hidden text (text, etc.)\n    ANTz [19]         Page annotation (hyperlinks, etc.)\n";

                var data = document.DumpPageData(1, false);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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
                Assert.Equal<int>(31, fileCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 11894, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12, \"Size\": 10, \"Width\": 2215, \"Height\": 3639, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Cida\", \"ID\": \"CIDa\", \"NodeOffset\": 30, \"Size\": 36 },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 74, \"Size\": 5398, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FG44\", \"ID\": \"FG44\", \"NodeOffset\": 5480, \"Size\": 1910, \"Description\": \"IW4 data #1\", \"Slices\": 100, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 185, \"Height\": 304 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 7398, \"Size\": 1119, \"Description\": \"IW4 data #1\", \"Slices\": 74, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 739, \"Height\": 1213 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 8526, \"Size\": 797, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 9332, \"Size\": 263, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 9604, \"Size\": 1878, \"Description\": \"IW4 data #4\", \"Slices\": 9 },\n            { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 11490, \"Size\": 375, \"Description\": \"Hidden text (text, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 11874, \"Size\": 19, \"Description\": \"Page annotation (hyperlinks, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(1, true);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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
                Assert.Equal<int>(31, fileCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 11894, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12, \"Size\": 10, \"Width\": 2215, \"Height\": 3639, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Cida\", \"ID\": \"CIDa\", \"NodeOffset\": 30, \"Size\": 36 },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 74, \"Size\": 5398, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FG44\", \"ID\": \"FG44\", \"NodeOffset\": 5480, \"Size\": 1910, \"Description\": \"IW4 data #1\", \"Slices\": 100, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 185, \"Height\": 304 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 7398, \"Size\": 1119, \"Description\": \"IW4 data #1\", \"Slices\": 74, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 739, \"Height\": 1213 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 8526, \"Size\": 797, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 9332, \"Size\": 263, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 9604, \"Size\": 1878, \"Description\": \"IW4 data #4\", \"Slices\": 9 },\n            { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 11490, \"Size\": 375, \"Description\": \"Hidden text (text, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 11874, \"Size\": 19, \"Description\": \"Page annotation (hyperlinks, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(1);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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
                Assert.Equal<int>(31, fileCount);
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

                string expected = "  FORM:DJVU [24134] \n    INFO [10]         DjVu 2480x3260, v21, 300 dpi, gamma=2.2, orientation=0\n    INCL [12]         Indirection chunk --> {dict0013.iff}\n    Sjbz [13542]      JB2 bilevel data\n    FGbz [85]         JB2 colors data, v0, 8 colors\n    BG44 [1119]       IW4 data #1, 72 slices, v1.2 (color), 827x1087\n    BG44 [311]        IW4 data #2, 11 slices\n    BG44 [892]        IW4 data #3, 10 slices\n    BG44 [791]        IW4 data #4, 10 slices\n    ANTz [117]        Page annotation (hyperlinks, etc.)\n    TXTz [7166]       Hidden text (text, etc.)\n";

                var data = document.DumpPageData(0, false);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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

                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 24134, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12, \"Size\": 10, \"Width\": 2480, \"Height\": 3260, \"Version\": 21, \"Dpi\": 300, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 30, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"dict0013.iff\" },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 50, \"Size\": 13542, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 13600, \"Size\": 85, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 8 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 13694, \"Size\": 1119, \"Description\": \"IW4 data #1\", \"Slices\": 72, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 827, \"Height\": 1087 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 14822, \"Size\": 311, \"Description\": \"IW4 data #2\", \"Slices\": 11 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 15142, \"Size\": 892, \"Description\": \"IW4 data #3\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 16042, \"Size\": 791, \"Description\": \"IW4 data #4\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 16842, \"Size\": 117, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 16968, \"Size\": 7166, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(0, true);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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

                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 24134, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12, \"Size\": 10, \"Width\": 2480, \"Height\": 3260, \"Version\": 21, \"Dpi\": 300, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 30, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"dict0013.iff\" },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 50, \"Size\": 13542, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 13600, \"Size\": 85, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 8 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 13694, \"Size\": 1119, \"Description\": \"IW4 data #1\", \"Slices\": 72, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 827, \"Height\": 1087 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 14822, \"Size\": 311, \"Description\": \"IW4 data #2\", \"Slices\": 11 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 15142, \"Size\": 892, \"Description\": \"IW4 data #3\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 16042, \"Size\": 791, \"Description\": \"IW4 data #4\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 16842, \"Size\": 117, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 16968, \"Size\": 7166, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(0);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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
                Assert.Equal<int>(101, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "  FORM:DJVU [27068] \n    INFO [10]         DjVu 3916x6054, v25, 600 dpi, gamma=2.2, orientation=0\n    INCL [38]         Indirection chunk --> {Schultens_Страница_005_1L.djbz}\n    Sjbz [5941]       JB2 bilevel data\n    FGbz [110]        JB2 colors data, v0, 14 colors\n    BG44 [6577]       IW4 data #1, 75 slices, v1.2 (color), 979x1514\n    BG44 [4057]       IW4 data #2, 10 slices\n    BG44 [3433]       IW4 data #3, 4 slices\n    BG44 [6315]       IW4 data #4, 8 slices\n    TXTz [477]        Hidden text (text, etc.)\n    ANTz [19]         Page annotation (hyperlinks, etc.)\n";

                var data = document.DumpPageData(0, false);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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
                Assert.Equal<int>(101, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 27068, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12, \"Size\": 10, \"Width\": 3916, \"Height\": 6054, \"Version\": 25, \"Dpi\": 600, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 30, \"Size\": 38, \"Description\": \"Indirection chunk\", \"Name\": \"Schultens_Страница_005_1L.djbz\" },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 76, \"Size\": 5941, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 6026, \"Size\": 110, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 14 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 6144, \"Size\": 6577, \"Description\": \"IW4 data #1\", \"Slices\": 75, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 979, \"Height\": 1514 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 12730, \"Size\": 4057, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 16796, \"Size\": 3433, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 20238, \"Size\": 6315, \"Description\": \"IW4 data #4\", \"Slices\": 8 },\n            { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 26562, \"Size\": 477, \"Description\": \"Hidden text (text, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 27048, \"Size\": 19, \"Description\": \"Page annotation (hyperlinks, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(0, true);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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
                Assert.Equal<int>(101, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 27068, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12, \"Size\": 10, \"Width\": 3916, \"Height\": 6054, \"Version\": 25, \"Dpi\": 600, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 30, \"Size\": 38, \"Description\": \"Indirection chunk\", \"Name\": \"Schultens_Страница_005_1L.djbz\" },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 76, \"Size\": 5941, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 6026, \"Size\": 110, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 14 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 6144, \"Size\": 6577, \"Description\": \"IW4 data #1\", \"Slices\": 75, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 979, \"Height\": 1514 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 12730, \"Size\": 4057, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 16796, \"Size\": 3433, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 20238, \"Size\": 6315, \"Description\": \"IW4 data #4\", \"Slices\": 8 },\n            { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 26562, \"Size\": 477, \"Description\": \"Hidden text (text, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 27048, \"Size\": 19, \"Description\": \"Page annotation (hyperlinks, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(0);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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
                Assert.Equal<int>(101, pageCount);
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

                string expected = "  FORM:DJVU [18203] \n    INFO [10]         DjVu 4961x7016, v21, 600 dpi, gamma=2.2, orientation=0\n    INCL [12]         Indirection chunk --> {dict0013.iff}\n    Sjbz [10077]      JB2 bilevel data\n    FGbz [63]         JB2 colors data, v0, 3 colors\n    BG44 [283]        IW4 data #1, 97 slices, v1.2 (b&w), 414x585\n    ANTz [399]        Page annotation (hyperlinks, etc.)\n    TXTz [7295]       Hidden text (text, etc.)\n";

                var data = document.DumpPageData(1, false);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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

                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 18203, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12, \"Size\": 10, \"Width\": 4961, \"Height\": 7016, \"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 30, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"dict0013.iff\" },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 50, \"Size\": 10077, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 10136, \"Size\": 63, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 3 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 10208, \"Size\": 283, \"Description\": \"IW4 data #1\", \"Slices\": 97, \"Version\": 1.2, \"Color\": \"False\", \"Width\": 414, \"Height\": 585 },\n            { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 10500, \"Size\": 399, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 10908, \"Size\": 7295, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(1, true);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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

                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 18203, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12, \"Size\": 10, \"Width\": 4961, \"Height\": 7016, \"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 30, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"dict0013.iff\" },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 50, \"Size\": 10077, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 10136, \"Size\": 63, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 3 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 10208, \"Size\": 283, \"Description\": \"IW4 data #1\", \"Slices\": 97, \"Version\": 1.2, \"Color\": \"False\", \"Width\": 414, \"Height\": 585 },\n            { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 10500, \"Size\": 399, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 10908, \"Size\": 7295, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(1);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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

                string expected = "  FORM:DJVU [188579] \n    INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2, orientation=0\n    INCL [12]         Indirection chunk --> {dict0020.iff}\n    Sjbz [19225]      JB2 bilevel data\n    FGbz [1873]       JB2 colors data, v0, 255 colors\n    BG44 [35097]      IW4 data #1, 72 slices, v1.2 (color), 1700x2200\n    BG44 [32409]      IW4 data #2, 11 slices\n    BG44 [40554]      IW4 data #3, 10 slices\n    BG44 [55043]      IW4 data #4, 10 slices\n    ANTz [186]        Page annotation (hyperlinks, etc.)\n    TXTz [4081]       Hidden text (text, etc.)\n";

                var data = document.DumpPageData(2, false);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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

                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 188579, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12, \"Size\": 10, \"Width\": 5100, \"Height\": 6600, \"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 30, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"dict0020.iff\" },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 50, \"Size\": 19225, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 19284, \"Size\": 1873, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 255 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 21166, \"Size\": 35097, \"Description\": \"IW4 data #1\", \"Slices\": 72, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 1700, \"Height\": 2200 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 56272, \"Size\": 32409, \"Description\": \"IW4 data #2\", \"Slices\": 11 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 88690, \"Size\": 40554, \"Description\": \"IW4 data #3\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 129252, \"Size\": 55043, \"Description\": \"IW4 data #4\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 184304, \"Size\": 186, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 184498, \"Size\": 4081, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(2, true);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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
                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 188579, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12, \"Size\": 10, \"Width\": 5100, \"Height\": 6600, \"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 30, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"dict0020.iff\" },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 50, \"Size\": 19225, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 19284, \"Size\": 1873, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 255 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 21166, \"Size\": 35097, \"Description\": \"IW4 data #1\", \"Slices\": 72, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 1700, \"Height\": 2200 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 56272, \"Size\": 32409, \"Description\": \"IW4 data #2\", \"Slices\": 11 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 88690, \"Size\": 40554, \"Description\": \"IW4 data #3\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 129252, \"Size\": 55043, \"Description\": \"IW4 data #4\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 184304, \"Size\": 186, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 184498, \"Size\": 4081, \"Description\": \"Hidden text (text, etc.)\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(2);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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
        public void DumpPageData077()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(77)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(21, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "  FORM:DJVU [14606] \n    INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2, orientation=0\n    INCL [12]         Indirection chunk --> {dict0020.iff}\n    Sjbz [8238]       JB2 bilevel data\n    FGbz [114]        JB2 colors data, v0, 8 colors\n    BG44 [288]        IW4 data #1, 97 slices, v1.2 (b&w), 425x550\n    ANTz [377]        Page annotation (hyperlinks, etc.)\n    TXTz [5486]       Hidden text (text, etc.)\n    INCL [12]         Indirection chunk --> {anno0001.iff}\n";

                var data = document.DumpPageData(2, false);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData077Json()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(77)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(21, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 14606, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12, \"Size\": 10, \"Width\": 5100, \"Height\": 6600, \"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 30, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"dict0020.iff\" },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 50, \"Size\": 8238, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 8296, \"Size\": 114, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 8 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 8418, \"Size\": 288, \"Description\": \"IW4 data #1\", \"Slices\": 97, \"Version\": 1.2, \"Color\": \"False\", \"Width\": 425, \"Height\": 550 },\n            { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 8714, \"Size\": 377, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 9100, \"Size\": 5486, \"Description\": \"Hidden text (text, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 14594, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"anno0001.iff\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(2, true);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData077JsonDefault()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(77)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(21, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 14606, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12, \"Size\": 10, \"Width\": 5100, \"Height\": 6600, \"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 30, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"dict0020.iff\" },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 50, \"Size\": 8238, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 8296, \"Size\": 114, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 8 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 8418, \"Size\": 288, \"Description\": \"IW4 data #1\", \"Slices\": 97, \"Version\": 1.2, \"Color\": \"False\", \"Width\": 425, \"Height\": 550 },\n            { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 8714, \"Size\": 377, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 9100, \"Size\": 5486, \"Description\": \"Hidden text (text, etc.)\" },\n            { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 14594, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"anno0001.iff\" }\n        ]\n    }\n}\n";

                var data = document.DumpPageData(2);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpPageData077JsonParse()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(77)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(21, pageCount);
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
                Assert.Equal<int>(31, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expectedStart = "  FORM:DJVM [1634952] \n    DIRM [338]        Document directory (bundled, 31 files 31 pages)\n    FORM:DJVU [25228] {mcguff1steclprimer01mcguf_0005.djvu} [P1]\n      INFO [10]         DjVu 2260x3669, v25, 400 dpi, gamma=2.2, orientation=0\n      CIDa [36] \n      Sjbz [10327]      JB2 bilevel data\n      FG44 [2056]       IW4 data #1, 100 slices, v1.2 (color), 189x306\n      BG44 [2773]       IW4 data #1, 74 slices, v1.2 (color), 754x1223\n      BG44 [2894]       IW4 data #2, 10 slices\n      BG44 [2009]       IW4 data #3, 4 slices\n      BG44 [4643]       IW4 data #4, 9 slices\n      TXTz [372]        Hidden text (text, etc.)\n      ANTz [19]         Page annotation (hyperlinks, etc.)\n    FORM:DJVU [11894] {mcguff1steclprimer01mcguf_0006.djvu} [P2]\n      INFO [10]         DjVu 2215x3639, v25, 400 dpi, gamma=2.2, orientation=0\n      CIDa [36] \n      Sjbz [5398]       JB2 bilevel data\n      FG44 [1910]       IW4 data #1, 100 slices, v1.2 (color), 185x304\n      BG44 [1119]       IW4 data #1, 74 slices, v1.2 (color), 739x1213\n      BG44 [797]        IW4 data #2, 10 slices\n      BG44 [263]        IW4 data #3, 4 slices\n      BG44 [1878]       IW4 data #4, 9 slices\n      TXTz [375]        Hidden text (text, etc.)\n      ANTz [19]         Page annotation (hyperlinks, etc.)\n    FORM:DJVU [8214] {mcguff1steclprimer01mcguf_0007.djvu} [P3]\n      INFO [10]         DjVu 2215x3639, v25, 400 dpi, gamma=2.2, orientation=0\n      CIDa [36] \n      Sjbz [4484]       JB2 bilevel data\n      FG44 [1022]       IW4 data #1, 100 slices, v1.2 (color), 185x304\n      BG44 [848]        IW4 data #1, 74 slices, v1.2 (color), 739x1213\n      BG44 [403]        IW4 data #2, 10 slices\n      BG44 [34]         IW4 data #3, 4 slices\n      BG44 [858]        IW4 data #4, 9 slices\n      TXTz [414]        Hidden text (text, etc.)\n      ANTz [19]         Page annotation (hyperlinks, etc.)\n    FORM:DJVU [101782] {mcguff1steclprimer01mcguf_0008.djvu} [P4]\n      INFO [10]         DjVu 2215x3639, v25, 400 dpi, gamma=2.2, orientation=0\n ";
                string expectedEnd = "TXTz [786]        Hidden text (text, etc.)\n      ANTz [19]         Page annotation (hyperlinks, etc.)\n    FORM:DJVU [114994] {mcguff1steclprimer01mcguf_0034.djvu} [P30]\n      INFO [10]         DjVu 2238x3639, v25, 400 dpi, gamma=2.2, orientation=0\n      CIDa [36] \n      Sjbz [41920]      JB2 bilevel data\n      FG44 [5456]       IW4 data #1, 100 slices, v1.2 (color), 187x304\n      BG44 [8783]       IW4 data #1, 74 slices, v1.2 (color), 746x1213\n      BG44 [12339]      IW4 data #2, 10 slices\n      BG44 [15941]      IW4 data #3, 4 slices\n      BG44 [30279]      IW4 data #4, 9 slices\n      TXTz [121]        Hidden text (text, etc.)\n      ANTz [19]         Page annotation (hyperlinks, etc.)\n    FORM:DJVU [20228] {mcguff1steclprimer01mcguf_0035.djvu} [P31]\n      INFO [10]         DjVu 2215x3669, v25, 400 dpi, gamma=2.2, orientation=0\n      CIDa [36] \n      Sjbz [11471]      JB2 bilevel data\n      FG44 [2315]       IW4 data #1, 100 slices, v1.2 (color), 185x306\n      BG44 [1054]       IW4 data #1, 74 slices, v1.2 (color), 739x1223\n      BG44 [1172]       IW4 data #2, 10 slices\n      BG44 [522]        IW4 data #3, 4 slices\n      BG44 [2612]       IW4 data #4, 9 slices\n      TXTz [929]        Hidden text (text, etc.)\n      ANTz [19]         Page annotation (hyperlinks, etc.)\n";
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
                Assert.Equal<int>(31, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);
                string expectedStart = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvmForm\", \"ID\": \"FORM:DJVM\", \"NodeOffset\": 0, \"Size\": 1634952, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Dirm\", \"ID\": \"DIRM\", \"NodeOffset\": 16, \"Size\": 338, \"Description\": \"Document directory\", \"DocumentType\": \"bundled\", \"FileCount\": 31, \"PageCount\": 31 },\n            { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 362, \"Size\": 25228, \"Children\": [\n                    { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 374, \"Size\": 10, \"Width\": 2260, \"Height\": 3669, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2, \"Orientation\": 0 },\n                    { \"$type\": \"DjvuNet.Serialization.Cida\", \"ID\": \"CIDa\", \"NodeOffset\": 392, \"Size\": 36 },\n                    { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 436, \"Size\": 10327,";
                string expectedEnd = "\", \"NodeOffset\": 1614728, \"Size\": 20228, \"Children\": [\n                    { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 1614740, \"Size\": 10, \"Width\": 2215, \"Height\": 3669, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2, \"Orientation\": 0 },\n                    { \"$type\": \"DjvuNet.Serialization.Cida\", \"ID\": \"CIDa\", \"NodeOffset\": 1614758, \"Size\": 36 },\n                    { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 1614802, \"Size\": 11471, \"Description\": \"JB2 bilevel data\" },\n                    { \"$type\": \"DjvuNet.Serialization.FG44\", \"ID\": \"FG44\", \"NodeOffset\": 1626282, \"Size\": 2315, \"Description\": \"IW4 data #1\", \"Slices\": 100, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 185, \"Height\": 306 },\n                    { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 1628606, \"Size\": 1054, \"Description\": \"IW4 data #1\", \"Slices\": 74, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 739, \"Height\": 1223 },\n                    { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 1629668, \"Size\": 1172, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n                    { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 1630848, \"Size\": 522, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n                    { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 1631378, \"Size\": 2612, \"Description\": \"IW4 data #4\", \"Slices\": 9 },\n                    { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 1633998, \"Size\": 929, \"Description\": \"Hidden text (text, etc.)\" },\n                    { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 1634936, \"Size\": 19, \"Description\": \"Page annotation (hyperlinks, etc.)\" }\n                ]\n            }\n        ]\n    }\n}\n";
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
                Assert.Equal<int>(31, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expectedStart = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvmForm\", \"ID\": \"FORM:DJVM\", \"NodeOffset\": 0, \"Size\": 1634952, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Dirm\", \"ID\": \"DIRM\", \"NodeOffset\": 16, \"Size\": 338, \"Description\": \"Document directory\", \"DocumentType\": \"bundled\", \"FileCount\": 31, \"PageCount\": 31 },\n            { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 362, \"Size\": 25228, \"Children\": [\n                    { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 374, \"Size\": 10, \"Width\": 2260, \"Height\": 3669, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2, \"Orientation\": 0 },\n                    { \"$type\": \"DjvuNet.Serialization.Cida\", \"ID\": \"CIDa\", \"NodeOffset\": 392, \"Size\": 36 },\n                    { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 436, \"Size\": 10327,";
                string expectedEnd = "\", \"NodeOffset\": 1614728, \"Size\": 20228, \"Children\": [\n                    { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 1614740, \"Size\": 10, \"Width\": 2215, \"Height\": 3669, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2, \"Orientation\": 0 },\n                    { \"$type\": \"DjvuNet.Serialization.Cida\", \"ID\": \"CIDa\", \"NodeOffset\": 1614758, \"Size\": 36 },\n                    { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 1614802, \"Size\": 11471, \"Description\": \"JB2 bilevel data\" },\n                    { \"$type\": \"DjvuNet.Serialization.FG44\", \"ID\": \"FG44\", \"NodeOffset\": 1626282, \"Size\": 2315, \"Description\": \"IW4 data #1\", \"Slices\": 100, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 185, \"Height\": 306 },\n                    { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 1628606, \"Size\": 1054, \"Description\": \"IW4 data #1\", \"Slices\": 74, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 739, \"Height\": 1223 },\n                    { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 1629668, \"Size\": 1172, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n                    { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 1630848, \"Size\": 522, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n                    { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 1631378, \"Size\": 2612, \"Description\": \"IW4 data #4\", \"Slices\": 9 },\n                    { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 1633998, \"Size\": 929, \"Description\": \"Hidden text (text, etc.)\" },\n                    { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 1634936, \"Size\": 19, \"Description\": \"Page annotation (hyperlinks, etc.)\" }\n                ]\n            }\n        ]\n    }\n}\n";
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
                Assert.Equal<int>(31, pageCount);
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

                string expectedStart = "  FORM:DJVM [2217878] \n    DIRM [348]        Document directory (bundled, 35 files 33 pages)\n    FORM:DJVI [12511] {dict0020.iff} [I]\n      Djbz [12499]      JB2 shared dictionary\n    FORM:DJVU [43535] {p0001.djvu} [P1]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2, orientation=0\n      INCL [12]         Indirection chunk --> {dict0020.iff}\n      Sjbz [29260]      JB2 bilevel data\n      FGbz [2013]       JB2 colors data, v0, 256 colors\n      BG44 [3264]       IW4 data #1, 72 slices, v1.2 (color), 1700x2200\n      BG44 [983]        IW4 data #2, 11 slices\n      BG44 [1345]       IW4 data #3, 10 slices\n      BG44 [1787]       IW4 data #4, 10 slices\n      ANTz [161]        Page annotation (hyperlinks, etc.)\n      TXTz [4611]       Hidden text (text, etc.)\n    FORM:DJVU [31328] {p0002.djvu} [P2]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2, orientation=0\n      INCL [12]         Indirection chunk --> {dict0020.iff}\n      Sjbz [19232]      JB2 bilevel data\n      ";
                string expectedEnd = "INCL [12]         Indirection chunk --> {dict0033.iff}\n      Sjbz [20985]      JB2 bilevel data\n      FGbz [1897]       JB2 colors data, v0, 255 colors\n      BG44 [288]        IW4 data #1, 97 slices, v1.2 (b&w), 425x550\n      ANTz [742]        Page annotation (hyperlinks, etc.)\n      TXTz [4831]       Hidden text (text, etc.)\n    FORM:DJVU [27654] {p0031.djvu} [P31]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2, orientation=0\n      INCL [12]         Indirection chunk --> {dict0033.iff}\n      Sjbz [20238]      JB2 bilevel data\n      FGbz [1886]       JB2 colors data, v0, 255 colors\n      BG44 [288]        IW4 data #1, 97 slices, v1.2 (b&w), 425x550\n      ANTz [584]        Page annotation (hyperlinks, etc.)\n      TXTz [4576]       Hidden text (text, etc.)\n    FORM:DJVU [29378] {p0032.djvu} [P32]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2, orientation=0\n      INCL [12]         Indirection chunk --> {dict0033.iff}\n      Sjbz [21197]      JB2 bilevel data\n      FGbz [1905]       JB2 colors data, v0, 255 colors\n      BG44 [288]        IW4 data #1, 97 slices, v1.2 (b&w), 425x550\n      ANTz [794]        Page annotation (hyperlinks, etc.)\n      TXTz [5110]       Hidden text (text, etc.)\n    FORM:DJVU [22258] {p0033.djvu} [P33]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2, orientation=0\n      INCL [12]         Indirection chunk --> {dict0033.iff}\n      Sjbz [16865]      JB2 bilevel data\n      FGbz [1875]       JB2 colors data, v0, 255 colors\n      BG44 [288]        IW4 data #1, 97 slices, v1.2 (b&w), 425x550\n      ANTz [389]        Page annotation (hyperlinks, etc.)\n      TXTz [2756]       Hidden text (text, etc.)\n";
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

                string expectedStart = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvmForm\", \"ID\": \"FORM:DJVM\", \"NodeOffset\": 0, \"Size\": 2217878, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Dirm\", \"ID\": \"DIRM\", \"NodeOffset\": 16, \"Size\": 348, \"Description\": \"Document directory\", \"DocumentType\": \"bundled\", \"FileCount\": 35, \"PageCount\": 33 },\n            { \"$type\": \"DjvuNet.Serialization.DjviForm\", \"ID\": \"FORM:DJVI\", \"NodeOffset\": 372, \"Size\": 12511, \"Children\": [\n                    { \"$type\": \"DjvuNet.Serialization.Djbz\", \"ID\": \"Djbz\", \"NodeOffset\": 384, \"Size\": 12499, \"Description\": \"JB2 shared dictionary\" }\n                ]\n            },\n            { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 12892, \"Size\": 43535, \"Children\": [\n                    { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12904, \"Size\": 10, \"Wid";
                string expectedEnd = "\"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2, \"Orientation\": 0 },\n                    { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 2195654, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"dict0033.iff\" },\n                    { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 2195674, \"Size\": 16865, \"Description\": \"JB2 bilevel data\" },\n                    { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 2212548, \"Size\": 1875, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 255 },\n                    { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 2214432, \"Size\": 288, \"Description\": \"IW4 data #1\", \"Slices\": 97, \"Version\": 1.2, \"Color\": \"False\", \"Width\": 425, \"Height\": 550 },\n                    { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 2214728, \"Size\": 389, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n                    { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 2215126, \"Size\": 2756, \"Description\": \"Hidden text (text, etc.)\" }\n                ]\n            }\n        ]\n    }\n}\n";
                var data = document.DumpDocumentData(true);
                Assert.NotNull(data);
                Assert.StartsWith(expectedStart, data);
                Assert.EndsWith(expectedEnd, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData023JsonDefault()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(23)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(33, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expectedStart = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvmForm\", \"ID\": \"FORM:DJVM\", \"NodeOffset\": 0, \"Size\": 2217878, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Dirm\", \"ID\": \"DIRM\", \"NodeOffset\": 16, \"Size\": 348, \"Description\": \"Document directory\", \"DocumentType\": \"bundled\", \"FileCount\": 35, \"PageCount\": 33 },\n            { \"$type\": \"DjvuNet.Serialization.DjviForm\", \"ID\": \"FORM:DJVI\", \"NodeOffset\": 372, \"Size\": 12511, \"Children\": [\n                    { \"$type\": \"DjvuNet.Serialization.Djbz\", \"ID\": \"Djbz\", \"NodeOffset\": 384, \"Size\": 12499, \"Description\": \"JB2 shared dictionary\" }\n                ]\n            },\n            { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 12892, \"Size\": 43535, \"Children\": [\n                    { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 12904, \"Size\": 10, \"Wid";
                string expectedEnd = "\"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2, \"Orientation\": 0 },\n                    { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 2195654, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"dict0033.iff\" },\n                    { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 2195674, \"Size\": 16865, \"Description\": \"JB2 bilevel data\" },\n                    { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 2212548, \"Size\": 1875, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 255 },\n                    { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 2214432, \"Size\": 288, \"Description\": \"IW4 data #1\", \"Slices\": 97, \"Version\": 1.2, \"Color\": \"False\", \"Width\": 425, \"Height\": 550 },\n                    { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 2214728, \"Size\": 389, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n                    { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 2215126, \"Size\": 2756, \"Description\": \"Hidden text (text, etc.)\" }\n                ]\n            }\n        ]\n    }\n}\n";
                var data = document.DumpDocumentData();
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
        public void DumpDocumentData030()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(30)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(1, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.SinglePage, type);

                string expected = "  FORM:DJVU [146736] \n    INFO [10]         DjVu 6518x9399, v25, 400 dpi, gamma=2.2, orientation=0\n    CIDa [36] \n    Sjbz [84753]      JB2 bilevel data\n    FG44 [31845]      IW4 data #1, 100 slices, v1.2 (color), 544x784\n    BG44 [7015]       IW4 data #1, 74 slices, v1.2 (color), 1630x2350\n    BG44 [6576]       IW4 data #2, 10 slices\n    BG44 [1245]       IW4 data #3, 4 slices\n    BG44 [15184]      IW4 data #4, 9 slices\n";
                var data = document.DumpDocumentData(false);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
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

                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 146736, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 16, \"Size\": 10, \"Width\": 6518, \"Height\": 9399, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Cida\", \"ID\": \"CIDa\", \"NodeOffset\": 34, \"Size\": 36 },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 78, \"Size\": 84753, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FG44\", \"ID\": \"FG44\", \"NodeOffset\": 84840, \"Size\": 31845, \"Description\": \"IW4 data #1\", \"Slices\": 100, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 544, \"Height\": 784 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 116694, \"Size\": 7015, \"Description\": \"IW4 data #1\", \"Slices\": 74, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 1630, \"Height\": 2350 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 123718, \"Size\": 6576, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 130302, \"Size\": 1245, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 131556, \"Size\": 15184, \"Description\": \"IW4 data #4\", \"Slices\": 9 }\n        ]\n    }\n}\n";
                var data = document.DumpDocumentData(true);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData030JsonDefault()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(30)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(1, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.SinglePage, type);

                string expected = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvuForm\", \"ID\": \"FORM:DJVU\", \"NodeOffset\": 0, \"Size\": 146736, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Info\", \"ID\": \"INFO\", \"NodeOffset\": 16, \"Size\": 10, \"Width\": 6518, \"Height\": 9399, \"Version\": 25, \"Dpi\": 400, \"Gamma\": 2.2, \"Orientation\": 0 },\n            { \"$type\": \"DjvuNet.Serialization.Cida\", \"ID\": \"CIDa\", \"NodeOffset\": 34, \"Size\": 36 },\n            { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 78, \"Size\": 84753, \"Description\": \"JB2 bilevel data\" },\n            { \"$type\": \"DjvuNet.Serialization.FG44\", \"ID\": \"FG44\", \"NodeOffset\": 84840, \"Size\": 31845, \"Description\": \"IW4 data #1\", \"Slices\": 100, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 544, \"Height\": 784 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 116694, \"Size\": 7015, \"Description\": \"IW4 data #1\", \"Slices\": 74, \"Version\": 1.2, \"Color\": \"True\", \"Width\": 1630, \"Height\": 2350 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 123718, \"Size\": 6576, \"Description\": \"IW4 data #2\", \"Slices\": 10 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 130302, \"Size\": 1245, \"Description\": \"IW4 data #3\", \"Slices\": 4 },\n            { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 131556, \"Size\": 15184, \"Description\": \"IW4 data #4\", \"Slices\": 9 }\n        ]\n    }\n}\n";
                var data = document.DumpDocumentData(true);
                Assert.NotNull(data);
                Assert.Equal(expected, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData030JsonParse()
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
        public void DumpDocumentData077()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(77)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(21, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expectedStart = "  FORM:DJVM [950606] \n    DIRM [251]        Document directory (bundled, 23 files 21 pages)\n    FORM:DJVI [14787] {dict0020.iff} [I]\n      Djbz [14775]      JB2 shared dictionary\n    FORM:DJVI [145] {anno0001.iff} [S]\n      ANTz [133]        Page annotation (hyperlinks, etc.)\n    FORM:DJVU [20128] {p0001.djvu} [P1]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2, orientation=0\n      INCL [12]         Indirection chunk --> {dict0020.iff}\n      Sjbz [13804]      JB2 bilevel data\n      FGbz [260]        JB2 colors data, v0, 19 colors\n      BG44 [288]        IW4 data #1, 97 slices, v1.2 (b&w), 425x550\n      ANTz [560]        Page annotation (hyperlinks, etc.)\n      TXTz [5113]       Hidden text (text, etc.)\n      INCL [12]         Indirection chunk --> {anno0001.iff}\n    FORM:DJVU [12490] {p0002.djvu} [P2]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2, orientation=0\n      INCL [12]         Indirection chunk --> {dict0020.iff}\n      Sjbz [6760]       JB2 bilevel ";
                string expectedEnd = "ANTz [174]        Page annotation (hyperlinks, etc.)\n      TXTz [3043]       Hidden text (text, etc.)\n      INCL [12]         Indirection chunk --> {anno0001.iff}\n    FORM:DJVU [14776] {p0019.djvu} [P19]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2, orientation=0\n      INCL [12]         Indirection chunk --> {dict0020.iff}\n      Sjbz [8313]       JB2 bilevel data\n      FGbz [145]        JB2 colors data, v0, 8 colors\n      BG44 [288]        IW4 data #1, 97 slices, v1.2 (b&w), 425x550\n      ANTz [787]        Page annotation (hyperlinks, etc.)\n      TXTz [5137]       Hidden text (text, etc.)\n      INCL [12]         Indirection chunk --> {anno0001.iff}\n    FORM:DJVU [17386] {p0020.djvu} [P20]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2, orientation=0\n      INCL [12]         Indirection chunk --> {dict0020.iff}\n      Sjbz [9917]       JB2 bilevel data\n      FGbz [150]        JB2 colors data, v0, 8 colors\n      BG44 [288]        IW4 data #1, 97 slices, v1.2 (b&w), 425x550\n      ANTz [756]        Page annotation (hyperlinks, etc.)\n      TXTz [6172]       Hidden text (text, etc.)\n      INCL [12]         Indirection chunk --> {anno0001.iff}\n    FORM:DJVU [15408] {p0021.djvu} [P21]\n      INFO [10]         DjVu 5100x6600, v21, 600 dpi, gamma=2.2, orientation=0\n      Sjbz [10392]      JB2 bilevel data\n      FGbz [122]        JB2 colors data, v0, 8 colors\n      BG44 [288]        IW4 data #1, 97 slices, v1.2 (b&w), 425x550\n      ANTz [556]        Page annotation (hyperlinks, etc.)\n      TXTz [3967]       Hidden text (text, etc.)\n      INCL [12]         Indirection chunk --> {anno0001.iff}\n";

                var data = document.DumpDocumentData(false);
                Assert.NotNull(data);
                Assert.StartsWith(expectedStart, data);
                Assert.EndsWith(expectedEnd, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData077Json()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(77)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(21, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expectedStart = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvmForm\", \"ID\": \"FORM:DJVM\", \"NodeOffset\": 0, \"Size\": 950606, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Dirm\", \"ID\": \"DIRM\", \"NodeOffset\": 16, \"Size\": 251, \"Description\": \"Document directory\", \"DocumentType\": \"bundled\", \"FileCount\": 23, \"PageCount\": 21 },\n            { \"$type\": \"DjvuNet.Serialization.DjviForm\", \"ID\": \"FORM:DJVI\", \"NodeOffset\": 276, \"Size\": 14787, \"Children\": [\n                    { \"$type\": \"DjvuNet.Serialization.Djbz\", \"ID\": \"Djbz\", \"NodeOffset\": 288, \"Size\": 14775, \"Description\": \"JB2 shared dictionary\" }\n                ]\n            },\n            { \"$type\": \"DjvuNet.Serialization.DjviForm\", \"ID\": \"FORM:DJVI\", \"NodeOffset\": 15072, \"Size\": 145, \"Children\": [\n                    { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 15084, \"Size\": 133, \"Descr";
                string expectedEnd = "\"Size\": 10, \"Width\": 5100, \"Height\": 6600, \"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2, \"Orientation\": 0 },\n                    { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 935232, \"Size\": 10392, \"Description\": \"JB2 bilevel data\" },\n                    { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 945632, \"Size\": 122, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 8 },\n                    { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 945762, \"Size\": 288, \"Description\": \"IW4 data #1\", \"Slices\": 97, \"Version\": 1.2, \"Color\": \"False\", \"Width\": 425, \"Height\": 550 },\n                    { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 946058, \"Size\": 556, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n                    { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 946622, \"Size\": 3967, \"Description\": \"Hidden text (text, etc.)\" },\n                    { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 950598, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"anno0001.iff\" }\n                ]\n            }\n        ]\n    }\n}\n";
                var data = document.DumpDocumentData(true);
                Assert.NotNull(data);
                Assert.StartsWith(expectedStart, data);
                Assert.EndsWith(expectedEnd, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData077JsonDefault()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(77)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(21, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);

                string expectedStart = "{ \"$type\":\"DjvuNet.Serialization.DjvuDoc\", \"DjvuData\":\n    { \"$type\": \"DjvuNet.Serialization.DjvmForm\", \"ID\": \"FORM:DJVM\", \"NodeOffset\": 0, \"Size\": 950606, \"Children\": [\n            { \"$type\": \"DjvuNet.Serialization.Dirm\", \"ID\": \"DIRM\", \"NodeOffset\": 16, \"Size\": 251, \"Description\": \"Document directory\", \"DocumentType\": \"bundled\", \"FileCount\": 23, \"PageCount\": 21 },\n            { \"$type\": \"DjvuNet.Serialization.DjviForm\", \"ID\": \"FORM:DJVI\", \"NodeOffset\": 276, \"Size\": 14787, \"Children\": [\n                    { \"$type\": \"DjvuNet.Serialization.Djbz\", \"ID\": \"Djbz\", \"NodeOffset\": 288, \"Size\": 14775, \"Description\": \"JB2 shared dictionary\" }\n                ]\n            },\n            { \"$type\": \"DjvuNet.Serialization.DjviForm\", \"ID\": \"FORM:DJVI\", \"NodeOffset\": 15072, \"Size\": 145, \"Children\": [\n                    { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 15084, \"Size\": 133, \"Descr";
                string expectedEnd = "\"Size\": 10, \"Width\": 5100, \"Height\": 6600, \"Version\": 21, \"Dpi\": 600, \"Gamma\": 2.2, \"Orientation\": 0 },\n                    { \"$type\": \"DjvuNet.Serialization.Sjbz\", \"ID\": \"Sjbz\", \"NodeOffset\": 935232, \"Size\": 10392, \"Description\": \"JB2 bilevel data\" },\n                    { \"$type\": \"DjvuNet.Serialization.FGbz\", \"ID\": \"FGbz\", \"NodeOffset\": 945632, \"Size\": 122, \"Description\": \"JB2 colors data\", \"Version\": 0, \"Colors\": 8 },\n                    { \"$type\": \"DjvuNet.Serialization.BG44\", \"ID\": \"BG44\", \"NodeOffset\": 945762, \"Size\": 288, \"Description\": \"IW4 data #1\", \"Slices\": 97, \"Version\": 1.2, \"Color\": \"False\", \"Width\": 425, \"Height\": 550 },\n                    { \"$type\": \"DjvuNet.Serialization.Antz\", \"ID\": \"ANTz\", \"NodeOffset\": 946058, \"Size\": 556, \"Description\": \"Page annotation (hyperlinks, etc.)\" },\n                    { \"$type\": \"DjvuNet.Serialization.Txtz\", \"ID\": \"TXTz\", \"NodeOffset\": 946622, \"Size\": 3967, \"Description\": \"Hidden text (text, etc.)\" },\n                    { \"$type\": \"DjvuNet.Serialization.Incl\", \"ID\": \"INCL\", \"NodeOffset\": 950598, \"Size\": 12, \"Description\": \"Indirection chunk\", \"Name\": \"anno0001.iff\" }\n                ]\n            }\n        ]\n    }\n}\n";
                var data = document.DumpDocumentData();
                Assert.NotNull(data);
                Assert.StartsWith(expectedStart, data);
                Assert.EndsWith(expectedEnd, data);
            }
        }

        [Fact(), Trait("Category", "DjvuLibre")]
        public void DumpDocumentData077JsonParse()
        {
            using (DjvuDocumentInfo document =
                DjvuDocumentInfo.CreateDjvuDocumentInfo(Util.GetTestFilePath(77)))
            {
                Assert.NotNull(document);
                int pageCount = document.PageCount;
                Assert.Equal<int>(21, pageCount);
                var type = document.DocumentType;
                Assert.Equal<DocumentType>(DocumentType.Bundled, type);
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
                Assert.Equal("eLife", annotation);
            }
        }

        [Fact(Skip = "Long running data dump test"), Trait("Category", "DjvuLibre"), Trait("Category", "Skip")]
        public void DumpAllDocumentsText()
        {
            for (int i = 1; i <= 77; i++)
            {
                string pageText = "";
                string path = null;
                DjvuLibre.DjvuDocumentInfo info = null;

                try
                {
                    path = Util.GetTestFilePath(i);
                    string outFile = Path.Combine(Util.ArtifactsDataPath, "dumps",
                        Path.GetFileNameWithoutExtension(path) + ".txt");
                    info = DjvuLibre.DjvuDocumentInfo.CreateDjvuDocumentInfo(path);

                    using (FileStream stream = new FileStream(outFile, FileMode.Create, FileAccess.ReadWrite))
                    using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    {
                        for (int pi = 0; pi < info.PageCount; pi++)
                        {
                            writer.WriteLine($"/*** Page ={pi}= ***/");
                            using (DjvuPageInfo page = new DjvuPageInfo(info, pi))
                            {
                                pageText = page.Text;
                            }

                            writer.WriteLine(pageText);
                            writer.WriteLine($"/*** End Page ={pi}= ***/");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
                finally
                {
                    info?.Dispose();
                }
            }
        }
    }
}
