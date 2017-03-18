// <copyright file="SmmrChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using DjvuNet.DataChunks.Enums;

namespace DjvuNet.DataChunks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class SmmrChunk : IFFChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Smmr; }
        }

        #endregion ChunkType

        #endregion Public Properties

        #region Constructors

        public SmmrChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            // Need to figure out the decoding for the MMR format
            throw new NotImplementedException();
        }

        #endregion Protected Methods
    }
}