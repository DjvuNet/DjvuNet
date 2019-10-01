using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Tests;
using DjvuNet.Tests.Xunit;
using System.Runtime.CompilerServices;

namespace DjvuNet.DataChunks.Tests
{
    public class DjvuChunkTests
    {

        [DjvuTheory]
        [ClassData(typeof(DjvuJsonDataSource))]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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

        [Fact]
        public void IncludesTest()
        {
            int pageCount = 0;
            using (DjvuDocument document = DjvuNet.Tests.Util.GetTestDocument(2, out pageCount))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount, document);
                IDjvuPage page = document.FirstPage;
                DjvuChunk form = page.PageForm as DjvuChunk;
                Assert.NotNull(form);

                var includes = form.Includes;
                Assert.NotNull(includes);
                Assert.Equal(1, includes.Count);
            }
        }

        [Fact]
        public void DataTest()
        {
            int pageCount = 0;
            using (DjvuDocument document = DjvuNet.Tests.Util.GetTestDocument(2, out pageCount))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount, document);
                IDjvuPage page = document.FirstPage;
                DjvuChunk form = page.PageForm as DjvuChunk;
                Assert.NotNull(form);

                var reader = form.Data;
                Assert.NotNull(reader);
                Assert.Equal(form.Length, reader.Length);
            }
        }

        [Fact]
        public void ExtractRawDataTest()
        {
            int pageCount = 0;
            using (DjvuDocument document = DjvuNet.Tests.Util.GetTestDocument(2, out pageCount))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount, document);
                IDjvuPage page = document.FirstPage;
                DjvuChunk form = page.PageForm as DjvuChunk;
                Assert.NotNull(form);

                var reader = form.ExtractRawData();
                Assert.NotNull(reader);
                Assert.Equal(form.Length, reader.Length);
            }
        }
    }
}
