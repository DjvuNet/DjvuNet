using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
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
    /// <br/>Therefore, the total number of bytes required (Height * BytesPerRow + Border) cannot exceed <see cref="Array.MaxLength"/> (~2GB).
    /// Attempting to decode or allocate images exceeding this size will throw a <see cref="DjvuArgumentOutOfRangeException"/>.
    /// </para>
    /// <para>
    /// <b>INTERMEDIATE ARCHITECTURE NOTE (IDisposable Struct):</b><br/>
    /// The implementation of <see cref="IDisposable"/> on this mutable struct is an intermediate transition pattern.
    /// It currently manages the lifecycle of individual pinned array allocations. DjvuNet documents can contain thousands 
    /// or tens of thousands of <see cref="Bitmap"/> instances with small backing buffers, alongside outliers requiring 
    /// large buffers. This struct-based allocation pattern will be deferred and replaced by a global memory management 
    /// mechanism based on a custom pinned array pool or unmanaged arena memory. Additionally, an RLE Compress/Decompress 
    /// pattern of Data buffers memory (3 - 100 compression ratio) is in advanced implementation stages to ensure pinned 
    /// buffers remain strictly short-lived.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct Bitmap : IEquatable<Bitmap>, IDisposable
    {
        private int _Width;

        private int _Height;

        private int _Border;

        /// <summary>End/size of the Data buffer</summary>
        private int _MaxRowOffset;

        private int _BytesPerRow;

        private byte _IsDisposed;

        private byte _Grays;

        internal sbyte[] _Data;

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

        /// <summary>
        /// Gets the raw Run-Length Encoded (RLE) data buffer, if present.
        /// </summary>
        public byte[] RleData
        {
            get
            {
                return _RleData;
            }
            private set
            {
                _RleData = value;
            }
        }


        /// <summary>
        /// Gets a value indicating whether this <see cref="Bitmap"/> has been disposed.
        /// </summary>
        public bool IsDisposed => _IsDisposed > 0;

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
        internal static void EnsureZeroBuffer(long required)
        {
            if (required > Array.MaxLength || required < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(required), required, $"Required {nameof(_ZeroBuffer)} size is invalid (less than zero or exceeds Array.MaxLength).");
            }

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

                    if (newSize > Array.MaxLength)
                    {
                        // If doubling exceeds the array limit, fallback to the required size 
                        // aligned up to the nearest 4KB page boundary (4096 bytes).
                        newSize = (required + 4095L) & ~4095L;

                        // Cap to Array.MaxLength if the 4KB alignment pushed it over
                        if (newSize > Array.MaxLength)
                        {
                            newSize = Array.MaxLength;
                        }
                    }

                    sbyte[] newBuffer = null;
                    try
                    {
                        newBuffer = GC.AllocateArray<sbyte>((int)newSize, pinned: true);
                    }
                    catch (Exception ex)
                    {
                        DjvuExceptionUtil.ThrowInvalidOperation($"Failed to allocate pinned _ZeroBuffer of size {newSize}.", ex);
                    }

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

        /// <summary>
        /// Resizes the core image memory buffer.
        /// <b>Note:</b> If the current <see cref="Data"/> buffer is smaller than the new dimensions, this method 
        /// strictly throws an exception rather than reallocating. To grow an existing buffer in-place, <see cref="Data"/> MUST be nullified first.
        /// </summary>
        /// <param name="uninitialized">
        /// <b>UNSAFE CONTRACT:</b> If true, skips CLR memory zeroing. The caller MUST manually zero-initialize 
        /// the border regions to maintain structural integrity.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize(int width, int height, int border, int bytesPerRow, bool uninitialized = false)
        {
            Resize(width, height, border, bytesPerRow, Data, uninitialized, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize(int width, int height, int border, int bytesPerRow, sbyte[] newData)
        {
            Resize(width, height, border, bytesPerRow, newData, false, false);
        }

        /// <summary>
        /// Resizes the core image memory buffer and strictly validates dimensions and lengths.
        /// <b>Note:</b> If <paramref name="newData"/> is provided but is smaller than the required dimensions, this method 
        /// strictly throws an exception rather than automatically reallocating. To up-size, the caller MUST pass null.
        /// By default, this method guarantees safe Garbage Collector (GC) pinning by unconditionally allocating 
        /// a new unmanaged pinned array and copying the contents of <paramref name="newData"/> into it. 
        /// This ensures that unsafe <see cref="DataPointer"/> memory accesses will not corrupt the heap if the GC compacts memory.
        /// When <paramref name="useNewData"/> is true, this double-allocation is entirely bypassed. The method 
        /// safely adopts direct ownership of <paramref name="newData"/> under the strict requirement that the caller 
        /// has already allocated it utilizing <see cref="GC.AllocateArray{T}(int, bool)"/> with pinned set to true,
        /// and that the array length is exactly equal to the maximum row offset to maintain structural integrity.
        /// </summary>
        /// <param name="width">The target width in pixels.</param>
        /// <param name="height">The target height in pixels.</param>
        /// <param name="border">The border size in bytes.</param>
        /// <param name="bytesPerRow">The number of bytes per row (width + border).</param>
        /// <param name="newData">The incoming pixel data array.</param>
        /// <param name="uninitialized">
        /// If true, allocates the new pinned array without zeroing memory to completely bypass CLR allocation overhead. 
        /// <b>UNSAFE CONTRACT:</b> This is an internal performance optimization. The caller assumes absolute responsibility 
        /// for fully overwriting the buffer, which strictly includes manually zero-initializing the <paramref name="border"/> 
        /// margins to guarantee they represent white pixels.
        /// </param>
        /// <param name="useNewData">
        /// If true, bypasses array duplication and explicitly adopts the <paramref name="newData"/> array. 
        /// <b>UNSAFE CONTRACT:</b> Because .NET lacks a public API to detect if an array is pinned, this parameter 
        /// relies on a strict internal API contract. The caller assumes absolute responsibility for guaranteeing 
        /// that <paramref name="newData"/> was explicitly pinned during allocation to prevent access to dangling pointers via DataPointer.
        /// </param>
        private void Resize(int width, int height, int border, int bytesPerRow, sbyte[] newData, bool uninitialized, bool useNewData)
        {
            // Validation: DjvuNet Bitmap uses 8bpp (8 bits per pixel) memory layout,
            // allocating 1 byte per pixel regardless of visual color depth (Grays).
            // Therefore, the bytes per row must physically accommodate width + border.
            // NOTE: The `bytesPerRow > 0` condition intentionally permits `0` to bypass bounds 
            // validation. This is an architecturally valid state representing an empty, 
            // 0-dimensional Bitmap with a 0-length Data buffer.
            if (bytesPerRow > 0 && bytesPerRow < width + border)
            {
                DjvuExceptionUtil.ThrowArgument("BytesPerRow is insufficient to hold the image width and border.", nameof(bytesPerRow));
            }

            // Promote to long to prevent 32-bit integer overflow during malicious/massive allocations.
            long maxOffsetCalc = ((long)height * bytesPerRow) + border;

            // ARCHITECTURAL LIMITATION:
            // The limit is imposed by a combination of two factors in the current Bitmap implementation:
            // 1. The fixed data type is sbyte/byte (1 byte per pixel).
            // 2. The underlying data structure is a standard .NET array (sbyte[]), which uses Int32 for its index.
            // Therefore, the total number of bytes required (maxOffsetCalc) cannot exceed Array.MaxLength (0x7FFFFFC7).
            // Future work to remove image size limits will require migrating away from a single 1D array
            // or utilizing advanced memory structures like MemoryMappedFiles or pointer arrays.
            if (maxOffsetCalc > Array.MaxLength || maxOffsetCalc < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(height), height, $"Image dimensions result in an invalid {nameof(Data)} buffer size (less than zero or exceeding Array.MaxLength).");
            }

            int newMaxRowOffset = (int)maxOffsetCalc;

            if (newData != null && newMaxRowOffset > 0 && newData.Length < newMaxRowOffset)
            {
                DjvuExceptionUtil.ThrowInvalidOperation(
                    $"Provided data buffer length ({newData.Length}) is too small for the specified dimensions. Required: {newMaxRowOffset}");
            }

            SetHeightPrv(height);
            SetWidthPrv(width);
            _Border = border;
            _BytesPerRow = bytesPerRow;
            _MaxRowOffset = newMaxRowOffset;

            long requiredZeroBuffer = (long)Math.Min(height, 1) * bytesPerRow + border;
            if (requiredZeroBuffer > Array.MaxLength || requiredZeroBuffer < 0)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(bytesPerRow), bytesPerRow, $"Requirements for {nameof(_ZeroBuffer)} result in an invalid size (less than zero or exceeding Array.MaxLength).");
            }

            EnsureZeroBuffer(requiredZeroBuffer);

            if (useNewData && newData != null && newData.Length == newMaxRowOffset)
            {
                Data = newData;
            }
            else if (Data != newData || Data == null)
            {
                try
                {
                    Data = uninitialized
                        ? GC.AllocateUninitializedArray<sbyte>(newMaxRowOffset, pinned: true)
                        : GC.AllocateArray<sbyte>(newMaxRowOffset, pinned: true);
                }
                catch (Exception ex)
                {
                    DjvuExceptionUtil.ThrowInvalidOperation($"Failed to allocate pinned Data array of size {newMaxRowOffset}.", ex);
                }
                
                if (newData != null)
                {
                    // Force allocation of a pinned array to secure DataPointer against GC compaction
                    Array.Copy(newData, Data, Math.Min(newData.Length, newMaxRowOffset));
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
                    Bitmap tmp = new Bitmap();
                    tmp.Init(ref this, value);
                    Resize(Width, Height, value, tmp.BytesPerRow, tmp.Data, false, true);
                    tmp.Data = null;
                }
                else
                {
                    long newStrideCalc = (long)BytesPerRow - _Border + value;
                    long newMaxRowOffsetCalc = ((long)Height * newStrideCalc) + value;

                    if (newMaxRowOffsetCalc > Array.MaxLength || newMaxRowOffsetCalc < 0)
                    {
                        DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(value), value,
                            $"Image dimensions result in an invalid {nameof(Data)} buffer size (less than zero or exceeding Array.MaxLength).");
                    }

                    // Architectural Note: This check is a dead code as long as Init()/Resize() 
                    // allocate an empty array (length 0) for Height=0. Because Data is never null after Init,
                    // execution always takes the (Data != null) branch above. The only way to reach here is 
                    // on a completely uninitialized struct, where newStrideCalc = value. Because value <= Array.MaxLength 
                    // (from the check above), it can never exceed int.MaxValue.
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

        public Bitmap(int height, int width, int border = Bitmap.BorderSize, bool uninitialized = false) : this()
        {
            Init(height, width, border, uninitialized);
        }

        public Bitmap(ref Bitmap bmp) : this()
        {
            if (Unsafe.IsNullRef(ref bmp))
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(bmp), $"{typeof(Bitmap).FullName} bmp reference is null.");
            }

            Init(ref bmp, bmp.Border);
        }

        public Bitmap(sbyte[] data, int height, int width, int border = Bitmap.BorderSize)
            : this()
        {
            Init(data, height, width, border);
        }

        /// <summary>
        /// Disposes the underlying pinned array resources.
        /// </summary>
        /// <remarks>
        /// <b>INTERMEDIATE ARCHITECTURE NOTE:</b><br/>
        /// This disposal pattern is an intermediate implementation. Due to the high allocation volume of <see cref="Bitmap"/> 
        /// instances (thousands per document, with highly variable buffer sizes), this method will be replaced by a centralized 
        /// memory management architecture utilizing custom pinned array pooling or unmanaged arena memory. Furthermore, 
        /// an RLE Compress/Decompress pattern of Data buffers memory (3 - 100 compression ratio) is in advanced implementation 
        /// stages to keep pinned buffers short-lived.
        /// </remarks>
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
            // go reading file
            if (magic[0] == 'P')
            {
                // PBM/PGM formats still require the pre-zeroed allocating constructor
                Bitmap bitmap = new Bitmap(height, width, border);
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
                        Bitmap bitmap = new Bitmap(height, width, border, uninitialized: true);
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
            byte* row = (byte*)(DataPointer + Border);
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ReadPgmTextStream(Stream stream, int maxval)
        {
            byte* row = (byte*)(DataPointer + Border);
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ReadPbmRawStream(Stream stream)
        {
            byte* row = (byte*)(DataPointer + Border);
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ReadPgmRawStream(Stream stream, int maxval)
        {
            int maxbin = (maxval > 255) ? 65536 : 256;
            byte[] ramp = new byte[maxbin];

            for (int i = 0; i < maxbin; i++)
            {
                ramp[i] = (byte)(i < maxval ? ((Grays - 1) * (maxval - i) + maxval / 2) / maxval : 0);
            }

            fixed (byte* bramp = ramp)
            {
                byte* row = (byte*)(DataPointer + Border);
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
                            Rle2Bitmap(Width, ref runs, runs_end, buf, false);
                            stream.Write(byteBuff, 0, count);
                        }
                    }
                }
            }
            else
            {
                if (Data == null)
                {
                    Decompress();
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
                Decompress();
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

        public Bitmap Duplicate()
        {
            if (IsDisposed)
            {
                DjvuExceptionUtil.ThrowObjectDisposed(typeof(Bitmap).FullName);
            }

            if (Height == 0 && Width == 0 && Border == 0 && Data == null && RleData == null)
                return default;                                                                                   

            Bitmap clone = new Bitmap();

            clone.Grays = Grays;

            clone.Resize(Width, Height, Border, BytesPerRow);

            if (Data != null && clone.Data != null)
            {
                Buffer.BlockCopy(Data, 0, clone.Data, 0, Data.Length);
            }
            else if (RleData != null)
            {
                if (clone.RleData == null)
                {
                    clone.RleData = new byte[RleData.Length];
                }

                Buffer.BlockCopy(RleData, 0, clone.RleData, 0, RleData.Length);
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

        /** Performs an additive blit of the GBitmap #bm# with anti-aliasing.  The
            GBitmap #bm# is first positioned above the current GBitmap in such a
            way that position (#u#,#v#) in GBitmap #bm# corresponds to position
            (#u#+#x#/#subsample#,#v#+#y#/#subsample#) in the current GBitmap.  This
            mapping results in a contraction of GBitmap #bm# by a factor
            #subsample#.  Each pixel of the current GBitmap can be covered by a
            maximum of #subsample^2# pixels of GBitmap #bm#.  The value of
            each pixel in GBitmap #bm# is then added to the value of the
            corresponding pixel in the current GBitmap.

            {\bf Example}: Assume for instance that the current GBitmap is initially
            white (all pixels have value zero).  Each pixel of the current GBitmap
            then contains the sum of the gray levels of the corresponding pixels in
            GBitmap #bm#.  There are up to #subsample*subsample# such pixels.  If
            for instance GBitmap #bm# is a bilevel image (pixels can be #0# or #1#),
            the pixels of the current GBitmap can take values in range #0# to
            #subsample*subsample#.  Note that function #blit# does not change the
            number of gray levels in the current GBitmap.  You must call
            \Ref{set_grays} to indicate that there are #subsample^2+1# gray
            levels.  Since there is at most 256 gray levels, this also means that
            #subsample# should never be greater than #15#.

            {\bf Remark}: Arguments #x# and #y# do not represent a position in the
            coordinate system of the current GBitmap.  According to the above
            discussion, the position is (#x/subsample#,#y/subsample#).  In other
            words, you can position the blit with a sub-pixel resolution.  The
            resulting anti-aliasing changes are paramount to the image quality. */
        // void blit(const GBitmap* shape, int x, int y, int subsample);

        /// <summary>
        /// Insert another bitmap at the specified location. Note that both bitmaps
        /// need to have the same number of grays.
        /// </summary>
        /// <param name="source">
        /// Bitmap to insert
        /// </param>
        /// <param name="xInsertPos">
        /// Horizontal location to insert at
        /// </param>
        /// <param name="yInsertPos">
        /// Vertical location to insert at
        /// </param>
        /// <param name="subSample">
        /// Subsample value at
        /// </param>
        /// <returns>
        /// True if the blit intersected this bitmap
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool Blit(ref Bitmap source, int xInsertPos, int yInsertPos, int subSample)
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

            if (subSample < 1 || subSample > 15)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(subSample), subSample, $"Subsample factor {subSample} is out of range. Factors > 15 or < 1 are not supported.");
            }

            if (subSample == 1)
            {
                return InsertMap(ref source, xInsertPos, yInsertPos, true);
            }

            if ((xInsertPos >= (Width * subSample)) || (yInsertPos >= (Height * subSample)) ||
                ((xInsertPos + source.Width) < 0) || ((yInsertPos + source.Height) < 0))
            {
                return false;
            }

            if (source.Data != null)
            {
                int startDestRow = yInsertPos / subSample;
                int subPixelRowOffset = yInsertPos - (subSample * startDestRow);

                if (subPixelRowOffset < 0)
                {
                    startDestRow--;
                    subPixelRowOffset += subSample;
                }

                int startDestColumn = xInsertPos / subSample;
                int subPixelColumnOffset = xInsertPos - (subSample * startDestColumn);

                if (subPixelColumnOffset < 0)
                {
                    startDestColumn--;
                    subPixelColumnOffset += subSample;
                }

                switch (subSample)
                {
                    case 2:
                        return BlitSubSampleDoubleWord<Factor2>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    case 3:
                        return BlitSubSample3<Factor3>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    case 4:
                        return BlitSubSampleDoubleWord<Factor4>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    case 5:
                        return BlitSubSampleQuadWord<Factor5>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    case 6:
                        return BlitSubSampleQuadWord<Factor6>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    case 7:
                        return BlitSubSampleQuadWord<Factor7>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    case 8:
                        return BlitSubSampleQuadWord<Factor8>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    case 9:
                        return BlitSubSample9<Factor9>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    case 10:
                        return BlitSubSample128Lane<Factor10>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    case 11:
                        return BlitSubSample128Lane<Factor11>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    case 12:
                        return BlitSubSample128Lane<Factor12>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    case 13:
                        return BlitSubSample128Lane<Factor13>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    case 14:
                        return BlitSubSample128Lane<Factor14>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    case 15:
                        return BlitSubSample128Lane<Factor15>(ref source, startDestRow, subPixelRowOffset, startDestColumn, subPixelColumnOffset);
                    default:
                        DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(subSample), subSample, $"Subsample factor {subSample} is out of range. Factors > 15 or < 1 are not supported.");
                        return false;


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
            uint bufferLength = (uint)(Width < 64 ? Width : 64);
            sbyte* buffer = stackalloc sbyte[(int)bufferLength];
            sbyte v = (sbyte)value;
            Unsafe.InitBlockUnaligned(buffer, (byte)v, bufferLength);

            for (int r = 0; r < Height; r++)
            {
                Span<sbyte> rowSpan = new Span<sbyte>(GetRow(r), Width);
                int offset = 0;
                while (offset < Width)
                {
                    int toCopy = Math.Min(64, Width - offset);
                    new ReadOnlySpan<sbyte>(buffer, toCopy).CopyTo(rowSpan.Slice(offset, toCopy));
                    offset += toCopy;
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
        public ref Bitmap Init(int height, int width, int border, bool uninitialized = false)
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

            if (_IsDisposed > 0)
            {
                DjvuExceptionUtil.ThrowObjectDisposed(typeof(Bitmap).FullName);
            }

            if (Data != null || _RleData != null)
            {
                DjvuExceptionUtil.ThrowInvalidOperation("Cannot initialize an already populated Bitmap.");
            }

            Data = null;
            Grays = 2;

            // The allocation logic matches C++ DjVuLibre GBitmap::init parity:
            // BytesPerRow represents single-sided row padding (Width + Border).
            // RowOffset(Height) calculates: (Height * BytesPerRow) + Border
            // which adds one final border cap at the very end of the contiguous memory buffer.

            // Potential integer overflow is guarded by Resize checks
            int bytesPerRow = width + border;
            Resize(width, height, border, bytesPerRow, uninitialized);

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

            if (_IsDisposed > 0)
            {
                DjvuExceptionUtil.ThrowObjectDisposed(typeof(Bitmap).FullName);
            }

            // TODO: When .NET provides an API to detect if an array is pinned (dotnet/runtime issue),
            // we can change this implementation to take direct ownership if `data` is already pinned,
            // avoiding the forced allocation and copy.
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
            if (IsDisposed)
            {
                DjvuExceptionUtil.ThrowObjectDisposed(typeof(Bitmap).FullName);
            }

            if (Unsafe.IsNullRef(ref source))
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(source), $"{typeof(Bitmap).FullName} source reference is null.");
            }

            // 3. Source State Validation
            if (source.IsDisposed)
            {
                DjvuExceptionUtil.ThrowArgument("The source Bitmap has been disposed.", nameof(source));
            }

            if (!Unsafe.AreSame(ref this, ref source))
            {
                int srcHeight = source.Height;
                int srcWidth = source.Width;

                if (srcWidth == 0 && srcHeight > 0 && border > 0)
                {
                    return ref Init(srcHeight, srcWidth, border);
                }

                if (border == source.Border)
                {
                    Init(srcHeight, srcWidth, border, uninitialized: true);
                    Grays = source.Grays;
                    int bufferLength = _MaxRowOffset;
                    if (bufferLength > 0)
                    {
                        sbyte* dst = DataPointer;
                        sbyte* src = source.DataPointer;

                        if (Vector512.IsHardwareAccelerated && bufferLength >= 64)
                        {
                            int bufferLengthMinus64 = bufferLength - 64;
                            for (int offset = 0; offset <= bufferLengthMinus64; offset += 64)
                            {
                                Vector512.Store(Vector512.Load(src + offset), dst + offset);
                            }
                            Vector512.Store(Vector512.Load(src + bufferLengthMinus64), dst + bufferLengthMinus64);
                        }
                        else if (Vector256.IsHardwareAccelerated && bufferLength >= 32)
                        {
                            int bufferLengthMinus32 = bufferLength - 32;
                            for (int offset = 0; offset <= bufferLengthMinus32; offset += 32)
                            {
                                Vector256.Store(Vector256.Load(src + offset), dst + offset);
                            }
                            Vector256.Store(Vector256.Load(src + bufferLengthMinus32), dst + bufferLengthMinus32);
                        }
                        else if (Vector128.IsHardwareAccelerated && bufferLength >= 16)
                        {
                            int bufferLengthMinus16 = bufferLength - 16;
                            for (int offset = 0; offset <= bufferLengthMinus16; offset += 16)
                            {
                                Vector128.Store(Vector128.Load(src + offset), dst + offset);
                            }
                            Vector128.Store(Vector128.Load(src + bufferLengthMinus16), dst + bufferLengthMinus16);
                        }
                        else
                        {
                            for (int offset = 0; offset < bufferLength; offset++)
                            {
                                dst[offset] = src[offset];
                            }
                        }
                    }
                }
                else
                {
                    Init(srcHeight, srcWidth, border, uninitialized: true);
                    Grays = source.Grays;
                    sbyte* dPtr = DataPointer;
                    int bufferLength = _MaxRowOffset;

                    if (border > 0)
                    {
                        if (Vector512.IsHardwareAccelerated && bufferLength >= 64)
                        {
                            var zero = Vector512<sbyte>.Zero;
                            int borderMinus64 = border - 64;
                            
                            for (int offset = 0; offset <= borderMinus64; offset += 64)
                            {
                                Vector512.Store(zero, dPtr + offset);
                            }
                            
                            int tailOffset = Math.Max(0, border - 64);
                            Vector512.Store(zero, dPtr + tailOffset);
                        }
                        else if (Vector256.IsHardwareAccelerated && bufferLength >= 32)
                        {
                            var zero = Vector256<sbyte>.Zero;
                            int borderMinus32 = border - 32;
                            
                            for (int offset = 0; offset <= borderMinus32; offset += 32)
                            {
                                Vector256.Store(zero, dPtr + offset);
                            }
                            
                            int tailOffset = Math.Max(0, border - 32);
                            Vector256.Store(zero, dPtr + tailOffset);
                        }
                        else if (Vector128.IsHardwareAccelerated && bufferLength >= 16)
                        {
                            var zero = Vector128<sbyte>.Zero;
                            int borderMinus16 = border - 16;
                            
                            for (int offset = 0; offset <= borderMinus16; offset += 16)
                            {
                                Vector128.Store(zero, dPtr + offset);
                            }
                            
                            int tailOffset = Math.Max(0, border - 16);
                            Vector128.Store(zero, dPtr + tailOffset);
                        }
                        else
                        {
                            for (int offset = 0; offset < border; offset++)
                            {
                                dPtr[offset] = 0;
                            }
                        }
                    }

                    if (srcWidth > 0 && srcHeight > 0)
                    {
                        sbyte* dstStart = dPtr + border;
                        sbyte* srcStart = source.DataPointer + source.Border;
                        int dstStride = srcWidth + border;
                        int srcStride = srcWidth + source.Border;

                        if (Vector512.IsHardwareAccelerated && srcWidth >= 64)
                        {
                            var zero = Vector512<sbyte>.Zero;
                            int widthMinus64 = srcWidth - 64;

                            if (border > 0)
                            {
                                int borderMinus64 = border - 64;
                                int tailOffset = srcWidth + border - 64;

                                for (int row = 0; row < srcHeight; row++)
                                {
                                    for (int offset = 0; offset <= widthMinus64; offset += 64)
                                    {
                                        Vector512.Store(Vector512.Load(srcStart + offset), dstStart + offset);
                                    }

                                    for (int offset = 0; offset <= borderMinus64; offset += 64)
                                    {
                                        Vector512.Store(zero, dstStart + srcWidth + offset);
                                    }

                                    Vector512.Store(zero, dstStart + tailOffset);
                                    Vector512.Store(Vector512.Load(srcStart + widthMinus64), dstStart + widthMinus64);

                                    dstStart += dstStride;
                                    srcStart += srcStride;
                                }
                            }
                            else
                            {
                                for (int row = 0; row < srcHeight; row++)
                                {
                                    for (int offset = 0; offset <= widthMinus64; offset += 64)
                                    {
                                        Vector512.Store(Vector512.Load(srcStart + offset), dstStart + offset);
                                    }
                                    
                                    Vector512.Store(Vector512.Load(srcStart + widthMinus64), dstStart + widthMinus64);

                                    dstStart += dstStride;
                                    srcStart += srcStride;
                                }
                            }
                        }
                        else if (Vector256.IsHardwareAccelerated && srcWidth >= 32)
                        {
                            var zero = Vector256<sbyte>.Zero;
                            int widthMinus32 = srcWidth - 32;

                            if (border > 0)
                            {
                                int borderMinus32 = border - 32;
                                int tailOffset = srcWidth + border - 32;

                                for (int row = 0; row < srcHeight; row++)
                                {
                                    for (int offset = 0; offset <= widthMinus32; offset += 32)
                                    {
                                        Vector256.Store(Vector256.Load(srcStart + offset), dstStart + offset);
                                    }

                                    for (int offset = 0; offset <= borderMinus32; offset += 32)
                                    {
                                        Vector256.Store(zero, dstStart + srcWidth + offset);
                                    }

                                    Vector256.Store(zero, dstStart + tailOffset);
                                    Vector256.Store(Vector256.Load(srcStart + widthMinus32), dstStart + widthMinus32);

                                    dstStart += dstStride;
                                    srcStart += srcStride;
                                }
                            }
                            else
                            {
                                for (int row = 0; row < srcHeight; row++)
                                {
                                    for (int offset = 0; offset <= widthMinus32; offset += 32)
                                    {
                                        Vector256.Store(Vector256.Load(srcStart + offset), dstStart + offset);
                                    }
                                    
                                    Vector256.Store(Vector256.Load(srcStart + widthMinus32), dstStart + widthMinus32);

                                    dstStart += dstStride;
                                    srcStart += srcStride;
                                }
                            }
                        }
                        else if (Vector128.IsHardwareAccelerated && srcWidth >= 16)
                        {
                            var zero = Vector128<sbyte>.Zero;
                            int widthMinus16 = srcWidth - 16;

                            if (border > 0)
                            {
                                int borderMinus16 = border - 16;
                                int tailOffset = srcWidth + border - 16;

                                for (int row = 0; row < srcHeight; row++)
                                {
                                    for (int offset = 0; offset <= widthMinus16; offset += 16)
                                    {
                                        Vector128.Store(Vector128.Load(srcStart + offset), dstStart + offset);
                                    }

                                    for (int offset = 0; offset <= borderMinus16; offset += 16)
                                    {
                                        Vector128.Store(zero, dstStart + srcWidth + offset);
                                    }

                                    Vector128.Store(zero, dstStart + tailOffset);
                                    Vector128.Store(Vector128.Load(srcStart + widthMinus16), dstStart + widthMinus16);

                                    dstStart += dstStride;
                                    srcStart += srcStride;
                                }
                            }
                            else
                            {
                                for (int row = 0; row < srcHeight; row++)
                                {
                                    for (int offset = 0; offset <= widthMinus16; offset += 16)
                                    {
                                        Vector128.Store(Vector128.Load(srcStart + offset), dstStart + offset);
                                    }
                                    
                                    Vector128.Store(Vector128.Load(srcStart + widthMinus16), dstStart + widthMinus16);

                                    dstStart += dstStride;
                                    srcStart += srcStride;
                                }
                            }
                        }
                        else
                        {
                            if (border > 0)
                            {
                                for (int row = 0; row < srcHeight; row++)
                                {
                                    for (int offset = 0; offset < srcWidth; offset++)
                                    {
                                        dstStart[offset] = srcStart[offset];
                                    }

                                    for (int offset = 0; offset < border; offset++)
                                    {
                                        dstStart[srcWidth + offset] = 0;
                                    }

                                    dstStart += dstStride;
                                    srcStart += srcStride;
                                }
                            }
                            else
                            {
                                for (int row = 0; row < srcHeight; row++)
                                {
                                    for (int offset = 0; offset < srcWidth; offset++)
                                    {
                                        dstStart[offset] = srcStart[offset];
                                    }

                                    dstStart += dstStride;
                                    srcStart += srcStride;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (border > Border)
                {
                    SetMinimumBorder(border);
                }
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
            if (IsDisposed)
            {
                DjvuExceptionUtil.ThrowObjectDisposed(typeof(Bitmap).FullName);
            }

            if (Unsafe.IsNullRef(ref source))
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(source), $"{typeof(Bitmap).FullName} source reference is null.");
            }

            // 3. Source State Validation
            if (source.IsDisposed)
            {
                DjvuExceptionUtil.ThrowArgument("The source Bitmap has been disposed.", nameof(source));
            }

            // Special Case 1: Target is Source (NO-OP check)
            if (Unsafe.AreSame(ref this, ref source))
            {
                if (rect.XMin == 0 && rect.YMin == 0 && rect.Width == Width && rect.Height == Height && border == Border)
                {
                    return ref this;
                }
            }

            // Special Case 2: Exact Full Copy
            if (rect.XMin == 0 && rect.YMin == 0 && rect.Width == source.Width && rect.Height == source.Height)
            {
                return ref Init(ref source, border);
            }

            // Execute a shallow struct copy on the stack. 
            // Because Bitmap is a value type, this creates a strict bitwise copy of its fields (pointers, dimensions) 
            // with zero heap allocation. This preserves the source state against mutation: if 'ref source' and 'ref this' 
            // point to the same instance, 'this.Init()' will irrevocably mutate the instance properties, but our stack 
            // copy safely preserves the original geometry required for read-pointer calculations. Crucially, this allows 
            // 'this.Init()' to re-use the underlying POH array, eliminating the transient double-allocation bug.
            Bitmap originalSource = source;

            // Map the target crop rectangle to the source's coordinate space.
            // Translate offsets the rectangle to a 0,0 origin to establish the valid intersection bounds.
            Rectangle rect2 = new Rectangle(0, 0, originalSource.Width, originalSource.Height);
            rect2.Intersect(rect2, rect);
            rect2.Translate(-rect.XMin, -rect.YMin);

            // Determine if the allocation should bypass CLR zero-initialization (fast path).
            // uninitialized is set to true strictly when the crop fully covers the target geometry and dimensions > 0.
            // If coverage is partial or dimensions are empty, we fall back to false to ensure the CLR zeroes the out-of-bounds padding.
            bool isFullCoverage = (rect2.Width == rect.Width && rect2.Height == rect.Height);
            bool uninitialized = !rect2.Empty && isFullCoverage;

            bool isSelfAliased = Unsafe.AreSame(ref this, ref source);
            int bytesPerRow = rect.Width + border;
            long requiredLength = ((long)rect.Height * bytesPerRow) + border;

            if (isSelfAliased || (Data != null && Data.Length < requiredLength))
            {
                // Force Resize to explicitly allocate a fresh pinned buffer. 
                Data = null;
            }

            Resize(rect.Width, rect.Height, border, bytesPerRow, uninitialized: uninitialized);
            Grays = originalSource.Grays;

            if (!rect2.Empty)
            {
                sbyte* targetDataPtr = DataPointer;
                int bufferSize = _MaxRowOffset;
                int w = rect2.Width;
                int h = rect2.Height;

                bool isContiguous = (w == originalSource.Width && w == Width && border == 0 && originalSource.Border == 0);

                if (isContiguous)
                {
                    // Fast path for fully contiguous memory layouts
                    sbyte* dstStart = GetRow(0);
                    sbyte* srcStart = originalSource.GetRow(rect.YMin) + rect.XMin;
                    Unsafe.CopyBlockUnaligned(dstStart, srcStart, (uint)(w * h));
                }
                else if (bufferSize < 16)
                {
                    if (uninitialized && border > 0)
                    {
                        for (int offset = 0; offset < border; offset++)
                        {
                            targetDataPtr[offset] = 0;
                        }
                    }

                    for (int y = rect2.YMin; y < rect2.YMax; y++)
                    {
                        sbyte* dstStart = GetRow(y) + rect2.XMin;
                        sbyte* srcStart = originalSource.GetRow(y + rect.YMin) + rect.XMin + rect2.XMin;

                        for (int offset = 0; offset < w; offset++)
                        {
                            dstStart[offset] = srcStart[offset];
                        }

                        if (uninitialized && border > 0)
                        {
                            sbyte* borderStart = dstStart + w;
                            for (int offset = 0; offset < border; offset++)
                            {
                                borderStart[offset] = 0;
                            }
                        }
                    }
                }
                else if (border == 0)
                {
                    // If no border padding is required
                    for (int y = rect2.YMin; y < rect2.YMax; y++)
                    {
                        sbyte* dstStart = GetRow(y) + rect2.XMin;
                        sbyte* srcStart = originalSource.GetRow(y + rect.YMin) + rect.XMin + rect2.XMin;
                        Unsafe.CopyBlockUnaligned(dstStart, srcStart, (uint)w);
                    }
                }
                else
                {
                    if (uninitialized)
                    {
                        if (Vector512.IsHardwareAccelerated && bufferSize >= 64)
                        {
                            var zero = Vector512<sbyte>.Zero;
                            int borderMinus64 = border - 64;
                            for (int offset = 0; offset <= borderMinus64; offset += 64)
                                Vector512.Store(zero, targetDataPtr + offset);
                            Vector512.Store(zero, targetDataPtr + Math.Max(0, border - 64));
                        }
                        else if (Vector256.IsHardwareAccelerated && bufferSize >= 32)
                        {
                            var zero = Vector256<sbyte>.Zero;
                            int borderMinus32 = border - 32;
                            for (int offset = 0; offset <= borderMinus32; offset += 32)
                                Vector256.Store(zero, targetDataPtr + offset);
                            Vector256.Store(zero, targetDataPtr + Math.Max(0, border - 32));
                        }
                        // Condition guaranteed by previous bufferSize >= 16 check
                        else if (Vector128.IsHardwareAccelerated && bufferSize >= 16)
                        {
                            var zero = Vector128<sbyte>.Zero;
                            int borderMinus16 = border - 16;
                            for (int offset = 0; offset <= borderMinus16; offset += 16)
                                Vector128.Store(zero, targetDataPtr + offset);
                            Vector128.Store(zero, targetDataPtr + Math.Max(0, border - 16));
                        }
                    }

                    // Vector-accelerated block copy loops.
                    if (Vector512.IsHardwareAccelerated && w >= 64)
                    {
                        var zero = Vector512<sbyte>.Zero;
                        int widthMinus64 = w - 64;
                        int borderMinus64 = border - 64;
                        int tailOffset = w + border - 64;

                        for (int y = rect2.YMin; y < rect2.YMax; y++)
                        {
                            sbyte* dstStart = GetRow(y) + rect2.XMin;
                            sbyte* srcStart = originalSource.GetRow(y + rect.YMin) + rect.XMin + rect2.XMin;

                            for (int offset = 0; offset <= widthMinus64; offset += 64)
                                Vector512.Store(Vector512.Load(srcStart + offset), dstStart + offset);

                            for (int offset = 0; offset <= borderMinus64; offset += 64)
                                Vector512.Store(zero, dstStart + w + offset);

                            Vector512.Store(zero, dstStart + tailOffset);
                            Vector512.Store(Vector512.Load(srcStart + widthMinus64), dstStart + widthMinus64);
                        }
                    }
                    else if (Vector256.IsHardwareAccelerated && w >= 32)
                    {
                        var zero = Vector256<sbyte>.Zero;
                        int widthMinus32 = w - 32;
                        int borderMinus32 = border - 32;
                        int tailOffset = w + border - 32;

                        for (int y = rect2.YMin; y < rect2.YMax; y++)
                        {
                            sbyte* dstStart = GetRow(y) + rect2.XMin;
                            sbyte* srcStart = originalSource.GetRow(y + rect.YMin) + rect.XMin + rect2.XMin;

                            for (int offset = 0; offset <= widthMinus32; offset += 32)
                                Vector256.Store(Vector256.Load(srcStart + offset), dstStart + offset);

                            for (int offset = 0; offset <= borderMinus32; offset += 32)
                                Vector256.Store(zero, dstStart + w + offset);

                            Vector256.Store(zero, dstStart + tailOffset);
                            Vector256.Store(Vector256.Load(srcStart + widthMinus32), dstStart + widthMinus32);
                        }
                    }
                    else if (Vector128.IsHardwareAccelerated && w >= 16)
                    {
                        var zero = Vector128<sbyte>.Zero;
                        int widthMinus16 = w - 16;
                        int borderMinus16 = border - 16;
                        int tailOffset = w + border - 16;

                        for (int y = rect2.YMin; y < rect2.YMax; y++)
                        {
                            sbyte* dstStart = GetRow(y) + rect2.XMin;
                            sbyte* srcStart = originalSource.GetRow(y + rect.YMin) + rect.XMin + rect2.XMin;

                            for (int offset = 0; offset <= widthMinus16; offset += 16)
                                Vector128.Store(Vector128.Load(srcStart + offset), dstStart + offset);

                            for (int offset = 0; offset <= borderMinus16; offset += 16)
                                Vector128.Store(zero, dstStart + w + offset);

                            Vector128.Store(zero, dstStart + tailOffset);
                            Vector128.Store(Vector128.Load(srcStart + widthMinus16), dstStart + widthMinus16);
                        }
                    }
                    else
                    {
                        for (int y = rect2.YMin; y < rect2.YMax; y++)
                        {
                            sbyte* dstStart = GetRow(y) + rect2.XMin;
                            sbyte* srcStart = originalSource.GetRow(y + rect.YMin) + rect.XMin + rect2.XMin;

                            if (w < 16)
                            {
                                for (int offset = 0; offset < w; offset++)
                                {
                                    dstStart[offset] = srcStart[offset];
                                }
                            }
                            else
                            {
                                Unsafe.CopyBlockUnaligned(dstStart, srcStart, (uint)w);
                            }

                            if (uninitialized)
                            {
                                sbyte* borderStart = dstStart + w;
                                for (int offset = 0; offset < border; offset++)
                                {
                                    borderStart[offset] = 0;
                                }
                            }
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
            if (Unsafe.IsNullRef(ref retVal))
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(retVal), $"{typeof(Bitmap).FullName} retVal reference is null.");
            }

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
        /// Find the bounding box for non-white (non-zero) pixels.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Architectural Divergence (C++ Quirk Preservation):</b>
        /// This method deliberately replicates a geometric quirk from the reference C++ DjVuLibre implementation.
        /// The bounds (<c>xmin, ymin, xmax, ymax</c>) are calculated as <i>inclusive</i> coordinates of the furthest non-zero pixels. 
        /// Because the <see cref="Rectangle"/> struct calculates width and height <i>exclusively</i> (e.g., <c>Width = xmax - xmin</c>), 
        /// a bounding box for a fully populated 10x10 image will yield a Width and Height of 9.
        /// </para>
        /// <para>
        /// Furthermore, if the image contains exactly one non-zero pixel (e.g., at <c>x=3, y=5</c>), the inclusive coordinates 
        /// will be <c>xmin=3, xmax=3, ymin=5, ymax=5</c>. This mathematically collapses the rectangle's area to 0, 
        /// marking it explicitly as <c>Empty</c>. This single-pixel annihilation is expected behavior to maintain parser parity.
        /// </para>
        /// </remarks>
        /// <returns>
        /// Bounding rectangle
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public Rectangle ComputeBoundingBox()
        {
            if (Data == null && RleData == null && Width > 0 && Height > 0)
            {
                // This state should be statically unreachable due to encapsulation safeguards.
                // If it throws, it means an initialization check was bypassed or memory was externally corrupted.
                DjvuExceptionUtil.ThrowNullReference($"Cannot compute bounding box: the underlying {nameof(Data)} and {nameof(RleData)} buffers are null. Width: {Width}, Height: {Height}");
            }

            // Short circuit no op optimization
            if (Data == null && RleData != null)
            {
                if (Width > 0 && Height > 0)
                {
                    Decompress();
                }
                else
                {
                    return new Rectangle();
                }
            }

            int w = Width;
            int h = Height;
            
            int ymin = 0, ymax = h - 1;
            int xmin = w, xmax = 0;

            // Find ymin (Top-down)
            for (; ymin < h; ymin++)
            {
                if (new ReadOnlySpan<byte>(GetRow(ymin), w).IndexOfAnyExcept((byte)0) != -1)
                {
                    break;
                }
            }

            // If the image is completely empty, mathematically enforce empty rectangle 
            // by setting bounds that trigger the Rectangle's empty flag logic
            if (ymin == h)
            {
                return new Rectangle();
            }

            // Find ymax (Bottom-up)
            for (; ymax >= ymin; ymax--)
            {
                if (new ReadOnlySpan<byte>(GetRow(ymax), w).IndexOfAnyExcept((byte)0) != -1)
                {
                    break;
                }
            }

            // Find xmin and xmax horizontally (Cache-friendly & Vectorized)
            for (int y = ymin; y <= ymax; y++)
            {
                ReadOnlySpan<byte> rowSpan = new ReadOnlySpan<byte>(GetRow(y), w);
                
                int first = rowSpan.IndexOfAnyExcept((byte)0);
                if (first != -1)
                {
                    if (first < xmin) xmin = first;
                    
                    int last = rowSpan.LastIndexOfAnyExcept((byte)0);
                    if (last > xmax) xmax = last;
                }
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
                        DecodeRleCore(rle, _RleData.Length, buffer, Border, Height, BytesPerRow, Width);
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

            // Using compressed Bitmap.RleData for equality comparison seems to be much more efficient than specially decompressing and allocating Data buffer which most probably will be from 10 to 20 times larger than compressed bitonal RLE image
            if (bmp1.Data == null && bmp2.Data == null)
            {
                if (ReferenceEquals(bmp1.RleData, bmp2.RleData))
                    return true;

                if (bmp1.RleData == null || bmp2.RleData == null || bmp1.RleData.Length != bmp2.RleData.Length)
                    return false;

                return new ReadOnlySpan<byte>(bmp1.RleData).SequenceEqual(new ReadOnlySpan<byte>(bmp2.RleData));
            }

            sbyte[] rented1 = null;
            sbyte[] rented2 = null;

            try
            {
                sbyte[] data1 = bmp1.Data;
                int npixels1 = bmp1.Data != null ? bmp1.Data.Length : bmp1.Height * bmp1.BytesPerRow + bmp1.Border;
                if (data1 == null && bmp1.RleData != null)
                {
                    rented1 = ArrayPool<sbyte>.Shared.Rent((int)npixels1);
                    Array.Clear(rented1, 0, npixels1);
                    fixed (byte* rle = bmp1.RleData)
                    {
                        DecodeRleCore(rle, bmp1.RleData.Length, rented1, bmp1.Border, bmp1.Height, bmp1.BytesPerRow, bmp1.Width);
                    }
                    data1 = rented1;
                }

                sbyte[] data2 = bmp2.Data;
                int npixels2 = bmp2.Data != null ? bmp2.Data.Length : bmp2.Height * bmp2.BytesPerRow + bmp2.Border;
                if (data2 == null && bmp2.RleData != null)
                {
                    rented2 = ArrayPool<sbyte>.Shared.Rent((int)npixels2);
                    Array.Clear(rented2, 0, npixels2);
                    fixed (byte* rle = bmp2.RleData)
                    {
                        DecodeRleCore(rle, bmp2.RleData.Length, rented2, bmp2.Border, bmp2.Height, bmp2.BytesPerRow, bmp2.Width);
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
