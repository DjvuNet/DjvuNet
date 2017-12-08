using System.Runtime.InteropServices;

namespace DjvuNet.Wavelet
{
    public class InterWaveMapEncoder : InterWaveMap
    {
        #region Constructors

        public InterWaveMapEncoder(int width, int height) : base(width, height)
        {
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        ///
        /// </summary>
        /// <param name="img8"></param>
        /// <param name="imgrowsize"></param>
        /// <param name="msk8"></param>
        /// <param name="mskrowsize"></param>
        public unsafe void Create(sbyte* img8, int imgrowsize, sbyte* msk8, int mskrowsize = 0)
        {
            int i, j;

            // Allocate decomposition buffer
            short[] bData16 = new short[BlockWidth * BlockHeight];
            GCHandle pData16 = GCHandle.Alloc(bData16, GCHandleType.Pinned);
            short* data16 = (short*)pData16.AddrOfPinnedObject();

            // Copy pixels
            short* p = data16;
            sbyte* row = img8;

            for (i = 0; i < Height; i++)
            {
                for (j = 0; j < Width; j++)
                    *p++ = (short)(row[j] << InterWaveEncoder.iw_shift);

                row += imgrowsize;

                for (j = Width; j < BlockWidth; j++)
                    *p++ = 0;
            }

            for (i = Height; i < BlockHeight; i++)
                for (j = 0; j < BlockWidth; j++)
                    *p++ = 0;

            // Handle bitmask
            if (msk8 != (sbyte*)0)
            {
                InterpolateMask(data16, Width, Height, BlockWidth, msk8, mskrowsize);
                ForwardMask(data16, Width, Height, BlockWidth, 1, 32, msk8, mskrowsize);
            }
            else
            {
                // Perform traditional decomposition
                InterWaveTransform.Forward(data16, Width, Height, BlockWidth, 1, 32);
            }

            // Copy coefficient into blocks
            p = data16;
            InterWaveBlock[] blocks = Blocks;
            int bidx = 0;

            for (i = 0; i < BlockHeight; i += 32)
            {
                for (j = 0; j < BlockWidth; j += 32)
                {
                    short[] liftblock = new short[1024];
                    GCHandle pLiftblock = GCHandle.Alloc(liftblock, GCHandleType.Pinned);

                    // transfer coefficients at (p+j) into aligned block
                    short* pp = p + j;
                    short* pl = (short*)pLiftblock.AddrOfPinnedObject();

                    for (int ii = 0; ii < 32; ii++, pp += BlockWidth)
                        for (int jj = 0; jj < 32; jj++)
                            *pl++ = pp[jj];
                    // transfer into IW44Image::Block (apply zigzag and scaling)
                    blocks[bidx].ReadLiftBlock(liftblock);
                    bidx++;
                }
                // next row of blocks
                p += 32 * BlockWidth;
            }
        }

        /// <summary>
        /// Calculates multi scale iterative masked decomposition.
        /// </summary>
        /// <param name="data16"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="mask8"></param>
        /// <param name="mskrowsize"></param>
        public static unsafe void ForwardMask(short* data16, int w, int h, int rowsize,
            int begin, int end, sbyte* mask8, int mskrowsize)
        {
            int i, j;
            sbyte* m;
            short* p;
            short* d;
            // Allocate buffers
            short[] ssdata = new short[w * h];
            GCHandle sHandle = GCHandle.Alloc(ssdata, GCHandleType.Pinned);
            short* sdata = (short*)sHandle.AddrOfPinnedObject();

            sbyte[] ssmask = new sbyte[w * h];
            GCHandle sMask = GCHandle.Alloc(ssdata, GCHandleType.Pinned);
            sbyte* smask = (sbyte*)sMask.AddrOfPinnedObject();

            // Copy mask
            m = smask;

            for (i = 0; i < h; i += 1, m += w, mask8 += mskrowsize)
                MemoryUtilities.MoveMemory((void*)m, (void*)mask8, w);

            // Loop over scale
            for (int scale = begin; scale < end; scale <<= 1)
            {
                // Copy data into sdata buffer
                p = data16;
                d = sdata;
                for (i = 0; i < h; i += scale)
                {
                    for (j = 0; j < w; j += scale)
                    {
                        d[j] = p[j];
                    }

                    p += rowsize * scale;
                    d += w * scale;
                }

                // Decompose
                InterWaveTransform.Forward(sdata, w, h, w, scale, scale + scale);

                // Cancel masked coefficients
                d = sdata;
                m = smask;
                for (i = 0; i < h; i += scale + scale)
                {
                    for (j = scale; j < w; j += scale + scale)
                        if (m[j] != 0)
                            d[j] = 0;
                    d += w * scale;
                    m += w * scale;
                    if (i + scale < h)
                    {
                        for (j = 0; j < w; j += scale)
                            if (m[j] != 0)
                                d[j] = 0;
                        d += w * scale;
                        m += w * scale;
                    }
                }

                // Reconstruct
                InterWaveTransform.Backward(sdata, w, h, w, scale + scale, scale);

                // Correct visible pixels
                p = data16;
                d = sdata;
                m = smask;
                for (i = 0; i < h; i += scale)
                {
                    for (j = 0; j < w; j += scale)
                    {
                        if (m[j] == 0)
                            d[j] = p[j];
                    }
                    p += rowsize * scale;
                    m += w * scale;
                    d += w * scale;
                }

                // Decompose again (no need to iterate actually!)
                InterWaveTransform.Forward(sdata, w, h, w, scale, scale + scale);

                // Copy coefficients from sdata buffer
                p = data16;
                d = sdata;
                for (i = 0; i < h; i += scale)
                {
                    for (j = 0; j < w; j += scale)
                        p[j] = d[j];
                    p += rowsize * scale;
                    d += w * scale;
                }

                // Compute new mask for next scale
                m = smask;
                sbyte* m0 = m;
                sbyte* m1 = m;
                for (i = 0; i < h; i += scale + scale)
                {
                    m0 = m1;
                    if (i + scale < h)
                        m1 = m + w * scale;
                    for (j = 0; j < w; j += scale + scale)
                    {
                        if (m[j] != 0 && m0[j] != 0 && m1[j] != 0 && (j <= 0 || m[j - scale] != 0) && (j + scale >= w || m[j + scale] != 0))
                            m[j] = 1;
                        else
                            m[j] = 0;
                    }
                    m = m1 + w * scale;
                }
            }
            // Free buffers
            sMask.Free();
            sHandle.Free();
        }

        /// <summary>
        /// Interpolates pixels below mask in mixed raster image.
        /// </summary>
        /// <param name="data16"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="mask8"></param>
        /// <param name="mskrowsize"></param>
        public static unsafe void InterpolateMask(short* data16, int w, int h, int rowsize, sbyte* mask8, int mskrowsize)
        {
            int i, j;
            // count masked bits
            short[] ccount = new short[w * h];
            GCHandle ccHandle = GCHandle.Alloc(ccount, GCHandleType.Pinned);
            short* count = (short*)ccHandle.AddrOfPinnedObject();
            short* cp = count;

            for (i = 0; i < h; i++, cp += w, mask8 += mskrowsize)
                for (j = 0; j < w; j++)
                    cp[j] = (short)(mask8[j] != 0 ? 0 : 0x1000);

            // copy image
            short[] ssdata = new short[w * h];
            GCHandle sHandle = GCHandle.Alloc(ssdata, GCHandleType.Pinned);
            short* sdata = (short*)sHandle.AddrOfPinnedObject();
            short* p = sdata;
            short* q = data16;

            for (i = 0; i < h; i++, p += w, q += rowsize)
            {
                for (j = 0; j < w; j++)
                    p[j] = q[j];
            }

            // iterate over resolutions
            int split = 1;
            int scale = 2;
            int again = 1;
            while (again != 0 && scale < w && scale < h)
            {
                again = 0;
                p = data16;
                q = sdata;
                cp = count;
                // iterate over block
                for (i = 0; i < h; i += scale, cp += w * scale, q += w * scale, p += rowsize * scale)
                {
                    for (j = 0; j < w; j += scale)
                    {
                        int ii, jj;
                        int gotz = 0;
                        int gray = 0;
                        int npix = 0;
                        short* cpp = cp;
                        short* qq = q;

                        // look around when square goes beyond border
                        int istart = i;
                        if (istart + split > h)
                        {
                            istart -= scale;
                            cpp -= w * scale;
                            qq -= w * scale;
                        }

                        int jstart = j;

                        if (jstart + split > w)
                            jstart -= scale;
                        // compute gray level
                        for (ii = istart; ii < i + scale && ii < h; ii += split, cpp += w * split, qq += w * split)
                        {
                            for (jj = jstart; jj < j + scale && jj < w; jj += split)
                            {
                                if (cpp[jj] > 0)
                                {
                                    npix += cpp[jj];
                                    gray += cpp[jj] * qq[jj];
                                }
                                else if (ii >= i && jj >= j)
                                {
                                    gotz = 1;
                                }
                            }
                        }

                        // process result
                        if (npix == 0)
                        {
                            // continue to next resolution
                            again = 1;
                            cp[j] = 0;
                        }
                        else
                        {
                            gray = gray / npix;
                            // check whether initial image require fix
                            if (gotz != 0)
                            {
                                cpp = cp;
                                qq = p;
                                for (ii = i; ii < i + scale && ii < h; ii += 1, cpp += w, qq += rowsize)
                                {
                                    for (jj = j; jj < j + scale && jj < w; jj += 1)
                                    {
                                        if (cpp[jj] == 0)
                                        {
                                            qq[jj] = (short)gray;
                                            cpp[jj] = 1;
                                        }
                                    }
                                }
                            }
                            // store average for next iteration
                            cp[j] = (short)(npix >> 2);
                            q[j] = (short)gray;
                        }
                    }
                }
                // double resolution
                split = scale;
                scale = scale + scale;
            }

            ccHandle.Free();
            sHandle.Free();
        }


        /// <summary>
        /// Decreases image resolution.
        /// </summary>
        /// <param name="res"></param>
        public void Slashres(int res)
        {
            int minbucket = 1;

            if (res < 2)
                return;

            if (res < 4)
                minbucket = 16;
            else if (res < 8)
                minbucket = 4;

            for (int blockno = 0; blockno < Blocks.Length; blockno++)
            {
                for (int buckno = minbucket; buckno < 64; buckno++)
                    Blocks[blockno].ClearBlock(buckno);
            }
        }

        #endregion Methods
    }
}
