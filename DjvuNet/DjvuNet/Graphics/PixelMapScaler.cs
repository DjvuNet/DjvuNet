using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DjvuNet.Graphics
{
    public class PixelMapScaler
    {
        #region Private Variables

        private static int FRACBITS = 4;
        private static int FRACSIZE = (1 << FRACBITS);
        private static int FRACSIZE2 = (FRACSIZE >> 1);
        private static int FRACMASK = (FRACSIZE - 1);
        private static short[][] interp = new short[FRACSIZE][];

        private PixelMap p1 = null;
        private PixelMap p2 = null;
        private int[] hcoord = null;

        // Fixed point coordinates
        private int[] vcoord = null;
        private int destHeight = 0;
        private int destWidth = 0;

        // Temporaries
        private int l1 = (-1);
        private int l2 = (-1);
        private int redh = 0;
        private int redw = 0;
        private int srcHeight = 0;

        // The sizes
        private int srcWidth = 0;
        private int xshift = 0;
        private int yshift = 0;

        #endregion

        #region Public Properties
        #endregion

        #region Constructors

        public PixelMapScaler()
        {
            // Nothing
        }

        public PixelMapScaler(int srcWidth, int srcHeight, int destWidth, int destHeight)
        {
            SetSrcSize(srcWidth, srcHeight);
            SetDestSize(destWidth, destHeight);
        }

        static PixelMapScaler()
        {
            for (int i = 0; i < FRACSIZE; i++)
            {
                interp[i] = new short[512];
                short[] deltas = interp[i];

                for (int j = -255; j <= 255; j++)
                {
                    deltas[256 + j] = (short)(((j * i) + FRACSIZE2) >> FRACBITS);
                }
            }
        }

        #endregion

        #region Public Methods

        public void SetDestSize(int destWidth, int destHeight)
        {
            this.destWidth = destWidth;
            this.destHeight = destHeight;
            vcoord = null;
            hcoord = null;
        }

        public void SetHorzRatio(int numer, int denom)
        {
            if (
              (srcWidth <= 0)
              || (srcHeight <= 0)
              || (destWidth <= 0)
              || (destHeight <= 0))
            {
                throw new FormatException("Scaler undefined size");
            }

            // Implicit ratio (determined by the input/output sizes)
            if ((numer == 0) && (denom == 0))
            {
                numer = destWidth;
                denom = srcWidth;
            }
            else if ((numer <= 0) || (denom <= 0))
            {
                throw new FormatException("Scaler illegal ratio");
            }

            // Compute horz reduction
            xshift = 0;
            redw = srcWidth;

            while ((numer + numer) < denom)
            {
                xshift++;
                redw = (redw + 1) >> 1;
                numer <<= 1;
            }

            // Compute coordinate table
            if (hcoord == null)
            {
                hcoord = new int[destWidth];
            }

            PrepareCoord(hcoord, redw, destWidth, denom, numer);
        }

        public Rectangle GetRequiredRect(Rectangle targetRect)
        {
            Rectangle red = new Rectangle();

            return CreateRectangles(targetRect, red);
        }

        public void SetSrcSize(int srcWidth, int srcHeight)
        {
            this.srcWidth = srcWidth;
            this.srcHeight = srcHeight;
            vcoord = null;
            hcoord = null;
        }

        public void SetVertRatio(int numer, int denom)
        {
            if (
              (srcWidth <= 0)
              || (srcHeight <= 0)
              || (destWidth <= 0)
              || (destHeight <= 0))
            {
                throw new FormatException("Scaler undefined size");
            }

            // Implicit ratio (determined by the input/output sizes)
            if ((numer == 0) && (denom == 0))
            {
                numer = destHeight;
                denom = srcHeight;
            }
            else if ((numer <= 0) || (denom <= 0))
            {
                throw new FormatException("Scaler illegal ratio");
            }

            // Compute horz reduction
            yshift = 0;
            redh = srcHeight;

            while ((numer + numer) < denom)
            {
                yshift++;
                redh = (redh + 1) >> 1;
                numer <<= 1;
            }

            // Compute coordinate table
            if (vcoord == null)
            {
                vcoord = new int[destHeight];
            }

            PrepareCoord(vcoord, redh, destHeight, denom, numer);
        }

        public Rectangle CreateRectangles(Rectangle desired, Rectangle red)
        {
            Rectangle inp = new Rectangle();

            // Parameter validation
            if (
              (desired.XMin < 0)
              || (desired.YMin < 0)
              || (desired.XMax > destWidth)
              || (desired.YMax > destHeight))
            {
                throw new FormatException(
                  "desired rectangle too big: " + desired.XMin + "," + desired.YMin
                  + "," + desired.XMax + "," + desired.YMax + "," + destWidth + ","
                  + destHeight);
            }

            // Compute ratio (if not done yet)
            if (vcoord == null)
            {
                SetVertRatio(0, 0);
            }

            if (hcoord == null)
            {
                SetHorzRatio(0, 0);
            }

            // Compute reduced bounds
            red.XMin = (hcoord[desired.XMin]) >> FRACBITS;
            red.YMin = (vcoord[desired.YMin]) >> FRACBITS;
            red.XMax = ((hcoord[desired.XMax - 1] + FRACSIZE) - 1) >> FRACBITS;
            red.YMax = ((vcoord[desired.YMax - 1] + FRACSIZE) - 1) >> FRACBITS;

            // Borders
            red.XMin = (red.XMin > 0)
              ? red.XMin
              : 0;
            red.XMax = (red.XMax < redw)
              ? (red.XMax + 1)
              : redw;
            red.YMin = (red.YMin > 0)
              ? red.YMin
              : 0;
            red.YMax = (red.YMax < redh)
              ? (red.YMax + 1)
              : redh;

            // Input
            inp.XMin = red.XMin << xshift;

            if (inp.XMin < 0)
            {
                inp.XMin = 0;
            }

            inp.XMax = red.XMax << xshift;

            if (inp.XMax > srcWidth)
            {
                inp.XMax = srcWidth;
            }

            inp.YMin = red.YMin << yshift;

            if (inp.YMin < 0)
            {
                inp.YMin = 0;
            }

            inp.YMax = red.YMax << yshift;

            if (inp.YMax > srcHeight)
            {
                inp.YMax = srcHeight;
            }

            return inp;
        }

        public void Scale(Rectangle provided_input, PixelMap sourceMap, Rectangle targetRect, PixelMap output)
        {
            // Compute rectangles
            Rectangle required_red = new Rectangle();
            Rectangle sourceRect = CreateRectangles(targetRect, required_red);

            // Parameter validation
            if (
              (provided_input.Width != sourceMap.ImageWidth)
              || (provided_input.Height != sourceMap.ImageHeight))
            {
                throw new FormatException("invalid rectangle");
            }

            if (
              (provided_input.XMin > sourceRect.XMin)
              || (provided_input.YMin > sourceRect.YMin)
              || (provided_input.XMax < sourceRect.XMax)
              || (provided_input.YMax < sourceRect.YMax))
            {
                throw new FormatException("invalid rectangle");
            }

            // Adjust output pixmap
            if (
              (targetRect.Width != (int)output.ImageWidth)
              || (targetRect.Height != (int)output.ImageHeight))
            {
                output.Init(
                  targetRect.Height,
                  targetRect.Width,
                  null);
            }

            // Prepare temp stuff 
            int bufw = required_red.Width;
            Pixel[] lbuffer = new Pixel[bufw + 2];

            for (int i = 0; i < lbuffer.Length; )
            {
                lbuffer[i++] = new Pixel();
            }

            try
            {
                if ((xshift > 0) || (yshift > 0))
                {
                    p1 = new PixelMap().Init(1, bufw, null);
                    p2 = new PixelMap().Init(2, bufw, null);
                    l1 = l2 = -1;
                }

                // Loop on output lines
                for (int y = targetRect.YMin; y < targetRect.YMax; y++)
                {
                    // Perform vertical interpolation
                    {
                        int fy = vcoord[y];
                        int fy1 = fy >> FRACBITS;
                        int fy2 = fy1 + 1;
                        PixelReference upper;
                        PixelReference lower;

                        // Obtain upper and lower line in reduced image
                        if ((xshift > 0) || (yshift > 0))
                        {
                            lower = GetLine(fy1, required_red, provided_input, sourceMap);
                            upper = GetLine(fy2, required_red, provided_input, sourceMap);
                        }
                        else
                        {
                            int dx = required_red.XMin - provided_input.XMin;

                            if (required_red.YMin > fy1)
                            {
                                fy1 = required_red.YMin;
                            }

                            if (required_red.YMax <= fy2)
                            {
                                fy2 = required_red.YMax - 1;
                            }

                            lower =
                              sourceMap.CreateGPixelReference(fy1 - provided_input.YMin, dx);
                            upper =
                              sourceMap.CreateGPixelReference(fy2 - provided_input.YMin, dx);
                        }

                        // Compute line
                        int idest = 1;
                        short[] deltas = interp[fy & FRACMASK];

                        for (
                          int edest = idest + bufw;
                          idest < edest;
                          upper.IncOffset(), lower.IncOffset())
                        {
                            Pixel dest = lbuffer[idest++];
                            int lower_r = lower.Red;
                            int delta_r = deltas[(256 + upper.Red) - lower_r];
                            int lower_g = lower.Green;
                            int delta_g = deltas[(256 + upper.Green) - lower_g];
                            int lower_b = lower.Blue;
                            int delta_b = deltas[(256 + upper.Blue) - lower_b];
                            dest.SetBGR(lower_b + delta_b, lower_g + delta_g, lower_r + delta_r);
                        }
                    }

                    // Perform horizontal interpolation
                    {
                        // Prepare for side effects
                        lbuffer[0] = lbuffer[1];

                        // lbuffer[bufw] = lbuffer[bufw];
                        int line = 1 - required_red.XMin;
                        PixelReference dest =
                          output.CreateGPixelReference(y - targetRect.YMin, 0);

                        // Loop horizontally
                        for (int x = targetRect.XMin; x < targetRect.XMax; x++)
                        {
                            int n = hcoord[x];
                            int lower = line + (n >> FRACBITS);
                            Pixel lower0 = lbuffer[lower];
                            Pixel lower1 = lbuffer[lower + 1];
                            short[] deltas = interp[n & FRACMASK];
                            int lower_r = lower0.Red;
                            int delta_r = deltas[(256 + lower1.Red) - lower_r];
                            int lower_g = lower0.Green;
                            int delta_g = deltas[(256 + lower1.Green) - lower_g];
                            int lower_b = lower0.Blue;
                            int delta_b = deltas[(256 + lower1.Blue) - lower_b];
                            dest.SetBGR(lower_b + delta_b, lower_g + delta_g, lower_r + delta_r);
                            dest.IncOffset();
                        }
                    }
                }
            }
            finally
            {
                p1 = null;
                p2 = null;
            }
        }

        #endregion

        #region Private Methods

        private void PrepareCoord(int[] coord, int inmax, int outmax, int @in, int @out)
        {
            int len = (@in * FRACSIZE);
            int beg = ((len + @out) / (2 * @out)) - FRACSIZE2;

            // Bresenham algorithm
            int y = beg;
            int z = @out / 2;
            int inmaxlim = (inmax - 1) * FRACSIZE;

            for (int x = 0; x < outmax; x++)
            {
                coord[x] = (y < inmaxlim)
                  ? y
                  : inmaxlim;
                z = z + len;
                y = y + (z / @out);
                z = z % @out;
            }

            // Result must fit exactly
            if ((@out == outmax) && (y != (beg + len)))
            {
                throw new FormatException("Scaler assertion");
            }
        }

        private PixelReference GetLine(int fy, Rectangle required_red, Rectangle provided_input, PixelMap input)
        {
            if (fy < required_red.YMin)
            {
                fy = required_red.YMin;
            }
            else if (fy >= required_red.YMax)
            {
                fy = required_red.YMax - 1;
            }

            // Cached line
            if (fy == l2)
            {
                return p2.CreateGPixelReference(0);
            }

            if (fy == l1)
            {
                return p1.CreateGPixelReference(0);
            }

            // Shift
            PixelMap p = p1;
            p1 = p2;
            l1 = l2;
            p2 = p;
            l2 = fy;

            // Compute location of line
            Rectangle line = new Rectangle();
            line.XMin = required_red.XMin << xshift;
            line.XMax = required_red.XMax << xshift;
            line.YMin = fy << yshift;
            line.YMax = (fy + 1) << yshift;
            line.Intersect(line, provided_input);
            line.Translate(-provided_input.XMin, -provided_input.YMin);

            // Prepare variables
            int botline = input.RowOffset(line.YMin);
            int rowsize = input.GetRowSize();
            int sw = 1 << xshift;
            int div = xshift + yshift;
            int rnd = 1 << (div - 1);
            int rnd2 = rnd + rnd;

            PixelReference inp1 = input.CreateGPixelReference(0);
            PixelReference ip = p.CreateGPixelReference(0);

            // Compute averages
            for (int x = line.XMin; x < line.XMax; x += sw, ip.IncOffset())
            {
                int r = 0;
                int g = 0;
                int b = 0;
                int s = 0;
                int inp0 = botline + x;
                int sy2 = line.Height;
                int sy1 = (1 << yshift);

                if (sy1 > sy2)
                {
                    sy1 = sy2;
                }

                for (int sy = 0; sy < sy1; sy++, inp0 += rowsize)
                {
                    int sx1 = x + sw;
                    inp1.SetOffset(inp0);

                    if (sx1 > line.XMax)
                    {
                        sx1 = line.XMax;
                    }

                    for (int sx = sx1 - x; sx-- > 0; s++, inp1.IncOffset())
                    {
                        r += inp1.Red;
                        g += inp1.Green;
                        b += inp1.Blue;
                    }
                }

                if (s == rnd2)
                {
                    ip.SetBGR((b + rnd) >> div, (g + r) >> div, (r + rnd) >> div);
                }
                else
                {
                    ip.SetBGR((b + (s / 2)) / 2, (g + (s / 2)) / s, (r + (s / 2)) / s);
                }
            }

            // Return
            return p2.CreateGPixelReference(0);
        }

        #endregion
    }
}
