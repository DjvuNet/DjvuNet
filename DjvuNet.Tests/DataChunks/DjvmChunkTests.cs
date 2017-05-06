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
    public class DjvmChunkTests
    {

        [DjvuTheory]
        [ClassData(typeof(DjvuJsonDataSource))]
        public void DjvmChunk_Theory(DjvuJsonDocument doc, int index)
        {
            // TODO Fix libdjvulibre DjvuDumpHelper implementation - fails 
            // for index 39, 63, 64 in some tests - need DjVuLibre tests
            if (index == 63)
                return;

            int pageCount = 0;
            using (DjvuDocument document = DjvuNet.Tests.Util.GetTestDocument(index, out pageCount))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount, document);
                DjvuNet.Tests.Util.VerifyDjvuDocumentCtor(pageCount, document);

                if (pageCount <= 1)
                    Assert.IsNotType<DjvmChunk>(document.RootForm);
                else
                {
                    Assert.IsType<DjvmChunk>(document.RootForm);

                    DjvmChunk djvm = document.RootForm as DjvmChunk;
                    Assert.NotNull(djvm);

                    Assert.NotNull(djvm.Dirm);
                    Assert.NotNull(djvm.Pages);
                    Assert.True(djvm.Pages.Count > 1);
                    Assert.Equal<int>(pageCount, djvm.Pages.Count);
                    Assert.Equal<int>(doc.Data.Files.Length, djvm.Files.Count);
                    if (doc.Data.Includes != null)
                        Assert.Equal<int>(doc.Data.Includes.Length, djvm.Includes.Count);
                    if (doc.Data.Thumbnails != null)
                        Assert.Equal<int>(doc.Data.Thumbnails.Length, djvm.Thumbnails.Count);
                }
            }
        }
    }
}