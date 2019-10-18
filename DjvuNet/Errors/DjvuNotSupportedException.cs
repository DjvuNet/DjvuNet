using System;

namespace DjvuNet.Errors
{
    [Serializable]
    public class DjvuNotSupportedException : NotSupportedException
    {
        public DjvuNotSupportedException() : base()
        {
        }

        public DjvuNotSupportedException(string message) : base(message)
        {
        }

        public DjvuNotSupportedException(string message, Exception innerException)
            : base (message, innerException)
        {
        }

        public DjvuNotSupportedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base (info, context)
        {
        }
    }
}
