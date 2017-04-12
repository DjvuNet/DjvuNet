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

        /// <summary>
        /// Gets the text items for the search
        /// </summary>
        public PageTextItem[] TextItems { get; internal set; }

        #endregion TextItems

        #endregion Public Properties

        #region Constructors

        public PageTextSearch(IDjvuPage page, TextZone[] items)
            : base(page)
        {
            TextItems = items.Select(x => new PageTextItem(page, this, x)).ToArray();
        }

        public PageTextSearch(IDjvuPage page, PageTextItem[] items)
            : base(page)
        {
            TextItems = items;
        }

        #endregion Constructors
    }
}