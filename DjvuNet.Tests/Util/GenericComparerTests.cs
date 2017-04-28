using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.DataChunks;
using DjvuNet.Utilities;
using Moq;
using Xunit;

namespace DjvuNet.Utilities.Tests
{
    public class GenericComparerTests
    {
        [Fact()]
        public void GenericComparerTest()
        {
            GenericComparer<IDjvuNode> nodeComparer = 
                new GenericComparer<IDjvuNode>(
                    x => (x.ChunkType.GetHashCode() + x.DataOffset.GetHashCode()));

            Mock<IDjvuNode> nodeMock1 = new Mock<IDjvuNode>();
            nodeMock1.Setup(x => x.ChunkType).Returns(ChunkType.Anta);
            nodeMock1.Setup(x => x.DataOffset).Returns(2048);

            Mock<IDjvuNode> nodeMock2 = new Mock<IDjvuNode>();
            nodeMock2.Setup(x => x.ChunkType).Returns(ChunkType.Anta);
            nodeMock2.Setup(x => x.DataOffset).Returns(2048);

            Mock<IDjvuNode> nodeMock3 = new Mock<IDjvuNode>();
            nodeMock3.Setup(x => x.ChunkType).Returns(ChunkType.Antz);
            nodeMock3.Setup(x => x.DataOffset).Returns(2048);

            Assert.NotSame(nodeMock1.Object, nodeMock2.Object);
            Assert.True(nodeComparer.Equals(nodeMock1.Object, nodeMock2.Object));
            Assert.Equal<int>(nodeComparer.GetHashCode(nodeMock1.Object), nodeComparer.GetHashCode(nodeMock2.Object));

            Assert.NotSame(nodeMock1.Object, nodeMock3.Object);
            Assert.False(nodeComparer.Equals(nodeMock1.Object, nodeMock3.Object));
            Assert.NotEqual<int>(nodeComparer.GetHashCode(nodeMock1.Object), nodeComparer.GetHashCode(nodeMock3.Object));
        }

        [Fact()]
        public void GenericComparerTest1()
        {
            GenericComparer<IDjvuNode> nodeComparer =
                new GenericComparer<IDjvuNode>(
                    (x, y) => x.ChunkType == y.ChunkType && x.DataOffset == y.DataOffset);

            Mock<IDjvuNode> nodeMock1 = new Mock<IDjvuNode>();
            nodeMock1.Setup(x => x.ChunkType).Returns(ChunkType.Anta);
            nodeMock1.Setup(x => x.DataOffset).Returns(2048);

            Mock<IDjvuNode> nodeMock2 = new Mock<IDjvuNode>();
            nodeMock2.Setup(x => x.ChunkType).Returns(ChunkType.Anta);
            nodeMock2.Setup(x => x.DataOffset).Returns(2048);

            Mock<IDjvuNode> nodeMock3 = new Mock<IDjvuNode>();
            nodeMock3.Setup(x => x.ChunkType).Returns(ChunkType.Antz);
            nodeMock3.Setup(x => x.DataOffset).Returns(2048);

            Assert.NotSame(nodeMock1.Object, nodeMock2.Object);
            Assert.True(nodeComparer.Equals(nodeMock1.Object, nodeMock2.Object));
            Assert.NotEqual<int>(nodeComparer.GetHashCode(nodeMock1.Object), nodeComparer.GetHashCode(nodeMock2.Object));

            Assert.NotSame(nodeMock1.Object, nodeMock3.Object);
            Assert.False(nodeComparer.Equals(nodeMock1.Object, nodeMock3.Object));
            Assert.NotEqual<int>(nodeComparer.GetHashCode(nodeMock1.Object), nodeComparer.GetHashCode(nodeMock3.Object));
        }

        [Fact()]
        public void GenericComparerTest2()
        {
            GenericComparer<IDjvuNode> nodeComparer =
                new GenericComparer<IDjvuNode>(
                    x => (x.ChunkType.GetHashCode() + x.DataOffset.GetHashCode()),
                    (x, y) => x.ChunkType == y.ChunkType && x.DataOffset == y.DataOffset);

            Mock<IDjvuNode> nodeMock1 = new Mock<IDjvuNode>();
            nodeMock1.Setup(x => x.ChunkType).Returns(ChunkType.Anta);
            nodeMock1.Setup(x => x.DataOffset).Returns(2048);

            Mock<IDjvuNode> nodeMock2 = new Mock<IDjvuNode>();
            nodeMock2.Setup(x => x.ChunkType).Returns(ChunkType.Anta);
            nodeMock2.Setup(x => x.DataOffset).Returns(2048);

            Mock<IDjvuNode> nodeMock3 = new Mock<IDjvuNode>();
            nodeMock3.Setup(x => x.ChunkType).Returns(ChunkType.Antz);
            nodeMock3.Setup(x => x.DataOffset).Returns(2048);

            Assert.NotSame(nodeMock1.Object, nodeMock2.Object);
            Assert.True(nodeComparer.Equals(nodeMock1.Object, nodeMock2.Object));
            Assert.Equal<int>(nodeComparer.GetHashCode(nodeMock1.Object), nodeComparer.GetHashCode(nodeMock2.Object));

            Assert.NotSame(nodeMock1.Object, nodeMock3.Object);
            Assert.False(nodeComparer.Equals(nodeMock1.Object, nodeMock3.Object));
            Assert.NotEqual<int>(nodeComparer.GetHashCode(nodeMock1.Object), nodeComparer.GetHashCode(nodeMock3.Object));
        }

