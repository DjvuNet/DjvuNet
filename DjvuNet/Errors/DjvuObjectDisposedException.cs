using System;

namespace DjvuNet.Errors
{
    /// <summary>
    /// The exception that is thrown when an operation is performed on a disposed object.
    /// </summary>
    public class DjvuObjectDisposedException : ObjectDisposedException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DjvuObjectDisposedException"/> class with a string containing the name of the disposed object.
        /// </summary>
        /// <param name="objectName">A string containing the name of the disposed object.</param>
        public DjvuObjectDisposedException(string objectName) : base(objectName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DjvuObjectDisposedException"/> class with the specified object name and message.
        /// </summary>
        /// <param name="objectName">The name of the disposed object.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DjvuObjectDisposedException(string objectName, string message) : base(objectName, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DjvuObjectDisposedException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DjvuObjectDisposedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
