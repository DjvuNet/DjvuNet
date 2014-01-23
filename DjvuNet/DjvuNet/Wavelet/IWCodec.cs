using DjvuNet.Compression;

namespace DjvuNet.Wavelet
{
    public sealed class IWCodec
    {
        #region Private Variables

        private readonly sbyte[] _bucketstate;
        private readonly sbyte[] _coeffstate;
        private readonly MutableValue<sbyte>[][] _ctxBucket;
        private readonly MutableValue<sbyte> _ctxMant;
        private readonly MutableValue<sbyte> _ctxRoot;
        private readonly MutableValue<sbyte>[] _ctxStart;
        private readonly int[] _quantHi;
        private readonly int[] _quantLo;
        private int _curband;
        private int _curbit;
        private IWMap _map;

        //private const int ZERO = 1;
        //private const int ACTIVE = 2;
        //private const int NEW = 4;
        //private const int UNK = 8;

        private static readonly int[] IwQuant = new[]
                                                      {
                                                          0x10000, 0x20000, 0x20000, 0x40000, 0x40000, 0x40000, 0x80000,
                                                          0x80000, 0x80000, 0x100000, 0x100000, 0x100000, 0x200000,
                                                          0x100000, 0x100000, 0x200000
                                                      };

        private static readonly IWBucket[] Bandbuckets = new[]
                                                            {
                                                                new IWBucket(0, 1), new IWBucket(1, 1), new IWBucket(2, 1),
                                                                new IWBucket(3, 1), new IWBucket(4, 4), new IWBucket(8, 4),
                                                                new IWBucket(12, 4), new IWBucket(16, 16), new IWBucket(32, 16),
                                                                new IWBucket(48, 16)
                                                            };

        #endregion Private Variables

        #region Constructors

        /// <summary> Creates a new Codec object.</summary>
        public IWCodec()
        {
            _ctxStart = new MutableValue<sbyte>[32];

            for (int i = 0; i < _ctxStart.Length; i++)
            {
                _ctxStart[i] = new MutableValue<sbyte>();
            }

            _ctxBucket = new MutableValue<sbyte>[10][];
            for (int i2 = 0; i2 < _ctxBucket.Length; i2++)
            {
                _ctxBucket[i2] = new MutableValue<sbyte>[8];
            }

            for (int i = 0; i < _ctxBucket.Length; i++)
            {
                for (int j = 0; j < _ctxBucket[i].Length; j++)
                {
                    _ctxBucket[i][j] = new MutableValue<sbyte>();
                }
            }

            _quantHi = new int[10];
            _quantLo = new int[16];
            _coeffstate = new sbyte[256];
            _bucketstate = new sbyte[16];
            _curband = 0;
            _curbit = 1;
            _ctxMant = new MutableValue<sbyte>();
            _ctxRoot = new MutableValue<sbyte>();
        }

        #endregion Constructors

        #region Public Methods

        public int CodeSlice(ZPCodec zp)
        {
            if (_curbit < 0)
            {
                return 0;
            }

            if (IsNullSlice(_curbit, _curband) == 0)
            {
                for (int blockno = 0; blockno < _map.Nb; blockno++)
                {
                    int fbucket = Bandbuckets[_curband].Start;
                    int nbucket = Bandbuckets[_curband].Size;
                    DecodeBuckets(zp, _curbit, _curband, _map.Blocks[blockno], fbucket, nbucket);
                }
            }

            if (++_curband >= Bandbuckets.Length)
            {
                _curband = 0;
                _curbit++;

                if (NextQuant() == 0)
                {
                    _curbit = -1;

                    return 0;
                }
            }

            return 1;
        }

