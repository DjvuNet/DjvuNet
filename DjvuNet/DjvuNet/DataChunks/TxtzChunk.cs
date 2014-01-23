// <copyright file="TxtzChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using DjvuNet.Compression;
using DjvuNet.DataChunks.Enums;
using DjvuNet.DataChunks.Text;

namespace DjvuNet.DataChunks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class TxtzChunk : TextChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkTypes ChunkType
        {
            get { return ChunkTypes.Txtz; }
        }

        #endregion ChunkType

        #endregion Public Properties

        #region Constructors

        public TxtzChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
            : base(reader, parent, document)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override DjvuReader GetTextDataReader(long position)
        {
            return Reader.CloneReader(position).GetBZZEncodedReader(Length);
        }

        #endregion Protected Methods
    }
}