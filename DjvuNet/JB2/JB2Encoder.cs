using System;
using DjvuNet.Compression;
using DjvuNet.Graphics;
using DjvuNet.Errors;

namespace DjvuNet.JB2
{
    public class JB2Encoder : JB2Codec
    {
        public JB2Encoder() : base(true)
        {
        }

        protected override void CodeAbsoluteLocation(JB2Blit jblt, int rows, int columns)
        {
            DjvuExceptionUtil.ThrowNotImplemented();
        }

        protected override void CodeAbsoluteMarkSize(IBitmap bm, int border)
        {
            DjvuExceptionUtil.ThrowNotImplemented();
        }

        protected override bool CodeBit(bool bit, MutableValue<sbyte> ctx)
        {
            DjvuExceptionUtil.ThrowNotImplemented();
            return default;
        }

        protected override int CodeBit(bool bit, sbyte[] array, int offset)
        {
            DjvuExceptionUtil.ThrowNotImplemented();
            return default;
        }

        protected override void CodeBitmapByCrossCoding(IBitmap bm, IBitmap cbm, int xd2c, int dw, int dy, int cy, int up1, int up0, int xup1, int xup0, int xdn1)
        {
            DjvuExceptionUtil.ThrowNotImplemented();
        }

        protected override void CodeBitmapDirectly(IBitmap bm, int dw, int dy, int up2, int up1, int up0)
        {
            DjvuExceptionUtil.ThrowNotImplemented();
        }

        protected override string CodeComment(string comment)
        {
            DjvuExceptionUtil.ThrowNotImplemented();
            return default;
        }

        protected override void CodeInheritedShapeCount(JB2Dictionary jim)
        {
            DjvuExceptionUtil.ThrowNotImplemented();
        }

        protected override int CodeMatchIndex(int index, JB2Dictionary jim)
        {
            DjvuExceptionUtil.ThrowNotImplemented();
            return default;
        }

        protected override int CodeRecordType(int rectype)
        {
            DjvuExceptionUtil.ThrowNotImplemented();
            return default;
        }

        protected override void CodeRelativeMarkSize(IBitmap bm, int cw, int ch, int border)
        {
            DjvuExceptionUtil.ThrowNotImplemented();
        }

        protected override int GetDiff(int ignored, MutableValue<int> rel_loc)
        {
            DjvuExceptionUtil.ThrowNotImplemented();
            return default;
        }
    }
}
