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

    }
}