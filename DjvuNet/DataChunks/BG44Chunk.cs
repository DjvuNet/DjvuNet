// <copyright file="BG44Chunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class BG44Chunk : IFFChunk
    {
        #region Private Members

        private long _dataLocation;

        #endregion Private Members

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.BG44; }
        }

        #endregion ChunkType

        #region BackgroundImage

        private IWPixelMap _backgroundImage;

        /// <summary>
        /// Gets the background image for the chunk
        /// </summary>
        public IWPixelMap BackgroundImage
        {
            get
            {
                if (_backgroundImage == null)
                    _backgroundImage = DecodeBackgroundImage();

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

        public BG44Chunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Progressively decodes the background image
        /// </summary>
        /// <param name="backgroundMap"></param>
        public IWPixelMap ProgressiveDecodeBackground(IWPixelMap backgroundMap)
        {
            using (DjvuReader reader = Reader.CloneReader(_dataLocation, Length))
            {
                backgroundMap.Decode(reader);
            }

            return backgroundMap;
        }

        #endregion Public Methods

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
        /// Decodes the background image for this chunk
        /// </summary>
        /// <returns></returns>
        private IWPixelMap DecodeBackgroundImage()
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