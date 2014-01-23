// -----------------------------------------------------------------------
// <copyright file="DjvuImage.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DjvuNet.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public abstract class DjvuImage
    {
        #region Public Properties

        /// <summary>
        /// Gets the width of the image
        /// </summary>
        public abstract int ImageWidth { get; set; }

        /// <summary>
        /// Gets the height of the image
        /// </summary>
        public abstract int ImageHeight { get; set; }

        /// <summary>
        /// Gets the total colors in the image
        /// </summary>
        public abstract int BytesPerPixel { get; set; }

        /// <summary>
        /// Gets the data for the image
        /// </summary>
        public abstract sbyte[] Data { get; set; }

        #endregion Public Properties

        #region Public Methods

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
            else throw new Exception(string.Format("Unknown pixel format for byte count: {0}", BytesPerPixel));

            System.Drawing.Bitmap image = CopyDataToBitmap(ImageWidth, ImageHeight, byteData, format);
            image.RotateFlip(rotation);

            return image;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        ///
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

        #endregion Private Methods
    }
}