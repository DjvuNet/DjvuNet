using System;
using System.Collections;
using System.Collections.Generic;
using DjvuNet.Compression;
using DjvuNet.Graphics;

namespace DjvuNet.JB2
{
    public abstract class JB2Codec
    {
        #region Private Variables

        private readonly bool _encoding;

        private const sbyte NewMark = 1;
        private const sbyte NewMarkLibraryOnly = 2;
        private const sbyte NewMarkImageOnly = 3;
        private const sbyte MatchedRefine = 4;
        private const sbyte MatchedRefineLibraryOnly = 5;
        private const sbyte MatchedRefineImageOnly = 6;
        private const sbyte MatchedCopy = 7;
        private const sbyte NonMarkData = 8;
        private const sbyte RequiredDictOrReset = 9;
        private const sbyte PreservedComment = 10;

        private const Int32 MinusOneObject = -1;

        private readonly List<MutableValue<sbyte>> _bitcells = new List<MutableValue<sbyte>>();
        private readonly MutableValue<sbyte> _distRefinementFlag = new MutableValue<sbyte>();
        private int _lastBottom;
        private int _lastLeft;
        private int _lastRight;
        private int _lastRowBottom;
        private int _lastRowLeft;
        private readonly List<MutableValue<int>> _leftcell = new List<MutableValue<int>>();

        private readonly List<Rectangle> _libinfo = new List<Rectangle>();
        private readonly MutableValue<sbyte> _offsetTypeDist = new MutableValue<sbyte>();
        private bool _refinementp;
        private readonly MutableValue<int> rel_loc_x_current = new MutableValue<int>();
        private readonly MutableValue<int> rel_loc_x_last = new MutableValue<int>();
        private readonly MutableValue<int> rel_loc_y_current = new MutableValue<int>();
        private readonly MutableValue<int> rel_loc_y_last = new MutableValue<int>();

        private readonly List<MutableValue<int>> rightcell = new List<MutableValue<int>>();
        private readonly List<int> shape2lib_Renamed_Field = new List<int>();

        private readonly int[] short_list = new int[3];
        private int _shortListPos;

        #endregion Private Variables

        #region Protected Variables

        protected const int Bigpositive = 262142;
        protected const int Bignegative = -262143;
        protected const sbyte StartOfData = 0;
        protected const sbyte EndOfData = 11;

        protected MutableValue<int> AbsLocX = new MutableValue<int>();
        protected MutableValue<int> AbsLocY = new MutableValue<int>();
        protected MutableValue<int> AbsSizeX = new MutableValue<int>();
        protected MutableValue<int> AbsSizeY = new MutableValue<int>();
        protected sbyte[] Bitdist = new sbyte[1024];
        protected sbyte[] Cbitdist = new sbyte[2048];
        protected MutableValue<int> DistCommentByte = new MutableValue<int>();
        protected MutableValue<int> DistCommentLength = new MutableValue<int>();
        protected MutableValue<int> DistMatchIndex = new MutableValue<int>();

        protected MutableValue<int> DistRecordType = new MutableValue<int>();
        protected bool GotStartRecordP;
        protected int ImageColumns;
        protected int ImageRows;
        protected MutableValue<int> ImageSizeDist = new MutableValue<int>();
        protected MutableValue<int> InheritedShapeCountDist = new MutableValue<int>();
        protected List<int> Lib2Shape = new List<int>();
        protected MutableValue<int> RelSizeX = new MutableValue<int>();
        protected MutableValue<int> RelSizeY = new MutableValue<int>();

        #endregion Protected Variables

        #region Constructors

        protected JB2Codec(bool encoding)
        {
            _encoding = encoding;

            for (int i = 0; i < Bitdist.Length; )
            {
                Bitdist[i++] = 0;
            }

            for (int i = 0; i < Cbitdist.Length; )
            {
                Cbitdist[i++] = 0;
            }

            _bitcells.Add(new MutableValue<sbyte>());
            _leftcell.Add(new MutableValue<int>());
            rightcell.Add(new MutableValue<int>());
        }

        #endregion Constructors

        #region Protected Methods

