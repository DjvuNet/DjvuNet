using Xunit;
using DjvuNet.Wavelet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DjvuNet.Tests;
using DjvuNet.Graphics;
using DjvuNet.Errors;

namespace DjvuNet.Wavelet.Tests
{
    public class InterWaveMapTests
    {
        /// <summary>
        /// Verifies that the InterWaveMap constructor correctly calculates the 
        /// required number of 32x32 macroblocks based on the given dimensions.
        /// </summary>
        [Fact]
        public void InterWaveMap_Constructor_CalculatesBlockNumberCorrectly()
        {
            int width = 64;
            int height = 64;
            InterWaveMap map = new InterWaveMap(width, height);
            
            Assert.Equal(4, map.BlockNumber);
        }

        /// <summary>
        /// Verifies that the InterWaveMap constructor correctly sizes 
        /// the backing array of InterWaveBlock instances.
        /// </summary>
        [Fact]
        public void InterWaveMap_Constructor_AllocatesBlocksArrayCorrectly()
        {
            int width = 64;
            int height = 64;
            InterWaveMap map = new InterWaveMap(width, height);
            
            Assert.Equal(4, map.Blocks.Length);
        }

        /// <summary>
        /// Verifies that the InterWaveMap constructor correctly instantiates 
        /// an InterWaveBlock object for every element in the backing array.
        /// </summary>
        [Fact]
        public void InterWaveMap_Constructor_InitializesBlockInstances()
        {
            int width = 64;
            int height = 64;
            InterWaveMap map = new InterWaveMap(width, height);
            
            Assert.All(map.Blocks, block => Assert.NotNull(block));
        }

