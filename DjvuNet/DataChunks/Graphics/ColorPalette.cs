using System;
using System.Collections.Generic;
using DjvuNet.Graphics;

namespace DjvuNet.DataChunks
{
    public class ColorPalette
    {
        #region Private Members

        private FGbzChunk _parent;

        #endregion Private Members

        #region Public Properties

        #region Version

        /// <summary>
        /// Gets the version of the palette data
        /// </summary>
        public int Version { get; internal set; }

        #endregion Version

        #region PaletteColors

        /// <summary>
        /// Gets the colors for the palette
        /// </summary>
        public Pixel[] PaletteColors { get; internal set; }

        #endregion PaletteColors

        #region BlitColors

        /// <summary>
        /// Gets the list of blit color indexes
        /// </summary>
        public int[] BlitColors { get; internal set; }

        #endregion BlitColors

        public FGbzChunk Parent
        {
            get { return _parent; }
        }

        #endregion Public Properties

        #region Constructors

        public ColorPalette(DjvuReader reader, FGbzChunk parent)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            _parent = parent;
            ReadPaletteData(reader);
        }

        #endregion Constructors

        #region Public Methods

        /// <summary> 
        /// Overwrites #p# with the color located at position 
        /// #index# in the palette.
        /// </summary>
        /// <param name="index">DOCUMENT ME!
        /// </param>
        /// <param name="p">DOCUMENT ME!
        /// </param>
        public void IndexToColor(int index, DjvuNet.Graphics.Pixel p)
        {
            p.CopyFrom(PaletteColors[index]);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Decodes the palette data
        /// </summary>
        /// <param name="reader"></param>
        internal void ReadPaletteData(DjvuReader reader)
        {
            byte header = reader.ReadByte();
            bool isShapeTable = (header >> 7) == 1;
            Version = header & 0x7f;

            // Color palette size is expressed as unsigned short int
            // in reference implementation while INT16 is indicated in
            // standard document
            ushort paletteSize = reader.ReadUInt16BigEndian();

            // Read in the palette colors
            List<Pixel> paletteColors = new List<Pixel>(paletteSize);
            for (int x = 0; x < paletteSize; x++)
            {
                sbyte b = reader.ReadSByte();
                sbyte g = reader.ReadSByte();
                sbyte r = reader.ReadSByte();

                paletteColors.Add(new Pixel(b, g, r));
            }
            PaletteColors = paletteColors.ToArray();

            List<int> blitColors = new List<int>();

            if (isShapeTable == true)
            {
                int totalBlits = (int) reader.ReadUInt24BigEndian();
                DjvuReader compressed = reader.GetBZZEncodedReader();

                // Read in the blit colors
                for (int x = 0; x < totalBlits; x++)
                {
                    int index = compressed.ReadUInt16BigEndian();
                    blitColors.Add(index);
                }
            }

            BlitColors = blitColors.ToArray();

        }

        #endregion Private Methods
    }
}