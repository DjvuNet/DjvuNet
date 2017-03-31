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
        private InfoMessage(NativeInfoMessage nativeMsg)
        {
            Tag = nativeMsg.Tag;
            Message = (string)UTF8StringMarshaler.GetInstance("").MarshalNativeToManaged(nativeMsg.MessagePtr);
        }

        public MessageTag Tag;        // TODO - this field equally well could be short - sufficient to have enum expressed

        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        public String Message;

        [StructLayout(LayoutKind.Sequential)]
        private class NativeInfoMessage
        {
            internal MessageTag Tag;
            internal IntPtr MessagePtr;
        }

        public static InfoMessage GetMessage(IntPtr nativeChunkMsg)
        {
            NativeInfoMessage msg = Marshal.PtrToStructure<NativeInfoMessage>(nativeChunkMsg);
            InfoMessage message = new InfoMessage(msg);
            return message;
        }
    }
}