using System;
using System.IO;
using DjvuNet.Errors;

namespace DjvuNet.Graphics
{
    public static class ParserUtil
    {
        public static uint ReadInteger(ref char @char, Stream stream)
        {
            if (stream == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(stream));
            }

            uint xinteger = 0;

            while (@char == ' ' || @char == '\t' || @char == '\r' || @char == '\n' || @char == '#')
            {
                if (@char == '#')
                {
                    int b;
                    do
                    {
                        b = stream.ReadByte();
                        if (b < 0)
                        {
                            DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream while parsing header comment.");
                        }
                        @char = (char)b;
                    }
                    while (@char != '\n' && @char != '\r');
                }
                @char = (char)0;

                int nextByte = stream.ReadByte();
                if (nextByte < 0)
                {
                    DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream while parsing header whitespace.");
                }
                @char = (char)nextByte;
            }

            if (@char < '0' || @char > '9')
            {
                DjvuExceptionUtil.ThrowFormatException($"Expected integer value. Actual value: {@char}");
            }

            while (@char >= '0' && @char <= '9')
            {
                checked
                {
                    try
                    {
                        xinteger = (xinteger * 10) + (uint)(@char - '0');
                    }
                    catch (OverflowException ex)
                    {
                        DjvuExceptionUtil.ThrowFormatException("Parsed integer exceeds maximum representable bounds for uint.", ex);
                    }
                }
                @char = (char)0;

                int valByte = stream.ReadByte();
                if (valByte < 0)
                {
                    // EOF while reading digits is valid (it means we reached the end of the number at the end of the file)
                    break;
                }
                @char = (char)valByte;
            }

            return xinteger;
        }
    }
}
