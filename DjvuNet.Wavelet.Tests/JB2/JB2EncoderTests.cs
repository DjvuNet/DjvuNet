using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using DjvuNet;
using DjvuNet.Tests;
using DjvuNet.Compression;
using DjvuNet.JB2;
using DjvuNet.Errors;
using DjvuNet.Graphics;

namespace DjvuNet.JB2.Tests
{
    public static class JB2Constants
    {
        public const int StartOfData = 0;
        public const int NewMark = 1;
        public const int NewMarkLibraryOnly = 2;
        public const int NewMarkImageOnly = 3;
        public const int MatchedRefine = 4;
        public const int MatchedRefineLibraryOnly = 5;
        public const int MatchedRefineImageOnly = 6;
        public const int MatchedCopy = 7;
        public const int NonMarkData = 8;
        public const int RequiredDictOrReset = 9;
        public const int PreservedForBzz = 10;
        public const int EndOfData = 11;
    }

    public class JB2EncoderTests
    {
        private class TestJB2Encoder : JB2Encoder
        {
            public void InvokeCodeAbsoluteLocation(JB2Blit jblt, int rows, int columns)
            {
                CodeAbsoluteLocation(jblt, rows, columns);
            }

            public void InvokeCodeAbsoluteMarkSize(DjvuNet.Graphics.IBitmap bm, int border)
            {
                CodeAbsoluteMarkSize(bm, border);
            }

            public void InvokeCodeRelativeMarkSize(DjvuNet.Graphics.IBitmap bm, int cw, int ch, int border)
            {
                CodeRelativeMarkSize(bm, cw, ch, border);
            }

            public int InvokeGetDiff(int diff, MutableValue<int> rel_loc)
            {
                return GetDiff(diff, rel_loc);
            }

            public void InvokeCodeBitmapDirectly(DjvuNet.Graphics.IBitmap bm)
            {
                CodeBitmapDirectly(bm);
            }

            public void InvokeCodeBitmapByCrossCoding(DjvuNet.Graphics.IBitmap bm, DjvuNet.Graphics.IBitmap cbm)
            {
                bm.SetMinimumBorder(2);
                cbm.SetMinimumBorder(2);
                int dy = bm.Height - 1;
                CodeBitmapByCrossCoding(bm, cbm, 0, bm.Width, dy, dy, 
                    bm.RowOffset(dy + 1), bm.RowOffset(dy),
                    cbm.RowOffset(dy + 1), cbm.RowOffset(dy), cbm.RowOffset(dy - 1));
            }

            public void EncodeRecordType(int rectype)
            {
                CodeRecordType(rectype);
            }

            public void SetGotStartRecord(bool value = true)
            {
                _GotStartRecordP = value;
            }

            public void SetImageSize(int columns, int rows)
            {
                _ImageColumns = columns;
                _ImageRows = rows;
            }
        }

        private class TestJB2Decoder : JB2Decoder
        {
            public int DecodeRecordType()
            {
                return CodeRecordType(0);
            }

            public void DecodeAbsoluteLocation(JB2Blit jblt, int rows, int columns)
            {
                CodeAbsoluteLocation(jblt, rows, columns);
            }

            public void DecodeAbsoluteMarkSize(DjvuNet.Graphics.IBitmap bm, int border)
            {
                CodeAbsoluteMarkSize(bm, border);
            }

            public void DecodeRelativeMarkSize(DjvuNet.Graphics.IBitmap bm, int cw, int ch, int border)
            {
                CodeRelativeMarkSize(bm, cw, ch, border);
            }

            public int DecodeGetDiff(MutableValue<int> rel_loc)
            {
                return GetDiff(0, rel_loc);
            }

            public void DecodeBitmapDirectly(DjvuNet.Graphics.IBitmap bm)
            {
                CodeBitmapDirectly(bm);
            }

            public void DecodeBitmapByCrossCoding(DjvuNet.Graphics.IBitmap bm, DjvuNet.Graphics.IBitmap cbm)
            {
                bm.SetMinimumBorder(2);
                cbm.SetMinimumBorder(2);
                int dy = bm.Height - 1;
                CodeBitmapByCrossCoding(bm, cbm, 0, bm.Width, dy, dy, 
                    bm.RowOffset(dy + 1), bm.RowOffset(dy),
                    cbm.RowOffset(dy + 1), cbm.RowOffset(dy), cbm.RowOffset(dy - 1));
            }

            public void SetImageSize(int columns, int rows)
            {
                _ImageColumns = columns;
                _ImageRows = rows;
            }

            public void SetGotStartRecord(bool value = true)
            {
                _GotStartRecordP = value;
            }
        }

        [Fact]
        public void JB2Encoder_Creation_Success()
        {
            JB2Encoder encoder = new JB2Encoder();
            Assert.NotNull(encoder);
        }

        [Theory]
        [InlineData(JB2Constants.StartOfData)]
        [InlineData(JB2Constants.NewMark)]
        [InlineData(JB2Constants.NewMarkLibraryOnly)]
        [InlineData(JB2Constants.NewMarkImageOnly)]
        [InlineData(JB2Constants.MatchedRefine)]
        [InlineData(JB2Constants.MatchedRefineLibraryOnly)]
        [InlineData(JB2Constants.MatchedRefineImageOnly)]
        [InlineData(JB2Constants.MatchedCopy)]
        [InlineData(JB2Constants.NonMarkData)]
        [InlineData(JB2Constants.RequiredDictOrReset)]
        [InlineData(JB2Constants.PreservedForBzz)]
        [InlineData(JB2Constants.EndOfData)]
        public void JB2Encoder_Init_And_EncodeRecordType(int recordType)
        {
            using (var stream = new MemoryStream())
            {
                using (var encoder = new TestJB2Encoder())
                {
                    // This should initialize the ZPCodec inside JB2Encoder
                    encoder.Init(stream, null);

                    // Encode the record type
                    encoder.EncodeRecordType(recordType);
                }
            }
        }

