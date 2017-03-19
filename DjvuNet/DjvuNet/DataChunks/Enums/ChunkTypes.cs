// <copyright file="ChunkTypes.cs" company="">
// TODO: Update copyright text.
// </copyright>

namespace DjvuNet.DataChunks.Enums
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public enum ChunkType
    {
        /// <summary>
        /// Unknown chunk type
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The composite chunk. The first four data bytes of the FORM chunk
        /// are a secondary identifier. Such chunks are referred to as
        /// FORM:XXXX where "XXXX" stands for the secondary identifier.
        /// </summary>
        Form,

        /// <summary>
        /// A multipage DjVu document. Composite chunk that contains the
        /// DIRM chunk, possibly shared/included chunks and subsequent
        /// FORM:DJVU chunks which make up a multipage document
        /// </summary>
        Djvm,

        /// <summary>
        /// A DjVu Page / single page DjVu document. Composite chunk that
        /// contains the chunks which make up a page in a djvu document
        /// </summary>
        Djvu,

        /// <summary>
        /// A "shared" DjVu file which is included via the INCL chunk. Shared annotations, shared shape dictionary.
        /// </summary>
        Djvi,

        /// <summary>
        /// Composite chunk that contains the TH44 chunks which are the embedded thumbnails
        /// </summary>
        Thum,

        /// <summary>
        /// Page name information for multi-page documents
        /// </summary>
        Dirm,

        /// <summary>
        /// Bookmark information
        /// </summary>
        Navm,

        /// <summary>
        /// Annotations including both initial view settings and overlaid hyperlinks, text boxes, etc.
        /// </summary>
        Anta,

        /// <summary>
        /// Annotations including both initial view settings and overlaid hyperlinks, text boxes, etc.
        /// </summary>
        Antz,

        /// <summary>
        /// Unicode Text and layout information
        /// </summary>
        Txta,

        /// <summary>
        /// Unicode Text and layout information
        /// </summary>
        Txtz,

        /// <summary>
        /// Shared shape table.
        /// </summary>
        Djbz,

        /// <summary>
        /// BZZ compressed JB2 bitonal data used to store mask.
        /// </summary>
        Sjbz,

        /// <summary>
        /// IW44 data used to store foreground
        /// </summary>
        FG44,

        /// <summary>
        /// IW44 data used to store background
        /// </summary>
        BG44,

        /// <summary>
        /// IW44 data used to store embedded thumbnail images
        /// </summary>
        TH44,

        /// <summary>
        /// JB2 data required to remove a watermark
        /// </summary>
        WMRM,

        /// <summary>
        /// Foreground Color JB2 Chunk.
        /// </summary>
        FGbz,

        /// <summary>
        /// Information about the a DjVu page
        /// </summary>
        Info,

        /// <summary>
        /// The ID of an included FORM:DJVI chunk.
        /// </summary>
        Incl,

        /// <summary>
        /// JPEG encoded background
        /// </summary>
        BGjp,

        /// <summary>
        /// JPEG encoded foreground
        /// </summary>
        FGjp,

        /// <summary>
        /// G4 encoded mask
        /// </summary>
        Smmr,

        /// <summary>
        /// Unsupported since version 3.23 - 2002 July
        /// </summary>
        Cida,

        /// <summary>
        /// Abstract TextChunk type
        /// </summary>
        Text = int.MaxValue,
    }
}