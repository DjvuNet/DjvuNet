// -----------------------------------------------------------------------
// <copyright file="INavigation.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace DjvuNet.DataChunks.Navigation.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public interface INavigation
    {
        /// <summary>
        /// Gets the list of document bookmarks
        /// </summary>
        Bookmark[] Bookmarks { get; }
    }
}