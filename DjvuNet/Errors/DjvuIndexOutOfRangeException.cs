using System;

namespace DjvuNet.Errors
{
    public class DjvuIndexOutOfRangeException : ArgumentOutOfRangeException
    {
        public DjvuIndexOutOfRangeException() : base()
        {
        }

        public DjvuIndexOutOfRangeException(string message) : base(message, (Exception)null)
        {
        }

        public DjvuIndexOutOfRangeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
