using System;

namespace DjvuNet.JB2
{
    /// <summary>
    /// This class represent a JB2 encoded shape.
    /// </summary>
    public class JB2Blit
    {
        #region Protected Variables

        #endregion Protected Variables

        #region Public Properties

        #region Bottom

        private int _bottom;

        /// <summary>
        /// Gets or sets the bottom of the blit
        /// </summary>
        public int Bottom
        {
            get
            {
                return 0xffff & _bottom;
            }

            set
            {
                if (Bottom != value)
                {
                    _bottom = (short)value;
                }
            }
        }

        #endregion Bottom

        #region Left

        private int _left;

        /// <summary>
        /// Gets or sets the left of the blit
        /// </summary>
        public int Left
        {
            get { return 0xffff & _left; }

            set
            {
                if (Left != value)
                {
                    _left = (short)value;
                }
            }
        }

        #endregion Left

        #region ShapeNumber

        private int _shapeNumber;

        /// <summary>
        /// Gets or sets the shape number of the blit
        /// </summary>
        public int ShapeNumber
        {
            get { return _shapeNumber; }

            set
            {
                if (ShapeNumber != value)
                {
                    _shapeNumber = value;
                }
            }
        }

        #endregion ShapeNumber

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