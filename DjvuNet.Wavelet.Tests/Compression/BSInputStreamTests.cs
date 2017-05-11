using Xunit;
using DjvuNet.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DjvuNet.Compression.Tests
{
    public class BSInputStreamTests
    {
        [Fact()]
        public void BSInputStreamTest()
        {
            using(MemoryStream stream = new MemoryStream())
            using (BSInputStream bsi = new BSInputStream(stream))
            {
                Assert.False(bsi.CanWrite);
                Assert.True(bsi.CanRead);
                Assert.IsType<BSInputStream>(bsi);
            } 
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void BSInputStreamTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void FlushTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void InitTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ReadByteTest()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}