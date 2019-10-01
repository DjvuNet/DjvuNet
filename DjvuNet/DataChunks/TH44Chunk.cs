// <copyright file="TH44Chunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using DjvuNet.Graphics;
using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class TH44Chunk : DjvuNode, ITH44Chunk
    {
        #region Properties

        public override ChunkType ChunkType
        {
            get { return ChunkType.TH44; }
        }

        private IInterWavePixelMap _thumbnail;

        /// <summary>
        /// Gets the thumbnail image
        /// </summary>
        public IInterWavePixelMap Thumbnail
        {
            get
            {
                if (_thumbnail != null)
                {
                    return _thumbnail;
                }
                else
                {
                    _thumbnail = DecodeThumbnailImage();
                    return _thumbnail;
                }
            }

            internal set
            {
                if (_thumbnail != value)
                {
                    _thumbnail = value;
                }
            }
        }

        private IPixelMap _image;

        /// <summary>
        /// Gets the image of the thumbnail
        /// </summary>
        public IPixelMap Image
        {
            get
            {
                if (_image != null)
                {
                    return _image;
                }
                else
                {
                    _image = Thumbnail.GetPixelMap();
                    return _image;
                }
            }
        }

        #endregion Properties

        #region Constructors

        public TH44Chunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public TH44Chunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Decodes the thumbnail image for this chunk
        /// </summary>
        /// <returns></returns>
        internal IInterWavePixelMap DecodeThumbnailImage()
        {
            using (IDjvuReader reader = Reader.CloneReaderToMemory(DataOffset, Length))
            {
                IInterWavePixelMap thumbnail = new InterWavePixelMapDecoder();
                thumbnail.Decode(reader);

                return thumbnail;
            }
        }

        #endregion Methods
    }
}
