// <copyright file="FG44Chunk.cs" company="">
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
    public class FG44Chunk : DjvuNode, IFG44Chunk
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

        private IInterWavePixelMap _foregroundImage;

        /// <summary>
        /// Gets the Foreground image for the chunk
        /// </summary>
        public IInterWavePixelMap ForegroundImage
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

        public FG44Chunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Public Methods

        public override void ReadData(IDjvuReader reader)
        {
            _dataLocation = reader.Position;
            reader.Position += Length;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Decodes the foreground image for this chunk
        /// </summary>
        /// <returns></returns>
        internal IInterWavePixelMap DecodeForegroundImage()
        {
            using (IDjvuReader reader = Reader.CloneReaderToMemory(_dataLocation, Length))
            {
                IInterWavePixelMap background = new InterWavePixelMap();
                background.Decode(reader);

                return background;
            }
        }

        #endregion Private Methods
    }
}