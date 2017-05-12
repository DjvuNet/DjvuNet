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
    public class BG44Chunk : DjvuNode, IBG44Chunk
    {

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.BG44; }
        }

        #endregion ChunkType

        #region BackgroundImage

        private IInterWavePixelMap _backgroundImage;

        /// <summary>
        /// Gets the background image for the chunk
        /// </summary>
        public IInterWavePixelMap BackgroundImage
        {
            get
            {
                if (_backgroundImage == null)
                    _backgroundImage = DecodeBackgroundImage();

                return _backgroundImage;
            }

            internal set
            {
                if (_backgroundImage != value)
                    _backgroundImage = value;
            }
        }

        #endregion BackgroundImage

        #endregion Public Properties

        #region Constructors

        public BG44Chunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public BG44Chunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Progressively decodes the background image
        /// </summary>
        /// <param name="backgroundMap"></param>
        public IInterWavePixelMap ProgressiveDecodeBackground(IInterWavePixelMap backgroundMap)
        {
            using (IDjvuReader reader = Reader.CloneReaderToMemory(DataOffset, Length))
            {
                backgroundMap.Decode(reader);
            }

            return backgroundMap;
        }

        /// <summary>
        /// Decodes the background image for this chunk
        /// </summary>
        /// <returns></returns>
        internal IInterWavePixelMap DecodeBackgroundImage()
        {
            using (IDjvuReader reader = Reader.CloneReaderToMemory(DataOffset, Length))
            {
                IInterWavePixelMap background = new InterWavePixelMap();
                background.Decode(reader);

                return background;
            }
        }

        #endregion Methods
    }
}