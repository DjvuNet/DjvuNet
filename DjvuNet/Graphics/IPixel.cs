using System;

namespace DjvuNet.Graphics
{
    public interface IPixel : IEquatable<IPixel>
    {
        sbyte Blue { get; set; }

        sbyte Green { get; set; }

        sbyte Red { get; set; }

        void CopyFrom(IPixel pixel);

        IPixel Duplicate();

        int GetHashCode();

        void SetBGR(int color);

        void SetBGR(int blue, int green, int red);

        void SetGray(sbyte gray);

        string ToString();
    }
}