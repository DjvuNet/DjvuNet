// <copyright file="WmrmChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using DjvuNet.JB2;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class WmrmChunk : DjvuNode, IWmrmChunk
    {
        #region Properties

        public override ChunkType ChunkType
        {
            get { return ChunkType.Wmrm; }
        }

        private JB2Image _watermarkImage;

        /// <summary>
        /// Gets the image used to remove the watermark
        /// </summary>
        public JB2Image WatermarkImage
        {
            get
            {
                if (_watermarkImage != null)
                {
                    return _watermarkImage;
                }
                else
                {
                    _watermarkImage = ReadCompressedWatermarkImage();
                    return _watermarkImage;
                }
            }

            internal set
            {
                if (_watermarkImage != value)
                {
                    _watermarkImage = value;
                }
            }
        }

        #endregion Properties

        #region Constructors

        public WmrmChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public WmrmChunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Reads the image which is used to remove the watermark
        /// </summary>
        /// <returns></returns>
        internal JB2Image ReadCompressedWatermarkImage()
        {
            using (IDjvuReader reader = Reader.CloneReaderToMemory(DataOffset, Length))
            {
                JB2Image image = new JB2Image();
                image.Decode(reader);

                return image;
            }
        }

        #endregion Methods
    }
}
