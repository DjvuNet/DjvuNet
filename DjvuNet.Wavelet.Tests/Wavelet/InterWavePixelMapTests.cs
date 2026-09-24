using Xunit;
using DjvuNet.Wavelet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using DjvuNet;
using DjvuNet.DataChunks;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.Tests;
using System.Text.Json;
using DjvuNet.Extensions;

namespace DjvuNet.Wavelet.Tests
{
    public class InterWavePixelMapTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(-1)]
        [InlineData(255)]
        public void CrCbDelay_Theory(int delay)
        {
            var map = new InterWavePixelMap();
            map.CrCbDelay = delay;
            Assert.Equal(delay, map.CrCbDelay);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CrCbHalf_Theory(bool half)
        {
            var map = new InterWavePixelMap();
            map.CrCbHalf = half;
            Assert.Equal(half, map.CrCbHalf);
        }

        [Fact]
        public void Close_ResetsInternalFields()
        {
            var map = new InterWavePixelMap();
            map.Close();
            Assert.Equal(0, map.Bytes);
            Assert.Equal(0, map.Slices);
            Assert.Equal(0, map.Serial);
        }

        [Fact]
        public void GetPixelMap_NullYMap_ReturnsNull()
        {
            var map = new InterWavePixelMap();
            Assert.Null(map.GetPixelMap());
        }

        [Fact]
        public void GetPixelMap_Dimensions_Throws()
        {
            var map = new InterWavePixelMap();
            map._YMap = new InterWaveMap(30000, 30000); // 900M area is valid for map, but 2.7B pixels exceeds Array.MaxLength
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => map.GetPixelMap());
            Assert.Contains("invalid Data buffer size", ex.Message);
        }

        [Fact]
        public void GetPixelMap_Rect_NullYMap_ReturnsNull()
        {
            var map = new InterWavePixelMap();
            var rect = new Rectangle(0, 0, 100, 100);
            Assert.Null(map.GetPixelMap(1, rect, null));
        }

        [Fact]
        public void GetPixelMap_Rect_Throws()
        {
            var map = new InterWavePixelMap();
            map._YMap = new InterWaveMap(100, 100);
            var rect = new Rectangle(0, 0, 30000, 30000); // Exceeds Array.MaxLength when multiplied by 3
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => map.GetPixelMap(1, rect, null));
            Assert.Contains("invalid Data buffer size", ex.Message);
        }

        public static TheoryData<string, int> BG44PackData
        {
            get
            {
                var data = new TheoryData<string, int>();
                string dataDir = Path.Combine(Util.RepoRoot, "artifacts", "data");
                string[] allBg44 = Directory.GetFiles(dataDir, "*.bg44");
                
                var prefixes = new HashSet<string>();
                foreach (string file in allBg44)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    int lastUnder = name.LastIndexOf('_');
                    if (lastUnder > 0)
                    {
                        prefixes.Add(name.Substring(0, lastUnder));
                    }
                    else
                    {
                        prefixes.Add(name);
                    }
                }
                
                foreach (string prefix in prefixes)
                {
                    int chunkCount = Directory.GetFiles(dataDir, $"{prefix}_*.bg44").Length;
                    if (chunkCount > 0)
                    {
                        data.Add(prefix, chunkCount);
                    }
                }
                return data;
            }
        }

        //[Theory]
        //[MemberData(nameof(BG44PackData))]
        //public void GetPixelMap_BG44(string prefix, int chunkCount)
        //{
        //    string dataDir = Path.Combine(Util.RepoRoot, "artifacts", "data");
        //    var map = new InterWavePixelMapDecoder();

        //    for (int i = 0; i < chunkCount; i++)
        //    {
        //        string file = Path.Combine(dataDir, $"{prefix}_{i}.bg44");
        //        using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
        //        using (IDjvuReader reader = new DjvuReader(fs))
        //        {
        //            BG44Chunk chunk = new BG44Chunk(reader, null, null, "BG44", fs.Length);
        //            chunk.Initialize();
        //            chunk.ProgressiveDecodeBackground(map);
        //        }
        //    }

        //    PixelMap pixelMap = map.GetPixelMap();
        //    Assert.NotNull(pixelMap);
        //    Assert.Equal(map.Width, pixelMap.Width);
        //    Assert.Equal(map.Height, pixelMap.Height);
        //    Assert.NotNull(pixelMap.Data);
            
        //    var rect = new Rectangle(0, 0, map.Width / 2, map.Height / 2);
        //    PixelMap rectPixelMap = map.GetPixelMap(1, rect, null);
        //    Assert.NotNull(rectPixelMap);
        //    Assert.Equal(rect.Width, rectPixelMap.Width);
        //    Assert.Equal(rect.Height, rectPixelMap.Height);
        //    Assert.NotNull(rectPixelMap.Data);
        //}

        [Theory]
        [MemberData(nameof(BG44PackData))]
        public void GetPixelMapFused_BG44(string prefix, int chunkCount)
        {
            string dataDir = Path.Combine(Util.RepoRoot, "artifacts", "data");
            var map = new InterWavePixelMapDecoder();

            for (int i = 0; i < chunkCount; i++)
            {
                string file = Path.Combine(dataDir, $"{prefix}_{i}.bg44");
                using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (IDjvuReader reader = new DjvuReader(fs))
                {
                    BG44Chunk chunk = new BG44Chunk(reader, null, null, "BG44", fs.Length);
                    chunk.Initialize();
                    chunk.ProgressiveDecodeBackground(map);
                }
            }

            PixelMap fusedMap = map.GetPixelMap();

            string pngPath = Path.Combine(Util.RepoRoot, "artifacts", "data", "dumps", $"{prefix}_{chunkCount - 1}_bg44.png");
            if (File.Exists(pngPath))
            {
                using (System.Drawing.Bitmap oracle = new System.Drawing.Bitmap(pngPath))
                {
                    bool result = Util.ImageBinarySimilarity(oracle, fusedMap, 0.0);
                    System.Drawing.Bitmap image = null;
                    try
                    {
                        if (!result)
                        {
                            image = fusedMap.ToImage();
                            Util.DumpImageMismatch(oracle, image, chunkCount - 1, $"{prefix}_BG44");
                        }
                    }
                    finally
                    {
                        image?.Dispose();
                    }
                    Assert.True(result);
                }
            }
        }

        private static TheoryData<string> _fg44ColorData;
        private static TheoryData<string> _fg44GrayscaleData;

        private static void EnsureFG44DataLoaded()
        {
            if (_fg44ColorData != null) return;
            
            _fg44ColorData = new TheoryData<string>();
            _fg44GrayscaleData = new TheoryData<string>();
            
            string dataDir = Path.Combine(Util.RepoRoot, "artifacts", "data");
            string[] allFg44 = Directory.GetFiles(dataDir, "*.fg44");
            
            foreach (string file in allFg44)
            {
                using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (IDjvuReader reader = new DjvuReader(fs))
                {
                    FG44Chunk chunk = new FG44Chunk(reader, null, null, "FG44", fs.Length);
                    chunk.Initialize();
                    var map = (InterWavePixelMapDecoder)chunk.ForegroundImage;
                    
                    if (map._CbMap == null)
                    {
                        _fg44GrayscaleData.Add(Path.GetFileName(file));
                    }
                    else
                    {
                        _fg44ColorData.Add(Path.GetFileName(file));
                    }
                }
            }
        }

        public static TheoryData<string> FG44ColorData
        {
            get
            {
                EnsureFG44DataLoaded();
                return _fg44ColorData;
            }
        }

        public static TheoryData<string> FG44GrayScaleData
        {
            get
            {
                EnsureFG44DataLoaded();
                return _fg44GrayscaleData;
            }
        }

        [Theory]
        [MemberData(nameof(FG44ColorData))]
        public void GetPixelMapFused_ColorFG44(string fileName)
        {
            TestFG44Parity(fileName, expectedGrayscale: false);
        }

        [Theory]
        [MemberData(nameof(FG44GrayScaleData))]
        public void GetPixelMapFused_GrayScaleFG44(string fileName)
        {
            TestFG44Parity(fileName, expectedGrayscale: true);
        }

        private void TestFG44Parity(string fileName, bool expectedGrayscale)
        {
            string file = Path.Combine(Util.RepoRoot, "artifacts", "data", fileName);
            using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (IDjvuReader reader = new DjvuReader(fs))
            {
                FG44Chunk chunk = new FG44Chunk(reader, null, null, "FG44", fs.Length);
                chunk.Initialize();
                var map = (InterWavePixelMapDecoder)chunk.ForegroundImage;

                Assert.Equal(expectedGrayscale, map._CbMap == null);

                PixelMap fusedMap = map.GetPixelMap();

                string pngPath = Path.Combine(Util.RepoRoot, "artifacts", "data", "dumps", Path.GetFileNameWithoutExtension(fileName) + "_fg44.png");
                if (File.Exists(pngPath))
                {
                    using (System.Drawing.Bitmap oracle = new System.Drawing.Bitmap(pngPath))
                    {
                        bool result = Util.ImageBinarySimilarity(oracle, fusedMap, 0.0);
                        System.Drawing.Bitmap image = null;
                        try
                        {
                            if (!result)
                            {
                                image = fusedMap.ToImage();
                                Util.DumpImageMismatch(oracle, image, 0, Path.GetFileNameWithoutExtension(fileName) + "_FG44");
                            }
                        }
                        finally
                        {
                            image?.Dispose();
                        }
                        Assert.True(result);
                    }
                }
            }
        }

        [Fact()]
        public void ImageDataTest()
        {
            InterWavePixelMap map = new InterWavePixelMap();
            Assert.True(map.ImageData);
        }

        private InterWaveMap CreateNoiseMap(int width, int height, int seedOffset)
        {
            var map = new InterWaveMap(width, height);
            var rand = new Random(42 + seedOffset);
            for (int b = 0; b < map.BlockNumber; b++)
            {
                short[] flatBlock = new short[1024];
                for (int c = 0; c < 1024; c++)
                {
                    flatBlock[c] = (short)(rand.Next(-128, 128) << 6);
                }
                map.Blocks[b].ReadLiftBlock(flatBlock);
            }
            return map;
        }

        public static TheoryData<int, int> CascadingWidthData => new TheoryData<int, int> 
        { 
            {15, 16}, {16, 16}, {17, 16},
            {31, 16}, {32, 16}, {33, 16},
            {63, 16}, {64, 16}, {65, 16},
            {113, 16},
            {1031, 16}
        };

        [Theory]
        [MemberData(nameof(CascadingWidthData))]
        public void GetPixelMapFused_CascadingBoundaries_Parity(int width, int height)
        {
            var decoder = new InterWavePixelMapDecoder();
            
            decoder._YMap = CreateNoiseMap(width, height, 0);
            decoder._CbMap = CreateNoiseMap(width, height, 1);
            decoder._CrMap = CreateNoiseMap(width, height, 2);
            decoder._CrCbDelay = 10;

            PixelMap legacyMap = decoder.GetPixelMapStaged();
            PixelMap fusedMap = decoder.GetPixelMap();

            AssertPixelMapParity(legacyMap, fusedMap);
        }

        private unsafe void AssertPixelMapParity(PixelMap legacyMap, PixelMap fusedMap)
        {
            Assert.NotNull(legacyMap);
            Assert.NotNull(fusedMap);
            Assert.Equal(legacyMap.Width, fusedMap.Width);
            Assert.Equal(legacyMap.Height, fusedMap.Height);
            
            string testName = Xunit.TestContext.Current.Test.TestDisplayName;
            int rowSize = legacyMap.Width * 3;
            fixed (sbyte* pLegacy = legacyMap.Data)
            fixed (sbyte* pFused = fusedMap.Data)
            {
                double diff = Util.ImageBinaryDiff((byte*)pLegacy, (byte*)pFused, legacyMap.Width, legacyMap.Height, rowSize);
                if (diff > 0.0)
                {
                    Util.DumpImageMismatch((byte*)pLegacy, (byte*)pFused, legacyMap.Data.Length, legacyMap.Width, 0, testName);
                    Assert.Fail($"Image mismatch in {testName}. Diff score: {diff}");
                }
            }
        }

        [Fact]
        public void VerifyTheoryDataMatchesJsonGroundTruth()
        {
            string dataDir = Path.Combine(Util.RepoRoot, "artifacts", "data");
            
            var bg44Prefixes = new HashSet<string>();
            foreach (var f in Directory.GetFiles(dataDir, "*.bg44"))
            {
                string name = Path.GetFileNameWithoutExtension(f);
                int lastUnder = name.LastIndexOf('_');
                bg44Prefixes.Add(lastUnder > 0 ? name.Substring(0, lastUnder) : name);
            }

            var fg44Files = new HashSet<string>();
            foreach (var f in Directory.GetFiles(dataDir, "*.fg44"))
            {
                fg44Files.Add(Path.GetFileName(f));
            }

            for (int i = 1; i <= 75; i++)
            {
                string prefix = $"test{i:D3}C";
                string jsonPath = Path.Combine(Util.RepoRoot, "artifacts", "json", $"{prefix}.json");
                if (!File.Exists(jsonPath)) continue;

                string jsonStr = File.ReadAllText(jsonPath);
                using (JsonDocument doc = JsonDocument.Parse(jsonStr))
                {
                    JsonElement firstPage = FindFirstFormDjvu(doc.RootElement);
                    if (firstPage.ValueKind != JsonValueKind.Object || !firstPage.TryGetProperty("Children", out var children))
                        continue;

                    bool hasBg44 = false;
                    bool hasFg44 = false;

                    foreach (var child in children.EnumerateArray())
                    {
                        if (child.TryGetProperty("ID", out var id))
                        {
                            if (id.GetString() == "BG44") hasBg44 = true;
                            if (id.GetString() == "FG44") hasFg44 = true;
                        }
                    }

                    if (hasBg44) Assert.Contains($"{prefix}_P01", bg44Prefixes);
                    if (hasFg44) Assert.Contains($"{prefix}_P01.fg44", fg44Files);
                }
            }
        }

        private JsonElement FindFirstFormDjvu(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("ID", out var id) && id.GetString() == "FORM:DJVU")
                    return element;

                foreach (var prop in element.EnumerateObject())
                {
                    var result = FindFirstFormDjvu(prop.Value);
                    if (result.ValueKind == JsonValueKind.Object) return result;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var result = FindFirstFormDjvu(item);
                    if (result.ValueKind == JsonValueKind.Object) return result;
                }
            }
            return default;
        }
    }
}