        protected virtual int CodeNum(int low, int high, MutableValue<int> ctx, int v)
        {
            bool negative = false;
            int cutoff = 0;

            int ictx = ctx.Value;

            if (ictx >= _bitcells.Count)
            {
                throw new IndexOutOfRangeException("Image bad MutableValue<int>");
            }

            for (int phase = 1, range = -1; range != 1; ictx = ctx.Value)
            {
                bool decision;

                if (ictx == 0)
                {
                    ictx = _bitcells.Count;
                    ctx.Value = ictx;

                    MutableValue<sbyte> pbitcells = new MutableValue<sbyte>();
                    MutableValue<int> pleftcell = new MutableValue<int>();
                    MutableValue<int> prightcell = new MutableValue<int>();

                    _bitcells.Add(pbitcells);
                    _leftcell.Add(pleftcell);
                    rightcell.Add(prightcell);
                    decision = _encoding
                                   ? (((low < cutoff) && (high >= cutoff))
                                          ? CodeBit((v >= cutoff), pbitcells)
                                          : (v >= cutoff))
                                   : ((low >= cutoff) || ((high >= cutoff) && CodeBit(false, pbitcells)));

                    ctx = (decision ? prightcell : pleftcell);
                }
                else
                {
                    decision = _encoding
                                   ? (((low < cutoff) && (high >= cutoff))
                                          ? CodeBit((v >= cutoff), (MutableValue<sbyte>)_bitcells[ictx])
                                          : (v >= cutoff))
                                   : ((low >= cutoff) ||
                                      ((high >= cutoff) && CodeBit(false, (MutableValue<sbyte>)_bitcells[ictx])));

                    ctx = (MutableValue<int>)(decision ? rightcell[ictx] : _leftcell[ictx]);
                }

                switch (phase)
                {
                    case 1:
                        {
                            negative = !decision;

                            if (negative)
                            {
                                if (_encoding)
                                {
                                    v = -v - 1;
                                }

                                int temp = -low - 1;
                                low = -high - 1;
                                high = temp;
                            }

                            phase = 2;
                            cutoff = 1;

                            break;
                        }

                    case 2:
                        {
                            if (!decision)
                            {
                                phase = 3;
                                range = (cutoff + 1) >> 1;

                                if (range == 1)
                                {
                                    cutoff = 0;
                                }
                                else
                                {
                                    cutoff -= (range >> 1);
                                }
                            }
                            else
                            {
                                cutoff = (cutoff << 1) + 1;
                            }

                            break;
                        }

                    case 3:
                        {
                            range /= 2;

                            if (range != 1)
                            {
                                if (!decision)
                                {
                                    cutoff -= (range >> 1);
                                }
                                else
                                {
                                    cutoff += (range >> 1);
                                }
                            }
                            else if (!decision)
                            {
                                cutoff--;
                            }

                            break;
                        }
                }
            }

            int result = negative ? (-cutoff - 1) : cutoff;

            return result;
        }

        protected abstract void CodeAbsoluteLocation(JB2Blit jblt, int rows, int columns);

        protected abstract void CodeBitmapByCrossCoding(Bitmap bm, Bitmap cbm, int xd2c, int dw, int dy,
                                                                     int cy, int up1, int up0, int xup1, int xup0,
                                                                     int xdn1);

        protected abstract void CodeBitmapDirectly(Bitmap bm, int dw, int dy, int up2, int up1, int up0);

        protected virtual void CodeEventualLosslessRefinement()
        {
            _refinementp = CodeBit(_refinementp, _distRefinementFlag);
        }

        protected abstract void CodeInheritedShapeCount(JB2Dictionary jim);

        protected void CodeAbsoluteMarkSize(Bitmap bm)
        {
            CodeAbsoluteMarkSize(bm, 0);
        }

        protected abstract void CodeAbsoluteMarkSize(Bitmap bm, int border);

        protected virtual void CodeImageSize(JB2Dictionary ignored)
        {
            _lastLeft = 1;
            _lastRowBottom = 0;
            _lastRowLeft = _lastRight = 0;
            FillShortList(_lastRowBottom);
            GotStartRecordP = true;
        }

