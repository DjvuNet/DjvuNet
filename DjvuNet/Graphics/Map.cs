using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DjvuNet.Errors;
using DjvuNet.Wavelet;

namespace DjvuNet.Graphics
{
    /// <summary>
    /// This is an abstract class for representing pixel maps.
    /// </summary>
    public abstract class Map : IMap
    {
        #region Public Properties

        /// <summary>
        /// Gets the property values
        /// </summary>
        public Hashtable Properties { get; internal set; }

        /// <summary>
        /// Gets or sets the image data
        /// </summary>
        public sbyte[] Data { get; set; }

        private int _width;

        /// <summary>
        /// Gets or sets the width of the image (ncolumns)
        /// </summary>
        public int Width
        {
            get { return _width; }
            set { _width = Math.Abs(value); }
        }

        private int _height;

        /// <summary>
        /// Gets or sets the height of the image (nrows)
        /// </summary>
        public int Height
        {
            get { return _height; }
            set { _height = Math.Abs(value); }
        }


        /// <summary>
        /// Gets or sets the number of bytes per pixel (NColumns)
        /// </summary>
        public int BytesPerPixel { get; set; }


        /// <summary>
        /// Gets or sets the offset to the blue color
        /// </summary>
        public int BlueOffset { get; set; }

        /// <summary>
        /// Gets or sets the offset to the green color
        /// </summary>
        public int GreenOffset { get; set; }

        /// <summary>
        /// Gets or sets the offset to the red color
        /// </summary>
        public int RedOffset { get; set; }

        /// <summary>
        /// True if the ramp call is needed, false otherwise
        /// </summary>
        public bool IsRampNeeded { get; set; }

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

            PixelFormat format = PixelFormat.Undefined;

            if (BytesPerPixel == 1) format = PixelFormat.Format8bppIndexed;
            else if (BytesPerPixel == 2) format = PixelFormat.Format16bppRgb555;
            else if (BytesPerPixel == 3) format = PixelFormat.Format24bppRgb;
            else if (BytesPerPixel == 4) format = PixelFormat.Format32bppArgb;
            else throw new DjvuFormatException($"Unknown pixel format for byte count: {BytesPerPixel}");

            GCHandle hData = default(GCHandle);
            System.Drawing.Bitmap image = null;
            try
            {
                hData = GCHandle.Alloc(Data, GCHandleType.Pinned);
                image = CopyDataToBitmap(Width, Height, hData.AddrOfPinnedObject(), Data.Length, format);
            }
            catch(ArgumentException aex)
            {
                throw new DjvuAggregateException("Failed to copy data to Sytem.Drawing.Bitmap.", aex);
            }
            finally
            {
                if (hData.IsAllocated)
                    hData.Free();
            }

            image.RotateFlip(rotation);

            return image;
        }



        /// <summary>
        /// Fast copy of managed pixel array data into System.Drawing.Bitmap image.
        /// No checking of passed parameters, therefore, it is a caller responsibility
        /// to provid valid parameter values.
        /// </summary>
        /// <param name="width">
        /// Image width <see cref="System.Int32"/> in pixels
        /// </param>
        /// <param name="height">
        /// Image height <see cref="System.Int32"/> in pixels
        /// </param>
        /// <param name="data">
        /// Pointer <see cref="System.IntPtr"/> to buffer with image data
        /// </param>
        /// <param name="length">
        /// Length <see cref="System.Int64"/> of buffer in bytes
        /// </param>
        /// <param name="format">
        /// Format of image pixel expressed with <see cref="System.Drawing.Imaging.PixelFormat"/> enumeration
        /// </param>
        /// <returns>
        /// <see cref="System.Drawing.Bitmap"/> created with data copied from Data buffer 
        /// of this instance of <see cref="DjvuNet.Graphics.Map"/> 
        /// </returns>
        public static System.Drawing.Bitmap CopyDataToBitmap(
            int width, int height, IntPtr data, long length, PixelFormat format)
        {
            System.Drawing.Bitmap bmp = null;
            BitmapData bmpData = null;

            try
            {
                bmp = new System.Drawing.Bitmap(width, height, format);
                bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                                     ImageLockMode.WriteOnly, bmp.PixelFormat);

                NativeMethods.MoveMemory(bmpData.Scan0, data, length);
            }
            catch(Exception ex)
            {
                throw new DjvuAggregateException(ex);
            }
            finally
            {
                bmp?.UnlockBits(bmpData);
            }

            return bmp;
        }

        #endregion Public Methods
    }
}