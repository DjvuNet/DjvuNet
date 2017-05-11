using System;
using System.Collections;
using System.Collections.Generic;
using DjvuNet.Compression;
using DjvuNet.Graphics;

namespace DjvuNet.JB2
{
    public abstract class JB2Codec
    {
        #region Constans

        internal const sbyte NewMark = 1;
        internal const sbyte NewMarkLibraryOnly = 2;
        internal const sbyte NewMarkImageOnly = 3;
        internal const sbyte MatchedRefine = 4;
        internal const sbyte MatchedRefineLibraryOnly = 5;
        internal const sbyte MatchedRefineImageOnly = 6;
        internal const sbyte MatchedCopy = 7;
        internal const sbyte NonMarkData = 8;
        internal const sbyte RequiredDictOrReset = 9;
        internal const sbyte PreservedComment = 10;

        internal const Int32 MinusOneObject = -1;

        protected const int Bigpositive = 262142;
        protected const int Bignegative = -262143;
        protected const sbyte StartOfData = 0;
        protected const sbyte EndOfData = 11;

        #endregion Constans

        #region Fields

        internal bool _Encoding;

        internal readonly List<MutableValue<sbyte>> _BitCells;
        internal readonly MutableValue<sbyte> _DistRefinementFlag;
        internal int _LastBottom;
        internal int _LastLeft;
        internal int _LastRight;
        internal int _LastRowBottom;
        internal int _LastRowLeft;
        internal readonly List<MutableValue<int>> _LeftCell;

        internal readonly List<Rectangle> _LibInfo;
        internal readonly MutableValue<sbyte> _OffsetTypeDist;
        internal bool _Refinementp;
        internal readonly MutableValue<int> _RelLocXCurrent;
        internal readonly MutableValue<int> _RelLocXLast;
        internal readonly MutableValue<int> _RelLocYCurrent;
        internal readonly MutableValue<int> _RelLocYLast;

        internal readonly List<MutableValue<int>> _RightCell;
        internal readonly List<int> _Shape2Lib;

        internal readonly int[] _ShortList;
        internal int _ShortListPos;

        protected MutableValue<int> _AbsLocX;
        protected MutableValue<int> _AbsLocY;
        protected MutableValue<int> _AbsSizeX;
        protected MutableValue<int> _AbsSizeY;
        protected sbyte[] _BitDist;
        protected sbyte[] _CBitDist;
        protected MutableValue<int> _DistCommentByte;
        protected MutableValue<int> _DistCommentLength;
        protected MutableValue<int> _DistMatchIndex;

        protected MutableValue<int> _DistRecordType;
        protected bool _GotStartRecordP;
        protected int _ImageColumns;
        protected int _ImageRows;
        protected MutableValue<int> _ImageSizeDist;
        protected MutableValue<int> _InheritedShapeCountDist;
        protected List<int> _Lib2Shape;
        protected MutableValue<int> _RelSizeX;
        protected MutableValue<int> _RelSizeY;

        #endregion Fields

        #region Constructors

        protected JB2Codec(bool encoding)
        {
            _BitCells = new List<MutableValue<sbyte>>();
            _DistRefinementFlag = new MutableValue<sbyte>();
            _LeftCell = new List<MutableValue<int>>();
            _LibInfo = new List<Rectangle>();
            _OffsetTypeDist = new MutableValue<sbyte>();
            _RelLocXCurrent = new MutableValue<int>();
            _RelLocXLast = new MutableValue<int>();
            _RelLocYCurrent = new MutableValue<int>();
            _RelLocYLast = new MutableValue<int>();
            _RightCell = new List<MutableValue<int>>();
            _Shape2Lib = new List<int>();
            _ShortList = new int[3];

            _AbsLocX = new MutableValue<int>();
            _AbsLocY = new MutableValue<int>();
            _AbsSizeX = new MutableValue<int>();
            _AbsSizeY = new MutableValue<int>();
            _BitDist = new sbyte[1024];
            _CBitDist = new sbyte[2048];
            _DistCommentByte = new MutableValue<int>();
            _DistCommentLength = new MutableValue<int>();
            _DistMatchIndex = new MutableValue<int>();

            _DistRecordType = new MutableValue<int>();
            _ImageSizeDist = new MutableValue<int>();
            _InheritedShapeCountDist = new MutableValue<int>();
            _Lib2Shape = new List<int>();
            _RelSizeX = new MutableValue<int>();
            _RelSizeY = new MutableValue<int>();

            _Encoding = encoding;

            _BitCells.Add(new MutableValue<sbyte>());
            _LeftCell.Add(new MutableValue<int>());
            _RightCell.Add(new MutableValue<int>());
        }

