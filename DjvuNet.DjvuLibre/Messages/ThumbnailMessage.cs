using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    /// <summary>
    /// This message is sent when additional thumbnails are available.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class ThumbnailMessage
    {
        public AnyMassege Any;

        public int PageNumber;
    }
}