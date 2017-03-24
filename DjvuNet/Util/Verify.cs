using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Util
{
    public static class Verify
    {
        public const int SubsambpleMin = 1;
        public const int SubsampleMax = 12;

        public static void SubsampleRange(int subsample)
        {
            if (subsample < SubsambpleMin || subsample > SubsampleMax)
                throw new ArgumentException(
                    $"Argument is outside of allowed values expected from {SubsambpleMin} to {SubsampleMax}" +
                    $" actual value {subsample}");
        }
    }
}
