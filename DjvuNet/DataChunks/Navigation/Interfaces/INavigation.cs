// -----------------------------------------------------------------------
// <copyright file="INavigation.cs" company="">
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
    public interface INavigation
    {
        /// <summary>
        /// Gets the list of document bookmarks
        /// </summary>
        IReadOnlyList<IBookmark> Bookmarks { get; }
    }
}