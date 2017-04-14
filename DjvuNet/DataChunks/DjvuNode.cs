// <copyright file="IFFChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DjvuNet;

namespace DjvuNet.DataChunks
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public abstract class DjvuNode : IDjvuNode
    {
        /// <summary>
        /// Holds a value indicating constant for each Node difference
        /// between DataOffset and NodeOffset. Most nodes will have it
        /// at 8 bytes, while FORM type nodes (elements) will have 12 bytes.
        /// </summary>
        protected virtual int OffsetDiff { get { return 8; } }

        #region Public Properties

        /// <summary>
        /// Gets the reader for the chunk data
        /// </summary>
        //[DataMember]
        public IDjvuReader Reader { get; set; }

        /// <summary>
        /// Gets the chunk type
        /// </summary>
        public abstract ChunkType ChunkType { get; }

        /// <summary>
        /// Gets the parent for the IFF chunk
        /// </summary>
        public virtual IDjvuElement Parent { get; set; }

        /// <summary>
        /// Gets the length of the chunk data
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// Gets the chunk identifier
        /// </summary>
        public string ChunkID { get; internal set; }

        /// <summary>
        /// Gets the offset in the file to the start of the chunk data
        /// </summary>
        public virtual long DataOffset { get; set; }

        /// <summary>
        /// Gets the offset in the file to the start of the chunk.
        /// The difference 
        /// </summary>
        public virtual long NodeOffset { get; set; }

        /// <summary>
        /// Gets the name of the chunk
        /// </summary>
        //[DataMember]
        public string Name { get { return ChunkType.ToString(); } }

        /// <summary>
        /// True if the chunk is a sub form chunk, false otherwise
        /// </summary>
        public static bool IsFormChunk(ChunkType type)
        {
            switch (type)
            {
                case ChunkType.Djvu:
                case ChunkType.Djvi:
                case ChunkType.Thum:
                case ChunkType.Djvm:
                case ChunkType.Form:
                    return true;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Three types of FORM chunks could form a root of DjVu document / file:
        /// DJVU for single page documents, DJVM for multi page documents 
        /// and DJVI for include files associated with separate multi page 
        /// document - include files can not function independently (?).
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsRootFormChild(ChunkType type, ChunkType rootType = ChunkType.Djvm)
        {
            if (rootType == ChunkType.Djvm)
            {
                switch (type)
                {
                    case ChunkType.Djvu:
                    case ChunkType.Djvi:
                    case ChunkType.Thum:
                    case ChunkType.Form:
                    case ChunkType.Dirm:
                    case ChunkType.Navm:
                        return true;
                    default:
                        return false;
                }
            }
            else if (rootType == ChunkType.Djvu)
            {
                switch (type)
                {
                    case ChunkType.Djvu:
                    case ChunkType.Djvi:
                    case ChunkType.Djvm:
                    case ChunkType.Thum:
                    case ChunkType.Form:
                    case ChunkType.Dirm:
                    case ChunkType.Navm:
                        return false;
                }
            }

            // TODO Implement other single file docs??? 

            // DJVM form is the only chunk type which is always in document root
            if (type == ChunkType.Djvm) return false;

            return IsFormChunk(type) || type == ChunkType.Dirm || 
                type == ChunkType.Navm ;
        }

        public virtual bool IsInitialized { get; internal set; }

        public virtual byte[] ChunkData { get; set; }

        public IDjvuRootElement RootElement { get; set; }

        /// <summary>
        /// Gets the root Djvu document for the form
        /// </summary>
        public IDjvuDocument Document { get; internal set; }

        #endregion Public Properties

        #region Constructors

        /// <summary>
        /// IFFChunk parameterless constructor mainly used for testing
        /// </summary>
        protected DjvuNode() { }

        /// <summary>
        /// IFFChunk constructor
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="parent"></param>
        /// <param name="document"></param>
        /// <param name="chunkID"></param>
        /// <param name="length"></param>
        public DjvuNode(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document, 
            string chunkID = "", long length = 0)
        {
            Reader = reader;
            Parent = parent;
            Document = document;
            Length = length;
            ChunkID = chunkID;
            DataOffset = reader.Position;
        }

        public virtual void Initialize()
        {
            Initialize(Reader);
        }

        /// <summary>
        /// Initialize allows to delay reading of IFFChunk content to the moment it is needed.
        /// </summary>
        /// <param name="reader"></param>
        public virtual void Initialize(IDjvuReader reader)
        {
            reader.Position = DataOffset;
            ReadData(reader);
            IsInitialized = true;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Gets the chunk type based on the ID
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static ChunkType GetChunkType(string ID)
        {
            try { return (ChunkType)Enum.Parse(typeof(ChunkType), ID, true); }
            catch (ArgumentException) { } // Catch unsupported chunks i.e. "LTAnno"

            return ChunkType.Unknown;
        }

        /// <summary>
        /// Builds the appropriate chunk for the ID
        /// </summary>
        /// <returns></returns>
        public static IDjvuNode BuildIffChunk(IDjvuReader reader, IDjvuDocument rootDocument,
            IDjvuElement parent, ChunkType chunkType,
            string chunkID = "", long length = 0)
        {
            IDjvuNode result = null;

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
                case ChunkType.Cida:
                    result = new CidaChunk(reader, parent, rootDocument, chunkID, length);
                    break;
                default:
                    result = new UnknownChunk(reader, parent, rootDocument, chunkID, length);
                    break;
            }
           
            return result;
        }

        /// <summary>
        /// Gets all the children items of the given type
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <returns></returns>
        public TItem[] GetChildrenItems<TItem>() where TItem : DjvuNode
        {
            return GetChildrenItems<TItem>(this);
        }

        /// <summary>
        /// Gets the string representation of the item
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.GetType().Name} {{ Name: {Name}; ID: {ChunkID} Offset: {DataOffset} Length: {Length} }}";
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Reads the data for the chunk
        /// </summary>
        /// <param name="reader"></param>
        public abstract void ReadData(IDjvuReader reader);

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Gets all the children items of the given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="page"></param>
        /// <returns></returns>
        internal T[] GetChildrenItems<T>(IDjvuNode page) where T : DjvuNode
        {
            // Check if this is a thumbnail
            if (page is T)
            {
                return new T[] { (T)page };
            }

            // No items if not form
            if (page is DjvuFormElement == false)
            {
                return new T[0];
            }

            List<T> results = new List<T>();
            DjvuFormElement form = (DjvuFormElement)page;

            foreach (IDjvuNode chunk in form.Children)
            {
                results.AddRange(GetChildrenItems<T>(chunk));
            }

            return results.ToArray();
        }

        #endregion Private Methods
    }
}