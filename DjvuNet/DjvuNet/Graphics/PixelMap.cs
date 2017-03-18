using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DjvuNet.Graphics
{
    /// <summary>
    /// This class represents 24 bit color image maps.
    /// </summary>
    public class PixelMap : Map
    {
        #region Private Variables

        /// <summary>Used to represent division as multiplication. </summary>
        private static readonly int[] _invmap = new int[256];

        /// <summary>Identity color correction table. </summary>
        private static readonly int[] _ctableI = new int[256];

        /// <summary>Cached color correction table. </summary>
        private static int[] _ctable = new int[256];

        /// <summary> The color correction subsample for the cached color table. </summary>
        private static double _lgamma = -1D;

        /// <summary>Used to quickly clip out of bounds values. </summary>
        private static readonly sbyte[] _clip = new sbyte[512];

        /// <summary>Used for attenuation </summary>
        private static readonly Object[] _multiplierRefArray = new Object[256];

        private static object _syncObject = new object();

        #endregion Private Variables

        #region Public Properties

        #endregion Public Properties

        #region Constructors

        static PixelMap()
        {
            for (int i = 0; i < _clip.Length; i++)
            {
                _clip[i] = (sbyte)((i < 256) ? i : 255);
            }

            for (int i = 1; i < _invmap.Length; i++)
            {
                _invmap[i] = 0x10000 / i;
            }

            for (int i = 0; i < _ctableI.Length; i++)
            {
                _ctableI[i] = i;
            }

            for (int i = 1; i < _multiplierRefArray.Length; )
            {
                _multiplierRefArray[i++] = null;
            }
        }

        /// <summary> Creates a new PixelMap object.</summary>
        public PixelMap()
            : base(3, 2, 1, 0, false)
        {
            // Nothing
        }

        #endregion Constructors

        #region Public Methods

        /// <summary> 
        /// Fill the array with color correction constants.
        /// </summary>
        /// <param name="gamma">
        /// color correction subsample
        /// </param>
        /// <returns> 
        /// the new color correction table
        /// </returns>
        public static int[] GetColorCorrection(double gamma)
        {
            lock (_syncObject)
            {
                if ((gamma < 0.10000000000000001D) || (gamma > 10D))
                {
                    throw new Exception("Illegal gamma: " + gamma);
                }

                int[] retval;
                if ((gamma < 1.0009999999999999D) && (gamma > 0.999D))
                {
                    retval = _ctableI;
                }
                else
                {
                    if (gamma != _lgamma)
                    {
                        _ctable = new int[256];
                        for (int i = 0; i < _ctable.Length; i++)
                        {
                            double x = i / 255D;

                            //if (DjVuOptions.BEZIERGAMMA)
                            {
                                double t = (Math.Sqrt(1.0D + (((gamma * gamma) - 1.0D) * x)) - 1.0D) / (gamma - 1.0D);
                                x = ((((1.0D - gamma) * t) + (2D * gamma)) * t) / (gamma + 1.0D);
                            }
                            //else
                            //{
                            //    x = System.Math.Pow(x, 1.0D / gamma);
                            //}

                            _ctable[i] = (int)Math.Floor((255D * x) + 0.5D);
                        }
                        _lgamma = gamma;
                    }
                    retval = _ctable;
                }
                return retval;
            }
        }

        /// <summary> 
        /// Attenuate the specified bitmap.
        /// </summary>
        /// <param name="bm">
        /// Bitmap to attenuate
        /// </param>
        /// <param name="xpos">
        /// horizontal position
        /// </param>
        /// <param name="ypos">
        /// vertical position
        /// </param>
        public virtual void Attenuate(Bitmap bm, int xpos, int ypos)
        {
            // Check
            // Compute number of rows and columns
            int xrows = ypos + bm.ImageHeight;

            if (xrows > ImageHeight)
            {
                xrows = ImageHeight;
            }

            if (ypos > 0)
            {
                xrows -= ypos;
            }

            int xcolumns = xpos + bm.ImageWidth;

            if (xcolumns > ImageWidth)
            {
                xcolumns = ImageWidth;
            }

            if (xpos > 0)
            {
                xcolumns -= xpos;
            }

            if ((xrows <= 0) || (xcolumns <= 0))
            {
                return;
            }

            // Precompute multiplier map
            int maxgray = bm.Grays - 1;
            int[] multiplier = GetMultiplier(maxgray);

            // Compute starting point
            int src = bm.RowOffset((ypos < 0) ? (-ypos) : 0) - ((xpos < 0) ? xpos : 0);
            int dst = RowOffset((ypos > 0) ? ypos : 0) + ((xpos > 0) ? xpos : 0);

            PixelReference dstPixel = CreateGPixelReference(0);

            // Loop over rows
            for (int y = 0; y < xrows; y++)
            {
                // Loop over columns
                dstPixel.SetOffset(dst);

                for (int x = 0; x < xcolumns; dstPixel.IncOffset())
                {
                    int srcpix = bm.GetByteAt(src + (x++));

                    // Perform pixel operation
                    if (srcpix > 0)
                    {
                        if (srcpix >= maxgray)
                        {
                            dstPixel.SetGray(0);
                        }
                        else
                        {
                            int level = multiplier[srcpix];
                            dstPixel.SetBGR((dstPixel.Blue * level) >> 16, (dstPixel.Green * level) >> 16,
                                            (dstPixel.Red * level) >> 16);
                        }
                    }
                }

                // Next line
                dst += GetRowSize();
                src += bm.GetRowSize();
            }
        }

        /// <summary> 
        /// Insert the specified bitmap with the specified color.
        /// </summary>
        /// <param name="bm">
        /// bitmap to insert
        /// </param>
        /// <param name="xpos">
        /// horizontal position
        /// </param>
        /// <param name="ypos">
        /// vertical position
        /// </param>
        /// <param name="color">
        /// color to insert bitmap with
        /// </param>
        public virtual void Blit(Bitmap bm, int xpos, int ypos, Pixel color)
        {
            // Check
            if (color == null)
            {
                return;
            }

            // Compute number of rows and columns
            int xrows = ypos + bm.ImageHeight;

            if (xrows > ImageHeight)
            {
                xrows = ImageHeight;
            }

            if (ypos > 0)
            {
                xrows -= ypos;
            }

            int xcolumns = xpos + bm.ImageWidth;

            if (xcolumns > ImageWidth)
            {
                xcolumns = ImageWidth;
            }

            if (xpos > 0)
            {
                xcolumns -= xpos;
            }

            if ((xrows <= 0) || (xcolumns <= 0))
            {
                return;
            }

            // Precompute multiplier map
            int maxgray = bm.Grays - 1;
            int[] multiplier = new int[maxgray];

            for (int i = 0; i < maxgray; i++)
            {
                multiplier[i] = 0x10000 - ((i << 16) / maxgray);
            }

            // Cache target color
            int gr = color.Red;
            int gg = color.Green;
            int gb = color.Blue;

            // Compute starting point
            int src = bm.RowOffset((ypos < 0) ? (-ypos) : 0) - ((xpos < 0) ? xpos : 0);
            int dst = ((ypos > 0) ? RowOffset(ypos) : 0) + ((xpos > 0) ? xpos : 0);

            PixelReference dstPixel = CreateGPixelReference(dst);

            // Loop over rows
            for (int y = 0; y < xrows; y++)
            {
                // Loop over columns
                dstPixel.SetOffset(dst);

                for (int x = 0; x < xcolumns; dstPixel.IncOffset())
                {
                    int srcpix = bm.GetByteAt(src + (x++));

                    // Perform pixel operation
                    if (srcpix != 0)
                    {
                        if (srcpix >= maxgray)
                        {
                            dstPixel.SetBGR(gb, gg, gr);
                        }
                        else
                        {
                            int level0 = multiplier[srcpix];
                            int level1 = 0x10000 - level0;
                            dstPixel.SetBGR(_clip[((dstPixel.Blue * level0) + (gb * level1)) >> 16],
                                            _clip[((dstPixel.Green * level0) + (gg * level1)) >> 16],
                                            _clip[((dstPixel.Red * level0) + (gr * level1)) >> 16]);
                        }
                    }
                }

                // Next line
                dst += GetRowSize();
                src += bm.GetRowSize();
            }
        }

        /// <summary> 
        /// Correct the colors with a gamma subsample normalized to 1.0 for no correction.
        /// </summary>
        /// <param name="gamma">
        /// Color correction
        /// </param>
        public void ApplyGammaCorrection(double gamma)
        {
            if (((gamma > 0.999D) && (gamma < 1.0009999999999999D)))
            {
                return;
            }

            int[] gtable = GetColorCorrection(gamma);

            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = (sbyte)gtable[Data[i]];
            }
        }

        /// <summary> 
        /// Fill this image from another source at reduced resolution.  Pixel
        /// averaging will be used.
        /// </summary>
        /// <param name="src">image map to reduce
        /// </param>
        /// <param name="subsample">rate to subsample
        /// </param>
        /// <param name="pdr">target bounds
        /// </param>
        public virtual void Downsample(Map src, int subsample, Rectangle pdr)
        {
            Rectangle rect = new Rectangle(0, 0, ((src.ImageWidth + subsample) - 1) / subsample,
                                   ((src.ImageHeight + subsample) - 1) / subsample);

            if (pdr != null)
            {
                if ((pdr.Right < rect.Right) || (pdr.Bottom < rect.Bottom) || (pdr.Left > rect.Left) || (pdr.Top > rect.Top))
                {
                    throw new ArgumentException("Specified rectangle overflows destination PixelMap");
                }
                rect = pdr;
            }

            Init(rect.Height, rect.Width, null);

            int sy = rect.Bottom * subsample;
            int sxz = rect.Right * subsample;
            int sidx = src.RowOffset(sy);
            int didx = 0;

            PixelReference sptr = src.CreateGPixelReference(0);
            PixelReference dptr = CreateGPixelReference(0);

            for (int y = 0; y < ImageHeight; y++)
            {
                int sx = sxz;

                for (int x = ImageWidth; x-- > 0; dptr.IncOffset())
                {
                    int r = 0;
                    int g = 0;
                    int b = 0;
                    int s = 0;
                    int kidx = sidx;
                    int lsy = sy + subsample;

                    if (lsy > src.ImageHeight)
                    {
                        lsy = src.ImageHeight;
                    }

                    int lsx = sx + subsample;

                    if (lsx > src.ImageWidth)
                    {
                        lsx = src.ImageWidth;
                    }

                    for (int rsy = sy; rsy < lsy; rsy++)
                    {
                        sptr.SetOffset(kidx + sx);
                        if (!IsRampNeeded)
                        {
                            for (int rsx = lsx - sx; rsx-- > 0; sptr.IncOffset())
                            {
                                r += sptr.Red;
                                g += sptr.Green;
                                b += sptr.Blue;
                                s++;
                            }
                        }
                        else
                        {
                            for (int rsx = lsx - sx; rsx-- > 0; sptr.IncOffset())
                            {
                                Pixel pix = src.PixelRamp(sptr);
                                r += pix.Red;
                                g += pix.Green;
                                b += pix.Blue;
                                s++;
                            }
                        }

                        kidx += src.GetRowSize();
                    }

                    if (s >= _invmap.Length)
                    {
                        dptr.SetBGR(b / s, g / s, r / s);
                    }
                    else
                    {
                        dptr.SetBGR(((b * _invmap[s]) + 32768) >> 16, ((g * _invmap[s]) + 32768) >> 16,
                                    ((r * _invmap[s]) + 32768) >> 16);
                    }

                    sx += subsample;
                }

                sy += subsample;
                sidx += src.RowOffset(subsample);
                dptr.SetOffset(didx += GetRowSize());
            }
        }

        /// <summary> Fill this image from another source at reduced resolution of 4 vertical
        /// pixels to 3.  An extrapolating pixel averaging algorithm is used.
        ///
        /// </summary>
        /// <param name="src">image map to reduce
        /// </param>
        /// <param name="pdr">target bounds
        ///
        /// </param>
        /// <throws>  IllegalArgumentException if the target rectangle is out of bounds </throws>
        public virtual void Downsample43(Map src, Rectangle pdr)
        {
            int srcwidth = src.ImageWidth;
            int srcheight = src.ImageHeight;
            //UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
            int destwidth = (int)Math.Ceiling(srcwidth * 0.75D);
            //UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
            int destheight = (int)Math.Ceiling(srcheight * 0.75D);
            Rectangle rect = new Rectangle(0, 0, destwidth, destheight);

            if (pdr != null)
            {
                if ((pdr.Right < rect.Right) || (pdr.Bottom < rect.Bottom) || (pdr.Left > rect.Left) || (pdr.Top > rect.Top))
                {
                    throw new ArgumentException("rectangle out of bounds" + "pdr=(" + pdr.Right + "," + pdr.Bottom + "," +
                                                pdr.Left + "," + pdr.Top + "),rect=(" + rect.Right + "," + rect.Bottom +
                                                "," + rect.Left + "," + rect.Top + ")");
                }

                rect = pdr;
                destwidth = rect.Width;
                destheight = rect.Height;
            }

            Init(destheight, destwidth, null);

            int sy = rect.Bottom / 3;
            int dy = rect.Bottom - (3 * sy);

            //    if(dy < 0)
            //    {
            //      sy--;
            //      dy += 3;
            //    }

            int sxz = rect.Right / 3;
            int dxz = rect.Right - (3 * sxz);

            if (dxz < 0)
            {
                sxz--;
                dxz += 3;
            }

            sxz *= 4;
            sy *= 4;

            PixelReference spix0 = src.CreateGPixelReference(0);
            PixelReference spix1 = src.CreateGPixelReference(0);
            PixelReference spix2 = src.CreateGPixelReference(0);
            PixelReference spix3 = src.CreateGPixelReference(0);
            PixelReference dpix0 = CreateGPixelReference(0);
            PixelReference dpix1 = CreateGPixelReference(0);
            PixelReference dpix2 = CreateGPixelReference(0);
            while (dy < destheight)
            {
                spix0.SetOffset(sy++, sxz);

                if (sy >= srcheight)
                {
                    sy--;
                }

                spix1.SetOffset(sy++, sxz);

                if (sy >= srcheight)
                {
                    sy--;
                }

                spix2.SetOffset(sy++, sxz);

                if (sy >= srcheight)
                {
                    sy--;
                }

                spix3.SetOffset(sy++, sxz);

                dpix0.SetOffset((dy < 0) ? 0 : dy, dxz);

                if (++dy >= destheight)
                {
                    dy--;
                }

                dpix1.SetOffset((dy < 0) ? 0 : dy, dxz);

                if (++dy >= destheight)
                {
                    dy--;
                }

                dpix2.SetOffset(dy++, dxz);
                int dx = dxz;
                int sx = sxz;

                Pixel pix0 = src.PixelRamp(spix0);
                Pixel pix1 = src.PixelRamp(spix1);
                Pixel pix2 = src.PixelRamp(spix2);
                Pixel pix3 = src.PixelRamp(spix3);
                while (dx < destwidth)
                {
                    int s00b = pix0.Blue;
                    int s00g = pix0.Green;
                    int s00r = pix0.Red;
                    int s01b = pix1.Blue;
                    int s01g = pix1.Green;
                    int s01r = pix1.Red;
                    int s02b = pix2.Blue;
                    int s02g = pix2.Green;
                    int s02r = pix2.Red;
                    int s03b = pix3.Blue;
                    int s03g = pix3.Green;
                    int s03r = pix3.Red;

                    if (++sx < srcwidth)
                    {
                        spix0.IncOffset();
                        spix1.IncOffset();
                        spix2.IncOffset();
                        spix3.IncOffset();
                        pix0 = src.PixelRamp(spix0);
                        pix1 = src.PixelRamp(spix1);
                        pix2 = src.PixelRamp(spix2);
                        pix3 = src.PixelRamp(spix3);
                    }

                    int s10b = pix0.Blue;
                    int s10g = pix0.Green;
                    int s10r = pix0.Red;
                    int s11b = pix1.Blue;
                    int s11g = pix1.Green;
                    int s11r = pix1.Red;
                    int s12b = pix2.Blue;
                    int s12g = pix2.Green;
                    int s12r = pix2.Red;
                    int s13b = pix3.Blue;
                    int s13g = pix3.Green;
                    int s13r = pix3.Red;

                    if (++sx < srcwidth)
                    {
                        spix0.IncOffset();
                        spix1.IncOffset();
                        spix2.IncOffset();
                        spix3.IncOffset();
                        pix0 = src.PixelRamp(spix0);
                        pix1 = src.PixelRamp(spix1);
                        pix2 = src.PixelRamp(spix2);
                        pix3 = src.PixelRamp(spix3);
                    }

                    int s20b = pix0.Blue;
                    int s20g = pix0.Green;
                    int s20r = pix0.Red;
                    int s21b = pix1.Blue;
                    int s21g = pix1.Green;
                    int s21r = pix1.Red;
                    int s22b = pix2.Blue;
                    int s22g = pix2.Green;
                    int s22r = pix2.Red;
                    int s23b = pix3.Blue;
                    int s23g = pix3.Green;
                    int s23r = pix3.Red;

                    if (++sx < srcwidth)
                    {
                        spix0.IncOffset();
                        spix1.IncOffset();
                        spix2.IncOffset();
                        spix3.IncOffset();
                        pix0 = src.PixelRamp(spix0);
                        pix1 = src.PixelRamp(spix1);
                        pix2 = src.PixelRamp(spix2);
                        pix3 = src.PixelRamp(spix3);
                    }

                    int s30b = pix0.Blue;
                    int s30g = pix0.Green;
                    int s30r = pix0.Red;
                    int s31b = pix1.Blue;
                    int s31g = pix1.Green;
                    int s31r = pix1.Red;
                    int s32b = pix2.Blue;
                    int s32g = pix2.Green;
                    int s32r = pix2.Red;
                    int s33b = pix3.Blue;
                    int s33g = pix3.Green;
                    int s33r = pix3.Red;

                    if (++sx < srcwidth)
                    {
                        spix0.IncOffset();
                        spix1.IncOffset();
                        spix2.IncOffset();
                        spix3.IncOffset();
                        pix0 = src.PixelRamp(spix0);
                        pix1 = src.PixelRamp(spix1);
                        pix2 = src.PixelRamp(spix2);
                        pix3 = src.PixelRamp(spix3);
                    }

                    dpix0.Blue = (sbyte)(((11 * s00b) + (2 * (s01b + s10b)) + s11b + 8) >> 4);
                    dpix0.Green = (sbyte)(((11 * s00g) + (2 * (s01g + s10g)) + s11g + 8) >> 4);
                    dpix0.Red = (sbyte)(((11 * s00r) + (2 * (s01r + s10r)) + s11r + 8) >> 4);
                    dpix1.Blue = (sbyte)(((7 * (s01b + s02b)) + s11b + s12b + 8) >> 4);
                    dpix1.Green = (sbyte)(((7 * (s01g + s02g)) + s11g + s12g + 8) >> 4);
                    dpix1.Red = (sbyte)(((7 * (s01r + s02r)) + s11r + s12r + 8) >> 4);
                    dpix2.Blue = (sbyte)(((11 * s03b) + (2 * (s02b + s13b)) + s12b + 8) >> 4);
                    dpix2.Green = (sbyte)(((11 * s03g) + (2 * (s02g + s13g)) + s12g + 8) >> 4);
                    dpix2.Red = (sbyte)(((11 * s03r) + (2 * (s02r + s13r)) + s12r + 8) >> 4);

                    if (++dx < destwidth)
                    {
                        dpix0.IncOffset();
                        dpix1.IncOffset();
                        dpix2.IncOffset();
                    }

                    dpix0.Blue = (sbyte)(((7 * (s10b + s20b)) + s11b + s21b + 8) >> 4);
                    dpix0.Green = (sbyte)(((7 * (s10g + s20g)) + s11g + s21g + 8) >> 4);
                    dpix0.Red = (sbyte)(((7 * (s10r + s20r)) + s11r + s21r + 8) >> 4);
                    dpix1.Blue = (sbyte)((s12b + s22b + s11b + s21b + 2) >> 2);
                    dpix1.Green = (sbyte)((s12g + s22g + s11g + s21g + 2) >> 2);
                    dpix1.Red = (sbyte)((s12r + s22r + s11r + s21r + 2) >> 2);
                    dpix2.Blue = (sbyte)(((7 * (s13b + s23b)) + s12b + s22b + 8) >> 4);
                    dpix2.Green = (sbyte)(((7 * (s13g + s23g)) + s12g + s22g + 8) >> 4);
                    dpix2.Red = (sbyte)(((7 * (s13r + s23r)) + s12r + s22r + 8) >> 4);

                    if (++dx < destwidth)
                    {
                        dpix0.IncOffset();
                        dpix1.IncOffset();
                        dpix2.IncOffset();
                    }

                    dpix0.Blue = (sbyte)(((11 * s30b) + (2 * (s31b + s20b)) + s21b + 8) >> 4);
                    dpix0.Green = (sbyte)(((11 * s30g) + (2 * (s31g + s20g)) + s21g + 8) >> 4);
                    dpix0.Red = (sbyte)(((11 * s30r) + (2 * (s31r + s20r)) + s21r + 8) >> 4);
                    dpix1.Blue = (sbyte)(((7 * (s31b + s32b)) + s21b + s22b + 8) >> 4);
                    dpix1.Green = (sbyte)(((7 * (s31g + s32g)) + s21g + s22g + 8) >> 4);
                    dpix1.Red = (sbyte)(((7 * (s31r + s32r)) + s21r + s22r + 8) >> 4);
                    dpix2.Blue = (sbyte)(((11 * s33b) + (2 * (s32b + s23b)) + s22b + 8) >> 4);
                    dpix2.Green = (sbyte)(((11 * s33g) + (2 * (s32g + s23g)) + s22g + 8) >> 4);
                    dpix2.Red = (sbyte)(((11 * s33r) + (2 * (s32r + s23r)) + s22r + 8) >> 4);

                    if (++dx < destwidth)
                    {
                        dpix0.IncOffset();
                        dpix1.IncOffset();
                        dpix2.IncOffset();
                    }
                }
            }
        }

        /// <summary> Insert the reference map at the specified location.
        ///
        /// </summary>
        /// <param name="ref">map to insert
        /// </param>
        /// <param name="dx">horizontal position to insert at
        /// </param>
        /// <param name="dy">vertical position to insert at
        /// </param>
        public override void Fill(Map ref_Renamed, int dx, int dy)
        {
            int x0 = (dx > 0) ? dx : 0;
            int y0 = (dy > 0) ? dy : 0;
            int x1 = (dx < 0) ? (-dx) : 0;
            int y1 = (dy < 0) ? (-dy) : 0;
            int w0 = ImageWidth - x0;
            int w1 = ref_Renamed.ImageWidth - x1;
            int w = (w0 < w1) ? w0 : w1;
            int h0 = ImageHeight - y0;
            int h1 = ref_Renamed.ImageHeight - y1;
            int h = (h0 < h1) ? h0 : h1;

            if ((w > 0) && (h > 0))
            {
                PixelReference pixel = CreateGPixelReference(0);
                PixelReference refPixel = ref_Renamed.CreateGPixelReference(0);

                do
                {
                    pixel.SetOffset(y0++, x0);
                    refPixel.SetOffset(y1++, x1);

                    if (!IsRampNeeded)
                    {
                        pixel.SetPixels(refPixel, w);
                    }
                    else
                    {
                        int i = w;
                        do
                        {
                            pixel.CopyFrom(ref_Renamed.PixelRamp(refPixel));
                            pixel.IncOffset();
                            refPixel.IncOffset();
                        } while (--i > 0);
                    }
                } while (--h > 0);
            }
        }

        /// <summary> Initialize this PixelMap to the specified size and fill in the specified color.
        ///
        /// </summary>
        /// <param name="arows">number of rows
        /// </param>
        /// <param name="acolumns">number of columns
        /// </param>
        /// <param name="filler">fill color
        ///
        /// </param>
        /// <returns> the initialized PixelMap
        /// </returns>
        public virtual PixelMap Init(int arows, int acolumns, Pixel filler)
        {
            //    boolean needFill=false;
            if ((arows != ImageHeight) || (acolumns != ImageWidth))
            {
                Data = null;
                ImageHeight = arows;
                ImageWidth = acolumns;
            }

            int npix = RowOffset(ImageHeight);

            if (npix > 0)
            {
                if (Data == null)
                {
                    Data = new sbyte[npix * 3];
                    //        needFill=true;
                }

                if (filler != null)
                {
                    sbyte b = filler.Blue;
                    sbyte g = filler.Green;
                    sbyte r = filler.Red;

                    for (int i = 0; i < Data.Length; )
                    {
                        Data[i++] = b;
                        Data[i++] = g;
                        Data[i++] = r;
                    }
                }
                //      else if(needFill)
                //      {
                //        for(int i = 0; i < data.length;)
                //        {
                //          data[i++] = -51;
                //        }
                //      }
            }

            return this;
        }

        /// <summary> Initialize this PixelMap from a segment of another image map.
        ///
        /// </summary>
        /// <param name="ref">image map to initialize from
        /// </param>
        /// <param name="rect">bounding rectangle to initialize from
        ///
        /// </param>
        /// <returns> the initialized PixelMap
        /// </returns>
        public virtual PixelMap Init(Map ref_Renamed, Rectangle rect)
        {
            Init(rect.Height, rect.Width, ((null)));

            Rectangle rect2 = new Rectangle(0, 0, ref_Renamed.ImageWidth, ref_Renamed.ImageHeight);
            rect2.Intersect(rect2, rect);
            rect2.Translate(-rect.Right, -rect.Bottom);

            if (!rect2.Empty)
            {
                PixelReference pixel = CreateGPixelReference(0);
                PixelReference refPixel = ref_Renamed.CreateGPixelReference(0);

                for (int y = rect2.Bottom; y < rect2.Top; y++)
                {
                    pixel.SetOffset(y, rect2.Right);
                    refPixel.SetOffset(y + rect.Bottom, rect.Right + rect2.Right);

                    if (!IsRampNeeded)
                    {
                        for (int x = rect2.Left - rect2.Right; x-- > 0; pixel.IncOffset(), refPixel.IncOffset())
                        {
                            pixel.CopyFrom(refPixel);
                        }
                    }
                    else
                    {
                        for (int x = rect2.Left - rect2.Right; x-- > 0; pixel.IncOffset(), refPixel.IncOffset())
                        {
                            pixel.CopyFrom(ref_Renamed.PixelRamp(refPixel));
                        }
                    }
                }
            }

            return this;
        }

        /// <summary> Initialize this PixelMap from another image map.
        ///
        /// </summary>
        /// <param name="ref">image map to initialize from
        ///
        /// </param>
        /// <returns> the initialized PixelMap
        /// </returns>
        public virtual PixelMap Init(Map ref_Renamed)
        {
            Init(ref_Renamed.ImageHeight, ref_Renamed.ImageWidth, ((null)));

            PixelReference pixel = CreateGPixelReference(0);

            if ((ImageHeight > 0) && (ImageWidth > 0))
            {
                PixelReference refPixel = (ref_Renamed).CreateGPixelReference(0);

                for (int y = 0; y < ImageHeight; y++)
                {
                    pixel.SetOffset(y, 0);
                    refPixel.SetOffset(y, 0);

                    if (!IsRampNeeded)
                    {
                        for (int x = ImageWidth; x-- > 0; pixel.IncOffset(), refPixel.IncOffset())
                        {
                            pixel.CopyFrom(refPixel);
                        }
                    }
                    else
                    {
                        for (int x = ImageWidth; x-- > 0; pixel.IncOffset(), refPixel.IncOffset())
                        {
                            pixel.CopyFrom(ref_Renamed.PixelRamp(refPixel));
                        }
                    }
                }
            }

            return this;
        }

        /// <summary> 
        /// Draw the foreground layer onto this background image.
        /// </summary>
        /// <param name="mask">
        /// the mask layer
        /// </param>
        /// <param name="foregroundMap">
        /// the foreground colors
        /// </param>
        /// <param name="supersample">
        /// rate to upsample the foreground colors
        /// </param>
        /// <param name="subsample">
        /// rate to subsample the foreground colors
        /// </param>
        /// <param name="bounds">
        /// the target rectangle
        /// </param>
        /// <param name="gamma">
        /// color correction factor
        /// </param>
        /// <throws>  
        /// IllegalArgumentException if the specified bounds are not contained in the page 
        /// </throws>
        public unsafe virtual void Stencil(Bitmap mask, PixelMap foregroundMap, int supersample, int subsample, Rectangle bounds,
                                    double gamma)
        {
            // Check arguments
            Rectangle rect = new Rectangle(0, 0, (foregroundMap.ImageWidth * supersample + subsample - 1) / subsample,
                                   (foregroundMap.ImageHeight * supersample + subsample - 1) / subsample);

            if (bounds != null)
            {
                if ((bounds.Right < rect.Right) || (bounds.Bottom < rect.Bottom) || (bounds.Left > rect.Left) ||
                    (bounds.Top > rect.Top))
                {
                    throw new ArgumentException("rectangle out of bounds" + "bounds=(" + bounds.Right + "," + bounds.Bottom +
                                                "," + bounds.Left + "," + bounds.Top + "),rect=(" + rect.Right + "," +
                                                rect.Bottom + "," + rect.Left + "," + rect.Top + ")");
                }

                rect = bounds;
            }

            // Compute number of rows
            int xrows = ImageHeight;

            if (mask.ImageHeight < xrows)
            {
                xrows = mask.ImageHeight;
            }

            if (rect.Height < xrows)
            {
                xrows = rect.Height;
            }

            // Compute number of columns
            int xcolumns = ImageWidth;

            if (mask.ImageWidth < xcolumns)
            {
                xcolumns = mask.ImageWidth;
            }

            if (rect.Width < xcolumns)
            {
                xcolumns = rect.Width;
            }

            // Precompute multiplier map
            int maxgray = mask.Grays - 1;
            int[] multiplier = new int[maxgray];

            for (int i = 1; i < maxgray; i++)
            {
                multiplier[i] = (0x10000 * i) / maxgray;
            }

            // Prepare color correction table
            int[] gtable = GetColorCorrection(gamma);

            double ratioFg = supersample / (double)subsample;
            // Compute starting point in blown up foreground PixelMap
            int fgy = (rect.Bottom * subsample) / supersample;
            double fgy1 = rect.Bottom - ratioFg * fgy;

            if (fgy1 < 0)
            {
                fgy--;
                fgy1 += ratioFg;
            }

            int fgxz = (rect.Right * subsample) / supersample;
            double fgx1z = rect.Right - ratioFg * fgxz;

            if (fgx1z < 0)
            {
                fgxz--;
                fgx1z += ratioFg;
            }

            int fg = foregroundMap.RowOffset(fgy);
            PixelReference fgx = foregroundMap.CreateGPixelReference(0);
            PixelReference dst = CreateGPixelReference(0);

            // Loop over rows            
            for (int y = 0; y < xrows; y++)
            {
                // Loop over columns
                fgx.SetOffset(fg + fgxz);

                double fgx1 = fgx1z;
                dst.SetOffset(y, 0);

                int src = mask.RowOffset(y);

                for (int x = 0; x < xcolumns; x++, dst.IncOffset())
                {
                    int srcpix = mask.GetByteAt(src + x);

                    // Perform pixel operation
                    if (srcpix > 0)
                    {
                        fixed (int* gTableLocation = gtable)
                        {
                            if (srcpix >= maxgray)
                            {
                                dst.SetBGR(gTableLocation[fgx.Blue], gTableLocation[fgx.Green],
                                           gTableLocation[fgx.Red]);
                            }
                            else
                            {
                                int level = multiplier[srcpix];
                                dst.SetBGR(((dst.Blue * (0x10000 - level)) + (level * gTableLocation[fgx.Blue])) >> 16,
                                           ((dst.Green * (0x10000 - level)) + (level * gTableLocation[fgx.Green])) >> 16,
                                           ((dst.Red * (0x10000 - level)) + (level * gTableLocation[fgx.Red])) >> 16);
                            }
                        }
                    }

                    // Next column
                    if (++fgx1 >= ratioFg)
                    {
                        fgx1 -= ratioFg;
                        fgx.IncOffset();
                    }
                }

                // Next line
                if (++fgy1 >= ratioFg)
                {
                    fgy1 -= ratioFg;
                    fg += foregroundMap.GetRowSize();
                }
            }
        }

        /// <summary> 
        /// Copy this image with a translated origin.
        /// </summary>
        /// <param name="dx">
        /// horizontal distance to translate
        /// </param>
        /// <param name="dy">
        /// vertical distance to translate
        /// </param>
        /// <param name="retval">
        /// an old image to try and reuse for the return value
        /// </param>
        /// <returns> the translated image
        /// </returns>
        public override Map Translate(int dx, int dy, Map retval)
        {
            if (!(retval is PixelMap) || (retval.ImageWidth != ImageWidth) || (retval.ImageHeight != ImageHeight))
            {
                retval = new PixelMap().Init(ImageHeight, ImageWidth, null);
            }

            retval.Fill(this, -dx, -dy);

            return retval;
        }

        /// <summary> 
        /// Initialize this PixelMap with a preallocated buffer.
        /// </summary>
        /// <param name="data">
        /// buffer to use
        /// </param>
        /// <param name="arows">
        /// number of rows
        /// </param>
        /// <param name="acolumns">
        /// number of columns
        /// </param>
        /// <returns> the initialized PixelMap
        /// </returns>
        public virtual PixelMap Init(sbyte[] data, int arows, int acolumns)
        {
            ImageHeight = arows;
            ImageWidth = acolumns;
            this.Data = data;

            return this;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary> Creates or retrieves a cached multiplier array to use when attenuating.
        ///
        /// </summary>
        /// <returns> attenuation array
        /// </returns>
        protected static int[] GetMultiplier(int maxgray)
        {
            int[] retval = (int[])_multiplierRefArray[maxgray];
            if (retval == null)
            {
                retval = new int[maxgray];

                for (int i = 0; i < maxgray; i++)
                {
                    retval[i] = 0x10000 - ((i << 16) / maxgray);
                }
                _multiplierRefArray[maxgray] = retval;
            }
            return retval;
        }

        #endregion Protected Methods
    }
}