using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace Vpay.Optum.Tests.Nc835Segments
{
    public class DTMTests
    {
        [Fact]
        public void Constructor_ValidData_ShouldInitializeProperties()
        {
            // Arrange
            var data = new[] { "Value1", "Value2", "Value3" };

            // Act
            var dtm = new DTM(data);

            // Assert
            Assert.Equal("Value1", dtm.DTM00);
            Assert.Equal("Value2", dtm.DTM01);
            Assert.Equal("Value3", dtm.DTM02);
        }

        [Fact]
        public void Constructor_InvalidData_ShouldThrowArgumentException()
        {
            // Arrange
            var data = new[] { "Value1", "Value2" }; // Less than 3 elements

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new DTM(data));
            Assert.Equal("Must have at least 3 strings in the array (Parameter 'data')", exception.Message);
        }

        [Fact]
        public void DTM00_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var data = new[] { "Value1", "Value2", "Value3" };
            var dtm = new DTM(data);

            // Act
            dtm.DTM00 = "NewValue";

            // Assert
            Assert.Equal("NewValue", dtm.DTM00);
        }

        [Fact]
        public void DTM01_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var data = new[] { "Value1", "Value2", "Value3" };
            var dtm = new DTM(data);

            // Act
            dtm.DTM01 = "NewValue";

            // Assert
            Assert.Equal("NewValue", dtm.DTM01);
        }

        [Fact]
        public void DTM02_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var data = new[] { "Value1", "Value2", "Value3" };
            var dtm = new DTM(data);

            // Act
            dtm.DTM02 = "NewValue";

            // Assert
            Assert.Equal("NewValue", dtm.DTM02);
        }
    }
}