        #endregion Constructors

        #region Protected Methods

        protected virtual int CodeNum(int low, int high, MutableValue<int> ctx, int v)
        {
            bool negative = false;
            int cutoff = 0;

            int ictx = ctx.Value;

            if (ictx >= _BitCells.Count)
                throw new IndexOutOfRangeException("Image bad MutableValue<int>");

            for (int phase = 1, range = -1; range != 1; ictx = ctx.Value)
            {
                bool decision;

                if (ictx == 0)
                {
                    ictx = _BitCells.Count;
                    ctx.Value = ictx;

                    MutableValue<sbyte> pbitcells = new MutableValue<sbyte>();
                    MutableValue<int> pleftcell = new MutableValue<int>();
                    MutableValue<int> prightcell = new MutableValue<int>();

                    _BitCells.Add(pbitcells);
                    _LeftCell.Add(pleftcell);
                    _RightCell.Add(prightcell);
                    decision = _Encoding
                                   ? (((low < cutoff) && (high >= cutoff))
                                          ? CodeBit((v >= cutoff), pbitcells)
                                          : (v >= cutoff))
                                   : ((low >= cutoff) || ((high >= cutoff) && CodeBit(false, pbitcells)));

                    ctx = (decision ? prightcell : pleftcell);
                }
                else
                {
                    decision = _Encoding
                                   ? (((low < cutoff) && (high >= cutoff))
                                          ? CodeBit((v >= cutoff), (MutableValue<sbyte>)_BitCells[ictx])
                                          : (v >= cutoff))
                                   : ((low >= cutoff) ||
                                      ((high >= cutoff) && CodeBit(false, (MutableValue<sbyte>)_BitCells[ictx])));

                    ctx = (MutableValue<int>)(decision ? _RightCell[ictx] : _LeftCell[ictx]);
                }

                switch (phase)
                {
                    case 1:
                        {
                            negative = !decision;

                            if (negative)
                            {
                                if (_Encoding)
                                    v = -v - 1;

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
                                    cutoff = 0;
                                else
                                    cutoff -= (range >> 1);
                            }
                            else
                                cutoff = (cutoff << 1) + 1;

                            break;
                        }

                    case 3:
                        {
                            range /= 2;

                            if (range != 1)
                            {
                                if (!decision)
                                    cutoff -= (range >> 1);
                                else
                                    cutoff += (range >> 1);
                            }
                            else if (!decision)
                                cutoff--;

                            break;
                        }
                }
            }

            int result = negative ? (-cutoff - 1) : cutoff;

            return result;
        }

        protected abstract void CodeAbsoluteLocation(JB2Blit jblt, int rows, int columns);

        protected abstract void CodeBitmapByCrossCoding(IBitmap bm, IBitmap cbm, int xd2c, int dw, int dy,
                                                                     int cy, int up1, int up0, int xup1, int xup0,
                                                                     int xdn1);

        protected abstract void CodeBitmapDirectly(IBitmap bm, int dw, int dy, int up2, int up1, int up0);

        protected virtual void CodeEventualLosslessRefinement()
        {
            _Refinementp = CodeBit(_Refinementp, _DistRefinementFlag);
        }

        protected abstract void CodeInheritedShapeCount(JB2Dictionary jim);

        protected void CodeAbsoluteMarkSize(IBitmap bm)
        {
            CodeAbsoluteMarkSize(bm, 0);
        }

        protected abstract void CodeAbsoluteMarkSize(IBitmap bm, int border);

        protected virtual void CodeImageSize(JB2Dictionary ignored)
        {
            _LastLeft = 1;
            _LastRowBottom = 0;
            _LastRowLeft = _LastRight = 0;
            FillShortList(_LastRowBottom);
            _GotStartRecordP = true;
        }

        protected virtual void CodeImageSize(JB2Image ignored)
        {
            _LastLeft = 1 + _ImageColumns;
            _LastRowBottom = _ImageRows;
            _LastRowLeft = _LastRight = 0;
            FillShortList(_LastRowBottom);
            _GotStartRecordP = true;
        }

