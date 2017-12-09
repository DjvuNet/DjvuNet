using System.Runtime.InteropServices;

namespace DjvuNet.Compression
{
    /// <summary>
    /// TODO docs
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class ZPTable
    {
        #region Public Properties

        /// <summary>
        /// Gets the P-Value for the item
        /// </summary>
        public ushort PValue;

        /// <summary>
        /// Gets the M-Value for the item
        /// </summary>
        public ushort MValue;

        /// <summary>
        /// Gets the down value for the item
        /// </summary>
        public byte Down;

        /// <summary>
        /// Gets the up value for the item
        /// </summary>
        public byte Up;

        #endregion Public Properties

        #region Constructors

        /// <summary>
        /// Initialize the ZPTable
        /// </summary>
        /// <param name="pValue"></param>
        /// <param name="mValue"></param>
        /// <param name="up"></param>
        /// <param name="dn"></param>
        public ZPTable(ushort pValue, ushort mValue, byte up, byte dn)
        {
            PValue = pValue;
            MValue = mValue;
            Up = up;
            Down = dn;
        }

        #endregion Constructors
    }
}
