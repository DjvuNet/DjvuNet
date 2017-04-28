using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.DjvuLibre
{
    public static class DjvuMarshal
    {
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_alloc",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern IntPtr AllocHGlobal(uint size);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_free",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void FreeHGlobal(IntPtr block);

    }
}
