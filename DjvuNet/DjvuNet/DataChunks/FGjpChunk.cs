// <copyright file="FGjpChunk.cs" company="">
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
    public class FGjpChunk : IFFChunk
    {
        #region Private Variables

        private long _dataLocation = 0;

        #endregion Private Variables

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.FGjp; }
        }

        #endregion ChunkType

        #region ForegroundImage

        private Image _foregroundImage;

        /// <summary>
        /// Gets the foreground image for this chunk
        /// </summary>
        public Image ForegroundImage
        {
            get
            {
                if (_foregroundImage == null)
                {
                    _foregroundImage = DecodeImageData();
                }

                return _foregroundImage;
            }

            private set
            {
                if (ForegroundImage != value)
                {
                    _foregroundImage = value;
                }
            }
        }

        #endregion ForegroundImage

        #endregion Public Properties

        #region Constructors

        public FGjpChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
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