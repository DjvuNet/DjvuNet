using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DjvuNet.Errors
{
    /// <summary>
    /// Centralized exception throwing helpers. Abstracting 'throw' statements
    /// into non-inlined methods significantly improves the JIT code generation
    /// for the caller by removing the cold-path exception initialization logic
    /// from the caller's instruction stream.
    /// </summary>
    public static class DjvuExceptionUtil
    {
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentNull(string paramName, string message = null)
        {
            if (message == null)
                throw new DjvuArgumentNullException(paramName);
            throw new DjvuArgumentNullException(paramName, message);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRange(string paramName, string message = null)
        {
            if (message == null)
                throw new DjvuArgumentOutOfRangeException(paramName);
            throw new DjvuArgumentOutOfRangeException(paramName, message);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRange(string message, Exception innerException)
        {
            throw new DjvuArgumentOutOfRangeException(message, innerException);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRange<T>(string paramName, T actualValue, string message = null)
        {
            throw new DjvuArgumentOutOfRangeException(paramName, actualValue, message);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgument(string message, string paramName = null)
        {
            if (paramName == null)
                throw new DjvuArgumentException(message);
            throw new DjvuArgumentException(message, paramName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperation(string message, Exception innerException = null)
        {
            if (innerException == null)
                throw new DjvuInvalidOperationException(message);
            throw new DjvuInvalidOperationException(message, innerException);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNotSupported(string message, Exception innerException = null)
        {
            if (innerException == null)
                throw new DjvuNotSupportedException(message);
            throw new DjvuNotSupportedException(message, innerException);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowFileNotFound(string message, string fileName = null)
        {
            if (fileName == null)
                throw new DjvuFileNotFoundException(message);
            throw new DjvuFileNotFoundException(message, fileName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowEndOfStream(string message, Exception innerException = null)
        {
            if (innerException == null)
                throw new DjvuEndOfStreamException(message);
            throw new DjvuEndOfStreamException(message, innerException);
        }

        /// <summary>
        /// Overload which allows for using exception throwing helper in ternary expressions.
        /// Useful for throwing exceptions during parsing with very limited impact on performance
        /// for non error paths due to Jit creating conditional move operations.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T ThrowEndOfStream<T>(string message, Exception innerException = null) where T : struct
        {
            ThrowEndOfStream(message, innerException);
            return default(T);

        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowAggregate(string message, IEnumerable<Exception> innerExceptions)
        {
            throw new DjvuAggregateException(message, innerExceptions);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowFormatException(string message, Exception innerException = null)
        {
            if (innerException == null)
                throw new DjvuFormatException(message);
            throw new DjvuFormatException(message, innerException);
        }
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNullReference(string message = null, Exception innerException = null)
        {
            if (message == null && innerException == null)
                throw new DjvuNullReferenceException();
            if (innerException == null)
                throw new DjvuNullReferenceException(message);
            throw new DjvuNullReferenceException(message, innerException);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowIndexOutOfRange(string message = null, Exception innerException = null)
        {
            if (message == null && innerException == null)
                throw new DjvuIndexOutOfRangeException();
            if (innerException == null)
                throw new DjvuIndexOutOfRangeException(message);
            throw new DjvuIndexOutOfRangeException(message, innerException);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNotImplemented(string message = null, Exception innerException = null)
        {
            if (message == null && innerException == null)
                throw new DjvuNotImplementedException();
            if (innerException == null)
                throw new DjvuNotImplementedException(message);
            throw new DjvuNotImplementedException(message, innerException);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowTimeoutException(string message = null, Exception innerException = null)
        {
            if (message == null && innerException == null)
                throw new DjvuTimeoutException();
            if (innerException == null)
                throw new DjvuTimeoutException(message);
            throw new DjvuTimeoutException(message, innerException);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowObjectDisposed(string objectName, string message = null)
        {
            if (message == null)
                throw new DjvuObjectDisposedException(objectName);
            throw new DjvuObjectDisposedException(objectName, message);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowObjectDisposed(string message, Exception innerException)
        {
            throw new DjvuObjectDisposedException(message, innerException);
        }
    }
}
