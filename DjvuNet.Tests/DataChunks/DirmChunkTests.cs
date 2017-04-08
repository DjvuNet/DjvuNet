﻿using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.DataChunks.Directory;
using DjvuNet.Tests;

namespace DjvuNet.DataChunks.Tests
{
    public class DirmChunkTests
    {
        [Fact()]
        public void DirmChunk_ctor001()
        {
            int pageCount = 0;
            using (DjvuDocument document = DjvuNet.Tests.Util.GetTestDocument(1, out pageCount))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount, document);
                DjvuNet.Tests.Util.VerifyDjvuDocumentCtor(pageCount, document);
                DirmChunk dirm = ((DjvmChunk)document.RootForm).DirmData;

                Assert.NotNull(dirm);
                DirmComponent[] components = dirm.Components;

                Assert.NotNull(components);
                Assert.Equal<int>(31, components.Length);
            }
        }

        [Fact]
        public void DirmChunk_ctor003()
        {
            int pageCount = 0;
            using (DjvuDocument document = DjvuNet.Tests.Util.GetTestDocument(3, out pageCount))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount, document);
                DjvuNet.Tests.Util.VerifyDjvuDocumentCtor(pageCount, document);
                DirmChunk dirm = ((DjvmChunk)document.RootForm).DirmData;

                Assert.NotNull(dirm);

                DirmComponent[] components = dirm.Components;

                Assert.NotNull(components);
                Assert.Equal<int>(112, components.Length);
            }
        }

    }
}