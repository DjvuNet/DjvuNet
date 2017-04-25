using System;
using System.IO;
using System.Runtime.CompilerServices;
using DjvuNet.Configuration;

namespace DjvuNet.Compression
{
    /// <summary> 
    /// This class decodes a bzz encoded InputStream.
    /// </summary>
    public sealed class BSInputStream : MemoryStream
    {
        #region Private Members

        /// <summary>
        /// Minimum block size
        /// </summary>
        private const int MINBLOCK = 10;

        /// <summary>
        /// Maximum block size
        /// </summary>
        private const int MAXBLOCK = 4096;

        /// <summary>
        /// Sorting threshold
        /// </summary>
        private const int FREQMAX = 4;

        /// <summary>
        /// Sorting threshold
        /// </summary>
        private const int CTXIDS = 3;

        /// <summary>
        /// Sorting threshold
        /// </summary>
        private static readonly sbyte[] MTF = new sbyte[256];

        /// <summary>
        /// Decoder to use
        /// </summary>
        internal IDataCoder Coder;

        /// <summary>
        /// Values being decoded
        /// </summary>
        private byte[] _ctx = new byte[300];

        /// <summary>
        /// Decoded data
        /// </summary>
        private byte[] _data;

        /// <summary>
        /// True if the EOF has been read
        /// </summary>
        private bool _eof;

        /// <summary>
        /// Block size of the data
        /// </summary>
        private int _blocksize;

        /// <summary>
        /// Offset into the data
        /// </summary>
        private int _bptr = 0;

        /// <summary>
        /// Size of the data read
        /// </summary>
        private int _size = 0;

        #endregion Private Members

        #region Properties

        public override long Length
        {
            get
            {
                if (_data == null)
                    return 0;
                else
                    return _data.Length;
            }
        }

        #endregion Properties

        #region Constructors

        static BSInputStream()
        {
            for (int i = 0; i < MTF.Length; i++)
            {
                MTF[i] = (sbyte)i;
            }
        }

        /// <summary>
        /// TODO docs
        /// </summary>
        public BSInputStream() : base()
        {
            Init((MemoryStream)this);
        }

        public BSInputStream(int capacity):base(capacity)
        {
            Init((MemoryStream)this);
        }

        public BSInputStream(byte[] buffer): base(buffer)
        {
            Init((MemoryStream)this);
        }

        //
        // Summary:
        //     Initializes a new non-resizable instance of the System.IO.MemoryStream class
        //     based on the specified byte array with the System.IO.MemoryStream.CanWrite property
        //     set as specified.
        //
        // Parameters:
        //   buffer:
        //     The array of unsigned bytes from which to create this stream.
        //
        //   writable:
        //     The setting of the System.IO.MemoryStream.CanWrite property, which determines
        //     whether the stream supports writing.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     buffer is null.
        public BSInputStream(byte[] buffer, bool writable) : base(buffer, writable)
        {
            Init((MemoryStream)this);
        }
        //
        // Summary:
        //     Initializes a new non-resizable instance of the System.IO.MemoryStream class
        //     based on the specified region (index) of a byte array.
        //
        // Parameters:
        //   buffer:
        //     The array of unsigned bytes from which to create this stream.
        //
        //   index:
        //     The index into buffer at which the stream begins.
        //
        //   count:
        //     The length of the stream in bytes.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     buffer is null.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     index or count is less than zero.
        //
        //   T:System.ArgumentException:
        //     The buffer length minus index is less than count.
        public BSInputStream(byte[] buffer, int index, int count) : base(buffer, index, count)
        {
            Init((MemoryStream)this);
        }
        //
        // Summary:
        //     Initializes a new non-resizable instance of the System.IO.MemoryStream class
        //     based on the specified region of a byte array, with the System.IO.MemoryStream.CanWrite
        //     property set as specified.
        //
        // Parameters:
        //   buffer:
        //     The array of unsigned bytes from which to create this stream.
        //
        //   index:
        //     The index in buffer at which the stream begins.
        //
        //   count:
        //     The length of the stream in bytes.
        //
        //   writable:
        //     The setting of the System.IO.MemoryStream.CanWrite property, which determines
        //     whether the stream supports writing.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     buffer is null.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     index or count are negative.
        //
        //   T:System.ArgumentException:
        //     The buffer length minus index is less than count.
        public BSInputStream(byte[] buffer, int index, int count, bool writable) 
            : base(buffer, index, count, writable)
        {
            Init((MemoryStream)this);
        }
        //
        // Summary:
        //     Initializes a new instance of the System.IO.MemoryStream class based on the specified
        //     region of a byte array, with the System.IO.MemoryStream.CanWrite property set
        //     as specified, and the ability to call System.IO.MemoryStream.GetBuffer set as
        //     specified.
        //
        // Parameters:
        //   buffer:
        //     The array of unsigned bytes from which to create this stream.
        //
        //   index:
        //     The index into buffer at which the stream begins.
        //
        //   count:
        //     The length of the stream in bytes.
        //
        //   writable:
        //     The setting of the System.IO.MemoryStream.CanWrite property, which determines
        //     whether the stream supports writing.
        //
        //   publiclyVisible:
        //     true to enable System.IO.MemoryStream.GetBuffer, which returns the unsigned byte
        //     array from which the stream was created; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     buffer is null.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     index or count is negative.
        //
        //   T:System.ArgumentException:
        //     The buffer length minus index is less than count.
        public BSInputStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible) 
            : base(buffer, index, count, writable, publiclyVisible)
        {
            Init((MemoryStream)this);
        }

