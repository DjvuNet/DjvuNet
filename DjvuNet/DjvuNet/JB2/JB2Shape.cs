using System;
using DjvuNet.Graphics;

namespace DjvuNet.JB2
{
    public class JB2Shape : JB2Item
    {
        #region Private Members

        #endregion Private Members

        #region Public Properties

        #region Parent

        /// <summary>
        /// Gets or sets the parent for the shape
        /// </summary>
        public int Parent;

        #endregion Parent

        #region UserData

        /// <summary>
        /// Gets or sets the user data for the shape
        /// </summary>
        public long UserData;

        #endregion UserData

        #region Bitmap

        /// <summary>
        /// Gets or sets the bitmap for the shape
        /// </summary>
        public Bitmap Bitmap;

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
        }

        #endregion Constructors

        #region Public Methods

        public virtual JB2Shape Duplicate()
        {
            JB2Shape retval = new JB2Shape
            {
                Bitmap = Bitmap?.Duplicate(),
                Parent = Parent,
                UserData = UserData
            };

            // TODO test bitmap copy logic

            //if (retval.Bitmap != null)
            //    retval.Bitmap = retval.Bitmap.Duplicate();

            return retval;
        }

        public virtual JB2Shape Init(int parent)
        {
            this.Parent = parent;
            Bitmap = new Bitmap();
            return this;
        }

        #endregion Public Methods
    }
}