// <copyright file="DjviChunk.cs" company="">
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
    public class DjviChunk : DjvuFormElement
    {
        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Djvi; }
        }

        #endregion ChunkType

        #region Dictionary

        public string Dictionary { get; set; }

        #endregion Dictionary

        #endregion Public Properties

        #region Constructors

        public DjviChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        public override void ReadChunkData(IDjvuReader reader)
        {
            if (Length > 0)
            {
                ReadChildren(reader);
            }
        }

        #endregion Protected Methods
    }
}