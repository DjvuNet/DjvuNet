using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using DjvuNet;
using DjvuNet.Graphics;

namespace DjvuNet.Tests
{
    public static partial class Util
    {
        public const int MaxTestBufferSize = 4194304;
        public static readonly sbyte[] SharedSourceBuffer = GC.AllocateUninitializedArray<sbyte>(MaxTestBufferSize, pinned: true);
        public static readonly sbyte[] SharedTargetBuffer = GC.AllocateUninitializedArray<sbyte>(MaxTestBufferSize, pinned: true);
        public static readonly sbyte[] SharedScalarBuffer = GC.AllocateUninitializedArray<sbyte>(MaxTestBufferSize, pinned: true);
        public static readonly sbyte[] SharedRandomData = GenerateSharedRandomData(MaxTestBufferSize);

        public static sbyte[] GenerateSharedRandomData(int size)
        {
            sbyte[] arr = GC.AllocateUninitializedArray<sbyte>(size, pinned: true);
            Span<byte> byteSpan = MemoryMarshal.Cast<sbyte, byte>(arr.AsSpan());
            new Random(42).NextBytes(byteSpan);

            unsafe
            {
                fixed (sbyte* ptr = arr)
                {
                    if (Vector512.IsHardwareAccelerated)
                    {
                        var mask = Vector512.Create((sbyte)1);
                        for (int i = 0; i < size; i += 128)
                        {
                            Vector512.Store(Vector512.Load(ptr + i) & mask, ptr + i);
                            Vector512.Store(Vector512.Load(ptr + i + 64) & mask, ptr + i + 64);
                        }
                    }
                    else if (Vector256.IsHardwareAccelerated)
                    {
                        var mask = Vector256.Create((sbyte)1);
                        for (int i = 0; i < size; i += 128)
                        {
                            Vector256.Store(Vector256.Load(ptr + i) & mask, ptr + i);
                            Vector256.Store(Vector256.Load(ptr + i + 32) & mask, ptr + i + 32);
                            Vector256.Store(Vector256.Load(ptr + i + 64) & mask, ptr + i + 64);
                            Vector256.Store(Vector256.Load(ptr + i + 96) & mask, ptr + i + 96);
                        }
                    }
                    else if (Vector128.IsHardwareAccelerated)
                    {
                        var mask = Vector128.Create((sbyte)1);
                        for (int i = 0; i < size; i += 64)
                        {
                            Vector128.Store(Vector128.Load(ptr + i) & mask, ptr + i);
                            Vector128.Store(Vector128.Load(ptr + i + 16) & mask, ptr + i + 16);
                            Vector128.Store(Vector128.Load(ptr + i + 32) & mask, ptr + i + 32);
                            Vector128.Store(Vector128.Load(ptr + i + 48) & mask, ptr + i + 48);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < size; i++)
                            ptr[i] &= 1;
                    }
                }
            }
            return arr;
        }

        public static unsafe void ZeroMemorySimd(sbyte* ptr, int length)
        {
            if (length == 0)
                return;
            int offset = 0;
            if (Vector512.IsHardwareAccelerated && length >= 64)
            {
                var zero = Vector512<sbyte>.Zero;
                for (int lenMinus64 = length - 64; offset <= lenMinus64; offset += 64)
                    Vector512.Store(zero, ptr + offset);
            }
            else if (Vector256.IsHardwareAccelerated && length >= 32)
            {
                var zero = Vector256<sbyte>.Zero;
                for (int lenMinus32 = length - 32; offset <= lenMinus32; offset += 32)
                    Vector256.Store(zero, ptr + offset);
            }
            else if (Vector128.IsHardwareAccelerated && length >= 16)
            {
                var zero = Vector128<sbyte>.Zero;
                for (int lenMinus16 = length - 16; offset <= lenMinus16; offset += 16)
                    Vector128.Store(zero, ptr + offset);
            }
            for (; offset < length; offset++)
                ptr[offset] = 0;
        }

        public static void InitSharedBitmap(ref Bitmap bitmap, sbyte[] pinnedBuffer, int width, int height, int border)
        {
            ref BitmapSurrogate surrogate = ref Unsafe.As<Bitmap, BitmapSurrogate>(ref bitmap);
            surrogate._Width = width;
            surrogate._Height = height;
            surrogate._Border = border;
            surrogate._BytesPerRow = width + border;
            surrogate._MaxRowOffset = (height * surrogate._BytesPerRow) + border;
            surrogate._IsDisposed = 0;
            surrogate._Grays = 2;
            surrogate._Data = pinnedBuffer;
            surrogate._RleData = null;
        }

        public static void PrepareTestBitmap(ref Bitmap bmp, sbyte[] buffer, int width, int height, int border)
        {
            InitSharedBitmap(ref bmp, buffer, width, height, border);
            int bytesPerRow = width + border;

            unsafe
            {
                fixed (sbyte* dstPtr = buffer)
                fixed (sbyte* srcPtr = SharedRandomData)
                {
                    if (border > 0)
                    {
                        ZeroMemorySimd(dstPtr, border); // Initial left/top border cap
                    }

                    int randomOffset = 0;
                    for (int y = 0; y < height; y++)
                    {
                        sbyte* dstRow = dstPtr + bmp.RowOffset(y);
                        sbyte* srcRow = srcPtr + randomOffset;

                        // 1. Vectorized sequential image copying chunk block
                        int offset = 0;
                        if (Vector512.IsHardwareAccelerated && width >= 64)
                        {
                            int widthMinus64 = width - 64;
                            for (; offset <= widthMinus64; offset += 64)
                                Vector512.Store(Vector512.Load(srcRow + offset), dstRow + offset);
                        }
                        else if (Vector256.IsHardwareAccelerated && width >= 32)
                        {
                            int widthMinus32 = width - 32;
                            for (; offset <= widthMinus32; offset += 32)
                                Vector256.Store(Vector256.Load(srcRow + offset), dstRow + offset);
                        }
                        else if (Vector128.IsHardwareAccelerated && width >= 16)
                        {
                            int widthMinus16 = width - 16;
                            for (; offset <= widthMinus16; offset += 16)
                                Vector128.Store(Vector128.Load(srcRow + offset), dstRow + offset);
                        }

                        // 2. Zero the tail end of the buffer & 3. Restore valid pixels
                        int copyStart = offset;
                        if (border > 0)
                        {
                            if (Vector128.IsHardwareAccelerated && bytesPerRow >= 16)
                            {
                                int zeroStart = bytesPerRow - 16;
                                Vector128.Store(Vector128<sbyte>.Zero, dstRow + zeroStart);
                                copyStart = zeroStart < offset ? zeroStart : offset;
                            }
                            else
                            {
                                for (int i = 0; i < border; i++)
                                    dstRow[width + i] = 0;
                            }
                        }

                        int remaining = width - copyStart;
                        if (remaining > 0)
                        {
                            new Span<sbyte>(srcRow + copyStart, remaining).CopyTo(new Span<sbyte>(dstRow + copyStart, remaining));
                        }

                        randomOffset += width;
                    }
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BitmapSurrogate
    {
        public int _Width;
        public int _Height;
        public int _Border;
        public int _MaxRowOffset;
        public int _BytesPerRow;
        public byte _IsDisposed;
        public byte _Grays;
        public sbyte[] _Data;
        public byte[] _RleData;
    }
}
