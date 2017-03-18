// <copyright file="FGbzChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks.Enums;
using DjvuNet.DataChunks.Graphics;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class FGbzChunk : IFFChunk
    {
        #region Private Variables

        private long _dataLocation = 0;

        private byte _firstByte;

        #endregion Private Variables

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
                if (_palette == null)
                {
                    _palette = DecodePaletteData();
                }

                return _palette;
            }

            private set
            {
                if (Palette != value)
                {
                    _palette = value;
                }
            }
        }

        #endregion Palette

        #endregion Public Properties

        #region Constructors

        public FGbzChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            _dataLocation = reader.Position;
            _firstByte = reader.ReadByte();

            // Skip past the bytes which will be delayed read
            // account for already read byte
            reader.Position += (Length - 1);
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Decodes the data for the color palette contained by FGbz chunk
        /// </summary>
        /// <returns></returns>
        private ColorPalette DecodePaletteData()
        {
            using (DjvuReader reader = Reader.CloneReader(_dataLocation, Length))
            {
                // Read in the palette data
                return new ColorPalette(reader, this);
            }
        }

        #endregion Private Methods
    }
}