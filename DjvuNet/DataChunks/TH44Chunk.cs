// <copyright file="TH44Chunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class TH44Chunk : DjvuNode, ITH44Chunk
    {
        #region Private Members

        private long _dataLocation = 0;

        #endregion Private Members

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.TH44; }
        }

        #endregion ChunkType

        #region Thumbnail

        private IWPixelMap _thumbnail;

        /// <summary>
        /// Gets the thumbnail image
        /// </summary>
        public IWPixelMap Thumbnail
        {
            get
            {
                if (_thumbnail == null)
                    _thumbnail = DecodeThumbnailImage();

                return _thumbnail;
            }

            private set
            {
                if (_thumbnail != value)
                    _thumbnail = value;
            }
        }

        #endregion Thumbnail

        #region Image

        private System.Drawing.Bitmap _image;

        /// <summary>
        /// Gets the image of the thumbnail
        /// </summary>
        public System.Drawing.Bitmap Image
        {
            get
            {
                if (_image == null)
                    _image = Thumbnail.GetPixelMap().ToImage();

                return _image;
            }
        }

        #endregion Image

        #endregion Public Properties

        #region Constructors

        public TH44Chunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
            // Nothing
        }

        #endregion Constructors

        #region Protected Methods

        public override void ReadData(IDjvuReader reader)
        {
            _dataLocation = reader.Position;
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Decodes the thumbnail image for this chunk
        /// </summary>
        /// <returns></returns>
        internal IWPixelMap DecodeThumbnailImage()
        {
            using (IDjvuReader reader = Reader.CloneReaderToMemory(_dataLocation, Length))
            {
                IWPixelMap thumbnail = new IWPixelMap();
                thumbnail.Decode(reader);

                return thumbnail;
            }
        }

        #endregion Private Methods
    }
}