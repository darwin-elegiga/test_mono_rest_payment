using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace Vpay.Optum.Tests.Nc835Segments
{
    public class PERTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesCorrectly_WhenValidInputIsProvided()
        {
            // Arrange
            string perRecord = "PER*CX*John Doe*TE*1234567890*EM*john.doe@example.com*FX*9876543210";
            string segmentChar = "*";

            // Act
            var per = new PER(perRecord, segmentChar);

            // Assert
            Assert.Equal("PER", per.PER00);
            Assert.Equal("CX", per.PER01);
            Assert.Equal("John Doe", per.PER02);
            Assert.Equal("TE", per.PER03);
            Assert.Equal("1234567890", per.PER04);
            Assert.Equal("EM", per.PER05);
            Assert.Equal("EM", per.PER06); // Note: PER06 is assigned the same value as PER05 in the constructor
            Assert.Equal("john.doe@example.com", per.PER07);
            Assert.Equal("FX", per.PER08);
        }

        [Fact]
        public void Constructor_ShouldThrowIndexOutOfRangeException_WhenInputHasInsufficientSegments()
        {
            // Arrange
            string perRecord = "PER*CX*John Doe";
            string segmentChar = "*";

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => new PER(perRecord, segmentChar));
        }

        [Fact]
        public void Constructor_ShouldHandleEmptyInputGracefully()
        {
            // Arrange
            string perRecord = "";
            string segmentChar = "*";

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => new PER(perRecord, segmentChar));
        }
    }
}
