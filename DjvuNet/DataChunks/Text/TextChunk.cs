// <copyright file="TextChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Text;


namespace DjvuNet.DataChunks
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public abstract class TextChunk : DjvuNode, ITextChunk
    {
        #region Private Members

        private bool _isDecoded = false;

        #endregion Private Members

        #region Public Properties

        public override ChunkType ChunkType { get { return ChunkType.Text; } }

        #region TextLength

        private int _textLength;

        /// <summary>
        /// Gets the length of the text
        /// </summary>
        public int TextLength
        {
            get
            {
                DecodeIfNeeded();
                return _textLength;
            }

            internal set
            {
                if (_textLength != value)
                    _textLength = value;
            }
        }

        #endregion TextLength

        #region Text

        private string _text;

        /// <summary>
        /// Gets the text for the chunk
        /// </summary>
        public string Text
        {
            get
            {
                if (_text == null)
                    DecodeIfNeeded();

                return _text;
            }

            internal set
            {
                if (_text != value)
                    _text = value;
            }
        }

        #endregion Text

        #region Version

        private byte _version;

        /// <summary>
        /// Gets the version of the text chunk
        /// </summary>
        public int Version
        {
            get
            {
                DecodeIfNeeded();
                return _version;
            }

            internal set
            {
                if (_version != value)
                    _version = (byte) value;
            }
        }

        #endregion Version

        #region Zone

        private TextZone _zone;

        /// <summary>
        /// Gets the text zone for the chunk
        /// </summary>
        public TextZone Zone
        {
            get
            {
                DecodeIfNeeded();
                return _zone;
            }

            internal set
            {
                if (_zone != value)
                    _zone = value;
            }
        }

        #endregion Zone

        #endregion Public Properties

        #region Constructors

        public TextChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public TextChunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Gets the reader for the text data
        /// </summary>
        /// <returns></returns>
        internal abstract IDjvuReader GetTextDataReader(long position);

        /// <summary>
        /// Decodes the compressed data if needed
        /// </summary>
        internal void DecodeIfNeeded()
        {
            if (!_isDecoded)
                ReadCompressedTextData();
        }

        /// <summary>
        /// Reads the compressed text data
        /// </summary>
        internal void ReadCompressedTextData()
        {
            if (Length > 0)
            {
                using (IDjvuReader reader = GetTextDataReader(DataOffset))
                {
                    int length = (int)reader.ReadUInt24BigEndian();
                    byte[] textBytes = reader.ReadBytes(length);
                    Text = Encoding.UTF8.GetString(textBytes);
                    TextLength = _text.Length;
                    Version = reader.ReadByte();

                    Zone = new TextZone(reader, null, null, this);
                }
            }
            _isDecoded = true;
        }

        #endregion Methods
    }
}