using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DjvuNet.Graphics
{
    /// <summary>
    /// Represents bi tonal and gray scale images
    /// </summary>
    public class Bitmap : Map, IBitmap
    {
        // TODO Verify if this change does not break rendering

        // As this is read and assigned from instance methods changing 
        // to instance field - will verify results but perhaps it is
        // one of the bugs which prevents proper image rendering
        private Object[] RampRefArray = new Object[256];

        #region Private Members

        /// <summary>end of the buffer  </summary>
        private int _maxRowOffset;

        private Pixel[] _rampData;

        private object _syncObject = new object();

        #endregion Private Members

        #region Properties

        /// <summary>
        /// Gets or sets Set the number of rows.
        /// </summary>
        internal int Rows
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value != ImageHeight)
                {
                    ImageHeight = value;
                    _maxRowOffset = RowOffset(ImageHeight);
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ImageHeight; }
        }

        #region Grays

        private int _grays;

        /// <summary>
        /// Gets or sets the depth of colors
        /// </summary>
        public int Grays
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _grays; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_grays != value)
                {
                    if ((value < 2) || (value > 256))
                        throw new ArgumentOutOfRangeException(nameof(value),
                            "Gray levels outside of range");

                    _grays = value;
                    _rampData = null;
                }
            }
        }

        #endregion Grays

        #region Border

        private int _border;

        /// <summary>
        /// Gets or sets the number of border pixels
        /// </summary>
        public int Border
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _border; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_border != value)
                {
                    _border = value;
                    _maxRowOffset = RowOffset(ImageHeight);
                }
            }
        }

        #endregion Border
        
        public Pixel[] Ramp
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_rampData != null)
                    return _rampData;
                else
                    return RampNullGrays();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Pixel[] RampNullGrays()
        {
            Pixel[] retval = (Pixel[])RampRefArray[Grays];
            if (retval != null)
                return _rampData = retval;
            else 
                return _rampData = RampNullRefArrayGreys(Grays);
        }

        internal Pixel[] RampNullRefArrayGreys(int grays)
        {
            Pixel[] retval = new Pixel[256];
            retval[0] = Pixel.WhitePixel;
            int color = 0xff0000;
            int gmax = (grays > 1) ? (grays - 1) : 1;
            int i = 1;
            if (gmax > 1)
            {
                int delta = color / gmax;
                do
                {
                    color -= delta;
                    sbyte c = (sbyte)(color >> 16);
                    retval[i++] = new Pixel(c, c, c);
                } while (i < gmax);
            }

            while (i < retval.Length)
                retval[i++] = Pixel.BlackPixel;

            RampRefArray[grays] = retval;
            return retval;
        }

        #region BytesPerRow

        private int _bytesPerRow;

        /// <summary>
        /// Gets or sets the number of bytes per row
        /// </summary>
        public int BytesPerRow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _bytesPerRow; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal set
            {
                if (_bytesPerRow != value)
                {
                    _bytesPerRow = value;
                    _maxRowOffset = RowOffset(ImageHeight);
                }
            }
        }

        #endregion BytesPerRow

        /// <summary>
        /// Set the minimum border needed
        /// </summary>
        /// <param name="minimum">
        /// The minumum border needed
        /// </param>
        public int MinimumBorder
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_border < value)
                {
                    if (Data != null)
                    {
                        IBitmap tmp = new Bitmap().Init(this, value);
                        BytesPerRow = tmp.GetRowSize();
                        Data = tmp.Data;
                        tmp.Data = null;
                    }

                    _border = value;
                }
            }
        }

        #endregion Public Properties

        #region Constructors

        /// <summary>
        /// Creates a new Bitmap object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitmap()
            : base(1, 0, 0, 0, true)
        {
            IsRampNeeded = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitmap(int height, int width, int border = 0) : base(1, 0, 0, 0, true)
        {
            Init(height, width, border);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitmap(IBitmap bmp) : base(1, 0, 0, 0, true)
        {
            Init(bmp, bmp.Border);
        }

        #endregion Constructors

        #region Public Methods

        public IBitmap Duplicate()
        {
            return new Bitmap
            {
                BlueOffset = BlueOffset,
                Border = Border,
                Data = Data,
                Grays = Grays,
                GreenOffset = GreenOffset,
                _maxRowOffset = _maxRowOffset,
                BytesPerPixel = BytesPerPixel,
                ImageWidth = ImageWidth,
                IsRampNeeded = IsRampNeeded,
                ImageHeight = ImageHeight,
                Properties = Properties,
                _rampData = _rampData,
                RedOffset = RedOffset,
                BytesPerRow = BytesPerRow
            };
        }

        /// <summary> 
        /// Query a pixel as boolean
        /// </summary>
        /// <param name="offset">
        /// Position to query
        /// </param>
        /// <returns> 
        /// True if zero
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetBooleanAt(int offset)
        {
            return (offset < Border) || (offset >= _maxRowOffset) || (Data[offset] == 0);
        }

        /// <summary> Set the pixel value.
        ///
        /// </summary>
        /// <param name="offset">position of the pixel to set
        /// </param>
        /// <param name="value">gray scale value to set
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByteAt(int offset, sbyte value)
        {
            if ((offset >= Border) || (offset < _maxRowOffset))
            {
                Data[offset] = (sbyte)value;
            }
        }

        /// <summary> 
        /// Query the pixel at a particular location
        /// </summary>
        /// <param name="offset">
        /// The pixel location
        /// </param>
        /// <returns> 
        /// The gray scale value
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe int GetByteAt(int offset)
        {
            fixed (sbyte* dataLocation = Data)
            {
                return ((offset < Border) || (offset >= _maxRowOffset)) ? 0 : (0xff & dataLocation[offset]);
            }
        }

        /// <summary> 
        /// Insert another bitmap at the specified location.  Note that both bitmaps
        /// need to have the same number of grays.
        /// </summary>
        /// <param name="bm">
        /// Bitmap to insert
        /// </param>
        /// <param name="xh">
        /// Horizontal location to insert at
        /// </param>
        /// <param name="yh">
        /// Vertical location to insert at
        /// </param>
        /// <param name="subsample">
        /// Subsample value at
        /// </param>
        /// <returns> 
        /// True if the blit intersected this bitmap
        /// </returns>
        public bool Blit(IBitmap bm, int xh, int yh, int subsample)
        {
            int pidx = 0;
            int qidx = 0;

            if (subsample == 1)
                return InsertMap(bm, xh, yh, true);

            if ((xh >= (ImageWidth * subsample)) || (yh >= (ImageHeight * subsample)) || ((xh + bm.ImageWidth) < 0) ||
                ((yh + bm.ImageHeight) < 0))
            {
                return false;
            }

            if (bm.Data != null)
            {
                int dr = yh / subsample;
                int dr1 = yh - (subsample * dr);

                if (dr1 < 0)
                {
                    dr--;
                    dr1 += subsample;
                }

                int zdc = xh / subsample;
                int zdc1 = xh - (subsample * zdc);

                if (zdc1 < 0)
                {
                    zdc--;
                    zdc1 += subsample;
                }

                int sr = 0;
                int idx = 0;

                for (; sr < bm.ImageHeight; sr++)
                {
                    if ((dr >= 0) && (dr < ImageHeight))
                    {
                        int dc = zdc;
                        int dc1 = zdc1;
                        qidx = bm.RowOffset(sr);
                        pidx = RowOffset(dr);

                        for (int sc = 0; sc < bm.ImageWidth; sc++)
                        {
                            if ((dc >= 0) && (dc < ImageWidth))
                                Data[pidx + dc] = (sbyte)(Data[pidx + dc] + bm.Data[qidx + sc]);

                            if (++dc1 >= subsample)
                            {
                                dc1 = 0;
                                dc++;
                            }
                        }
                    }

                    if (++dr1 >= subsample)
                    {
                        dr1 = 0;
                        dr++;
                        idx++;
                    }
                }
            }

            return true;
        }

        /// <summary> 
        /// Query the start offset of a row.
        /// </summary>
        /// <param name="row">
        /// The row to query
        /// </param>
        /// <returns> 
        /// The offset to the pixel data
        /// </returns>
        // TODO virtual methods are not inlined - find some other optimizations
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int RowOffset(int row)
        {
            return (row * BytesPerRow) + Border;
        }

        /// <summary> 
        /// Query the number of bytes per row.
        /// </summary>
        /// <returns> 
        /// Bytes per row
        /// </returns>
        // TODO virtual methods are not inlined - find some other optimizations
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRowSize()
        {
            return BytesPerRow;
        }

        /// <summary> 
        /// Set the value of all pixels.
        /// </summary>
        /// <param name="value">
        /// Gray scale value to assign to all pixels
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(short value)
        {
            int idx = 0;

            sbyte v = (sbyte)value;
            for (int y = 0; y < ImageHeight; y++)
            {
                idx = RowOffset(y);

                for (int x = 0; x < ImageWidth; x++)
                    Data[idx + x] = v;
            }
        }

        /// <summary> 
        /// Insert the reference map at the specified location.
        /// </summary>
        /// <param name="ref">
        /// Map to insert
        /// </param>
        /// <param name="dx">
        /// Horizontal position to insert at
        /// </param>
        /// <param name="dy">
        /// Vertical position to insert at
        /// </param>
        // TODO virtual methods are not inlined - find some other optimizations
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(IMap2 source, int dx, int dy)
        {
            InsertMap((IBitmap)source, dx, dy, false);
        }

        /// <summary> 
        /// Insert the reference map at the specified location.
        /// </summary>
        /// <param name="bit">map to insert
        /// </param>
        /// <param name="dx">horizontal position to insert at
        /// </param>
        /// <param name="dy">vertical position to insert at
        /// </param>
        /// <param name="doBlit">
        /// True if the gray scale values should be added
        /// </param>
        /// <returns> 
        /// True if pixels are inserted
        /// </returns>
        public unsafe bool InsertMap(IBitmap bit, int dx, int dy, bool doBlit)
        {
            int x0 = (dx > 0) ? dx : 0;
            int y0 = (dy > 0) ? dy : 0;
            int x1 = (dx < 0) ? (-dx) : 0;
            int y1 = (dy < 0) ? (-dy) : 0;
            int w0 = ImageWidth - x0;
            int w1 = bit.ImageWidth - x1;
            int w = (w0 < w1) ? w0 : w1;
            int h0 = ImageHeight - y0;
            int h1 = bit.ImageHeight - y1;
            int h = (h0 < h1) ? h0 : h1;

            if ((w > 0) && (h > 0))
            {
                sbyte gmax = (sbyte)(Grays - 1);
                do
                {
                    int offset = RowOffset(y0++) + x0;
                    int refOffset = bit.RowOffset(y1++) + x1;
                    int i = w;

                    if (doBlit)
                    {
                        fixed (sbyte* dataLocation = Data, bitDataLocation = bit.Data)
                        {
                            // This is not really correct.  We should reduce the original level by the
                            // amount of the new level.  But since we are normally dealing with non-overlapping
                            // or bitonal blits it really doesn't matter.
                            do
                            {
                                int g = dataLocation[offset] + bitDataLocation[refOffset++];
                                dataLocation[offset++] = (g < Grays) ? (sbyte)g : gmax;
                            } while (--i > 0);
                        }

                        //// This is not really correct.  We should reduce the original level by the
                        //// amount of the new level.  But since we are normally dealing with non-overlapping
                        //// or bitonal blits it really doesn't matter.
                        //do
                        //{
                        //    int g = Data[offset] + bit.Data[refOffset++];
                        //    Data[offset++] = (g < Grays) ? (sbyte)g : gmax;
                        //} while (--i > 0);
                    }
                    else
                    {
                        fixed (sbyte* dataLocation = Data, bitDataLocation = bit.Data)
                        {
                            do
                            {
                                dataLocation[offset++] = bitDataLocation[refOffset++];
                            } while (--i > 0);
                        }

                        //do
                        //{
                        //    Data[offset++] = bit.Data[refOffset++];
                        //} while (--i > 0);
                    }
                } while (--h > 0);
                return true;
            }

            return false;
        }

        /// <summary> 
        /// Initialize this image with the specified values.
        /// </summary>
        /// <param name="height">
        /// Height of image
        /// </param>
        /// <param name="width">
        /// Width of image
        /// </param>
        /// <param name="border">
        /// Width of the border
        /// </param>
        /// <returns> 
        /// The initialized image map
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBitmap Init(int height, int width, int border)
        {
            Data = null;
            Grays = 2;
            Rows = height;
            ImageWidth = width;
            Border = border;
            BytesPerRow = (ImageWidth + Border);   
            // TODO: Verify if value of Bitmap.Border is double sided or single sided?

            int npixels = RowOffset(ImageHeight);

            if (npixels > 0)
                Data = new sbyte[npixels];

            return this;
        }

        /// <summary>
        /// Initialize this map by copying a reference map
        /// </summary>
        /// <param name="source">
        /// Map to copy
        /// </param>
        /// <param name="border">
        /// Number of border pixels
        /// </param>
        /// <returns> 
        /// The initialized Bitmap
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBitmap Init(IBitmap source, int border = 0)
        {
            if (this != source)
            {
                Init(source.ImageHeight, source.ImageWidth, border);
                Grays = source.Grays;

                for (int i = 0; i < ImageHeight; i++)
                {
                    for (int j = ImageWidth, k = RowOffset(i), kr = source.RowOffset(i); j-- > 0; )
                        Data[k++] = source.Data[kr++];
                }
            }
            else if (border > Border)
                MinimumBorder = border;

            return this;
        }

        /// <summary>
        /// Initialize this map by copying a reference map
        /// </summary>
        /// <param name="source">
        /// Map to copy
        /// </param>
        /// <param name="rect">
        /// Area to copy
        /// </param>
        /// <param name="border">
        /// Number of border pixels
        /// </param>
        /// <returns> 
        /// Initialized map
        /// </returns>
        public IBitmap Init(IBitmap source, Rectangle rect, int border)
        {
            if (this == source)
            {
                Bitmap tmp = new Bitmap();
                tmp.Grays = (Grays);
                tmp.Border = ((short)border);
                tmp.BytesPerRow = (BytesPerRow);
                tmp.ImageWidth = ImageWidth;
                tmp.Rows = ImageHeight;
                tmp.Data = Data;
                Data = null;
                Init(tmp, rect, border);
            }
            else
            {
                Init(rect.Height, rect.Width, border);
                Grays = source.Grays;

                Rectangle rect2 = new Rectangle(0, 0, source.ImageWidth, source.ImageHeight);
                rect2.Intersect(rect2, rect);
                rect2.Translate(-rect.Right, -rect.Bottom);

                if (!rect2.Empty)
                {
                    int dstIdx = 0;
                    int srcIdx = 0;

                    for (int y = rect2.Bottom; y < rect2.Top; y++)
                    {
                        dstIdx = RowOffset(y);
                        srcIdx = source.RowOffset(y + rect.Bottom);

                        for (int x = rect2.Right; x < rect2.Top; x++)
                        {
                            Data[dstIdx + x] = source.Data[srcIdx + x];
                        }
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Shift the origin of the image by coping the pixel data.
        /// </summary>
        /// <param name="dx">
        /// Amount to shift the origin on the x-axis
        /// </param>
        /// <param name="dy">
        /// Amount to shift the origin on the y-axis
        /// </param>
        /// <param name="retval">
        /// The image to copy the data into
        /// </param>
        /// <returns> the translated image
        /// </returns>
        public IMap2 Translate(int dx, int dy, IMap2 retval)
        {
            if (!(retval is Bitmap) || (retval.ImageWidth != ImageWidth) || (retval.ImageHeight != ImageHeight))
            {
                IBitmap r = new Bitmap().Init(ImageHeight, ImageWidth, 0);

                if ((Grays >= 2) && (Grays <= 256))
                    r.Grays = (Grays);

                retval = r;
            }

            retval.Fill(this, -dx, -dy);
            return retval;
        }

        /// <summary>
        /// Convert the pixel to 24 bit color.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPixel PixelRamp(IPixelReference pixRef)
        {
            return Ramp[pixRef.Blue];
        }

        /// <summary>
        /// Find the bounding box for non-white pixels.
        /// </summary>
        /// <returns> 
        /// Bounding rectangle
        /// </returns>
        public Rectangle ComputeBoundingBox()
        {
            lock (_syncObject)
            {
                int w = ImageWidth;
                int h = ImageHeight;
                int s = GetRowSize();

                int xmin, xmax, ymin, ymax;
                for (xmax = w - 1; xmax >= 0; xmax--)
                {
                    int p = RowOffset(0) + xmax;
                    int pe = p + (s * h);

                    while ((p < pe) && GetBooleanAt(p))
                        p += s;

                    if (p < pe)
                        break;
                }

                for (ymax = h - 1; ymax >= 0; ymax--)
                {
                    int p = RowOffset(ymax);
                    int pe = p + w;

                    while ((p < pe) && GetBooleanAt(p))
                        ++p;

                    if (p < pe)
                        break;
                }

                for (xmin = 0; xmin <= xmax; xmin++)
                {
                    int p = RowOffset(0) + xmin;
                    int pe = p + (s * h);

                    while ((p < pe) && GetBooleanAt(p))
                        p += s;

                    if (p < pe)
                        break;
                }

                for (ymin = 0; ymin <= ymax; ymin++)
                {
                    int p = RowOffset(ymin);
                    int pe = p + w;

                    while ((p < pe) && GetBooleanAt(p))
                    {
                        ++p;
                    }

                    if (p < pe)
                    {
                        break;
                    }
                }
                Rectangle retval = new Rectangle();
                retval.Right = xmin;
                retval.Left = xmax;
                retval.Bottom = ymin;
                retval.Top = ymax;
                return retval;
            }
        }

        #endregion Methods
    }
}