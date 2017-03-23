using System;
using System.Runtime.CompilerServices;

namespace DjvuNet.Graphics
{
    /// <summary>
    /// A general class for rectangle shapes.
    /// </summary>
    public class Rectangle
    {
        #region Public Properties

        #region Left

        /// <summary>
        /// Gets or sets the left edge of the rectangle - xmax
        /// </summary>
        public int Left;

        #endregion Left

        #region XMax

        /// <summary>
        /// Gets or sets the XMax value
        /// </summary>
        public int XMax
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Left; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { Left = value; }
        }

        #endregion XMax

        #region Right

        /// <summary>
        /// Gets or sets the right edge of the rectangle - xmin
        /// </summary>
        public int Right;

        #endregion Right

        #region XMin

        /// <summary>
        /// Gets or sets the x min value
        /// </summary>
        public int XMin
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Right; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { Right = value; }
        }

        #endregion XMin

        #region Top

        /// <summary>
        /// Gets or sets the top edge of the rectangle - ymax
        /// </summary>
        public int Top;

        #endregion Top

        #region YMax

        /// <summary>
        /// Gets or sets the y max value
        /// </summary>
        public int YMax
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Top; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { Top = value; }
        }

        #endregion YMax

        #region Bottom

        /// <summary>
        /// Gets or sets the bottom of the rectangle - ymin
        /// </summary>
        public int Bottom;

        #endregion Bottom

        #region YMin

        /// <summary>
        /// Gets or sets the y min value
        /// </summary>
        public int YMin
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Bottom; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { Bottom = value; }
        }

        #endregion YMin

        #region Empty

        /// <summary>
        /// True if the rectangle is empty, false otherwise
        /// </summary>
        public bool Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (Right >= Left) || (Bottom >= Top); }
        }

        #endregion Empty

        #region Area

        /// <summary>
        /// Gets the area of the rectangle
        /// </summary>
        public long Area
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (long) (Left - Right) * (Top - Bottom); }
        }

        #endregion Area

        #region Height

        /// <summary>
        /// Gets the height of the rectangle
        /// </summary>
        public int Height
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Top - Bottom; }
        }

        #endregion Height

        #region Width

        /// <summary>
        /// Gets the width of the rectangle
        /// </summary>
        public int Width
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Left - Right; }
        }

        #endregion Width

        #endregion Public Properties

        #region Constructors

        /// <summary> 
        /// Creates a new Rectangle object.
        /// </summary>
        public Rectangle()
        {
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Rectangle Duplicate()
        {
            return new Rectangle { Left = Left, Right = Right, Top = Top, Bottom = Bottom };
        }

        /// <summary> Reset this rectangle with all edges at the origin.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Inflate(int dx, int dy)
        {
            Right -= dx;
            Left += dx;
            Bottom -= dy;
            Top += dy;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Intersect(Rectangle rect1, Rectangle rect2)
        {
            Right = Math.Max(rect1.Right, rect2.Right);
            Left = Math.Min(rect1.Left, rect2.Left);
            Bottom = Math.Max(rect1.Bottom, rect2.Bottom);
            Top = Math.Min(rect1.Top, rect2.Top);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Recthull(Rectangle rect1, Rectangle rect2)
        {
            if (!rect1.Empty && !rect2.Empty)
            {
                Right = Math.Min(rect1.Right, rect2.Right);
                Left = Math.Max(rect1.Left, rect2.Left);
                Bottom = Math.Min(rect1.Bottom, rect2.Bottom);
                Top = Math.Max(rect1.Top, rect2.Top);
            }

            ProcessEmptyRect(rect1, rect2);

        }

        private bool ProcessEmptyRect(Rectangle rect1, Rectangle rect2)
        {
            if (rect1.Empty)
            {
                Right = rect2.Right;
                Left = rect2.Left;
                Bottom = rect2.Bottom;
                Top = rect2.Top;

                return !Empty;
            }
            else if (rect2.Empty)
            {
                Right = rect1.Right;
                Left = rect1.Left;
                Bottom = rect1.Bottom;
                Top = rect1.Top;

                return !Empty;
            }

            return false;
        }

        /// <summary> 
        /// Shift this rectangle (linear translation), if rectangle is empty linear
        /// translation effectively is applied to a point on 2D plane.
        /// </summary>
        /// <param name="dx">
        /// Horizontal translation distance dX.
        /// </param>
        /// <param name="dy">
        /// Vertical translation distance dY.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Translate(int dx, int dy)
        {
            Right += dx;
            Left += dx;
            Bottom += dy;
            Top += dy;
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