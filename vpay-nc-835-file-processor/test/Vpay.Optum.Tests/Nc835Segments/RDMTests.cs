using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace VPay.Optum.Tests.Nc835Segments
{
    public class RDMTests
    {
        [Fact]
        public void Constructor_ValidInput_SetsPropertiesCorrectly()
        {
            // Arrange
            string record = "Value1|Value2|Value3";
            string segmentChar = "|";

            // Act
            var rdm = new RDM(record, segmentChar);

            // Assert
            Assert.Equal("Value1", rdm.RDM00);
            Assert.Equal("Value2", rdm.RDM01);
            Assert.Equal("Value3", rdm.RDM02);
        }

        [Fact]
        public void Constructor_InvalidInput_ThrowsArgumentException()
        {
            // Arrange
            string record = "Value1|Value2"; // Less than 3 segments
            string segmentChar = "|";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new RDM(record, segmentChar));
            Assert.Equal("Must have at least 3 strings in the array (Parameter 'arr')", exception.Message);
        }

        [Fact]
        public void Constructor_EmptyRecord_ThrowsArgumentException()
        {
            // Arrange
            string record = "";
            string segmentChar = "|";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new RDM(record, segmentChar));
            Assert.Equal("Must have at least 3 strings in the array (Parameter 'arr')", exception.Message);
        }

        [Fact]
        public void Constructor_NullRecord_ThrowsArgumentNullException()
        {
            // Arrange
            string record = null!;
            string segmentChar = "|";

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => new RDM(record, segmentChar));
        }
    }
}
