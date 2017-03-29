


using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{
    public static class NativeMethods
    {

        /* -------------------------------------------------- */
        /* DDJVU_CONTEXT_T                                    */
        /* -------------------------------------------------- */

        /* There is usually only one <ddjvu_context_t> object.  
           This object holds global data structures such as the 
           cache of decoded pages, or the list of pending 
           event messages.
         */



        /* ddjvu_get_version_string() ---
           Returns a string that described the underlying code. */

        //DDJVUAPI const char*
        //ddjvu_get_version_string(void);


        /* ddjvu_context_create ---
           Creates a <ddjvu_context_t> object.
           Argument <programname> is the name of the calling executable. */

        //DDJVUAPI ddjvu_context_t *
        //ddjvu_context_create(const char* programname);


        /* ddjvu_context_release ---
           Release a reference to a <ddjvu_context_t> object.
           The calling program should no longer reference this object.
           The object itself will be destroyed as soon as no other object
           or thread needs it. */

        //DDJVUAPI void
        //ddjvu_context_release(ddjvu_context_t* context);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_context_create", 
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, PreserveSig = true, ThrowOnUnmappableChar = true)]
        internal static extern IntPtr CreateDjvuContext(String programName);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_context_release",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr ReleaseDjvuContext(IntPtr context);

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

        /* ddjvu_document_create_by_filename ---
           Creates a document for a DjVu document stored in a file.
           The document will directly access the specified DjVu file 
           or related files without generating <m_newstream> messages.
           The standard function expects the filename in locale encoding. 
           The utf8 variant expects an utf8 encoded filename. */

        //  DDJVUAPI ddjvu_document_t *
        //  ddjvu_document_create_by_filename(ddjvu_context_t* context,
        //                                  const char* filename,
        //                                  int cache);

        //  DDJVUAPI ddjvu_document_t *
        //  ddjvu_document_create_by_filename_utf8(ddjvu_context_t* context,
        //                                       const char* filename,
        //                                       int cache);

        [DllImport("libdjvulibre.dll", EntryPoint= "ddjvu_document_create_by_filename_utf8",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Auto, PreserveSig = true, ThrowOnUnmappableChar = true)]
        internal static extern IntPtr GetDjvuDocumentUtf8(IntPtr context, String filePath, int cache);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_create_by_filename",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, PreserveSig = true, ThrowOnUnmappableChar = true)]
        internal static extern IntPtr GetDjvuDocument(IntPtr context, String filePath, int cache);

        /*  ddjvu_document_get_pagenum ---
         *  Returns the number of pages in a DjVu document.
         *  This function might return 1 when called 
         *  before receiving a <m_docinfo> message */

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_document_get_pagenum",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern int GetDjvuDocumentPageCount(IntPtr doc);


        //  DDJVUAPI ddjvu_document_type_t
        //  ddjvu_document_get_type(ddjvu_document_t* document);


        /// <summary>
        /// Returns the type of a DjVu document. This function might return <DDJVU_DOCTYPE_UNKNOWN>
        /// when called before receiving a <m_docinfo> message.
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


        /* ddjvu_message_peek ---
           Returns a pointer to the next DDJVU message.
           This function returns 0 if no message is available.
           It does not remove the message from the queue. */

        //  DDJVUAPI ddjvu_message_t *
        //  ddjvu_message_peek(ddjvu_context_t* context);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_message_peek",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr DjvuPeekMessage(IntPtr context);


        /* ddjvu_message_wait ---
           Returns a pointer to the next DDJVU message.
           This function waits until a message is available.
           It does not remove the message from the queue. */

        //  DDJVUAPI ddjvu_message_t *
        //  ddjvu_message_wait(ddjvu_context_t* context);

        [DllImport("libdjvulibre.dll", EntryPoint = "ddjvu_message_wait",
            CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        internal static extern IntPtr DjvuWaitMessage(IntPtr context);


        /* ddjvu_message_pop ---
           Removes one message from the queue.
           This function must be called after processing the message.
           Pointers returned by previous calls to <ddjvu_message_peek> 
           or <ddjvu_message_wait> are no longer valid after 
           calling <ddjvu_message_pop>. */

        //  DDJVUAPI void
        //  ddjvu_message_pop(ddjvu_context_t* context);

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
        /// <param name="context">DjvuLibre API Context</param>
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
        internal static extern int GetDjvuJobStatus(IntPtr job);


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
    }
}
