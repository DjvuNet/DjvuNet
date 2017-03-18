using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DjvuNet.Graphics
{
    /// <summary>
    /// This is an abstract class for representing pixel maps.
    /// </summary>
    public abstract class Map
    {
        #region Public Properties

        #region Properties

        private Hashtable _properties = Hashtable.Synchronized(new Hashtable());

        /// <summary>
        /// Gets the property values
        /// </summary>
        public Hashtable Properties
        {
            get { return _properties; }

            protected set
            {
                if (Properties != value)
                {
                    _properties = value;
                }
            }
        }

        #endregion Properties

        #region Data

        private sbyte[] _data;

        /// <summary>
        /// Gets or sets the image data
        /// </summary>
        public sbyte[] Data
        {
            get { return _data; }

            set
            {
                if (Data != value)
                {
                    _data = value;
                }
            }
        }

        #endregion Data

        #region ImageWidth

        private int _imageWidth;

        /// <summary>
        /// Gets or sets the width of the image (ncolumns)
        /// </summary>
        public int ImageWidth
        {
            get { return _imageWidth; }

            set
            {
                if (ImageWidth != value)
                {
                    _imageWidth = value;
                }
            }
        }

        #endregion ImageWidth

        #region ImageHeight

        private int _imageHeight;

        /// <summary>
        /// Gets or sets the height of the image (nrows)
        /// </summary>
        public int ImageHeight
        {
            get { return _imageHeight; }

            set
            {
                if (ImageHeight != value)
                {
                    _imageHeight = value;
                }
            }
        }

        #endregion ImageHeight

        #region BytesPerPixel

        private int _bytesPerPixel;

        /// <summary>
        /// Gets or sets the number of bytes per pixel (NColumns)
        /// </summary>
        public int BytesPerPixel
        {
            get { return _bytesPerPixel; }

            set
            {
                if (BytesPerPixel != value)
                {
                    _bytesPerPixel = value;
                }
            }
        }

        #endregion BytesPerPixel

        #region BlueOffset

        private int _blueOffset;

        /// <summary>
        /// Gets or sets the offset to the blue color
        /// </summary>
        public int BlueOffset
        {
            get { return _blueOffset; }

            set
            {
                if (BlueOffset != value)
                {
                    _blueOffset = value;
                }
            }
        }

        #endregion BlueOffset

        #region GreenOffset

        private int _greenOffset;

        /// <summary>
        /// Gets or sets the offset to the green color
        /// </summary>
        public int GreenOffset
        {
            get { return _greenOffset; }

            set
            {
                if (GreenOffset != value)
                {
                    _greenOffset = value;
                }
            }
        }

        #endregion GreenOffset

        #region RedOffset

        private int _redOffset;

        /// <summary>
        /// Gets or sets the offset to the red color
        /// </summary>
        public int RedOffset
        {
            get { return _redOffset; }

            set
            {
                if (RedOffset != value)
                {
                    _redOffset = value;
                }
            }
        }

        #endregion RedOffset

        #region IsRampNeeded

        private bool _isRampNeeded;

        /// <summary>
        /// True if the ramp call is needed, false otherwise
        /// </summary>
        public bool IsRampNeeded
        {
            get { return _isRampNeeded; }

            set
            {
                if (IsRampNeeded != value)
                {
                    _isRampNeeded = value;
                }
            }
        }

        #endregion IsRampNeeded

        #endregion Public Properties

        #region Constructors

        public Map(int ncolors, int redOffset, int greenOffset, int blueOffset, bool isRampNeeded)
        {
            BytesPerPixel = ncolors;
            this.IsRampNeeded = isRampNeeded;
            this.RedOffset = redOffset;
            this.GreenOffset = greenOffset;
            this.BlueOffset = blueOffset;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary> Insert the reference map at the specified location.
        ///
        /// </summary>
        /// <param name="ref">map to insert
        /// </param>
        /// <param name="dx">horizontal position to insert at
        /// </param>
        /// <param name="dy">vertical position to insert at
        /// </param>
        public abstract void Fill(Map ref_Renamed, int dx, int dy);

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
        public virtual void FillRgbPixels(int x, int y, int w, int h, int[] pixels, int off, int scansize)
        {
            CreateGPixelReference(0).FillRgbPixels(x, y, w, h, pixels, off, scansize);
        }

        /// <summary> Shift the origin of the image by coping the pixel data.
        ///
        /// </summary>
        /// <param name="dx">amount to shift the origin of the x-axis
        /// </param>
        /// <param name="dy">amount to shift the origin of the y-axis
        /// </param>
        /// <param name="retval">the image to copy the data into
        ///
        /// </param>
        /// <returns> the translated image
        /// </returns>
        public abstract Map Translate(int dx, int dy, Map retval);

        /// <summary> Query the start offset of a row.
        ///
        /// </summary>
        /// <param name="row">the row to query
        ///
        /// </param>
        /// <returns> the offset to the pixel data
        /// </returns>
        public virtual int RowOffset(int row)
        {
            return row * GetRowSize();
        }

        /// <summary> Query the getRowSize.
        ///
        /// </summary>
        /// <returns> the getRowSize
        /// </returns>
        public virtual int GetRowSize()
        {
            return ImageWidth;
        }

        /// <summary> Create a PixelReference (a pixel iterator) that refers to this map
        /// starting at the specified offset.
        ///
        /// </summary>
        /// <param name="offset">position of the first pixel to reference
        ///
        /// </param>
        /// <returns> the newly created PixelReference
        /// </returns>
        public virtual PixelReference CreateGPixelReference(int offset)
        {
            return new PixelReference(this, offset);
        }

        /// <summary> Create a PixelReference (a pixel iterator) that refers to this map
        /// starting at the specified position.
        ///
        /// </summary>
        /// <param name="row">initial vertical position
        /// </param>
        /// <param name="column">initial horizontal position
        ///
        /// </param>
        /// <returns> the newly created PixelReference
        /// </returns>
        public virtual PixelReference CreateGPixelReference(int row, int column)
        {
            return new PixelReference(this, row, column);
        }

        /// <summary> Convert the pixel to 24 bit color.
        ///
        /// </summary>
        /// <returns>
        /// the converted pixel
        /// </returns>
        public virtual Pixel PixelRamp(PixelReference pixel)
        {
            return pixel;
        }

        /// <summary>
        /// Converts the pixel data to an image
        /// </summary>
        /// <returns></returns>
        public System.Drawing.Bitmap ToImage(RotateFlipType rotation = RotateFlipType.Rotate180FlipX)
        {
            byte[] byteData = new byte[Data.Length];
            Buffer.BlockCopy(Data, 0, byteData, 0, Data.Length);

            PixelFormat format = PixelFormat.Undefined;

            if (BytesPerPixel == 1) format = PixelFormat.Format8bppIndexed;
            else if (BytesPerPixel == 2) format = PixelFormat.Format16bppRgb555;
            else if (BytesPerPixel == 3) format = PixelFormat.Format24bppRgb;
            else if (BytesPerPixel == 4) format = PixelFormat.Format32bppArgb;
            else throw new FormatException(string.Format("Unknown pixel format for byte count: {0}", BytesPerPixel));

            System.Drawing.Bitmap image = CopyDataToBitmap(ImageWidth, ImageHeight, byteData, format);
            image.RotateFlip(rotation);

            return image;
        }

        /// <summary>
        /// TODO create summary
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap CopyDataToBitmap(int width, int height, byte[] data, PixelFormat format)
        {
            //Here create the Bitmap to the know height, width and format
            //PixelFormat.Format24bppRgb
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, format);

            //Create a BitmapData and Lock all pixels to be written
            BitmapData bmpData = bmp.LockBits(
                                 new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                                 ImageLockMode.WriteOnly, bmp.PixelFormat);

            //Copy the data from the byte array into BitmapData.Scan0
            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);

            //Unlock the pixels
            bmp.UnlockBits(bmpData);

            //Return the bitmap
            return bmp;
        }

        #endregion Public Methods
    }
}