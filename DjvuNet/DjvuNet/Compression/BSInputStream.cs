using System;
using System.IO;
using System.Runtime.CompilerServices;

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
        private ZPCodec zp = null;

        /// <summary>
        /// Values being decoded
        /// </summary>
        private MutableValue<sbyte>[] ctx = new MutableValue<sbyte>[300];

        /// <summary>
        /// Decoded data
        /// </summary>
        private byte[] data = null;

        /// <summary>
        /// True if the EOF has been read
        /// </summary>
        private bool eof = false;

        /// <summary>
        /// Block size of the data
        /// </summary>
        private int blocksize = 0;

        /// <summary>
        /// Offset into the data
        /// </summary>
        private int bptr = 0;

        /// <summary>
        /// Size of the data read
        /// </summary>
        private int size = 0;

        #endregion Private Members

        #region Constructors

        static BSInputStream()
        {
            {
                for (int i = 0; i < MTF.Length; i++)
                {
                    MTF[i] = (sbyte)i;
                }
            }
        }

        /// <summary>
        /// TODO docs
        /// </summary>
        public BSInputStream()
        {
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

        #region Public Methods

        /// <summary>
        /// TODO documentation
        /// </summary>
        public override void Flush()
        {
            size = bptr = 0;
        }

        /// <summary>
        /// TODO documentation
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public BSInputStream Init(Stream input)
        {
            zp = new ZPCodec().Init(input);

            for (int i = 0; i < ctx.Length; )
            {
                ctx[i++] = new MutableValue<sbyte>();
            }

            return this;
        }

        /// <summary>
        /// TODO Documentation
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="sz"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int sz)
        {
            int copied = 0;

            if (eof)
                return copied;

            while (sz > 0 && !eof)
            {
                // Decoder if needed
                if (size == 0)
                {
                    bptr = 0;

                    if (Decode() == 0)
                    {
                        size = 1;
                        eof = true;
                    }

                    size--;
                }

                // Compute remaining
                int bytes = (size > sz) ? sz : size;

                // Transfer
                if (bytes > 0)
                {
                    Array.Copy(data, bptr, buffer, offset, bytes);
                    offset += bytes;
                }

                size -= bytes;
                bptr += bytes;
                sz -= bytes;
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
                int b = zp.Decoder(ctx[ctxoff + n]);
                n = (n << 1) | b;
            }

            return n - m;
        }

        /// <summary>
        /// TODO documentation
        /// </summary>
        /// <returns></returns>
        private int Decode()
        {
            /////////////////////////////////
            ////////////  Decoder input stream
            size = DecodeRaw(24);

            if (size == 0)
                return 0;

            if (size > MAXBLOCK * 1024)
                throw new System.IO.IOException("ByteStream.corrupt");

            // Allocate
            if (blocksize < size)
            {
                blocksize = size;
                data = new byte[blocksize];
            }
            else if (data == null)
                data = new byte[blocksize];

            // Decoder Estimation Speed
            int fshift = 0;

            if (zp.Decoder() != 0)
            {
                fshift++;

                if (zp.Decoder() != 0)
                    fshift++;
            }

            // Prepare Quasi MTF
            byte[] mtf = (byte[])MTF.Clone();

            int[] freq = new int[FREQMAX];

            int fadd = 4;

            // Decoder
            int mtfno = 3;
            int markerpos = -1;

            for (int i = 0; i < size; i++)
            {
                int ctxid = CTXIDS - 1;

                if (ctxid > mtfno)
                    ctxid = mtfno;

                int ctxoff = 0;

                switch (0)
                {
                    default:

                        if (zp.Decoder(ctx[ctxoff + ctxid]) != 0)
                        {
                            mtfno = 0;
                            data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += CTXIDS;

                        if (zp.Decoder(ctx[ctxoff + ctxid]) != 0)
                        {
                            mtfno = 1;
                            data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += CTXIDS;

                        if (zp.Decoder(ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 2 + DecodeBinary(ctxoff + 1, 1);
                            data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 1);

                        if (zp.Decoder(ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 4 + DecodeBinary(ctxoff + 1, 2);
                            data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 3);

                        if (zp.Decoder(ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 8 + DecodeBinary(ctxoff + 1, 3);
                            data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 7);

                        if (zp.Decoder(ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 16 + DecodeBinary(ctxoff + 1, 4);
                            data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 15);

                        if (zp.Decoder(ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 32 + DecodeBinary(ctxoff + 1, 5);
                            data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 31);

                        if (zp.Decoder(ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 64 + DecodeBinary(ctxoff + 1, 6);
                            data[i] = mtf[mtfno];

                            break;
                        }

                        ctxoff += (1 + 63);

                        if (zp.Decoder(ctx[ctxoff + 0]) != 0)
                        {
                            mtfno = 128 + DecodeBinary(ctxoff + 1, 7);
                            data[i] = mtf[mtfno];

                            break;
                        }

                        mtfno = 256;
                        data[i] = 0;
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

                mtf[k] = data[i];
                freq[k] = fc;
            }

            /////////////////////////////////
            ////////// Reconstruct the string
            if ((markerpos < 1) || (markerpos >= size))
                throw new System.IO.IOException("ByteStream.corrupt");

            // Allocate pointers
            int[] pos = new int[size];

            // Prepare count buffer
            int[] count = new int[256];

            // Fill count buffer
            for (int i = 0; i < markerpos; i++)
            {
                sbyte c = (sbyte)data[i];
                pos[i] = (c << 24) | (count[0xff & c] & 0xffffff);
                count[0xff & c]++;
            }

            for (int i = markerpos + 1; i < size; i++)
            {
                sbyte c = (sbyte)data[i];
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
            last = size - 1;

            while (last > 0)
            {
                int n = pos[j2];
                sbyte c = (sbyte)(pos[j2] >> 24);
                data[--last] = (byte)c;
                j2 = count[0xff & c] + (n & 0xffffff);
            }

            // Free and check
            if (j2 != markerpos)
                throw new System.IO.IOException("ByteStream.corrupt");

            return size;
        }

        /// <summary>
        /// TODO documentation
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        private int DecodeRaw(int bits)
        {
            int n = 1;
            int m = (1 << bits);

            while (n < m)
            {
                int b = zp.Decoder();
                n = (n << 1) | b;
            }

            return n - m;
        }

        #endregion Private Methods
    }
}