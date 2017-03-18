// -----------------------------------------------------------------------
// <copyright file="IWBucket.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DjvuNet.Wavelet
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class IWBucket
    {
        #region Public Properties

        #region Sz

        private static int _sz = 8;

        /// <summary>
        /// Gets or sets the Sz value
        /// </summary>
        public static int Sz
        {
            get
            {
                return _sz;
            }

            set
            {
                if (Sz != value)
                {
                    _sz = value;
                }
            }
        }

        #endregion Sz

        #region Size

        private int _size;

        /// <summary>
        /// Gets or sets the bucket size
        /// </summary>
        public int Size
        {
            get { return _size; }

            set
            {
                if (Size != value)
                {
                    _size = value;
                }
            }
        }

        #endregion Size

        #region Start

        private int _start;

        /// <summary>
        /// Gets or sets the bucket start
        /// </summary>
        public int Start
        {
            get { return _start; }

            set
            {
                if (Start != value)
                {
                    _start = value;
                }
            }
        }

        #endregion Start

        #endregion Public Properties

        #region Constructors

        public IWBucket(int start, int size)
        {
            Start = start;
            Size = size;
        }

        #endregion Constructors
    }
}