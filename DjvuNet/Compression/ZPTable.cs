using System;

namespace DjvuNet.Compression
{
    /// <summary>
    /// TODO docs
    /// </summary>
    public class ZPTable
    {
        #region Public Properties

        #region PValue

        /// <summary>
        /// Gets the P-Value for the item
        /// </summary>
        public int PValue { get; set; }

        #endregion PValue

        #region MValue

        /// <summary>
        /// Gets the M-Value for the item
        /// </summary>
        public int MValue { get; set; }

        #endregion MValue

        #region Down

        /// <summary>
        /// Gets the down value for the item
        /// </summary>
        public short Down { get; set; }

        #endregion Down

        #region Up

        /// <summary>
        /// Gets the up value for the item
        /// </summary>
        public short Up { get; set; }

        #endregion Up

        #endregion Public Properties

        #region Constructors

        /// <summary>
        /// Initialize the ZPTable
        /// </summary>
        /// <param name="pValue"></param>
        /// <param name="mValue"></param>
        /// <param name="up"></param>
        /// <param name="dn"></param>
        public ZPTable(int pValue, int mValue, short up, short dn)
        {
            PValue = pValue;
            MValue = mValue;
            Up = up;
            Down = dn;
        }

        #endregion Constructors
    }
}