        [Fact()]
        public void EqualsTest()
        {
            GenericComparer<IDjvuNode> nodeComparer =
                new GenericComparer<IDjvuNode>(
                    x => (x.ChunkType.GetHashCode() + x.DataOffset.GetHashCode()),
                    (x, y) => x.ChunkType == y.ChunkType && x.DataOffset == y.DataOffset);

            Mock<IDjvuNode> nodeMock1 = new Mock<IDjvuNode>();
            nodeMock1.Setup(x => x.ChunkType).Returns(ChunkType.Anta);
            nodeMock1.Setup(x => x.DataOffset).Returns(2048);

            Mock<IDjvuNode> nodeMock2 = new Mock<IDjvuNode>();
            nodeMock2.Setup(x => x.ChunkType).Returns(ChunkType.Anta);
            nodeMock2.Setup(x => x.DataOffset).Returns(2048);

            Mock<IDjvuNode> nodeMock3 = new Mock<IDjvuNode>();
            nodeMock3.Setup(x => x.ChunkType).Returns(ChunkType.Antz);
            nodeMock3.Setup(x => x.DataOffset).Returns(2048);

            Mock<IDjvuNode> nodeMock4 = new Mock<IDjvuNode>();
            nodeMock4.Setup(x => x.ChunkType).Returns(ChunkType.Antz);
            nodeMock4.Setup(x => x.DataOffset).Returns(1024);

            Assert.NotSame(nodeMock1.Object, nodeMock2.Object);
            Assert.True(nodeComparer.Equals(nodeMock1.Object, nodeMock2.Object));
            Assert.Equal<int>(nodeComparer.GetHashCode(nodeMock1.Object), nodeComparer.GetHashCode(nodeMock2.Object));

            Assert.NotSame(nodeMock1.Object, nodeMock3.Object);
            Assert.False(nodeComparer.Equals(nodeMock1.Object, nodeMock3.Object));
            Assert.NotEqual<int>(nodeComparer.GetHashCode(nodeMock1.Object), nodeComparer.GetHashCode(nodeMock3.Object));

            Assert.NotSame(nodeMock4.Object, nodeMock3.Object);
            Assert.False(nodeComparer.Equals(nodeMock4.Object, nodeMock3.Object));
            Assert.NotEqual<int>(nodeComparer.GetHashCode(nodeMock4.Object), nodeComparer.GetHashCode(nodeMock3.Object));
        }

        [Fact()]
        public void GetHashCodeTest()
        {
            GenericComparer<IDjvuNode> nodeComparer =
                new GenericComparer<IDjvuNode>(
                    x => (x.ChunkType.GetHashCode() + x.DataOffset.GetHashCode()),
                    (x, y) => x.ChunkType == y.ChunkType && x.DataOffset == y.DataOffset);

            Mock<IDjvuNode> nodeMock1 = new Mock<IDjvuNode>();
            nodeMock1.Setup(x => x.ChunkType).Returns(ChunkType.Anta);
            nodeMock1.Setup(x => x.DataOffset).Returns(2048);

            Mock<IDjvuNode> nodeMock2 = new Mock<IDjvuNode>();
            nodeMock2.Setup(x => x.ChunkType).Returns(ChunkType.Anta);
            nodeMock2.Setup(x => x.DataOffset).Returns(2048);

            Mock<IDjvuNode> nodeMock3 = new Mock<IDjvuNode>();
            nodeMock3.Setup(x => x.ChunkType).Returns(ChunkType.Antz);
            nodeMock3.Setup(x => x.DataOffset).Returns(2048);

            Mock<IDjvuNode> nodeMock4 = new Mock<IDjvuNode>();
            nodeMock4.Setup(x => x.ChunkType).Returns(ChunkType.Antz);
            nodeMock4.Setup(x => x.DataOffset).Returns(1024);

            Assert.NotSame(nodeMock1.Object, nodeMock2.Object);
            Assert.True(nodeComparer.Equals(nodeMock1.Object, nodeMock2.Object));
            Assert.Equal<int>(nodeComparer.GetHashCode(nodeMock1.Object), nodeComparer.GetHashCode(nodeMock2.Object));
            Assert.Equal<int>(nodeMock1.Object.ChunkType.GetHashCode() + nodeMock1.Object.DataOffset.GetHashCode(), nodeComparer.GetHashCode(nodeMock1.Object));
            Assert.Equal<int>(nodeMock2.Object.ChunkType.GetHashCode() + nodeMock2.Object.DataOffset.GetHashCode(), nodeComparer.GetHashCode(nodeMock2.Object));

            Assert.NotSame(nodeMock1.Object, nodeMock3.Object);
            Assert.False(nodeComparer.Equals(nodeMock1.Object, nodeMock3.Object));
            Assert.NotEqual<int>(nodeComparer.GetHashCode(nodeMock1.Object), nodeComparer.GetHashCode(nodeMock3.Object));
            Assert.Equal<int>(nodeMock3.Object.ChunkType.GetHashCode() + nodeMock3.Object.DataOffset.GetHashCode(), nodeComparer.GetHashCode(nodeMock3.Object));

            Assert.NotSame(nodeMock4.Object, nodeMock3.Object);
            Assert.False(nodeComparer.Equals(nodeMock4.Object, nodeMock3.Object));
            Assert.NotEqual<int>(nodeComparer.GetHashCode(nodeMock4.Object), nodeComparer.GetHashCode(nodeMock3.Object));
            Assert.Equal<int>(nodeMock4.Object.ChunkType.GetHashCode() + nodeMock4.Object.DataOffset.GetHashCode(), nodeComparer.GetHashCode(nodeMock4.Object));
        }
    }
}