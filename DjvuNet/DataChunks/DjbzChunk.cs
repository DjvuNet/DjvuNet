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
    public class DjbzChunk : DjvuNode, IDjbzChunk
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

        public DjbzChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Public Methods

        public override void ReadData(IDjvuReader reader)
        {
            _dataLocation = reader.Position;
            reader.Position += Length;
        }

        #endregion Public Methods

        #region Internal Methods

        internal JB2.JB2Dictionary DecodeShapeDictionary()
        {
            using (DjvuReader reader = Reader.CloneReader(_dataLocation, Length))
            {
                JB2.JB2Dictionary dictionary = new JB2Dictionary();
                dictionary.Decode(reader);

                return dictionary;
            }
        }

        #endregion Internal Methods
    }
}