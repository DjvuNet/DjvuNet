using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;


namespace DjvuNet
{
    /// <summary>
    /// IFFParserException is thrown to indicate an error in parsing IFF 85 formatting
    /// of DjVu file which is not related to DjVu specific formatting.
    /// </summary>
    public class IFFParserException : System.FormatException
    {

        /// <summary>
        /// Initializes a new instance of the DjvuNet.IFFParserException class.
        /// </summary>
        public IFFParserException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the DjvuNet.IFFParserException class with a specified
        /// error message.
        /// </summary>
        /// <parameter name="message">
        /// The message that describes the error.
        /// </param>
        public IFFParserException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DjvuNet.IFFParserException class with a specified
        /// error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <parameter name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <parameter name="innerException">
        /// The exception that is the cause of the current exception. If the innerException
        /// parameter is not a null reference (Nothing in Visual Basic), the current exception
        /// is raised in a catch block that handles the inner exception.
        /// </param>    
        public IFFParserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DjvuNet.IFFParserException class with serialized data.
        /// </summary>   
        /// <parameter name="info">
        /// The object that holds the serialized object data.
        /// </parameter>
        /// <parameter name="context">
        /// The contextual information about the source or destination.
        /// </parameter>    
        protected IFFParserException(SerializationInfo info, StreamingContext context):
            base (info, context)
        {
        }
    }
}
