using System;
using System.Runtime.InteropServices;
using DjvuNet.Graphics;

namespace DjvuNet.DjvuLibre
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DjvuRectangle
    {
        public int X;
        public int Y;
        public uint Width;
        public uint Height;

        public static implicit operator Rectangle(DjvuRectangle rect)
        {
            return new Rectangle(rect.X, rect.Y, (int)rect.Width, (int)rect.Height);
        }

        public static implicit operator DjvuRectangle(Rectangle rect)
        {
            return new DjvuRectangle
            {
                X = rect.XMin,
                Y = rect.YMin,
                Width = (uint)Math.Max(0, rect.Width),
                Height = (uint)Math.Max(0, rect.Height)
            };
        }
    }
}
