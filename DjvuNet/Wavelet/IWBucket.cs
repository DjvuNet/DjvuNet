// -----------------------------------------------------------------------
// <copyright file="IWBucket.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace DjvuNet.Wavelet
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class IWBucket
    {
        #region Public Fields

        #region Sz

        /// <summary>
        /// Gets or sets the Sz value
        /// </summary>
        public static int Sz = 8;

        #endregion Sz

        #region Size

        /// <summary>
        /// Gets or sets the bucket size
        /// </summary>
        public int Size;

        #endregion Size

        #region Start

        /// <summary>
        /// Gets or sets the bucket start
        /// </summary>
        public int Start;

        #endregion Start

        #endregion Public Fields

        #region Constructors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IWBucket(int start, int size)
        {
            Start = start;
            Size = size;
        }

        #endregion Constructors
    }
}