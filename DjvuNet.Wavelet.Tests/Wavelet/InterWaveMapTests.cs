using Xunit;
using DjvuNet.Wavelet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DjvuNet.Tests;
using System.Collections.Concurrent;
using DjvuNet.Graphics;
using DjvuNet.Errors;
using DjvuNet.DataChunks;

namespace DjvuNet.Wavelet.Tests
{
    public sealed class InterWaveMapCache : IDisposable
    {
        private readonly ConcurrentDictionary<string, InterWaveMap> _bg44Cache = new ConcurrentDictionary<string, InterWaveMap>();
        private readonly ConcurrentDictionary<string, InterWaveMap> _fg44Cache = new ConcurrentDictionary<string, InterWaveMap>();

        public InterWaveMap GetBG44Map(string prefix)
        {
            return _bg44Cache.GetOrAdd(prefix, p =>
            {
                string dataDir = Path.Combine(Util.RepoRoot, "artifacts", "data");
                var decoder = new InterWavePixelMapDecoder();
                int chunkCount = Directory.GetFiles(dataDir, $"{p}_*.bg44").Length;
                
                for (int i = 0; i < chunkCount; i++)
                {
                    string file = Path.Combine(dataDir, $"{p}_{i}.bg44");
                    using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (IDjvuReader reader = new DjvuReader(fs))
                    {
                        BG44Chunk chunk = new BG44Chunk(reader, null, null, "BG44", fs.Length);
                        chunk.Initialize();
                        chunk.ProgressiveDecodeBackground(decoder);
                    }
                }
                return decoder._YMap;
            });
        }

        public InterWaveMap GetFG44Map(string fileName)
        {
            return _fg44Cache.GetOrAdd(fileName, f =>
            {
                string file = Path.Combine(Util.RepoRoot, "artifacts", "data", f);
                using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (IDjvuReader reader = new DjvuReader(fs))
                {
                    FG44Chunk chunk = new FG44Chunk(reader, null, null, "FG44", fs.Length);
                    chunk.Initialize();
                    var decoder = (InterWavePixelMapDecoder)chunk.ForegroundImage;
                    return decoder._YMap;
                }
            });
        }

        public void Dispose()
        {
            _bg44Cache.Clear();
            _fg44Cache.Clear();
        }
    }

    public class InterWaveMapTests : IClassFixture<InterWaveMapCache>
    {
        private readonly InterWaveMapCache _cache;

