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
        ReLayout,
        ReDisplay,
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

}
