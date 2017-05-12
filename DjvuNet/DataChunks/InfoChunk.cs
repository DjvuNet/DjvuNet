// <copyright file="InfoChunk.cs" company="">
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
    public class InfoChunk : DjvuNode, IInfoChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Info; }
        }

        #endregion ChunkType

        #region Width

        /// <summary>
        /// Gets the width for the page
        /// </summary>
        public int Width { get; internal set; }

        #endregion Width

        #region Height

        /// <summary>
        /// Gets the height for the page
        /// </summary>
        public int Height { get; internal set; }

        #endregion Height

        #region MinorVersion

        /// <summary>
        /// Gets the minor version for the page encoder
        /// </summary>
        public byte MinorVersion { get; internal set; }

        #endregion MinorVersion

        #region DPI

        /// <summary>
        /// Gets dots per inch (2.54 cm) for the image
        /// </summary>
        public int DPI { get; internal set; }

        #endregion DPI

        #region MajorVersion

        /// <summary>
        /// Gets the major version for the page encoder
        /// </summary>
        public byte MajorVersion { get; internal set; }

        #endregion MajorVersion

        #region Gamma

        /// <summary>
        /// Gets the gamma amount for the page
        /// </summary>
        public float Gamma { get; internal set; }

        #endregion Gamma

        #region PageRotation

        /// <summary>
        /// Gets the rotation for the page
        /// </summary>
        public PageRotation PageRotation { get; internal set; }

        #endregion PageRotation

        #endregion Public Properties

        #region Constructors

        public InfoChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public InfoChunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Methods

        public override void ReadData(IDjvuReader reader)
        {
            DataOffset = reader.Position;

            Width = reader.ReadUInt16BigEndian();
            Height = reader.ReadUInt16BigEndian();
            MinorVersion = reader.ReadByte();
            MajorVersion = reader.ReadByte();
            DPI = reader.ReadInt16(); // LSB
            Gamma = (float)reader.ReadByte() / 10;

            byte flag = reader.ReadByte();
            // B[3..0]
            PageRotation = (PageRotation)(flag & 0x07);
        }

        #endregion Methods
    }
}