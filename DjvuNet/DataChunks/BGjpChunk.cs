// <copyright file="BGjpChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;


namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class BGjpChunk : IFFChunk
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

        private Image _backgroundImage;

        /// <summary>
        /// Gets the background image for this chunk
        /// </summary>
        public Image BackgroundImage
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

        public BGjpChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            _dataLocation = reader.Position;

            // Skip past the data which will be delayed read
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        private Image DecodeImageData()
        {
            using (DjvuReader reader = Reader.CloneReader(_dataLocation))
            {
                return reader.GetJPEGImage(Length);
            }
        }

        #endregion Private Methods
    }
}