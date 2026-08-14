using System;
using System.IO;
using DjvuNet.Compression;
using DjvuNet.Graphics;
using DjvuNet.Errors;
using System.Text;
using System.Runtime.CompilerServices;

namespace DjvuNet.JB2
{
    public class JB2Encoder : JB2Codec, IDisposable
    {
        protected bool _Disposed;
        internal JB2Dictionary _ZDict;
        internal ZPCodec _Coder;
        internal byte _ZpBitHolder;

        public JB2Encoder() : base(true)
        {
        }

        public void Init(Stream stream, JB2Dictionary zdict)
        {
            this._ZDict = zdict;
            _Coder = new ZPCodec(stream, encoding: true);
        }

        public void Flush()
        {
            _Coder?.Flush();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    _Coder?.Dispose();
                }

                _Disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }



        public void Encode(JB2Image image)
        {
            if (_Disposed)
                DjvuExceptionUtil.ThrowInvalidOperation("JB2Encoder encoding failed: Cannot encode because the encoder has been disposed.");

            if (_Coder == null)
                DjvuExceptionUtil.ThrowInvalidOperation("JB2Encoder encoding failed: Encoder is not initialized. Call Init() before encoding.");

            if (image == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(image), "JB2 encoding failed: An image is required but was not provided.");
            }

            if (image.InheritedShapes < 0)
                DjvuExceptionUtil.ThrowArgumentOutOfRange("InheritedShapes", image.InheritedShapes, "JB2Encoder encoding failed: InheritedShapes cannot be negative.");

            int firstShape = image.InheritedShapes;
            int nShape = image.ShapeCount;
            int nBlit = image.Blits.Length;
            
            if (firstShape > 0 && image.InheritedDictionary == null)
            {
                DjvuExceptionUtil.ThrowFormatException("JB2Encoder encoding failed: Image requires an inherited dictionary but it was not provided.");
            }

            InitLibrary(image);

            // Tracks if a shape is already encoded into the stream library
            /// TODO: optimizations for lowering GC pressure required
            int[] shape2lib = new int[nShape];
            for (int i = firstShape; i < nShape; i++)
            {
                shape2lib[i] = -1;
            }

            // Code headers
            if (image.InheritedShapes > 0)
            {
                CodeRecordB(RequiredDictOrReset, image, ref Unsafe.NullRef<JB2Shape>(), ref Unsafe.NullRef<JB2Blit>());
            }

            CodeRecordB(StartOfData, image, ref Unsafe.NullRef<JB2Shape>(), ref Unsafe.NullRef<JB2Blit>());

            // Code Comment 
            if (!string.IsNullOrEmpty(image.Comment))
            {
                CodeRecordB(PreservedComment, image, ref Unsafe.NullRef<JB2Shape>(), ref Unsafe.NullRef<JB2Blit>());
            }

            // Encode every blit
            for (int blitNo = 0; blitNo < nBlit; blitNo++)
            {
                JB2Blit jblt = image.Blits[blitNo];
                int shapeNo = jblt.ShapeNumber;

                if (shapeNo < 0 || shapeNo >= nShape)
                    DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(shapeNo), shapeNo, $"JB2 encoding failed: Blit {blitNo} references an out-of-bounds shape number ({shapeNo}). Max valid shape is {nShape - 1}.");

                JB2Shape jshp = image.GetShape(shapeNo);

