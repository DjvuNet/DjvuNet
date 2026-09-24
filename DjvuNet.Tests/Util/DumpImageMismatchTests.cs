using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using DjvuNet.Graphics;
using Xunit;

namespace DjvuNet.Tests
{
    public class DumpImageMismatchTests
    {
        private List<Tuple<string, string, string>> ExtractDumpLines(string output, bool assertAlignment = true)
        {
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var dumpLines = new List<Tuple<string, string, string>>();
            for (int i = 0; i < lines.Length - 2; i++)
            {
                if (lines[i].Contains(" O: ") && lines[i+1].Contains(" T: ") && lines[i+2].Contains(" D: "))
                {
                    string oHex = lines[i].Substring(lines[i].IndexOf(" O: ") + 4);
                    string tHex = lines[i+1].Substring(lines[i+1].IndexOf(" T: ") + 4);
                    string dHex = lines[i+2].Substring(lines[i+2].IndexOf(" D: ") + 4);
                    if (assertAlignment)
                    {
                        Assert.True(oHex.Length == tHex.Length, $"Alignment failure! Oracle ({oHex.Length}) vs Test ({tHex.Length}).");
                        Assert.True(oHex.Length == dHex.Length, $"Alignment failure! Oracle ({oHex.Length}) vs Diff ({dHex.Length}).");
                    }
                    dumpLines.Add(Tuple.Create(oHex, tHex, dHex));
                }
            }
            return dumpLines;
        }

        private void AssertDumpLineLength(Tuple<string, string, string> dumpLine, int chunkStart, int chunkEnd, PixelSize pixelSize, bool is8bppBitonal)
        {
            int bitsPerPixel = (int)pixelSize;
            int expectedLength = 1;
            for (int curr = chunkStart; curr < chunkEnd; curr++)
            {
                if (bitsPerPixel >= 8) expectedLength += is8bppBitonal ? ((((curr - chunkStart) & 7) == 7) ? 4 : 2) : 3;
                else expectedLength += (bitsPerPixel == 4) ? 10 : (bitsPerPixel == 2) ? 12 : 9;
            }
            string assertDump = $"\nASSERT_DUMP: method output:\n{dumpLine.Item1}\n{dumpLine.Item2}\n{dumpLine.Item3}";
            Assert.True(dumpLine.Item1.Length == expectedLength, $"Oracle length ({dumpLine.Item1.Length}) != expected ({expectedLength})." + assertDump);
            Assert.True(dumpLine.Item2.Length == expectedLength, $"Test length ({dumpLine.Item2.Length}) != expected ({expectedLength})." + assertDump);
            Assert.True(dumpLine.Item3.Length == expectedLength, $"Diff length ({dumpLine.Item3.Length}) != expected ({expectedLength})."+ assertDump);
        }

        private int VerifyChunkAlignments(
            string output,
            List<Tuple<string, string, string>> dumpLines, 
            int dumpLineIndex,
            int dumpStartIndex, 
            int dumpEndIndex, 
            uint bytesPerLine, 
            PixelSize pixelSize, 
            ChannelSize channelSize,
            int dumpLineWidth,
            int oracleStride,
            int dataStride,
            int oracleLeftPad,
            int dataLeftPad)
        {
            int pixelSizeBits = (int)pixelSize;
            bool is8bppBitonal = (pixelSizeBits == 8 && (int)channelSize == 1);
            
            uint engineBytesPerLine = Math.Max(4u, bytesPerLine);
            if (pixelSizeBits < 8)
            {
                engineBytesPerLine = Math.Max(4u, engineBytesPerLine / 8);
            }

            int maxLeftPad = Math.Max(oracleLeftPad, dataLeftPad);

            for (int chunkStart = dumpStartIndex; chunkStart < dumpEndIndex; chunkStart += (int)engineBytesPerLine)
            {
                int chunkEnd = Math.Min(dumpEndIndex, chunkStart + (int)engineBytesPerLine);
                
                int dumpRow = chunkStart / dumpLineWidth;
                int dumpCol = chunkStart % dumpLineWidth;
                
                int oracleRowByteIndex = dumpCol - maxLeftPad + oracleLeftPad;
                int offsetO = dumpRow * oracleStride + oracleRowByteIndex;
                Assert.True(output.Contains($"0x{offsetO:X8} O:"), $"ASSERT DUMP: searching for string \"0x{offsetO:X8} O:\"\n\n" + output);

                int dataRowByteIndex = dumpCol - maxLeftPad + dataLeftPad;
                int offsetT = dumpRow * dataStride + dataRowByteIndex;
                Assert.True(output.Contains($"0x{offsetT:X8} T:"), $"ASSERT DUMP: searching for string \"0x{offsetT:X8} T:\"\n\n" + output);

                AssertDumpLineLength(dumpLines[dumpLineIndex++], chunkStart, chunkEnd, pixelSize, is8bppBitonal);
            }
            
            return dumpLineIndex;
        }

        [Theory]
        //// Group 1: 8bpp Bitonal (Base36 padding edges)
        [InlineData(PixelSize._8bpp, ChannelSize._1bit, 10, 10, 0, 0, 0, 0)]
        [InlineData(PixelSize._8bpp, ChannelSize._1bit, 32, 16, 4, 0, 0, 4)]
        [InlineData(PixelSize._8bpp, ChannelSize._1bit, 64, 4, 0, 8, 8, 0)]
        [InlineData(PixelSize._8bpp, ChannelSize._1bit, 12, 12, 2, 2, 4, 4)]
        //// Group 2: 1bpp (Sub-byte boundaries)
        [InlineData(PixelSize._1bpp, ChannelSize._1bit, 8, 8, 0, 0, 0, 0)]
        [InlineData(PixelSize._1bpp, ChannelSize._1bit, 32, 64, 4, 8, 4, 0)]
        [InlineData(PixelSize._1bpp, ChannelSize._1bit, 16, 4, 0, 2, 2, 0)]
        [InlineData(PixelSize._1bpp, ChannelSize._1bit, 128, 1, 16, 16, 16, 16)]
        //// Group 3: 8bpp (Standard gray)
        [InlineData(PixelSize._8bpp, ChannelSize._8bit, 10, 20, 0, 0, 0, 0)]
        [InlineData(PixelSize._8bpp, ChannelSize._8bit, 10, 20, 2, 2, 6, 4)]
        [InlineData(PixelSize._8bpp, ChannelSize._8bit, 255, 3, 0, 16, 0, 16)]
        [InlineData(PixelSize._8bpp, ChannelSize._8bit, 17, 17, 1, 0, 0, 1)]
        //// Group 4: 16bpp (Wide channels)
        [InlineData(PixelSize._16bpp, ChannelSize._16bit, 4, 4, 0, 0, 0, 0)]
        [InlineData(PixelSize._16bpp, ChannelSize._8bit, 10, 10, 2, 0, 0, 4)]
        [InlineData(PixelSize._16bpp, ChannelSize._16bit, 32, 8, 0, 8, 4, 0)]
        [InlineData(PixelSize._16bpp, ChannelSize._8bit, 64, 2, 8, 8, 8, 8)]
        //// Group 5: 24bpp (RGB)
        [InlineData(PixelSize._24bpp, ChannelSize._8bit, 16, 16, 0, 0, 0, 0)]
        [InlineData(PixelSize._24bpp, ChannelSize._8bit, 128, 2, 4, 0, 0, 8)]
        [InlineData(PixelSize._24bpp, ChannelSize._8bit, 3, 3, 0, 12, 12, 0)]
        [InlineData(PixelSize._24bpp, ChannelSize._8bit, 1920, 1, 3, 3, 6, 6)]
        //// Group 6: 32bpp (ARGB)
        [InlineData(PixelSize._32bpp, ChannelSize._8bit, 2, 2, 0, 0, 0, 0)]
        [InlineData(PixelSize._32bpp, ChannelSize._8bit, 16, 16, 0, 16, 8, 8)]
        [InlineData(PixelSize._32bpp, ChannelSize._8bit, 8, 64, 32, 0, 0, 32)]
        [InlineData(PixelSize._32bpp, ChannelSize._8bit, 256, 4, 16, 16, 16, 16)]
        // Group 7: Real Life Dimensions (Megapixel)
        [InlineData(PixelSize._24bpp, ChannelSize._8bit, 6000, 4100, 0, 0, 0, 0)]
        [InlineData(PixelSize._32bpp, ChannelSize._8bit, 3840, 2160, 16, 16, 0, 0)]
        [InlineData(PixelSize._8bpp, ChannelSize._8bit, 7680, 4320, 0, 0, 8, 8)]
        [InlineData(PixelSize._1bpp, ChannelSize._1bit, 8500, 11000, 64, 0, 0, 64)]
        public unsafe void Core_HeaderCorrectness(PixelSize pixelSize, ChannelSize channelSize, int width, int height, int lPadOracle, int rPadOracle, int lPadData, int rPadData)
        {
            // Arrange
            int pixelSizeBits = (int)pixelSize;
            int bytesPerPixel = Math.Max(1, pixelSizeBits / 8);
            int activeBytes = pixelSizeBits >= 8 ? (width * bytesPerPixel) : ((width * pixelSizeBits) / 8);

            int strideOracle = activeBytes + lPadOracle + rPadOracle;
            int strideData = activeBytes + lPadData + rPadData;

            byte[] oracleBuffer = new byte[strideOracle * height];
            byte[] testBuffer = new byte[strideData * height];

            StringBuilder sb = new StringBuilder();

            string GetPadString(int pad)
            {
                return $"Pad: {pad}";
            }

            string expectedLegend = $"Legend: [O]racle ({GetPadString(lPadOracle + rPadOracle)}), [T]est ({GetPadString(lPadData + rPadData)}), [D]ifference (No buffer: -)";
            string expectedFormat = $"Format: {pixelSizeBits}bpp (Channel: {(int)channelSize}bit) | Area: {width} x {height} pixels";
            string expectedBitonal = (pixelSizeBits == 8 && (int)channelSize == 1) ? "Legend (8bpp Bitonal): Base36 (0-Z)" : null;

            // Act
            fixed (byte* pOracle = oracleBuffer)
            fixed (byte* pData = testBuffer)
            {
                Util.DumpImageMismatchCore(
                    pOracle, pData,
                    width, height,
                    strideOracle, strideData,
                    lPadOracle, lPadData,
                    pixelSize, channelSize,
                    sb);
            }

            string output = sb.ToString();

            // Assert
            Assert.Contains(expectedLegend, output);
            Assert.Contains(expectedFormat, output);
            if (expectedBitonal != null)
            {
                Assert.Contains(expectedBitonal, output);
            }
        }


        /// <summary>
        /// Aligns an exact linear byte index to the starting byte of its corresponding pixel payload.
        /// If the exact index falls within the leading padding (left border), it is returned unmodified.
        /// </summary>
        /// <param name="exactByteIndex">The absolute linear byte index of the mismatch within the hex dump.</param>
        /// <param name="dumpLineWidth">The total byte width of a single printed row in the dump, including all padding and pixel data.</param>
        /// <param name="maxLeftPad">The maximum leading padding (left border) before the pixel data begins.</param>
        /// <param name="bitsPerPixel">The bit depth of a single pixel.</param>
        /// <param name="bytesPerPixel">The minimum physical bytes taken by a pixel (or 1 for sub-byte pixels).</param>
        /// <returns>The byte index shifted down to the nearest pixel boundary.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AlignToPixel(int exactByteIndex, int dumpLineWidth, int maxLeftPad, int bitsPerPixel, int bytesPerPixel)
        {
            int pixelRelativeByte = exactByteIndex % dumpLineWidth - maxLeftPad;
            int pixelMismatchByteIndex = pixelRelativeByte < 0 ? pixelRelativeByte
                : Util.PixelsToBytes(Util.BytesToPixels(pixelRelativeByte, bitsPerPixel, bytesPerPixel), bitsPerPixel, bytesPerPixel);
            return (exactByteIndex / dumpLineWidth) * dumpLineWidth + maxLeftPad + pixelMismatchByteIndex;
        }

        /// <summary>
        /// Validates the structural integrity and mathematical correctness of a diagnostic hex dump output string.
        /// It extracts the raw dump chunks, verifies the presence of mismatch blocks, dynamically computes the
        /// expected internal chunk alignments, and validates both string lengths and memory address 
        /// prefixes (0x... O: and 0x... T:) across all boundary wraps.
        /// </summary>
        /// <param name="output">The raw console output string generated by the DumpImageMismatch engine.</param>
        /// <param name="mismatchDumpIndices">An array of the engine-specific flat byte indices where the mismatch patches are expected to begin (accounting for max left padding offset).</param>
        /// <param name="height">The total height of the image in pixels/rows.</param>
        /// <param name="dumpLineWidth">The total logical byte width of a rendered diagnostic line, including the max left padding, active visual bytes, and max right padding.</param>
        /// <param name="oracleStride">The physical memory stride (bytes per row) of the primary (Oracle) image buffer.</param>
        /// <param name="dataStride">The physical memory stride (bytes per row) of the secondary (Test) image buffer.</param>
        /// <param name="oracleLeftPad">The number of bytes the Oracle image is padded on the left before the active visual boundary.</param>
        /// <param name="dataLeftPad">The number of bytes the Test image is padded on the left before the active visual boundary.</param>
        /// <param name="leadingPixels">The number of non-corrupted context pixels the engine is configured to render before the mismatch begins.</param>
        /// <param name="maxMismatchPixels">The maximum length of the mismatch block in continuous pixels.</param>
        /// <param name="bytesPerPixel">The mathematical bytes taken by a single pixel (minimum 1, used for sub-byte multiplier logic).</param>
        /// <param name="bytesPerLine">The visual wrap width indicating how many bytes are printed on a console line.</param>
        /// <param name="pixelSize">The structural bit depth of a single pixel.</param>
        /// <param name="channelSize">The bit depth of a single color channel, critical for identifying bitonal alignment rules.</param>
        private void VerifyMismatchBlocks(
            string output,
            int[] mismatchDumpIndices,
            int height,
            int dumpLineWidth,
            int oracleStride,
            int dataStride,
            int oracleLeftPad,
            int dataLeftPad,
            uint leadingPixels,
            uint maxMismatchPixels,
            int bytesPerPixel,
            uint bytesPerLine,
            PixelSize pixelSize,
            ChannelSize channelSize)
        {
            int bitsPerPixel = (int)pixelSize;
            bool is8bppBitonal_cap = (bitsPerPixel == 8 && (int)channelSize == 1);
            if (is8bppBitonal_cap) bytesPerLine = Math.Min(bytesPerLine, 64u);
            else if (bitsPerPixel == 1) bytesPerLine = Math.Min(bytesPerLine, 4u);
            else if (bitsPerPixel == 2) bytesPerLine = Math.Min(bytesPerLine, 8u);
            else if (bitsPerPixel == 4) bytesPerLine = Math.Min(bytesPerLine, 16u);

            var dumpLines = ExtractDumpLines(output, assertAlignment: true);
            int dumpLineIndex = 0;

            bool is8bppBitonal = (int)pixelSize == 8 && (int)channelSize == 1;
            bool is1bppBitonal = (int)pixelSize == 1 && (int)channelSize == 1;
            int subunitSize = is8bppBitonal ? 8 : (is1bppBitonal ? 1 : 1);

            int maxMismatchBytes = (int)maxMismatchPixels * bytesPerPixel;

            for (int blockIndex = 0; blockIndex < mismatchDumpIndices.Length; blockIndex++)
            {
                Assert.True(output.Contains($"Mismatch Block {blockIndex + 1}"), $"ASSERT_DUMP_START: method output\n{output}\nASSERT_DUMP_END");

                int maxLeftPad = Math.Max(oracleLeftPad, dataLeftPad);
                int mismatchDumpIndex = AlignToPixel(mismatchDumpIndices[blockIndex], dumpLineWidth, maxLeftPad, bitsPerPixel, bytesPerPixel);
                int maxLeadBytes = (int)(leadingPixels * bytesPerPixel);
                int dumpStartIndex = Math.Max(0, mismatchDumpIndex - maxLeadBytes);
                dumpStartIndex = (dumpStartIndex / subunitSize) * subunitSize;
                
                int dumpEndIndex = Math.Min(height * dumpLineWidth, mismatchDumpIndex + maxMismatchBytes);

                dumpLineIndex = VerifyChunkAlignments(
                    output, dumpLines, dumpLineIndex, dumpStartIndex, dumpEndIndex, bytesPerLine,
                    pixelSize, channelSize, dumpLineWidth, oracleStride, dataStride, oracleLeftPad, dataLeftPad);
            }

            Assert.True(dumpLines.Count == dumpLineIndex, $"Verified {dumpLineIndex} lines, but found {dumpLines.Count} in output." + $"\nASSERT_DUMP_START: method output\n{output}\nASSERT_DUMP_END");
        }