        [Fact]
        public void JB2Encoder_EncodeDictionary_WithInheritedDictionary_RoundTrips()
        {
            // 1. Establish the inherited parent dictionary (dictA)
            var dictA = new JB2Dictionary();
            var djbzPath = Path.Combine(Util.ArtifactsDataPath, "extracted", "test048C_D158436.djbz");
            using (var reader = new DjvuReader(File.OpenRead(djbzPath)))
            {
                dictA.Decode(reader);
            }

            // Fallback to test002C if the smallest file has no shapes
            if (dictA.ShapeCount == 0)
            {
                djbzPath = Path.Combine(Util.ArtifactsDataPath, "extracted", "test002C_D1868.djbz");
                using (var reader = new DjvuReader(File.OpenRead(djbzPath)))
                {
                    dictA.Decode(reader);
                }
            }

            // 2. Establish the child dictionary (dictB)
            var dictB = new JB2Dictionary();
            
            // Organically satisfies integrity checks and sets dictB.InheritedShapes = dictA.ShapeCount
            dictB.InheritedDictionary = dictA; 

            // 3. Add organic delta content to dictB
            JB2Shape baseShape = dictA.GetShape(0);
            var newShape = new JB2Shape { Parent = -1, Bitmap = baseShape.Bitmap };
            dictB.AddShape(newShape);

            // 4. Encode dictB (Organically hits: if (dict.InheritedShapes > 0) CodeRecordA(RequiredDictOrReset...))
            byte[] buffer;
            using (var ms = new MemoryStream())
            using (var encoder = new JB2Encoder())
            {
                encoder.Init(ms, null);
                encoder.Encode(dictB); 
                encoder.Flush();
                buffer = ms.ToArray();
            }

            // 5. Decode and Verify (Organically hits: if (!_GotStartRecordP) CodeInheritedShapeCount(jim))
            using (var ms = new MemoryStream(buffer))
            using (var reader = new DjvuReader(ms))
            using (var decoder = new JB2Decoder())
            {
                var decodedDictB = new JB2Dictionary();
                decoder.Init(reader, dictA); // Crucially supply dictA as context
                decoder.Code(decodedDictB);
                
                Assert.Equal(dictB.InheritedShapes, decodedDictB.InheritedShapes);
                Assert.Equal(dictB.ShapeCount, decodedDictB.ShapeCount);
            }
        }

        [Fact]
        public void Encode_NullJB2Image_ThrowsArgumentNullException()
        {
            using (var encoder = new JB2Encoder())
            {
                encoder.Init(null, null);
                var ex = Assert.Throws<DjvuArgumentNullException>(() => encoder.Encode((JB2Image)null));
                Assert.Contains("JB2 encoding failed", ex.Message);
            }
        }

        [Theory]
        [InlineData(JB2Constants.StartOfData)]
        [InlineData(JB2Constants.NewMark)]
        [InlineData(JB2Constants.NewMarkLibraryOnly)]
        [InlineData(JB2Constants.NewMarkImageOnly)]
        [InlineData(JB2Constants.MatchedRefine)]
        [InlineData(JB2Constants.MatchedRefineLibraryOnly)]
        [InlineData(JB2Constants.MatchedRefineImageOnly)]
        [InlineData(JB2Constants.MatchedCopy)]
        [InlineData(JB2Constants.NonMarkData)]
        [InlineData(JB2Constants.RequiredDictOrReset)]
        [InlineData(JB2Constants.PreservedForBzz)]
        [InlineData(JB2Constants.EndOfData)]
        public void JB2Encoder_RoundTrip_RecordType(int recordType)
        {
            byte[] buffer;

            // Phase 1: Encode
            using (var ms = new MemoryStream())
            {
                using (var encoder = new TestJB2Encoder())
                {
                    encoder.Init(ms, null);
                    encoder.EncodeRecordType(recordType);
                }
                buffer = ms.ToArray();
            }

            // Phase 2 & 3: Decode
            using (var ms = new MemoryStream(buffer))
            {
                using (var decoder = new TestJB2Decoder())
                {
                    // Use Stream Init directly! (since we just implemented it)
                    decoder.Init(ms, null);
                    int decodedType = decoder.DecodeRecordType();
                    Assert.Equal(recordType, decodedType);
                }
            }
        }

        [Theory]
        [InlineData(JB2Constants.NewMark)]
        [InlineData(JB2Constants.NewMarkLibraryOnly)]
        [InlineData(JB2Constants.NewMarkImageOnly)]
        [InlineData(JB2Constants.MatchedRefine)]
        [InlineData(JB2Constants.MatchedRefineLibraryOnly)]
        [InlineData(JB2Constants.MatchedRefineImageOnly)]
        [InlineData(JB2Constants.MatchedCopy)]
        [InlineData(JB2Constants.NonMarkData)]
        [InlineData(JB2Constants.RequiredDictOrReset)]
        [InlineData(JB2Constants.PreservedForBzz)]
        public void JB2Encoder_RoundTrip_MultipleRecords(int middleRecord)
        {
            byte[] buffer;
            int[] recordsToEncode = new[]
            {
                JB2Constants.StartOfData,
                middleRecord,
                JB2Constants.EndOfData
            };

            // Phase 1: Encode
            using (var ms = new MemoryStream())
            {
                using (var encoder = new TestJB2Encoder())
                {
                    encoder.Init(ms, null);
                    foreach (int record in recordsToEncode)
                    {
                        encoder.EncodeRecordType(record);
                    }
                }
                buffer = ms.ToArray();
            }

            // Phase 2 & 3: Decode
            using (var ms = new MemoryStream(buffer))
            {
                using (var decoder = new TestJB2Decoder())
                {
                    decoder.Init(ms, null);
                    foreach (int expectedRecord in recordsToEncode)
                    {
                        int decodedType = decoder.DecodeRecordType();
                        Assert.Equal(expectedRecord, decodedType);
                    }
                }
            }
        }

