using Xunit;
using DjvuNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}