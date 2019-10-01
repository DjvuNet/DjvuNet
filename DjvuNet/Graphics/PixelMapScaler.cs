using DjvuNet.Errors;

namespace DjvuNet.Graphics
{
    public class PixelMapScaler
    {
        #region Fields

        private static int FRACBITS = 4;
        private static int FRACSIZE = (1 << FRACBITS);
        private static int FRACSIZE2 = (FRACSIZE >> 1);
        private static int FRACMASK = (FRACSIZE - 1);
        private static short[][] interp = new short[FRACSIZE][];

        private int _Redh;
        private int _Redw;
        private int _XShift;
        private int _YShift;

        internal IPixelMap _PixelMap1;
        internal IPixelMap _PixelMap2;
        internal int[] _HCoord;
        internal int[] _VCoord;
        internal int _SrcHeight;
        internal int _SrcWidth;
        internal int _DestHeight;
        internal int _DestWidth;
        internal int _L1;
        internal int _L2;

        #endregion

        #region Constructors

        public PixelMapScaler()
        {
            _L1 = -1;
            _L2 = -1;
        }

        public PixelMapScaler(int srcWidth, int srcHeight, int destWidth, int destHeight) : this()
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
            _DestWidth = destWidth;
            _DestHeight = destHeight;
            _VCoord = null;
            _HCoord = null;
        }

        public void SetHorzRatio(int numer, int denom)
        {
            if (_SrcWidth <= 0 || _SrcHeight <= 0 || _DestWidth <= 0 || _DestHeight <= 0)
            {
                throw new DjvuInvalidOperationException("Scaler has undefined size");
            }

            // Implicit ratio (determined by the input/output sizes)
            if ((numer == 0) && (denom == 0))
            {
                numer = _DestWidth;
                denom = _SrcWidth;
            }
            else if ((numer <= 0) || (denom <= 0))
            {
                throw new DjvuArgumentOutOfRangeException("Scaler illegal ratio");
            }

            // Compute horz reduction
            _XShift = 0;
            _Redw = _SrcWidth;

            while ((numer + numer) < denom)
            {
                _XShift++;
                _Redw = (_Redw + 1) >> 1;
                numer <<= 1;
            }

            // Compute coordinate table
            if (_HCoord == null)
            {
                _HCoord = new int[_DestWidth];
            }

            PrepareCoord(_HCoord, _Redw, _DestWidth, denom, numer);
        }

        public Rectangle GetRequiredRect(Rectangle targetRect)
        {
            Rectangle red = new Rectangle();
            return CreateRectangles(targetRect, red);
        }

        public void SetSrcSize(int srcWidth, int srcHeight)
        {
            _SrcWidth = srcWidth;
            _SrcHeight = srcHeight;
            _VCoord = null;
            _HCoord = null;
        }

        public void SetVertRatio(int numerator, int denominator)
        {
            if ((_SrcWidth <= 0) || (_SrcHeight <= 0) || (_DestWidth <= 0) || (_DestHeight <= 0))
            {
                throw new DjvuInvalidOperationException("Scaler undefined size");
            }

            // Implicit ratio (determined by the input/output sizes)
            if ((numerator == 0) && (denominator == 0))
            {
                numerator = _DestHeight;
                denominator = _SrcHeight;
            }
            else if ((numerator <= 0) || (denominator <= 0))
            {
                throw new DjvuArgumentOutOfRangeException("Scaler illegal ratio");
            }

            // Compute horz reduction
            _YShift = 0;
            _Redh = _SrcHeight;

            while ((numerator + numerator) < denominator)
            {
                _YShift++;
                _Redh = (_Redh + 1) >> 1;
                numerator <<= 1;
            }

            // Compute coordinate table
            if (_VCoord == null)
            {
                _VCoord = new int[_DestHeight];
            }

            PrepareCoord(_VCoord, _Redh, _DestHeight, denominator, numerator);
        }

