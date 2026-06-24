using Xunit;
using DjvuNet.JB2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Tests;

namespace DjvuNet.JB2.Tests
{
    public class JB2DictionaryTests
    {
        public static IEnumerable<object[]> JB2DictionaryTestData => Util.GetJB2DictionaryTestData(
            skipDocs: new int[] { },
            skipChunks: new string[] { }
        );

        [Theory]
        [MemberData(nameof(JB2DictionaryTestData))]
        [InlineData("testE002_001.djbz")]
        [InlineData("testE003_001.djbz")]
        [InlineData("testE004_001.djbz")]
        [InlineData("testE005_001.djbz")]
        public void DecodeDjbzTest(string djbzFileName)
        {
            byte[] djbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName));

            JB2Dictionary managedDict = new JB2Dictionary();

            using (var ms = new MemoryStream(djbzPayload))
            using (var reader = new DjvuReader(ms))
            {
                managedDict.Decode(reader);
            }

            // Console.WriteLine($"Decoded {djbzFileName} with djbz chunk: dictionary containing {managedDict.ShapeCount} shapes.");

            Assert.True(managedDict.ShapeCount > 0, "Managed dictionary decoded 0 shapes.");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void AddShapeTest()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetShapeTest()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void InitTest()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void SetInheritedDictTest()
        {
            Assert.Fail("This test needs an implementation");
        }
    }
}
