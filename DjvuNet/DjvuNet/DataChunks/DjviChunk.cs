// <copyright file="DjviChunk.cs" company="">
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
    public class DjviChunk : FormChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Djvi; }
        }

        #endregion ChunkType

        #endregion Public Properties

        #region Constructors

        public DjviChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
            Length = (int)reader.ReadUInt32MSB();
        }

        #endregion Constructors

        #region Protected Methods

        //protected override void ReadChunkData(DjvuReader reader)
        //{
        //    // Nothing
        //}

        #endregion Protected Methods
    }
}