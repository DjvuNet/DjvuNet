using System;
using System.Runtime.Serialization;

namespace DjvuNet.Errors
{
    [Serializable]
    public class DjvuNullReferenceException : NullReferenceException
    {
        public DjvuNullReferenceException()
        {
        }

        public DjvuNullReferenceException(string message)
            : base(message)
        {
        }

        public DjvuNullReferenceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
