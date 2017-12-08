using System;
using System.IO;

namespace DjvuNet.Errors
{
    public class DjvuEndOfStreamException : EndOfStreamException
    {
        public DjvuEndOfStreamException() : base()
        {
        }

        public DjvuEndOfStreamException(string message) : base(message)
        {
        }

        public DjvuEndOfStreamException(string message, Exception innerException)
            : base (message, innerException)
        {
        }

#if !NETSTANDARD2_0
        public DjvuEndOfStreamException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base (info, context)
        {
        }
#endif
    }
}
