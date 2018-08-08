using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DjvuNet.DjvuLibre
{
    public class DjvuPageInfo : IDisposable
    {
        private DjvuDocumentInfo _DocumentInfo;
        private PageInfo _Info;
        private string _Text;
        private string _Annotation;
        private PageType _PageType;

        public DjvuPageInfo(DjvuDocumentInfo docInfo, int pageNumber)
        {
            if (docInfo == null)
                throw new ArgumentNullException(nameof(docInfo));

            if ((_Info = docInfo.GetPageInfo(pageNumber)) == null)
                throw new ArgumentException(
                    $"{nameof(DjvuDocumentInfo)} object {nameof(docInfo)} is not fully initialized.",
                    nameof(docInfo));

            if (pageNumber < 0 || pageNumber >= docInfo.PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageNumber));

            _DocumentInfo = docInfo;
            Number = pageNumber;

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

            PageType = NativeMethods.GetDjvuPageType(Page);
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

        public int Number { get; protected set; }

        public PageInfo PageInfo { get { return _Info; } }

        public PageType PageType
        {
            get
            {
                if (_PageType != PageType.Unknown)
                    return _PageType;
                else
                {
                    _PageType = NativeMethods.GetDjvuPageType(Page);
                    return _PageType;
                }
            }
            protected set
            {
                _PageType = value;
            }
        }

        public PageType GetPageType()
        {
            return NativeMethods.GetDjvuPageType(Page);
        }

        public string Text
        {
            get
            {
                if (_Text != null)
                    return _Text;
                else
                {
                    try
                    {
                        _Text = NativeMethods.GetDjvuDocumentPageTextUtf8(
                            _DocumentInfo.Document, Number, "word");
                    }
                    finally
                    {
                        // Avoid repeating search for null
                        if (_Text == null)
                            _Text = String.Empty;
                    }

                    return _Text;
                }
            }
        }

        public string Annotation
        {
            get
            {
                if (_Annotation != null)
                    return _Annotation;
                else
                {
                    IntPtr miniexp = IntPtr.Zero;
                    try
                    {
                        miniexp = NativeMethods.GetDjvuPageAnnotation(_DocumentInfo.Document, Number);
                        _Annotation = ExtractTextFromMiniexp(_DocumentInfo.Document, miniexp);
                    }
                    finally
                    {
                        if (_Annotation == null)
                            _Annotation = String.Empty;
                        if (miniexp != IntPtr.Zero)
                            NativeMethods.ReleaseDjvuMiniexp(_DocumentInfo.Document, miniexp);
                    }

                    return _Annotation;
                }

            }
        }

        internal static string ExtractTextFromMiniexp(IntPtr document, IntPtr miniexp, int count = 0)
        {
            string text = null;

            text = NativeMethods.GetMiniexpString(miniexp);
            if (!String.IsNullOrWhiteSpace(text))
                return text;

            if (count == 0)
                count = NativeMethods.MiniexpLength(miniexp);

            for (int i = 0; i < count; i++)
            {
                IntPtr element = IntPtr.Zero;
                try
                {
                    element = NativeMethods.MiniexpItem(i, miniexp);
                    int count2 = NativeMethods.MiniexpLength(element);

                    text = ExtractTextFromMiniexp(document, element, count2);
                    if (!String.IsNullOrWhiteSpace(text))
                        return text;

                    if (NativeMethods.IsMiniexpString(element))
                    {
                        text = NativeMethods.GetMiniexpString(miniexp);
                        if (!String.IsNullOrWhiteSpace(text))
                            return text;
                    }
                }
                finally
                {
                    if (element != IntPtr.Zero)
                        NativeMethods.ReleaseDjvuMiniexp(document, element);
                }
            }
            return text;
        }

        public IntPtr RenderPage(RenderMode mode)
        {
            DjvuRectangle targetRect = new DjvuRectangle
            {
                X = 0,
                Y = 0,
                Height = (uint)Height,
                Width = (uint)Width
            };

            return RenderPage(mode, ref targetRect);
        }

        public IntPtr RenderPage(RenderMode mode, ref DjvuRectangle targetRect)
        {
            IntPtr format = NativeMethods.CreateDjvuFormat(FormatStyle.BGR24, 0, IntPtr.Zero);
            NativeMethods.SetDjvuFormatRowOrder(format, 1);
            NativeMethods.SetDjvuFormatYDirection(format, 1);
            NativeMethods.SetDjvuFormatDitherBits(format, 24);

            DjvuRectangle pageRect = new DjvuRectangle
            {
                X = 0,
                Y = 0,
                Height = (uint) Height,
                Width = (uint) Width
            };

            IntPtr buffer = DjvuMarshal.AllocHGlobal((uint)(Width * 3 * Height));
            int result = 0;

            try
            {
                result = NativeMethods.RenderDjvuPage(Page, mode, ref pageRect, ref targetRect,
                    format, (uint)Width * 3, buffer);

                if (result == 0)
                    throw new DjvuLibreException(
                        $"Failed to render image at this time - result: {result}. Try later again.");
            }
            catch(Exception ex)
            {
                DjvuMarshal.FreeHGlobal(buffer);
                buffer = IntPtr.Zero;
                NativeMethods.ReleaseDjvuFormat(format);
                format = IntPtr.Zero;

                string message = "Error while rendering page. Check InnerException message for details.";

                if (ex is DjvuLibreException)
                    throw new DjvuLibreException(message, ex);

                throw new AggregateException(message, ex);
            }

            return buffer;
        }

    }
}
