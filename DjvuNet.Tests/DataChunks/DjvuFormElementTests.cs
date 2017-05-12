using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Parser;
using DjvuNet.Utilities;
using Moq;
using DjvuNet.Tests;
using System.IO;

namespace DjvuNet.DataChunks.Tests
{
    public class DjvuFormElementTests
    {

        [Fact()]
        public void InitializeTest001()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            DjvuFormElement form = formMock.Object;

            using (MemoryStream stream = new MemoryStream())
            using (DjvuReader reader = new DjvuReader(stream))
            {
                form.DataOffset = 1250;
                form.Length = 0;
                form.Initialize(reader);
                Assert.Equal<long>(form.DataOffset + 4, reader.Position);
                formMock.Verify(x => x.ReadChildren(reader), Times.Never());
            }
        }

        [Fact()]
        public void ToStringTest()
        {
            IDjvuReader reader = null;
            Mock<IDjvuReader> readerMock = new Mock<IDjvuReader>();
            readerMock.Setup<long>(x => x.Position).Returns(1024);
            reader = readerMock.Object;

            IDjvuElement element = (IDjvuElement) DjvuParser.CreateDecodedDjvuNode(reader, null, null, ChunkType.Djvi, "DJVI", 127);
            string result = element.ToString();
            Assert.False(String.IsNullOrWhiteSpace(result));
            Assert.Contains("ID: DJVI", result);
            Assert.Contains("Name: Djvi", result);
            Assert.Contains("Length: 127", result);
            Assert.Contains("Offset: 1024", result);
            Assert.Contains("Children: 0", result);

        }

