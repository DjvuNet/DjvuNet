using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Serialization;
using DjvuNet.Tests.Xunit;
using Newtonsoft.Json;
using Xunit;

namespace DjvuNet.Tests
{
    public class SerializationTests
    {
        public static IEnumerable<object[]> DeserializeTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                string[] files = Directory.GetFiles(
                    Util.ArtifactsJsonPath, "*.json");

                foreach(string f in files)
                {
                    retVal.Add(new object[]
                    {
                        Path.GetFileName(f),
                        f
                    });
                }

                return retVal;
            }
        }

        [DjvuTheory]
        [MemberData(nameof(DeserializeTestData))]
        public void Deserialize_Theory(string fileName, string filePath)
        {
            JsonConverter[] converters = new JsonConverter[]
            {
                new DjvuDocConverter(),
                new NodeBaseConverter(),
            };

            string json = File.ReadAllText(filePath, new UTF8Encoding(false));
            DjvuDoc doc = JsonConvert.DeserializeObject<DjvuNet.Serialization.DjvuDoc>(json, converters);

            Assert.NotNull(doc);
            Assert.NotNull(doc.File);
            Assert.NotNull(doc.DjvuData);
        }

        [Theory]
        [InlineData(new object[] { 1, 2, 3})]
        [InlineData(new object[] { 3, 2, 5})]
        public void Test_Theory(int var1, int var2, int result)
        {
            Assert.Equal(result, var1 + var2);
        }
    }
}
