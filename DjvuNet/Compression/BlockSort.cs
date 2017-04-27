using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Compression
{
    public class BlockSort : IBlockSort
    {
        private const int OVERFLOW = 32;

        // Sorting thresholds
        private const int RankSortThreshold = 10;
        private const int QuickSortStack = 512;
        private const int PresortThreshold = 10;
        private const int PresortDepth = 8;
        private const int RadixThreshold = 32768;

        private const int FREQS0 = 100000;
        private const int FREQS1 = 1000000;

        private int _Size;
        private byte[] _Data;
        private int[] _Posn;
        private int[] _Rank;

        public BlockSort() { }

        public BlockSort(byte[] pdata, int psize)
        {
            if (psize <= 0 || psize >= 0x1000000)
                throw new ArgumentOutOfRangeException(nameof(_Size));

            _Size = psize;
            _Data = pdata;
            _Posn = new int[_Size];
            _Rank = new int[_Size + 1];
        }

        public void Sort(ref int markerpos)
        {
            int lo, hi;

            if (_Size <= 0)
                throw new InvalidOperationException();

            if (!(_Data[_Size - 1] == 0))
                throw new InvalidOperationException();

            // Step 1: Radix sort 
            int depth = 0;
            if (_Size > RadixThreshold)
            {
                RadixSort16();
                depth = 2;
            }
            else
            {
                RadixSort8();
                depth = 1;
            }

            // Step 2: Perform presort to depth PRESORT_DEPTH
            for (lo = 0; lo < _Size; lo++)
            {
                hi = _Rank[_Posn[lo]];
                if (lo < hi)
                    QuickSort3d(lo, hi, depth);
                lo = hi;
            }

            depth = PresortDepth;

            // Step 3: Perform rank doubling

            int again = 1;
            while (again != 0)
            {
                again = 0;
                int sorted_lo = 0;
                for (lo = 0; lo < _Size; lo++)
                {
                    hi = _Rank[_Posn[lo] & 0xffffff];
                    if (lo == hi)
                    {
                        lo += (_Posn[lo] >> 24) & 0xff;
                    }
                    else
                    {
                        if (hi - lo < RankSortThreshold)
                        {
                            RankSort(lo, hi, depth);
                        }
                        else
                        {

                            again += 1;
                            while (sorted_lo < lo - 1)
                            {
                                int step = Math.Min(255, lo - 1 - sorted_lo);
                                _Posn[sorted_lo] = (_Posn[sorted_lo] & 0xffffff) | (step << 24);
                                sorted_lo += step + 1;
                            }

                            QuickSort3r(lo, hi, depth);
                            sorted_lo = hi + 1;
                        }
                        lo = hi;
                    }
                }

                // Finish threading
                while (sorted_lo < lo - 1)
                {
                    int step = Math.Min(255, lo - 1 - sorted_lo);
                    _Posn[sorted_lo] = (_Posn[sorted_lo] & 0xffffff) | (step << 24);
                    sorted_lo += step + 1;
                }

                // Double depth
                depth += depth;

            } // end while (again != 0)

            // Step 4: Permute data
            int i;
            markerpos = -1;

            for (i = 0; i < _Size; i++)
                _Rank[i] = _Data[i];

            for (i = 0; i < _Size; i++)
            {
                int j = _Posn[i] & 0xffffff;
                if (j > 0)
                {
                    _Data[i] = (byte) _Rank[j - 1];
                }
                else
                {
                    _Data[i] = 0;
                    markerpos = i;
                }
            }

            if (!(markerpos >= 0 && markerpos < _Size))
                throw new InvalidOperationException();
 
        }


        public static void BlockSortData(byte[] data, int size, ref int markerpos)
        {
            BlockSort bsort = new BlockSort(data, size);
            bsort.Sort(ref markerpos);
        }

        internal static void ValueSwap(int i, int j, int n, int[] x)
        {
            while (n-- > 0)
            {
                int tmp = x[i];
                x[i++] = x[j];
                x[j++] = tmp;
            }
        }

        internal bool GT(int p1, int p2, int depth)
        {
            int r1, r2;
            int twod = depth + depth;

            while (true)
            {
                r1 = _Rank[p1 + depth]; r2 = _Rank[p2 + depth];
                p1 += twod; p2 += twod;

                if (r1 != r2)
                    return (r1 > r2);

                r1 = _Rank[p1]; r2 = _Rank[p2];
                if (r1 != r2)
                    return (r1 > r2);

                r1 = _Rank[p1 + depth]; r2 = _Rank[p2 + depth];
                p1 += twod; p2 += twod;
                if (r1 != r2)
                    return (r1 > r2);

                r1 = _Rank[p1]; r2 = _Rank[p2];
                if (r1 != r2)
                    return (r1 > r2);

                r1 = _Rank[p1 + depth]; r2 = _Rank[p2 + depth];
                p1 += twod; p2 += twod;
                if (r1 != r2)
                    return (r1 > r2);

                r1 = _Rank[p1]; r2 = _Rank[p2];
                if (r1 != r2)
                    return (r1 > r2);

                r1 = _Rank[p1 + depth]; r2 = _Rank[p2 + depth];
                p1 += twod; p2 += twod;
                if (r1 != r2)
                    return (r1 > r2);

                r1 = _Rank[p1]; r2 = _Rank[p2];
                if (r1 != r2)
                    return (r1 > r2);
            };
        }

        /// <summary>
        /// GTD function compares suffixes using data information
        /// (up to depth PRESORT_DEPTH)
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        internal bool GTD(int p1, int p2, int depth)
        {
            byte c1, c2;
            p1 += depth;
            p2 += depth;

            while (depth < PresortDepth)
            {
                // Perform two
                c1 = _Data[p1];
                c2 = _Data[p2];

                if (c1 != c2)
                    return (c1 > c2);

                c1 = _Data[p1 + 1];
                c2 = _Data[p2 + 1];

                p1 += 2;
                p2 += 2;
                depth += 2;

                if (c1 != c2)
                    return c1 > c2;
            }

            if (p1 < _Size && p2 < _Size)
                return false;

            return p1 < p2;
        }

        /// <summary>
        /// A simple insertion sort based on GT
        /// </summary>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        /// <param name="d"></param>
        internal void RankSort(int lo, int hi, int depth)
        {
            int i, j;
            for (i = lo + 1; i <= hi; i++)
            {
                int tmp = _Posn[i];

                for (j = i - 1; j >= lo && GT(_Posn[j], tmp, depth); j--)
                    _Posn[j + 1] = _Posn[j];

                _Posn[j + 1] = tmp;
            }

            for (i = lo; i <= hi; i++)
                _Rank[_Posn[i]] = i;
        }

        /// <summary>
        /// Doubling sort
        /// </summary>
        /// <param name="rr"></param>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        /// <returns></returns>
        internal int Pivot3r(int[] rr, int lo, int hi, int position)
        {
            int c1, c2, c3;
            if (hi - lo > 256)
            {
                c1 = Pivot3r(rr, lo, (6 * lo + 2 * hi) / 8, position);
                c2 = Pivot3r(rr, (5 * lo + 3 * hi) / 8, (3 * lo + 5 * hi) / 8, position);
                c3 = Pivot3r(rr, (2 * lo + 6 * hi) / 8, hi, position);
            }
            else
            {
                c1 = rr[_Posn[lo] + position];
                c2 = rr[_Posn[(lo + hi) / 2] + position];
                c3 = rr[_Posn[hi] + position];
            }

            // Extract median
            if (c1 > c3)
            {
                int tmp = c1;
                c1 = c3;
                c3 = tmp;
            }

            if (c2 <= c1)
                return c1;
            else if (c2 >= c3)
                return c3;
            else
                return c2;
        }

        internal void QuickSort3r(int lo, int hi, int depth)
        {
            /* Initialize stack */
            int[] slo = new int[QuickSortStack];
            int[] shi = new int[QuickSortStack];
            int sp = 1;
            slo[0] = lo;
            shi[0] = hi;

            // Recursion elimination loop
            while (--sp >= 0)
            {
                lo = slo[sp];
                hi = shi[sp];

                // Test for insertion sort
                if (hi - lo < RankSortThreshold)
                {
                    RankSort(lo, hi, depth);
                }
                else
                {
                    int tmp;
                    int med = Pivot3r(_Rank, lo, hi, depth);

                    // -- positions are organized as follows:
                    //   [lo..l1[ [l1..l[ ]h..h1] ]h1..hi]
                    //      =        <       >        =
                    int l1 = lo;
                    int h1 = hi;

                    while (_Rank[_Posn[l1] + depth] == med && l1 < h1) { l1++; }
                    while (_Rank[_Posn[h1] + depth] == med && l1 < h1) { h1--; }

                    int l = l1;
                    int h = h1;
                    // -- partition set
                    for (;;)
                    {
                        while (l <= h)
                        {
                            int c = _Rank[_Posn[l] + depth] - med;
                            if (c > 0) break;
                            if (c == 0) { tmp = _Posn[l]; _Posn[l] = _Posn[l1]; _Posn[l1++] = tmp; }
                            l++;
                        }
                        while (l <= h)
                        {
                            int c = _Rank[_Posn[h] + depth] - med;
                            if (c < 0) break;
                            if (c == 0) { tmp = _Posn[h]; _Posn[h] = _Posn[h1]; _Posn[h1--] = tmp; }
                            h--;
                        }
                        if (l > h) break;
                        tmp = _Posn[l]; _Posn[l] = _Posn[h]; _Posn[h] = tmp;
                    }

                    // -- reorganize as follows
                    //   [lo..l1[ [l1..h1] ]h1..hi]
                    //      <        =        > 
                    tmp = Math.Min(l1 - lo, l - l1);
                    ValueSwap(lo, l - tmp, tmp, _Posn);
                    l1 = lo + (l - l1);

                    tmp = Math.Min(hi - h1, h1 - h);
                    ValueSwap(hi - tmp + 1, h + 1, tmp, _Posn);
                    h1 = hi - (h1 - h);

                    // -- process segments
                    if (!(sp + 2 < QuickSortStack))
                        throw new InvalidOperationException(
                            $"Value of {nameof(sp)} variable outside of expected range: {sp}");

                    // ----- middle segment (=?) [l1, h1]
                    for (int i = l1; i <= h1; i++)
                        _Rank[_Posn[i]] = h1;

                    // ----- lower segment (<) [lo, l1[
                    if (l1 > lo)
                    {
                        for (int i = lo; i < l1; i++)
                            _Rank[_Posn[i]] = l1 - 1;
                        slo[sp] = lo;
                        shi[sp] = l1 - 1;
                        if (slo[sp] < shi[sp])
                            sp++;
                    }

                    // ----- upper segment (>) ]h1, hi]
                    if (h1 < hi)
                    {
                        slo[sp] = h1 + 1;
                        shi[sp] = hi;
                        if (slo[sp] < shi[sp])
                            sp++;
                    }
                }
            }
        }

        // -- presort to depth PRESORT_DEPTH
        internal byte Pivot3d(byte[] rr, int lo, int hi, int depth = 0)
        {
            byte c1, c2, c3;
            if (hi - lo > 256)
            {
                c1 = Pivot3d(rr, lo, (6 * lo + 2 * hi) / 8, depth);
                c2 = Pivot3d(rr, (5 * lo + 3 * hi) / 8, (3 * lo + 5 * hi) / 8, depth);
                c3 = Pivot3d(rr, (2 * lo + 6 * hi) / 8, hi, depth);
            }
            else
            {
                c1 = rr[_Posn[lo] + depth];
                c2 = rr[_Posn[(lo + hi) / 2] + depth];
                c3 = rr[_Posn[hi] + depth];
            }
            // Extract median
            if (c1 > c3)
            {
                byte tmp = c1;
                c1 = c3;
                c3 = tmp;
            }

            if (c2 <= c1)
                return c1;
            else if (c2 >= c3)
                return c3;
            else
                return c2;
        }

        internal void QuickSort3d(int lo, int hi, int depth)
        {
            /* Initialize stack */
            int[] slo = new int[QuickSortStack];
            int[] shi = new int[QuickSortStack];
            int[] sd = new int[QuickSortStack];

            int sp = 1;
            slo[0] = lo;
            shi[0] = hi;
            sd[0] = depth;

            // Recursion elimination loop
            while (--sp >= 0)
            {
                lo = slo[sp];
                hi = shi[sp];
                depth = sd[sp];

                // Test for insertion sort
                if (depth >= PresortDepth)
                {
                    for (int i = lo; i <= hi; i++)
                        _Rank[_Posn[i]] = hi;
                }
                else if (hi - lo < PresortThreshold)
                {
                    int i, j;
                    for (i = lo + 1; i <= hi; i++)
                    {
                        int tmp = _Posn[i];
                        for (j = i - 1; j >= lo && GTD(_Posn[j], tmp, depth); j--)
                            _Posn[j + 1] = _Posn[j];
                        _Posn[j + 1] = tmp;
                    }
                    for (i = hi; i >= lo; i = j)
                    {
                        int tmp = _Posn[i];
                        _Rank[tmp] = i;
                        for (j = i - 1; j >= lo && !GTD(tmp, _Posn[j], depth); j--)
                            _Rank[_Posn[j]] = i;
                    }
                }
                else
                {
                    int tmp;
                    byte med = Pivot3d(_Data, lo, hi, depth);

                    // -- positions are organized as follows:
                    //   [lo..l1[ [l1..l[ ]h..h1] ]h1..hi]
                    //      =        <       >        =
                    int l1 = lo;
                    int h1 = hi;

                    while (_Data[_Posn[l1] + depth] == med && l1 < h1) { l1++; }
                    while (_Data[_Posn[h1] + depth] == med && l1 < h1) { h1--; }

                    int l = l1;
                    int h = h1;

                    // -- partition set
                    for (;;)
                    {
                        while (l <= h)
                        {
                            int c = (int)_Data[_Posn[l] + depth] - (int)med;
                            if (c > 0) break;
                            if (c == 0) { tmp = _Posn[l]; _Posn[l] = _Posn[l1]; _Posn[l1++] = tmp; }
                            l++;
                        }
                        while (l <= h)
                        {
                            int c = (int)_Data[_Posn[h] + depth] - (int)med;
                            if (c < 0) break;
                            if (c == 0) { tmp = _Posn[h]; _Posn[h] = _Posn[h1]; _Posn[h1--] = tmp; }
                            h--;
                        }
                        if (l > h) break;
                        tmp = _Posn[l]; _Posn[l] = _Posn[h]; _Posn[h] = tmp;
                    }

                    // -- reorganize as follows
                    //   [lo..l1[ [l1..h1] ]h1..hi]
                    //      <        =        > 
                    tmp = Math.Min(l1 - lo, l - l1);
                    ValueSwap(lo, l - tmp, tmp, _Posn);
                    l1 = lo + (l - l1);

                    tmp = Math.Min(hi - h1, h1 - h);
                    ValueSwap(hi - tmp + 1, h + 1, tmp, _Posn);
                    h1 = hi - (h1 - h);

                    // -- process segments
                    if(!(sp + 3 < QuickSortStack))
                        throw new InvalidOperationException(
                            $"Value of {nameof(sp)} variable outside of expected range: {sp}");

                    // ----- middle segment (=?) [l1, h1]
                    l = l1; h = h1;
                    if (med == 0) // special case for marker [slow]
                    {
                        for (int i = l; i <= h; i++)
                        {
                            if ((int)_Posn[i] + depth == _Size - 1)
                            {
                                tmp = _Posn[i]; _Posn[i] = _Posn[l]; _Posn[l] = tmp;
                                _Rank[tmp] = l++; break;
                            }
                        }
                    }

                    if (l < h)
                    {
                        slo[sp] = l;
                        shi[sp] = h;
                        sd[sp++] = depth + 1;
                    }
                    else if (l == h)
                    {
                        _Rank[_Posn[h]] = h;
                    }

                    // ----- lower segment (<) [lo, l1[
                    l = lo;
                    h = l1 - 1;
                    if (l < h)
                    {
                        slo[sp] = l;
                        shi[sp] = h;
                        sd[sp++] = depth;
                    }
                    else if (l == h)
                    {
                        _Rank[_Posn[h]] = h;
                    }

                    // ----- upper segment (>) ]h1, hi]
                    l = h1 + 1;
                    h = hi;
                    if (l < h)
                    {
                        slo[sp] = l;
                        shi[sp] = h;
                        sd[sp++] = depth;
                    }
                    else if (l == h)
                    {
                        _Rank[_Posn[h]] = h;
                    }
                }
            }
        }

        // -- radixsort
        internal void RadixSort16()
        {
            int i;
            // Initialize frequency array
            int[] ftab = new int[65536];

            // Count occurrences
            byte c1 = _Data[0];
            for (i = 0; i < _Size - 1; i++)
            {
                byte c2 = _Data[i + 1];
                ftab[(c1 << 8) | c2]++;
                c1 = c2;
            }

            // Generate upper position
            for (i = 1; i < 65536; i++)
                ftab[i] += ftab[i - 1];

            // Fill rank array with upper bound
            c1 = _Data[0];
            for (i = 0; i < _Size - 2; i++)
            {
                byte c2 = _Data[i + 1];
                _Rank[i] = ftab[(c1 << 8) | c2];
                c1 = c2;
            }

            // Fill posn array (backwards)
            c1 = _Data[_Size - 2];
            for (i = _Size - 3; i >= 0; i--)
            {
                byte c2 = _Data[i];
                _Posn[ftab[(c2 << 8) | c1]--] = i;
                c1 = c2;
            }

            // Fixup marker stuff
            if(!(_Data[_Size - 1] == 0))
                throw new InvalidOperationException();

            c1 = _Data[_Size - 2];
            _Posn[0] = _Size - 1;
            _Posn[ftab[(c1 << 8)]] = _Size - 2;
            _Rank[_Size - 1] = 0;
            _Rank[_Size - 2] = ftab[(c1 << 8)];

            // Extra element
            _Rank[_Size] = -1;
        }

        internal void RadixSort8()
        {
            int i;
            // Initialize frequency array
            int[] lo = new int[256];
            int[] hi = new int[256];

            for (i = 0; i < 256; i++)
                hi[i] = lo[i] = 0;

            // Count occurences
            for (i = 0; i < _Size - 1; i++)
                hi[_Data[i]]++;

            // Compute positions (lo)
            int last = 1;
            for (i = 0; i < 256; i++)
            {
                lo[i] = last;
                hi[i] = last + hi[i] - 1;
                last = hi[i] + 1;
            }

            for (i = 0; i < _Size - 1; i++)
            {
                _Posn[lo[_Data[i]]++] = i;
                _Rank[i] = hi[_Data[i]];
            }

            // Process marker "$"
            _Posn[0] = _Size - 1;
            _Rank[_Size - 1] = 0;

            // Extra element
            _Rank[_Size] = -1;
        }
    }
}
