// -----------------------------------------------------------------------
// <copyright file="PageTextItem.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks.Text;

namespace DjvuNet.Text
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class PageTextItem : PageText
    {
        #region Public Properties

        #region Parent

        private PageTextSearch _parent;

        /// <summary>
        /// Gets the parent for the text item
        /// </summary>
        public PageTextSearch Parent
        {
            get { return _parent; }

            internal set
            {
                if (Parent != value)
                    _parent = value;
            }
        }

        #endregion Parent

        #region Zone

        private TextZone _zone;

        /// <summary>
        /// Gets the text zone for the item
        /// </summary>
        public TextZone Zone
        {
            get { return _zone; }

            private set
            {
                if (Zone != value)
                    _zone = value;
            }
        }

        #endregion Zone

        #endregion Public Properties

        #region Constructors

        public PageTextItem(DjvuPage page, PageTextSearch parent, TextZone zone)
            : base(page)
        {
            _zone = zone;
            _parent = parent;
        }

        #endregion Constructors
    }
}