using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace Vpay.Optum.Tests.Nc835Segments
{
    public class N4Tests
    {
        [Fact]
        public void Constructor_ShouldAssignProperties_WhenValidDataProvided()
        {
            // Arrange
            var data = new[] { "N4", "CityName", "StateCode", "PostalCode", "CountryCode", "LocationQualifier", "LocationIdentifier", "CountrySubdivisionCode" };

            // Act
            var n4 = new N4(data);

            // Assert
            Assert.Equal("N4", n4.N400);
            Assert.Equal("CityName", n4.N401);
            Assert.Equal("StateCode", n4.N402);
            Assert.Equal("PostalCode", n4.N403);
            Assert.Equal("CountryCode", n4.N404);
            Assert.Equal("LocationQualifier", n4.N405);
            Assert.Equal("LocationIdentifier", n4.N406);
            Assert.Equal("CountrySubdivisionCode", n4.N407);
        }

        [Fact]
        public void Constructor_ShouldHandleMissingOptionalFields()
        {
            // Arrange
            var data = new[] { "N4", "CityName" };

            // Act
            var n4 = new N4(data);

            // Assert
            Assert.Equal("N4", n4.N400);
            Assert.Equal("CityName", n4.N401);
            Assert.Equal("", n4.N402);
            Assert.Equal("", n4.N403);
            Assert.Equal("", n4.N404);
            Assert.Equal("", n4.N405);
            Assert.Equal("", n4.N406);
            Assert.Equal("", n4.N407);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentException_WhenDataArrayIsEmpty()
        {
            // Arrange
            var data = Array.Empty<string>();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new N4(data));
            Assert.Equal("Must have at least 1 strings in the array (Parameter 'data')", exception.Message);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentException_WhenDataIsNull()
        {
            // Arrange
            string[] data = null!;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => new N4(data!));
        }
    }
}