        public Rectangle CreateRectangles(Rectangle desired, Rectangle red)
        {
            Rectangle inp = new Rectangle();

            // Parameter validation
            if ((desired.XMin < 0) || (desired.YMin < 0) || (desired.XMax > _DestWidth) || (desired.YMax > _DestHeight))
            {
                throw new DjvuArgumentOutOfRangeException(
                  $"Desired rectangle too big: {desired.XMin}, {desired.YMin}, {desired.XMax}, " +
                  $"{desired.YMax}, {_DestWidth}, {_DestHeight}", nameof(desired));
            }

            // Compute ratio (if not done yet)
            if (_VCoord == null)
            {
                SetVertRatio(0, 0);
            }

            if (_HCoord == null)
            {
                SetHorzRatio(0, 0);
            }

            // Compute reduced bounds
            red.XMin = (_HCoord[desired.XMin]) >> FRACBITS;
            red.YMin = (_VCoord[desired.YMin]) >> FRACBITS;
            red.XMax = ((_HCoord[desired.XMax - 1] + FRACSIZE) - 1) >> FRACBITS;
            red.YMax = ((_VCoord[desired.YMax - 1] + FRACSIZE) - 1) >> FRACBITS;

            // Borders
            red.XMin = (red.XMin > 0) ? red.XMin : 0;
            red.XMax = (red.XMax < _Redw) ? (red.XMax + 1) : _Redw;
            red.YMin = (red.YMin > 0) ? red.YMin : 0;
            red.YMax = (red.YMax < _Redh) ? (red.YMax + 1) : _Redh;

            // Input
            inp.XMin = red.XMin << _XShift;

            if (inp.XMin < 0)
            {
                inp.XMin = 0;
            }

            inp.XMax = red.XMax << _XShift;

            if (inp.XMax > _SrcWidth)
            {
                inp.XMax = _SrcWidth;
            }

            inp.YMin = red.YMin << _YShift;

            if (inp.YMin < 0)
            {
                inp.YMin = 0;
            }

            inp.YMax = red.YMax << _YShift;

            if (inp.YMax > _SrcHeight)
            {
                inp.YMax = _SrcHeight;
            }

            return inp;
        }

        public void Scale(Rectangle srcRect, IPixelMap srcMap, Rectangle targetRect, IPixelMap targetMap)
        {
            // Parameter validation
            if ((srcRect.Width != srcMap.Width) || (srcRect.Height != srcMap.Height))
            {
                throw new DjvuArgumentException("Invalid rectangle", nameof(srcRect));
            }

            // Compute rectangles
            Rectangle required_red = new Rectangle();
            Rectangle sourceRect = CreateRectangles(targetRect, required_red);

            if ( (srcRect.XMin > sourceRect.XMin) || (srcRect.YMin > sourceRect.YMin)
              || (srcRect.XMax < sourceRect.XMax) || (srcRect.YMax < sourceRect.YMax))
            {
                throw new DjvuArgumentException("Invalid rectangle", nameof(srcRect));
            }

            // Adjust output pixmap
            if ((targetRect.Width != (int)targetMap.Width) || (targetRect.Height != (int)targetMap.Height))
            {
                targetMap.Init(targetRect.Height, targetRect.Width, null);
            }

            // Prepare temp stuff
            int bufw = required_red.Width;
            Pixel[] lbuffer = new Pixel[bufw + 2];

            try
            {
                if ((_XShift > 0) || (_YShift > 0))
                {
                    _PixelMap1 = new PixelMap().Init(1, bufw, null);
                    _PixelMap2 = new PixelMap().Init(2, bufw, null);
                    _L1 = _L2 = -1;
                }

                IPixelReference upper = srcMap.CreateGPixelReference(0, 0);
                IPixelReference lower = srcMap.CreateGPixelReference(0, 0);
                IPixelReference dest = targetMap.CreateGPixelReference(0, 0);

                // Loop on output lines
                for (int y = targetRect.YMin; y < targetRect.YMax; y++)
                {
                    // Perform vertical interpolation
                    {
                        int fy = _VCoord[y];
                        int fy1 = fy >> FRACBITS;
                        int fy2 = fy1 + 1;

                        // Obtain upper and lower line in reduced image
                        if ((_XShift > 0) || (_YShift > 0))
                        {
                            lower = GetLine(fy1, required_red, srcRect, srcMap);
                            upper = GetLine(fy2, required_red, srcRect, srcMap);
                        }
                        else
                        {
                            int dx = required_red.XMin - srcRect.XMin;

                            if (required_red.YMin > fy1)
                            {
                                fy1 = required_red.YMin;
                            }

                            if (required_red.YMax <= fy2)
                            {
                                fy2 = required_red.YMax - 1;
                            }

                            lower.SetOffset(fy1 - srcRect.YMin, dx);
                            // srcMap.CreateGPixelReference(fy1 - srcRect.YMin, dx);
                            upper.SetOffset(fy2 - srcRect.YMin, dx);
                             // srcMap.CreateGPixelReference(fy2 - srcRect.YMin, dx);
                        }

                        // Compute line
                        int idest = 1;
                        short[] deltas = interp[fy & FRACMASK];

                        unsafe
                        {
                            for (int edest = idest + bufw; idest < edest; upper.IncOffset(), lower.IncOffset())
                            {
                                Pixel destPix = lbuffer[idest++];

                                int color = 0;
                                sbyte* colorPtr = (sbyte*)&color;
                                // Skip alpha and set pointer to Blue
                                colorPtr++;
                                *colorPtr = lower.Blue;
                                *colorPtr += (sbyte)deltas[(256 + upper.Blue) - *colorPtr];

                                // Set pointer to Green
                                colorPtr++;
                                *colorPtr = lower.Green;
                                *colorPtr += (sbyte)deltas[(256 + upper.Green) - *colorPtr];

                                // Set pointer to Red
                                colorPtr++;
                                *colorPtr = lower.Red;
                                *colorPtr += (sbyte)deltas[(256 + upper.Red) - *colorPtr];

                                //Pixel d = (Pixel) lower.ToPixel();
                                //destPix.SetBGR(d);
                                destPix.SetBGR(*colorPtr);
                            }
                        }
                    }

                    // Perform horizontal interpolation
                    {
                        // Prepare for side effects
                        lbuffer[0] = lbuffer[1];

                        // lbuffer[bufw] = lbuffer[bufw];
                        int line = 1 - required_red.XMin;
                        dest.SetOffset(y - targetRect.YMin, 0);
                            //= targetMap.CreateGPixelReference(y - targetRect.YMin, 0);

                        // Loop horizontally
                        unsafe
                        {
                            for (int x = targetRect.XMin; x < targetRect.XMax; x++)
                            {
                                int n = _HCoord[x];
                                int lowerl = line + (n >> FRACBITS);
                                Pixel lower0 = lbuffer[lowerl];
                                Pixel lower1 = lbuffer[lowerl + 1];
                                short[] deltas = interp[n & FRACMASK];

                                int color = 0;
                                sbyte* colorPtr = (sbyte*) &color;
                                // Skip alpha and set pointer to Blue
                                colorPtr++;
                                *colorPtr = lower0.Blue;
                                *colorPtr += (sbyte) deltas[(256 + lower1.Blue) - *colorPtr];

                                // Set pointer to Green
                                colorPtr++;
                                *colorPtr = lower0.Green;
                                *colorPtr += (sbyte) deltas[(256 + lower1.Green) - *colorPtr];

                                // Set pointer to Red
                                colorPtr++;
                                *colorPtr = lower0.Red;
                                *colorPtr += (sbyte) deltas[(256 + lower1.Red) - *colorPtr];

                                dest.SetBGR(*colorPtr);
                                dest.IncOffset();
                            }
                        }
                    }
                }
            }
            finally
            {
                _PixelMap1 = null;
                _PixelMap2 = null;
            }
        }

