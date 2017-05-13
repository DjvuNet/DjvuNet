using System;
using System.Collections.Generic;
using System.Linq;
using DjvuNet.Compression;
using DjvuNet.Interfaces;

namespace DjvuNet.Wavelet
{
    public class InterWaveDecoder : InterWaveCodec//, IDecoder
    {

        public InterWaveDecoder(InterWaveMap map) : base(map)
        {
            Init(map);
        }

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

                    if (NextQuant() == 0)
                    {
                        _CurrentBitPlane = -1;
                        return 0;
                    }
                }
                return 1;
            }
            return 0;
        }

        public void DecodeBuckets(IDataCoder coder, int bit, int band, 
            InterWaveBlock blk, int fbucket, int nbucket)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bit"></param>
        /// <param name="band"></param>
        /// <returns></returns>
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
    }
}
