using Xunit;
using DjvuNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace DjvuNet.Tests
{
    public class DjvuFormatExceptionTests
    {
        [Fact()]
        public void DjvuFormatExceptionTest()
        {
            DjvuFormatException exception = null;
            string testStr = "test";
            exception = new DjvuFormatException(testStr);

            Assert.NotNull(exception);
            Assert.NotNull(exception.Message);
            Assert.Equal<string>(testStr, exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact()]
        public void DjvuFormatExceptionTest1()
        {
            DjvuFormatException exception = null;
            Exception innerException = new Exception();
            string testStr = "test";
            exception = new DjvuFormatException(testStr, innerException);

            Assert.NotNull(exception);
            Assert.NotNull(exception.Message);
            Assert.Equal<string>(testStr, exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Same(innerException, exception.InnerException);
        }

        [Fact()]
        public void DjvuFormatExceptionTest2()
        {
            DjvuFormatException exception = null;
            exception = new DjvuFormatException();

            Assert.NotNull(exception);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void DjvuFormatExceptionTest3()
        {
            string innerMessage = "Invalid test message";
            InvalidOperationException inner = new InvalidOperationException(innerMessage);
            DjvuFormatException exception = null;
            string djvuMessage = "Test message";
            exception = new DjvuFormatException(djvuMessage, inner);

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
                DjvuFormatException exDeserialized = (DjvuFormatException) formatter.Deserialize(stream);

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