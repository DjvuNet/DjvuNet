// <copyright file="FG44Chunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using DjvuNet.DataChunks.Enums;
using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class FG44Chunk : IFFChunk
    {
        #region Private Variables

        private long _dataLocation = 0;

        #endregion Private Variables

        #region Public Properties

        #region ChunkType

        public override ChunkTypes ChunkType
        {
            get { return ChunkTypes.FG44; }
        }

        #endregion ChunkType

        #region ForegroundImage

        private IWPixelMap _foregroundImage;

        /// <summary>
        /// Gets the Foreground image for the chunk
        /// </summary>
        public IWPixelMap ForegroundImage
        {
            get
            {
                if (_foregroundImage == null)
                {
                    _foregroundImage = DecodeForegroundImage();
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

        public FG44Chunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
            : base(reader, parent, document)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            _dataLocation = reader.Position;

            // Skip the data since it will be delay read
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Decodes the foreground image for this chunk
        /// </summary>
        /// <returns></returns>
        private IWPixelMap DecodeForegroundImage()
        {
            using (DjvuReader reader = Reader.CloneReader(_dataLocation, Length))
            {
                IWPixelMap background = new IWPixelMap();
                background.Decode(reader);

                return background;
            }
        }

        #endregion Private Methods
    }
}