        [Fact]
        public void FirstSiblingTest001()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.Pages[11];
                var firstPage = document.Pages[0];
                IDjvuElement element = page.PageForm;
                IDjvuNode firstSibling = element.FirstSibling;
                Assert.NotNull(firstSibling);
                Assert.IsType<DirmChunk>(firstSibling);
            }
        }

        [Fact]
        public void FirstSiblingTest002()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<DjvuFormElement> formMock1 = new Mock<DjvuFormElement> { CallBase = true };

            Assert.Throws<InvalidOperationException>(() => formMock.Object.FirstSibling = formMock1.Object);
        }

        [Fact]
        public void FirstSiblingTest003()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<DjvuFormElement> formMock1 = new Mock<DjvuFormElement> { CallBase = true };
            formMock.Object.Parent = formMock1.Object;

            Assert.Throws<InvalidOperationException>(() => formMock.Object.FirstSibling = null);
        }

        [Fact]
        public void FirstSiblingTest004()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<DjvuFormElement> formMock1 = new Mock<DjvuFormElement> { CallBase = true };
            formMock.Object.FirstSibling = null;

            Assert.Null(formMock.Object.FirstSibling);
        }

        [Fact]
        public void FirstSiblingTest005()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<DjvuFormElement> formMock1 = new Mock<DjvuFormElement> { CallBase = true };
            Mock<DjvuFormElement> formMock2 = new Mock<DjvuFormElement> { CallBase = true };

            formMock.Object.Parent = formMock1.Object;
            formMock.Object.FirstSibling = formMock2.Object;

            Assert.NotNull(formMock.Object.FirstSibling);
            Assert.Same(formMock2.Object, formMock.Object.FirstSibling);
        }

        [Fact]
        public void LastSiblingTest001()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.Pages[11];
                var lastPage = document.Pages[document.Pages.Count - 1];
                IDjvuElement element = page.PageForm;
                IDjvuNode lastSibling = element.LastSibling;
                Assert.NotNull(lastSibling);
                Assert.Same(lastSibling, lastPage.PageForm);
            }
        }

        [Fact]
        public void LastSiblingTest002()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<DjvuFormElement> formMock1 = new Mock<DjvuFormElement> { CallBase = true };

            Assert.Throws<InvalidOperationException>(() => formMock.Object.LastSibling = formMock1.Object);
        }

        [Fact]
        public void LastSiblingTest003()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<DjvuFormElement> formMock1 = new Mock<DjvuFormElement> { CallBase = true };
            formMock.Object.Parent = formMock1.Object;

            Assert.Throws<InvalidOperationException>(() => formMock.Object.LastSibling = null);
        }

        [Fact]
        public void LastSiblingTest004()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<DjvuFormElement> formMock1 = new Mock<DjvuFormElement> { CallBase = true };
            formMock.Object.LastSibling = null;

            Assert.Null(formMock.Object.LastSibling);
        }

        [Fact]
        public void LastSiblingTest005()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<DjvuFormElement> formMock1 = new Mock<DjvuFormElement> { CallBase = true };
            Mock<DjvuFormElement> formMock2 = new Mock<DjvuFormElement> { CallBase = true };

            formMock.Object.Parent = formMock1.Object;
            formMock.Object.LastSibling = formMock2.Object;

            Assert.NotNull(formMock.Object.LastSibling);
            Assert.Same(formMock2.Object, formMock.Object.LastSibling);
        }

        [Fact]
        public void FirstChildTest()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.Pages[11];
                IDjvuElement element = page.PageForm;
                IDjvuNode firstChild = element.FirstChild;
                Assert.NotNull(firstChild);
                Assert.IsType<InfoChunk>(firstChild);
            }
        }

        [Fact]
        public void LastChildTest()
        {
            int pageCount = 0;
            using (DjvuDocument document = Util.GetTestDocument(2, out pageCount))
            {
                Util.VerifyDjvuDocument(pageCount, document);

                var page = document.Pages[11];
                IDjvuElement element = page.PageForm;
                IDjvuNode lastChild = element.LastChild;
                Assert.NotNull(lastChild);
                Assert.IsType<TxtzChunk>(lastChild);
            }
        }

        [Fact]
        public void AddChildTest()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<IDjvuNode> nodeMock = new Mock<IDjvuNode>();

            int result = formMock.Object.AddChild(nodeMock.Object);
            Assert.Equal<int>(1, result);
            Assert.Equal<int>(1, formMock.Object.Children.Count);
        }

        [Fact]
        public void ClearChildrenTest()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<IDjvuNode> nodeMock = new Mock<IDjvuNode>();

            int result = formMock.Object.AddChild(nodeMock.Object);
            Assert.Equal<int>(1, result);
            Assert.Equal<int>(1, formMock.Object.Children.Count);

            formMock.Object.ClearChildren();
            Assert.Equal<int>(0, formMock.Object.Children.Count);
        }

        [Fact]
        public void ContainsChildTest001()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<IDjvuNode> nodeMock1 = new Mock<IDjvuNode>();
            Mock<IDjvuNode> nodeMock2 = new Mock<IDjvuNode>();
            Mock<IDjvuNode> nodeMock3 = new Mock<IDjvuNode>();

            int result = formMock.Object.AddChild(nodeMock1.Object);
            Assert.Equal<int>(1, result);
            Assert.Equal<int>(1, formMock.Object.Children.Count);

            result = formMock.Object.AddChild(nodeMock2.Object);
            Assert.Equal<int>(2, result);
            Assert.Equal<int>(2, formMock.Object.Children.Count);
            Assert.Same(nodeMock1.Object, formMock.Object.FirstChild);
            Assert.Same(nodeMock2.Object, formMock.Object.LastChild);

            Assert.True(formMock.Object.ContainsChild(nodeMock1.Object));
            Assert.True(formMock.Object.ContainsChild(nodeMock2.Object));
            Assert.False(formMock.Object.ContainsChild(nodeMock3.Object));
        }

        [Fact]
        public void ContainsChildTest002()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };

            Mock<IDjvuNode> nodeMock1 = new Mock<IDjvuNode>();
            nodeMock1.Setup(x => x.Name).Returns("Node1");

            Mock<IDjvuNode> nodeMock2 = new Mock<IDjvuNode>();
            nodeMock2.Setup(x => x.Name).Returns("Node2");

            Mock<IDjvuNode> nodeMock3 = new Mock<IDjvuNode>();
            nodeMock3.Setup(x => x.Name).Returns("Node3");

            Mock<IDjvuNode> nodeMock4 = new Mock<IDjvuNode>();
            nodeMock4.Setup(x => x.Name).Returns("Node3");

            int result = formMock.Object.AddChild(nodeMock1.Object);
            Assert.Equal<int>(1, result);
            Assert.Equal<int>(1, formMock.Object.Children.Count);

            result = formMock.Object.AddChild(nodeMock2.Object);
            Assert.Equal<int>(2, result);
            Assert.Equal<int>(2, formMock.Object.Children.Count);
            Assert.Same(nodeMock1.Object, formMock.Object.FirstChild);
            Assert.Same(nodeMock2.Object, formMock.Object.LastChild);

            var comparer = new GenericComparer<IDjvuNode>((x, y) => x.Name == y.Name);
            Assert.True(formMock.Object.ContainsChild(nodeMock1.Object, comparer));
            Assert.True(formMock.Object.ContainsChild(nodeMock2.Object, comparer));
            Assert.False(formMock.Object.ContainsChild(nodeMock3.Object, comparer));

            result = formMock.Object.AddChild(nodeMock4.Object);
            Assert.Equal<int>(3, result);
            Assert.Equal<int>(3, formMock.Object.Children.Count);
            Assert.Same(nodeMock1.Object, formMock.Object.FirstChild);
            Assert.Same(nodeMock4.Object, formMock.Object.LastChild);

            Assert.True(formMock.Object.ContainsChild(nodeMock3.Object, comparer));
            Assert.False(formMock.Object.ContainsChild(nodeMock3.Object));
        }

        [Fact]
        public void RemoveChildTest()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<IDjvuNode> nodeMock = new Mock<IDjvuNode>();

            int result = formMock.Object.AddChild(nodeMock.Object);
            Assert.Equal<int>(1, result);
            Assert.Equal<int>(1, formMock.Object.Children.Count);
            Assert.Same(nodeMock.Object, formMock.Object.FirstChild);
            Assert.Same(nodeMock.Object, formMock.Object.LastChild);

            Assert.True(formMock.Object.RemoveChild(nodeMock.Object));
            Assert.Equal<int>(0, formMock.Object.Children.Count);
            Assert.Null(formMock.Object.FirstChild);
            Assert.Null(formMock.Object.LastChild);
        }

        [Fact]
        public void RemoveChildAtTest()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<IDjvuNode> nodeMock1 = new Mock<IDjvuNode>();
            Mock<IDjvuNode> nodeMock2 = new Mock<IDjvuNode>();

            int result = formMock.Object.AddChild(nodeMock1.Object);
            Assert.Equal<int>(1, result);
            Assert.Equal<int>(1, formMock.Object.Children.Count);

            result = formMock.Object.AddChild(nodeMock2.Object);
            Assert.Equal<int>(2, result);
            Assert.Equal<int>(2, formMock.Object.Children.Count);
            Assert.Same(nodeMock1.Object, formMock.Object.FirstChild);
            Assert.Same(nodeMock2.Object, formMock.Object.LastChild);

            formMock.Object.RemoveChildAt(0);
            Assert.Equal<int>(1, formMock.Object.Children.Count);
            Assert.Same(nodeMock2.Object, formMock.Object.FirstChild);
            Assert.Same(nodeMock2.Object, formMock.Object.LastChild);
        }

        [Fact]
        public void InsertChildTest()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Mock<IDjvuNode> nodeMock1 = new Mock<IDjvuNode>();
            Mock<IDjvuNode> nodeMock2 = new Mock<IDjvuNode>();

            int result = formMock.Object.AddChild(nodeMock1.Object);
            Assert.Equal<int>(1, result);
            Assert.Equal<int>(1, formMock.Object.Children.Count);

            result = formMock.Object.InsertChild(0, nodeMock2.Object);
            Assert.Equal<int>(2, result);
            Assert.Equal<int>(2, formMock.Object.Children.Count);
            Assert.Same(nodeMock2.Object, formMock.Object.FirstChild);
            Assert.Same(nodeMock1.Object, formMock.Object.LastChild);

            result = formMock.Object.RemoveChildAt(0);
            Assert.Equal<int>(1, result);
            Assert.Equal<int>(1, formMock.Object.Children.Count);
            Assert.Same(nodeMock1.Object, formMock.Object.FirstChild);
            Assert.Same(nodeMock1.Object, formMock.Object.LastChild);
        }

        [Fact]
        public void WriteDataTest001()
        {
            byte[] buffer = new byte[2048];

            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            formMock.Object.Length = buffer.Length;

            Mock<DjvuNode> nodeMock1 = new Mock<DjvuNode> { CallBase = true };
            nodeMock1.Object.ChunkData = buffer;
            nodeMock1.Setup(x => x.Name).Returns("NAVM");
            nodeMock1.Object.Length = buffer.Length;

            Mock<DjvuNode> nodeMock2 = new Mock<DjvuNode> { CallBase = true };
            nodeMock2.Object.ChunkData = buffer;
            nodeMock2.Setup(x => x.Name).Returns("DIRM");
            nodeMock2.Object.Length = buffer.Length;

            Mock<IDjvuWriter> writerMock = new Mock<IDjvuWriter>();
            writerMock.Setup<long>(x => x.WriteUTF8String(It.IsAny<string>())).Returns(4);
            writerMock.Setup(x => x.WriteUInt32BigEndian(It.IsAny<uint>()));
            writerMock.Setup(x => x.Write(buffer, 0, buffer.Length));


            int result = formMock.Object.AddChild(nodeMock1.Object);
            Assert.Equal<int>(1, result);
            Assert.Equal<int>(1, formMock.Object.Children.Count);

            result = formMock.Object.InsertChild(0, nodeMock2.Object);
            Assert.Equal<int>(2, result);
            Assert.Equal<int>(2, formMock.Object.Children.Count);

            formMock.Object.WriteData(writerMock.Object, true);

            writerMock.Verify(x => x.WriteUTF8String("FORM"), Times.Once());
            writerMock.Verify(x => x.WriteUTF8String("Unknown"), Times.Once());
            writerMock.Verify(x => x.WriteUTF8String("NAVM"), Times.Once());
            writerMock.Verify(x => x.WriteUTF8String("DIRM"), Times.Once());
            writerMock.Verify(x => x.WriteUInt32BigEndian((uint)buffer.Length), Times.Exactly(2));
            writerMock.Verify(x => x.WriteUInt32BigEndian(((uint)buffer.Length + 8) * 2), Times.Exactly(1));
            writerMock.Verify(x => x.Write(buffer, 0, buffer.Length), Times.Exactly(2));
        }

        [Fact]
        public void WriteDataTest002()
        {
            byte[] buffer = new byte[2048];

            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            formMock.Object.Length = buffer.Length;

            Mock<DjvuNode> nodeMock1 = new Mock<DjvuNode> { CallBase = true };
            nodeMock1.Object.ChunkData = buffer;
            nodeMock1.Setup(x => x.Name).Returns("NAVM");
            nodeMock1.Object.Length = buffer.Length;

            Mock<DjvuNode> nodeMock2 = new Mock<DjvuNode> { CallBase = true };
            nodeMock2.Object.ChunkData = buffer;
            nodeMock2.Setup(x => x.Name).Returns("DIRM");
            nodeMock2.Object.Length = buffer.Length;

            Mock<IDjvuWriter> writerMock = new Mock<IDjvuWriter>();
            writerMock.Setup<long>(x => x.WriteUTF8String(It.IsAny<string>())).Returns(4);
            writerMock.Setup(x => x.WriteUInt32BigEndian(It.IsAny<uint>()));
            writerMock.Setup(x => x.Write(buffer, 0, buffer.Length));


            int result = formMock.Object.AddChild(nodeMock1.Object);
            Assert.Equal<int>(1, result);
            Assert.Equal<int>(1, formMock.Object.Children.Count);

            result = formMock.Object.InsertChild(0, nodeMock2.Object);
            Assert.Equal<int>(2, result);
            Assert.Equal<int>(2, formMock.Object.Children.Count);

            formMock.Object.WriteData(writerMock.Object);

            writerMock.Verify(x => x.WriteUTF8String("FORM"), Times.Once());
            writerMock.Verify(x => x.WriteUTF8String("Unknown"), Times.Once());
            writerMock.Verify(x => x.WriteUTF8String("NAVM"), Times.Once());
            writerMock.Verify(x => x.WriteUTF8String("DIRM"), Times.Once());
            writerMock.Verify(x => x.WriteUInt32BigEndian((uint)buffer.Length), Times.Exactly(2));
            writerMock.Verify(x => x.WriteUInt32BigEndian(((uint)buffer.Length + 8) * 2), Times.Exactly(1));
            writerMock.Verify(x => x.Write(buffer, 0, buffer.Length), Times.Exactly(2));
        }

        [Fact]
        public void WriteDataTest003()
        {
            byte[] buffer = new byte[2048];

            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            formMock.Object.Length = buffer.Length;

            Mock<DjvuNode> nodeMock1 = new Mock<DjvuNode> { CallBase = true };
            nodeMock1.Object.ChunkData = buffer;
            nodeMock1.Setup(x => x.Name).Returns("NAVM");
            nodeMock1.Object.Length = buffer.Length;

            Mock<DjvuNode> nodeMock2 = new Mock<DjvuNode> { CallBase = true };
            nodeMock2.Object.ChunkData = buffer;
            nodeMock2.Setup(x => x.Name).Returns("NAVM");
            nodeMock2.Object.Length = buffer.Length;


            Mock<IDjvuWriter> writerMock = new Mock<IDjvuWriter>();
            writerMock.Setup<long>(x => x.WriteUTF8String(It.IsAny<string>())).Returns(4);
            writerMock.Setup(x => x.WriteUInt32BigEndian(It.IsAny<uint>()));
            writerMock.Setup(x => x.Write(buffer, 0, buffer.Length));

            int result = formMock.Object.AddChild(nodeMock1.Object);
            Assert.Equal<int>(1, result);
            Assert.Equal<int>(1, formMock.Object.Children.Count);

            result = formMock.Object.InsertChild(0, nodeMock2.Object);
            Assert.Equal<int>(2, result);
            Assert.Equal<int>(2, formMock.Object.Children.Count);

            formMock.Object.WriteData(writerMock.Object, false);

            writerMock.Verify(x => x.WriteUTF8String("FORM"), Times.Never());
            writerMock.Verify(x => x.WriteUTF8String("Unknown"), Times.Never());
            writerMock.Verify(x => x.WriteUTF8String("NAVM"), Times.Never());
            writerMock.Verify(x => x.WriteUTF8String("DIRM"), Times.Never());
            writerMock.Verify(x => x.WriteUInt32BigEndian((uint)buffer.Length), Times.Never());
            writerMock.Verify(x => x.WriteUInt32BigEndian(((uint)buffer.Length + 8) * 2), Times.Never());
            writerMock.Verify(x => x.Write(buffer, 0, buffer.Length), Times.Exactly(2));
        }

        [Fact]
        public void WriteDataTest004()
        {
            Mock<DjvuFormElement> formMock = new Mock<DjvuFormElement> { CallBase = true };
            Assert.Throws<ArgumentNullException>("writer", () => formMock.Object.WriteData(null, true));
        }

    }
}