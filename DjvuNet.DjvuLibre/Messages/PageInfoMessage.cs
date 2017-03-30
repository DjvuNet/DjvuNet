using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    /// <summary>
    /// The page decoding process generates this message
    /// - when basic page information is available and
    ///   before any <m_relayout> or <m_redisplay> message,
    /// - when the page decoding thread terminates.
    /// You can distinguish both cases using
    /// function ddjvu_page_decoding_status().
    /// Messages <m_pageinfo> are also generated as a consequence of
    /// function calls such as <ddjvu_document_get_pageinfo>.
    /// The field <m_any.page> of such message is null.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class PageInfoMessage
    { 
        public AnyMassege Any;
    }
}