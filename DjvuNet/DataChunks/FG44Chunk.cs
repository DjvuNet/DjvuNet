// <copyright file="FG44Chunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks.Enums;
using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class FG44Chunk : IFFChunk
    {
        #region Private Members

        private long _dataLocation = 0;

        #endregion Private Members

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.FG44; }
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
                    _foregroundImage = DecodeForegroundImage();

                return _foregroundImage;
            }

            private set
            {
                if (_foregroundImage != value)
                    _foregroundImage = value;
            }
        }

        #endregion ForegroundImage

        #endregion Public Properties

        #region Constructors

        public FG44Chunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
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