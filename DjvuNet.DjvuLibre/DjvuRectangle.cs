using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
