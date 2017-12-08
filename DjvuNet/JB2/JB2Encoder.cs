using System;
using DjvuNet.Compression;
using DjvuNet.Graphics;

namespace DjvuNet.JB2
{
    public class JB2Encoder : JB2Codec
    {
        public JB2Encoder() : base(true)
        {
        }

        protected override void CodeAbsoluteLocation(JB2Blit jblt, int rows, int columns)
        {
            throw new NotImplementedException();
        }

        protected override void CodeAbsoluteMarkSize(IBitmap bm, int border)
        {
            throw new NotImplementedException();
        }

        protected override bool CodeBit(bool bit, MutableValue<sbyte> ctx)
        {
            throw new NotImplementedException();
        }

        protected override int CodeBit(bool bit, sbyte[] array, int offset)
        {
            throw new NotImplementedException();
        }

        protected override void CodeBitmapByCrossCoding(IBitmap bm, IBitmap cbm, int xd2c, int dw, int dy, int cy, int up1, int up0, int xup1, int xup0, int xdn1)
        {
            throw new NotImplementedException();
        }

        protected override void CodeBitmapDirectly(IBitmap bm, int dw, int dy, int up2, int up1, int up0)
        {
            throw new NotImplementedException();
        }

        protected override string CodeComment(string comment)
        {
            throw new NotImplementedException();
        }

        protected override void CodeInheritedShapeCount(JB2Dictionary jim)
        {
            throw new NotImplementedException();
        }

        protected override int CodeMatchIndex(int index, JB2Dictionary jim)
        {
            throw new NotImplementedException();
        }

        protected override int CodeRecordType(int rectype)
        {
            throw new NotImplementedException();
        }

        protected override void CodeRelativeMarkSize(IBitmap bm, int cw, int ch, int border)
        {
            throw new NotImplementedException();
        }

        protected override int GetDiff(int ignored, MutableValue<int> rel_loc)
        {
            throw new NotImplementedException();
        }
    }
}
