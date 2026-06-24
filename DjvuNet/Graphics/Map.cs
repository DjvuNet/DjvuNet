using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
        /// Gets or sets the image data
        /// </summary>
        public sbyte[] Data { get; protected set; }

        /// <summary>
        /// Gets or sets the width of the image
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Gets or sets the height of the image
        /// </summary>
        public int Height { get; private set; }


        /// <summary>
        /// Gets or sets the number of bytes per pixel (NColumns)
        /// </summary>
        public int BytesPerPixel { get; protected set; }


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

        /// <summary>
        /// Initializes a new instance of the Map class.
        /// </summary>
        /// <remarks>
        /// See <see cref="Map"/> class remarks for architectural limits regarding maximum dimensions.
        /// </remarks>
        public Map(int ncolors, int redOffset, int greenOffset, int blueOffset, bool isRampNeeded)
        {
            BytesPerPixel = ncolors;
            IsRampNeeded = isRampNeeded;
            RedOffset = redOffset;
            GreenOffset = greenOffset;
            BlueOffset = blueOffset;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Explicitly sets the width of the image map.
        /// </summary>
        protected virtual void SetWidth(int width)
        {
            if (width < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width cannot be negative.");
            }
            Width = width;
        }

        /// <summary>
        /// Explicitly sets the height of the image map.
        /// </summary>
        protected virtual void SetHeight(int height)
        {
            if (height < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height cannot be negative.");
            }
            Height = height;
        }

        public static uint ReadInteger(ref char @char, Stream stream)
        {
            if (stream == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(stream));
            }

            uint xinteger = 0;

            while (@char == ' ' || @char == '\t' || @char == '\r' || @char == '\n' || @char == '#')
            {
                if (@char == '#')
                {
                    int b;
                    do
                    {
                        b = stream.ReadByte();
                        if (b < 0)
                        {
                            DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream while parsing Map header comment.");
                        }
                        @char = (char)b;
                    }
                    while (@char != '\n' && @char != '\r');
                }
                @char = (char)0;

                int nextByte = stream.ReadByte();
                if (nextByte < 0)
                {
                    DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream while parsing Map header whitespace.");
                }
                @char = (char)nextByte;
            }

            if (@char < '0' || @char > '9')
            {
                throw new DjvuFormatException($"Expected integer value. Actual value: {@char}");
            }

            while (@char >= '0' && @char <= '9')
            {
                checked
                {
                    try
                    {
                        xinteger = (xinteger * 10) + (uint)(@char - '0');
                    }
                    catch (OverflowException ex)
                    {
                        throw new DjvuFormatException("Parsed integer exceeds maximum representable bounds for uint.", ex);
                    }
                }
                @char = (char)0;

                int valByte = stream.ReadByte();
                if (valByte < 0)
                {
                    // EOF while reading digits is valid (it means we reached the end of the number at the end of the file)
                    break;
                }
                @char = (char)valByte;
            }

            return xinteger;
        }

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
        /// <remarks>
        /// See <see cref="Map"/> class remarks for architectural limits regarding maximum dimensions.
        /// </remarks>
        public void FillRgbPixels(int x, int y, int w, int h, int[] pixels, int off, int scansize)
        {
            if (pixels == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(pixels));
            }

            // Reference: DjVuLibre explicitly throws on negative dimensions via (unsigned short) casting
            // but explicitly supports 0 via (npix > 0) allocation guards. (See GPixmap.cpp / GBitmap.cpp).
            if (w < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(w), w, "Width cannot be negative.");
            }

            if (h < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(h), h, "Height cannot be negative.");
            }

            if (x < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(x), x, "X coordinate cannot be negative.");
            }

            if (y < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(y), y, "Y coordinate cannot be negative.");
            }

            if (x + w > Width)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(w), w, "Region exceeds horizontal bounds.");
            }

            if (y + h > Height)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(h), h, "Region exceeds vertical bounds.");
            }

            if (off < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(off), off, "Offset cannot be negative.");
            }

            if (scansize < w)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(scansize), scansize, "Scansize cannot be smaller than width.");
            }

            // Calculate required buffer space to prevent buffer over-writes in the nested PixelReference loops.
            long requiredSpace = (h > 0) ? (long)off + ((long)(h - 1) * scansize) + w : off;
            if (requiredSpace > pixels.Length)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"Destination buffer too small. Required: {requiredSpace}, Actual: {pixels.Length}.");
            }

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
        /// Allocates a System.Drawing.Bitmap and copies the pixel data into its buffer.
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
        public System.Drawing.Bitmap ToImage()
        {
            if (Data == null)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"Cannot create image: {nameof(Data)} buffer is null.");
            }

            if (Width <= 0 || Height <= 0)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"Cannot create image: Dimensions must be greater than zero. Actual: {Width}x{Height}.");
            }

            PixelFormat format = default(PixelFormat);
            if (BytesPerPixel == 1)
                format = PixelFormat.Format8bppIndexed;
            else if (BytesPerPixel == 2)
                format = PixelFormat.Format16bppRgb555;
            else if (BytesPerPixel == 3)
                format = PixelFormat.Format24bppRgb;
            else if (BytesPerPixel == 4)
                format = PixelFormat.Format32bppArgb;
            else if (BytesPerPixel == 6)
                format = PixelFormat.Format48bppRgb;
            else if (BytesPerPixel == 8)
                format = PixelFormat.Format64bppArgb;
            else
                DjvuExceptionUtil.ThrowFormatException($"Unknown pixel format for byte count: {BytesPerPixel}");

            int bytesPerRow = -1;
            if (this is Bitmap bitmap)
            {
                bytesPerRow = bitmap.BytesPerRow;
            }
            else if (this is PixelMap)
            {
                // Cast to long to prevent 32-bit integer overflow during stride calculation
                long calculatedBytesPerRow = (long)BytesPerPixel * Width;
                if (calculatedBytesPerRow > int.MaxValue)
                {
                    DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(Width), Width, "Calculated stride exceeds Int32 limits.");
                }
                bytesPerRow = (int)calculatedBytesPerRow;
            }
            else
            {
                // TODO: Rearchitect Map hierarchy to eliminate type-sniffing in the base class.
                // Consider extracting BytesPerRow/Stride calculation into an abstract or virtual property.
                DjvuExceptionUtil.ThrowNotSupported($"Unsupported Map derived type: {this.GetType().FullName}");
            }

            int dataOffset = this switch
            {
                Bitmap { Border: int border } => border,
                _ => 0
            };

            GCHandle hData = default(GCHandle);
            System.Drawing.Bitmap image = null;
            try
            {
                hData = GCHandle.Alloc(Data, GCHandleType.Pinned);
                IntPtr offsetPointer = (IntPtr)((long)hData.AddrOfPinnedObject() + dataOffset);
                image = CopyDataToBitmap(Width, Height, offsetPointer, Data.Length - dataOffset, format, bytesPerRow);
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

            if (format == PixelFormat.Format8bppIndexed)
            {
                System.Drawing.Imaging.ColorPalette palette = image.Palette;
                int grays = 256;
                if (this is Bitmap bmpObj)
                {
                    grays = bmpObj.Grays;
                }

                if (grays == 2)
                {
                    palette.Entries[0] = System.Drawing.Color.Black;
                    palette.Entries[1] = System.Drawing.Color.White;
                }
                else
                {
                    for (int i = 0; i < 256; i++)
                    {
                        int g = 255 - (i * 255 / Math.Max(1, grays - 1));
                        if (g < 0)
                            g = 0;
                        if (g > 255)
                            g = 255;
                        palette.Entries[i] = System.Drawing.Color.FromArgb(g, g, g);
                    }
                }
                image.Palette = palette;
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
        /// of this instance of <see cref="DjvuNet.Graphics.Map"/>
        /// </returns>
        protected System.Drawing.Bitmap CopyDataToBitmap(
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

                for (int i = 0; i < height; i++)
                {
                    MemoryUtilities.MoveMemory(dataPtr, data, bytesPerRow);

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

        #endregion Public Methods
    }
}