        protected virtual void CodeImageSize(JB2Image ignored)
        {
            _lastLeft = 1 + ImageColumns;
            _lastRowBottom = ImageRows;
            _lastRowLeft = _lastRight = 0;
            FillShortList(_lastRowBottom);
            GotStartRecordP = true;
        }

        protected virtual int CodeRecordA(int rectype, JB2Dictionary jim, JB2Shape xjshp)
        {
            Bitmap bm = null;
            int shapeno = -1;
            rectype = CodeRecordType(rectype);

            JB2Shape jshp = xjshp;

            switch (rectype)
            {
                case NewMarkLibraryOnly:
                case MatchedRefineLibraryOnly:
                    {
                        if (!_encoding)
                        {
                            jshp = new JB2Shape().Init(-1);
                        }
                        else if (jshp == null)
                        {
                            jshp = new JB2Shape();
                        }

                        bm = jshp.Bitmap;

                        break;
                    }
            }

            switch (rectype)
            {
                case StartOfData:
                    {
                        CodeImageSize(jim);
                        CodeEventualLosslessRefinement();

                        if (!_encoding)
                        {
                            InitLibrary(jim);
                        }

                        break;
                    }

                case NewMarkLibraryOnly:
                    {
                        CodeAbsoluteMarkSize(bm, 4);
                        CodeBitmapDirectly(bm);

                        break;
                    }

                case MatchedRefineLibraryOnly:
                    {
                        int match = CodeMatchIndex(jshp.Parent, jim);

                        if (!_encoding)
                        {
                            jshp.Parent = Convert.ToInt32((Lib2Shape[match]));
                        }

                        Bitmap cbm = jim.GetShape(jshp.Parent).Bitmap;
                        Rectangle lmatch = (Rectangle)_libinfo[match];
                        CodeRelativeMarkSize(bm, (1 + lmatch.Left) - lmatch.Right, (1 + lmatch.Top) - lmatch.Bottom, 4);
                        CodeBitmapByCrossCoding(bm, cbm, jshp.Parent);

                        break;
                    }

                case PreservedComment:
                    {
                        jim.Comment = CodeComment(jim.Comment);

                        break;
                    }

                case RequiredDictOrReset:
                    {
                        if (!GotStartRecordP)
                        {
                            CodeInheritedShapeCount(jim);
                        }
                        else
                        {
                            ResetNumcoder();
                        }

                        break;
                    }

                case EndOfData:
                    break;

                default:
                    throw new ArgumentException("Image bad type");
            }

            if (!_encoding)
            {
                switch (rectype)
                {
                    case NewMarkLibraryOnly:
                    case MatchedRefineLibraryOnly:
                        {
                            if (xjshp != null)
                            {
                                jshp = jshp.Duplicate();
                            }

                            shapeno = jim.AddShape(jshp);
                            AddLibrary(shapeno, jshp);

                            break;
                        }
                }
            }

            return rectype;
        }

