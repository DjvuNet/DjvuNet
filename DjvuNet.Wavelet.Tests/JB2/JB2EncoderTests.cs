using System;
using Xunit;
using DjvuNet.JB2;
using DjvuNet.Errors;

namespace DjvuNet.JB2.Tests
{
    public class JB2EncoderTests
    {
        private class TestJB2Encoder : JB2Encoder
        {
            public void InvokeCodeInheritedShapeCount()
            {
                CodeInheritedShapeCount(new JB2Dictionary());
            }
        }

        [Fact]
        public void JB2Encoder_Creation_Success()
        {
            JB2Encoder encoder = new JB2Encoder();
            Assert.NotNull(encoder);
        }

        [Fact]
        public void JB2Encoder_Methods_ThrowDjvuNotImplementedException()
        {
            var encoder = new TestJB2Encoder();
            Assert.Throws<DjvuNotImplementedException>(() => encoder.InvokeCodeInheritedShapeCount());
        }
    }
}
