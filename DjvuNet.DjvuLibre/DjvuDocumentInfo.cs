using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.DjvuLibre
{
    public class DjvuDocumentInfo : IDisposable
    {
        private NativeMethods.DjvuMessageCallbackDelegate _MessageCallbackDelegate;
        //private NativeMethods.DjvuMessageCallbackDelegate _PreviousMessageCallback;

        private DjvuDocumentInfo(IntPtr context, IntPtr document, string filePath)
        {
            Context = context;
            Document = document;
            Path = filePath;

            _MessageCallbackDelegate = 
                new NativeMethods.DjvuMessageCallbackDelegate(this.DjvuMessageCallback);

            IntPtr callback = Marshal.GetFunctionPointerForDelegate(_MessageCallbackDelegate);

            NativeMethods.DjvuSetMessageCallback(Context, callback, IntPtr.Zero);
            DocumentType = NativeMethods.GetDjvuDocumentType(Document);
            PageCount = NativeMethods.GetDjvuDocumentPageCount(Document);
        }

        public static DjvuDocumentInfo CreateDjvuDocumentInfo(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            if (String.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File was not found - path: {filePath}");

            Process proc = Process.GetCurrentProcess();
            string programName = $"{proc.ProcessName}{proc.Id:0000#}";

            IntPtr context = IntPtr.Zero;
            IntPtr document = IntPtr.Zero;

            try
            {

                context = NativeMethods.CreateDjvuContext(programName);
                if (context == IntPtr.Zero)
                    throw new ApplicationException("Failed to create native ddjvu_context object");

                document = NativeMethods.GetDjvuDocument(context, filePath, 1);
                if (document == IntPtr.Zero)
                    throw new ApplicationException($"Failed to open native djvu_document: {filePath}");

                int status = 0;
                while (true)
                {
                    status = NativeMethods.GetDjvuJobStatus(document);
                    if (status >= 2)
                        break;
                    else
                        ProcessMessage(context, true);
                }

                if (status == 2)
                    return new DjvuDocumentInfo(context, document, filePath);
                else if (status == 3)
                    throw new ApplicationException($"Failed to parse document: {filePath}");
                else if (status == 4)
                    throw new ApplicationException(
                        $"Parsing of DjVu document interrupted by user. Document path: {filePath}");

                throw new ApplicationException($"Unknown error: \"{status}\" while processing document: {filePath}");
            }
            catch(Exception error)
            {
                if (document != IntPtr.Zero)
                {
                    NativeMethods.ReleaseDjvuDocument(document);
                    document = IntPtr.Zero;
                }

                if (context != IntPtr.Zero)
                {
                    NativeMethods.ReleaseDjvuContext(context);
                    context = IntPtr.Zero;
                }

                throw new AggregateException($"Failed to create DjvuDocumenyInfo from file {filePath}", error);
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

            if (Document != IntPtr.Zero)
            {
                NativeMethods.ReleaseDjvuDocument(Document);
                Document = IntPtr.Zero;
            }

            if (Context != IntPtr.Zero)
            {
                NativeMethods.ReleaseDjvuContext(Context);
                Context = IntPtr.Zero;
            }

            if (_MessageCallbackDelegate != null)
            {
                _MessageCallbackDelegate = null;
            }

            _Disposed = true;
        }

        ~DjvuDocumentInfo()
        {
            Dispose(false);
        }

        #endregion IDisposable implementation

        protected void DjvuMessageCallback(IntPtr context, IntPtr closure)
        {
            ProcessMessage(context, true);
        }

        protected static void ProcessMessage(IntPtr context, bool wait = true)
        {
            if (context == IntPtr.Zero)
                throw new ArgumentException(nameof(context));

            IntPtr message = IntPtr.Zero;

            // Wait for message
            if (wait)
                message = NativeMethods.DjvuWaitMessage(context);

            // Pop all messages currently in the queue
            message = NativeMethods.DjvuPeekMessage(context);
            while(message != IntPtr.Zero)
            {
                var djvuMsg = Marshal.PtrToStructure<DjvuMessage>(message);
                switch (djvuMsg.DocInfo.Tag)
                {
                    case MessageTag.DocInfo:
                        break;
                    case MessageTag.Error:
                        throw new ApplicationException("DjvuLibre error");
                    case MessageTag.Chunk:
                        break;
                    case MessageTag.Info:
                        break;
                    case MessageTag.PageInfo:
                        break;
                    case MessageTag.Progress:
                        break;
                    case MessageTag.NewStream:
                        break;
                    case MessageTag.ReDisplay:
                        break;
                    case MessageTag.ReLayout:
                        break;
                }
                NativeMethods.DjvuPopMessage(context);
                message = NativeMethods.DjvuPeekMessage(context);
            }
        }

        public virtual DocumentType DocumentType { get; protected set; }

        public virtual int PageCount { get; protected set; }

        public virtual IntPtr Document { get; protected set; }

        public virtual IntPtr Context { get; protected set; }

        public virtual String Path { get; protected set; }


    }
}
