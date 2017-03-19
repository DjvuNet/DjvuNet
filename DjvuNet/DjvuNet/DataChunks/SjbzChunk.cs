// <copyright file="SjbzChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks.Enums;
using DjvuNet.JB2;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class SjbzChunk : IFFChunk
    {
        #region Private Variables

        private long _dataLocation = 0;

        #endregion Private Variables

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Sjbz; }
        }

        #endregion ChunkType

        #region Image

        private JB2Image _image;

        /// <summary>
        /// Gets the image this chunk represents
        /// </summary>
        public JB2Image Image
        {
            get
            {
                if (_image == null)
                {
                    _image = ReadCompressedImage();
                }

                return _image;
            }

            private set
            {
                if (Image != value)
                {
                    _image = value;
                }
            }
        }

        #endregion Image

        #endregion Public Properties

        #region Constructors

        public SjbzChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            _dataLocation = reader.Position;

            // Skip the data since it is delayed read
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        private JB2Image ReadCompressedImage()
        {
            using (DjvuReader reader = Reader.CloneReader(_dataLocation, Length))
            {
                JB2Image image = new JB2Image();

                JB2.JB2Dictionary includedDictionary = null;

                if (Parent is FormChunk)
                {
                    InclChunk[] includes = ((FormChunk)Parent).IncludedItems;

                    if (includes != null && includes.Length > 0)
                    {
                        string includeID = includes.FirstOrDefault<InclChunk>(x => x.ChunkType == ChunkType.Incl)?.IncludeID;
                        var includeItem = Document.GetChunkByID<DjbzChunk>(includeID);

                        if (includeItem != null)
                        {
                            includedDictionary = includeItem.ShapeDictionary;
                        }
                    }
                }

                image.Decode(reader, includedDictionary);

                return image;
            }
        }

        #endregion Private Methods
    }
}