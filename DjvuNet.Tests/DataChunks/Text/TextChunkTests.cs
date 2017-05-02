using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using System.IO;
using DjvuNet.Tests;

namespace DjvuNet.DataChunks.Tests
{
    public class TextChunkTests
    {
        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void TextChunkTest()
        {
            try
            {
                string file = Path.Combine(Util.ArtifactsDataPath, "test077C_P01.txtz");
                Mock<TextChunk> textChunkMock = new Mock<TextChunk>();
                textChunkMock.Setup(x => x.GetTextDataReader(5113)).Returns((IDjvuReader)null);
                TextChunk txtChunk = textChunkMock.Object;

                using (FileStream stream = new FileStream(file, FileMode.Open))
                using (DjvuReader reader = new DjvuReader(stream))
                {
                    Assert.Equal<ChunkType>(ChunkType.Text, txtChunk.ChunkType);
                    Assert.True(txtChunk.TextLength > 0);
                    Assert.NotNull(txtChunk.Text);
                    Assert.Equal(txtChunk.TextLength, txtChunk.Text.Length);
                }
            }
            catch(Exception ex)
            {

            }
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadDataTest()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}