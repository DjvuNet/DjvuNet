using Xunit;
using DjvuNet.Wavelet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DjvuNet.Tests;

namespace DjvuNet.Wavelet.Tests
{
    public class InterWaveMapEncoderTests
    {
        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void InterWaveMapEncoderTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void CreateTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void ForwardMaskTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void InterpolateMaskTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Test implementation bug"), Trait("Category", "Skip")]
        [Trait("Category", "Bugtrack")]
        public void SlashresTest001()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C_P01_0.bg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                var map = new InterWavePixelMapEncoder();
                Assert.NotNull(map._YMap);
                Assert.NotNull(map._YEncoder);
                Assert.NotNull(map._CbMap);
                Assert.NotNull(map._CbEncoder);
                Assert.NotNull(map._CrMap);
                Assert.NotNull(map._CrEncoder);

                var emap = (InterWaveMapEncoder)map._YMap;
                emap.Slashres(1);
            }
        }

        [Fact(Skip = "Test implementation bug"), Trait("Category", "Skip")]
        [Trait("Category", "Bugtrack")]
        public void SlashresTest002()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C_P01_0.bg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                var map = new InterWavePixelMapEncoder();
                Assert.NotNull(map._YMap);
                Assert.NotNull(map._YEncoder);
                Assert.NotNull(map._CbMap);
                Assert.NotNull(map._CbEncoder);
                Assert.NotNull(map._CrMap);
                Assert.NotNull(map._CrEncoder);

                var emap = (InterWaveMapEncoder)map._YMap;
                emap.Slashres(2);
            }
        }

        [Fact(Skip = "Test implementation bug"), Trait("Category", "Skip")]
        [Trait("Category", "Bugtrack")]
        public void SlashresTest003()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C_P01_0.bg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                var map = new InterWavePixelMapEncoder();
                Assert.NotNull(map._YMap);
                Assert.NotNull(map._YEncoder);
                Assert.NotNull(map._CbMap);
                Assert.NotNull(map._CbEncoder);
                Assert.NotNull(map._CrMap);
                Assert.NotNull(map._CrEncoder);

                var emap = (InterWaveMapEncoder)map._YMap;
                emap.Slashres(4);
            }
        }

        [Fact(Skip = "Test implementation bug"), Trait("Category", "Skip")]
        [Trait("Category", "Bugtrack")]
        public void SlashresTest004()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C_P01_0.bg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                var map = new InterWavePixelMapEncoder();
                Assert.NotNull(map._YMap);
                Assert.NotNull(map._YEncoder);
                Assert.NotNull(map._CbMap);
                Assert.NotNull(map._CbEncoder);
                Assert.NotNull(map._CrMap);
                Assert.NotNull(map._CrEncoder);

                var emap = (InterWaveMapEncoder)map._YMap;
                emap.Slashres(8);
            }
        }
    }
}