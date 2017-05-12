using DjvuNet.Compression;

namespace DjvuNet.Wavelet
{
    public class InterWaveCodec : InterWaveCodecBase, IInterWaveCodec
    {
        #region Constructors

        /// <summary> 
        /// Creates a new Codec object.
        /// </summary>
        public InterWaveCodec()
        {
            _CtxStart = new byte[32];

            _CtxBucket = new byte[10][];
            for (int i2 = 0; i2 < _CtxBucket.Length; i2++)
                _CtxBucket[i2] = new byte[8];

            _QuantHigh = new int[10];
            _QuantLow = new int[16];
            _CoefficientState = new sbyte[256];
            _BucketState = new sbyte[16];
            _CurrentBand = 0;
            _CurrentBitPlane = 1;
        }

        #endregion Constructors

        #region Public Methods

        public int CodeSlice(IDataCoder coder)
        {
            if (_CurrentBitPlane >= 0)
            {
                if (!IsNullSlice(_CurrentBitPlane, _CurrentBand))
                {
                    for (int blockno = 0; blockno < _Map.BlockNumber; blockno++)
                    {
                        int fbucket = _BandBuckets[_CurrentBand].Start;
                        int nbucket = _BandBuckets[_CurrentBand].Size;
                        DecodeBuckets(coder, _CurrentBitPlane, _CurrentBand, _Map.Blocks[blockno], fbucket, nbucket);
                    }
                }

                if (++_CurrentBand >= _BandBuckets.Length)
                {
                    _CurrentBand = 0;
                    _CurrentBitPlane++;

                    if (NextQuantFast() == 0)
                    {
                        _CurrentBitPlane = -1;
                        return 0;
                    }
                }
                return 1;
            }
            return 0;
        }

        public void DecodeBuckets(IDataCoder coder, int bit, int band, InterWaveBlock blk, int fbucket, int nbucket)
        {
            int thres = _QuantHigh[band];
            int bbstate = 0;
            sbyte[] cstate = _CoefficientState;
            int cidx = 0;

            for (int buckno = 0; buckno < nbucket; buckno++, cidx += 16)
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
            }

            if (nbucket < 16 || (bbstate & 2) != 0)
                bbstate |= 4;
            else if ((bbstate & 8) != 0)
            {
                if (coder.Decoder(ref _CtxRoot) != 0)
                    bbstate |= 4;
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
                        if ((bbstate & 2) != 0)
                            ctx |= 4;

                        if (coder.Decoder(ref _CtxBucket[band][ctx]) != 0)
                            _BucketState[buckno] |= 4;
                    }
                }
            }

            if ((bbstate & 4) != 0)
            {
                cstate = _CoefficientState;
                cidx = 0;

                for (int buckno = 0; buckno < nbucket; buckno++, cidx += 16)
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

                                if (coder.Decoder(ref _CtxStart[ctx]) != 0)
                                {
                                    cstate[cidx + i] |= 4;

                                    int halfthres = thres >> 1;
                                    int coeff = (thres + halfthres) - (halfthres >> 2);

                                    if (coder.IWDecoder() != 0)
                                        pcoeff[i] = (short)(-coeff);
                                    else
                                        pcoeff[i] = (short)coeff;
                                }

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

                    ;
                    ;
                }
            }

            if ((bbstate & 2) != 0)
            {
                cstate = _CoefficientState;
                cidx = 0;

                for (int buckno = 0; buckno < nbucket; buckno++, cidx += 16)
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

                                    if (coder.Decoder(ref _CtxMant) != 0)
                                        coeff += (thres >> 1);
                                    else
                                        coeff = (coeff - thres) + (thres >> 1);
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
                        } // end for (int i = 0 ...
                    }                    
                } // end for (int buckno = 0 ....
            }
        }

        public InterWaveCodec Init(InterWaveMap map)
        {
            _Map = map;

            int i = 0;
            int[] q = _IwQuant;
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
                NextQuantFast();

            return this;
        }

        public bool IsNullSlice(int bit, int band)
        {
            if (band == 0)
            {
                bool is_null = true;

                for (int i = 0; i < 16; i++)
                {
                    int threshold = _QuantLow[i];
                    _CoefficientState[i] = 1;

                    if ((threshold > 0) && (threshold < 32768))
                    {
                        is_null = false;
                        _CoefficientState[i] = 0;
                    }
                }

                return is_null;
            }

            int threshold2 = _QuantHigh[band];

            if ((threshold2 <= 0) || (threshold2 >= 32768))
                return true;

            for (int i = 0; i < (_BandBuckets[band].Size << 4); i++)
                _CoefficientState[i] = 0;

            return false;
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

        /// <summary>
        /// Faster version of NextQuant - NextQuantFast has 41,5 % shorter execution time or
        /// alternatively NextQuant is 70,7% slower than NextQuantFast. Optimizations comprised
        /// hand unrolled loops, removed conditionals and use of same variable int in result 
        /// accumulation and as return value
        /// </summary>
        /// <returns></returns>
        public int NextQuantFast()
        {
            int flag = 0;

            flag = (_QuantLow[0] = _QuantLow[0] >> 1);
            flag = (_QuantLow[1] = _QuantLow[1] >> 1) | flag;
            flag = (_QuantLow[2] = _QuantLow[2] >> 1) | flag;
            flag = (_QuantLow[3] = _QuantLow[3] >> 1) | flag;
            flag = (_QuantLow[4] = _QuantLow[4] >> 1) | flag;
            flag = (_QuantLow[5] = _QuantLow[5] >> 1) | flag;
            flag = (_QuantLow[6] = _QuantLow[6] >> 1) | flag;
            flag = (_QuantLow[7] = _QuantLow[7] >> 1) | flag;
            flag = (_QuantLow[8] = _QuantLow[8] >> 1) | flag;
            flag = (_QuantLow[9] = _QuantLow[9] >> 1) | flag;
            flag = (_QuantLow[10] = _QuantLow[10] >> 1) | flag;
            flag = (_QuantLow[11] = _QuantLow[11] >> 1) | flag;
            flag = (_QuantLow[12] = _QuantLow[12] >> 1) | flag;
            flag = (_QuantLow[13] = _QuantLow[13] >> 1) | flag;
            flag = (_QuantLow[14] = _QuantLow[14] >> 1) | flag;
            flag = (_QuantLow[15] = _QuantLow[15] >> 1) | flag;

            flag = (_QuantHigh[0] = _QuantHigh[0] >> 1) | flag;
            flag = (_QuantHigh[1] = _QuantHigh[1] >> 1) | flag;
            flag = (_QuantHigh[2] = _QuantHigh[2] >> 1) | flag;
            flag = (_QuantHigh[3] = _QuantHigh[3] >> 1) | flag;
            flag = (_QuantHigh[4] = _QuantHigh[4] >> 1) | flag;
            flag = (_QuantHigh[5] = _QuantHigh[5] >> 1) | flag;
            flag = (_QuantHigh[6] = _QuantHigh[6] >> 1) | flag;
            flag = (_QuantHigh[7] = _QuantHigh[7] >> 1) | flag;
            flag = (_QuantHigh[8] = _QuantHigh[8] >> 1) | flag;
            flag = (_QuantHigh[9] = _QuantHigh[9] >> 1) | flag;

            return flag;
        }

        #endregion Public Methods
    }
}