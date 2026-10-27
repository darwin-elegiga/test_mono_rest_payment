using System;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace Vpay.Optum.Tests.Nc835Segments
{
    public class TRNTests
    {
        [Fact]
        public void Constructor_ValidData_ShouldInitializeProperties()
        {
            // Arrange  
            var data = new[] { "TRN", "01", "12345", "CompanyID", "SupplementalID" };

            // Act  
            var trn = new TRN(data);

            // Assert  
            Assert.Equal("TRN", trn.TRN00);
            Assert.Equal("01", trn.TRN01);
            Assert.Equal("12345", trn.TRN02);
            Assert.Equal("CompanyID", trn.TRN03);
            Assert.Equal("SupplementalID", trn.TRN04);
        }

        [Fact]
        public void Constructor_InvalidData_ShouldThrowArgumentException()
        {
            // Arrange  
            var data = new[] { "TRN", "01", "12345" }; // Less than 5 elements  

            // Act & Assert  
            var exception = Assert.Throws<ArgumentException>(() => new TRN(data));
            Assert.Equal("Must have at least 5 strings in the array (Parameter 'data')", exception.Message);
        }

        [Fact]
        public void Constructor_NullData_ShouldThrowArgumentNullException()
        {
            // Act & Assert  
            Assert.Throws<NullReferenceException>(() => new TRN(null!));
        }
    }
}
