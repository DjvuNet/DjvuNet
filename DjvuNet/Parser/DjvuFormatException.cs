using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;


namespace DjvuNet
{
    public class DjvuFormatException : System.FormatException
    {
        //
        // Summary:
        //     Initializes a new instance of the DjvuNet.DjvuFormatException class.
        public DjvuFormatException()
        {
        }
        //
        // Summary:
        //     Initializes a new instance of the DjvuNet.IFFParsingException class with a specified
        //     error message.
        //
        // Parameters:
        //   message:
        //     The message that describes the error.
        public DjvuFormatException(string message) : base(message)
        {
        }
        //
        // Summary:
        //     Initializes a new instance of the DjvuNet.IFFParsingException class with a specified
        //     error message and a reference to the inner exception that is the cause of this
        //     exception.
        //
        // Parameters:
        //   message:
        //     The error message that explains the reason for the exception.
        //
        //   innerException:
        //     The exception that is the cause of the current exception. If the innerException
        //     parameter is not a null reference (Nothing in Visual Basic), the current exception
        //     is raised in a catch block that handles the inner exception.
        public DjvuFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        //
        // Summary:
        //     Initializes a new instance of the DjvuNet.IFFParsingException class with serialized
        //     data.
        //
        // Parameters:
        //   info:
        //     The object that holds the serialized object data.
        //
        //   context:
        //     The contextual information about the source or destination.
        protected DjvuFormatException(SerializationInfo info, StreamingContext context):
            base (info, context)
        {
        }
    }
}
