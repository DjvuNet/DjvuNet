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
        protected DjvuReader Reader
        {
            get
            {
                return _reader;
            }

            private set
            {
                //if (ValidateReader(value) == false) return;
                if (Reader != value)
                {
                    _reader = value;
                }
            }
        }

        #endregion Reader

        #endregion Protected Properties

        #region Public Properties

        #region ChunkType

        /// <summary>
        /// Gets the chunk type
        /// </summary>
        public abstract ChunkTypes ChunkType
        {
            get;
        }

        #endregion ChunkType

        #region Parent

        private IFFChunk _parent;

        /// <summary>
        /// Gets the parent for the IFF chunk
        /// </summary>
        public IFFChunk Parent
        {
            get { return _parent; }

            private set
            {
                if (Parent != value)
                {
                    _parent = value;
                }
            }
        }

        #endregion Parent

        #region Length

        private int _length;

        /// <summary>
        /// Gets the length of the chunk data
        /// </summary>
        public int Length
        {
            get { return _length; }

            protected set
            {
                if (Length != value)
                {
                    _length = value;
                }
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

            private set
            {
                if (ChunkID != value)
                {
                    _chunkID = value;
                }
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

            protected set
            {
                if (Offset != value)
                {
                    _offset = value;
                }
            }
        }

        #endregion Offset

        #region Name

        /// <summary>
        /// Gets the name of the chunk
        /// </summary>
        //[DataMember]
        public string Name
        {
            get
            {
                return ChunkType.ToString().ToUpper().Replace("_", ":");
            }
        }

        #endregion Name

        #region IsSubFormChunk

        /// <summary>
        /// True if the chunk is a sub form chunk, false otherwise
        /// </summary>
        public bool IsSubFormChunk
        {
            get
            {
                return ChunkType == ChunkTypes.Form_Djvu || ChunkType == ChunkTypes.Form_Djvi ||
                       ChunkType == ChunkTypes.Form_Thum || ChunkType == ChunkTypes.Form_Djvm;
            }
        }

        #endregion IsSubFormChunk

        #region Document

        private DjvuDocument _document;

        /// <summary>
        /// Gets the root Djvu document for the form
        /// </summary>
        public DjvuDocument Document
        {
            get { return _document; }

            private set
            {
                if (Document != value)
                {
                    _document = value;
                }
            }
        }

        #endregion Document

        #endregion Public Properties

        #region Constructors

        /// <summary>
        /// Initializes the IFFChunk
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="parent"></param>
        public IFFChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
        {
            _reader = reader;
            _parent = parent;
            _document = document;

            // Move back 4 to compensate for the chunk type already read
            _offset = reader.Position - 4;

            ReadChunkHeader(reader);
            ReadChunkData(reader);
        }

        #endregion Constructors

        #region Public Methods

        public DjvuReader GetReader()
        {
            return Reader;
        }

        /// <summary>
        /// Builds the appropriate chunk for the ID
        /// </summary>
        /// <returns></returns>
        public static IFFChunk BuildIFFChunk(DjvuReader reader, DjvuDocument rootDocument, IFFChunk parent, ChunkTypes chunkType)
        {
            IFFChunk result = null;

            if (chunkType == ChunkTypes.Form) result = new FormChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Form_Djvm) result = new DjvmChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Form_Djvu) result = new DjvuChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Form_Djvi) result = new DjviChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Form_Thum) result = new ThumChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Dirm) result = new DirmChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Navm) result = new NavmChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Anta) result = new AntaChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Antz) result = new AntzChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Txta) result = new TxtaChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Txtz) result = new TxtzChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Djbz) result = new DjbzChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Sjbz) result = new SjbzChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.FG44) result = new FG44Chunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.BG44) result = new BG44Chunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.TH44) result = new TH44Chunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.WMRM) result = new WmrmChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.FGbz) result = new FGbzChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Info) result = new InfoChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Incl) result = new InclChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.BGjp) result = new BGjpChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.FGjp) result = new FGjpChunk(reader, parent, rootDocument);
            else if (chunkType == ChunkTypes.Smmr) result = new SmmrChunk(reader, parent, rootDocument);
            else result = new UnknownChunk(reader, parent, rootDocument);

            //Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// Gets the chunk type based on the ID
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static ChunkTypes GetChunkType(string ID)
        {
            string lowerID = ID.ToLower();

            var values = Enum.GetValues(typeof(ChunkTypes)).Cast<ChunkTypes>();

            foreach (ChunkTypes value in values)
            {
                if (value.ToString().ToLower().Replace("form_", "") == lowerID)
                {
                    return value;
                }
            }

            return ChunkTypes.Unknown;
            //throw new Exception("Unknown chunk type: " + ID);
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
            return string.Format("N:{0}; O:{1};", Name, Offset);
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
        private T[] GetChildrenItems<T>(IFFChunk page) where T : IFFChunk
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

        /// <summary>
        /// Reads in the chunk data
        /// </summary>
        /// <param name="reader"></param>
        private void ReadChunkHeader(DjvuReader reader)
        {
            ChunkID = reader.ReadUTF8String(4);

            if (IsSubFormChunk == false)
            {
                Length = reader.ReadInt32MSB();
            }
        }

        #endregion Private Methods
    }
}