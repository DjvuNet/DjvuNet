// -----------------------------------------------------------------------
// <copyright file="IWBucket.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace DjvuNet.Wavelet
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class InterWaveBucket
    {
        #region Public Fields

        /// <summary>
        /// Gets or sets the Sz value
        /// </summary>
        public static int Sz = 8;

        /// <summary>
        /// Gets or sets the bucket size
        /// </summary>
        public int Size;

        /// <summary>
        /// Gets or sets the bucket start
        /// </summary>
        public int Start;

        #endregion Public Fields

        #region Constructors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InterWaveBucket(int start, int size)
        {
            Start = start;
            Size = size;
        }

        #endregion Constructors
    }
}
