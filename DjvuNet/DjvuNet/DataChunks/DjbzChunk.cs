// <copyright file="DjbzChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using DjvuNet.DataChunks.Enums;
using DjvuNet.JB2;

namespace DjvuNet.DataChunks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DjbzChunk : IFFChunk
    {
        #region Private Variables

        private long _dataLocation = 0;

        #endregion Private Variables

        #region Public Properties

        #region ChunkType

        public override ChunkTypes ChunkType
        {
            get { return ChunkTypes.Djbz; }
        }

        #endregion ChunkType

        #region ShapeDictionary

        private JB2.JB2Dictionary _shapeDictionary;

        /// <summary>
        /// Gets the shared shape dictionary
        /// </summary>
        public JB2.JB2Dictionary ShapeDictionary
        {
            get
            {
                if (_shapeDictionary == null)
                {
                    _shapeDictionary = DecodeShapeDictionary();
                }

                return _shapeDictionary;
            }

            private set
            {
                if (ShapeDictionary != value)
                {
                    _shapeDictionary = value;
                }
            }
        }

        #endregion ShapeDictionary

        #endregion Public Properties

        #region Constructors

        public DjbzChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
            : base(reader, parent, document)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            _dataLocation = reader.Position;

            // Skip the shape data which is delayed read
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        private JB2.JB2Dictionary DecodeShapeDictionary()
        {
            using (DjvuReader reader = Reader.CloneReader(_dataLocation, Length))
            {
                JB2.JB2Dictionary dictionary = new JB2Dictionary();
                dictionary.Decode(reader);

                return dictionary;
            }
        }

        #endregion Private Methods
    }
}