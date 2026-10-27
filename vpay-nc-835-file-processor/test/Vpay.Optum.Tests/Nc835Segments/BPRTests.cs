using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace VPay.Optum.Tests.Nc835Segments
{
    public class BPRTests
    {
        [Fact]
        public void Constructor_ShouldThrowArgumentException_WhenDataArrayIsTooShort()
        {
            // Arrange
            var data = new string[16]; // Less than 17 elements

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new BPR(data));
            Assert.Equal("Must have at least 17 strings in the array (Parameter 'data')", exception.Message);
        }

        [Fact]
        public void Constructor_ShouldInitializeProperties_WhenDataArrayIsValid()
        {
            // Arrange
            var data = new string[]
            {
                "BPR00", "BPR01", "BPR02", "BPR03", "BPR04", "BPR05", "BPR06", "BPR07", "BPR08",
                "BPR09", "BPR10", "BPR11", "BPR12", "BPR13", "BPR14", "BPR15", "BPR16"
            };

            // Act
            var bpr = new BPR(data);

            // Assert
            Assert.Equal("BPR00", bpr.BPR00);
            Assert.Equal("BPR01", bpr.BPR01);
            Assert.Equal("BPR02", bpr.BPR02);
            Assert.Equal("BPR03", bpr.BPR03);
            Assert.Equal("BPR04", bpr.BPR04);
            Assert.Equal("BPR05", bpr.BPR05);
            Assert.Equal("BPR06", bpr.BPR06);
            Assert.Equal("BPR07", bpr.BPR07);
            Assert.Equal("BPR08", bpr.BPR08);
            Assert.Equal("BPR09", bpr.BPR09);
            Assert.Equal("BPR10", bpr.BPR10);
            Assert.Equal("BPR11", bpr.BPR11);
            Assert.Equal("BPR12", bpr.BPR12);
            Assert.Equal("BPR13", bpr.BPR13);
            Assert.Equal("BPR14", bpr.BPR14);
            Assert.Equal("BPR15", bpr.BPR15);
            Assert.Equal("BPR16", bpr.BPR16);
        }
    }
}
