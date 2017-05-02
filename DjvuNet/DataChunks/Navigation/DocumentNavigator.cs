// -----------------------------------------------------------------------
// <copyright file="DocumentNavigator.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DjvuNet.DataChunks
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DocumentNavigator : INavigation
    {
        #region Public Properties

        #region Bookmarks

        /// <summary>
        /// Gets the list of document bookmarks
        /// </summary>
        public IReadOnlyList<IBookmark> Bookmarks { get; internal set; }

        #endregion Bookmarks

        #endregion Public Properties

        #region Constructors

        public DocumentNavigator(IDjvuDocument document)
        {
            BuildNavigation(document);
        }

        #endregion Constructors

        #region Private Methods

        internal void BuildNavigation(IDjvuDocument document)
        {
            List<Bookmark> bookmarks = new List<Bookmark>();

            for (int x = 0; x < document.Pages.Count; x++)
            {
                IDjvuPage page = document.Pages[x];
                int pageNum = x + 1;
                bookmarks.Add(new Bookmark(document, null, null, 
                    string.Format("Page {0}", pageNum), string.Format("#{0}", pageNum), new Bookmark[0]));
            }

            Bookmarks = bookmarks;
        }

        #endregion Private Methods
    }
}