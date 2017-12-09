using System.Runtime.CompilerServices;

namespace DjvuNet.JB2
{
    /// <summary>
    /// This class represent a JB2 encoded shape.
    /// </summary>
    public class JB2Blit
    {
        #region Public Properties

        private int _bottom;

        /// <summary>
        /// Gets or sets the bottom of the blit
        /// </summary>
        public int Bottom
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return 0xffff & _bottom; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _bottom = (short)value; }
        }

        private int _left;

        /// <summary>
        /// Gets or sets the left of the blit
        /// </summary>
        public int Left
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return 0xffff & _left; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _left = (short)value; }
        }

        /// <summary>
        /// Gets or sets the shape number of the blit
        /// </summary>
        public int ShapeNumber;

        #endregion Public Properties

        #region Constructors

        /// <summary>
        /// Creates a new Blit object.
        /// </summary>
        public JB2Blit()
        {
        }

        #endregion Constructors

        #region Public Methods

        public virtual JB2Blit Duplicate()
        {
            return (JB2Blit)base.MemberwiseClone();
        }

        #endregion Public Methods
    }
}
