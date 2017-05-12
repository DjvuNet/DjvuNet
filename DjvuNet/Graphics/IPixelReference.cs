using System;

namespace DjvuNet.Graphics
{
    public interface IPixelReference : IPixel, IEquatable<IPixelReference>
    {
        int ColorNumber { get; }

        int RedOffset { get; }

        int GreenOffset { get; }

        int BlueOffset { get; }

        IMap2 Parent { get; }

        int Offset { get; }

        void FillRgbPixels(int x, int y, int w, int h, int[] pixels, int off, int scansize);

        void IncOffset();

        void IncOffset(int offset);

        void SetOffset(int offset);

        void SetOffset(int row, int column);

        void SetPixels(IPixelReference source, int length);

        IPixel ToPixel();

        void Ycc2Rgb(int count);
    }
}