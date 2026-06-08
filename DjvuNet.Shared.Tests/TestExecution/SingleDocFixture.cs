using System;

namespace DjvuNet.Tests
{
    /// <summary>
    /// Abstract base fixture for caching a single DjvuDocument instance for tests.
    /// This prevents high memory consumption that a global document cache would cause,
    /// while still avoiding loading the same document repeatedly within a single test class.
    /// </summary>
    public abstract class SingleDocFixture : IDisposable
    {
        public DjvuDocument Document { get; protected set; }

        protected SingleDocFixture(int fileIndex)
        {
            Document = new DjvuDocument(Util.GetTestFilePath(fileIndex));
        }

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    Document?.Dispose();
                    Document = null; // Release object graph to prevent GC pinning
                }
                
                // Clean up native resources (none here)
                _disposed = true;
            }
        }
    }
}