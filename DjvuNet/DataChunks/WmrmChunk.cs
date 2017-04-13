// <copyright file="WmrmChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DjvuNet.JB2;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class WmrmChunk : DjvuNode
    {
        #region Private Members

        private long _dataLocation = 0;

        #endregion Private Members

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.WMRM; }
        }

        #endregion ChunkType

        #region WatermarkImage

        private JB2Image _watermarkImage;

        /// <summary>
        /// Gets the image used to remove the watermark
        /// </summary>
        public JB2Image WatermarkImage
        {
            get
            {
                if (_watermarkImage == null)
                    _watermarkImage = ReadCompressedWatermarkImage();

                return _watermarkImage;
            }

            internal set
            {
                if (_watermarkImage != value)
                    _watermarkImage = value;
            }
        }

        #endregion WatermarkImage

        #endregion Public Properties

        #region Constructors

        public WmrmChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        public override void ReadData(IDjvuReader reader)
        {
            _dataLocation = reader.Position;

            // Skip the data since it is delayed read
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Reads the image which is used to remove the watermark
        /// </summary>
        /// <returns></returns>
        private JB2Image ReadCompressedWatermarkImage()
        {
            using (DjvuReader reader = Reader.CloneReader(_dataLocation, Length))
            {
                JB2Image image = new JB2Image();
                image.Decode(reader);

                return image;
            }
        }

        #endregion Private Methods
    }
}