        public InterWaveMapTests(InterWaveMapCache cache)
        {
            _cache = cache;
        }
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
        public void InterWaveMap_Constructor_NegativeDimensions_Throws()
        {
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => new InterWaveMap(-100, 100));
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => new InterWaveMap(100, -100));
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => new InterWaveMap(0, 100));
        }

        [Fact]
        public void InterWaveMap_Constructor_MassiveDimensions_Throws()
        {
            // Simulates a maliciously crafted header where dimensions trigger an integer overflow 
            // during the BlockNumber calculation ((w * h) / 1024), preventing OutOfMemoryException
            // or negative array allocation crashes.
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => new InterWaveMap(int.MaxValue, int.MaxValue));
        }

        [Fact]
        public void Image_Rectangle_Throws()
        {
            // Creates a map bypassing the constructor limits to specifically test the Image method's 
            // internal dynamic boundary check. By setting Width to 67108833 and Height to 33, 
            // the Inflate boundaries in Image() force dataSize to exactly 4294967296, 
            // which wraps to 0 in 32-bit math but is correctly caught by our 64-bit promotion.
            var map = new InterWaveMap();
            map.Width = 67108833;
            map.Height = 33;
            map.BlockWidth = 67108864;
            map.BlockHeight = 64;
            
            var rect = new Rectangle(0, 0, 67108833, 33);
            
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => map.Image(1, rect, 0, new sbyte[0], 0, 0, false));
                
            Assert.Contains("invalid buffer size", ex.Message);
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
        public void BuildUnifiedData_OddDimensions()
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


        [Theory]
        // Base Boundaries
        [InlineData(64, 1)] // AVX-512 single loop
        [InlineData(32, 1)] // AVX2 single loop
        [InlineData(16, 1)] // SSE2 single loop
        
        // Multiple Loops
        [InlineData(128, 1)] // AVX-512 double loop
        [InlineData(192, 1)] // AVX-512 triple loop
        
        // Cascading Tier Fallbacks (AVX-512 -> AVX2 -> SSE2 -> Scalar)
        [InlineData(113, 1)] // 64 (V512) + 32 (V256) + 16 (V128) + 1 (Scalar)
        [InlineData(127, 3)] // 64 (V512) + 32 (V256) + 16 (V128) + 15 (Scalar), pixsep 3
        
        // -1 and +1 Edges
        [InlineData(63, 1)]  // V512 - 1
        [InlineData(65, 4)]  // V512 + 1, pixsep 4
        [InlineData(31, 1)]  // V256 - 1
        [InlineData(33, 3)]  // V256 + 1
        [InlineData(15, 1)]  // V128 - 1 (Pure scalar)
        [InlineData(17, 3)]  // V128 + 1
        
        // Larger Prime Sizes
        [InlineData(1031, 1)] 
        [InlineData(1031, 3)]
        [InlineData(2053, 4)]
        public void Image_SIMDBoundaries(int width, int pixsep)
        {
            int height = 16;
            InterWaveMap map = new InterWaveMap(width, height);
            
            int bufferSize = width * height * pixsep;
            sbyte[] img8 = new sbyte[bufferSize];

            map.Image(0, img8, width * pixsep, pixsep, true);
        }



        private unsafe void AssertParity(sbyte[] expectedImg, sbyte[] actualImg, Rectangle rect, int pixsep, string testName)
        {
            fixed (sbyte* pExp = expectedImg)
            fixed (sbyte* pAct = actualImg)
            {
                double diff = Util.ImageBinaryDiff((byte*)pExp, (byte*)pAct, rect.Width, rect.Height, rect.Width * pixsep, 0, (PixelSize)(pixsep * 8), ChannelSize._8bit);
                if (diff > 0.0)
                {
                    Util.DumpImageMismatch((byte*)pExp, (byte*)pAct, expectedImg.Length, rect.Width, 0, testName);
                }
                Assert.True(diff == 0.0, $"[{testName}] SIMD Pixel Parity mismatch! Diff score: {diff}.");
            }
        }

        [Theory]
        [InlineData(64, 1)] 
        [InlineData(32, 1)] 
        [InlineData(16, 1)] 
        [InlineData(113, 1)] 
        [InlineData(63, 1)]  
        [InlineData(31, 1)]  
        [InlineData(64, 3)] 
        [InlineData(113, 3)] 
        [InlineData(31, 3)]  
        public void Image_SIMDParity(int width, int pixsep)
        {
            int height = 16;
            InterWaveMap map = new InterWaveMap(width, height);
            
            for (int b = 0; b < map.BlockNumber; b++)
            {
                short[] flatBlock = new short[1024];
                for (int c = 0; c < 1024; c++)
                    flatBlock[c] = (short)((c % 512) - 256); 
                map.Blocks[b].ReadLiftBlock(flatBlock);
            }
            
            int bufferSize = width * height * pixsep;
            sbyte[] actualImg = new sbyte[bufferSize];

            map.Image(0, actualImg, width * pixsep, pixsep, true);

            sbyte[] expectedImg = new sbyte[bufferSize];
            map.ImageScalar(0, expectedImg, width * pixsep, pixsep, true);

            Rectangle rect = new Rectangle(0, 0, width, height);

            AssertParity(expectedImg, actualImg, rect, pixsep, $"SIMD Parity Check (Width: {width}, PixSep: {pixsep})");
        }

        [Theory]
        [InlineData(64, 1)] 
        [InlineData(32, 1)] 
        [InlineData(16, 1)] 
        [InlineData(128, 1)] 
        [InlineData(192, 1)] 
        [InlineData(113, 1)] 
        [InlineData(127, 3)] 
        [InlineData(63, 1)]  
        [InlineData(65, 4)]  
        [InlineData(31, 1)]  
        [InlineData(33, 3)]  
        [InlineData(15, 1)]  
        [InlineData(17, 3)]  
        [InlineData(1031, 1)] 
        [InlineData(1031, 3)]
        [InlineData(2053, 4)]
        public void Image_Rectangle_SIMDBoundaries(int width, int pixsep)
        {
            int height = 16;
            InterWaveMap map = new InterWaveMap(width, height);
            
            int bufferSize = width * height * pixsep;
            sbyte[] img8 = new sbyte[bufferSize];

            Rectangle rect = new Rectangle(0, 0, width, height);
            map.Image(1, rect, 0, img8, width * pixsep, pixsep, true);
        }

        public static TheoryData<int, int> RectangleParityData
        {
            get
            {
                var data = new TheoryData<int, int>();
                int[] widths = new int[] 
                { 
                    15, 16, 17, 
                    31, 32, 33, 
                    47, 48, 49,
                    7, 13, 23, 41, 53, 71, 
                    15 * 7, 31 * 3, 47 * 5,
                    63, 64, 113, 128
                };
                int[] pixseps = new int[] { 1, 3, 4 };
                
                foreach (var w in widths)
                    foreach (var p in pixseps)
                        data.Add(w, p);

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(RectangleParityData))]
        public void Image_Rectangle_SIMDParity(int width, int pixsep)
        {
            int height = 16;
            InterWaveMap map = new InterWaveMap(width, height);
            
            for (int b = 0; b < map.BlockNumber; b++)
            {
                short[] flatBlock = new short[1024];
                for (int c = 0; c < 1024; c++)
                    flatBlock[c] = (short)((c % 512) - 256); 
                map.Blocks[b].ReadLiftBlock(flatBlock);
            }
            
            int bufferSize = width * height * pixsep;
            sbyte[] actualImg = new sbyte[bufferSize];

            Rectangle rect = new Rectangle(0, 0, width, height);
            map.Image(1, rect, 0, actualImg, width * pixsep, pixsep, true);

            sbyte[] expectedImg = new sbyte[bufferSize];
            map.ImageScalar(1, rect, 0, expectedImg, width * pixsep, pixsep, true);

            AssertParity(expectedImg, actualImg, rect, pixsep, $"SIMD Rectangle Parity Check (Width: {width}, PixSep: {pixsep})");
        }

        [Theory]
        [MemberData(nameof(RectangleParityData))]
        public void Image_Rectangle_Cropped_SIMDParity(int width, int pixsep)
        {
            int height = 16;
            InterWaveMap map = new InterWaveMap(width, height);
            
            for (int b = 0; b < map.BlockNumber; b++)
            {
                short[] flatBlock = new short[1024];
                for (int c = 0; c < 1024; c++) flatBlock[c] = (short)((c % 512) - 256); 
                map.Blocks[b].ReadLiftBlock(flatBlock);
            }
            
            Rectangle rect = new Rectangle(width / 4, 1, width / 2, 8);
            int bufferSize = rect.Width * rect.Height * pixsep;
            sbyte[] actualImg = new sbyte[bufferSize];

            map.Image(1, rect, 0, actualImg, rect.Width * pixsep, pixsep, true);
            
            sbyte[] expectedImg = new sbyte[bufferSize];
            map.ImageScalar(1, rect, 0, expectedImg, rect.Width * pixsep, pixsep, true);
            
            AssertParity(expectedImg, actualImg, rect, pixsep, $"SIMD Cropped Rectangle Parity Check (Width: {width}, PixSep: {pixsep})");
        }

        public static TheoryData<string, int, int, int, int, int> RealBG44Matrix
        {
            get
            {
                var data = new TheoryData<string, int, int, int, int, int>();
                string[] bgPrefixes = { "test001C_P01", "test002C_P01", "test042C_P01" };
                
                foreach (string prefix in bgPrefixes)
                {
                    data.Add(prefix, 1, 0, 0, 1024, 1024);   // Grayscale, full block
                    data.Add(prefix, 3, 0, 0, 1024, 1024);   // RGB Interleaving
                    data.Add(prefix, 3, 10, 20, 200, 300);   // Unaligned Cropped RGB
                }
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(RealBG44Matrix))]
        public void Image_RealBG44_SIMDParity(string prefix, int pixsep, int x, int y, int width, int height)
        {
            InterWaveMap map = _cache.GetBG44Map(prefix);
            Assert.NotNull(map);

            int safeWidth = Math.Min(width, map.Width - x);
            int safeHeight = Math.Min(height, map.Height - y);
            Rectangle rect = new Rectangle(x, y, safeWidth, safeHeight);
            
            int bufferSize = rect.Width * rect.Height * pixsep;
            sbyte[] actualImg = new sbyte[bufferSize];

            map.Image(1, rect, 0, actualImg, rect.Width * pixsep, pixsep, true);
            sbyte[] expectedImg = new sbyte[bufferSize];
            map.ImageScalar(1, rect, 0, expectedImg, rect.Width * pixsep, pixsep, true);
            
            AssertParity(expectedImg, actualImg, rect, pixsep, 
                $"Real BG44 parity check (Prefix: {prefix}, PixSep: {pixsep}, Crop: {rect.Width}x{rect.Height})");
        }

        public static TheoryData<string, int, int, int, int, int> RealFG44Matrix
        {
            get
            {
                var data = new TheoryData<string, int, int, int, int, int>();
                string[] fgFiles = { "test001C_P01.fg44", "test030C_P01.fg44", "test056C_P01.fg44" };
                
                foreach (string file in fgFiles)
                {
                    data.Add(file, 1, 0, 0, 128, 128);       // Grayscale, full block
                    data.Add(file, 3, 0, 0, 128, 128);       // RGB Interleaving
                    data.Add(file, 1, 5, 5, 50, 50);         // Unaligned Cropped Grayscale
                }
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(RealFG44Matrix))]
        public void Image_RealFG44_SIMDParity(string file, int pixsep, int x, int y, int width, int height)
        {
            InterWaveMap map = _cache.GetFG44Map(file);
            Assert.NotNull(map);

            int safeWidth = Math.Min(width, map.Width - x);
            int safeHeight = Math.Min(height, map.Height - y);
            Rectangle rect = new Rectangle(x, y, safeWidth, safeHeight);
            
            int bufferSize = rect.Width * rect.Height * pixsep;
            sbyte[] actualImg = new sbyte[bufferSize];

            map.Image(1, rect, 0, actualImg, rect.Width * pixsep, pixsep, true);
            sbyte[] expectedImg = new sbyte[bufferSize];
            map.ImageScalar(1, rect, 0, expectedImg, rect.Width * pixsep, pixsep, true);
            
            AssertParity(expectedImg, actualImg, rect, pixsep, 
                $"Real FG44 parity check (File: {file}, PixSep: {pixsep}, Crop: {rect.Width}x{rect.Height})");
        }
    }
}
