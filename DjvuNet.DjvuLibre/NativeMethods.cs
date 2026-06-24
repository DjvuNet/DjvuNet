


using System;
using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{
    internal static class NativeMethods
    {
        public const string DjVuLibrePath = "../libdjvulibre";
        //  DDJVUAPI const char*
        //  ddjvu_get_version_string(void);

        /// <summary>
        /// Returns a string that described the underlying code.
        /// </summary>
        /// <returns>
        /// DjVuLibre library version string.
        /// </returns>
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_get_version_string", CallingConvention = CallingConvention.Cdecl, PreserveSig = true, ThrowOnUnmappableChar = true)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern string GetDjvuLibreVersion();

        /* -------------------------------------------------- */
        /* DDJVU_CONTEXT_T                                    */
        /* -------------------------------------------------- */

        //DDJVUAPI ddjvu_context_t *
        //ddjvu_context_create(const char* programname);

        /// <summary>
        /// Creates a ddjvu_context_t object. Argument programName is the name
        /// of the calling executable.
        /// There is usually only one ddjvu_context_t object.
        /// This object holds global data structures such as the
        /// cache of decoded pages, or the list of pending event messages.
        /// </summary>
        /// <param name="programName"></param>
        /// <returns>
        /// Pointer to the ddjvu_context_t.
        /// </returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_context_create", CallingConvention = CallingConvention.Cdecl, PreserveSig = true, ThrowOnUnmappableChar = true)]
        internal static extern IntPtr CreateDjvuContext(
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(UTF8StringMarshaler))]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
            string programName);

        /* ddjvu_context_release ---
           Release a reference to a <ddjvu_context_t> object.
           The calling program should no longer reference this object.
           The object itself will be destroyed as soon as no other object
           or thread needs it. */

        //DDJVUAPI void
        //ddjvu_context_release(ddjvu_context_t* context);

        /// <summary>
        /// Release a reference to a ddjvu_context_t object.
        /// The calling program should no longer reference this object.
        /// The object itself will be destroyed as soon as no other object
        /// or thread needs it.
        /// </summary>
        /// <param name="context"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_context_release", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void ReleaseDjvuContext(IntPtr context);

        /* ------- CACHE ------- */

        //  DDJVUAPI void
        //  ddjvu_cache_set_size(ddjvu_context_t* context,
        //                     unsigned long cachesize);

        /// <summary>
        /// Sets the maximum size of the cache of decoded page data.
        /// </summary>
        /// <param name="context">
        /// Pointer to valid context.
        /// </param>
        /// <param name="size">
        /// The argument is expressed in bytes.
        /// </param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_cache_set_size", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void DjvuSetCacheSize(IntPtr context, uint size);

        //  DDJVUAPI unsigned long
        //  ddjvu_cache_get_size(ddjvu_context_t* context);

        /// <summary>
        /// Returns the maximum size of the cache.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>
        /// Size of cache in bytes.
        /// </returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_cache_get_size", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern uint DjvuGetCacheSize(IntPtr context);

        //  DDJVUAPI void
        //  ddjvu_cache_clear(ddjvu_context_t* context);

        /// <summary>
        /// Clears all cached data.
        /// </summary>
        /// <param name="context"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_cache_clear", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void DjvuClearCache(IntPtr context);

        /* -------------------------------------------------- */
        /* DDJVU_DOCUMENT_T                                   */
        /* -------------------------------------------------- */

        /* ddjvu_document_create ---
           Creates a decoder for a DjVu document and starts
           decoding.  This function returns immediately.  The
           decoding job then generates messages to request the raw
           data and to indicate the state of the decoding process.

           Argument <url> specifies an optional URL for the document.
           The URL follows the usual syntax (<"protocol://machine/path">).
           It should not end with a slash. It only serves two purposes:
           - The URL is used as a key for the cache of decoded pages.
           - The URL is used to document <m_newstream> messages.

           Setting argument <cache> to <TRUE> indicates that decoded pages
           should be cached when possible.  This only works when
           argument <url> is not the null pointer.

           It is important to understand that the URL is not used to
           access the data.  The document generates <m_newstream>
           messages to indicate which data is needed.  The caller must
           then provide the raw data using <ddjvu_stream_write>
           and <ddjvu_stream_close>.

           Localized characters in argument <url> should be in
           urlencoded UTF-8 (like "%2A"). What is happening for non
           ascii characters is unclear (probably UTF-8). */

        //  DDJVUAPI ddjvu_document_t *
        //  ddjvu_document_create(ddjvu_context_t* context,
        //                      const char* url,
        //                      int cache);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_create", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr LoadDjvuDocumentUrl(
            IntPtr context,
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(UTF8StringMarshaler))]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
            string url,
            int cache);

        //  DDJVUAPI ddjvu_document_t *
        //  ddjvu_document_create_by_filename_utf8(ddjvu_context_t* context,
        //                                       const char* filename,
        //                                       int cache);

        /// <summary>
        /// Creates a document for a DjVu document stored in a file.
        /// The document will directly access the specified DjVu file
        /// or related files without generating m_newstream messages.
        /// This overload accepts Unicode international strings.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filePath">
        /// Path to file which may include international Unicode characters.
        /// </param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint= "ddjvu_document_create_by_filename_utf8", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr LoadDjvuDocumentUtf8(
            IntPtr context,
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(UTF8StringMarshaler))]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
            string filePath,
            int cache);

        //  DDJVUAPI ddjvu_document_t *
        //  ddjvu_document_create_by_filename(ddjvu_context_t* context,
        //                                  const char* filename,
        //                                  int cache);

        /// <summary>
        /// Creates a document for a DjVu document stored in a file.
        /// The document will directly access the specified DjVu file
        /// or related files without generating m_newstream messages.
        /// This overload accepts strings encoded in computer codepage.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filePath">
        /// Path to file which may include only system codepage characters.
        /// </param>
        /// <param name="cache"></param>
        /// <returns></returns>
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_create_by_filename", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, PreserveSig = true, ThrowOnUnmappableChar = true)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        internal static extern IntPtr LoadDjvuDocument(
            IntPtr context,
            // String in computer codepage encoding. Non codepage characters will be replaced by '?'.
            string filePath,
            int cache);

        /// <summary>
        /// Returns the number of pages in a DjVu document.
        /// This function might return 1 when called
        /// before receiving a m_docinfo message
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_pagenum", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int GetDjvuDocumentPageCount(IntPtr doc);


        //  DDJVUAPI ddjvu_document_type_t
        //  ddjvu_document_get_type(ddjvu_document_t* document);


        /// <summary>
        /// Returns the type of a DjVu document. This function might return DDJVU_DOCTYPE_UNKNOWN
        /// when called before receiving a m_docinfo message.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_type", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern DocumentType GetDjvuDocumentType(IntPtr doc);

        //  #define ddjvu_document_release(document) \
        //    ddjvu_job_release(ddjvu_document_job(document))

        /// <summary>
        /// Release a reference to a <ddjvu_document_t> object.
        /// The calling program should no longer reference this object.
        /// The object itself will be destroyed as soon as no other object
        /// or thread needs it.
        /// </summary>
        /// <param name="doc"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_job_release", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void ReleaseDjvuDocument(IntPtr doc);

        /* ------- MESSAGE QUEUE ------- */

        /* Messages produced by the ddjvu api accumulate into
           the message queue. Processing messages is not optional!
           A typical message handling routine looks like this:

           void handle_ddjvu_messages(ddjvu_context_t *ctx, int wait)
           {
             const ddjvu_message_t *msg;
             if (wait)
               ddjvu_message_wait(ctx);
             while ((msg = ddjvu_message_peek(ctx)))
             {
               switch(msg->m_any.tag)
               {
               case DDJVU_ERROR:      .... ; break;
               case DDJVU_INFO:       .... ; break;
               case DDJVU_NEWSTREAM:  .... ; break;
               ....
               default: break;
               }
               ddjvu_message_pop(ctx);
             }
           }
        */

        //  DDJVUAPI ddjvu_message_t *
        //  ddjvu_message_peek(ddjvu_context_t* context);

        /// <summary>
        /// Returns a pointer to the next DDJVU message.
        /// This function returns 0 if no message is available.
        /// It does not remove the message from the queue.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_message_peek", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr DjvuPeekMessage(IntPtr context);

        //  DDJVUAPI ddjvu_message_t *
        //  ddjvu_message_wait(ddjvu_context_t* context);

        /// <summary>
        /// Returns a pointer to the next DDJVU message.
        /// This function waits until a message is available.
        /// It does not remove the message from the queue.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_message_wait", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr DjvuWaitMessage(IntPtr context);

        //  DDJVUAPI void
        //  ddjvu_message_pop(ddjvu_context_t* context);

        /// <summary>
        /// Removes one message from the queue.
        /// This function must be called after processing the message.
        /// Pointers returned by previous calls to ddjvu_message_peek
        /// or ddjvu_message_wait are no longer valid after
        /// calling ddjvu_message_pop.
        /// </summary>
        /// <param name="context"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_message_pop", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void DjvuPopMessage(IntPtr context);

        //  typedef void
        //  (*ddjvu_message_callback_t)(ddjvu_context_t* context, void* closure);

        //  DDJVUAPI void
        //  ddjvu_message_set_callback(ddjvu_context_t* context,
        //                           ddjvu_message_callback_t callback,
        //                           void* closure);

        /// <summary>
        /// Defines a callback function invoked whenever
        /// a new message is posted to the ddjvuapi message queue,
        /// and returns a pointer to the previous callback function.
        /// This callback function can be called at any time
        /// while other code is executing. Do not call ddjvuapi
        /// functions from this callback. It should simply signal
        /// the main application event loop that new ddjvuapi messages
        /// are available. Under WIN32, this is usually achieved
        /// by posting a user window message.  Under UNIX, this is
        /// usually achieved using a pipe: the callback writes
        /// a single byte into the pipe; the main application loop
        /// monitors the reading end of the pipe and detects
        /// the presence of data.
        /// </summary>
        /// <param name="context">
        /// DjvuLibre API Context
        /// </param>
        /// <param name="closure">
        /// Custom arguments which will be passed to callback.
        /// One can set them while calling DjvuSetMessageCallback
        /// </param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DjvuMessageCallbackDelegate(IntPtr context, IntPtr closure);

        /// <summary>
        /// Defines a callback function invoked whenever
        /// a new message is posted to the ddjvuapi message queue,
        /// and returns a pointer to the previous callback function.
        /// This callback function can be called at any time
        /// while other code is executing.Do not call ddjvuapi
        /// functions from this callback.It should simply signal
        /// the main application event loop that new ddjvuapi messages
        /// are available.Under WIN32, this is usually achieved
        /// by posting a user window message.  Under UNIX, this is
        /// usually achieved using a pipe: the callback writes
        /// a single byte into the pipe; the main application loop
        /// monitors the reading end of the pipe and detects
        /// the presence of data.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="callback">
        /// Create DjvuMessageCallbackDelegate, get delegate pointer
        /// by calling Marshal.GetFunctionPointerForDelegate(yourDelegate)
        /// and pass it to the DjvuSetMessageCallback function along
        /// Context pointer and
        /// </param>
        /// <param name="closure"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_message_set_callback", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void DjvuSetMessageCallback(IntPtr context, IntPtr callback, IntPtr closure);

        /* -------------------------------------------------- */
        /* DDJVU_JOB_T                                        */
        /* -------------------------------------------------- */

        /* Many essential ddjvuapi functions initiate asynchronous operations.
           These "jobs" run in separate threads and report their
           progress by posting messages into the ddjvu context event queue.
           Jobs are sometimes represented by a ddjvu_job_t object. */

        /* ddjvu_job_status ---
           Returns the status of the specified job. */

        //  typedef enum {
        //    DDJVU_JOB_NOTSTARTED, /* operation was not even started */
        //    DDJVU_JOB_STARTED,    /* operation is in progress */
        //    DDJVU_JOB_OK,         /* operation terminated successfully */
        //    DDJVU_JOB_FAILED,     /* operation failed because of an error */
        //    DDJVU_JOB_STOPPED     /* operation was interrupted by user */
        //  }
        //  ddjvu_status_t;

        //  DDJVUAPI ddjvu_status_t
        //  ddjvu_job_status(ddjvu_job_t* job);

        //  #define ddjvu_job_done(job) \
        //    (ddjvu_job_status(job) >= DDJVU_JOB_OK)
        //  #define ddjvu_job_error(job) \
        //    (ddjvu_job_status(job) >= DDJVU_JOB_FAILED)

        /// <summary>
        /// Returns the status of the specified job.
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_job_status", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern DjvuJobStatus GetDjvuJobStatus(IntPtr job);

        //  #define ddjvu_document_decoding_status(document) \
        //   ddjvu_job_status(ddjvu_document_job(document))
        //  #define ddjvu_document_decoding_done(document) \
        //   (ddjvu_document_decoding_status(document) >= DDJVU_JOB_OK)
        //  #define ddjvu_document_decoding_error(document) \
        //   (ddjvu_document_decoding_status(document) >= DDJVU_JOB_FAILED)

        //  ddjvu_job_t*
        //  ddjvu_document_job(ddjvu_document_t* document)
        //  {
        //    return document;
        //  }

        //  ddjvu_status_t
        //  ddjvu_job_status(ddjvu_job_t* job)
        //  {
        //    G_TRY
        //    {
        //        if (!job)
        //            return DDJVU_JOB_NOTSTARTED;
        //        return job->status();
        //    }
        //    G_CATCH(ex)
        //    {
        //        ERROR1(job, ex);
        //    }
        //    G_ENDCATCH;
        //    return DDJVU_JOB_FAILED;
        //  }

        //  static ddjvu_document_t*
        //  ddjvu_document_create_by_filename_imp(ddjvu_context_t* ctx,
        //                                      const char* filename,
        //                                      int cache, int utf8)
        //  {
        //      ddjvu_document_t* d = 0;
        //        G_TRY
        //    {
        //      DjVuFileCache* xcache = ctx->cache;
        //      if (! cache) xcache = 0;
        //      GURL gurl;
        //      if (utf8)
        //        gurl = GURL::Filename::UTF8(filename);
        //      else
        //        gurl = GURL::Filename::Native(filename);
        //      d = new ddjvu_document_s;
        //      ref(d);
        //      GMonitorLock lock(&d->monitor);
        //      d->streamid = -1;
        //      d->fileflag = true;
        //      d->pageinfoflag = false;
        //      d->urlflag = false;
        //      d->docinfoflag = false;
        //      d->myctx = ctx;
        //      d->mydoc = 0;
        //      d->doc = DjVuDocument::create_noinit();
        //      d->doc->start_init(gurl, d, xcache);
        //    }
        //  G_CATCH(ex)
        //    {
        //        if (d)
        //            unref(d);
        //        d = 0;
        //        ERROR1(ctx, ex);
        //    }
        //    G_ENDCATCH;
        //  return d;
        //}

        //  #define ddjvu_document_decoding_status(document) \
        //   ddjvu_job_status(ddjvu_document_job(document))
        //  #define ddjvu_document_decoding_done(document) \
        //   (ddjvu_document_decoding_status(document) >= DDJVU_JOB_OK)
        //  #define ddjvu_document_decoding_error(document) \
        //   (ddjvu_document_decoding_status(document) >= DDJVU_JOB_FAILED)

        //  ddjvu_context_t* ctx;
        //  ddjvu_document_t* doc;

        //  unsigned long timingdata[4];

        //  double flag_scale = -1;
        //  int flag_size = -1;
        //  int flag_aspect = -1;
        //  int flag_subsample = -1;
        //  int flag_segment = 0;
        //  int flag_verbose = 0;
        //  char flag_mode = 0;     /* 'c', 'k', 's', 'f','b' */
        //  char flag_format = 0;   /* '4','5','6','p','r','t', 'f' */
        //  int flag_quality = -1; /* 1-100 jpg, 900 zip, 901 lzw, 1000 raw */
        //  int flag_skipcorrupted = 0;
        //  int flag_eachpage = 0;
        //  const char* flag_pagespec = 0;
        //  ddjvu_rect_t info_size;
        //  ddjvu_rect_t info_segment;
        //  const char* programname = 0;
        //  const char* inputfilename = 0;
        //  const char* outputfilename = 0;

        //  char* pagefilename = 0;
        //  #if HAVE_TIFF2PDF
        //  char *tempfilename = 0;
        //  int tiffd = -1;
        //  #endif
        //  #if HAVE_TIFF
        //  TIFF *tiff = 0;
        //  #endif
        //  FILE* fout = 0;

        /* Djvuapi events */

        //  void
        //  handle(int wait)
        //  {
        //    const ddjvu_message_t* msg;
        //    if (!ctx)
        //        return;
        //    if (wait)
        //        msg = ddjvu_message_wait(ctx);
        //    while ((msg = ddjvu_message_peek(ctx)))
        //    {
        //        switch (msg->m_any.tag)
        //        {
        //            case DDJVU_ERROR:
        //                fprintf(stderr, "ddjvu: %s\n", msg->m_error.message);
        //                if (msg->m_error.filename)
        //                    fprintf(stderr, "ddjvu: '%s:%d'\n",
        //                            msg->m_error.filename, msg->m_error.lineno);
        //            default:
        //                break;
        //        }
        //        ddjvu_message_pop(ctx);
        //    }
        //  }

        //  /* Create context and document */
        //
        //  programname = argv[0];
        //  if (! (ctx = ddjvu_context_create(programname)))
        //      die(i18n("Cannot create djvu context."));
        //  if (! (doc = ddjvu_document_create_by_filename(ctx, inputfilename, TRUE)))
        //      die(i18n("Cannot open djvu document '%s'."), inputfilename);
        //  while (! ddjvu_document_decoding_done(doc))
        //      handle(TRUE);
        //  if (ddjvu_document_decoding_error(doc))
        //      die(i18n("Cannot decode document."));

        //  /* Process all pages */
        //
        //  i = ddjvu_document_get_pagenum(doc);
        //  parse_pagespec(flag_pagespec, i, dopage);
        //
        //  /* Close output file */
        //  closefile(0);
        //
        //  /* Release */
        //  if (doc)
        //      ddjvu_document_release(doc);
        //  if (ctx)
        //      ddjvu_context_release(ctx);
        //  return 0;

        /* ------- ADVANCED ------- */

        //  DDJVUAPI int
        //  ddjvu_document_get_filenum(ddjvu_document_t* document);

        /// <summary>
        /// Returns the number of component files.
        /// This function might return 0 when called
        /// before receiving a <m_docinfo> message.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_filenum", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int GetDjvuDocumentFileCount(IntPtr document);

        /* ddjvu_document_get_fileinfo --
           Returns information about component file <fileno>.
           This function might return <DDJVU_JOB_STARTED> when
           called before receiving a <m_docinfo> message.
           string pointers in the returned data structure
           might be null. Strings are UTF8 encoded and remain
           allocated as long as the ddjvu_document_t object exists.

           Changes for ddjvuapi=18
           - Redefined as a macro passing the structure size.
        */

        //  typedef struct ddjvu_fileinfo_s
        //  {
        //    char type;                   /* [P]age, [T]humbnails, [I]nclude. */
        //    int pageno;                 /* Negative when not applicable. */
        //    int size;                   /* Negative when unknown. */
        //    const char* id;               /* File identifier. */
        //    const char* name;             /* Name for indirect documents. */
        //    const char* title;            /* Page title. */
        //  }
        //  ddjvu_fileinfo_t;

        //  #define ddjvu_document_get_fileinfo(d,f,i) \
        //   ddjvu_document_get_fileinfo_imp(d, f, i,sizeof(ddjvu_fileinfo_t))

        //  DDJVUAPI ddjvu_status_t
        //  ddjvu_document_get_fileinfo_imp(ddjvu_document_t* document, int fileno,
        //                                ddjvu_fileinfo_t* info, unsigned int infosz);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_fileinfo_imp", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int GetDjvuDocumentFileInfo(IntPtr document, int fileNumber,
            IntPtr pageInfo, int size = 24);

        //  DDJVUAPI int
        //  ddjvu_document_check_pagedata(ddjvu_document_t* document, int pageno);

        /// <summary>
        /// Returns a non zero result if the data for page <pageno>
        /// is already in memory. When this is the case, functions
        /// <ddjvu_document_get_pageinfo> and <ddjvu_document_get_pagetext>
        /// return the information immediately.
        /// This function causes the emission of <m_pageinfo> messages
        /// with zero in the <m_any.page> field whenever a new file
        /// is completely downloaded.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="pageNumber">
        /// Zero based index page number (values from 0 to page count - 1).
        /// </param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_check_pagedata", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int CheckDjvuDocumentPageData(IntPtr document, int pageNumber);

        //  #define ddjvu_document_get_pageinfo(d,p,i) \
        //   ddjvu_document_get_pageinfo_imp(d, p, i,sizeof(ddjvu_pageinfo_t))

        //  DDJVUAPI ddjvu_status_t
        //  ddjvu_document_get_pageinfo_imp(ddjvu_document_t* document, int pagenum,
        //                                ddjvu_pageinfo_t* info, unsigned int infosz);

        /// <summary>
        /// Attempts to obtain information about page with given page number
        /// without decoding the page. If the information is available,
        /// the function returns DDJVU_JOB_OK and fills the info structure.
        /// Otherwise it starts fetching page data and returns DDJVU_JOB_STARTED.
        /// This function causes the emission of m_pageinfo messages
        /// with zero in the m_any.page field.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="pageNumber">
        /// Zero based index page number (values from 0 to page count - 1).
        /// </param>
        /// <param name="pageInfo"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        /// <remarks>
        /// Typical synchronous usage:
        ///
        /// ddjvu_status_t r;
        /// ddjvu_pageinfo_t info;
        /// while ((r=ddjvu_document_get_pageinfo(doc,pageno,&info)) less than DDJVU_JOB_OK)
        ///     handle_ddjvu_messages(ctx, TRUE);
        /// if (r>=DDJVU_JOB_FAILED)
        ///   signal_error();
        /// </remarks>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_pageinfo_imp", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int GetDjvuDocumentPageInfo(IntPtr document, int pageNumber,
            IntPtr pageInfo, int size = 20);


        //  DDJVUAPI char*
        //  ddjvu_document_get_dump(ddjvu_document_t* document, bool json);

        /// <summary>
        /// This function returns a UTF8 encoded text describing the contents
        /// of entire document using the same format as command djvudump.
        /// If parameter json is set to true output will be json formatted.
        /// The returned string must be deallocated using free().
        /// It returns 0 when the information is not yet available.
        /// It may then cause then the emission of m_pageinfo
        /// messages with null m_any.page.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="json">
        /// Parameter controls output formatting.
        /// Set to true (default value) to get json formatted output.
        /// </param>
        /// <returns></returns>
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_dump", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern string GetDjvuDocumentDump(IntPtr document, bool json = true);


        //  DDJVUAPI char*
        //  ddjvu_document_get_pagedump(ddjvu_document_t* document, int pageno);

        /// <summary>
        /// This function returns a UTF8 encoded text describing the contents
        /// of page pageno using the same format as command djvudump.
        /// The returned string must be deallocated using free().
        /// It returns 0 when the information is not yet available.
        /// It may then cause then the emission of m_pageinfo
        /// messages with null m_any.page.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="pageNumber">
        /// Zero based index page number (values from 0 to page count - 1).
        /// </param>
        /// <returns>
        /// string with formatted dump of page data.
        /// </returns>
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_pagedump", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern string GetDjvuDocumentPageDump(IntPtr document, int pageNumber);

        //  DDJVUAPI char*
        //  ddjvu_document_get_pagedump_json(ddjvu_document_t* document, int pageno, bool json);

        /// <summary>
        /// This function returns a UTF8 encoded json formatted text
        /// describing the contents of page pageno using the same
        /// format as command djvudump. The returned string must
        /// be deallocated using free().
        /// It returns 0 when the information is not yet available.
        /// It may then cause then the emission of m_pageinfo
        /// messages with null m_any.page.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="pageNumber">
        /// Zero based index page number (values from 0 to page count - 1).
        /// </param>
        /// <param name="json"></param>
        /// <returns>
        /// string with formatted dump of page data.
        /// </returns>
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_pagedump_json", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern string GetDjvuDocumentPageDump(IntPtr document, int pageNumber, bool json);

        //  DDJVUAPI char*
        //  ddjvu_document_get_filedump(ddjvu_document_t* document, int fileno);

        /// <summary>
        /// This function returns a UTF8 encoded text describing the contents
        /// of file fileno using the same format as command djvudump.
        /// The returned string must be deallocated using free().
        /// It returns 0 when the information is not yet available.
        /// It may then cause then the emission of m_pageinfo
        ///  messages with null m_any.page.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="fileNumber"></param>
        /// <returns>
        /// Pointer to null terminated UTF8 string which has to be released by caller.
        /// </returns>
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_filedump", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern string GetDjvuDocumentFileDump(IntPtr document, int fileNumber);

        //  DDJVUAPI char*
        //  ddjvu_document_get_filedump_json(ddjvu_document_t* document, int fileno, bool json);

        /// <summary>
        /// This function returns a UTF8 encoded json formatted text
        /// describing the contents of file fileno using the same
        /// format as command djvudump.
        /// The returned string must be deallocated using free().
        /// It returns 0 when the information is not yet available.
        /// It may then cause then the emission of m_pageinfo
        /// messages with null m_any.page.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="fileNumber"></param>
        /// <param name="json"></param>
        /// <returns>
        /// Pointer to null terminated UTF8 string which has to be released by caller.
        /// </returns>
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_filedump_json", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern string GetDjvuDocumentFileDump(IntPtr document, int fileNumber, bool json);

        //  DDJVUAPI ddjvu_page_t *
        //  ddjvu_page_create_by_pageno(ddjvu_document_t* document, int pageno);

        /// <summary>
        /// Each page of a document can be accessed by creating a
        /// ddjvu_page_t object with this function. Argument
        /// pageno indicates the page number, starting with page
        /// 0 to pagenum - 1. This function may return NULL
        /// when called before receiving the m_docinfo message.
        /// Calling this function also initiates the data transfer
        /// and the decoding threads for the specified page.
        /// Various messages will document the progress of these
        /// operations. Error messages will be generated if
        /// the page does not exists.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="pageNumber">
        /// Zero based index page number (values from 0 to page count - 1).
        /// </param>
        /// <returns>
        /// Pointer to null terminated UTF8 string which has to be released by caller.
        /// </returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_create_by_pageno", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr GetDjvuDocumentPage(IntPtr document, int pageNumber);

        //  DDJVUAPI ddjvu_page_t *
        //  ddjvu_page_create_by_pageid(ddjvu_document_t* document,
        //                            const char* pageid);

        /// <summary>
        /// This function is similar to GetDjvuDocumentPage other overload
        /// but identifies the desired page by name instead of page number
        /// </summary>
        /// <param name="document"></param>
        /// <param name="pageName"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_create_by_pageid", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr GetDjvuDocumentPage(
            IntPtr document,
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
            string pageName);

        //  DDJVUAPI double
        //  ddjvu_page_get_gamma(ddjvu_page_t* page);

        /// <summary>
        /// Returns the gamma of the display for which this page was designed.
        /// Calling this function before receiving a m_pageinfo message
        /// yields a meaningless but plausible value.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_get_gamma", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern double GetDjvuPageGamma(IntPtr page);

        //  DDJVUAPI ddjvu_page_type_t
        //  ddjvu_page_get_type(ddjvu_page_t* page);

        /// <summary>
        /// Returns the type of the page data.
        /// Calling this function before the termination of the
        /// decoding process might returns PageType.Unknown.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_get_type", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern PageType GetDjvuPageType(IntPtr page);

        //  DDJVUAPI void
        //  ddjvu_page_set_rotation(ddjvu_page_t* page,
        //                        ddjvu_page_rotation_t rot);

        /// <summary>
        /// Changes the counter-clockwise rotation angle for a DjVu page.
        /// Calling this function before receiving a m_pageinfo
        /// message has no good effect.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="rotation"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_set_rotation", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void SetDjvuPageRotation(IntPtr page, PageRotation rotation);

        //  DDJVUAPI ddjvu_page_rotation_t
        //  ddjvu_page_get_rotation(ddjvu_page_t* page);

        /// <summary>
        /// Returns the counter-clockwise rotation angle for the DjVu page.
        /// The rotation is automatically taken into account
        /// by ddjvu_page_render, ddjvu_page_get_width and ddjvu_page_get_height
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_get_rotation", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern PageRotation GetDjvuPageRotation(IntPtr page);

        //  DDJVUAPI ddjvu_page_rotation_t
        //  ddjvu_page_get_initial_rotation(ddjvu_page_t* page);

        /// <summary>
        /// Returns the page rotation specified by the
        /// orientation flags in the DjVu file.
        /// [brain damage warning] This is useful because
        /// maparea coordinates in the annotation chunks
        /// are expressed relative to the rotated coordinates
        /// whereas text coordinates in the hidden text data
        /// are expressed relative to the unrotated coordinates.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_get_initial_rotation", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern PageRotation GetDjvuPageInitialRotation(IntPtr page);

        //  DDJVUAPI int
        //  ddjvu_page_get_version(ddjvu_page_t* page);

        /// <summary>
        /// Returns the version of the djvu file format.
        /// Calling this function before receiving a m_pageinfo
        /// message yields a meaningless but plausible value.
        /// Before calling this function one can check if data
        /// are available by calling CheckDjvuDocumentPageData.
        /// </summary>
        /// <param name="page"></param>
        /// <returns>
        /// DjVu format version of the page file.
        /// </returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_get_version", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int GetDjvuPageVersion(IntPtr page);

        //  DDJVUAPI int
        //  ddjvu_code_get_version(void);

        /// <summary>
        /// Returns the version of the djvu file format
        /// implemented by this library. More or less graceful
        /// degradation might arise if this is smaller than
        /// the number returned by ddjvu_page_get_version.
        /// </summary>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_code_get_version", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int GetDjvuCodeVersion();

        //  DDJVUAPI int
        //  ddjvu_page_render(ddjvu_page_t* page,
        //          const ddjvu_render_mode_t mode,
        //          const ddjvu_rect_t* pagerect,
        //          const ddjvu_rect_t* renderrect,
        //          const ddjvu_format_t* pixelformat,
        //          unsigned long rowsize,
        //          char* imagebuffer );

        /// <summary>
        /// Renders a segment of a page with arbitrary scale.
        /// Argument<mode> indicates what image layers
        /// should be rendered.
        ///
        /// Conceptually this function renders the full page
        /// into a rectangle pageRectangle and copies the
        /// pixels specified by rectangle renderRectangle
        /// into the buffer starting at position imageBuffer.
        /// The actual code is much more efficient than that.
        ///
        /// The final image is written into buffer imageBuffer.
        /// Argument pixelFormat specifies the expected pixel format.
        ///
        /// Argument rowSize specifies the number of bytes from
        /// one row to the next in the buffer. The buffer must be
        /// large enough to accommodate the desired image.
        ///
        /// This function makes a best effort to compute an image
        /// that reflects the most recently decoded data.It might
        /// return false to indicate that no image could be
        /// computed at this point, and that nothing was written into
        /// the buffer.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="mode"></param>
        /// <param name="pageRectangle"></param>
        /// <param name="renderRectangle"></param>
        /// <param name="pixelFormat"></param>
        /// <param name="rowSize"></param>
        /// <param name="imageBuffer"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_render", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int RenderDjvuPage(
            IntPtr page,
            RenderMode mode,
            ref DjvuRectangle pageRectangle,
            ref DjvuRectangle renderRectangle,
            IntPtr pixelFormat,
            uint rowSize,
            IntPtr imageBuffer
            );

        //  DDJVUAPI ddjvu_format_t *
        //  ddjvu_format_create(ddjvu_format_style_t style,
        //                    int nargs, unsigned int* args);

        /// <summary>
        /// Creates a ddjvu_format_t object describing a pixel format.
        /// Argument style describes the generic pixel format.
        /// Argument args is an array of numberOfArgs unsigned integers
        /// providing additional information:
        /// - When style is RGBMASK*, argument numberOfArgs must be 3 or 4.
        ///   The three first entries of array args are three contiguous
        ///   bit masks for the red, green, and blue components of each pixel.
        ///   The resulting color is then xored with the optional fourth entry.
        /// - When style is PALETTE*, argument numberOfArgs must be 216
        ///   and array args contains the 6*6*6 entries of a web color cube.
        /// - Otherwise numberOfArgs must be 0
        /// </summary>
        /// <param name="style"></param>
        /// <param name="numberOfArgs"></param>
        /// <param name="args"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_format_create", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr CreateDjvuFormat(FormatStyle style, int numberOfArgs, IntPtr args);

        //  DDJVUAPI void
        //  ddjvu_format_set_row_order(ddjvu_format_t* format, int top_to_bottom);

        /// <summary>
        /// Sets a flag indicating whether the rows in the pixel buffer
        /// are stored starting from the top or the bottom of the image.
        /// Default ordering starts from the bottom of the image.
        /// This is the opposite of the X11 convention.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="topToBottom"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_format_set_row_order", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void SetDjvuFormatRowOrder(IntPtr format, int topToBottom);

        //  DDJVUAPI void
        //  ddjvu_format_set_y_direction(ddjvu_format_t* format, int top_to_bottom);

        /// <summary>
        /// Sets a flag indicating whether the y coordinates in the drawing
        /// area are oriented from bottom to top, or from top to bottom.
        /// The default is bottom to top, similar to PostScript.
        /// This is the opposite of the X11 convention.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="topToBottom"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_format_set_y_direction", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void SetDjvuFormatYDirection(IntPtr format, int topToBottom);

        //  DDJVUAPI void
        //  ddjvu_format_set_ditherbits(ddjvu_format_t* format, int bits);

        /// <summary>
        /// Specifies the final depth of the image on the screen.
        /// This is used to decide which dithering algorithm should be used.
        /// The default is usually appropriate.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="bits"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_format_set_ditherbits", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void SetDjvuFormatDitherBits(IntPtr format, int bits);

        //  DDJVUAPI void
        //  ddjvu_format_set_gamma(ddjvu_format_t* format, double gamma);

        /// <summary>
        /// Sets the gamma of the display for which the pixels are
        /// intended.  This will be combined with the gamma stored in
        /// DjVu documents in order to compute a suitable color
        /// correction. The default value is 2.2.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="gamma"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_format_set_gamma", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void SetDjvuFormatGamma(IntPtr format, double gamma);

        //  DDJVUAPI void
        //  ddjvu_format_set_white(ddjvu_format_t* format,
        //                       unsigned char b, unsigned char g, unsigned char r);

        /// <summary>
        /// Sets the white point of the display for which the pixels are
        /// intended. This will be combined with the gamma stored in
        /// DjVu documents in order to compute a suitable color
        /// correction. The default value is 0xff,0xff,0xff.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="blue"></param>
        /// <param name="green"></param>
        /// <param name="red"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_format_set_white", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void SetDjvuFormatWhite(IntPtr format,
            byte blue, byte green, byte red);

        //  DDJVUAPI void
        //  ddjvu_format_release(ddjvu_format_t* format);

        /// <summary>
        /// Release a reference to a ddjvu_format_t object.
        /// The calling program should no longer reference this object.
        /// </summary>
        /// <param name="format"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_format_release", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void ReleaseDjvuFormat(IntPtr format);


        /* -------------------------------------------------- */
        /* THUMBNAILS                                         */
        /* -------------------------------------------------- */

        //  DDJVUAPI ddjvu_status_t
        //  ddjvu_thumbnail_status(ddjvu_document_t* document, int pagenum, int start);

        /// <summary>
        /// Determine whether a thumbnail is available for page with pageNumber.
        /// Calling this function with non zero argument start initiates
        /// a thumbnail calculation job. Regardless of its success,
        /// the completion of the job is signaled by a subsequent
        /// m_thumbnail message.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="pageNumber"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_thumbnail_status", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern DjvuJobStatus GetDjvuThumbnailStatus(IntPtr document, int pageNumber, int start);

        //  DDJVUAPI int
        //  ddjvu_thumbnail_render(ddjvu_document_t* document, int pagenum,
        //                          int* wptr, int* hptr,
        //                          const ddjvu_format_t* pixelformat,
        //                          unsigned long rowsize,
        //                          char* imagebuffer);

        /// <summary>
        /// Renders a thumbnail for page with pageNumber.
        /// Argument imageBuffer must be large enough to contain
        /// an image of size *pWidth x *pHeight using pixel format
        /// pixelFormat. Argument rowSize specifies the number
        /// of bytes from one row to the next row in the buffer.
        /// This function returns false when no thumbnail is available.
        /// Otherwise it returns true, adjusts *pWidth and *pHeight to
        /// reflect the thumbnail size, and, if the pointer imageBuffer
        /// is non zero, writes the pixel data into the image buffer.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pWidth"></param>
        /// <param name="pHeight"></param>
        /// <param name="pixelFormat"></param>
        /// <param name="rowSize"></param>
        /// <param name="imageBuffer"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_thumbnail_render", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern DjvuJobStatus RenderDjvuThumbnail(
            IntPtr document,
            int pageNumber,
            ref int pWidth,
            ref int pHeight,
            IntPtr pixelFormat,
            int rowSize,
            IntPtr imageBuffer);

        /* -------------------------------------------------- */
        /* S-EXPRESSIONS                                      */
        /* -------------------------------------------------- */


        /* DjVu files can contain ancillary information such as
           document outline, hidden text, hyperlinks, and metadata.
           Program <djvused> provides for manipulating such
           information.  Like <djvused>, the DDJVU API represents
           this information using a lisp s-expressions.  See file
           <"libdjvu/miniexp.h"> for the s-expression documentation
           and manipulation functions.  See the <djvused> man page
           for the specification of the s-expressions representing
           outlines, hidden text and annotations. It often help
           to print s-expressions using function <miniexp_pprint>.

           WARNING: All strings in s-expression are UTF-8 encoded.
           Strings returned by miniexp_to_str might have to be
           converted to the locale encoding. */


        /* miniexp_t --
           Opaque type representing s-expressions.
           The same definition also appears in
           file <"libdjvu/miniexp.h">. */


        //  typedef struct miniexp_s* miniexp_t;

        //  DDJVUAPI void
        //  ddjvu_miniexp_release(ddjvu_document_t* document, miniexp_t expr);

        /// <summary>
        /// This function controls the allocation of the
        /// s-expressions returned by functions from the DDJVU
        /// API. It indicates that the s-expression expr is no
        /// longer needed and can be deallocated as soon as
        /// necessary. Otherwise the s-expression remains allocated
        /// as long as the document object exists.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="expression"></param>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_miniexp_release", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void ReleaseDjvuMiniexp(IntPtr document, IntPtr expression);

        //  DDJVUAPI miniexp_t
        //  ddjvu_document_get_outline(ddjvu_document_t* document);

        /// <summary>
        /// This function tries to obtain the document outline.
        /// If this information is available, it returns a
        /// s-expression with the same syntax as function
        /// print-outline of program djvused.
        /// Otherwise it returns <miniexp_dummy> until the document
        /// header gets fully decoded.
        /// </summary>
        /// <remarks>
        /// Typical synchronous usage:
        /// miniexp_t r;
        /// while ((r=ddjvu_document_get_outline(doc))==miniexp_dummy)
        ///   handle_ddjvu_messages(ctx, TRUE);
        /// </remarks>
        /// <param name="document"></param>
        /// <returns>
        /// Function returns s-expression with the same syntax as function
        /// print-outline of program djvused.
        /// This function returns the empty list <miniexp_nil> when
        /// the document contains no outline information. It can also
        /// return symbols <failed> or <stopped> when an error occurs
        /// while accessing the desired information.
        /// </returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_outline", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr GetDjvuDocumentOutline(IntPtr document);

        /* ddjvu_document_get_anno --
           This function returns the document-wide annotations.
           This corresponds to a proposed change in the djvu format.
           When no new-style document-wide annotations are available
           and <compat> is true, this function searches a shared
           annotation chunk and returns its contents.

           This function returns <miniexp_dummy> if the information
           is not yet available. It may then cause the emission
           of <m_pageinfo> messages with null <m_any.page>.

           This function returns the empty list <miniexp_nil> when
           the document does not contain page annotations. It can also
           return symbols <failed> or <stopped> when an error occurs
           while accessing the desired information. */

        //  DDJVUAPI miniexp_t
        //  ddjvu_document_get_anno(ddjvu_document_t* document, int compat);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_anno", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr GetDjvuDocumentAnnotation(IntPtr document, int compat);

        /* ddjvu_document_get_pagetext --
           This function tries to obtain the text information for
           page <pageno>. If this information is available, it
           returns a s-expression with the same syntax as function
           <print-txt> of program <djvused>.  Otherwise it starts
           fetching the page data and returns <miniexp_dummy>.
           This function causes the emission of <m_pageinfo> messages
           with zero in the <m_any.page> field.
           Typical synchronous usage:

            miniexp_t r;
            while ((r=ddjvu_document_get_pagetext(doc,pageno,0))==miniexp_dummy)
              handle_ddjvu_messages(ctx, TRUE);

           This function returns the empty list <miniexp_nil> when
           the page contains no text information. It can also return
           symbols <failed> or <stopped> when an error occurs while
           accessing the desired information.

           Argument <maxdetail> controls the level of detail in the
           returned s-expression. Values "page", "column", "region", "para",
           "line", and "word" restrict the output to the specified granularity.
           All other values produce a s-expression that represents
           the hidden text data as finely as possible. */

        //  DDJVUAPI miniexp_t
        //  ddjvu_document_get_pagetext(ddjvu_document_t* document, int pageno,
        //                            const char* maxdetail);

        /// <summary>
        /// This function tries to obtain the text information for
        /// page pageNumber. If this information is available, it
        /// returns a s-expression with the same syntax as function
        /// print-txt of program djvused.  Otherwise it starts
        /// fetching the page data and returns miniexp_dummy.
        /// This function causes the emission of m_pageinfo messages
        /// with zero in the m_any.page field.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="pageNumber"></param>
        /// <param name="maxDetail"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_pagetext", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr GetDjvuDocumentPageText(
            IntPtr document,
            int pageNumber,
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(UTF8StringMarshaler))]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
            string maxDetail);

        //  DDJVUAPI char*
        //  ddjvu_document_get_pagetext_utf8(ddjvu_document_t* document, int pageno,
        //                                  const char* maxdetail)

        /// <summary>
        /// Function returns decoded DjVu document page text bypassing miniexp
        /// and directly reading from native text buffer with support of modified
        /// libdjvulibre library C API. Fidelity is on par with original miniexp
        /// based API but skips unnecessary miniexp encoding and decoding.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="pageNumber"></param>
        /// <param name="maxDetail"></param>
        /// <returns></returns>
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_pagetext_utf8", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern string GetDjvuDocumentPageTextUtf8(
            IntPtr document,
            int pageNumber,
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(UTF8StringMarshaler))]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
            string maxDetail);

        //  DDJVUAPI miniexp_t
        //  ddjvu_document_get_pageanno(ddjvu_document_t* document, int pageno);

        /// <summary>
        /// This function tries to obtain the annotations for
        /// page pageNumber. If this information is available, it
        /// returns a s-expression with the same syntax as function
        /// print-ant of program djvused.  Otherwise it starts
        /// fetching the page data and returns miniexp_dummy.
        /// This function causes the emission of m_pageinfo messages
        /// with zero in the m_any.page field.
        /// </summary>
        /// <remarks>
        /// Typical synchronous usage:
        /// miniexp_t r;
        /// while ((r = ddjvu_document_get_pageanno(doc,pageno))==miniexp_dummy)
        ///   handle_ddjvu_messages(ctx, TRUE);
        ///
        /// This function returns the empty list miniexp_nil when
        /// the page contains no annotations. It can also return
        /// symbols failed or stopped when an error occurs while
        /// accessing the desired information.
        /// </remarks>
        /// <param name="document"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_pageanno", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr GetDjvuPageAnnotation(IntPtr document, int pageNumber);

        /* miniexp_to_name --
           Returns the symbol name as a string.
           Returns NULL if the expression is not a symbol. */

        // MINILISPAPI const char* miniexp_to_name(miniexp_t p);

