using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DjvuNet.DataChunks;
using DjvuNet.Diagnostics.Tests;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.Tests;
using Xunit;

namespace DjvuNet.JB2.Tests
{
    public sealed class JB2ImageEtwFixture : IDisposable
    {
        private readonly BitmapEventListener _listener;

        public JB2ImageEtwFixture()
        {
            _listener = new BitmapEventListener();
        }

        public void Dispose()
        {
            string framework = "net" + Environment.Version.Major + ".0";
            string dir = Path.Combine(Util.RepoRoot, "TestResults", framework);
            Directory.CreateDirectory(dir);
            BitmapEventListener.ExportToJsonl(Path.Combine(dir, "BitmapAnalytics.jsonl"));
            BitmapEventListener.Records.Clear();
            _listener.Dispose();
        }
    }

    public class JB2ImageMemoryTracedTests : IClassFixture<JB2ImageEtwFixture>
    {

        public static TheoryData<string, string> JB2ImageFullTestData
        {
            get
            {
                var data = new TheoryData<string, string>();
                foreach (var row in Util.GetJB2ImageTestData(new int[0], new string[0], TestCoverage.All))
                {
                    data.Add((string)row[0], (string)row[1]);
                }
                return data;
            }
        }

        [Theory(Skip = ""), Trait("Category", "Skip")]
        [MemberData(nameof(JB2ImageFullTestData))]
        public void DecodeRleRoundtripTest(string djbzFileName, string sjbzFileName)
        {
            DecodeRleRoundtripInternal(djbzFileName, sjbzFileName);
        }

        [Theory(Skip =""), Trait("Category", "Skip")]
        [MemberData(nameof(JB2ImageFullTestData))]
        public void MaskBitmap_Compression_All(string djbzFileName, string sjbzFileName)
        {
            string djbzFile = Util.GetDjbzForSjbz(sjbzFileName);
            JB2Dictionary jb2Dict = null;
            
            if (djbzFile != null)
            {
                jb2Dict = new JB2Dictionary();
                byte[] djbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFile));
                using (var ms = new MemoryStream(djbzPayload))
                using (var reader = new DjvuReader(ms))
                {
                    jb2Dict.Decode(reader);
                }
            }

