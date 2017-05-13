using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    /// <summary>
    /// Class represents chunk which holds wavelet encoded color image data.
    /// </summary>
    public class PM44Chunk : DjvuNode, IPM44Chunk
    {

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.PM44; }
        }

        #endregion ChunkType

        #region Image

        private IInterWavePixelMap _Image;

        /// <summary>
        /// Gets the background image for the chunk
        /// </summary>
        public IInterWavePixelMap Image
        {
            get
            {
                if (_Image == null)
                    _Image = DecodeImage();

                return _Image;
            }

            private set
            {
                if (_Image != value)
                    _Image = value;
            }
        }

        #endregion Image

        #endregion Public Properties

        #region Constructors

        public PM44Chunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public PM44Chunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Progressively decodes the background image
        /// </summary>
        /// <param name="pixelMap"></param>
        public IInterWavePixelMap ProgressiveDecodeBackground(IInterWavePixelMap pixelMap)
        {
            using (IDjvuReader reader = Reader.CloneReaderToMemory(DataOffset, Length))
            {
                pixelMap.Decode(reader);
            }
            return pixelMap;
        }

        /// <summary>
        /// Decodes the background image for this chunk
        /// </summary>
        /// <returns></returns>
        internal IInterWavePixelMap DecodeImage()
        {
            using (IDjvuReader reader = Reader.CloneReaderToMemory(DataOffset, Length))
            {
                IInterWavePixelMap pixelMap = new InterWavePixelMapDecoder();
                pixelMap.Decode(reader);
                return pixelMap;
            }
        }

        #endregion Methods
    }
}
