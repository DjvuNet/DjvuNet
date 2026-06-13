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
        public void ThrowArgumentNull_NoMessage_ThrowsCorrectly()
        {
            var ex = Assert.Throws<DjvuArgumentNullException>(() => DjvuExceptionUtil.ThrowArgumentNull("testParam"));
            Assert.Equal("testParam", ex.ParamName);
        }

        /// <summary>
        /// Verifies that ThrowArgumentNull correctly constructs and throws a DjvuArgumentNullException
        /// with both a parameter name and a custom message. This tests the overload resolution inside the helper.
        /// </summary>
        [Fact]
        public void ThrowArgumentNull_WithMessage_ThrowsCorrectly()
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
        public void ThrowArgumentOutOfRange_NoValueNoMessage_ThrowsCorrectly()
        {
            var ex = Assert.Throws<DjvuArgumentOutOfRangeException>(() => DjvuExceptionUtil.ThrowArgumentOutOfRange("testParam"));
            Assert.Equal("testParam", ex.ParamName);
        }

        /// <summary>
        /// Verifies that ThrowArgumentOutOfRange correctly handles a custom message alongside the parameter name.
        /// </summary>
        [Fact]
        public void ThrowArgumentOutOfRange_NoValueWithMessage_ThrowsCorrectly()
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
        public void ThrowArgumentOutOfRangeGeneric_WithValue_ThrowsCorrectly()
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
        public void ThrowArgument_NoParamName_ThrowsCorrectly()
        {
            var ex = Assert.Throws<DjvuArgumentException>(() => DjvuExceptionUtil.ThrowArgument("Custom message"));
            Assert.Equal("Custom message", ex.Message);
            Assert.Null(ex.ParamName);
        }

        /// <summary>
        /// Verifies that ThrowArgument maps correctly to DjvuArgumentException when both a message and parameter name are provided.
        /// </summary>
        [Fact]
        public void ThrowArgument_WithParamName_ThrowsCorrectly()
        {
            var ex = Assert.Throws<DjvuArgumentException>(() => DjvuExceptionUtil.ThrowArgument("Custom message", "testParam"));
            Assert.Contains("Custom message", ex.Message);
            Assert.Equal("testParam", ex.ParamName);
        }

        /// <summary>
        /// Verifies that ThrowInvalidOperation correctly constructs the exception with just a message.
        /// </summary>
        [Fact]
        public void ThrowInvalidOperation_NoInner_ThrowsCorrectly()
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
        public void ThrowInvalidOperation_WithInner_ThrowsCorrectly()
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
        public void ThrowNotSupported_NoInner_ThrowsCorrectly()
        {
            var ex = Assert.Throws<DjvuNotSupportedException>(() => DjvuExceptionUtil.ThrowNotSupported("Custom message"));
            Assert.Equal("Custom message", ex.Message);
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowNotSupported correctly nests an inner exception when provided.
        /// </summary>
        [Fact]
        public void ThrowNotSupported_WithInner_ThrowsCorrectly()
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
        public void ThrowFileNotFound_NoFileName_ThrowsCorrectly()
        {
            var ex = Assert.Throws<DjvuFileNotFoundException>(() => DjvuExceptionUtil.ThrowFileNotFound("Custom message"));
            Assert.Equal("Custom message", ex.Message);
        }

        /// <summary>
        /// Verifies that ThrowFileNotFound correctly assigns the file name property when provided,
        /// which is critical for IO debugging.
        /// </summary>
        [Fact]
        public void ThrowFileNotFound_WithFileName_ThrowsCorrectly()
        {
            var ex = Assert.Throws<DjvuFileNotFoundException>(() => DjvuExceptionUtil.ThrowFileNotFound("Custom message", "missing.txt"));
            Assert.Equal("Custom message", ex.Message);
            Assert.Equal("missing.txt", ex.FileName);
        }

        /// <summary>
        /// Verifies that ThrowEndOfStream correctly constructs the exception with just a message.
        /// </summary>
        [Fact]
        public void ThrowEndOfStream_NoInner_ThrowsCorrectly()
        {
            var ex = Assert.Throws<DjvuEndOfStreamException>(() => DjvuExceptionUtil.ThrowEndOfStream("Custom message"));
            Assert.Equal("Custom message", ex.Message);
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowEndOfStream correctly nests an inner exception when provided.
        /// </summary>
        [Fact]
        public void ThrowEndOfStream_WithInner_ThrowsCorrectly()
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
        public void ThrowAggregate_ThrowsCorrectly()
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
        public void ThrowArgumentOutOfRange_WithInner_ThrowsCorrectly()
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
        public void ThrowFormatException_NoInner_ThrowsCorrectly()
        {
            var ex = Assert.Throws<DjvuFormatException>(() => DjvuExceptionUtil.ThrowFormatException("Custom message"));
            Assert.Equal("Custom message", ex.Message);
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that ThrowFormatException correctly nests an inner exception when provided.
        /// </summary>
        [Fact]
        public void ThrowFormatException_WithInner_ThrowsCorrectly()
        {
            var inner = new Exception("Inner");
            var ex = Assert.Throws<DjvuFormatException>(() => DjvuExceptionUtil.ThrowFormatException("Custom message", inner));
            Assert.Equal("Custom message", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }
    }
}
