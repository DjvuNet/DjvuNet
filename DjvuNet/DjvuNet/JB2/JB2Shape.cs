using System;
using DjvuNet.Graphics;

namespace DjvuNet.JB2
{
    public class JB2Shape : JB2Item
    {
        #region Private Variables

        #endregion Private Variables

        #region Public Properties

        #region Parent

        private int _parent;

        /// <summary>
        /// Gets or sets the parent for the shape
        /// </summary>
        public int Parent
        {
            get
            {
                return _parent;
            }

            set
            {
                if (Parent != value)
                {
                    _parent = value;
                }
            }
        }

        #endregion Parent

        #region UserData

        private long _userData;

        /// <summary>
        /// Gets or sets the user data for the shape
        /// </summary>
        public long UserData
        {
            get { return _userData; }

            set
            {
                if (UserData != value)
                {
                    _userData = value;
                }
            }
        }

        #endregion UserData

        #region Bitmap

        private Bitmap _bitmap;

        /// <summary>
        /// Gets or sets the bitmap for the shape
        /// </summary>
        public Bitmap Bitmap
        {
            get { return _bitmap; }

            private set
            {
                if (Bitmap != value)
                {
                    _bitmap = value;
                }
            }
        }

        #endregion Bitmap

        #endregion Public Properties

        #region Constructors

        public JB2Shape(int parent)
        {
            Init(parent);
        }

        /// <summary>
        /// Creates a new Shape object.
        /// </summary>
        public JB2Shape()
        {
            Parent = 0;
            _bitmap = null;
        }

        #endregion Constructors

        #region Public Methods

        public virtual JB2Shape Duplicate()
        {
            JB2Shape retval = new JB2Shape { _bitmap = _bitmap, Bitmap = Bitmap, Parent = Parent, UserData = UserData };

            if (retval._bitmap != null)
            {
                retval._bitmap = retval._bitmap.Duplicate();
            }

            return retval;
        }

        public virtual JB2Shape Init(int parent)
        {
            this.Parent = parent;
            _bitmap = new Bitmap();

            return this;
        }

        #endregion Public Methods
    }
}