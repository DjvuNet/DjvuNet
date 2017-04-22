using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.DataChunks.Directory;
using DjvuNet.Tests;
using DjvuNet.Tests.Xunit;

namespace DjvuNet.DataChunks.Tests
{
    public class DirmChunkTests
    {

        [Fact()]
        public void DirmChunk001()
        {
            int pageCount = 0;
            using (DjvuDocument document = DjvuNet.Tests.Util.GetTestDocument(1, out pageCount))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount, document);
                DjvuNet.Tests.Util.VerifyDjvuDocumentCtor(pageCount, document);
                DirmChunk dirm = ((DjvmChunk)document.RootForm).Dirm;

                Assert.NotNull(dirm);
                var components = dirm.Components;

                Assert.NotNull(components);
                Assert.Equal<int>(31, components.Count);
            }
        }

        [Fact]
        public void DirmChunk003()
        {
            int pageCount = 0;
            using (DjvuDocument document = DjvuNet.Tests.Util.GetTestDocument(3, out pageCount))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount, document);
                DjvuNet.Tests.Util.VerifyDjvuDocumentCtor(pageCount, document);
                DirmChunk dirm = ((DjvmChunk)document.RootForm).Dirm;

                Assert.NotNull(dirm);

                var components = dirm.Components;

                Assert.NotNull(components);
                Assert.Equal<int>(112, components.Count);
            }
        }

        [DjvuTheory]
        [ClassData(typeof(DjvuJsonDataSource))]
        public void DirmChunk_Theory(DjvuJsonDocument doc, int index)
        {
            int pageCount = 0;
            using (DjvuDocument document = DjvuNet.Tests.Util.GetTestDocument(index, out pageCount))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount, document);
                DjvuNet.Tests.Util.VerifyDjvuDocumentCtor(pageCount, document);

                // DirmChunk is present only in multi page documents
                // in which root form is of DjvmChunk type
                if (document.RootForm.ChunkType == ChunkType.Djvm)
                {
                    DirmChunk dirm = ((DjvmChunk)document.RootForm).Dirm;

                    Assert.NotNull(dirm);

                    Assert.True(dirm.IsBundled ? doc.Data.Dirm.DocumentType == "bundled" : doc.Data.Dirm.DocumentType == "indirect");

                    var components = dirm.Components;
                    Assert.Equal<int>(components.Count, doc.Data.Dirm.FileCount);
                }
            }
        }
    }
}