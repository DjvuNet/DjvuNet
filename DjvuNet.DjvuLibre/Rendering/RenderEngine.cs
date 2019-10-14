using System;

namespace DjvuNet.DjvuLibre
{
    public class RenderEngine : IRenderEngine, IDisposable
    {
        private IntPtr _PageNative;
        private DjvuPageInfo _Page;

        public RenderEngine(DjvuPageInfo page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            if (page.Page == IntPtr.Zero)
                throw new ArgumentException("Invalid native Page pointer.", nameof(page));

            _Page = page;
            _PageNative = page.Page;
        }

        #region IDisposable implementation

        protected bool _Disposed;

        public bool Disposed { get { return _Disposed; } }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (_Disposed)
                return;

            if (disposing)
            {
            }

            _Disposed = true;
        }

        ~RenderEngine()
        {
            Dispose(false);
        }

        #endregion IDisposable implementation


        public int X
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int Y
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int Width
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int Height
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int Dpi
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public FormatStyle FormatStyle
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public RenderMode Mode
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public IntPtr Buffer
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public void CreateBuffer()
        {
            throw new NotImplementedException();
        }

        public IntPtr Render()
        {
            throw new NotImplementedException();
        }

        public void ReleaseBuffer()
        {
            throw new NotImplementedException();
        }

    }
}
