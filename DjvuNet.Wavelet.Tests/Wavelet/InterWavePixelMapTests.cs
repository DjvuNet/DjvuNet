using Xunit;
using DjvuNet.Wavelet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DjvuNet.Tests;
using DjvuNet.Graphics.Tests;
using DjvuNet.Graphics;

namespace DjvuNet.Wavelet.Tests
{
    public class InterWavePixelMapTests
    {
        [Fact()]
        public void InterWavePixelMapTest()
        {
            InterWavePixelMap map = new InterWavePixelMap();
        }

        [Fact()]
        public void DecodeTest001()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C_P01_0.bg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                InterWavePixelMap map = new InterWavePixelMap();
                map.Decode(reader);
                Assert.NotNull(map._YMap);
                Assert.NotNull(map._YDecoder);
                Assert.NotNull(map._CbMap);
                Assert.NotNull(map._CbDecoder);
                Assert.NotNull(map._CrMap);
                Assert.NotNull(map._CrDecoder);
            }
        }

        [Fact()]
        public void DecodeTest002()
        {
            byte[] buffer = new byte[] { 0x00, 0x48, 0x03, 0x02, 0x03, 0x3B, 0x04, 0x3F, 0x8A, 0xFF, 0xFF, 0xE9, 0xFB, 0x80, 0x3E, 0xA2 };
            using (MemoryStream stream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                InterWavePixelMap map = new InterWavePixelMap();
                Assert.Throws<DjvuFormatException>(() => map.Decode(reader));
            }
        }

        [Fact()]
        public void DecodeTest003()
        {
            byte[] buffer = new byte[] { 0x00, 0x48, 0x01, 0x05, 0x03, 0x3B, 0x04, 0x3F, 0x8A, 0xFF, 0xFF, 0xE9, 0xFB, 0x80, 0x3E, 0xA2 };
            using (MemoryStream stream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                InterWavePixelMap map = new InterWavePixelMap();
                Assert.Throws<DjvuFormatException>(() => map.Decode(reader));
            }
        }

        [Fact()]
        public void DecodeTest004()
        {
            byte[] buffer = new byte[] { 0x01, 0x48, 0x01, 0x05, 0x03, 0x3B, 0x04, 0x3F, 0x8A, 0xFF, 0xFF, 0xE9, 0xFB, 0x80, 0x3E, 0xA2 };
            using (MemoryStream stream = new MemoryStream(buffer))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                InterWavePixelMap map = new InterWavePixelMap();
                Assert.Throws<DjvuFormatException>(() => map.Decode(reader));
            }
        }

        [Fact()]
        public void CloseCodecTest()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C_P01_0.bg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                InterWavePixelMap map = new InterWavePixelMap();
                map.Decode(reader);
                Assert.NotNull(map._YMap);
                Assert.NotNull(map._YDecoder);
                Assert.NotNull(map._CbMap);
                Assert.NotNull(map._CbDecoder);
                Assert.NotNull(map._CrMap);
                Assert.NotNull(map._CrDecoder);

                map.CloseCodec();
                Assert.NotNull(map._YMap);
                Assert.Null(map._YDecoder);
                Assert.NotNull(map._CbMap);
                Assert.Null(map._CbDecoder);
                Assert.NotNull(map._CrMap);
                Assert.Null(map._CrDecoder);
            }
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetPixelMapTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void GetPixelMapTest1()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void ImageDataTest()
        {
            InterWavePixelMap map = new InterWavePixelMap();
            Assert.True(map.ImageData);
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void EncodeChunkTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void EncodeImageTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void CloseEncoderTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        public void InitializeEncoderTest()
        {
            int width = 512;
            int height = 256;
            var pixMap = PixelMapTests.CreateInitVerifyPixelMap(width, height, Pixel.WhitePixel);
            InterWavePixelMap map = new InterWavePixelMap();
            map.InitializeEncoder(pixMap);
        }
    }
}