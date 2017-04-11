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
    public class ThumChunkTests
    {
        [Theory]
        [ClassData(typeof(DjvuJsonDataSource))]
        public void ThumChunk_Theory(DjvuJsonDocument doc, int index)
        {
            int pageCount = 0;
            using (DjvuDocument document = DjvuNet.Tests.Util.GetTestDocument(index, out pageCount))
            {
                DjvuNet.Tests.Util.VerifyDjvuDocument(pageCount, document);
                DjvuNet.Tests.Util.VerifyDjvuDocumentCtor(pageCount, document);

                if (doc.Data.Thumbnails?.Length > 0)
                {
                    var thumbs = document.RootForm.Children.Where((x) => x.ChunkType == ChunkType.Thum).ToArray();
                    Assert.Equal<int>(doc.Data.Thumbnails.Length, thumbs.Length);
                }
                else
                {
                    var thumbs = document.RootForm.Children.Where((x) => x.ChunkType == ChunkType.Thum).ToArray();
                    Assert.Equal<int>(0, thumbs.Length);
                }
            }
        }
    }
}