using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace Vpay.Optum.Tests.Nc835Segments
{
    public class STTests
    {
        [Fact]
        public void Constructor_ValidData_InitializesProperties()
        {
            // Arrange
            var data = new[] { "ST", "123", "456" };

            // Act
            var st = new ST(data);

            // Assert
            Assert.Equal("ST", st.ST00);
            Assert.Equal("123", st.ST01);
            Assert.Equal("456", st.ST02);
        }

        [Fact]
        public void Constructor_InvalidData_ThrowsArgumentException()
        {
            // Arrange
            var data = new[] { "ST", "123" }; // Less than 3 elements

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ST(data));
            Assert.Equal("Must have at least 3 strings in the array (Parameter 'data')", exception.Message);
        }

        [Fact]
        public void Properties_CanBeSetAfterInitialization()
        {
            // Arrange
            var data = new[] { "ST", "123", "456" };
            var st = new ST(data);

            // Act
            st.ST00 = "NEW_ST";
            st.ST01 = "789";
            st.ST02 = "101112";

            // Assert
            Assert.Equal("NEW_ST", st.ST00);
            Assert.Equal("789", st.ST01);
            Assert.Equal("101112", st.ST02);
        }
    }
}
