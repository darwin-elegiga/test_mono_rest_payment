using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace VPay.Optum.Tests.Nc835Segments
{
    public class IEATests
    {
        [Fact]
        public void Constructor_ValidData_ShouldInitializeProperties()
        {
            // Arrange
            var data = new[] { "IEA", "5", "12345" };

            // Act
            var iea = new IEA(data);

            // Assert
            Assert.Equal("IEA", iea.IEA00);
            Assert.Equal("5", iea.IEA01);
            Assert.Equal("12345", iea.IEA02);
        }

        [Fact]
        public void Constructor_InvalidData_ThrowsArgumentException()
        {
            // Arrange
            var data = new[] { "IEA", "5" }; // Less than 3 elements

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new IEA(data));
            Assert.Equal("Must have at least 3 strings in the array (Parameter 'data')", exception.Message);
        }

        [Fact]
        public void IEA00_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var data = new[] { "IEA", "5", "12345" };
            var iea = new IEA(data);

            // Act
            iea.IEA00 = "NEW";

            // Assert
            Assert.Equal("NEW", iea.IEA00);
        }

        [Fact]
        public void IEA01_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var data = new[] { "IEA", "5", "12345" };
            var iea = new IEA(data);

            // Act
            iea.IEA01 = "10";

            // Assert
            Assert.Equal("10", iea.IEA01);
        }

        [Fact]
        public void IEA02_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var data = new[] { "IEA", "5", "12345" };
            var iea = new IEA(data);

            // Act
            iea.IEA02 = "67890";

            // Assert
            Assert.Equal("67890", iea.IEA02);
        }
    }
}