            byte[] payload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, sjbzFileName));
            JB2Image image = new JB2Image();
            using (var ms = new MemoryStream(payload))
            using (var reader = new DjvuReader(ms))
            {
                image.Decode(reader, jb2Dict);
            }
            
            Bitmap bm = image.GetBitmap();
            int uncompressedSize = bm.Data.Length;
            bm.Compress();
            int compressedSize = bm.RleData.Length;
            double ratio = (double)uncompressedSize / compressedSize;
            
            Console.WriteLine($"[COMPRESSION_DATA] File: {Path.GetFileName(sjbzFileName)} | Uncompressed: {uncompressedSize} | Compressed: {compressedSize} | Ratio: {ratio:F2}");
        }

        private void DecodeRleRoundtripInternal(string djbzFileName, string sjbzFileName)
        {
            JB2Dictionary jb2Dict = null;

            BitmapEventListener.CurrentDjbz.Value = djbzFileName;
            BitmapEventListener.CurrentSjbz.Value = sjbzFileName;

            if (djbzFileName != null)
            {
                jb2Dict = new JB2Dictionary();
                byte[] djbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName));

                using (var ms = new MemoryStream(djbzPayload))
                using (var reader = new DjvuReader(ms))
                {
                    BitmapEventListener.CurrentPhase.Value = "JB2Dictionary_Decode";
                    jb2Dict.Decode(reader);
                }
                Assert.True(jb2Dict.ShapeCount > 0, "Managed dictionary decoded 0 shapes.");
            }

            byte[] sjbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, sjbzFileName));
            JB2Image image = new JB2Image();

            using (var ms = new MemoryStream(sjbzPayload))
            using (var reader = new DjvuReader(ms))
            {
                BitmapEventListener.CurrentPhase.Value = "JB2Image_Decode";
                image.Decode(reader, jb2Dict);
            }

            BitmapEventListener.CurrentPhase.Value = "Bitmap_RleCompression";
            for (int i = 0; i < image.ShapeCount; i++)
            {
                ref JB2Shape shape = ref image.GetShape(i);
                shape.Bitmap.Compress();
                shape.Bitmap.Decompress();
            }

            Assert.True(image.ShapeCount > 0, "JB2Image decoded 0 shapes.");
        }
    }

    public class JB2ImageTests 
    {

        public static IEnumerable<object[]> JB2ImageTestData => Util.GetJB2ImageTestData(
            skipDocs: new int[] { },
            skipChunks: new string[] { },
            TestCoverage.UniqueOnly
        );

        public static IEnumerable<object[]> ExtractedRareVariantData => Util.GetExtractedRareVariantPayloads(encoderCoverage: JB2EncoderTestCoverage.AllVariants);

        [Theory]
        [InlineData(@"extracted/test003C_D453132.djbz", @"extracted/test003C_P53.sjbz")]
        [InlineData(@"extracted/test003C_D453132.djbz", @"extracted/test003C_P54.sjbz")]
        public void Decode_Tokens6And8(string djbzFileName, string sjbzFileName)
        {
            DecodeInternal(djbzFileName, sjbzFileName);
        }

        [Theory]
        [MemberData(nameof(JB2ImageTestData))]
        public void DecodeTest(string djbzFileName, string sjbzFileName)
        {
            DecodeInternal(djbzFileName, sjbzFileName);
        }

        [Theory]
        [InlineData(@"extracted/test002C_P01.sjbz")]
        [InlineData(@"extracted/test007C_P02.sjbz")]
        [InlineData(@"extracted/test011C_P03.sjbz")]
        [InlineData(@"extracted/test023C_P01.sjbz")]
        [InlineData(@"extracted/test032C_P01.sjbz")]
        [InlineData(@"extracted/test033C_P01.sjbz")]
        public void DecodeGeometry(string sjbzFileName)
        {
            byte[] sjbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, sjbzFileName));

            JB2Image imageGeom = new JB2Image();
            using (var ms = new MemoryStream(sjbzPayload))
            using (var reader = new DjvuReader(ms))
            {
                imageGeom.DecodeGeometry(reader);
            }

            // Compute JSON properties from SJBZ path (e.g., "extracted/test007C_P02.sjbz")
            string fileName = Path.GetFileNameWithoutExtension(sjbzFileName);
            string[] parts = fileName.Split('_');
            string baseName = parts[0];
            int pageNumber = int.Parse(parts[1].Substring(1)); // "P02" -> 2

            // Read JSON to verify against the ground-truth INFO chunk
            string jsonPath = Path.Combine(Util.ArtifactsPath, baseName + ".json");
            string jsonContent = File.ReadAllText(jsonPath);
            using (JsonDocument doc = JsonDocument.Parse(jsonContent))
            {
                var rootChildren = doc.RootElement.GetProperty("DjvuData").GetProperty("Children");
                
                // Find the Nth FORM:DJVU page object using a zero-allocation loop
                JsonElement targetPageDjvu = default;
                int pageCount = 0;
                foreach (var child in rootChildren.EnumerateArray())
                {
                    if (child.GetProperty("ID").GetString() == "FORM:DJVU")
                    {
                        pageCount++;
                        if (pageCount == pageNumber)
                        {
                            targetPageDjvu = child;
                            break;
                        }
                    }
                }

                // The INFO chunk contains the page dimensions
                JsonElement infoChunk = default;
                foreach (var child in targetPageDjvu.GetProperty("Children").EnumerateArray())
                {
                    if (child.GetProperty("ID").GetString() == "INFO")
                    {
                        infoChunk = child;
                        break;
                    }
                }

                int expectedWidth = infoChunk.GetProperty("Width").GetInt32();
                int expectedHeight = infoChunk.GetProperty("Height").GetInt32();

                Assert.Equal(expectedWidth, imageGeom.Width);
                Assert.Equal(expectedHeight, imageGeom.Height);
            }

            Assert.Equal(0, imageGeom.ShapeCount);
            Assert.True(imageGeom.Blits.Length == 0);
        }

        [Theory]
        [MemberData(nameof(ExtractedRareVariantData))]
        public void Decode_GeneratedVariants(string variantFile, string originalSjbzPath, string djbzPath)
        {
            JB2Dictionary jb2Dict = null;
            
            if (djbzPath != null)
            {
                jb2Dict = new JB2Dictionary();
                byte[] djbzPayload = File.ReadAllBytes(djbzPath);
                using (var ms = new MemoryStream(djbzPayload))
                using (var reader = new DjvuReader(ms))
                {
                    jb2Dict.Decode(reader);
                }
                Assert.True(jb2Dict.ShapeCount > 0, "Managed dictionary decoded 0 shapes.");
            }

            byte[] origPayload = File.ReadAllBytes(originalSjbzPath);
            JB2Image originalImage = new JB2Image();
            using (var ms = new MemoryStream(origPayload))
            using (var reader = new DjvuReader(ms))
            {
                originalImage.Decode(reader, jb2Dict);
            }

            byte[] sjbzPayload = File.ReadAllBytes(variantFile);
            JB2Image variantImage = new JB2Image();
            
            using (var ms = new MemoryStream(sjbzPayload))
            using (var reader = new DjvuReader(ms))
            {
                variantImage.Decode(reader, jb2Dict);
            }
            
            Bitmap origBitmap = originalImage.GetBitmap();
            Bitmap varBitmap = variantImage.GetBitmap();

            Assert.Equal(origBitmap.Width, varBitmap.Width);
            Assert.Equal(origBitmap.Height, varBitmap.Height);

            double diff;
            unsafe
            {
                fixed (sbyte* pOrig = origBitmap.Data)
                fixed (sbyte* pVar = varBitmap.Data)
                {
                    diff = Util.ImageBinaryDiff((byte*)pOrig + origBitmap.Border, (byte*)pVar + varBitmap.Border, origBitmap.Width, origBitmap.Height, origBitmap.BytesPerRow, 8);
                }
            }
            Assert.True(diff == 0.0, $"Managed pixel parity mismatch! Diff: {diff}. Variant: {Path.GetFileName(variantFile)} vs {Path.GetFileName(originalSjbzPath)}");
        }

        [Theory]
        [InlineData(@"extracted/test053C_P02.sjbz")] // Micro 
        [InlineData(@"extracted/test032C_P01.sjbz")] // Standard 
        [InlineData(@"extracted/test074C_P01.sjbz")] // Massive 
        public void MaskBitmap_Compression(string file)
        {
            string djbzFile = Util.GetDjbzForSjbz(file);
            JB2Dictionary jb2Dict = null;
            
            if (djbzFile != null)
            {
                jb2Dict = new JB2Dictionary();
                byte[] djbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFile));
                using (var ms = new MemoryStream(djbzPayload))
                using (var reader = new DjvuReader(ms))
                {
                    jb2Dict.Decode(reader);
                }
            }

            byte[] payload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, file));
            JB2Image image = new JB2Image();
            using (var ms = new MemoryStream(payload))
            using (var reader = new DjvuReader(ms))
            {
                image.Decode(reader, jb2Dict);
            }
            
            Bitmap bm = image.GetBitmap();
            int uncompressedSize = bm.Data.Length;
            bm.Compress();
            int compressedSize = bm.RleData.Length;
            double ratio = (double)uncompressedSize / compressedSize;
            
            Console.WriteLine($"RLE Compress Mask Bitmap => File: {Path.GetFileName(file)} | Data: {uncompressedSize / 1024.0:F2} KB | RleData: {compressedSize / 1024.0:F2} KB | Ratio: {ratio:F2}x");
        }

        private void DecodeInternal(string djbzFileName, string sjbzFileName)
        {

            string prefixStr = "JB2ImageTests.DecodeTest => ";
            JB2Dictionary jb2Dict = null;

            BitmapEventListener.CurrentDjbz.Value = djbzFileName;
            BitmapEventListener.CurrentSjbz.Value = sjbzFileName;

            if (djbzFileName != null)
            {
                jb2Dict = new JB2Dictionary();
                byte[] djbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName));

                using (var ms = new MemoryStream(djbzPayload))
                using (var reader = new DjvuReader(ms))
                {
                    BitmapEventListener.CurrentPhase.Value = "SharedDictionary_Decode";
                    // This should not throw DjvuEndOfStreamException
                    jb2Dict.Decode(reader);
                }

                // Console.Write($"{prefixStr}Decoded {djbzFileName} with djbz chunk: dictionary containing {jb2Dict.ShapeCount} shapes => ");
                Assert.True(jb2Dict.ShapeCount > 0, "Managed dictionary decoded 0 shapes.");
            }


            byte[] sjbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, sjbzFileName));

            JB2Image image = new JB2Image();

            using (var ms = new MemoryStream(sjbzPayload))
            using (var reader = new DjvuReader(ms))
            {
                BitmapEventListener.CurrentPhase.Value = "PageImage_Decode";
                // This should not throw DjvuNet.DjvuFormatException : Image dictionary not provided.
                image.Decode(reader, jb2Dict);
            }

            string prefix = djbzFileName != null ? String.Empty : prefixStr;

            // Console.WriteLine($"{prefix}Decoded {sjbzFileName} with sjbz chunk: JB2Image containing {image.ShapeCount} shapes.");

            Assert.True(image.ShapeCount > 0, "JB2Image decoded 0 shapes.");
        }

        private JB2Image CreateTestImage(int width, int height)
        {
            var image = new JB2Image { Width = width, Height = height };
            var shape = new JB2Shape() { Parent = -1 }; // Prevent AddShape validation failure
            shape.Bitmap = new DjvuNet.Graphics.Bitmap();
            shape.Bitmap.Init(10, 10, 0);
            shape.Bitmap.Data[0] = 1;

            image.AddShape(ref shape);
            
            var blit = new JB2Blit { Left = 10, Bottom = 10, ShapeNumber = 0 };
            image.AddBlit(ref blit);

            return image;
        }

        [Fact]
        public void Encode_Throws()
        {
            var image = new JB2Image();
            Assert.Throws<DjvuArgumentNullException>(() => image.Encode(null, null));
        }

        [Fact]
        public void Encode()
        {
            var image = CreateTestImage(10, 10);
            
            using (var msEncode = new MemoryStream())
            using (var writer = new DjvuWriter(msEncode))
            {
                image.Encode(writer, null);
                Assert.True(msEncode.Length > 0);
            }
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(10, 0)]
        [InlineData(0, 0)]
        public void GetBitmap_EmptyDimensions_Throws(int width, int height)
        {
            var image = new JB2Image { Width = width, Height = height };
            Assert.Throws<DjvuFormatException>(() => image.GetBitmap());
            Assert.Throws<DjvuFormatException>(() => image.GetBitmap(new Rectangle(0, 0, 10, 10)));
        }

        [Fact]
        public void GetPixelMap_EmptyDimensions_Throws()
        {
            var image = new JB2Image();
            Assert.Throws<DjvuFormatException>(() => image.GetPixelMap(null, 1, 1));
        }

        [Theory]
        // Base valid bounds
        [InlineData(100, 100, 1, 4, 100, 100, 0)]
        // Subsampling logic: swidth = (Width + subsample - 1) / subsample
        [InlineData(100, 100, 2, 4, 50, 50, 2)]
        [InlineData(101, 101, 2, 4, 51, 51, 1)]
        [InlineData(99, 99, 2, 4, 50, 50, 2)]
        [InlineData(10, 10, 12, 1, 1, 1, 0)] // Heavy subsample edge case
        [InlineData(12, 12, 12, 4, 1, 1, 3)] // Border padding evaluation
        // Heavy alignment border calculations
        [InlineData(50, 50, 1, 8, 50, 50, 6)]
        [InlineData(50, 50, 1, 16, 50, 50, 14)]
        [InlineData(50, 50, 1, 32, 50, 50, 14)]
        // Large resolution boundaries
        [InlineData(10000, 10000, 1, 1, 10000, 10000, 0)]
        [InlineData(10000, 10000, 4, 4, 2500, 2500, 0)]
        [InlineData(9999, 9999, 4, 4, 2500, 2500, 0)]
        public void GetBitmap_DimensionsBorder(
            int width, int height, int subsample, int align, 
            int expectedWidth, int expectedHeight, int expectedBorder)
        {
            var image = CreateTestImage(width, height);
            
            Bitmap bm = image.GetBitmap(subsample, align);
            
            Assert.Equal(expectedHeight, bm.Height); 
            Assert.Equal(expectedWidth, bm.Width);
            Assert.Equal(expectedBorder, bm.Border);
            Assert.Equal(expectedWidth + expectedBorder, bm.BytesPerRow); // C++ RowSize equivalent
        }

        [Theory]
        // Valid rectangular intersections
        [InlineData(0, 0, 50, 50, 1, 4, 0, 50, 2)] // x, y, w, h, subsample, align, dispy, expectedW, expectedBorder
        [InlineData(10, 10, 25, 25, 2, 4, 0, 25, 3)]
        [InlineData(-100, -100, 200, 200, 1, 1, 0, 200, 0)] // Negative origin rect
        [InlineData(0, 0, 1, 1, 1, 16, 10, 1, 15)] // Tiny rect, huge align
        [InlineData(500, 500, 10, 10, 4, 4, -5, 10, 2)] // Rect outside bounds, negative dispy
        public void GetBitmap_RectangleDimensions(
            int x, int y, int w, int h, int subsample, int align, int dispy, 
            int expectedWidth, int expectedBorder)
        {
            var image = CreateTestImage(100, 100);
            var rect = new Rectangle(x, y, w, h);
            
            Bitmap bm = image.GetBitmap(rect, subsample, align, dispy);
            
            Assert.Equal(expectedWidth, bm.Width);
            Assert.Equal(h, bm.Height); // Rectangle Height determines bitmap height directly
            Assert.Equal(expectedBorder, bm.Border);
            Assert.Equal(expectedWidth + expectedBorder, bm.BytesPerRow); // C++ RowSize equivalent
        }

        [Theory]
        // Coordinate shifts and components evaluation
        [InlineData(0, 0, 50, 50, 1, 1, 0, 1)] // Rect encompasses blit at (10, 10)
        [InlineData(20, 20, 50, 50, 1, 1, 0, 0)] // Rect misses blit
        [InlineData(10, 10, 1, 1, 1, 1, 0, 1)] // Rect perfectly touches blit
        [InlineData(0, 0, 50, 50, 2, 1, 0, 1)] // Intersect during subsampling
        public void GetBitmap_Rectangle_Intersections(
            int x, int y, int w, int h, int subsample, int align, int dispy, 
            int expectedComponentCount)
        {
            var image = CreateTestImage(100, 100);
            var rect = new Rectangle(x, y, w, h);
            var components = new List<int>();
            
            image.GetBitmap(rect, subsample, align, dispy, components);
            
            Assert.Equal(expectedComponentCount, components.Count);
            if (expectedComponentCount > 0) 
                Assert.Equal(0, components[0]); // Blit index 0
        }

        [Fact]
        public void GetBitmap_ComponentsNull()
        {
            var image = CreateTestImage(100, 100);
            var rect = new Rectangle(0, 0, 20, 20);
            
            Bitmap bm = image.GetBitmap(rect, 1, 1, 0, null);
            Assert.NotEqual<Bitmap>(bm, default);
        }

        [Fact]
        public void GetBitmap_Components_EmptyImage_Throws()
        {
            var image = new JB2Image { Width = 0, Height = 0 };
            var rect = new Rectangle(0, 0, 20, 20);
            var components = new List<int>();
            
            Assert.Throws<DjvuFormatException>(() => image.GetBitmap(rect, 1, 1, 0, components));
        }

        [Fact]
        public void GetBitmap_Components_ShapeBitmapNull_SkipsBlit()
        {
            var image = CreateTestImage(100, 100);
            image.GetShape(0).Bitmap = default; // Invalidate the shape's bitmap
            var rect = new Rectangle(0, 0, 50, 50); // Encompasses the blit
            var components = new List<int>();
            
            image.GetBitmap(rect, 1, 1, 0, components);
            
            // Because the shape's bitmap is null, no blit should be added to components
            Assert.Empty(components);
        }

        [Theory]
        [InlineData(100, 100, 1, 4, 100, 100)]
        [InlineData(100, 100, 2, 4, 50, 50)]
        [InlineData(101, 101, 2, 4, 51, 51)]
        [InlineData(10, 10, 12, 1, 1, 1)]
        public void GetPixelMap_Dimensions(
            int width, int height, int subsample, int align, 
            int expectedWidth, int expectedHeight)
        {
            // Empty image with no blits to avoid NullReferenceException on null palette
            var image = new JB2Image { Width = width, Height = height }; 

            PixelMap pm = image.GetPixelMap(null, subsample, align);
            
            Assert.NotNull(pm);
            Assert.Equal(expectedWidth, pm.Width);
            Assert.Equal(expectedHeight, pm.Height);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(999)]
        [InlineData(int.MaxValue)]
        public void AddBlit_InvalidShapeNumber_Throws(int shapeNumber)
        {
            JB2Image image = new JB2Image();
            JB2Blit blit = new JB2Blit { ShapeNumber = shapeNumber };

            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => image.AddBlit(ref blit));
            Assert.Contains("JB2 decoding failed: Illegal shape number in JB2Blit.", ex.Message);
            Assert.Equal("jb2Blit", ex.ParamName);
        }

        [Fact]
        public void GetBitmap_Parameterless()
        {
            var image = CreateTestImage(50, 50);
            Bitmap bm = image.GetBitmap();
            Assert.NotNull(bm);
        }

        [Fact]
        public void GetBitmap_Subsample()
        {
            var image = CreateTestImage(50, 50);
            Bitmap bm = image.GetBitmap(1);
            Assert.NotNull(bm);
        }

        [Fact]
        public void GetBitmap_Rectangle()
        {
            var image = CreateTestImage(50, 50);
            var rect = new Rectangle(0, 0, 20, 20);
            Bitmap bm = image.GetBitmap(rect);
            Assert.NotNull(bm);
        }

        [Fact]
        public void GetBitmap_RectangleSubsample()
        {
            var image = CreateTestImage(50, 50);
            var rect = new Rectangle(0, 0, 20, 20);
            Bitmap bm = image.GetBitmap(rect, 1);
            Assert.NotNull(bm);
        }

        [Fact]
        public void GetBitmap_RectangleSubsampleAlign()
        {
            var image = CreateTestImage(50, 50);
            var rect = new Rectangle(0, 0, 20, 20);
            Bitmap bm = image.GetBitmap(rect, 1, 4);
            Assert.NotNull(bm);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        [InlineData(-1)]
        public void GetBitmap_InvalidSubsample_Throws(int subsample)
        {
            var image = new JB2Image { Width = 10, Height = 10 };
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => image.GetBitmap(subsample));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        [InlineData(-1)]
        public void GetPixelMap_InvalidSubsample_Throws(int subsample)
        {
            var image = new JB2Image { Width = 10, Height = 10 };
            Assert.Throws<DjvuArgumentOutOfRangeException>(() => image.GetPixelMap(null, subsample, 1));
        }

        [Fact]
        public void GetBitmap_NullComponents()
        {
            var image = CreateTestImage(20, 20);
            var rect = new Rectangle(0, 0, 20, 20);
            Bitmap bm = image.GetBitmap(rect, 1, 4, 0, null);
            Assert.NotNull(bm);
        }

        [Fact]
        public void GetBitmap_NullShapeBitmap_IgnoresSilently()
        {
            var image = new JB2Image { Width = 20, Height = 20 };
            var shape = new JB2Shape().Init(-1);
            shape.Bitmap = default; // explicitly null
            image.AddShape(ref shape);
            JB2Blit blit = new JB2Blit { ShapeNumber = 0, Left = 5, Bottom = 5 };
            image.AddBlit(ref blit);

            Bitmap bm = image.GetBitmap();
            Assert.NotNull(bm);
            Assert.Equal(0, bm.Data.Count(b => b != 0)); // Blank bitmap
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void GetBitmap_SubsampleRendersPixels(int subsample)
        {
            // Create image with 10x10 shape placed at (10, 10)
            var image = CreateTestImage(50, 50); 
            
            Bitmap bm = image.GetBitmap(subsample);
            Assert.True(bm.Data.Count(x => x > 0) > 0); 
        }

        [Theory]
        [InlineData(5, 5, 20, 20, true)] // Viewport clipping correctly includes the shape
        [InlineData(30, 30, 20, 20, false)] // Viewport clipping correctly excludes the shape
        public void GetBitmap_RectangleIntersection(int rx, int ry, int rw, int rh, bool expectedToHit)
        {
            // Create image with 10x10 shape placed at (10, 10)
            var image = CreateTestImage(50, 50); 
            
            var rect = new Rectangle(rx, ry, rw, rh); 
            Bitmap bm = image.GetBitmap(rect);
            
            if (expectedToHit)
            {
                Assert.True(bm.Data.Count(x => x > 0) > 0);
            }
            else
            {
                Assert.Equal(0, bm.Data.Count(x => x > 0)); 
            }
        }

        [Fact]
        public void GetPixelMap_RendersWithPalette()
        {
            var image = new JB2Image { Width = 20, Height = 20 };
            var shape = new JB2Shape().Init(-1);
            shape.Bitmap = new Bitmap();
            shape.Bitmap.Init(2, 2, 0); 
            shape.Bitmap.Data[0] = 1; shape.Bitmap.Data[1] = 1;
            shape.Bitmap.Data[2] = 1; shape.Bitmap.Data[3] = 1; 
            image.AddShape(ref shape);
            JB2Blit blit = new JB2Blit { ShapeNumber = 0, Left = 5, Bottom = 5 };
            image.AddBlit(ref blit);

            var palette = (ColorPalette)RuntimeHelpers.GetUninitializedObject(typeof(ColorPalette));
            // Single red color in palette, map blit 0 to color 0
            palette.PaletteColors = new [] { Pixel.RedPixel }; 
            palette.BlitColors = new int[] { 0 };

            PixelMap pm = image.GetPixelMap(palette, 1, 4);

            Assert.NotNull(pm);
            Assert.Equal(20, pm.Width);
            Assert.Equal(20, pm.Height);
            
            // Verify pixel map received colored pixels from the palette
            Assert.True(pm.Data.Any(p => p != 0));
        }

        [Fact]
        public void Init_ClearsState()
        {
            var image = CreateTestImage(100, 100);
            image.Init();

            Assert.Equal(0, image.Width);
            Assert.Equal(0, image.Height);
            Assert.Equal(image.Blits.Length, 0);
        }
        [Fact]
        public void Decode_Throws()
        {
            JB2Image image = new JB2Image();
            var ex = Assert.Throws<DjvuArgumentNullException>(() => image.Decode(null, null));
            Assert.Contains("cannot be null", ex.Message);
        }

        [Fact]
        public void DecodeGeometry_Throws()
        {
            JB2Image image = new JB2Image();
            var ex = Assert.Throws<DjvuArgumentNullException>(() => image.DecodeGeometry(null));
            Assert.Contains("cannot be null", ex.Message);
        }



    }
}