#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
        [DllImport(DjVuLibrePath, EntryPoint = "miniexp_to_name", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern string MiniexpToName(IntPtr miniexp);

        /* miniexp_symbol --
           Returns the unique symbol expression with the specified name. */

        // MINILISPAPI miniexp_t miniexp_symbol(const char* name);

        [DllImport(DjVuLibrePath, EntryPoint = "miniexp_symbol", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr CreateSymbolMiniexp(
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
            string miniexp);

        // MINILISPAPI int miniexp_length(miniexp_t p);

        /// <summary>
        /// Returns the length of a list. Returns 0 for non lists,
        /// -1 for circular lists.
        /// </summary>
        /// <param name="miniexp"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "miniexp_length", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int MiniexpLength(IntPtr miniexp);

        /* miniexp_nth --
           Returns the n-th element of a list. */

        // MINILISPAPI miniexp_t miniexp_nth(int n, miniexp_t l);

        /// <summary>
        /// Returns the n-th element of a list.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="miniexpList"></param>
        /// <returns>
        /// n-th miniexp element in miniexpList.
        /// </returns>
        [DllImport(DjVuLibrePath, EntryPoint = "miniexp_nth", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr MiniexpItem(int index, IntPtr miniexpList);

        //  MINILISPAPI int miniexp_stringp(miniexp_t p);

        /// <summary>
        /// Tests if an expression is a string.
        /// </summary>
        /// <param name="miniexp"></param>
        /// <returns></returns>
        [DllImport(DjVuLibrePath, EntryPoint = "miniexp_stringp", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool IsMiniexpString(IntPtr miniexp);

        // MINILISPAPI const char* miniexp_to_str(miniexp_t p);

        /// <summary>
        /// Returns the c string represented by the expression.
        /// Returns NULL if the expression is not a string.
        /// The c string remains valid as long as the corresponding
        /// lisp object exists.
        /// </summary>
        /// <param name="miniexp"></param>
        /// <returns></returns>
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments - false positive as there is custom marshaling implemented in UTF8StringMarshaler
        [DllImport(DjVuLibrePath, EntryPoint = "miniexp_to_str", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern string GetMiniexpString(IntPtr miniexp);

        /* miniexp_to_lstr ----
           Returns the length of the string represented by the expression.
           Optionally returns the c string into *sp.
           Return 0 and makes *sp null if the expression is not a string. */

        // MINILISPAPI size_t miniexp_to_lstr(miniexp_t p, const char** sp);

        /* miniexp_string --
           Constructs a string expression by copying zero terminated string s. */

        // MINILISPAPI miniexp_t miniexp_string(const char* s);

        // MINILISPAPI miniexp_t miniexp_print(miniexp_t p);

        [DllImport(DjVuLibrePath, EntryPoint = "miniexp_print", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr MiniexpPrint(IntPtr miniexp);


        /// <summary>
        /// Retrieves the total number of unique shapes present in the JB2 foreground image dictionary of the specified page.
        /// </summary>
        /// <param name="page">A pointer to the native ddjvu_page_t object.</param>
        /// <param name="count">When the method returns, contains the total number of shapes in the JB2 dictionary.</param>
        /// <returns>True if the count was successfully retrieved; otherwise, false.</returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_get_jb2_shape_count", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool GetDjvuPageJb2ShapeCount(IntPtr page, out int count);

        /// <summary>
        /// Retrieves the raw pixel data and dimensions for a specific JB2 shape from the native dictionary.
        /// </summary>
        /// <param name="page">A pointer to the native ddjvu_page_t object.</param>
        /// <param name="shapeNo">The zero-based index of the shape to retrieve.</param>
        /// <param name="width">When the method returns, contains the width (columns) of the shape's bitmap.</param>
        /// <param name="height">When the method returns, contains the height (rows) of the shape's bitmap.</param>
        /// <param name="rowsize">When the method returns, contains the memory row stride (in bytes) of the shape's bitmap.</param>
        /// <param name="pixels">An array to receive the raw bitmap pixels. Set to null to just query dimensions.</param>
        /// <returns>True if the shape was successfully retrieved; otherwise, false.</returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_get_jb2_shape", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool GetDjvuPageJb2Shape(IntPtr page, int shapeNo, out int width, out int height, out int rowsize, [Out] byte[] pixels);

        // --------------------------------------------------------------------------
        // JB2 Isolated Chunk Decoding & Bitmap Extraction Hooks
        // --------------------------------------------------------------------------

        /// <summary>
        /// Instantiates a pristine native JB2Dict from a raw chunk stream (djbz).
        /// </summary>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_jb2dict_create_from_chunk", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool CreateDjvuJb2DictFromChunk(IntPtr chunkData, int chunkSize, out int shapeCount, out IntPtr outHandle);

        /// <summary>
        /// Instantiates a pristine native JB2Image from a raw chunk stream (sjbz).
        /// </summary>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_jb2image_create_from_chunk", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool CreateDjvuJb2ImageFromChunk(IntPtr sjbzData, int sjbzSize, IntPtr djbzData, int djbzSize, out IntPtr outHandle);

        /// <summary>
        /// Frees the opaque handle returned by CreateDjvuJb2DictFromChunk.
        /// </summary>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_jb2dict_free", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool FreeDjvuJb2Dict(IntPtr handle);

        /// <summary>
        /// Frees the opaque handle returned by CreateDjvuJb2ImageFromChunk.
        /// </summary>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_jb2image_free", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool FreeDjvuJb2Image(IntPtr handle);

        /// <summary>
        /// Extracts the pristine 8bpp native bitmap from an isolated JB2Image handle.
        /// </summary>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_jb2image_get_bitmap", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool GetDjvuJb2ImageBitmap(
            IntPtr handle, int align, 
            out int width, out int height, out int rowsize, out int border,
            IntPtr buffer, int bufferSize);

        /* ddjvu_page_get_jb2_blit_count ---
           Populates the count pointer with the total number of blits in the foreground JB2 image.
           Returns TRUE (1) on success, FALSE (0) on failure. */

        // DDJVUAPI int ddjvu_page_get_jb2_blit_count(ddjvu_page_t *page, int *count);

        /// <summary>
        /// Retrieves the total number of shape blits present in the JB2 foreground image of the specified page.
        /// This is used for deep binary compatibility testing to verify the arithmetic entropy decoding.
        /// </summary>
        /// <param name="page">A pointer to the native ddjvu_page_t object.</param>
        /// <param name="count">When the method returns, contains the total number of blits in the JB2 image.</param>
        /// <returns>True if the count was successfully retrieved; otherwise, false (e.g., if the page is null or has no JB2 data).</returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_get_jb2_blit_count", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool GetDjvuPageJb2BlitCount(IntPtr page, out int count);

        /* ddjvu_page_get_jb2_blit ---
           Retrieves the blit at the specified index. Returns TRUE (1) on success, FALSE (0) on failure. */

        // DDJVUAPI int ddjvu_page_get_jb2_blit(ddjvu_page_t *page, int index, unsigned int *shapeno, int *left, int *bottom);

        /// <summary>
        /// Retrieves the exact layout data (shape ID and coordinates) for a specific JB2 blit.
        /// </summary>
        /// <param name="page">A pointer to the native ddjvu_page_t object.</param>
        /// <param name="index">The zero-based index of the blit to retrieve.</param>
        /// <param name="shapeNo">When the method returns, contains the ID of the shape used by this blit.</param>
        /// <param name="left">When the method returns, contains the X coordinate (left) of the blit.</param>
        /// <param name="bottom">When the method returns, contains the Y coordinate (bottom) of the blit.</param>
        /// <returns>True if the blit data was successfully retrieved; otherwise, false.</returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_get_jb2_blit", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool GetDjvuPageJb2Blit(IntPtr page, int index, out uint shapeNo, out int left, out int bottom);

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeJb2Blit
        {
            public uint ShapeNo;
            public int Left;
            public int Bottom;
        }

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_get_jb2_blits", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool GetDjvuPageJb2Blits(IntPtr page, [Out] NativeJb2Blit[] blitsArray, int bufferCount);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_incl_count", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int GetDjvuDocumentInclCount(IntPtr document, int pageNo);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_incl_id", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr GetDjvuDocumentInclId(IntPtr document, int pageNo, int index);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_dirm_component_count", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int GetDjvuDocumentDirmComponentCount(IntPtr document);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_dirm_component_id", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr GetDjvuDocumentDirmComponentId(IntPtr document, int index);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_dirm_component_name", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr GetDjvuDocumentDirmComponentName(IntPtr document, int index);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_dirm_component_title", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr GetDjvuDocumentDirmComponentTitle(IntPtr document, int index);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_dirm_component_size", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int GetDjvuDocumentDirmComponentSize(IntPtr document, int index, out int size);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_document_get_dirm_component_flags", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int GetDjvuDocumentDirmComponentFlags(IntPtr document, int index, out bool isPage, out bool isInclude, out bool isThumbnails, out bool isSharedAnno);

        /// <summary>
        /// Converts YCbCr data to RGB.
        /// </summary>
        /// <param name="pixels">Pointer to the unmanaged memory buffer containing the pixel data.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <param name="rowSizeInPixels">
        /// The exact stride of the input buffer in units of pixels (where one pixel is 3 bytes).
        /// Providing a byte stride instead of a pixel stride will result in memory access violations
        /// in the native implementation.
        /// </param>
        /// <returns>True if the transformation was successful, otherwise false.</returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_ycbcr_to_rgb", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool YCbCrToRgb(IntPtr pixels, int width, int height, int rowSizeInPixels);

        /// <summary>
        /// Converts RGB data to Y planar data.
        /// </summary>
        /// <param name="pixels">Pointer to the input interleaved RGB data.</param>
        /// <param name="width">Width of the image in pixels.</param>
        /// <param name="height">Height of the image in pixels.</param>
        /// <param name="rowSizeInPixels">
        /// The exact stride of the input RGB buffer in units of pixels (where one pixel is 3 bytes).
        /// Providing a byte stride instead of a pixel stride will result in memory access violations
        /// in the native implementation.
        /// </param>
        /// <param name="out">Pointer to the output Y planar data.</param>
        /// <param name="outRowSizeInBytes">The stride of the output planar buffer in bytes.</param>
        /// <returns>True if the transformation was successful, otherwise false.</returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_rgb_to_y", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RgbToY(IntPtr pixels, int width, int height, int rowSizeInPixels, IntPtr @out, int outRowSizeInBytes);

        /// <summary>
        /// Converts RGB data to Cb planar data.
        /// </summary>
        /// <param name="pixels">Pointer to the input interleaved RGB data.</param>
        /// <param name="width">Width of the image in pixels.</param>
        /// <param name="height">Height of the image in pixels.</param>
        /// <param name="rowSizeInPixels">
        /// The exact stride of the input RGB buffer in units of pixels (where one pixel is 3 bytes).
        /// Providing a byte stride instead of a pixel stride will result in memory access violations
        /// in the native implementation.
        /// </param>
        /// <param name="out">Pointer to the output Cb planar data.</param>
        /// <param name="outRowSizeInBytes">The stride of the output planar buffer in bytes.</param>
        /// <returns>True if the transformation was successful, otherwise false.</returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_rgb_to_cb", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RgbToCb(IntPtr pixels, int width, int height, int rowSizeInPixels, IntPtr @out, int outRowSizeInBytes);

        /// <summary>
        /// Converts RGB data to Cr planar data.
        /// </summary>
        /// <param name="pixels">Pointer to the input interleaved RGB data.</param>
        /// <param name="width">Width of the image in pixels.</param>
        /// <param name="height">Height of the image in pixels.</param>
        /// <param name="rowSizeInPixels">
        /// The exact stride of the input RGB buffer in units of pixels (where one pixel is 3 bytes).
        /// Providing a byte stride instead of a pixel stride will result in memory access violations
        /// in the native implementation.
        /// </param>
        /// <param name="out">Pointer to the output Cr planar data.</param>
        /// <param name="outRowSizeInBytes">The stride of the output planar buffer in bytes.</param>
        /// <returns>True if the transformation was successful, otherwise false.</returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_rgb_to_cr", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RgbToCr(IntPtr pixels, int width, int height, int rowSizeInPixels, IntPtr @out, int outRowSizeInBytes);

        /// <summary>
        /// Converts RGB data to Y, Cb, and Cr planar data.
        /// </summary>
        /// <param name="pixels">Pointer to the input interleaved RGB data.</param>
        /// <param name="width">Width of the image in pixels.</param>
        /// <param name="height">Height of the image in pixels.</param>
        /// <param name="rowSizeInPixels">
        /// The exact stride of the input RGB buffer in units of pixels (where one pixel is 3 bytes).
        /// Providing a byte stride instead of a pixel stride will result in memory access violations
        /// in the native implementation.
        /// </param>
        /// <param name="outY">Pointer to the output Y planar data.</param>
        /// <param name="outCb">Pointer to the output Cb planar data.</param>
        /// <param name="outCr">Pointer to the output Cr planar data.</param>
        /// <param name="outRowSizeInBytes">The stride of the output planar buffers in bytes.</param>
        /// <returns>True if the transformation was successful, otherwise false.</returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_rgb_to_ycbcr", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RgbToYCbCr(IntPtr pixels, int width, int height, int rowSizeInPixels, IntPtr outY, IntPtr outCb, IntPtr outCr, int outRowSizeInBytes);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_create_from_chunk", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr CreateIW44ImageFromChunk(IntPtr chunkData, int chunkSize, int isColor);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_decode_chunk", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool DecodeIW44Chunk(IntPtr iw44Handle, IntPtr chunkData, int chunkSize);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_free", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void FreeIW44Image(IntPtr iw44Handle);

        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_get_map_info", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool GetIW44MapInfo(IntPtr iw44Handle, int mapIndex, out int bw, out int bh, out int nb);

        /// <summary>
        /// Extracts exactly 1024 raw wavelet coefficients for a specific 32x32 macroblock.
        ///
        /// STATE CONTEXT: The coefficients returned by this method represent the pristine,
        /// entropy-decoded (ZP) state of the block *immediately after* decompression from the
        /// chunk stream, but *before* any spatial lifting filters or inverse wavelet
        /// transformations are applied. This is critical for isolating entropy parser parity.
        /// </summary>
        /// <param name="iw44Handle">The native handle to the decoded IW44Image.</param>
        /// <param name="mapIndex">The color map index (0=Y, 1=Cb, 2=Cr).</param>
        /// <param name="blockIndex">The linear index of the 32x32 macroblock within the map.</param>
        /// <param name="outCoeff">A pinned pointer to a managed array to receive the data.</param>
        /// <param name="coeffLength">Must be exactly 1024 to prevent native buffer overflow.</param>
        /// <returns>True if the block was successfully extracted, False otherwise.</returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_get_block_data", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool GetIW44BlockData(IntPtr iw44Handle, int mapIndex, int blockIndex, IntPtr outCoeff, int coeffLength);

        /// <summary>
        /// Extracts exactly 1024 raw background wavelet coefficients directly from a loaded DjVu page.
        ///
        /// STATE CONTEXT: The coefficients returned by this method represent the pristine,
        /// entropy-decoded (ZP) state of the block *immediately after* decompression from the
        /// chunk stream, but *before* any spatial lifting filters or inverse wavelet
        /// transformations are applied. This is critical for isolating entropy parser parity.
        /// </summary>
        /// <param name="pageHandle">The native handle to the ddjvu_page_t.</param>
        /// <param name="mapIndex">The color map index (0=Y, 1=Cb, 2=Cr).</param>
        /// <param name="blockIndex">The linear index of the 32x32 macroblock within the map.</param>
        /// <param name="outCoeff">A pinned pointer to a managed array to receive the data.</param>
        /// <param name="coeffLength">Must be exactly 1024 to prevent native buffer overflow.</param>
        /// <returns>True if the block was successfully extracted, False otherwise.</returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_page_get_iw44_block_data", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool GetPageIW44BlockData(IntPtr pageHandle, int mapIndex, int blockIndex, IntPtr outCoeff, int coeffLength);

        /// <summary>
        /// Executes the high-level forward IW44 wavelet transformation (encoding path).
        ///
        /// STATE & LAYOUT: This is a destructive, in-place transformation. The input buffer
        /// must be a continuous 1D array representing a 2D image matrix. The pointer `p`
        /// points to the top-left element. Navigation to the next row is strictly defined
        /// by the `rowSize` stride.
        ///
        /// BEFORE: The buffer contains physical spatial pixel data (e.g., YCbCr values).
        /// AFTER: The buffer is mutated in-place into frequency-domain wavelet coefficients,
        /// ready to be compressed by the ZP entropy coder.
        /// </summary>
        /// <param name="p">Pinned pointer to the continuous buffer.</param>
        /// <param name="w">Width of the image/map.</param>
        /// <param name="h">Height of the image/map.</param>
        /// <param name="rowSize">The row stride in elements (not bytes).</param>
        /// <param name="begin">The starting scale level.</param>
        /// <param name="end">The ending scale level.</param>
        /// <returns>True if the transformation succeeded, False on invalid parameters.</returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_transform_forward", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool IW44TransformForward(IntPtr p, int w, int h, int rowSize, int begin, int end);

        /// <summary>
        /// Executes the high-level backward IW44 wavelet transformation (decoding path).
        ///
        /// STATE & LAYOUT: This is a destructive, in-place transformation. The input buffer
        /// must be a continuous 1D array representing a 2D image matrix. The pointer `p`
        /// points to the top-left element. Navigation to the next row is strictly defined
        /// by the `rowSize` stride.
        ///
        /// BEFORE: The buffer contains frequency-domain wavelet coefficients (entropy-decoded ZP state).
        /// AFTER: The buffer is mutated in-place back into physical spatial pixel data
        /// (e.g., YCbCr values) ready for rendering or RGB conversion.
        /// </summary>
        /// <param name="p">Pinned pointer to the continuous buffer.</param>
        /// <param name="w">Width of the image/map.</param>
        /// <param name="h">Height of the image/map.</param>
        /// <param name="rowSize">The row stride in elements (not bytes).</param>
        /// <param name="begin">The starting scale level.</param>
        /// <param name="end">The ending scale level.</param>
        /// <returns>True if the transformation succeeded, False on invalid parameters.</returns>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_transform_backward", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool IW44TransformBackward(IntPtr p, int w, int h, int rowSize, int begin, int end);

        /// <summary>
        /// Executes the backward horizontal spatial lifting filter.
        /// LAYOUT: Destructive, in-place mutation of the continuous buffer.
        /// BEFORE: Coefficients represent horizontal frequency data.
        /// AFTER: Coefficients are partially lifted into the spatial domain along the X axis.
        /// </summary>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_filter_bh", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool FilterBh(IntPtr p, int w, int h, int rowSize, int scale);

        /// <summary>
        /// Executes the backward vertical spatial lifting filter.
        /// LAYOUT: Destructive, in-place mutation of the continuous buffer using `rowSize` stride.
        /// BEFORE: Coefficients represent vertical frequency data.
        /// AFTER: Coefficients are partially lifted into the spatial domain along the Y axis.
        /// </summary>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_filter_bv", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool FilterBv(IntPtr p, int w, int h, int rowSize, int scale);

        /// <summary>
        /// Executes the forward horizontal spatial lifting filter.
        /// LAYOUT: Destructive, in-place mutation of the continuous buffer.
        /// BEFORE: Buffer contains spatial data along the X axis.
        /// AFTER: Buffer is separated into horizontal high and low frequency components.
        /// </summary>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_filter_fh", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool FilterFh(IntPtr p, int w, int h, int rowSize, int scale);

        /// <summary>
        /// Executes the forward vertical spatial lifting filter.
        /// LAYOUT: Destructive, in-place mutation of the continuous buffer using `rowSize` stride.
        /// BEFORE: Buffer contains spatial data along the Y axis.
        /// AFTER: Buffer is separated into vertical high and low frequency components.
        /// </summary>
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_iw44_filter_fv", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool FilterFv(IntPtr p, int w, int h, int rowSize, int scale);

        /// <summary>
        /// Retrieves the last native exception message captured on the current thread.
        /// </summary>
        /// <returns>The UTF-8 formatted error string, or an empty string if no error occurred.</returns>
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_get_last_error", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern string GetLastError();

        /* -------------------------------------------------- */
        /* COMPATIBILITY TESTING HOOKS: GRect                 */
        /* -------------------------------------------------- */

        //class DJVUAPI GRect
        //{
        //public:
        //    /** Constructs an empty rectangle */
        //    GRect();
        //    /** Constructs a rectangle given its minimal coordinates #xmin# and #ymin#,
        //        and its measurements #width# and #height#. Setting #width# or #height# to zero
        //        produces an empty rectangle.  */
        //    GRect(int xmin, int ymin, unsigned int width = 0, unsigned int height = 0);
        //    /** Returns the rectangle width. */
        //    int width() const;
        //    /** Returns the rectangle height. */
        //    int height() const;
        //    /** Returns the area of the rectangle. */
        //    int area() const;
        //    /** Returns true if the rectangle is empty. */
        //    bool isempty() const;
        //    /** Returns true if the rectangle contains pixel (#x#,#y#).  A rectangle
        //        contains all pixels with horizontal pixel coordinates in range #xmin#
        //        (inclusive) to #xmax# (exclusive) and vertical coordinates #ymin#
        //        (inclusive) to #ymax# (exclusive). */
        //    int contains(int x, int y) const;
        //    /** Returns true if this rectangle contains the passed rectangle #rect#.
        //        The function basically checks, that the intersection of this rectangle
        //        with #rect# is #rect#. */
        //    int contains(const GRect & rect) const;
        //    /** Returns true if rectangles #r1# and #r2# are equal. */
        //    friend int operator ==(const GRect & r1, const GRect & r2);
        //    /** Returns true if rectangles #r1# and #r2# are not equal. */
        //    friend int operator !=(const GRect & r1, const GRect & r2);
        //    /** Resets the rectangle to the empty rectangle */
        //    void clear();
        //    /** Fatten the rectangle. Both vertical sides of the rectangle are pushed
        //        apart by #dx# units. Both horizontal sides of the rectangle are pushed
        //        apart by #dy# units. Setting arguments #dx# (resp. #dy#) to a negative
        //        value reduces the rectangle horizontal (resp. vertical) size. */
        //    int inflate(int dx, int dy);
        //    /** Translate the rectangle. The new rectangle is composed of all the points
        //        of the old rectangle translated by #dx# units horizontally and #dy#
        //        units vertically. */
        //    int translate(int dx, int dy);
        //    /** Sets the rectangle to the intersection of rectangles #rect1# and #rect2#.
        //        This function returns true if the intersection rectangle is not empty. */
        //    int intersect(const GRect &rect1, const GRect &rect2);
        //    /** Sets the rectangle to the smallest rectangle containing the points of
        //    both rectangles #rect1# and #rect2#. This function returns true if the
        //    created rectangle is not empty. */
        //    int recthull(const GRect &rect1, const GRect &rect2);
        //    /** Multiplies xmin, ymin, xmax, ymax by factor and scales the rectangle*/
        //    void scale(float factor);
        //    /** Multiplies xmin, xmax by xfactor and ymin, ymax by yfactor and scales the rectangle*/
        //    void scale(float xfactor, float yfactor);
        //    /** Minimal horizontal point coordinate of the rectangle. */
        //    int xmin;
        //    /** Minimal vertical point coordinate of the rectangle. */
        //    int ymin;
        //    /** Maximal horizontal point coordinate of the rectangle. */
        //    int xmax;
        //    /** Maximal vertical point coordinate of the rectangle. */
        //    int ymax;
        //};

        /// <summary>
        /// Checks if two rectangles are geometrically equal.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>WARNING: Unintuitive Equality Logic</b><br/>
        /// This method proxies to the native <c>GRect::operator==</c>.
        /// The native implementation considers <i>any</i> two empty rectangles to be mathematically equal,
        /// completely ignoring their actual spatial coordinate origins. If <c>r1</c> and <c>r2</c> are both
        /// Empty, this method returns <c>true</c>.
        /// </para>
        /// <para>
        /// <b>Original DjVuLibre Implementation Reference (GRect.cpp):</b>
        /// <code>
        /// int operator==(const GRect &amp; r1, const GRect &amp; r2) {
        ///   bool isempty1 = r1.isempty();
        ///   bool isempty2 = r2.isempty();
        ///   if (isempty1 || isempty2)
        ///     if (isempty1 &amp;&amp; isempty2)
        ///       return 1;
        ///   if ( r1.xmin==r2.xmin &amp;&amp; r1.xmax==r2.xmax
        ///        &amp;&amp; r1.ymin==r2.ymin &amp;&amp; r1.ymax==r2.ymax )
        ///     return 1;
        ///   return 0;
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="r1">Reference to the first rectangle.</param>
        /// <param name="r2">Reference to the second rectangle.</param>
        /// <returns><c>true</c> if the rectangles are exactly equal or if both are Empty; otherwise <c>false</c>.</returns>
        //  DDJVUAPI int ddjvu_grect_equals(const struct ddjvu_grect* r1, const struct ddjvu_grect* r2);
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_grect_equals", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool GRectEquals(ref Graphics.Rectangle r1, ref Graphics.Rectangle r2);

        //  DDJVUAPI int ddjvu_grect_isempty(const struct ddjvu_grect* rect);
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_grect_isempty", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool IsEmptyGRect(ref Graphics.Rectangle rect);

        //  DDJVUAPI int ddjvu_grect_contains_point(const struct ddjvu_grect* rect, int x, int y);
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_grect_contains_point", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool GRectContainsPoint(ref Graphics.Rectangle rect, int x, int y);

        //  DDJVUAPI int ddjvu_grect_contains_rect(const struct ddjvu_grect* rect, const struct ddjvu_grect* other);
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_grect_contains_rect", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool GRectContainsRect(ref Graphics.Rectangle rect, ref Graphics.Rectangle other);

        //  DDJVUAPI int ddjvu_grect_intersect(struct ddjvu_grect* out_rect, const struct ddjvu_grect* r1, const struct ddjvu_grect* r2);
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_grect_intersect", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool GRectIntersect(out Graphics.Rectangle outRect, ref Graphics.Rectangle r1, ref Graphics.Rectangle r2);

        //  DDJVUAPI int ddjvu_grect_recthull(struct ddjvu_grect* out_rect, const struct ddjvu_grect* r1, const struct ddjvu_grect* r2);
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_grect_recthull", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool GRectRecthull(out Graphics.Rectangle outRect, ref Graphics.Rectangle r1, ref Graphics.Rectangle r2);

        //  DDJVUAPI void ddjvu_grect_inflate(struct ddjvu_grect* rect, int dx, int dy);
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_grect_inflate", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void GRectInflate(ref Graphics.Rectangle rect, int dx, int dy);

        //  DDJVUAPI void ddjvu_grect_translate(struct ddjvu_grect* rect, int dx, int dy);
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_grect_translate", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void GRectTranslate(ref Graphics.Rectangle rect, int dx, int dy);

        //  DDJVUAPI int ddjvu_grect_scale(struct ddjvu_grect* rect, float factor);
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_grect_scale", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool GRectScale(ref Graphics.Rectangle rect, float factor);

        //  DDJVUAPI int ddjvu_grect_scale_xy(struct ddjvu_grect* rect, float xfactor, float yfactor);
        [DllImport(DjVuLibrePath, EntryPoint = "ddjvu_grect_scale_xy", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool GRectScale(ref Graphics.Rectangle rect, float xfactor, float yfactor);
    }
}
