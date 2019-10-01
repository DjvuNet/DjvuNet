// <copyright file="InclChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class InclChunk : DjvuNode, IInclChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Incl; }
        }

        #endregion ChunkType

        #region IncludeID

        private string _IncludeID;

        /// <summary>
        /// Gets the ID of the element to include
        /// </summary>
        public string IncludeID
        {
            get
            {
                if (_IncludeID != null)
                {
                    return _IncludeID;
                }
                else
                {
                    ReadData(Reader);
                    if (_IncludeID == null)
                        _IncludeID = String.Empty;
                    return _IncludeID;
                }
            }
            set
            {
                _IncludeID = value;
            }
        }

        #endregion IncludeID

        #endregion Public Properties

        #region Constructors

        public InclChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public InclChunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Methods

        public override void ReadData(IDjvuReader reader)
        {
            long prevPos = reader.Position;
            reader.Position = DataOffset;
            IncludeID = reader.ReadUTF8String(Length);
            reader.Position = prevPos;
        }

        #endregion Methods
    }
}