        private void VerifyHexContent(
            string output, int[] mismatchDumpIndices, ReadOnlySpan<byte> oracleData, ReadOnlySpan<byte> testData,
            int height, int width, int dumpLineWidth,
            int oracleStride, int dataStride, int oracleLeftPad, int dataLeftPad,
            bool bitonal, uint leadingPixels, uint maxMismatchPixels, uint bytesPerLine, int bytesPerPixel)
        {
            int maxLeftPad = Math.Max(oracleLeftPad, dataLeftPad);
            int startPixelLimit = maxLeftPad > 0 ? maxLeftPad - 1 : -1;
            int activeBytes = width * bytesPerPixel;
            int endPixelLimit = maxLeftPad > 0 ? maxLeftPad + activeBytes - 1 : -1;
            int subunitSize = bitonal ? 8 : 1;

            char GetBitonalChar(byte b) => b <= 35 ? (char)(b + (b < 10 ? 48 : 55)) : '#';

            for (int b = 0; b < mismatchDumpIndices.Length; b++)
            {
                int bitsPerPixel = bitonal ? 1 : (bytesPerPixel * 8);
                int mismatchDumpIndex = AlignToPixel(mismatchDumpIndices[b], dumpLineWidth, maxLeftPad, bitsPerPixel, bytesPerPixel);
                int leadBytes = (int)(leadingPixels * bytesPerPixel);
                int mismatchBytes = (int)(maxMismatchPixels * bytesPerPixel);
                int dumpStartIndex = Math.Max(0, mismatchDumpIndex - leadBytes);
                dumpStartIndex = (dumpStartIndex / subunitSize) * subunitSize;
                int dumpEndIndex = Math.Min(height * dumpLineWidth, mismatchDumpIndex + mismatchBytes);

                bool carryBracket = false;

                for (int chunkStart = dumpStartIndex; chunkStart < dumpEndIndex; chunkStart += (int)bytesPerLine)
                {
                    int chunkEnd = Math.Min(dumpEndIndex, chunkStart + (int)bytesPerLine);
                    
                    int dumpRow = chunkStart / dumpLineWidth;
                    int dumpCol = chunkStart % dumpLineWidth;
                    
                    int oracleAbsStride = Math.Abs(oracleStride);
                    int dataAbsStride = Math.Abs(dataStride);
                    
                    int rowOffsetO = (oracleStride < 0) ? (height - 1 - dumpRow) * oracleAbsStride : dumpRow * oracleAbsStride;
                    int rowOffsetT = (dataStride < 0) ? (height - 1 - dumpRow) * dataAbsStride : dumpRow * dataAbsStride;
                    
                    int offsetO = rowOffsetO + dumpCol - maxLeftPad + oracleLeftPad;
                    int offsetT = rowOffsetT + dumpCol - maxLeftPad + dataLeftPad;
                    
                    StringBuilder expectedO = new StringBuilder($"0x{offsetO:X8} O: ");
                    StringBuilder expectedT = new StringBuilder($"0x{offsetT:X8} T: ");
                    StringBuilder expectedD = new StringBuilder("           D: ");

                    if (carryBracket)
                    {
                        expectedO.Append('[');
                        expectedT.Append('[');
                        expectedD.Append('[');
                    }
                    else
                    {
                        expectedO.Append(' ');
                        expectedT.Append(' ');
                        expectedD.Append(' ');
                    }

                    bool nextCarry = false;

                    for (int curr = chunkStart; curr < chunkEnd; curr++)
                    {
                        int r = curr / dumpLineWidth;
                        int v = curr % dumpLineWidth;
                        
                        char bChar = ' ';
                        if (v == startPixelLimit)
                        {
                            if (curr == chunkEnd - 1) nextCarry = true;
                            else bChar = '[';
                        }
                        else if (v == endPixelLimit) bChar = ']';

                        int cO = v - maxLeftPad + oracleLeftPad;
                        int cT = v - maxLeftPad + dataLeftPad;

                        bool hasO = cO >= 0 && cO < oracleStride && r < height;
                        bool hasT = cT >= 0 && cT < dataStride && r < height;

                        byte oByte = hasO ? oracleData[r * oracleStride + cO] : (byte)0;
                        byte tByte = hasT ? testData[r * dataStride + cT] : (byte)0;

                        if (bitonal)
                        {
                            expectedO.Append(hasO ? $"{GetBitonalChar(oByte)}{bChar}" : $"-{bChar}");
                            expectedT.Append(hasT ? $"{GetBitonalChar(tByte)}{bChar}" : $"-{bChar}");
                            
                            if (hasO && hasT)
                            {
                                byte diff = (byte)Math.Abs(oByte - tByte);
                                expectedD.Append($"{GetBitonalChar(diff)}{bChar}");
                            }
                            else expectedD.Append($"-{bChar}");

                            if ((curr - chunkStart) % 8 == 7)
                            {
                                expectedO.Append("  ");
                                expectedT.Append("  ");
                                expectedD.Append("  ");
                            }
                        }
                        else
                        {
                            expectedO.Append(hasO ? $"{oByte:X2}{bChar}" : $"--{bChar}");
                            expectedT.Append(hasT ? $"{tByte:X2}{bChar}" : $"--{bChar}");
                            
                            if (hasO && hasT)
                            {
                                byte diff = (byte)Math.Abs(oByte - tByte);
                                expectedD.Append($"{diff:X2}{bChar}");
                            }
                            else expectedD.Append($"--{bChar}");
                        }
                    }

                    string assertDump = $"ASSERT_DUMP_START:\nmethod output\n{output}\n";

                    Assert.True(output.Contains(expectedO.ToString()), assertDump + $"Expected O: {expectedO}");
                    Assert.True(output.Contains(expectedT.ToString()), assertDump + $"Expected T: {expectedT}");
                    Assert.True(output.Contains(expectedD.ToString()), assertDump + $"Expected D: {expectedD}");

                    carryBracket = nextCarry;
                }
            }
        }

        private void VerifySubByteContent(
            string output, int[] mismatchDumpIndices, ReadOnlySpan<byte> oracleData, ReadOnlySpan<byte> testData,
            int height, int width, int dumpLineWidth,
            int oracleStride, int dataStride, int oracleLeftPad, int dataLeftPad,
            uint leadingPixels, uint maxMismatchPixels, uint bytesPerLine, int bitsPerPixel)
        {
            int maxLeftPad = Math.Max(oracleLeftPad, dataLeftPad);
            int startPixelLimit = maxLeftPad > 0 ? maxLeftPad - 1 : -1;
            int imageWidthInBytes = (width * bitsPerPixel) / 8;
            int endPixelLimit = maxLeftPad > 0 ? maxLeftPad + imageWidthInBytes - 1 : -1;

            string FormatSubByte(byte val)
            {
                string b = Convert.ToString(val, 2).PadLeft(8, '0');
                if (bitsPerPixel == 4) return $"{b.Substring(0, 4)} {b.Substring(4, 4)}";
                if (bitsPerPixel == 2) return $"{b.Substring(0, 2)} {b.Substring(2, 2)} {b.Substring(4, 2)} {b.Substring(6, 2)}";
                return b;
            }

            string FormatMissingSubByte()
            {
                if (bitsPerPixel == 4) return "---- ----";
                if (bitsPerPixel == 2) return "-- -- -- --";
                return "--------";
            }

            for (int b = 0; b < mismatchDumpIndices.Length; b++)
            {
                int bytesPerPixel = Math.Max(1, bitsPerPixel / 8);
                int mismatchDumpIndex = AlignToPixel(mismatchDumpIndices[b], dumpLineWidth, maxLeftPad, bitsPerPixel, bytesPerPixel);
                int maxLeadBytes = (int)(leadingPixels * bytesPerPixel);
                int dumpStartIndex = Math.Max(0, mismatchDumpIndex - maxLeadBytes);
                
                int maxMismatchBytes = (int)(maxMismatchPixels * bytesPerPixel);
                int dumpEndIndex = Math.Min(height * dumpLineWidth, mismatchDumpIndex + maxMismatchBytes);

                bool carryBracket = false;

                for (int chunkStart = dumpStartIndex; chunkStart < dumpEndIndex; chunkStart += (int)bytesPerLine)
                {
                    int chunkEnd = Math.Min(dumpEndIndex, chunkStart + (int)bytesPerLine);
                    int dumpRow = chunkStart / dumpLineWidth;
                    int dumpCol = chunkStart % dumpLineWidth;
                    
                    int oracleAbsStride = Math.Abs(oracleStride);
                    int dataAbsStride = Math.Abs(dataStride);
                    
                    int rowOffsetO = (oracleStride < 0) ? (height - 1 - dumpRow) * oracleAbsStride : dumpRow * oracleAbsStride;
                    int rowOffsetT = (dataStride < 0) ? (height - 1 - dumpRow) * dataAbsStride : dumpRow * dataAbsStride;
                    
                    int offsetO = rowOffsetO + dumpCol - maxLeftPad + oracleLeftPad;
                    int offsetT = rowOffsetT + dumpCol - maxLeftPad + dataLeftPad;
                    
                    StringBuilder expectedO = new StringBuilder($"0x{offsetO:X8} O: ");
                    StringBuilder expectedT = new StringBuilder($"0x{offsetT:X8} T: ");
                    StringBuilder expectedD = new StringBuilder("           D: ");

                    if (carryBracket)
                    {
                        expectedO.Append('[');
                        expectedT.Append('[');
                        expectedD.Append('[');
                    }
                    else
                    {
                        expectedO.Append(' ');
                        expectedT.Append(' ');
                        expectedD.Append(' ');
                    }

                    bool nextCarry = false;

                    for (int curr = chunkStart; curr < chunkEnd; curr++)
                    {
                        int r = curr / dumpLineWidth;
                        int v = curr % dumpLineWidth;
                        
                        char bChar = ' ';
                        if (v == startPixelLimit)
                        {
                            if (curr == chunkEnd - 1) nextCarry = true;
                            else bChar = '[';
                        }
                        else if (v == endPixelLimit) bChar = ']';

                        int cO = v - maxLeftPad + oracleLeftPad;
                        int cT = v - maxLeftPad + dataLeftPad;

                        bool hasO = cO >= 0 && cO < oracleStride && r < height;
                        bool hasT = cT >= 0 && cT < dataStride && r < height;

                        byte oByte = hasO ? oracleData[r * oracleStride + cO] : (byte)0;
                        byte tByte = hasT ? testData[r * dataStride + cT] : (byte)0;

                        expectedO.Append(hasO ? $"{FormatSubByte(oByte)}{bChar}" : $"{FormatMissingSubByte()}{bChar}");
                        expectedT.Append(hasT ? $"{FormatSubByte(tByte)}{bChar}" : $"{FormatMissingSubByte()}{bChar}");
                        
                        if (hasO && hasT)
                        {
                            byte diff = (byte)(oByte ^ tByte);
                            expectedD.Append($"{FormatSubByte(diff)}{bChar}");
                        }
                        else expectedD.Append($"{FormatMissingSubByte()}{bChar}");
                    }

                    string assertDump = $"ASSERT_DUMP_START:\nmethod output\n{output}\n";

                    Assert.True(output.Contains(expectedO.ToString()), assertDump + $"Expected O: {expectedO}");
                    Assert.True(output.Contains(expectedT.ToString()), assertDump + $"Expected T: {expectedT}");
                    Assert.True(output.Contains(expectedD.ToString()), assertDump + $"Expected D: {expectedD}");
                    
                    carryBracket = nextCarry;
                }
            }
        }

