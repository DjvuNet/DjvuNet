using System.Runtime.CompilerServices;
using DjvuNet.Graphics;

namespace DjvuNet.Wavelet
{
    /// <summary>
    /// Interpolated wavelet Transform
    /// </summary>
    public class InterWaveTransform
    {
        public static int[] redYLUT = new int[256];
        public static int[] greenYLUT = new int[256];
        public static int[] blueYLUT = new int[256];

        public static int[] redCbLUT = new int[256];
        public static int[] greenCbLUT = new int[256];
        public static int[] blueCbLUT = new int[256];

        public static int[] redCrLUT = new int[256];
        public static int[] greenCrLUT = new int[256];
        public static int[] blueCrLUT = new int[256];

        public static bool IsLutInitialized;

        public static float[][] Rgb2YccCoeff = new float[3][]
        {
            new float[] { 0.304348F, 0.608696F, 0.086956F },
            new float[] { 0.463768F, -0.405797F, -0.057971F },
            new float[] { -0.173913F, -0.347826F,  0.521739F }
        };

        /// <summary>
        /// Packed integer assembly functions initialization call.
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        private static void FilterBegin(int w, int h)
        {
#if MMX
            //if (MMXControl::mmxflag < 0)
            //    MMXControl::enable_mmx();
#endif
        }

        /// <summary>
        /// Packed integer assembly function exit routine.
        /// </summary>
        private static void FilterEnd()
        {
#if MMX
            if (MMXControl::mmxflag > 0)
                MMXemms;
#endif
        }

        /// <summary>
        /// Integer minimum function which should always be inlined.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Min(int x, int y) { return (x < y) ? x : y; }

        /// <summary>
        /// Integer maximum function which should always be inlined.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Max(int x, int y) { return (x > y) ? x : y; }

        /// <summary>
        /// Calculates multi scale iterative decomposition.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public static unsafe void Forward(short* p, int w, int h, int rowsize, int begin, int end)
        {
            FilterBegin(w, h);

            for (int scale = begin; scale < end; scale <<= 1)
            {
                FilterFh(p, w, h, rowsize, scale);
                FilterFv(p, w, h, rowsize, scale);
            }

            FilterEnd();
        }

        /// <summary>
        /// Backward transform
        /// </summary>
        /// <param name="p"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public static unsafe void Backward(short* p, int w, int h, int rowsize, int begin, int end)
        {
            FilterBegin(w, h);

            for (int scale = begin >> 1; scale >= end; scale >>= 1)
            {
                FilterBv(p, w, h, rowsize, scale);
                FilterBh(p, w, h, rowsize, scale);
            }

            FilterEnd();
        }

