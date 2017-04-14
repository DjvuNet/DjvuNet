// -----------------------------------------------------------------------
// <copyright file="PageTextItem.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks;

namespace DjvuNet.Text
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class PageTextItem : PageText
    {
        #region Public Properties

        #region Parent

        /// <summary>
        /// Gets the parent for the text item
        /// </summary>
        public PageTextSearch Parent { get; internal set; }

        #endregion Parent

        #region Zone

        /// <summary>
        /// Gets the text zone for the item
        /// </summary>
        public TextZone Zone { get; internal set; }

        #endregion Zone

        #endregion Public Properties

        #region Constructors

        public PageTextItem(IDjvuPage page, PageTextSearch parent, TextZone zone)
            : base(page)
        {
            Zone = zone;
            Parent = parent;
        }

        #endregion Constructors
    }
}