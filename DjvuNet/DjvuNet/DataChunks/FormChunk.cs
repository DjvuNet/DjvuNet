// <copyright file="FormChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks.Enums;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public abstract class FormChunk : IFFChunk
    {
        #region Public Properties

        #region TempChildren

        protected List<IFFChunk> TempChildren;

        #endregion TempChildren

        #region Children

        private IFFChunk[] _children = new IFFChunk[0];

        /// <summary>
        /// Gets the children chunks for this chunk
        /// </summary>
        public IFFChunk[] Children
        {
            get { return _children; }

            internal set
            {
                if (Children != value)
                    _children = value;
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
                    _data = ExtractRawData();

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
                    _dirmData = (DirmChunk)Children.FirstOrDefault<IFFChunk>(x => x.ChunkType == ChunkType.Dirm);

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
                if (Children != null && Children.Length > 0 && _NavmData == null )
                    _NavmData = (NavmChunk)Children.FirstOrDefault<IFFChunk>(x => x.ChunkType == ChunkType.Navm);

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

        protected FormChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        public static FormChunk GetRootForm(DjvuReader reader, IFFChunk parent, DjvuDocument document)
        {
            string formStr = reader.ReadUTF8String(4);
            // use of int in Djvu Format limits file size to 2 GB - should be long
            int length = (int) reader.ReadUInt32MSB(); 
            string formType = reader.ReadUTF8String(4);
            reader.Position -= 4;

            ChunkType type = IFFChunk.GetChunkType(formType);

            FormChunk formObj = (FormChunk)IFFChunk.BuildIFFChunk(reader, document, parent, type, formType, length);

            return formObj;
        }


        public override void Initialize(DjvuReader reader)
        {
            reader.Position = Offset + 4;
            ReadChunkData(reader);
        }

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

        protected void AddRootFormChild(FormChunk form)
        {

        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Decodes the children of this chunk
        /// </summary>
        /// <param name="reader"></param>
        internal void ReadChildren(DjvuReader reader)
        {
            TempChildren = new List<IFFChunk>();

            long maxPosition = this.Length + Offset;

            // Jump to next FORM data "space" - it is skipped for root FORM
            if (this != Document.RootForm)
            {
                reader.Position = Offset;
                if (IsFormChunk(ChunkType))
                    maxPosition -= 4;
            }

            // Read in all the chunks
            while (reader.Position <  maxPosition)
            {
                if (reader.Position % 2 == 1)
                {
                    reader.Position++;
                }

                // Read the chunk ID
                string id = reader.ReadUTF8String(4);
                ChunkType type = IFFChunk.GetChunkType(id);
                long length = reader.ReadInt32MSB();

                bool isFormChunk = IsFormChunk(type);

                if (isFormChunk)
                {
                    id = reader.ReadUTF8String(4);
                    type = IFFChunk.GetChunkType(id);
                }

                // Reset the stream position
                // reader.Position -= 4;

                var chunk = IFFChunk.BuildIFFChunk(reader, Document, this, type, id, length);

                if (chunk != null)
                {
                    if (!IsRootFormChild(type))
                    {
                        TempChildren.Add(chunk);
                        TempChildren[TempChildren.Count - 1].Initialize(reader);
                        reader.Position = chunk.Length + chunk.Offset;
                    }
                    else
                    {
                        Document.RootForm.TempChildren.Add(chunk);
                        reader.Position = chunk.Length + chunk.Offset;
                        if (isFormChunk)
                            reader.Position -= 4;
                    }
                }                
            }

            Children = TempChildren.ToArray();
            TempChildren = null;
        }

        /// <summary>
        /// Extracts the raw data from the chunk
        /// </summary>
        /// <returns></returns>
        internal DjvuReader ExtractRawData()
        {
            // Read the data in
            return Reader.CloneReader(Offset + 4 + 4, Length);
        }

        #endregion Private Methods
    }
}