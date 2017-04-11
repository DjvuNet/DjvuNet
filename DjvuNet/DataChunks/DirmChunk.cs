// <copyright file="DirmChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using DjvuNet.Compression;
using DjvuNet.DataChunks.Directory;


namespace DjvuNet.DataChunks
{


    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DirmChunk : IFFChunk
    {
        #region Private Members

        private bool _isInitialized = false;
        private long _dataLocation = 0;
        private int _compressedSectionLength = 0;

        #endregion Private Members

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Dirm; }
        }

        #endregion ChunkType

        #region IsBundled

        /// <summary>
        /// True if the document is bundled, false otherwise
        /// </summary>
        //[DataMember]
        public bool IsBundled { get; internal set; }

        #endregion IsBundled

        #region Version

        /// <summary>
        /// Gets the version of the dirm information
        /// </summary>
        //[DataMember]
        public int Version { get; internal set; }

        #endregion Version

        #region Components

        private DirmComponent[] _components;

        /// <summary>
        /// Gets the dirm components
        /// </summary>
        public DirmComponent[] Components
        {
            get
            {
                if (_isInitialized == false)
                {
                    using (DjvuReader reader = Reader.CloneReader(_dataLocation, Length))
                        ReadCompressedData(reader, _components.Length, _compressedSectionLength);
                }

                return _components;
            }

            internal set
            {
                if (_components != value)
                    _components = value;
            }
        }

        #endregion Components

        #endregion Public Properties

        #region Constructors

        public DirmChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            reader.Position = Offset;
            byte flagByte = reader.ReadByte();

            // B[7]
            IsBundled = (flagByte >> 7) == 1;

            // B[6..0]
            Version = flagByte & 0x7f;

            int count = reader.ReadUInt16BigEndian();

            ReadComponentData(reader, count);
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Reads the data for the components
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="count"></param>
        internal void ReadComponentData(DjvuReader reader, int count)
        {
            List<DirmComponent> components = new List<DirmComponent>(count);

            // Read the offsets for the components
            for (int x = 0; x < count; x++)
            {
                int offset = (int) reader.ReadUInt32BigEndian();
                components.Add(new DirmComponent(offset));
            }

            _dataLocation = reader.Position;
            _isInitialized = false;
            _compressedSectionLength = (int)(Length - (reader.Position - Offset - 12));

            // Skip the bytes since this section is delayed read
            reader.Position += _compressedSectionLength;

            _components = components.ToArray();
        }

        /// <summary>
        /// Reads the compressed data from the djvu file
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="count"></param>
        /// <param name="compressedSectionLength"></param>
        internal unsafe void ReadCompressedData(DjvuReader reader, int count, int compressedSectionLength)
        {
            long prevPos = reader.Position;

            BzzReader bzReader = reader.GetBZZEncodedReader(compressedSectionLength);

            // Read the component sizes
            for (int x = 0; x < count; x++)
                _components[x].Size = bzReader.ReadInt24BigEndian();

            // Read the component flag information
            for (int x = 0; x < count; x++)
                _components[x].DecodeFlags(bzReader.ReadByte());

            // Read the component strings
            for (int x = 0; x < count; x++)
            {
                _components[x].ID = bzReader.ReadNullTerminatedString();
                if (_components[x].HasName == true)
                    _components[x].Name = bzReader.ReadNullTerminatedString();

                if (_components[x].HasTitle == true)
                    _components[x].Title = bzReader.ReadNullTerminatedString();
            }

            _isInitialized = true;

            reader.Position = prevPos;
        }

        #endregion Private Methods
    }
}