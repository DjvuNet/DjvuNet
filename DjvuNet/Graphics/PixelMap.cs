using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DjvuNet.Errors;

namespace DjvuNet.Graphics
{
    /// <summary>
    /// This class represents 24 bit color image maps.
    /// </summary>
    public sealed class PixelMap : Map, IPixelMap
    {
        #region Private Members

        /// <summary>
        /// Used to represent division as multiplication.
        /// </summary>
        private static readonly int[] _invmap = new int[256];

        /// <summary>
        /// Identity color correction table.
        /// </summary>
        internal static readonly int[] IdentityGammaCorr = new int[256];

        /// <summary>
        /// Cached color correction table.
        /// </summary>
        internal static int[] CachedGammaTable = new int[256];

        /// <summary>
        /// The color correction subsample for the cached color table.
        /// </summary>
        internal static double CachedGamma = -1D;

        /// <summary>
        /// Used to quickly clip out of bounds values.
        /// </summary>
        private static readonly sbyte[] _clip = new sbyte[512];

        /// <summary>
        /// Used for attenuation
        /// </summary>
        private static readonly Object[] _multiplierRefArray = new Object[256];

        private static object _syncObject = new object();

        #endregion Private Members

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

            for (int i = 0; i < IdentityGammaCorr.Length; i++)
            {
                IdentityGammaCorr[i] = i;
            }
        }

        /// <summary> Creates a new PixelMap object.</summary>
        public PixelMap()
            : base(3, 2, 1, 0, false)
        {
        }

        public PixelMap(sbyte[] data, int width, int height)
            : base(3, 2, 1, 0, false)
        {
            Data = data;
            Width = width;
            Height = height;
        }

        #endregion Constructors

        #region Public Methods

        public IPixel GetPixelAt(int row, int column)
        {
            return CreateGPixelReference(row, column).ToPixel();
        }

