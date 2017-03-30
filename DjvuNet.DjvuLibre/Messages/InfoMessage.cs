using System;
using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    /// <summary>
    /// This messages provides informational text indicating
    /// the progress of the decoding process.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class InfoMessage
    {
        public MessageTag Tag;        // TODO - this field equally well could be short - sufficient to have enum expressed

        private IntPtr _Message;
    }
}