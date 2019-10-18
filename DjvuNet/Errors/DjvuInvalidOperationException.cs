using System;

namespace DjvuNet.Errors
{
    [Serializable]
    public class DjvuInvalidOperationException : InvalidOperationException
    {
        public DjvuInvalidOperationException() : base()
        {
        }

        public DjvuInvalidOperationException(string message) : base(message)
        {
        }

        public DjvuInvalidOperationException(string message, Exception innerException)
            : base (message, innerException)
        {
        }

        public DjvuInvalidOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base (info, context)
        {
        }
    }
}
