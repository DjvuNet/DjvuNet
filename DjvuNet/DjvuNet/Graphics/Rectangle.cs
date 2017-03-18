using System;

namespace DjvuNet.Graphics
{
    /// <summary>
    /// A general class for rectange shapes.
    /// </summary>
    public class Rectangle
    {
        #region Public Properties

        #region Left

        private int _left;

        /// <summary>
        /// Gets or sets the left edge of the rectangle - xmax
        /// </summary>
        public int Left
        {
            get
            {
                return _left;
            }

            set
            {
                if (Left != value)
                {
                    _left = value;
                }
            }
        }

        #endregion Left

        #region XMax

        /// <summary>
        /// Gets or sets the XMax value
        /// </summary>
        public int XMax
        {
            get { return Left; }

            set
            {
                if (XMax != value)
                {
                    Left = value;
                    //if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("XMax"));
                }
            }
        }

        #endregion XMax

        #region Right

        private int _right;

        /// <summary>
        /// Gets or sets the right edge of the rectangle - xmin
        /// </summary>
        public int Right
        {
            get { return _right; }

            set
            {
                if (Right != value)
                {
                    _right = value;
                }
            }
        }

        #endregion Right

        #region XMin

        /// <summary>
        /// Gets or sets the x min value
        /// </summary>
        public int XMin
        {
            get { return Right; }

            set
            {
                if (XMin != value)
                {
                    Right = value;
                    //if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("XMin"));
                }
            }
        }

        #endregion XMin

        #region Top

        private int _top;

        /// <summary>
        /// Gets or sets the top edge of the rectangle - ymax
        /// </summary>
        public int Top
        {
            get { return _top; }

            set
            {
                if (Top != value)
                {
                    _top = value;
                }
            }
        }

        #endregion Top

        #region YMax

        /// <summary>
        /// Gets or sets the y max value
        /// </summary>
        public int YMax
        {
            get { return Top; }

            set
            {
                if (YMax != value)
                {
                    Top = value;
                    //if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("YMax"));
                }
            }
        }

        #endregion YMax

        #region Bottom

        private int _bottom;

        /// <summary>
        /// Gets or sets the bottom of the rectangle - ymin
        /// </summary>
        public int Bottom
        {
            get { return _bottom; }

            set
            {
                if (Bottom != value)
                {
                    _bottom = value;
                }
            }
        }

        #endregion Bottom

        #region YMin

        /// <summary>
        /// Gets or sets the y min value
        /// </summary>
        public int YMin
        {
            get { return Bottom; }

            set
            {
                if (YMin != value)
                {
                    Bottom = value;
                    //if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("YMin"));
                }
            }
        }

        #endregion YMin

        #region Empty

        /// <summary>
        /// True if the rectangle is empty, false otherwise
        /// </summary>
        public bool Empty
        {
            get { return (Right >= Left) || (Bottom >= Top); }
        }

        #endregion Empty

        #region Area

        /// <summary>
        /// Gets the area of the rectangle
        /// </summary>
        public long Area
        {
            get { return (long) (Left - Right) * (Top - Bottom); }
        }

        #endregion Area

        #region Height

        /// <summary>
        /// Gets the height of the rectangle
        /// </summary>
        public int Height
        {
            get { return Top - Bottom; }
        }

        #endregion Height

        #region Width

        /// <summary>
        /// Gets the width of the rectangle
        /// </summary>
        public int Width
        {
            get { return Left - Right; }
        }

        #endregion Width

        #endregion Public Properties

        #region Constructors

        /// <summary> Creates a new Rectangle object.</summary>
        public Rectangle()
        {
            Right = Left = Bottom = Top = 0;
        }

