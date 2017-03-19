// -----------------------------------------------------------------------
// <copyright file="DocumentNavigator.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks.Navigation.Interfaces;

namespace DjvuNet.DataChunks.Navigation
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DocumentNavigator : INavigation
    {
        #region Public Properties

        #region Bookmarks

        private Bookmark[] _bookmarks;

        /// <summary>
        /// Gets the list of document bookmarks
        /// </summary>
        public Bookmark[] Bookmarks
        {
            get { return _bookmarks; }

            private set
            {
                if (_bookmarks != value)
                    _bookmarks = value;
            }
        }

        #endregion Bookmarks

        #endregion Public Properties

        #region Constructors

        public DocumentNavigator(DjvuDocument document)
        {
            BuildNavigation(document);
        }

        #endregion Constructors

        #region Private Methods

        private void BuildNavigation(DjvuDocument document)
        {
            List<Bookmark> bookmarks = new List<Bookmark>();

            for (int x = 0; x < document.Pages.Length; x++)
            {
                DjvuPage page = document.Pages[x];
                int pageNum = x + 1;
                bookmarks.Add(new Bookmark(document, null, 
                    string.Format("Page {0}", pageNum), string.Format("#{0}", pageNum), new Bookmark[0]));
            }

            _bookmarks = bookmarks.ToArray();
        }

        #endregion Private Methods
    }
}