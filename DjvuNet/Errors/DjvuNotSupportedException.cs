using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Errors
{
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

#if !NETSTANDARD2_0
        public DjvuNotSupportedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base (info, context)
        {
        }
#endif
    }
}
