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
        #region Private Members

        private JB2Dictionary _zdict;
        private ZPCodec _zp;

        #endregion Private Members

        #region Protected Members

        protected MutableValue<sbyte> ZpBitHolder = new MutableValue<sbyte>();

        #endregion Protected Members

        #region Constructors

        /// <summary>
        /// Creates a new Decoder object.
        /// </summary>
        public JB2Decoder()
            : base(false)
        {
            // Nothing
        }

        #endregion Constructors

        #region Public Methods

        public virtual void Code(JB2Image jim)
        {
            int rectype = StartOfData;

            do
            {
                rectype = CodeRecordB(rectype, jim, null, null);
            } while (rectype != EndOfData);

            if (!GotStartRecordP)
                throw new DjvuFormatException("Image has no start record");
        }

        public virtual void Code(JB2Dictionary jim)
        {
            int rectype = StartOfData;

            do
            {
                rectype = CodeRecordA(rectype, jim, null);
            } while (rectype != EndOfData);

            if (!GotStartRecordP)
                throw new DjvuFormatException("Image has no start record");
        }

        public virtual void Init(IBinaryReader gbs, JB2Dictionary zdict)
        {
            this._zdict = zdict;
            _zp = new ZPCodec(gbs.BaseStream);
        }

        #endregion Public Methods

        #region Protected Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool CodeBit(bool ignored, MutableValue<sbyte> ctx)
        {
            int value = _zp.Decoder(ctx);
            return (value != 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override int CodeBit(bool ignored, sbyte[] array, int offset)
        {
            ZpBitHolder.Value = array[offset];
            int retval = _zp.Decoder(ZpBitHolder);
            array[offset] = ZpBitHolder.Value;
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
            if (!GotStartRecordP)
                throw new DjvuFormatException("Image no start");

            int left = CodeNum(1, ImageColumns, AbsLocX);
            int top = CodeNum(1, ImageRows, AbsLocY);
            jblt.Bottom = top - rows;
            jblt.Left = left - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void CodeAbsoluteMarkSize(Bitmap bm, int border)
        {
            int xsize = CodeNum(0, Bigpositive, AbsSizeX);
            int ysize = CodeNum(0, Bigpositive, AbsSizeY);

            if ((xsize != (0xffff & xsize)) || (ysize != (0xffff & ysize)))
                throw new DjvuFormatException("Image bad number");

            bm.Init(ysize, xsize, border);
        }

        protected override void CodeBitmapByCrossCoding(Bitmap bm, Bitmap cbm, int xd2c, int dw, int dy,
                                                                     int cy, int up1, int up0, int xup1, int xup0,
                                                                     int xdn1)
        {
            while (dy >= 0)
            {
                int context = GetCrossContext(bm, cbm, up1, up0, xup1, xup0, xdn1, 0);

                for (int dx = 0; dx < dw; )
                {
                    int n = CodeBit(false, Cbitdist, context);
                    bm.SetByteAt(up0 + dx++, n);
                    context = ShiftCrossContext(bm, cbm, context, n, up1, up0, xup1, xup0, xdn1, dx);
                }

                up1 = up0;
                up0 = bm.RowOffset(--dy);
                xup1 = xup0;
                xup0 = xdn1;
                xdn1 = cbm.RowOffset((--cy) - 1) + xd2c;
            }
        }

        protected override void CodeBitmapDirectly(Bitmap bm, int dw, int dy, int up2, int up1, int up0)
        {
            while (dy >= 0)
            {
                int context = GetDirectContext(bm, up2, up1, up0, 0);

                for (int dx = 0; dx < dw; )
                {
                    int n = CodeBit(false, Bitdist, context);

                    bm.SetByteAt(up0 + dx++, n);
                    context = ShiftDirectContext(bm, context, n, up2, up1, up0, dx);
                }

                up2 = up1;
                up1 = up0;
                up0 = bm.RowOffset(--dy);
            }
        }

        protected override String CodeComment(String comment)
        {
            int size = CodeNum(0, Bigpositive, DistCommentLength);
            byte[] combuf = new byte[size];

            for (int i = 0; i < combuf.Length; i++)
                combuf[i] = (byte)CodeNum(0, 255, DistCommentByte);

            string result = Encoding.UTF8.GetString(combuf);
            return result;
        }

        protected override void CodeImageSize(JB2Dictionary jim)
        {
            int w = CodeNum(0, Bigpositive, ImageSizeDist);
            int h = CodeNum(0, Bigpositive, ImageSizeDist);

            if ((w != 0) || (h != 0))
                throw new DjvuFormatException("Image bad dict 2");

            base.CodeImageSize(jim);
        }

        protected override void CodeImageSize(JB2Image jim)
        {
            ImageColumns = CodeNum(0, Bigpositive, ImageSizeDist);
            ImageRows = CodeNum(0, Bigpositive, ImageSizeDist);

            if ((ImageColumns == 0) || (ImageRows == 0))
                throw new DjvuFormatException("Image with zero size");

            jim.Width = ImageColumns;
            jim.Height = ImageRows;
            base.CodeImageSize(jim);
        }

        protected override void CodeInheritedShapeCount(JB2Dictionary jim)
        {
            int size = CodeNum(0, Bigpositive, InheritedShapeCountDist);
            JB2Dictionary dict = jim.InheritedDictionary;

            if ((dict == null) && (size > 0))
            {
                if (_zdict != null)
                {
                    dict = _zdict;
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
            int result = CodeNum(0, Lib2Shape.Count - 1, DistMatchIndex);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override int CodeRecordType(int ignored)
        {
            int result = CodeNum(StartOfData, EndOfData, DistRecordType);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void CodeRelativeMarkSize(Bitmap bm, int cw, int ch, int border)
        {
            int xdiff = CodeNum(Bignegative, Bigpositive, RelSizeX);
            int ydiff = CodeNum(Bignegative, Bigpositive, RelSizeY);
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