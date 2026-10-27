using System.Collections.Generic;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace VPay.Optum.Tests.Nc835Segments
{
    public class Loop1000BTests
    {
        private static readonly string[] PayeeIdentificationData = { "N1", "PE", "Payee Name", "XX", "123456789" };
        private static readonly string[] PayeeAddressData = { "N3", "123 Main St" };
        private static readonly string[] PayeeCityStateZipData = { "N4", "City", "ST", "12345" };

        [Fact]
        public void PayeeIdentification_Should_SetAndGetCorrectly()
        {
            // Arrange
            var loop1000B = new Loop1000B();
            var payeeIdentification = new N1(PayeeIdentificationData);

            // Act
            loop1000B.PayeeIdentification = payeeIdentification;

            // Assert
            Assert.Equal(payeeIdentification, loop1000B.PayeeIdentification);
        }

        [Fact]
        public void PayeeAddress_Should_SetAndGetCorrectly()
        {
            // Arrange
            var loop1000B = new Loop1000B();
            var payeeAddress = new N3(PayeeAddressData);

            // Act
            loop1000B.PayeeAddress = payeeAddress;

            // Assert
            Assert.Equal(payeeAddress, loop1000B.PayeeAddress);
        }

        [Fact]
        public void PayeeCityStateZip_Should_SetAndGetCorrectly()
        {
            // Arrange
            var loop1000B = new Loop1000B();
            var payeeCityStateZip = new N4(PayeeCityStateZipData);

            // Act
            loop1000B.PayeeCityStateZip = payeeCityStateZip;

            // Assert
            Assert.Equal(payeeCityStateZip, loop1000B.PayeeCityStateZip);
        }

        [Fact]
        public void Refs_Should_HaveDefaultEmptyList()
        {
            // Arrange
            var loop1000B = new Loop1000B();

            // Assert
            Assert.NotNull(loop1000B.Refs);
            Assert.Empty(loop1000B.Refs);
        }

        [Fact]
        public void Refs_Should_AddItemsCorrectly()
        {
            // Arrange
            var loop1000B = new Loop1000B();
            var reference = new REF("REF*EV*12345", "*");

            // Act
            loop1000B.Refs.Add(reference);

            // Assert
            Assert.Single(loop1000B.Refs);
            Assert.Contains(reference, loop1000B.Refs);
        }

        [Fact]
        public void RemittanceDeliveryMethod_Should_SetAndGetCorrectly()
        {
            // Arrange
            var loop1000B = new Loop1000B();
            var remittanceDeliveryMethod = new RDM("RDM*01*02", "*");

            // Act
            loop1000B.RemittanceDeliveryMethod = remittanceDeliveryMethod;

            // Assert
            Assert.Equal(remittanceDeliveryMethod, loop1000B.RemittanceDeliveryMethod);
        }

        [Fact]
        public void PerBL_Should_HaveDefaultEmptyList()
        {
            // Arrange
            var loop1000B = new Loop1000B();

            // Assert
            Assert.NotNull(loop1000B.PerBL);
            Assert.Empty(loop1000B.PerBL);
        }

        [Fact]
        public void PerBL_Should_AddItemsCorrectly()
        {
            // Arrange
            var loop1000B = new Loop1000B();
            var per = new PER("PER*CX*Contact*Name*TE*1234567890*test*data", "*");

            // Act
            loop1000B.PerBL.Add(per);

            // Assert
            Assert.Single(loop1000B.PerBL);
            Assert.Contains(per, loop1000B.PerBL);
        }

        [Fact]
        public void PerIC_Should_SetAndGetCorrectly()
        {
            // Arrange
            var loop1000B = new Loop1000B();
            var perIC = new PER("PER*IC*Contact*Name*TE*1234567890*test*data", "*");

            // Act
            loop1000B.PerIC = perIC;

            // Assert
            Assert.Equal(perIC, loop1000B.PerIC);
        }
    }
}
