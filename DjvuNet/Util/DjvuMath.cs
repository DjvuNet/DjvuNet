using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Utilities
{
    public static class DjvuMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void EuclidianRatio(int a, int b, out int q, out int r)
        {
            q = a / b;
            r = a - b * q;
            if (r < 0)
            {
                q -= 1;
                r += b;
            }
        }
    }
}
