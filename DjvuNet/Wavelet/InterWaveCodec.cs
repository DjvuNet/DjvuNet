using DjvuNet.Compression;

namespace DjvuNet.Wavelet
{
    public sealed class InterWaveCodec : IInterWaveCodec
    {
        public const byte MajorVersion = 0x01;
        public const byte MinorVersion = 0x02;

        #region Private Members

        private readonly sbyte[] _BucketState;
        private readonly sbyte[] _CoefficientState;
        private readonly MutableValue<sbyte>[][] _ctxBucket;
        private readonly MutableValue<sbyte> _ctxMant;
        private readonly MutableValue<sbyte> _ctxRoot;
        private readonly MutableValue<sbyte>[] _ctxStart;
        private readonly int[] _QuantHigh;
        private readonly int[] _QuantLow;
        private int _CurrentBand;
        private int _CurrentBitPlane;
        private InterWaveMap _Map;

        //private const int ZERO = 1;
        //private const int ACTIVE = 2;
        //private const int NEW = 4;
        //private const int UNK = 8;

        private readonly int[] IwQuant = 
            new[]
            {
                0x10000, 0x20000, 0x20000, 0x40000, 0x40000, 0x40000, 0x80000,
                0x80000, 0x80000, 0x100000, 0x100000, 0x100000, 0x200000,
                0x100000, 0x100000, 0x200000
            };

        private readonly InterWaveBucket[] Bandbuckets = 
            new[]
            {
                new InterWaveBucket(0, 1), new InterWaveBucket(1, 1), new InterWaveBucket(2, 1),
                new InterWaveBucket(3, 1), new InterWaveBucket(4, 4), new InterWaveBucket(8, 4),
                new InterWaveBucket(12, 4), new InterWaveBucket(16, 16), new InterWaveBucket(32, 16),
                new InterWaveBucket(48, 16)
            };

        #endregion Private Members

        #region Constructors

        /// <summary> Creates a new Codec object.</summary>
        public InterWaveCodec()
        {
            _ctxStart = new MutableValue<sbyte>[32];

            for (int i = 0; i < _ctxStart.Length; i++)
                _ctxStart[i] = new MutableValue<sbyte>();

            _ctxBucket = new MutableValue<sbyte>[10][];
            for (int i2 = 0; i2 < _ctxBucket.Length; i2++)
                _ctxBucket[i2] = new MutableValue<sbyte>[8];

            for (int i = 0; i < _ctxBucket.Length; i++)
            {
                for (int j = 0; j < _ctxBucket[i].Length; j++)
                    _ctxBucket[i][j] = new MutableValue<sbyte>();
            }

            _QuantHigh = new int[10];
            _QuantLow = new int[16];
            _CoefficientState = new sbyte[256];
            _BucketState = new sbyte[16];
            _CurrentBand = 0;
            _CurrentBitPlane = 1;
            _ctxMant = new MutableValue<sbyte>();
            _ctxRoot = new MutableValue<sbyte>();
        }

        #endregion Constructors

        #region Public Methods

        public int CodeSlice(IDataCoder coder)
        {
            if (_CurrentBitPlane < 0)
                return 0;

            if (IsNullSlice(_CurrentBitPlane, _CurrentBand) == 0)
            {
                for (int blockno = 0; blockno < _Map.Nb; blockno++)
                {
                    int fbucket = Bandbuckets[_CurrentBand].Start;
                    int nbucket = Bandbuckets[_CurrentBand].Size;
                    DecodeBuckets(coder, _CurrentBitPlane, _CurrentBand, _Map.Blocks[blockno], fbucket, nbucket);
                }
            }

            if (++_CurrentBand >= Bandbuckets.Length)
            {
                _CurrentBand = 0;
                _CurrentBitPlane++;

                if (NextQuant() == 0)
                {
                    _CurrentBitPlane = -1;
                    return 0;
                }
            }

            return 1;
        }

