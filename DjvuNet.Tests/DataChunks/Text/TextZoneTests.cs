using Xunit;
using DjvuNet.DataChunks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Tests;
using System.IO;

namespace DjvuNet.DataChunks.Tests
{
    public class TextZoneTests
    {
        public static IEnumerable<object[]> TextZoneData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                string[] files = System.IO.Directory.GetFiles(Util.ArtifactsDataPath, "*.txtz");
                foreach (string f in files)
                    retVal.Add(new object[] { f });

                return retVal;
            }
        }

        [Fact()]
        public void TextZoneTest001()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test077C_P01.txtz");
            using (FileStream stream = new FileStream(file, FileMode.Open))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                TxtzChunk unk = new TxtzChunk(reader, null, null, null, stream.Length);

                Assert.True(unk.TextLength > 0);
                Assert.NotNull(unk.Text);
                Assert.Equal(unk.TextLength, unk.Text.Length);
                Assert.Equal(1, unk.Version);

                VisitTextZones(unk.Zone);
            }
        }

        public static void VisitTextZones(TextZone zone)
        {
            var preText = zone.GetPreTextData();
            var postText = zone.GetPostTextData();
            var text = zone.GetTextData();

            Assert.NotNull(text);
            Assert.NotNull(preText);
            Assert.NotNull(postText);

            foreach (TextZone z in zone.Children)
                VisitTextZones(z);
        }

        [Fact()]
        public void SearchForTextRectangleTest()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test077C_P01.txtz");
            using (FileStream stream = new FileStream(file, FileMode.Open))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                TxtzChunk unk = new TxtzChunk(reader, null, null, null, stream.Length);

                Assert.True(unk.TextLength > 0);
                Assert.NotNull(unk.Text);
                Assert.Equal(unk.TextLength, unk.Text.Length);
                Assert.Equal(1, unk.Version);

                var result = unk.Zone.SearchForText(new System.Drawing.Rectangle(0, 0, 10000, 10000));
                Assert.NotNull(result);
                Assert.True(result.Length > 0);
            }
        }

        [Fact()]
        public void SearchForTextStringTest()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test077C_P01.txtz");
            using (FileStream stream = new FileStream(file, FileMode.Open))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                TxtzChunk unk = new TxtzChunk(reader, null, null, null, stream.Length);
                string test = "landscapes";

                Assert.True(unk.TextLength > 0);
                Assert.NotNull(unk.Text);
                Assert.Equal(unk.TextLength, unk.Text.Length);
                Assert.Equal(1, unk.Version);
                Assert.True(unk.Text.Contains(test), "Text not found on page");

                int contains = 0;
                int preContains = 0;
                int postContains = 0;

                VerifyZoneContainsText(unk.Zone, test, ref contains, ref preContains, ref postContains);

                Assert.True(contains > 0, "Text not found in zones");
                //Assert.True(postContains > 0, "Text not found in PostText");
                //Assert.True(preContains > 0, "Text not found in PreText");
            }
        }

        public static void VerifyZoneContainsText(TextZone zone, string text, ref int contains, ref int preContains, ref int postContains)
        {
            var result = zone.SearchForText(text);

            foreach (TextZone z in result)
            {
                if (z.Text.Contains(text))
                    contains++;
                if (z.PreText.Contains(text))
                    preContains++;
                if (z.PostText.Contains(text))
                    postContains++;

                foreach (TextZone c in z.Children)
                    VerifyZoneContainsText(z, text, ref contains, ref preContains, ref postContains);
            }
        }

        [Fact()]
        public void OrientedSearchForTextTest()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test077C_P01.txtz");
            using (FileStream stream = new FileStream(file, FileMode.Open))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                TxtzChunk unk = new TxtzChunk(reader, null, null, null, stream.Length);

                Assert.True(unk.TextLength > 0);
                Assert.NotNull(unk.Text);
                Assert.Equal(unk.TextLength, unk.Text.Length);
                Assert.Equal(1, unk.Version);

                var result = unk.Zone.OrientedSearchForText(
                    new System.Drawing.Rectangle(0, 0, 10000, 10000), 1000);
                Assert.True(result != null, "Oriented search result is null");
                Assert.True(result.Length > 0, "Oriented search result length is zero");
            }
        }

        [Theory]
        [MemberData(nameof(TextZoneData))]
        public void TextZone_Theory(string file)
        {
            using (FileStream stream = new FileStream(file, FileMode.Open))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                TxtzChunk unk = new TxtzChunk(reader, null, null, null, stream.Length);

                Assert.True(unk.TextLength > 0);
                Assert.NotNull(unk.Text);
                Assert.Equal(unk.TextLength, unk.Text.Length);
                Assert.Equal(1, unk.Version);

                TextZone zone = unk.Zone;
                var preText = zone.GetPreTextData();
                var postText = zone.GetPostTextData();
                var text = zone.GetTextData();

                Assert.NotNull(text);
            }
        }
    }
}