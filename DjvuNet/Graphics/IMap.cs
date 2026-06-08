using System.Collections;
using System.Drawing;

namespace DjvuNet.Graphics
{
    public interface IMap
    {
        int BlueOffset { get; set; }

        int BytesPerPixel { get; }

        sbyte[] Data { get; }

        int GreenOffset { get; set; }

        int Height { get; }

        int Width { get; }

        bool IsRampNeeded { get; set; }

        Hashtable Properties { get; }

        int RedOffset { get; set; }

        IPixelReference CreateGPixelReference(int offset);

        IPixelReference CreateGPixelReference(int row, int column);

        void FillRgbPixels(int x, int y, int w, int h, int[] pixels, int off, int scansize);

        System.Drawing.Bitmap ToImage(RotateFlipType rotation = RotateFlipType.Rotate180FlipX);

    }
}
