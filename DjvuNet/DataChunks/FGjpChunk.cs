// <copyright file="FGjpChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;


namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class FGjpChunk : DjvuNode, IFGjpChunk
    {

        #region Properties

        public override ChunkType ChunkType
        {
            get { return ChunkType.FGjp; }
        }

        private byte[] _foregroundImage;

        /// <summary>
        /// Gets the foreground image for this chunk
        /// </summary>
        public byte[] ForegroundImage
        {
            get
            {
                if (_foregroundImage == null)
                    _foregroundImage = DecodeImageData();

                return _foregroundImage;
            }

            internal set
            {
                if (_foregroundImage != value)
                    _foregroundImage = value;
            }
        }

        #endregion Properties

        #region Constructors

        public FGjpChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
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