        #endregion

        #region Internal Methods

        internal void PrepareCoord(int[] coord, int inmax, int outmax, int @in, int @out)
        {
            int len = (@in * FRACSIZE);
            int beg = ((len + @out) / (2 * @out)) - FRACSIZE2;

            // Bresenham algorithm
            int y = beg;
            int z = @out / 2;
            int inmaxlim = (inmax - 1) * FRACSIZE;

            for (int x = 0; x < outmax; x++)
            {
                coord[x] = (y < inmaxlim) ? y : inmaxlim;
                z = z + len;
                y = y + (z / @out);
                z = z % @out;
            }

            // Result must fit exactly
            if ((@out == outmax) && (y != (beg + len)))
            {
                throw new DjvuInvalidOperationException("Scaler assertion");
            }
        }

        internal IPixelReference GetLine(int fy, Rectangle redRect, Rectangle srcRect, IPixelMap srcMap)
        {
            if (fy < redRect.YMin)
            {
                fy = redRect.YMin;
            }
            else if (fy >= redRect.YMax)
            {
                fy = redRect.YMax - 1;
            }

            // Cached line
            if (fy == _L2)
            {
                return _PixelMap2.CreateGPixelReference(0);
            }

            if (fy == _L1)
            {
                return _PixelMap1.CreateGPixelReference(0);
            }

            // Shift
            IPixelMap p = _PixelMap1;
            _PixelMap1 = _PixelMap2;
            _L1 = _L2;
            _PixelMap2 = p;
            _L2 = fy;

            // Compute location of line
            Rectangle line = new Rectangle();
            line.XMin = redRect.XMin << _XShift;
            line.XMax = redRect.XMax << _XShift;
            line.YMin = fy << _YShift;
            line.YMax = (fy + 1) << _YShift;
            line.Intersect(line, srcRect);
            line.Translate(-srcRect.XMin, -srcRect.YMin);

            // Prepare variables
            int botline = srcMap.RowOffset(line.YMin);
            int rowsize = srcMap.GetRowSize();
            int sw = 1 << _XShift;
            int div = _XShift + _YShift;
            int rnd = 1 << (div - 1);
            int rnd2 = rnd + rnd;

            var inp1 = srcMap.CreateGPixelReference(0);
            var ip = p.CreateGPixelReference(0);

            // Compute averages
            for (int x = line.XMin; x < line.XMax; x += sw, ip.IncOffset())
            {
                int r = 0;
                int g = 0;
                int b = 0;
                int s = 0;
                int inp0 = botline + x;
                int sy2 = line.Height;
                int sy1 = (1 << _YShift);

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
            return _PixelMap2.CreateGPixelReference(0);
        }

        #endregion
    }
}
