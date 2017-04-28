using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Configuration;

namespace DjvuNet.Compression
{
    public class BSOutputStream : BSBaseStream
    {
        #region Constructors

        /// <summary>
        /// TODO docs
        /// </summary>
        public BSOutputStream() : base()
        {
            SetBlockSize(0);
        }

        /// <summary>
        /// TODO docs
        /// </summary>
        /// <param name="input"></param>
        public BSOutputStream(Stream input, int blockSize) : base (input)
        {
            SetBlockSize(blockSize);
        }

        #endregion Constructors

        public override void Close()
        {
            Flush();
            EncodeRaw(Coder, 24, 0);
            Coder.Dispose();
            base.Close();
        }

        internal static void EncodeRaw(IDataCoder coder, int bits, int x)
        {
            int n = 1;
            int m = (1 << bits);
            while (n < m)
            {
                x = (x & (m - 1)) << 1;
                int b = (x >> bits);
                coder.Encoder(b);
                n = (n << 1) | b;
            }
        }

        internal static void EncodeBinary(IDataCoder coder, byte[] ctx, int bits, int x, int offset = 0)
        {
            // Require 2^bits-1  contexts
            int n = 1;
            int m = (1 << bits);
            while (n < m)
            {
                x = (x & (m - 1)) << 1;
                int b = (x >> bits);
                coder.Encoder(b, ref ctx[n - 1 + offset]);
                n = (n << 1) | b;
            }
        }

        public void Encode()
        {

            int markerpos = _Size - 1;
            BlockSort.BlockSortData(_Data, _Size, ref markerpos);

            // Encode Output Stream

            // Header
            EncodeRaw(Coder, 24, _Size);

            // Determine and Encode Estimation Speed
            int fshift = 0;
            if (_Size < FreqS0)
            {
                fshift = 0;
                Coder.Encoder(0);
            }
            else if (_Size < FreqS1)
            {
                fshift = 1;
                Coder.Encoder(1);
                Coder.Encoder(0);
            }
            else
            {
                fshift = 2;
                Coder.Encoder(1);
                Coder.Encoder(1);
            }
            // MTF
            byte[] mtf = new byte[256];
            byte[] rmtf = new byte[256];
            uint[] freq = new uint[FreqMax];

            uint m = 0;
            for (m = 0; m < 256; m++)
                mtf[m] = (byte) m;

            for (m = 0; m < 256; m++)
                rmtf[mtf[m]] = (byte) m;

            int fadd = 4;

            // Encode
            int i;
            int mtfno = 3;
            for (i = 0; i < _Size; i++)
            {
                // Get MTF data
                int c = _Data[i];
                int ctxid = CTXIDS - 1;
                if (ctxid > mtfno)
                    ctxid = mtfno;

                mtfno = rmtf[c];

                if (i == markerpos)
                    mtfno = 256;

                // Encode using ZPCoder
                int b;
                b = (mtfno == 0) ? 1 : 0;
                int cx = 0;

                Coder.Encoder(b, ref _Context[ctxid]);

                if (b != 0)
                    goto rotate;

                cx += CTXIDS;

                b = (mtfno == 1) ? 1 : 0;

                Coder.Encoder(b, ref _Context[ctxid + cx]);

                if (b != 0)
                    goto rotate;

                cx += CTXIDS;
                b = (mtfno < 4) ? 1 : 0;

                Coder.Encoder(b, ref _Context[cx]);

                if (b != 0)
                {
                    EncodeBinary(Coder, _Data, 1, mtfno - 2, cx + 1);
                    goto rotate;
                }

                cx += 1 + 1;

                b = (mtfno < 8) ? 1 : 0;

                Coder.Encoder(b, ref _Context[cx]);

                if (b != 0)
                {
                    EncodeBinary(Coder, _Data, 2, mtfno - 4, cx + 1);
                    goto rotate;
                }

                cx += 1 + 3;
                b = (mtfno < 16) ? 1 : 0;

                Coder.Encoder(b, ref _Context[cx]);

                if (b != 0)
                {
                    EncodeBinary(Coder, _Data, 3, mtfno - 8, cx + 1);
                    goto rotate;
                }

                cx += 1 + 7;
                b = (mtfno < 32) ? 1 : 0;

                Coder.Encoder(b, ref _Context[cx]);

                if (b != 0)
                {
                    EncodeBinary(Coder, _Data, 4, mtfno - 16, cx + 1);
                    goto rotate;
                }

                cx += 1 + 15;
                b = (mtfno < 64) ? 1 : 0;

                Coder.Encoder(b, ref _Context[cx]);

                if (b != 0)
                {
                    EncodeBinary(Coder, _Data, 5, mtfno - 32, cx + 1);
                    goto rotate;
                }

                cx += 1 + 31;
                b = (mtfno < 128) ? 1 : 0;

                Coder.Encoder(b, ref _Context[cx]);

                if (b != 0)
                {
                    EncodeBinary(Coder, _Data, 6, mtfno - 64, cx + 1);
                    goto rotate;
                }

                cx += 1 + 63;
                b = (mtfno < 256) ? 1 : 0;

                Coder.Encoder(b, ref _Context[cx]);

                if (b != 0)
                {
                    EncodeBinary(Coder, _Data, 7, mtfno - 128, cx + 1);
                    goto rotate;
                }

                continue;

                // Rotate MTF according to empirical frequencies (new!)
                rotate:
                // Adjust frequencies for overflow
                fadd = fadd + (fadd >> fshift);
                if (fadd > 0x10000000)
                {
                    fadd = fadd >> 24;
                    freq[0] >>= 24;
                    freq[1] >>= 24;
                    freq[2] >>= 24;
                    freq[3] >>= 24;
                    for (int kk = 4; kk < FreqMax; kk++)
                        freq[kk] >>= 24;
                }
                // Relocate new char according to new freq
                uint fc = (uint) fadd;

                if (mtfno < FreqMax)
                    fc += freq[mtfno];

                int k;
                for (k = mtfno; k >= FreqMax; k--)
                {
                    mtf[k] = mtf[k - 1];
                    rmtf[mtf[k]] = (byte)k;
                }

                for (; k > 0 && fc >= freq[k - 1]; k--)
                {
                    mtf[k] = mtf[k - 1];
                    freq[k] = freq[k - 1];
                    rmtf[mtf[k]] = (byte)k;
                }

                mtf[k] = (byte) c;
                freq[k] = fc;
                rmtf[mtf[k]] = (byte) k;
            }
        }