        /// <summary>
        /// Fill the array with color correction constants.
        /// </summary>
        /// <param name="gamma">
        /// Color correction subsample
        /// </param>
        /// <returns>
        /// The new color correction table
        /// </returns>
#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public static int[] GetGammaCorrection(double gamma)
        {
            lock (_syncObject)
            {
                if ((gamma < 0.10000000000000001D) || (gamma > 10D))
                {
                    throw new DjvuArgumentOutOfRangeException(nameof(gamma), $"Gamma out of range: {gamma}");
                }

                int[] retval;
                if ((gamma < 1.0009999999999999D) && (gamma > 0.999D))
                {
                    retval = IdentityGammaCorr;
                }
                else
                {
                    if (!(gamma > (CachedGamma - 0.000000001000000001D) && gamma < (CachedGamma + 0.000000001000000001D)))
                    {
                        CachedGammaTable = new int[256];
                        for (int i = 0; i < CachedGammaTable.Length; i++)
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

                            CachedGammaTable[i] = (int)Math.Floor((255D * x) + 0.5D);
                        }
                        CachedGamma = gamma;
                    }
                    retval = CachedGammaTable;
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
#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public void Attenuate(IBitmap bm, int xpos, int ypos)
        {
            // Check
            // Compute number of rows and columns
            int xrows = ypos + bm.Height;

            if (xrows > Height)
            {
                xrows = Height;
            }

            if (ypos > 0)
            {
                xrows -= ypos;
            }

            int xcolumns = xpos + bm.Width;

            if (xcolumns > Width)
            {
                xcolumns = Width;
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

            IPixelReference dstPixel = CreateGPixelReference(0);

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
#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public void Blit(IBitmap bm, int xpos, int ypos, IPixel color)
        {
            // Check
            if (color == null)
            {
                return;
            }

            // Compute number of rows and columns
            int xrows = ypos + bm.Height;

            if (xrows > Height)
            {
                xrows = Height;
            }

            if (ypos > 0)
            {
                xrows -= ypos;
            }

            int xcolumns = xpos + bm.Width;

            if (xcolumns > Width)
            {
                xcolumns = Width;
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

            IPixelReference dstPixel = CreateGPixelReference(dst);

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
                            dstPixel.SetBGR(_clip[unchecked((byte)((dstPixel.Blue * level0) + (gb * level1)) >> 16)],
                                            _clip[unchecked((byte)((dstPixel.Green * level0) + (gg * level1)) >> 16)],
                                            _clip[unchecked((byte)((dstPixel.Red * level0) + (gr * level1)) >> 16)]);
                        }
                    }
                }

                // Next line
                dst += GetRowSize();
                src += bm.GetRowSize();
            }
        }

        /// <summary>
        /// Correct the colors of the <see cref="DjvuNet.Graphics.PixelMap"/> instance with a gamma subsample normalized to 1.0 for no correction.
        /// </summary>
        /// <param name="gamma">
        /// Color gamma correction
        /// </param>
        public void ApplyGammaCorrection(double gamma)
        {
            ApplyGamma(gamma, Data);
        }

        /// <summary>
        /// Apply gamma correction to passed image data.
        /// </summary>
        /// <param name="gamma"></param>
        /// <param name="data"></param>
        public static void ApplyGamma(double gamma, sbyte[] data)
        {
            if ((gamma > 0.999D) && (gamma < 1.0009999999999999D))
            {
                return;
            }

            int[] gtable = GetGammaCorrection(gamma);

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (sbyte)gtable[unchecked((byte)data[i])];
            }
        }

#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public static unsafe void ApplyGammaFastMT(double gamma, sbyte[] data)
        {
            if ((gamma > 0.999D) && (gamma < 1.0009999999999999D))
            {
                return;
            }

            int[] gtable = GetGammaCorrection(gamma);

            GCHandle hData = default(GCHandle);
            GCHandle hGTable = default(GCHandle);

            hData = GCHandle.Alloc(data, GCHandleType.Pinned);
            hGTable = GCHandle.Alloc(gtable, GCHandleType.Pinned);
            byte* pData = (byte*)hData.AddrOfPinnedObject();
            int* gammaLUT = (int*)hGTable.AddrOfPinnedObject();
            int dataLength = data.Length;
            int reminderLength = data.Length % 48;

            // Parallel.For on 4 cores is 70% slower than single threaded
            // on one core - extremely inefficient loop call design
            int prllReminder = 0;
            int procCount = Environment.ProcessorCount;
            int part = Math.DivRem(dataLength, procCount, out prllReminder);

            TaskFactory tskFactory = new TaskFactory();
            List<Task> tasks = new List<Task>();
            for (int k = 0; k < procCount; k++)
            {
                tasks.Add(null);
            }

            for (int k = 0; k < procCount; k++)
            {
                int index = part * k;
                tasks[k] = tskFactory.StartNew(() =>
                {
                    int i = index;
                    int partEnd = index + part - 48;
                    if ((k + 1) == procCount)
                    {
                        partEnd += prllReminder;
                    }

                    for (; i < partEnd; i++)
                    {
                        pData[i] = (byte)gammaLUT[pData[i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];

                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];

                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];

                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];

                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];

                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                        pData[i] = (byte)gammaLUT[pData[++i]];
                    }
                });
            }

            Task.WaitAll(tasks.ToArray());

            if (hData.IsAllocated)
            {
                hData.Free();
            }

            if (hGTable.IsAllocated)
            {
                hGTable.Free();
            }
        }

#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public static unsafe void ApplyGammaFastST(double gamma, sbyte[] data)
        {
            if ((gamma > 0.999D) && (gamma < 1.0009999999999999D))
            {
                return;
            }

            int[] gtable = GetGammaCorrection(gamma);
            GCHandle hData = default(GCHandle);
            GCHandle hGTable = default(GCHandle);

            hData = GCHandle.Alloc(data, GCHandleType.Pinned);
            hGTable = GCHandle.Alloc(gtable, GCHandleType.Pinned);
            byte* pData = (byte*)hData.AddrOfPinnedObject();
            int* gammaLUT = (int*)hGTable.AddrOfPinnedObject();

            int dataLength = data.Length;
            int reminderLength = data.Length % 48;
            dataLength -= reminderLength;

            for (int i = 0; i < dataLength; i++)
            {
                pData[i] = (byte)gammaLUT[pData[i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];

                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];

                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];

                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];

                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];

                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
            }

            for (int i = dataLength; i < dataLength + reminderLength; i++)
            {
                pData[i] = (byte)gammaLUT[pData[i]];
            }
            if (hData.IsAllocated)
            {
                hData.Free();
            }

            if (hGTable.IsAllocated)
            {
                hGTable.Free();
            }
        }

