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

        protected byte[] _ChunkData;
        protected bool _IsDirty;

        #region Public Properties

        /// <summary>
        /// Holds a value indicating constant for each Node difference
        /// between DataOffset and NodeOffset. Most nodes will have it
        /// at 8 bytes, while FORM type nodes (elements) will have 12 bytes.
        /// </summary>
        public virtual uint OffsetDiff { get { return 8; } }

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
        public virtual long Length { get; set; }

        /// <summary>
        /// Gets the chunk identifier
        /// </summary>
        public virtual string ChunkID { get; set; }

        /// <summary>
        /// Gets the offset in the file to the start of the chunk data
        /// </summary>
        public virtual long DataOffset { get; set; }

        /// <summary>
        /// Gets the offset in the file to the start of the chunk.
        /// </summary>
        public virtual long NodeOffset { get; set; }

        /// <summary>
        /// Gets the name of the chunk
        /// </summary>
        //[DataMember]
        public virtual string Name { get { return ChunkType.ToString(); } }

        public virtual bool IsDirty { get; set; }

        public virtual bool IsInitialized { get; internal set; }

        public virtual byte[] ChunkData
        {
            get
            {
                if (_ChunkData != null)
                    return _ChunkData;
                else
                {
                    // Read without side effects
                    long prevPos = Reader.Position;
                    Reader.Position = DataOffset;

                    try
                    {
                        _ChunkData = Reader.ReadBytes((int)Length);
                    }
                    finally
                    {
                        Reader.Position = prevPos;
                    }

                    return _ChunkData;
                }
            }
            set
            {
                if (_ChunkData != value)
                {
                    _ChunkData = value;
                    _IsDirty = true;    
                }
            }
        }

        public virtual IDjvuRootElement RootElement { get; set; }

        /// <summary>
        /// Gets the root Djvu document for the form
        /// </summary>
        public virtual IDjvuDocument Document { get; set; }

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
        /// Gets all the children items of the given type
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <returns></returns>
        public virtual TItem[] GetChildrenItems<TItem>() where TItem : DjvuNode
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

        /// <summary>
        /// Reads the data for the chunk
        /// </summary>
        /// <param name="reader"></param>
        public abstract void ReadData(IDjvuReader reader);

        /// <summary>
        /// Writes node data using passed writer and advances 
        /// writer position by the length of bytes written.
        /// In the case writeHeader parameter is true node
        /// first writes it's ID, than length in Big Endian
        /// uint format followed by node data.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="writeHeader"></param>
        public virtual void WriteData(IDjvuWriter writer, bool writeHeader = true)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (writeHeader)
            {
                writer.WriteUTF8String(Name.ToUpper());
                writer.WriteUInt32BigEndian((uint)Length);
            }

            writer.Write(ChunkData, 0, (int)Length);
        }

        #endregion Public Methods

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