        public override BSBaseStream Init(Stream input)
        {
            Coder = DjvuSettings.Current.CoderFactory.CreateCoder(input, true);
            return this;
        }

        private void SetBlockSize(int blockSize)
        {
            int encoding = (blockSize < MinBlock) ? MinBlock : blockSize;
            if (encoding > MaxBlock)
                throw new ArgumentException("Block size exceeds maximum value.", nameof(blockSize));

            _BlockSize = 1024 * 1024;
        }

        public override void Flush()
        {
            if (BlockOffset > 0)
            {
                if (!(BlockOffset < _BlockSize))
                    throw new InvalidOperationException();

                for (int i = 0; i < Overflow; i++)
                    _Data[BlockOffset + i] = 0;

                _Size = BlockOffset + 1;
                Encode();
                Coder.Flush();
            }

            _Size = BlockOffset = 0;
        }

        public override void Write(byte[] buffer, int offset, int sz)
        {
            // Trivial checks
            if (sz == 0)
                return;

            // Loop
            int copied = 0;
            while (sz > 0)
            {
                // Initialize
                if (_Data == null)
                {
                    BlockOffset = 0;
                    _Data = new byte[_BlockSize + Overflow];
                }
                // Compute remaining
                int bytes = _BlockSize - 1 - BlockOffset;

                if (bytes > sz)
                    bytes = sz;

                Buffer.BlockCopy(buffer, copied, _Data, BlockOffset, bytes);

                BlockOffset = (BlockOffset + bytes);
                sz = (sz - bytes);
                copied = (copied + bytes);
                _Offset = (_Offset + bytes);
                // Flush when needed
                if (BlockOffset + 1 >= _BlockSize)
                    Flush();
            }
        }
    }
}
