// <copyright file="ThumChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ThumChunk : FormChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Thum; }
        }

        #endregion ChunkType

        #endregion Public Properties

        #region Constructors

        public ThumChunk(IDjvuReader reader, IffChunk parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(IDjvuReader reader)
        {
            base.ReadChunkData(reader);
        }


        #endregion Protected Methods
    }
}