        public void DecodeBuckets(IDataCoder coder, int bit, int band, InterWaveBlock blk, int fbucket, int nbucket)
        {
            int thres = _QuantHigh[band];
            int bbstate = 0;
            sbyte[] cstate = _CoefficientState;
            int cidx = 0;

            for (int buckno = 0; buckno < nbucket; )
            {
                int bstatetmp = 0;
                short[] pcoeff = blk.GetBlock(fbucket + buckno);

                if (pcoeff == null)
                    bstatetmp = 8;
                else
                {
                    for (int i = 0; i < 16; i++)
                    {
                        int cstatetmp = cstate[cidx + i] & 1;

                        if (cstatetmp == 0)
                        {
                            if (pcoeff[i] != 0)
                                cstatetmp |= 2;
                            else
                                cstatetmp |= 8;
                        }

                        cstate[cidx + i] = (sbyte)cstatetmp;
                        bstatetmp |= cstatetmp;
                    }
                }

                _BucketState[buckno] = (sbyte)bstatetmp;
                bbstate |= bstatetmp;
                buckno++;
                cidx += 16;
            }

            if ((nbucket < 16) || ((bbstate & 2) != 0))
            {
                bbstate |= 4;
            }
            else if ((bbstate & 8) != 0)
            {
                byte value = unchecked((byte) _ctxRoot.Value);
                if (coder.Decoder(ref value) != 0)
                    bbstate |= 4;
                _ctxRoot.Value = unchecked((sbyte)value);
            }

            if ((bbstate & 4) != 0)
            {
                for (int buckno = 0; buckno < nbucket; buckno++)
                {
                    if ((_BucketState[buckno] & 8) != 0)
                    {
                        int ctx = 0;

                        //if (!DjVuOptions.NOCTX_BUCKET_UPPER && (band > 0))
                        if ((band > 0))
                        {
                            int k = (fbucket + buckno) << 2;
                            short[] b = blk.GetBlock(k >> 4);

                            if (b != null)
                            {
                                k &= 0xf;

                                if (b[k] != 0)
                                    ctx++;

                                if (b[k + 1] != 0)
                                    ctx++;

                                if (b[k + 2] != 0)
                                    ctx++;

                                if ((ctx < 3) && (b[k + 3] != 0))
                                    ctx++;
                            }
                        }

                        //if (!DjVuOptions.NOCTX_BUCKET_ACTIVE && ((bbstate & 2) != 0))
                        if (((bbstate & 2) != 0))
                            ctx |= 4;

                        byte value = unchecked((byte)_ctxBucket[band][ctx].Value);
                        if (coder.Decoder(ref value) != 0)
                            _BucketState[buckno] |= 4;
                        _ctxBucket[band][ctx].Value = unchecked((sbyte)value);
                    }
                }
            }

            if ((bbstate & 4) != 0)
            {
                cstate = _CoefficientState;
                cidx = 0;

                for (int buckno = 0; buckno < nbucket; )
                {
                    if ((_BucketState[buckno] & 4) != 0)
                    {
                        short[] pcoeff = blk.GetBlock(fbucket + buckno);

                        if (pcoeff == null)
                        {
                            pcoeff = blk.GetInitializedBlock(fbucket + buckno);

                            for (int i = 0; i < 16; i++)
                            {
                                if ((cstate[cidx + i] & 1) == 0)
                                    cstate[cidx + i] = 8;
                            }
                        }

                        int gotcha = 0;
                        int maxgotcha = 7;

                        //if (!DjVuOptions.NOCTX_EXPECT)
                        {
                            for (int i = 0; i < 16; i++)
                            {
                                if ((cstate[cidx + i] & 8) != 0)
                                    gotcha++;
                            }
                        }

                        for (int i = 0; i < 16; i++)
                        {
                            if ((cstate[cidx + i] & 8) != 0)
                            {
                                if (band == 0)
                                    thres = _QuantLow[i];

                                int ctx = 0;

                                //if (!DjVuOptions.NOCTX_EXPECT)
                                {
                                    if (gotcha >= maxgotcha)
                                        ctx = maxgotcha;
                                    else
                                        ctx = gotcha;
                                }

                                //if (!DjVuOptions.NOCTX_ACTIVE && ((bucketstate[buckno] & 2) != 0))
                                if (((_BucketState[buckno] & 2) != 0))
                                    ctx |= 8;

                                byte value = unchecked((byte)_ctxStart[ctx].Value);
                                if (coder.Decoder(ref value) != 0)
                                {
                                    cstate[cidx + i] |= 4;

                                    int halfthres = thres >> 1;
                                    int coeff = (thres + halfthres) - (halfthres >> 2);

                                    if (coder.IWDecoder() != 0)
                                        pcoeff[i] = (short)(-coeff);
                                    else
                                        pcoeff[i] = (short)coeff;
                                }
                                _ctxStart[ctx].Value = unchecked((sbyte)value);

                                //if (!DjVuOptions.NOCTX_EXPECT)
                                {
                                    if ((cstate[cidx + i] & 4) != 0)
                                        gotcha = 0;
                                    else if (gotcha > 0)
                                        gotcha--;
                                }
                            }
                        }
                    }

                    buckno++;
                    cidx += 16;
                }
            }

            if ((bbstate & 2) != 0)
            {
                cstate = _CoefficientState;
                cidx = 0;

                for (int buckno = 0; buckno < nbucket; )
                {
                    if ((_BucketState[buckno] & 2) != 0)
                    {
                        short[] pcoeff = blk.GetBlock(fbucket + buckno);

                        for (int i = 0; i < 16; i++)
                        {
                            if ((cstate[cidx + i] & 2) != 0)
                            {
                                int coeff = pcoeff[i];

                                if (coeff < 0)
                                    coeff = -coeff;

                                if (band == 0)
                                    thres = _QuantLow[i];

                                if (coeff <= (3 * thres))
                                {
                                    coeff += (thres >> 2);

                                    byte value = unchecked((byte)_ctxMant.Value);
                                    if (coder.Decoder(ref value) != 0)
                                        coeff += (thres >> 1);
                                    else
                                        coeff = (coeff - thres) + (thres >> 1);
                                    _ctxMant.Value = unchecked((sbyte)value);
                                }
                                else
                                {
                                    if (coder.IWDecoder() != 0)
                                        coeff += (thres >> 1);
                                    else
                                        coeff = (coeff - thres) + (thres >> 1);
                                }

                                if (pcoeff[i] > 0)
                                    pcoeff[i] = (short)coeff;
                                else
                                    pcoeff[i] = (short)(-coeff);
                            }
                        }
                    }

                    buckno++;
                    cidx += 16;
                }
            }
        }

