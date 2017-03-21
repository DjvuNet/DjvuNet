using System;
using System.Runtime.CompilerServices;

namespace DjvuNet.Graphics
{
    public class PixelReference : DjvuNet.Graphics.Pixel
    {
        #region Private Members

        private readonly int _blueOffset;
        private readonly int _greenOffset;
        private readonly int _ncolors;
        private readonly Map _parent;
        private readonly int _redOffset;
        private int _offset;

        #endregion Private Members

        #region Public Properties

        #region Blue

        /// <summary>
        /// Gets or sets the referenced blue value
        /// </summary>
        public override sbyte Blue
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
        public override sbyte Green
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
        public new sbyte Red
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
        public PixelReference(Map parent, int offset) : base()
        {
            _parent = parent;
            _ncolors = parent.BytesPerPixel;
            _offset = offset * _ncolors;
            _blueOffset = parent.BlueOffset;
            _greenOffset = parent.GreenOffset;
            _redOffset = parent.RedOffset;
        }

        public PixelReference(Map parent, int row, int column) : base()
        {
            _parent = parent;
            _ncolors = parent.BytesPerPixel;
            _offset = (parent.RowOffset(row) + column) * _ncolors;
            _blueOffset = parent.BlueOffset;
            _greenOffset = parent.GreenOffset;
            _redOffset = parent.RedOffset;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary> Copy the pixel values.
        ///
        /// </summary>
        /// <param name="ref">pixel to copy
        /// </param>
        public void SetPixels(PixelReference ref_Renamed, int length)
        {
            if (ref_Renamed._ncolors != _ncolors || ref_Renamed._blueOffset != _blueOffset ||
                ref_Renamed._greenOffset != _greenOffset || ref_Renamed._redOffset != _redOffset)
            {
                while (length-- > 0)
                {
                    CopyFrom(ref_Renamed);
                    ref_Renamed.IncOffset();
                    IncOffset();
                }
            }
            else
            {
                Array.Copy(ref_Renamed._parent.Data, ref_Renamed._offset, _parent.Data, _offset, length * _ncolors);
                ref_Renamed.IncOffset(length);
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
                throw new SystemException($"{nameof(Ycc2Rgb)} only legal with three colors");

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

        /// <summary> Set the blue, green, and red values of the current pixel.
        ///
        /// </summary>
        /// <param name="blue">pixel value
        /// </param>
        /// <param name="green">pixel value
        /// </param>
        /// <param name="red">pixel value
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SetBGR(int blue, int green, int red)
        {
            _parent.Data[_offset + _blueOffset] = (sbyte)blue;
            _parent.Data[_offset + _greenOffset] = (sbyte)green;
            _parent.Data[_offset + _redOffset] = (sbyte)red;
        }

        /// <summary> Create a duplicate of this PixelReference.
        ///
        /// </summary>
        /// <returns> the newly created PixelReference
        /// </returns>
        public new PixelReference Duplicate()
        {
            return new PixelReference(_parent, _offset);
        }

        /// <summary> Fills an array of pixels from the specified values.
        ///
        /// </summary>
        /// <param name="x">the x-coordinate of the upper-left corner of the region of
        /// pixels
        /// </param>
        /// <param name="y">the y-coordinate of the upper-left corner of the region of
        /// pixels
        /// </param>
        /// <param name="w">the width of the region of pixels
        /// </param>
        /// <param name="h">the height of the region of pixels
        /// </param>
        /// <param name="pixels">the array of pixels
        /// </param>
        /// <param name="off">the offset into the pixel array
        /// </param>
        /// <param name="scansize">the distance from one row of pixels to the next in the
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
                PixelReference ref_Renamed = _parent.CreateGPixelReference(0);
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

        /// <summary> Skip past the specified number of pixels.  Care should be taken when stepping
        /// past the end of a row.
        ///
        /// </summary>
        /// <param name="offset">number of pixels to step past.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncOffset(int offset)
        {
            _offset += (_ncolors * offset);
        }

        #endregion Public Methods
    }
}