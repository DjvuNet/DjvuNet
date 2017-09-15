using System;
using System.Runtime.InteropServices;
using DjvuNet.Errors;
using DjvuNet.Graphics;

namespace DjvuNet.Wavelet
{
    /// <summary>
    /// This class represents structured wavelet data.
    /// </summary>
    public class InterWaveMap : IInterWaveMap
    {
        #region Public Fields

        /// <summary>
        /// Image height in blocks
        /// </summary>
        public int BlockHeight;

        /// <summary>
        /// Array of image blocks
        /// </summary>
        public InterWaveBlock[] Blocks;

        /// <summary>
        /// Width of image in blocks
        /// </summary>
        public int BlockWidth;

        /// <summary>
        /// Image height
        /// </summary>
        public int Height;

        /// <summary>
        /// Image width
        /// </summary>
        public int Width;

        /// <summary>
        /// Gets or sets the Nb value
        /// </summary>
        public int BlockNumber;

        /// <summary>
        /// Gets or sets the top value
        /// </summary>
        public int Top;

        #endregion Public Fields

        #region Constructors

        /// <summary> Creates a new Map object.</summary>
        public InterWaveMap()
        {
        }

        public InterWaveMap(int width, int height)
        {
            Width = width;
            Height = height;
            BlockWidth = ((width + 0x20) - 1) & unchecked((int)0xffffffe0);
            BlockHeight = ((height + 0x20) - 1) & unchecked((int)0xffffffe0);
            BlockNumber = (BlockWidth * BlockHeight) / 1024;
            Blocks = new InterWaveBlock[BlockNumber];

            for (int i = 0; i < Blocks.Length; i++)
                Blocks[i] = new InterWaveBlock();
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Duplicate InterWaveMap
        /// </summary>
        /// <returns></returns>
        public InterWaveMap Duplicate()
        {
            InterWaveMap retval = null;

            try
            {
                retval = new InterWaveMap
                {
                    BlockHeight = BlockHeight,
                    Blocks = (InterWaveBlock[])Blocks.Clone(),
                    BlockWidth = BlockWidth,
                    Height = Height,
                    Width = Width,
                    BlockNumber = BlockNumber,
                    Top = Top
                };

                //IWBlock[] blocks = (IWBlock[])this.Blocks.Clone();
                //((IWMap)retval).Blocks = blocks;

                for (int i = 0; i < BlockNumber; i++)
                    retval.Blocks[i] = Blocks[i].Duplicate();
            }
            catch
            {
            }

            return retval;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="p"></param>
        /// <param name="pidx"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public static void Backward(short[] p, int pidx, int w, int h, int rowsize, int begin, int end)
        {
            for (int scale = begin >> 1; scale >= end; scale >>= 1)
            {
                for (int j = 0; j < w; j += scale)
                    BackwardFilter(p, pidx, j, j + (h * rowsize), j, scale * rowsize);

                for (int i = 0; i < h; i += scale)
                    BackwardFilter(p, pidx, i * rowsize, (i * rowsize) + w, i * rowsize, scale);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="p"></param>
        /// <param name="pidx"></param>
        /// <param name="b"></param>
        /// <param name="e"></param>
        /// <param name="z"></param>
        /// <param name="s"></param>
        public static void BackwardFilter(short[] p, int pidx, int b, int e, int z, int s)
        {
            int s3 = 3 * s;

            if ((z < b) || (z > e))
                throw new DjvuFormatException("Filter parameters were out of bounds");

            int n = z;
            int bb;
            int cc;
            int aa = bb = cc = 0;
            int dd = ((n + s) >= e) ? 0 : ((p[pidx + n + s]));

            for (; (n + s3) < e; n = (n + s3) - s)
            {
                aa = bb;
                bb = cc;
                cc = dd;
                dd = p[pidx + n + s3];
                p[pidx + n] = (short)(p[pidx + n] - ((((9 * (bb + cc)) - (aa + dd)) + 16) >> 5));
            }

            for (; n < e; n = n + s + s)
            {
                aa = bb;
                bb = cc;
                cc = dd;
                dd = 0;
                p[pidx + n] = (short)(p[pidx + n] - ((((9 * (bb + cc)) - (aa + dd)) + 16) >> 5));
            }

            n = z + s;
            aa = 0;
            bb = p[(pidx + n) - s];
            cc = ((n + s) >= e) ? 0 : ((p[pidx + n + s]));
            dd = ((n + s3) >= e) ? 0 : ((p[pidx + n + s3]));

            if (n < e)
            {
                int x = bb;

                if ((n + s) < e)
                    x = (bb + cc + 1) >> 1;

                p[pidx + n] = (short)(p[pidx + n] + x);
                n = n + s + s;
            }

            for (; (n + s3) < e; n = (n + s3) - s)
            {
                aa = bb;
                bb = cc;
                cc = dd;
                dd = p[pidx + n + s3];

                int x = (((9 * (bb + cc)) - (aa + dd)) + 8) >> 4;
                p[pidx + n] = (short)(p[pidx + n] + x);
            }

            if ((n + s) < e)
            {
                aa = bb;
                bb = cc;
                cc = dd;
                dd = 0;

                int x = (bb + cc + 1) >> 1;
                p[pidx + n] = (short)(p[pidx + n] + x);
                n = n + s + s;
            }

            if (n < e)
            {
                aa = bb;
                bb = cc;
                cc = dd;
                dd = 0;

                int x = bb;
                p[pidx + n] = (short)(p[pidx + n] + x);
            }
        }

        /// <summary>
        /// Returns bucket count.
        /// </summary>
        /// <returns></returns>
        public int GetBucketCount()
        {
            int buckets = 0;

            for (int blockno = 0; blockno < BlockNumber; blockno++)
            {
                for (int buckno = 0; buckno < 64; buckno++)
                {
                    if (Blocks[blockno].GetBlock(buckno) != null)
                        buckets++;
                }
            }

            return buckets;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <param name="img8"></param>
        /// <param name="rowsize"></param>
        /// <param name="pixsep"></param>
        /// <param name="fast"></param>
        public void Image(int index, sbyte[] img8, int rowsize, int pixsep, bool fast)
        {
            short[] data16 = new short[BlockWidth * BlockHeight];
            short[] liftblock = new short[1024];
            int pidx = 0;
            InterWaveBlock[] block = Blocks;
            int blockidx = 0;
            int ppidx = 0;

            for (int i = 0; i < BlockHeight; i += 32, pidx += (32 * BlockWidth))
            {
                for (int j = 0; j < BlockWidth; j += 32)
                {
                    // when passed to WriteLiftBlock liftblock should contain zeros only
                    block[blockidx].WriteLiftBlock(liftblock, 0, 64);
                    blockidx++;

                    ppidx = pidx + j;

                    for (int ii = 0, p1idx = 0; ii++ < 32; p1idx += 32, ppidx += BlockWidth)
                        Array.Copy(liftblock, p1idx, data16, ppidx, 32);
                }
            }

            if (fast)
            {
                Backward(data16, 0, Width, Height, BlockWidth, 32, 2);
                pidx = 0;

                for (int i = 0; i < BlockHeight; i += 2, pidx += BlockWidth)
                {
                    for (int jj = 0; jj < BlockWidth; jj += 2, pidx += 2)
                        data16[pidx + BlockWidth] = data16[pidx + BlockWidth + 1] = data16[pidx + 1] = data16[pidx];
                }
            }
            else
                Backward(data16, 0, Width, Height, BlockWidth, 32, 1);

            pidx = 0;

            for (int i = 0, rowidx = index; i++ < Height; rowidx += rowsize, pidx += BlockWidth)
            {
                for (int j = 0, pixidx = rowidx; j < Width; pixidx += pixsep)
                {
                    int x = (data16[pidx + (j++)] + 32) >> 6;

                    if (x < -128)
                        x = -128;
                    else if (x > 127)
                        x = 127;

                    img8[pixidx] = (sbyte)x;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="subsample"></param>
        /// <param name="rect"></param>
        /// <param name="index"></param>
        /// <param name="img8"></param>
        /// <param name="rowsize"></param>
        /// <param name="pixsep"></param>
        /// <param name="fast"></param>
        public void Image(int subsample, Rectangle rect, int index, sbyte[] img8, int rowsize, int pixsep, bool fast)
        {
            int nlevel = 0;

            while ((nlevel < 5) && ((32 >> nlevel) > subsample))
                nlevel++;

            int boxsize = 1 << nlevel;

            if (subsample != (32 >> nlevel))
                throw new DjvuArgumentOutOfRangeException("Unsupported subsampling factor");

            if (rect.Empty)
                throw new DjvuArgumentException("Rectangle is empty", nameof(rect));

            Rectangle irect = new Rectangle(
                0, 0, ((Width + subsample) - 1) / subsample, ((Height + subsample) - 1) / subsample);

            if ((rect.Right < 0) || (rect.Bottom < 0) || (rect.Left > irect.Left) || (rect.Top > irect.Top))
                throw new DjvuArgumentException(
                    "Rectangle is out of bounds: " + rect.Right + "," + rect.Bottom +
                    "," + rect.Left + "," + rect.Top + "," + irect.Left + "," + irect.Top, nameof(rect));

            Rectangle[] needed = new Rectangle[8];
            Rectangle[] recomp = new Rectangle[8];

            for (int i = 0; i < 8; )
            {
                needed[i] = new Rectangle();
                recomp[i++] = new Rectangle();
            }

            int r = 1;
            needed[nlevel] = (Rectangle)rect.Duplicate();
            recomp[nlevel] = (Rectangle)rect.Duplicate();

            for (int i = nlevel - 1; i >= 0; i--)
            {
                needed[i] = recomp[i + 1];
                needed[i].Inflate(3 * r, 3 * r);
                needed[i].Intersect(needed[i], irect);
                r += r;
                recomp[i].Right = ((needed[i].Right + r) - 1) & ~(r - 1);
                recomp[i].Left = needed[i].Left & ~(r - 1);
                recomp[i].Bottom = ((needed[i].Bottom + r) - 1) & ~(r - 1);
                recomp[i].Top = needed[i].Top & ~(r - 1);
            }

            Rectangle work = new Rectangle();
            work.Right = needed[0].Right & ~(boxsize - 1);
            work.Bottom = needed[0].Bottom & ~(boxsize - 1);
            work.Left = ((needed[0].Left - 1) & ~(boxsize - 1)) + boxsize;
            work.Top = ((needed[0].Top - 1) & ~(boxsize - 1)) + boxsize;

            int dataw = work.Width;
            short[] data = new short[dataw * work.Height];
            int blkw = BlockWidth >> 5;
            int lblock = ((work.Bottom >> nlevel) * blkw) + (work.Right >> nlevel);

            short[] liftblock = new short[1024];

            for (int by = work.Bottom, ldata = 0;
                 by < work.Top;
                 by += boxsize, ldata += (dataw << nlevel), lblock += blkw)
            {
                for (int bx = work.Right, bidx = lblock, rdata = ldata;
                     bx < work.Left;
                     bx += boxsize, bidx++, rdata += boxsize)
                {
                    InterWaveBlock block = Blocks[bidx];
                    int mlevel = nlevel;

                    if ((nlevel > 2) &&
                        (((bx + 31) < needed[2].Right) || (bx > needed[2].Left) || ((by + 31) < needed[2].Bottom) ||
                         (by > needed[2].Top)))
                    {
                        mlevel = 2;
                    }

                    int bmax = ((1 << (mlevel + mlevel)) + 15) >> 4;
                    int ppinc = 1 << (nlevel - mlevel);
                    int ppmod1 = dataw << (nlevel - mlevel);
                    int ttmod0 = 32 >> mlevel;
                    int ttmod1 = ttmod0 << 5;

                    // liftblock should contain zeros only
                    block.WriteLiftBlock(liftblock, 0, bmax);

                    for (int ii = 0, tt = 0, pp = rdata; ii < boxsize; ii += ppinc, pp += ppmod1, tt += (ttmod1 - 32))
                    {
                        for (int jj = 0; jj < boxsize; jj += ppinc, tt += ttmod0)
                            data[pp + jj] = liftblock[tt];
                    }
                }
            }

            r = boxsize;

            for (int i = 0; i < nlevel; i++)
            {
                Rectangle comp = needed[i];
                comp.Right = comp.Right & ~(r - 1);
                comp.Bottom = comp.Bottom & ~(r - 1);
                comp.Translate(-work.Right, -work.Bottom);

                if (fast && (i >= 4))
                {
                    for (int ii = comp.Bottom, pp = (comp.Bottom * dataw); ii < comp.Top; ii += 2, pp += (dataw + dataw))
                    {
                        for (int jj = comp.Right; jj < comp.Left; jj += 2)
                            data[pp + jj + dataw] = data[pp + jj + dataw + 1] = data[pp + jj + 1] = data[pp + jj];
                    }

                    break;
                }

                Backward(data, (comp.Bottom * dataw) + comp.Right, comp.Width, comp.Height, dataw, r, r >> 1);
                r >>= 1;
            }

            Rectangle nrect = rect.Duplicate();
            nrect.Translate(-work.Right, -work.Bottom);

            for (int i = nrect.Bottom, pidx = (nrect.Bottom * dataw), ridx = index;
                 i++ < nrect.Top;
                 ridx += rowsize, pidx += dataw)
            {
                for (int j = nrect.Right, pixidx = ridx; j < nrect.Left; j++, pixidx += pixsep)
                {
                    int x = (data[pidx + j] + 32) >> 6;

                    if (x < -128)
                        x = -128;
                    else if (x > 127)
                        x = 127;

                    img8[pixidx] = (sbyte)x;
                }
            }
        }

        #endregion Public Methods
    }
}