        protected virtual int CodeRecordB(int rectype, JB2Image jim, JB2Shape xjshp, JB2Blit xjblt)
        {
            Bitmap bm = null;
            int shapeno = -1;
            JB2Shape jshp = xjshp;
            JB2Blit jblt = xjblt;
            rectype = CodeRecordType(rectype);

            switch (rectype)
            {
                case NewMark:
                case NewMarkImageOnly:
                case MatchedRefine:
                case MatchedRefineImageOnly:
                case NonMarkData:
                    {
                        if (jblt == null)
                        {
                            jblt = new JB2Blit();
                        }

                        // fall through
                    }
                    goto case NewMarkLibraryOnly;

                case NewMarkLibraryOnly:
                case MatchedRefineLibraryOnly:
                    {
                        if (!_encoding)
                        {
                            jshp = new JB2Shape().Init((rectype == NonMarkData) ? (-2) : (-1));
                        }
                        else if (jshp == null)
                        {
                            jshp = new JB2Shape();
                        }

                        bm = jshp.Bitmap;

                        break;
                    }

                case MatchedCopy:
                    {
                        if (jblt == null)
                        {
                            jblt = new JB2Blit();
                        }

                        break;
                    }
            }

            bool needAddLibrary = false;
            bool needAddBlit = false;

            switch (rectype)
            {
                case StartOfData:
                    {
                        CodeImageSize(jim);
                        CodeEventualLosslessRefinement();

                        if (!_encoding)
                        {
                            InitLibrary(jim);
                        }

                        break;
                    }

                case NewMark:
                    {
                        needAddBlit = needAddLibrary = true;
                        CodeAbsoluteMarkSize(bm, 4);
                        CodeBitmapDirectly(bm);
                        CodeRelativeLocation(jblt, bm.ImageHeight, bm.ImageWidth);

                        break;
                    }

                case NewMarkLibraryOnly:
                    {
                        needAddLibrary = true;
                        CodeAbsoluteMarkSize(bm, 4);
                        CodeBitmapDirectly(bm);

                        break;
                    }

                case NewMarkImageOnly:
                    {
                        needAddBlit = true;
                        CodeAbsoluteMarkSize(bm, 3);
                        CodeBitmapDirectly(bm);
                        CodeRelativeLocation(jblt, bm.ImageHeight, bm.ImageWidth);

                        break;
                    }

                case MatchedRefine:
                    {
                        needAddBlit = true;
                        needAddLibrary = true;

                        int match = CodeMatchIndex(jshp.Parent, jim);

                        if (!_encoding)
                        {
                            jshp.Parent = Convert.ToInt32((Lib2Shape[match]));
                        }

                        Bitmap cbm = jim.GetShape(jshp.Parent).Bitmap;
                        Rectangle lmatch = (Rectangle)_libinfo[match];
                        CodeRelativeMarkSize(bm, (1 + lmatch.Left) - lmatch.Right, (1 + lmatch.Top) - lmatch.Bottom, 4);

                        //          verbose("2.d time="+System.currentTimeMillis()+",rectype="+rectype);
                        CodeBitmapByCrossCoding(bm, cbm, match);

                        //          verbose("2.e time="+System.currentTimeMillis()+",rectype="+rectype);
                        CodeRelativeLocation(jblt, bm.ImageHeight, bm.ImageWidth);

                        break;
                    }

                case MatchedRefineLibraryOnly:
                    {
                        needAddLibrary = true;

                        int match = CodeMatchIndex(jshp.Parent, jim);

                        if (!_encoding)
                        {
                            jshp.Parent = Convert.ToInt32((Lib2Shape[match]));
                        }

                        Bitmap cbm = jim.GetShape(jshp.Parent).Bitmap;
                        Rectangle lmatch = (Rectangle)_libinfo[match];
                        CodeRelativeMarkSize(bm, (1 + lmatch.Left) - lmatch.Right, (1 + lmatch.Top) - lmatch.Bottom, 4);

                        break;
                    }

                case MatchedRefineImageOnly:
                    {
                        needAddBlit = true;

                        int match = CodeMatchIndex(jshp.Parent, jim);

                        if (!_encoding)
                        {
                            jshp.Parent = Convert.ToInt32((Lib2Shape[match]));
                        }

                        Bitmap cbm = jim.GetShape(jshp.Parent).Bitmap;
                        Rectangle lmatch = (Rectangle)_libinfo[match];
                        CodeRelativeMarkSize(bm, (1 + lmatch.Left) - lmatch.Right, (1 + lmatch.Top) - lmatch.Bottom, 4);
                        CodeBitmapByCrossCoding(bm, cbm, match);
                        CodeRelativeLocation(jblt, bm.ImageHeight, bm.ImageWidth);

                        break;
                    }

                case MatchedCopy:
                    {
                        int match = CodeMatchIndex(jblt.ShapeNumber, jim);

                        if (!_encoding)
                        {
                            jblt.ShapeNumber = Convert.ToInt32((Lib2Shape[match]));
                        }

                        bm = jim.GetShape(jblt.ShapeNumber).Bitmap;

                        Rectangle lmatch = (Rectangle)_libinfo[match];
                        jblt.Left += lmatch.Right;
                        jblt.Bottom += lmatch.Bottom;

                        CodeRelativeLocation(jblt, (1 + lmatch.Top) - lmatch.Bottom,
                                               (1 + lmatch.Left) - lmatch.Right);

                        jblt.Left -= lmatch.Right;
                        jblt.Bottom -= lmatch.Bottom;

                        break;
                    }

                case NonMarkData:
                    {
                        needAddBlit = true;
                        CodeAbsoluteMarkSize(bm, 3);
                        CodeBitmapDirectly(bm);
                        CodeAbsoluteLocation(jblt, bm.ImageHeight, bm.ImageWidth);

                        break;
                    }

                case PreservedComment:
                    {
                        jim.Comment = CodeComment(jim.Comment);

                        break;
                    }

                case RequiredDictOrReset:
                    {
                        if (!GotStartRecordP)
                        {
                            CodeInheritedShapeCount(jim);
                        }
                        else
                        {
                            ResetNumcoder();
                        }

                        break;
                    }

                case EndOfData:
                    break;

                default:
                    throw new ArgumentException("Image unknown type");
            }

            if (!_encoding)
            {
                switch (rectype)
                {
                    case NewMark:
                    case NewMarkLibraryOnly:
                    case MatchedRefine:
                    case MatchedRefineLibraryOnly:
                    case NewMarkImageOnly:
                    case MatchedRefineImageOnly:
                    case NonMarkData:
                        {
                            if (xjshp != null)
                            {
                                jshp = jshp.Duplicate();
                            }

                            shapeno = jim.AddShape(jshp);
                            Shape2Lib(shapeno, MinusOneObject);

                            if (needAddLibrary)
                            {
                                AddLibrary(shapeno, jshp);
                            }

                            if (needAddBlit)
                            {
                                jblt.ShapeNumber = shapeno;

                                if (xjblt != null)
                                {
                                    jblt = (JB2Blit)xjblt.Duplicate();
                                }

                                jim.AddBlit(jblt);
                            }

                            break;
                        }

                    case MatchedCopy:
                        {
                            if (xjblt != null)
                            {
                                jblt = (JB2Blit)xjblt.Duplicate();
                            }

                            jim.AddBlit(jblt);

                            break;
                        }
                }
            }

            return rectype;
        }

