using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace DjvuNet.Skia
{
    public class DjvuSkiaGraphics
    {
        public static byte[] GetImageData(string file)
        {
            using (var bmp = SKBitmap.Decode(file))
                return bmp.Bytes;
        }
    }
}
