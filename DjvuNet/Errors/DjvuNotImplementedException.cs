using System;

namespace DjvuNet.Errors
{
    public class DjvuNotImplementedException : NotImplementedException
    {
        public DjvuNotImplementedException() : base()
        {
        }

        public DjvuNotImplementedException(string message) : base(message)
        {
        }

        public DjvuNotImplementedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
