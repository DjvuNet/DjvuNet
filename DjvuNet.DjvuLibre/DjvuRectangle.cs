using System;
using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DjvuRectangle
    {
        public int X;
        public int Y;
        public uint Width;
        public uint Height;
    }
}
