using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.DataChunks;
using Moq;
using Xunit;

namespace DjvuNet.DataChunks.Tests
{
    public class AnnotationChunkTests
    {
        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void AnnotationChunkTest()
        {
            Mock<AnnotationChunk> annoMock = new Mock<AnnotationChunk> { CallBase = true };

            AnnotationChunk unk = annoMock.Object;
            IAnnotationChunk anno = unk as IAnnotationChunk;
            Assert.NotNull(anno);
        }
    }
}