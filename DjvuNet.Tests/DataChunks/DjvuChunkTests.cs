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
    public class DjvuChunkTests
    {

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [ClassData(typeof(DjvuJsonDataSource))]
        public void DjvuChunk_Theory(DjvuJsonDocument doc, int index)
        {
            int pageCount = 0;
            using (DjvuDocument document = DjvuNet.Tests.Util.GetTestDocument(index, out pageCount))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount, document);
                DjvuNet.Tests.Util.VerifyDjvuDocumentCtor(pageCount, document);

                if (pageCount <= 1)
                {
                    Assert.IsNotType<DjvmChunk>(document.RootForm);
                    Assert.IsType<DjvuChunk>(document.RootForm);
                    DjvuChunk djvu = document.RootForm as DjvuChunk;
                    Assert.NotNull(djvu);
                    Assert.NotNull(djvu.Info);
                    // TODO - Json deserialization is not supporting single page docs
                    // data are sliced to fit into other format - need to fix
                }
                else
                {
                    Assert.IsType<DjvmChunk>(document.RootForm);
                    DjvmChunk djvm = document.RootForm as DjvmChunk;
                    Assert.NotNull(djvm);
                    Assert.NotNull(djvm.Pages);
                    Assert.True(djvm.Pages.Count > 1);
                }
            }
        }
    }
}