using System;
using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    /// <summary>
    /// Newstream messages are generated whenever the decoder
    /// needs to access raw DjVu data.  The caller must then
    /// provide the requested data using <ddjvu_stream_write>
    /// and <ddjvu_stream_close>.
    /// </summary>
    /// <remarks>
    /// In the case of indirect documents, a single decoder
    /// might simultaneously request several streams of data.
    /// Each stream is identified by a small integer <streamid>.
    /// 
    /// The first <m_newstream> message always has member
    /// <streamid> set to zero and member <name> set to the null
    /// pointer.  It indicates that the decoder needs to access
    /// the data in the main DjVu file.  In fact, data can be
    /// written to stream <0> as soon as the <ddjvu_document_t>
    /// object is created.
    /// 
    /// Further <m_newstream> messages are generated to access
    /// the auxiliary files of indirect or indexed DjVu
    /// documents.  Member <name> then provides the basename of
    /// the auxiliary file.
    /// 
    /// Member <url> is set according to the url argument
    /// provided to function <ddjvu_document_create>.  The first
    /// newstream message always contain the url passed to
    /// <ddjvu_document_create>.  Subsequent newstream messages
    /// contain the url of the auxiliary files for indirect or
    /// indexed DjVu documents.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public class NewStreamMessage
    {

        private NewStreamMessage(NativeNewStreamMessage nativeMsg)
        {
            Tag = nativeMsg.Tag;
            StreamID = nativeMsg.StreamID;
            Name = (string)UTF8StringMarshaler.GetInstance("").MarshalNativeToManaged(nativeMsg.NamePtr);
            Url = (string)UTF8StringMarshaler.GetInstance("").MarshalNativeToManaged(nativeMsg.UrlPtr);
        }

        public MessageTag Tag;        // TODO - this field equally well could be short - sufficient to have enum expressed

        public int StreamID;

        public String Name;

        public String Url;

        [StructLayout(LayoutKind.Sequential)]
        private class NativeNewStreamMessage
        {
            public MessageTag Tag;

            public int StreamID;

            public IntPtr NamePtr;

            public IntPtr UrlPtr;
        }

        public static NewStreamMessage GetMessage(IntPtr nativekMsg)
        {
            NativeNewStreamMessage msg = Marshal.PtrToStructure<NativeNewStreamMessage>(nativekMsg);
            NewStreamMessage message = new NewStreamMessage(msg);
            return message;
        }
    }
}