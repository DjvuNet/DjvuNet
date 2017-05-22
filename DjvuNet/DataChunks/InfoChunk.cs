// <copyright file="InfoChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DjvuNet.Errors;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class InfoChunk : DjvuNode, IInfoChunk
    {
        internal const byte _MinorVersion = 26;
        internal const byte _MajorVersion = 0;

        #region Properties

        public override ChunkType ChunkType
        {
            get { return ChunkType.Info; }
        }

        public override long Length
        {
            get { return 10; }
            set { }
        }

        /// <summary>
        /// Gets the width for the page
        /// </summary>
        public int Width { get; internal set; }

        /// <summary>
        /// Gets the height for the page
        /// </summary>
        public int Height { get; internal set; }

        /// <summary>
        /// Gets the minor version for the page encoder
        /// </summary>
        public byte MinorVersion { get; internal set; }

        /// <summary>
        /// Gets dots per inch (2.54 cm) for the image
        /// </summary>
        public int DPI { get; internal set; }

        /// <summary>
        /// Gets the major version for the page encoder
        /// </summary>
        public byte MajorVersion { get; internal set; }

        /// <summary>
        /// Gets the gamma amount for the page
        /// </summary>
        public float Gamma { get; internal set; }

        /// <summary>
        /// Gets the rotation for the page
        /// </summary>
        public PageRotation PageRotation { get; internal set; }

        #endregion Properties

        #region Constructors

        public InfoChunk() { }

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
            DPI = reader.ReadUInt16(); // LSB
            Gamma = (float)reader.ReadByte() / 10;

            byte flag = reader.ReadByte();
            switch (flag & 0x7)
            {
                case 6:
                    PageRotation = PageRotation.CounterClockwise;
                    break;
                case 2:
                    PageRotation = PageRotation.UpsideDown;
                    break;
                case 5:
                    PageRotation = PageRotation.Clockwise;
                    break;
                default:
                    PageRotation = PageRotation.RightSideUp;
                    break;
            }
        }

        public override void WriteData(IDjvuWriter writer, bool writeHeader = true)
        {
            if (writer == null)
                throw new DjvuArgumentNullException(nameof(writer));

            AdjustAlignment(writer);

            if (writeHeader)
            {
                writer.WriteUTF8String(Name.ToUpper());
                writer.WriteUInt32BigEndian((uint)Length);
            }

            writer.WriteUInt16BigEndian((ushort)Width);
            writer.WriteUInt16BigEndian((ushort)Height);
            writer.Write(_MinorVersion);
            writer.Write(_MajorVersion);
            writer.Write((ushort)DPI);

            byte gamma = (byte)(10.0 * Gamma + 0.5);
            writer.Write(gamma);

            byte flag = 0;
            switch (PageRotation)
            {
                case PageRotation.CounterClockwise:
                    flag = 6;
                    break;
                case PageRotation.UpsideDown:
                    flag = 2;
                    break;
                case PageRotation.Clockwise:
                    flag = 5;
                    break;
                case PageRotation.RightSideUp:
                    flag = 1;
                    break;
            }

            writer.Write(flag);

        }

        #endregion Methods
    }
}