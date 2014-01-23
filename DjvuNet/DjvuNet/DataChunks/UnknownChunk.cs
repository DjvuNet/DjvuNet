// <copyright file="UnknownChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System.IO;
using DjvuNet.DataChunks.Enums;

namespace DjvuNet.DataChunks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class UnknownChunk : IFFChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkTypes ChunkType
        {
            get { return ChunkTypes.Smmr; }
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

        public UnknownChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
            : base(reader, parent, document)
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
            {
                //Console.WriteLine("Creating unknown chunk for: {0}", ChunkID);
            }
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