using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DjvuNet.Errors;

namespace DjvuNet.Graphics
{

    /// <summary>
    /// A general struct for rectangle shapes, mapping to DjVuLibre's GRect.
    /// Cartesian coordinate system origin (0,0) is bottom-left.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Rectangle : IEquatable<Rectangle>
    {
        #region Private Backing Fields
        // Fields are ordered explicitly to match C++ GRect layout: xmin, ymin, xmax, ymax.

        private int _XMin;
        private int _YMin;
        private int _XMax;
        private int _YMax;

        #endregion Private Backing Fields

        #region Public Properties

        /// <summary>
        /// Gets the minimum X coordinate (xmin)
        /// </summary>
        public int XMin
        {
            get { return _XMin; }
            internal set { _XMin = value; }
        }

        /// <summary>
        /// Gets the minimum Y coordinate (ymin)
        /// </summary>
        public int YMin
        {
            get { return _YMin; }
            internal set { _YMin = value; }
        }

        /// <summary>
        /// Gets the maximum X coordinate (xmax)
        /// </summary>
        public int XMax
        {
            get { return _XMax; }
            internal set { _XMax = value; }
        }

        /// <summary>
        /// Gets the maximum Y coordinate (ymax)
        /// </summary>
        public int YMax
        {
            get { return _YMax; }
            internal set { _YMax = value; }
        }

        /// <summary>
        /// True if the rectangle is empty, false otherwise.
        /// Matches DjVuLibre: return (xmin >= xmax || ymin >= ymax);
        /// Additionally clamps to true if structural bounds mathematically exceed 32-bit limits.
        /// </summary>
        public readonly bool Empty
        {
            get 
            { 
                return (_XMin >= _XMax) || 
                       (_YMin >= _YMax) || 
                       ((long)_XMax - _XMin > int.MaxValue) || 
                       ((long)_YMax - _YMin > int.MaxValue); 
            }
        }

        /// <summary>
        /// Gets the area of the rectangle.
        /// Matches DjVuLibre: isempty() ? 0 : (xmax-xmin)*(ymax-ymin);
        /// </summary>
        public readonly long Area
        {
            get { return Empty ? 0 : ((long)_XMax - _XMin) * ((long)_YMax - _YMin); }
        }

        /// <summary>
        /// Gets the height of the rectangle
        /// </summary>
        public readonly int Height
        {
            get
            {
                if (Empty)
                    return 0;
                long height = (long)_YMax - _YMin;
                if (height < 0 || height > int.MaxValue)
                    return 0;
                return (int)height;
            }
        }

        /// <summary>
        /// Gets the width of the rectangle
        /// </summary>
        public readonly int Width
        {
            get
            {
                if (Empty)
                    return 0;
                long width = (long)_XMax - _XMin;
                if (width < 0 || width > int.MaxValue)
                    return 0;
                return (int)width;
            }
        }

        #endregion Public Properties

        #region Constructors

        /// <summary>
        /// Creates a new Rectangle initialized with provided values.
        /// </summary>
        /// <param name="xMin">Minimum X coordinate (xmin)</param>
        /// <param name="yMin">Minimum Y coordinate (ymin)</param>
        /// <param name="width">Horizontal length</param>
        /// <param name="height">Vertical length</param>
        /// <remarks>
        /// <para>
        /// <b>Architectural Divergence from C++ DjVuLibre:</b>
        /// The original C++ implementation of GRect strictly truncates dimensions using 16-bit
        /// (unsigned short) casts, limiting image sizes to 65,535. DjvuNet deliberately diverges
        /// from this limitation and supports dimensions up to <see cref="Int32.MaxValue"/>.
        /// </para>
        /// <para>
        /// Furthermore, the C++ implementation utilizes 32-bit unchecked integer wrap-around
        /// which allows a massive width (e.g. 4,294,967,294) to evaluate as a negative magnitude,
        /// inadvertently creating a structurally "valid" but corrupted bounding box. To avoid
        /// unsafe behaviors in managed code, this constructor explicitly avoids underflows and
        /// overflows. If an invalid geometrical condition is created by exceeding <see cref="Int32.MaxValue"/>,
        /// it is clamped to an Empty rectangle.
        /// </para>
        /// </remarks>
        public Rectangle(int xMin, int yMin, int width, int height)
        {
            _XMin = xMin;
            _YMin = yMin;

            // 1. Restrict invalid parameters.
            // Any provided dimension less than zero or exceeding Int32 limits physically cannot be
            // represented by the struct's own properties and must be clamped to Empty.
            if (width < 0 || height < 0 || width > int.MaxValue || height > int.MaxValue)
            {
                _XMax = xMin;
                _YMax = yMin;
                return;
            }

            // 2. Use 64-bit arithmetic to safely detect geometric Cartesian overflow
            long xMaxCalc = (long)xMin + width;
            long yMaxCalc = (long)yMin + height;

            if (xMaxCalc > int.MaxValue || yMaxCalc > int.MaxValue)
            {
                // Force the rectangle into an explicitly Empty state upon geometric invalidity
                _XMax = xMin;
                _YMax = yMin;
            }
            else
            {
                _XMax = (int)xMaxCalc;
                _YMax = (int)yMaxCalc;
            }
        }

        #endregion Constructors

        #region Public Methods

        /// <summary> Reset this rectangle with all edges at the origin.</summary>
        public void Clear()
        {
            _XMin = _XMax = _YMin = _YMax = 0;
        }

        /// <summary>
        /// Test if a point is contained in this rectangle.
        /// </summary>
        public readonly bool Contains(int x, int y)
        {
            return (x >= _XMin) && (x < _XMax) && (y >= _YMin) && (y < _YMax);
        }

        /// <summary>
        /// Test if a rectangle is contained within this rectangle.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Architectural Divergence (C++ Quirk Preservation):</b>
        /// From a strict 2D geometric perspective, if the <paramref name="rect"/> parameter is Empty 
        /// (acting as a dimensionless point), this method should evaluate if the coordinate origin 
        /// falls within the bounds. However, the reference C++ DjVuLibre implementation of 
        /// <c>GRect::contains(const GRect &amp; rect)</c> evaluates containment via intersection 
        /// (<c>tmp_rect.intersect(*this, rect); return tmp_rect==rect;</c>). Because intersection 
        /// with an empty rectangle forces the result to empty, and the C++ equality operator considers 
        /// <i>any</i> two empty rectangles to be equal regardless of coordinates, the C++ code always 
        /// returns <c>true</c> when <paramref name="rect"/> is empty.
        /// </para>
        /// <para>
        /// DjvuNet strictly preserves this quirk to guarantee 1:1 parser parity with the native C++ library.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(Rectangle rect)
        {
            bool thisEmpty = this.Empty;
            bool rectEmpty = rect.Empty;

            if (thisEmpty && !rectEmpty)
                return false;

            if (rectEmpty)
            {
                // C++ Quirk: A valid rectangle (or an empty one) "contains" any empty rectangle
                // because GRect::intersect zeroes out coordinates when intersecting with empty,
                // and GRect::operator== returns true if both are empty.
                return true;
            }

            return (Contains(rect._XMin, rect._YMin) && Contains(rect._XMax - 1, rect._YMax - 1));
        }

        /// <summary>
        /// Grow the size of this rectangle by moving all the edges outwards.
        /// </summary>
        public void Inflate(int dx, int dy)
        {
            long newXMin = (long)_XMin - dx;
            long newXMax = (long)_XMax + dx;
            long newYMin = (long)_YMin - dy;
            long newYMax = (long)_YMax + dy;

            if (newXMin > int.MaxValue || newXMin < int.MinValue ||
                newXMax > int.MaxValue || newXMax < int.MinValue ||
                newYMin > int.MaxValue || newYMin < int.MinValue ||
                newYMax > int.MaxValue || newYMax < int.MinValue)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(dx) + ", " + nameof(dy),
                    $"Inflate by dx: {dx}, dy: {dy} caused an overflow/underflow of the internal Int32 coordinate boundaries. " +
                    $"Current state: {this}");
            }

