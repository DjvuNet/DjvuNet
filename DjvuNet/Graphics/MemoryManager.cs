using System.Buffers;
using DjvuNet.Diagnostics;

namespace DjvuNet.Graphics
{
    internal sealed class TracedArrayPool<T> : ArrayPool<T>
    {
        private readonly ArrayPool<T> _inner;
        private readonly string _poolName;
        private readonly string _rentSite;
        private readonly string _returnSite;
        
        private static readonly bool _isBytePool = typeof(T) == typeof(byte);

        public TracedArrayPool(ArrayPool<T> inner, string poolName)
        {
            _inner = inner;
            _poolName = poolName;
            _rentSite = poolName + "_Rent";
            _returnSite = poolName + "_Return";
        }

        public override T[] Rent(int minimumLength)
        {
            return Rent(minimumLength, _rentSite);
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            Return(array, _returnSite, clearArray);
        }

        public T[] Rent(int minimumLength, string callSite)
        {
            var arr = _inner.Rent(minimumLength);
            // We currently only trace byte arrays natively in DjvuEventSource
            if (_isBytePool)
            {
                GraphicsEventSource.Log.OnRleDataRented(arr.Length, callSite);
            }
            return arr;
        }

        public void Return(T[] array, string callSite, bool clearArray = false)
        {
            if (array != null)
            {
                if (_isBytePool)
                {
                    GraphicsEventSource.Log.OnRleDataReturned(array.Length, callSite);
                }
                _inner.Return(array, clearArray);
            }
        }
    }

    /// <summary>
    /// Centralized memory pool manager for DjvuNet.Graphics allocating ultra-large
    /// intermediary buffers that bypass the limitations of ArrayPool{T}.Shared.
    /// </summary>
    internal static class MemoryManager
    {
        /// <summary>
        /// Dedicated ArrayPool tuned specifically for RLE compression pipelines, 
        /// peaking at 8 MB to prevent fallback GC thrashing on 4K images while keeping memory footprint lean.
        /// </summary>
        public static readonly TracedArrayPool<byte> RlePool = 
            new TracedArrayPool<byte>(
                ArrayPool<byte>.Create(maxArrayLength: 1024 * 1024 * 8, maxArraysPerBucket: 8), 
                "RlePool");
    }
}
