// <copyright file="FormChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using DjvuNet.DataChunks.Enums;

namespace DjvuNet.DataChunks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class FormChunk : IFFChunk
    {
        #region Public Properties

        #region ChunkType

        public override ChunkTypes ChunkType
        {
            get { return ChunkTypes.Form; }
        }

        #endregion ChunkType

        #region Children

        private IFFChunk[] _children = new IFFChunk[0];

        /// <summary>
        /// Gets the children chunks for this chunk
        /// </summary>
        public IFFChunk[] Children
        {
            get
            {
                return _children;
            }

            private set
            {
                if (Children != value)
                {
                    _children = value;
                }
            }
        }

        #endregion Children

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

        #region DirmData

        private DirmChunk _dirmData;

        /// <summary>
        /// Gets the DIRM chunk for the form
        /// </summary>
        public DirmChunk DirmData
        {
            get
            {
                if (_dirmData == null && Children != null)
                {
                    _dirmData = (DirmChunk)Children.FirstOrDefault(x => x.ChunkType == ChunkTypes.Dirm);
                }

                return _dirmData;
            }
        }

        #endregion DirmData

        #region NavmData

        private NavmChunk _NavmData;

        /// <summary>
        /// Gets the Navm chunk for the form
        /// </summary>
        public NavmChunk NavmData
        {
            get
            {
                if (_NavmData == null && Children != null)
                {
                    _NavmData = (NavmChunk)Children.FirstOrDefault(x => x.ChunkType == ChunkTypes.Navm);
                }

                return _NavmData;
            }
        }

        #endregion NavmData

        #region IncludedItems

        /// <summary>
        /// Gets the included items for the form
        /// </summary>
        public InclChunk[] IncludedItems
        {
            get { return Children.Where(x => x is InclChunk).Cast<InclChunk>().ToArray(); }
        }

        #endregion IncludedItems

        #endregion Public Properties

        #region Constructors

        public FormChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
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
            return string.Format("N:{0}; L:{1}; O:{2}; C:{3}", Name, Length, Offset, Children.Length);
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            if (Length > 0)
            {
                ReadChildren(reader);
            }
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Decodes the children of this chunk
        /// </summary>
        /// <param name="reader"></param>
        private void ReadChildren(DjvuReader reader)
        {
            List<IFFChunk> children = new List<IFFChunk>();

            // Read in all the chunks
            while (reader.Position < Offset + Length + 8)
            {
                if (reader.Position % 2 == 1)
                {
                    reader.Position++;
                }

                // Read the chunk ID
                string id = reader.ReadUTF8String(4);
                ChunkTypes type = IFFChunk.GetChunkType(id);

                // Reset the stream position
                reader.Position -= 4;

                var chunk = IFFChunk.BuildIFFChunk(reader, Document, this, type);

                if (chunk != null)
                {
                    children.Add(chunk);
                }
            }

            Children = children.ToArray();
        }

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