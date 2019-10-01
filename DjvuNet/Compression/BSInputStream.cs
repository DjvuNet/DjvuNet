using System;
using System.IO;
using System.Runtime.CompilerServices;
using DjvuNet.Configuration;
using DjvuNet.Errors;

namespace DjvuNet.Compression
{
    /// <summary>
    /// This class decodes a bzz encoded InputStream.
    /// </summary>
    public partial class BSInputStream : BSBaseStream
    {

        #region Properties

        public override bool CanWrite => false;

        public override bool CanRead => true;

        public override long Length
        {
            get
            {
                if (_Data == null)
                {
                    return 0;
                }
                else
                {
                    return _Data.Length;
                }
            }
        }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// TODO docs
        /// </summary>
        public BSInputStream() : base()
        {
        }

        /// <summary>
        /// TODO docs
        /// </summary>
        /// <param name="input"></param>
        public BSInputStream(Stream input) : base(input)
        {
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
            _Size = BlockOffset = 0;
        }

        /// <summary>
        /// TODO documentation
        /// </summary>
        /// <param name="dataStream"></param>
        /// <returns></returns>
        public override BSBaseStream Init(Stream dataStream)
        {
            if (!dataStream.CanRead)
            {
                throw new DjvuArgumentException("Stream was not readable.", nameof(dataStream));
            }

            BaseStream = dataStream;
            Coder = DjvuSettings.Current.CoderFactory.CreateCoder(dataStream, false);
            return this;
        }

