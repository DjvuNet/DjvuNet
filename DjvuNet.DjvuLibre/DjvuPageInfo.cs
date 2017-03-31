using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.DjvuLibre
{
    public class DjvuPageInfo : IDisposable
    {
        private DjvuDocumentInfo _DocumentInfo;
        private PageInfo _Info;
        
        public DjvuPageInfo(DjvuDocumentInfo docInfo, int pageNumber)
        {
            if (docInfo == null)
                throw new ArgumentNullException(nameof(docInfo));

            if ((_Info = docInfo.GetPageInfo(pageNumber)) == null)
                throw new ArgumentException(
                    $"{nameof(DjvuDocumentInfo)} object {nameof(docInfo)} is not initialized properly.", 
                    nameof(docInfo));

            if (pageNumber < 0 || pageNumber >= docInfo.PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageNumber));

            _DocumentInfo = docInfo;

            Page = NativeMethods.GetDjvuDocumentPage(_DocumentInfo.Document, pageNumber);

            DjvuJobStatus status = DjvuJobStatus.NotStarted;
            List<object> messagesList = new List<object>();
            while (true)
            {
                status = NativeMethods.GetDjvuJobStatus(Page);
                if ((int)status >= 2)
                    break;
                else
                    messagesList.AddRange(DjvuDocumentInfo.ProcessMessage(docInfo.Context, true));
            }

            if (status == DjvuJobStatus.Failed)
                throw new ApplicationException($"Failed to parse document page: {pageNumber}");
            else if (status == DjvuJobStatus.Stopped)
                throw new ApplicationException(
                    $"Parsing of DjVu document page interrupted by user. Page number: {pageNumber}");

            if (_Info != null)
            {
                Width = _Info.Width;
                Height = _Info.Height;
                Dpi = _Info.Dpi;
                Version = _Info.Version;
                Rotation = _Info.Rotation;
            }
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

            if (Page != IntPtr.Zero)
            {
                NativeMethods.ReleaseDjvuDocument(Page);
                Page = IntPtr.Zero;
            }

            _Disposed = true;
        }

        ~DjvuPageInfo()
        {
            Dispose(false);
        }

        #endregion IDisposable implementation

        public IntPtr Page { get; protected set; }

        public int Width { get; protected set; }

        public int Height { get; protected set; }

        public int Dpi { get; protected set; }

        public int Rotation { get; protected set; }

        public int Version { get; protected set; }

        public PageType GetPageType()
        {
            return NativeMethods.GetDjvuPageType(Page);
        }

    }
}
