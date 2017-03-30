using System;
using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    /// <summary>
    /// This structure is a member of the union <djvu_message_t>.
    /// It represents the information common to all kinds of
    /// messages.  Member <tag> indicates the kind of message.
    /// Members <context>, <document>, <page>, and <job> indicate
    /// the origin of the message.  These fields contain null
    /// pointers when they are not relevant.
    /// These fields are also cleared when the corresponding
    /// object is released with ddjvu_{job,page,document}_release.
    /// If the message has not yet been passed to the user
    /// with ddjvu_message_{peek,wait}, it is silently
    /// removed from the message queue.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class AnyMassege
    {
        public MessageTag Tag;        // TODO - this field equally well could be short - sufficient to have enum expressed
        public IntPtr Context;
        public IntPtr Document;
        public IntPtr Page;
        public IntPtr Job;
    }
}