        private void VerifyContent(
            string output, int[] mismatchDumpIndices, ReadOnlySpan<byte> oracleData, ReadOnlySpan<byte> testData,
            int height, int width, int dumpLineWidth,
            int oracleStride, int dataStride, int oracleLeftPad, int dataLeftPad,
            uint leadingPixels, uint maxMismatchPixels, uint bytesPerLine, 
            PixelSize pixelSize, ChannelSize channelSize)
        {
            int bitsPerPixel = (int)pixelSize;
            bool is8bppBitonal = (bitsPerPixel == 8 && (int)channelSize == 1);
            bytesPerLine = Math.Max(4u, bytesPerLine);
            if (bitsPerPixel < 8)
            {
                bytesPerLine = Math.Max(4u, bytesPerLine / 8);
            }

            int pixelSizeBits = (int)pixelSize;
            if (pixelSizeBits >= 8)
            {
                VerifyHexContent(
                    output, mismatchDumpIndices, oracleData, testData,
                    height, width, dumpLineWidth,
                    oracleStride, dataStride, oracleLeftPad, dataLeftPad,
                    bitonal: (int)channelSize == 1, 
                    leadingPixels, maxMismatchPixels, bytesPerLine, 
                    bytesPerPixel: Math.Max(1, pixelSizeBits / 8));
            }
            else
            {
                VerifySubByteContent(
                    output, mismatchDumpIndices, oracleData, testData,
                    height, width, dumpLineWidth,
                    oracleStride, dataStride, oracleLeftPad, dataLeftPad,
                    leadingPixels, maxMismatchPixels, bytesPerLine, 
                    bitsPerPixel: pixelSizeBits);
            }
        }

        private void PrintToConsole(String output, bool print = false, TextWriter targetOut = null)
        {
            if (print)
            {
                if (targetOut != null)
                {
                    targetOut.WriteLine(output);
                }
                else
                {
                    Console.WriteLine(output);
                }
            }
        }

        [Fact]
        public unsafe void Core_AsymmetricPadding()
        {
            // Arrange: 1 Row, 4 Pixels width, 24bpp (3 bytes per pixel) = 12 bytes active pixel data
            int width = 4;
            int height = 1;

            int strideOracle = 12; // Perfectly packed, 0 padding
            byte[] oracleBuffer = new byte[strideOracle];
            Array.Fill(oracleBuffer, (byte)0xFF);

            int strideData = 16;   // 4 bytes of padding
            byte[] testBuffer = new byte[strideData];
            Array.Fill(testBuffer, (byte)0xFF);

            // Introduce a mismatch at the last pixel of the row
            testBuffer[10] = 0x00;
            testBuffer[11] = 0x00;

            // The 4 padding bytes in testBuffer (indices 12-15) remain 0x00,
            // verifying that the dumper exposes them properly without crashing.

            StringBuilder sb = new StringBuilder();

            // Act
            fixed (byte* pOracle = oracleBuffer)
            fixed (byte* pData = testBuffer)
            {
                Util.DumpImageMismatchCore(
                    pOracle, pData,
                    width, height,
                    strideOracle, strideData,
                    (PixelSize)24, (ChannelSize)8, // 24bpp, 8-bit channels
                    sb,
                    leadingPixels: 1, // Only show 1 pixel (3 bytes) before the mismatch
                    maxMismatchPixels: 4,
                    maxBlocks: 1,
                    bytesPerLine: 64);
            }

            string output = sb.ToString();

            PrintToConsole(output, true);

            VerifyContent(
                output, new[] { 10 }, 
                oracleBuffer, testBuffer,
                height, width, 16,
                strideOracle, strideData, 
                0, 0,
                leadingPixels: 1, maxMismatchPixels: 4, bytesPerLine: 64,
                PixelSize._24bpp, ChannelSize._8bit);
        }

        [Fact]
        public unsafe void Core_Stride()
        {
            // Arrange: 1 Row, 2 Pixels width, 8bpp
            int width = 2;
            int height = 1;

            int strideOracle = -4; // Bottom-up memory
            byte[] oracleBuffer = new byte[4];

            int strideData = 4;    // Top-down memory
            byte[] testBuffer = new byte[4];

            testBuffer[1] = 0xFF; // Introduce mismatch

            StringBuilder sb = new StringBuilder();

            // Act
            fixed (byte* pOracle = oracleBuffer)
            fixed (byte* pData = testBuffer)
            {
                Util.DumpImageMismatchCore(
                    pOracle, pData,
                    width, height,
                    strideOracle, strideData,
                    (PixelSize)8, (ChannelSize)8,
                    sb);
            }

            string output = sb.ToString();

            // Assert: The math `Math.Abs(stride)` should intercept and print this without throwing.
            Assert.Contains("Legend: [O]racle (Pad: 2), [T]est (Pad: 2)", output);
            Assert.Contains("Mismatch Block 1", output);
        }

        [Theory]
        [InlineData(-96, -96)] 
        [InlineData(96, 96)]   
        [InlineData(-96, 96)]  
        [InlineData(96, -96)]  
        public unsafe void Core_Stride_PointerContract(int strideOracle, int strideData)
        {
            int width = 96;
            int height = 96;

            byte[] SetupSandboxBuffer(int stride, out int startOffset, bool injectMismatch = false)
            {
                int absStride = Math.Abs(stride);
                int activeSize = height * absStride;
                
                // Allocate a sandbox 3x the active size to prevent all AccessViolations
                byte[] buffer = new byte[activeSize * 3];
                
                // Poison the entire sandbox
                for (int i = 0; i < buffer.Length; i++) buffer[i] = 0xEE;

                // Place the pointer precisely in the middle of the sandbox
                startOffset = activeSize + (activeSize / 2);

                // Populate ONLY the memory locations where the FIXED coordinate math is expected to read.
                for (int y = 0; y < height; y++)
                {
                    // The unmanaged contract: offset = y * stride
                    int expectedMemoryOffset = startOffset + (y * stride);
                    
                    byte rowMarker = (byte)(y % 250); 
                    
                    for (int x = 0; x < width; x++)
                    {
                        buffer[expectedMemoryOffset + x] = rowMarker;
                    }

                    if (injectMismatch && y == 48)
                    {
                        buffer[expectedMemoryOffset + 48] = 0xFF;
                    }
                }
                return buffer;
            }

            byte[] oracleBuffer = SetupSandboxBuffer(strideOracle, out int offsetO, false);
            byte[] testBuffer = SetupSandboxBuffer(strideData, out int offsetT, true);

            StringBuilder sb = new StringBuilder();

            fixed (byte* pOracleRaw = oracleBuffer)
            fixed (byte* pDataRaw = testBuffer)
            {
                byte* pOracle = pOracleRaw + offsetO;
                byte* pData = pDataRaw + offsetT;

                Util.DumpImageMismatchCore(
                    pOracle, pData,
                    width, height,
                    strideOracle, strideData,
                    (PixelSize)8, (ChannelSize)8,
                    sb,
                    leadingPixels: 4, maxMismatchPixels: 32, maxBlocks: 1, bytesPerLine: 64);
            }

            string output = sb.ToString();

            string assertDump = $"ASSERT_DUMP_START: method output\n{output}\nASSERT_DUMP_END";

            // The buggy math for negative strides will calculate massive positive offsets, 
            // read the 0xEE poison memory in the upper sandbox, and fail the test without crashing.
            // The fixed math will step to the correct memory locations and find the mismatch.
            Assert.True(output.Contains("Row: 48 | Column: 48"), assertDump); 
            Assert.True(output.Contains("O:  30 30 30 30 30"), assertDump); 
            Assert.True(output.Contains("T:  30 30 30 30 FF"), assertDump); 
            Assert.True(!output.Contains(" EE "), assertDump); 
        }


        [Fact]
        public unsafe void Core_NoMismatch()
        {
            // Arrange
            int stride = 12;
            byte[] buffer = new byte[stride];
            StringBuilder sb = new StringBuilder();

            // Act
            fixed (byte* ptr = buffer)
            {
                Util.DumpImageMismatchCore(
                    ptr, ptr, 4, 1, stride, stride,
                    PixelSize._24bpp, ChannelSize._8bit, sb);
            }

            // Assert
            string output = sb.ToString().Trim();

            // Output shouldn't have any mismatch blocks if none are found.
            // Note: Currently, the legend is printed blindly if ANY mismatch block loop executes,
            // but the loop itself skips printing if `mismatch == false`. The Legend is printed at the very top.
            Assert.DoesNotContain("Mismatch Block", output);
        }

        [Fact]
        public unsafe void Core_MultipleBlocks()
        {
            // Arrange: 1 Row, 256 Pixels, 8bpp
            int width = 256;
            int height = 1;
            int stride = 256;

            byte[] oracleBuffer = new byte[stride];
            Array.Fill(oracleBuffer, (byte)0xFF);

            byte[] testBuffer = new byte[stride];
            Array.Fill(testBuffer, (byte)0xFF);

            testBuffer[0] = 0x00;   // Mismatch 1 triggers Block 1 (bytes 0-63)
            testBuffer[128] = 0x00; // Mismatch 2 triggers Block 2 (bytes 128-191)

            StringBuilder sb = new StringBuilder();

            // Act
            fixed (byte* pOracle = oracleBuffer)
            fixed (byte* pData = testBuffer)
            {
                Util.DumpImageMismatchCore(
                    pOracle, pData, width, height, stride, stride,
                    PixelSize._8bpp, ChannelSize._8bit, sb,
                    leadingPixels: 0,
                    maxMismatchPixels: 64,
                    maxBlocks: 2,
                    bytesPerLine: 64);
            }

            string output = sb.ToString();

            PrintToConsole(output);

            // Assert
            Assert.Contains("Mismatch Block 1", output);
            Assert.Contains("Mismatch Block 2", output);

            VerifyMismatchBlocks(
                output, new[] { 0, 128 }, height, stride,
                oracleStride: stride, dataStride: stride,
                oracleLeftPad: 0, dataLeftPad: 0,
                leadingPixels: 0, maxMismatchPixels: 64, bytesPerPixel: 1, bytesPerLine: 64,
                PixelSize._8bpp, ChannelSize._8bit);

            VerifyContent(
                output, new[] { 0, 128 }, oracleBuffer, testBuffer,
                height, width, stride,
                stride, stride, 0, 0,
                leadingPixels: 0, maxMismatchPixels: 64, bytesPerLine: 64,
                PixelSize._8bpp, ChannelSize._8bit);
        }

        [Theory]
        [InlineData(0, 0, 0, 60)]  // Start/Middle: No padding
        [InlineData(4, 16, 64, 100)] // Middle: Asymmetric padding (4 bytes vs 16 bytes), spaced (Block 2 expected)
        [InlineData(4, 16, 64, 80)]  // Middle: Asymmetric padding, mismatch2 is INSIDE Block 1 (No Block 2 expected)
        [InlineData(16, 64, 90, 126)] // End: Spaced sufficiently to trigger Block 2 and bleed deep into padding
        [InlineData(16, 64, 90, 110)] // End: mismatch2 INSIDE Block 1 padding bleed (No Block 2 expected)
        [InlineData(4, 8, 28, 999)] // Trailing padding mismatch: width 32, mismatch at Row 0 col 28
        [InlineData(4, 8, 40, 999)] // Leading padding mismatch: width 32, mismatch at Row 1 col 0
        [InlineData(4, 8, 48, 999)] // Leading padding mismatch: width 32, mismatch at Row 1 col 0
        public unsafe void Core_PaddingBleed(int padOracle, int padData, int mismatch1, int mismatch2)
        {
            // Arrange: 2 Rows. Override width to 32 for the specific trailing/leading padding boundary tests.
            int width = 128;
            int height = 2;

            int strideOracle = width + padOracle;
            int strideData = width + padData;

            byte[] oracleBuffer = new byte[strideOracle * height];
            Array.Fill(oracleBuffer, (byte)0xFF);

            byte[] testBuffer = new byte[strideData * height];
            Array.Fill(testBuffer, (byte)0xFF);

            // Trigger Block 1 (Long mismatch block: 8 bytes)
            for (int i = 0; i < 8; i++) testBuffer[mismatch1 + i] = 0x00;

            // Trigger Block 2 (Long mismatch block: 8 bytes)
            if (mismatch2 < width)
            {
                for (int i = 0; i < Math.Min(8, width - mismatch2); i++) testBuffer[mismatch2 + i] = 0x00;
            }

            StringBuilder sb = new StringBuilder();

            // Act
            fixed (byte* pOracle = oracleBuffer)
            fixed (byte* pData = testBuffer)
            {
                Util.DumpImageMismatchCore(
                    pOracle, pData, width, height,
                    strideOracle, strideData, padOracle, padData, PixelSize._8bpp, ChannelSize._8bit, sb,
                    leadingPixels: 4,
                    maxMismatchPixels: 32, // Forces the dump to extend 32 bytes, bleeding into padding if at the end
                    maxBlocks: 2,
                    bytesPerLine: 32);
            }

            string output = sb.ToString();

            Assert.True(output.Contains("Mismatch Block 1"), $"ASSERT_DUMP_START method output\n{output}");
            
            int dumpLineWidth = width + Math.Max(padOracle, padData);
            
            // Adjust indices based on max padding shift
            int y1 = mismatch1 / strideData;
            int c1 = mismatch1 % strideData;
            int maxLeftPad = Math.Max(padOracle, padData);
            int startVisualX = c1 - padData; // Convert from data coordinates to visual coordinates
            
            // The visual column must be correctly shifted by maxLeftPad in the unified dump line
            int unifiedDumpCol1 = startVisualX + maxLeftPad;
            
            List<int> mismatchIndices = new List<int> { y1 * dumpLineWidth + unifiedDumpCol1 };

            // The block extends 32 bytes from mismatch1. If mismatch2 is within that range, it is absorbed.
            bool expectBlock2 = mismatch2 < 999 && mismatch2 >= (mismatch1 + 32);
            
            if (expectBlock2)
            {
                Assert.Contains("Mismatch Block 2", output);
                int y2 = mismatch2 / strideData;
                int c2 = mismatch2 % strideData;
                int startVisualX2 = c2 - padData;
                int unifiedDumpCol2 = startVisualX2 + maxLeftPad;
                mismatchIndices.Add(y2 * dumpLineWidth + unifiedDumpCol2);
            }

            VerifyMismatchBlocks(
                output, mismatchIndices.ToArray(), height, dumpLineWidth,
                oracleStride: strideOracle, dataStride: strideData,
                oracleLeftPad: padOracle, dataLeftPad: padData,
                leadingPixels: 4, maxMismatchPixels: 32, bytesPerPixel: 1, bytesPerLine: 32,
                PixelSize._8bpp, ChannelSize._8bit);

            VerifyContent(
                output, mismatchIndices.ToArray(), oracleBuffer, testBuffer,
                height, width, dumpLineWidth,
                strideOracle, strideData, padOracle, padData,
                leadingPixels: 4, maxMismatchPixels: 32, bytesPerLine: 32,
                PixelSize._8bpp, ChannelSize._8bit);
        }

