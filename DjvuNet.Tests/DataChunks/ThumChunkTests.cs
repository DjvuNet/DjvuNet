using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Tests;
using DjvuNet.Tests.Xunit;
using System.IO;

namespace DjvuNet.DataChunks.Tests
{
    public class ThumChunkTests
    {

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
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
                    DjvuJsonDocument.RootChild[] thumbsJson = doc.Data.Thumbnails;

                    var thumbs = document.RootForm.Children.Where((x) => x.ChunkType == ChunkType.Thum).ToArray();
                    Assert.Equal<int>(thumbsJson.Length, thumbs.Length);

                    for (int i = 0; i < thumbs.Length; i++)
                    {
                        IDjvuNode th = thumbs[i];
                        IDjvuElement thum = th as IDjvuElement;
                        DjvuJsonDocument.RootChild r = thumbsJson[i];
                        Assert.Equal<int>(thum.Children.Count, r.Children.Length);

                        //DumpTH44ChunkList(index, thum.Children, i);

                        for(int k = 0; k < thum.Children.Count; k++)
                        {
                            IDjvuNode th44 = thum.Children[k];
                            Assert.Equal<ChunkType>(ChunkType.TH44, th44.ChunkType);

                            DjvuJsonDocument.Chunk c = r.Children[k];
                            Assert.Equal<long>(th44.Length, c.Size);
                        }
                    }
                }
                else
                {
                    var thumbs = document.RootForm.Children.Where((x) => x.ChunkType == ChunkType.Thum).ToArray();
                    Assert.Equal<int>(0, thumbs.Length);
                }
            }
        }

        private void DumpTH44ChunkList(int docIndex, IReadOnlyList<IDjvuNode> th44List, int i)
        {
            string docFile = Util.GetTestFilePath(docIndex);
            string fileName = Path.GetFileNameWithoutExtension(docFile);
            string outFileTemplate = Path.Combine(Util.ArtifactsDataPath, fileName);

            for(int k = 0; k < th44List.Count; k++)
            {
                TH44Chunk th = (TH44Chunk) th44List[k];
                string file = outFileTemplate + "_" + (i+1).ToString("00") + "_" + (k+1).ToString("00") + ".th44";
                using (FileStream outFile = new FileStream(file, FileMode.Create, FileAccess.ReadWrite))
                {
                    outFile.Write(th.ChunkData, 0, th.ChunkData.Length);
                }
            }
        }
    }
}