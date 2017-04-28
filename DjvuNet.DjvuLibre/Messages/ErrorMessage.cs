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
        private ErrorMessage(NativeErrorMessage nativeMsg)
        {
            Tag = nativeMsg.Tag;
            Message = (string)UTF8StringMarshaler.GetInstance("").MarshalNativeToManaged(nativeMsg.MessagePtr);
            Function = (string)UTF8StringMarshaler.GetInstance("").MarshalNativeToManaged(nativeMsg.FunctionPtr);
            File = (string)UTF8StringMarshaler.GetInstance("").MarshalNativeToManaged(nativeMsg.FilePtr);
            LineNumber = nativeMsg.LineNumber;

        }

        public MessageTag Tag;        // TODO - this field equally well could be short - sufficient to have enum expressed

        public String Message;

        public String Function;

        public String File;

        public int LineNumber;

        [StructLayout(LayoutKind.Sequential)]
        private class NativeErrorMessage
        {
            public MessageTag Tag;
            public IntPtr MessagePtr;
            public IntPtr FunctionPtr;
            public IntPtr FilePtr;
            public int LineNumber;
        }

        public static ErrorMessage GetMessage(IntPtr nativeChunkMsg)
        {
            NativeErrorMessage msg = Marshal.PtrToStructure<NativeErrorMessage>(nativeChunkMsg);
            ErrorMessage message = new ErrorMessage(msg);
            return message;
        }
    }
}