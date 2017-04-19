// <copyright file="FormChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public abstract class DjvuFormElement : DjvuNode, IDjvuElement
    {

        protected List<IDjvuNode> _TempChildren;

        protected List<IDjvuNode> _Children;

        protected IDjvuElement _FirstSibling;
        protected IDjvuElement _LastSibling;
        protected IDjvuElement _NextSibling;
        protected IDjvuElement _PreviousSibling;

        protected override int OffsetDiff { get { return 12; } }

        #region Public Properties

        /// <summary>
        /// Gets the children chunks for this chunk
        /// </summary>
        public IReadOnlyList<IDjvuNode> Children
        {
            get { return _Children; }

            internal set { _Children = (List<IDjvuNode>) value; }
        }

        private DjvuReader _data;

        /// <summary>
        /// Gets the raw chunk data
        /// </summary>
        public DjvuReader Data
        {
            get
            {
                if (_data == null)
                    _data = ExtractRawData();

                return _data;
            }
        }

        /// <summary>
        /// Gets the included items for the form
        /// </summary>
        public IReadOnlyList<InclChunk> IncludedItems
        {
            get { return Children.Where(x => x.ChunkType == ChunkType.Incl).Cast<InclChunk>().ToList(); }
        }

        public IDjvuNode FirstChild
        {
            get
            {
                if (_Children?.Count > 0)
                    return _Children[0];
                else
                    return null;
            }
        }

        public IDjvuNode LastChild
        {
            get
            {
                if (_Children?.Count > 0)
                    return _Children[_Children.Count - 1];
                else
                    return null;
            }
        }

        public IDjvuElement PreviousSibling
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public IDjvuElement NextSibling
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public IDjvuElement FirstSibling
        {
            get
            {
                if (_FirstSibling != null)
                    return _FirstSibling;
                else
                {
                    var parent = Parent as IDjvuElement;
                    if (parent != null && parent.Children?.Count > 0)
                        _FirstSibling = (IDjvuElement) parent.Children[0];
                    return _FirstSibling;
                }   
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public IDjvuElement LastSibling
        {
            get
            {
                if (_LastSibling != null)
                    return _LastSibling;
                else
                {
                    var parent = (IDjvuElement)Parent;
                    if (parent != null && parent.Children?.Count > 0)
                        _LastSibling = (IDjvuElement) parent.Children[parent.Children.Count - 1];
                    return _LastSibling;
                }
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion Public Properties

        #region Constructors

        protected DjvuFormElement(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
            _Children = new List<IDjvuNode>();
            _TempChildren = new List<IDjvuNode>();
        }

        #endregion Constructors

        public static DjvuFormElement GetRootForm(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document)
        {
            string formStr = reader.ReadUTF8String(4);
            // use of int in Djvu Format limits file read size to 2 GB - should be long (int64)
            int length = (int) reader.ReadUInt32BigEndian(); 
            string formType = reader.ReadUTF8String(4);
            reader.Position -= 4;

            ChunkType type = DjvuNode.GetChunkType(formType);

            DjvuFormElement formObj = (DjvuFormElement)DjvuNode.CreateDjvuNode(reader, document, parent, type, formType, length);

            return formObj;
        }


        #region Public Methods

        public override void Initialize(IDjvuReader reader)
        {
            reader.Position = DataOffset + 4;
            ReadData(reader);
        }

        /// <summary>
        /// Gets the string representation of the item
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.GetType().Name} {{ Name: {Name}; ID: {ChunkID} Offset: {DataOffset}" + 
                $" Length: {Length} Children: {Children?.Count} }}";
        }

        public override void ReadData(IDjvuReader reader)
        {
            if (Length > 0)
                ReadChildren(reader);
        }

        /// <summary>
        /// Recursively writes element data and data of it's
        /// children elements or nodes using supplied writer.
        /// When writeHeader is true (default value) it writes
        /// own header data (ID, Length) and asks it's children
        /// to do the same. 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="writeHeader"></param>
        public override void WriteData(IDjvuWriter writer, bool writeHeader = true)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (!(bool)writer.BaseStream?.CanWrite)
                throw new ArgumentException("Writer cannot write.", nameof(writer));

            if (writeHeader)
            {
                writer.WriteUTF8String("FORM");
                writer.WriteUInt32BigEndian((uint)Length);
                writer.WriteUTF8String(Name);
            }

            foreach (IDjvuNode node in Children)
                node.WriteData(writer, writeHeader);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Decodes the children of this chunk
        /// </summary>
        /// <param name="reader"></param>
        internal void ReadChildren(IDjvuReader reader)
        {
            _TempChildren = new List<IDjvuNode>();

            long maxPosition = this.Length + DataOffset;

            // Jump to next FORM data "space" - it is skipped for root FORM
            if (this != Document.RootForm)
            {
                reader.Position = DataOffset;
                if (IsFormChunk(ChunkType))
                    maxPosition -= 4;
            }

            // Read in all the chunks
            while (true)
            {
                if (reader.Position % 2 == 1)
                {
                    reader.Position++;
                }

                if (reader.Position >= maxPosition)
                    break;

                // Read the chunk ID
                string id = reader.ReadUTF8String(4);
                ChunkType type = DjvuNode.GetChunkType(id);
                long length = reader.ReadUInt32BigEndian();

                bool isFormChunk = IsFormChunk(type);

                if (isFormChunk)
                {
                    id = reader.ReadUTF8String(4);
                    type = DjvuNode.GetChunkType(id);
                }

                // Reset the stream position
                // reader.Position -= 4;

                var chunk = DjvuNode.CreateDjvuNode(reader, Document, this, type, id, length);

                if (chunk != null)
                {
                    if (!IsRootFormChild(type))
                    {
                        _TempChildren.Add(chunk);
                        _TempChildren[_TempChildren.Count - 1].Initialize(reader);
                        reader.Position = chunk.Length + chunk.DataOffset;
                    }
                    else
                    {
                        Document.RootForm._TempChildren.Add(chunk);
                        reader.Position = chunk.Length + chunk.DataOffset;
                        if (isFormChunk)
                            reader.Position -= 4;
                    }
                }                
            }

            Children = _TempChildren;
            _TempChildren = null;
        }

        /// <summary>
        /// Extracts the raw data from the chunk
        /// </summary>
        /// <returns></returns>
        internal DjvuReader ExtractRawData()
        {
            // Read the data in
            return Reader.CloneReader(DataOffset + 4 + 4, Length);
        }

        public int AddNode(IDjvuNode node)
        {
            throw new NotImplementedException();
        }

        public void ClearChildren()
        {
            throw new NotImplementedException();
        }

        public void InsertNode(IDjvuNode node, int index)
        {
            throw new NotImplementedException();
        }

        public bool RemoveNode(IDjvuNode node)
        {
            throw new NotImplementedException();
        }

        public void RemoveNodeAt(int index)
        {
            throw new NotImplementedException();
        }

        #endregion Private Methods
    }
}