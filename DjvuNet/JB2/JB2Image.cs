using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DjvuNet.DataChunks;
using DjvuNet.Errors;
using DjvuNet.Graphics;
using DjvuNet.Utilities;

namespace DjvuNet.JB2
{
    public class JB2Image : JB2Dictionary
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the height of the image
        /// </summary>
        public int Height;

        /// <summary>
        /// Gets or sets the width of the image
        /// </summary>
        public int Width;

        private List<JB2Blit> _Blits;

        /// <summary>
        /// Gets the Span of JB2Blit for the image.
        /// </summary>
        /// <remarks>
        /// <para><b>CRITICAL LIFECYCLE WARNING:</b></para>
        /// <para>
        /// This property returns a direct memory span into the active backing array of the underlying <see cref="List{JB2Blit}"/>.
        /// You <b>MUST NOT</b> mutate the list (e.g., calling <see cref="AddBlit"/>) while holding a reference to this span.
        /// If the list is resized, the backing array will be reallocated, and this span will become a dangling pointer
        /// to an abandoned memory block, leading to silent data loss or corruption upon mutation.
        /// </para>
        /// </remarks>
        public Span<JB2Blit> Blits
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                    return CollectionsMarshal.AsSpan(_Blits);
            }
        }

        #endregion Public Properties

        #region Constructors

        public JB2Image() : base()
        {
            _Blits = new List<JB2Blit>(256);
        }

        #endregion Constructors

        #region Public Methods

        public Bitmap GetBitmap()
        {
            return GetBitmap(1);
        }

        public Bitmap GetBitmap(int subsample)
        {
            return GetBitmap(subsample, 4);
        }

        public Bitmap GetBitmap(int subsample, int align)
        {
            Verify.SubsampleRange(subsample);

            if ((Width == 0) || (Height == 0))
            {
                DjvuExceptionUtil.ThrowFormatException(
                    $"Image is empty and can not be used to create bitmap. Width: {Width}, Height {Height}");
            }

            int swidth = ((Width + subsample) - 1) / subsample;
            int sheight = ((Height + subsample) - 1) / subsample;
            int border = (((swidth + align) - 1) & ~(align - 1)) - swidth;

            Bitmap bm = new Bitmap();
            bm.Init(sheight, swidth, border);
            bm.Grays = (1 + (subsample * subsample));

            for (int blitno = 0; blitno < Blits.Length; blitno++)
            //Parallel.For(
            //    0,
            //    Blits.Count,
            //    blitno =>
            //    {
            {
                JB2Blit pblit = GetBlit(blitno);
                ref JB2Shape pshape = ref GetShape(pblit.ShapeNumber);

                if (pshape.Bitmap != default)
                {
                    bm.Blit(ref pshape.Bitmap, pblit.Left, pblit.Bottom, subsample);
                }
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
            {
                DjvuExceptionUtil.ThrowFormatException(
                    $"Image is empty and can not be used to create bitmap. Width: {Width}, Height {Height}");
            }

            Verify.SubsampleRange(subsample);

            int rxmin = rect.XMin * subsample;
            int rymin = rect.YMin * subsample;
            int swidth = rect.Width;
            int sheight = rect.Height;
            int border = (((swidth + align) - 1) & ~(align - 1)) - swidth;

            Bitmap bm = new Bitmap();
            bm.Init(sheight, swidth, border);
            bm.Grays = (1 + (subsample * subsample));

            for (int blitno = 0; blitno < Blits.Length; )
            {
                JB2Blit pblit = GetBlit(blitno++);
                ref JB2Shape pshape = ref GetShape(pblit.ShapeNumber);

                if (pshape.Bitmap != default)
                {
                    bm.Blit(ref pshape.Bitmap, pblit.Left - rxmin, (dispy + pblit.Bottom) - rymin, subsample);
                }
            }

            return bm;
        }

        public Bitmap GetBitmap(Rectangle rect, int subsample, int align, int dispy, List<int> components)
        {
            if (components == null)
            {
                return GetBitmap(rect, subsample, align, dispy);
            }

            if ((Width == 0) || (Height == 0))
            {
                DjvuExceptionUtil.ThrowFormatException(
                    $"Image is empty can not be used to create bitmap. Width: {Width}, Height {Height}");
            }

            Verify.SubsampleRange(subsample);

            int rxmin = rect.XMin * subsample;
            int rymin = rect.YMin * subsample;
            int swidth = rect.Width;
            int sheight = rect.Height;
            int border = (((swidth + align) - 1) & ~(align - 1)) - swidth;
            
            Bitmap bm = new Bitmap();
            bm.Init(sheight, swidth, border);
            bm.Grays = (1 + (subsample * subsample));

            for (int blitno = 0; blitno < Blits.Length; blitno++)
            {
                JB2Blit pblit = GetBlit(blitno);
                ref JB2Shape pshape = ref GetShape(pblit.ShapeNumber);

                if (pshape.Bitmap != default && bm.Blit(ref pshape.Bitmap, pblit.Left - rxmin, (dispy + pblit.Bottom) - rymin, subsample))
                {
                    components.Add((blitno));
                }
            }

            return bm;
        }

        public PixelMap GetPixelMap(ColorPalette palette, int subsample, int align)
        {
            Verify.SubsampleRange(subsample);

            if ((Width == 0) || (Height == 0))
            {
                DjvuExceptionUtil.ThrowFormatException(
                    $"Image is empty and can not be used to create bitmap. Width: {Width}, Height {Height}");
            }

            int swidth = ((Width + subsample) - 1) / subsample;
            int sheight = ((Height + subsample) - 1) / subsample;
            int border = (((swidth + align) - 1) & ~(align - 1)) - swidth;

            PixelMap pixelMap = new PixelMap(new sbyte[swidth*sheight*3], swidth, sheight);

            // NOTE (Optimization Opportunity): 
            // The C++ reference implementation (DjVuImage::get_pixmap) optimizes this rendering 
            // by grouping all blits that share the exact same palette color index into a single batch,
            // and performing one bulk pm->blit for that entire layer before moving to the next color.
            // This C# implementation skips that batching and sequentially resolves/draws each blit.
            for (int blitno = 0; blitno < Blits.Length; blitno++)
            //Parallel.For(
            //    0,
            //    Blits.Count,
            //    blitno =>
            //    {
            {
                JB2Blit pblit = GetBlit(blitno);
                ref JB2Shape pshape = ref GetShape(pblit.ShapeNumber);
                Pixel color = palette.PaletteColors[palette.BlitColors[blitno]];

                if (pshape.Bitmap != default)
                {
                    pixelMap.Blit(ref pshape.Bitmap, pblit.Left, pblit.Bottom, color);
                }
                //});
            }

            return pixelMap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JB2Blit GetBlit(int blitNo)
        {
            if (blitNo >= 0 && blitNo < Blits.Length)
            {
                return Blits[blitNo];
            }
            else
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(blitNo), $"JB2Blit index out of range {blitNo}");
                return default;
            }
        }

        public virtual int AddBlit(ref JB2Blit jb2Blit)
        {
            if (jb2Blit.ShapeNumber < 0 || jb2Blit.ShapeNumber >= ShapeCount)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(jb2Blit), jb2Blit.ShapeNumber, "JB2 decoding failed: Illegal shape number in JB2Blit.");
            }

            int retval = _Blits.Count;
            _Blits.Add(jb2Blit);
            //System.IO.File.AppendAllText(@"E:\src\.net\DjvuNet\managed_blits.txt", $"{jb2Blit.ShapeNumber},{jb2Blit.Left},{jb2Blit.Bottom}\n");

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
            _Blits.Clear();
            base.Init();
        }

        #endregion Public Methods
    }
}
