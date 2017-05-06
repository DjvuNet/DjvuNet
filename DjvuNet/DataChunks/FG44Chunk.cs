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

        #region Properties

        public override ChunkType ChunkType
        {
            get { return ChunkType.FG44; }
        }

        private IInterWavePixelMap _foregroundImage;

        /// <summary>
        /// Gets the Foreground image for the chunk
        /// </summary>
        public IInterWavePixelMap ForegroundImage
        {
            get
            {
                if (_foregroundImage != null)
                    return _foregroundImage;
                else
                {
                    _foregroundImage = DecodeForegroundImage();
                    return _foregroundImage;
                }
            }

            internal set
            {
                if (_foregroundImage != value)
                    _foregroundImage = value;
            }
        }

        #endregion Properties

        #region Constructors

        public FG44Chunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Decodes the foreground image for this chunk
        /// </summary>
        /// <returns></returns>
        internal IInterWavePixelMap DecodeForegroundImage()
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