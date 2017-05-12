using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuNet.Graphics;

namespace DjvuNet.Wavelet
{
    /// <summary>
    /// Interpolated wavelet Transform
    /// </summary>
    public class InterWaveTransform
    {

        public static float[][] rgb_to_ycc = new float[3][] 
        { 
            new float[] { 0.304348F, 0.608696F, 0.086956F },
            new float[] { 0.463768F, -0.405797F, -0.057971F },
            new float[] { -0.173913F, -0.347826F,  0.521739F }
        };

        private static void filter_begin(int w, int h)
        {
#if MMX
            //if (MMXControl::mmxflag < 0)
            //    MMXControl::enable_mmx();
#endif
        }

        private static void filter_end()
        {
#if MMX
            if (MMXControl::mmxflag > 0)
                MMXemms;
#endif
        }

        private static int min(int x, int y) { return (x < y) ? x : y; }

        private static int max(int x, int y) { return (x>y) ? x : y; }

        /// <summary>
        /// Forward transform.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public static unsafe void Forward(short* p, int w, int h, int rowsize, int begin, int end)
        {
            filter_begin(w, h);

            for (int scale = begin; scale < end; scale <<= 1)
            {
                filter_fh(p, w, h, rowsize, scale);
                filter_fv(p, w, h, rowsize, scale);
            }

            filter_end();
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
            filter_begin(w, h);

            for (int scale = begin >> 1; scale >= end; scale >>= 1)
            {
                filter_bv(p, w, h, rowsize, scale);
                filter_bh(p, w, h, rowsize, scale);
            }

            filter_end();
        }

        /// <summary>
        /// Color transform - extracts Y (luminance) from RGB images
        /// </summary>
        /// <param name="p"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="out"></param>
        /// <param name="outrowsize"></param>
        public static unsafe void RGB_to_Y(Pixel* p, int w, int h, int rowsize, sbyte* @out, int outrowsize)
        {
            int[] rmul = new int[256];
            int[] gmul = new int[256];
            int[] bmul = new int[256];
            for (int k = 0; k < 256; k++)
            {
                rmul[k] = (int)(k * 0x10000 * rgb_to_ycc[0][0]);
                gmul[k] = (int)(k * 0x10000 * rgb_to_ycc[0][1]);
                bmul[k] = (int)(k * 0x10000 * rgb_to_ycc[0][2]);
            }
            for (int i = 0; i < h; i++, p += rowsize, @out += outrowsize)
            {
                Pixel* p2 = p;
                sbyte* out2 = @out;
                for (int j = 0; j < w; j++, p2++, out2++)
                {
                    int y = rmul[p2->Red] + gmul[p2->Green] + bmul[p2->Blue] + 32768;
                    *out2 = (sbyte) ((y >> 16) - 128);
                }
            }
        }

        /// <summary>
        /// Color transform - extracts Cb (blue chrominance) form RGB images
        /// </summary>
        /// <param name="p"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="out"></param>
        /// <param name="outrowsize"></param>
        public static unsafe void RGB_to_Cb(Pixel* p, int w, int h, int rowsize, sbyte* @out, int outrowsize)
        {
            int[] rmul = new int[256];
            int[] gmul = new int[256];
            int[] bmul = new int[256];
            for (int k = 0; k < 256; k++)
            {
                rmul[k] = (int)(k * 0x10000 * rgb_to_ycc[2][0]);
                gmul[k] = (int)(k * 0x10000 * rgb_to_ycc[2][1]);
                bmul[k] = (int)(k * 0x10000 * rgb_to_ycc[2][2]);
            }
            for (int i = 0; i < h; i++, p += rowsize, @out += outrowsize)
            {
                Pixel* p2 = p;
                sbyte* out2 = @out;
                for (int j = 0; j < w; j++, p2++, out2++)
                {
                    int c = rmul[p2->Red] + gmul[p2->Green] + bmul[p2->Blue] + 32768;
                    *out2 = (sbyte) max(-128, min(127, c >> 16));
                }
            }
        }

        /// <summary>
        /// Color transform - extracts Cr (red chrominance) from RGB images
        /// </summary>
        /// <param name="p"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="out"></param>
        /// <param name="outrowsize"></param>
        public static unsafe void RGB_to_Cr(Pixel* p, int w, int h, int rowsize, sbyte* @out, int outrowsize)
        {
            int[] rmul = new int[256];
            int[] gmul = new int[256];
            int[] bmul = new int[256];
            for (int k = 0; k < 256; k++)
            {
                rmul[k] = (int)((k * 0x10000) * rgb_to_ycc[1][0]);
                gmul[k] = (int)((k * 0x10000) * rgb_to_ycc[1][1]);
                bmul[k] = (int)((k * 0x10000) * rgb_to_ycc[1][2]);
            }
            for (int i = 0; i < h; i++, p += rowsize, @out += outrowsize)
            {
                Pixel* p2 = p;
                sbyte* out2 = @out;
                for (int j = 0; j < w; j++, p2++, out2++)
                {
                    int c = rmul[p2->Red] + gmul[p2->Green] + bmul[p2->Blue] + 32768;
                    *out2 = (sbyte) max(-128, min(127, c >> 16));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="scale"></param>
        public static unsafe void filter_fv(short* p, int w, int h, int rowsize, int scale)
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
//# ifdef MMX
//                        if (scale == 1 && MMXControl::mmxflag > 0)
//                            mmx_fv_1(q, e, s, s3);
//#endif
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
//# ifdef MMX
//                        if (scale == 1 && MMXControl::mmxflag > 0)
//                            mmx_fv_2(q, e, s, s3);
//#endif
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
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="rowsize"></param>
        /// <param name="scale"></param>
        public static unsafe void filter_fh(short* p, int w, int h, int rowsize, int scale)
        {
            int y = 0;
            int s = scale;
            int s3 = s + s + s;
            rowsize *= scale;
            while (y < h)
            {
                short* q = p + s;
                short* e = p + w;
                int a0 = 0, a1 = 0, a2 = 0, a3 = 0;
                int b0 = 0, b1 = 0, b2 = 0, b3 = 0;
                if (q < e)
                {
                    // Special case: x=1
                    a1 = a2 = a3 = q[-s];
                    if (q + s < e)
                        a2 = q[s];
                    if (q + s3 < e)
                        a3 = q[s3];
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
                        q[-s3] = (short)((int)q[-s3] + ((((b1 + b2) << 3) + (b1 + b2) - b0 - b3 + 16) >> 5));
                    q += s + s;
                }
                y += scale;
                p += rowsize;
            }
        }


        public static unsafe void filter_bv(short* p, int w, int h, int rowsize, int scale)
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
                        // Generic case
#if MMX
                        if (scale == 1 && MMXControl::mmxflag > 0)
                            mmx_bv_1(q, e, s, s3);
#endif
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
#if MMX
                        if (scale == 1 && MMXControl::mmxflag > 0)
                            mmx_bv_2(q, e, s, s3);
#endif
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


        public static unsafe void filter_bh(short* p, int w, int h, int rowsize, int scale)
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
