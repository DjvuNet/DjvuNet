using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using DjvuNet.Graphics;
using DjvuNet.Utilities;

namespace DjvuNet.JB2
{
    public class JB2Image : JB2Dictionary
    {
        #region Private Members

        #endregion Private Members

        #region Public Properties

        #region Height

        /// <summary>
        /// Gets or sets the height of the image
        /// </summary>
        public int Height;

        #endregion Height

        #region Width

        /// <summary>
        /// Gets or sets the width of the image
        /// </summary>
        public int Width;

        #endregion Width

        #region Blits

        private List<JB2Blit> _blits;

        /// <summary>
        /// Gets the blits for the image
        /// </summary>
        public IReadOnlyList<JB2Blit> Blits
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _blits; }
        }

        #endregion Blits

        #endregion Public Properties

        #region Constructors

        public JB2Image() : base()
        {
            _blits = new List<JB2Blit>();
        }

        #endregion Constructors

        #region Public Methods

        public Bitmap GetBitmap()
        {
            return GetBitmap(1);
        }

        public Bitmap GetBitmap(int subsample)
        {
            return GetBitmap(subsample, 1);
        }

        public Bitmap GetBitmap(int subsample, int align)
        {
            Verify.SubsampleRange(subsample);

            if ((Width == 0) || (Height == 0))
                throw new DjvuFormatException(
                    $"Image is empty and can not be used to create bitmap. Width: {Width}, Height {Height}");

            int swidth = ((Width + subsample) - 1) / subsample;
            int sheight = ((Height + subsample) - 1) / subsample;
            int border = (((swidth + align) - 1) & ~(align - 1)) - swidth;

            Bitmap bm = new Bitmap();
            bm.Init(sheight, swidth, border);
            bm.Grays = (1 + (subsample * subsample));

            for (int blitno = 0; blitno < Blits.Count; blitno++)
            //Parallel.For(
            //    0,
            //    Blits.Count,
            //    blitno =>
            //    {
            {
                JB2Blit pblit = GetBlit(blitno);
                JB2Shape pshape = GetShape(pblit.ShapeNumber);

                if (pshape.Bitmap != null)
                    bm.Blit(pshape.Bitmap, pblit.Left, pblit.Bottom, subsample);
                //});
            }

            return bm;
        }

        public Bitmap GetBitmap(Rectangle rect)
        {
            return GetBitmap(rect, 1);
        }

        public Bitmap GetBitmap(Rectangle rect, int subsample)
        {
            return GetBitmap(rect, subsample, 1);
        }

        public Bitmap GetBitmap(Rectangle rect, int subsample, int align)
        {
            return GetBitmap(rect, subsample, align, 0);
        }

        public Bitmap GetBitmap(Rectangle rect, int subsample, int align, int dispy)
        {
            if ((Width == 0) || (Height == 0))
                throw new DjvuFormatException(
                    $"Image is empty and can not be used to create bitmap. Width: {Width}, Height {Height}");

            Verify.SubsampleRange(subsample);

            int rxmin = rect.Right * subsample;
            int rymin = rect.Bottom * subsample;
            int swidth = rect.Width;
            int sheight = rect.Height;
            int border = (((swidth + align) - 1) & ~(align - 1)) - swidth;

            Bitmap bm = new Bitmap();
            bm.Init(sheight, swidth, border);
            bm.Grays = (1 + (subsample * subsample));

            for (int blitno = 0; blitno < Blits.Count; )
            {
                JB2Blit pblit = GetBlit(blitno++);
                JB2Shape pshape = GetShape(pblit.ShapeNumber);

                if (pshape.Bitmap != null)
                    bm.Blit(pshape.Bitmap, pblit.Left - rxmin, (dispy + pblit.Bottom) - rymin, subsample);
            }

            return bm;
        }

        public Bitmap GetBitmap(Rectangle rect, int subsample, int align, int dispy, List<int> components)
        {
            if (components == null)
                return GetBitmap(rect, subsample, align, dispy);

            if ((Width == 0) || (Height == 0))
                throw new DjvuFormatException(
                    $"Image is empty can not be used to create bitmap. Width: {Width}, Height {Height}");

            Verify.SubsampleRange(subsample);

            int rxmin = rect.Right * subsample;
            int rymin = rect.Bottom * subsample;
            int swidth = rect.Width;
            int sheight = rect.Height;
            int border = (((swidth + align) - 1) & ~(align - 1)) - swidth;
            Bitmap bm = new Bitmap();
            bm.Init(sheight, swidth, border);
            bm.Grays = (1 + (subsample * subsample));

            for (int blitno = 0; blitno < Blits.Count; )
            {
                JB2Blit pblit = GetBlit(blitno++);
                JB2Shape pshape = GetShape(pblit.ShapeNumber);

                if (pshape.Bitmap != null)
                {
                    if (bm.Blit(pshape.Bitmap, pblit.Left - rxmin, (dispy + pblit.Bottom) - rymin, subsample))
                        components.Add((blitno - 1));
                }
            }

            return bm;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JB2Blit GetBlit(int blitno)
        {
            return (JB2Blit)_blits[blitno];
        }

        public virtual int AddBlit(JB2Blit jb2Blit)
        {
            if (jb2Blit.ShapeNumber >= ShapeCount)
                throw new ArgumentException("Image bad shape", nameof(jb2Blit));

            int retval = _blits.Count;
            _blits.Add(jb2Blit);

            return retval;
        }

        public override void Decode(IBinaryReader gbs, JB2Dictionary zdict)
        {
            Init();

            JB2Decoder codec = new JB2Decoder();
            codec.Init(gbs, zdict);
            codec.Code(this);
        }

        public override void Init()
        {
            Width = Height = 0;
            _blits.Clear();
            base.Init();
        }

        #endregion Public Methods
    }
}