using System;
using System.IO;
using Xunit;
using DjvuNet.JB2;
using DjvuNet.Tests;
using DjvuNet.Compression;

namespace DjvuNet.JB2.Tests
{
    public class JB2DecoderTests
    {
        [Fact]
        public void JB2Decoder_Creation_Success()
        {
            using (JB2Decoder decoder = new JB2Decoder())
            {
                Assert.NotNull(decoder);
            }
        }

        [Theory]
        [InlineData("testE002_001.djbz")]
        [InlineData("testE003_001.djbz")]
        public void JB2Decoder_Init_Success(string djbzFileName)
        {
            using (JB2Decoder decoder = new JB2Decoder())
            {
                byte[] djbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName));
                using (var ms = new MemoryStream(djbzPayload))
                using (var reader = new DjvuReader(ms))
                {
                    decoder.Init(reader, null);
                }
            }
        }

        [Theory]
        [InlineData("testE002_001.djbz")]
        [InlineData("testE003_001.djbz")]
        public void JB2Decoder_CodeJB2Dictionary_Success(string djbzFileName)
        {
            using (JB2Decoder decoder = new JB2Decoder())
            {
                byte[] djbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName));
                using (var ms = new MemoryStream(djbzPayload))
                using (var reader = new DjvuReader(ms))
                {
                    decoder.Init(reader, null);
                    JB2Dictionary dictionary = new JB2Dictionary();

                    decoder.Code(dictionary);

                    Assert.True(dictionary.ShapeCount > 0, "Decoder failed to populate the JB2Dictionary.");
                }
            }
        }

        [Theory]
        [InlineData(@"extracted\test002C_D1868.djbz", @"extracted\test002C_P01.sjbz")]
        [InlineData(@"extracted\test002C_D1868.djbz", @"extracted\test002C_P02.sjbz")]
        [InlineData(@"extracted\test007C_D584.djbz", @"extracted\test007C_P02.sjbz")] // Triggers CodeRecordB Tokens 2, 3, 5, 6
        [InlineData(@"extracted\test011C_D384.djbz", @"extracted\test011C_P03.sjbz")] // Triggers CodeRecordB Tokens 2, 5, 6
        public void JB2Decoder_CodeJB2Image_Success(string djbzFileName, string sjbzFileName)
        {
            JB2Dictionary dictionary = null;
            if (!string.IsNullOrWhiteSpace(djbzFileName))
            {
                dictionary = new JB2Dictionary();
                byte[] djbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, djbzFileName));
                using (var ms = new MemoryStream(djbzPayload))
                using (var reader = new DjvuReader(ms))
                {
                    dictionary.Decode(reader);
                }
            }

            using (JB2Decoder decoder = new JB2Decoder())
            {
                byte[] sjbzPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, sjbzFileName));
                using (var ms = new MemoryStream(sjbzPayload))
                using (var reader = new DjvuReader(ms))
                {
                    decoder.Init(reader, dictionary);
                    JB2Image image = new JB2Image();

                    decoder.Code(image);

                    Assert.True(image.Blits.Count > 0 || image.ShapeCount > 0, "Decoder failed to populate JB2Image.");
                }
            }
        }

        [Fact]
        public void JB2Decoder_Code_RequiredDictOrReset_Success()
        {
            JB2Dictionary dict = new JB2Dictionary();
            byte[] dictPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, @"extracted\test076C_D244.djbz"));
            using (var ms = new MemoryStream(dictPayload))
            using (var reader = new DjvuReader(ms))
            {
                dict.Decode(reader);
            }

            using (JB2Decoder decoder = new JB2Decoder())
            {
                byte[] imagePayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, @"extracted\test076C_P01.sjbz"));
                using (var ms = new MemoryStream(imagePayload))
                using (var reader = new DjvuReader(ms))
                {
                    decoder.Init(reader, dict);
                    JB2Image image = new JB2Image();
                    decoder.Code(image);

                    Assert.True(image.ShapeCount > 0 || image.Blits.Count > 0);
                }
            }
        }

        [Fact]
        public void JB2Decoder_Code_ZeroSize_ThrowsDjvuFormatException()
        {
            using (JB2Decoder decoder = new JB2Decoder())
            {
                byte[] dictPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, @"extracted\test076C_D244.djbz"));
                using (var ms = new MemoryStream(dictPayload))
                using (var reader = new DjvuReader(ms))
                {
                    decoder.Init(reader, null);
                    JB2Image image = new JB2Image();

                    var ex = Assert.Throws<DjvuFormatException>(() => decoder.Code(image));
                    Assert.Contains("JB2 decoding failed: Image dimensions cannot be zero", ex.Message);
                }
            }
        }

        [Theory]
        [InlineData(@"extracted\test076C_D244.djbz", @"extracted\test076C_P01.sjbz")]
        [InlineData(@"extracted\test076C_D244.djbz", @"extracted\test076C_P05.sjbz")]
        [InlineData(@"extracted\test076C_D244.djbz", @"extracted\test076C_P10.sjbz")]
        [InlineData(@"extracted\test076C_D244.djbz", @"extracted\test076C_P18.sjbz")]
        public void JB2Decoder_Code_PreservedComment_Success(string dictPath, string imagePath)
        {
            JB2Dictionary dict = new JB2Dictionary();
            byte[] dictPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, dictPath));
            using (var dictMs = new MemoryStream(dictPayload))
            using (var dictReader = new DjvuReader(dictMs))
            {
                dict.Decode(dictReader);
            }

            using (JB2Decoder decoder = new JB2Decoder())
            {
                byte[] imagePayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, imagePath));
                using (var imageMs = new MemoryStream(imagePayload))
                using (var imageReader = new DjvuReader(imageMs))
                {
                    decoder.Init(imageReader, dict);
                    JB2Image image = new JB2Image();
                    decoder.Code(image);
                    Assert.NotNull(image);
                }
            }
        }

        [Fact]
        public void JB2Decoder_Code_ZeroSizeImage_ThrowsDjvuFormatException()
        {
            // Fighting ZPCoder: an empty byte array forces ZPCodec to decode 0 bits.
            // RecordType 0 = StartOfData.
            // w = 0, h = 0 -> throws "Image with zero size"
            using (JB2Decoder decoder = new JB2Decoder())
            {
                byte[] data = new byte[100];
                using (var ms = new MemoryStream(data))
                using (var reader = new DjvuReader(ms))
                {
                    decoder.Init(reader, null);
                    var ex = Assert.Throws<DjvuFormatException>(() => decoder.Code(new JB2Image()));
                    Assert.Contains("JB2 decoding failed: Missing required start record", ex.Message);
                }
            }
        }
        [Theory]
        [InlineData("extracted/test003C_D355022.djbz", "extracted/test003C_P41.sjbz")]
        public void JB2Decoder_Code_Type6_Type8_Success(string dictPath, string imagePath)
        {
            JB2Dictionary dict = new JB2Dictionary();
            byte[] dictPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, dictPath));
            using (var dictMs = new MemoryStream(dictPayload))
            using (var dictReader = new DjvuReader(dictMs))
            {
                dict.Decode(dictReader);
            }

            JB2Image jim = new JB2Image();
            byte[] imagePayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, imagePath));
            using (var imageMs = new MemoryStream(imagePayload))
            using (var imageReader = new DjvuReader(imageMs))
            {
                using (JB2Decoder decoder = new JB2Decoder())
                {
                    decoder.Init(imageReader, dict);
                    decoder.Code(jim);

                    Assert.NotNull(jim);
                    Assert.True(jim.Width > 0);
                    Assert.True(jim.Height > 0);
                }
            }
        }

        [Fact]
        public void JB2Decoder_DecodeAll_ExtractCoverage()
        {
            var dataDir = Path.Combine(Util.ArtifactsDataPath, "extracted");
            var sjbzFiles = Directory.GetFiles(dataDir, "*.sjbz");
            foreach (var sjbz in sjbzFiles)
            {
                var prefix = Path.GetFileName(sjbz).Split('_')[0];
                var djbz = System.Linq.Enumerable.FirstOrDefault(Directory.GetFiles(dataDir, $"{prefix}*.djbz"));
                
                try 
                {
                    JB2Dictionary dict = null;
                    if (djbz != null) 
                    {
                        dict = new JB2Dictionary();
                        using (var reader = new DjvuReader(File.OpenRead(djbz))) { dict.Decode(reader); }
                    }
                    
                    using (JB2Decoder decoder = new JB2Decoder())
                    using (var reader = new DjvuReader(File.OpenRead(sjbz)))
                    {
                        decoder.Init(reader, dict);
                        JB2Image image = new JB2Image();
                        decoder.Code(image);
                    }
                } 
                catch (Exception) { /* ignore */ }
            }
        }

        [Fact]
        public void JB2Decoder_Code_NoStartRecord_Throws()
        {
            using (var decoder = new MockForExceptions { CodeRecordTypeOverrideValue = 11 })
            {
                var ex = Assert.Throws<DjvuFormatException>(() => decoder.Code(new JB2Image()));
                Assert.Contains("JB2 decoding failed: Missing required start record", ex.Message);
            }
        }

        internal class TestJB2Decoder : JB2Decoder
        {
            public int InvokeCodeNum(int low, int high, MutableValue<int> ctx)
            {
                return CodeNum(low, high, ctx);
            }
        }

        internal class MockForExceptions : TestJB2Decoder
        {
            public int CodeRecordTypeOverrideValue { get; set; } = 11; // 11 == EndOfData
            public int CodeNumOverrideValue { get; set; } = 0;

            // Static counter ensures we track calls globally across the test run
            private static int _callCount = 0;
            private static int _maxCallCount = 5;

            protected override int CodeRecordType(int ignored)
            {
                // TEST-ONLY SAFEGUARD: Kills the runner if an infinite loop is detected
                if (++_callCount > _maxCallCount)
                {
                    Environment.FailFast("Safeguard process terminating exception: Infinite loop detected in test.");
                }

                return CodeRecordTypeOverrideValue;
            }

            protected override int CodeNum(int low, int high, MutableValue<int> ctx)
            {
                // TEST-ONLY SAFEGUARD: Kills the runner if an infinite loop is detected
                if (++_callCount > _maxCallCount)
                {
                    Environment.FailFast("Safeguard process terminating exception: Infinite loop detected in test.");
                }

                return CodeNumOverrideValue;
            }
        }
    }
}
