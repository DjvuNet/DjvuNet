// <copyright file="DjbzChunk.cs" company="">
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
    public class DjbzChunk : IffChunk
    {
        #region Private Members

        private long _dataLocation = 0;

        #endregion Private Members

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Djbz; }
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
                    _shapeDictionary = DecodeShapeDictionary();

                return _shapeDictionary;
            }

            internal set
            {
                if (_shapeDictionary != value)
                    _shapeDictionary = value;
            }
        }

        #endregion ShapeDictionary

        #endregion Public Properties

        #region Constructors

        public DjbzChunk(IDjvuReader reader, IffChunk parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(IDjvuReader reader)
        {
            _dataLocation = reader.Position;

            // Skip the shape data which is delayed read
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        internal JB2.JB2Dictionary DecodeShapeDictionary()
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