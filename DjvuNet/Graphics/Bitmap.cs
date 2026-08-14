using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Buffers;
using DjvuNet.Compression;
using DjvuNet.Errors;

namespace DjvuNet.Graphics
{
    /// <summary>
    /// Instances of Bitmap class represent bilevel or gray-level images.
    /// </summary>
    /// <remarks>
    /// The DjvuNet library uses "bottom left" coordinate system.
    /// Line zero of a bitmap is the bottom line in the bitmap. Pixels are
    /// organized from left to right within each line.
    ///
    /// Class Bitmap can internally represent bilevel images
    /// using a run-length encoded representation. Some algorithms may benefit
    /// from a direct access to this run information.
    ///
    /// Bilevel and gray-level images. Instances of class GBitmap represent
    /// bilevel or gray-level images.Images are usually represented using one
    /// byte per pixel. Value zero represents a white pixel. A value equal to
    /// the number of gray levels minus one represents a black pixel.  The number
    /// of gray levels is returned by the property Grays and can be set
    /// using this same property. This convention of gray representation
    /// is the opposite to conventionally used in .NET or many other libraries.
    ///
    /// The bracket [] indexing operator returns a pointer to the bytes composing
    /// one line of the image.This pointer can be used to read or write the image pixels.
    /// Line zero represents the bottom line of the image.
    ///
    /// The memory organization is setup in such a way that you can safely read a
    /// few pixels located in a small border surrounding all four sides of the
    /// image.  The width of this border can be modified using the property
    /// MinBorder.  The border pixels are initialized to zero and therefore
    /// represent white pixels. You should never write anything into border
    /// pixels because they are shared between images and between lines.
    ///
    /// <para>
    /// <b>ARCHITECTURAL LIMITATION (Image Dimensions):</b><br/>
    /// The maximum supported image size is constrained by a combination of two factors in the current implementation:
    /// <br/>1. The fixed data type is <c>sbyte</c>/<c>byte</c> (1 byte allocated per pixel, regardless of visual color depth).
    /// <br/>2. The underlying data structure is a single standard .NET array (<c>sbyte[] Data</c>), which uses <c>Int32</c> for its index.
    /// <br/>Therefore, the total number of bytes required (Height * BytesPerRow + Border) cannot exceed <c>int.MaxValue</c> (~2GB).
    /// Attempting to decode or allocate images exceeding this size will throw a <see cref="DjvuArgumentOutOfRangeException"/>.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Bitmap : IEquatable<Bitmap>, IDisposable
    {
        private int _Width;

        private int _Height;

        private int _Border;

        /// <summary>End/size of the Data buffer</summary>
        private int _MaxRowOffset;

        private int _BytesPerRow;

        private byte _IsDisposed;

        private byte _Grays;

        private sbyte[] _Data;

        internal byte[] _RleData;

        /// <summary>
        /// Gets or sets the width of the image
        /// </summary>
        public int Width
        {
            get
            {
                return _Width;
            }
            private set
            {
                _Width = value;
            }
        }

        /// <summary>
        /// Gets or sets the height of the image
        /// </summary>
        public int Height
        {
            get
            {
                return _Height;
            }
            private set
            {
                _Height = value;
            }
        }

        /// <summary>
        /// Gets the number of border pixels
        /// </summary>
        public int Border
        {
            get
            {
                return _Border;
            }
        }

        /// <summary>
        /// Gets or sets the number of bytes per row
        /// </summary>
        public int BytesPerRow
        {
            get { return _BytesPerRow; }
        }

        /// <summary>
        /// DjvuNet Bitmap uses 8bpp storage (one byte per pixel)
        /// </summary>
        public int BytesPerPixel => 1;

        /// <summary>
        /// Gets or sets the depth of colors - indirectly influences
        /// effectively used pixel size expressed in bits
        /// </summary>
        public int Grays
        {
            // Grays have to be in range from 2 to 256
            // To fit them into byte we compress bits
            // required for storing max value by subtracting 2
            get { return _Grays + 2; }
            set
            {
                int grays = value - 2;
                if (_Grays != grays)
                {
                    if ((value < 2) || (value > 256))
                    {
                        DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(value),
                            $"Gray levels outside of range: {value}");
                    }

                    _Grays = (byte)grays;
                }
            }
        }