        /// <summary>
        /// TODO Documentation
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public override int Read(byte[] buffer, int offset, int count)
        {
            int copied = 0;

            if (_Eof)
            {
                return copied;
            }

            while (count > 0 && !_Eof)
            {
                // Decoder if needed
                if (_Size == 0)
                {
                    BlockOffset = 0;

                    if (Decode() == 0)
                    {
                        _Size = 1;
                        _Eof = true;
                    }

                    _Size--;
                }

                // Compute remaining
                int bytes = (_Size > count) ? count : _Size;

                // Transfer
                if (bytes > 0)
                {
                    Array.Copy(_Data, BlockOffset, buffer, offset, bytes);
                    offset += bytes;
                }

                _Size -= bytes;
                BlockOffset += bytes;
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
        private int DecodeBinary(int ctxoff, int bits)
        {
            int n = 1;
            int m = (1 << bits);
            ctxoff--;

            while (n < m)
            {
                int b = Coder.Decoder(ref _Cxt[ctxoff + n]);
                n = (n << 1) | b;
            }

            return n - m;
        }

        /// <summary>
        /// TODO documentation
        /// </summary>
        /// <returns></returns>
#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        internal int Decode()
        {
            /////////////////////////////////
            ////////////  Decoder input stream
            _Size = DecodeRaw(24);

            if (_Size == 0)
            {
                return 0;
            }

            if (_Size > MaxBlock * 1024)
            {
                throw new DjvuFormatException("ByteStream.corrupt");
            }

            // Allocate
            if (_BlockSize < _Size)
            {
                _BlockSize = _Size;
                _Data = new byte[_BlockSize];
            }
            else if (_Data == null)
            {
                _Data = new byte[_BlockSize];
            }

            // Decoder Estimation Speed
            int fshift = 0;

            if (Coder.Decoder() != 0)
            {
                fshift++;
                if (Coder.Decoder() != 0)
                {
                    fshift++;
                }
            }

            // Prepare Quasi MTF
            byte[] mtf = new byte[256];
            uint m = 0;
            for (m = 0; m < 256; m++)
            {
                mtf[m] = (byte)m;
            }

            int[] freq = new int[FreqMax];

            int fadd = 4;

            // Decoder
            int mtfno = 3;
            int markerpos = -1;

            for (int i = 0; i < _Size; i++)
            {
                int ctxid = CTXIDS - 1;

                if (ctxid > mtfno)
                {
                    ctxid = mtfno;
                }

                int ctxoff = 0;

                switch (0)
                {
                    default:

                        if (Coder.Decoder(ref _Cxt[ctxoff + ctxid]) != 0)
                        {
                            mtfno = 0;
                            _Data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += CTXIDS;

                        if (Coder.Decoder(ref _Cxt[ctxoff + ctxid]) != 0)
                        {
                            mtfno = 1;
                            _Data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += CTXIDS;

                        if (Coder.Decoder(ref _Cxt[ctxoff + 0]) != 0)
                        {
                            mtfno = 2 + DecodeBinary(ctxoff + 1, 1);
                            _Data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 1);

                        if (Coder.Decoder(ref _Cxt[ctxoff + 0]) != 0)
                        {
                            mtfno = 4 + DecodeBinary(ctxoff + 1, 2);
                            _Data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 3);

                        if (Coder.Decoder(ref _Cxt[ctxoff + 0]) != 0)
                        {
                            mtfno = 8 + DecodeBinary(ctxoff + 1, 3);
                            _Data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 7);

                        if (Coder.Decoder(ref _Cxt[ctxoff + 0]) != 0)
                        {
                            mtfno = 16 + DecodeBinary(ctxoff + 1, 4);
                            _Data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 15);

                        if (Coder.Decoder(ref _Cxt[ctxoff + 0]) != 0)
                        {
                            mtfno = 32 + DecodeBinary(ctxoff + 1, 5);
                            _Data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 31);

                        if (Coder.Decoder(ref _Cxt[ctxoff + 0]) != 0)
                        {
                            mtfno = 64 + DecodeBinary(ctxoff + 1, 6);
                            _Data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 63);

                        if (Coder.Decoder(ref _Cxt[ctxoff + 0]) != 0)
                        {
                            mtfno = 128 + DecodeBinary(ctxoff + 1, 7);
                            _Data[i] = mtf[mtfno];

                            break;
                        }

                        mtfno = 256;
                        _Data[i] = 0;
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
                    {
                        freq[k] >>= 24;
                    }
                }

                // Relocate new char according to new freq
                int fc = fadd;

                if (mtfno < FreqMax)
                {
                    fc += freq[mtfno];
                }

                for (k = mtfno; k >= FreqMax; k--)
                {
                    mtf[k] = mtf[k - 1];
                }

                for (; (k > 0) && ((unchecked((int)0xffffffffL) & fc) >= (unchecked((int)0xffffffffL) & freq[k - 1])); k--)
                {
                    mtf[k] = mtf[k - 1];
                    freq[k] = freq[k - 1];
                }

                mtf[k] = _Data[i];
                freq[k] = fc;
            }

            /////////////////////////////////
            ////////// Reconstruct the string
            if ((markerpos < 1) || (markerpos >= _Size))
            {
                throw new DjvuFormatException("ByteStream.corrupt");
            }

            // Allocate pointers
            int[] pos = new int[_Size];

            // Prepare count buffer
            int[] count = new int[256];

            // Fill count buffer
            for (int i = 0; i < markerpos; i++)
            {
                sbyte c = (sbyte)_Data[i];
                pos[i] = (c << 24) | (count[0xff & c] & 0xffffff);
                count[0xff & c]++;
            }

            for (int i = markerpos + 1; i < _Size; i++)
            {
                sbyte c = (sbyte)_Data[i];
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
            last = _Size - 1;

            while (last > 0)
            {
                int n = pos[j2];
                sbyte c = (sbyte)(pos[j2] >> 24);
                _Data[--last] = (byte)c;
                j2 = count[0xff & c] + (n & 0xffffff);
            }

            // Free and check
            if (j2 != markerpos)
            {
                throw new DjvuFormatException("ByteStream.corrupt");
            }

            return _Size;
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

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new DjvuInvalidOperationException("Unsupported operation.");
        }

        #endregion Private Methods
    }
}
