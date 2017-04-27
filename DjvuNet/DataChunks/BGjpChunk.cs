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
        #region Private Members

        private long _dataLocation = 0;

        #endregion Private Members

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.BGjp; }
        }

        #endregion ChunkType

        #region BackgroundImage

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

            private set
            {
                if (_backgroundImage != value)
                    _backgroundImage = value;
            }
        }

        #endregion BackgroundImage

        #endregion Public Properties

        #region Constructors

        public BGjpChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        public override void ReadData(IDjvuReader reader)
        {
            _dataLocation = reader.Position;

            // Skip past the data which will be delayed read
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        private byte[] DecodeImageData()
        {
            using (IDjvuReader reader = Reader.CloneReader(_dataLocation))
            {
                return reader.GetJPEGImage(Length);
            }
        }

        #endregion Private Methods
    }
}