            _XMin = (int)newXMin;
            _XMax = (int)newXMax;
            _YMin = (int)newYMin;
            _YMax = (int)newYMax;
        }

        /// <summary>
        /// Set this rectangle as the intersection of two rectangles.
        /// </summary>
        public void Intersect(Rectangle rect1, Rectangle rect2)
        {
            _XMin = Math.Max(rect1._XMin, rect2._XMin);
            _XMax = Math.Min(rect1._XMax, rect2._XMax);
            _YMin = Math.Max(rect1._YMin, rect2._YMin);
            _YMax = Math.Min(rect1._YMax, rect2._YMax);

            if (Empty)
            {
                Clear();
            }
        }

        /// <summary>
        /// Set this rectangle as the union of two rectangles.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Recthull(Rectangle rect1, Rectangle rect2)
        {
            if (!rect1.Empty && !rect2.Empty)
            {
                _XMin = Math.Min(rect1._XMin, rect2._XMin);
                _XMax = Math.Max(rect1._XMax, rect2._XMax);
                _YMin = Math.Min(rect1._YMin, rect2._YMin);
                _YMax = Math.Max(rect1._YMax, rect2._YMax);
            }
            else
            {
                ProcessEmptyRect(rect1, rect2);
            }
        }

        internal bool ProcessEmptyRect(Rectangle rect1, Rectangle rect2)
        {
            if (rect1.Empty)
            {
                _XMin = rect2._XMin;
                _XMax = rect2._XMax;
                _YMin = rect2._YMin;
                _YMax = rect2._YMax;
                return !Empty;
            }
            else if (rect2.Empty)
            {
                _XMin = rect1._XMin;
                _XMax = rect1._XMax;
                _YMin = rect1._YMin;
                _YMax = rect1._YMax;
                return !Empty;
            }
            return false;
        }

        /// <summary>
        /// Shift this rectangle (linear translation).
        /// </summary>
        public void Translate(int dx, int dy)
        {
            long newXMin = (long)_XMin + dx;
            long newXMax = (long)_XMax + dx;
            long newYMin = (long)_YMin + dy;
            long newYMax = (long)_YMax + dy;

            if (newXMin > int.MaxValue || newXMin < int.MinValue ||
                newXMax > int.MaxValue || newXMax < int.MinValue ||
                newYMin > int.MaxValue || newYMin < int.MinValue ||
                newYMax > int.MaxValue || newYMax < int.MinValue)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(dx) + ", " + nameof(dy),
                    $"Translate by dx: {dx}, dy: {dy} caused an overflow/underflow of the internal Int32 coordinate boundaries. " +
                    $"Current state: {this}");
            }

            _XMin = (int)newXMin;
            _XMax = (int)newXMax;
            _YMin = (int)newYMin;
            _YMax = (int)newYMax;
        }

        /// <summary>
        /// Resizes the rectangle to the specified width and height while anchoring the origin (XMin, YMin).
        /// Dimensions exceeding Int32.MaxValue or causing coordinate overflow will clamp the rectangle to Empty
        /// and throw an exception.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        /// <exception cref="DjvuNet.Errors.DjvuArgumentOutOfRangeException">
        /// Thrown when the <paramref name="width"/> or <paramref name="height"/> exceeds <see cref="int.MaxValue"/>, 
        /// or when the resulting geometrical edges exceed the 32-bit Cartesian space.
        /// </exception>
        public void Resize(int width, int height)
        {
            if (width < 0 || height < 0 || width > int.MaxValue || height > int.MaxValue)
            {
                string paramName = (width < 0 || width > int.MaxValue) && (height < 0 || height > int.MaxValue) ? "width, height" :
                                   ((width < 0 || width > int.MaxValue) ? nameof(width) : nameof(height));
                DjvuExceptionUtil.ThrowArgumentOutOfRange(paramName,
                    $"Resize width ({width}) or height ({height}) is invalid or exceeds Int32.MaxValue.");
                return; // Unreachable, throw terminates
            }

            long xMaxCalc = (long)_XMin + width;
            long yMaxCalc = (long)_YMin + height;

            if (xMaxCalc > int.MaxValue || yMaxCalc > int.MaxValue)
            {
                string paramName = (xMaxCalc > int.MaxValue && yMaxCalc > int.MaxValue) ? "width, height" :
                                   (xMaxCalc > int.MaxValue ? nameof(width) : nameof(height));
                DjvuExceptionUtil.ThrowArgumentOutOfRange(paramName,
                    $"Resize by width: {width}, height: {height} caused an overflow of the internal Int32 coordinate boundaries.");
            }
            else
            {
                _XMax = (int)xMaxCalc;
                _YMax = (int)yMaxCalc;
            }
        }

        /// <summary>
        /// Multiplies xmin, ymin, xmax, ymax by factor and scales the rectangle
        /// </summary>
        public void Scale(float factor)
        {
            Scale(factor, factor);
        }

        /// <summary>
        /// Multiplies xmin, xmax by xfactor and ymin, ymax by yfactor and scales the rectangle
        /// </summary>
        public void Scale(float xfactor, float yfactor)
        {
            // 1. Enforce Orientation: Prevent negative scaling which would invert the Rectangle
            if (xfactor < 0 || yfactor < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(xfactor) + ", " + nameof(yfactor),
                    $"Scale factors cannot be negative (inversion is not supported). Provided: xfactor={xfactor}, yfactor={yfactor}");
            }

            // 2. Prevent NaN/Infinity which cannot be mapped to Cartesian coordinates
            if (float.IsNaN(xfactor) || float.IsInfinity(xfactor) ||
                float.IsNaN(yfactor) || float.IsInfinity(yfactor))
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(xfactor) + ", " + nameof(yfactor), 
                    "Scale factors must be finite numbers.");
            }

            // 3. Emulate C++ FPU truncation exactly: (int)(((float)xmin) * factor)
            // Evaluated as doubles to safely detect Int32 boundary overflows before casting
            double dXMin = (double)((float)_XMin * xfactor);
            double dYMin = (double)((float)_YMin * yfactor);
            double dXMax = (double)((float)_XMax * xfactor);
            double dYMax = (double)((float)_YMax * yfactor);

            // 4. Validate Cartesian Boundary Limits
            if (dXMin > int.MaxValue || dXMin < int.MinValue ||
                dXMax > int.MaxValue || dXMax < int.MinValue ||
                dYMin > int.MaxValue || dYMin < int.MinValue ||
                dYMax > int.MaxValue || dYMax < int.MinValue)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(xfactor) + ", " + nameof(yfactor),
                    $"Scale by xfactor: {xfactor}, yfactor: {yfactor} caused an overflow/underflow of the internal Int32 coordinate boundaries. " +
                    $"Current state: {this}");
            }

            long newXMin = (long)dXMin;
            long newYMin = (long)dYMin;
            long newXMax = (long)dXMax;
            long newYMax = (long)dYMax;

            // 5. Validate Structural Dimensions Limits
            long newWidth = newXMax - newXMin;
            long newHeight = newYMax - newYMin;

            if (newWidth > int.MaxValue || newHeight > int.MaxValue)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(xfactor) + ", " + nameof(yfactor),
                    $"Scale by xfactor: {xfactor}, yfactor: {yfactor} results in dimensions exceeding Int32.MaxValue.");
            }

            _XMin = (int)newXMin;
            _YMin = (int)newYMin;
            _XMax = (int)newXMax;
            _YMax = (int)newYMax;
        }

        /// <summary>
        /// Moves the rectangle to an absolute origin (xMin, yMin) while preserving its current Width and Height.
        /// </summary>
        /// <param name="xMin">The new minimum X coordinate.</param>
        /// <param name="yMin">The new minimum Y coordinate.</param>
        /// <exception cref="DjvuNet.Errors.DjvuArgumentOutOfRangeException">
        /// Thrown when the required translation distance exceeds <see cref="int.MaxValue"/> or <see cref="int.MinValue"/>,
        /// or when the final translated coordinates overflow the 32-bit Cartesian space.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveTo(int xMin, int yMin)
        {
            long dxCalc = (long)xMin - _XMin;
            long dyCalc = (long)yMin - _YMin;

            if (dxCalc > int.MaxValue || dxCalc < int.MinValue ||
                dyCalc > int.MaxValue || dyCalc < int.MinValue)
            {
                string paramName = (dxCalc > int.MaxValue || dxCalc < int.MinValue) && (dyCalc > int.MaxValue || dyCalc < int.MinValue)
                    ? "xMin, yMin"
                    : ((dxCalc > int.MaxValue || dxCalc < int.MinValue) ? nameof(xMin) : nameof(yMin));

                DjvuExceptionUtil.ThrowArgumentOutOfRange(paramName,
                    $"MoveTo destination (xMin: {xMin}, yMin: {yMin}) causes a translation delta that exceeds Int32.MaxValue or Int32.MinValue. " +
                    $"Current state: {this}");
            }

            Translate((int)dxCalc, (int)dyCalc);
        }

        public override readonly string ToString() => $"Rectangle({_XMin}, {_YMin} to {_XMax}, {_YMax})";

        #endregion Public Methods

        #region Equality and Operators (Issue #149 Resolution)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Rectangle other)
        {
            bool isEmpty1 = Empty;
            bool isEmpty2 = other.Empty;

            if (isEmpty1 || isEmpty2)
                return isEmpty1 && isEmpty2;

            return (_XMin == other._XMin) && (_XMax == other._XMax) &&
                   (_YMin == other._YMin) && (_YMax == other._YMax);
        }

        public override readonly bool Equals(object obj) => obj is Rectangle other && Equals(other);

        public static bool operator ==(Rectangle first, Rectangle second) => first.Equals(second);

        public static bool operator !=(Rectangle first, Rectangle second) => !(first == second);

        public override readonly int GetHashCode()
        {
            if (Empty)
                return 0;
            return HashCode.Combine(_XMin, _XMax, _YMin, _YMax);
        }

        /// <summary>
        /// Implicit conversion to <see cref="System.Drawing.Rectangle"/>.
        /// Performs a pure numerical geometric transformation without any page height context.
        /// </summary>
        /// <remarks>
        /// <para>
        /// There are two distinct types of transformations when converting between a DjVu Cartesian Rectangle 
        /// (where Y grows UP from the bottom-left origin) and a GDI+ Screen Rectangle (where Y grows DOWN from the top-left origin):
        /// </para>
        /// <para>
        /// <b>1. Pure Numerical Parity (This Operator):</b>
        /// Maps the raw numerical bounds. Since GDI+ draws downwards (from <c>Y</c> to <c>Y + Height</c>), 
        /// setting <c>Y</c> to the Cartesian <c>YMin</c> ensures the resulting GDI+ rectangle covers the 
        /// exact same mathematical span <c>[YMin, YMax]</c>.
        /// <code>
        ///      DjVu Cartesian (Y grows UP)                     GDI+ Screen (Y grows DOWN)
        ///                                    
        ///           YMax +------------------------+              Y +------------------------+
        ///           (80) |                        |           (20) |                        |
        ///                |                        |                |                        |
        ///                |      Height = 60       |                |      Height = 60       |
        ///                |                        |                |                        |
        ///                |                        |                |                        |
        ///           YMin +------------------------+     Y + Height +------------------------+
        ///           (20)                                      (80)                           
        /// </code>
        /// If <c>Y</c> were incorrectly mapped to <c>YMax (80)</c>, the GDI+ rectangle would span from 80 down to 140.
        /// </para>
        /// <para>
        /// <b>2. Visual Coordinate Transformation within Page:</b>
        /// To visually invert the rectangle so that a box at the top of a Cartesian page appears at the exact same 
        /// physical location on a Screen page, the total page height is required. This visual inversion is handled 
        /// explicitly by <see cref="DjvuNet.Extensions.ExtensionMethods.OrientRectangle(Graphics.Rectangle, int)"/>.
        /// <code>
        ///              Cartesian Page                                   Screen Page
        /// 
        /// Page Top (100) +------------------------+ Screen Top   0 +------------------------+
        ///                |                        |                |                        |
        ///           YMax |========================|              Y |========================|
        ///           (80) |                        |           (20) |                        |
        ///                |      Rectangle         |                |      Rectangle         |
        ///           YMin |========================|     Y + Height |========================|
        ///           (60) |                        |           (40) |                        |
        ///                |                        |                |                        |
        ///                |                        |                |                        |
        ///                |                        |                |                        |
        ///  Page Bottom 0 +------------------------+ Screen Bot 100 +------------------------+
        /// </code>
        /// </para>
        /// </remarks>
        public static implicit operator System.Drawing.Rectangle(Rectangle rect)
        {
            return rect.Empty
                ? System.Drawing.Rectangle.Empty
                : new System.Drawing.Rectangle(rect._XMin, rect._YMin, rect.Width, rect.Height);
        }

        #endregion Equality and Operators
    }
}
