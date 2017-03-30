using System;
using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    /// <summary>
    /// Error messages are generated whenever the decoder or the
    /// DDJVU API encounters an error condition.  All errors are
    /// reported as error messages because they can occur
    /// asynchronously.  Member <message> is the error message.
    /// Members <function>, <filename> and <lineno>
    /// indicates the place where the error was detected.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class ErrorMessage
    {
        public MessageTag Tag;        // TODO - this field equally well could be short - sufficient to have enum expressed

        private IntPtr _Message;

        private IntPtr _Function;

        private IntPtr _File;

        public int LineNumber;
    }
}