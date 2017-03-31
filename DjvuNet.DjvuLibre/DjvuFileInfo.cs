using System;
using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    [StructLayout(LayoutKind.Sequential)]
    public class DjvuFileInfo
    {
        /// <summary>
        /// [P]age, [T]humbnails, [I]nclude.
        /// </summary>
        public char Type;

        /// <summary>
        /// Negative when not applicable.
        /// </summary>
        public int PageNumber;

        /// <summary>
        /// Negative when unknown.
        /// </summary>
        public int Size;

        /// <summary>
        /// File identifier.
        /// </summary>
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        public String ID;

        /// <summary>
        /// Name for indirect documents.
        /// </summary>
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        public String Name;

        /// <summary>
        /// Page title.
        /// </summary>
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        public String Title;
    }
}