        protected virtual int CodeRecordA(int rectype, JB2Dictionary jim, JB2Shape xjshp)
        {
            IBitmap bm = null;
            int shapeno = -1;
            rectype = CodeRecordType(rectype);

            JB2Shape jshp = xjshp;

            switch (rectype)
            {
                case NewMarkLibraryOnly:
                case MatchedRefineLibraryOnly:
                    {
                        if (!_Encoding)
                            jshp = new JB2Shape().Init(-1);
                        else if (jshp == null)
                            jshp = new JB2Shape();

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

                        if (!_Encoding)
                            InitLibrary(jim);

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

                        if (!_Encoding)
                            jshp.Parent = Convert.ToInt32((_Lib2Shape[match]));

                        var cbm = jim.GetShape(jshp.Parent).Bitmap;
                        Rectangle lmatch = (Rectangle)_LibInfo[match];
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
                        if (!_GotStartRecordP)
                            CodeInheritedShapeCount(jim);
                        else
                            ResetNumcoder();

                        break;
                    }

                case EndOfData:
                    break;

                default:
                    throw new DjvuFormatException("Image bad type");
            }

            if (!_Encoding)
            {
                switch (rectype)
                {
                    case NewMarkLibraryOnly:
                    case MatchedRefineLibraryOnly:
                        {
                            if (xjshp != null)
                                jshp = jshp.Duplicate();

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
            IBitmap bm = null;
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
                            jblt = new JB2Blit();
                        // fall through
                    }
                    goto case NewMarkLibraryOnly;

                case NewMarkLibraryOnly:
                case MatchedRefineLibraryOnly:
                    {
                        if (!_Encoding)
                            jshp = new JB2Shape().Init((rectype == NonMarkData) ? (-2) : (-1));
                        else if (jshp == null)
                            jshp = new JB2Shape();

                        bm = jshp.Bitmap;
                        break;
                    }

                case MatchedCopy:
                    {
                        if (jblt == null)
                            jblt = new JB2Blit();

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

                        if (!_Encoding)
                            InitLibrary(jim);

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

                        if (!_Encoding)
                            jshp.Parent = Convert.ToInt32((_Lib2Shape[match]));

                        var cbm = jim.GetShape(jshp.Parent).Bitmap;
                        Rectangle lmatch = (Rectangle)_LibInfo[match];
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

                        if (!_Encoding)
                            jshp.Parent = Convert.ToInt32((_Lib2Shape[match]));

                        var cbm = jim.GetShape(jshp.Parent).Bitmap;
                        Rectangle lmatch = (Rectangle)_LibInfo[match];
                        CodeRelativeMarkSize(bm, (1 + lmatch.Left) - lmatch.Right, (1 + lmatch.Top) - lmatch.Bottom, 4);

                        break;
                    }

                case MatchedRefineImageOnly:
                    {
                        needAddBlit = true;

                        int match = CodeMatchIndex(jshp.Parent, jim);

                        if (!_Encoding)
                            jshp.Parent = Convert.ToInt32((_Lib2Shape[match]));

                        var cbm = jim.GetShape(jshp.Parent).Bitmap;
                        Rectangle lmatch = (Rectangle)_LibInfo[match];
                        CodeRelativeMarkSize(bm, (1 + lmatch.Left) - lmatch.Right, (1 + lmatch.Top) - lmatch.Bottom, 4);
                        CodeBitmapByCrossCoding(bm, cbm, match);
                        CodeRelativeLocation(jblt, bm.ImageHeight, bm.ImageWidth);

                        break;
                    }

                case MatchedCopy:
                    {
                        int match = CodeMatchIndex(jblt.ShapeNumber, jim);

                        if (!_Encoding)
                            jblt.ShapeNumber = Convert.ToInt32((_Lib2Shape[match]));

                        bm = jim.GetShape(jblt.ShapeNumber).Bitmap;

                        Rectangle lmatch = (Rectangle)_LibInfo[match];
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
                        if (!_GotStartRecordP)
                            CodeInheritedShapeCount(jim);
                        else
                            ResetNumcoder();

                        break;
                    }

                case EndOfData:
                    break;

                default:
                    throw new ArgumentException("Image unknown type");
            }

            if (!_Encoding)
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
                                jshp = jshp.Duplicate();

                            shapeno = jim.AddShape(jshp);
                            Shape2Lib(shapeno, MinusOneObject);

                            if (needAddLibrary)
                                AddLibrary(shapeno, jshp);

                            if (needAddBlit)
                            {
                                jblt.ShapeNumber = shapeno;

                                if (xjblt != null)
                                    jblt = (JB2Blit)xjblt.Duplicate();

                                jim.AddBlit(jblt);
                            }

                            break;
                        }

                    case MatchedCopy:
                        {
                            if (xjblt != null)
                                jblt = (JB2Blit)xjblt.Duplicate();

                            jim.AddBlit(jblt);
                            break;
                        }
                }
            }

