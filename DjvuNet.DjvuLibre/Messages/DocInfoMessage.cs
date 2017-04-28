using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    /// <summary>
    /// The <m_docinfo> message indicates that basic information
    /// about the document has been obtained and decoded.
    /// Not much can be done before this happens.
    /// Call <ddjvu_document_decoding_status> to determine
    /// whether the operation was successful.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class DocInfoMessage
    {
        public AnyMassege Tag;        // TODO - this field equally well could be short - sufficient to have enum expressed
    }
}