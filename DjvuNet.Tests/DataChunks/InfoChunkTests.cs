using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Tests;

namespace DjvuNet.DataChunks.Tests
{
    public class InfoChunkTests
    {

        [Theory]
        [ClassData(typeof(DjvuJsonDataSource))]
        public void InfoChunk_Theory(DjvuJsonDocument doc, int index)
        {
            // Skip document where json dump is incomplete
            if (index == 63)
                return;

            int pageCount = 0;
            using (DjvuDocument document = DjvuNet.Tests.Util.GetTestDocument(index, out pageCount))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount, document);
                DjvuNet.Tests.Util.VerifyDjvuDocumentCtor(pageCount, document);

                for (int i = 0; i < pageCount; i++)
                {

                    var page = document.Pages[i];
                    var info = page.Info;
                    if (doc.Data.Pages != null && doc.Data.Pages.Length > 0)
                    {
                        var pageJson = doc.Data.Pages[i];
                        var infoJson = pageJson.Children[0];

                        Assert.Equal<int>(infoJson.Dpi.Value, info.DPI);
                        Assert.Equal<double>(infoJson.Gamma.Value, Math.Round(info.Gamma, 3));
                        Assert.Equal<int>(infoJson.Height.Value, info.Height);
                        Assert.Equal<int>(infoJson.Width.Value, info.Width);
                        //Assert.Equal<double>(
                        //    Math.Round(infoJson.Version.Value, 3), 
                        //    Math.Round((double)((long)info.MajorVersion << 8 & (long)info.MinorVersion), 3));
                    }
                }
            }
        }
    }
}