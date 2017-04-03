


using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{
    internal static class NativeMethods
    {

        //  DDJVUAPI const char*
        //  ddjvu_get_version_string(void);

        /// <summary>
        /// Returns a string that described the underlying code.
        /// </summary>
        /// <returns>
        /// DjVuLibre library version string.
        /// </returns>
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_get_version_string",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern String GetDjvuLibreVersion();

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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_context_create", 
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true, ThrowOnUnmappableChar = true)]
        internal static extern IntPtr CreateDjvuContext(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(UTF8StringMarshaler))]
            String programName);

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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_context_release",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_cache_set_size",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_cache_get_size",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern uint DjvuGetCacheSize(IntPtr context);

        //  DDJVUAPI void
        //  ddjvu_cache_clear(ddjvu_context_t* context);

        /// <summary>
        /// Clears all cached data.
        /// </summary>
        /// <param name="context"></param>
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_cache_clear",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_create",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr LoadDjvuDocumentUrl(
            IntPtr context,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(UTF8StringMarshaler))]
            String url,
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
        [DllImport("libdjvulibre.dll", EntryPoint= "ddjvu_document_create_by_filename_utf8",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr LoadDjvuDocumentUtf8(
            IntPtr context,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(UTF8StringMarshaler))]
            String filePath, 
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_create_by_filename",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, PreserveSig = true, ThrowOnUnmappableChar = true)]
        internal static extern IntPtr LoadDjvuDocument(IntPtr context, String filePath, int cache);

        /// <summary>
        /// Returns the number of pages in a DjVu document.
        /// This function might return 1 when called
        /// before receiving a m_docinfo message
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_pagenum",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int GetDjvuDocumentPageCount(IntPtr doc);


        //  DDJVUAPI ddjvu_document_type_t
        //  ddjvu_document_get_type(ddjvu_document_t* document);


        /// <summary>
        /// Returns the type of a DjVu document. This function might return DDJVU_DOCTYPE_UNKNOWN
        /// when called before receiving a m_docinfo message.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_type",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_job_release",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_message_peek",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_message_wait",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_message_pop",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_message_set_callback",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_job_status",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_filenum",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int GetDjvuDocumentFileCount(IntPtr document);


        /* ddjvu_document_get_fileinfo --
           Returns information about component file <fileno>.
           This function might return <DDJVU_JOB_STARTED> when
           called before receiving a <m_docinfo> message.
           String pointers in the returned data structure 
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

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_fileinfo_imp",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_check_pagedata",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_pageinfo_imp",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_dump",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern String GetDjvuDocumentDump(IntPtr document, bool json = true);


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
        /// String with formatted dump of page data.
        /// </returns>
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_pagedump",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern String GetDjvuDocumentPageDump(IntPtr document, int pageNumber);

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
        /// String with formatted dump of page data.
        /// </returns>
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_pagedump_json",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern String GetDjvuDocumentPageDump(IntPtr document, int pageNumber, bool json);

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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_filedump",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern String GetDjvuDocumentFileDump(IntPtr document, int fileNumber);

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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_filedump_json",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern String GetDjvuDocumentFileDump(IntPtr document, int fileNumber, bool json);

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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_page_create_by_pageno",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_page_create_by_pageid",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr GetDjvuDocumentPage(
            IntPtr document,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] 
            String pageName);

        //  DDJVUAPI double
        //  ddjvu_page_get_gamma(ddjvu_page_t* page);

        /// <summary>
        /// Returns the gamma of the display for which this page was designed.
        /// Calling this function before receiving a m_pageinfo message
        /// yields a meaningless but plausible value.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_page_get_gamma",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_page_get_type",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_page_set_rotation",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_page_get_rotation",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_page_get_initial_rotation",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern PageRotation GetDjvuPageInitialRotation(IntPtr page);

        /* ddjvu_page_get_version ---
           Returns the version of the djvu file format.
           Calling this function before receiving a <m_pageinfo>
           message yields a meaningless but plausible value. */

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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_page_get_version",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_code_get_version",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_page_render",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int RenderDjvuPage(
            IntPtr page,
            ref RenderMode mode,
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
        /// Argument args is an array of nargs unsigned integers
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_format_create",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void CreateDjvuFormat(FormatStyle style, int numberOfArgs, IntPtr args);

        /* ddjvu_format_set_row_order ---
           Sets a flag indicating whether the rows in the pixel buffer
           are stored starting from the top or the bottom of the image.
           Default ordering starts from the bottom of the image.
           This is the opposite of the X11 convention. */

        //  DDJVUAPI void
        //  ddjvu_format_set_row_order(ddjvu_format_t* format, int top_to_bottom);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_format_set_row_order",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void SetDjvuFormatRowOrder(IntPtr format, int topToBottom);

        /* ddjvu_format_set_y_direction ---
           Sets a flag indicating whether the y coordinates in the drawing 
           area are oriented from bottom to top, or from top to bottom.  
           The default is bottom to top, similar to PostScript.
           This is the opposite of the X11 convention. */

        //  DDJVUAPI void
        //  ddjvu_format_set_y_direction(ddjvu_format_t* format, int top_to_bottom);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_format_set_y_direction",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void SetDjvuFormatYDirection(IntPtr format, int topToBottom);


        /* ddjvu_format_set_ditherbits ---
           Specifies the final depth of the image on the screen.
           This is used to decide which dithering algorithm should be used.
           The default is usually appropriate. */

        //  DDJVUAPI void
        //  ddjvu_format_set_ditherbits(ddjvu_format_t* format, int bits);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_format_set_ditherbits",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void SetDjvuFormatDitherBits(IntPtr format, int bits);

        /* ddjvu_format_set_gamma ---
           Sets the gamma of the display for which the pixels are
           intended.  This will be combined with the gamma stored in
           DjVu documents in order to compute a suitable color
           correction.  The default value is 2.2. */

        //  DDJVUAPI void
        //  ddjvu_format_set_gamma(ddjvu_format_t* format, double gamma);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_format_set_gamma",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void SetDjvuFormatGamma(IntPtr format, double gamma);

        /* ddjvu_format_set_white ---
           Sets the white point of the display for which the pixels are
           intended. This will be combined with the gamma stored in
           DjVu documents in order to compute a suitable color
           correction. The default value is 0xff,0xff,0xff. */

        //  DDJVUAPI void
        //  ddjvu_format_set_white(ddjvu_format_t* format,
        //                       unsigned char b, unsigned char g, unsigned char r);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_format_set_white",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void SetDjvuFormatWhite(IntPtr format, 
            byte blue, byte green, byte red);

        //  DDJVUAPI void
        //  ddjvu_format_release(ddjvu_format_t* format);

        /// <summary>
        /// Release a reference to a ddjvu_format_t object.
        /// The calling program should no longer reference this object.
        /// </summary>
        /// <param name="format"></param>
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_format_release",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void ReleaseDjvuFormat(IntPtr format);


        /* -------------------------------------------------- */
        /* THUMBNAILS                                         */
        /* -------------------------------------------------- */

        //  DDJVUAPI ddjvu_status_t
        //  ddjvu_thumbnail_status(ddjvu_document_t* document, int pagenum, int start);

        /// <summary>
        /// Determine whether a thumbnail is available for page pageNumber.
        /// Calling this function with non zero argument start initiates
        /// a thumbnail calculation job. Regardless of its success,
        /// the completion of the job is signaled by a subsequent
        /// m_thumbnail message.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="pageNumber"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_thumbnail_status",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern DjvuJobStatus GetDjvuThumbnailStatus(IntPtr document, int pageNumber, int start);

        //  DDJVUAPI int
        //  ddjvu_thumbnail_render(ddjvu_document_t* document, int pagenum,
        //               int* wptr, int* hptr,
        //               const ddjvu_format_t* pixelformat,
        //               unsigned long rowsize,
        //               char* imagebuffer);

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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_thumbnail_render",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern unsafe DjvuJobStatus RenderDjvuThumbnail(
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


        /* ddjvu_miniexp_release -- 
           This function controls the allocation of the
           s-expressions returned by functions from the DDJVU
           API. It indicates that the s-expression <expr> is no
           longer needed and can be deallocated as soon as
           necessary. Otherwise the s-expression remains allocated
           as long as the document object exists. */

        //  DDJVUAPI void
        //  ddjvu_miniexp_release(ddjvu_document_t* document, miniexp_t expr);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_miniexp_release",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern void ReleaseDjvuMiniexp(IntPtr document, IntPtr expression);


        /* ddjvu_document_get_outline -- 
           This function tries to obtain the document outline.  
           If this information is available, it returns a
           s-expression with the same syntax as function
           <print-outline> of program <djvused>.  
           Otherwise it returns <miniexp_dummy> until 
           the document header gets fully decoded.
           Typical synchronous usage:

            miniexp_t r;
            while ((r=ddjvu_document_get_outline(doc))==miniexp_dummy)
              handle_ddjvu_messages(ctx, TRUE); 

           This function returns the empty list <miniexp_nil> when
           the document contains no outline information. It can also
           return symbols <failed> or <stopped> when an error occurs
           while accessing the desired information. */

        //  DDJVUAPI miniexp_t
        //  ddjvu_document_get_outline(ddjvu_document_t* document);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_outline",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_anno",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_pagetext",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr GetDjvuDocumentPageText(
            IntPtr document,
            int pageNumber,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(UTF8StringMarshaler))]
            string maxDetail);

        //  DDJVUAPI char*
        //  ddjvu_document_get_pagetext_utf8(ddjvu_document_t* document, int pageno,
        //                                  const char* maxdetail)

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_pagetext_utf8",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern string GetDjvuDocumentPageTextUtf8(
            IntPtr document,
            int pageNumber,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(UTF8StringMarshaler))]
            string maxDetail);

        /* ddjvu_document_get_pageanno -- 
           This function tries to obtain the annotations for
           page <pageno>. If this information is available, it
           returns a s-expression with the same syntax as function
           <print-ant> of program <djvused>.  Otherwise it starts
           fetching the page data and returns <miniexp_dummy>.
           This function causes the emission of <m_pageinfo> messages 
           with zero in the <m_any.page> field.
           Typical synchronous usage:

             miniexp_t r;
             while ((r = ddjvu_document_get_pageanno(doc,pageno))==miniexp_dummy)
               handle_ddjvu_messages(ctx, TRUE); 

           This function returns the empty list <miniexp_nil> when
           the page contains no annotations. It can also return
           symbols <failed> or <stopped> when an error occurs while
           accessing the desired information. */

        //  DDJVUAPI miniexp_t
        //  ddjvu_document_get_pageanno(ddjvu_document_t* document, int pageno);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_pageanno",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr GetDjvuPageAnnotation(IntPtr document, int pageNumber);

        /* miniexp_to_name --
           Returns the symbol name as a string.
           Returns NULL if the expression is not a symbol. */

        // MINILISPAPI const char* miniexp_to_name(miniexp_t p);

        [DllImport("libdjvulibre.dll", EntryPoint = "miniexp_to_name",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        internal static extern string MiniexpToName(IntPtr miniexp);

        /* miniexp_symbol --
           Returns the unique symbol expression with the specified name. */

        // MINILISPAPI miniexp_t miniexp_symbol(const char* name);

        [DllImport("libdjvulibre.dll", EntryPoint = "miniexp_symbol",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr CreateSymbolMiniexp(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
            string miniexp);

        // MINILISPAPI int miniexp_length(miniexp_t p);

        /// <summary>
        /// Returns the length of a list. Returns 0 for non lists,
        /// -1 for circular lists.
        /// </summary>
        /// <param name="miniexp"></param>
        /// <returns></returns>
        [DllImport("libdjvulibre.dll", EntryPoint = "miniexp_length",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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
        [DllImport("libdjvulibre.dll", EntryPoint = "miniexp_nth",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr MiniexpItem(int index, IntPtr miniexpList);

        /* miniexp_stringp --
           Tests if an expression is a string. */

        //  MINILISPAPI int miniexp_stringp(miniexp_t p);

        [DllImport("libdjvulibre.dll", EntryPoint = "miniexp_stringp",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern bool IsMiniexpString(IntPtr miniexp);

        /* miniexp_to_str --
           Returns the c string represented by the expression.
           Returns NULL if the expression is not a string.
           The c string remains valid as long as the
           corresponding lisp object exists. */

        // MINILISPAPI const char* miniexp_to_str(miniexp_t p);

        [DllImport("libdjvulibre.dll", EntryPoint = "miniexp_to_str",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
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

        [DllImport("libdjvulibre.dll", EntryPoint = "miniexp_print",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr MiniexpPrint(IntPtr miniexp);
    }
}