        protected void CodeRelativeMarkSize(Bitmap bm, int cw, int ch)
        {
            CodeRelativeMarkSize(bm, cw, ch, 0);
        }

        protected void FillShortList(int v)
        {
            short_list[0] = short_list[1] = short_list[2] = v;
            _shortListPos = 0;
        }

        protected int GetCrossContext(Bitmap bm, Bitmap cbm, int up1, int up0, int xup1, int xup0, int xdn1,
                                                 int column)
        {
            return ((bm.GetByteAt((up1 + column) - 1) << 10) | (bm.GetByteAt(up1 + column) << 9) |
                    (bm.GetByteAt(up1 + column + 1) << 8) | (bm.GetByteAt((up0 + column) - 1) << 7) |
                    (cbm.GetByteAt(xup1 + column) << 6) | (cbm.GetByteAt((xup0 + column) - 1) << 5) |
                    (cbm.GetByteAt(xup0 + column) << 4) | (cbm.GetByteAt(xup0 + column + 1) << 3) |
                    (cbm.GetByteAt((xdn1 + column) - 1) << 2) | (cbm.GetByteAt(xdn1 + column) << 1) |
                    (cbm.GetByteAt(xdn1 + column + 1)));
        }

        protected int GetDirectContext(Bitmap bm, int up2, int up1, int up0, int column)
        {
            return ((bm.GetByteAt((up2 + column) - 1) << 9) | (bm.GetByteAt(up2 + column) << 8) |
                    (bm.GetByteAt(up2 + column + 1) << 7) | (bm.GetByteAt((up1 + column) - 2) << 6) |
                    (bm.GetByteAt((up1 + column) - 1) << 5) | (bm.GetByteAt(up1 + column) << 4) |
                    (bm.GetByteAt(up1 + column + 1) << 3) | (bm.GetByteAt(up1 + column + 2) << 2) |
                    (bm.GetByteAt((up0 + column) - 2) << 1) | (bm.GetByteAt((up0 + column) - 1)));
        }

        protected abstract void CodeRelativeMarkSize(Bitmap bm, int cw, int ch, int border);

        protected abstract int GetDiff(int ignored, MutableValue<int> rel_loc);

