using System;
using Xunit;

namespace DjvuNet.Tests
{
    public class SingleDocFixtureTests
    {
        private class TestDocFixture : SingleDocFixture
        {
            public TestDocFixture() : base(1) { } // test001C.djvu
        }

        [Fact]
        public void SingleDocFixture_Ctor_LoadsDocument()
        {
            using (var fixture = new TestDocFixture())
            {
                Assert.NotNull(fixture.Document);
                Assert.False(fixture.Document.IsDisposed);
                Assert.True(fixture.Document.Pages.Count > 0);
            }
        }

        [Fact]
        public void SingleDocFixture_Dispose_DisposesDocument()
        {
            DjvuDocument doc;
            using (var fixture = new TestDocFixture())
            {
                doc = fixture.Document;
            }
            
            Assert.True(doc.IsDisposed);
        }
    }
}