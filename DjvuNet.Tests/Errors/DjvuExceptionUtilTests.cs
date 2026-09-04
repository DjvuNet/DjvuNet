using System;
using System.Collections.Generic;
using Xunit;
using DjvuNet.Errors;

namespace DjvuNet.Tests.Errors
{
    public class DjvuExceptionUtilTests
    {
        /// <summary>
        /// Verifies that ThrowArgumentNull correctly constructs and throws a DjvuArgumentNullException
        /// when only a parameter name is provided. This ensures the fast-path helper accurately maps
        /// to the underlying system exception signature.
        /// </summary>
        [Fact]
        public void ThrowArgumentNull_NoMessage()
        {
            var ex = Assert.Throws<DjvuArgumentNullException>(() => DjvuExceptionUtil.ThrowArgumentNull("testParam"));
            Assert.Equal("testParam", ex.ParamName);
        }

        /// <summary>
        /// Verifies that ThrowArgumentNull correctly constructs and throws a DjvuArgumentNullException
        /// with both a parameter name and a custom message. This tests the overload resolution inside the helper.
        /// </summary>
        [Fact]
        public void ThrowArgumentNull_WithMessage()
        {
            var ex = Assert.Throws<DjvuArgumentNullException>(() => DjvuExceptionUtil.ThrowArgumentNull("testParam", "Custom message"));
            Assert.Equal("testParam", ex.ParamName);
            Assert.Contains("Custom message", ex.Message);
        }

        /// <summary>
        /// Verifies that ThrowArgumentOutOfRange maps correctly when only a parameter name is provided.
        /// Essential for validating the helper doesn't default to incorrect underlying constructors.
        /// </summary>
        [Fact]
        public void ThrowArgumentOutOfRange_NoValueNoMessage()
        {
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => DjvuExceptionUtil.ThrowArgumentOutOfRange("testParam"));
            Assert.Equal("testParam", ex.ParamName);
        }

