// -----------------------------------------------------------------------
// <copyright file="Bookmark.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace DjvuNet.DataChunks.Navigation
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Bookmark
    {
        #region Private Members

        #endregion Private Members

        #region Public Properties

        #region Name

        /// <summary>
        /// Gets the name of the bookmark
        /// </summary>
        public string Name { get; internal set; }

        #endregion Name

        #region Url

        /// <summary>
        /// Gets the url of the bookmark
        /// </summary>
        public string Url { get; internal set; }

        #endregion Url

        #region Children

        /// <summary>
        /// Gets the children bookmarks
        /// </summary>
        public Bookmark[] Children { get; internal set; }

        #endregion Children

        #region Parent

        /// <summary>
        /// Gets the parent bookmark for this item
        /// </summary>
        public Bookmark Parent { get; internal set; }

        #endregion Parent

        #region TotalBookmarks

        /// <summary>
        /// Gets the total number of children bookmarks
        /// </summary>
        public int TotalBookmarks
        {
            get { return Children.Length + Children.Sum(x => x.TotalBookmarks); }
        }

        #endregion TotalBookmarks

        #region ReferencedPage

        /// <summary>
        /// Gets the page this bookmark references
        /// </summary>
        public DjvuPage ReferencedPage { get; internal set; }

        #endregion ReferencedPage

        #region Document

        /// <summary>
        /// Gets the document this bookmark pertains to
        /// </summary>
        public DjvuDocument Document { get; internal set; }

        #endregion Document

        #endregion Public Properties

        #region Constructors

        public Bookmark(DjvuReader reader, DjvuDocument document, Bookmark parent)
        {
            Document = document;
            Parent = parent;
            DecodeBookmarkData(reader);

            LoadReferencedPage();
        }

        public Bookmark(DjvuDocument document, Bookmark parent, string name, string url, Bookmark[] children)
        {
            Document = document;
            Parent = parent;
            Name = name;
            Url = url;
            Children = children;

            LoadReferencedPage();
        }

        #endregion Constructors

        #region Public Methods

        public override string ToString()
        {
            return string.Format("{0} : {1}", Name, Url);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Load the page this bookmark references
        /// </summary>
        private void LoadReferencedPage()
        {
            int pageNumber = 0;

            if (int.TryParse(Url, out pageNumber) || (Url.StartsWith("#") && int.TryParse(Url.Substring(1), out pageNumber)))
            {
                pageNumber--;

                if (pageNumber < 0 || pageNumber >= Document.Pages.Length)
                {
                    throw new InvalidOperationException("Navigation URL is out of range: " + Url);
                }

                ReferencedPage = Document.Pages[pageNumber];
            }
            else
            {
                throw new InvalidOperationException("Navigation URL in unknown format: " + Url);
            }
        }

        /// <summary>
        /// Loads the bookmark data
        /// </summary>
        /// <param name="reader"></param>
        private void DecodeBookmarkData(DjvuReader reader)
        {
            int childrenCount = reader.ReadByte();

            int textSize = reader.ReadInt24MSB();
            Name = reader.ReadUTF8String(textSize);

            int urlSize = reader.ReadInt24MSB();
            Url = reader.ReadUTF8String(urlSize);

            // Read in all the children bookmarks
            List<Bookmark> children = new List<Bookmark>();
            for (int x = 0; x < childrenCount; x++)
            {
                children.Add(new Bookmark(reader, Document, this));
            }
            Children = children.ToArray();
        }

        #endregion Private Methods
    }
}