        protected virtual int AddLibrary(int shapeno, JB2Shape jshp)
        {
            int libno = Lib2Shape.Count;
            Lib2Shape.Add(shapeno);
            Shape2Lib(shapeno, libno);

            //final Rectangle r = new Rectangle();
            //libinfo.addElement(r);
            //jshp.getGBitmap().compute_bounding_box(r);
            _libinfo.Add(jshp.Bitmap.ComputeBoundingBox());

            return libno;
        }

        protected virtual void ResetNumcoder()
        {
            DistCommentByte.Value = 0;
            DistCommentLength.Value = 0;
            DistRecordType.Value = 0;
            DistMatchIndex.Value = 0;
            AbsLocX.Value = 0;
            AbsLocY.Value = 0;
            AbsSizeX.Value = 0;
            AbsSizeY.Value = 0;
            ImageSizeDist.Value = 0;
            InheritedShapeCountDist.Value = 0;
            rel_loc_x_current.Value = 0;
            rel_loc_x_last.Value = 0;
            rel_loc_y_current.Value = 0;
            rel_loc_y_last.Value = 0;
            RelSizeX.Value = 0;
            RelSizeY.Value = 0;
            _bitcells.Clear();
            _leftcell.Clear();
            rightcell.Clear();
            _bitcells.Add(new MutableValue<sbyte>());
            _leftcell.Add(new MutableValue<int>());
            rightcell.Add(new MutableValue<int>());

            GC.Collect();
        }

        protected void Shape2Lib(int shapeno, int libno)
        {
            int size = shape2lib_Renamed_Field.Count;

            if (size <= shapeno)
            {
                while (size++ < shapeno)
                {
                    shape2lib_Renamed_Field.Add(MinusOneObject);
                }

                shape2lib_Renamed_Field.Add(libno);
            }
            else
            {
                shape2lib_Renamed_Field[shapeno] = libno;
            }
        }

        protected int ShiftCrossContext(Bitmap bm, Bitmap cbm, int context, int n, int up1, int up0,
                                                   int xup1, int xup0, int xdn1, int column)
        {
            return (((context << 1) & 0x636) | (bm.GetByteAt(up1 + column + 1) << 8) |
                    (cbm.GetByteAt(xup1 + column) << 6) | (cbm.GetByteAt(xup0 + column + 1) << 3) |
                    (cbm.GetByteAt(xdn1 + column + 1)) | (n << 7));
        }

        protected int ShiftDirectContext(Bitmap bm, int context, int next, int up2, int up1, int up0,
                                                    int column)
        {
            return (((context << 1) & 0x37a) | (bm.GetByteAt(up1 + column + 2) << 2) |
                    (bm.GetByteAt(up2 + column + 1) << 7) | next);
        }

        protected abstract bool CodeBit(bool bit, MutableValue<sbyte> ctx);

        protected abstract int CodeBit(bool bit, sbyte[] array, int offset);

        protected abstract String CodeComment(String comment);

        protected abstract int CodeMatchIndex(int index, JB2Dictionary jim);

        protected abstract int CodeRecordType(int rectype);

        protected virtual void CodeBitmapByCrossCoding(Bitmap bm, Bitmap cbm, int libno)
        {
            // Bitmap cbm=new Bitmap();
            // synchronized(xcbm)
            // {
            //   cbm.init(xcbm);
            // }
            lock (bm)
            {
                int cw = cbm.ImageWidth;
                int dw = bm.ImageWidth;
                int dh = bm.ImageHeight;
                Rectangle lmatch = (Rectangle)_libinfo[libno];
                //int xd2c = ((1 + (dw / 2)) - dw) - ((((1 + lmatch.Left) - lmatch.Right) / 2) - lmatch.Left);
                //int yd2c = ((1 + (dh / 2)) - dh) - ((((1 + lmatch.Top) - lmatch.Bottom) / 2) - lmatch.Top);

                int xd2c = ((1 + (dw >> 1)) - dw) - ((((1 + lmatch.Left) - lmatch.Right) >> 1) - lmatch.Left);
                int yd2c = ((1 + (dh >> 1)) - dh) - ((((1 + lmatch.Top) - lmatch.Bottom) >> 1) - lmatch.Top);

                bm.MinimumBorder = 2;
                cbm.MinimumBorder = 2 - xd2c;
                cbm.MinimumBorder = (2 + dw + xd2c) - cw;

                int dy = dh - 1;
                int cy = dy + yd2c;
                CodeBitmapByCrossCoding(bm, cbm, xd2c, dw, dy, cy, bm.RowOffset(dy + 1), bm.RowOffset(dy),
                                            cbm.RowOffset(cy + 1) + xd2c, cbm.RowOffset(cy) + xd2c,
                                            cbm.RowOffset(cy - 1) + xd2c);
            }
        }

