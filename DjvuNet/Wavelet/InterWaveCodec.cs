namespace DjvuNet.Wavelet
{
    public class InterWaveCodec
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

        public const int Zero = 1;
        public const int Active = 2;
        public const int New = 4;
        public const int Unk = 8;

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

        #region Constructors

        /// <summary>
        /// Creates a new Codec object.
        /// </summary>
        public InterWaveCodec(InterWaveMap map)
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
            _Map = map;
            Init(map);
        }

        #endregion Constructors

        #region Public Methods

        public InterWaveCodec Init(InterWaveMap map)
        {
            int i = 0;
            int[] q = _IwQuant;
            int qidx = 0;

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
        /// alternatively NextQuant is 70,7% slower than NextQuantFast. Optimizations comprise
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
