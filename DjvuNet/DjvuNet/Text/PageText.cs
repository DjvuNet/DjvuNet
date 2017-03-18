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

        private DjvuPage _page;

        /// <summary>
        /// Gets the page for the text items
        /// </summary>
        public DjvuPage Page
        {
            get
            {
                return _page;
            }

            private set
            {
                if (Page != value)
                {
                    _page = value;
                }
            }
        }

        #endregion Page

        #endregion Public Properties

        #region Constructors

        public PageText(DjvuPage page)
        {
            _page = page;
        }

        #endregion Constructors
    }
}