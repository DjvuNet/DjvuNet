using Xunit;
using DjvuNet.DataChunks.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using DjvuNet.Errors;

namespace DjvuNet.DataChunks.Graphics.Tests
{
    public class ColorPaletteTests
    {
        [Fact()]
        public void ColorPaletteTest001()
        {
            Assert.Throws<DjvuArgumentNullException>("reader", () =>  new ColorPalette(null, null));
        }

        [Fact()]
        public void ColorPaletteTest002()
        {
            using (MemoryStream ms = new MemoryStream())
            using (DjvuReader reader = new DjvuReader(ms))
            {
                Assert.Throws<DjvuArgumentNullException>("parent", () => new ColorPalette(reader, null));
            }
        }

        [Fact()]
        public void ColorPaletteTest003()
        {
            using (MemoryStream ms = new MemoryStream(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0}))
            using (DjvuReader reader = new DjvuReader(ms))
            {
                Mock<FGbzChunk> fgbzMoq = new Mock<FGbzChunk>();
                ColorPalette pallette = new ColorPalette(reader, fgbzMoq.Object);
                Assert.NotNull(pallette);
                Assert.NotNull(pallette.PaletteColors);
                Assert.Equal<int>(0, pallette.PaletteColors.Length);
                Assert.NotNull(pallette.BlitColors);
                Assert.Equal<int>(0, pallette.BlitColors.Length);
            }
        }

        [Fact()]
        public void ReadPaletteDataTest001()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] testBuffer = new byte[] { 0, 0, 0x03, 0x77, 0x77, 0x77, 0x21, 0x31, 0x51, 0xff, 0xdd, 0xaa };
                ms.Write(testBuffer, 0, testBuffer.Length);
                ms.Flush();
                ms.Position = 0;

                using (DjvuReader reader = new DjvuReader(ms))
                {
                    Mock<FGbzChunk> fgbzMoq = new Mock<FGbzChunk>();
                    ColorPalette pallette = new ColorPalette(reader, fgbzMoq.Object);
                    Assert.NotNull(pallette);
                    Assert.NotNull(pallette.PaletteColors);
                    Assert.Equal<int>(3, pallette.PaletteColors.Length);
                }
            }
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void index_to_colorTest()
        {
            Assert.True(false, "This test needs an implementation");
        }
    }
}