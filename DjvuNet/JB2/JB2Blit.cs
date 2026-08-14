using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DjvuNet.JB2
{
    /// <summary>
    /// This class represent a JB2 encoded shape.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct JB2Blit
    {
        private int _bottom;

        private int _left;

        private int _shapeNumber;
                      
        #region Public Properties

        /// <summary>
        /// Gets or sets the bottom of the blit
        /// </summary>
        public int Bottom
        {
            get { return 0xffff & _bottom; }
            set { _bottom = (short)value; }
        }

        /// <summary>
        /// Gets or sets the left of the blit
        /// </summary>
        public int Left
        {
            get { return 0xffff & _left; }
            set { _left = (short)value; }
        }

        /// <summary>
        /// Gets or sets the shape number of the blit
        /// </summary>
        public int ShapeNumber {
            get => _shapeNumber;
            set { _shapeNumber = value; }
        }

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

        public JB2Blit Duplicate()
        {
            // struct semantics: we return copy
            return this;
        }

        #endregion Public Methods
    }
}
