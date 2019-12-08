using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Bitmap = System.Drawing.Bitmap;
using Rectangle = System.Drawing.Rectangle;
using DjvuNet;
using DjvuNet.Graphics;
using System.ComponentModel;

namespace DjvuNet.Drawing
{
    public class DjvuPageVisuals : IDisposable
    {
        private DjvuPage _Page;
        private Bitmap _Image;
        private Bitmap _ThumbnailImage;
        private bool _IsInverted;

        public DjvuPageVisuals(DjvuPage page)
        {
            _Page = page ?? throw new ArgumentNullException(nameof(page));
        }

        #region IDisposable implementation

        protected bool _Disposed;

        public bool Disposed { get { return _Disposed; } }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
            {
                return;
            }

            if (disposing)
            {
                _Image?.Dispose();
                _Image = null;

                _ThumbnailImage?.Dispose();
                _ThumbnailImage = null;
            }

            _Disposed = true;
        }

        ~DjvuPageVisuals()
        {
            Dispose(false);
        }

        #endregion IDisposable implementation

        public event PropertyChangedEventHandler PropertyChanged;

        public DjvuPage Page
        {
            get { return _Page; }
            private set
            {
                if (_Page != value)
                {
                    _Page = value;
                    OnPropertyChanged(nameof(Page));
                }
            }
        }

        public Bitmap Image
        {
            get
            {
                if (_Image != null)
                {
                    return _Image;
                }

                _Image = this.BuildPageImage();
                OnPropertyChanged(nameof(Image));
                return _Image;
            }
        }

        /// <summary>
        /// Gets or sets the thumbnail image for the page
        /// </summary>-+
        public System.Drawing.Bitmap ThumbnailImage
        {
            get { return _ThumbnailImage; }

            set
            {
                if (ThumbnailImage != value)
                {
                    _ThumbnailImage = value;
                    OnPropertyChanged(nameof(ThumbnailImage));
                }
            }
        }

        /// <summary>
        /// True if the image is inverted, false otherwise
        /// </summary>
        public bool IsInverted
        {
            get { return _IsInverted; }

            set
            {
                if (_IsInverted != value)
                {
                    _IsInverted = value;
                    OnPropertyChanged(nameof(IsInverted));
                }
            }
        }

        /// <summary>
        /// Sends the property changed notification
        /// </summary>
        /// <param name="property"></param>
        protected void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
