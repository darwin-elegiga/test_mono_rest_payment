using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace VPay.Optum.Tests.Nc835Segments
{
    public class GETests
    {
        [Fact]
        public void Constructor_ValidData_InitializesProperties()
        {
            // Arrange
            var data = new[] { "GE", "5", "12345" };

            // Act
            var ge = new GE(data);

            // Assert
            Assert.Equal("GE", ge.GE00);
            Assert.Equal("5", ge.GE01);
            Assert.Equal("12345", ge.GE02);
        }

        [Fact]
        public void Constructor_InvalidData_ThrowsArgumentException()
        {
            // Arrange
            var data = new[] { "GE", "5" }; // Less than 3 elements

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new GE(data));
            Assert.Equal("Must have at least 3 strings in the array (Parameter 'data')", exception.Message);
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var data = new[] { "GE", "5", "12345" };
            var ge = new GE(data);

            // Act
            ge.GE00 = "UpdatedGE";
            ge.GE01 = "10";
            ge.GE02 = "67890";

            // Assert
            Assert.Equal("UpdatedGE", ge.GE00);
            Assert.Equal("10", ge.GE01);
            Assert.Equal("67890", ge.GE02);
        }
    }
}
