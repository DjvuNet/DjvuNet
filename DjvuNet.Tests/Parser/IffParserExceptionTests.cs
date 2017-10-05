using Xunit;
using DjvuNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace DjvuNet.Tests
{
    public class IffParserExceptionTests
    {
        [Fact()]
        public void IffParserExceptionTest()
        {
            IffParserException exception = null;
            string testStr = "test";
            exception = new IffParserException(testStr);

            Assert.NotNull(exception);
            Assert.NotNull(exception.Message);
            Assert.Equal(testStr, exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact()]
        public void IffParserExceptionTest1()
        {
            IffParserException exception = null;
            Exception innerException = new Exception();
            string testStr = "test";
            exception = new IffParserException(testStr, innerException);

            Assert.NotNull(exception);
            Assert.NotNull(exception.Message);
            Assert.Equal(testStr, exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Same(innerException, exception.InnerException);
        }

        [Fact()]
        public void IffParserExceptionTest2()
        {
            IffParserException exception = null;
            exception = new IffParserException();

            Assert.NotNull(exception);
            Assert.Null(exception.InnerException);
        }

        [Fact, SkipOnTargetFramework(TargetFrameworkMonikers.Netcoreapp)]
        public void BinarySerializationRoundtripTest()
        {
            string innerMessage = "Invalid test message";
            InvalidOperationException inner = new InvalidOperationException(innerMessage);
            IffParserException exception = null;
            string djvuMessage = "Test message";
            exception = new IffParserException(djvuMessage, inner);

            Assert.NotNull(exception);
            Assert.NotNull(exception.InnerException);
            Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.NotNull(exception.Message);
            Assert.Equal(djvuMessage, exception.Message);
            Assert.NotNull(exception.InnerException.Message);
            Assert.Equal(innerMessage, exception.InnerException.Message);

            IFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, exception);
                stream.Position = 0;
                IffParserException exDeserialized = (IffParserException)formatter.Deserialize(stream);

                Assert.NotNull(exDeserialized);
                Assert.NotNull(exDeserialized.InnerException);
                Assert.IsType<InvalidOperationException>(exDeserialized.InnerException);
                Assert.NotNull(exDeserialized.Message);
                Assert.Equal(djvuMessage, exDeserialized.Message);
                Assert.NotNull(exDeserialized.InnerException.Message);
                Assert.Equal(innerMessage, exDeserialized.InnerException.Message);
            }
        }
    }
}
