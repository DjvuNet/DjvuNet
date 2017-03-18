// <copyright file="ThumChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using DjvuNet.DataChunks.Enums;
using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

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

        public ThumChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
            : base(reader, parent, document)
        {
        }

        #endregion Constructors

        #region Protected Methods

#if DEBUG
        protected override void ReadChunkData(DjvuReader reader)
        {
            base.ReadChunkData(reader);
        }

#endif

        #endregion Protected Methods
    }
}