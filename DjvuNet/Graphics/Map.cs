using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DjvuNet.Graphics
{
    /// <summary>
    /// This is an abstract class for representing pixel maps.
    /// </summary>
    public abstract class Map : IMap
    {
        #region Public Properties

        #region Properties

        /// <summary>
        /// Gets the property values
        /// </summary>
        public Hashtable Properties { get; internal set; }

        #endregion Properties

        #region Data

        /// <summary>
        /// Gets or sets the image data
        /// </summary>
        public sbyte[] Data { get; set; }

        #endregion Data

        #region ImageWidth

        private int _width;

        /// <summary>
        /// Gets or sets the width of the image (ncolumns)
        /// </summary>
        public int ImageWidth
        {
            get { return _width; }
            set { _width = Math.Abs(value); }
        }

        #endregion ImageWidth

        #region ImageHeight

        private int _height;

        /// <summary>
        /// Gets or sets the height of the image (nrows)
        /// </summary>
        public int ImageHeight
        {
            get { return _height; }
            set { _height = Math.Abs(value); }
        }

        #endregion ImageHeight

        #region BytesPerPixel

        /// <summary>
        /// Gets or sets the number of bytes per pixel (NColumns)
        /// </summary>
        public int BytesPerPixel { get; set; }

        #endregion BytesPerPixel

        #region BlueOffset

        /// <summary>
        /// Gets or sets the offset to the blue color
        /// </summary>
        public int BlueOffset { get; set; }

        #endregion BlueOffset

        #region GreenOffset

        /// <summary>
        /// Gets or sets the offset to the green color
        /// </summary>
        public int GreenOffset { get; set; }

        #endregion GreenOffset

        #region RedOffset

        /// <summary>
        /// Gets or sets the offset to the red color
        /// </summary>
        public int RedOffset { get; set; }

        #endregion RedOffset

        #region IsRampNeeded

        /// <summary>
        /// True if the ramp call is needed, false otherwise
        /// </summary>
        public bool IsRampNeeded { get; set; }

        #endregion IsRampNeeded

        #endregion Public Properties

        #region Constructors

        public Map(int ncolors, int redOffset, int greenOffset, int blueOffset, bool isRampNeeded)
        {
            Properties = Hashtable.Synchronized(new Hashtable());
            BytesPerPixel = ncolors;
            IsRampNeeded = isRampNeeded;
            RedOffset = redOffset;
            GreenOffset = greenOffset;
            BlueOffset = blueOffset;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary> 
        /// Fills an array of pixels from the specified values.
        /// </summary>
        /// <param name="x">
        /// The x-coordinate of the upper-left corner of the region of pixels
        /// </param>
        /// <param name="y">
        /// The y-coordinate of the upper-left corner of the region of pixels
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
        /// The distance from one row of pixels to the next in the array
        /// </param>
        public void FillRgbPixels(int x, int y, int w, int h, int[] pixels, int off, int scansize)
        {
            CreateGPixelReference(0).FillRgbPixels(x, y, w, h, pixels, off, scansize);
        }


        /// <summary> 
        /// Create a PixelReference (a pixel iterator) that refers to this map
        /// starting at the specified offset.
        /// </summary>
        /// <param name="offset">
        /// Position of the first pixel to reference
        /// </param>
        /// <returns> 
        /// The newly created PixelReference
        /// </returns>
        public IPixelReference CreateGPixelReference(int offset)
        {
            return new PixelReference((IMap2)this, offset);
        }

        /// <summary> 
        /// Create a PixelReference (a pixel iterator) that refers to this map
        /// starting at the specified position.
        /// </summary>
        /// <param name="row">initial vertical position
        /// </param>
        /// <param name="column">initial horizontal position
        ///
        /// </param>
        /// <returns> the newly created PixelReference
        /// </returns>
        public IPixelReference CreateGPixelReference(int row, int column)
        {
            return new PixelReference((IMap2)this, row, column);
        }

        /// <summary>
        /// Converts the pixel data to an image
        /// </summary>
        /// <returns></returns>
        public System.Drawing.Bitmap ToImage(RotateFlipType rotation = RotateFlipType.Rotate180FlipX)
        {
            // TODO replace this code with efficient xplat memory copy
            // perhaps it should be done inside CopyDataToBitmap
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
        /// Fast copy of managed pixel array data into System.Drawing.Bitmap image.
        /// No checking of passed parameters, therefore, it is a caller responsibility
        /// to provid valid parameter values.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap CopyDataToBitmap(int width, int height, byte[] data, PixelFormat format)
        {
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, format);

            BitmapData bmpData = bmp.LockBits(
                                 new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                                 ImageLockMode.WriteOnly, bmp.PixelFormat);

            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        #endregion Public Methods
    }
}