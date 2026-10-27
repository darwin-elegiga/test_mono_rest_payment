using System.Collections.Generic;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace VPay.Optum.Nc835Segments.Tests
{
    public class Loop1000ATests
    {
        private static readonly string[] PayerIdentificationData = { "N1", "PR", "Payer Name", "XX", "12345" };
        private static readonly string[] PayerAddressData = { "N3", "123 Main St" };
        private static readonly string[] PayerCityStateZipData = { "N4", "City", "ST", "12345", "US" };

        [Fact]
        public void PayerIdentification_Should_SetAndGetCorrectly()
        {
            // Arrange
            var loop1000A = new Loop1000A();
            var payerIdentification = new N1(PayerIdentificationData);

            // Act
            loop1000A.PayerIdentification = payerIdentification;

            // Assert
            Assert.Equal(payerIdentification, loop1000A.PayerIdentification);
        }

        [Fact]
        public void PayerAddress_Should_SetAndGetCorrectly()
        {
            // Arrange
            var loop1000A = new Loop1000A();
            var payerAddress = new N3(PayerAddressData);

            // Act
            loop1000A.PayerAddress = payerAddress;

            // Assert
            Assert.Equal(payerAddress, loop1000A.PayerAddress);
        }

        [Fact]
        public void PayerCityStateZip_Should_SetAndGetCorrectly()
        {
            // Arrange
            var loop1000A = new Loop1000A();
            var payerCityStateZip = new N4(PayerCityStateZipData);

            // Act
            loop1000A.PayerCityStateZip = payerCityStateZip;

            // Assert
            Assert.Equal(payerCityStateZip, loop1000A.PayerCityStateZip);
        }

        [Fact]
        public void Ref2U_Should_SetAndGetCorrectly()
        {
            // Arrange
            var loop1000A = new Loop1000A();
            var ref2U = new REF("REF*2U*12345", "*");

            // Act
            loop1000A.Ref2U = ref2U;

            // Assert
            Assert.Equal(ref2U, loop1000A.Ref2U);
        }

        [Fact]
        public void PerBL_Should_AddAndRetrieveItemsCorrectly()
        {
            // Arrange
            var loop1000A = new Loop1000A();
            var perBL = new PER("PER*BL*Contact*Name*TE*1234567890*test*data", "*");

            // Act
            loop1000A.PerBL.Add(perBL);

            // Assert
            Assert.Contains(perBL, loop1000A.PerBL);
        }

        [Fact]
        public void PerIC_Should_SetAndGetCorrectly()
        {
            // Arrange
            var loop1000A = new Loop1000A();
            var perIC = new PER("PER*IC*Contact*Name*TE*1234567890*test*data", "*");

            // Act
            loop1000A.PerIC = perIC;

            // Assert
            Assert.Equal(perIC, loop1000A.PerIC);
        }
    }
}
