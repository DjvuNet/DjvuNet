// <copyright file="FGjpChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;


namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class FGjpChunk : DjvuNode, IFGjpChunk
    {
        #region Private Members

        private long _dataLocation = 0;

        #endregion Private Members

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.FGjp; }
        }

        #endregion ChunkType

        #region ForegroundImage

        private Image _foregroundImage;

        /// <summary>
        /// Gets the foreground image for this chunk
        /// </summary>
        public Image ForegroundImage
        {
            get
            {
                if (_foregroundImage == null)
                    _foregroundImage = DecodeImageData();

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

        public FGjpChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        public override void ReadData(IDjvuReader reader)
        {
            _dataLocation = reader.Position;

            // Skip past the data which will be delayed read
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        private Image DecodeImageData()
        {
            using (IDjvuReader reader = Reader.CloneReader(_dataLocation))
            {
                return reader.GetJPEGImage(Length);
            }
        }

        #endregion Private Methods
    }
}