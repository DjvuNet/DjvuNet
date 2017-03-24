using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.DataChunks.Directory;

namespace DjvuNet.DataChunks.Tests
{
    public class DirmChunkTests
    {
        [Fact(Skip = "Not implemented")]
        public void DirmChunk_ctor001()
        {
            int pageCount = 62;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test001.djvu"))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocumentCtor(pageCount, document);
                DirmChunk dirm = ((DjvmChunk)document.RootForm).DirmData;

                Assert.NotNull(dirm);
            }
        }

        [Fact]
        public void DirmChunk_ctor003()
        {
            int pageCount = 300;
            using (DjvuDocument document = new DjvuDocument("..\\..\\..\\artifacts\\test003.djvu"))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocumentCtor(pageCount, document);
                DirmChunk dirm = ((DjvmChunk)document.RootForm).DirmData;

                Assert.NotNull(dirm);

                DirmComponent[] components = dirm.Components;

                Assert.NotNull(components);
                Assert.Equal<int>(330, components.Length);
            }
        }

    }
}