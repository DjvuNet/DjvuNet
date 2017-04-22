using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Utilities
{
    public class GenericComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> ComparerFunc;
        private readonly Func<T, int> HashFunc;

        public GenericComparer(Func<T, int> hashFunc) 
            : this(hashFunc, (x, y) => hashFunc(x) == hashFunc(y)) { }
       
        public GenericComparer(Func<T, T, bool> comparer) 
            : this((x) => x.GetHashCode(), comparer) { }

        public GenericComparer(Func<T, int> hashFunc, Func<T, T, bool> comparer)
        {
            if (hashFunc == null)
                throw new ArgumentNullException(nameof(hashFunc));

            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            HashFunc = hashFunc;
            ComparerFunc = comparer;
        }

        public bool Equals(T x, T y)
        {
            return ComparerFunc(x, y);
        }

        public int GetHashCode(T obj)
        {
            return HashFunc(obj);
        }
    }

    public static class EnumerableExtensions
    {
        public static bool Contains<T>(this IEnumerable<T> e, T item, Func<T, T, bool> comparer)
        {
            return e.Contains<T>(item, comparer);
        }
    }
}
