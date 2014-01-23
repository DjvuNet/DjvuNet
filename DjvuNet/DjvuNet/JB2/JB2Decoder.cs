using System;
using System.IO;
using System.Text;
using DjvuNet.Compression;
using DjvuNet.Graphics;

namespace DjvuNet.JB2
{
    public class JB2Decoder : JB2Codec
    {
        #region Private Variables

        private JB2Dictionary _zdict;
        private ZPCodec _zp;

        #endregion Private Variables

        #region Protected Variables

        protected MutableValue<sbyte> ZpBitHolder = new MutableValue<sbyte>();

        #endregion Protected Variables

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
                //Console.WriteLine("Jb2D c(i) rectype in: " + rectype);
                rectype = CodeRecordB(rectype, jim, null, null);
                //Console.WriteLine("Jb2D c(i) rectype out: " + rectype);
            } while (rectype != EndOfData);

            if (!GotStartRecordP)
            {
                throw new SystemException("Image no start");
            }
        }

        public virtual void Code(JB2Dictionary jim)
        {
            int rectype = StartOfData;

            do
            {
                //Console.WriteLine("Jb2D c(d) rectype in: " + rectype);
                rectype = CodeRecordA(rectype, jim, null);
                //Console.WriteLine("Jb2D c(d) rectype out: " + rectype);
            } while (rectype != EndOfData);

            if (!GotStartRecordP)
            {
                throw new SystemException("Image no start");
            }
        }

        public virtual void Init(BinaryReader gbs, JB2Dictionary zdict)
        {
            this._zdict = zdict;
            _zp = new ZPCodec(gbs.BaseStream);
        }

        #endregion Public Methods

        #region Protected Methods

        protected override bool CodeBit(bool ignored, MutableValue<sbyte> ctx)
        {
            int value = _zp.Decoder(ctx);

            return (value != 0);
        }

        protected override int CodeBit(bool ignored, sbyte[] array, int offset)
        {
            ZpBitHolder.Value = array[offset];

            int retval = _zp.Decoder(ZpBitHolder);
            array[offset] = ZpBitHolder.Value;

            return retval;
        }

        protected virtual int CodeNum(int low, int high, MutableValue<int> ctx)
        {
            int result = CodeNum(low, high, ctx, 0);

            return result;
        }

        protected override void CodeAbsoluteLocation(JB2Blit jblt, int rows, int columns)
        {
            if (!GotStartRecordP)
            {
                throw new SystemException("Image no start");
            }

            int left = CodeNum(1, ImageColumns, AbsLocX);
            int top = CodeNum(1, ImageRows, AbsLocY);

            jblt.Bottom = top - rows;
            jblt.Left = left - 1;
        }

        protected override void CodeAbsoluteMarkSize(Bitmap bm, int border)
        {
            int xsize = CodeNum(0, Bigpositive, AbsSizeX);
            int ysize = CodeNum(0, Bigpositive, AbsSizeY);

            if ((xsize != (0xffff & xsize)) || (ysize != (0xffff & ysize)))
            {
                throw new SystemException("Image bad number");
            }

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
            {
                combuf[i] = (byte)CodeNum(0, 255, DistCommentByte);
            }

            string result = Encoding.UTF8.GetString(combuf);

            return result;
        }

        protected override void CodeImageSize(JB2Dictionary jim)
        {
            //Console.WriteLine("Jb2D cis(d) image_size_dist: " + image_size_dist.Value);

            int w = CodeNum(0, Bigpositive, ImageSizeDist);
            int h = CodeNum(0, Bigpositive, ImageSizeDist);

            //Console.WriteLine("Jb2D cis(d) w: " + w);
            //Console.WriteLine("Jb2D cis(d) h: " + h);

            //Console.WriteLine("Jb2D cis(d) image_size_dist: " + image_size_dist.Value);

            if ((w != 0) || (h != 0))
            {
                throw new SystemException("Image bad dict 2");
            }

            base.CodeImageSize(jim);
        }

        protected override void CodeImageSize(JB2Image jim)
        {
            //Console.WriteLine("Jb2D cis(im) image_size_dist: " + image_size_dist.Value);

            ImageColumns = CodeNum(0, Bigpositive, ImageSizeDist);
            ImageRows = CodeNum(0, Bigpositive, ImageSizeDist);

            //Console.WriteLine("Jb2D cis(im) image_columns: " + image_columns);
            //Console.WriteLine("Jb2D cis(im) image_rows: " + image_rows);

            //Console.WriteLine("Jb2D cis(im) image_size_dist: " + image_size_dist.Value);

            if ((ImageColumns == 0) || (ImageRows == 0))
            {
                throw new SystemException("Image zero size");
            }

            jim.Width = ImageColumns;
            jim.Height = ImageRows;
            base.CodeImageSize(jim);
        }

        protected override void CodeInheritedShapeCount(JB2Dictionary jim)
        {
            //Console.WriteLine("Jb2D cisc(d) inherited_shape_count_dist: " + inherited_shape_count_dist.Value);

            int size = CodeNum(0, Bigpositive, InheritedShapeCountDist);

            //Console.WriteLine("Jb2D cisc(d) size: " + size);
            //Console.WriteLine("Jb2D cisc(d) inherited_shape_count_dist: " + inherited_shape_count_dist.Value);

            JB2Dictionary dict = jim.InheritedDictionary;

            //Console.WriteLine("Jb2D cisc(d) dictnull: " + dict == null);
            //Console.WriteLine("Jb2D cisc(d) zdictnull: " + zdict == null);

            if ((dict == null) && (size > 0))
            {
                if (_zdict != null)
                {
                    dict = _zdict;
                    jim.InheritedDictionary = dict;
                }
                else
                {
                    throw new SystemException("Image dictionary not provided");
                }
            }

            if ((dict != null) && (size != dict.ShapeCount))
            {
                throw new SystemException("Image dictionary is invalid");
            }
        }

        protected override int CodeMatchIndex(int index, JB2Dictionary ignored)
        {
            //Console.WriteLine("Jb2D cmi(i d) index: " + index);
            //Console.WriteLine("Jb2D cmi(i d) lib2shape.Count: " + lib2shape.Count);
            //Console.WriteLine("Jb2D cmi(i d) dist_match_index: " + dist_match_index.Value);

            int result = CodeNum(0, Lib2Shape.Count - 1, DistMatchIndex);

            //Console.WriteLine("Jb2D cmi(i d) result: " + result);
            return result;
        }

        protected override int CodeRecordType(int ignored)
        {
            //Console.WriteLine("Jb2D crt(int) dist_record_type: " + dist_record_type.Value);
            int result = CodeNum(StartOfData, EndOfData, DistRecordType);

            //Console.WriteLine("Jb2D crt(int) result: " + result);
            return result;
        }

        protected override void CodeRelativeMarkSize(Bitmap bm, int cw, int ch, int border)
        {
            //Console.WriteLine("Jb2D crms(b i i i) cw: " + cw);
            //Console.WriteLine("Jb2D crms(b i i i) ch: " + ch);
            //Console.WriteLine("Jb2D crms(b i i i) border: " + border);

            //Console.WriteLine("Jb2D crms(b i i i) rel_size_x: " + rel_size_x.Value);
            //Console.WriteLine("Jb2D crms(b i i i) rel_size_y: " + rel_size_y.Value);

            int xdiff = CodeNum(Bignegative, Bigpositive, RelSizeX);
            int ydiff = CodeNum(Bignegative, Bigpositive, RelSizeY);

            //Console.WriteLine("Jb2D crms(b i i i) xdiff: " + xdiff);
            //Console.WriteLine("Jb2D crms(b i i i) ydiff: " + ydiff);

            int xsize = cw + xdiff;
            int ysize = ch + ydiff;

            //Console.WriteLine("Jb2D crms(b i i i) xsize: " + xsize);
            //Console.WriteLine("Jb2D crms(b i i i) ysize: " + ysize);

            if ((xsize != (0xffff & xsize)) || (ysize != (0xffff & ysize)))
            {
                throw new SystemException("Image image decoder value");
            }

            bm.Init(ysize, xsize, border);
        }

        protected override int GetDiff(int ignored, MutableValue<int> rel_loc)
        {
            //Console.WriteLine("Jb2D gd(i mi) rel_loc: " + rel_loc);
            int result = CodeNum(Bignegative, Bigpositive, rel_loc);
            //Console.WriteLine("Jb2D gd(i mi) result: " + result);

            return result;
        }

        #endregion Protected Methods
    }
}