#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public static unsafe void ApllyGamma(byte* pData, int dataLengthRem, int dataLength, int* gammaLUT)
        {
            for (int i = 0; i < dataLength; i++)
            {
                pData[i] = (byte)gammaLUT[pData[i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];

                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];

                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];

                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];

                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];

                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
                pData[i] = (byte)gammaLUT[pData[++i]];
            }

            for (int i = dataLength; i < dataLengthRem; i++)
            {
                pData[i] = (byte)gammaLUT[pData[i]];
            }
        }

        public Rectangle BoundingRectangle
        {
            get
            {
                return new Rectangle
                {
                    Left = 0,
                    Bottom = 0,
                    Right = Width,
                    Top = Height
                };
            }
        }

        /// <summary>
        /// Fill this image from another source at reduced resolution.  Pixel
        /// averaging will be used.
        /// </summary>
        /// <param name="src">
        /// Image source to reduce
        /// </param>
        /// <param name="subsample">
        /// Subsample value
        /// </param>
        /// <param name="targetRect">
        /// Target bounds
        /// </param>
#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public void Downsample(IMap2 src, int subsample, Rectangle targetRect)
        {
            Utilities.Verify.SubsampleRange(subsample);

            if (src == this && ((targetRect == null || targetRect == BoundingRectangle) && subsample == 1))
            {
                return;
            }

            Rectangle rect = BoundingRectangle;

            if (targetRect != null)
            {
                if ((targetRect.Right < rect.Right) || (targetRect.Bottom < rect.Bottom) ||
                    (targetRect.Left > rect.Left) || (targetRect.Top > rect.Top))
                {
                    throw new DjvuArgumentOutOfRangeException(nameof(targetRect),
                        $"Specified rectangle overflows destination PixelMap {nameof(BoundingRectangle)}");
                }

                rect = targetRect;
            }
            else
            {
                rect = new Rectangle(0, 0, ((src.Width + subsample) - 1) / subsample,
                                      ((src.Height + subsample) - 1) / subsample);
            }

            Init(rect.Height, rect.Width, null);

            int sy = rect.Bottom * subsample;
            int sxz = rect.Right * subsample;
            int sidx = src.RowOffset(sy);
            int didx = 0;

            IPixelReference sptr = src.CreateGPixelReference(0);
            IPixelReference dptr = CreateGPixelReference(0);

            for (int y = 0; y < Height; y++)
            {
                int sx = sxz;

                for (int x = Width; x-- > 0; dptr.IncOffset())
                {
                    int r = 0;
                    int g = 0;
                    int b = 0;
                    int s = 0;
                    int kidx = sidx;
                    int lsy = sy + subsample;

                    if (lsy > src.Height)
                    {
                        lsy = src.Height;
                    }

                    int lsx = sx + subsample;

                    if (lsx > src.Width)
                    {
                        lsx = src.Width;
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
                                IPixel pix = src.PixelRamp(sptr);
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

        /// <summary>
        /// Fill this image from another source at reduced resolution of 4 vertical
        /// pixels to 3.  An extrapolating pixel averaging algorithm is used.
        /// </summary>
        /// <param name="src">
        /// Image map to reduce
        /// </param>
        /// <param name="targetRect">
        /// Target bounds
        /// </param>
        /// <throws>
        /// <see cref="DjvuNet.Errors.DjvuArgumentOutOfRangeException"/> if the target rectangle is out of bounds
        /// </throws>
#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public void Downsample43(IMap2 src, Rectangle targetRect)
        {
            int srcwidth = src.Width;
            int srcheight = src.Height;
            int destwidth = (int)Math.Ceiling(srcwidth * 0.75D);
            int destheight = (int)Math.Ceiling(srcheight * 0.75D);
            Rectangle rect = new Rectangle(0, 0, destwidth, destheight);

            if (targetRect != null)
            {
                if ((targetRect.Right < rect.Right) || (targetRect.Bottom < rect.Bottom) || (targetRect.Left > rect.Left) || (targetRect.Top > rect.Top))
                {
                    throw new DjvuArgumentOutOfRangeException(nameof(targetRect), "Rectangle out of bounds" + "pdr=(" + targetRect.Right + "," + targetRect.Bottom + "," +
                                                targetRect.Left + "," + targetRect.Top + "),rect=(" + rect.Right + "," + rect.Bottom +
                                                "," + rect.Left + "," + rect.Top + ")");
                }

                rect = targetRect;
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

            if (dxz < 0)   // always false what can be trivially derived from proceeding 2 loc
            {
                sxz--;
                dxz += 3;
            }

            sxz *= 4;
            sy *= 4;

            var spix0 = src.CreateGPixelReference(0);
            var spix1 = src.CreateGPixelReference(0);
            var spix2 = src.CreateGPixelReference(0);
            var spix3 = src.CreateGPixelReference(0);
            var dpix0 = CreateGPixelReference(0);
            var dpix1 = CreateGPixelReference(0);
            var dpix2 = CreateGPixelReference(0);
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

                var pix0 = src.PixelRamp(spix0);
                var pix1 = src.PixelRamp(spix1);
                var pix2 = src.PixelRamp(spix2);
                var pix3 = src.PixelRamp(spix3);
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

        /// <summary>
        /// Insert the reference map at the specified location.
        /// </summary>
        /// <param name="source">
        /// Map to insert
        /// </param>
        /// <param name="dx">
        /// Horizontal position to insert at
        /// </param>
        /// <param name="dy">
        /// Vertical position to insert at
        /// </param>
        public void Fill(IMap2 source, int dx, int dy)
        {
            int x0 = (dx > 0) ? dx : 0;
            int y0 = (dy > 0) ? dy : 0;
            int x1 = (dx < 0) ? (-dx) : 0;
            int y1 = (dy < 0) ? (-dy) : 0;
            int w0 = Width - x0;
            int w1 = source.Width - x1;
            int w = (w0 < w1) ? w0 : w1;
            int h0 = Height - y0;
            int h1 = source.Height - y1;
            int h = (h0 < h1) ? h0 : h1;

            if ((w > 0) && (h > 0))
            {
                var pixel = CreateGPixelReference(0);
                var refPixel = source.CreateGPixelReference(0);

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
                            pixel.CopyFrom(source.PixelRamp(refPixel));
                            pixel.IncOffset();
                            refPixel.IncOffset();
                        } while (--i > 0);
                    }
                } while (--h > 0);
            }
        }

        /// <summary>
        /// Initialize this PixelMap to the specified size and fill in the specified color.
        /// </summary>
        /// <param name="height">
        /// Number of rows
        /// </param>
        /// <param name="width">
        /// Number of columns
        /// </param>
        /// <param name="color">
        /// Fill color
        /// </param>
        /// <returns>
        /// The initialized PixelMap
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PixelMap Init(int height, int width, IPixel color)
        {
            if ((height != Height) || (width != Width))
            {
                Data = null;
                Height = height;
                Width = width;
            }

            int npix = RowOffset(Height);

            if (npix > 0)
            {
                if (Data == null)
                {
                    Data = new sbyte[npix * 3];
                }

                if (color != null && (color.Blue != 0 || color.Green != 0 || color.Red != 0 ))
                {
                    sbyte b = color.Blue;
                    sbyte g = color.Green;
                    sbyte r = color.Red;
                    unsafe
                    {
                        fixed (sbyte* pdata = Data)
                        {
                            for (int i = 0; i < Data.Length;)
                            {
                                pdata[i++] = b;
                                pdata[i++] = g;
                                pdata[i++] = r;
                            }
                        }
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Initialize this PixelMap from a segment of another image map.
        /// </summary>
        /// <param name="source">
        /// Image map to initialize from
        /// </param>
        /// <param name="rect">
        /// Bounding rectangle to initialize from
        /// </param>
        /// <returns>
        /// The initialized PixelMap
        /// </returns>
        public PixelMap Init(IMap2 source, Rectangle rect)
        {
            Init(rect.Height, rect.Width, null);

            Rectangle rect2 = new Rectangle(0, 0, source.Width, source.Height);
            rect2.Intersect(rect2, rect);
            rect2.Translate(-rect.Right, -rect.Bottom);

            if (!rect2.Empty)
            {
                var pixel = CreateGPixelReference(0);
                var refPixel = source.CreateGPixelReference(0);

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
                            pixel.CopyFrom(source.PixelRamp(refPixel));
                        }
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Initialize this PixelMap from another image map.
        /// </summary>
        /// <param name="source">
        /// Image map to initialize from
        /// </param>
        /// <returns>
        /// The initialized PixelMap
        /// </returns>
        public PixelMap Init(IMap2 source)
        {
            Init(source.Height, source.Width, null);

            var pixel = CreateGPixelReference(0);

            if ((Height > 0) && (Width > 0))
            {
                var refPixel = (source).CreateGPixelReference(0);

                for (int y = 0; y < Height; y++)
                {
                    pixel.SetOffset(y, 0);
                    refPixel.SetOffset(y, 0);

                    if (!IsRampNeeded)
                    {
                        for (int x = Width; x-- > 0; pixel.IncOffset(), refPixel.IncOffset())
                        {
                            pixel.CopyFrom(refPixel);
                        }
                    }
                    else
                    {
                        for (int x = Width; x-- > 0; pixel.IncOffset(), refPixel.IncOffset())
                        {
                            pixel.CopyFrom(source.PixelRamp(refPixel));
                        }
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Query the getRowSize.
        /// </summary>
        /// <returns>
        /// Row size in bytes.
        /// </returns>
        public int GetRowSize()
        {
            return Width;
        }

        /// <summary> Query the start offset of a row.
        ///
        /// </summary>
        /// <param name="row">the row to query
        ///
        /// </param>
        /// <returns> the offset to the pixel data
        /// </returns>
        public int RowOffset(int row)
        {
            return row * GetRowSize();
        }

        /// <summary>
        /// Convert the pixel to 24 bit color.
        /// </summary>
        /// <returns>
        /// The converted pixel
        /// </returns>
        public IPixel PixelRamp(IPixelReference pixel)
        {
            return pixel.ToPixel();
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
        /// <see cref="DjvuNet.Errors.DjvuArgumentOutOfRangeException"/>  if the specified bounds are not contained in the page
        /// </throws>
#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
# endif
        public unsafe void Stencil(IBitmap mask, IPixelMap foregroundMap, int supersample,
        int subsample, Rectangle bounds, double gamma)
        {
            // Check arguments
            Rectangle rect = new Rectangle(0, 0, ((foregroundMap.Width * supersample) + subsample - 1) / subsample,
                                   ((foregroundMap.Height * supersample) + subsample - 1) / subsample);

            if (bounds != null)
            {
                if ((bounds.Right < rect.Right) || (bounds.Bottom < rect.Bottom) || (bounds.Left > rect.Left) ||
                    (bounds.Top > rect.Top))
                {
                    throw new DjvuArgumentOutOfRangeException(nameof(bounds),
                        "Rectangle out of bounds" + "bounds=(" + bounds.Right + "," + bounds.Bottom +
                                                "," + bounds.Left + "," + bounds.Top + "),rect=(" + rect.Right + "," +
                                                rect.Bottom + "," + rect.Left + "," + rect.Top + ")");
                }

                rect = bounds;
            }

            // Compute number of rows
            int xrows = Height;

            if (mask.Height < xrows)
            {
                xrows = mask.Height;
            }

            if (rect.Height < xrows)
            {
                xrows = rect.Height;
            }

            // Compute number of columns
            int xcolumns = Width;

            if (mask.Width < xcolumns)
            {
                xcolumns = mask.Width;
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
            int[] gtable = GetGammaCorrection(gamma);

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
            var fgx = foregroundMap.CreateGPixelReference(0);
            var dst = CreateGPixelReference(0);

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
        public IMap2 Translate(int dx, int dy, IMap2 retval)
        {
            if (!(retval is PixelMap) || (retval.Width != Width) || (retval.Height != Height))
            {
                retval = new PixelMap().Init(Height, Width, null);
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
        public PixelMap Init(sbyte[] data, int arows, int acolumns)
        {
            Height = arows;
            Width = acolumns;
            this.Data = data;

            return this;
        }

#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public unsafe PixelMap Init(IDjvuReader reader)
        {
            // Read header
            bool raw = false;
            bool grey = false;
            ushort magic = reader.ReadUInt16BigEndian();
            Bitmap bm = null;
            switch (magic)
            {
                case (('P' << 8) + '2'):
                    grey = true;
                    break;
                case (('P' << 8) + '3'):
                    break;
                case (('P' << 8) + '5'):
                    raw = grey = true;
                    break;
                case (('P' << 8) + '6'):
                    raw = true;
                    break;
                case ('P' << 8) + '1':
                case ('P' << 8) + '4':
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    bm = Bitmap.CreateBitmap(reader.BaseStream);
                    Init(bm);
                    return this;
                default:
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    //JPEGDecoder::decode(bs, *this);
                    return this;

            }

            // Read image size
            char lookahead = '\n';
            int bytesperrow = 0;
            int bytespercomp = 1;
            uint acolumns = ReadInteger(ref lookahead, reader.BaseStream);
            uint arows = ReadInteger(ref lookahead, reader.BaseStream);
            uint maxval = ReadInteger(ref lookahead, reader.BaseStream);

            if (maxval > 65535)
            {
                throw new DjvuFormatException("Cannot read PPM data with depth greater than 48 bits.");
            }

            if (maxval > 255)
            {
                bytespercomp = 2;
            }

            Init((int)arows, (int)acolumns, Pixel.BlackPixel);

            // Prepare ramp
            byte[] ramp = null;
            int maxbin = 1 << (8 * bytespercomp);
            Array.Resize(ref ramp, maxbin - 1);

            for (int i = 0; i < maxbin; i++)
            {
                ramp[i] = (byte) (i < maxval ? (255 * i + maxval / 2) / maxval : 255);
            }

            fixed (byte* pramp = ramp)
            fixed (sbyte* pData = Data)
            {
                sbyte* bramp = (sbyte*)pramp;
                // Read image data
                if (raw && grey)
                {
                    bytesperrow = Width * bytespercomp;
                    byte[] line = new  byte[bytesperrow];

                    for (int y = Height - 1; y >= 0; y--)
                    {
                        fixed (byte* pg = line)
                        {
                            byte* g = pg;
                            Pixel* p = (Pixel*)pData + y * bytesperrow;
                            if (reader.Read(line, 0, bytesperrow) < bytesperrow)
                            {
                                throw new DjvuEndOfStreamException("Unexpected end of stream");
                            }

                            if (bytespercomp <= 1)
                            {
                                for (int x = 0; x < Width; x += 1, g += 1)
                                {
                                    p[x].Red = p[x].Green = p[x].Blue = bramp[g[0]];
                                }
                            }
                            else
                            {
                                for (int x = 0; x < Width; x += 1, g += 2)
                                {
                                    p[x].Red = p[x].Green = p[x].Blue = bramp[g[0] * 256 + g[1]];
                                }
                            }
                        }
                    }
                }
                else if (raw)
                {
                    bytesperrow = Width * bytespercomp * 3;
                    byte[] line = new byte[bytesperrow];

                    for (int y = Height - 1; y >= 0; y--)
                    {
                        Pixel* p = (Pixel*)pData + y * bytesperrow;
                        fixed (byte* prgb = line)
                        {
                            byte* rgb = prgb;
                            if (reader.Read(line, 0, bytesperrow) < bytesperrow)
                            {
                                throw new DjvuEndOfStreamException("Unexpected end of stream");
                            }

                            if (bytespercomp <= 1)
                            {
                                for (int x = 0; x < Width; x += 1, rgb += 3)
                                {
                                    p[x].Red = bramp[rgb[0]];
                                    p[x].Green = bramp[rgb[1]];
                                    p[x].Blue = bramp[rgb[2]];
                                }
                            }
                            else
                            {
                                for (int x = 0; x < Width; x += 1, rgb += 6)
                                {
                                    p[x].Red = bramp[rgb[0] * 256 + rgb[1]];
                                    p[x].Green = bramp[rgb[2] * 256 + rgb[3]];
                                    p[x].Blue = bramp[rgb[4] * 256 + rgb[5]];
                                }
                            }
                        }
                    }
                }
                else
                {
                    bytesperrow = Width * bytespercomp * 3;

                    for (int y = Height - 1; y >= 0; y--)
                    {
                        Pixel* p = (Pixel*)pData + y * bytesperrow;

                        for (int x = 0; x < Width; x++)
                        {
                            if (grey)
                            {
                                p[x].Green = p[x].Blue = p[x].Red = bramp[(int)ReadInteger(ref lookahead, reader.BaseStream)];
                            }
                            else
                            {
                                p[x].Red = bramp[(int)ReadInteger(ref lookahead, reader.BaseStream)];
                                p[x].Green = bramp[(int)ReadInteger(ref lookahead, reader.BaseStream)];
                                p[x].Blue = bramp[(int)ReadInteger(ref lookahead, reader.BaseStream)];
                            }
                        }
                    }
                }
            }

            return this;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Creates or retrieves a cached multiplier array to use when attenuating.
        /// </summary>
        /// <returns>
        /// Attenuation array
        /// </returns>
        internal static int[] GetMultiplier(int maxgray)
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