        /// <summary>
        /// Structurally verifies that the internal block tiling logic correctly 
        /// maps sparse 32x32 macroblocks onto the flat 2D canvas, guaranteeing 
        /// geometric parity for future SIMD memory layout refactoring.
        /// </summary>
        [Fact]
        public void BuildUnifiedData_MaintainsGeometricIntegrity()
        {
            int width = 64;
            int height = 64;
            InterWaveMap map = new InterWaveMap(width, height);
            
            // Populate each block with a unique spatial identifier based on its grid position
            for (int b = 0; b < map.BlockNumber; b++)
            {
                short[] flatBlock = new short[1024];
                for (int c = 0; c < 1024; c++)
                {
                    flatBlock[c] = (short)(b + 1); 
                }
                map.Blocks[b].ReadLiftBlock(flatBlock);
            }

            // Extract the unified grid
            short[] unifiedData = map.BuildUnifiedData();
            
            // Verify the geometric stitching
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Determine which block this coordinate belongs to
                    int expectedBlockId = 0;
                    if (y < 32 && x < 32) expectedBlockId = 1;      // Top-Left (Block 0)
                    else if (y < 32 && x >= 32) expectedBlockId = 2;  // Top-Right (Block 1)
                    else if (y >= 32 && x < 32) expectedBlockId = 3;  // Bottom-Left (Block 2)
                    else if (y >= 32 && x >= 32) expectedBlockId = 4; // Bottom-Right (Block 3)
                    
                    int idx = (y * width) + x;
                    Assert.Equal(expectedBlockId, unifiedData[idx]);
                }
            }
        }

        [Fact]
        public void InterWaveMap_Constructor_NegativeDimensions_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => new InterWaveMap(-100, 100));
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => new InterWaveMap(100, -100));
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => new InterWaveMap(0, 100));
        }

        [Fact]
        public void InterWaveMap_Constructor_MassiveDimensions_ThrowsArgumentOutOfRangeException()
        {
            // Simulates a maliciously crafted header where dimensions trigger an integer overflow 
            // during the BlockNumber calculation ((w * h) / 1024), preventing OutOfMemoryException
            // or negative array allocation crashes.
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => new InterWaveMap(int.MaxValue, int.MaxValue));
        }

        [Fact()]
        public void DuplicateTest001()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C_P01_0.bg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                var map = new InterWavePixelMapDecoder();
                map.Decode(reader);
                Assert.NotNull(map._YMap);
                Assert.NotNull(map._YDecoder);
                Assert.NotNull(map._CbMap);
                Assert.NotNull(map._CbDecoder);
                Assert.NotNull(map._CrMap);
                Assert.NotNull(map._CrDecoder);

                var dyMap = map._YMap.Duplicate();
                Assert.NotNull(dyMap);
                Assert.Equal(map._YMap.Width, dyMap.Width);
                Assert.Equal(map._YMap.Height, dyMap.Height);
                Assert.Equal(map._YMap.Blocks.Length, dyMap.Blocks.Length);
            }
        }

        [Fact()]
        public void DuplicateTest002()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C_P01_0.bg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                var map = new InterWavePixelMapDecoder();
                map.Decode(reader);
                Assert.NotNull(map._YMap);
                Assert.NotNull(map._YDecoder);
                Assert.NotNull(map._CbMap);
                Assert.NotNull(map._CbDecoder);
                Assert.NotNull(map._CrMap);
                Assert.NotNull(map._CrDecoder);

                map._YMap.Blocks = null;
                var dyMap = map._YMap.Duplicate();
                Assert.Null(dyMap);
            }
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void BackwardTest()
        {
            Assert.Fail("This test needs an implementation");
        }

        [Fact()]
        public void BackwardFilterTest001()
        {
            InterWaveMap map = new InterWaveMap();
            Assert.Throws<DjvuFormatException>(() => InterWaveMap.BackwardFilter(null, 0, 10, 16, 9, 0));
        }

        [Fact()]
        public void BackwardFilterTest002()
        {
            InterWaveMap map = new InterWaveMap();
            Assert.Throws<DjvuFormatException>(() => InterWaveMap.BackwardFilter(null, 0, 10, 16, 17, 0));
        }

        [Fact()]
        public void GetBucketCountTest()
        {
            string file = Path.Combine(Util.ArtifactsDataPath, "test002C_P01_0.bg44");
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (DjvuReader reader = new DjvuReader(stream))
            {
                var map = new InterWavePixelMapDecoder();
                map.Decode(reader);
                Assert.NotNull(map._YMap);
                Assert.NotNull(map._YDecoder);
                Assert.NotNull(map._CbMap);
                Assert.NotNull(map._CbDecoder);
                Assert.NotNull(map._CrMap);
                Assert.NotNull(map._CrDecoder);

                int result = map._YMap.GetBucketCount();
                Assert.Equal(910, result);
            }
        }

        [Fact()]
        public void ImageTest001()
        {
            int width = 32;
            int height = 32;

            int subsample = 24;

            Rectangle rect = new Rectangle
            {
                XMax = width,
                YMax = height
            };

            InterWaveMap map = new InterWaveMap(width, height);
            Assert.Throws<DjvuArgumentOutOfRangeException>(
                () => map.Image(subsample, rect, 1, new sbyte[width * height], width, 1, true));
        }

        [Fact()]
        public void ImageTest002()
        {
            int width = 32;
            int height = 32;

            int subsample = 4;

            Rectangle rect = new Rectangle
            {
                XMax = 0,
                YMax = height
            };

            InterWaveMap map = new InterWaveMap(width, height);
            Assert.Throws<DjvuArgumentException>(
                () => map.Image(subsample, rect, 1, new sbyte[width * height], width, 1, true));
        }

        [Fact()]
        public void ImageTest003()
        {
            int width = 32;
            int height = 32;

            int subsample = 4;

            Rectangle rect = new Rectangle
            {
                XMin = -3,
                XMax = width - 3,
                YMax = height
            };

            InterWaveMap map = new InterWaveMap(width, height);
            Assert.Throws<DjvuArgumentException>(
                () => map.Image(subsample, rect, 1, new sbyte[width * height], width, 1, true));
        }

        [Fact()]
        public void ImageTest004()
        {
            int width = 32;
            int height = 32;

            int subsample = 4;

            Rectangle rect = new Rectangle
            {
                XMin = 0,
                XMax = width,
                YMin = -3,
                YMax = height - 3
            };

            InterWaveMap map = new InterWaveMap(width, height);
            Assert.Throws<DjvuArgumentException>(
                () => map.Image(subsample, rect, 1, new sbyte[width * height], width, 1, true));
        }

        [Fact()]
        public void ImageTest005()
        {
            int width = 32;
            int height = 32;

            Rectangle rect = new Rectangle
            {
                XMin = 0,
                XMax = width,
                YMin = 0,
                YMax = height
            };

            InterWaveMap map = new InterWaveMap(width, height);
            map.Image(0, new sbyte[width * height], width, 1, true);
        }

        [Fact()]
        public void ImageTest006()
        {
            int width = 32;
            int height = 32;
            int subsample = 1;

            Rectangle rect = new Rectangle
            {
                XMin = 0,
                XMax = width,
                YMin = 0,
                YMax = height
            };

            InterWaveMap map = new InterWaveMap(width, height);
            map.Image(subsample, rect, 1, new sbyte[width * height * 3], width, 1, true);
        }

        [Fact()]
        public void ImageTest007()
        {
            int width = 32;
            int height = 32;
            int subsample = 1;

            Rectangle rect = new Rectangle
            {
                XMin = 0,
                XMax = width,
                YMin = 0,
                YMax = height
            };

            InterWaveMap map = new InterWaveMap(width, height);
            map.Image(subsample, rect, 1, new sbyte[width * height + 1], width, 1, true);
        }

        /// <summary>
        /// EDGE CASE: Image dimensions are not multiples of 32.
        /// BuildUnifiedData relies on the constructor to align BlockWidth/Height.
        /// This test verifies that odd dimensions pad cleanly without IndexOutOfRangeException.
        /// </summary>
        [Fact]
        public void BuildUnifiedData_OddDimensions_PadsToMacroblockBoundaryCleanly()
        {
            int width = 77;
            int height = 99;
            InterWaveMap map = new InterWaveMap(width, height);
            
            // Expected padded dimensions: w=96 (3 blocks), h=128 (4 blocks) => 12 blocks total
            Assert.Equal(96, map.BlockWidth);
            Assert.Equal(128, map.BlockHeight);
            Assert.Equal(12, map.BlockNumber);

            // This should execute cleanly and return the fully padded canvas
            short[] unifiedData = map.BuildUnifiedData();
            
            Assert.Equal(96 * 128, unifiedData.Length);
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void CreateTest()
        {
            Assert.Fail("This test needs an implementation");
        }
    }
}
