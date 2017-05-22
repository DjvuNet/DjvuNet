using System.Collections;
using System.Drawing;

namespace DjvuNet.Graphics
{
    public interface IMap
    {
        int BlueOffset { get; set; }

        int BytesPerPixel { get; set; }

        sbyte[] Data { get; set; }

        int GreenOffset { get; set; }

        int Height { get; set; }

        int Width { get; set; }

        bool IsRampNeeded { get; set; }

        Hashtable Properties { get; }

        int RedOffset { get; set; }

        IPixelReference CreateGPixelReference(int offset);

        IPixelReference CreateGPixelReference(int row, int column);

        void FillRgbPixels(int x, int y, int w, int h, int[] pixels, int off, int scansize);

        System.Drawing.Bitmap ToImage(RotateFlipType rotation = RotateFlipType.Rotate180FlipX);

    }
}