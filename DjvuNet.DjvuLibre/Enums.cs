using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.DjvuLibre
{
    public enum DjvuJobStatus : sbyte
    {
        NotStarted, /* operation was not even started */
        Started,    /* operation is in progress */
        OK,         /* operation terminated successfully */
        Failed,     /* operation failed because of an error */
        Stopped     /* operation was interrupted by user */
    }

    public enum MessageTag : short
    {
        Error,
        Info,
        NewStream,
        DocInfo,
        PageInfo,
        Layout,
        Display,
        Chunk,
        Thumbnail,
        Progress,
    }

    public enum DocumentType
    {
        Unknown = 0,
        SinglePage,
        Bundled,
        Indirect,
        OldBundled, /* obsolete */
        OldIndexed, /* obsolete */
    }

   public enum PageType : sbyte
    {
        Unknown,
        Bitonal,
        Photo,
        Compound,
    }

    public enum PageRotation : sbyte
    {
        Rotate0 = 0,
        Rotate90 = 1,
        Rotate180 = 2,
        Rotate270 = 3,
    }

    /// <summary>
    /// Various ways to render a page.
    /// </summary>
    public enum RenderMode
    {
        Color = 0,       /* color page or stencil */
        Black,           /* stencil or color page */
        ColorOnly,       /* color page or fail */
        MaskOnly,        /* stencil or fail */
        Background,      /* color background layer */
        Foreground,      /* color foreground layer */
    }

    public enum FormatStyle
    {
        BGR24,           /* truecolor 24 bits in BGR order */
        RGB24,           /* truecolor 24 bits in RGB order */
        RGBMASK16,       /* truecolor 16 bits with masks */
        RGBMASK32,       /* truecolor 32 bits with masks */
        GREY8,           /* greylevel 8 bits */
        PALETTE8,        /* paletized 8 bits (6x6x6 color cube) */
        MSBTOLSB,        /* packed bits, msb on the left */
        LSBTOMSB,        /* packed bits, lsb on the left */
    }
}
