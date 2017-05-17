using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Errors
{
    public class DjvuFileNotFoundException : FileNotFoundException
    {
        public DjvuFileNotFoundException() : base()
        {
        }

        public DjvuFileNotFoundException(string message) : base(message)
        {
        }

        public DjvuFileNotFoundException(string message, Exception innerException) : base (message, innerException)
        {
        }

        public DjvuFileNotFoundException(string message, string fileName) : base (message, fileName)
        {
        }

        public DjvuFileNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) 
            : base (info, context)
        {
        }

        public DjvuFileNotFoundException(string message, string fileName, Exception innerException) 
            : base(message, fileName, innerException)
        {
        }
    }
}
