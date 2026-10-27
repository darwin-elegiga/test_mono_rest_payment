using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace Vpay.Optum.Tests.Nc835Segments
{
    public class GSTests
    {
        [Fact]
        public void Constructor_ValidData_AssignsPropertiesCorrectly()
        {
            // Arrange
            var data = new[]
            {
                "GS", "FI", "SenderCode", "ReceiverCode", "20250101", "1200", "123456", "X", "005010"
            };

            // Act
            var gs = new GS(data);

            // Assert
            Assert.Equal("GS", gs.GS00);
            Assert.Equal("FI", gs.GS01);
            Assert.Equal("SenderCode", gs.GS02);
            Assert.Equal("ReceiverCode", gs.GS03);
            Assert.Equal("20250101", gs.GS04);
            Assert.Equal("1200", gs.GS05);
            Assert.Equal("123456", gs.GS06);
            Assert.Equal("X", gs.GS07);
            Assert.Equal("005010", gs.GS08);
        }

        [Fact]
        public void Constructor_InvalidData_ThrowsArgumentException()
        {
            // Arrange
            var data = new[] { "GS", "FI", "SenderCode" }; // Less than 9 elements

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new GS(data));
            Assert.Equal("Must have at least 9 strings in the array (Parameter 'data')", exception.Message);
        }

        [Fact]
        public void Properties_CanBeSetAfterConstruction()
        {
            // Arrange
            var data = new[]
            {
                "GS", "FI", "SenderCode", "ReceiverCode", "20250101", "1200", "123456", "X", "005010"
            };
            var gs = new GS(data);

            // Act
            gs.GS00 = "NewGS";
            gs.GS01 = "NewFI";

            // Assert
            Assert.Equal("NewGS", gs.GS00);
            Assert.Equal("NewFI", gs.GS01);
        }
    }
}
