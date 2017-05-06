using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Tests;
using DjvuNet.Tests.Xunit;

namespace DjvuNet.DataChunks.Tests
{
    public class InfoChunkTests
    {

        [DjvuTheory]
        [ClassData(typeof(DjvuJsonDataSource))]
        public void InfoChunk_Theory(DjvuJsonDocument doc, int index)
        {
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
                        try
                        {
                            Assert.Equal<int>(infoJson.Dpi.Value, info.DPI);
                            Assert.Equal<double>(infoJson.Gamma.Value, Math.Round(info.Gamma, 3));
                            Assert.Equal<int>(infoJson.Height.Value, info.Height);
                            Assert.Equal<int>(infoJson.Width.Value, info.Width);

                            var major = info.MajorVersion;
                            var minor = info.MinorVersion;
                            var rotation = info.PageRotation;
                            var minorJson = (int) (infoJson.Version.HasValue ? infoJson.Version.Value : 0);
                            Assert.Equal(0, major);
                            Assert.Equal(minorJson, minor);
                        }
                        catch (Exception e)
                        {
                            throw new AggregateException(
                                $"Validation InfoChunk value mismatch Document index: {index}, page: {i + 1}", e);
                        }
                    }
                }
            }
        }
    }
}