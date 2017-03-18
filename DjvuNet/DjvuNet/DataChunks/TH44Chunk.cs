// <copyright file="TH44Chunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks.Enums;
using DjvuNet.Wavelet;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class TH44Chunk : IFFChunk
    {
        #region Private Variables

        private long _dataLocation = 0;

        #endregion Private Variables

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
                {
                    _thumbnail = DecodeThumbnailImage();
                }

                return _thumbnail;
            }

            private set
            {
                if (Thumbnail != value)
                {
                    _thumbnail = value;
                }
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
                {
                    _image = Thumbnail.GetPixmap().ToImage();
                }

                return _image;
            }
        }

        #endregion Image

        #endregion Public Properties

        #region Constructors

        public TH44Chunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
            // Nothing
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            // Save the data location for reading later
            _dataLocation = reader.Position;

            //Skip the thumbnail bytes which are delayed read
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Decodes the thumbnail image for this chunk
        /// </summary>
        /// <returns></returns>
        private IWPixelMap DecodeThumbnailImage()
        {
            using (DjvuReader reader = Reader.CloneReader(_dataLocation, Length))
            {
                IWPixelMap thumbnail = new IWPixelMap();
                thumbnail.Decode(reader);

                return thumbnail;
            }
        }

        #endregion Private Methods
    }
}