using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace Vpay.Optum.Tests.Nc835Segments
{
    public class N1Tests
    {
        [Fact]
        public void Constructor_ValidData_InitializesProperties()
        {
            // Arrange
            var data = new[] { "N1", "EntityCode", "PayerName", "Qualifier", "Code" };

            // Act
            var n1 = new N1(data);

            // Assert
            Assert.Equal("N1", n1.N100);
            Assert.Equal("EntityCode", n1.N101);
            Assert.Equal("PayerName", n1.N102);
            Assert.Equal("Qualifier", n1.N103);
            Assert.Equal("Code", n1.N104);
        }

        [Fact]
        public void Constructor_ValidDataWithOptionalFields_InitializesProperties()
        {
            // Arrange
            var data = new[] { "N1", "EntityCode", "PayerName" };

            // Act
            var n1 = new N1(data);

            // Assert
            Assert.Equal("N1", n1.N100);
            Assert.Equal("EntityCode", n1.N101);
            Assert.Equal("PayerName", n1.N102);
            Assert.Equal("", n1.N103); // Optional field
            Assert.Equal("", n1.N104); // Optional field
        }

        [Fact]
        public void Constructor_InvalidData_ThrowsArgumentException()
        {
            // Arrange
            var data = new[] { "N1", "EntityCode" }; // Less than 3 elements

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new N1(data));
            Assert.Equal("Must have at least 3 strings in the array (Parameter 'data')", exception.Message);
        }

        [Fact]
        public void Properties_CanBeSetAfterInitialization()
        {
            // Arrange
            var data = new[] { "N1", "EntityCode", "PayerName" };
            var n1 = new N1(data);

            // Act
            n1.N100 = "NewN1";
            n1.N101 = "NewEntityCode";
            n1.N102 = "NewPayerName";
            n1.N103 = "NewQualifier";
            n1.N104 = "NewCode";

            // Assert
            Assert.Equal("NewN1", n1.N100);
            Assert.Equal("NewEntityCode", n1.N101);
            Assert.Equal("NewPayerName", n1.N102);
            Assert.Equal("NewQualifier", n1.N103);
            Assert.Equal("NewCode", n1.N104);
        }
    }
}
