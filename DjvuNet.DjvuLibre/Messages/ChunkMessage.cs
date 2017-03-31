using System;
using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    /// <summary>
    /// This message indicates that an additional chunk
    /// of DjVu data has been decoded.  Member <chunkid>
    /// indicates the type of the DjVu chunk.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class ChunkMessage
    {
        private ChunkMessage(NativeChunkMessage nativeMsg)
        {
            Any = nativeMsg.Any;
            ChunkID = (string) UTF8StringMarshaler.GetInstance("").MarshalNativeToManaged(nativeMsg.ChunkIdPtr);
        }

        [StructLayout(LayoutKind.Sequential)]
        private class NativeChunkMessage
        {
            public AnyMassege Any;
            public IntPtr ChunkIdPtr;
        }
        public AnyMassege Any;

        public String ChunkID;

        public static ChunkMessage GetMessage(IntPtr nativeChunkMsg)
        {
            NativeChunkMessage msg = Marshal.PtrToStructure<NativeChunkMessage>(nativeChunkMsg);
            ChunkMessage message = new ChunkMessage(msg);
            return message;
        }
    };
}