        /// <summary> 
        /// Creates a new Rectangle object initialized with provided values.
        /// </summary>
        /// <param name="right">
        /// left edge
        /// </param>
        /// <param name="bottom">
        /// bottom edge
        /// </param>
        /// <param name="width">
        /// horizontal length
        /// </param>
        /// <param name="height">
        /// vertical length
        /// </param>
        public Rectangle(int right, int bottom, int width, int height)
        {
            Right = right;
            Bottom = bottom;
            Left = right + width;
            Top = bottom + height;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary> 
        /// Create a clone of this rectangle.
        /// </summary>
        /// <returns> 
        /// the newly created copy
        /// </returns>
        public Rectangle Duplicate()
        {
            return new Rectangle { Left = Left, Right = Right, Top = Top, Bottom = Bottom };
        }

        /// <summary> Reset this rectangle with all edges at the origin.</summary>
        public virtual void Clear()
        {
            Right = Left = Bottom = Top = 0;
        }

        /// <summary> 
        /// Test if a point is contained in this rectangle. The definition of point 
        /// contained by rectangle on 2D plane is
        /// that point lies inside or on the edge of the rectangle.
        /// </summary>
        /// <param name="x">
        /// horizontal coordinate
        /// </param>
        /// <param name="y">
        /// vertical coordinate
        /// </param>
        /// <returns> 
        /// true if the point is within this rectangle
        /// </returns>
        public virtual bool Contains(int x, int y)
        {
            return (x >= Right) && (x <= Left) && (y >= Bottom) && (y <= Top);
        }

        /// <summary> 
        /// Test if a rectangle is contained within this rectangle.
        /// Empty rectangle is treated as a point on 2D plane with coordinates which may 
        /// "contain" other points with same coordinates. In other words on 2D plane
        /// rectangle with width and height equal to zero is reduced to a point.
        /// </summary>
        /// <param name="rect">
        /// rectangle to test
        /// </param>
        /// <returns> 
        /// true if the rectangle is contained within this rectangle
        /// </returns>
        public virtual bool Contains(Rectangle rect)
        {
            // First check for special cases

            if (this.Empty && !rect.Empty)
                return false;

            // Test point coordinates on 2D plane
            if (this.Empty && rect.Empty)
                return (this.Right == rect.Right) && (this.Bottom == rect.Bottom);

            return (Contains(rect.Right, rect.Bottom) && Contains(rect.Left - 1, rect.Top - 1));
        }

        /// <summary> 
        /// Test if two rectangles are equal.
        /// </summary>
        /// <param name="obj">
        /// reference rectangle to compare with
        /// </param>
        /// <returns> 
        /// true if all the edges are equal
        /// </returns>
        public override bool Equals(object obj)
        {
            Rectangle r = obj as Rectangle;
            if (null != r)
            {
                bool isempty1 = Empty;
                bool isempty2 = r.Empty;

                return ((isempty1 || isempty2) && isempty1 && isempty2) ||
                       ((Right == r.Right) && (Left == r.Left) && (Bottom == r.Bottom) && (Top == r.Top));
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (Right.GetHashCode() - Width.GetHashCode() + Bottom.GetHashCode() 
                - Height.GetHashCode()).GetHashCode();
        }

        /// <summary> 
        /// Grow the size of this rectangle by moving all the edges outwards.
        /// </summary>
        /// <param name="dx">
        /// Amount to grow the horizontal edges
        /// </param>
        /// <param name="dy">
        /// Amount to grow the vertical edges
        /// </param>
        /// <returns> 
        /// true if not empty.
        /// </returns>
        public virtual bool Inflate(int dx, int dy)
        {
            Right -= dx;
            Left += dx;
            Bottom -= dy;
            Top += dy;

            if (!Empty)
            {
                return true;
            }
            else
            {
                Right = Bottom = Left = Top = 0;

                return false;
            }
        }

        /// <summary> 
        /// Set this rectangle as the intersection of two rectangles.
        /// </summary>
        /// <param name="rect1">
        /// rectangle to intersect
        /// </param>
        /// <param name="rect2">
        /// rectangle to intersect
        /// </param>
        /// <returns> 
        /// true if the intersection is not empty
        /// </returns>
        public virtual bool Intersect(Rectangle rect1, Rectangle rect2)
        {
            Right = Math.Max(rect1.Right, rect2.Right);
            Left = Math.Min(rect1.Left, rect2.Left);
            Bottom = Math.Max(rect1.Bottom, rect2.Bottom);
            Top = Math.Min(rect1.Top, rect2.Top);

            if (Empty)
            {
                Right = Bottom = Left = Top = 0;

                return false;
            }

            return true;
        }

        /// <summary>
        /// Set this rectangle as the union of two rectangles.
        /// </summary>
        /// <param name="rect1">
        /// rectangle to union
        /// </param>
        /// <param name="rect2">
        /// rectangle to union
        /// </param>
        /// <returns> 
        /// true if the results are non-empty
        /// </returns>
        public virtual bool Recthull(Rectangle rect1, Rectangle rect2)
        {
            if (rect1.Empty)
            {
                Right = rect2.Right;
                Left = rect2.Left;
                Bottom = rect2.Bottom;
                Top = rect2.Top;

                return !Empty;
            }

            if (rect2.Empty)
            {
                Right = rect1.Right;
                Left = rect1.Left;
                Bottom = rect1.Bottom;
                Top = rect1.Top;

                return !Empty;
            }
            Right = Math.Min(rect1.Right, rect2.Right);
            Left = Math.Max(rect1.Left, rect2.Left);
            Bottom = Math.Min(rect1.Bottom, rect2.Bottom);
            Top = Math.Max(rect1.Top, rect2.Top);
            return true;
        }

        /// <summary> 
        /// Shift this rectangle (linear translation)
        /// </summary>
        /// <param name="dx">
        /// horizontal distance to shift
        /// </param>
        /// <param name="dy">
        /// vertical distance to shift
        /// </param>
        /// <returns> 
        /// true if not empty
        /// </returns>
        public virtual bool Translate(int dx, int dy)
        {
            if (!Empty)
            {
                Right += dx;
                Left += dx;
                Bottom += dy;
                Top += dy;
                return true;
            }
            Right = Bottom = Left = Top = 0;
            return false;
        }

        /// <summary>
        /// Implicit conversion to <typeparamref name="System.Drawing.Rectangle">
        /// System.Drawing.Rectangle</typeparamref>
        /// </summary>
        /// <param name="rect">
        /// </param>
        /// <returns>
        /// </returns>
        public static implicit operator System.Drawing.Rectangle(Rectangle rect)
        {
            return new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
        }

        #endregion Public Methods
    }
}