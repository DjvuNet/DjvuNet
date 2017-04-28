using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace DjvuNet.DjvuLibre
{
    public class DjvuLibreException : ApplicationException
    {
        public DjvuLibreException() : base()
        {

        }

        public DjvuLibreException(string message) : base(message)
        {

        }

        public DjvuLibreException(string message, Exception innerException) 
            : base(message, innerException)
        {

        }

        public DjvuLibreException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {

        }
    }
}
