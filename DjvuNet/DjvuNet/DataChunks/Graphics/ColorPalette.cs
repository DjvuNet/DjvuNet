using System.Collections.Generic;
using DjvuNet.Graphics;

namespace DjvuNet.DataChunks.Graphics
{
    public class ColorPalette
    {
        #region Private Variables

        private FGbzChunk _parent;

        #endregion Private Variables

        #region Public Properties

        #region Version

        private int _version;

        /// <summary>
        /// Gets the version of the palette data
        /// </summary>
        public int Version
        {
            get
            {
                return _version;
            }

            private set
            {
                if (Version != value)
                {
                    _version = value;
                }
            }
        }

        #endregion Version

        #region PaletteColors

        private Pixel[] _paletteColors;

        /// <summary>
        /// Gets the colors for the palette
        /// </summary>
        public Pixel[] PaletteColors
        {
            get { return _paletteColors; }

            private set
            {
                if (PaletteColors != value)
                {
                    _paletteColors = value;
                }
            }
        }

        #endregion PaletteColors

        #region BlitColors

        private int[] _blitColors;

        /// <summary>
        /// Gets the list of blit color indexes
        /// </summary>
        public int[] BlitColors
        {
            get { return _blitColors; }

            private set
            {
                if (BlitColors != value)
                {
                    _blitColors = value;
                }
            }
        }

        #endregion BlitColors

        public FGbzChunk Parent
        {
            get { return _parent; }
        }

        #endregion Public Properties

        #region Constructors

        public ColorPalette(DjvuReader reader, FGbzChunk parent)
        {
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
        public void index_to_color(int index, DjvuNet.Graphics.Pixel p)
        {
            p.CopyFrom(PaletteColors[index]);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Decodes the palette data
        /// </summary>
        /// <param name="reader"></param>
        private void ReadPaletteData(DjvuReader reader)
        {
            sbyte header = reader.ReadSByte();
            bool isShapeTable = (header >> 7) == 1;
            _version = header & 127;

            // Color palette size is expressed as unsigned short int
            // in reference implementation while INT16 is indicated in
            // standard document
            ushort paletteSize = reader.ReadUInt16MSB();

            // Read in the palette colors
            List<Pixel> paletteColors = new List<Pixel>(paletteSize);
            for (int x = 0; x < paletteSize; x++)
            {
                sbyte b = reader.ReadSByte();
                sbyte g = reader.ReadSByte();
                sbyte r = reader.ReadSByte();

                paletteColors.Add(new Pixel(b, g, r));
            }
            _paletteColors = paletteColors.ToArray();

            if (isShapeTable == true)
            {
                int totalBlits = (int) reader.ReadUInt24MSB();

                DjvuReader compressed = reader.GetBZZEncodedReader();

                // Read in the blit colors
                List<int> blitColors = new List<int>();
                for (int x = 0; x < totalBlits; x++)
                {
                    int index = compressed.ReadInt16MSB();
                    blitColors.Add(index);
                }
                _blitColors = blitColors.ToArray();
            }
        }

        #endregion Private Methods
    }
}