        protected virtual void CodeBitmapDirectly(Bitmap bm)
        {
            lock (bm)
            {
                bm.MinimumBorder = 3;

                int dy = bm.ImageHeight - 1;
                CodeBitmapDirectly(bm, bm.ImageWidth, dy, bm.RowOffset(dy + 2), bm.RowOffset(dy + 1), bm.RowOffset(dy));
            }
        }

        protected virtual void CodeRelativeLocation(JB2Blit jblt, int rows, int columns)
        {
            if (!GotStartRecordP)
            {
                throw new SystemException("Image no start");
            }

            int bottom = 0;
            int left = 0;
            int top = 0;
            int right = 0;

            if (_encoding)
            {
                left = jblt.Left + 1;
                bottom = jblt.Bottom + 1;
                right = (left + columns) - 1;
                top = (bottom + rows) - 1;
            }

            bool new_row = CodeBit((left < _lastLeft), _offsetTypeDist);

            if (new_row)
            {
                int x_diff = GetDiff(left - _lastRowLeft, rel_loc_x_last);
                int y_diff = GetDiff(top - _lastRowBottom, rel_loc_y_last);

                if (!_encoding)
                {
                    left = _lastRowLeft + x_diff;
                    top = _lastRowBottom + y_diff;
                    right = (left + columns) - 1;
                    bottom = (top - rows) + 1;
                }

                _lastLeft = _lastRowLeft = left;
                _lastRight = right;
                _lastBottom = _lastRowBottom = bottom;
                FillShortList(bottom);
            }
            else
            {
                int x_diff = GetDiff(left - _lastRight, rel_loc_x_current);
                int y_diff = GetDiff(bottom - _lastBottom, rel_loc_y_current);

                if (!_encoding)
                {
                    left = _lastRight + x_diff;
                    bottom = _lastBottom + y_diff;
                    right = (left + columns) - 1;
                    top = (bottom + rows) - 1;
                }

                _lastLeft = left;
                _lastRight = right;
                _lastBottom = UpdateShortList(bottom);
            }

            if (!_encoding)
            {
                jblt.Bottom = bottom - 1;
                jblt.Left = left - 1;
            }
        }

        protected virtual void InitLibrary(JB2Dictionary jim)
        {
            int nshape = jim.InheritedShapes;
            shape2lib_Renamed_Field.Clear();
            Lib2Shape.Clear();
            _libinfo.Clear();

            for (int i = 0; i < nshape; i++)
            {
                int x = i;
                shape2lib_Renamed_Field.Add(x);
                Lib2Shape.Add(x);

                JB2Shape jshp = jim.GetShape(i);
                //final Rectangle  r = new Rectangle();
                //libinfo.addElement(r);
                //jshp.getGBitmap().compute_bounding_box(r);
                _libinfo.Add(jshp.Bitmap.ComputeBoundingBox());
            }
        }

        protected virtual int UpdateShortList(int v)
        {
            if (++_shortListPos == 3)
            {
                _shortListPos = 0;
            }

            short_list[_shortListPos] = v;

            return (short_list[0] >= short_list[1])
                       ? ((short_list[0] > short_list[2])
                              ? ((short_list[1] >= short_list[2]) ? short_list[1] : short_list[2])
                              : short_list[0])
                       : ((short_list[0] < short_list[2])
                              ? ((short_list[1] >= short_list[2]) ? short_list[2] : short_list[1])
                              : short_list[0]);
        }

        #endregion Protected Methods
    }
}