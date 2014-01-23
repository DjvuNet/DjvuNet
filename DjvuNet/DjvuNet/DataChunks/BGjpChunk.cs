// <copyright file="BGjpChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System.Drawing;
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
    public class BGjpChunk : IFFChunk
    {
        #region Private Variables

        private long _dataLocation = 0;

        #endregion Private Variables

        #region Public Properties

        #region ChunkType

        public override ChunkTypes ChunkType
        {
            get { return ChunkTypes.BGjp; }
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
                {
                    _backgroundImage = DecodeImageData();
                }

                return _backgroundImage;
            }

            private set
            {
                if (BackgroundImage != value)
                {
                    _backgroundImage = value;
                }
            }
        }

        #endregion BackgroundImage

        #endregion Public Properties

        #region Constructors

        public BGjpChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
            : base(reader, parent, document)
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