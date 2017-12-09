// <copyright file="DjviChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DjviChunk : DjvuFormElement, IDjviChunk
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

        public DjviChunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        public override void ReadData(IDjvuReader reader)
        {
            if (Length > 0)
            {
                ReadChildren(reader);
            }
        }

        #endregion Protected Methods
    }
}