        /// <summary>
        /// Color space transform - extracts Y (luminance) from RGB images
        /// </summary>
        /// <param name="pPix"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="rowsize"></param>
        /// <param name="@out"></param>
        /// <param name="outrowsize"></param>
        public static unsafe void Rgb2Y(Pixel* pPix, int width, int height, int rowsize, sbyte* @out, int outrowsize)
        {
            int[] rmul = new int[256];
            int[] gmul = new int[256];
            int[] bmul = new int[256];
            for (int k = 0; k < 256; k++)
            {
                rmul[k] = (int)(k * 0x10000 * Rgb2YccCoeff[0][0]);
                gmul[k] = (int)(k * 0x10000 * Rgb2YccCoeff[0][1]);
                bmul[k] = (int)(k * 0x10000 * Rgb2YccCoeff[0][2]);
            }

            Pixel* p2 = pPix;
            sbyte* out2 = @out;

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++, p2++, out2++)
                {
                    Pixel pix = *p2;
                    int y = rmul[unchecked((byte)pix.Red)] + gmul[unchecked((byte)pix.Green)] + bmul[unchecked((byte)pix.Blue)] + 32768;
                    *out2 = (sbyte) ((y >> 16) - 128);
                }
            }
        }

        /// <summary>
        /// Color space transform - extracts Cb (blue chrominance) form RGB images
        /// </summary>
        /// <param name="pPixBuffer"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="rowsize"></param>
        /// <param name="out"></param>
        /// <param name="outrowsize"></param>
        public static unsafe void Rgb2Cb(Pixel* pPixBuffer, int width, int height, int rowsize, sbyte* @out, int outrowsize)
        {
            int[] rmul = new int[256];
            int[] gmul = new int[256];
            int[] bmul = new int[256];
            for (int k = 0; k < 256; k++)
            {
                rmul[k] = (int)(k * 0x10000 * Rgb2YccCoeff[2][0]);
                gmul[k] = (int)(k * 0x10000 * Rgb2YccCoeff[2][1]);
                bmul[k] = (int)(k * 0x10000 * Rgb2YccCoeff[2][2]);
            }

            Pixel* p2 = pPixBuffer;
            sbyte* out2 = @out;

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++, p2++, out2++)
                {
                    int c = rmul[unchecked((byte)p2->Red)] + gmul[unchecked((byte)p2->Green)] + bmul[unchecked((byte)p2->Blue)] + 32768;
                    *out2 = (sbyte) Max(-128, Min(127, c >> 16));
                }
            }
        }

        /// <summary>
        /// Color space transform - extracts Cr (red chrominance) from RGB images
        /// </summary>
        /// <param name="p"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="out"></param>
        /// <param name="outrowsize"></param>
        public static unsafe void Rgb2Cr(Pixel* p, int w, int h, int rowsize, sbyte* @out, int outrowsize)
        {
            int[] rmul = new int[256];
            int[] gmul = new int[256];
            int[] bmul = new int[256];
            for (int k = 0; k < 256; k++)
            {
                rmul[k] = (int)((k * 0x10000) * Rgb2YccCoeff[1][0]);
                gmul[k] = (int)((k * 0x10000) * Rgb2YccCoeff[1][1]);
                bmul[k] = (int)((k * 0x10000) * Rgb2YccCoeff[1][2]);
            }

            Pixel* p2 = p;
            sbyte* out2 = @out;

            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++, p2++, out2++)
                {
                    int c = rmul[unchecked((byte)p2->Red)] + gmul[unchecked((byte)p2->Green)] + bmul[unchecked((byte)p2->Blue)] + 32768;
                    *out2 = (sbyte) Max(-128, Min(127, c >> 16));
                }
            }
        }

        /// <summary>
        /// Color Space Conversion from RGB to YCbCr
        /// </summary>
        /// <param name="pPixBuff"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="rowsize"></param>
        /// <param name="outY"></param>
        /// <param name="outCb"></param>
        /// <param name="outCr"></param>
        /// <param name="outrowsize"></param>
        public static unsafe void Rgb2YCbCr(
            Pixel* pPixBuff, int width, int height, int rowsize,
            sbyte* @outY, sbyte* @outCb, sbyte* @outCr, int outrowsize)
        {

            fixed (int* pRedYLUT = redYLUT)
            fixed (int* pGreenYLUT = greenYLUT)
            fixed (int* pBlueYLUT = blueYLUT)
            fixed (int* pRedCbLUT = redCbLUT)
            fixed (int* pGreenCbLUT = greenCbLUT)
            fixed (int* pBlueCbLUT = blueCbLUT)
            fixed (int* pRedCrLUT = redCrLUT)
            fixed (int* pGreenCrLUT = greenCrLUT)
            fixed (int* pBlueCrLUT = blueCrLUT)
            {
                if (!IsLutInitialized)
                {
                    // Create lookup tables
                    for (int k = 0; k < 256; k++)
                    {
                        pRedYLUT[k] = (int)(k * 0x10000 * Rgb2YccCoeff[0][0]);
                        pGreenYLUT[k] = (int)(k * 0x10000 * Rgb2YccCoeff[0][1]);
                        pBlueYLUT[k] = (int)(k * 0x10000 * Rgb2YccCoeff[0][2]);

                        pRedCbLUT[k] = (int)(k * 0x10000 * Rgb2YccCoeff[2][0]);
                        pGreenCbLUT[k] = (int)(k * 0x10000 * Rgb2YccCoeff[2][1]);
                        pBlueCbLUT[k] = (int)(k * 0x10000 * Rgb2YccCoeff[2][2]);

                        pRedCrLUT[k] = (int)((k * 0x10000) * Rgb2YccCoeff[1][0]);
                        pGreenCrLUT[k] = (int)((k * 0x10000) * Rgb2YccCoeff[1][1]);
                        pBlueCrLUT[k] = (int)((k * 0x10000) * Rgb2YccCoeff[1][2]);
                    }
                    IsLutInitialized = true;
                }

                Pixel* p2 = pPixBuff;
                sbyte* pOutY = @outY;
                sbyte* pOutCb = @outCb;
                sbyte* pOutCr = @outCr;

                int dataLength = width * height;

                for (int i = 0; i < dataLength; i++, p2++, pOutCr++, pOutCb++, pOutY++)
                {
                    byte red = unchecked((byte)p2->Red);
                    byte green = unchecked((byte)p2->Green);
                    byte blue = unchecked((byte)p2->Blue);

                    int y = pRedYLUT[red] + pGreenYLUT[green] + pBlueYLUT[blue] + 32768;
                    *pOutY = (sbyte)((y >> 16) - 128);

                    int cb = pRedCbLUT[red] + pGreenCbLUT[green] + pBlueCbLUT[blue] + 32768;
                    *pOutCb = (sbyte)Max(-128, Min(127, cb >> 16));

                    int cr = pRedCrLUT[red] + pGreenCrLUT[green] + pBlueCrLUT[blue] + 32768;
                    *pOutCr = (sbyte)Max(-128, Min(127, cr >> 16));
                }
            }
        }

        /// <summary>
        /// Function for in place conversion of Pixel buffer from YCbCr color space to RGB color space.
        /// </summary>
        /// <param name="pPixBuff">Pointer to Pixel buffer</param>
        /// <param name="width">Width of image in pixels</param>
        /// <param name="height">Height of image in pixels</param>
        public static unsafe void YCbCr2Rgb(Pixel* pPixBuff, int width, int height)
        {
            Pixel* q = pPixBuff;
            int dataLength = width * height;

            for (int i = 0; i < dataLength; i++, q++)
            {
                sbyte y = q->Blue;
                sbyte b = q->Green;
                sbyte r = q->Red;
                // This is the Pigeon transform
                int t1 = b >> 2;
                int t2 = r + (r >> 1);
                int t3 = y + 128 - t1;
                int tr = y + 128 + t2;
                int tg = t3 - (t2 >> 1);
                int tb = t3 + (b << 1);
                q->Red = (sbyte) Max(0, Min(255, tr));
                q->Green = (sbyte) Max(0, Min(255, tg));
                q->Blue = (sbyte) Max(0, Min(255, tb));
            }
        }

        /// <summary>
        /// Add documentation
        /// </summary>
        /// <param name="p"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="scale"></param>
        public static unsafe void FilterFv(short* p, int w, int h, int rowsize, int scale)
        {
            int y = 0;
            int s = scale * rowsize;
            int s3 = s + s + s;
            h = ((h - 1) / scale) + 1;
            y += 1;
            p += s;
            while (y - 3 < h)
            {
                // 1-Delta
                {
                    short* q = p;
                    short* e = q + w;
                    if (y >= 3 && y + 3 < h)
                    {
                        // Generic case
                        while (q < e)
                        {
                            int a = (int)q[-s] + (int)q[s];
                            int b = (int)q[-s3] + (int)q[s3];
                            *q -= (short) (((a << 3) + a - b + 8) >> 4);
                            q += scale;
                        }
                    }
                    else if (y < h)
                    {
                        // Special cases
                        short* q1 = (y + 1 < h ? q + s : q - s);
                        while (q < e)
                        {
                            int a = (int)q[-s] + (int)(*q1);
                            *q -= (short) ((a + 1) >> 1);
                            q += scale;
                            q1 += scale;
                        }
                    }
                }
                // 2-Update
                {
                    short* q = p - s3;
                    short* e = q + w;
                    if (y >= 6 && y < h)
                    {
                        // Generic case
                        while (q < e)
                        {
                            int a = (int)q[-s] + (int)q[s];
                            int b = (int)q[-s3] + (int)q[s3];
                            *q += (short) (((a << 3) + a - b + 16) >> 5);
                            q += scale;
                        }
                    }
                    else if (y >= 3)
                    {
                        // Special cases
                        short* q1 = (y - 2) < h ? q + s : (short*)0;
                        short* q3 = (y < h ? q + s3 : (short*)0);
                        if (y >= 6)
                        {
                            while (q < e)
                            {
                                int a = (int)q[-s] + (q1 != (short*) 0 ? (int)(*q1) : 0);
                                int b = (int)q[-s3] + (q3 != (short*)0 ? (int)(*q3) : 0);
                                *q += (short) (((a << 3) + a - b + 16) >> 5);
                                q += scale;
                                if (q1 != (short*)0) q1 += scale;
                                if (q3 != (short*)0) q3 += scale;
                            }
                        }
                        else if (y >= 4)
                        {
                            while (q < e)
                            {
                                int a = (int)q[-s] + (q1 != (short*)0 ? (int)(*q1) : 0);
                                int b = (q3 != (short*)0 ? (int)(*q3) : 0);
                                *q += (short)(((a << 3) + a - b + 16) >> 5);
                                q += scale;
                                if (q1 != (short*)0) q1 += scale;
                                if (q3 != (short*)0) q3 += scale;
                            }
                        }
                        else
                        {
                            while (q < e)
                            {
                                int a = (q1 != (short*)0 ? (int)(*q1) : 0);
                                int b = (q3 != (short*)0 ? (int)(*q3) : 0);
                                *q += (short)(((a << 3) + a - b + 16) >> 5);
                                q += scale;
                                if (q1 != (short*)0) q1 += scale;
                                if (q3 != (short*)0) q3 += scale;
                            }
                        }
                    }
                }
                y += 2;
                p += s + s;
            }
        }

        /// <summary>
        /// Add docs
        /// </summary>
        /// <param name="p"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="rowsize"></param>
        /// <param name="scale"></param>
        public static unsafe void FilterFh(short* p, int width, int height, int rowsize, int scale)
        {
            int y = 0;
            int s = scale;
            int s3 = s + s + s;
            rowsize *= scale;
            while (y < height)
            {
                short* q = p + s;
                short* e = p + width;
                int a0 = 0, a1 = 0, a2 = 0, a3 = 0;
                int b0 = 0, b1 = 0, b2 = 0, b3 = 0;
                if (q < e)
                {
                    // Special case: x=1
                    a1 = a2 = a3 = q[-s];
                    if (q + s < e)
                    {
                        a2 = q[s];
                    }

                    if (q + s3 < e)
                    {
                        a3 = q[s3];
                    }

                    b3 = q[0] - ((a1 + a2 + 1) >> 1);
                    q[0] = (short) b3;
                    q += s + s;
                }
                while (q + s3 < e)
                {
                    // Generic case
                    a0 = a1;
                    a1 = a2;
                    a2 = a3;
                    a3 = q[s3];
                    b0 = b1;
                    b1 = b2;
                    b2 = b3;
                    b3 = q[0] - ((((a1 + a2) << 3) + (a1 + a2) - a0 - a3 + 8) >> 4);
                    q[0] = (short) b3;
                    q[-s3] = (short) ((int)q[-s3] + ((((b1 + b2) << 3) + (b1 + b2) - b0 - b3 + 16) >> 5));
                    q += s + s;
                }
                while (q < e)
                {
                    // Special case: w-3 <= x < w
                    a1 = a2;
                    a2 = a3;
                    b0 = b1;
                    b1 = b2;
                    b2 = b3;
                    b3 = q[0] - ((a1 + a2 + 1) >> 1);
                    q[0] = (short) b3;
                    q[-s3] = (short)((int)q[-s3] + ((((b1 + b2) << 3) + (b1 + b2) - b0 - b3 + 16) >> 5));
                    q += s + s;
                }
                while (q - s3 < e)
                {
                    // Special case  w <= x < w+3
                    b0 = b1;
                    b1 = b2;
                    b2 = b3;
                    b3 = 0;
                    if (q - s3 >= p)
                    {
                        q[-s3] = (short)((int)q[-s3] + ((((b1 + b2) << 3) + (b1 + b2) - b0 - b3 + 16) >> 5));
                    }

                    q += s + s;
                }
                y += scale;
                p += rowsize;
            }
        }

        /// <summary>
        /// Add docs
        /// </summary>
        /// <param name="p"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="scale"></param>
        public static unsafe void FilterBv(short* p, int w, int h, int rowsize, int scale)
        {
            int y = 0;
            int s = scale * rowsize;
            int s3 = s + s + s;
            h = ((h - 1) / scale) + 1;

            while (y - 3 < h)
            {
                // 1-Lifting
                {
                    short* q = p;
                    short* e = q + w;
                    if (y >= 3 && y + 3 < h)
                    {
                        // Generic cas
                        while (q < e)
                        {
                            int a = (int)q[-s] + (int)q[s];
                            int b = (int)q[-s3] + (int)q[s3];
                            *q -= (short) (((a << 3) + a - b + 16) >> 5);
                            q += scale;
                        }
                    }
                    else if (y < h)
                    {
                        // Special cases
                        short* q1 = (y + 1 < h ? q + s : (short*) 0);
                        short* q3 = (y + 3 < h ? q + s3 : (short*) 0);
                        if (y >= 3)
                        {
                            while (q < e)
                            {
                                int a = (int)q[-s] + (q1 != (short*)0 ? (int)(*q1) : 0);
                                int b = (int)q[-s3] + (q3 != (short*)0 ? (int)(*q3) : 0);
                                *q -= (short) (((a << 3) + a - b + 16) >> 5);
                                q += scale;
                                if (q1 != (short*)0) q1 += scale;
                                if (q3 != (short*)0) q3 += scale;
                            }
                        }
                        else if (y >= 1)
                        {
                            while (q < e)
                            {
                                int a = (int)q[-s] + (q1 != (short*)0 ? (int)(*q1) : 0);
                                int b = (q3 != (short*)0 ? (int)(*q3) : 0);
                                *q -=  (short)(((a << 3) + a - b + 16) >> 5);
                                q += scale;
                                if (q1 != (short*)0) q1 += scale;
                                if (q3 != (short*)0) q3 += scale;
                            }
                        }
                        else
                        {
                            while (q < e)
                            {
                                int a = (q1 != (short*)0 ? (int)(*q1) : 0);
                                int b = (q3 != (short*)0 ? (int)(*q3) : 0);
                                *q -=  (short) (((a << 3) + a - b + 16) >> 5);
                                q += scale;
                                if (q1 != (short*) 0) q1 += scale;
                                if (q3 != (short*)0) q3 += scale;
                            }
                        }
                    }
                }
                // 2-Interpolation
                {
                    short* q = p - s3;
                    short* e = q + w;
                    if (y >= 6 && y < h)
                    {
                        // Generic case
                        while (q < e)
                        {
                            int a = (int)q[-s] + (int)q[s];
                            int b = (int)q[-s3] + (int)q[s3];
                            *q += (short) (((a << 3) + a - b + 8) >> 4);
                            q += scale;
                        }
                    }
                    else if (y >= 3)
                    {
                        // Special cases
                        short* q1 = (y - 2 < h ? q + s : q - s);
                        while (q < e)
                        {
                            int a = (int)q[-s] + (int)(*q1);
                            *q += (short) ((a + 1) >> 1);
                            q += scale;
                            q1 += scale;
                        }
                    }
                }
                y += 2;
                p += s + s;
            }
        }

        /// <summary>
        /// Add docs
        /// </summary>
        /// <param name="p"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="scale"></param>
        public static unsafe void FilterBh(short* p, int w, int h, int rowsize, int scale)
        {
            int y = 0;
            int s = scale;
            int s3 = s + s + s;
            rowsize *= scale;
            while (y < h)
            {
                short* q = p;
                short* e = p + w;
                int a0 = 0, a1 = 0, a2 = 0, a3 = 0;
                int b0 = 0, b1 = 0, b2 = 0, b3 = 0;
                if (q < e)
                {
                    // Special case:  x=0
                    if (q + s < e)
                        a2 = q[s];
                    if (q + s3 < e)
                        a3 = q[s3];
                    b2 = b3 = q[0] - ((((a1 + a2) << 3) + (a1 + a2) - a0 - a3 + 16) >> 5);
                    q[0] = (short) b3;
                    q += s + s;
                }
                if (q < e)
                {
                    // Special case:  x=2
                    a0 = a1;
                    a1 = a2;
                    a2 = a3;
                    if (q + s3 < e)
                        a3 = q[s3];
                    b3 = q[0] - ((((a1 + a2) << 3) + (a1 + a2) - a0 - a3 + 16) >> 5);
                    q[0] = (short) b3;
                    q += s + s;
                }
                if (q < e)
                {
                    // Special case:  x=4
                    b1 = b2;
                    b2 = b3;
                    a0 = a1;
                    a1 = a2;
                    a2 = a3;
                    if (q + s3 < e)
                        a3 = q[s3];
                    b3 = q[0] - ((((a1 + a2) << 3) + (a1 + a2) - a0 - a3 + 16) >> 5);
                    q[0] = (short) b3;
                    q[-s3] = (short) (q[-s3] + ((b1 + b2 + 1) >> 1));
                    q += s + s;
                }
                while (q + s3 < e)
                {
                    // Generic case
                    a0 = a1;
                    a1 = a2;
                    a2 = a3;
                    a3 = q[s3];
                    b0 = b1;
                    b1 = b2;
                    b2 = b3;
                    b3 = q[0] - ((((a1 + a2) << 3) + (a1 + a2) - a0 - a3 + 16) >> 5);
                    q[0] = (short) b3;
                    q[-s3] = (short)(q[-s3] + ((((b1 + b2) << 3) + (b1 + b2) - b0 - b3 + 8) >> 4));
                    q += s + s;
                }
                while (q < e)
                {
                    // Special case:  w-3 <= x < w
                    a0 = a1;
                    a1 = a2;
                    a2 = a3;
                    a3 = 0;
                    b0 = b1;
                    b1 = b2;
                    b2 = b3;
                    b3 = q[0] - ((((a1 + a2) << 3) + (a1 + a2) - a0 - a3 + 16) >> 5);
                    q[0] = (short)b3;
                    q[-s3] = (short)(q[-s3] + ((((b1 + b2) << 3) + (b1 + b2) - b0 - b3 + 8) >> 4));
                    q += s + s;
                }
                while (q - s3 < e)
                {
                    // Special case  w <= x < w+3
                    b0 = b1;
                    b1 = b2;
                    b2 = b3;
                    if (q - s3 >= p)
                        q[-s3] = (short)(q[-s3] + ((b1 + b2 + 1) >> 1));
                    q += s + s;
                }
                y += scale;
                p += rowsize;
            }
        }
    }
}
