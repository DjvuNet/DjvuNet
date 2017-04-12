// <copyright file="InclChunk.cs" company="">
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
    public class InclChunk : DjvuNode
    {
        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Incl; }
        }

        #endregion ChunkType

        #region IncludeID

        /// <summary>
        /// Gets the ID of the element to include
        /// </summary>
        public string IncludeID { get; internal set; }

        #endregion IncludeID

        #endregion Public Properties

        #region Constructors

        public InclChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        public override void ReadChunkData(IDjvuReader reader)
        {
            IncludeID = reader.ReadUTF8String(Length);
        }

        #endregion Protected Methods
    }
}