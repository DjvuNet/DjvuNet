using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.JB2;
using DjvuNet.Tests;
using Xunit;

namespace DjvuNet.JB2.Tests
{
    public class JB2ImageTests
    {

        public static IEnumerable<object[]> JB2ImageTestData => Util.GetJB2ImageTestData(
            skipDocs: new int[] { },
            skipChunks: new string[] { }
        );

        [Theory]
        [MemberData(nameof(JB2ImageTestData))]
        public void DecodeTest(string djbzFileName, string sjbzFileName)
        {

            string prefixStr = "JB2ImageTests.DecodeTest => ";
            JB2Dictionary jb2Dict = null;

            if (djbzFileName != null)
            {
                jb2Dict = new JB2Dictionary();
                byte[] djbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName));

                using (var ms = new MemoryStream(djbzPayload))
                using (var reader = new DjvuReader(ms))
                {
                    // This should not throw DjvuEndOfStreamException
                    jb2Dict.Decode(reader);
                }

                // Console.Write($"{prefixStr}Decoded {djbzFileName} with djbz chunk: dictionary containing {jb2Dict.ShapeCount} shapes => ");
                Assert.True(jb2Dict.ShapeCount > 0, "Managed dictionary decoded 0 shapes.");
            }


            byte[] sjbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, sjbzFileName));

            JB2Image image = new JB2Image();

            using (var ms = new MemoryStream(sjbzPayload))
            using (var reader = new DjvuReader(ms))
            {
                // This should not throw DjvuNet.DjvuFormatException : Image dictionary not provided.
                image.Decode(reader, jb2Dict);
            }

            string prefix = djbzFileName != null ? String.Empty : prefixStr;

            // Console.WriteLine($"{prefix}Decoded {sjbzFileName} with sjbz chunk: JB2Image containing {image.ShapeCount} shapes.");

            Assert.True(image.ShapeCount > 0, "JB2Image decoded 0 shapes.");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void JB2ImageTest()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetBitmapTest()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetBitmapTest1()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetBitmapTest2()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetBitmapTest3()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetBitmapTest4()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetBitmapTest5()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetBitmapTest6()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetBitmapTest7()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetBlitTest()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void AddBlitTest()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void InitTest()
        {
            Assert.Fail("This test needs an implementation");
        }
    }
}
