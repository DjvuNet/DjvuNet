using System;
using System.Collections.Generic;

namespace DjvuNet.Errors
{
    public class DjvuAggregateException : AggregateException
    {
        public DjvuAggregateException() : base()
        {
        }

        public DjvuAggregateException(string message) : base (message)
        {
        }

        public DjvuAggregateException(params Exception[] innerExceptions)
            : base(innerExceptions)
        {
        }

        public DjvuAggregateException(IEnumerable<Exception> exceptions)
            : base(exceptions)
        {
        }

        public DjvuAggregateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public DjvuAggregateException(string message, params Exception[] innerExceptions)
            : base(message, innerExceptions)
        {
        }

        public DjvuAggregateException(string message, IEnumerable<Exception> exceptions)
            : base(message, exceptions)
        {
        }

#if !NETSTANDARD2_0
        public DjvuAggregateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base (info, context)
        {
        }
#endif
    }
}
