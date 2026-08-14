using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using DjvuNet.Compression;
using DjvuNet.Graphics;
using DjvuNet.Errors;

namespace DjvuNet.JB2
{
    public class JB2Decoder : JB2Codec, IDisposable
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

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _Coder?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        #region Public Methods

        public void Code(JB2Image jim)
        {
            int rectype = StartOfData;

            do
            {
                rectype = CodeRecordB(rectype, jim, ref Unsafe.NullRef<JB2Shape>(), ref Unsafe.NullRef<JB2Blit>());
            } while (rectype != EndOfData);

            if (!_GotStartRecordP)
            {
                DjvuExceptionUtil.ThrowFormatException("JB2 decoding failed: Missing required start record.");
            }
        }

        public void Code(JB2Dictionary jim)
        {
            int rectype = StartOfData;

            do
            {
                rectype = CodeRecordA(rectype, jim, ref Unsafe.NullRef<JB2Shape>());
            } while (rectype != EndOfData);

            if (!_GotStartRecordP)
            {
                DjvuExceptionUtil.ThrowFormatException("JB2 decoding failed: Missing required start record.");
            }
        }

        public void Init(Stream stream, JB2Dictionary zdict)
        {
            this._ZDict = zdict;
            _Coder = new ZPCodec(stream);
        }

