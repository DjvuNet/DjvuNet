using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Wavelet
{
    public class InterWaveCodecBase
    {
        public const byte MajorVersion = 0x01;
        public const byte MinorVersion = 0x02;

        #region Internal Fields

        internal sbyte[] _BucketState;
        internal sbyte[] _CoefficientState;
        internal byte[][] _CtxBucket;
        internal byte _CtxMant;
        internal byte _CtxRoot;
        internal byte[] _CtxStart;
        internal int[] _QuantHigh;
        internal int[] _QuantLow;
        internal int _CurrentBand;
        internal int _CurrentBitPlane;
        internal InterWaveMap _Map;

        public const int ZERO = 1;
        public const int ACTIVE = 2;
        public const int NEW = 4;
        public const int UNK = 8;

        internal readonly int[] _IwQuant = new int[]
        {
            0x10000, 0x20000, 0x20000, 0x40000, 0x40000, 0x40000, 0x80000,
            0x80000, 0x80000, 0x100000, 0x100000, 0x100000, 0x200000,
            0x100000, 0x100000, 0x200000
        };

        internal readonly InterWaveBucket[] _BandBuckets = new InterWaveBucket[]
        {
            new InterWaveBucket(0, 1), new InterWaveBucket(1, 1), new InterWaveBucket(2, 1),
            new InterWaveBucket(3, 1), new InterWaveBucket(4, 4), new InterWaveBucket(8, 4),
            new InterWaveBucket(12, 4), new InterWaveBucket(16, 16), new InterWaveBucket(32, 16),
            new InterWaveBucket(48, 16)
        };

        #endregion Internal Fields
    }
}
