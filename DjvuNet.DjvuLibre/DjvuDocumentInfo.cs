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
        //private NativeMethods.DjvuMessageCallbackDelegate _MessageCallbackDelegate;
        //private NativeMethods.DjvuMessageCallbackDelegate _PreviousMessageCallback;

        private DjvuDocumentInfo(IntPtr context, IntPtr document, string filePath)
        {
            Context = context;
            Document = document;
            Path = filePath;

            //_MessageCallbackDelegate =
            //    new NativeMethods.DjvuMessageCallbackDelegate(this.DjvuMessageCallback);
            //IntPtr callback = Marshal.GetFunctionPointerForDelegate(_MessageCallbackDelegate);
            //NativeMethods.DjvuSetMessageCallback(Context, callback, IntPtr.Zero);

            DocumentType = NativeMethods.GetDjvuDocumentType(Document);
            PageCount = NativeMethods.GetDjvuDocumentPageCount(Document);
            FileCount = NativeMethods.GetDjvuDocumentFileCount(Document);
        }

        public static DjvuDocumentInfo CreateDjvuDocumentInfo(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File was not found - path: {filePath}");
            }

            Process proc = Process.GetCurrentProcess();
            string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            string programName = $"{proc.ProcessName}:{proc.Id:0000#}:{fileName}:{DateTime.UtcNow}";

            IntPtr context = IntPtr.Zero;
            IntPtr document = IntPtr.Zero;

            try
            {

                context = NativeMethods.CreateDjvuContext(programName);
                if (context == IntPtr.Zero)
                {
                    throw new ApplicationException("Failed to create native ddjvu_context object");
                }

                document = NativeMethods.LoadDjvuDocument(context, filePath, 1);
                if (document == IntPtr.Zero)
                {
                    throw new ApplicationException($"Failed to open native djvu_document: {filePath}");
                }

                DjvuJobStatus status = DjvuJobStatus.NotStarted;
                while (true)
                {
                    status = NativeMethods.GetDjvuJobStatus(document);
                    if ((int)status >= 2)
                    {
                        break;
                    }
                    else
                    {
                        ProcessMessage(context, true);
                    }
                }

                if (status == DjvuJobStatus.OK)
                {
                    return new DjvuDocumentInfo(context, document, filePath);
                }
                else if (status == DjvuJobStatus.Failed)
                {
                    throw new ApplicationException($"Failed to parse document: {filePath}");
                }
                else if (status == DjvuJobStatus.Stopped)
                {
                    throw new ApplicationException(
                       $"Parsing of DjVu document interrupted by user. Document path: {filePath}");
                }

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
            {
                return;
            }

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

            //if (_MessageCallbackDelegate != null)
            //{
            //    _MessageCallbackDelegate = null;
            //}

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

        internal static List<object> ProcessMessage(IntPtr context, bool wait = true)
        {
            if (context == IntPtr.Zero)
            {
                return null;
            }

            IntPtr message = IntPtr.Zero;

            // Wait for message
            if (wait)
            {
                message = NativeMethods.DjvuWaitMessage(context);
            }

            List<object> messages = new List<object>();
            // Pop all messages currently in the queue
            message = NativeMethods.DjvuPeekMessage(context);
            while(message != IntPtr.Zero)
            {
                var djvuMsg = new DjvuMessage(message);
                switch (djvuMsg.Any.Tag)
                {
                    case MessageTag.DocInfo:
                        messages.Add(djvuMsg.DocInfo);
                        break;
                    case MessageTag.Error:
                        messages.Add(djvuMsg.Error);
                        throw new ApplicationException("DjvuLibre error");
                    case MessageTag.Chunk:
                        messages.Add(djvuMsg.Chunk);
                        break;
                    case MessageTag.Info:
                        messages.Add(djvuMsg.Info);
                        break;
                    case MessageTag.PageInfo:
                        messages.Add(djvuMsg.PageInfo);
                        break;
                    case MessageTag.Progress:
                        messages.Add(djvuMsg.Progress);
                        break;
                    case MessageTag.NewStream:
                        messages.Add(djvuMsg.NewStream);
                        break;
                    case MessageTag.Display:
                        messages.Add(djvuMsg.Display);
                        break;
                    case MessageTag.Layout:
                        messages.Add(djvuMsg.Layout);
                        break;
                }
                NativeMethods.DjvuPopMessage(context);
                message = NativeMethods.DjvuPeekMessage(context);
            }

            return messages;
        }

        public virtual DocumentType DocumentType { get; protected set; }

        public virtual int PageCount { get; protected set; }

        public virtual int FileCount { get; protected set; }

        public virtual IntPtr Document { get; protected set; }

        public virtual IntPtr Context { get; protected set; }

        public virtual String Path { get; protected set; }

        public PageInfo GetPageInfo(int pageNumber)
        {
            if (pageNumber < 0 || pageNumber >= PageCount)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber));
            }

            PageInfo info = null;
            int status = 0;
            int size = Marshal.SizeOf<PageInfo>();
            IntPtr buffer = Marshal.AllocHGlobal(size);

            while (true)
            {
                status = NativeMethods.GetDjvuDocumentPageInfo(Document, pageNumber, buffer, size);
                if (status >= 2)
                {
                    break;
                }
                else
                {
                    ProcessMessage(Context, true);
                }
            }

            if (status == 2)
            {
                info = Marshal.PtrToStructure<PageInfo>(buffer);
                Marshal.FreeHGlobal(buffer);
                return info;
            }
            else
            {
                throw new ApplicationException($"Failed to get PageInfo for page number: {pageNumber}");
            }
        }

        public DjvuFileInfo GetFileInfo(int fileNumber)
        {
            if (fileNumber < 0 || fileNumber >= FileCount)
            {
                throw new ArgumentOutOfRangeException(nameof(fileNumber));
            }

            DjvuFileInfo info = null;
            int status = 0;
            int size = Marshal.SizeOf<DjvuFileInfo>();
            IntPtr buffer = Marshal.AllocHGlobal(size);

            status = NativeMethods.GetDjvuDocumentFileInfo(Document, fileNumber, buffer, size);

            // DjvuDocumentInfo is not initialized
            if (status < 2)
            {
                return null;
            }

            if (status == 2)
            {
                info = DjvuFileInfo.GetDjvuFileInfo(buffer);
                Marshal.FreeHGlobal(buffer);
                return info;
            }
            else
            {
                throw new ApplicationException($"Failed to get PageInfo for page number: {fileNumber}");
            }
        }

        public string DumpDocumentData(bool json = true)
        {
            return NativeMethods.GetDjvuDocumentDump(Document, json);
        }

        public string DumpPageData(int pageNumber, bool json = true)
        {
            return NativeMethods.GetDjvuDocumentPageDump(Document, pageNumber, json);
        }

        public string DumpFileData(int fileNumber, bool json = true)
        {
            return NativeMethods.GetDjvuDocumentFileDump(Document, fileNumber, json);
        }

        public string GetDocumentAnnotation()
        {
            IntPtr miniexp = IntPtr.Zero;
            try
            {
                miniexp = NativeMethods.GetDjvuDocumentAnnotation(Document, 1);
                if (miniexp != IntPtr.Zero)
                {
                    // TODO - improve data extraction from miniexp - only part is recovered now
                    string result = DjvuPageInfo.ExtractTextFromMiniexp(Document, miniexp);
                    if (result == null)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return result;
                    }
                }
                return null;
            }
            finally
            {
                if (miniexp != IntPtr.Zero)
                {
                    NativeMethods.ReleaseDjvuMiniexp(Document, miniexp);
                }
            }
        }

        public string GetPageText(int pageNumber)
        {
            return string.Empty;
        }
    }
}
