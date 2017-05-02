// -----------------------------------------------------------------------
// <copyright file="Bookmark.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace DjvuNet.DataChunks
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Bookmark : IBookmark
    {

        #region Public Properties

        /// <summary>
        /// Gets the name of the bookmark
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the url of the bookmark
        /// </summary>
        public string Url { get; internal set; }

        /// <summary>
        /// Gets the children bookmarks
        /// </summary>
        public IBookmark[] Children { get; internal set; }

        /// <summary>
        /// Gets the parent bookmark for this item
        /// </summary>
        public IBookmark Parent { get; internal set; }

        /// <summary>
        /// Gets the total number of children bookmarks
        /// </summary>
        public int TotalBookmarks
        {
            get { return Children.Length + Children.Sum(x => x.TotalBookmarks); }
        }

        /// <summary>
        /// Gets the page this bookmark references
        /// </summary>
        public IDjvuPage ReferencedPage { get; internal set; }

        /// <summary>
        /// Gets the document this bookmark pertains to
        /// </summary>
        public IDjvuDocument Document { get; internal set; }

        public INavmChunk NavmNode { get; internal set; }

        #endregion Public Properties

        #region Constructors

        public Bookmark(IDjvuReader reader, IDjvuDocument document, INavmChunk navm, Bookmark parent)
        {
            InitializeReferences(document, navm, parent);
            DecodeBookmarkData(reader);
            LoadReferencedPage();
        }

        public Bookmark(IDjvuDocument document, INavmChunk navm, Bookmark parent, string name, string url, Bookmark[] children)
        {
            InitializeReferences(document, navm, parent);

            Name = name;
            Url = url;
            Children = children;

            LoadReferencedPage();
        }

        internal void InitializeReferences(IDjvuDocument document, INavmChunk navm, Bookmark parent)
        {
            Document = document;
            Parent = parent;

            if (navm != null)
                NavmNode = navm;
            else if (parent != null)
                NavmNode = parent.NavmNode;
            else if (document != null)
                NavmNode = document.RootForm.Children
                    .Where(x => x.ChunkType == ChunkType.Navm)
                    .FirstOrDefault() as INavmChunk;
        }

        #endregion Constructors

        #region Public Methods

        public override string ToString()
        {
            return string.Format("{0} : {1}", Name, Url);
        }

        /// <summary>
        /// Load the page this bookmark references
        /// </summary>
        internal void LoadReferencedPage()
        {
            int pageNumber = 0;

            if (int.TryParse(Url, out pageNumber) || (Url.StartsWith("#") && int.TryParse(Url.Substring(1), out pageNumber)))
            {
                pageNumber--;
                if (pageNumber < 0 || pageNumber >= Document.Pages.Count)
                    throw new InvalidOperationException("Navigation URL is out of range: " + Url);

                ReferencedPage = Document.Pages[pageNumber];
            }
            else
                throw new InvalidOperationException("Navigation URL in unknown format: " + Url);
        }

        /// <summary>
        /// Loads the bookmark data
        /// </summary>
        /// <param name="reader"></param>
        internal void DecodeBookmarkData(IDjvuReader reader)
        {
            int childrenCount = reader.ReadByte();

            int textSize = reader.ReadInt24BigEndian();
            Name = reader.ReadUTF8String(textSize);

            int urlSize = reader.ReadInt24BigEndian();
            Url = reader.ReadUTF8String(urlSize);

            // Read in all the children bookmarks
            List<Bookmark> children = new List<Bookmark>();
            for (int x = 0; x < childrenCount; x++)
            {
                children.Add(new Bookmark(reader, Document, NavmNode, this));
            }
            Children = children.ToArray();
        }

        #endregion Methods
    }
}