            return rectype;
        }

        protected void CodeRelativeMarkSize(IBitmap bm, int cw, int ch)
        {
            CodeRelativeMarkSize(bm, cw, ch, 0);
        }

        protected void FillShortList(int v)
        {
            _ShortList[0] = _ShortList[1] = _ShortList[2] = v;
            _ShortListPos = 0;
        }

        protected int GetCrossContext(IBitmap bm, IBitmap cbm, int up1, int up0, int xup1, int xup0, int xdn1,
                                                 int column)
        {
            return ((bm.GetByteAt((up1 + column) - 1) << 10) | (bm.GetByteAt(up1 + column) << 9) |
                    (bm.GetByteAt(up1 + column + 1) << 8) | (bm.GetByteAt((up0 + column) - 1) << 7) |
                    (cbm.GetByteAt(xup1 + column) << 6) | (cbm.GetByteAt((xup0 + column) - 1) << 5) |
                    (cbm.GetByteAt(xup0 + column) << 4) | (cbm.GetByteAt(xup0 + column + 1) << 3) |
                    (cbm.GetByteAt((xdn1 + column) - 1) << 2) | (cbm.GetByteAt(xdn1 + column) << 1) |
                    (cbm.GetByteAt(xdn1 + column + 1)));
        }

        protected int GetDirectContext(IBitmap bm, int up2, int up1, int up0, int column)
        {
            return ((bm.GetByteAt((up2 + column) - 1) << 9) | (bm.GetByteAt(up2 + column) << 8) |
                    (bm.GetByteAt(up2 + column + 1) << 7) | (bm.GetByteAt((up1 + column) - 2) << 6) |
                    (bm.GetByteAt((up1 + column) - 1) << 5) | (bm.GetByteAt(up1 + column) << 4) |
                    (bm.GetByteAt(up1 + column + 1) << 3) | (bm.GetByteAt(up1 + column + 2) << 2) |
                    (bm.GetByteAt((up0 + column) - 2) << 1) | (bm.GetByteAt((up0 + column) - 1)));
        }

        protected abstract void CodeRelativeMarkSize(IBitmap bm, int cw, int ch, int border);

        protected abstract int GetDiff(int ignored, MutableValue<int> rel_loc);

        protected virtual int AddLibrary(int shapeno, JB2Shape jshp)
        {
            int libno = _Lib2Shape.Count;
            _Lib2Shape.Add(shapeno);
            Shape2Lib(shapeno, libno);

            //final Rectangle r = new Rectangle();
            //libinfo.addElement(r);
            //jshp.getGBitmap().compute_bounding_box(r);
            _LibInfo.Add(jshp.Bitmap.ComputeBoundingBox());

            return libno;
        }

        protected virtual void ResetNumcoder()
        {
            _DistCommentByte.Value = 0;
            _DistCommentLength.Value = 0;
            _DistRecordType.Value = 0;
            _DistMatchIndex.Value = 0;
            _AbsLocX.Value = 0;
            _AbsLocY.Value = 0;
            _AbsSizeX.Value = 0;
            _AbsSizeY.Value = 0;
            _ImageSizeDist.Value = 0;
            _InheritedShapeCountDist.Value = 0;
            _RelLocXCurrent.Value = 0;
            _RelLocXLast.Value = 0;
            _RelLocYCurrent.Value = 0;
            _RelLocYLast.Value = 0;
            _RelSizeX.Value = 0;
            _RelSizeY.Value = 0;
            _BitCells.Clear();
            _LeftCell.Clear();
            _RightCell.Clear();
            _BitCells.Add(new MutableValue<sbyte>());
            _LeftCell.Add(new MutableValue<int>());
            _RightCell.Add(new MutableValue<int>());

            GC.Collect();
        }

        protected void Shape2Lib(int shapeno, int libno)
        {
            int size = _Shape2Lib.Count;

            if (size <= shapeno)
            {
                while (size++ < shapeno)
                    _Shape2Lib.Add(MinusOneObject);

                _Shape2Lib.Add(libno);
            }
            else
                _Shape2Lib[shapeno] = libno;
        }

        protected int ShiftCrossContext(IBitmap bm, IBitmap cbm, int context, int n, int up1, int up0,
                                                   int xup1, int xup0, int xdn1, int column)
        {
            return (((context << 1) & 0x636) | (bm.GetByteAt(up1 + column + 1) << 8) |
                    (cbm.GetByteAt(xup1 + column) << 6) | (cbm.GetByteAt(xup0 + column + 1) << 3) |
                    (cbm.GetByteAt(xdn1 + column + 1)) | (n << 7));
        }

        protected int ShiftDirectContext(IBitmap bm, int context, int next, int up2, int up1, int up0,
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

        protected virtual void CodeBitmapByCrossCoding(IBitmap bm, IBitmap cbm, int libno)
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
                Rectangle lmatch = (Rectangle)_LibInfo[libno];
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

        protected virtual void CodeBitmapDirectly(IBitmap bm)
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
            if (!_GotStartRecordP)
                throw new SystemException("Image no start");

            int bottom = 0;
            int left = 0;
            int top = 0;
            int right = 0;

            if (_Encoding)
            {
                left = jblt.Left + 1;
                bottom = jblt.Bottom + 1;
                right = (left + columns) - 1;
                top = (bottom + rows) - 1;
            }

            bool new_row = CodeBit((left < _LastLeft), _OffsetTypeDist);

            if (new_row)
            {
                int x_diff = GetDiff(left - _LastRowLeft, _RelLocXLast);
                int y_diff = GetDiff(top - _LastRowBottom, _RelLocYLast);

                if (!_Encoding)
                {
                    left = _LastRowLeft + x_diff;
                    top = _LastRowBottom + y_diff;
                    right = (left + columns) - 1;
                    bottom = (top - rows) + 1;
                }

                _LastLeft = _LastRowLeft = left;
                _LastRight = right;
                _LastBottom = _LastRowBottom = bottom;
                FillShortList(bottom);
            }
            else
            {
                int x_diff = GetDiff(left - _LastRight, _RelLocXCurrent);
                int y_diff = GetDiff(bottom - _LastBottom, _RelLocYCurrent);

                if (!_Encoding)
                {
                    left = _LastRight + x_diff;
                    bottom = _LastBottom + y_diff;
                    right = (left + columns) - 1;
                    top = (bottom + rows) - 1;
                }

                _LastLeft = left;
                _LastRight = right;
                _LastBottom = UpdateShortList(bottom);
            }

            if (!_Encoding)
            {
                jblt.Bottom = bottom - 1;
                jblt.Left = left - 1;
            }
        }

        protected virtual void InitLibrary(JB2Dictionary jim)
        {
            int nshape = jim.InheritedShapes;
            _Shape2Lib.Clear();
            _Lib2Shape.Clear();
            _LibInfo.Clear();

            for (int i = 0; i < nshape; i++)
            {
                int x = i;
                _Shape2Lib.Add(x);
                _Lib2Shape.Add(x);

                JB2Shape jshp = jim.GetShape(i);
                //final Rectangle  r = new Rectangle();
                //libinfo.addElement(r);
                //jshp.getGBitmap().compute_bounding_box(r);
                _LibInfo.Add(jshp.Bitmap.ComputeBoundingBox());
            }
        }

        protected virtual int UpdateShortList(int v)
        {
            if (++_ShortListPos == 3)
                _ShortListPos = 0;

            _ShortList[_ShortListPos] = v;

            return (_ShortList[0] >= _ShortList[1])
                       ? ((_ShortList[0] > _ShortList[2])
                              ? ((_ShortList[1] >= _ShortList[2]) ? _ShortList[1] : _ShortList[2])
                              : _ShortList[0])
                       : ((_ShortList[0] < _ShortList[2])
                              ? ((_ShortList[1] >= _ShortList[2]) ? _ShortList[2] : _ShortList[1])
                              : _ShortList[0]);
        }

        #endregion Protected Methods
    }
}