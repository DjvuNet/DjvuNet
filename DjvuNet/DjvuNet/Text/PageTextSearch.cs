// -----------------------------------------------------------------------
// <copyright file="PageTextSearch.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using DjvuNet.DataChunks.Text;
using System.Linq;

namespace DjvuNet.Text
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class PageTextSearch : PageText
    {
        #region Public Properties

        #region TextItems

        private PageTextItem[] _textItems;

        /// <summary>
        /// Gets the text items for the search
        /// </summary>
        public PageTextItem[] TextItems
        {
            get { return _textItems; }

            private set
            {
                if (TextItems != value)
                {
                    _textItems = value;
                }
            }
        }

        #endregion TextItems

        #endregion Public Properties

        #region Constructors

        public PageTextSearch(DjvuPage page, TextZone[] items)
            : base(page)
        {
            _textItems = items.Select(x => new PageTextItem(page, this, x)).ToArray();
        }

        public PageTextSearch(DjvuPage page, PageTextItem[] items)
            : base(page)
        {
            _textItems = items;
        }

        #endregion Constructors
    }
}