        /// <summary>
        /// TODO docs
        /// </summary>
        /// <param name="input"></param>
        public BSInputStream(Stream input)
        {
            Init(input);
        }

        #endregion Constructors

        public long PositionEncoded
        {
            get { return Coder.DataStream.Position; }
            set { Coder.DataStream.Position = value; }
        }

        #region Public Methods

        /// <summary>
        /// TODO documentation
        /// </summary>
        public override void Flush()
        {
            _size = _bptr = 0;
        }

        /// <summary>
        /// TODO documentation
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public BSInputStream Init(Stream input)
        {
            Coder = DjvuSettings.Current.CoderFactory.CreateCoder(input, false);
            return this;
        }

        /// <summary>
        /// TODO Documentation
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int copied = 0;

            if (_eof)
                return copied;

            while (count > 0 && !_eof)
            {
                // Decoder if needed
                if (_size == 0)
                {
                    _bptr = 0;

                    if (Decode() == 0)
                    {
                        _size = 1;
                        _eof = true;
                    }

                    _size--;
                }

                // Compute remaining
                int bytes = (_size > count) ? count : _size;

                // Transfer
                if (bytes > 0)
                {
                    Array.Copy(_data, _bptr, buffer, offset, bytes);
                    offset += bytes;
                }

                _size -= bytes;
                _bptr += bytes;
                count -= bytes;
                copied += bytes;
            }

