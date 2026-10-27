using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Common.Models.Sql;
using VPay.DocSys.Generation.Models;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class StpB2BRemittanceDocumentTests
    {
        private const string PayeeParty = "Payee Name";
        private const string PayeePartyTooLong = "Payee Name with more than 30 characters";
        private const string PayeeAddress = "123 Payee St";
        private const string PayeeAddressTooLong = "123 Super Long Payee Address Street";
        private const string PayeeCity = "123 Payee St";
        private const string PayeeCityTooLong = "City Name with more than 30 characters";
        private const string PayeeState = "TX";
        private const string PayeeZip = "12345";

        private const string NetworkName = "Test Network";
        private const string NetworkUuid = "123e4567-e89b-12d3-a456-426614174000";

        public class Create : StpB2BRemittanceDocumentTests
        {
            [Fact]
            public void WhenEobHasValidFields_ShouldCreateDocument()
            {
                // Arrange
                var eob = new EobXsl
                {
                    PayeeName = PayeeParty,
                    MainGovtIdNumber = "123456789",
                    PayeeAddress1 = PayeeAddress,
                    PayeeCity = PayeeCity,
                    PayeeState = PayeeState,
                    PayeePostalCode = PayeeZip
                };

                long transactionId = 123456;
                string trnSegment = "TRN*1*123456789*987654321";
                var tpaAddress = new TpaAddress
                {
                    Address = "TPA Address Line 1",
                    City = "TPA City",
                    State = "TPA State",
                    Zip = "TPA Zip"
                };
                var document = new StpB2BRemittanceDocument();

                // Act
                document.Create(eob, NetworkName, transactionId, trnSegment, tpaAddress, NetworkUuid);

                // Assert
                Assert.NotNull(document.Header);
                Assert.Equal(PayeeParty, document.Payee.Name);
                Assert.Equal(PayeeAddress, document.Payee.AddressLine1);
                Assert.Equal(PayeeCity, document.Payee.City);
                Assert.Equal(PayeeState, document.Payee.State);
                Assert.Equal(PayeeZip, document.Payee.Zip);
                Assert.Equal(NetworkUuid, document.Payee.Uuid);
                Assert.Equal(NetworkName, document.Header.NetworkPartner);
                Assert.Equal(transactionId.ToString(), document.Header.OptumTransactionId);
            }

            [Fact]
            public void WhenEobHasLongPayeeName_ShouldCreateDocumentWithTruncatedPayeeName()
            {
                // Arrange
                var eob = new EobXsl
                {
                    PayeeName = PayeePartyTooLong,
                    MainGovtIdNumber = "123456789",
                    PayeeAddress1 = PayeeAddress,
                    PayeeCity = PayeeCity,
                    PayeeState = PayeeState,
                    PayeePostalCode = PayeeZip
                };

                long transactionId = 123456;
                string trnSegment = "TRN*1*123456789*987654321";
                var tpaAddress = new TpaAddress
                {
                    Address = "TPA Address Line 1",
                    City = "TPA City",
                    State = "TPA State",
                    Zip = "TPA Zip"
                };
                var document = new StpB2BRemittanceDocument();

                // Act
                document.Create(eob, NetworkName, transactionId, trnSegment, tpaAddress, NetworkUuid);

                // Assert
                Assert.NotNull(document.Header);
                Assert.Equal(PayeePartyTooLong.TruncateAndTrim(30), document.Payee.Name);
                Assert.Equal(PayeeAddress, document.Payee.AddressLine1);
                Assert.Equal(PayeeCity, document.Payee.City);
                Assert.Equal(PayeeState, document.Payee.State);
                Assert.Equal(PayeeZip, document.Payee.Zip);
                Assert.Equal(NetworkUuid, document.Payee.Uuid);
                Assert.Equal(NetworkName, document.Header.NetworkPartner);
                Assert.Equal(transactionId.ToString(), document.Header.OptumTransactionId);
            }

            [Fact]
            public void WhenEobHasLongPayeeAddressLine_ShouldCreateDocumentWithTruncatedPayeeAddressLine()
            {
                // Arrange
                var eob = new EobXsl
                {
                    PayeeName = PayeeParty,
                    MainGovtIdNumber = "123456789",
                    PayeeAddress1 = PayeeAddressTooLong,
                    PayeeCity = PayeeCity,
                    PayeeState = PayeeState,
                    PayeePostalCode = PayeeZip
                };

                long transactionId = 123456;
                string trnSegment = "TRN*1*123456789*987654321";
                var tpaAddress = new TpaAddress
                {
                    Address = "TPA Address Line 1",
                    City = "TPA City",
                    State = "TPA State",
                    Zip = "TPA Zip"
                };
                var document = new StpB2BRemittanceDocument();

                // Act
                document.Create(eob, NetworkName, transactionId, trnSegment, tpaAddress, NetworkUuid);

                // Assert
                Assert.NotNull(document.Header);
                Assert.Equal(PayeeParty, document.Payee.Name);
                Assert.Equal(PayeeAddressTooLong.TruncateAndTrim(30), document.Payee.AddressLine1);
                Assert.Equal(PayeeCity, document.Payee.City);
                Assert.Equal(PayeeState, document.Payee.State);
                Assert.Equal(PayeeZip, document.Payee.Zip);
                Assert.Equal(NetworkUuid, document.Payee.Uuid);
                Assert.Equal(NetworkName, document.Header.NetworkPartner);
                Assert.Equal(transactionId.ToString(), document.Header.OptumTransactionId);
            }

            [Fact]
            public void WhenEobHasLongPayeeCity_ShouldCreateDocumentWithTruncatedPayeeCity()
            {
                // Arrange
                var eob = new EobXsl
                {
                    PayeeName = PayeeParty,
                    MainGovtIdNumber = "123456789",
                    PayeeAddress1 = PayeeAddress,
                    PayeeCity = PayeeCityTooLong,
                    PayeeState = PayeeState,
                    PayeePostalCode = PayeeZip
                };

                long transactionId = 123456;
                string trnSegment = "TRN*1*123456789*987654321";
                var tpaAddress = new TpaAddress
                {
                    Address = "TPA Address Line 1",
                    City = "TPA City",
                    State = "TPA State",
                    Zip = "TPA Zip"
                };
                var document = new StpB2BRemittanceDocument();

                // Act
                document.Create(eob, NetworkName, transactionId, trnSegment, tpaAddress, NetworkUuid);

                // Assert
                Assert.NotNull(document.Header);
                Assert.Equal(PayeeParty, document.Payee.Name);
                Assert.Equal(PayeeAddress, document.Payee.AddressLine1);
                Assert.Equal(PayeeCityTooLong.TruncateAndTrim(30), document.Payee.City);
                Assert.Equal(PayeeState, document.Payee.State);
                Assert.Equal(PayeeZip, document.Payee.Zip);
                Assert.Equal(NetworkUuid, document.Payee.Uuid);
                Assert.Equal(NetworkName, document.Header.NetworkPartner);
                Assert.Equal(transactionId.ToString(), document.Header.OptumTransactionId);
            }
        }
    }
}
