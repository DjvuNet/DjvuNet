// <copyright file="UnknownChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks.Enums;

namespace DjvuNet.DataChunks
{


    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class UnknownChunk : IFFChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Smmr; }
        }

        #endregion ChunkType

        #region Data

        private DjvuReader _data;

        /// <summary>
        /// Gets the raw chunk data
        /// </summary>
        public DjvuReader Data
        {
            get
            {
                if (_data == null)
                {
                    _data = ExtractRawData();
                }

                return _data;
            }
        }

        #endregion Data

        #endregion Public Properties

        #region Constructors

        public UnknownChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
            // Nothing
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Gets the string representation of the item
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("Unknown N:{0}; O:{2};", Name, Offset);
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            // Skip the data bytes which are delayed read
            reader.Position += Length;

            // CIDa is a known unknown chunk
            if (ChunkID != "CIDa")
                Debug.WriteLine("Creating unknown chunk for: {0}", ChunkID);
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Extracts the raw data from the chunk
        /// </summary>
        /// <returns></returns>
        private DjvuReader ExtractRawData()
        {
            // Read the data in
            return Reader.CloneReader(Offset + 4 + 4, Length);
        }

        #endregion Private Methods
    }
}