        /// <summary>
        /// Verifies that ThrowArgumentOutOfRange correctly handles a custom message alongside the parameter name.
        /// </summary>
        [Fact]
        public void ThrowArgumentOutOfRange_NoValueWithMessage()
        {
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => DjvuExceptionUtil.ThrowArgumentOutOfRange("testParam", "Custom message"));
            Assert.Equal("testParam", ex.ParamName);
            Assert.Contains("Custom message", ex.Message);
        }

        /// <summary>
        /// Verifies the generic implementation of ThrowArgumentOutOfRange. 
        /// This test ensures that passing a value type (like int) does not cause boxing at the caller site,
        /// and that the value is correctly passed into the exception's ActualValue property.
        /// </summary>
        [Fact]
        public void ThrowArgumentOutOfRangeGeneric_WithValue()
        {
            int badValue = 42;
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => DjvuExceptionUtil.ThrowArgumentOutOfRange("testParam", badValue, "Custom message"));
            Assert.Equal("testParam", ex.ParamName);
            Assert.Equal(42, ex.ActualValue);
            Assert.Contains("Custom message", ex.Message);
        }

        /// <summary>
        /// Verifies that ThrowArgument maps correctly to DjvuArgumentException when only a custom message is provided.
        /// </summary>
        [Fact]
        public void ThrowArgument_NoParamName()
        {
            var ex = Assert.Throws<DjvuArgumentException>(() => DjvuExceptionUtil.ThrowArgument("Custom message"));
            Assert.Equal("Custom message", ex.Message);
            Assert.Null(ex.ParamName);
        }

        /// <summary>
        /// Verifies that ThrowArgument maps correctly to DjvuArgumentException when both a message and parameter name are provided.
        /// </summary>
        [Fact]
        public void ThrowArgument_WithParamName()
        {
            var ex = Assert.Throws<DjvuArgumentException>(() => DjvuExceptionUtil.ThrowArgument("Custom message", "testParam"));
            Assert.Contains("Custom message", ex.Message);
            Assert.Equal("testParam", ex.ParamName);
        }

        /// <summary>
        /// Verifies that ThrowInvalidOperation correctly constructs the exception with just a message.
        /// </summary>
        [Fact]
        public void ThrowInvalidOperation_NoInner()
        {
            var ex = Assert.Throws<DjvuInvalidOperationException>(() => DjvuExceptionUtil.ThrowInvalidOperation("Custom message"));
            Assert.Equal("Custom message", ex.Message);
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowInvalidOperation correctly nests an inner exception when provided,
        /// ensuring the stack trace chain is preserved by the helper.
        /// </summary>
        [Fact]
        public void ThrowInvalidOperation_WithInner()
        {
            var inner = new Exception("Inner");
            var ex = Assert.Throws<DjvuInvalidOperationException>(() => DjvuExceptionUtil.ThrowInvalidOperation("Custom message", inner));
            Assert.Equal("Custom message", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowNotSupported correctly constructs the exception with just a message.
        /// </summary>
        [Fact]
        public void ThrowNotSupported_NoInner()
        {
            var ex = Assert.Throws<DjvuNotSupportedException>(() => DjvuExceptionUtil.ThrowNotSupported("Custom message"));
            Assert.Equal("Custom message", ex.Message);
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowNotSupported correctly nests an inner exception when provided.
        /// </summary>
        [Fact]
        public void ThrowNotSupported_WithInner()
        {
            var inner = new Exception("Inner");
            var ex = Assert.Throws<DjvuNotSupportedException>(() => DjvuExceptionUtil.ThrowNotSupported("Custom message", inner));
            Assert.Equal("Custom message", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowFileNotFound correctly constructs the exception with just a message.
        /// </summary>
        [Fact]
        public void ThrowFileNotFound_NoFileName()
        {
            var ex = Assert.Throws<DjvuFileNotFoundException>(() => DjvuExceptionUtil.ThrowFileNotFound("Custom message"));
            Assert.Equal("Custom message", ex.Message);
        }

        /// <summary>
        /// Verifies that ThrowFileNotFound correctly assigns the file name property when provided,
        /// which is critical for IO debugging.
        /// </summary>
        [Fact]
        public void ThrowFileNotFound_WithFileName()
        {
            var ex = Assert.Throws<DjvuFileNotFoundException>(() => DjvuExceptionUtil.ThrowFileNotFound("Custom message", "missing.txt"));
            Assert.Equal("Custom message", ex.Message);
            Assert.Equal("missing.txt", ex.FileName);
        }

        /// <summary>
        /// Verifies that ThrowEndOfStream correctly constructs the exception with just a message.
        /// </summary>
        [Fact]
        public void ThrowEndOfStream_NoInner()
        {
            var ex = Assert.Throws<DjvuEndOfStreamException>(() => DjvuExceptionUtil.ThrowEndOfStream("Custom message"));
            Assert.Equal("Custom message", ex.Message);
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowEndOfStream correctly nests an inner exception when provided.
        /// </summary>
        [Fact]
        public void ThrowEndOfStream_WithInner()
        {
            var inner = new Exception("Inner");
            var ex = Assert.Throws<DjvuEndOfStreamException>(() => DjvuExceptionUtil.ThrowEndOfStream("Custom message", inner));
            Assert.Equal("Custom message", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowAggregate correctly wraps a collection of inner exceptions,
        /// ensuring the helper successfully maps IEnumerable arguments to the AggregateException constructor.
        /// </summary>
        [Fact]
        public void ThrowAggregate()
        {
            var innerList = new List<Exception> { new Exception("1"), new Exception("2") };
            var ex = Assert.Throws<DjvuAggregateException>(() => DjvuExceptionUtil.ThrowAggregate("Custom message", innerList));
            Assert.StartsWith("Custom message", ex.Message);
            Assert.Equal(2, ex.InnerExceptions.Count);
        }

        /// <summary>
        /// Verifies that ThrowArgumentOutOfRange correctly nests an inner exception when provided
        /// with a custom message.
        /// </summary>
        [Fact]
        public void ThrowArgumentOutOfRange_WithInner()
        {
            var inner = new Exception("Inner");
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => DjvuExceptionUtil.ThrowArgumentOutOfRange("Custom message", inner));
            Assert.Equal("Custom message", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowFormatException correctly constructs the exception with just a message.
        /// </summary>
        [Fact]
        public void ThrowFormatException_NoInner()
        {
            var ex = Assert.Throws<DjvuFormatException>(() => DjvuExceptionUtil.ThrowFormatException("Custom message"));
            Assert.Equal("Custom message", ex.Message);
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowFormatException correctly nests an inner exception when provided.
        /// </summary>
        [Fact]
        public void ThrowFormatException_WithInner()
        {
            var inner = new Exception("Inner");
            var ex = Assert.Throws<DjvuFormatException>(() => DjvuExceptionUtil.ThrowFormatException("Custom message", inner));
            Assert.Equal("Custom message", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }
        /// <summary>
        /// Verifies that ThrowNullReference correctly constructs a parameterless exception.
        /// </summary>
        [Fact]
        public void ThrowNullReference_NoMessageNoInner()
        {
            var ex = Assert.Throws<DjvuNullReferenceException>(() => DjvuExceptionUtil.ThrowNullReference());
            Assert.Null(ex.InnerException);
            // Default NullReferenceException message is implementation defined, so we don't assert it strictly.
        }

        /// <summary>
        /// Verifies that ThrowNullReference correctly constructs the exception with just a message.
        /// </summary>
        [Fact]
        public void ThrowNullReference_WithMessage()
        {
            var ex = Assert.Throws<DjvuNullReferenceException>(() => DjvuExceptionUtil.ThrowNullReference("Custom null message"));
            Assert.Equal("Custom null message", ex.Message);
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowNullReference correctly nests an inner exception when provided.
        /// </summary>
        [Fact]
        public void ThrowNullReference_WithMessageAndInner()
        {
            var inner = new Exception("Inner exception");
            var ex = Assert.Throws<DjvuNullReferenceException>(() => DjvuExceptionUtil.ThrowNullReference("Custom null message", inner));
            Assert.Equal("Custom null message", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowIndexOutOfRange correctly constructs a parameterless exception.
        /// </summary>
        [Fact]
        public void ThrowIndexOutOfRange_NoMessageNoInner()
        {
            var ex = Assert.Throws<DjvuIndexOutOfRangeException>(() => DjvuExceptionUtil.ThrowIndexOutOfRange());
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowIndexOutOfRange correctly constructs the exception with just a message.
        /// </summary>
        [Fact]
        public void ThrowIndexOutOfRange_WithMessage()
        {
            var ex = Assert.Throws<DjvuIndexOutOfRangeException>(() => DjvuExceptionUtil.ThrowIndexOutOfRange("Custom index message"));
            Assert.Equal("Custom index message", ex.Message);
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowIndexOutOfRange correctly nests an inner exception when provided.
        /// </summary>
        [Fact]
        public void ThrowIndexOutOfRange_WithMessageAndInner()
        {
            var inner = new Exception("Inner exception");
            var ex = Assert.Throws<DjvuIndexOutOfRangeException>(() => DjvuExceptionUtil.ThrowIndexOutOfRange("Custom index message", inner));
            Assert.Equal("Custom index message", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowNotImplemented correctly constructs a parameterless exception.
        /// </summary>
        [Fact]
        public void ThrowNotImplemented_NoMessage()
        {
            var ex = Assert.Throws<DjvuNotImplementedException>(() => DjvuExceptionUtil.ThrowNotImplemented());
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowNotImplemented correctly constructs the exception with just a message.
        /// </summary>
        [Fact]
        public void ThrowNotImplemented_WithMessage()
        {
            var ex = Assert.Throws<DjvuNotImplementedException>(() => DjvuExceptionUtil.ThrowNotImplemented("Custom not implemented message"));
            Assert.Equal("Custom not implemented message", ex.Message);
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowNotImplemented correctly nests an inner exception when provided.
        /// </summary>
        [Fact]
        public void ThrowNotImplemented_WithMessageAndInner()
        {
            var inner = new Exception("Inner exception");
            var ex = Assert.Throws<DjvuNotImplementedException>(() => DjvuExceptionUtil.ThrowNotImplemented("Custom not implemented message", inner));
            Assert.Equal("Custom not implemented message", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowTimeoutException correctly constructs the exception with no arguments.
        /// </summary>
        [Fact]
        public void ThrowTimeoutException_NoMessageNoInner()
        {
            var ex = Assert.Throws<DjvuTimeoutException>(() => DjvuExceptionUtil.ThrowTimeoutException());
            Assert.NotNull(ex.Message); // Default system message
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowTimeoutException correctly constructs the exception with just a message.
        /// </summary>
        [Fact]
        public void ThrowTimeoutException_WithMessageNoInner()
        {
            var ex = Assert.Throws<DjvuTimeoutException>(() => DjvuExceptionUtil.ThrowTimeoutException("Custom message"));
            Assert.Equal("Custom message", ex.Message);
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowTimeoutException correctly constructs the exception with no message but an inner exception.
        /// </summary>
        [Fact]
        public void ThrowTimeoutException_NoMessageWithInner()
        {
            var inner = new Exception("Inner exception");
            var ex = Assert.Throws<DjvuTimeoutException>(() => DjvuExceptionUtil.ThrowTimeoutException(null, inner));
            Assert.NotNull(ex.Message); // Default system message
            Assert.Same(inner, ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowTimeoutException correctly constructs the exception with a message and an inner exception.
        /// </summary>
        [Fact]
        public void ThrowTimeoutException_WithMessageAndInner()
        {
            var inner = new Exception("Inner exception");
            var ex = Assert.Throws<DjvuTimeoutException>(() => DjvuExceptionUtil.ThrowTimeoutException("Custom message", inner));
            Assert.Equal("Custom message", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        // --- OVERLOAD 1: ThrowObjectDisposed(string objectName, string message = null) ---

        [Fact]
        public void ThrowObjectDisposed_ObjectName()
        {
            var ex = Assert.Throws<DjvuObjectDisposedException>(() => DjvuExceptionUtil.ThrowObjectDisposed("TestObject"));
            Assert.Equal("TestObject", ex.ObjectName);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void ThrowObjectDisposed_ObjectNameAndMessage()
        {
            var ex = Assert.Throws<DjvuObjectDisposedException>(() => DjvuExceptionUtil.ThrowObjectDisposed("TestObject", "Custom message"));
            Assert.Equal("TestObject", ex.ObjectName);
            Assert.Contains("Custom message", ex.Message);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void ThrowObjectDisposed_NullObjectNameNoMessage()
        {
            var ex = Assert.Throws<DjvuObjectDisposedException>(() => DjvuExceptionUtil.ThrowObjectDisposed((string)null));
            Assert.Equal("", ex.ObjectName); // BCL ObjectDisposedException defaults null objectName to empty string
            Assert.NotNull(ex.Message); // System default
        }

        [Fact]
        public void ThrowObjectDisposed_NullObjectNameWithMessage()
        {
            var ex = Assert.Throws<DjvuObjectDisposedException>(() => DjvuExceptionUtil.ThrowObjectDisposed((string)null, "Custom message"));
            Assert.Equal("", ex.ObjectName); // BCL ObjectDisposedException defaults null objectName to empty string
            Assert.Contains("Custom message", ex.Message);
        }

        // --- OVERLOAD 2: ThrowObjectDisposed(string message, Exception innerException) ---

        [Fact]
        public void ThrowObjectDisposed_MessageAndInner()
        {
            var inner = new Exception("Inner exception");
            var ex = Assert.Throws<DjvuObjectDisposedException>(() => DjvuExceptionUtil.ThrowObjectDisposed("Custom message", inner));
            Assert.Contains("Custom message", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void ThrowObjectDisposed_NullMessageAndInner()
        {
            var inner = new Exception("Inner exception");
            var ex = Assert.Throws<DjvuObjectDisposedException>(() => DjvuExceptionUtil.ThrowObjectDisposed(null, inner));
            Assert.NotNull(ex.Message); // System default
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void ThrowObjectDisposed_MessageAndNullInner()
        {
            var ex = Assert.Throws<DjvuObjectDisposedException>(() => DjvuExceptionUtil.ThrowObjectDisposed("Custom message", (Exception)null));
            Assert.Contains("Custom message", ex.Message);
            Assert.Null(ex.InnerException);
        }

    }
}
