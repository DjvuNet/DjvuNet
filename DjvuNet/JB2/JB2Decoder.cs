using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using DjvuNet.Compression;
using DjvuNet.Graphics;

namespace DjvuNet.JB2
{
    public class JB2Decoder : JB2Codec
    {
        #region Internal Fields

        internal JB2Dictionary _ZDict;
        internal ZPCodec _Coder;
        internal byte _ZpBitHolder;

        #endregion Internal Fields

        #region Constructors

        /// <summary>
        /// Creates a new Decoder object.
        /// </summary>
        public JB2Decoder()
            : base(false)
        {
        }

        #endregion Constructors

        #region Public Methods

        public void Code(JB2Image jim)
        {
            int rectype = StartOfData;

            do
            {
                rectype = CodeRecordB(rectype, jim, null, null);
            } while (rectype != EndOfData);

            if (!_GotStartRecordP)
                throw new DjvuFormatException("Image has no start record");
        }

        public void Code(JB2Dictionary jim)
        {
            int rectype = StartOfData;

            do
            {
                rectype = CodeRecordA(rectype, jim, null);
            } while (rectype != EndOfData);

            if (!_GotStartRecordP)
                throw new DjvuFormatException("Image has no start record");
        }

        public void Init(IBinaryReader gbs, JB2Dictionary zdict)
        {
            this._ZDict = zdict;
            _Coder = new ZPCodec(gbs.BaseStream);
        }

        #endregion Public Methods

        #region Protected Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool CodeBit(bool ignored, MutableValue<sbyte> ctx)
        {
            byte ctxVal = unchecked((byte)ctx.Value);
            int value = _Coder.Decoder(ref ctxVal);
            ctx.Value = (sbyte) ctxVal;
            return (value != 0);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override int CodeBit(bool ignored, sbyte[] array, int offset)
        {
            _ZpBitHolder = unchecked((byte )array[offset]);
            int retval = _Coder.Decoder(ref _ZpBitHolder);
            array[offset] = unchecked((sbyte)_ZpBitHolder);
            return retval;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual int CodeNum(int low, int high, MutableValue<int> ctx)
        {
            int result = CodeNum(low, high, ctx, 0);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void CodeAbsoluteLocation(JB2Blit jblt, int rows, int columns)
        {
            if (!_GotStartRecordP)
                throw new DjvuFormatException("Image no start");

            int left = CodeNum(1, _ImageColumns, _AbsLocX);
            int top = CodeNum(1, _ImageRows, _AbsLocY);
            jblt.Bottom = top - rows;
            jblt.Left = left - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void CodeAbsoluteMarkSize(IBitmap bm, int border)
        {
            int xsize = CodeNum(0, Bigpositive, _AbsSizeX);
            int ysize = CodeNum(0, Bigpositive, _AbsSizeY);

            if ((xsize != (0xffff & xsize)) || (ysize != (0xffff & ysize)))
                throw new DjvuFormatException("Image bad number");

            bm.Init(ysize, xsize, border);
        }

        protected override void CodeBitmapByCrossCoding(IBitmap bm, IBitmap cbm, int xd2c, int dw, int dy,
                                                                     int cy, int up1, int up0, int xup1, int xup0,
                                                                     int xdn1)
        {
            while (dy >= 0)
            {
                int context = GetCrossContext(bm, cbm, up1, up0, xup1, xup0, xdn1, 0);

                for (int dx = 0; dx < dw; )
                {
                    int n = CodeBit(false, _CBitDist, context);
                    bm.SetByteAt(up0 + dx++, (sbyte) n);
                    context = ShiftCrossContext(bm, cbm, context, n, up1, up0, xup1, xup0, xdn1, dx);
                }

                up1 = up0;
                up0 = bm.RowOffset(--dy);
                xup1 = xup0;
                xup0 = xdn1;
                xdn1 = cbm.RowOffset((--cy) - 1) + xd2c;
            }
        }

        protected override void CodeBitmapDirectly(IBitmap bm, int dw, int dy, int up2, int up1, int up0)
        {
            while (dy >= 0)
            {
                int context = GetDirectContext(bm, up2, up1, up0, 0);

                for (int dx = 0; dx < dw; )
                {
                    int n = CodeBit(false, _BitDist, context);

                    bm.SetByteAt(up0 + dx++, (sbyte) n);
                    context = ShiftDirectContext(bm, context, n, up2, up1, up0, dx);
                }

                up2 = up1;
                up1 = up0;
                up0 = bm.RowOffset(--dy);
            }
        }

        protected override String CodeComment(String comment)
        {
            int size = CodeNum(0, Bigpositive, _DistCommentLength);
            byte[] combuf = new byte[size];

            for (int i = 0; i < combuf.Length; i++)
                combuf[i] = (byte)CodeNum(0, 255, _DistCommentByte);

            string result = Encoding.UTF8.GetString(combuf);
            return result;
        }

        protected override void CodeImageSize(JB2Dictionary jim)
        {
            int w = CodeNum(0, Bigpositive, _ImageSizeDist);
            int h = CodeNum(0, Bigpositive, _ImageSizeDist);

            if ((w != 0) || (h != 0))
                throw new DjvuFormatException("Image bad dict 2");

            base.CodeImageSize(jim);
        }

        protected override void CodeImageSize(JB2Image jim)
        {
            _ImageColumns = CodeNum(0, Bigpositive, _ImageSizeDist);
            _ImageRows = CodeNum(0, Bigpositive, _ImageSizeDist);

            if ((_ImageColumns == 0) || (_ImageRows == 0))
                throw new DjvuFormatException("Image with zero size");

            jim.Width = _ImageColumns;
            jim.Height = _ImageRows;
            base.CodeImageSize(jim);
        }

        protected override void CodeInheritedShapeCount(JB2Dictionary jim)
        {
            int size = CodeNum(0, Bigpositive, _InheritedShapeCountDist);
            JB2Dictionary dict = jim.InheritedDictionary;

            if ((dict == null) && (size > 0))
            {
                if (_ZDict != null)
                {
                    dict = _ZDict;
                    jim.InheritedDictionary = dict;
                }
                else
                    throw new DjvuFormatException("Image dictionary not provided");
            }

            if ((dict != null) && (size != dict.ShapeCount))
                throw new DjvuFormatException("Image dictionary is invalid");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override int CodeMatchIndex(int index, JB2Dictionary ignored)
        {
            int result = CodeNum(0, _Lib2Shape.Count - 1, _DistMatchIndex);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override int CodeRecordType(int ignored)
        {
            int result = CodeNum(StartOfData, EndOfData, _DistRecordType);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void CodeRelativeMarkSize(IBitmap bm, int cw, int ch, int border)
        {
            int xdiff = CodeNum(Bignegative, Bigpositive, _RelSizeX);
            int ydiff = CodeNum(Bignegative, Bigpositive, _RelSizeY);
            int xsize = cw + xdiff;
            int ysize = ch + ydiff;

            if ((xsize != (0xffff & xsize)) || (ysize != (0xffff & ysize)))
                throw new DjvuFormatException("Image decoder value");

            bm.Init(ysize, xsize, border);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override int GetDiff(int ignored, MutableValue<int> rel_loc)
        {
            int result = CodeNum(Bignegative, Bigpositive, rel_loc);
            return result;
        }

        #endregion Protected Methods
    }
}