// <copyright file="TxtaChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

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
    public class TxtaChunk : TextChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Txta; }
        }

        #endregion ChunkType

        #endregion Public Properties

        #region Constructors

        public TxtaChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
            : base(reader, parent, document)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override DjvuReader GetTextDataReader(long position)
        {
            return Reader.CloneReader(position, Length);
        }

        #endregion Protected Methods
    }
}