using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DjvuNet.Compression
{
    public class ZPCodec : IDataCoder, IDisposable
    {
        #region Protected Fields

        protected const int _ArraySize = 256;
        protected byte _ZByte;
        protected byte _SCount;
        protected byte _Delay;
        protected uint _Code;
        protected uint _Fence;
        protected uint _Buffer;
        protected uint _NRun;
        protected uint _Subend;
        protected ulong _Bitcount;
        protected ZPTable[] _DefaultTable;
        protected sbyte[] _FFZT;

        #endregion Protected Fields

        #region Internal Properties

        /// <summary>
        /// Gets the FFZT data
        /// </summary>
        internal sbyte[] FFZT { get { return _FFZT; } }

        /// <summary>
        /// Gets or sets the A Value for the item
        /// </summary>
        internal uint _AValue;

        /// <summary>
        /// Gets the Ffzt data
        /// </summary>
        internal sbyte[] _Ffzt;

        #endregion Internal Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets the down values for the item
        /// </summary>
        public byte[] _Down;

        /// <summary>
        /// Gets or sets the up values for the item
        /// </summary>
        public byte[] _Up;

        /// <summary>
        /// Gets or sets the M Array values for the item
        /// </summary>
        public uint[] _MArray;

        /// <summary>
        /// Gets or sets the P Array values for the item
        /// </summary>
        public uint[] _PArray;

        #endregion Public Properties

        #region Constructors

        public ZPCodec()
        {
            InitializeInternal();
        }


        public ZPCodec(Stream dataStream, bool encoding = false, bool djvuCompat = true)
        {
            InitializeInternal(encoding, djvuCompat);
            Initializa(dataStream);
        }

        #endregion Constructors


        internal void InitializeInternal(bool encoding = false, bool djvuCompat = true)
        {
            Encoding = encoding;
            DjvuCompat = djvuCompat;

            _FFZT = new sbyte[_ArraySize];

            for (int i = 0; i < _ArraySize; i++)
            {
                for (int j = i; (j & 0x80) > 0; j <<= 1)
                    FFZT[i]++;
            }

            _Ffzt = new sbyte[FFZT.Length];
            Buffer.BlockCopy(FFZT, 0, _Ffzt, 0, _Ffzt.Length);

            _Down = new byte[_ArraySize];
            _Up = new byte[_ArraySize];
            _MArray = new uint[_ArraySize];
            _PArray = new uint[_ArraySize];

            NewTable(DefaultTable);

            if (!DjvuCompat)
            {

                for (int j = 0; j < 256; j++)
                {
                    ushort a = (ushort)(0x10000 - _PArray[j]);

                    while (a >= 0x8000)
                        a = (ushort)(a << 1);

                    if (_MArray[j] > 0 && a + _PArray[j] >= 0x8000 && a >= _MArray[j])
                    {
                        byte x = DefaultTable[j].Down;
                        byte y = DefaultTable[x].Down;
                        _Down[j] = y;
                    }
                }
            }
        }

        #region IDisposable Implementation

        protected bool _Disposed;

        public bool Disposed { get { return _Disposed; } }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_Disposed && Encoding)
                Flush();

            _Disposed = true;
        }

        ~ZPCodec()
        {
            Dispose(false);
        }

        #endregion IDisposable Implementation

        #region IDataCoder Implementation

        public Stream DataStream { get; internal set; }

        /// <summary>
        /// Gets the default ZP table
        /// </summary>
        public ZPTable[] DefaultTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_DefaultTable != null)
                    return _DefaultTable;
                else
                {
                    _DefaultTable = CreateDefaultTable();
                    return _DefaultTable;
                }
            }
        }

        public bool Encoding
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            internal set;
        }

        public bool DjvuCompat
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            internal set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IWDecoder()
        {
            return DecodeSubSimple(0, 0x8000 + ((_AValue + _AValue + _AValue) >> 3));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IWEncoder(bool bit)
        {
            uint z = 0x8000 + ((_AValue + _AValue + _AValue) >> 3);
            if (bit)
                EncodeLpsSimple((uint)z);
            else
                EncodeMpsSimple((uint)z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Decoder()
        {
            return DecodeSubSimple(0, 0x8000 + (_AValue >> 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encoder(int bit)
        {
            if (bit != 0)
                EncodeLpsSimple((uint)(0x8000 + (_AValue >> 1)));
            else
                EncodeMpsSimple((uint)(0x8000 + (_AValue >> 1)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Decoder(ref byte ctx)
        {
            uint z = _AValue + _PArray[ctx];

            if (z <= _Fence)
            {
                _AValue = z;
                return ctx & 1;
            }
            else
                return DecodeSub(ref ctx, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encoder(int bit, ref byte ctx)
        {
            uint z = _AValue + _PArray[ctx];

            if (bit != (ctx & 1))
                EncodeLps(ref ctx, z);
            else if (z >= 0x8000)
                EncodeMps(ref ctx, z);
            else
                _AValue = z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int DecoderNoLearn(ref byte ctx)
        {
            uint z = _AValue + _PArray[ctx];
            if (z <= _Fence)
            {
                _AValue = z;
                return (ctx & 1);
            }

            return DecodeSubNolearn((ctx & 1), z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EncoderNoLearn(int bit, ref byte ctx)
        {
            uint z = _AValue + _PArray[ctx];
            if (bit != (ctx & 1))
                EncodeLpsNolearn(z);
            else if (z >= 0x8000)
                EncodeMpsNolearn(z);
            else
                _AValue = z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NewTable(ZPTable[] table)
        {
            for (int i = 0; i < _ArraySize; i++)
            {
                ZPTable z = table[i];
                _PArray[i] = z.PValue;
                _MArray[i] = z.MValue;
                _Up[i] = z.Up;
                _Down[i] = z.Down;
            }
        }

        public byte State(float prob1)
        {
            // Return a state representing 'prob1' in the steady chain
            int mps = (prob1 <= 0.5 ? 0 : 1);
            float plps = (float)(mps != 0 ? 1.0 - prob1 : prob1);

            // Locate steady chain (ordered, decreasing)
            int sz = 0;
            int lo = (mps != 0 ? 1 : 2);

            while (_PArray[lo + sz + sz + 2] < _PArray[lo + sz + sz]) sz += 1;

            // Bisection
            while (sz > 1)
            {
                int nsz = sz >> 1;
                float nplps = P2PlPs((ushort)_PArray[lo + nsz + nsz]);
                if (nplps < plps)
                    sz = nsz;
                else
                {
                    lo = lo + nsz + nsz;
                    sz = sz - nsz;
                }
            }

            // Choose closest one
            float f1 = P2PlPs((ushort)_PArray[lo]) - plps;
            float f2 = plps - P2PlPs((ushort)_PArray[lo + 2]);

            return (byte)((f1 < f2) ? lo : lo + 2);
        }

        #endregion IDataCoder Implementation

        public int DecodeSub(ref byte ctx, uint z)
        {
            int bit = ctx & 1;
#if ZCODER
            if (z >= 0x8000)
                z = 0x4000 + (z>>1);
#else
            uint d = 0x6000 + ((z + _AValue) >> 2);
            if (z > d) 
                z = d;
#endif

              /* Test MPS/LPS */
            if (z > _Code)
            {
                /* LPS branch */
                z = 0x10000 - z;
                _AValue = _AValue + z;
                _Code = _Code + z;
                /* LPS adaptation */
                ctx = _Down[ctx];
                /* LPS renormalization */
                int shift = FFZ(_AValue);
                _SCount -= (byte)shift;
                _AValue = (ushort)(_AValue << shift);
                _Code = (ushort)(_Code << shift) | ((_Buffer >> _SCount) & ((1u << shift) - 1));
#if ZPCODEC_BITCOUNT
                bitcount += shift;
#endif
                if (_SCount < 16)
                    Preload();
                /* Adjust fence */
                _Fence = _Code;
                if (_Code >= 0x8000)
                    _Fence = 0x7fff;
                return bit ^ 1;
            }
            else
            {
                /* MPS adaptation */
                if (_AValue >= _MArray[ctx])
                    ctx = _Up[ctx];
                /* MPS renormalization */
                _SCount -= 1;
                _AValue = (ushort)(z << 1);
                _Code = (ushort)(_Code << 1) | ((_Buffer >> _SCount) & 1);
#if ZPCODEC_BITCOUNT
                _bitcount += 1;
#endif
                if (_SCount < 16)
                    Preload();
                /* Adjust fence */
                _Fence = _Code;
                if (_Code >= 0x8000)
                    _Fence = 0x7fff;

                return bit;
            }
        }

        public int DecodeSubNolearn(int mps, uint z)
        {
#if ZCODER
            if (z >= 0x8000)
                z = 0x4000 + (z >> 1);
#else
            uint d = 0x6000 + ((z + _AValue) >> 2);
            if (z > d)
                z = d;
#endif
            /* Test MPS/LPS */
            if (z > _Code)
            {
                /* LPS branch */
                z = 0x10000 - z;
                _AValue += z;
                _Code += z;
                /* LPS renormalization */
                int shift = FFZ(_AValue);
                _SCount -= (byte)shift;
                _AValue = (ushort)(_AValue << shift);
                _Code = (ushort)((int)_Code << shift) | ((_Buffer >> _SCount) & ((1u << shift) - 1));
#if ZPCODEC_BITCOUNT
                _bitcount += shift;
#endif
                if (_SCount < 16)
                    Preload();
                /* Adjust fence */
                _Fence = _Code;
                if (_Code >= 0x8000)
                    _Fence = 0x7fff;
                return mps ^ 1;
            }
            else
            {
                /* MPS renormalization */
                _SCount -= 1;
                _AValue = (ushort)(z << 1);
                _Code = (ushort)(_Code << 1) | ((_Buffer >> _SCount) & 1);
#if ZPCODEC_BITCOUNT
                _bitcount += 1;
#endif
                if (_SCount < 16)
                    Preload();
                /* Adjust fence */
                _Fence = _Code;
                if (_Code >= 0x8000)
                    _Fence = 0x7fff;
                return mps;
            }
        }

        public int DecodeSubSimple(int mps, uint z)
        {
            /* Test MPS/LPS */
            if (z > _Code)
            {
                /* LPS branch */
                z = 0x10000 - z;
                _AValue += z;
                _Code += z;
                /* LPS renormalization */
                int shift = FFZ(_AValue);
                _SCount -= (byte)shift;
                _AValue = (ushort)(_AValue << shift);
                _Code = (ushort)(_Code << shift) | ((_Buffer >> _SCount) & ((1u << shift) - 1));
#if ZPCODEC_BITCOUNT
                _bitcount += shift;
#endif
                if (_SCount < 16)
                    Preload();
                /* Adjust fence */
                _Fence = _Code;
                if (_Code >= 0x8000)
                    _Fence = 0x7fff;
                return mps ^ 1;
            }
            else
            {
                /* MPS renormalization */
                _SCount -= 1;
                _AValue = (ushort)(z << 1);
                _Code = (ushort)(_Code << 1) | ((_Buffer >> _SCount) & 1);
#if ZPCODEC_BITCOUNT
                _bitcount += 1;
#endif
                if (_SCount < 16)
                    Preload();
                /* Adjust fence */
                _Fence = _Code;
                if (_Code >= 0x8000)
                    _Fence = 0x7fff;
                return mps;
            }
        }

        public void Flush()
        {
            if (_Subend > 0x8000)
                _Subend = 0x10000;
            else if (_Subend > 0)
                _Subend = 0x8000;
            
            while (_Buffer != 0xffffff || _Subend != 0)
            {
                Zemit((int)(1 - (_Subend >> 15)));
                _Subend = (_Subend << 1) & 0xffff;
            }

            Outbit(1);

            while (_NRun-- > 0)
                Outbit(0);

            _NRun = 0;
            
            while (_SCount > 0)
                Outbit(1);

            _Delay = 0xff;
        }

        internal void Outbit(int bit)
        {
            if (_Delay > 0)
            {
                if (_Delay < 0xff)
                    _Delay -= 1;
            }
            else
            {
                _ZByte =(byte) ((_ZByte << 1) | bit);
                if (++_SCount == 8)
                {
                    if (Encoding)
                    {
                        DataStream.WriteByte((byte)_ZByte);
                        _SCount = 0;
                        _ZByte = 0;
                    }
                    else
                    {
                        throw new InvalidOperationException($"{nameof(ZPCodec)} is not in encoding mode.");
                    }
                }
            }
        }

        internal void Zemit(int b)
        {
            _Buffer = (_Buffer << 1) + (uint)b;
            b = (int) (_Buffer >> 24);
            _Buffer = (_Buffer & 0xffffff);

            switch (b)
            {
                case 1:
                    Outbit(1);
                    while (_NRun-- > 0)
                        Outbit(0);
                    _NRun = 0;
                    break;
                case 0xff:
                    Outbit(0);
                    while (_NRun-- > 0)
                        Outbit(1);
                    _NRun = 0;
                    break;
                case 0:
                    _NRun += 1;
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected value of variable b during execution: {b}");
            }
#if ZPCODEC_BITCOUNT
            _bitcount += 1;
#endif
        }

        internal void EncodeMps(ref byte ctx, uint z)
        {
            /* Avoid interval reversion */
#if ZCODER
            if (z >= 0x8000)
                z = 0x4000 + (z >> 1);
#else
            uint d = 0x6000 + ((z + (uint)_AValue) >> 2);
            if (z > d)
                z = d;
#endif
            if (_AValue >= _MArray[ctx])
                ctx = _Up[ctx];
            _AValue = z;

            if (_AValue >= 0x8000)
            {
                Zemit((int)(1 - (_Subend >> 15)));
                _Subend = (ushort)(_Subend << 1);
                _AValue = (ushort)(_AValue << 1);
            }
        }

        internal void EncodeLps(ref byte ctx, uint z)
        {
#if ZCODER
            if (z >= 0x8000)
                z = 0x4000 + (z >> 1);
#else
            uint d = (uint)(0x6000 + ((z + _AValue) >> 2));
            if (z > d)
                z = d;
#endif
            ctx = _Down[ctx];
            z = 0x10000 - z;
            _Subend += z;
            _AValue += z;
          
            while (_AValue >= 0x8000)
            {
                Zemit((int)(1 - (_Subend >> 15)));
                _Subend = (ushort)(_Subend << 1);
                _AValue = (ushort)(_AValue << 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EncodeMpsSimple(uint z)
        {
            _AValue = z;
            
            if (_AValue >= 0x8000)
            {
                Zemit((int)(1 - (_Subend >> 15)));
                _Subend = (ushort)(_Subend << 1);
                _AValue = (ushort)(_AValue << 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EncodeLpsSimple(uint z)
        {
            z = 0x10000 - z;
            _Subend += z;
            _AValue += z;
            
            while (_AValue >= 0x8000)
            {
                Zemit((int)(1 - (_Subend >> 15)));
                _Subend = (ushort)(_Subend << 1);
                _AValue = (ushort)(_AValue << 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EncodeMpsNolearn(uint z)
        {
#if ZCODER
            if (z >= 0x8000)
                z = 0x4000 + (z >> 1);
#else
            uint d = 0x6000 + ((z + (uint)_AValue) >> 2);
            if (z > d)
                z = d;
#endif
            _AValue = z;

            while (_AValue >= 0x8000)
            {
                Zemit((int)(1 - (_Subend >> 15)));
                _Subend = (ushort)(_Subend << 1);
                _AValue = (ushort)(_AValue << 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EncodeLpsNolearn(uint z)
        {
#if ZCODER
            if (z >= 0x8000)
                z = 0x4000 + (z >> 1);
#else
            uint d = 0x6000 + ((z + (uint)_AValue) >> 2);
            if (z > d)
                z = d;
#endif
            z = 0x10000 - z;
            _Subend += z;
            _AValue += z;

            while (_AValue >= 0x8000)
            {
                Zemit((int)(1 - (_Subend >> 15)));
                _Subend = (ushort)(_Subend << 1);
                _AValue = (ushort)(_AValue << 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float P2PlPs(ushort p)
        {
            float fplps;
            float fp = (float)(p) / (float)(0x10000);
            const float log2 = (float)0.69314718055994530942;
#if ZCODER
            fplps = fp - (fp + 0.5) * Math.Log(fp + 0.5) + (fp - 0.5) * log2;
#else
            if (fp <= (1.0 / 6.0))
                fplps = fp * 2 * log2;
            else
                fplps = (float)((1.5 * fp - 0.25) - (1.5 * fp + 0.25) * Math.Log(1.5 * fp + 0.25) + (0.5 * fp - 0.25) * log2);
#endif
            return fplps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FFZ(uint x)
        {
            return ((unchecked((int)0xffffffffL) & x) < 65280L) ? _Ffzt[0xff & (x >> 8)] : (_Ffzt[0xff & x] + 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ZPCodec Initializa(Stream inputStream)
        {
            DataStream = inputStream;
            if (!Encoding)
                DecoderInitialize();
            else
                EncoderInitialize();
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EncoderInitialize()
        {
            _Delay = 25;
            _Buffer = 0xffffff;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Preload()
        {
            ushort scount = _SCount;
            short zByte = _ZByte;
            uint buffer = _Buffer;

            for (; scount <= 24; scount += 8)
            {
                zByte = -1;
                zByte = (short)DataStream.ReadByte();

                if (zByte == -1)
                {
                    zByte = 255;

                    if (--_Delay < 1)
                        throw new IOException("EOF");
                }
                buffer = (buffer << 8) | (byte) zByte;
            }

            _SCount = (byte )scount;
            _ZByte = (byte) zByte;
            _Buffer = buffer;
        }

        internal void DecoderInitialize()
        {
            _Code = 0xff00;

            try
            {
                _Code &= (uint)(DataStream.ReadByte() << 8);
                _ZByte = (byte)(0xff & DataStream.ReadByte());
            }
            catch (IOException)
            {
                _ZByte = 255;
            }

            _Code |= _ZByte;
            _Delay = 25;
            _SCount = 0;
            Preload();
            _Fence = _Code;

            if (_Code >= 0x8000)
                _Fence = 0x7fff;
        }

        /// <summary>
        /// Builds the default version of the ZP Table
        /// </summary>
        /// <returns></returns>
        internal ZPTable[] CreateDefaultTable()
        {
            return new[] 
            {
                       
#if !ZCODER
                new ZPTable( 0x8000,  0x0000,  84, 145 ),    /* 000: p=0.500000 (    0,    0) */
                new ZPTable( 0x8000,  0x0000,   3,   4 ),    /* 001: p=0.500000 (    0,    0) */
                new ZPTable( 0x8000,  0x0000,   4,   3 ),    /* 002: p=0.500000 (    0,    0) */
                new ZPTable( 0x6bbd,  0x10a5,   5,   1 ),    /* 003: p=0.465226 (    0,    0) */
                new ZPTable( 0x6bbd,  0x10a5,   6,   2 ),    /* 004: p=0.465226 (    0,    0) */
                new ZPTable( 0x5d45,  0x1f28,   7,   3 ),    /* 005: p=0.430708 (    0,    0) */
                new ZPTable( 0x5d45,  0x1f28,   8,   4 ),    /* 006: p=0.430708 (    0,    0) */
                new ZPTable( 0x51b9,  0x2bd3,   9,   5 ),    /* 007: p=0.396718 (    0,    0) */
                new ZPTable( 0x51b9,  0x2bd3,  10,   6 ),    /* 008: p=0.396718 (    0,    0) */
                new ZPTable( 0x4813,  0x36e3,  11,   7 ),    /* 009: p=0.363535 (    0,    0) */
                new ZPTable( 0x4813,  0x36e3,  12,   8 ),    /* 010: p=0.363535 (    0,    0) */
                new ZPTable( 0x3fd5,  0x408c,  13,   9 ),    /* 011: p=0.331418 (    0,    0) */
                new ZPTable( 0x3fd5,  0x408c,  14,  10 ),    /* 012: p=0.331418 (    0,    0) */
                new ZPTable( 0x38b1,  0x48fd,  15,  11 ),    /* 013: p=0.300585 (    0,    0) */
                new ZPTable( 0x38b1,  0x48fd,  16,  12 ),    /* 014: p=0.300585 (    0,    0) */
                new ZPTable( 0x3275,  0x505d,  17,  13 ),    /* 015: p=0.271213 (    0,    0) */
                new ZPTable( 0x3275,  0x505d,  18,  14 ),    /* 016: p=0.271213 (    0,    0) */
                new ZPTable( 0x2cfd,  0x56d0,  19,  15 ),    /* 017: p=0.243438 (    0,    0) */
                new ZPTable( 0x2cfd,  0x56d0,  20,  16 ),    /* 018: p=0.243438 (    0,    0) */
                new ZPTable( 0x2825,  0x5c71,  21,  17 ),    /* 019: p=0.217391 (    0,    0) */
                new ZPTable( 0x2825,  0x5c71,  22,  18 ),    /* 020: p=0.217391 (    0,    0) */
                new ZPTable( 0x23ab,  0x615b,  23,  19 ),    /* 021: p=0.193150 (    0,    0) */
                new ZPTable( 0x23ab,  0x615b,  24,  20 ),    /* 022: p=0.193150 (    0,    0) */
                new ZPTable( 0x1f87,  0x65a5,  25,  21 ),    /* 023: p=0.170728 (    0,    0) */
                new ZPTable( 0x1f87,  0x65a5,  26,  22 ),    /* 024: p=0.170728 (    0,    0) */
                new ZPTable( 0x1bbb,  0x6962,  27,  23 ),    /* 025: p=0.150158 (    0,    0) */
                new ZPTable( 0x1bbb,  0x6962,  28,  24 ),    /* 026: p=0.150158 (    0,    0) */
                new ZPTable( 0x1845,  0x6ca2,  29,  25 ),    /* 027: p=0.131418 (    0,    0) */
                new ZPTable( 0x1845,  0x6ca2,  30,  26 ),    /* 028: p=0.131418 (    0,    0) */
                new ZPTable( 0x1523,  0x6f74,  31,  27 ),    /* 029: p=0.114460 (    0,    0) */
                new ZPTable( 0x1523,  0x6f74,  32,  28 ),    /* 030: p=0.114460 (    0,    0) */
                new ZPTable( 0x1253,  0x71e6,  33,  29 ),    /* 031: p=0.099230 (    0,    0) */
                new ZPTable( 0x1253,  0x71e6,  34,  30 ),    /* 032: p=0.099230 (    0,    0) */
                new ZPTable( 0x0fcf,  0x7404,  35,  31 ),    /* 033: p=0.085611 (    0,    0) */
                new ZPTable( 0x0fcf,  0x7404,  36,  32 ),    /* 034: p=0.085611 (    0,    0) */
                new ZPTable( 0x0d95,  0x75d6,  37,  33 ),    /* 035: p=0.073550 (    0,    0) */
                new ZPTable( 0x0d95,  0x75d6,  38,  34 ),    /* 036: p=0.073550 (    0,    0) */
                new ZPTable( 0x0b9d,  0x7768,  39,  35 ),    /* 037: p=0.062888 (    0,    0) */
                new ZPTable( 0x0b9d,  0x7768,  40,  36 ),    /* 038: p=0.062888 (    0,    0) */
                new ZPTable( 0x09e3,  0x78c2,  41,  37 ),    /* 039: p=0.053539 (    0,    0) */
                new ZPTable( 0x09e3,  0x78c2,  42,  38 ),    /* 040: p=0.053539 (    0,    0) */
                new ZPTable( 0x0861,  0x79ea,  43,  39 ),    /* 041: p=0.045365 (    0,    0) */
                new ZPTable( 0x0861,  0x79ea,  44,  40 ),    /* 042: p=0.045365 (    0,    0) */
                new ZPTable( 0x0711,  0x7ae7,  45,  41 ),    /* 043: p=0.038272 (    0,    0) */
                new ZPTable( 0x0711,  0x7ae7,  46,  42 ),    /* 044: p=0.038272 (    0,    0) */
                new ZPTable( 0x05f1,  0x7bbe,  47,  43 ),    /* 045: p=0.032174 (    0,    0) */
                new ZPTable( 0x05f1,  0x7bbe,  48,  44 ),    /* 046: p=0.032174 (    0,    0) */
                new ZPTable( 0x04f9,  0x7c75,  49,  45 ),    /* 047: p=0.026928 (    0,    0) */
                new ZPTable( 0x04f9,  0x7c75,  50,  46 ),    /* 048: p=0.026928 (    0,    0) */
                new ZPTable( 0x0425,  0x7d0f,  51,  47 ),    /* 049: p=0.022444 (    0,    0) */
                new ZPTable( 0x0425,  0x7d0f,  52,  48 ),    /* 050: p=0.022444 (    0,    0) */
                new ZPTable( 0x0371,  0x7d91,  53,  49 ),    /* 051: p=0.018636 (    0,    0) */
                new ZPTable( 0x0371,  0x7d91,  54,  50 ),    /* 052: p=0.018636 (    0,    0) */
                new ZPTable( 0x02d9,  0x7dfe,  55,  51 ),    /* 053: p=0.015421 (    0,    0) */
                new ZPTable( 0x02d9,  0x7dfe,  56,  52 ),    /* 054: p=0.015421 (    0,    0) */
                new ZPTable( 0x0259,  0x7e5a,  57,  53 ),    /* 055: p=0.012713 (    0,    0) */
                new ZPTable( 0x0259,  0x7e5a,  58,  54 ),    /* 056: p=0.012713 (    0,    0) */
                new ZPTable( 0x01ed,  0x7ea6,  59,  55 ),    /* 057: p=0.010419 (    0,    0) */
                new ZPTable( 0x01ed,  0x7ea6,  60,  56 ),    /* 058: p=0.010419 (    0,    0) */
                new ZPTable( 0x0193,  0x7ee6,  61,  57 ),    /* 059: p=0.008525 (    0,    0) */
                new ZPTable( 0x0193,  0x7ee6,  62,  58 ),    /* 060: p=0.008525 (    0,    0) */
                new ZPTable( 0x0149,  0x7f1a,  63,  59 ),    /* 061: p=0.006959 (    0,    0) */
                new ZPTable( 0x0149,  0x7f1a,  64,  60 ),    /* 062: p=0.006959 (    0,    0) */
                new ZPTable( 0x010b,  0x7f45,  65,  61 ),    /* 063: p=0.005648 (    0,    0) */
                new ZPTable( 0x010b,  0x7f45,  66,  62 ),    /* 064: p=0.005648 (    0,    0) */
                new ZPTable( 0x00d5,  0x7f6b,  67,  63 ),    /* 065: p=0.004506 (    0,    0) */
                new ZPTable( 0x00d5,  0x7f6b,  68,  64 ),    /* 066: p=0.004506 (    0,    0) */
                new ZPTable( 0x00a5,  0x7f8d,  69,  65 ),    /* 067: p=0.003480 (    0,    0) */
                new ZPTable( 0x00a5,  0x7f8d,  70,  66 ),    /* 068: p=0.003480 (    0,    0) */
                new ZPTable( 0x007b,  0x7faa,  71,  67 ),    /* 069: p=0.002602 (    0,    0) */
                new ZPTable( 0x007b,  0x7faa,  72,  68 ),    /* 070: p=0.002602 (    0,    0) */
                new ZPTable( 0x0057,  0x7fc3,  73,  69 ),    /* 071: p=0.001843 (    0,    0) */
                new ZPTable( 0x0057,  0x7fc3,  74,  70 ),    /* 072: p=0.001843 (    0,    0) */
                new ZPTable( 0x003b,  0x7fd7,  75,  71 ),    /* 073: p=0.001248 (    0,    0) */
                new ZPTable( 0x003b,  0x7fd7,  76,  72 ),    /* 074: p=0.001248 (    0,    0) */
                new ZPTable( 0x0023,  0x7fe7,  77,  73 ),    /* 075: p=0.000749 (    0,    0) */
                new ZPTable( 0x0023,  0x7fe7,  78,  74 ),    /* 076: p=0.000749 (    0,    0) */
                new ZPTable( 0x0013,  0x7ff2,  79,  75 ),    /* 077: p=0.000402 (    0,    0) */
                new ZPTable( 0x0013,  0x7ff2,  80,  76 ),    /* 078: p=0.000402 (    0,    0) */
                new ZPTable( 0x0007,  0x7ffa,  81,  77 ),    /* 079: p=0.000153 (    0,    0) */
                new ZPTable( 0x0007,  0x7ffa,  82,  78 ),    /* 080: p=0.000153 (    0,    0) */
                new ZPTable( 0x0001,  0x7fff,  81,  79 ),    /* 081: p=0.000027 (    0,    0) */
                new ZPTable( 0x0001,  0x7fff,  82,  80 ),    /* 082: p=0.000027 (    0,    0) */
                new ZPTable( 0x5695,  0x0000,   9,  85 ),    /* 083: p=0.411764 (    2,    3) */
                new ZPTable( 0x24ee,  0x0000,  86, 226 ),    /* 084: p=0.199988 (    1,    0) */
                new ZPTable( 0x8000,  0x0000,   5,   6 ),    /* 085: p=0.500000 (    3,    3) */
                new ZPTable( 0x0d30,  0x0000,  88, 176 ),    /* 086: p=0.071422 (    4,    0) */
                new ZPTable( 0x481a,  0x0000,  89, 143 ),    /* 087: p=0.363634 (    1,    2) */
                new ZPTable( 0x0481,  0x0000,  90, 138 ),    /* 088: p=0.024388 (   13,    0) */
                new ZPTable( 0x3579,  0x0000,  91, 141 ),    /* 089: p=0.285711 (    1,    3) */
                new ZPTable( 0x017a,  0x0000,  92, 112 ),    /* 090: p=0.007999 (   41,    0) */
                new ZPTable( 0x24ef,  0x0000,  93, 135 ),    /* 091: p=0.199997 (    1,    5) */
                new ZPTable( 0x007b,  0x0000,  94, 104 ),    /* 092: p=0.002611 (  127,    0) */
                new ZPTable( 0x1978,  0x0000,  95, 133 ),    /* 093: p=0.137929 (    1,    8) */
                new ZPTable( 0x0028,  0x0000,  96, 100 ),    /* 094: p=0.000849 (  392,    0) */
                new ZPTable( 0x10ca,  0x0000,  97, 129 ),    /* 095: p=0.090907 (    1,   13) */
                new ZPTable( 0x000d,  0x0000,  82,  98 ),    /* 096: p=0.000276 ( 1208,    0) */
                new ZPTable( 0x0b5d,  0x0000,  99, 127 ),    /* 097: p=0.061537 (    1,   20) */
                new ZPTable( 0x0034,  0x0000,  76,  72 ),    /* 098: p=0.001102 ( 1208,    1) */
                new ZPTable( 0x078a,  0x0000, 101, 125 ),    /* 099: p=0.040815 (    1,   31) */
                new ZPTable( 0x00a0,  0x0000,  70, 102 ),    /* 100: p=0.003387 (  392,    1) */
                new ZPTable( 0x050f,  0x0000, 103, 123 ),    /* 101: p=0.027397 (    1,   47) */
                new ZPTable( 0x0117,  0x0000,  66,  60 ),    /* 102: p=0.005912 (  392,    2) */
                new ZPTable( 0x0358,  0x0000, 105, 121 ),    /* 103: p=0.018099 (    1,   72) */
                new ZPTable( 0x01ea,  0x0000, 106, 110 ),    /* 104: p=0.010362 (  127,    1) */
                new ZPTable( 0x0234,  0x0000, 107, 119 ),    /* 105: p=0.011940 (    1,  110) */
                new ZPTable( 0x0144,  0x0000,  66, 108 ),    /* 106: p=0.006849 (  193,    1) */
                new ZPTable( 0x0173,  0x0000, 109, 117 ),    /* 107: p=0.007858 (    1,  168) */
                new ZPTable( 0x0234,  0x0000,  60,  54 ),    /* 108: p=0.011925 (  193,    2) */
                new ZPTable( 0x00f5,  0x0000, 111, 115 ),    /* 109: p=0.005175 (    1,  256) */
                new ZPTable( 0x0353,  0x0000,  56,  48 ),    /* 110: p=0.017995 (  127,    2) */
                new ZPTable( 0x00a1,  0x0000,  69, 113 ),    /* 111: p=0.003413 (    1,  389) */
                new ZPTable( 0x05c5,  0x0000, 114, 134 ),    /* 112: p=0.031249 (   41,    1) */
                new ZPTable( 0x011a,  0x0000,  65,  59 ),    /* 113: p=0.005957 (    2,  389) */
                new ZPTable( 0x03cf,  0x0000, 116, 132 ),    /* 114: p=0.020618 (   63,    1) */
                new ZPTable( 0x01aa,  0x0000,  61,  55 ),    /* 115: p=0.009020 (    2,  256) */
                new ZPTable( 0x0285,  0x0000, 118, 130 ),    /* 116: p=0.013652 (   96,    1) */
                new ZPTable( 0x0286,  0x0000,  57,  51 ),    /* 117: p=0.013672 (    2,  168) */
                new ZPTable( 0x01ab,  0x0000, 120, 128 ),    /* 118: p=0.009029 (  146,    1) */
                new ZPTable( 0x03d3,  0x0000,  53,  47 ),    /* 119: p=0.020710 (    2,  110) */
                new ZPTable( 0x011a,  0x0000, 122, 126 ),    /* 120: p=0.005961 (  222,    1) */
                new ZPTable( 0x05c5,  0x0000,  49,  41 ),    /* 121: p=0.031250 (    2,   72) */
                new ZPTable( 0x00ba,  0x0000, 124,  62 ),    /* 122: p=0.003925 (  338,    1) */
                new ZPTable( 0x08ad,  0x0000,  43,  37 ),    /* 123: p=0.046979 (    2,   47) */
                new ZPTable( 0x007a,  0x0000,  72,  66 ),    /* 124: p=0.002586 (  514,    1) */
                new ZPTable( 0x0ccc,  0x0000,  39,  31 ),    /* 125: p=0.069306 (    2,   31) */
                new ZPTable( 0x01eb,  0x0000,  60,  54 ),    /* 126: p=0.010386 (  222,    2) */
                new ZPTable( 0x1302,  0x0000,  33,  25 ),    /* 127: p=0.102940 (    2,   20) */
                new ZPTable( 0x02e6,  0x0000,  56,  50 ),    /* 128: p=0.015695 (  146,    2) */
                new ZPTable( 0x1b81,  0x0000,  29, 131 ),    /* 129: p=0.148935 (    2,   13) */
                new ZPTable( 0x045e,  0x0000,  52,  46 ),    /* 130: p=0.023648 (   96,    2) */
                new ZPTable( 0x24ef,  0x0000,  23,  17 ),    /* 131: p=0.199999 (    3,   13) */
                new ZPTable( 0x0690,  0x0000,  48,  40 ),    /* 132: p=0.035533 (   63,    2) */
                new ZPTable( 0x2865,  0x0000,  23,  15 ),    /* 133: p=0.218748 (    2,    8) */
                new ZPTable( 0x09de,  0x0000,  42, 136 ),    /* 134: p=0.053434 (   41,    2) */
                new ZPTable( 0x3987,  0x0000, 137,   7 ),    /* 135: p=0.304346 (    2,    5) */
                new ZPTable( 0x0dc8,  0x0000,  38,  32 ),    /* 136: p=0.074626 (   41,    3) */
                new ZPTable( 0x2c99,  0x0000,  21, 139 ),    /* 137: p=0.241378 (    2,    7) */
                new ZPTable( 0x10ca,  0x0000, 140, 172 ),    /* 138: p=0.090907 (   13,    1) */
                new ZPTable( 0x3b5f,  0x0000,  15,   9 ),    /* 139: p=0.312499 (    3,    7) */
                new ZPTable( 0x0b5d,  0x0000, 142, 170 ),    /* 140: p=0.061537 (   20,    1) */
                new ZPTable( 0x5695,  0x0000,   9,  85 ),    /* 141: p=0.411764 (    2,    3) */
                new ZPTable( 0x078a,  0x0000, 144, 168 ),    /* 142: p=0.040815 (   31,    1) */
                new ZPTable( 0x8000,  0x0000, 141, 248 ),    /* 143: p=0.500000 (    2,    2) */
                new ZPTable( 0x050f,  0x0000, 146, 166 ),    /* 144: p=0.027397 (   47,    1) */
                new ZPTable( 0x24ee,  0x0000, 147, 247 ),    /* 145: p=0.199988 (    0,    1) */
                new ZPTable( 0x0358,  0x0000, 148, 164 ),    /* 146: p=0.018099 (   72,    1) */
                new ZPTable( 0x0d30,  0x0000, 149, 197 ),    /* 147: p=0.071422 (    0,    4) */
                new ZPTable( 0x0234,  0x0000, 150, 162 ),    /* 148: p=0.011940 (  110,    1) */
                new ZPTable( 0x0481,  0x0000, 151,  95 ),    /* 149: p=0.024388 (    0,   13) */
                new ZPTable( 0x0173,  0x0000, 152, 160 ),    /* 150: p=0.007858 (  168,    1) */
                new ZPTable( 0x017a,  0x0000, 153, 173 ),    /* 151: p=0.007999 (    0,   41) */
                new ZPTable( 0x00f5,  0x0000, 154, 158 ),    /* 152: p=0.005175 (  256,    1) */
                new ZPTable( 0x007b,  0x0000, 155, 165 ),    /* 153: p=0.002611 (    0,  127) */
                new ZPTable( 0x00a1,  0x0000,  70, 156 ),    /* 154: p=0.003413 (  389,    1) */
                new ZPTable( 0x0028,  0x0000, 157, 161 ),    /* 155: p=0.000849 (    0,  392) */
                new ZPTable( 0x011a,  0x0000,  66,  60 ),    /* 156: p=0.005957 (  389,    2) */
                new ZPTable( 0x000d,  0x0000,  81, 159 ),    /* 157: p=0.000276 (    0, 1208) */
                new ZPTable( 0x01aa,  0x0000,  62,  56 ),    /* 158: p=0.009020 (  256,    2) */
                new ZPTable( 0x0034,  0x0000,  75,  71 ),    /* 159: p=0.001102 (    1, 1208) */
                new ZPTable( 0x0286,  0x0000,  58,  52 ),    /* 160: p=0.013672 (  168,    2) */
                new ZPTable( 0x00a0,  0x0000,  69, 163 ),    /* 161: p=0.003387 (    1,  392) */
                new ZPTable( 0x03d3,  0x0000,  54,  48 ),    /* 162: p=0.020710 (  110,    2) */
                new ZPTable( 0x0117,  0x0000,  65,  59 ),    /* 163: p=0.005912 (    2,  392) */
                new ZPTable( 0x05c5,  0x0000,  50,  42 ),    /* 164: p=0.031250 (   72,    2) */
                new ZPTable( 0x01ea,  0x0000, 167, 171 ),    /* 165: p=0.010362 (    1,  127) */
                new ZPTable( 0x08ad,  0x0000,  44,  38 ),    /* 166: p=0.046979 (   47,    2) */
                new ZPTable( 0x0144,  0x0000,  65, 169 ),    /* 167: p=0.006849 (    1,  193) */
                new ZPTable( 0x0ccc,  0x0000,  40,  32 ),    /* 168: p=0.069306 (   31,    2) */
                new ZPTable( 0x0234,  0x0000,  59,  53 ),    /* 169: p=0.011925 (    2,  193) */
                new ZPTable( 0x1302,  0x0000,  34,  26 ),    /* 170: p=0.102940 (   20,    2) */
                new ZPTable( 0x0353,  0x0000,  55,  47 ),    /* 171: p=0.017995 (    2,  127) */
                new ZPTable( 0x1b81,  0x0000,  30, 174 ),    /* 172: p=0.148935 (   13,    2) */
                new ZPTable( 0x05c5,  0x0000, 175, 193 ),    /* 173: p=0.031249 (    1,   41) */
                new ZPTable( 0x24ef,  0x0000,  24,  18 ),    /* 174: p=0.199999 (   13,    3) */
                new ZPTable( 0x03cf,  0x0000, 177, 191 ),    /* 175: p=0.020618 (    1,   63) */
                new ZPTable( 0x2b74,  0x0000, 178, 222 ),    /* 176: p=0.235291 (    4,    1) */
                new ZPTable( 0x0285,  0x0000, 179, 189 ),    /* 177: p=0.013652 (    1,   96) */
                new ZPTable( 0x201d,  0x0000, 180, 218 ),    /* 178: p=0.173910 (    6,    1) */
                new ZPTable( 0x01ab,  0x0000, 181, 187 ),    /* 179: p=0.009029 (    1,  146) */
                new ZPTable( 0x1715,  0x0000, 182, 216 ),    /* 180: p=0.124998 (    9,    1) */
                new ZPTable( 0x011a,  0x0000, 183, 185 ),    /* 181: p=0.005961 (    1,  222) */
                new ZPTable( 0x0fb7,  0x0000, 184, 214 ),    /* 182: p=0.085105 (   14,    1) */
                new ZPTable( 0x00ba,  0x0000,  69,  61 ),    /* 183: p=0.003925 (    1,  338) */
                new ZPTable( 0x0a67,  0x0000, 186, 212 ),    /* 184: p=0.056337 (   22,    1) */
                new ZPTable( 0x01eb,  0x0000,  59,  53 ),    /* 185: p=0.010386 (    2,  222) */
                new ZPTable( 0x06e7,  0x0000, 188, 210 ),    /* 186: p=0.037382 (   34,    1) */
                new ZPTable( 0x02e6,  0x0000,  55,  49 ),    /* 187: p=0.015695 (    2,  146) */
                new ZPTable( 0x0496,  0x0000, 190, 208 ),    /* 188: p=0.024844 (   52,    1) */
                new ZPTable( 0x045e,  0x0000,  51,  45 ),    /* 189: p=0.023648 (    2,   96) */
                new ZPTable( 0x030d,  0x0000, 192, 206 ),    /* 190: p=0.016529 (   79,    1) */
                new ZPTable( 0x0690,  0x0000,  47,  39 ),    /* 191: p=0.035533 (    2,   63) */
                new ZPTable( 0x0206,  0x0000, 194, 204 ),    /* 192: p=0.010959 (  120,    1) */
                new ZPTable( 0x09de,  0x0000,  41, 195 ),    /* 193: p=0.053434 (    2,   41) */
                new ZPTable( 0x0155,  0x0000, 196, 202 ),    /* 194: p=0.007220 (  183,    1) */
                new ZPTable( 0x0dc8,  0x0000,  37,  31 ),    /* 195: p=0.074626 (    3,   41) */
                new ZPTable( 0x00e1,  0x0000, 198, 200 ),    /* 196: p=0.004750 (  279,    1) */
                new ZPTable( 0x2b74,  0x0000, 199, 243 ),    /* 197: p=0.235291 (    1,    4) */
                new ZPTable( 0x0094,  0x0000,  72,  64 ),    /* 198: p=0.003132 (  424,    1) */
                new ZPTable( 0x201d,  0x0000, 201, 239 ),    /* 199: p=0.173910 (    1,    6) */
                new ZPTable( 0x0188,  0x0000,  62,  56 ),    /* 200: p=0.008284 (  279,    2) */
                new ZPTable( 0x1715,  0x0000, 203, 237 ),    /* 201: p=0.124998 (    1,    9) */
                new ZPTable( 0x0252,  0x0000,  58,  52 ),    /* 202: p=0.012567 (  183,    2) */
                new ZPTable( 0x0fb7,  0x0000, 205, 235 ),    /* 203: p=0.085105 (    1,   14) */
                new ZPTable( 0x0383,  0x0000,  54,  48 ),    /* 204: p=0.019021 (  120,    2) */
                new ZPTable( 0x0a67,  0x0000, 207, 233 ),    /* 205: p=0.056337 (    1,   22) */
                new ZPTable( 0x0547,  0x0000,  50,  44 ),    /* 206: p=0.028571 (   79,    2) */
                new ZPTable( 0x06e7,  0x0000, 209, 231 ),    /* 207: p=0.037382 (    1,   34) */
                new ZPTable( 0x07e2,  0x0000,  46,  38 ),    /* 208: p=0.042682 (   52,    2) */
                new ZPTable( 0x0496,  0x0000, 211, 229 ),    /* 209: p=0.024844 (    1,   52) */
                new ZPTable( 0x0bc0,  0x0000,  40,  34 ),    /* 210: p=0.063636 (   34,    2) */
                new ZPTable( 0x030d,  0x0000, 213, 227 ),    /* 211: p=0.016529 (    1,   79) */
                new ZPTable( 0x1178,  0x0000,  36,  28 ),    /* 212: p=0.094593 (   22,    2) */
                new ZPTable( 0x0206,  0x0000, 215, 225 ),    /* 213: p=0.010959 (    1,  120) */
                new ZPTable( 0x19da,  0x0000,  30,  22 ),    /* 214: p=0.139999 (   14,    2) */
                new ZPTable( 0x0155,  0x0000, 217, 223 ),    /* 215: p=0.007220 (    1,  183) */
                new ZPTable( 0x24ef,  0x0000,  26,  16 ),    /* 216: p=0.199998 (    9,    2) */
                new ZPTable( 0x00e1,  0x0000, 219, 221 ),    /* 217: p=0.004750 (    1,  279) */
                new ZPTable( 0x320e,  0x0000,  20, 220 ),    /* 218: p=0.269229 (    6,    2) */
                new ZPTable( 0x0094,  0x0000,  71,  63 ),    /* 219: p=0.003132 (    1,  424) */
                new ZPTable( 0x432a,  0x0000,  14,   8 ),    /* 220: p=0.344827 (    6,    3) */
                new ZPTable( 0x0188,  0x0000,  61,  55 ),    /* 221: p=0.008284 (    2,  279) */
                new ZPTable( 0x447d,  0x0000,  14, 224 ),    /* 222: p=0.349998 (    4,    2) */
                new ZPTable( 0x0252,  0x0000,  57,  51 ),    /* 223: p=0.012567 (    2,  183) */
                new ZPTable( 0x5ece,  0x0000,   8,   2 ),    /* 224: p=0.434782 (    4,    3) */
                new ZPTable( 0x0383,  0x0000,  53,  47 ),    /* 225: p=0.019021 (    2,  120) */
                new ZPTable( 0x8000,  0x0000, 228,  87 ),    /* 226: p=0.500000 (    1,    1) */
                new ZPTable( 0x0547,  0x0000,  49,  43 ),    /* 227: p=0.028571 (    2,   79) */
                new ZPTable( 0x481a,  0x0000, 230, 246 ),    /* 228: p=0.363634 (    2,    1) */
                new ZPTable( 0x07e2,  0x0000,  45,  37 ),    /* 229: p=0.042682 (    2,   52) */
                new ZPTable( 0x3579,  0x0000, 232, 244 ),    /* 230: p=0.285711 (    3,    1) */
                new ZPTable( 0x0bc0,  0x0000,  39,  33 ),    /* 231: p=0.063636 (    2,   34) */
                new ZPTable( 0x24ef,  0x0000, 234, 238 ),    /* 232: p=0.199997 (    5,    1) */
                new ZPTable( 0x1178,  0x0000,  35,  27 ),    /* 233: p=0.094593 (    2,   22) */
                new ZPTable( 0x1978,  0x0000, 138, 236 ),    /* 234: p=0.137929 (    8,    1) */
                new ZPTable( 0x19da,  0x0000,  29,  21 ),    /* 235: p=0.139999 (    2,   14) */
                new ZPTable( 0x2865,  0x0000,  24,  16 ),    /* 236: p=0.218748 (    8,    2) */
                new ZPTable( 0x24ef,  0x0000,  25,  15 ),    /* 237: p=0.199998 (    2,    9) */
                new ZPTable( 0x3987,  0x0000, 240,   8 ),    /* 238: p=0.304346 (    5,    2) */
                new ZPTable( 0x320e,  0x0000,  19, 241 ),    /* 239: p=0.269229 (    2,    6) */
                new ZPTable( 0x2c99,  0x0000,  22, 242 ),    /* 240: p=0.241378 (    7,    2) */
                new ZPTable( 0x432a,  0x0000,  13,   7 ),    /* 241: p=0.344827 (    3,    6) */
                new ZPTable( 0x3b5f,  0x0000,  16,  10 ),    /* 242: p=0.312499 (    7,    3) */
                new ZPTable( 0x447d,  0x0000,  13, 245 ),    /* 243: p=0.349998 (    2,    4) */
                new ZPTable( 0x5695,  0x0000,  10,   2 ),    /* 244: p=0.411764 (    3,    2) */
                new ZPTable( 0x5ece,  0x0000,   7,   1 ),    /* 245: p=0.434782 (    3,    4) */
                new ZPTable( 0x8000,  0x0000, 244,  83 ),    /* 246: p=0.500000 (    2,    2) */
                new ZPTable( 0x8000,  0x0000, 249, 250 ),    /* 247: p=0.500000 (    1,    1) */
                new ZPTable( 0x5695,  0x0000,  10,   2 ),    /* 248: p=0.411764 (    3,    2) */
                new ZPTable( 0x481a,  0x0000,  89, 143 ),    /* 249: p=0.363634 (    1,    2) */
                new ZPTable( 0x481a,  0x0000, 230, 246 ),    /* 250: p=0.363634 (    2,    1) */
                new ZPTable( 0x0000,  0x0000, 0, 0 ),
                new ZPTable( 0x0000,  0x0000, 0, 0 ),
                new ZPTable( 0x0000,  0x0000, 0, 0 ),
                new ZPTable( 0x0000,  0x0000, 0, 0 ),
                new ZPTable( 0x0000,  0x0000, 0, 0 ),
#else
                new ZPTable( 0x8000,  0x0000,  84, 139 ),    /* 000: p=0.500000 (    0,    0) */
                new ZPTable( 0x8000,  0x0000,   3,   4 ),    /* 001: p=0.500000 (    0,    0) */
                new ZPTable( 0x8000,  0x0000,   4,   3 ),    /* 002: p=0.500000 (    0,    0) */
                new ZPTable( 0x7399,  0x10a5,   5,   1 ),    /* 003: p=0.465226 (    0,    0) */
                new ZPTable( 0x7399,  0x10a5,   6,   2 ),    /* 004: p=0.465226 (    0,    0) */
                new ZPTable( 0x6813,  0x1f28,   7,   3 ),    /* 005: p=0.430708 (    0,    0) */
                new ZPTable( 0x6813,  0x1f28,   8,   4 ),    /* 006: p=0.430708 (    0,    0) */
                new ZPTable( 0x5d65,  0x2bd3,   9,   5 ),    /* 007: p=0.396718 (    0,    0) */
                new ZPTable( 0x5d65,  0x2bd3,  10,   6 ),    /* 008: p=0.396718 (    0,    0) */
                new ZPTable( 0x5387,  0x36e3,  11,   7 ),    /* 009: p=0.363535 (    0,    0) */
                new ZPTable( 0x5387,  0x36e3,  12,   8 ),    /* 010: p=0.363535 (    0,    0) */
                new ZPTable( 0x4a73,  0x408c,  13,   9 ),    /* 011: p=0.331418 (    0,    0) */
                new ZPTable( 0x4a73,  0x408c,  14,  10 ),    /* 012: p=0.331418 (    0,    0) */
                new ZPTable( 0x421f,  0x48fe,  15,  11 ),    /* 013: p=0.300562 (    0,    0) */
                new ZPTable( 0x421f,  0x48fe,  16,  12 ),    /* 014: p=0.300562 (    0,    0) */
                new ZPTable( 0x3a85,  0x5060,  17,  13 ),    /* 015: p=0.271166 (    0,    0) */
                new ZPTable( 0x3a85,  0x5060,  18,  14 ),    /* 016: p=0.271166 (    0,    0) */
                new ZPTable( 0x339b,  0x56d3,  19,  15 ),    /* 017: p=0.243389 (    0,    0) */
                new ZPTable( 0x339b,  0x56d3,  20,  16 ),    /* 018: p=0.243389 (    0,    0) */
                new ZPTable( 0x2d59,  0x5c73,  21,  17 ),    /* 019: p=0.217351 (    0,    0) */
                new ZPTable( 0x2d59,  0x5c73,  22,  18 ),    /* 020: p=0.217351 (    0,    0) */
                new ZPTable( 0x27b3,  0x615e,  23,  19 ),    /* 021: p=0.193091 (    0,    0) */
                new ZPTable( 0x27b3,  0x615e,  24,  20 ),    /* 022: p=0.193091 (    0,    0) */
                new ZPTable( 0x22a1,  0x65a7,  25,  21 ),    /* 023: p=0.170683 (    0,    0) */
                new ZPTable( 0x22a1,  0x65a7,  26,  22 ),    /* 024: p=0.170683 (    0,    0) */
                new ZPTable( 0x1e19,  0x6963,  27,  23 ),    /* 025: p=0.150134 (    0,    0) */
                new ZPTable( 0x1e19,  0x6963,  28,  24 ),    /* 026: p=0.150134 (    0,    0) */
                new ZPTable( 0x1a0f,  0x6ca3,  29,  25 ),    /* 027: p=0.131397 (    0,    0) */
                new ZPTable( 0x1a0f,  0x6ca3,  30,  26 ),    /* 028: p=0.131397 (    0,    0) */
                new ZPTable( 0x167b,  0x6f75,  31,  27 ),    /* 029: p=0.114441 (    0,    0) */
                new ZPTable( 0x167b,  0x6f75,  32,  28 ),    /* 030: p=0.114441 (    0,    0) */
                new ZPTable( 0x1353,  0x71e6,  33,  29 ),    /* 031: p=0.099214 (    0,    0) */
                new ZPTable( 0x1353,  0x71e6,  34,  30 ),    /* 032: p=0.099214 (    0,    0) */
                new ZPTable( 0x108d,  0x7403,  35,  31 ),    /* 033: p=0.085616 (    0,    0) */
                new ZPTable( 0x108d,  0x7403,  36,  32 ),    /* 034: p=0.085616 (    0,    0) */
                new ZPTable( 0x0e1f,  0x75d7,  37,  33 ),    /* 035: p=0.073525 (    0,    0) */
                new ZPTable( 0x0e1f,  0x75d7,  38,  34 ),    /* 036: p=0.073525 (    0,    0) */
                new ZPTable( 0x0c01,  0x7769,  39,  35 ),    /* 037: p=0.062871 (    0,    0) */
                new ZPTable( 0x0c01,  0x7769,  40,  36 ),    /* 038: p=0.062871 (    0,    0) */
                new ZPTable( 0x0a2b,  0x78c2,  41,  37 ),    /* 039: p=0.053524 (    0,    0) */
                new ZPTable( 0x0a2b,  0x78c2,  42,  38 ),    /* 040: p=0.053524 (    0,    0) */
                new ZPTable( 0x0895,  0x79ea,  43,  39 ),    /* 041: p=0.045374 (    0,    0) */
                new ZPTable( 0x0895,  0x79ea,  44,  40 ),    /* 042: p=0.045374 (    0,    0) */
                new ZPTable( 0x0737,  0x7ae7,  45,  41 ),    /* 043: p=0.038280 (    0,    0) */
                new ZPTable( 0x0737,  0x7ae7,  46,  42 ),    /* 044: p=0.038280 (    0,    0) */
                new ZPTable( 0x060b,  0x7bbe,  47,  43 ),    /* 045: p=0.032175 (    0,    0) */
                new ZPTable( 0x060b,  0x7bbe,  48,  44 ),    /* 046: p=0.032175 (    0,    0) */
                new ZPTable( 0x050b,  0x7c75,  49,  45 ),    /* 047: p=0.026926 (    0,    0) */
                new ZPTable( 0x050b,  0x7c75,  50,  46 ),    /* 048: p=0.026926 (    0,    0) */
                new ZPTable( 0x0431,  0x7d10,  51,  47 ),    /* 049: p=0.022430 (    0,    0) */
                new ZPTable( 0x0431,  0x7d10,  52,  48 ),    /* 050: p=0.022430 (    0,    0) */
                new ZPTable( 0x0379,  0x7d92,  53,  49 ),    /* 051: p=0.018623 (    0,    0) */
                new ZPTable( 0x0379,  0x7d92,  54,  50 ),    /* 052: p=0.018623 (    0,    0) */
                new ZPTable( 0x02dd,  0x7dff,  55,  51 ),    /* 053: p=0.015386 (    0,    0) */
                new ZPTable( 0x02dd,  0x7dff,  56,  52 ),    /* 054: p=0.015386 (    0,    0) */
                new ZPTable( 0x025b,  0x7e5b,  57,  53 ),    /* 055: p=0.012671 (    0,    0) */
                new ZPTable( 0x025b,  0x7e5b,  58,  54 ),    /* 056: p=0.012671 (    0,    0) */
                new ZPTable( 0x01ef,  0x7ea7,  59,  55 ),    /* 057: p=0.010414 (    0,    0) */
                new ZPTable( 0x01ef,  0x7ea7,  60,  56 ),    /* 058: p=0.010414 (    0,    0) */
                new ZPTable( 0x0195,  0x7ee6,  61,  57 ),    /* 059: p=0.008529 (    0,    0) */
                new ZPTable( 0x0195,  0x7ee6,  62,  58 ),    /* 060: p=0.008529 (    0,    0) */
                new ZPTable( 0x0149,  0x7f1b,  63,  59 ),    /* 061: p=0.006935 (    0,    0) */
                new ZPTable( 0x0149,  0x7f1b,  64,  60 ),    /* 062: p=0.006935 (    0,    0) */
                new ZPTable( 0x010b,  0x7f46,  65,  61 ),    /* 063: p=0.005631 (    0,    0) */
                new ZPTable( 0x010b,  0x7f46,  66,  62 ),    /* 064: p=0.005631 (    0,    0) */
                new ZPTable( 0x00d5,  0x7f6c,  67,  63 ),    /* 065: p=0.004495 (    0,    0) */
                new ZPTable( 0x00d5,  0x7f6c,  68,  64 ),    /* 066: p=0.004495 (    0,    0) */
                new ZPTable( 0x00a5,  0x7f8d,  69,  65 ),    /* 067: p=0.003484 (    0,    0) */
                new ZPTable( 0x00a5,  0x7f8d,  70,  66 ),    /* 068: p=0.003484 (    0,    0) */
                new ZPTable( 0x007b,  0x7faa,  71,  67 ),    /* 069: p=0.002592 (    0,    0) */
                new ZPTable( 0x007b,  0x7faa,  72,  68 ),    /* 070: p=0.002592 (    0,    0) */
                new ZPTable( 0x0057,  0x7fc3,  73,  69 ),    /* 071: p=0.001835 (    0,    0) */
                new ZPTable( 0x0057,  0x7fc3,  74,  70 ),    /* 072: p=0.001835 (    0,    0) */
                new ZPTable( 0x0039,  0x7fd8,  75,  71 ),    /* 073: p=0.001211 (    0,    0) */
                new ZPTable( 0x0039,  0x7fd8,  76,  72 ),    /* 074: p=0.001211 (    0,    0) */
                new ZPTable( 0x0023,  0x7fe7,  77,  73 ),    /* 075: p=0.000740 (    0,    0) */
                new ZPTable( 0x0023,  0x7fe7,  78,  74 ),    /* 076: p=0.000740 (    0,    0) */
                new ZPTable( 0x0013,  0x7ff2,  79,  75 ),    /* 077: p=0.000402 (    0,    0) */
                new ZPTable( 0x0013,  0x7ff2,  80,  76 ),    /* 078: p=0.000402 (    0,    0) */
                new ZPTable( 0x0007,  0x7ffa,  81,  77 ),    /* 079: p=0.000153 (    0,    0) */
                new ZPTable( 0x0007,  0x7ffa,  82,  78 ),    /* 080: p=0.000153 (    0,    0) */
                new ZPTable( 0x0001,  0x7fff,  81,  79 ),    /* 081: p=0.000027 (    0,    0) */
                new ZPTable( 0x0001,  0x7fff,  82,  80 ),    /* 082: p=0.000027 (    0,    0) */
                new ZPTable( 0x620b,  0x0000,   9,  85 ),    /* 083: p=0.411764 (    2,    3) */
                new ZPTable( 0x294a,  0x0000,  86, 216 ),    /* 084: p=0.199988 (    1,    0) */
                new ZPTable( 0x8000,  0x0000,   5,   6 ),    /* 085: p=0.500000 (    3,    3) */
                new ZPTable( 0x0db3,  0x0000,  88, 168 ),    /* 086: p=0.071422 (    4,    0) */
                new ZPTable( 0x538e,  0x0000,  89, 137 ),    /* 087: p=0.363634 (    1,    2) */
                new ZPTable( 0x0490,  0x0000,  90, 134 ),    /* 088: p=0.024388 (   13,    0) */
                new ZPTable( 0x3e3e,  0x0000,  91, 135 ),    /* 089: p=0.285711 (    1,    3) */
                new ZPTable( 0x017c,  0x0000,  92, 112 ),    /* 090: p=0.007999 (   41,    0) */
                new ZPTable( 0x294a,  0x0000,  93, 133 ),    /* 091: p=0.199997 (    1,    5) */
                new ZPTable( 0x007c,  0x0000,  94, 104 ),    /* 092: p=0.002611 (  127,    0) */
                new ZPTable( 0x1b75,  0x0000,  95, 131 ),    /* 093: p=0.137929 (    1,    8) */
                new ZPTable( 0x0028,  0x0000,  96, 100 ),    /* 094: p=0.000849 (  392,    0) */
                new ZPTable( 0x12fc,  0x0000,  97, 129 ),    /* 095: p=0.097559 (    1,   12) */
                new ZPTable( 0x000d,  0x0000,  82,  98 ),    /* 096: p=0.000276 ( 1208,    0) */
                new ZPTable( 0x0cfb,  0x0000,  99, 125 ),    /* 097: p=0.067795 (    1,   18) */
                new ZPTable( 0x0034,  0x0000,  76,  72 ),    /* 098: p=0.001102 ( 1208,    1) */
                new ZPTable( 0x08cd,  0x0000, 101, 123 ),    /* 099: p=0.046511 (    1,   27) */
                new ZPTable( 0x00a0,  0x0000,  70, 102 ),    /* 100: p=0.003387 (  392,    1) */
                new ZPTable( 0x05de,  0x0000, 103, 119 ),    /* 101: p=0.031249 (    1,   41) */
                new ZPTable( 0x0118,  0x0000,  66,  60 ),    /* 102: p=0.005912 (  392,    2) */
                new ZPTable( 0x03e9,  0x0000, 105, 117 ),    /* 103: p=0.020942 (    1,   62) */
                new ZPTable( 0x01ed,  0x0000, 106, 110 ),    /* 104: p=0.010362 (  127,    1) */
                new ZPTable( 0x0298,  0x0000, 107, 115 ),    /* 105: p=0.013937 (    1,   94) */
                new ZPTable( 0x0145,  0x0000,  66, 108 ),    /* 106: p=0.006849 (  193,    1) */
                new ZPTable( 0x01b6,  0x0000, 109, 113 ),    /* 107: p=0.009216 (    1,  143) */
                new ZPTable( 0x0237,  0x0000,  60,  54 ),    /* 108: p=0.011925 (  193,    2) */
                new ZPTable( 0x0121,  0x0000,  65, 111 ),    /* 109: p=0.006097 (    1,  217) */
                new ZPTable( 0x035b,  0x0000,  56,  48 ),    /* 110: p=0.017995 (  127,    2) */
                new ZPTable( 0x01f9,  0x0000,  59,  53 ),    /* 111: p=0.010622 (    2,  217) */
                new ZPTable( 0x05de,  0x0000, 114, 130 ),    /* 112: p=0.031249 (   41,    1) */
                new ZPTable( 0x02fc,  0x0000,  55,  49 ),    /* 113: p=0.016018 (    2,  143) */
                new ZPTable( 0x03e9,  0x0000, 116, 128 ),    /* 114: p=0.020942 (   62,    1) */
                new ZPTable( 0x0484,  0x0000,  51,  45 ),    /* 115: p=0.024138 (    2,   94) */
                new ZPTable( 0x0298,  0x0000, 118, 126 ),    /* 116: p=0.013937 (   94,    1) */
                new ZPTable( 0x06ca,  0x0000,  47,  39 ),    /* 117: p=0.036082 (    2,   62) */
                new ZPTable( 0x01b6,  0x0000, 120, 124 ),    /* 118: p=0.009216 (  143,    1) */
                new ZPTable( 0x0a27,  0x0000,  41, 121 ),    /* 119: p=0.053434 (    2,   41) */
                new ZPTable( 0x0121,  0x0000,  66, 122 ),    /* 120: p=0.006097 (  217,    1) */
                new ZPTable( 0x0e57,  0x0000,  37,  31 ),    /* 121: p=0.074626 (    3,   41) */
                new ZPTable( 0x01f9,  0x0000,  60,  54 ),    /* 122: p=0.010622 (  217,    2) */
                new ZPTable( 0x0f25,  0x0000,  37,  29 ),    /* 123: p=0.078651 (    2,   27) */
                new ZPTable( 0x02fc,  0x0000,  56,  50 ),    /* 124: p=0.016018 (  143,    2) */
                new ZPTable( 0x1629,  0x0000,  33, 127 ),    /* 125: p=0.112902 (    2,   18) */
                new ZPTable( 0x0484,  0x0000,  52,  46 ),    /* 126: p=0.024138 (   94,    2) */
                new ZPTable( 0x1ee8,  0x0000,  27,  21 ),    /* 127: p=0.153845 (    3,   18) */
                new ZPTable( 0x06ca,  0x0000,  48,  40 ),    /* 128: p=0.036082 (   62,    2) */
                new ZPTable( 0x200f,  0x0000,  27,  19 ),    /* 129: p=0.159089 (    2,   12) */
                new ZPTable( 0x0a27,  0x0000,  42, 132 ),    /* 130: p=0.053434 (   41,    2) */
                new ZPTable( 0x2dae,  0x0000,  21,  15 ),    /* 131: p=0.218748 (    2,    8) */
                new ZPTable( 0x0e57,  0x0000,  38,  32 ),    /* 132: p=0.074626 (   41,    3) */
                new ZPTable( 0x4320,  0x0000,  15,   7 ),    /* 133: p=0.304346 (    2,    5) */
                new ZPTable( 0x11a0,  0x0000, 136, 164 ),    /* 134: p=0.090907 (   13,    1) */
                new ZPTable( 0x620b,  0x0000,   9,  85 ),    /* 135: p=0.411764 (    2,    3) */
                new ZPTable( 0x0bbe,  0x0000, 138, 162 ),    /* 136: p=0.061537 (   20,    1) */
                new ZPTable( 0x8000,  0x0000, 135, 248 ),    /* 137: p=0.500000 (    2,    2) */
                new ZPTable( 0x07f3,  0x0000, 140, 160 ),    /* 138: p=0.042104 (   30,    1) */
                new ZPTable( 0x294a,  0x0000, 141, 247 ),    /* 139: p=0.199988 (    0,    1) */
                new ZPTable( 0x053e,  0x0000, 142, 158 ),    /* 140: p=0.027971 (   46,    1) */
                new ZPTable( 0x0db3,  0x0000, 143, 199 ),    /* 141: p=0.071422 (    0,    4) */
                new ZPTable( 0x0378,  0x0000, 144, 156 ),    /* 142: p=0.018604 (   70,    1) */
                new ZPTable( 0x0490,  0x0000, 145, 167 ),    /* 143: p=0.024388 (    0,   13) */
                new ZPTable( 0x024d,  0x0000, 146, 154 ),    /* 144: p=0.012384 (  106,    1) */
                new ZPTable( 0x017c,  0x0000, 147, 101 ),    /* 145: p=0.007999 (    0,   41) */
                new ZPTable( 0x0185,  0x0000, 148, 152 ),    /* 146: p=0.008197 (  161,    1) */
                new ZPTable( 0x007c,  0x0000, 149, 159 ),    /* 147: p=0.002611 (    0,  127) */
                new ZPTable( 0x0100,  0x0000,  68, 150 ),    /* 148: p=0.005405 (  245,    1) */
                new ZPTable( 0x0028,  0x0000, 151, 155 ),    /* 149: p=0.000849 (    0,  392) */
                new ZPTable( 0x01c0,  0x0000,  62,  56 ),    /* 150: p=0.009421 (  245,    2) */
                new ZPTable( 0x000d,  0x0000,  81, 153 ),    /* 151: p=0.000276 (    0, 1208) */
                new ZPTable( 0x02a7,  0x0000,  58,  52 ),    /* 152: p=0.014256 (  161,    2) */
                new ZPTable( 0x0034,  0x0000,  75,  71 ),    /* 153: p=0.001102 (    1, 1208) */
                new ZPTable( 0x0403,  0x0000,  54,  46 ),    /* 154: p=0.021472 (  106,    2) */
                new ZPTable( 0x00a0,  0x0000,  69, 157 ),    /* 155: p=0.003387 (    1,  392) */
                new ZPTable( 0x0608,  0x0000,  48,  42 ),    /* 156: p=0.032110 (   70,    2) */
                new ZPTable( 0x0118,  0x0000,  65,  59 ),    /* 157: p=0.005912 (    2,  392) */
                new ZPTable( 0x0915,  0x0000,  44,  38 ),    /* 158: p=0.047945 (   46,    2) */
                new ZPTable( 0x01ed,  0x0000, 161, 165 ),    /* 159: p=0.010362 (    1,  127) */
                new ZPTable( 0x0db4,  0x0000,  40,  32 ),    /* 160: p=0.071428 (   30,    2) */
                new ZPTable( 0x0145,  0x0000,  65, 163 ),    /* 161: p=0.006849 (    1,  193) */
                new ZPTable( 0x1417,  0x0000,  34,  26 ),    /* 162: p=0.102940 (   20,    2) */
                new ZPTable( 0x0237,  0x0000,  59,  53 ),    /* 163: p=0.011925 (    2,  193) */
                new ZPTable( 0x1dd6,  0x0000,  30, 166 ),    /* 164: p=0.148935 (   13,    2) */
                new ZPTable( 0x035b,  0x0000,  55,  47 ),    /* 165: p=0.017995 (    2,  127) */
                new ZPTable( 0x294a,  0x0000,  24,  18 ),    /* 166: p=0.199999 (   13,    3) */
                new ZPTable( 0x11a0,  0x0000, 169, 195 ),    /* 167: p=0.090907 (    1,   13) */
                new ZPTable( 0x31a3,  0x0000, 170, 212 ),    /* 168: p=0.235291 (    4,    1) */
                new ZPTable( 0x0bbe,  0x0000, 171, 193 ),    /* 169: p=0.061537 (    1,   20) */
                new ZPTable( 0x235a,  0x0000, 172, 208 ),    /* 170: p=0.173910 (    6,    1) */
                new ZPTable( 0x07f3,  0x0000, 173, 191 ),    /* 171: p=0.042104 (    1,   30) */
                new ZPTable( 0x18b3,  0x0000, 174, 206 ),    /* 172: p=0.124998 (    9,    1) */
                new ZPTable( 0x053e,  0x0000, 175, 189 ),    /* 173: p=0.027971 (    1,   46) */
                new ZPTable( 0x1073,  0x0000, 176, 204 ),    /* 174: p=0.085105 (   14,    1) */
                new ZPTable( 0x0378,  0x0000, 177, 187 ),    /* 175: p=0.018604 (    1,   70) */
                new ZPTable( 0x0b35,  0x0000, 178, 200 ),    /* 176: p=0.058822 (   21,    1) */
                new ZPTable( 0x024d,  0x0000, 179, 185 ),    /* 177: p=0.012384 (    1,  106) */
                new ZPTable( 0x0778,  0x0000, 180, 198 ),    /* 178: p=0.039603 (   32,    1) */
                new ZPTable( 0x0185,  0x0000, 181, 183 ),    /* 179: p=0.008197 (    1,  161) */
                new ZPTable( 0x04ed,  0x0000, 182, 194 ),    /* 180: p=0.026315 (   49,    1) */
                new ZPTable( 0x0100,  0x0000,  67,  59 ),    /* 181: p=0.005405 (    1,  245) */
                new ZPTable( 0x0349,  0x0000, 184, 192 ),    /* 182: p=0.017621 (   74,    1) */
                new ZPTable( 0x02a7,  0x0000,  57,  51 ),    /* 183: p=0.014256 (    2,  161) */
                new ZPTable( 0x022e,  0x0000, 186, 190 ),    /* 184: p=0.011730 (  112,    1) */
                new ZPTable( 0x0403,  0x0000,  53,  45 ),    /* 185: p=0.021472 (    2,  106) */
                new ZPTable( 0x0171,  0x0000,  64, 188 ),    /* 186: p=0.007767 (  170,    1) */
                new ZPTable( 0x0608,  0x0000,  47,  41 ),    /* 187: p=0.032110 (    2,   70) */
                new ZPTable( 0x0283,  0x0000,  58,  52 ),    /* 188: p=0.013513 (  170,    2) */
                new ZPTable( 0x0915,  0x0000,  43,  37 ),    /* 189: p=0.047945 (    2,   46) */
                new ZPTable( 0x03cc,  0x0000,  54,  48 ),    /* 190: p=0.020349 (  112,    2) */
                new ZPTable( 0x0db4,  0x0000,  39,  31 ),    /* 191: p=0.071428 (    2,   30) */
                new ZPTable( 0x05b6,  0x0000,  50,  42 ),    /* 192: p=0.030434 (   74,    2) */
                new ZPTable( 0x1417,  0x0000,  33,  25 ),    /* 193: p=0.102940 (    2,   20) */
                new ZPTable( 0x088a,  0x0000,  44, 196 ),    /* 194: p=0.045161 (   49,    2) */
                new ZPTable( 0x1dd6,  0x0000,  29, 197 ),    /* 195: p=0.148935 (    2,   13) */
                new ZPTable( 0x0c16,  0x0000,  40,  34 ),    /* 196: p=0.063291 (   49,    3) */
                new ZPTable( 0x294a,  0x0000,  23,  17 ),    /* 197: p=0.199999 (    3,   13) */
                new ZPTable( 0x0ce2,  0x0000,  40,  32 ),    /* 198: p=0.067307 (   32,    2) */
                new ZPTable( 0x31a3,  0x0000, 201, 243 ),    /* 199: p=0.235291 (    1,    4) */
                new ZPTable( 0x1332,  0x0000,  36, 202 ),    /* 200: p=0.098590 (   21,    2) */
                new ZPTable( 0x235a,  0x0000, 203, 239 ),    /* 201: p=0.173910 (    1,    6) */
                new ZPTable( 0x1adc,  0x0000,  30,  24 ),    /* 202: p=0.135134 (   21,    3) */
                new ZPTable( 0x18b3,  0x0000, 205, 237 ),    /* 203: p=0.124998 (    1,    9) */
                new ZPTable( 0x1be7,  0x0000,  30,  22 ),    /* 204: p=0.139999 (   14,    2) */
                new ZPTable( 0x1073,  0x0000, 207, 235 ),    /* 205: p=0.085105 (    1,   14) */
                new ZPTable( 0x294a,  0x0000,  26,  16 ),    /* 206: p=0.199998 (    9,    2) */
                new ZPTable( 0x0b35,  0x0000, 209, 231 ),    /* 207: p=0.058822 (    1,   21) */
                new ZPTable( 0x3a07,  0x0000,  20, 210 ),    /* 208: p=0.269229 (    6,    2) */
                new ZPTable( 0x0778,  0x0000, 211, 229 ),    /* 209: p=0.039603 (    1,   32) */
                new ZPTable( 0x4e30,  0x0000,  14,   8 ),    /* 210: p=0.344827 (    6,    3) */
                new ZPTable( 0x04ed,  0x0000, 213, 225 ),    /* 211: p=0.026315 (    1,   49) */
                new ZPTable( 0x4fa6,  0x0000,  14, 214 ),    /* 212: p=0.349998 (    4,    2) */
                new ZPTable( 0x0349,  0x0000, 215, 223 ),    /* 213: p=0.017621 (    1,   74) */
                new ZPTable( 0x6966,  0x0000,   8,   2 ),    /* 214: p=0.434782 (    4,    3) */
                new ZPTable( 0x022e,  0x0000, 217, 221 ),    /* 215: p=0.011730 (    1,  112) */
                new ZPTable( 0x8000,  0x0000, 218,  87 ),    /* 216: p=0.500000 (    1,    1) */
                new ZPTable( 0x0171,  0x0000,  63, 219 ),    /* 217: p=0.007767 (    1,  170) */
                new ZPTable( 0x538e,  0x0000, 220, 246 ),    /* 218: p=0.363634 (    2,    1) */
                new ZPTable( 0x0283,  0x0000,  57,  51 ),    /* 219: p=0.013513 (    2,  170) */
                new ZPTable( 0x3e3e,  0x0000, 222, 244 ),    /* 220: p=0.285711 (    3,    1) */
                new ZPTable( 0x03cc,  0x0000,  53,  47 ),    /* 221: p=0.020349 (    2,  112) */
                new ZPTable( 0x294a,  0x0000, 224, 242 ),    /* 222: p=0.199997 (    5,    1) */
                new ZPTable( 0x05b6,  0x0000,  49,  41 ),    /* 223: p=0.030434 (    2,   74) */
                new ZPTable( 0x1b75,  0x0000, 226, 240 ),    /* 224: p=0.137929 (    8,    1) */
                new ZPTable( 0x088a,  0x0000,  43, 227 ),    /* 225: p=0.045161 (    2,   49) */
                new ZPTable( 0x12fc,  0x0000, 228, 238 ),    /* 226: p=0.097559 (   12,    1) */
                new ZPTable( 0x0c16,  0x0000,  39,  33 ),    /* 227: p=0.063291 (    3,   49) */
                new ZPTable( 0x0cfb,  0x0000, 230, 234 ),    /* 228: p=0.067795 (   18,    1) */
                new ZPTable( 0x0ce2,  0x0000,  39,  31 ),    /* 229: p=0.067307 (    2,   32) */
                new ZPTable( 0x08cd,  0x0000, 112, 232 ),    /* 230: p=0.046511 (   27,    1) */
                new ZPTable( 0x1332,  0x0000,  35, 233 ),    /* 231: p=0.098590 (    2,   21) */
                new ZPTable( 0x0f25,  0x0000,  38,  30 ),    /* 232: p=0.078651 (   27,    2) */
                new ZPTable( 0x1adc,  0x0000,  29,  23 ),    /* 233: p=0.135134 (    3,   21) */
                new ZPTable( 0x1629,  0x0000,  34, 236 ),    /* 234: p=0.112902 (   18,    2) */
                new ZPTable( 0x1be7,  0x0000,  29,  21 ),    /* 235: p=0.139999 (    2,   14) */
                new ZPTable( 0x1ee8,  0x0000,  28,  22 ),    /* 236: p=0.153845 (   18,    3) */
                new ZPTable( 0x294a,  0x0000,  25,  15 ),    /* 237: p=0.199998 (    2,    9) */
                new ZPTable( 0x200f,  0x0000,  28,  20 ),    /* 238: p=0.159089 (   12,    2) */
                new ZPTable( 0x3a07,  0x0000,  19, 241 ),    /* 239: p=0.269229 (    2,    6) */
                new ZPTable( 0x2dae,  0x0000,  22,  16 ),    /* 240: p=0.218748 (    8,    2) */
                new ZPTable( 0x4e30,  0x0000,  13,   7 ),    /* 241: p=0.344827 (    3,    6) */
                new ZPTable( 0x4320,  0x0000,  16,   8 ),    /* 242: p=0.304346 (    5,    2) */
                new ZPTable( 0x4fa6,  0x0000,  13, 245 ),    /* 243: p=0.349998 (    2,    4) */
                new ZPTable( 0x620b,  0x0000,  10,   2 ),    /* 244: p=0.411764 (    3,    2) */
                new ZPTable( 0x6966,  0x0000,   7,   1 ),    /* 245: p=0.434782 (    3,    4) */
                new ZPTable( 0x8000,  0x0000, 244,  83 ),    /* 246: p=0.500000 (    2,    2) */
                new ZPTable( 0x8000,  0x0000, 249, 250 ),    /* 247: p=0.500000 (    1,    1) */
                new ZPTable( 0x620b,  0x0000,  10,   2 ),    /* 248: p=0.411764 (    3,    2) */
                new ZPTable( 0x538e,  0x0000,  89, 137 ),    /* 249: p=0.363634 (    1,    2) */
                new ZPTable( 0x538e,  0x0000, 220, 246 ),    /* 250: p=0.363634 (    2,    1) */
#endif
            };
        }
    }
}