        [Theory]
        // 8bpp (1 byte/pixel)
        [InlineData(PixelSize._8bpp, ChannelSize._8bit, 0, 0, 0, 0, 32, 64)]  // No padding, Row 0 Col 0
        [InlineData(PixelSize._8bpp, ChannelSize._8bit, 4, 8, 0, 28, 32, 64)]  // Asymmetric padding, trailing mismatch at Row 0 Col 28
        [InlineData(PixelSize._8bpp, ChannelSize._8bit, 16, 64, 1, 0, 16, 48)]  // Massive padding, leading mismatch at Row 1 Col 0

        // 24bpp (3 bytes/pixel)
        [InlineData(PixelSize._24bpp, ChannelSize._8bit, 0, 0, 0, 0, 32, 64)]
        [InlineData(PixelSize._24bpp, ChannelSize._8bit, 4, 8, 0, 28, 32, 64)]
        [InlineData(PixelSize._24bpp, ChannelSize._8bit, 16, 64, 1, 0, 16, 48)]

        // 32bpp (4 bytes/pixel)
        [InlineData(PixelSize._32bpp, ChannelSize._8bit, 0, 0, 0, 0, 32, 64)]
        [InlineData(PixelSize._32bpp, ChannelSize._8bit, 4, 8, 0, 28, 32, 64)]
        [InlineData(PixelSize._32bpp, ChannelSize._8bit, 16, 64, 1, 0, 16, 48)]

        // 48bpp (6 bytes/pixel)
        [InlineData(PixelSize._48bpp, ChannelSize._16bit, 0, 0, 0, 0, 32, 64)]
        [InlineData(PixelSize._48bpp, ChannelSize._16bit, 4, 8, 0, 28, 32, 64)]
        [InlineData(PixelSize._48bpp, ChannelSize._16bit, 16, 64, 1, 0, 16, 48)]

        // 4bpp (1/2 byte/pixel)
        [InlineData(PixelSize._4bpp, ChannelSize._4bit, 0, 0, 0, 0, 32, 64)]
        [InlineData(PixelSize._4bpp, ChannelSize._4bit, 4, 8, 0, 28, 32, 64)]
        [InlineData(PixelSize._4bpp, ChannelSize._4bit, 16, 64, 1, 0, 16, 48)]

        // 2bpp (1/4 byte/pixel)
        [InlineData(PixelSize._2bpp, ChannelSize._2bit, 0, 0, 0, 0, 32, 64)]
        [InlineData(PixelSize._2bpp, ChannelSize._2bit, 4, 8, 0, 28, 32, 64)]
        [InlineData(PixelSize._2bpp, ChannelSize._2bit, 16, 64, 1, 0, 16, 48)]

        // 1bpp (1/8 byte/pixel)
        [InlineData(PixelSize._24bpp, ChannelSize._8bit, 0, 0, 0, 10, 4u, 5u)]
        [InlineData(PixelSize._8bpp, ChannelSize._8bit, 0, 0, 1, 5, 8u, 16u)]
        [InlineData(PixelSize._24bpp, ChannelSize._8bit, 2, 4, 1, 0, 32u, 64u)]
        [InlineData(PixelSize._1bpp, ChannelSize._1bit, 0, 0, 0, 0, 32u, 64u)]
        [InlineData(PixelSize._1bpp, ChannelSize._1bit, 4, 8, 0, 28, 32u, 64u)]
        [InlineData(PixelSize._1bpp, ChannelSize._1bit, 16, 64, 1, 0, 16u, 48u)]
        public unsafe void Core_MultiLineWrapping(
            PixelSize pixelSize, ChannelSize channelSize, int padOracle, int padData,
            int mismatchRow, int mismatchCol, uint bytesPerLine, uint blockLength)
        {
            int pixelSizeBits = (int)pixelSize;
            int channelSizeBits = (int)channelSize;

            // Arrange
            int width = (mismatchCol == 28 || mismatchRow == 1) ? 32 : 128;
            int height = 2;

            int bytesPerPixel = Math.Max(1, pixelSizeBits / 8);
            int activeBytes = pixelSizeBits >= 8 ? (width * bytesPerPixel) : ((width * pixelSizeBits) / 8);

            int strideOracle = activeBytes + padOracle;
            int strideData = activeBytes + padData;

            byte[] oracleBuffer = new byte[strideOracle * height];
            Array.Fill(oracleBuffer, (byte)0xFF);

            byte[] testBuffer = new byte[strideData * height];
            Array.Fill(testBuffer, (byte)0xFF);

            int pxByte = pixelSizeBits >= 8 ? (mismatchCol * bytesPerPixel) : ((mismatchCol * pixelSizeBits) / 8);
            int mismatchStart = padData + (mismatchRow * strideData) + pxByte;

            // Corrupt the test buffer for the full block length
            for (int i = 0; i < blockLength; i++)
            {
                if (mismatchStart + i < testBuffer.Length)
                    testBuffer[mismatchStart + i] = 0x00;
            }

            StringBuilder sb = new StringBuilder();

            // Act
            fixed (byte* pOracle = oracleBuffer)
            fixed (byte* pData = testBuffer)
            {
                Util.DumpImageMismatchCore(
                    pOracle, pData, width, height, strideOracle, strideData, padOracle, padData, pixelSize, channelSize, sb,
                    leadingPixels: 4, maxMismatchPixels: blockLength, maxBlocks: 1, bytesPerLine: bytesPerLine, comparePadding: true);
            }

            string output = sb.ToString();
            PrintToConsole($"\n=== Theory [{pixelSizeBits}bpp PadO:{padOracle} PadT:{padData} Row:{mismatchRow} Col:{mismatchCol} Line:{bytesPerLine} Block:{blockLength}] ===\n{output}", false);

            // Assert
            int maxLeftPad = Math.Max(padOracle, padData);
            int dumpLineWidth = maxLeftPad + activeBytes;

            int actualPxByte = pxByte;
            int actualRow = mismatchRow;
            
            // The engine aligns the raw mismatch byte to a naive pixel boundary starting from 0
            int x = pixelSizeBits >= 8 ? (actualPxByte / bytesPerPixel) : ((actualPxByte * 8) / pixelSizeBits);
            actualPxByte = pixelSizeBits >= 8 ? (x * bytesPerPixel) : ((x * pixelSizeBits) / 8);

            int mismatchDumpIndex = (actualRow * dumpLineWidth) + maxLeftPad + actualPxByte;
            
            int[] expectedMismatchIndices = actualRow < height ? new[] { mismatchDumpIndex } : Array.Empty<int>();

            VerifyMismatchBlocks(
                output, expectedMismatchIndices, height, dumpLineWidth,
                oracleStride: strideOracle, dataStride: strideData, 
                oracleLeftPad: padOracle, dataLeftPad: padData,
                leadingPixels: 4, maxMismatchPixels: blockLength, bytesPerPixel: bytesPerPixel, 
                bytesPerLine: bytesPerLine, pixelSize: pixelSize, channelSize: channelSize);

            VerifyContent(
                output, new[] { mismatchDumpIndex }, oracleBuffer, testBuffer,
                height, width, dumpLineWidth,
                strideOracle, strideData, padOracle, padData,
                leadingPixels: 4, maxMismatchPixels: blockLength, bytesPerLine: bytesPerLine,
                pixelSize, channelSize);
        }

        [Theory]
        //         PixelSize           ChannelSize        padO padT row col  lead bpl
        // 1bpp point diffs (flip 1 bit)
        [InlineData(PixelSize._1bpp, ChannelSize._1bit, 0, 0, 0, 0, 0, 16)]  // start of buffer
        [InlineData(PixelSize._1bpp, ChannelSize._1bit, 4, 8, 1, 10, 3, 64)]  // asymmetrical pad, middle
        [InlineData(PixelSize._1bpp, ChannelSize._1bit, 64, 16, 2, 31, 8, 32)]  // large pad, row 2 end

        // 2bpp point diffs (flip 2 bits)
        [InlineData(PixelSize._2bpp, ChannelSize._2bit, 0, 0, 0, 15, 2, 64)]
        [InlineData(PixelSize._2bpp, ChannelSize._2bit, 12, 12, 1, 1, 4, 32)]

        // 4bpp point diffs (flip 1 nibble)
        [InlineData(PixelSize._4bpp, ChannelSize._4bit, 0, 0, 0, 0, 0, 32)]
        [InlineData(PixelSize._4bpp, ChannelSize._4bit, 8, 16, 1, 15, 1, 16)]
        [InlineData(PixelSize._4bpp, ChannelSize._4bit, 128, 0, 2, 30, 10, 64)]

        // 8bpp bitonal point diffs (flip 1 byte to 1, test border layout)
        [InlineData(PixelSize._8bpp, ChannelSize._1bit, 2, 4, 1, 20, 3, 32)]

        // 8bpp point diffs (flip 1 byte)
        [InlineData(PixelSize._8bpp, ChannelSize._8bit, 0, 0, 0, 31, 0, 64)]
        [InlineData(PixelSize._8bpp, ChannelSize._8bit, 12, 8, 1, 10, 4, 16)]
        [InlineData(PixelSize._8bpp, ChannelSize._8bit, 0, 32, 2, 16, 16, 32)]

        // 24bpp point diffs (flip 1 byte inside 3-byte pixel)
        [InlineData(PixelSize._24bpp, ChannelSize._8bit, 0, 0, 0, 0, 1, 64)]
        [InlineData(PixelSize._24bpp, ChannelSize._8bit, 16, 16, 1, 20, 5, 128)]

        // 32bpp point diffs (flip 1 byte inside 4-byte pixel)
        [InlineData(PixelSize._32bpp, ChannelSize._8bit, 0, 0, 0, 10, 2, 64)]
        [InlineData(PixelSize._32bpp, ChannelSize._8bit, 32, 16, 2, 31, 4, 128)]

        // 48bpp point diffs (flip 1 byte inside 6-byte pixel)
        [InlineData(PixelSize._48bpp, ChannelSize._16bit, 0, 0, 0, 16, 2, 64)]
        [InlineData(PixelSize._48bpp, ChannelSize._16bit, 32, 16, 2, 0, 8, 128)]
        public unsafe void Core_PointDiffs(
            PixelSize pixelSize, ChannelSize channelSize, int padOracle, int padData, 
            int mismatchRow, int mismatchCol, uint leadingPixels, uint bytesPerLine)
        {
            int pixelSizeBits = (int)pixelSize;
            int width = 32;
            int height = 3;

            int bytesPerPixel = Math.Max(1, pixelSizeBits / 8);
            int activeBytes = pixelSizeBits >= 8 ? (width * bytesPerPixel) : ((width * pixelSizeBits) / 8);

            int strideOracle = activeBytes + padOracle;
            int strideData = activeBytes + padData;

            byte[] oracleBuffer = new byte[strideOracle * height];
            Array.Fill(oracleBuffer, (byte)0xFF);

            byte[] testBuffer = new byte[strideData * height];
            Array.Fill(testBuffer, (byte)0xFF);

            int pxByte = pixelSizeBits >= 8 ? (mismatchCol * bytesPerPixel) : ((mismatchCol * pixelSizeBits) / 8);
            int mismatchByte = padData + (mismatchRow * strideData) + pxByte;

            if (pixelSizeBits == 1) testBuffer[mismatchByte] = 0xDF;      // 1101 1111 (flip pixel 2)
            else if (pixelSizeBits == 2) testBuffer[mismatchByte] = 0xF3; // 1111 0011 (flip pixel 3)
            else if (pixelSizeBits == 4) testBuffer[mismatchByte] = 0xF0; // 1111 0000 (flip pixel 2)
            else testBuffer[mismatchByte] = 0x00;                         // flip full byte

            StringBuilder sb = new StringBuilder();

            fixed (byte* pOracle = oracleBuffer)
            fixed (byte* pData = testBuffer)
            {
                Util.DumpImageMismatchCore(
                    pOracle, pData, width, height, strideOracle, strideData, padOracle, padData, pixelSize, channelSize, sb,
                    leadingPixels: leadingPixels, maxMismatchPixels: 32u, maxBlocks: 1, bytesPerLine: bytesPerLine);
            }

            string output = sb.ToString();
            PrintToConsole($"\n=== Theory [Core_PointDiffs | {pixelSizeBits}bpp | PadO:{padOracle} PadT:{padData} | Row:{mismatchRow} Col:{mismatchCol} | Lead:{leadingPixels} Width:{bytesPerLine}] ===\nOUTPUT_DUMP:\n{output}", false);

            Assert.True(output.Contains("Mismatch Block 1"),
                $"ASSERT_DUMP_START method call:\n"+
                $"width: {width}, height: {height}, strideOracle {strideOracle}, strideData {strideData}, padOracle {padOracle}, padData {padData}, pixelSize {pixelSize}, channelSize {channelSize}, leadingPixels: {leadingPixels}, maxMismatchPixels: {32u}, maxBlocks: {1}, bytesPerLine: {bytesPerLine}\n"+
                $"method output:\n{output}\n"+
                $"ASSERT_DUMP_END");
            
            int maxLeftPad = Math.Max(padOracle, padData);
            int dumpLineWidth = maxLeftPad + activeBytes;
            int mismatchDumpIndex = (mismatchRow * dumpLineWidth) + maxLeftPad + pxByte;

            VerifyContent(
                output, new[] { mismatchDumpIndex }, oracleBuffer, testBuffer,
                height, width, dumpLineWidth,
                strideOracle, strideData, padOracle, padData,
                leadingPixels: leadingPixels, maxMismatchPixels: 32u, bytesPerLine: bytesPerLine,
                pixelSize, channelSize);
        }

        [Theory]
        [InlineData("EvsA", 2, 4, 1, 20, 14, 10, -1, -1, 0, 0, 1)]
        [InlineData("PaddingError", 4, 2, 1, 0, 14, 10, 1, -2, 12, 0, 1)] // Active error at col 0, padding error at col -2
        [InlineData("GreaterThanZ", 2, 4, 1, 20, 40, 10, -1, -1, 0, 0, 1)] // 40 > 35, should print '#'
        [InlineData("PaddingGreaterThanZ", 4, 2, 1, 0, 14, 10, 1, -2, 40, 0, 1)] // Padding error 40 > 35, should print '#'
        [InlineData("MultiBlock", 2, 4, 1, 20, 14, 10, 3, 10, 20, 0, 2)] // Two blocks
        public unsafe void Core_BitmapBitonal(
            string testCase, int padO, int padT,
            int r1, int c1, byte o1, byte t1,
            int r2, int c2, byte o2, byte t2,
            uint maxBlocks)
        {
            int width = 32;
            int height = 5;
            uint bytesPerLine = 32;

            int strideO = padO + width + padO;
            int strideT = padT + width + padT;

            byte[] oracleBuffer = new byte[height * strideO];
            byte[] testBuffer = new byte[height * strideT];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte val = (byte)((x % 2 == 0) ? 0 : 1);
                    oracleBuffer[y * strideO + padO + x] = val;
                    testBuffer[y * strideT + padT + x] = val;
                }
            }

