// <copyright file="FGbzChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using DjvuNet.DataChunks.Enums;
using DjvuNet.DataChunks.Graphics;

namespace DjvuNet.DataChunks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class FGbzChunk : IFFChunk
    {
        #region Private Variables

        private long _dataLocation = 0;

        #endregion Private Variables

        #region Public Properties

        #region ChunkType

        public override ChunkTypes ChunkType
        {
            get { return ChunkTypes.FGbz; }
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

        public FGbzChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
            : base(reader, parent, document)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            _dataLocation = reader.Position;

            // Skip past the bytes which will be delayed read
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Decodes the data for the palette
        /// </summary>
        /// <returns></returns>
        private ColorPalette DecodePaletteData()
        {
            using (DjvuReader reader = Reader.CloneReader(_dataLocation, Length))
            {
                // Read in the palette data
                return new ColorPalette(reader);
            }
        }

        #endregion Private Methods
    }
}