using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DjvuNet;
using DjvuNet.DataChunks;
using DjvuNet.Tests;
using Xunit;

namespace DjvuNet.Wavelet.Tests
{
    public class JB2ChunkExtractor
    {
        [Fact(Skip = "")]
        public void ExtractAllJB2Chunks()
        {
            List<object[]> mappings = new List<object[]>();
            string outDir = Path.Combine(Util.ArtifactsPath, "data", "extracted");
            Directory.CreateDirectory(outDir);

            for (int i = 1; i <= 79; i++)
            {
                string filePath = Util.GetTestFilePath(i);
                if (!File.Exists(filePath))
                    continue;

                using (DjvuDocument doc = new DjvuDocument(filePath))
                {
                    int pageIndex = 1;
                    foreach (DjvuPage page in doc.Pages)
                    {
                        SjbzChunk sjbz = page.PageForm.Children.OfType<SjbzChunk>().FirstOrDefault();
                        if (sjbz != null)
                        {
                            DjbzChunk djbzItem = GetAssociatedDjbzChunk(sjbz, doc);

                            string sjbzName = $"test{i:000}C_P{pageIndex:00}.sjbz";
                            File.WriteAllBytes(Path.Combine(outDir, sjbzName), sjbz.ChunkData);

                            string djbzName = null;
                            if (djbzItem != null)
                            {
                                // To make unique names, we can use the offset
                                djbzName = $"test{i:000}C_D{djbzItem.DataOffset}.djbz";
                                File.WriteAllBytes(Path.Combine(outDir, djbzName), djbzItem.ChunkData);
                            }

                            mappings.Add(new object[] { i, pageIndex, djbzName, sjbzName });
                        }
                        pageIndex++;
                    }
                }
            }

            string json = JsonSerializer.Serialize(mappings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(outDir, "jb2_chunk_map.json"), json);
        }

        private DjbzChunk GetAssociatedDjbzChunk(SjbzChunk sjbz, DjvuDocument doc)
        {
            if (!(sjbz.Parent is DjvuChunk djvuChunk)) return null;

            IReadOnlyList<InclChunk> includes = djvuChunk.IncludedItems;
            if (includes == null || includes.Count == 0) return null;

            List<InclChunk> includeIDs = includes.Where(x => x.ChunkType == ChunkType.Incl).ToList();
            DjvmChunk root = doc.RootForm as DjvmChunk;
            DjbzChunk djbzItem = null;

            IReadOnlyList<DirmComponent> components = root?.Dirm?.Components;
            IReadOnlyList<IDjviChunk> includeForms = root?.Includes;

            foreach (InclChunk iChunk in includeIDs)
            {
                if (components == null) break;

                string targetID = iChunk.IncludeID;
                DirmComponent component = null;

                for (int i = 0; i < components.Count; i++)
                {
                    DirmComponent c = components[i];
                    if (c.ID == targetID || c.Name == targetID || c.Title == targetID)
                    {
                        component = c;
                        break;
                    }
                }

                if (component != null && includeForms != null)
                {
                    for (int i = 0; i < includeForms.Count; i++)
                    {
                        IDjviChunk includeForm = includeForms[i];
                        if (includeForm.DataOffset == (component.Offset + 12))
                        {
                            IReadOnlyList<IDjvuNode> children = includeForm.Children;
                            if (children != null)
                            {
                                for (int j = 0; j < children.Count; j++)
                                {
                                    if (children[j] is DjbzChunk djbz)
                                    {
                                        djbzItem = djbz;
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    }
                    if (djbzItem != null) break;
                }
            }

            return djbzItem;
        }
    }
}