                // Tests if shape already exists in library
                if (shape2lib[shapeNo] >= 0)
                {
                    CodeRecordB(MatchedCopy, image, ref Unsafe.NullRef<JB2Shape>(), ref jblt);
                }
                else if (jshp.Bitmap != default) 
                {
                    // Make sure all parents have been coded
                    if (jshp.Parent >= 0)
                    {
                        if (jshp.Parent >= nShape)
                            DjvuExceptionUtil.ThrowArgumentOutOfRange("Parent", jshp.Parent, $"JB2 encoding failed: Shape {shapeNo} references an out-of-bounds parent shape ({jshp.Parent}).");

                        if (shape2lib[jshp.Parent] < 0)
                            EncodeLibOnlyShape(image, jshp.Parent, shape2lib);
                    }

                    // For NewMark and MatchedRefine, DjvuLibre uses LIBRARY_CONTAINS_ALL (libraryp = true)
                    // This forces all shapes to be added to the dictionary, maximizing reuse compression
                    if (jshp.Parent < 0)
                    {
                        CodeRecordB(NewMark, image, ref jshp, ref jblt);
                    }
                    else
                    {
                        CodeRecordB(MatchedRefine, image, ref jshp, ref jblt);
                    }

                    // Add shape to library
                    AddLibrary(shapeNo, ref jshp);
                    shape2lib[shapeNo] = 1;
                }

