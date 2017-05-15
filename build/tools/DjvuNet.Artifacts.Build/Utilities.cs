using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Artifacts.Build
{
    // After http://stackoverflow.com/questions/623104/byte-to-hex-string/3974535#3974535
    // this method gives best performance according to benchmarks discussed above this answer

    public static class Utilities
    {
        public static string ToHexString(this byte[] bytes)
        {
            char[] charArray = new char[bytes.Length * 2];

            byte @byte;

            for (int bx = 0, cx = 0; bx < bytes.Length; ++bx, ++cx)
            {
                @byte = ((byte)(bytes[bx] >> 4));
                charArray[cx] = (char)(@byte > 9 ? @byte + 0x37 + 0x20 : @byte + 0x30);

                @byte = ((byte)(bytes[bx] & 0x0F));
                charArray[++cx] = (char)(@byte > 9 ? @byte + 0x37 + 0x20 : @byte + 0x30);
            }

            return new string(charArray);
        }

        public static byte[] HexStringToBytes(this string @string)
        {
            if (@string.Length == 0 || @string.Length % 2 != 0)
                return new byte[0];

            byte[] buffer = new byte[@string.Length / 2];
            char @char;
            for (int bx = 0, sx = 0; bx < buffer.Length; ++bx, ++sx)
            {
                @char = @string[sx];
                buffer[bx] = (byte)((@char > '9' ? (@char > 'Z' ? (@char - 'a' + 10) : (@char - 'A' + 10)) : (@char - '0')) << 4);

                @char = @string[++sx];
                buffer[bx] |= (byte)(@char > '9' ? (@char > 'Z' ? (@char - 'a' + 10) : (@char - 'A' + 10)) : (@char - '0'));
            }

            return buffer;
        }
    }
}