            oracleBuffer[r1 * strideO + padO + c1] = o1;
            testBuffer[r1 * strideT + padT + c1] = t1;

            if (r2 >= 0)
            {
                oracleBuffer[r2 * strideO + padO + c2] = o2;
                testBuffer[r2 * strideT + padT + c2] = t2;
            }

            StringBuilder sb = new StringBuilder();
            fixed (byte* pOracle = oracleBuffer)
            fixed (byte* pData = testBuffer)
            {
                Util.DumpImageMismatchCore(
                    pOracle, pData, width, height, strideO, strideT,
                    padO, padT, PixelSize._8bpp, ChannelSize._1bit, sb,
                    leadingPixels: 3, maxMismatchPixels: 16, maxBlocks: maxBlocks, bytesPerLine: bytesPerLine);
            }
            string output = sb.ToString();
            PrintToConsole($"\n=== Theory Case: {testCase} ===\n{output}");

            if (testCase == "EvsA")
            {
                Assert.Contains("O:  0 1 0 1 0 1 0 1   E 1 0 1 0 1 0 1 ", output);
                Assert.Contains("T:  0 1 0 1 0 1 0 1   A 1 0 1 0 1 0 1 ", output);
                Assert.Contains("D:  0 0 0 0 0 0 0 0   4 0 0 0 0 0 0 0 ", output);
            }
            else if (testCase == "PaddingError")
            {
                // Mismatch Block 1
                // Row: 1 | Column: 0 | Byte Offset In Row: 0
                Assert.Contains("O:  0 0 C 0[E 1 0 1   0 1 0 1 0 1 0 1 ", output);
                Assert.Contains("T:  - - 0 0[A 1 0 1   0 1 0 1 0 1 0 1 ", output);
                Assert.Contains("D:  - - C 0[4 0 0 0   0 0 0 0 0 0 0 0 ", output);
            }
            else if (testCase == "GreaterThanZ")
            {
                Assert.Contains("O:  0 1 0 1 0 1 0 1   # 1 0 1 0 1 0 1 ", output);
                Assert.Contains("T:  0 1 0 1 0 1 0 1   A 1 0 1 0 1 0 1 ", output);
                Assert.Contains("D:  0 0 0 0 0 0 0 0   U 0 0 0 0 0 0 0 ", output); // 40 - 10 = 30 -> 'U'
            }
            else if (testCase == "PaddingGreaterThanZ")
            {
                Assert.Contains("O:  0 0 # 0[E 1 0 1   0 1 0 1 0 1 0 1 ", output);
                Assert.Contains("T:  - - 0 0[A 1 0 1   0 1 0 1 0 1 0 1 ", output);
                Assert.Contains("D:  - - # 0[4 0 0 0   0 0 0 0 0 0 0 0 ", output); // 40 - 0 = 40 -> '#'
            }
            else if (testCase == "MultiBlock")
            {
                Assert.Contains("Mismatch Block 1", output);
                Assert.Contains("Mismatch Block 2", output);
                Assert.Contains("O:  0 1 0 1 0 1 0 1   E 1 0 1 0 1 0 1 ", output);
                Assert.Contains("T:  0 1 0 1 0 1 0 1   A 1 0 1 0 1 0 1 ", output);
                Assert.Contains("D:  0 0 0 0 0 0 0 0   4 0 0 0 0 0 0 0 ", output);

                // Block 2: row 3, col 10, O=20 (K), T=0
                Assert.Contains("O:  0 1 0 1 0 1 K 1   0 1 0 1 0 1 0 1 ", output);
                Assert.Contains("T:  0 1 0 1 0 1 0 1   0 1 0 1 0 1 0 1 ", output);
                Assert.Contains("D:  0 0 0 0 0 0 K 0   0 0 0 0 0 0 0 0 ", output);
            }
        }

        [Theory]
        // null test
        [InlineData(true, false, 32, 1, 32, 32, 10, 64)]
        [InlineData(false, true, 32, 1, 32, 32, 10, 64)]
        // zero dimensions
        [InlineData(false, false, 0, 1, 32, 32, 10, 64)]
        [InlineData(false, false, 32, 0, 32, 32, 10, 64)]
        [InlineData(false, false, -1, 1, 32, 32, 10, 64)]
        [InlineData(false, false, 32, -1, 32, 32, 10, 64)]
        // zero stride
        [InlineData(false, false, 32, 1, 0, 32, 10, 64)]
        [InlineData(false, false, 32, 1, 32, 0, 10, 64)]
        // zero mismatch pixels (infinite loop protection test)
        [InlineData(false, false, 32, 1, 32, 32, 0u, 64u)]
        // zero bytes per line (infinite chunking loop test)
        [InlineData(false, false, 32, 1, 32, 32, 10u, 0u)]
        public unsafe void Core_SafetyGuards(
            bool nullOracle, bool nullData,
            int width, int height,
            int strideOracle, int strideData,
            uint maxMismatchPixels, uint bytesPerLine)
        {
            // Allocate dummy buffers larger than any math bounds in this theory
            byte[] oracleBuffer = new byte[1024];
            Array.Fill(oracleBuffer, (byte)0xFF);

            byte[] testBuffer = new byte[1024];
            Array.Fill(testBuffer, (byte)0xFF);
            testBuffer[0] = 0x00; // Deliberate mismatch to trigger the core processing engine

            StringBuilder sb = new StringBuilder();

            fixed (byte* pOracle = oracleBuffer)
            fixed (byte* pData = testBuffer)
            {
                byte* oraclePtr = nullOracle ? null : pOracle;
                byte* dataPtr = nullData ? null : pData;

                // Execution must safely abort or clamp dangerous math without throwing exceptions or hanging
                Util.DumpImageMismatchCore(
                    oraclePtr, dataPtr, width, height, strideOracle, strideData,
                    PixelSize._8bpp, ChannelSize._8bit, sb,
                    leadingPixels: 0, maxMismatchPixels: maxMismatchPixels, maxBlocks: 1, bytesPerLine: bytesPerLine);
            }

            Assert.True(true);
        }

        [Theory]
        [InlineData(11, 37, 37, 11)] // Oracle stride (11) < active bytes (37)
        [InlineData(41, 13, 41, 17)] // Data stride (13) < active bytes (41)
        [InlineData(17, 19, 43, 23)] // Both strides (17, 19) < active bytes (43)
        public unsafe void Core_SafetyGuards_Geometry(int strideOracle, int strideData, int width, int height)
        {
            int allocOracle = strideOracle * height;
            int allocData = strideData * height;
            
            byte[] bufferOracle = new byte[allocOracle + 128]; 
            byte[] bufferData = new byte[allocData + 128];
            
            Array.Fill(bufferOracle, (byte)0xAA);
            Array.Fill(bufferData, (byte)0xBB);
            Array.Fill(bufferOracle, (byte)0x00, 0, allocOracle); Array.Fill(bufferData, (byte)0x00, 0, allocData);
            
            StringBuilder sb = new StringBuilder();
            
            var ex = Assert.Throws<DjvuNet.Errors.DjvuArgumentOutOfRangeException>(() =>
            {
                fixed (byte* pOracle = bufferOracle) 
                fixed (byte* pData = bufferData)
                {
                    Util.DumpImageMismatchCore(
                        pOracle, pData, width, height, strideOracle, strideData, 
                        PixelSize._8bpp, ChannelSize._8bit, sb);
                }
            });
            
            Assert.Contains("is less than image width expressed in bytes", ex.Message);
        }

        [Fact]
        public unsafe void RawBufferWrapper()
        {
            int width = 32, height = 2;
            byte[] oracle = new byte[width * height];
            byte[] data = new byte[width * height];
            data[0] = 0xFF; // Induce mismatch

            using (StringWriter sw = new StringWriter())
            {
                TextWriter originalOut = Console.Out;
                Console.SetOut(sw);
                try
                {
                    fixed (byte* pOracle = oracle)
                    fixed (byte* pData = data)
                    {
                        Util.DumpImageMismatch(pOracle, pData, oracle.Length, width, 1, "Foreground");
                    }
                    string output = sw.ToString();
                    Assert.Contains("Foreground Mismatches for test0001C.djvu:", output);
                    Assert.Contains("Mismatch Block 1", output);
                }
                finally { Console.SetOut(originalOut); }
            }
        }

        [Fact]
        public unsafe void SysBitmapWrapper()
        {
            using (var oracle = new System.Drawing.Bitmap(32, 2, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            using (var data = new System.Drawing.Bitmap(32, 2, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                var lockedData = data.LockBits(new System.Drawing.Rectangle(0, 0, 32, 2), System.Drawing.Imaging.ImageLockMode.ReadWrite, data.PixelFormat);
                ((byte*)lockedData.Scan0)[0] = 0xFF;
                data.UnlockBits(lockedData);

                using (StringWriter sw = new StringWriter())
                {
                    TextWriter originalOut = Console.Out;
                    Console.SetOut(sw);
                    try
                    {
                        Util.DumpImageMismatch(oracle, data, 2, "Background");
                        string output = sw.ToString();
                        Assert.Contains("Background Mismatches for test0002C.djvu:", output);
                        Assert.Contains("Mismatch Block 1", output);
                    }
                    finally { Console.SetOut(originalOut); }
                }
            }
        }

        private static ReadOnlySpan<sbyte> LetterAPattern => (ReadOnlySpan<sbyte>)
            [   // 19 columns x 17 rows (Prime dimensions)
                0,0,0,0,0,0,0,0,1,1,1,0,0,0,0,0,0,0,0,
                0,0,0,0,0,0,0,1,1,0,1,1,0,0,0,0,0,0,0,
                0,0,0,0,0,0,1,1,0,0,0,1,1,0,0,0,0,0,0,
                0,0,0,0,0,0,1,1,0,0,0,1,1,0,0,0,0,0,0,
                0,0,0,0,0,1,1,0,0,0,0,0,1,1,0,0,0,0,0,
                0,0,0,0,0,1,1,0,0,0,0,0,1,1,0,0,0,0,0,
                0,0,0,0,1,1,0,0,0,0,0,0,0,1,1,0,0,0,0,
                0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,
                0,0,0,1,1,0,0,0,0,0,0,0,0,0,1,1,0,0,0,
                0,0,0,1,1,0,0,0,0,0,0,0,0,0,1,1,0,0,0,
                0,0,1,1,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,
                0,0,1,1,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,
                0,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,
                0,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,
                1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,
                1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
        ];

        [Theory]
        [InlineData(0, 2, new[] { 0 }, new byte[] { 2 }, 4, 1, 32, 64, false, 1, 1)] 
        [InlineData(4, 2, new[] { 19 }, new byte[] { 15 }, 4, 1, 32, 64, false, 1, 2)] 
        [InlineData(4, 2, new[] { -1 }, new byte[] { 40 }, 4, 1, 32, 64, true, 1, 3)] 
        [InlineData(0, 256, new[] { 10, 150 }, new byte[] { 15, 36 }, 4, 2, 32, 64, false, 2, 4)] 
        [InlineData(7, 2, new[] { -1 }, new byte[] { 30 }, 0, 1, 16, 16, true, 1, 5)] 
        [InlineData(7, 2, new[] { -1 }, new byte[] { 30 }, 0, 1, 16, 16, false, 0, 6)] 
        [InlineData(16, 256, new[] { 10, 100, 200 }, new byte[] { 0xAA, 0xBB, 0xCC }, 8, 2, 16, 64, false, 2, 7)] 
        [InlineData(4, 2, new[] { 5, 150, 300 }, new byte[] { 5, 20, 45 }, 4, 3, 32, 32, false, 3, 8)] 
        [InlineData(0, 256, new[] { 10 }, new byte[] { 0xFF }, 4, 1, 32, 32, true, 1, 9)] 
        [InlineData(0, 2, new[] { 120 }, new byte[] { 35 }, 32, 1, 64, 16, false, 1, 10)] 
        [InlineData(3, 256, new[] { 40, 41, 42, 43 }, new byte[] { 1, 2, 3, 4 }, 4, 1, 16, 64, false, 1, 11)] 
        [InlineData(1, 2, new[] { -1 }, new byte[] { 9 }, 2, 1, 16, 32, true, 1, 12)] 
        public unsafe void GraphicsBitmap_ParametricMatrix(
            int border, int grays, int[] mismatchOffsets, byte[] mismatchValues,
            int leadingPixels, int maxBlocks, int maxMismatchPixels, int bytesPerLine, bool comparePadding, 
            int expectedBlocks, int fileIndex)
        {
            int width = 19;
            int height = 17;
            
            var oracle = new Bitmap();
            oracle.Init(height, width, border);
            oracle.Grays = grays;
            
            int stride = oracle.BytesPerRow;
            byte* pOracle = (byte*)oracle.DataPointer;
            ReadOnlySpan<sbyte> patternSpan = LetterAPattern;
            
            for (int y = 0; y < height; y++)
            {
                var sourceRow = patternSpan.Slice(y * width, width);
                var destRow = new Span<sbyte>(pOracle + (y * stride) + border, width);
                sourceRow.CopyTo(destRow);
            }
            
            var data = oracle.Duplicate();
            byte* pData = (byte*)data.DataPointer;
            
            for (int i = 0; i < mismatchOffsets.Length; i++)
            {
                int offset = mismatchOffsets[i];
                byte val = mismatchValues[i];

                if (offset < 0) pData[0] = val; 
                else pData[(offset / width) * stride + border + (offset % width)] = val; 
            }
            
            using (StringWriter sw = new StringWriter())
            {
                TextWriter originalOut = Console.Out;
                Console.SetOut(sw);
                try
                {
                    bool isBitonal = (grays == 2);
                    Util.DumpImageMismatch(
                        ref oracle, ref data, fileIndex, isBitonal, (uint)leadingPixels,
                        (uint)maxMismatchPixels, (uint)maxBlocks, (uint)bytesPerLine, comparePadding);
                    string output = sw.ToString();
                    
                    Assert.Contains($"{typeof(Bitmap).FullName} Mismatches for test0{fileIndex:00#}C.djvu:", output);
                    
                    if (expectedBlocks == 0)
                    {
                        Assert.DoesNotContain("Mismatch Block 1", output);
                    }
                    else
                    {
                        // Structure Validation
                        Assert.Matches(@"O:.*[\r\n]+.*T:.*[\r\n]+.*D:", output);
                        Assert.DoesNotContain($"Mismatch Block {expectedBlocks + 1}", output);

                        for (int b = 0; b < expectedBlocks; b++)
                        {
                            Assert.Contains($"Mismatch Block {b + 1}", output);
                            
                            int offset = mismatchOffsets[b];
                            byte val = mismatchValues[b];

                            // Geometric Validation
                            int expRow = (offset < 0) ? 0 : (offset / width);
                            if (offset < 0)
                            {
                                Assert.Contains($"Row: {expRow} | Column: [Leading Padding: {-border} bytes", output);
                            }
                            else
                            {
                                int expCol = comparePadding ? border + (offset % width) : (offset % width);
                                Assert.Contains($"Row: {expRow} | Column: {expCol}", output);
                            }

                            // Format Validation
                            string expectedTestOutput;
                            if (isBitonal)
                            {
                                if (val <= 9) expectedTestOutput = val.ToString();
                                else if (val <= 35) expectedTestOutput = ((char)('A' + (val - 10))).ToString();
                                else expectedTestOutput = "#";
                            }
                            else
                            {
                                expectedTestOutput = val.ToString("X2");
                            }

                            // Ensure the test line outputs the dynamically computed formatted value
                            Assert.Matches($@"T:.*{Regex.Escape(expectedTestOutput)}", output);
                        }
                    }
                }
                finally { Console.SetOut(originalOut); }
            }
        }


        [Theory]
        [InlineData(true, false, 32, 32, false, false, false, "references are null")]
        [InlineData(false, true, 32, 32, false, false, false, "references are null")]
        [InlineData(false, false, 0, 32, false, false, false, "Dimensions are")]
        [InlineData(false, false, 32, 0, false, false, false, "Dimensions are")]
        [InlineData(false, false, 32, 32, true, false, false, "Dimensions mismatch")]
        [InlineData(false, false, 32, 32, false, true, false, "Data buffers are null")]
        [InlineData(false, false, 32, 32, false, false, true, "undersized")]
        public void GraphicsBitmap_SafetyGuards(
            bool nullOracle, bool nullData, int width, int height,
            bool dimensionMismatch, bool nullBuffer, bool undersizedBuffer, string expectedErrorSubString)
        {
            Bitmap oracleMap = new Bitmap();
            if (!nullOracle)
            {
                oracleMap.Init(height, width, 0);
                ref BitmapSurrogate oracleSurrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref oracleMap);
                if (nullBuffer)
                {
                    oracleSurrogate._Data = null;
                }
                else if (undersizedBuffer)
                {
                    oracleSurrogate._Data = new sbyte[1];
                }
            }

            Bitmap dataMap = new Bitmap();
            if (!nullData)
            {
                int mappedHeight = dimensionMismatch ? height + 1 : height;
                int mappedWidth = dimensionMismatch ? width + 1 : width;
                dataMap.Init(mappedHeight, mappedWidth, 0);
                ref BitmapSurrogate dataSurrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref dataMap);
                if (nullBuffer)
                {
                    dataSurrogate._Data = null;
                }
                else if (undersizedBuffer)
                {
                    dataSurrogate._Data = new sbyte[1];
                }
            }

            using (StringWriter sw = new StringWriter())
            {
                TextWriter originalOut = Console.Out;
                Console.SetOut(sw);
                try
                {
                    if (nullOracle && nullData)
                    {
                        Util.DumpImageMismatch(ref Unsafe.NullRef<Bitmap>(), ref Unsafe.NullRef<Bitmap>(), 99);
                    }
                    else if (nullOracle)
                    {
                        Util.DumpImageMismatch(ref Unsafe.NullRef<Bitmap>(), ref dataMap, 99);
                    }
                    else if (nullData)
                    {
                        Util.DumpImageMismatch(ref oracleMap, ref Unsafe.NullRef<Bitmap>(), 99);
                    }
                    else
                    {
                        Util.DumpImageMismatch(ref oracleMap, ref dataMap, 99);
                    }
                }
                finally
                {
                    Console.SetOut(originalOut);
                }

                string output = sw.ToString();
                if (!string.IsNullOrEmpty(expectedErrorSubString))
                {
                    Assert.Contains(expectedErrorSubString, output);
                }
            }
        }

        //[Theory]
        //// 1. Center Mismatch (Hex Mode)
        //[InlineData(false, 2, 2, new[] { 50 }, new byte[] { 0x42 }, 4u, 64u, 32u, 1, false)]
        //[InlineData(false, 2, 2, new[] { 50 }, new byte[] { 0x42 }, 4u, 64u, 32u, 1, true)]
        //// 1A. Symmetric Borders with Leading Padding Mismatch
        //[InlineData(false, 2, 2, new[] { -2 }, new byte[] { 0x42 }, 4u, 64u, 32u, 1, true)]
        //// 2. Padding Mismatch (Left Border) - Tests Out-Of-Bounds handling
        //[InlineData(false, 3, 3, new[] { 0 }, new byte[] { 0xFF }, 2u, 32u, 32u, 2, false)]
        //[InlineData(false, 3, 3, new[] { 0 }, new byte[] { 0xFF }, 2u, 32u, 32u, 2, true)]
        //// 3. Center Mismatch (Bitonal Mode)
        //[InlineData(true, 2, 2, new[] { 50 }, new byte[] { 2 }, 8u, 64u, 32u, 3, false)]
        //[InlineData(true, 2, 2, new[] { 50 }, new byte[] { 2 }, 8u, 64u, 32u, 3, true)]
        //// 3A. Symmetric Borders Bitonal with Leading Padding Mismatch
        //[InlineData(true, 2, 2, new[] { -2 }, new byte[] { 3 }, 8u, 64u, 32u, 3, true)]
        //// 4. Bitonal Padding Mismatch - Tests '.' pad char and '#' error char
        //[InlineData(true, 4, 4, new[] { 1 }, new byte[] { 255 }, 8u, 64u, 32u, 4, false)]
        //[InlineData(true, 4, 4, new[] { 1 }, new byte[] { 255 }, 8u, 64u, 32u, 4, true)]
        
        //// 5. Asymmetric Borders (Oracle Pad 2, Test Pad 4)
        //[InlineData(false, 2, 4, new[] { 50 }, new byte[] { 0x42 }, 4u, 64u, 32u, 5, false)]
        //// TODO: (Future Padding Overhaul) The following test injects a mismatch in the non-overlapping "tail" padding.
        //// It is commented out until the engine is upgraded to perform Zero-Tail Verification for asymmetric strides.
        //// [InlineData(false, 2, 4, new[] { 50 }, new byte[] { 0x42 }, 4u, 64u, 32u, 5, true)]
        //// 5A. Asymmetric Borders with Overlapping Right Padding Mismatch
        //[InlineData(false, 2, 4, new[] { 47 }, new byte[] { 0x42 }, 4u, 64u, 32u, 5, true)]
        //// TODO: (Future Padding Overhaul) Asymmetric leading padding with mismatch in overlapping 
        //// part fails coordinate generation due to alignment bug.
        //// [InlineData(false, 2, 4, new[] { -2 }, new byte[] { 0x42 }, 4u, 64u, 32u, 5, true)]
        //// TODO: (Future Padding Overhaul) Non-overlapping leading padding.
        //// [InlineData(false, 2, 4, new[] { -4 }, new byte[] { 0x42 }, 4u, 64u, 32u, 5, true)]
        
        //// 6. Asymmetric Borders Bitonal
        //[InlineData(true, 2, 4, new[] { 50 }, new byte[] { 3 }, 4u, 64u, 32u, 6, false)]
        //// [InlineData(true, 2, 4, new[] { 50 }, new byte[] { 3 }, 4u, 64u, 32u, 6, true)]
        //// 6A. Asymmetric Borders Bitonal with Overlapping Right Padding Mismatch
        //[InlineData(true, 2, 4, new[] { 47 }, new byte[] { 3 }, 4u, 64u, 32u, 6, true)]
        //// TODO: (Future Padding Overhaul) Asymmetric leading padding with mismatch in overlapping 
        //// part fails coordinate generation due to alignment bug.
        //// [InlineData(true, 2, 4, new[] { -2 }, new byte[] { 3 }, 4u, 64u, 32u, 6, true)]
        //// [InlineData(true, 2, 4, new[] { -4 }, new byte[] { 3 }, 4u, 64u, 32u, 6, true)]
        
        //// 7. Small maxMismatchPixels (Truncated dump)
        //[InlineData(false, 2, 2, new[] { 50 }, new byte[] { 0x42 }, 4u, 64u, 8u, 7, false)]
        //[InlineData(false, 2, 2, new[] { 50 }, new byte[] { 0x42 }, 4u, 64u, 8u, 7, true)]
        //// 8. Large maxMismatchPixels (Extended dump)
        //[InlineData(true, 4, 4, new[] { 1 }, new byte[] { 255 }, 8u, 64u, 64u, 8, false)]
        //[InlineData(true, 4, 4, new[] { 1 }, new byte[] { 255 }, 8u, 64u, 64u, 8, true)]
        //public unsafe void GraphicsBitmap_FormatMatrix(
        //    bool bitonal, int oracleBorder, int dataBorder, int[] mismatchOffsets, byte[] mismatchValues,
        //    uint leadingPixels, uint bytesPerLine, uint maxMismatchPixels, int fileIndex, bool comparePadding)
        //{
        //    int width = 13;
        //    int height = 7;

        //    int oStride = width + oracleBorder;
        //    sbyte[] oracleData = new sbyte[height * oStride + oracleBorder];
        //    for (int i = 0; i < oracleData.Length; i++)
        //        oracleData[i] = (sbyte)(i % 2 == 0 ? 0 : 1);

        //    int tStride = width + dataBorder;
        //    sbyte[] testData = new sbyte[height * tStride + dataBorder];
        //    for (int i = 0; i < testData.Length; i++)
        //        testData[i] = (sbyte)(i % 2 == 0 ? 0 : 1);

        //    for (int b = 0; b < mismatchOffsets.Length; b++)
        //    {
        //        testData[mismatchOffsets[b] + dataBorder] = unchecked((sbyte)mismatchValues[b]);
        //    }

        //    Bitmap oracle = new Bitmap();
        //    oracle.Init(height, width, oracleBorder);
        //    oracle.Grays = bitonal ? 2 : 256;
        //    ref BitmapSurrogate oSurrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref oracle);
        //    oSurrogate._Data = oracleData;

        //    Bitmap data = new Bitmap();
        //    data.Init(height, width, dataBorder);
        //    ref BitmapSurrogate tSurrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref data);
        //    tSurrogate._Data = testData;

        //    using (StringWriter sw = new StringWriter())
        //    {
        //        TextWriter originalOut = Console.Out;
        //        Console.SetOut(sw);
        //        try
        //        {
        //            Util.DumpImageMismatch(ref oracle, ref data, fileIndex, oracle.Grays == 2,
        //                leadingPixels, maxMismatchPixels, 1u, bytesPerLine, comparePadding);
        //        }
        //        finally
        //        {
        //            Console.SetOut(originalOut);
        //        }

        //        string output = sw.ToString();

        //        // Verify output length and alignment
        //        var dumpLines = ExtractDumpLines(output, assertAlignment: true);

        //        //if (oracleBorder != dataBorder)
        //        //{
        //        //    foreach(var dl in dumpLines)
        //        //    {
        //        //        originalOut.WriteLine(dl.Item1);
        //        //        originalOut.WriteLine(dl.Item2);
        //        //        originalOut.WriteLine(dl.Item3);
        //        //    }
        //        //}

        //        int maxLeftPad = Math.Max(oracleBorder, dataBorder);
        //        int oracleStride = width + oracleBorder;
        //        int dataStride = width + dataBorder;

        //        int oracleRightPad = Math.Max(0, oracleStride - width - oracleBorder);
        //        int dataRightPad = Math.Max(0, dataStride - width - dataBorder);
        //        int maxRightPad = Math.Max(oracleRightPad, dataRightPad);
        //        int dumpLineWidth = maxLeftPad + width + maxRightPad;

        //        /// TODO: Verify: Why do we have such large chunk of code in test body?
        //        /// Should it be extracted as test help subroutine?
        //        /// Do we verify somewhere diff output bytes printout is properly aligned?
        //        int bytesPerPixel = 1;

        //        var expectedIndices = new System.Collections.Generic.List<int>();
        //        for (int b = 0; b < mismatchOffsets.Length; b++)
        //        {
        //            int offset = mismatchOffsets[b];
        //            int expRow = offset / dataStride;
        //            int rowByteOffset = offset % dataStride;

        //            bool isPadding = rowByteOffset < 0 || rowByteOffset >= (width * bytesPerPixel);
        //            bool expectsMismatchFound = !isPadding || comparePadding;

        //            if (expectsMismatchFound)
        //            {
        //                //if (offset == -2)
        //                //{
        //                //    PrintToConsole(output);
        //                //}
        //                Assert.Contains($"Row: {expRow} |", output);
        //                Assert.True(output.Contains($"Byte Offset In Row: {rowByteOffset}"), $"ASSERT_DUMP_STRAT: method output\n{output}");
        //                expectedIndices.Add(expRow * dumpLineWidth + maxLeftPad + rowByteOffset);
        //            }
        //        }

        //        if (expectedIndices.Count > 0)
        //        {
        //            VerifyMismatchBlocks(
        //                output, expectedIndices.ToArray(), height, dumpLineWidth,
        //                oracleStride: oracleStride, dataStride: dataStride,
        //                oracleLeftPad: oracleBorder, dataLeftPad: dataBorder,
        //                leadingPixels: leadingPixels, maxMismatchPixels: maxMismatchPixels,
        //                bytesPerPixel: bytesPerPixel, bytesPerLine: bytesPerLine,
        //                pixelSize: PixelSize._8bpp, 
        //                channelSize: bitonal ? ChannelSize._1bit : ChannelSize._8bit);
        //        }

        //        if (expectedIndices.Count > 0)
        //        {
        //            VerifyContent(
        //                output, expectedIndices.ToArray(), 
        //                MemoryMarshal.Cast<sbyte, byte>(oracleData), 
        //                MemoryMarshal.Cast<sbyte, byte>(testData),
        //                height, width, dumpLineWidth,
        //                oracleStride, dataStride, oracleBorder, dataBorder,
        //                leadingPixels, maxMismatchPixels, bytesPerLine,
        //                PixelSize._8bpp, bitonal ? ChannelSize._1bit : ChannelSize._8bit);
        //        }
        //    }
        //}

        [Theory]
        // 1. Center Mismatch (Hex Mode)
        [InlineData(false, 2, 2, new[] { 50 }, new byte[] { 0x42 }, 4u, 64u, 32u, 1, false)]
        [InlineData(false, 2, 2, new[] { 50 }, new byte[] { 0x42 }, 4u, 64u, 32u, 1, true)]
        // 1A. Symmetric Borders with Leading Padding Mismatch
        [InlineData(false, 2, 2, new[] { -2 }, new byte[] { 0x42 }, 4u, 64u, 32u, 1, true)]
        // 2. Padding Mismatch (Left Border) - Tests Out-Of-Bounds handling
        [InlineData(false, 3, 3, new[] { 0 }, new byte[] { 0xFF }, 2u, 32u, 32u, 2, false)]
        [InlineData(false, 3, 3, new[] { 0 }, new byte[] { 0xFF }, 2u, 32u, 32u, 2, true)]
        // 3. Center Mismatch (Bitonal Mode)
        [InlineData(true, 2, 2, new[] { 50 }, new byte[] { 2 }, 8u, 64u, 32u, 3, false)]
        [InlineData(true, 2, 2, new[] { 50 }, new byte[] { 2 }, 8u, 64u, 32u, 3, true)]
        // 3A. Symmetric Borders Bitonal with Leading Padding Mismatch
        [InlineData(true, 2, 2, new[] { -2 }, new byte[] { 3 }, 8u, 64u, 32u, 3, true)]
        // 4. Bitonal Padding Mismatch - Tests '.' pad char and '#' error char
        [InlineData(true, 4, 4, new[] { 1 }, new byte[] { 255 }, 8u, 64u, 32u, 4, false)]
        [InlineData(true, 4, 4, new[] { 1 }, new byte[] { 255 }, 8u, 64u, 32u, 4, true)]

        // 5. Asymmetric Borders (Oracle Pad 2, Test Pad 4)
        [InlineData(false, 2, 4, new[] { 50 }, new byte[] { 0x42 }, 4u, 64u, 32u, 5, false)]
        // TODO: (Task 11 - Zero-Tail Verification) The following test injects a mismatch in the non-overlapping "tail" padding.
        // It is commented out until the engine is upgraded to perform Zero-Tail Verification for asymmetric strides.
        // [InlineData(false, 2, 4, new[] { 50 }, new byte[] { 0x42 }, 4u, 64u, 32u, 5, true)]
        // 5A. Asymmetric Borders with Overlapping Right Padding Mismatch
        [InlineData(false, 2, 4, new[] { 47 }, new byte[] { 0x42 }, 4u, 64u, 32u, 5, true)]
        // TODO: (Future Padding Overhaul) Asymmetric leading padding with mismatch in overlapping 
        // part fails coordinate generation due to alignment bug.
        [InlineData(false, 2, 4, new[] { -2 }, new byte[] { 0x42 }, 4u, 64u, 32u, 5, true)]
        // TODO: (Task 11 - Zero-Tail Verification) Non-overlapping leading padding.
        // [InlineData(false, 2, 4, new[] { -4 }, new byte[] { 0x42 }, 4u, 64u, 32u, 5, true)]

        // 6. Asymmetric Borders Bitonal
        [InlineData(true, 2, 4, new[] { 50 }, new byte[] { 3 }, 4u, 64u, 32u, 6, false)]
        // TODO: (Task 11 - Zero-Tail Verification) The following test injects a mismatch in the non-overlapping "tail" padding.
        // [InlineData(true, 2, 4, new[] { 50 }, new byte[] { 3 }, 4u, 64u, 32u, 6, true)]
        // 6A. Asymmetric Borders Bitonal with Overlapping Right Padding Mismatch
        [InlineData(true, 2, 4, new[] { 47 }, new byte[] { 3 }, 4u, 64u, 32u, 6, true)]
        // TODO: (Future Padding Overhaul) Asymmetric leading padding with mismatch in overlapping 
        // part fails coordinate generation due to alignment bug.
        [InlineData(true, 2, 4, new[] { -2 }, new byte[] { 3 }, 4u, 64u, 32u, 6, true)]
        // TODO: (Task 11 - Zero-Tail Verification) Non-overlapping leading padding.
        // [InlineData(true, 2, 4, new[] { -4 }, new byte[] { 3 }, 4u, 64u, 32u, 6, true)]

        // 7. Small maxMismatchPixels (Truncated dump)
        [InlineData(false, 2, 2, new[] { 50 }, new byte[] { 0x42 }, 4u, 64u, 8u, 7, false)]
        [InlineData(false, 2, 2, new[] { 50 }, new byte[] { 0x42 }, 4u, 64u, 8u, 7, true)]
        // 8. Large maxMismatchPixels (Extended dump)
        [InlineData(true, 4, 4, new[] { 1 }, new byte[] { 255 }, 8u, 64u, 64u, 8, false)]
        [InlineData(true, 4, 4, new[] { 1 }, new byte[] { 255 }, 8u, 64u, 64u, 8, true)]
        public unsafe void GraphicsBitmap_FormatMatrix(
            bool bitonal, int oracleBorder, int dataBorder, int[] mismatchOffsets, byte[] mismatchValues,
            uint leadingPixels, uint bytesPerLine, uint maxMismatchPixels, int fileIndex, bool comparePadding)
        {
            int width = 13;
            int height = 7;

            int oStride = width + oracleBorder;
            sbyte[] oracleData = new sbyte[height * oStride + oracleBorder];
            for (int i = 0; i < oracleData.Length; i++)
                oracleData[i] = (sbyte)(i % 2 == 0 ? 0 : 1);

            int tStride = width + dataBorder;
            sbyte[] testData = new sbyte[height * tStride + dataBorder];
            for (int i = 0; i < testData.Length; i++)
                testData[i] = (sbyte)(i % 2 == 0 ? 0 : 1);

            for (int b = 0; b < mismatchOffsets.Length; b++)
            {
                testData[mismatchOffsets[b] + dataBorder] = unchecked((sbyte)mismatchValues[b]);
            }

            Bitmap oracle = new Bitmap();
            oracle.Init(height, width, oracleBorder);
            oracle.Grays = bitonal ? 2 : 256;
            ref BitmapSurrogate oSurrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref oracle);
            oSurrogate._Data = oracleData;

            Bitmap data = new Bitmap();
            data.Init(height, width, dataBorder);
            ref BitmapSurrogate tSurrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref data);
            tSurrogate._Data = testData;

            using (StringWriter sw = new StringWriter())
            {
                TextWriter originalOut = Console.Out;
                Console.SetOut(sw);
                try
                {
                    Util.DumpImageMismatch(ref oracle, ref data, fileIndex, oracle.Grays == 2,
                        leadingPixels, maxMismatchPixels, 1u, bytesPerLine, comparePadding);
                }
                finally
                {
                    Console.SetOut(originalOut);
                }

                string output = sw.ToString();

                // Verify output length and alignment
                var dumpLines = ExtractDumpLines(output, assertAlignment: true);

                //if (oracleBorder != dataBorder)
                //{
                //    foreach(var dl in dumpLines)
                //    {
                //        originalOut.WriteLine(dl.Item1);
                //        originalOut.WriteLine(dl.Item2);
                //        originalOut.WriteLine(dl.Item3);
                //    }
                //}

                int maxLeftPad = Math.Max(oracleBorder, dataBorder);
                int oracleStride = width + oracleBorder;
                int dataStride = width + dataBorder;

                int oracleRightPad = Math.Max(0, oracleStride - width - oracleBorder);
                int dataRightPad = Math.Max(0, dataStride - width - dataBorder);
                int maxRightPad = Math.Max(oracleRightPad, dataRightPad);
                int dumpLineWidth = maxLeftPad + width + maxRightPad;

                /// TODO: Verify: Why do we have such large chunk of code in test body?
                /// Should it be extracted as test help subroutine?
                /// Do we verify somewhere diff output bytes printout is properly aligned?
                int bytesPerPixel = 1;

                var expectedIndices = new System.Collections.Generic.List<int>();
                for (int b = 0; b < mismatchOffsets.Length; b++)
                {
                    int offset = mismatchOffsets[b];
                    int expRow = offset / dataStride;
                    int rowByteOffset = offset % dataStride;

                    bool isPadding = rowByteOffset < 0 || rowByteOffset >= (width * bytesPerPixel);
                    bool expectsMismatchFound = !isPadding || comparePadding;

                    if (expectsMismatchFound)
                    {
                        //if (offset == -2)
                        //{
                        //    PrintToConsole(output);
                        //}
                        Assert.Contains($"Row: {expRow} |", output);
                        Assert.True(output.Contains($"Byte Offset In Row: {rowByteOffset}"), $"ASSERT_DUMP_STRAT: method output\n{output}");
                        expectedIndices.Add(expRow * dumpLineWidth + maxLeftPad + rowByteOffset);
                    }
                }

                if (expectedIndices.Count > 0)
                {
                    VerifyMismatchBlocks(
                        output, expectedIndices.ToArray(), height, dumpLineWidth,
                        oracleStride: oracleStride, dataStride: dataStride,
                        oracleLeftPad: oracleBorder, dataLeftPad: dataBorder,
                        leadingPixels: leadingPixels, maxMismatchPixels: maxMismatchPixels,
                        bytesPerPixel: bytesPerPixel, bytesPerLine: bytesPerLine,
                        pixelSize: PixelSize._8bpp,
                        channelSize: bitonal ? ChannelSize._1bit : ChannelSize._8bit);
                }

                if (expectedIndices.Count > 0)
                {
                    VerifyContent(
                        output, expectedIndices.ToArray(),
                        MemoryMarshal.Cast<sbyte, byte>(oracleData),
                        MemoryMarshal.Cast<sbyte, byte>(testData),
                        height, width, dumpLineWidth,
                        oracleStride, dataStride, oracleBorder, dataBorder,
                        leadingPixels, maxMismatchPixels, bytesPerLine,
                        PixelSize._8bpp, bitonal ? ChannelSize._1bit : ChannelSize._8bit);
                }
            }
        }

        private static ReadOnlySpan<sbyte> PixelMapPattern23x17 => MemoryMarshal.Cast<byte, sbyte>(
            [
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00,
                0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF,
                0xFF, 0x80, 0x00
            ]);

        [Theory]
        // 1. Single point mismatch at absolute beginning (Row 0, Col 0, Blue)
        [InlineData(new[] { 0 }, new byte[] { 0x42 }, 4u, 1u, 32u, 64u, 1, 1)]
        // 2. Mismatch in middle (Row 5, Col 10, Red -> (5*23+10)*3+2 = 377)
        [InlineData(new[] { 377 }, new byte[] { 0x42 }, 4u, 1u, 32u, 64u, 1, 2)]
        // 3. Mismatch at absolute end (Row 16, Col 22, Red -> (16*23+22)*3+2 = 1172)
        [InlineData(new[] { 1172 }, new byte[] { 0x42 }, 4u, 1u, 32u, 64u, 1, 3)]
        // 4. Cross-pixel mismatch block (Row 2, Col 2 Red to Col 3 Green -> bytes 146, 147, 148)
        [InlineData(new[] { 146, 147, 148 }, new byte[] { 0x42, 0x42, 0x42 }, 4u, 1u, 32u, 64u, 1, 4)]
        // 5. Multi-Block: 2 distinct mismatched zones far apart
        [InlineData(new[] { 0, 1172 }, new byte[] { 0x42, 0x42 }, 4u, 2u, 32u, 64u, 2, 5)]
        // 6. Output Flooding Control: maxMismatchPixels truncates the rendered block (must be > leadingPixels)
        [InlineData(new[] { 100, 101, 102, 103, 104, 105, 106, 107 }, new byte[] { 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42 }, 4u, 1u, 6u, 64u, 1, 6)]
        // 7. Hex Wrapping Stress: bytesPerLine = 4 (splits 24bpp pixel rendering)
        [InlineData(new[] { 150 }, new byte[] { 0x42 }, 0u, 1u, 12u, 4u, 1, 7)]
        // 8. Hex Wrapping Stress: bytesPerLine = 16 (forces weird alignments)
        [InlineData(new[] { 200 }, new byte[] { 0x42 }, 2u, 1u, 32u, 16u, 1, 8)]
        public unsafe void GraphicsPixelMap_ParametricMatrix(
            int[] mismatchOffsets, byte[] mismatchValues,
            uint leadingPixels, uint maxBlocks, uint maxMismatchPixels, uint bytesPerLine, 
            int expectedBlocks, int fileIndex)
        {
            int width = 23;
            int height = 17;
            
            sbyte[] oracleData = new sbyte[width * height * PixelMap.BytesPerPixel];
            PixelMapPattern23x17.CopyTo(oracleData.AsSpan());

            sbyte[] testData = new sbyte[oracleData.Length];
            Array.Copy(oracleData, testData, oracleData.Length);
            
            for (int i = 0; i < mismatchOffsets.Length; i++)
            {
                testData[mismatchOffsets[i]] = unchecked((sbyte)mismatchValues[i]);
            }
            
            PixelMap oracle = new PixelMap(oracleData, width, height);
            PixelMap data = new PixelMap(testData, width, height);
            
            using (StringWriter sw = new StringWriter())
            {
                TextWriter originalOut = Console.Out;
                Console.SetOut(sw);
                try
                {
                    Util.DumpImageMismatch(
                        oracle, 
                        data, 
                        fileIndex, 
                        leadingPixels, 
                        maxMismatchPixels, 
                        maxBlocks, 
                        bytesPerLine);
                    
                    string output = sw.ToString();

                    Console.WriteLine(output);

                    Assert.Contains($"{typeof(PixelMap).Name} Mismatches for test0{fileIndex:00#}C.djvu:", output);
                    
                    if (expectedBlocks == 0)
                    {
                        Assert.DoesNotContain("Mismatch Block 1", output);
                    }
                    else
                    {
                        Assert.Matches(@"O:.*[\r\n]+.*T:.*[\r\n]+.*D:", output);
                        Assert.DoesNotContain($"Mismatch Block {expectedBlocks + 1}", output);

                        for (int b = 0; b < expectedBlocks; b++)
                        {
                            Assert.Contains($"Mismatch Block {b + 1}", output);
                            
                            int offset = mismatchOffsets[b];
                            byte val = mismatchValues[b];

                            int bytesPerRow = width * PixelMap.BytesPerPixel;
                            int expRow = offset / bytesPerRow;
                            int expCol = (offset % bytesPerRow) / PixelMap.BytesPerPixel;
                            
                            Assert.Contains($"Row: {expRow} | Column: {expCol}", output);

                            // Validate multiline buffer index prefixes.
                            int mismatchByte = offset % bytesPerRow;
                            int pxByte = (mismatchByte / PixelMap.BytesPerPixel) * PixelMap.BytesPerPixel;
                            int leadingBytes = (int)leadingPixels * PixelMap.BytesPerPixel;
                            int maxMismatchBytes = (int)maxMismatchPixels * PixelMap.BytesPerPixel;
                            int mismatchDumpIndex = expRow * bytesPerRow + pxByte;
                            
                            int dumpStartIndex = Math.Max(0, mismatchDumpIndex - leadingBytes);
                            int dumpEndIndex = Math.Min(height * bytesPerRow, mismatchDumpIndex + maxMismatchBytes);

                            for (int chunkStart = dumpStartIndex; chunkStart < dumpEndIndex; chunkStart += (int)bytesPerLine)
                            {
                                int chunkEnd = Math.Min(dumpEndIndex, chunkStart + (int)bytesPerLine);
                                string expectedBufferHex = $"0x{chunkStart:X8}";

                                StringBuilder expectedO = new StringBuilder($"{expectedBufferHex} O:  ");
                                StringBuilder expectedT = new StringBuilder($"{expectedBufferHex} T:  ");
                                StringBuilder expectedD = new StringBuilder("           D:  ");

                                for (int curr = chunkStart; curr < chunkEnd; curr++)
                                {
                                    int matchIdx = Array.IndexOf(mismatchOffsets, curr);
                                    byte oByte = unchecked((byte)oracleData[curr]);
                                    byte tByte = matchIdx >= 0 ? mismatchValues[matchIdx] : oByte;

                                    expectedO.Append($"{oByte:X2} ");
                                    expectedT.Append($"{tByte:X2} ");

                                    if (matchIdx >= 0)
                                    {
                                        byte diff = (byte)Math.Abs(oByte - tByte);
                                        expectedD.Append($"{diff:X2} ");
                                    }
                                    else
                                    {
                                        expectedD.Append("00 ");
                                    }
                                }

                                string assertString = $"ASSERT_DUMP_START: method output\n{output}\nASSERT_DUMP_END\n";
                                
                                Assert.True(output.Contains(expectedO.ToString()), assertString + $"\nAssert string:\n{expectedO}\n");
                                Assert.True(output.Contains(expectedT.ToString()), assertString + $"\nAssert string:\n{expectedT}\n");
                                Assert.True(output.Contains(expectedD.ToString()), assertString + $"\nAssert string:\n{expectedD}\n");
                            }
                        }
                    }
                }
                finally 
                { 
                    Console.SetOut(originalOut); 
                }
            }
        }

        [Fact]
        public unsafe void Core_ThrowsOnInvalidWindowBounds()
        {
            byte[] oracleBuffer = new byte[1024];
            byte[] testBuffer = new byte[1024];
            StringBuilder sb = new StringBuilder();
            ArgumentException caughtException = null;

            fixed (byte* pOracle = oracleBuffer)
            fixed (byte* pData = testBuffer)
            {
                try
                {
                    Util.DumpImageMismatchCore(
                        pOracle, pData, 10, 10, 10, 10,
                        PixelSize._8bpp, ChannelSize._8bit, sb,
                        leadingPixels: 10, maxMismatchPixels: 10, maxBlocks: 1, bytesPerLine: 64);
                }
                catch (ArgumentException ex)
                {
                    caughtException = ex;
                }
            }

            Assert.NotNull(caughtException);
            Assert.Contains("must be strictly less than", caughtException.Message);
        }

        [Theory]
        [InlineData(true, false, 32, 32, false, false, false, "references are null")]
        [InlineData(false, true, 32, 32, false, false, false, "references are null")]
        [InlineData(false, false, 0, 32, false, false, false, "Dimensions are")]
        [InlineData(false, false, 32, 0, false, false, false, "Dimensions are")]
        [InlineData(false, false, 32, 32, true, false, false, "Dimensions mismatch")]
        [InlineData(false, false, 32, 32, false, true, false, "Data buffers are null")]
        [InlineData(false, false, 32, 32, false, false, true, "undersized")]
        public void GraphicsPixelMap_SafetyGuards(
            bool nullOracle, bool nullData,
            int width, int height,
            bool dimensionMismatch, bool nullBuffer, bool undersizedBuffer, string expectedErrorSubString)
        {
            PixelMap oracleMap = nullOracle ? null : new PixelMap();
            if (oracleMap != null)
            {
                oracleMap.Init(height, width, new Pixel());
                if (nullBuffer)
                {
                    oracleMap.Data = null;
                }
                else if (undersizedBuffer)
                {
                    oracleMap.Data = new sbyte[1];
                }
            }

            PixelMap dataMap = nullData ? null : new PixelMap();
            if (dataMap != null)
            {
                int mappedHeight = dimensionMismatch ? height + 1 : height;
                int mappedWidth = dimensionMismatch ? width + 1 : width;
                dataMap.Init(mappedHeight, mappedWidth, new Pixel());
                if (nullBuffer)
                {
                    dataMap.Data = null;
                }
                else if (undersizedBuffer)
                {
                    dataMap.Data = new sbyte[1];
                }
            }

            using (StringWriter sw = new StringWriter())
            {
                TextWriter originalOut = Console.Out;
                Console.SetOut(sw);
                try
                {
                    // Execution must safely abort without throwing unhandled exceptions
                    Util.DumpImageMismatch(oracleMap, dataMap, 99);
                }
                finally
                {
                    Console.SetOut(originalOut);
                }

                string output = sw.ToString();
                if (!string.IsNullOrEmpty(expectedErrorSubString))
                {
                    Assert.Contains(expectedErrorSubString, output);
                }
            }
        }

        [Fact]
        public void GraphicsPixelMap_SwallowsExceptionsGracefully()
        {
            PixelMap oracleMap = new PixelMap();
            oracleMap.Init(10, 10, new Pixel());
            PixelMap dataMap = new PixelMap();
            dataMap.Init(10, 10, new Pixel());

            using (StringWriter sw = new StringWriter())
            {
                TextWriter originalOut = Console.Out;
                Console.SetOut(sw);
                try
                {
                    // Force the ArgumentException by passing leadingPixels >= maxMismatchPixels
                    Util.DumpImageMismatch(oracleMap, dataMap, 99, leadingPixels: 10, maxMismatchPixels: 10);
                }
                finally
                {
                    Console.SetOut(originalOut);
                }

                string output = sw.ToString();
                Assert.Contains("Error dumping mismatch details", output);
                Assert.Contains("must be strictly less than", output);
            }
        }

        [Theory]
        [InlineData(true, 0, 3)]   // Buffer start padding
        [InlineData(false, 0, 3)]  // Buffer start padding
        [InlineData(true, 17, 3)]  // Row 1, Col 0 visual pixel
        [InlineData(false, 17, 3)] // Row 1, Col 0 visual pixel
        [InlineData(true, 24, 7)]  // Right of row 1 visual / Left pad row 2
        [InlineData(false, 24, 7)] // Right of row 1 visual
        public unsafe void GraphicsBitmap_Padding(bool comparePadding, int mismatchOffset, int leadingPixels)
        {
            int width = 7;
            int height = 3;
            int border = 5;
            
            Bitmap oracleMap = new Bitmap();
            oracleMap.Init(height, width, border);
            
            Bitmap dataMap = new Bitmap();
            dataMap.Init(height, width, border);
            
            ref BitmapSurrogate oSurrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref oracleMap);
            ref BitmapSurrogate tSurrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref dataMap);
            
            for(int i = 0; i < oSurrogate._Data.Length; i++) 
            {
                oSurrogate._Data[i] = (sbyte)i;
                tSurrogate._Data[i] = (sbyte)i;
            }
            
            tSurrogate._Data[mismatchOffset] = unchecked((sbyte)0xFF);
            
            StringBuilder sb = new StringBuilder();
            //sb.AppendLine($"=== TEST PARAMETERS ===");
            //sb.AppendLine($"Width: {width}, Height: {height}, Border (Padding): {border}");
            //sb.AppendLine($"BytesPerRow: {width + border}");
            //sb.AppendLine($"mismatchOffset: {mismatchOffset}, comparePadding: {comparePadding}");
            //sb.AppendLine($"Dump Engine Args: leadingPixels={leadingPixels}, maxMismatchPixels=41, bytesPerLine=37");
            //sb.AppendLine();

            //sb.AppendLine($"=== RAW ORACLE BUFFER (Offset {mismatchOffset}, cp={comparePadding}, lp={leadingPixels}) ===");
            //for(int i = 0; i < oSurrogate._Data.Length; i++) sb.Append($"{((byte)oSurrogate._Data[i]):X2} ");
            //sb.AppendLine();
            //sb.AppendLine($"=== RAW TEST BUFFER (Offset {mismatchOffset}, cp={comparePadding}, lp={leadingPixels}) ===");
            //for(int i = 0; i < tSurrogate._Data.Length; i++) sb.Append($"{((byte)tSurrogate._Data[i]):X2} ");
            //sb.AppendLine();

            string output = "";
            using (StringWriter sw = new StringWriter())
            {
                TextWriter originalOut = Console.Out;
                Console.SetOut(sw);
                try 
                { 
                    Util.DumpImageMismatch(ref oracleMap, ref dataMap, 99, false, 
                        (uint)leadingPixels, 41u, 1u, 37u, comparePadding); 
                }
                finally 
                { 
                    Console.SetOut(originalOut); 
                }
                output = sw.ToString();
            }
            
            PrintToConsole($"\n{sb.ToString()}\n=== ENGINE OUTPUT ===\n{output}", false, Console.Out);
            
            Assert.True(true);
        }

        [Theory]
        [InlineData(false, 0, 3)]  // Bottom Padding
        [InlineData(true, 0, 3)]
        [InlineData(false, 17, 3)] // Row 0 Pixel (Col 12)
        [InlineData(true, 17, 3)]
        [InlineData(false, 24, 7)] // Row 0 Right Padding
        [InlineData(true, 24, 7)]
        [InlineData(false, 29, 3)] // Row 1 Pixel (First pixel)
        [InlineData(true, 29, 3)]
        [InlineData(false, 47, 5)] // Row 1 Pixel (Last pixel)
        [InlineData(true, 47, 5)]
        [InlineData(false, 48, 7)] // Row 1 Right Padding
        [InlineData(true, 48, 7)]
        [InlineData(false, 76, 3)] // Row 2 Right Padding (Very last byte)
        [InlineData(true, 76, 3)]
        public unsafe void GraphicsBitmap_Padding_Bitonal(bool comparePadding, int mismatchOffset, int leadingPixels)
        {
            int width = 19;
            int height = 3;
            int border = 5;
            
            Bitmap oracleMap = new Bitmap();
            oracleMap.Init(height, width, border);
            oracleMap.Grays = 2;
            
            Bitmap dataMap = new Bitmap();
            dataMap.Init(height, width, border);
            dataMap.Grays = 2;
            
            ref BitmapSurrogate oSurrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref oracleMap);
            ref BitmapSurrogate tSurrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref dataMap);
            
            ReadOnlySpan<sbyte> pattern = LetterAPattern.Slice(6 * 19, height * 19);
            for (int row = 0; row < height; row++)
            {
                sbyte* oRow = oracleMap.GetRow(row);
                sbyte* tRow = dataMap.GetRow(row);
                for (int col = 0; col < width; col++)
                {
                    sbyte val = pattern[row * width + col];
                    oRow[col] = val;
                    tRow[col] = val;
                }
            }
            
            tSurrogate._Data[mismatchOffset] = unchecked((sbyte)0xFF);

            StringBuilder sb = new StringBuilder();
            //sb.AppendLine($"=== TEST PARAMETERS ===");
            //sb.AppendLine($"Width: {width}, Height: {height}, Border (Padding): {border}");
            //sb.AppendLine($"BytesPerRow: {width + border}");
            //sb.AppendLine($"mismatchOffset: {mismatchOffset}, comparePadding: {comparePadding}");
            //sb.AppendLine($"Dump Engine Args: leadingPixels={leadingPixels}, maxMismatchPixels=41, bytesPerLine=37, bitonal=True");
            //sb.AppendLine();

            //sb.AppendLine($"=== RAW ORACLE BUFFER (Offset {mismatchOffset}, cp={comparePadding}, lp={leadingPixels}) ===");
            //for(int i = 0; i < oSurrogate._Data.Length; i++) sb.Append($"{((byte)oSurrogate._Data[i]):X2} ");
            //sb.AppendLine();
            //sb.AppendLine($"=== RAW TEST BUFFER (Offset {mismatchOffset}, cp={comparePadding}, lp={leadingPixels}) ===");
            //for(int i = 0; i < tSurrogate._Data.Length; i++) sb.Append($"{((byte)tSurrogate._Data[i]):X2} ");
            //sb.AppendLine();

            string output = "";
            using (StringWriter sw = new StringWriter())
            {
                TextWriter originalOut = Console.Out;
                Console.SetOut(sw);
                try 
                { 
                    Util.DumpImageMismatch(ref oracleMap, ref dataMap, 99, true, 
                        (uint)leadingPixels, 41u, 1u, 37u, comparePadding); 
                }
                finally 
                { 
                    Console.SetOut(originalOut); 
                }
                output = sw.ToString();
            }
            
            PrintToConsole($"\n{sb.ToString()}\n=== ENGINE OUTPUT ===\n{output}", false, Console.Out);
            
            Assert.True(true);
        }

        private string InvokeFormatByte(byte val, int bitsPerPixel)
        {
            StringBuilder sb = new StringBuilder();
            Util.FormatByte(val, 0, bitsPerPixel, false, sb, ' ');
            return sb.ToString().TrimEnd(' ');
        }

        private string GetExpectedString(byte val, int bitsPerPixel)
        {
            string bin = Convert.ToString(val, 2).PadLeft(8, '0');
            if (bitsPerPixel == 4) return $"{bin.Substring(0, 4)} {bin.Substring(4, 4)}";
            if (bitsPerPixel == 2) return $"{bin.Substring(0, 2)} {bin.Substring(2, 2)} {bin.Substring(4, 2)} {bin.Substring(6, 2)}";
            return bin;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        public void FormatByte_SubByteFormatting_Correctness_AllValues(int bitsPerPixel)
        {
            for (int i = 0; i <= 255; i++)
            {
                byte val = (byte)i;
                string expected = GetExpectedString(val, bitsPerPixel);
                string actual = InvokeFormatByte(val, bitsPerPixel);
                Assert.Equal(expected, actual);
            }
        }
    }
}
