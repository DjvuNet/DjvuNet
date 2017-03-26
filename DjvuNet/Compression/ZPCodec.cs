using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DjvuNet.Compression
{
    public class ZPCodec
    {
        #region Private Members

        private int _buffer;
        private long _code;
        private short _delay;
        private long _fence;
        internal Stream InputStream;
        private short _scount;
        private short _zByte;
        private const int _arraySize = 256;

        #endregion Private Members

        #region Protected Properties

        #region FFZT

        private sbyte[] _FFZT;

        /// <summary>
        /// Gets the FFZT data
        /// </summary>
        internal sbyte[] FFZT { get { return _FFZT; } }

        #endregion FFZT

        #region AValue

        /// <summary>
        /// Gets or sets the A Value for the item
        /// </summary>
        internal int AValue { get; set; }

        #endregion AValue

        #region Ffzt

        /// <summary>
        /// Gets the Ffzt data
        /// </summary>
        internal sbyte[] Ffzt;

        #endregion Ffzt

        #endregion Protected Properties

        #region Public Properties

        #region Down

        /// <summary>
        /// Gets or sets the down values for the item
        /// </summary>
        public MutableValue<sbyte>[] Down;

        #endregion Down

        #region Up

        /// <summary>
        /// Gets or sets the up values for the item
        /// </summary>
        public MutableValue<sbyte>[] Up;

        #endregion Up

        #region MArray

        /// <summary>
        /// Gets or sets the M Array values for the item
        /// </summary>
        public int[] MArray;

        #endregion MArray

        #region PArray

        /// <summary>
        /// Gets or sets the P Array values for the item
        /// </summary>
        public int[] PArray;

        #endregion PArray

        #region DefaultZPTable

        internal ZPTable[] _defaultZPTable;

        /// <summary>
        /// Gets the default ZP table
        /// </summary>
        public ZPTable[] DefaultZPTable
        {
            get
            {
                if (_defaultZPTable == null)
                    _defaultZPTable = BuildDefaultZPTable();

                return _defaultZPTable;
            }
        }

        #endregion DefaultZPTable

        #endregion Public Properties

        #region Constructors


        public ZPCodec()
        {
            _FFZT = new sbyte[_arraySize];

            for (int i = 0; i < _arraySize; i++)
            {
                for (int j = i; (j & 0x80) > 0; j <<= 1)
                    FFZT[i]++;
            }

            Ffzt = new sbyte[FFZT.Length];
            Array.Copy(FFZT, 0, Ffzt, 0, Ffzt.Length);

            Down = new MutableValue<sbyte>[_arraySize];
            Up = new MutableValue<sbyte>[_arraySize];
            MArray = new int[_arraySize];
            PArray = new int[_arraySize];

            for (int i = 0; i < _arraySize; i++)
            {
                Up[i] = new MutableValue<sbyte>();
                Down[i] = new MutableValue<sbyte>();
            }
        }

        public ZPCodec(Stream inputStream)
            : this()
        {
            Initializa(inputStream);
        }

        #endregion Constructors

        #region Public Methods

        public int IWDecoder()
        {
            return DecodeSubSimple(0, 0x8000 + ((AValue + AValue + AValue) >> 3));
        }

        public int DecodeSub(MutableValue<sbyte> ctx, int z)
        {
            int bit = ctx.Value & 1;

            int d = 24576 + ((z + AValue) >> 2);

            if (z > d)
                z = d;

            if (z > _code)
            {
                z = 0x10000 - z;
                AValue += z;
                _code += z;
                ctx.Value = Down[0xff & ctx.Value].Value;

                int shift = FFZ(AValue);
                _scount = (short)(_scount - shift);
                AValue = 0xffff & (AValue << shift);
                _code = 0xffff & ((_code << shift) | (((long)_buffer >> _scount) & ((1 << shift) - 1)));

                if (_scount < 16)
                    Preload();

                _fence = _code;

                if (_code >= 32768L)
                    _fence = 32767L;

                return bit ^ 1;
            }

            if ((unchecked((int)0xffffffffL) & AValue) >= (unchecked((int)0xffffffffL) & MArray[0xff & ctx.Value]))
                ctx.Value = Up[0xff & ctx.Value].Value;

            _scount--;
            AValue = 0xffff & (z << 1);
            _code = 0xffff & ((_code << 1) | (((long)_buffer >> _scount) & 1));

            if (_scount < 16)
                Preload();

            _fence = _code;

            if (_code >= 32768L)
                _fence = 32767L;

            return bit;
        }

        public int DecodeSubNolearn(int mps, int z)
        {
            int d = 24576 + ((z + AValue) >> 2);

            if (z > d)
                z = d;

            if (z > _code)
            {
                z = 0x10000 - z;
                AValue += z;
                _code += z;

                int shift = FFZ(AValue);
                _scount = (short)(_scount - shift);
                AValue = 0xffff & (AValue << shift);
                _code = 0xffff & ((_code << shift) | (((long)_buffer >> _scount) & ((1 << shift) - 1)));

                if (_scount < 16)
                    Preload();

                _fence = _code;

                if (_code >= 32768L)
                    _fence = 32767L;

                return mps ^ 1;
            }

            _scount--;
            AValue = 0xffff & (z << 1);
            _code = 0xffff & ((_code << 1) | (((long)_buffer >> _scount) & 1));

            if (_scount < 16)
                Preload();

            _fence = _code;

            if (_code >= 32768L)
                _fence = 32767L;

            return mps;
        }

        public int DecodeSubSimple(int mps, int z)
        {
            if (z > _code)
            {
                z = 0x10000 - z;
                AValue += z;
                _code += z;

                int shift = FFZ(AValue);
                _scount = (short)(_scount - shift);
                AValue = 0xffff & (AValue << shift);
                _code = 0xffff & ((_code << shift) | (((long)_buffer >> _scount) & ((1 << shift) - 1)));

                if (_scount < 16)
                    Preload();

                _fence = _code;

                if (_code >= 32768L)
                    _fence = 32767L;

                return mps ^ 1;
            }

            _scount--;
            AValue = 0xffff & (z << 1);
            _code = 0xffff & ((_code << 1) | (((long)_buffer >> _scount) & 1));

            if (_scount < 16)
                Preload();

            _fence = _code;

            if (_code >= 32768L)
                _fence = 32767L;

            return mps;
        }

        public int Decoder()
        {
            return DecodeSubSimple(0, 0x8000 + (AValue >> 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Decoder(MutableValue<sbyte> ctx)
        {
            int ictx = 0xff & ctx.Value;
            int z = AValue + PArray[ictx];

            if (z <= _fence)
            {
                AValue = z;
                return ictx & 1;
            }
            else
                return DecodeSub(ctx, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FFZ(int x)
        {
            return ((unchecked((int)0xffffffffL) & x) < 65280L) ? Ffzt[0xff & (x >> 8)] : (Ffzt[0xff & x] + 8);
        }

        public ZPCodec Initializa(Stream inputStream)
        {
            InputStream = inputStream;
            DecoderInitialize();

            return this;
        }

        public void NewZPTable(ZPTable[] table)
        {
            for (int i = 0; i < _arraySize; i++)
            {
                PArray[i] = table[i].PValue;
                MArray[i] = table[i].MValue;
                Up[i].Value = (sbyte)table[i].Up;
                Down[i].Value = (sbyte)table[i].Down;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Preload()
        {
            short scount = _scount;
            short zByte = _zByte;
            int buffer = _buffer;

            for (; scount <= 24; scount = (short)(scount + 8))
            {
                zByte = -1;
                zByte = (short)InputStream.ReadByte();

                if (zByte == -1)
                {
                    zByte = 255;

                    if (--_delay < 1)
                        throw new IOException("EOF");
                }
                buffer = (buffer << 8) |(int) zByte;
            }

            _scount = scount;
            _zByte = zByte;
            _buffer = buffer;
        }

        #endregion Public Methods

        #region Private Methods

        private void DecoderInitialize()
        {
            AValue = 0;
            NewZPTable(DefaultZPTable);
            _code = 0xff00;

            try
            {
                _code &= (InputStream.ReadByte() << 8);
                _zByte = (short)(0xff & InputStream.ReadByte());
            }
            catch (IOException)
            {
                _zByte = 255;
            }

            _code |=(long) _zByte;
            _delay = 25;
            _scount = 0;
            Preload();
            _fence = _code;

            if (_code >= 32768L)
                _fence = 32767L;
        }

        /// <summary>
        /// Builds the default version of the ZP Table
        /// </summary>
        /// <returns></returns>
        internal ZPTable[] BuildDefaultZPTable()
        {
            return new[]
                       {
                           new ZPTable(32768, 0, 84, 145),
                           new ZPTable(32768, 0, 3, 4),
                           new ZPTable(32768, 0, 4, 3),
                           new ZPTable(27581, 4261, 5, 1),
                           new ZPTable(27581, 4261, 6, 2),
                           new ZPTable(23877, 7976, 7, 3),
                           new ZPTable(23877, 7976, 8, 4),
                           new ZPTable(20921, 11219, 9, 5),
                           new ZPTable(20921, 11219, 10, 6),
                           new ZPTable(18451, 14051, 11, 7),
                           new ZPTable(18451, 14051, 12, 8),
                           new ZPTable(16341, 16524, 13, 9),
                           new ZPTable(16341, 16524, 14, 10),
                           new ZPTable(14513, 18685, 15, 11),
                           new ZPTable(14513, 18685, 16, 12),
                           new ZPTable(12917, 20573, 17, 13),
                           new ZPTable(12917, 20573, 18, 14),
                           new ZPTable(11517, 22224, 19, 15),
                           new ZPTable(11517, 22224, 20, 16),
                           new ZPTable(10277, 23665, 21, 17),
                           new ZPTable(10277, 23665, 22, 18),
                           new ZPTable(9131, 24923, 23, 19),
                           new ZPTable(9131, 24923, 24, 20),
                           new ZPTable(8071, 26021, 25, 21),
                           new ZPTable(8071, 26021, 26, 22),
                           new ZPTable(7099, 26978, 27, 23),
                           new ZPTable(7099, 26978, 28, 24),
                           new ZPTable(6213, 27810, 29, 25),
                           new ZPTable(6213, 27810, 30, 26),
                           new ZPTable(5411, 28532, 31, 27),
                           new ZPTable(5411, 28532, 32, 28),
                           new ZPTable(4691, 29158, 33, 29),
                           new ZPTable(4691, 29158, 34, 30),
                           new ZPTable(4047, 29700, 35, 31),
                           new ZPTable(4047, 29700, 36, 32),
                           new ZPTable(3477, 30166, 37, 33),
                           new ZPTable(3477, 30166, 38, 34),
                           new ZPTable(2973, 30568, 39, 35),
                           new ZPTable(2973, 30568, 40, 36),
                           new ZPTable(2531, 30914, 41, 37),
                           new ZPTable(2531, 30914, 42, 38),
                           new ZPTable(2145, 31210, 43, 39),
                           new ZPTable(2145, 31210, 44, 40),
                           new ZPTable(1809, 31463, 45, 41),
                           new ZPTable(1809, 31463, 46, 42),
                           new ZPTable(1521, 31678, 47, 43),
                           new ZPTable(1521, 31678, 48, 44),
                           new ZPTable(1273, 31861, 49, 45),
                           new ZPTable(1273, 31861, 50, 46),
                           new ZPTable(1061, 32015, 51, 47),
                           new ZPTable(1061, 32015, 52, 48),
                           new ZPTable(881, 32145, 53, 49),
                           new ZPTable(881, 32145, 54, 50),
                           new ZPTable(729, 32254, 55, 51),
                           new ZPTable(729, 32254, 56, 52),
                           new ZPTable(601, 32346, 57, 53),
                           new ZPTable(601, 32346, 58, 54),
                           new ZPTable(493, 32422, 59, 55),
                           new ZPTable(493, 32422, 60, 56),
                           new ZPTable(403, 32486, 61, 57),
                           new ZPTable(403, 32486, 62, 58),
                           new ZPTable(329, 32538, 63, 59),
                           new ZPTable(329, 32538, 64, 60),
                           new ZPTable(267, 32581, 65, 61),
                           new ZPTable(267, 32581, 66, 62),
                           new ZPTable(213, 32619, 67, 63),
                           new ZPTable(213, 32619, 68, 64),
                           new ZPTable(165, 32653, 69, 65),
                           new ZPTable(165, 32653, 70, 66),
                           new ZPTable(123, 32682, 71, 67),
                           new ZPTable(123, 32682, 72, 68),
                           new ZPTable(87, 32707, 73, 69),
                           new ZPTable(87, 32707, 74, 70),
                           new ZPTable(59, 32727, 75, 71),
                           new ZPTable(59, 32727, 76, 72),
                           new ZPTable(35, 32743, 77, 73),
                           new ZPTable(35, 32743, 78, 74),
                           new ZPTable(19, 32754, 79, 75),
                           new ZPTable(19, 32754, 80, 76),
                           new ZPTable(7, 32762, 81, 77),
                           new ZPTable(7, 32762, 82, 78),
                           new ZPTable(1, 32767, 81, 79),
                           new ZPTable(1, 32767, 82, 80),
                           new ZPTable(22165, 0, 9, 85),
                           new ZPTable(9454, 0, 86, 226),
                           new ZPTable(32768, 0, 5, 6),
                           new ZPTable(3376, 0, 88, 176),
                           new ZPTable(18458, 0, 89, 143),
                           new ZPTable(1153, 0, 90, 138),
                           new ZPTable(13689, 0, 91, 141),
                           new ZPTable(378, 0, 92, 112),
                           new ZPTable(9455, 0, 93, 135),
                           new ZPTable(123, 0, 94, 104),
                           new ZPTable(6520, 0, 95, 133),
                           new ZPTable(40, 0, 96, 100),
                           new ZPTable(4298, 0, 97, 129),
                           new ZPTable(13, 0, 82, 98),
                           new ZPTable(2909, 0, 99, 127),
                           new ZPTable(52, 0, 76, 72),
                           new ZPTable(1930, 0, 101, 125),
                           new ZPTable(160, 0, 70, 102),
                           new ZPTable(1295, 0, 103, 123),
                           new ZPTable(279, 0, 66, 60),
                           new ZPTable(856, 0, 105, 121),
                           new ZPTable(490, 0, 106, 110),
                           new ZPTable(564, 0, 107, 119),
                           new ZPTable(324, 0, 66, 108),
                           new ZPTable(371, 0, 109, 117),
                           new ZPTable(564, 0, 60, 54),
                           new ZPTable(245, 0, 111, 115),
                           new ZPTable(851, 0, 56, 48),
                           new ZPTable(161, 0, 69, 113),
                           new ZPTable(1477, 0, 114, 134),
                           new ZPTable(282, 0, 65, 59),
                           new ZPTable(975, 0, 116, 132),
                           new ZPTable(426, 0, 61, 55),
                           new ZPTable(645, 0, 118, 130),
                           new ZPTable(646, 0, 57, 51),
                           new ZPTable(427, 0, 120, 128),
                           new ZPTable(979, 0, 53, 47),
                           new ZPTable(282, 0, 122, 126),
                           new ZPTable(1477, 0, 49, 41),
                           new ZPTable(186, 0, 124, 62),
                           new ZPTable(2221, 0, 43, 37),
                           new ZPTable(122, 0, 72, 66),
                           new ZPTable(3276, 0, 39, 31),
                           new ZPTable(491, 0, 60, 54),
                           new ZPTable(4866, 0, 33, 25),
                           new ZPTable(742, 0, 56, 50),
                           new ZPTable(7041, 0, 29, 131),
                           new ZPTable(1118, 0, 52, 46),
                           new ZPTable(9455, 0, 23, 17),
                           new ZPTable(1680, 0, 48, 40),
                           new ZPTable(10341, 0, 23, 15),
                           new ZPTable(2526, 0, 42, 136),
                           new ZPTable(14727, 0, 137, 7),
                           new ZPTable(3528, 0, 38, 32),
                           new ZPTable(11417, 0, 21, 139),
                           new ZPTable(4298, 0, 140, 172),
                           new ZPTable(15199, 0, 15, 9),
                           new ZPTable(2909, 0, 142, 170),
                           new ZPTable(22165, 0, 9, 85),
                           new ZPTable(1930, 0, 144, 168),
                           new ZPTable(32768, 0, 141, 248),
                           new ZPTable(1295, 0, 146, 166),
                           new ZPTable(9454, 0, 147, 247),
                           new ZPTable(856, 0, 148, 164),
                           new ZPTable(3376, 0, 149, 197),
                           new ZPTable(564, 0, 150, 162),
                           new ZPTable(1153, 0, 151, 95),
                           new ZPTable(371, 0, 152, 160),
                           new ZPTable(378, 0, 153, 173),
                           new ZPTable(245, 0, 154, 158),
                           new ZPTable(123, 0, 155, 165),
                           new ZPTable(161, 0, 70, 156),
                           new ZPTable(40, 0, 157, 161),
                           new ZPTable(282, 0, 66, 60),
                           new ZPTable(13, 0, 81, 159),
                           new ZPTable(426, 0, 62, 56),
                           new ZPTable(52, 0, 75, 71),
                           new ZPTable(646, 0, 58, 52),
                           new ZPTable(160, 0, 69, 163),
                           new ZPTable(979, 0, 54, 48),
                           new ZPTable(279, 0, 65, 59),
                           new ZPTable(1477, 0, 50, 42),
                           new ZPTable(490, 0, 167, 171),
                           new ZPTable(2221, 0, 44, 38),
                           new ZPTable(324, 0, 65, 169),
                           new ZPTable(3276, 0, 40, 32),
                           new ZPTable(564, 0, 59, 53),
                           new ZPTable(4866, 0, 34, 26),
                           new ZPTable(851, 0, 55, 47),
                           new ZPTable(7041, 0, 30, 174),
                           new ZPTable(1477, 0, 175, 193),
                           new ZPTable(9455, 0, 24, 18),
                           new ZPTable(975, 0, 177, 191),
                           new ZPTable(11124, 0, 178, 222),
                           new ZPTable(645, 0, 179, 189),
                           new ZPTable(8221, 0, 180, 218),
                           new ZPTable(427, 0, 181, 187),
                           new ZPTable(5909, 0, 182, 216),
                           new ZPTable(282, 0, 183, 185),
                           new ZPTable(4023, 0, 184, 214),
                           new ZPTable(186, 0, 69, 61),
                           new ZPTable(2663, 0, 186, 212),
                           new ZPTable(491, 0, 59, 53),
                           new ZPTable(1767, 0, 188, 210),
                           new ZPTable(742, 0, 55, 49),
                           new ZPTable(1174, 0, 190, 208),
                           new ZPTable(1118, 0, 51, 45),
                           new ZPTable(781, 0, 192, 206),
                           new ZPTable(1680, 0, 47, 39),
                           new ZPTable(518, 0, 194, 204),
                           new ZPTable(2526, 0, 41, 195),
                           new ZPTable(341, 0, 196, 202),
                           new ZPTable(3528, 0, 37, 31),
                           new ZPTable(225, 0, 198, 200),
                           new ZPTable(11124, 0, 199, 243),
                           new ZPTable(148, 0, 72, 64),
                           new ZPTable(8221, 0, 201, 239),
                           new ZPTable(392, 0, 62, 56),
                           new ZPTable(5909, 0, 203, 237),
                           new ZPTable(594, 0, 58, 52),
                           new ZPTable(4023, 0, 205, 235),
                           new ZPTable(899, 0, 54, 48),
                           new ZPTable(2663, 0, 207, 233),
                           new ZPTable(1351, 0, 50, 44),
                           new ZPTable(1767, 0, 209, 231),
                           new ZPTable(2018, 0, 46, 38),
                           new ZPTable(1174, 0, 211, 229),
                           new ZPTable(3008, 0, 40, 34),
                           new ZPTable(781, 0, 213, 227),
                           new ZPTable(4472, 0, 36, 28),
                           new ZPTable(518, 0, 215, 225),
                           new ZPTable(6618, 0, 30, 22),
                           new ZPTable(341, 0, 217, 223),
                           new ZPTable(9455, 0, 26, 16),
                           new ZPTable(225, 0, 219, 221),
                           new ZPTable(12814, 0, 20, 220),
                           new ZPTable(148, 0, 71, 63),
                           new ZPTable(17194, 0, 14, 8),
                           new ZPTable(392, 0, 61, 55),
                           new ZPTable(17533, 0, 14, 224),
                           new ZPTable(594, 0, 57, 51),
                           new ZPTable(24270, 0, 8, 2),
                           new ZPTable(899, 0, 53, 47),
                           new ZPTable(32768, 0, 228, 87),
                           new ZPTable(1351, 0, 49, 43),
                           new ZPTable(18458, 0, 230, 246),
                           new ZPTable(2018, 0, 45, 37),
                           new ZPTable(13689, 0, 232, 244),
                           new ZPTable(3008, 0, 39, 33),
                           new ZPTable(9455, 0, 234, 238),
                           new ZPTable(4472, 0, 35, 27),
                           new ZPTable(6520, 0, 138, 236),
                           new ZPTable(6618, 0, 29, 21),
                           new ZPTable(10341, 0, 24, 16),
                           new ZPTable(9455, 0, 25, 15),
                           new ZPTable(14727, 0, 240, 8),
                           new ZPTable(12814, 0, 19, 241),
                           new ZPTable(11417, 0, 22, 242),
                           new ZPTable(17194, 0, 13, 7),
                           new ZPTable(15199, 0, 16, 10),
                           new ZPTable(17533, 0, 13, 245),
                           new ZPTable(22165, 0, 10, 2),
                           new ZPTable(24270, 0, 7, 1),
                           new ZPTable(32768, 0, 244, 83),
                           new ZPTable(32768, 0, 249, 250),
                           new ZPTable(22165, 0, 10, 2),
                           new ZPTable(18458, 0, 89, 143),
                           new ZPTable(18458, 0, 230, 246),
                           new ZPTable(0, 0, 0, 0),
                           new ZPTable(0, 0, 0, 0),
                           new ZPTable(0, 0, 0, 0),
                           new ZPTable(0, 0, 0, 0),
                           new ZPTable(0, 0, 0, 0)
                       };
        }

        #endregion Private Methods
    }
}