        public void Init(IBinaryReader gbs, JB2Dictionary zdict)
        {
            Init(gbs.BaseStream, zdict);
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
        protected int CodeNum(int low, int high, MutableValue<int> ctx)
        {
            int result = CodeNum(low, high, ctx, 0);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void CodeAbsoluteLocation(ref JB2Blit jblt, int rows, int columns)
        {
            if (!_GotStartRecordP)
            {
                DjvuExceptionUtil.ThrowFormatException("JB2 decoding failed: Missing required start record.");
            }

            int left = CodeNum(1, _ImageColumns, _AbsLocX);
            int top = CodeNum(1, _ImageRows, _AbsLocY);
            jblt.Bottom = top - rows;
            jblt.Left = left - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void CodeAbsoluteMarkSize(ref Bitmap bm, int border)
        {
            int xsize = CodeNum(0, BigPositive, _AbsSizeX);
            int ysize = CodeNum(0, BigPositive, _AbsSizeY);

            if ((xsize != (0xffff & xsize)) || (ysize != (0xffff & ysize)))
            {
                DjvuExceptionUtil.ThrowFormatException("JB2 decoding failed: Decoded value falls outside permitted bounds.");
            }

            bm.Init(ysize, xsize, border);
        }

        protected override void CodeBitmapByCrossCoding(ref Bitmap bm, ref Bitmap cbm, int xd2c, int dw, int dy,
                                                                     int cy, int up1, int up0, int xup1, int xup0,
                                                                     int xdn1)
        {

            while (dy >= 0)
            {
                int context = GetCrossContext(ref bm, ref cbm, up1, up0, xup1, xup0, xdn1, 0);

                for (int dx = 0; dx < dw; )
                {
                    int n = CodeBit(false, _CBitDist, context);
                    bm.SetByteAt(up0 + dx++, (sbyte) n);
                    context = ShiftCrossContext(ref bm, ref cbm, context, n, up1, up0, xup1, xup0, xdn1, dx);
                }

                up1 = up0;
                up0 = bm.RowOffset(--dy);
                xup1 = xup0;
                xup0 = xdn1;
                xdn1 = cbm.RowOffset((--cy) - 1) + xd2c;
            }
        }

        protected override unsafe void CodeBitmapByCrossCoding(ref Bitmap bm, ref Bitmap cbm, int xd2c, int dw, int dy,
                                                                     int cy, sbyte* pUp1, sbyte* pUp0, sbyte* pXup1, sbyte* pXup0,
                                                                     sbyte* pXdn1)
        {
            while (dy >= 0)
            {
                int context = GetCrossContext(pUp1, pUp0, pXup1, pXup0, pXdn1, 0);

                for (int dx = 0; dx < dw; )
                {
                    int n = CodeBit(false, _CBitDist, context);
                    pUp0[dx++] = (sbyte)n;
                    context = ShiftCrossContext(context, n, pUp1, pUp0, pXup1, pXup0, pXdn1, dx);
                }

                pUp1 = pUp0;
                pUp0 = bm.GetRow(--dy);
                pXup1 = pXup0;
                pXup0 = pXdn1;
                pXdn1 = cbm.GetRow((--cy) - 1) + xd2c;
            }
        }

        protected override void CodeBitmapDirectly(ref Bitmap bm, int dw, int dy, int up2, int up1, int up0)
        {
            while (dy >= 0)
            {
                int context = GetDirectContext(ref bm, up2, up1, up0, 0);

                for (int dx = 0; dx < dw; )
                {
                    int n = CodeBit(false, _BitDist, context);

                    bm.SetByteAt(up0 + dx++, (sbyte) n);
                    context = ShiftDirectContext(ref bm, context, n, up2, up1, up0, dx);
                }

                up2 = up1;
                up1 = up0;
                up0 = bm.RowOffset(--dy);
            }
        }

        protected override unsafe void CodeBitmapDirectly(ref Bitmap bm, int dw, int dy, sbyte* pUp2, sbyte* pUp1, sbyte* pUp0)
        {
            while (dy >= 0)
            {
                int context = GetDirectContext(pUp2, pUp1, pUp0, 0);

                for (int dx = 0; dx < dw; )
                {
                    int n = CodeBit(false, _BitDist, context);

                    pUp0[dx++] = (sbyte)n;
                    context = ShiftDirectContext(context, n, pUp2, pUp1, pUp0, dx);
                }

                pUp2 = pUp1;
                pUp1 = pUp0;
                pUp0 = bm.GetRow(--dy);
            }
        }

        protected override String CodeComment(String comment)
        {
            int size = CodeNum(0, BigPositive, _DistCommentLength);
            byte[] combuf = new byte[size];

            for (int i = 0; i < combuf.Length; i++)
            {
                combuf[i] = (byte)CodeNum(0, 255, _DistCommentByte);
            }

            return Encoding.UTF8.GetString(combuf);
        }

        protected override void CodeImageSize(JB2Dictionary jim)
        {
            int w = CodeNum(0, BigPositive, _ImageSizeDist);
            int h = CodeNum(0, BigPositive, _ImageSizeDist);

            if ((w != 0) || (h != 0))
            {
                DjvuExceptionUtil.ThrowFormatException("JB2 decoding failed: Dictionary contains non-zero image dimensions.");
            }

            base.CodeImageSize(jim);
        }

        protected override void CodeImageSize(JB2Image jim)
        {
            _ImageColumns = CodeNum(0, BigPositive, _ImageSizeDist);
            _ImageRows = CodeNum(0, BigPositive, _ImageSizeDist);

            if ((_ImageColumns == 0) || (_ImageRows == 0))
            {
                DjvuExceptionUtil.ThrowFormatException("JB2 decoding failed: Image dimensions cannot be zero.");
            }

            jim.Width = _ImageColumns;
            jim.Height = _ImageRows;
            base.CodeImageSize(jim);
        }

        protected override void CodeInheritedShapeCount(JB2Dictionary jim)
        {
            int size = CodeNum(0, BigPositive, _InheritedShapeCountDist);
            JB2Dictionary dict = jim.InheritedDictionary;

            if ((dict == null) && (size > 0))
            {
                if (_ZDict != null)
                {
                    dict = _ZDict;
                    jim.InheritedDictionary = dict;
                }
                else
                {
                    DjvuExceptionUtil.ThrowFormatException("JB2 decoding failed: A shape dictionary is required but was not provided.");
                }
            }

            if ((dict != null) && (size != dict.ShapeCount))
            {
                DjvuExceptionUtil.ThrowFormatException("JB2 decoding failed: Shape dictionary size mismatch.");
            }
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
        protected override void CodeRelativeMarkSize(ref Bitmap bm, int cw, int ch, int border)
        {
            int xdiff = CodeNum(BigNegative, BigPositive, _RelSizeX);
            int ydiff = CodeNum(BigNegative, BigPositive, _RelSizeY);
            int xsize = cw + xdiff;
            int ysize = ch + ydiff;

            if ((xsize != (0xffff & xsize)) || (ysize != (0xffff & ysize)))
            {
                DjvuExceptionUtil.ThrowFormatException("JB2 decoding failed: Decoded value falls outside permitted bounds.");
            }

            bm.Init(ysize, xsize, border);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override int GetDiff(int ignored, MutableValue<int> rel_loc)
        {
            int result = CodeNum(BigNegative, BigPositive, rel_loc);
            return result;
        }

        #endregion Protected Methods
    }
}
