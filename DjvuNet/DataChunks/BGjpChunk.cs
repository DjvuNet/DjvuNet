// <copyright file="BGjpChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;


namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class BGjpChunk : DjvuNode, IBGjpChunk
    {

        #region Public Properties

        public override ChunkType ChunkType
        {
            get { return ChunkType.BGjp; }
        }

        private byte[] _backgroundImage;

        /// <summary>
        /// Gets the background image for this chunk
        /// </summary>
        public byte[] BackgroundImage
        {
            get
            {
                if (_backgroundImage == null)
                    _backgroundImage = DecodeImageData();

                return _backgroundImage;
            }

            internal set
            {
                if (_backgroundImage != value)
                    _backgroundImage = value;
            }
        }

        #endregion Properties

        #region Constructors

        public BGjpChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public BGjpChunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Methods

        internal byte[] DecodeImageData()
        {
            using (IDjvuReader reader = Reader.CloneReaderToMemory(DataOffset, Length))
            {
                return reader.ReadBytes((int)Length);
            }
        }

        #endregion Methods
    }
}