using System;
using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    [StructLayout(LayoutKind.Sequential)]
    public class DjvuFileInfo
    {

        private DjvuFileInfo(NativeFileInfo nativeData)
        {
            Type = nativeData.Type;
            PageNumber = nativeData.PageNumber;
            Size = nativeData.Size;
            ID = (string)UTF8StringMarshaler.GetInstance("").MarshalNativeToManaged(nativeData.IDPtr);
            Name = (string)UTF8StringMarshaler.GetInstance("").MarshalNativeToManaged(nativeData.NamePtr);
            Title = (string)UTF8StringMarshaler.GetInstance("").MarshalNativeToManaged(nativeData.TitlePtr);
        }

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
        public String ID;

        /// <summary>
        /// Name for indirect documents.
        /// </summary>
        public String Name;

        /// <summary>
        /// Page title.
        /// </summary>
        public String Title;

        [StructLayout(LayoutKind.Sequential)]
        private class NativeFileInfo
        {
            public char Type;
            public int PageNumber;
            public int Size;
            public IntPtr IDPtr;
            public IntPtr NamePtr;
            public IntPtr TitlePtr;
        }

        public static DjvuFileInfo GetDjvuFileInfo(IntPtr nativeInfo)
        {
            NativeFileInfo infoNative = Marshal.PtrToStructure<NativeFileInfo>(nativeInfo);
            DjvuFileInfo info = new DjvuFileInfo(infoNative);
            return info;
        }

        public static explicit operator DjvuFileInfo(IntPtr nativePtr)
        {
            return GetDjvuFileInfo(nativePtr);
        }
    }
}