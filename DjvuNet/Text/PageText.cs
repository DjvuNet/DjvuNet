// -----------------------------------------------------------------------
// <copyright file="PageText.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace DjvuNet.Text
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public abstract class PageText
    {
        #region Public Properties

        #region Page

        /// <summary>
        /// Gets the page for the text items
        /// </summary>
        public IDjvuPage Page { get; internal set; }

        #endregion Page

        #endregion Public Properties

        #region Constructors

        public PageText(IDjvuPage page)
        {
            Page = page;
        }

        #endregion Constructors
    }
}
