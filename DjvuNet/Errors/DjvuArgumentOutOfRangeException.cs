using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Errors
{
    public class DjvuArgumentOutOfRangeException : ArgumentOutOfRangeException
    {
        public DjvuArgumentOutOfRangeException() : base()
        {
        }

        public DjvuArgumentOutOfRangeException(string paramName) : base(paramName)
        {
        }

        public DjvuArgumentOutOfRangeException(string message, Exception innerException) : base (message, innerException)
        {
        }

        public DjvuArgumentOutOfRangeException(string paramName, string message) : base (paramName, message)
        {
        }

#if !NETSTANDARD2_0
        public DjvuArgumentOutOfRangeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) 
            : base (info, context)
        {
        }
#endif

        public DjvuArgumentOutOfRangeException(string paramName, object actualValue, string message) 
            : base(paramName, actualValue, message)
        {
        }
    }
}
