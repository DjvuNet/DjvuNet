using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Tests;
using Moq;
using DjvuNet.Tests.Xunit;

namespace DjvuNet.DataChunks.Tests
{
    public class InclChunkTests
    {
        public static IEnumerable<object[]> InclTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();
                string directory = Path.Combine(Util.RepoRoot, "artifacts", "data");
                string[] testFiles = System.IO.Directory.GetFiles(directory, "*.incl");
                for (int i = 0; i < testFiles.Length; i++)
                {
                    string testNoStr = Path.GetFileNameWithoutExtension(testFiles[i])
                        .Substring(4, 3).TrimStart(new char[] { '0' });
                    int testNumber = int.Parse(testNoStr);
                    var jsonDoc = UtilJson.GetJsonDocument(testNumber - 1);

                    retVal.Add(new object[]
                    {
                        testFiles[i],
                        jsonDoc,
                    });

                }
                return retVal;
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(InclTestData))]
        public void InclChunk_Theory(string testFile, DjvuJsonDocument doc)
        {
            byte[] buffer = null;
            using (FileStream fs = File.OpenRead(testFile))
            {
                buffer = new byte[fs.Length];
                int result = fs.Read(buffer, 0, buffer.Length);
                Assert.Equal<long>(fs.Length, result);
            }
            Mock<IDjvuDocument> docMock = new Mock<IDjvuDocument>();
            Mock<IDjvuElement> parentMock = new Mock<IDjvuElement>();
            IDjvuElement parent = parentMock.Object;
            IDjvuDocument document = docMock.Object;

            using (MemoryStream ms = new MemoryStream(buffer, false))
            using (IDjvuReader reader = new DjvuReader(ms))
            {
                InclChunk chunk = new InclChunk(reader, parent, document, "INCL", buffer.Length);
                Assert.False(chunk.IsInitialized);
                // Support multiple INCL chunks in single DjvuChunk
                var testChunk = doc.Data.Pages[0].Children
                    .Where(x => x.ID == "INCL" && x.Name == chunk.IncludeID)
                    .FirstOrDefault<DjvuJsonDocument.Chunk>();
                Assert.NotNull(testChunk);
                Assert.Equal<string>(testChunk.Name, chunk.IncludeID);
            }
        }
    }
}