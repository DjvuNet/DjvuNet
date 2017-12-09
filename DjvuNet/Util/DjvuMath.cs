using System.Runtime.CompilerServices;

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
