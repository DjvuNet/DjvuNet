using Xunit;
using DjvuNet.Graphics;
using DjvuNet.Errors;
using DjvuNet.Extensions;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;

namespace DjvuNet.Graphics.Tests
{
    public class ParserUtilTests
    {


        /// <summary>
        /// Verifies the ReadInteger method successfully skips leading whitespaces and comments 
        /// before extracting the integer value from the stream.
        /// </summary>
        [Fact]
        public void ReadInteger_ValidInput()
        {
            string input = "  \t \n # comment \n 12345 ";
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(input)))
            {
                char c = (char)stream.ReadByte();
                uint result = ParserUtil.ReadInteger(ref c, stream);
                Assert.Equal(12345u, result);
            }
        }

        /// <summary>
        /// Verifies that providing an invalid (non-numeric) character to ReadInteger 
        /// correctly aborts parsing and throws a domain-specific DjvuFormatException.
        /// </summary>
        [Fact]
        public void ReadInteger_InvalidInput_Throws()
        {
            string input = "abc";
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(input)))
            {
                char c = (char)stream.ReadByte();
                Assert.Throws<DjvuFormatException>((Action)(() => ParserUtil.ReadInteger(ref c, stream)));
            }
        }

        /// <summary>
        /// Verifies that ReadInteger explicitly guards against null stream parameters,
        /// throwing DjvuArgumentNullException instead of a raw runtime NullReferenceException.
        /// </summary>
        [Fact]
        public void ReadInteger_NullStream_Throws()
        {
            char c = ' ';
            var ex = Assert.Throws<DjvuArgumentNullException>(() => ParserUtil.ReadInteger(ref c, null));
            Assert.Equal("stream", ex.ParamName);
        }

        /// <summary>
        /// Verifies that ReadInteger correctly handles a truncated stream during comment parsing
        /// by throwing a DjvuEndOfStreamException instead of entering an infinite loop.
        /// </summary>
        [Fact]
        public void ReadInteger_EofInComment_Throws()
        {
            // The string ends abruptly inside a comment (no \n or \r)
            string input = " # truncated comment";
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(input)))
            {
                char c = (char)stream.ReadByte();
                Assert.Throws<DjvuEndOfStreamException>((Action)(() => ParserUtil.ReadInteger(ref c, stream)));
            }
        }

        /// <summary>
        /// Verifies that ReadInteger correctly handles a truncated stream while consuming leading
        /// whitespace, throwing a DjvuEndOfStreamException to prevent buffer over-reads.
        /// </summary>
        [Fact]
        public void ReadInteger_EofInWhitespace_Throws()
        {
            // The string ends abruptly while still consuming trailing whitespace
            string input = "  ";
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(input)))
            {
                char c = (char)stream.ReadByte();
                Assert.Throws<DjvuEndOfStreamException>((Action)(() => ParserUtil.ReadInteger(ref c, stream)));
            }
        }

        /// <summary>
        /// Verifies that parsing integer strings at extreme valid boundaries parses correctly.
        /// </summary>
        [Theory]
        [InlineData(" 0 ", 0u)]
        [InlineData(" 4294967295 ", 4294967295u)] // uint.MaxValue
        public void ReadInteger_ValidBoundary(string input, uint expected)
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(input)))
            {
                char c = (char)stream.ReadByte();
                uint result = ParserUtil.ReadInteger(ref c, stream);
                Assert.Equal(expected, result);
            }
        }

        /// <summary>
        /// Verifies that parsing integer strings exceeding uint bounds safely throws a 
        /// DjvuFormatException rather than silently overflowing, testing exact boundaries and massive numbers.
        /// </summary>
        [Theory]
        [InlineData(" 4294967296 ")] // uint.MaxValue + 1
        [InlineData(" 5000000000 ")] // Large overflow
        [InlineData(" 999999999999999999999999999 ")] // Massive overflow (Multiple wraps)
        public void ReadInteger_OverflowBoundary_Throws(string input)
        {
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(input)))
            {
                char c = (char)stream.ReadByte();
                var ex = Assert.Throws<DjvuFormatException>((Action)(() => ParserUtil.ReadInteger(ref c, stream)));
                Assert.Contains("exceeds maximum representable bounds", ex.Message);
            }
        }

        [Fact]
        public void ReadInteger_EofAfterDigits()
        {
            byte[] data = Encoding.UTF8.GetBytes("12345");
            using (var stream = new MemoryStream(data))
            {
                char c = (char)stream.ReadByte();
                uint result = ParserUtil.ReadInteger(ref c, stream);
                Assert.Equal(12345u, result);
            }
        }
    }
}
