// <copyright file="NavmChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System.Collections.Generic;
using System.Linq;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class NavmChunk : DjvuNode, INavigation, INavmChunk
    {

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Navm; }
        }

        #endregion ChunkType

        #region Bookmarks

        private List<IBookmark> _bookmarks = null;

        /// <summary>
        /// Gets the list of document bookmarks
        /// </summary>
        public IReadOnlyList<IBookmark> Bookmarks
        {
            get
            {
                if (_bookmarks == null)
                {
                    _bookmarks = ReadBookmarkData();
                }

                return _bookmarks;
            }

            private set
            {
                if (_bookmarks != value)
                {
                    _bookmarks = (List<IBookmark>)value;
                }
            }
        }

        #endregion Bookmarks

        #endregion Public Properties

        #region Constructors

        public NavmChunk(IDjvuReader reader, IDjvuElement parent, IDjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        public NavmChunk(IDjvuWriter writer, IDjvuElement parent, long length = 0)
            : base(writer, parent, length)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Reads in the bookmark data
        /// </summary>
        /// <returns></returns>
        internal List<IBookmark> ReadBookmarkData()
        {
            using (IDjvuReader reader = Reader.CloneReader(DataOffset))
            {
                IDjvuReader decompressor = reader.GetBZZEncodedReader(Length);
                int totalBookmarks = decompressor.ReadUInt16BigEndian();

                List<IBookmark> bookmarks = new List<IBookmark>();

                // TODO improve parsing - slow while condition
                // Read in all the bookmarks
                while (bookmarks.Count + bookmarks.Sum(x => x.TotalBookmarks) != totalBookmarks)
                {
                    bookmarks.Add(new Bookmark(decompressor, Document, this, null));
                }

                return bookmarks;
            }
        }

        #endregion Methods
    }
}
