using System;
using System.Threading;

namespace DjvuNet.DjvuLibre.Tests
{
    /// <summary>
    /// Exception-safe guard that temporarily mutates native JB2 encoding options 
    /// and guarantees restoration upon disposal, even if test assertions fail.
    /// </summary>
    public sealed class Jb2EncodingContext : IDisposable
    {
        private readonly int _origAll;
        private readonly int _origShared;
        private readonly int _origMarks;

        // Static lock to prevent xUnit parallel test runners from corrupting global C++ state
        private static readonly object _syncLock = new object();

        public Jb2EncodingContext(int containsAll, int containsShared, int containsMarks)
        {
            Monitor.Enter(_syncLock);
            try
            {
                // 1. Capture the original state securely
                NativeMethods.GetJb2EncodingOptions(out _origAll, out _origShared, out _origMarks);
                
                // 2. Apply the requested test state
                NativeMethods.SetJb2EncodingOptions(containsAll, containsShared, containsMarks);
            }
            catch
            {
                Monitor.Exit(_syncLock);
                throw;
            }
        }

        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed) return;
            try
            {
                // 3. Guarantee restoration regardless of test failure
                NativeMethods.SetJb2EncodingOptions(_origAll, _origShared, _origMarks);
            }
            finally
            {
                Monitor.Exit(_syncLock);
                _isDisposed = true;
            }
        }
    }
}