        [Theory]
        [InlineData(0, 1, 8, 11)]
        [InlineData(0, 2, 8, 11)]
        [InlineData(0, 3, 8, 11)]
        [InlineData(0, 4, 8, 11)]
        [InlineData(0, 5, 8, 11)]
        [InlineData(0, 6, 8, 11)]
        [InlineData(0, 7, 8, 11)]
        [InlineData(0, 6, 7, 11)]
        [InlineData(0, 1, 9, 10, 11)]
        [InlineData(0, 2, 2, 2, 11)] // Repeating
        [InlineData(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)] // All sequential
        [InlineData(0, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 11)] // Reverse sequential
        [InlineData(0, 1, 8, 2, 8, 3, 8, 11)] // Interspersed
        [InlineData(0, 9, 9, 9, 9, 11)] // Repeating dict
        [InlineData(0, 11)] // Immediate end
        public void JB2Encoder_RoundTrip_VariableLengthSequence(params int[] recordsToEncode)
        {
            byte[] buffer;

            // Phase 1: Encode
            using (var ms = new MemoryStream())
            {
                using (var encoder = new TestJB2Encoder())
                {
                    encoder.Init(ms, null);
                    foreach (int record in recordsToEncode)
                    {
                        encoder.EncodeRecordType(record);
                    }
                }
                buffer = ms.ToArray();
            }

            // Phase 2 & 3: Decode
            using (var ms = new MemoryStream(buffer))
            {
                using (var decoder = new TestJB2Decoder())
                {
                    decoder.Init(ms, null);
                    foreach (int expectedRecord in recordsToEncode)
                    {
                        int decodedType = decoder.DecodeRecordType();
                        Assert.Equal(expectedRecord, decodedType);
                    }
                }
            }
        }

        [Fact]
        public void JB2Encoder_RoundTrip_CodeAbsoluteLocation()
        {
            byte[] buffer;
            var blitIn = new JB2Blit { Left = 10, Bottom = 20 };

            using (var ms = new MemoryStream())
            {
                using (var encoder = new TestJB2Encoder())
                {
                    encoder.Init(ms, null);
                    encoder.SetImageSize(100, 100);
                    encoder.EncodeRecordType(JB2Constants.StartOfData);
                    encoder.SetGotStartRecord();
                    encoder.InvokeCodeAbsoluteLocation(blitIn, 5, 5);
                    encoder.Flush();
                }
                buffer = ms.ToArray();
            }

            using (var ms = new MemoryStream(buffer))
            using (var decoder = new TestJB2Decoder())
            {
                decoder.Init(ms, null);
                decoder.SetImageSize(100, 100);
                decoder.DecodeRecordType();
                decoder.SetGotStartRecord();
                var blitOut = new JB2Blit();
                decoder.DecodeAbsoluteLocation(blitOut, 5, 5);
                Assert.Equal(blitIn.Left, blitOut.Left);
                Assert.Equal(blitIn.Bottom, blitOut.Bottom);
            }
        }

        [Fact]
        public void JB2Encoder_RoundTrip_CodeAbsoluteMarkSize()
        {
            byte[] buffer;
            var bmIn = new Bitmap();
            bmIn.Init(10, 20, 0);

            using (var ms = new MemoryStream())
            {
                using (var encoder = new TestJB2Encoder())
                {
                    encoder.Init(ms, null);
                    encoder.InvokeCodeAbsoluteMarkSize(bmIn, 0);
                    encoder.Flush();
                }
                buffer = ms.ToArray();
            }

            using (var ms = new MemoryStream(buffer))
            using (var decoder = new TestJB2Decoder())
            {
                decoder.Init(ms, null);
                var bmOut = new Bitmap();
                decoder.DecodeAbsoluteMarkSize(bmOut, 0);
                Assert.Equal(bmIn.Width, bmOut.Width);
                Assert.Equal(bmIn.Height, bmOut.Height);
            }
        }

        [Fact]
        public void JB2Encoder_RoundTrip_CodeRelativeMarkSize()
        {
            byte[] buffer;
            var bmIn = new Bitmap();
            bmIn.Init(15, 25, 0);

            using (var ms = new MemoryStream())
            {
                using (var encoder = new TestJB2Encoder())
                {
                    encoder.Init(ms, null);
                    encoder.InvokeCodeRelativeMarkSize(bmIn, 10, 20, 0);
                    encoder.Flush();
                }
                buffer = ms.ToArray();
            }

            using (var ms = new MemoryStream(buffer))
            using (var decoder = new TestJB2Decoder())
            {
                decoder.Init(ms, null);
                var bmOut = new Bitmap();
                decoder.DecodeRelativeMarkSize(bmOut, 10, 20, 0);
                Assert.Equal(bmIn.Width, bmOut.Width);
                Assert.Equal(bmIn.Height, bmOut.Height);
            }
        }

        [Fact]
        public void JB2Encoder_RoundTrip_GetDiff()
        {
            byte[] buffer;
            int diffIn = 42;
            var ctx = new MutableValue<int>(0);

            using (var ms = new MemoryStream())
            {
                using (var encoder = new TestJB2Encoder())
                {
                    encoder.Init(ms, null);
                    encoder.InvokeGetDiff(diffIn, ctx);
                    encoder.Flush();
                }
                buffer = ms.ToArray();
            }

            using (var ms = new MemoryStream(buffer))
            using (var decoder = new TestJB2Decoder())
            {
                decoder.Init(ms, null);
                ctx.Value = 0; // Reset context
                int diffOut = decoder.DecodeGetDiff(ctx);
                Assert.Equal(diffIn, diffOut);
            }
        }

