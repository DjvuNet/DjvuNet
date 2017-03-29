using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.DjvuLibre
{

    [StructLayout(LayoutKind.Sequential)]
    public struct AnyMassege
    {
        public MessageTag Tag;        // TODO - this field equally well could be short - sufficient to have enum expressed
        IntPtr Context;
        IntPtr Document;
        IntPtr Page;
        IntPtr Job;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ErrorMessage
    {
        public MessageTag Tag;        // TODO - this field equally well could be short - sufficient to have enum expressed
        IntPtr MessageStr;
        IntPtr FunctionStr;
        IntPtr FileStr;
        int LineNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InfoMessage
    {   /* ddjvu_message_t::m_info */
        public MessageTag Tag;        // TODO - this field equally well could be short - sufficient to have enum expressed
        IntPtr MessageStr;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NewStreamMessage
    { /* ddjvu_message_t::m_newstream */
        public MessageTag Tag;        // TODO - this field equally well could be short - sufficient to have enum expressed
        int StreamID;
        IntPtr Name;
        IntPtr Url;
    }


    /// <summary>
    /// The <m_docinfo> message indicates that basic information
    /// about the document has been obtained and decoded.
    /// Not much can be done before this happens.
    /// Call <ddjvu_document_decoding_status> to determine
    /// whether the operation was successful.
    /// </summary>
    public struct DocInfoMessage
    {
        public MessageTag Tag;        // TODO - this field equally well could be short - sufficient to have enum expressed
    }


    struct ddjvu_message_pageinfo_s { };
    struct ddjvu_message_chunk_s { };
    struct ddjvu_message_relayout_s { };
    struct ddjvu_message_redisplay_s { };
    struct ddjvu_message_thumbnail_s { };
    struct ddjvu_message_progress_s { };

    [StructLayout(LayoutKind.Explicit)]
    public struct DjvuMessage
    {
        [FieldOffset(0)]
        public AnyMassege          Any;

        [FieldOffset(0)]
        public ErrorMessage        Error;

        [FieldOffset(0)]
        public InfoMessage         Info;

        [FieldOffset(0)]
        public NewStreamMessage    NewStream;

        [FieldOffset(0)]
        public DocInfoMessage      DocInfo;

        [FieldOffset(0)]
        ddjvu_message_pageinfo_s   m_pageinfo;

        [FieldOffset(0)]
        ddjvu_message_chunk_s      m_chunk;

        [FieldOffset(0)]
        ddjvu_message_relayout_s   m_relayout;

        [FieldOffset(0)]
        ddjvu_message_redisplay_s  m_redisplay;

        [FieldOffset(0)]
        ddjvu_message_thumbnail_s  m_thumbnail;

        [FieldOffset(0)]
        ddjvu_message_progress_s   m_progress;
    };
}