            // Return copied bytes
            return copied;
        }

        /// <summary>
        /// Reads a single sbyte from the input stream
        /// </summary>
        /// <returns></returns>
        public override int ReadByte()
        {
            byte[] buffer = new byte[1];
            int countRead = Read(buffer, 0, buffer.Length);

            return countRead == 1 ? (0xff & buffer[0]) : (-1);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// TODO documentation
        /// </summary>
        /// <param name="ctxoff"></param>
        /// <param name="bits"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DecodeBinary(int ctxoff, int bits)
        {
            int n = 1;
            int m = (1 << bits);
            ctxoff--;

            while (n < m)
            {
                int b = Coder.Decoder(ref _ctx[ctxoff + n]);
                n = (n << 1) | b;
            }

            return n - m;
        }

        /// <summary>
        /// TODO documentation
        /// </summary>
        /// <returns></returns>
        internal int Decode()
        {
            /////////////////////////////////
            ////////////  Decoder input stream
            _size = DecodeRaw(24);

            if (_size == 0)
                return 0;

            if (_size > MAXBLOCK * 1024)
                throw new System.IO.IOException("ByteStream.corrupt");

            // Allocate
            if (_blocksize < _size)
            {
                _blocksize = _size;
                _data = new byte[_blocksize];
            }
            else if (_data == null)
                _data = new byte[_blocksize];

            // Decoder Estimation Speed
            int fshift = 0;

            if (Coder.Decoder() != 0)
            {
                fshift++;
                if (Coder.Decoder() != 0)
                    fshift++;
            }

            // Prepare Quasi MTF
            byte[] mtf = (byte[])MTF.Clone();

            int[] freq = new int[FREQMAX];

            int fadd = 4;

            // Decoder
            int mtfno = 3;
            int markerpos = -1;

            for (int i = 0; i < _size; i++)
            {
                int ctxid = CTXIDS - 1;

                if (ctxid > mtfno)
                    ctxid = mtfno;

                int ctxoff = 0;

                switch (0)
                {
                    default:

                        if (Coder.Decoder(ref _ctx[ctxoff + ctxid]) != 0)
                        {
                            mtfno = 0;
                            _data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += CTXIDS;

                        if (Coder.Decoder(ref _ctx[ctxoff + ctxid]) != 0)
                        {
                            mtfno = 1;
                            _data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += CTXIDS;

                        if (Coder.Decoder(ref _ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 2 + DecodeBinary(ctxoff + 1, 1);
                            _data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 1);

                        if (Coder.Decoder(ref _ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 4 + DecodeBinary(ctxoff + 1, 2);
                            _data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 3);

                        if (Coder.Decoder(ref _ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 8 + DecodeBinary(ctxoff + 1, 3);
                            _data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 7);

                        if (Coder.Decoder(ref _ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 16 + DecodeBinary(ctxoff + 1, 4);
                            _data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 15);

                        if (Coder.Decoder(ref _ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 32 + DecodeBinary(ctxoff + 1, 5);
                            _data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 31);

                        if (Coder.Decoder(ref _ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 64 + DecodeBinary(ctxoff + 1, 6);
                            _data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 63);

                        if (Coder.Decoder(ref _ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 128 + DecodeBinary(ctxoff + 1, 7);
                            _data[i] = mtf[mtfno];

                            break;
                        }

                        mtfno = 256;
                        _data[i] = 0;
                        markerpos = i;

                        continue;
                }

                // Rotate mtf according to empirical frequencies (new!)
                // Adjust frequencies for overflow
                int k;
                fadd = fadd + (fadd >> fshift);

                if (fadd > 0x10000000)
                {
                    fadd >>= 24;
                    freq[0] >>= 24;
                    freq[1] >>= 24;
                    freq[2] >>= 24;
                    freq[3] >>= 24;

                    for (k = 4; k < freq.Length; k++)
                        freq[k] >>= 24;
                }

                // Relocate new char according to new freq
                int fc = fadd;

                if (mtfno < FREQMAX)
                    fc += freq[mtfno];

                for (k = mtfno; k >= FREQMAX; k--)
                    mtf[k] = mtf[k - 1];

                for (; (k > 0) && ((unchecked((int)0xffffffffL) & fc) >= (unchecked((int)0xffffffffL) & freq[k - 1])); k--)
                {
                    mtf[k] = mtf[k - 1];
                    freq[k] = freq[k - 1];
                }

                mtf[k] = _data[i];
                freq[k] = fc;
            }

            /////////////////////////////////
            ////////// Reconstruct the string
            if ((markerpos < 1) || (markerpos >= _size))
                throw new System.IO.IOException("ByteStream.corrupt");

            // Allocate pointers
            int[] pos = new int[_size];

            // Prepare count buffer
            int[] count = new int[256];

            // Fill count buffer
            for (int i = 0; i < markerpos; i++)
            {
                sbyte c = (sbyte)_data[i];
                pos[i] = (c << 24) | (count[0xff & c] & 0xffffff);
                count[0xff & c]++;
            }

            for (int i = markerpos + 1; i < _size; i++)
            {
                sbyte c = (sbyte)_data[i];
                pos[i] = (c << 24) | (count[0xff & c] & 0xffffff);
                count[0xff & c]++;
            }

            // Compute sorted char positions
            int last = 1;

            for (int i = 0; i < 256; i++)
            {
                int tmp = count[i];
                count[i] = last;
                last += tmp;
            }

            // Undo the sort transform
            int j2 = 0;
            last = _size - 1;

            while (last > 0)
            {
                int n = pos[j2];
                sbyte c = (sbyte)(pos[j2] >> 24);
                _data[--last] = (byte)c;
                j2 = count[0xff & c] + (n & 0xffffff);
            }

            // Free and check
            if (j2 != markerpos)
                throw new System.IO.IOException("ByteStream.corrupt");

            return _size;
        }

        /// <summary>
        /// TODO documentation
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        internal int DecodeRaw(int bits)
        {
            int n = 1;
            int m = (1 << bits);

            while (n < m)
            {
                int b = Coder.Decoder();
                n = (n << 1) | b;
            }

            return n - m;
        }

        #endregion Private Methods
    }
}