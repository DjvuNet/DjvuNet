// <copyright file="AntaChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DjvuNet.DataChunks
{

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

        public AntaChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override IDjvuReader GetAnnotationDataReader(long position)
        {
            return Reader.CloneReaderToMemory(position, Length);
        }

        #endregion Protected Methods
    }
}