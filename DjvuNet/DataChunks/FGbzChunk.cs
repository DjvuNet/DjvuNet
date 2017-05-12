// <copyright file="FGbzChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class FGbzChunk : DjvuNode, IFGbzChunk
    {
        #region Private Members

        private long _dataLocation = 0;

        private byte _firstByte;

        #endregion Private Members

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.FGbz; }
        }

        /// <summary>
        /// FGbz chunk contains shape table correspondence data
        /// </summary>
        public bool HasShapeTableData
        {
            get { return (_firstByte & 0xf0) == 0x80; }
        }

        /// <summary>
        /// Version of the FGbz chunk
        /// </summary>
        public int Version
        {
            get { return (_firstByte & 0x7f); }
        }

        #endregion ChunkType

        #region Palette

        private ColorPalette _palette;

        /// <summary>
        /// Gets the color palette data for the foreground
        /// </summary>
        public ColorPalette Palette
        {
            get
            {
                if (_palette != null)
                    return _palette;
                else
                {
                    _palette = DecodePaletteData();
                    return _palette;
                }
            }

            internal set
            {
                if (_palette != value)
                    _palette = value;
            }
        }

        #endregion Palette

        #endregion Public Properties

        #region Constructors

        public FGbzChunk() : base()
        {
        }

        public FGbzChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public FGbzChunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Public Methods

        public override void ReadData(IDjvuReader reader)
        {
            _dataLocation = reader.Position;
            _firstByte = (byte) reader.ReadByte();
            reader.Position = _dataLocation;

            // Skip past the bytes which will be delayed read
            // account for already read byte
            reader.Position += Length;
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Decodes the data for the color palette contained by FGbz chunk
        /// </summary>
        /// <returns></returns>
        internal ColorPalette DecodePaletteData()
        {
            using (IDjvuReader reader = Reader.CloneReaderToMemory(_dataLocation, Length))
            {
                // Read in the palette data
                return new ColorPalette(reader, this);
            }
        }

        #endregion Internal Methods
    }
}