                // Check numcoder status
                if (_BitCells.Count > CellChunk)
                {
                    CodeRecordB(RequiredDictOrReset, null, ref Unsafe.NullRef<JB2Shape>(), ref Unsafe.NullRef<JB2Blit>());
                }
            }

            // Code end of data record
            CodeRecordB(EndOfData, image, ref Unsafe.NullRef<JB2Shape>(), ref Unsafe.NullRef<JB2Blit>()); 
        }

        private void EncodeLibOnlyShape(JB2Image image, int shapeNo, int[] shape2lib, int depth = 0)
        {
            if (depth > MaxParentDepth)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(depth), depth, $"JB2 encoding failed: Maximum shape inheritance depth ({MaxParentDepth}) exceeded. The shape dictionary contains a deep chain or a circular reference.");
            }

            JB2Shape jshp = image.GetShape(shapeNo);
            int parent = jshp.Parent;

            if (parent >= 0)
            {
                if (parent >= shape2lib.Length)
                    DjvuExceptionUtil.ThrowArgumentOutOfRange("Parent", parent, $"JB2 encoding failed: Shape {shapeNo} references an out-of-bounds parent shape ({parent}).");

                if (shape2lib[parent] < 0)
                {
                    EncodeLibOnlyShape(image, parent, shape2lib, depth + 1);
                }
            }
            
            int recType = (parent >= 0) ? MatchedRefineLibraryOnly : NewMarkLibraryOnly;
            
            CodeRecordB(recType, image, ref jshp, ref Unsafe.NullRef<JB2Blit>());
            AddLibrary(shapeNo, ref jshp);
            shape2lib[shapeNo] = 1;
        }

        public void Encode(JB2Dictionary dict)
        {
            if (_Disposed)
                DjvuExceptionUtil.ThrowInvalidOperation("JB2Encoder encoding failed: Cannot encode because the encoder has been disposed.");

            if (_Coder == null)
                DjvuExceptionUtil.ThrowInvalidOperation("JB2Encoder encoding failed: Encoder is not initialized. Call Init() before encoding.");

            if (dict == null)
            {
                DjvuExceptionUtil.ThrowArgumentNull(nameof(dict), "JB2 encoding failed: A shape dictionary is required but was not provided.");
            }

            if (dict.InheritedShapes < 0)
                DjvuExceptionUtil.ThrowArgumentOutOfRange("InheritedShapes", dict.InheritedShapes, "JB2Encoder encoding failed: InheritedShapes cannot be negative.");

            int firstShape = dict.InheritedShapes;
            int nShape = dict.ShapeCount;
            
            InitLibrary(dict);

            // Code headers
            if (dict.InheritedShapes > 0)
            {
                CodeRecordA(RequiredDictOrReset, dict, ref Unsafe.NullRef<JB2Shape>());
            }

            CodeRecordA(StartOfData, dict, ref Unsafe.NullRef<JB2Shape>());

            // Code Comment
            if (!string.IsNullOrEmpty(dict.Comment))
            {
                CodeRecordA(PreservedComment, dict, ref Unsafe.NullRef<JB2Shape>());
            }

            // Encode every shape
            for (int shapeNo = firstShape; shapeNo < nShape; shapeNo++)
            {
                JB2Shape jshp = dict.GetShape(shapeNo);
                
                int recType = (jshp.Parent >= 0) 
                    ? MatchedRefineLibraryOnly 
                    : NewMarkLibraryOnly;

                CodeRecordA(recType, dict, ref jshp);
                
                AddLibrary(shapeNo, ref jshp);

                // Check numcoder status
                if (_BitCells.Count > CellChunk) 
                {
                    CodeRecordA(RequiredDictOrReset, null, ref Unsafe.NullRef<JB2Shape>());
                }
            }

            // Code end of data record
            CodeRecordA(EndOfData, dict, ref Unsafe.NullRef<JB2Shape>()); 
        }

        protected override void CodeAbsoluteLocation(ref JB2Blit jblt, int rows, int columns)
        {
            if (!_GotStartRecordP)
                DjvuExceptionUtil.ThrowFormatException("JB2 encoding failed: Missing required start record.");

            CodeNum(jblt.Left + 1, 1, _ImageColumns, _AbsLocX);
            CodeNum(jblt.Bottom + rows, 1, _ImageRows, _AbsLocY);
        }

        protected override void CodeAbsoluteMarkSize(ref Bitmap bm, int border)
        {
            CodeNum(bm.Width, 0, BigPositive, _AbsSizeX);
            CodeNum(bm.Height, 0, BigPositive, _AbsSizeY);
        }

        protected override bool CodeBit(bool bit, MutableValue<sbyte> ctx)
        {
            byte ctxVal = unchecked((byte)ctx.Value);
            _Coder.Encoder(bit ? 1 : 0, ref ctxVal);
            ctx.Value = (sbyte) ctxVal;
            return bit;
        }

        protected override int CodeBit(bool bit, sbyte[] array, int offset)
        {
            _ZpBitHolder = unchecked((byte)array[offset]);
            _Coder.Encoder(bit ? 1 : 0, ref _ZpBitHolder);
            array[offset] = unchecked((sbyte)_ZpBitHolder);
            return bit ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CodeNum(int num, int low, int high, MutableValue<int> ctx)
        {
            if (num < low || num > high)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(num), num, $"JB2 encoding failed: Encoded value {num} falls outside permitted bounds [{low}, {high}].");
            }
            CodeNum(low, high, ctx, num);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        protected override void CodeBitmapByCrossCoding(ref Bitmap bm, ref Bitmap cbm, int xd2c, int dw, int dy, int cy, int up1, int up0, int xup1, int xup0, int xdn1)
        {
            for (; dy >= 0;)
            {
                int context = GetCrossContext(ref bm, ref cbm, up1, up0, xup1, xup0, xdn1, 0);
                for (int dx = 0; dx < dw;)
                {
                    int bit = bm.GetByteAt(up0 + dx);
                    CodeBit(bit == 1, _CBitDist, context);
                    dx++;
                    context = ShiftCrossContext(ref bm, ref cbm, context, bit, up1, up0, xup1, xup0, xdn1, dx);
                }
                up1 = up0;
                up0 = bm.RowOffset(--dy);
                xup1 = xup0;
                xup0 = xdn1;
                xdn1 = cbm.RowOffset((--cy) - 1) + xd2c;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        protected override unsafe void CodeBitmapByCrossCoding(ref Bitmap bm, ref Bitmap cbm, int xd2c, int dw, int dy, int cy, sbyte* pUp1, sbyte* pUp0, sbyte* pXup1, sbyte* pXup0, sbyte* pXdn1)
        {
            for (; dy >= 0;)
            {
                int context = GetCrossContext(pUp1, pUp0, pXup1, pXup0, pXdn1, 0);
                for (int dx = 0; dx < dw;)
                {
                    int bit = pUp0[dx];
                    CodeBit(bit == 1, _CBitDist, context);
                    dx++;
                    context = ShiftCrossContext(context, bit, pUp1, pUp0, pXup1, pXup0, pXdn1, dx);
                }
                pUp1 = pUp0;
                pUp0 = bm.GetRow(--dy);
                pXup1 = pXup0;
                pXup0 = pXdn1;
                pXdn1 = cbm.GetRow((--cy) - 1) + xd2c;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        protected override void CodeBitmapDirectly(ref Bitmap bm, int dw, int dy, int up2, int up1, int up0)
        {
            for (; dy >= 0;)
            {
                int context = GetDirectContext(ref bm, up2, up1, up0, 0);
                for (int dx = 0; dx < dw;)
                {
                    int bit = bm.GetByteAt(up0 + dx);
                    CodeBit(bit == 1, _BitDist, context);
                    dx++;
                    context = ShiftDirectContext(ref bm, context, bit, up2, up1, up0, dx);
                }
                up2 = up1;
                up1 = up0;
                up0 = bm.RowOffset(--dy);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        protected override unsafe void CodeBitmapDirectly(ref Bitmap bm, int dw, int dy, sbyte* pUp2, sbyte* pUp1, sbyte* pUp0)
        {
            for (; dy >= 0;)
            {
                int context = GetDirectContext(pUp2, pUp1, pUp0, 0);
                for (int dx = 0; dx < dw;)
                {
                    int bit = pUp0[dx];
                    CodeBit(bit == 1, _BitDist, context);
                    dx++;
                    context = ShiftDirectContext(context, bit, pUp2, pUp1, pUp0, dx);
                }
                pUp2 = pUp1;
                pUp1 = pUp0;
                pUp0 = bm.GetRow(--dy);
            }
        }

        protected override string CodeComment(string comment)
        {
            /// TODO: optimizations for lowering GC preassure required
            byte[] commentBuff = Encoding.UTF8.GetBytes(comment);
            int size = commentBuff.Length;

            CodeNum(size, 0, BigPositive, _DistCommentLength);

            for (int i = 0; i < size; i++)
            {
                CodeNum(commentBuff[i], 0, 255, _DistCommentByte);
            }

            return comment;
        }

        protected override void CodeInheritedShapeCount(JB2Dictionary jim)
        {
            int size = jim.InheritedShapes;
            CodeNum(size, 0, BigPositive, _InheritedShapeCountDist);
        }

        protected override int CodeMatchIndex(int index, JB2Dictionary jim)
        {
            if (index < 0 || index >= _Shape2Lib.Count)
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(index), index, $"JB2 encoding failed: Dictionary shape references an unencoded or forward-referenced parent shape at index {index}.");

            int match = _Shape2Lib[index];

            // Forward-referencing shape parents boundary check
            if (match < 0)
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(index), index, $"JB2 encoding failed: Dictionary shape references an unencoded or forward-referenced parent shape at index {index}.");

            CodeNum(match, 0, _Lib2Shape.Count - 1, _DistMatchIndex);
            return match;
        }

        protected override int CodeRecordType(int rectype)
        {
            CodeNum(rectype, StartOfData, EndOfData, _DistRecordType);
            return rectype;
        }

        protected override void CodeImageSize(JB2Dictionary jim)
        {
            CodeNum(0, 0, BigPositive, _ImageSizeDist);
            CodeNum(0, 0, BigPositive, _ImageSizeDist);
            base.CodeImageSize(jim);
        }

        protected override void CodeImageSize(JB2Image jim)
        {
            _ImageColumns = jim.Width;
            CodeNum(_ImageColumns, 0, BigPositive, _ImageSizeDist);
            _ImageRows = jim.Height;
            CodeNum(_ImageRows, 0, BigPositive, _ImageSizeDist);
            base.CodeImageSize(jim);
        }

        protected override void CodeRelativeMarkSize(ref Bitmap bm, int cw, int ch, int border = 0)
        {
            CodeNum(bm.Width - cw, BigNegative, BigPositive, _RelSizeX);
            CodeNum(bm.Height - ch, BigNegative, BigPositive, _RelSizeY);
        }

        protected override int GetDiff(int diff, MutableValue<int> rel_loc)
        {
            CodeNum(diff, BigNegative, BigPositive, rel_loc);
            return diff;
        }
    }
}
