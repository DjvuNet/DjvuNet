using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// CIDa chunk was supported till version 3.23 - 2002 July.
    /// Function is unknown and all it's content is skipped.
    /// </summary>
    public class CidaChunk : DjvuNode
    {

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Cida; }
        }

        #endregion ChunkType

        #endregion Public Properties

        #region Constructors

        public CidaChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public CidaChunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

    }
}