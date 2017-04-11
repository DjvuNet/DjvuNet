// <copyright file="NavmChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DjvuNet.DataChunks.Navigation;
using DjvuNet.DataChunks.Navigation.Interfaces;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class NavmChunk : IFFChunk, INavigation
    {
        #region Private Members

        private long _dataLocation = 0;

        #endregion Private Members

        #region Public Properties

        #region ChunkType

        public override ChunkType ChunkType
        {
            get { return ChunkType.Navm; }
        }

        #endregion ChunkType

        #region Bookmarks

        private Bookmark[] _bookmarks = null;

        /// <summary>
        /// Gets the list of document bookmarks
        /// </summary>
        public Bookmark[] Bookmarks
        {
            get
            {
                if (_bookmarks == null)
                    _bookmarks = ReadBookmarkData();

                return _bookmarks;
            }

            private set
            {
                if (_bookmarks != value)
                    _bookmarks = value;
            }
        }

        #endregion Bookmarks

        #endregion Public Properties

        #region Constructors

        public NavmChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document,
            string chunkID = "", long length = 0)
            : base(reader, parent, document, chunkID, length)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            _dataLocation = reader.Position;

            // Skip past the navigation data which will be delayed read
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Reads in the bookmark data
        /// </summary>
        /// <returns></returns>
        private Bookmark[] ReadBookmarkData()
        {
            using (DjvuReader reader = Reader.CloneReader(_dataLocation))
            {
                DjvuReader decompressor = reader.GetBZZEncodedReader(Length);

                int totalBookmarks = decompressor.ReadUInt16BigEndian();

                List<Bookmark> bookmarks = new List<Bookmark>();

                // Read in all the bookmarks
                while (bookmarks.Count + bookmarks.Sum(x => x.TotalBookmarks) != totalBookmarks)
                {
                    bookmarks.Add(new Bookmark(decompressor, Document, null));
                }

                return bookmarks.ToArray();
            }
        }

        #endregion Private Methods
    }
}