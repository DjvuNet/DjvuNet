// <copyright file="IFFChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DjvuNet.DataChunks.Directory;
using DjvuNet.DataChunks.Enums;

namespace DjvuNet.DataChunks
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public abstract class IFFChunk
    {
        #region Protected Properties

        #region Reader

        private DjvuReader _reader;

        /// <summary>
        /// Gets the reader for the chunk data
        /// </summary>
        //[DataMember]
        internal DjvuReader Reader
        {
            get { return _reader; }

            set
            {
                if (Reader != value)
                    _reader = value;
            }
        }

        #endregion Reader

        #endregion Protected Properties

        #region Public Properties

        #region ChunkType

        /// <summary>
        /// Gets the chunk type
        /// </summary>
        public abstract ChunkType ChunkType { get; }

        #endregion ChunkType

        #region Parent

        private IFFChunk _parent;

        /// <summary>
        /// Gets the parent for the IFF chunk
        /// </summary>
        public IFFChunk Parent
        {
            get { return _parent; }

            internal set
            {
                if (Parent != value)
                    _parent = value;
            }
        }

        #endregion Parent

        #region Length

        private long _length;

        /// <summary>
        /// Gets the length of the chunk data
        /// </summary>
        public long Length
        {
            get { return _length; }

            internal set
            {
                if (Length != value)
                    _length = value;
            }
        }

        #endregion Length

        #region ChunkID

        private string _chunkID;

        /// <summary>
        /// Gets the chunk identifier
        /// </summary>
        public string ChunkID
        {
            get { return _chunkID; }

            internal set
            {
                if (ChunkID != value)
                    _chunkID = value;
            }
        }

        #endregion ChunkID

        #region Offset

        private long _offset;

        /// <summary>
        /// Gets the offset to the start of the chunk data
        /// </summary>
        public long Offset
        {
            get { return _offset; }

            internal set
            {
                if (Offset != value)
                    _offset = value;
            }
        }

        #endregion Offset

        #region Name

        /// <summary>
        /// Gets the name of the chunk
        /// </summary>
        //[DataMember]
        public string Name { get { return ChunkType.ToString(); } }

        #endregion Name

        /// <summary>
        /// True if the chunk is a sub form chunk, false otherwise
        /// </summary>
        public bool IsFormChunk (ChunkType type)
        {
            return type == ChunkType.Djvu || type == ChunkType.Djvi ||
                    type == ChunkType.Thum || type == ChunkType.Djvm || type == ChunkType.Form;
        }

        public bool IsRootFormChild(ChunkType type)
        {
            return IsFormChunk(type) || type == ChunkType.Dirm;
        }

        #region Document

        private DjvuDocument _document;

        /// <summary>
        /// Gets the root Djvu document for the form
        /// </summary>
        public DjvuDocument Document
        {
            get { return _document; }

            internal set
            {
                if (Document != value)
                    _document = value;
            }
        }

        #endregion Document

        #endregion Public Properties

        #region Constructors

        /// <summary>
        /// IFFChunk constructor
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="parent"></param>
        /// <param name="document"></param>
        /// <param name="chunkID"></param>
        /// <param name="length"></param>
        public IFFChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document, 
            string chunkID = "", long length = 0)
        {
            _reader = reader;
            _parent = parent;
            _document = document;
            _length = length;
            _chunkID = chunkID;

            // Move back 4 to compensate for the chunk type already read
            _offset = reader.Position; // - 4;
        }

        /// <summary>
        /// Initialize allows to delay reading of IFFChunk content to the moment it is needed.
        /// </summary>
        /// <param name="reader"></param>
        public virtual void Initialize(DjvuReader reader)
        {
            reader.Position = Offset;
            ReadChunkData(reader);
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Builds the appropriate chunk for the ID
        /// </summary>
        /// <returns></returns>
        public static IFFChunk BuildIFFChunk(DjvuReader reader, DjvuDocument rootDocument, 
            IFFChunk parent, ChunkType chunkType,
            string chunkID = "", long length = 0)
        {
            IFFChunk result = null;

            switch (chunkType)
            {
                case ChunkType.Djvm:
                    result = new DjvmChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Djvu:
                    result = new DjvuChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Djvi:
                    result = new DjviChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Thum:
                    result = new ThumChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Dirm:
                    result = new DirmChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Navm:
                    result = new NavmChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Anta:
                    result = new AntaChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Antz:
                    result = new AntzChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Txta:
                    result = new TxtaChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Txtz:
                    result = new TxtzChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Djbz:
                    result = new DjbzChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Sjbz:
                    result = new SjbzChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.FG44:
                    result = new FG44Chunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.BG44:
                    result = new BG44Chunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.TH44:
                    result = new TH44Chunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.WMRM:
                    result = new WmrmChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.FGbz:
                    result = new FGbzChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Info:
                    result = new InfoChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Incl:
                    result = new InclChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.BGjp:
                    result = new BGjpChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.FGjp:
                    result = new FGjpChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                case ChunkType.Smmr:
                    result = new SmmrChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                default:
                    result = new UnknownChunk(reader, parent, rootDocument, chunkID, length);
                    break;
            }
           
            return result;
        }

        /// <summary>
        /// Gets the chunk type based on the ID
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static ChunkType GetChunkType(string ID)
        {
            try
            {
                return (ChunkType)Enum.Parse(typeof(ChunkType), ID, true);
            }
            catch(ArgumentException)  // Catch unsupported chunks i.e. "CIDa"
            {
            }

            return ChunkType.Unknown;
        }

        /// <summary>
        /// Gets all the children items of the given type
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <returns></returns>
        public TItem[] GetChildrenItems<TItem>() where TItem : IFFChunk
        {
            return GetChildrenItems<TItem>(this);
        }

        /// <summary>
        /// Gets the string representation of the item
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"IFF {{ Name:{Name}; ID:{ChunkID} Offset:{Offset} }}";
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Reads the data for the chunk
        /// </summary>
        /// <param name="reader"></param>
        protected abstract void ReadChunkData(DjvuReader reader);

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Gets all the children items of the given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="page"></param>
        /// <returns></returns>
        internal T[] GetChildrenItems<T>(IFFChunk page) where T : IFFChunk
        {
            // Check if this is a thumbnail
            if (page is T)
            {
                return new T[] { (T)page };
            }

            // No items if not form
            if (page is FormChunk == false)
            {
                return new T[0];
            }

            List<T> results = new List<T>();
            FormChunk form = (FormChunk)page;

            foreach (IFFChunk chunk in form.Children)
            {
                results.AddRange(GetChildrenItems<T>(chunk));
            }

            return results.ToArray();
        }

        #endregion Private Methods
    }
}