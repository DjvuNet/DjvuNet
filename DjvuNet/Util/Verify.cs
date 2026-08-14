using System;
using System.Runtime.CompilerServices;
using DjvuNet.Errors;

namespace DjvuNet.Utilities
{
    public static class Verify
    {
        public const int SubsambpleMin = 1;
        public const int SubsampleMax = 12;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SubsampleRange(int subsample)
        {
            if (subsample < SubsambpleMin || subsample > SubsampleMax)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(subsample), subsample,
                    $"Argument is outside of allowed values expected from {SubsambpleMin} to {SubsampleMax}" +
                    $" actual value {subsample}");
            }
        }
    }
}
