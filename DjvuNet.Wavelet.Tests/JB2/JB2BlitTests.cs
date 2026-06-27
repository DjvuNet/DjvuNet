using Xunit;
using DjvuNet.JB2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.JB2.Tests
{
    public class JB2BlitTests
    {
        [Theory]
        [InlineData(0, 0)]                           // Zero
        [InlineData(32767, 32767)]                   // short.MaxValue
        [InlineData(32768, 32768)]                   // Sign-bit flips
        [InlineData(65535, 65535)]                   // ushort.MaxValue
        [InlineData(65536, 0)]                       // 16-bit boundary overflow
        [InlineData(-1, 65535)]                      // -1 (0xFFFFFFFF)
        [InlineData(-32768, 32768)]                  // short.MinValue
        [InlineData(int.MaxValue, 65535)]            // 32-bit Max
        [InlineData(int.MinValue, 0)]                // 32-bit Min
        public void JB2Blit_Bottom_MasksToUnsigned16Bit(int input, int expected)
        {
            var blit = new JB2Blit();
            blit.Bottom = input;
            Assert.Equal(expected, blit.Bottom);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(32767, 32767)]
        [InlineData(32768, 32768)]
        [InlineData(65535, 65535)]
        [InlineData(65536, 0)]
        [InlineData(-1, 65535)]
        [InlineData(-32768, 32768)]
        [InlineData(int.MaxValue, 65535)]
        [InlineData(int.MinValue, 0)]
        public void JB2Blit_Left_MasksToUnsigned16Bit(int input, int expected)
        {
            var blit = new JB2Blit();
            blit.Left = input;
            Assert.Equal(expected, blit.Left);
        }

        [Fact]
        public void JB2Blit_ShapeNumber_SetsAndGets()
        {
            var blit = new JB2Blit();
            blit.ShapeNumber = 42;
            Assert.Equal(42, blit.ShapeNumber);
        }

        [Fact]
        public void JB2Blit_Duplicate_CreatesExactCopy()
        {
            var blit = new JB2Blit
            {
                Bottom = 150,
                Left = 250,
                ShapeNumber = 10
            };

            var duplicate = blit.Duplicate();

            Assert.NotSame(blit, duplicate); // Ensure distinct instance
            Assert.Equal(blit.Bottom, duplicate.Bottom);
            Assert.Equal(blit.Left, duplicate.Left);
            Assert.Equal(blit.ShapeNumber, duplicate.ShapeNumber);
        }
    }
}