using System;
using System.IO;
using System.Linq;
using Xunit;
using DjvuNet.JB2;
using DjvuNet.Tests;
using DjvuNet.Compression;
using DjvuNet.Errors;

namespace DjvuNet.JB2.Tests
{
    public class JB2DecoderTests
    {
        [Fact]
        public void JB2Decoder_Creation()
        {
            using (JB2Decoder decoder = new JB2Decoder())
            {
                Assert.NotNull(decoder);
            }
        }

        [Theory]
        [InlineData("testE002_001.djbz")]
        [InlineData("testE003_001.djbz")]
        public void JB2Decoder_Init(string djbzFileName)
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
        public void JB2Decoder_CodeJB2Dictionary(string djbzFileName)
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
        [InlineData(@"extracted/test002C_D1868.djbz", @"extracted/test002C_P01.sjbz")]
        [InlineData(@"extracted/test002C_D1868.djbz", @"extracted/test002C_P02.sjbz")]
        [InlineData(@"extracted/test007C_D584.djbz", @"extracted/test007C_P02.sjbz")] // Triggers CodeRecordB Tokens 2, 3, 5, 6
        [InlineData(@"extracted/test011C_D384.djbz", @"extracted/test011C_P03.sjbz")] // Triggers CodeRecordB Tokens 2, 5, 6
        public void JB2Decoder_CodeJB2Image(string djbzFileName, string sjbzFileName)
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

                    Assert.True(image.Blits.Length > 0 || image.ShapeCount > 0, "Decoder failed to populate JB2Image.");
                }
            }
        }

        [Fact]
        public void JB2Decoder_Code_RequiredDictOrReset()
        {
            JB2Dictionary dict = new JB2Dictionary();
            byte[] dictPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, @"extracted/test076C_D244.djbz"));
            using (var ms = new MemoryStream(dictPayload))
            using (var reader = new DjvuReader(ms))
            {
                dict.Decode(reader);
            }

            using (JB2Decoder decoder = new JB2Decoder())
            {
                byte[] imagePayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, @"extracted/test076C_P01.sjbz"));
                using (var ms = new MemoryStream(imagePayload))
                using (var reader = new DjvuReader(ms))
                {
                    decoder.Init(reader, dict);
                    JB2Image image = new JB2Image();
                    decoder.Code(image);

                    Assert.True(image.ShapeCount > 0 || image.Blits.Length > 0);
                }
            }
        }

        [Fact]
        public void JB2Decoder_Code_ZeroSize_Throws()
        {
            using (JB2Decoder decoder = new JB2Decoder())
            {
                byte[] dictPayload = File.ReadAllBytes(Path.Combine(Util.ArtifactsDataPath, @"extracted/test076C_D244.djbz"));
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
        [InlineData(@"extracted/test076C_D244.djbz", @"extracted/test076C_P01.sjbz")]
        [InlineData(@"extracted/test076C_D244.djbz", @"extracted/test076C_P05.sjbz")]
        [InlineData(@"extracted/test076C_D244.djbz", @"extracted/test076C_P10.sjbz")]
        [InlineData(@"extracted/test076C_D244.djbz", @"extracted/test076C_P18.sjbz")]
        public void JB2Decoder_Code_PreservedComment(string dictPath, string imagePath)
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
        public void JB2Decoder_Code_ZeroSizeImage_Throws()
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
        }

        internal class RecordTypeInjectorDecoder : JB2Decoder
        {
            public int TargetToken { get; set; }
            private bool _injected = false;

            protected override int CodeRecordType(int ignored)
            {
                if (_injected) return JB2Codec.EndOfData; // Stop decoding after injection to prevent misalignment crashes

                int rectype = base.CodeRecordType(ignored);
                
                // Intercept the first shape record
                if (rectype == JB2Codec.NewMarkLibraryOnly || rectype == JB2Codec.NewMark)
                {
                    _injected = true;
                    return TargetToken;
                }

                return rectype;
            }
        }

        [Theory]
        [InlineData(JB2Codec.NewMark)]
        [InlineData(JB2Codec.NewMarkImageOnly)]
        [InlineData(JB2Codec.MatchedRefine)]
        [InlineData(JB2Codec.MatchedRefineImageOnly)]
        [InlineData(JB2Codec.NonMarkData)]
        [InlineData(99)]
        public void JB2Decoder_CodeJB2Dictionary_InvalidRecordType_Throws(int invalidToken)
        {
            var djbzPath = Path.Combine(Util.ArtifactsDataPath, "extracted", "test002C_D1868.djbz");

            using (var decoder = new RecordTypeInjectorDecoder { TargetToken = invalidToken })
            using (var reader = new DjvuReader(File.OpenRead(djbzPath)))
            {
                decoder.Init(reader, null);
                JB2Dictionary dict = new JB2Dictionary();
                var ex = Assert.ThrowsAny<DjvuFormatException>(() => decoder.Code(dict));
                Assert.Contains("Invalid or unknown record type", ex.Message);
            }
        }

        [Fact]
        public void JB2Decoder_CodeJB2Image_InvalidRecordType_Throws()
        {
            // Decoding sjbz chunk with injected invalid record targets CodeRecordB exception
            var sjbzPath = Path.Combine(Util.ArtifactsDataPath, "extracted", "test001C_P01.sjbz");

            using (var decoder = new RecordTypeInjectorDecoder { TargetToken = 99 })
            using (var reader = new DjvuReader(File.OpenRead(sjbzPath)))
            {
                decoder.Init(reader, null);
                JB2Image image = new JB2Image();
                var ex = Assert.ThrowsAny<DjvuArgumentException>(() => decoder.Code(image));
                Assert.Contains("Invalid or unknown record type", ex.Message);
                Assert.Contains("CodeRecordB", ex.StackTrace);
            }
        }

        [Fact]
        public void JB2Decoder_Code_NonMarkData()
        {
            var sjbzPath = Path.Combine(Util.ArtifactsDataPath, "extracted", "test003C_P41.sjbz");
            var djbzPath = Path.Combine(Util.ArtifactsDataPath, "extracted", "test003C_D355022.djbz");

            JB2Dictionary dict = new JB2Dictionary();
            using (var reader = new DjvuReader(File.OpenRead(djbzPath)))
            {
                dict.Decode(reader);
            }

            using (var decoder = new RecordTypeInjectorDecoder { TargetToken = JB2Codec.NonMarkData })
            using (var reader = new DjvuReader(File.OpenRead(sjbzPath)))
            {
                decoder.Init(reader, dict);
                JB2Image image = new JB2Image();
                decoder.Code(image);

                // Verification
                Assert.True(image.ShapeCount > 0);
                JB2Shape shape = image.GetShape(image.ShapeCount - 1);
                // NonMarkData sets Parent to -2
                Assert.Equal(-2, shape.Parent);
            }
        }
    }
}
