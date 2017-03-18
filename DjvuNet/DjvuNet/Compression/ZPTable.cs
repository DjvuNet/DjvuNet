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

        private int _pValue;

        /// <summary>
        /// Gets the P-Value for the item
        /// </summary>
        public int PValue
        {
            get { return _pValue; }

            private set
            {
                if (PValue != value)
                {
                    _pValue = value;
                }
            }
        }

        #endregion PValue

        #region MValue

        private int _mValue;

        /// <summary>
        /// Gets the M-Value for the item
        /// </summary>
        public int MValue
        {
            get { return _mValue; }

            private set
            {
                if (MValue != value)
                {
                    _mValue = value;
                }
            }
        }

        #endregion MValue

        #region Down

        private short _down;

        /// <summary>
        /// Gets the down value for the item
        /// </summary>
        public short Down
        {
            get { return _down; }

            private set
            {
                if (Down != value)
                {
                    _down = value;
                }
            }
        }

        #endregion Down

        #region Up

        private short _up;

        /// <summary>
        /// Gets the up value for the item
        /// </summary>
        public short Up
        {
            get { return _up; }

            private set
            {
                if (Up != value)
                {
                    _up = value;
                }
            }
        }

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
