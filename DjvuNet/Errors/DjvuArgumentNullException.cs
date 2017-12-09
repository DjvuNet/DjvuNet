using System;

namespace DjvuNet.Errors
{
    public class DjvuArgumentNullException  : ArgumentNullException
    {
        public DjvuArgumentNullException() : base()
        {
        }

        public DjvuArgumentNullException(string paramName) : base(paramName)
        {
        }

        public DjvuArgumentNullException(string message, Exception innerException) : base (message, innerException)
        {
        }

        public DjvuArgumentNullException(string paramName, string message) : base (paramName, message)
        {
        }

#if !NETSTANDARD2_0
        public DjvuArgumentNullException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base (info, context)
        {
        }
#endif
    }
}
