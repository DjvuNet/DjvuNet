// <copyright file="FormChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DjvuNet.Errors;
using DjvuNet.Parser;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public abstract class DjvuFormElement : DjvuNode, IDjvuElement
    {

        protected List<IDjvuNode> _TempChildren;

        protected List<IDjvuNode> _Children;

        protected IDjvuNode _FirstSibling;
        protected IDjvuNode _LastSibling;
        protected IDjvuNode _NextSibling;
        protected IDjvuNode _PreviousSibling;

        public override uint OffsetDiff { get { return 12; } }

        #region Public Properties

        /// <summary>
        /// Gets the children chunks for this chunk
        /// </summary>
        public IReadOnlyList<IDjvuNode> Children
        {
            get { return _Children; }

            internal set { _Children = (List<IDjvuNode>) value; }
        }

        private IDjvuReader _data;

        /// <summary>
        /// Gets the raw chunk data
        /// </summary>
        public IDjvuReader Data
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

        public IDjvuNode PreviousSibling
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

        public IDjvuNode NextSibling
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

        public IDjvuNode FirstSibling
        {
            get
            {
                if (_FirstSibling != null)
                    return _FirstSibling;
                else
                {
                    var parent = Parent as IDjvuElement;
                    if (parent != null && parent.Children?.Count > 0)
                        _FirstSibling = parent.Children[0];
                    return _FirstSibling;
                }   
            }

            set
            {
                if (value != null)
                {
                    if (Parent == null)
                        throw new DjvuInvalidOperationException("Parent cannot be null for not null sibling.");

                    _FirstSibling = value;
                }
                else if (Parent != null)
                {
                    throw new DjvuInvalidOperationException("Parent has to be null if first sibling is null.");
                }
                else
                    _FirstSibling = value;
            }
        }

        public IDjvuNode LastSibling
        {
            get
            {
                if (_LastSibling != null)
                    return _LastSibling;
                else
                {
                    var parent = (IDjvuElement)Parent;
                    if (parent != null && parent.Children.Count > 0)
                        _LastSibling = parent.Children[parent.Children.Count - 1];
                    return _LastSibling;
                }
            }

            set
            {
                if (value != null)
                {
                    if (Parent == null)
                        throw new DjvuInvalidOperationException("Parent cannot be null for not null sibling.");

                    _LastSibling = value;
                }
                else if (Parent != null)
                {
                    throw new DjvuInvalidOperationException("Parent has to be null if first sibling is null.");
                }
                else
                    _LastSibling = value;
            }
        }

        #endregion Public Properties

        #region Constructors

        public DjvuFormElement()
        {
            InitializeInternal();
        }

        public DjvuFormElement(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
            InitializeInternal();
        }

        public DjvuFormElement(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base (writer, parent, length)
        {
            InitializeInternal();
        }

        protected virtual void InitializeInternal()
        {
            _Children = new List<IDjvuNode>();
            _TempChildren = new List<IDjvuNode>();
        }

        #endregion Constructors

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
                throw new DjvuArgumentNullException(nameof(writer));

            AdjustAlignment(writer);

            if (writeHeader)
            {
                writer.WriteUTF8String("FORM");

                uint length = (uint) GetDataLength();

                writer.WriteUInt32BigEndian(length);
                writer.WriteUTF8String(Name);
            }

            foreach (IDjvuNode node in Children)
            {
                AdjustAlignment(writer);
                node.WriteData(writer, writeHeader);
            }
        }

        public override long GetDataLength()
        {
            uint length = 0;
            foreach (IDjvuNode node in Children)
            {
                uint increment = ((uint)node.GetDataLength() + node.OffsetDiff);
                length += (increment + increment % 2);
            }

            Length = length;

            return length;
        }

        /// <summary>
        /// Decodes the children of this chunk
        /// </summary>
        /// <param name="reader"></param>
        internal virtual void ReadChildren(IDjvuReader reader)
        {
            _TempChildren = new List<IDjvuNode>();

            long maxPosition = this.Length + DataOffset;

            // Jump to next FORM data "space" - it is skipped for root FORM
            if (this != Document.RootForm)
            {
                reader.Position = DataOffset;
                if (DjvuParser.IsFormChunk(ChunkType))
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
                ChunkType type = DjvuParser.GetChunkType(id);
                long length = reader.ReadUInt32BigEndian();

                bool isFormChunk = DjvuParser.IsFormChunk(type);

                if (isFormChunk)
                {
                    id = reader.ReadUTF8String(4);
                    type = DjvuParser.GetChunkType(id);
                }

                // Reset the stream position
                // reader.Position -= 4;

                var chunk = DjvuParser.CreateDecodedDjvuNode(reader, Document, this, type, id, length);

                if (chunk != null)
                {
                    if (!DjvuParser.IsRootFormChild(type))
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
        internal IDjvuReader ExtractRawData()
        {
            return Reader.CloneReader(DataOffset + 4, Length);
        }

        public int AddChild(IDjvuNode node)
        {
            _Children.Add(node);
            return _Children.Count;
        }

        public void ClearChildren()
        {
            _Children.Clear();
        }

        public bool ContainsChild(IDjvuNode node)
        {
            return _Children.Contains(node);
        }

        public bool ContainsChild(IDjvuNode node, IEqualityComparer<IDjvuNode> comparer)
        {
            return _Children.Contains(node, comparer);
        }

        public int InsertChild(int index, IDjvuNode node)
        {
            _Children.Insert(index, node);
            return _Children.Count;
        }

        public bool RemoveChild(IDjvuNode node)
        {
            return _Children.Remove(node);
        }

        public int RemoveChildAt(int index)
        {
            _Children.RemoveAt(index);
            return _Children.Count;
        }

        #endregion Public Methods
    }
}