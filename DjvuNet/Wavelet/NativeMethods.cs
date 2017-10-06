using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Wavelet
{
    internal static class NativeMethods
    {
        public static unsafe void MoveMemory(void* dest, void* src, long length)
        {
            //CopyMemory(dest, src, (uint) length);
            Buffer.MemoryCopy(src, dest, length, length);
        }

        public static unsafe void MoveMemory(IntPtr dest, IntPtr src, long length)
        {
            //CopyMemory((void*) dest, (void*) src, (uint) length);
            Buffer.MemoryCopy((void*)src, (void*)dest, length, length);
        }

        private unsafe delegate void MemCopyDelegate(void* dest, void* src, long length);

        // [DllImport("NtDll.dll", EntryPoint = "RtlMoveMemory")]
        // private unsafe static extern void CopyMemory(void* dest, void* src, uint Length);
    }
}