        /// <summary>
        /// Directly accesses the pinned unmanaged pointer for the underlying Data array.
        /// Safe to use without a fixed block because Data is allocated as GC-pinned.
        /// </summary>
        [JsonIgnore]
        public sbyte* DataPointer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (sbyte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(_Data));
        }

        /// <summary>
        /// Retrieves an unmanaged pointer to the specified row, accounting for the border offset.
        /// Safely falls back to the pinned ZeroBuffer for out-of-bounds rows.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte* GetRow(int row)
        {
            return (row < 0 || row >= _Height) ? _ZeroBufferPointer + _Border :
                   (_Data != null) ? DataPointer + RowOffset(row) :
                   ThrowUninitialized();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private sbyte* ThrowUninitialized()
        {
            DjvuExceptionUtil.ThrowInvalidOperation("Cannot acquire a row pointer while the Bitmap is compressed or uninitialized.");
            return null;
        }

        /// <summary>
        /// Gets or sets the image data
        /// </summary>
        public sbyte[] Data
        {
            get
            {
                return _Data;
            }
            private set
            {
                _Data = value;
            }
        }

        private const int RunOverflow = 0xc0;
        /// <summary>
        /// Hard limitation of the DjVu RLE format.
        /// 2-byte runs are encoded as ((byte1 & 0x3F) << 8) | byte2.
        /// The mathematical maximum is (63 << 8) | 255 = 16383 (0x3FFF).
        /// </summary>
        private const int MaxRunSize = 0x3fff;
        private const int RunMsbMask = 0x3f;
        private const int RunLsbMask = 0xff;

        public const int BorderSize = 4;

        private static sbyte[] _ZeroBuffer;
        internal static volatile sbyte* _ZeroBufferPointer;
        internal static volatile int _ZeroBufferSize;
        private static ConcurrentQueue<sbyte[]> _ZeroBuffersHistory;
        internal static Lock _ZeroBufferLock;
        internal static int _LockTimeout;

#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
        [ModuleInitializer]
#pragma warning restore CA2255
        internal static void InitializeZeroBuffer()
        {
            _LockTimeout = 2000;
            _ZeroBufferSize = 8192;
            _ZeroBuffersHistory = new ConcurrentQueue<sbyte[]>();
            _ZeroBufferLock = new Lock();
            _ZeroBuffer = GC.AllocateArray<sbyte>(_ZeroBufferSize, pinned: true);
            unsafe { _ZeroBufferPointer = (sbyte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(_ZeroBuffer)); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void EnsureZeroBuffer(int required)
        {
            if (required <= _ZeroBufferSize) return;

            if(_ZeroBufferLock.TryEnter(_LockTimeout))
            {
                try
                {
                    if (required <= _ZeroBufferSize)
                        return;

                    long newSize = _ZeroBufferSize;
                    while (newSize < required)
                    {
                        newSize <<= 1;
                    }

                    var newBuffer = GC.AllocateArray<sbyte>((int)newSize, pinned: true);

                    _ZeroBuffersHistory.Enqueue(_ZeroBuffer);
                    _ZeroBufferPointer = (sbyte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(newBuffer));
                    _ZeroBufferSize = (int)newSize;
                    _ZeroBuffer = newBuffer;
                }
                finally
                {
                    _ZeroBufferLock.Exit();
                }
            }
            else
            {
                DjvuExceptionUtil.ThrowTimeoutException(
                    $"Deadlock detected: Attempt to acquire lock for ZeroBuffer resize operation timed out: {_LockTimeout}ms.");
            }
        }

        private void Resize(int width, int height, int border, int bytesPerRow)
        {
            Resize(width, height, border, bytesPerRow, Data);
        }

        private void Resize(int width, int height, int border, int bytesPerRow, sbyte[] newDataBuffer)
        {
            // Stride validation: DjvuNet Bitmap uses 8bpp (8 bits per pixel) memory layout,
            // allocating 1 byte per pixel regardless of visual color depth (Grays).
            // Therefore, the row stride (BytesPerRow) must physically accommodate width + border.
            if (bytesPerRow > 0 && bytesPerRow < width + border)
            {
                DjvuExceptionUtil.ThrowArgument("BytesPerRow (stride) is insufficient to hold the image width and border padding.", nameof(bytesPerRow));
            }

            // Promote to long to prevent 32-bit integer overflow during malicious/massive allocations.
            long maxOffsetCalc = ((long)height * bytesPerRow) + border;

            // ARCHITECTURAL LIMITATION:
            // The limit is imposed by a combination of two factors in the current Bitmap implementation:
            // 1. The fixed data type is sbyte/byte (1 byte per pixel).
            // 2. The underlying data structure is a standard .NET array (sbyte[]), which uses Int32 for its index.
            // Therefore, the total number of bytes required (maxOffsetCalc) cannot exceed int.MaxValue (2GB).
            // Future work to remove image size limits will require migrating away from a single 1D array
            // or utilizing advanced memory structures like MemoryMappedFiles or pointer arrays.
            if (maxOffsetCalc > int.MaxValue || maxOffsetCalc < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Image dimensions cause memory boundary calculation to exceed maximum integer size.");
            }

            int newMaxRowOffset = (int)maxOffsetCalc;

            if (newDataBuffer != null && newMaxRowOffset > 0 && newDataBuffer.Length < newMaxRowOffset)
            {
                DjvuExceptionUtil.ThrowInvalidOperation(
                    $"Provided data buffer length ({newDataBuffer.Length}) is too small for the specified dimensions. Required: {newMaxRowOffset}");
            }

            SetHeightPrv(height);
            SetWidthPrv(width);
            _Border = border;
            _BytesPerRow = bytesPerRow;
            _MaxRowOffset = newMaxRowOffset;

            EnsureZeroBuffer(border + bytesPerRow);

            if (Data != newDataBuffer || Data == null)
            {
                Data = GC.AllocateArray<sbyte>(newMaxRowOffset, pinned: true);
                if (newDataBuffer != null)
                {
                    // Force allocation of a pinned array to secure DataPointer against GC compaction
                    Array.Copy(newDataBuffer, Data, Math.Min(newDataBuffer.Length, newMaxRowOffset));
                }
            }
        }

        internal void SetWidth(int width)
        {
            Resize(width, Height, Border, BytesPerRow);
        }

        /// <summary>
        /// Explicitly sets the width of the image map without resizing. Do not call directly.
        /// </summary>
        private void SetWidthPrv(int width)
        {
            if (width < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width cannot be negative.");
            }
            Width = width;
        }

        internal void SetHeight(int height)
        {
            Resize(Width, height, Border, BytesPerRow);
        }

        /// <summary>
        /// Explicitly sets the height of the image map without resizing. Do not call directly.
        /// </summary>
        private void SetHeightPrv(int height)
        {
            if (height < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height cannot be negative.");
            }
            Height = height;
        }

        /// <summary>
        /// Set the minimum border needed
        /// </summary>
        public void SetMinimumBorder(int value)
        {
            if (value < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(value), value, "Minimum border cannot be negative.");
            }

            if (_Border < value)
            {
                if (Data != null)
                {
                    Bitmap tmp = (Bitmap)new Bitmap().Init(ref this, value);
                    Resize(Width, Height, value, tmp.GetRowSize(), tmp.Data);
                    tmp.Data = null;
                }
                else
                {
                    long newStrideCalc = (long)BytesPerRow - _Border + value;
                    if (newStrideCalc > int.MaxValue || newStrideCalc < 0)
                    {
                        DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(value), value, "Calculated stride exceeds bounds.");
                    }
                    Resize(Width, Height, value, (int)newStrideCalc);
                }
            }
        }

        #region Constructors & Dispose

        /// <summary>
        /// Creates a new Bitmap object.
        /// </summary>
        public Bitmap()
        {
        }

        public Bitmap(int height, int width, int border = Bitmap.BorderSize) : this()
        {
            Init(height, width, border);
        }

        public Bitmap(ref Bitmap bmp) : this()
        {
            Init(ref bmp, bmp.Border);
        }

        public Bitmap(sbyte[] data, int height, int width, int border = Bitmap.BorderSize)
            : this()
        {
            Init(data, height, width, border);
        }

        public void Dispose()
        {
            if (!(_IsDisposed > 0))
            {
                _IsDisposed = 1;
                _Data = null;
                _RleData = null;
            }
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Method creates bitmap and initializes it with deserialized data read from supplied Stream.
        /// </summary>
        /// <param name="stream">Stream with serialized data source.</param>
        /// <param name="border">Size of border surrounding bitmap data from all sides.</param>
        /// <returns>Bitmap initialized with data read from stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static Bitmap CreateBitmap(Stream stream, int border = Bitmap.BorderSize)
        {
            // TODO create multi threaded synchronization for accessing Bitmap data;

            // Get magic number
            byte[] magic = new byte[2];
            int b0 = stream.ReadByte();
            int b1 = stream.ReadByte();
            if (b0 == -1 || b1 == -1) DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream.");
            magic[0] = (byte)b0;
            magic[1] = (byte)b1;

            char lookahead = '\n';
            int width = (int)ParserUtil.ReadInteger(ref lookahead, stream);
            int height = (int)ParserUtil.ReadInteger(ref lookahead, stream);
            int maxval = 1;
            Bitmap bitmap = new Bitmap(height, width, border);
            // go reading file
            if (magic[0] == 'P')
            {
                switch (magic[1])
                {
                    case (byte)'1':
                        bitmap.Grays = 2;
                        bitmap.ReadPbmTextStream(stream);
                        return bitmap;

                    case (byte)'2':
                        maxval = (int)ParserUtil.ReadInteger(ref lookahead, stream);
                        if (maxval > 65535)
                        {
                            DjvuExceptionUtil.ThrowFormatException("Cannot read PGM formatted data with depth greater than 16 bits.");
                        }

                        bitmap.Grays = (maxval > 255 ? 256 : maxval + 1);
                        bitmap.ReadPgmTextStream(stream, maxval);
                        return bitmap;

                    case (byte)'4':
                        bitmap.Grays = 2;
                        bitmap.ReadPbmRawStream(stream);
                        return bitmap;

                    case (byte)'5':
                        maxval = (int)ParserUtil.ReadInteger(ref lookahead, stream);
                        if (maxval > 65535)
                        {
                            DjvuExceptionUtil.ThrowFormatException("Cannot read PGM formatted data with depth greater than 16 bits.");
                        }

                        bitmap.Grays = maxval > 255 ? 256 : maxval + 1;
                        bitmap.ReadPgmRawStream(stream, maxval);
                        return bitmap;
                }
            }
            else if (magic[0] == 'R')
            {
                switch (magic[1])
                {
                    case (byte)'4':
                        bitmap.Grays = 2;
                        bitmap.ReadRleStream(stream);
                        return bitmap;
                }
            }

            DjvuExceptionUtil.ThrowFormatException("Data format error.");
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ReadPbmTextStream(Stream stream)
        {
            GCHandle hData = GCHandle.Alloc(Data, GCHandleType.Pinned);
            IntPtr dataPtr = hData.AddrOfPinnedObject();
            try
            {
                byte* row = (byte*)(dataPtr + Border);
                row += (Height - 1) * BytesPerRow;
                for (int n = Height - 1; n >= 0; n--)
                {
                    for (int c = 0; c < Width; c++)
                    {
                        byte bit = (byte)' ';
                        int bitInt = 0;

                        while (bit == ' ' || bit == '\t' || bit == '\r' || bit == '\n')
                        {
                            bit = 0;
                            bitInt = stream.ReadByte();
                            if (bitInt == -1)
                            {
                                DjvuExceptionUtil.ThrowEndOfStream(
                                    $"End of stream reached. Stream: {nameof(stream)}, Position: {stream.Position}");
                            }

                            bit = (byte)bitInt;
                        }

                        if (bit == '1')
                        {
                            row[c] = 1;
                        }
                        else if (bit == '0')
                        {
                            row[c] = 0;
                        }
                        else
                        {
                            DjvuExceptionUtil.ThrowFormatException("Corrupted PBM data.");
                        }
                    }
                    row -= BytesPerRow;
                }
            }
            finally
            {
                if (hData.IsAllocated)
                {
                    hData.Free();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ReadPgmTextStream(Stream stream, int maxval)
        {
            GCHandle hData = GCHandle.Alloc(Data, GCHandleType.Pinned);
            IntPtr dataPtr = hData.AddrOfPinnedObject();
            try
            {
                byte* row = (byte*)(dataPtr + Border);
                row += (Height - 1) * BytesPerRow;
                char lookahead = '\n';

                byte[] ramp = new byte[maxval + 1];

                for (int i = 0; i <= maxval; i++)
                {
                    ramp[i] = (byte)(i < maxval ? ((Grays - 1) * (maxval - i) + maxval / 2) / maxval : 0);
                }

                for (int n = Height - 1; n >= 0; n--)
                {
                    for (int c = 0; c < Width; c++)
                    {
                        row[c] = ramp[(int)ParserUtil.ReadInteger(ref lookahead, stream)];
                    }

                    row -= BytesPerRow;
                }
            }
            finally
            {
                if (hData.IsAllocated)
                {
                    hData.Free();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ReadPbmRawStream(Stream stream)
        {
            GCHandle hData = GCHandle.Alloc(Data, GCHandleType.Pinned);
            IntPtr dataPtr = hData.AddrOfPinnedObject();
            try
            {
                byte* row = (byte*)(dataPtr + Border);
                row += (Height - 1) * BytesPerRow;
                for (int n = Height - 1; n >= 0; n--)
                {
                    byte acc = 0;
                    byte mask = 0;
                    for (int c = 0; c < Width; c++)
                    {
                        if (mask == 0)
                        {
                            int accInt = stream.ReadByte();
                            if (accInt == -1)
                            {
                                DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream.");
                            }

                            acc = (byte)accInt;
                            mask = (byte)0x80;
                        }
                        if ((acc & mask) != 0)
                        {
                            row[c] = 1;
                        }
                        else
                        {
                            row[c] = 0;
                        }

                        mask >>= 1;
                    }
                    row -= BytesPerRow;
                }
            }
            finally
            {
                if (hData.IsAllocated)
                {
                    hData.Free();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ReadPgmRawStream(Stream stream, int maxval)
        {
            int maxbin = (maxval > 255) ? 65536 : 256;
            byte[] ramp = new byte[maxbin];

            for (int i = 0; i < maxbin; i++)
            {
                ramp[i] = (byte)(i < maxval ? ((Grays - 1) * (maxval - i) + maxval / 2) / maxval : 0);
            }

            GCHandle hData = GCHandle.Alloc(Data, GCHandleType.Pinned);
            IntPtr dataPtr = hData.AddrOfPinnedObject();

            GCHandle hRamp = GCHandle.Alloc(ramp, GCHandleType.Pinned);
            IntPtr rampPtr = hRamp.AddrOfPinnedObject();
            try
            {
                byte* bramp = (byte*)rampPtr;
                byte* row = (byte*)(dataPtr + Border);
                row += (Height - 1) * BytesPerRow;
                for (int n = Height - 1; n >= 0; n--)
                {
                    if (maxbin > 256)
                    {
                        for (int c = 0; c < Width; c++)
                        {
                            int b0 = stream.ReadByte();
                            int b1 = stream.ReadByte();
                            if (b0 == -1 || b1 == -1) DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream.");
                            byte[] x = new byte[2];
                            x[0] = (byte)b0;
                            x[1] = (byte)b1;
                            row[c] = bramp[x[0] * 256 + x[1]];
                        }
                    }
                    else
                    {
                        for (int c = 0; c < Width; c++)
                        {
                            int xInt = stream.ReadByte();
                            if (xInt == -1)
                            {
                                DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream.");
                            }

                            row[c] = bramp[xInt];
                        }
                    }
                    row -= BytesPerRow;
                }
            }
            finally
            {
                if (hData.IsAllocated)
                {
                    hData.Free();
                }

                if (hRamp.IsAllocated)
                {
                    hRamp.Free();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ReadRleStream(Stream stream)
        {
            GCHandle hData = GCHandle.Alloc(Data, GCHandleType.Pinned);
            IntPtr dataPtr = hData.AddrOfPinnedObject();
            try
            {
                // interpret runs data
                int hInt = 0;
                byte p = 0;
                byte* row = (byte*)(dataPtr + Border);
                int n = Height - 1;
                row += n * BytesPerRow;
                int c = 0;

                while (n >= 0)
                {
                    hInt = stream.ReadByte();
                    if (hInt == -1)
                    {
                        DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream.");
                    }

                    int x = hInt;
                    if (x >= RunOverflow)
                    {
                        hInt = stream.ReadByte();
                        if (hInt == -1)
                        {
                            DjvuExceptionUtil.ThrowEndOfStream("Unexpected end of stream.");
                        }

                        x = hInt + ((x - RunOverflow) << 8);
                    }

                    if (c + x > Width)
                    {
                        DjvuExceptionUtil.ThrowFormatException("Bitmap RLE format data are not in sync");
                    }

                    while (x-- > 0)
                    {
                        row[c++] = p;
                    }

                    p = (byte)unchecked(1 - p);

                    if (c >= Width)
                    {
                        c = 0;
                        p = 0;
                        row -= BytesPerRow;
                        n -= 1;
                    }
                }
            }
            finally
            {
                if (hData.IsAllocated)
                {
                    hData.Free();
                }
            }
        }

        /// <summary>
        /// Method serializes Bitmap data to PBM raw or text format depending on value of raw parameter.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="raw">
        /// True to serialize to raw PBM format, false to serialize to text PBM format. Default value is true.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SerializeToPbm(Stream stream, bool raw = true)
        {
            // check arguments
            if (Grays > 2)
            {
                DjvuExceptionUtil.ThrowFormatException(
                    $"Only bi-level bitmaps can be saved in PBM format. Grays: {Grays}");
            }

            //GMonitorLock lock (monitor()) ;
            // header
            string header = $"P{(raw ? '4' : '1')}\n{Width} {Height}\n";
            byte[] buffer = new UTF8Encoding(false).GetBytes(header);
            stream.Write(buffer, 0, buffer.Length);

            // body
            if (raw)
            {
                if (_RleData == null)
                {
                    Compress();
                }

                fixed (byte* runsPtr = _RleData)
                {
                    byte* runs = runsPtr;
                    byte* runs_end = runs + _RleData.Length;
                    int count = (Width + 7) >> 3;
                    byte[] byteBuff = new byte[count];
                    fixed (byte* buf = byteBuff)
                    {
                        while (runs < runs_end)
                        {
                            Rle2Bitmap(Width, ref runs, buf, false);
                            stream.Write(byteBuff, 0, count);
                        }
                    }
                }
            }
            else
            {
                if (Data == null)
                {
                    Uncompress();
                }

                fixed (sbyte* rowStart = Data)
                {

                    byte* row = (byte*)rowStart + Border;
                    int n = Height - 1;
                    row += n * BytesPerRow;
                    while (n >= 0)
                    {
                        byte eol = (byte)'\n';
                        for (int c = 0; c < Width;)
                        {
                            byte bit = (byte)(row[c] != 0 ? '1' : '0');
                            stream.WriteByte(bit);
                            c += 1;
                            if (c == Width || (c & RunMsbMask) == 0)
                            {
                                stream.WriteByte(eol);
                            }
                        }
                        // next row
                        row -= BytesPerRow;
                        n -= 1;
                    }
                }
            }
        }

        /// <summary>
        /// Method serializes Bitmap data to PGM raw or text format depending on value of raw parameter.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="raw">
        /// True to serialize to raw PGM format, false to serialize to text PGM format. Default value is true.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SerializeToPgm(Stream stream, bool raw = true)
        {
            // checks
            //GMonitorLock lock (monitor()) ;
            if (Data == null)
            {
                Uncompress();
            }

            // header
            string head = $"P{(raw ? '5' : '2')}\n{Width} {Height}\n{Grays - 1}\n";
            Encoding utf8 = new UTF8Encoding(false);
            byte[] buffer = utf8.GetBytes(head);
            stream.Write(buffer, 0, buffer.Length);

            // body
            fixed (sbyte* bytes = Data)
            {
                byte* row = (byte*)bytes + Border;
                int n = Height - 1;
                row += n * BytesPerRow;
                while (n >= 0)
                {
                    if (raw)
                    {
                        for (int c = 0; c < Width; c++)
                        {
                            sbyte x = (sbyte)(Grays - 1 - row[c]);
                            stream.WriteByte((byte)x);
                        }
                    }
                    else
                    {
                        byte eol = (byte)'\n';
                        for (int c = 0; c < Width;)
                        {
                            string value = $"{Grays - 1 - row[c]} ";
                            byte[] data = utf8.GetBytes(value);
                            stream.Write(data, 0, data.Length);
                            c += 1;
                            if (c == Width || (c & 0x1f) == 0)
                            {
                                stream.WriteByte(eol);
                            }
                        }
                    }
                    row -= BytesPerRow;
                    n -= 1;
                }
            }
        }

        /// <summary>
        /// Serializes Bitmap data using Run Length Encoding compression to RLE format.
        /// </summary>
        /// <param name="stream"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SerializeToRle(Stream stream)
        {
            // checks
            if (Width == 0 || Height == 0)
            {
                DjvuExceptionUtil.ThrowInvalidOperation("Bitmap is not properly initialized.");
            }

            //GMonitorLock lock (monitor()) ;
            if (Grays > 2)
            {
                DjvuExceptionUtil.ThrowInvalidOperation(
                    $"Only bi-level bitmaps can be saved in PBM format. Grays: {Grays}");
            }

            // header
            string head = $"R4\n{Width} {Height}\n";
            byte[] buffer = new UTF8Encoding(false).GetBytes(head);
            stream.Write(buffer, 0, buffer.Length);

            // body
            if (_RleData != null)
            {
                stream.Write(_RleData, 0, _RleData.Length);
            }
            else
            {
                byte[] gruns;
                long size = RleEncode(out gruns);
                if (gruns != null && size > 0)
                {
                    stream.Write(gruns, 0, gruns.Length);
                }
            }
        }

        internal void Compress()
        {
            if (Grays > 2)
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"Cannot compress data with Grays: {Grays}");
            }

            //GMonitorLock lock (monitor()) ;
            if (Data != null)
            {
                byte[] grle;
                long rleLength = RleEncode(out grle);
                if (rleLength > 0)
                {
                    Data = null;
                    _RleData = grle;
                }
            }
        }

        internal void Uncompress()
        {
            // GMonitorLock lock (monitor()) ;
            if (Data == null && _RleData != null)
            {
                fixed (byte* rle = _RleData)
                {
                    RleDecode(rle);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal long RleEncode(out byte[] gpruns)
        {
            gpruns = null;

            // uncompress rle information
            if (Height == 0 || Width == 0)
            {
                return 0;
            }

            if (Data == null)
            {
                gpruns = _RleData;
                return _RleData != null ? _RleData.Length : 0;
            }

            // create run array
            long pos = 0;
            int maxpos = 1024 + Width + Height;
            byte[] runsBuff = new byte[maxpos];

            // encode bitmap as rle
            fixed (sbyte* bytes = Data)
            {
                byte* row = (byte*)bytes + Border;
                int n = Height - 1;
                row += n * BytesPerRow;
                while (n >= 0)
                {
                    if (maxpos < (pos + 2) + (2 * Width))
                    {
                        maxpos += (1024 + 2 * Width);
                        Array.Resize(ref runsBuff, maxpos);
                    }

                    fixed (byte* runs = runsBuff)
                    {
                        byte* runs_pos = runs + pos;
                        byte* runs_pos_start = runs_pos;

                        AppendLine(ref runs_pos, row, Width);

                        pos += (int)(runs_pos - runs_pos_start);
                    }
                    row -= BytesPerRow;
                    n -= 1;
                }
            }
            // return result
            Array.Resize(ref runsBuff, (int)pos);
            gpruns = runsBuff;
            return pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal void RleDecode(byte* runs)
        {
            // initialize pixel array
            if (Width == 0 || Height == 0)
            {
                DjvuExceptionUtil.ThrowInvalidOperation("Bitmap is not properly initialized.");
            }

            long newStrideCalc = (long)Width + Border;

            // This condition should be unreachable under normal circumstances because
            // the Init() and Resize() methods strictly validate memory boundaries beforehand.
            // If this throws, it indicates that encapsulation has been bypassed or a required
            // upstream check is missing.
            if (newStrideCalc > int.MaxValue || newStrideCalc < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(Width), Width, "Calculated stride exceeds bounds.");
            }

            Resize(Width, Height, Border, (int)newStrideCalc);

            if (runs == (byte*)0)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(runs));
            }

            long npixels = Height * BytesPerRow + Border;
            if (npixels > int.MaxValue || npixels < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(npixels), npixels, "Calculated data buffer size exceeds bounds.");
            }

            if (Data == null)
            {
                Data = GC.AllocateArray<sbyte>((int)npixels, true);
            }

            DecodeRleCore(runs, Data, Border, Height, BytesPerRow, Width);

            _RleData = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static void DecodeRleCore(byte* runs, sbyte[] data, int border, int height, int bytesPerRow, int width)
        {
            int c, n;
            byte p = 0;

            fixed (sbyte* pData = data)
            {
                byte* row = (byte*)pData + border;
                n = height - 1;
                row += n * bytesPerRow;
                c = 0;
                while (n >= 0)
                {
                    int x = ReadRun(ref runs);

                    if (c + x > width)
                    {
                        DjvuExceptionUtil.ThrowFormatException("Invalid RLE encoded data.");
                    }

                    while (x-- > 0)
                    {
                        row[c++] = p;
                    }

                    p = (byte)unchecked(1 - p);

                    if (c >= width)
                    {
                        c = 0;
                        p = 0;
                        row -= bytesPerRow;
                        n -= 1;
                    }
                }
            }
        }

        internal void AppendLine(ref byte* data, byte* row, int rowLength, bool invert = false)
        {
            byte* rowEnd = row + rowLength;
            bool p = !invert;

            while (row < rowEnd)
            {
                int count = 0;
                if ((p = !p))
                {
                    if (*row != 0)
                    {
                        for (++count, ++row; (row < rowEnd) && *row != 0; ++count, ++row) ;
                    }
                }
                else if (*row == 0)
                {
                    for (++count, ++row; (row < rowEnd) && *row == 0; ++count, ++row) ;
                }
                AppendRun(ref data, count);
            }
        }

        internal void AppendRun(ref byte* data, int count)
        {
            if (count < RunOverflow)
            {
                data[0] = (byte)count;
                data += 1;
            }
            else if (count <= MaxRunSize)
            {
                data[0] = (byte)((count >> 8) + RunOverflow);
                data[1] = (byte)(count & 0xff);
                data += 2;
            }
            else
            {
                AppendLongRun(ref data, count);
            }
        }

        /// <summary>
        /// Encodes runs larger than the 16383 format limit by chaining maximum-size segments
        /// separated by 0-length runs of the alternating color.
        /// </summary>
        internal void AppendLongRun(ref byte* data, int count)
        {
            while (count > MaxRunSize)
            {
                data[0] = data[1] = 0xff;
                data[2] = 0;
                data += 3;
                count -= MaxRunSize;
            }

            if (count < RunOverflow)
            {
                data[0] = (byte)count;
                data += 1;
            }
            else
            {
                data[0] = (byte)((count >> 8) + RunOverflow);
                data[1] = (byte)(count & 0xff);
                data += 2;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ReadRun(ref byte* data)
        {
            int z = *data++;
            return (z >= RunOverflow) ? ((z & ~RunOverflow) << 8) | (*data++) : z;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal void Rle2Bitmap(int width, ref byte* runs, byte* bitmap, bool invert = false)
        {
            int obyte_def = invert ? 0xff : 0;
            int obyte_ndef = invert ? 0 : 0xff;
            int mask = 0x80, obyte = 0;

            for (int c = width; c > 0;)
            {
                int x = ReadRun(ref runs);
                c -= x;

                while ((x--) > 0)
                {
                    if ((mask >>= 1) == 0)
                    {
                        *(bitmap++) = (byte)(obyte ^ obyte_def);
                        obyte = 0;
                        mask = 0x80;

                        for (; x >= 8; x -= 8)
                        {
                            *(bitmap++) = (byte)obyte_def;
                        }
                    }
                }

                if (c > 0)
                {
                    x = ReadRun(ref runs);
                    c -= x;
                    while ((x--) > 0)
                    {
                        obyte |= mask;
                        if ((mask >>= 1) == 0)
                        {
                            *(bitmap++) = (byte)(obyte ^ obyte_def);
                            obyte = 0;
                            mask = 0x80;

                            for (; x > 8; x -= 8)
                            {
                                *(bitmap++) = (byte)obyte_ndef;
                            }
                        }
                    }
                }
            }

            if (mask != 0x80)
            {
                *(bitmap++) = (byte)(obyte ^ obyte_def);
            }
        }

        public Bitmap Duplicate()
        {
            if (this == default)
                return default;                                                                                   

            Bitmap clone = new Bitmap();

            clone.Grays = Grays;

            clone.Resize(Width, Height, Border, BytesPerRow);

            if (Data != null && clone.Data != null)
            {
                Buffer.BlockCopy(Data, 0, clone.Data, 0, Data.Length);
            }

            return clone;
        }

        public IntPtr this[int rowIndex]
        {
            get { throw new NotImplementedException(); }
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
        public bool GetBooleanAt(int offset)
        {
            return /* (offset < Border) || (offset >= _MaxRowOffset) || */ (Data[offset] == 0);
        }

        /// <summary>
        /// Set the pixel value.
        /// </summary>
        /// <param name="offset">
        /// position of the pixel to set
        /// </param>
        /// <param name="value">
        /// gray scale value to set
        /// </param>
        public void SetByteAt(int offset, sbyte value)
        {
            //if ((offset >= Border) || (offset < _MaxRowOffset))
            //{
            Data[offset] = (sbyte)value;
            //}
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
        public int GetByteAt(int offset)
        {
                return ((offset < Border) || (offset >= _MaxRowOffset)) ? 0 : (0xff & Data[offset]);
        }

        /// <summary>
        /// Insert another bitmap at the specified location. Note that both bitmaps
        /// need to have the same number of grays.
        /// </summary>
        /// <param name="source">
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
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool Blit(ref Bitmap source, int xh, int yh, int subsample)
        {
            if (Unsafe.IsNullRef(ref source))
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(source), $"{typeof(Bitmap).FullName} source reference is null.");
            }

            if (source == default)
            {
                DjvuExceptionUtil.ThrowArgument(
                    $"Cannot Blit a default source {typeof(Bitmap).FullName} into the target as {nameof(source.Data)} is null.", nameof(source));
            }

            int pidx = 0;
            int qidx = 0;

            if (subsample == 1)
            {
                return InsertMap(ref source, xh, yh, true);
            }

            if ((xh >= (Width * subsample)) || (yh >= (Height * subsample)) || ((xh + source.Width) < 0) ||
                ((yh + source.Height) < 0))
            {
                return false;
            }

            if (source.Data != null)
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

                for (; sr < source.Height; sr++)
                {
                    if ((dr >= 0) && (dr < Height))
                    {
                        int dc = zdc;
                        int dc1 = zdc1;
                        qidx = source.RowOffset(sr);
                        pidx = RowOffset(dr);

                        for (int sc = 0; sc < source.Width; sc++)
                        {
                            if ((dc >= 0) && (dc < Width))
                            {
                                Data[pidx + dc] = (sbyte)(Data[pidx + dc] + source.Data[qidx + sc]);
                            }

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
        /// Binarizes a gray level image using a threshold. The number of gray
        /// levels is reduced to #2# as in a bilevel image. All pixels whose value
        /// was strictly greater than threshold are set to black. All other pixels
        /// are set to white.
        /// </summary>
        /// <param name="threshold"></param>
        public void BinarizeGrays(int threshold)
        {
            if (Data != null)
            {
                for (int row = 0; row < Height; row++)
                {
                    int offset = RowOffset(row);
                    for (int c = 0; c < Width; c++)
                    {
                        Data[offset + c] = (Data[offset + c] & 0xFF) > threshold ? (sbyte)1 : (sbyte)0;
                    }
                }
            }
            Grays = 2;
        }

        /// <summary>
        /// Changes the number of gray levels. The argument grays must be in the
        /// range from 2 to 256.  All the pixel values are then rescaled and clipped
        /// in range from 0 to grays-1.
        /// </summary>
        /// <param name="grays"></param>
        public void ChangeGrays(int grays)
        {
            if (grays < 2 || grays > 256)
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(grays), grays, "Gray levels outside of range");

            int ng = grays - 1;
            int og = Grays - 1;
            Grays = grays;

            byte[] conv = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                if (i > og) conv[i] = (byte)ng;
                else conv[i] = (byte)((i * ng + og / 2) / og);
            }

            if (Data != null)
            {
                for (int row = 0; row < Height; row++)
                {
                    int offset = RowOffset(row);
                    for (int n = 0; n < Width; n++)
                    {
                        Data[offset + n] = (sbyte)conv[Data[offset + n] & 0xFF];
                    }
                }
            }
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
        public void Fill(short value)
        {
            int idx = 0;

            sbyte v = (sbyte)value;
            for (int y = 0; y < Height; y++)
            {
                idx = RowOffset(y);

                for (int x = 0; x < Width; x++)
                {
                    Data[idx + x] = v;
                }
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
        public void Fill(ref Bitmap source, int dx, int dy)
        {
            InsertMap(ref source, dx, dy, false);
        }

        /// <summary>
        /// Insert the reference map at the specified location.
        /// </summary>
        /// <param name="source">
        /// map to insert
        /// </param>
        /// <param name="dx">
        /// horizontal position to insert at
        /// </param>
        /// <param name="dy">
        /// vertical position to insert at
        /// </param>
        /// <param name="doBlit">
        /// True if the gray scale values should be added
        /// </param>
        /// <returns>
        /// True if pixels are inserted
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool InsertMap(ref Bitmap source, int dx, int dy, bool doBlit)
        {
            if (Unsafe.IsNullRef(ref source))
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(source), $"{typeof(Bitmap).FullName} source reference is null.");
            }

            if (source == default)
            {
                DjvuExceptionUtil.ThrowArgument(
                    $"Cannot insert a default source {typeof(Bitmap).FullName} into the target as {nameof(source.Data)} is null.", nameof(source));
            }

            int x0 = (dx > 0) ? dx : 0;
            int y0 = (dy > 0) ? dy : 0;
            int x1 = (dx < 0) ? (-dx) : 0;
            int y1 = (dy < 0) ? (-dy) : 0;
            int w0 = Width - x0;
            int w1 = source.Width - x1;
            int w = (w0 < w1) ? w0 : w1;
            int h0 = Height - y0;
            int h1 = source.Height - y1;
            int h = (h0 < h1) ? h0 : h1;

            if ((w > 0) && (h > 0))
            {
                sbyte gmax = (sbyte)(Grays - 1);
                do
                {
                    int offset = RowOffset(y0++) + x0;
                    int refOffset = source.RowOffset(y1++) + x1;
                    int i = w;

                    if (doBlit)
                    {
                        fixed (sbyte* dataLocation = Data, bitDataLocation = source.Data)
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
                        fixed (sbyte* dataLocation = Data, bitDataLocation = source.Data)
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
        [UnscopedRef]
        public ref Bitmap Init(int height, int width, int border)
        {
            if (width < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width cannot be negative.");
            }

            if (height < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height cannot be negative.");
            }

            if (border < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(border), border, "Border cannot be negative.");
            }

            Data = null;
            Grays = 2;

            // The allocation logic matches C++ DjVuLibre GBitmap::init parity:
            // BytesPerRow represents single-sided row padding (Width + Border).
            // RowOffset(Height) calculates: (Height * BytesPerRow) + Border
            // which adds one final border cap at the very end of the contiguous memory buffer.
            int bytesPerRow = width + border;
            Resize(width, height, border, bytesPerRow);

            // POH allocation is fully delegated to Resize.

            return ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public ref Bitmap Init(sbyte[] data, int height, int width, int border)
        {
            if (width < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(width), width, "Width cannot be negative.");
            }

            if (height < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, "Height cannot be negative.");
            }

            if (border < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(border), border, "Border cannot be negative.");
            }

            Data = null;
            Grays = 2;

            // The allocation logic matches C++ DjVuLibre GBitmap::init parity:
            // BytesPerRow represents single-sided row padding (Width + Border).
            // RowOffset(Height) calculates: (Height * BytesPerRow) + Border
            // which adds one final border cap at the very end of the contiguous memory buffer.
            int bytesPerRow = width + border;
            long expectedLength = ((long)height * bytesPerRow) + border;

            if (expectedLength > 0 && (data == null || data.Length != expectedLength))
            {
                DjvuExceptionUtil.ThrowArgument(
                   "Mismatch in data size and Bitmap dimensions.", nameof(data));
            }

            // POH allocation and secure pinning copy is fully delegated to Resize
            Resize(width, height, border, bytesPerRow, data);

            return ref this;
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
        [UnscopedRef]
        public ref Bitmap Init(ref Bitmap source, int border = 0)
        {
            /// TODO: Protect against NullRef generic exception
            if (!Unsafe.AreSame(ref this, ref source))
            {
                Init(source.Height, source.Width, border);
                Grays = source.Grays;

                for (int i = 0; i < Height; i++)
                {
                    for (int j = Width, k = RowOffset(i), kr = source.RowOffset(i); j-- > 0;)
                    {
                        Data[k++] = source.Data[kr++];
                    }
                }
            }
            else if (border > Border)
            {
                SetMinimumBorder(border);
            }

            return ref this;
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
        [UnscopedRef]
        public ref Bitmap Init(ref Bitmap source, Rectangle rect, int border)
        {
            if (Unsafe.AreSame(ref this, ref source))
            {
                Bitmap tmp = new Bitmap();
                tmp.Grays = (Grays);
                tmp.Resize(Width, Height, Border, BytesPerRow, Data);
                Data = null;
                Init(ref tmp, rect, border);
            }
            else
            {
                Init(rect.Height, rect.Width, border);
                Grays = source.Grays;

                Rectangle rect2 = new Rectangle(0, 0, source.Width, source.Height);
                rect2.Intersect(rect2, rect);
                rect2.Translate(-rect.XMin, -rect.YMin);

                if (!rect2.Empty)
                {
                    int dstIdx = 0;
                    int srcIdx = 0;

                    for (int y = rect2.YMin; y < rect2.YMax; y++)
                    {
                        dstIdx = RowOffset(y);
                        /// TODO: Needs protection from NullRef and null Data
                        srcIdx = source.RowOffset(y + rect.YMin);

                        for (int x = rect2.XMin; x < rect2.XMax; x++)
                        {
                            Data[dstIdx + x] = source.Data[srcIdx + x + rect.XMin];
                        }
                    }
                }
            }

            return ref this;
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
        /// <param name="retVal">
        /// The image to copy the data into
        /// </param>
        /// <returns> the translated image
        /// </returns>
        public ref Bitmap Translate(int dx, int dy, ref Bitmap retVal)
        {
            /// TODO: NullRef retVal will throw generic exception
            ref Bitmap bmp = ref retVal;
            if (bmp.Width != Width || bmp.Height != Height)
            {
                bmp = new Bitmap().Init(Height, Width, 0);

                if ((Grays >= 2) && (Grays <= 256))
                {
                    bmp.Grays = Grays;
                }
            }

            bmp.Fill(ref this, -dx, -dy);
            return ref bmp;
        }

        /// <summary>
        /// Find the bounding box for non-white pixels.
        /// </summary>
        /// <returns>
        /// Bounding rectangle
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public Rectangle ComputeBoundingBox()
        {
            if (Data == null && Width > 0 && Height > 0)
            {
                // This state should be statically unreachable due to encapsulation safeguards.
                // If it throws, it means an initialization check was bypassed or memory was externally corrupted.
                DjvuExceptionUtil.ThrowNullReference($"Cannot compute bounding box: the underlying data buffer is null. Width: {Width}, Height: {Height}");
            }

            int w = Width;
            int h = Height;
            int s = GetRowSize();

            int xmin, xmax, ymin, ymax;
            for (xmax = w - 1; xmax >= 0; xmax--)
            {
                int p = RowOffset(0) + xmax;
                int pe = p + (s * h);

                while ((p < pe) && GetBooleanAt(p))
                {
                    p += s;
                }

                if (p < pe)
                {
                    break;
                }
            }

            for (ymax = h - 1; ymax >= 0; ymax--)
            {
                int p = RowOffset(ymax);
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

            for (xmin = 0; xmin <= xmax; xmin++)
            {
                int p = RowOffset(0) + xmin;
                int pe = p + (s * h);

                while ((p < pe) && GetBooleanAt(p))
                {
                    p += s;
                }

                if (p < pe)
                {
                    break;
                }
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

            if (xmin > xmax || ymin > ymax)
            {
                return new Rectangle();
            }

            // Use 64-bit arithmetic to safely detect underflow/overflow
            // matching the architectural pattern used throughout Bitmap.cs
            long rWidthCalc = (long)xmax - xmin;
            long rHeightCalc = (long)ymax - ymin;

            if (rWidthCalc > int.MaxValue || rWidthCalc < 0 ||
                rHeightCalc > int.MaxValue || rHeightCalc < 0)
            {
                return new Rectangle();
            }

            int rWidth = (int)rWidthCalc;
            int rHeight = (int)rHeightCalc;

            return new Rectangle(xmin, ymin, rWidth, rHeight);
        }

        public bool Equals(Bitmap other)
        {
            return this == other;
        }

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            return obj is Bitmap bitmap && this == bitmap;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Height);
            hash.Add(Width);
            hash.Add(Border);
            hash.Add(Grays);

            if (Data != null)
            {
                ReadOnlySpan<byte> byteSpan = new ReadOnlySpan<byte>(Unsafe.As<byte[]>(Data));
                hash.AddBytes(byteSpan);
            }
            else if (_RleData != null)
            {
                long npixels = (long)Height * BytesPerRow + Border;
                sbyte[] buffer = ArrayPool<sbyte>.Shared.Rent((int)npixels);
                try
                {
                    Array.Clear(buffer, 0, (int)npixels);
                    fixed (byte* rle = _RleData)
                    {
                        DecodeRleCore(rle, buffer, Border, Height, BytesPerRow, Width);
                    }
                    ReadOnlySpan<byte> byteSpan = new ReadOnlySpan<byte>(Unsafe.As<byte[]>(buffer), 0, (int)npixels);
                    hash.AddBytes(byteSpan);
                }
                finally
                {
                    ArrayPool<sbyte>.Shared.Return(buffer);
                }
            }

            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"{base.ToString()}: Width: {Width}, Height: {Height}, Border: {Border}, Grays: {Grays}, Data: {(Data == null ? "null" : Data.Length)} sbytes.";
        }

        public static bool operator ==(Bitmap bmp1, Bitmap bmp2)
        {
            if (bmp1.Height != bmp2.Height || bmp1.Width != bmp2.Width || bmp1.Border != bmp2.Border || bmp1.Grays != bmp2.Grays)
            {
                return false;
            }

            if (ReferenceEquals(bmp1.Data, bmp2.Data) && bmp1.Data != null)
            {
                return true;
            }

            // Using compressed Bitmap._RleData for equality comparison seems to be much more efficient than specially decompressing and allocating Data buffer which most probably will be from 10 to 20 times larger than compressed bitonal RLE image
            if (bmp1.Data == null && bmp2.Data == null)
            {
                if (ReferenceEquals(bmp1._RleData, bmp2._RleData))
                    return true;

                if (bmp1._RleData == null || bmp2._RleData == null || bmp1._RleData.Length != bmp2._RleData.Length)
                    return false;

                return new ReadOnlySpan<byte>(bmp1._RleData).SequenceEqual(new ReadOnlySpan<byte>(bmp2._RleData));
            }

            sbyte[] rented1 = null;
            sbyte[] rented2 = null;

            try
            {
                sbyte[] data1 = bmp1.Data;
                int npixels1 = bmp1.Data != null ? bmp1.Data.Length : bmp1.Height * bmp1.BytesPerRow + bmp1.Border;
                if (data1 == null && bmp1._RleData != null)
                {
                    rented1 = ArrayPool<sbyte>.Shared.Rent((int)npixels1);
                    Array.Clear(rented1, 0, npixels1);
                    fixed (byte* rle = bmp1._RleData)
                    {
                        DecodeRleCore(rle, rented1, bmp1.Border, bmp1.Height, bmp1.BytesPerRow, bmp1.Width);
                    }
                    data1 = rented1;
                }

                sbyte[] data2 = bmp2.Data;
                int npixels2 = bmp2.Data != null ? bmp2.Data.Length : bmp2.Height * bmp2.BytesPerRow + bmp2.Border;
                if (data2 == null && bmp2._RleData != null)
                {
                    rented2 = ArrayPool<sbyte>.Shared.Rent((int)npixels2);
                    Array.Clear(rented2, 0, npixels2);
                    fixed (byte* rle = bmp2._RleData)
                    {
                        DecodeRleCore(rle, rented2, bmp2.Border, bmp2.Height, bmp2.BytesPerRow, bmp2.Width);
                    }
                    data2 = rented2;
                }

                if (data1 == null || data2 == null || npixels1 != npixels2)
                {
                    return false;
                }

                return new ReadOnlySpan<sbyte>(data1, 0, npixels1).SequenceEqual(new ReadOnlySpan<sbyte>(data2, 0, npixels2));
            }
            finally
            {
                if (rented1 != null)
                    ArrayPool<sbyte>.Shared.Return(rented1);
                if (rented2 != null)
                    ArrayPool<sbyte>.Shared.Return(rented2);
            }
        }

        public static bool operator !=(Bitmap bmp1, Bitmap bmp2)
        {
            return !(bmp1 == bmp2);
        }

        #endregion Methods
    }
}
