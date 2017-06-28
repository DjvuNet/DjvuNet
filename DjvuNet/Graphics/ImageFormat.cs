using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Graphics
{
    public enum ImageFormat
    {
        Unknown = 0,

        /// <summary>
        /// Bitmap format defined in Windows and OS/2
        /// </summary>
        Bitmap = 1,

        /// <summary>
        /// Portable Network Graphics format as specified by ISO/IEC 15948:2004 standard.
        /// </summary>
        Png = 2,

        /// <summary>
        /// JPEG format as defined in ISO/IEC 10918-1 standard and later extensions.
        /// </summary>
        Jpeg = 3,


    }
}
