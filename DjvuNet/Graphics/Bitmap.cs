using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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
    /// </remarks>
    public class Bitmap : Map, IBitmap
    {
        // TODO Verify if this change does not break rendering

        // As this is read and assigned from instance methods changing 
        // to instance field - will verify results but perhaps it is
        // one of the bugs which prevents proper image rendering
        private Object[] RampRefArray = new Object[256];

        private const int RunOverflow = 0xc0;
        private const int MaxRunSize = 0x3fff;
        private const int RunMsbMask = 0x3f;
        private const int RunLsbMask = 0xff;

        #region Private Members

        /// <summary>end of the buffer  </summary>
        private int _MaxRowOffset;

        private Pixel[] _RampData;

        private object _SyncObject = new object();

        internal byte[] _RleData;

        #endregion Private Members

        #region Properties

        /// <summary>
        /// Gets or sets the number of pixel rows.
        /// </summary>
        internal int Rows
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value != Height)
                {
                    Height = value;
                    _MaxRowOffset = RowOffset(Height);
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Height; }
        }

        #region Grays

        private int _Grays;

        /// <summary>
        /// Gets or sets the depth of colors - indirectly influnces 
        /// effectively used pixel size expressed in bits
        /// </summary>
        public int Grays
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _Grays; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_Grays != value)
                {
                    if ((value < 2) || (value > 256))
                        throw new DjvuArgumentOutOfRangeException(nameof(value),
                            "Gray levels outside of range");

                    _Grays = value;
                    _RampData = null;
                }
            }
        }

        #endregion Grays

        #region Border

        private int _Border;

        /// <summary>
        /// Gets or sets the number of border pixels
        /// </summary>
        public int Border
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _Border; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_Border != value)
                {
                    _Border = value;
                    _MaxRowOffset = RowOffset(Height);
                }
            }
        }

        #endregion Border
        
        public Pixel[] Ramp
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_RampData != null)
                    return _RampData;
                else
                    return RampNullGrays();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Pixel[] RampNullGrays()
        {
            Pixel[] retval = (Pixel[])RampRefArray[Grays];
            if (retval != null)
                return _RampData = retval;
            else 
                return _RampData = RampNullRefArrayGreys(Grays);
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

        private int _BytesPerRow;

        /// <summary>
        /// Gets or sets the number of bytes per row
        /// </summary>
        public int BytesPerRow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _BytesPerRow; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal set
            {
                if (_BytesPerRow != value)
                {
                    _BytesPerRow = value;
                    _MaxRowOffset = RowOffset(Height);
                }
            }
        }

        #endregion BytesPerRow

        /// <summary>
        /// Set the minimum border needed
        /// </summary>
        public int MinimumBorder
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_Border < value)
                {
                    if (Data != null)
                    {
                        IBitmap tmp = new Bitmap().Init(this, value);
                        BytesPerRow = tmp.GetRowSize();
                        Data = tmp.Data;
                        tmp.Data = null;
                    }

                    _Border = value;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitmap(sbyte[] data, int height, int width, int border = 0)
            : base(1, 0, 0, 0, true)
        {
            Init(data, height, width, border);
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Method creates bitmap and initializes it with deserialized data read from supplied Stream. 
        /// </summary>
        /// <param name="stream">Stream with serialized data source.</param>
        /// <param name="border">Size of border surrounding bitmap data from all sides.</param>
        /// <returns>Bitmap initialized with data read from stream.</returns>
        public static Bitmap CreateBitmap(Stream stream, int border = 0)
        {
            // TODO create multithreaded synchronization for accessing Bitmap data;

            // Get magic number
            byte[] magic = new byte[2];
            magic[0] = magic[1] = 0;
            stream.Read(magic, 0, magic.Length);

            char lookahead = '\n';
            int width = (int)ReadInteger(ref lookahead, stream);
            int height = (int)ReadInteger(ref lookahead, stream);
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
                        maxval = (int)ReadInteger(ref lookahead, stream);
                        if (maxval > 65535)
                            throw new DjvuFormatException("Cannot read PGM formatted data with depth greater than 16 bits.");
                        bitmap.Grays = (maxval > 255 ? 256 : maxval + 1);
                        bitmap.ReadPgmTextStream(stream, maxval);
                        return bitmap;

                    case (byte)'4':
                        bitmap.Grays = 2;
                        bitmap.ReadPbmRawStream(stream);
                        return bitmap;

                    case (byte)'5':
                        maxval = (int)ReadInteger(ref lookahead, stream);
                        if (maxval > 65535)
                            throw new DjvuFormatException("Cannot read PGM formatted data with depth greater than 16 bits.");
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

            throw new DjvuFormatException("Data format error.");
        }

        internal static uint ReadInteger(ref char @char, Stream stream)
        {
            uint xinteger = 0;

            while (@char == ' ' || @char == '\t' || @char == '\r' || @char == '\n' || @char == '#')
            {
                if (@char == '#')
                {
                    do
                    {
                        @char = (char)stream.ReadByte();
                    }
                    while (@char != '\n' && @char != '\r');
                }
                @char = (char)0;
                @char = (char)stream.ReadByte();
            }

            if (@char < '0' || @char > '9')
                throw new DjvuFormatException($"Expected integer value. Actual value: {@char}");

            while (@char >= '0' && @char <= '9')
            {
                xinteger = xinteger * 10 + @char - '0';
                @char = (char)0;
                @char = (char)stream.ReadByte();
            }

            return xinteger;
        }

        public unsafe void ReadPbmTextStream(Stream stream)
        {
            GCHandle hData = GCHandle.Alloc(Data, GCHandleType.Pinned);
            IntPtr dataPtr = hData.AddrOfPinnedObject();
            try
            {
                byte* row = (byte*)(dataPtr + Border);
                row += (Rows - 1) * BytesPerRow;
                for (int n = Rows - 1; n >= 0; n--)
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
                                throw new DjvuEndOfStreamException(
                                    $"End of stream reached. Stream: {nameof(stream)}, Position: {stream.Position}");
                            bit = (byte)bitInt;
                        }

                        if (bit == '1')
                            row[c] = 1;
                        else if (bit == '0')
                            row[c] = 0;
                        else
                            throw new DjvuFormatException("Corrupted PBM data.");
                    }
                    row -= BytesPerRow;
                }
            }
            finally
            {
                if (hData.IsAllocated)
                    hData.Free();
            }
        }

        public unsafe void ReadPgmTextStream(Stream stream, int maxval)
        {
            GCHandle hData = GCHandle.Alloc(Data, GCHandleType.Pinned);
            IntPtr dataPtr = hData.AddrOfPinnedObject();
            try
            {
                byte* row = (byte*)(dataPtr + Border);
                row += (Rows - 1) * BytesPerRow;
                char lookahead = '\n';

                byte[] ramp = new byte[maxval + 1];

                for (int i = 0; i <= maxval; i++)
                    ramp[i] = (byte)(i < maxval ? ((Grays - 1) * (maxval - i) + maxval / 2) / maxval : 0);

                for (int n = Rows - 1; n >= 0; n--)
                {
                    for (int c = 0; c < Width; c++)
                        row[c] = ramp[(int)ReadInteger(ref lookahead, stream)];

                    row -= BytesPerRow;
                }
            }
            finally
            {
                if (hData.IsAllocated)
                    hData.Free();
            }
        }

        public unsafe void ReadPbmRawStream(Stream stream)
        {
            GCHandle hData = GCHandle.Alloc(Data, GCHandleType.Pinned);
            IntPtr dataPtr = hData.AddrOfPinnedObject();
            try
            {
                byte* row = (byte*)(dataPtr + Border);
                row += (Rows - 1) * BytesPerRow;
                for (int n = Rows - 1; n >= 0; n--)
                {
                    byte acc = 0;
                    byte mask = 0;
                    for (int c = 0; c < Width; c++)
                    {
                        if (mask == 0)
                        {
                            int accInt = stream.ReadByte();
                            if (accInt == -1)
                                throw new DjvuEndOfStreamException("Unexpected and of stream.");

                            acc = (byte)accInt;
                            mask = (byte)0x80;
                        }
                        if ((acc & mask) != 0)
                            row[c] = 1;
                        else
                            row[c] = 0;
                        mask >>= 1;
                    }
                    row -= BytesPerRow;
                }
            }
            finally
            {
                if (hData.IsAllocated)
                    hData.Free();
            }
        }

        public unsafe void ReadPgmRawStream(Stream stream, int maxval)
        {
            int maxbin = (maxval > 255) ? 65536 : 256;
            byte[] ramp = new byte[maxbin];

            for (int i = 0; i < maxbin; i++)
                ramp[i] = (byte)(i < maxval ? ((Grays - 1) * (maxval - i) + maxval / 2) / maxval : 0);

            GCHandle hData = GCHandle.Alloc(Data, GCHandleType.Pinned);
            IntPtr dataPtr = hData.AddrOfPinnedObject();

            GCHandle hRamp = GCHandle.Alloc(Data, GCHandleType.Pinned);
            IntPtr rampPtr = hData.AddrOfPinnedObject();
            try
            {
                byte* bramp = (byte*)rampPtr;
                byte* row = (byte*)(dataPtr + Border);
                row += (Rows - 1) * BytesPerRow;
                for (int n = Rows - 1; n >= 0; n--)
                {
                    if (maxbin > 256)
                    {
                        for (int c = 0; c < Width; c++)
                        {
                            byte[] x = new byte[2];
                            stream.Read(x, 0, 2);
                            row[c] = bramp[x[0] * 256 + x[1]];
                        }
                    }
                    else
                    {
                        for (int c = 0; c < Width; c++)
                        {
                            int xInt = stream.ReadByte();
                            if (xInt == -1)
                                throw new DjvuEndOfStreamException("Unexpected and of stream.");

                            row[c] = bramp[xInt];
                        }
                    }
                    row -= BytesPerRow;
                }
            }
            finally
            {
                if (hData.IsAllocated)
                    hData.Free();
                if (hRamp.IsAllocated)
                    hRamp.Free();
            }
        }

        public unsafe void ReadRleStream(Stream stream)
        {
            GCHandle hData = GCHandle.Alloc(Data, GCHandleType.Pinned);
            IntPtr dataPtr = hData.AddrOfPinnedObject();
            try
            {
                // interpret runs data
                int hInt = 0;
                byte p = 0;
                byte* row = (byte*)(dataPtr + Border);
                int n = Rows - 1;
                row += n * BytesPerRow;
                int c = 0;

                while (n >= 0)
                {
                    hInt = stream.ReadByte();
                    if (hInt == -1)
                        throw new DjvuEndOfStreamException("Unexpected and of stream.");

                    int x = hInt;
                    if (x >= RunOverflow)
                    {
                        hInt = stream.ReadByte();
                        if (hInt == -1)
                            throw new DjvuEndOfStreamException("Unexpected and of stream.");

                        x = hInt + ((x - RunOverflow) << 8);
                    }

                    if (c + x > Width)
                        throw new DjvuFormatException("Bitmap RLE format data are not in sync");

                    while (x-- > 0)
                        row[c++] = p;

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
                    hData.Free();
            }
        }

        /// <summary>
        /// Method serializes Bitmap data to PBM raw or text format depending on value of raw parameter.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="raw">
        /// True to serilize to raw PBM format, false to serialize to text PBM format. Default value is true.
        /// </param>
        public unsafe void SerializeToPbm(Stream stream, bool raw = true)
        {
            // check arguments
            if (Grays > 2)
                throw new DjvuFormatException(
                    $"Only bi-level bitmaps can be saved in PBM format. Grays: {Grays}");

            //GMonitorLock lock (monitor()) ;
            // header
            string header = $"P{(raw ? '4' : '1')}\n{Width} {Height}\n";
            byte[] buffer = new UTF8Encoding(false).GetBytes(header);
            stream.Write(buffer, 0, buffer.Length);

            // body
            if (raw)
            {
                if (_RleData != null)
                    Compress();
                fixed (byte* runs = _RleData)
                {
                    byte* runs_end = runs + _RleData.Length;
                    int count = (Width + 7) >> 3;
                    byte[] byteBuff = new byte[count];
                    fixed (byte* buf = byteBuff)
                    {
                        while (runs < runs_end)
                        {
                            Rle2Bitmap(Width, runs, buf, false);
                            stream.Write(byteBuff, 0, count);
                        }
                    }
                }
            }
            else
            {
                if (Data == null)
                    Uncompress();

                fixed (sbyte* rowStart = Data)
                {

                    byte* row = (byte*) rowStart + Border;
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
                                stream.WriteByte(eol);
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
        /// True to serilize to raw PGM format, false to serialize to text PGM format. Default value is true.
        /// </param>
        public unsafe void SerializeToPgm(Stream stream, bool raw = true)
        {
            // checks
            //GMonitorLock lock (monitor()) ;
            if (Data == null)
                Uncompress();

            // header
            string head = $"P{(raw ? '5' : '2')}\n{Width} {Height}\n{Grays - 1}\n";
            Encoding utf8 = new UTF8Encoding(false);
            byte[] buffer = utf8.GetBytes(head);
            stream.Write(buffer, 0, buffer.Length);

            // body
            fixed (sbyte* bytes = Data)
            {
                byte* row = (byte*) bytes + Border;
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
                                stream.WriteByte(eol);
                        }
                    }
                    row -= BytesPerRow;
                    n -= 1;
                }
            }
        }

        /// <summary>
        /// Method serializes Bitmap data using Run Length Encoding compression to RLE format.
        /// </summary>
        /// <param name="stream"></param>
        public unsafe void SerializeToRle(Stream stream)
        {
            // checks
            if (Width == 0 || Height == 0)
                throw new DjvuInvalidOperationException("Bitmap is not properly initialized.");

            //GMonitorLock lock (monitor()) ;
            if (Grays > 2)
                throw new DjvuInvalidOperationException(
                    $"Only bi-level bitmaps can be saved in PBM format. Grays: {Grays}");

            // header
            string head= $"R4\n{Width} {Height}\n";
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
                    stream.Write(gruns, 0, gruns.Length);
            }
        }


        internal unsafe void Compress()
        {
            if (Grays > 2)
                throw new DjvuInvalidOperationException($"Cannot compress data with Grays: {Grays}");

            //GMonitorLock lock (monitor()) ;
            if (Data != null)
            {
                byte[] grle;
                long rleLength = RleEncode(out grle);
                if (rleLength > 0)
                    Data = null;
            }
        }

        
        internal unsafe void Uncompress()
        {
            // GMonitorLock lock (monitor()) ;
            if (Data == null && _RleData != null)
            {
                fixed (byte* rle = _RleData)
                    RleDecode(rle);
            }
        }

        internal unsafe long RleEncode(out byte[] gpruns)
        {
            gpruns = null;

            // uncompress rle information
            if (Height == 0 || Width == 0)
                return 0;

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
            fixed (byte* runs = runsBuff)
            {
                byte* row = (byte*) bytes + Border;
                int n = Height - 1;
                row += n * BytesPerRow;
                while (n >= 0)
                {
                    if (maxpos < (pos + 2) + (2 * Width))
                    {
                        maxpos += (1024 + 2 * Width);
                        Array.Resize(ref runsBuff, maxpos);
                    }

                    byte* runs_pos = runs + pos;
                    byte* runs_pos_start = runs_pos;

                    AppendLine(runs_pos, row, Width);

                    pos += (runs_pos - runs_pos_start);
                    row -= BytesPerRow;
                    n -= 1;
                }
            }
            // return result
            Array.Resize(ref runsBuff, (int)pos);
            gpruns = runsBuff;
            return pos;
        }

        internal unsafe void RleDecode(byte* runs)
        {
            // initialize pixel array
            if (Width == 0 || Height == 0)
                throw new DjvuInvalidOperationException("Bitmap is not properly initialized.");

            BytesPerRow = Width + Border;
            if (runs == (byte*) 0)
                throw new DjvuArgumentNullException(nameof(runs));

            long npixels = Height * BytesPerRow + Border;

            if (Data == null)
                Data = new sbyte[npixels];

            // interpret runs data
            int c, n;
            byte p = 0;

            fixed (sbyte* pData = Data)
            {
                byte* row = (byte*)pData + Border;
                n = Height - 1;
                row += n * BytesPerRow;
                c = 0;
                while (n >= 0)
                {
                    int x = ReadRun(runs);

                    if (c + x > Width)
                        throw new DjvuFormatException("Invalid RLE encoded data.");

                    while (x-- > 0)
                        row[c++] = p;

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

            _RleData = null;
        }

        internal unsafe void AppendLine(byte* data, byte* row, int rowLength, bool invert = false)
        {
            byte* rowEnd = row + rowLength;
            bool p = !invert;
            while(row < rowEnd)
            {
                int count = 0;
                if ((p = !p)) 
                {
                    if (*row != 0)
                    {
                        for (++count, ++row; (row < rowEnd) && *row != 0; ++count, ++row) ;
                    }
                } 
                else if(*row == 0)
                {
                    for(++count, ++row; (row < rowEnd) && *row == 0; ++count, ++row);
                }
                AppendRun(data, count);
            }
        }

        internal unsafe void AppendRun(byte* data, int count)
        {
            if (count < RunOverflow)
            {
                data[0] = (byte) count;
                data += 1;
            }
            else if (count <= MaxRunSize)
            {
                data[0] = (byte) ((count >> 8) + RunOverflow);
                data[1] = (byte) (count & 0xff);
                data += 2;
            }
            else
            {
                AppendLongRun(data, count);
            }
        }

        internal unsafe void AppendLongRun(byte* data, int count)
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
                data[0] = (byte) count;
                data += 1;
            }
            else
            {
                data[0] = (byte)((count >> 8) + RunOverflow);
                data[1] = (byte)(count & 0xff);
                data += 2;
            }
        }

        internal unsafe int ReadRun(byte* data)
        {
            int z = *data++;
            return (z >= RunOverflow) ? ((z & ~RunOverflow) << 8) | (*data++) : z;
        }

        internal unsafe void Rle2Bitmap(int width, byte* runs, byte* bitmap, bool invert = false)
        {
            int obyte_def = invert ? 0xff : 0;
            int obyte_ndef = invert ? 0 : 0xff;
            int mask = 0x80, obyte = 0;

            for(int c = width; c > 0 ;)
            {
                int x = ReadRun(runs);
                c -= x;

                while((x--) > 0)
                {
                    if((mask >>= 1) == 0)
                    {
                        *(bitmap++) = (byte) (obyte ^ obyte_def);
                        obyte = 0;
                        mask=0x80;

                        for(; x >= 8; x -= 8)
                        {
                          *(bitmap++)= (byte) obyte_def;
                        }
                    }
                }

                if(c > 0)
                {
                    x = ReadRun(runs);
                    c -= x;
                    while((x--) > 0)
                    {
                        obyte |= mask;
                        if((mask >>= 1) == 0)
                        {
                            *(bitmap++) = (byte) (obyte ^ obyte_def);
                            obyte = 0;
                            mask = 0x80;

                            for(; x > 8 ; x -= 8)
                                *(bitmap++) = (byte) obyte_ndef;
                        }
                    }
                }
            }

            if(mask != 0x80)
                *(bitmap++) = (byte)(obyte ^ obyte_def);
        }

        public IBitmap Duplicate()
        {
            return new Bitmap
            {
                BlueOffset = BlueOffset,
                Border = Border,
                Data = Data,
                Grays = Grays,
                GreenOffset = GreenOffset,
                _MaxRowOffset = _MaxRowOffset,
                BytesPerPixel = BytesPerPixel,
                Width = Width,
                IsRampNeeded = IsRampNeeded,
                Height = Height,
                Properties = Properties,
                _RampData = _RampData,
                RedOffset = RedOffset,
                BytesPerRow = BytesPerRow
            };
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetBooleanAt(int offset)
        {
            return (offset < Border) || (offset >= _MaxRowOffset) || (Data[offset] == 0);
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
            if ((offset >= Border) || (offset < _MaxRowOffset))
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
                return ((offset < Border) || (offset >= _MaxRowOffset)) ? 0 : (0xff & dataLocation[offset]);
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

            if ((xh >= (Width * subsample)) || (yh >= (Height * subsample)) || ((xh + bm.Width) < 0) ||
                ((yh + bm.Height) < 0))
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

                for (; sr < bm.Height; sr++)
                {
                    if ((dr >= 0) && (dr < Height))
                    {
                        int dc = zdc;
                        int dc1 = zdc1;
                        qidx = bm.RowOffset(sr);
                        pidx = RowOffset(dr);

                        for (int sc = 0; sc < bm.Width; sc++)
                        {
                            if ((dc >= 0) && (dc < Width))
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
        /// Binarizes a gray level image using a threshold. The number of gray
        /// levels is reduced to #2# as in a bilevel image. All pixels whose value
        /// was strictly greater than threshold are set to black. All other pixels
        /// are set to white.
        /// </summary>
        /// <param name="threshold"></param>
        public void BinarizeGrays(int threshold)
        {
            throw new NotImplementedException();
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
                throw new DjvuArgumentOutOfRangeException(nameof(grays));

            throw new NotImplementedException();
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
            for (int y = 0; y < Height; y++)
            {
                idx = RowOffset(y);

                for (int x = 0; x < Width; x++)
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
            int w0 = Width - x0;
            int w1 = bit.Width - x1;
            int w = (w0 < w1) ? w0 : w1;
            int h0 = Height - y0;
            int h1 = bit.Height - y1;
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
            Width = width;
            Border = border;
            BytesPerRow = (Width + Border);   
            // TODO: Verify if value of Bitmap.Border is double sided or single sided?

            int npixels = RowOffset(Height);

            if (npixels > 0)
                Data = new sbyte[npixels];

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBitmap Init(sbyte[] data, int height, int width, int border)
        {
            Data = null;
            Grays = 2;
            Rows = height;
            Width = width;
            Border = border;
            BytesPerRow = (Width + Border);

            int npixels = RowOffset(Height);

            if (npixels > 0 && data != null && data.Length == npixels)
                Data = data;
            else
                throw new DjvuArgumentException(
                    "Mismatch in data size and Bitmap dimensions.", nameof(data));

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
                Init(source.Height, source.Width, border);
                Grays = source.Grays;

                for (int i = 0; i < Height; i++)
                {
                    for (int j = Width, k = RowOffset(i), kr = source.RowOffset(i); j-- > 0; )
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
                tmp.Width = Width;
                tmp.Rows = Height;
                tmp.Data = Data;
                Data = null;
                Init(tmp, rect, border);
            }
            else
            {
                Init(rect.Height, rect.Width, border);
                Grays = source.Grays;

                Rectangle rect2 = new Rectangle(0, 0, source.Width, source.Height);
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
            if (!(retval is Bitmap) || (retval.Width != Width) || (retval.Height != Height))
            {
                IBitmap r = new Bitmap().Init(Height, Width, 0);

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
            lock (_SyncObject)
            {
                int w = Width;
                int h = Height;
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