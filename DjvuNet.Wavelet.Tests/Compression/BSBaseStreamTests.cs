using Xunit;
using DjvuNet.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using System.IO;
using DjvuNet.Errors;

namespace DjvuNet.Compression.Tests
{
    public class BSBaseStreamTests
    {
        [Fact()]
        public void BSBaseStreamTest001()
        {
            int bufferLength = 1791;
            byte[] buffer = new byte[bufferLength];
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                Mock<BSBaseStream> bsStreamMock = new Mock<BSBaseStream> { CallBase = true };
                bsStreamMock.Setup(x => x.Init(stream));
                var bsStream = bsStreamMock.Object;
                bsStream.BaseStream = stream;

                Assert.Same(stream, bsStream.BaseStream);
                Assert.Equal(stream.Length, bsStream.BaseStream.Length);
                Assert.True(bsStream.CanRead);
                Assert.True(bsStream.CanWrite);
                Assert.True(bsStream.CanSeek);
                Assert.Equal(stream.Length, bsStream.Length);
                Assert.Equal(stream.Position, bsStream.Position);
            }
        }

        [Fact()]
        public void BSBaseStreamTest002()
        {
            int bufferLength = 1791;
            byte[] buffer = new byte[bufferLength];
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                Mock<BSBaseStream> bsStreamMock = new Mock<BSBaseStream> { CallBase = true };
                bsStreamMock.Setup(x => x.Init(stream));
                var bsStream = bsStreamMock.Object;
                bsStream.BaseStream = null;

                Assert.Null(bsStream.BaseStream);
                Assert.False(bsStream.CanRead);
                Assert.False(bsStream.CanWrite);
                Assert.False(bsStream.CanSeek);
                Assert.Equal(0, bsStream.Length);
                Assert.Equal(0, bsStream.Position);
            }
        }

        [Fact()]
        public void BSBaseStreamTest003()
        {
            int bufferLength = 1791;
            byte[] buffer = new byte[bufferLength];
            using (MemoryStream stream = new MemoryStream(buffer, false))
            {
                Mock<BSBaseStream> bsStreamMock = new Mock<BSBaseStream> { CallBase = true };
                bsStreamMock.Setup(x => x.Init(stream));
                var bsStream = bsStreamMock.Object;
                bsStream.BaseStream = stream;

                Assert.Same(stream, bsStream.BaseStream);
                Assert.Equal(stream.Length, bsStream.BaseStream.Length);
                Assert.True(bsStream.CanRead);
                Assert.False(bsStream.CanWrite);
                Assert.True(bsStream.CanSeek);
                Assert.Equal(stream.Length, bsStream.Length);
                Assert.Equal(stream.Position, bsStream.Position);
            }
        }

        [Fact()]
        public void SeekTest001()
        {
            int bufferLength = 1791;
            byte[] buffer = new byte[bufferLength];
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                Mock<BSBaseStream> bsStreamMock = new Mock<BSBaseStream> { CallBase = true };
                bsStreamMock.Setup(x => x.Init(stream));
                var bsStream = bsStreamMock.Object;
                bsStream.BaseStream = stream;

                bsStream.Seek(bufferLength, SeekOrigin.Begin);
                Assert.Equal(bufferLength, bsStream.Position);
                Assert.Equal(stream.Position, bsStream.Position);
            }
        }

        [Fact()]
        public void SeekTest002()
        {
            int bufferLength = 1791;
            byte[] buffer = new byte[bufferLength];
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                Mock<BSBaseStream> bsStreamMock = new Mock<BSBaseStream> { CallBase = true };
                bsStreamMock.Setup(x => x.Init(stream));
                var bsStream = bsStreamMock.Object;
                bsStream.BaseStream = null;

                Assert.Throws<DjvuInvalidOperationException>(() => bsStream.Seek(bufferLength, SeekOrigin.Begin));

                bsStream.BaseStream = stream;
                bsStream.Seek(bufferLength, SeekOrigin.Begin);
                Assert.Equal(bufferLength, bsStream.Position);
                Assert.Equal(stream.Position, bsStream.Position);
            }
        }

        [Fact()]
        public void SetLengthTest001()
        {
            int bufferLength = 1791;
            byte[] buffer = new byte[bufferLength];
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                Mock<BSBaseStream> bsStreamMock = new Mock<BSBaseStream> { CallBase = true };
                bsStreamMock.Setup(x => x.Init(stream));
                var bsStream = bsStreamMock.Object;
                bsStream.BaseStream = null;

                Assert.Throws<DjvuInvalidOperationException>(() => bsStream.SetLength(bufferLength * 2));
            }
        }

        [Fact()]
        public void SetLengthTest002()
        {
            int bufferLength = 1791;
            byte[] buffer = new byte[bufferLength];
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                Mock<BSBaseStream> bsStreamMock = new Mock<BSBaseStream> { CallBase = true };
                bsStreamMock.Setup(x => x.Init(stream));
                var bsStream = bsStreamMock.Object;
                bsStream.BaseStream = stream;

                Assert.Throws<NotSupportedException>(() => bsStream.SetLength(bufferLength * 2));
            }
        }

        [Fact()]
        public void SetLengthTest003()
        {
            int bufferLength = 1791;
            byte[] buffer = new byte[bufferLength];
            using (MemoryStream stream = new MemoryStream())
            {
                Mock<BSBaseStream> bsStreamMock = new Mock<BSBaseStream> { CallBase = true };
                bsStreamMock.Setup(x => x.Init(stream));
                var bsStream = bsStreamMock.Object;
                bsStream.BaseStream = stream;

                bsStream.SetLength(bufferLength * 2);

                Assert.Same(stream, bsStream.BaseStream);
                Assert.Equal(bufferLength * 2, bsStream.Length);
                Assert.Equal(stream.Length, bsStream.Length);
            }
        }
    }
}