        [Fact]
        public void JB2Encoder_RoundTrip_CodeBitmapDirectly()
        {
            byte[] buffer;
            var bmIn = new Bitmap();
            bmIn.Init(10, 10, 0);
            for (int i = 0; i < 100; i++)
            {
                bmIn.SetByteAt(i, (sbyte)(i % 2));
            }

            using (var ms = new MemoryStream())
            {
                using (var encoder = new TestJB2Encoder())
                {
                    encoder.Init(ms, null);
                    encoder.InvokeCodeBitmapDirectly(bmIn);
                    encoder.Flush();
                }
                buffer = ms.ToArray();
            }

            using (var ms = new MemoryStream(buffer))
            using (var decoder = new TestJB2Decoder())
            {
                decoder.Init(ms, null);
                var bmOut = new Bitmap();
                bmOut.Init(10, 10, 0);
                decoder.DecodeBitmapDirectly(bmOut);

                bool match = true;
                for (int i = 0; i < 100; i++)
                {
                    if (bmIn.GetByteAt(i) != bmOut.GetByteAt(i))
                    {
                        match = false;
                        break;
                    }
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("=== START JB2Encoder_RoundTrip_CodeBitmapDirectly ===");
                if (!match) sb.AppendLine("Bitmaps differ.");
                sb.AppendLine("bmIn:");
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        sb.Append(bmIn.GetByteAt(y * 10 + x) == 1 ? "1 " : "0 ");
                    }
                    sb.AppendLine();
                }
                sb.AppendLine("bmOut:");
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        sb.Append(bmOut.GetByteAt(y * 10 + x) == 1 ? "1 " : "0 ");
                    }
                    sb.AppendLine();
                }
                sb.AppendLine("=== END JB2Encoder_RoundTrip_CodeBitmapDirectly ===");
                Console.WriteLine(sb.ToString());
                Assert.True(match, "Bitmaps differ. Check console output above for details.");
            }
        }

        [Fact]
        public void JB2Encoder_RoundTrip_CodeBitmapByCrossCoding()
        {
            byte[] buffer;
            var bmIn = new Bitmap();
            bmIn.Init(10, 10, 0);
            var cbmIn = new Bitmap();
            cbmIn.Init(10, 10, 0);

            for (int i = 0; i < 100; i++)
            {
                bmIn.SetByteAt(i, (sbyte)(i % 2));
                cbmIn.SetByteAt(i, (sbyte)((i + 1) % 2)); // Use a slightly different pattern for the context bitmap
            }

            using (var ms = new MemoryStream())
            {
                using (var encoder = new TestJB2Encoder())
                {
                    encoder.Init(ms, null);
                    encoder.InvokeCodeBitmapByCrossCoding(bmIn, cbmIn);
                    encoder.Flush();
                }
                buffer = ms.ToArray();
            }

            using (var ms = new MemoryStream(buffer))
            using (var decoder = new TestJB2Decoder())
            {
                decoder.Init(ms, null);
                var bmOut = new Bitmap();
                bmOut.Init(10, 10, 0);
                decoder.DecodeBitmapByCrossCoding(bmOut, cbmIn);

                bool match = true;
                for (int i = 0; i < 100; i++)
                {
                    if (bmIn.GetByteAt(i) != bmOut.GetByteAt(i))
                    {
                        match = false;
                        break;
                    }
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("=== START JB2Encoder_RoundTrip_CodeBitmapByCrossCoding ===");
                if (!match) sb.AppendLine("Bitmaps differ.");
                sb.AppendLine("bmIn (CrossCoding):      cbmIn (Context):");
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        sb.Append(bmIn.GetByteAt(y * 10 + x) == 1 ? "1 " : "0 ");
                    }
                    sb.Append("     ");
                    for (int x = 0; x < 10; x++)
                    {
                        sb.Append(cbmIn.GetByteAt(y * 10 + x) == 1 ? "1 " : "0 ");
                    }
                    sb.AppendLine();
                }
                sb.AppendLine("bmOut (CrossCoding):");
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        sb.Append(bmOut.GetByteAt(y * 10 + x) == 1 ? "1 " : "0 ");
                    }
                    sb.AppendLine();
                }
                sb.AppendLine("=== END JB2Encoder_RoundTrip_CodeBitmapByCrossCoding ===");
                Console.WriteLine(sb.ToString());
                Assert.True(match, "Bitmaps differ. Check console output above for details.");
            }
        }

        [Theory]
        [InlineData(@"extracted/test048C_D158436.djbz")]
        [InlineData(@"extracted/test048C_D693758.djbz")]
        [InlineData(@"extracted/test048C_D1260006.djbz")]
        [InlineData(@"extracted/test074C_D1772.djbz")]
        public void Roundtrip_RealDjbzData(string file)
        {
            var dict1 = new JB2Dictionary();
            file = Path.Combine(Util.ArtifactsDataPath, file);

            using (var fs = File.OpenRead(file))
            {
                using (var reader = new DjvuReader(fs)) 
                {
                    dict1.Decode(reader);
                }
            }
            
            string newFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(file) + ".encoded.tmp");
            using (var fs = File.Create(newFile))
            {
                using (var encoder = new JB2Encoder())
                {
                    encoder.Init(fs, null);
                    encoder.Encode(dict1);
                }
            }
            
            string hash1 = GetHash(file);
            string hash2 = GetHash(newFile);
            Console.WriteLine($"  Original Hash: {hash1}");
            Console.WriteLine($"  New Hash:      {hash2}");
            if (hash1 == hash2)
                Console.WriteLine("  Hashes match perfectly!");
            else
                Console.WriteLine("  Hashes differ! Checking logical equivalence...");
            
            var dict2 = new JB2Dictionary();
            using (var fs = File.OpenRead(newFile))
            {
                using (var reader = new DjvuReader(fs))
                {
                    dict2.Decode(reader);
                }
            }
            
            Console.WriteLine($"  Dict1 Shapes: {dict1.ShapeCount}, Inherited: {dict1.InheritedShapes}");
            Console.WriteLine($"  Dict2 Shapes: {dict2.ShapeCount}, Inherited: {dict2.InheritedShapes}");
            
            Assert.Equal(dict1.ShapeCount, dict2.ShapeCount);
            for (int i = 0; i < dict1.ShapeCount; i++)
            {
                var s1 = dict1.GetShape(i);
                var s2 = dict2.GetShape(i);
                Assert.Equal(s1.Parent, s2.Parent);
                if (s1.Bitmap != null && s2.Bitmap != null)
                {
                    Assert.Equal(s1.Bitmap.Width, s2.Bitmap.Width);
                    Assert.Equal(s1.Bitmap.Height, s2.Bitmap.Height);
                }
            }
            File.Delete(newFile);
        }
        
        public static IEnumerable<object[]> JB2ImageTestData => Util.GetJB2ImageTestData(
            skipDocs: new int[] { },
            skipChunks: new string[] { },
            TestCoverage.UniqueOnly
        );

        [Theory]
        [MemberData(nameof(JB2ImageTestData))]
        public void Encode_JB2Image_ProducesIdenticalOutput(string djbzFileName, string sjbzFileName)
        {
            string sjbzPath = Path.Combine(Util.ArtifactsDataPath, sjbzFileName);

            // 1. Decode the inherited dictionary
            JB2Dictionary dict = null;
            if (djbzFileName != null)
            {
                string djbzPath = Path.Combine(Util.ArtifactsDataPath, djbzFileName);
                dict = new JB2Dictionary();
                using (var fs = File.OpenRead(djbzPath))
                {
                    using (var reader = new DjvuReader(fs)) 
                    {
                        dict.Decode(reader);
                    }
                }
            }

            // 2. Decode the page image (SJBZ) using the inherited dictionary
            var image = new JB2Image();
            using (var fs = File.OpenRead(sjbzPath))
            {
                using (var reader = new DjvuReader(fs))
                {
                    image.Decode(reader, dict);
                }
            }

            // 3. Re-encode the dictionary (DJBZ)
            string newDjbzFile = null;
            string hashDjbzOriginal = null;
            string hashDjbzNew = null;

            if (dict != null)
            {
                string djbzPath = Path.Combine(Util.ArtifactsDataPath, djbzFileName);
                newDjbzFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(djbzPath) + ".encoded.tmp");
                using (var fs = File.Create(newDjbzFile))
                {
                    using (var encoder = new JB2Encoder())
                    {
                        encoder.Init(fs, null);
                        encoder.Encode(dict);
                    }
                }
                hashDjbzOriginal = GetHash(djbzPath);
                hashDjbzNew = GetHash(newDjbzFile);
            }

            // 4. Re-encode the image (SJBZ)
            string newSjbzFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(sjbzPath) + ".encoded.tmp");
            using (var fs = File.Create(newSjbzFile))
            {
                using (var encoder = new JB2Encoder())
                {
                    // Note: image encoding uses dict context implicitly through the inherited shapes
                    encoder.Init(fs, dict);
                    encoder.Encode(image);
                }
            }

            // 5. Compare the hashes of both files
            string hashSjbzOriginal = GetHash(sjbzPath);
            string hashSjbzNew = GetHash(newSjbzFile);

            if (dict != null)
            {
                Assert.Equal(hashDjbzOriginal, hashDjbzNew);
                File.Delete(newDjbzFile);
            }

            Assert.Equal(hashSjbzOriginal, hashSjbzNew);
            File.Delete(newSjbzFile);
        }

        private string GetHash(string path)
        {
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
            }
        }

        [Fact]
        public void Encode_NullDictionary_ThrowsArgumentNullException()
        {
            using (var encoder = new JB2Encoder())
            {
                encoder.Init(null, null);
                var ex = Assert.Throws<DjvuArgumentNullException>(() => encoder.Encode((JB2Dictionary)null));
                Assert.Contains("JB2 encoding failed", ex.Message);
            }
        }

        [Fact]
        public void Encode_EmptyDictionary_EncodesHeadersAndEndOfData()
        {
            var dict = new JB2Dictionary();
            using (var ms = new MemoryStream())
            {
                using (var encoder = new JB2Encoder())
                {
                    encoder.Init(ms, null);
                    encoder.Encode(dict);
                }
                
                Assert.True(ms.Length > 0, "Encoder should write StartOfData and EndOfData records even for an empty dictionary.");
            }
        }

        [Fact]
        public void Encode_DictionaryWithComment_EncodesPreservedComment()
        {
            var dict = new JB2Dictionary();
            dict.Comment = "Test Comment";
            
            using (var ms = new MemoryStream())
            {
                using (var encoder = new JB2Encoder())
                {
                    encoder.Init(ms, null);
                    encoder.Encode(dict);
                }
                
                ms.Position = 0;
                var decodedDict = new JB2Dictionary();
                using (var reader = new DjvuReader(ms))
                {
                    decodedDict.Decode(reader);
                }
                
                Assert.Equal("Test Comment", decodedDict.Comment);
            }
        }

        [Fact]
        public void Encode_EmptyJB2Image_Success()
        {
            var image = new JB2Image(); // No shapes, no blits
            using (var ms = new MemoryStream())
            {
                using (var encoder = new JB2Encoder())
                {
                    encoder.Init(ms, null);
                    encoder.Encode(image);
                }
                
                Assert.True(ms.Length > 0, "Encoder should write StartOfData and EndOfData even for empty image");
            }
        }

        [Fact]
        public void Encode_JB2Image_WithMissingDictionary_Throws()
        {
            var image = new JB2Image();
            image.InheritedShapes = 1; // Explicitly simulate inheritance BEFORE adding shape
            var shape = new JB2Shape { Parent = 0 }; 
            image.AddShape(shape);
            
            using (var ms = new MemoryStream())
            {
                using (var encoder = new JB2Encoder())
                {
                    // No dict provided, but image has shapes referencing inherited shapes!
                    encoder.Init(ms, null);
                    
                    var ex = Assert.Throws<DjvuFormatException>(() => encoder.Encode(image));
                    Assert.Contains("JB2Encoder encoding failed: Image requires an inherited dictionary but it was not provided.", ex.Message);
                }
            }
        }
        [Fact]
        public void Encode_JB2Image_Uninitialized_ThrowsNullReferenceException()
        {
            var image = new JB2Image { Width = 100, Height = 100 };
            
            using (var encoder = new JB2Encoder())
            {
                var ex = Assert.Throws<DjvuInvalidOperationException>(() => encoder.Encode(image));
                Assert.Contains("Encoder is not initialized", ex.Message);
            }
        }

        [Fact]
        public void Encode_JB2Image_AfterDispose_ThrowsException()
        {
            var image = new JB2Image { Width = 100, Height = 100 };
            var encoder = new JB2Encoder();
            
            using (var ms = new MemoryStream())
            {
                encoder.Init(ms, null);
            }
            
            encoder.Dispose();

            var ex = Assert.Throws<DjvuInvalidOperationException>(() => encoder.Encode(image));
            Assert.Contains("encoder has been disposed", ex.Message);
        }

        [Fact]
        public void Encode_JB2Image_WithNegativeInheritedShapes_ThrowsIndexOutOfRangeException()
        {
            var image = new JB2Image { Width = 100, Height = 100 };
            image.InheritedShapes = -1;

            using (var ms = new MemoryStream())
            {
                using (var encoder = new JB2Encoder())
                {
                    encoder.Init(ms, null);
                    var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => encoder.Encode(image));
                    Assert.Contains("InheritedShapes cannot be negative", ex.Message);
                }
            }
        }

        [Fact]
        public void Encode_NullJB2Dictionary_ThrowsArgumentNullException()
        {
            using (var ms = new MemoryStream())
            {
                using (var encoder = new JB2Encoder())
                {
                    encoder.Init(ms, null);
                    JB2Dictionary dict = null;
                    var ex = Assert.Throws<DjvuArgumentNullException>(() => encoder.Encode(dict));
                    Assert.Contains("JB2 encoding failed: A shape dictionary is required", ex.Message);
                }
            }
        }


        [Fact]
        public void Encode_JB2Image_WithOversizedBitmap_ThrowsArgumentOutOfRangeException()
        {
            var image = new JB2Image { Width = 100, Height = 100 };
            
            // Width = 262145 is larger than BigPositive (262142), triggering out of bounds in CodeAbsoluteMarkSize
            int shapeId = image.AddShape(new JB2Shape { Parent = -1, Bitmap = new Bitmap(1, 262145, 0) });
            image.AddBlit(new JB2Blit { ShapeNumber = shapeId, Left = 10, Bottom = 10 });

            using (var ms = new MemoryStream())
            {
                using (var encoder = new JB2Encoder())
                {
                    encoder.Init(ms, null);
                    var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => encoder.Encode(image));
                    Assert.Contains("falls outside permitted bounds", ex.Message);
                }
            }
        }

        [Fact]
        public void Encode_JB2Image_WithRecursiveUnblittedParents_EncodesCorrectly()
        {
            var image = new JB2Image { Width = 50, Height = 50 };
            
            // Shape A (Parent = -1, never blitted directly)
            int shapeA = image.AddShape(new JB2Shape { Parent = -1, Bitmap = new Bitmap(5, 5, 0) });
            
            // Shape B (Parent = Shape A, never blitted directly)
            int shapeB = image.AddShape(new JB2Shape { Parent = shapeA, Bitmap = new Bitmap(5, 5, 0) });
            
            // Shape C (Parent = Shape B, Blitted)
            int shapeC = image.AddShape(new JB2Shape { Parent = shapeB, Bitmap = new Bitmap(5, 5, 0) });
            
            image.AddBlit(new JB2Blit { ShapeNumber = shapeC, Left = 10, Bottom = 10 });

            using (var ms = new MemoryStream())
            {
                using (var encoder = new JB2Encoder())
                {
                    encoder.Init(ms, null);
                    encoder.Encode(image); // This triggers EncodeLibonlyShape recursively for A and B
                }
                
                ms.Position = 0;
                var decodedImage = new JB2Image();
                using (var reader = new DjvuReader(ms))
                {
                    decodedImage.Decode(reader, null);
                }
                
                // Assert all 3 shapes were preserved due to the hierarchical requirement
                Assert.Equal(3, decodedImage.ShapeCount);
                Assert.Equal(-1, decodedImage.GetShape(0).Parent);
                Assert.Equal(0, decodedImage.GetShape(1).Parent);
                Assert.Equal(1, decodedImage.GetShape(2).Parent);
                
                // Assert only 1 blit exists
                Assert.Single(decodedImage.Blits);
                Assert.Equal(10, decodedImage.Blits[0].Left);
            }
        }

        [Theory]
        [InlineData(@"extracted/test002C_D1868.djbz", @"extracted/test002C_P01.sjbz")]
        [InlineData(@"extracted/test011C_D384.djbz", @"extracted/test011C_P03.sjbz")]
        [InlineData(@"extracted/test023C_D392.djbz", @"extracted/test023C_P01.sjbz")]
        public void Encode_JB2Image_WithComment_EncodesPreservedComment(string djbzFileName, string sjbzFileName)
        {
            string djbzPath = Path.Combine(Util.ArtifactsDataPath, djbzFileName);
            string sjbzPath = Path.Combine(Util.ArtifactsDataPath, sjbzFileName);

            var dict = new JB2Dictionary();
            using (var fs = File.OpenRead(djbzPath))
            using (var reader = new DjvuReader(fs)) 
                dict.Decode(reader);

            var image = new JB2Image();
            using (var fs = File.OpenRead(sjbzPath))
            using (var reader = new DjvuReader(fs))
                image.Decode(reader, dict);

            // Force the PreservedComment path
            string customComment = "DjvuNet Test Comment for " + sjbzFileName;
            image.Comment = customComment;

            using (var ms = new MemoryStream())
            {
                using (var encoder = new JB2Encoder())
                {
                    encoder.Init(ms, dict);
                    encoder.Encode(image);
                }

                ms.Position = 0;
                var decodedImage = new JB2Image();
                using (var reader = new DjvuReader(ms))
                {
                    decodedImage.Decode(reader, dict);
                }

                Assert.Equal(customComment, decodedImage.Comment);
            }
        }

        [Theory]
        [InlineData(@"extracted/test002C_D1868.djbz", @"extracted/test002C_P01.sjbz")]
        [InlineData(@"extracted/test011C_D384.djbz", @"extracted/test011C_P03.sjbz")]
        public void Encode_JB2Image_WithInheritedDictionary_EncodesRequiredDictOrReset(string djbzFileName, string sjbzFileName)
        {
            string djbzPath = Path.Combine(Util.ArtifactsDataPath, djbzFileName);
            string sjbzPath = Path.Combine(Util.ArtifactsDataPath, sjbzFileName);

            var dict = new JB2Dictionary();
            using (var fs = File.OpenRead(djbzPath))
            using (var reader = new DjvuReader(fs)) 
                dict.Decode(reader);

            var image = new JB2Image();
            using (var fs = File.OpenRead(sjbzPath))
            using (var reader = new DjvuReader(fs))
                image.Decode(reader, dict);

            int originalInheritedShapes = image.InheritedShapes;
            Assert.True(originalInheritedShapes > 0, "Test relies on an image that actually has an inherited dictionary.");

            // Force a new blit reusing an existing shape to explicitly hit MatchedCopy
            int existingShapeId = image.Blits[0].ShapeNumber;
            image.AddBlit(new JB2Blit { ShapeNumber = existingShapeId, Left = 100, Bottom = 100 });
            int expectedBlitCount = image.Blits.Count;

            using (var ms = new MemoryStream())
            {
                using (var encoder = new JB2Encoder())
                {
                    encoder.Init(ms, dict);
                    encoder.Encode(image);
                }

                ms.Position = 0;
                var decodedImage = new JB2Image();
                using (var reader = new DjvuReader(ms))
                {
                    decodedImage.Decode(reader, dict);
                }

                Assert.Equal(originalInheritedShapes, decodedImage.InheritedShapes);
                Assert.Equal(expectedBlitCount, decodedImage.Blits.Count);
                // Verify the copied blit was successfully decoded
                Assert.Equal(100, decodedImage.Blits[expectedBlitCount - 1].Left);
            }
        }

        private class ResetNumcoderTrackingDecoder : JB2Decoder
        {
            public int ResetNumcoderCallCount { get; private set; }

            protected override void ResetNumCoder()
            {
                base.ResetNumCoder();
                ResetNumcoderCallCount++;
            }
        }

        [Theory]
        [InlineData(@"extracted/test075C_D1476.djbz", @"extracted/test075C_P01.sjbz")]
        [InlineData(@"extracted/test074C_D1772.djbz", @"extracted/test074C_P01.sjbz")]
        public void Encode_Image_Exceeds20000Blits_ForcesRequiredDictOrReset(string djbzFileName, string sjbzFileName)
        {
            string djbzPath = Path.Combine(Util.ArtifactsDataPath, djbzFileName);
            string sjbzPath = Path.Combine(Util.ArtifactsDataPath, sjbzFileName);

            var dict = new JB2Dictionary();
            using (var fs = File.OpenRead(djbzPath))
            using (var reader = new DjvuReader(fs)) 
                dict.Decode(reader);

            var image = new JB2Image();
            using (var fs = File.OpenRead(sjbzPath))
            using (var reader = new DjvuReader(fs))
                image.Decode(reader, dict);

            // We start with a large number of organic blits, now we expand by 25,000 blits.
            // By using randomized coordinates, the relative distance between blits varies wildly,
            // which forces the JB2 arithmetic coder's binary context tree (_BitCells) to branch 
            // uncontrollably, guaranteeing we hit the CellChunk (20,000) limit organically.
            int originalBlitCount = image.Blits.Count;
            int targetBlitCount = originalBlitCount + 25000;
            
            // Reuse an existing shape to minimize memory impact during massive expansion
            int existingShapeId = image.Blits[0].ShapeNumber;
            Random rnd = new Random(42);
            
            for (int i = originalBlitCount; i < targetBlitCount; i++)
            {
                image.AddBlit(new JB2Blit { ShapeNumber = existingShapeId, Left = rnd.Next(0, 10000), Bottom = rnd.Next(0, 10000) });
            }

            using (var ms = new MemoryStream())
            {
                using (var encoder = new JB2Encoder())
                {
                    encoder.Init(ms, dict);
                    encoder.Encode(image);
                }

                ms.Position = 0;
                var decodedImage = new JB2Image();
                using (var reader = new DjvuReader(ms))
                using (var decoder = new ResetNumcoderTrackingDecoder())
                {
                    decoder.Init(reader, dict);
                    decoder.Code(decodedImage);
                    
                    Assert.Equal(targetBlitCount, decodedImage.Blits.Count);
                    
                    // Verify that the encoded stream organically triggered RequiredDictOrReset 
                    // after StartOfData, causing the decoder to naturally reset the arithmetic coder.
                    Assert.True(decoder.ResetNumcoderCallCount > 0, "Expected ResetNumcoder to be called after StartOfData record.");
                }
            }
        }

        [Fact]
        public void Encode_ShapeWithParent_UsesMatchedRefineLibraryOnly()
        {
            var dict = new JB2Dictionary();
            
            // Parent shape
            var parentBmp = new Bitmap(10, 10);
            parentBmp.Fill(1);
            var parentShape = new JB2Shape { Parent = -1 };
            parentShape.Bitmap = parentBmp;
            int parentId = dict.AddShape(parentShape);
            
            // Child shape referencing the parent
            var childShape = new JB2Shape { Parent = parentId };
            childShape.Bitmap = parentBmp; // Same bitmap for simplicity in refinement
            dict.AddShape(childShape);

            using (var ms = new MemoryStream())
            {
                using (var encoder = new JB2Encoder())
                {
                    encoder.Init(ms, null);
                    encoder.Encode(dict);
                }
                
                ms.Position = 0;
                var decodedDict = new JB2Dictionary();
                using (var reader = new DjvuReader(ms))
                {
                    decodedDict.Decode(reader);
                }
                
                Assert.Equal(2, decodedDict.ShapeCount);
                Assert.Equal(-1, decodedDict.GetShape(0).Parent);
                Assert.Equal(0, decodedDict.GetShape(1).Parent); // Child correctly referenced parent
            }
        }

        [Theory]
        [InlineData(@"extracted/test075C_D1476.djbz")]
        [InlineData(@"extracted/test074C_D1772.djbz")]
        public void Encode_Dictionary_Exceeds20000Shapes_ForcesRequiredDictOrReset(string djbzFileName)
        {
            string djbzPath = Path.Combine(Util.ArtifactsDataPath, djbzFileName);

            var dict = new JB2Dictionary();
            using (var fs = File.OpenRead(djbzPath))
            using (var reader = new DjvuReader(fs)) 
                dict.Decode(reader);

            int originalShapeCount = dict.ShapeCount;
            int targetShapeCount = originalShapeCount + 40000;
            Random rnd = new Random(42);

            // By using tiny 4x4 bitmaps with completely random pixel patterns, we guarantee 
            // the spatial context models of the arithmetic coder will uncontrollably branch the 
            // _BitCells tree, safely forcing the CellChunk (20,000) limit via CodeRecordA 
            // without causing memory pressure or OutOfMemory exceptions.
            for (int i = originalShapeCount; i < targetShapeCount; i++)
            {
                var bmp = new Bitmap();
                bmp.Init(4, 4, 0);
                for (int y = 0; y < 4; y++)
                {
                    int rowOffset = bmp.RowOffset(y);
                    for (int x = 0; x < 4; x++)
                        bmp.SetByteAt(rowOffset + x, (sbyte)rnd.Next(0, 2));
                }
                // Guarantee the bounding box is exactly 4x4 by setting corners to 1
                bmp.SetByteAt(bmp.RowOffset(0) + 0, 1);
                bmp.SetByteAt(bmp.RowOffset(0) + 3, 1);
                bmp.SetByteAt(bmp.RowOffset(3) + 0, 1);
                bmp.SetByteAt(bmp.RowOffset(3) + 3, 1);

                // Use a previously generated 4x4 shape to ensure exact match sizes
                // This wildly varies CodeMatchIndex and branches _BitCells organically.
                int parentId = (i == originalShapeCount) ? -1 : rnd.Next(originalShapeCount, i);
                dict.AddShape(new JB2Shape { Parent = parentId, Bitmap = bmp });
            }

            using (var ms = new MemoryStream())
            {
                using (var encoder = new JB2Encoder())
                {
                    encoder.Init(ms, null);
                    encoder.Encode(dict);
                }

                ms.Position = 0;
                var decodedDict = new JB2Dictionary();
                using (var reader = new DjvuReader(ms))
                using (var decoder = new ResetNumcoderTrackingDecoder())
                {
                    decoder.Init(reader, null);
                    decoder.Code(decodedDict);
                    
                    Assert.Equal(targetShapeCount, decodedDict.ShapeCount);
                    
                    // Verify that the encoded stream organically triggered RequiredDictOrReset 
                    // after StartOfData via CodeRecordA, causing the natural coder reset.
                    Assert.True(decoder.ResetNumcoderCallCount > 0, "Expected ResetNumcoder to be called after StartOfData record.");
                }
            }
        }

        [Fact]
        public void Encode_ImageWithSelfReferencingShape_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            JB2Image image = new JB2Image();
            JB2Shape maliciousShape = new JB2Shape { Bitmap = new Bitmap(), Parent = -1 };
            
            image.AddShape(maliciousShape);
            
            // Bypass AddShape validation by creating the cycle AFTER addition
            maliciousShape.Parent = 0; 
            image.AddBlit(new JB2Blit { ShapeNumber = 0, Left = 0, Bottom = 0 });

            // Act
            using (MemoryStream ms = new MemoryStream())
            using (JB2Encoder encoder = new JB2Encoder())
            {
                encoder.Init(ms, null);
                var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => encoder.Encode(image));
                Assert.Contains("Maximum shape inheritance depth", ex.Message);
            }
        }

        [Fact]
        public void Encode_ImageWithCircularShapeReference_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            JB2Image image = new JB2Image();
            JB2Shape shape0 = new JB2Shape { Bitmap = new Bitmap(), Parent = -1 }; 
            JB2Shape shape1 = new JB2Shape { Bitmap = new Bitmap(), Parent = -1 }; 
            
            image.AddShape(shape0);
            image.AddShape(shape1);
            
            // Bypass AddShape validation by creating the circular cycle AFTER addition
            shape0.Parent = 1; 
            shape1.Parent = 0; 
            image.AddBlit(new JB2Blit { ShapeNumber = 0, Left = 0, Bottom = 0 });

            // Act
            using (MemoryStream ms = new MemoryStream())
            using (JB2Encoder encoder = new JB2Encoder())
            {
                encoder.Init(ms, null);
                var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => encoder.Encode(image));
                Assert.Contains("Maximum shape inheritance depth", ex.Message);
            }
        }

        [Fact]
        public void Encode_ImageWithDeepLinearParentChain_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            int extremeDepth = 60000; // Increased to 60k to guarantee overflow on x64
            JB2Image image = new JB2Image();
            
            for (int i = 0; i < extremeDepth; i++)
            {
                JB2Shape shape = new JB2Shape { Bitmap = new Bitmap(), Parent = (i == 0) ? -1 : (i - 1) };
                image.AddShape(shape);
            }

            image.AddBlit(new JB2Blit { ShapeNumber = extremeDepth - 1, Left = 0, Bottom = 0 });

            // Act
            using (MemoryStream ms = new MemoryStream())
            using (JB2Encoder encoder = new JB2Encoder())
            {
                encoder.Init(ms, null);
                var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => encoder.Encode(image));
                Assert.Contains("Maximum shape inheritance depth", ex.Message);
            }
        }

        [Fact]
        public void Encode_BlitWithInvalidShapeNumber_ThrowsArgumentOutOfRangeException()
        {
            JB2Image image = new JB2Image();
            image.AddShape(new JB2Shape { Bitmap = new Bitmap(), Parent = -1 });
            JB2Blit blit = new JB2Blit { ShapeNumber = 0, Left = 0, Bottom = 0 };
            image.AddBlit(blit); 
            
            // Bypass AddBlit validation by modifying the ShapeNumber AFTER addition
            blit.ShapeNumber = 100; 

            using (MemoryStream ms = new MemoryStream())
            using (JB2Encoder encoder = new JB2Encoder())
            {
                encoder.Init(ms, null);
                // Validates boundary checks for shape indexing
                var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => encoder.Encode(image));
                Assert.Contains("references an out-of-bounds shape number", ex.Message);
            }
        }

        [Fact]
        public void Encode_ShapeWithInvalidParent_ThrowsArgumentOutOfRangeException()
        {
            JB2Image image = new JB2Image();
            JB2Shape shape0 = new JB2Shape { Bitmap = new Bitmap(), Parent = -1 }; 
            image.AddShape(shape0);
            
            // Bypass AddShape validation by modifying the parent AFTER addition
            shape0.Parent = 100; 
            image.AddBlit(new JB2Blit { ShapeNumber = 0, Left = 0, Bottom = 0 });

            using (MemoryStream ms = new MemoryStream())
            using (JB2Encoder encoder = new JB2Encoder())
            {
                encoder.Init(ms, null);
                // Validates boundary checks for parent shape indexing
                var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => encoder.Encode(image));
                Assert.Contains("references an out-of-bounds parent shape", ex.Message);
            }
        }

        [Fact]
        public void Encode_DictionaryForwardParentReference_Throws()
        {
            JB2Dictionary dict = new JB2Dictionary();
            JB2Shape shape0 = new JB2Shape { Bitmap = new Bitmap(), Parent = -1 }; 
            JB2Shape shape1 = new JB2Shape { Bitmap = new Bitmap(), Parent = -1 }; 
            dict.AddShape(shape0);
            dict.AddShape(shape1);

            // Bypass AddShape validation by modifying the parent AFTER addition
            // Shape 0 now references Shape 1 (a forward reference)
            shape0.Parent = 1;

            using (MemoryStream ms = new MemoryStream())
            using (JB2Encoder encoder = new JB2Encoder())
            {
                encoder.Init(ms, null);
                var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => encoder.Encode(dict));
                Assert.Contains("forward-referenced", ex.Message);
            }
        }

        [Fact]
        public void Encode_LargeComment_Throws()
        {
            JB2Dictionary dict = new JB2Dictionary();
            
            // Create a comment larger than JB2Encoder.BigPositive (262,142 bytes)
            dict.Comment = new string('A', 262143);

            using (MemoryStream ms = new MemoryStream())
            using (JB2Encoder encoder = new JB2Encoder())
            {
                encoder.Init(ms, null);
                // Verify that CodeNum generically blocks the oversized comment
                var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => encoder.Encode(dict));
                Assert.Contains("falls outside permitted bounds", ex.Message);
            }
        }
    }
}
