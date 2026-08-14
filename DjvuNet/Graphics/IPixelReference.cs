using System;

namespace DjvuNet.Graphics
{
    public interface IPixelReference : IPixel, IEquatable<IPixelReference>
    {
        int ColorNumber { get; }

        int RedOffset { get; }

        int GreenOffset { get; }

        int BlueOffset { get; }

        PixelMap Parent { get; }

        int Offset { get; }


        void IncOffset();

        void IncOffset(int offset);

        void SetOffset(int offset);

        void SetOffset(int row, int column);

        void SetPixels(IPixelReference source, int length);

        IPixel ToPixel();
    }
}
