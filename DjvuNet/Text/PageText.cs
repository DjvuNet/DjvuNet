// -----------------------------------------------------------------------
// <copyright file="PageText.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public DjvuPage Page { get; internal set; }

        #endregion Page

        #endregion Public Properties

        #region Constructors

        public PageText(DjvuPage page)
        {
            Page = page;
        }

        #endregion Constructors
    }
}