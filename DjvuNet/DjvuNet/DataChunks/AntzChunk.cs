// <copyright file="AntzChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using DjvuNet.DataChunks.Annotations;
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
    public class AntzChunk : AnnotationChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkTypes ChunkType
        {
            get { return ChunkTypes.Antz; }
        }

        #endregion ChunkType

        #endregion Public Properties

        #region Constructors

        public AntzChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
            : base(reader, parent, document)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override DjvuReader GetAnnotationDataReader(long position)
        {
            return Reader.CloneReader(position).GetBZZEncodedReader(Length);
        }

        #endregion Protected Methods
    }
}