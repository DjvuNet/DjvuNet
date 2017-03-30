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
        public AnyMassege Any;

        private IntPtr _ChunkID;
    };
}