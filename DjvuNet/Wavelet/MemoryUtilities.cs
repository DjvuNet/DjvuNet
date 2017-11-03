using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Wavelet
{
    internal static class MemoryUtilities
    {
        public static unsafe void MoveMemory(void* dest, void* src, long length)
        {
            Buffer.MemoryCopy(src, dest, length, length);
        }

        public static unsafe void MoveMemory(IntPtr dest, IntPtr src, long length)
        {
            Buffer.MemoryCopy((void*)src, (void*)dest, length, length);
        }

        private unsafe delegate void MemCopyDelegate(void* dest, void* src, long length);

    }
}
