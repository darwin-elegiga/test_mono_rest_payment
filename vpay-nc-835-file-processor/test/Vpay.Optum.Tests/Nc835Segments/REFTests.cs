using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace VPay.Optum.Tests.Nc835Segments
{
   
    public class REFTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesCorrectly_WhenValidInputProvided()
        {
            // Arrange
            string refRecord = "REF*EV*12345";
            string segmentChar = "*";

            // Act
            var refInstance = new REF(refRecord, segmentChar);

            // Assert
            Assert.Equal("REF",refInstance.REF00);
            Assert.Equal("EV", refInstance.REF01);
            Assert.Equal("12345", refInstance.REF02);
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenInputHasInsufficientSegments()
        {
            // Arrange
            string refRecord = "REF*EV";
            string segmentChar = "*";

            // Act

            // Assert is handled by the ExpectedException attribute
            var exception = Assert.Throws<IndexOutOfRangeException>(() => new REF(refRecord, segmentChar));

        }

        [Fact]
        public void Constructor_ShouldHandleEmptySegmentsCorrectly()
        {
            // Arrange
            string refRecord = "REF**";
            string segmentChar = "*";

            // Act
            var refInstance = new REF(refRecord, segmentChar);

            // Assert
            Assert.Equal("REF",refInstance.REF00);
            Assert.Equal("",refInstance.REF01);
            Assert.Equal("",refInstance.REF02);
        }
    }
}
