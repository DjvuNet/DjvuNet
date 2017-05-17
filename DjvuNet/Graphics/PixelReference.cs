using System;
using System.Runtime.CompilerServices;
using DjvuNet.Errors;

namespace DjvuNet.Graphics
{
    public class PixelReference : IPixel, IPixelReference
    {
        #region Private Members

        private int _blueOffset;
        private int _greenOffset;
        private int _ncolors;
        private IMap2 _parent;
        private int _redOffset;
        private int _offset;

        #endregion Private Members

        #region Public Properties

        public int ColorNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _ncolors; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal set { _ncolors = value; }
        }

        public int RedOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _redOffset; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal set { _redOffset = value; }
        }

        public int GreenOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _greenOffset; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal set { _greenOffset = value; }
        }

        public int BlueOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _blueOffset; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal set { _blueOffset = value; }
        }

        public IMap2 Parent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _parent; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal set { _parent = value; }
        }

        public int Offset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _offset; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal set { _offset = value; }
        }

        #region Blue

        /// <summary>
        /// Gets or sets the referenced blue value
        /// </summary>
        public sbyte Blue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _parent.Data[_offset + _blueOffset]; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _parent.Data[_offset + _blueOffset] = value; }
        }

        #endregion Blue

        #region Green

        /// <summary>
        /// Gets or sets the referenced green value
        /// </summary>
        public sbyte Green
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _parent.Data[_offset + _greenOffset]; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _parent.Data[_offset + _greenOffset] = value; }
        }

        #endregion Green

        #region Red

        /// <summary>
        /// Gets or sets the referenced red value
        /// </summary>
        public sbyte Red
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _parent.Data[_offset + _redOffset]; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _parent.Data[_offset + _redOffset] = value; }
        }

        #endregion Red

        #endregion Public Properties

        #region Constructors

        /// <summary> 
        /// Creates a createGPixelReference object.
        /// </summary>
        /// <param name="parent">
        /// the image map to refer to
        /// </param>
        /// <param name="offset">
        /// the initial pixel position to refer to
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PixelReference(IMap2 parent, int offset)
        {
            Initialize(parent);
            _offset = offset * _ncolors;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PixelReference(IMap2 parent, int row, int column) : base()
        {
            Initialize(parent);
            _offset = (parent.RowOffset(row) + column) * _ncolors;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize(IMap2 parent)
        {
            _parent = parent;
            ColorNumber = parent.BytesPerPixel;
            _blueOffset = parent.BlueOffset;
            _greenOffset = parent.GreenOffset;
            _redOffset = parent.RedOffset;
        }

        #endregion Constructors

        #region Methods

        /// <summary> 
        /// Copy pixel values from source.
        /// </summary>
        /// <param name="ref">
        /// Source
        /// </param>
        public void SetPixels(IPixelReference source, int length)
        {
            if (source.ColorNumber != _ncolors || source.BlueOffset != _blueOffset ||
                source.GreenOffset != _greenOffset || source.RedOffset != _redOffset)
            {
                while (length-- > 0)
                {
                    CopyFrom(source);
                    source.IncOffset();
                    IncOffset();
                }
            }
            else
            {
                Array.Copy(source.Parent.Data, source.Offset, _parent.Data, _offset, length * _ncolors);
                source.IncOffset(length);
                IncOffset(length);
            }
        }

        /// <summary> 
        /// Set the map image pixel we are referring to.
        /// </summary>
        /// <param name="offset">pixel position
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetOffset(int offset)
        {
            _offset = offset * _ncolors;
        }

        /// <summary> 
        /// Set the map image pixel we are referring to.
        /// </summary>
        /// <param name="row">vertical position
        /// </param>
        /// <param name="column">horizontal position
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetOffset(int row, int column)
        {
            _offset = (_parent.RowOffset(row) + column) * _ncolors;
        }

        /// <summary> 
        /// Convert the following number of pixels from YCC to RGB. The offset will
        /// be advanced to the end.
        /// </summary>
        /// <param name="count">
        /// The number of pixels to convert.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ycc2Rgb(int count)
        {
            if ((_ncolors != 3) || _parent.IsRampNeeded)
                throw new DjvuInvalidOperationException(
                    $"Function {nameof(Ycc2Rgb)} can be used only with three color based images.");

            while (count-- > 0)
            {
                int y = _parent.Data[_offset];
                int b = _parent.Data[_offset + 1];
                int r = _parent.Data[_offset + 2];
                int t2 = r + (r >> 1);
                int t3 = (y + 128) - (b >> 2);
                int b0 = t3 + (b << 1);
                _parent.Data[_offset++] = (sbyte)((b0 < 255) ? ((b0 > 0) ? b0 : 0) : 255);

                int g0 = t3 - (t2 >> 1);
                _parent.Data[_offset++] = (sbyte)((g0 < 255) ? ((g0 > 0) ? g0 : 0) : 255);

                int r0 = y + 128 + t2;
                _parent.Data[_offset++] = (sbyte)((r0 < 255) ? ((r0 > 0) ? r0 : 0) : 255);
            }
        }

        /// <summary> 
        /// Set the blue, green, and red values of the current pixel.
        /// </summary>
        /// <param name="blue">
        /// Pixel value
        /// </param>
        /// <param name="green">
        /// Pixel value
        /// </param>
        /// <param name="red">
        /// Pixel value
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetBGR(int blue, int green, int red)
        {
            fixed (sbyte* pD = _parent.Data)
            {
                sbyte* pData = pD + _offset;
                *pData++ = (sbyte)blue;
                *pData++ = (sbyte)green;
                *pData = (sbyte)red;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetBGR(int color)
        {
            fixed (sbyte* pD = _parent.Data)
            {
                sbyte* pData = pD + 2 + _offset;
                sbyte* pColor = (sbyte*)&color;
                *pData-- = *pColor++;
                *pData-- = *pColor++;
                *pData = *pColor;
            }
        }

        public unsafe void SetBGR(Pixel pixel)
        {
            fixed (sbyte* pD = _parent.Data)
            {
                Pixel* pData = (Pixel*) pD + _offset;
                *pData = pixel;
            }
        }

        /// <summary> 
        /// Create a duplicate of this PixelReference.
        /// </summary>
        /// <returns> the newly created PixelReference
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPixel Duplicate()
        {
            return new PixelReference(_parent, _offset);
        }

        /// <summary> 
        /// Fills an array of pixels from the specified values.
        /// </summary>
        /// <param name="x">
        /// The x-coordinate of the upper-left corner of the region of
        /// pixels
        /// </param>
        /// <param name="y">
        /// The y-coordinate of the upper-left corner of the region of
        /// pixels
        /// </param>
        /// <param name="w">
        /// The width of the region of pixels
        /// </param>
        /// <param name="h">
        /// The height of the region of pixels
        /// </param>
        /// <param name="pixels">
        /// The array of pixels
        /// </param>
        /// <param name="off">
        /// The offset into the pixel array
        /// </param>
        /// <param name="scansize">
        /// The distance from one row of pixels to the next in the
        /// array
        /// </param>
        public void FillRgbPixels(int x, int y, int w, int h, int[] pixels, int off, int scansize)
        {
            int yrev = _parent.ImageHeight - y;

            if (!_parent.IsRampNeeded)
            {
                for (int y0 = yrev; y0-- > (yrev - h); off += scansize)
                {
                    for (int i = off, j = (_parent.RowOffset(y0) + x) * _ncolors, k = w; k > 0; k--, j += _ncolors)
                    {
                        pixels[i++] = unchecked((int)0xff000000) | (0xff0000 & (_parent.Data[j + _redOffset] << 16)) |
                                      (0xff00 & (_parent.Data[j + _greenOffset] << 8)) |
                                      (0xff & _parent.Data[j + _blueOffset]);
                    }
                }
            }
            else
            {
                var ref_Renamed = _parent.CreateGPixelReference(0);
                for (int y0 = yrev; y0-- > (yrev - h); off += scansize)
                {
                    ref_Renamed.SetOffset(y0, x);
                    for (int i = off, k = w; k > 0; k--, ref_Renamed.IncOffset())
                    {
                        pixels[i++] = _parent.PixelRamp(ref_Renamed).GetHashCode();
                    }
                }
            }
        }

        /// <summary> Step to the next pixel.  Care should be taken when stepping past the end of a row.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncOffset()
        {
            _offset += _ncolors;
        }

        /// <summary> 
        /// Skip past the specified number of pixels. Care should be taken when stepping
        /// past the end of a row.
        /// </summary>
        /// <param name="offset">
        /// Number of pixels to step past.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncOffset(int offset)
        {
            _offset += (_ncolors * offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPixel ToPixel()
        {
            return new Pixel(Blue, Green, Red);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(IPixel pixel)
        {
            Red = pixel.Red;
            Green = pixel.Green;
            Blue = pixel.Blue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetGray(sbyte gray)
        {
            Red = gray;
            Green = gray;
            Blue = gray;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(IPixel other)
        {
            if (null != (object) other)
                return Red == other.Red && Green == other.Green && Blue == other.Blue;
            else
                return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(IPixelReference other)
        {
            if (null != (object)other)
                return Red == other.Red && Green == other.Green && Blue == other.Blue;
            else
                return false;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool Equals(PixelReference other)
        //{
        //    if (null != (object)other)
        //        return Red == other.Red && Green == other.Green && Blue == other.Blue;
        //    else
        //        return false;
        //}

        #endregion Methods

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(PixelReference first, PixelReference second)
        {
            return first?.Red == second?.Red && first?.Green == second?.Green && first?.Blue == second?.Blue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(PixelReference first, PixelReference second)
        {
            return first?.Red != second?.Red || first?.Green != second?.Green || first?.Blue != second?.Blue;
        }

        #endregion Operators


    }
}