using System;
using DjvuNet.Errors;
using Xunit;

namespace DjvuNet.Tests
{
    public class CalculateBufferSizeTests
    {
        [Fact]
        public void CalculateBufferSize_ValidSizes_ReturnsCorrectSize()
        {
            // Act
            int size1 = Util.CalculateBufferSize(100, 100);
            int size2 = Util.CalculateBufferSize(100, 100, 50);

            // Assert
            Assert.Equal(10000, size1);
            Assert.Equal(10050, size2);
        }

        [Fact]
        public void CalculateBufferSize_ZeroDimensions_ReturnsBorder()
        {
            // Act
            int size = Util.CalculateBufferSize(0, 0, 10);

            // Assert
            Assert.Equal(10, size);
        }

        [Fact]
        public void CalculateBufferSize_OverflowDimensions_ThrowsDjvuInvalidOperationException()
        {
            // Arrange
            int height = 50000;
            int rowSize = 50000;

            // Act & Assert
            var ex = Assert.Throws<DjvuInvalidOperationException>(() => Util.CalculateBufferSize(height, rowSize));
            Assert.Contains("Calculated buffer size exceeds maximum allowed limit", ex.Message);
        }

        [Fact]
        public void CalculateBufferSize_NegativeSize_ThrowsDjvuInvalidOperationException()
        {
            // Arrange
            int height = -100;
            int rowSize = 100;

            // Act & Assert
            var ex = Assert.Throws<DjvuInvalidOperationException>(() => Util.CalculateBufferSize(height, rowSize));
            Assert.Contains("Calculated buffer size exceeds maximum allowed limit", ex.Message);
        }
    }
}
