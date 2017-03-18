// <copyright file="AntaChunk.cs" company="">
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
    public class AntaChunk : AnnotationChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Anta; }
        }

        #endregion ChunkType

        #endregion Public Properties

        #region Constructors

        public AntaChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override DjvuReader GetAnnotationDataReader(long position)
        {
            return Reader.CloneReader(position, Length);
        }

        #endregion Protected Methods
    }
}