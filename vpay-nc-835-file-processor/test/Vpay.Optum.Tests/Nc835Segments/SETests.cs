using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace VPay.Optum.Nc835Segments.Tests
{
    public class SETests
    {
        [Fact]
        public void Constructor_ValidData_ShouldInitializeProperties()
        {
            // Arrange
            var data = new[] { "SE", "10", "12345" };

            // Act
            var se = new SE(data);

            // Assert
            Assert.Equal("SE", se.SE00);
            Assert.Equal("10", se.SE01);
            Assert.Equal("12345", se.SE02);
        }

        [Fact]
        public void Constructor_InvalidData_ThrowsArgumentException()
        {
            // Arrange
            var data = new[] { "SE", "10" }; // Less than 3 elements

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new SE(data));
            Assert.Equal("Must have at least 3 strings in the array (Parameter 'data')", exception.Message);
        }

        [Fact]
        public void Properties_ShouldAllowGetAndSet()
        {
            // Arrange
            var data = new[] { "SE", "10", "12345" };
            var se = new SE(data);

            // Act
            se.SE00 = "UpdatedSE";
            se.SE01 = "20";
            se.SE02 = "67890";

            // Assert
            Assert.Equal("UpdatedSE", se.SE00);
            Assert.Equal("20", se.SE01);
            Assert.Equal("67890", se.SE02);
        }
    }
}
