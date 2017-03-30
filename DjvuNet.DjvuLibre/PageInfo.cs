using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    [StructLayout(LayoutKind.Sequential)]
    public class PageInfo
    {
        /// <summary>
        /// Page width in pixels
        /// </summary>
        public int Width;
        
        /// <summary>
        /// Page height in pixels
        /// </summary>
        public int Height;

        /// <summary>
        /// Page resolution in dots per inch.
        /// </summary>
        public int Dpi;

        /// <summary>
        /// Initial page orientation.
        /// </summary>
        public int Rotation;

        /// <summary>
        /// Page version
        /// </summary>
        public int Version;

    }
}