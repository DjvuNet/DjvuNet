// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.Wavelet;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Bitmap = System.Drawing.Bitmap;

namespace DjvuNet.Extensions
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class ExtensionMethods
    {

        /// <summary>
        /// Orients the rectangle for the proper page location
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="pageHeight"></param>
        /// <returns></returns>
        public static System.Drawing.Rectangle OrientRectangle(this System.Drawing.Rectangle rectangle, int pageHeight)
        {
            return new System.Drawing.Rectangle(rectangle.X, pageHeight - rectangle.Y - rectangle.Height, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        /// Orients the rectangle for the proper page location
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="pageHeight"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Drawing.Rectangle OrientRectangle(this Graphics.Rectangle rectangle, int pageHeight)
        {
            return new System.Drawing.Rectangle(rectangle.XMin, pageHeight - rectangle.YMin - rectangle.Height, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        /// Allocates a System.Drawing.Bitmap and copies the Bitmap pixel data into its buffer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Coordinate System Mapping:</b><br/>
        /// The DjVu format stores image data using a Cartesian coordinate system where the origin (0,0) maps to index 0 of the pixel array.
        /// System.Drawing.Bitmap uses Screen coordinates where the origin (0,0) maps to the Scan0 memory address.
        /// </para>
        /// <para>
        /// This method transforms the data from Cartesian to Screen coordinates during the copy operation.
        /// The copy operation starts at the first image row and writes to the last Bitmap row, effectively inverting the image
        /// along a line parallel to the X axis located at 1/2 image height, which acts as a rotation axis.
        /// </para>
        /// </remarks>
        /// <returns>The populated System.Drawing.Bitmap object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public unsafe static System.Drawing.Bitmap ToImage(this ref Graphics.Bitmap bmp)
        {
            if (bmp.Data == null)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"Cannot create image: {nameof(bmp.Data)} buffer is null.");
            }

            if (bmp.Width <= 0 || bmp.Height <= 0)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"Cannot create image: Dimensions must be greater than zero. Actual: {bmp.Width}x{bmp.Height}.");
            }

            PixelFormat format = default(PixelFormat);
            if (bmp.BytesPerPixel == 1)
                format = PixelFormat.Format8bppIndexed;
            else
                DjvuExceptionUtil.ThrowFormatException($"Unsupported pixel format for Bitmap: {bmp.BytesPerPixel}");

            Bitmap image = CopyDataToBitmap(bmp.Width, bmp.Height, (IntPtr)bmp.GetRow(0), bmp.Data.Length - bmp.Border, format, bmp.BytesPerRow);


            if (format == PixelFormat.Format8bppIndexed)
            {
                ColorPalette palette = image.Palette;
                int grays = bmp.Grays;

                if (grays == 2)
                {
                    palette.Entries[0] = Color.Black;
                    palette.Entries[1] = Color.White;
                }
                else
                {
                    for (int i = 0; i < 256; i++)
                    {
                        int g = 255 - (i * 255 / Math.Max(1, grays - 1));
                        // Convince JIT to use asm conditional move
                        g = g < 0 ? 0 : (g > 255 ? 255 : g);

                        palette.Entries[i] = Color.FromArgb(g, g, g);
                    }
                }
                image.Palette = palette;
            }

            return image;
        }

        /// <summary>
        /// Allocates a System.Drawing.Bitmap and copies the PixelMap pixel data into its buffer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Coordinate System Mapping:</b><br/>
        /// The DjVu format stores image data using a Cartesian coordinate system where the origin (0,0) maps to index 0 of the pixel array.
        /// System.Drawing.Bitmap uses Screen coordinates where the origin (0,0) maps to the Scan0 memory address.
        /// </para>
        /// <para>
        /// This method transforms the data from Cartesian to Screen coordinates during the copy operation.
        /// The copy operation starts at the first image row and writes to the last Bitmap row, effectively inverting the image
        /// along a line parallel to the X axis located at 1/2 image height, which acts as a rotation axis.
        /// </para>
        /// </remarks>
        /// <returns>The populated System.Drawing.Bitmap object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static System.Drawing.Bitmap ToImage(this Graphics.PixelMap pixmp)
        {

            if (pixmp == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(pixmp), $"Cannot create image: {nameof(pixmp)} is null.");
            }

            if (pixmp.Data == null)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"Cannot create image: {nameof(pixmp.Data)} buffer is null.");
            }

            if (pixmp.Width <= 0 || pixmp.Height <= 0)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"Cannot create image: Dimensions must be greater than zero. Actual: {pixmp.Width}x{pixmp.Height}.");
            }

            PixelFormat format = default(PixelFormat);
            if (pixmp.BytesPerPixel == 3)
                format = PixelFormat.Format24bppRgb;
            else 
                DjvuExceptionUtil.ThrowFormatException($"Unsupported pixel format for byte count: {pixmp.BytesPerPixel}");

            // Cast to long to prevent 32-bit integer overflow during stride calculation
            long calculatedBytesPerRow = (long)pixmp.BytesPerPixel * pixmp.Width;
            if (calculatedBytesPerRow > int.MaxValue)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(pixmp.Width), pixmp.Width, "Calculated stride exceeds Int32 limits.");
            }
            int bytesPerRow = (int)calculatedBytesPerRow;

            int dataOffset = 0;

            GCHandle hData = default(GCHandle);
            System.Drawing.Bitmap image = null;
            try
            {
                hData = GCHandle.Alloc(pixmp.Data, GCHandleType.Pinned);
                IntPtr offsetPointer = (IntPtr)((long)hData.AddrOfPinnedObject() + dataOffset);
                image = CopyDataToBitmap(pixmp.Width, pixmp.Height, offsetPointer, pixmp.Data.Length - dataOffset, format, bytesPerRow);
            }
            // Let ArgumentExceptions (including DjvuArgumentOutOfRangeException) bubble up
            // so callers can accurately diagnose bounds and pixel format failures.
            finally
            {
                if (hData.IsAllocated)
                {
                    hData.Free();
                }
            }

            return image;
        }

        /// <summary>
        /// Fast copy of managed pixel array data into System.Drawing.Bitmap image.
        /// No checking of passed parameters, therefore, it is a caller responsibility
        /// to provide valid parameter values.
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
        /// <param name="bytesPerSrcRow">
        /// Defines the stride (size of pixel row with padding) for source data. Default value is 0 what
        /// causes function to use as a stride value multiplier of pixel size and image width.
        /// </param>
        /// <returns>
        /// <see cref="System.Drawing.Bitmap"/> created with data copied from Data buffer
        /// of this instance of <see cref="DjvuNet.Graphics.PixelMap"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static System.Drawing.Bitmap CopyDataToBitmap(
            int width, int height, IntPtr data, long length, PixelFormat format, int bytesPerSrcRow = 0)
        {
            int pixelSize = DjvuImage.GetPixelSize(format);

            long calculatedBytesPerRow = bytesPerSrcRow == 0 ? (long)width * pixelSize : bytesPerSrcRow;
            if (calculatedBytesPerRow > int.MaxValue)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width,
                   $"Image dimensions require a row stride ({calculatedBytesPerRow} bytes) that exceeds the 32-bit limits of GDI+.");
            }
            int bytesPerRow = (int)calculatedBytesPerRow;

            long requiredBufferLength = (long)height * bytesPerRow;
            if (requiredBufferLength > length)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(length), length,
                    $"The source buffer length ({length} bytes) is insufficient for the requested image dimensions and stride ({requiredBufferLength} bytes required).");
            }

            System.Drawing.Bitmap bmp = null;
            BitmapData bmpData = null;

            try
            {
                bmp = new System.Drawing.Bitmap(width, height, format);
                bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                                     ImageLockMode.WriteOnly, bmp.PixelFormat);

                // Start writing at the LAST row of the GDI+ bitmap memory
                IntPtr dataPtr = (IntPtr)((long)bmpData.Scan0 + (height - 1) * bmpData.Stride);
                int bytesToCopy = width * pixelSize;
                for (int i = 0; i < height; i++)
                {
                    MemoryUtilities.MoveMemory(dataPtr, data, bytesToCopy);

                    // Move the GDI+ pointer UP one row
                    dataPtr = (IntPtr)((long)dataPtr - bmpData.Stride);

                    // Move the Djvu pointer DOWN one row (as normal)
                    data = (IntPtr)((long)data + bytesPerRow);
                }
            }
            finally
            {
                bmp?.UnlockBits(bmpData);
            }

            return bmp;
        }

    }
}
