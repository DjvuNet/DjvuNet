using System;
using System.Runtime.CompilerServices;

namespace DjvuNet.Graphics
{
    /// <summary>
    /// A general class for rectangle shapes.
    /// </summary>
    public class Rectangle : IEquatable<Rectangle>
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the left edge of the rectangle - xmax
        /// </summary>
        private int _XMax;

        /// <summary>
        /// Gets or sets the XMax value
        /// </summary>
        public int XMax
        {
            get { return _XMax; }
            set { _XMax = value; }
        }

        /// <summary>
        /// Gets or sets the right edge of the rectangle - xmin
        /// </summary>
        private int _XMin;

        /// <summary>
        /// Gets or sets the x min value
        /// </summary>
        public int XMin
        {
            get { return _XMin; }
            set { _XMin = value; }
        }

        /// <summary>
        /// Gets or sets the top edge of the rectangle - ymax
        /// </summary>
        private int _YMax;

        /// <summary>
        /// Gets or sets the y max value
        /// </summary>
        public int YMax
        {
            get { return _YMax; }
            set { _YMax = value; }
        }

        /// <summary>
        /// Gets or sets the bottom of the rectangle - ymin
        /// </summary>
        private int _YMin;

        /// <summary>
        /// Gets or sets the y min value
        /// </summary>
        public int YMin
        {
            get { return _YMin; }
            set { _YMin = value; }
        }

        /// <summary>
        /// True if the rectangle is empty, false otherwise
        /// </summary>
        public bool Empty
        {
            get { return (XMin == XMax) || (YMin == YMax); }
        }

        /// <summary>
        /// Gets the area of the rectangle
        /// </summary>
        public long Area
        {
            get { return (long) Math.Abs( (XMax - XMin) * (YMax - YMin)); }
        }

        /// <summary>
        /// Gets the height of the rectangle
        /// </summary>
        public int Height
        {
            get { return YMax - YMin; }
        }

        /// <summary>
        /// Gets the width of the rectangle
        /// </summary>
        public int Width
        {
            get { return XMax - XMin; }
        }

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
        public Rectangle(int right, int bottom, int width, int height)
        {
            XMin = right;
            YMin = bottom;
            XMax = right + width;
            YMax = bottom + height;
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
            return new Rectangle { XMax = XMax, XMin = XMin, YMax = YMax, YMin = YMin };
        }

        /// <summary> Reset this rectangle with all edges at the origin.</summary>
        public virtual void Clear()
        {
            XMin = XMax = YMin = YMax = 0;
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
            return (x >= XMin) && (x <= XMax) && (y >= YMin) && (y <= YMax);
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
            {
                return false;
            }

            // Test point coordinates on 2D plane
            if (this.Empty && rect.Empty)
            {
                return (this.XMin == rect.XMin) && (this.YMin == rect.YMin);
            }

            return (Contains(rect.XMin, rect.YMin) && Contains(rect.XMax - 1, rect.YMax - 1));
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
                       ((XMin == r.XMin) && (XMax == r.XMax) && (YMin == r.YMin) && (YMax == r.YMax));
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (XMin.GetHashCode() - Width.GetHashCode() + YMin.GetHashCode()
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
            XMin -= dx;
            XMax += dx;
            YMin -= dy;
            YMax += dy;
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
        public void Intersect(Rectangle rect1, Rectangle rect2)
        {
            XMin = Math.Max(rect1.XMin, rect2.XMin);
            XMax = Math.Min(rect1.XMax, rect2.XMax);
            YMin = Math.Max(rect1.YMin, rect2.YMin);
            YMax = Math.Min(rect1.YMax, rect2.YMax);
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
        public void Recthull(Rectangle rect1, Rectangle rect2)
        {
            if (!rect1.Empty && !rect2.Empty)
            {
                XMin = Math.Min(rect1.XMin, rect2.XMin);
                XMax = Math.Max(rect1.XMax, rect2.XMax);
                YMin = Math.Min(rect1.YMin, rect2.YMin);
                YMax = Math.Max(rect1.YMax, rect2.YMax);
            }

            ProcessEmptyRect(rect1, rect2);

        }

        internal bool ProcessEmptyRect(Rectangle rect1, Rectangle rect2)
        {
            if (rect1.Empty)
            {
                XMin = rect2.XMin;
                XMax = rect2.XMax;
                YMin = rect2.YMin;
                YMax = rect2.YMax;

                return !Empty;
            }
            else if (rect2.Empty)
            {
                XMin = rect1.XMin;
                XMax = rect1.XMax;
                YMin = rect1.YMin;
                YMax = rect1.YMax;

                return !Empty;
            }

            return false;
        }

        /// <summary>
        /// Shift this rectangle (linear translation), if rectangle is empty linear
        /// translation is applied to a point (x,y) on 2D plane.
        /// </summary>
        /// <param name="dx">
        /// Horizontal translation distance dX.
        /// </param>
        /// <param name="dy">
        /// Vertical translation distance dY.
        /// </param>
        public void Translate(int dx, int dy)
        {
            XMin += dx;
            XMax += dx;
            YMin += dy;
            YMax += dy;
        }

        public bool Equals(Rectangle other)
        {
            return this == other;
        }

        public static bool operator ==(Rectangle first, Rectangle second)
        {
            if (null != (object)first && null != (object)second)
            {
                return first.XMax == second.XMax && first.YMax == second.YMax && first.Width == second.Width && first.Height == second.Height;
            }
            else
            {
                return null == (object)first && null == (object)second;
            }
        }

        public static bool operator !=(Rectangle first, Rectangle second)
        {
            if (null != (object)first && null != (object)second)
            {
                return first.XMax != second.XMax || first.YMax != second.YMax || first.Width != second.Width || first.Height != second.Height;
            }
            else
            {
                return !(null == (object)first && null == (object)second);
            }
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
            return new System.Drawing.Rectangle(rect.XMax, rect.YMax, rect.Width, rect.Height);
        }

        #endregion Public Methods
    }
}