        private void DecodeBuckets(ZPCodec zp, int bit, int band, IWBlock blk, int fbucket, int nbucket)
        {
            int thres = _quantHi[band];
            int bbstate = 0;
            sbyte[] cstate = _coeffstate;
            int cidx = 0;

            for (int buckno = 0; buckno < nbucket; )
            {
                int bstatetmp = 0;
                short[] pcoeff = blk.GetBlock(fbucket + buckno);

                if (pcoeff == null)
                {
                    bstatetmp = 8;
                }
                else
                {
                    for (int i = 0; i < 16; i++)
                    {
                        int cstatetmp = cstate[cidx + i] & 1;

                        if (cstatetmp == 0)
                        {
                            if (pcoeff[i] != 0)
                            {
                                cstatetmp |= 2;
                            }
                            else
                            {
                                cstatetmp |= 8;
                            }
                        }

                        cstate[cidx + i] = (sbyte)cstatetmp;
                        bstatetmp |= cstatetmp;
                    }
                }

                _bucketstate[buckno] = (sbyte)bstatetmp;
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
                if (zp.Decoder(_ctxRoot) != 0)
                {
                    bbstate |= 4;
                }
            }

            if ((bbstate & 4) != 0)
            {
                for (int buckno = 0; buckno < nbucket; buckno++)
                {
                    if ((_bucketstate[buckno] & 8) != 0)
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
                                {
                                    ctx++;
                                }

                                if (b[k + 1] != 0)
                                {
                                    ctx++;
                                }

                                if (b[k + 2] != 0)
                                {
                                    ctx++;
                                }

                                if ((ctx < 3) && (b[k + 3] != 0))
                                {
                                    ctx++;
                                }
                            }
                        }

                        //if (!DjVuOptions.NOCTX_BUCKET_ACTIVE && ((bbstate & 2) != 0))
                        if (((bbstate & 2) != 0))
                        {
                            ctx |= 4;
                        }

                        if (zp.Decoder(_ctxBucket[band][ctx]) != 0)
                        {
                            _bucketstate[buckno] |= 4;
                        }
                    }
                }
            }

            if ((bbstate & 4) != 0)
            {
                cstate = _coeffstate;
                cidx = 0;

                for (int buckno = 0; buckno < nbucket; )
                {
                    if ((_bucketstate[buckno] & 4) != 0)
                    {
                        short[] pcoeff = blk.GetBlock(fbucket + buckno);

                        if (pcoeff == null)
                        {
                            pcoeff = blk.GetInitializedBlock(fbucket + buckno);

                            for (int i = 0; i < 16; i++)
                            {
                                if ((cstate[cidx + i] & 1) == 0)
                                {
                                    cstate[cidx + i] = 8;
                                }
                            }
                        }

                        int gotcha = 0;
                        int maxgotcha = 7;

                        //if (!DjVuOptions.NOCTX_EXPECT)
                        {
                            for (int i = 0; i < 16; i++)
                            {
                                if ((cstate[cidx + i] & 8) != 0)
                                {
                                    gotcha++;
                                }
                            }
                        }

                        for (int i = 0; i < 16; i++)
                        {
                            if ((cstate[cidx + i] & 8) != 0)
                            {
                                if (band == 0)
                                {
                                    thres = _quantLo[i];
                                }

                                int ctx = 0;

                                //if (!DjVuOptions.NOCTX_EXPECT)
                                {
                                    if (gotcha >= maxgotcha)
                                    {
                                        ctx = maxgotcha;
                                    }
                                    else
                                    {
                                        ctx = gotcha;
                                    }
                                }

                                //if (!DjVuOptions.NOCTX_ACTIVE && ((bucketstate[buckno] & 2) != 0))
                                if (((_bucketstate[buckno] & 2) != 0))
                                {
                                    ctx |= 8;
                                }

                                if (zp.Decoder(_ctxStart[ctx]) != 0)
                                {
                                    cstate[cidx + i] |= 4;

                                    int halfthres = thres >> 1;
                                    int coeff = (thres + halfthres) - (halfthres >> 2);

                                    if (zp.IWDecoder() != 0)
                                    {
                                        pcoeff[i] = (short)(-coeff);
                                    }
                                    else
                                    {
                                        pcoeff[i] = (short)coeff;
                                    }
                                }

                                //if (!DjVuOptions.NOCTX_EXPECT)
                                {
                                    if ((cstate[cidx + i] & 4) != 0)
                                    {
                                        gotcha = 0;
                                    }
                                    else if (gotcha > 0)
                                    {
                                        gotcha--;
                                    }
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
                cstate = _coeffstate;
                cidx = 0;

                for (int buckno = 0; buckno < nbucket; )
                {
                    if ((_bucketstate[buckno] & 2) != 0)
                    {
                        short[] pcoeff = blk.GetBlock(fbucket + buckno);

                        for (int i = 0; i < 16; i++)
                        {
                            if ((cstate[cidx + i] & 2) != 0)
                            {
                                int coeff = pcoeff[i];

                                if (coeff < 0)
                                {
                                    coeff = -coeff;
                                }

                                if (band == 0)
                                {
                                    thres = _quantLo[i];
                                }

                                if (coeff <= (3 * thres))
                                {
                                    coeff += (thres >> 2);

                                    if (zp.Decoder(_ctxMant) != 0)
                                    {
                                        coeff += (thres >> 1);
                                    }
                                    else
                                    {
                                        coeff = (coeff - thres) + (thres >> 1);
                                    }
                                }
                                else
                                {
                                    if (zp.IWDecoder() != 0)
                                    {
                                        coeff += (thres >> 1);
                                    }
                                    else
                                    {
                                        coeff = (coeff - thres) + (thres >> 1);
                                    }
                                }

                                if (pcoeff[i] > 0)
                                {
                                    pcoeff[i] = (short)coeff;
                                }
                                else
                                {
                                    pcoeff[i] = (short)(-coeff);
                                }
                            }
                        }
                    }

                    buckno++;
                    cidx += 16;
                }
            }
        }

        public IWCodec Init(IWMap map)
        {
            this._map = map;

            int i = 0;
            int[] q = IwQuant;
            int qidx = 0;

            for (int j = 0; i < 4; j++)
            {
                _quantLo[i++] = q[qidx++];
            }

            for (int j = 0; j < 4; j++)
            {
                _quantLo[i++] = q[qidx];
            }

            qidx++;

            for (int j = 0; j < 4; j++)
            {
                _quantLo[i++] = q[qidx];
            }

            qidx++;

            for (int j = 0; j < 4; j++)
            {
                _quantLo[i++] = q[qidx];
            }

            qidx++;
            _quantHi[0] = 0;

            for (int j = 1; j < 10; j++)
            {
                _quantHi[j] = q[qidx++];
            }

            while (_quantLo[0] >= 32768)
            {
                NextQuant();
            }

            return this;
        }

        private int IsNullSlice(int bit, int band)
        {
            if (band == 0)
            {
                int is_null = 1;

                for (int i = 0; i < 16; i++)
                {
                    int threshold = _quantLo[i];
                    _coeffstate[i] = 1;

                    if ((threshold > 0) && (threshold < 32768))
                    {
                        is_null = _coeffstate[i] = 0;
                    }
                }

                return is_null;
            }

            int threshold2 = _quantHi[band];

            if ((threshold2 <= 0) || (threshold2 >= 32768))
            {
                return 1;
            }

            for (int i = 0; i < (Bandbuckets[band].Size << 4); i++)
            {
                _coeffstate[i] = 0;
            }

            return 0;
        }

        private int NextQuant()
        {
            int flag = 0;

            for (int i = 0; i < 16; i++)
            {
                if ((_quantLo[i] = _quantLo[i] >> 1) != 0)
                {
                    flag = 1;
                }
            }

            for (int i = 0; i < 10; i++)
            {
                if ((_quantHi[i] = _quantHi[i] >> 1) != 0)
                {
                    flag = 1;
                }
            }

            return flag;
        }

        #endregion Public Methods
    }
}