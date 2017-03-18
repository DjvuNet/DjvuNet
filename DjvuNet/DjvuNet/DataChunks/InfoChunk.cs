// <copyright file="InfoChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks.Enums;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class InfoChunk : IFFChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Info; }
        }

        #endregion ChunkType

        #region Width

        private int _width;

        /// <summary>
        /// Gets the width for the page
        /// </summary>
        public int Width
        {
            get { return _width; }

            private set
            {
                if (Width != value)
                {
                    _width = value;
                }
            }
        }

        #endregion Width

        #region Height

        private int _height;

        /// <summary>
        /// Gets the height for the page
        /// </summary>
        public int Height
        {
            get { return _height; }

            private set
            {
                if (Height != value)
                {
                    _height = value;
                }
            }
        }

        #endregion Height

        #region MinorVersion

        private sbyte _minorVersion;

        /// <summary>
        /// Gets the minor version for the page encoder
        /// </summary>
        public sbyte MinorVersion
        {
            get { return _minorVersion; }

            private set
            {
                if (MinorVersion != value)
                {
                    _minorVersion = value;
                }
            }
        }

        #endregion MinorVersion

        #region DPI

        private int _dPI;

        /// <summary>
        /// Gets dots per inch (2.54 cm) for the image
        /// </summary>
        public int DPI
        {
            get { return _dPI; }

            private set
            {
                if (DPI != value)
                {
                    _dPI = value;
                }
            }
        }

        #endregion DPI

        #region MajorVersion

        private sbyte _majorVersion;

        /// <summary>
        /// Gets the major version for the page encoder
        /// </summary>
        public sbyte MajorVersion
        {
            get { return _majorVersion; }

            private set
            {
                if (MajorVersion != value)
                {
                    _majorVersion = value;
                }
            }
        }

        #endregion MajorVersion

        #region Gamma

        private float _gamma;

        /// <summary>
        /// Gets the gamma amount for the page
        /// </summary>
        public float Gamma
        {
            get { return _gamma; }

            private set
            {
                if (Gamma != value)
                {
                    _gamma = value;
                }
            }
        }

        #endregion Gamma

        #region PageRotation

        private PageRotations _pageRotation;

        /// <summary>
        /// Gets the rotation for the page
        /// </summary>
        public PageRotations PageRotation
        {
            get { return _pageRotation; }

            private set
            {
                if (PageRotation != value)
                {
                    _pageRotation = value;
                }
            }
        }

        #endregion PageRotation

        #endregion Public Properties

        #region Constructors

        public InfoChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            Width = reader.ReadInt16MSB();
            Height = reader.ReadInt16MSB();
            MinorVersion = reader.ReadSByte();
            MajorVersion = reader.ReadSByte();
            DPI = reader.ReadInt16(); // LSB
            Gamma = (float)reader.ReadByte() / 10;

            sbyte flag = reader.ReadSByte();
            // B[3..0]
            PageRotation = (PageRotations)(flag & 0x07);
        }

        #endregion Protected Methods
    }
}