        public InterWaveCodec Init(InterWaveMap map)
        {
            this._Map = map;

            int i = 0;
            int[] q = IwQuant;
            int qidx = 0;

            // Ideal for 128/256 packed integer

            for (int j = 0; i < 4; j++)
                _QuantLow[i++] = q[qidx++];

            for (int j = 0; j < 4; j++)
                _QuantLow[i++] = q[qidx];

            qidx++;

            for (int j = 0; j < 4; j++)
                _QuantLow[i++] = q[qidx];

            qidx++;

            for (int j = 0; j < 4; j++)
                _QuantLow[i++] = q[qidx];

            qidx++;
            _QuantHigh[0] = 0;

            for (int j = 1; j < 10; j++)
                _QuantHigh[j] = q[qidx++];

            while (_QuantLow[0] >= 32768)
                NextQuant();

            return this;
        }

        public int IsNullSlice(int bit, int band)
        {
            if (band == 0)
            {
                int is_null = 1;

                for (int i = 0; i < 16; i++)
                {
                    int threshold = _QuantLow[i];
                    _CoefficientState[i] = 1;

                    if ((threshold > 0) && (threshold < 32768))
                        is_null = _CoefficientState[i] = 0;
                }

                return is_null;
            }

            int threshold2 = _QuantHigh[band];

            if ((threshold2 <= 0) || (threshold2 >= 32768))
                return 1;

            for (int i = 0; i < (Bandbuckets[band].Size << 4); i++)
                _CoefficientState[i] = 0;

            return 0;
        }

        public int NextQuant()
        {
            int flag = 0;

            for (int i = 0; i < 16; i++)
                if ((_QuantLow[i] = _QuantLow[i] >> 1) != 0)
                    flag = 1;

            for (int i = 0; i < 10; i++)
                if ((_QuantHigh[i] = _QuantHigh[i] >> 1) != 0)
                    flag = 1;

            return flag;
        }

        #endregion Public Methods
    }
}