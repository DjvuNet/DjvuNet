using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{
    public class BM44Chunk : DjvuNode, IBM44Chunk
    {
        #region Private Members

        private long _dataLocation;

        #endregion Private Members

        #region Public Properties

        public override ChunkType ChunkType
        {
            get { return ChunkType.BM44; }
        }

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

        #endregion Public Properties

        #region Constructors

        public BM44Chunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Progressively decodes the background image
        /// </summary>
        /// <param name="pixelMap"></param>
        public IInterWavePixelMap ProgressiveDecodeBackground(IInterWavePixelMap pixelMap)
        {
            using (IDjvuReader reader = Reader.CloneReaderToMemory(_dataLocation, Length))
            {
                pixelMap.Decode(reader);
            }
            return pixelMap;
        }

        public override void ReadData(IDjvuReader reader)
        {
            _dataLocation = reader.Position;
            reader.Position += Length;
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Decodes the background image for this chunk
        /// </summary>
        /// <returns></returns>
        internal IInterWavePixelMap DecodeImage()
        {
            using (IDjvuReader reader = Reader.CloneReaderToMemory(_dataLocation, Length))
            {
                IInterWavePixelMap pixelMap = new InterWavePixelMap();
                pixelMap.Decode(reader);
                return pixelMap;
            }
        }

        #endregion Internal Methods
    }
}
