using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Wavelet
{
    public class InterWaveMapEncoder : InterWaveMap
    {
        #region Constructors

        public InterWaveMapEncoder() : base()
        {
        }

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
            //try
            //{
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
                // Interpolate pixels below mask
                InterWaveEncoder.InterpolateMask(data16, Width, Height, BlockWidth, msk8, mskrowsize);

                // Multiscale iterative masked decomposition
                InterWaveEncoder.ForwardMask(data16, Width, Height, BlockWidth, 1, 32, msk8, mskrowsize);
            }
            else
            {
                // Perform traditional decomposition
                InterWaveTransform.Forward(data16, Width, Height, BlockWidth, 1, 32);
            }

            // Copy coefficient into blocks
            p = data16;
            InterWaveBlock[] blocks = Blocks;

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
                    try
                    {
                        // transfer into IW44Image::Block (apply zigzag and scaling)
                        blocks[i * j].ReadLiftBlock(liftblock);
                    }
                    catch (Exception iar)
                    {
                        throw new AggregateException(
                            $"Error while looping i: {i}, j: {j}, nb: {Blocks.Length}, bw: {BlockWidth}, bh: {BlockHeight}", iar);
                    }
                }
                // next row of blocks
                p += 32 * BlockWidth;
            }
            //}
            //catch(Exception ex)
            //{
            //    throw new AggregateException($"Error while ...", ex);
            //}
        }


        /// <summary>
        /// 
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
