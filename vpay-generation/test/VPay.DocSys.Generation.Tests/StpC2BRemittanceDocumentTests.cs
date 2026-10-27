using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Common.Models.Sql;
using VPay.DocSys.Generation.Models;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class StpC2BRemittanceDocumentTests
    {
        private const string PayeeParty = "Payee Name";
        private const string PayeePartyTooLong = "Payee Name with more than 30 characters";
        private const string PayeeAddress = "123 Payee St";
        private const string PayeeAddressTooLong = "123 Super Long Payee Address Street";
        private const string PayeeCity = "123 Payee St";
        private const string PayeeCityTooLong = "City Name with more than 30 characters";
        private const string PayeeState = "TX";
        private const string PayeeZip = "12345";
        private const string PatientName = "John Doe";
        private const string PatientDOB = "1980-01-01";
        private const string ClaimId = "C1234567890";
        private const string PatientAccountNumber = "PA1234567890";
        private const string Amount = "100.00";
        private const string ClaimDate = "2023-10-01";
        private const string SubscriberName = "Jane Doe";
        private const string ClientReferenceId = "123456789";
        private readonly string _trnSegment = $"TRN*1*{ClientReferenceId}*987654321";

        private const string ClaimIdTooLong = "C12345678901234567890";
        private const string ClientReferenceIdTooLong = "ClientReferenceId with more than 20 characters should be truncated";
        private readonly string _trnSegmentTooLong = $"TRN*1*{ClientReferenceIdTooLong}*987654321";

        private const string NetworkName = "Test Network";
        private const string NetworkUuid = "123e4567-e89b-12d3-a456-426614174000";

        public class Create : StpC2BRemittanceDocumentTests
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
                    PayeePostalCode = PayeeZip,
                    PatientName = PatientName,
                    PatientDOB = PatientDOB,
                    ClaimId = ClaimId,
                    PatientAccountNumber = PatientAccountNumber,
                    Amount = Amount,
                    ClaimDate = ClaimDate,
                    SubscriberName = SubscriberName,
                };

                long transactionId = 123456;
                string trnSegment = _trnSegment;
                var tpaAddress = new TpaAddress
                {
                    Address = "TPA Address Line 1",
                    City = "TPA City",
                    State = "TPA State",
                    Zip = "TPA Zip"
                };
                var document = new StpC2BRemittanceDocument();

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
                Assert.Equal(PatientName, document.Consumer.PatientName);
                Assert.Equal(PatientDOB, document.Consumer.PatientDOB);
                Assert.Equal(ClaimId, document.Consumer.ClaimId);
                Assert.Equal(PatientAccountNumber, document.Consumer.PatientAccountNumber);
                Assert.Equal(Amount, document.Consumer.Amount);
                Assert.Equal(ClaimDate, document.Consumer.ClaimDate);
                Assert.Equal(SubscriberName, document.Consumer.SubscriberName);
                Assert.Equal(ClientReferenceId, document.Consumer.ClientReferenceId);
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
                    PayeePostalCode = PayeeZip,
                    PatientName = PatientName,
                    PatientDOB = PatientDOB,
                    ClaimId = ClaimId,
                    PatientAccountNumber = PatientAccountNumber,
                    Amount = Amount,
                    ClaimDate = ClaimDate,
                    SubscriberName = SubscriberName,
                };

                long transactionId = 123456;
                string trnSegment = _trnSegment;
                var tpaAddress = new TpaAddress
                {
                    Address = "TPA Address Line 1",
                    City = "TPA City",
                    State = "TPA State",
                    Zip = "TPA Zip"
                };
                var document = new StpC2BRemittanceDocument();

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
                Assert.Equal(PatientName, document.Consumer.PatientName);
                Assert.Equal(PatientDOB, document.Consumer.PatientDOB);
                Assert.Equal(ClaimId, document.Consumer.ClaimId);
                Assert.Equal(PatientAccountNumber, document.Consumer.PatientAccountNumber);
                Assert.Equal(Amount, document.Consumer.Amount);
                Assert.Equal(ClaimDate, document.Consumer.ClaimDate);
                Assert.Equal(SubscriberName, document.Consumer.SubscriberName);
                Assert.Equal(ClientReferenceId, document.Consumer.ClientReferenceId);
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
                    PayeePostalCode = PayeeZip,
                    PatientName = PatientName,
                    PatientDOB = PatientDOB,
                    ClaimId = ClaimId,
                    PatientAccountNumber = PatientAccountNumber,
                    Amount = Amount,
                    ClaimDate = ClaimDate,
                    SubscriberName = SubscriberName,
                };

                long transactionId = 123456;
                string trnSegment = _trnSegment;
                var tpaAddress = new TpaAddress
                {
                    Address = "TPA Address Line 1",
                    City = "TPA City",
                    State = "TPA State",
                    Zip = "TPA Zip"
                };
                var document = new StpC2BRemittanceDocument();

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
                Assert.Equal(PatientName, document.Consumer.PatientName);
                Assert.Equal(PatientDOB, document.Consumer.PatientDOB);
                Assert.Equal(ClaimId, document.Consumer.ClaimId);
                Assert.Equal(PatientAccountNumber, document.Consumer.PatientAccountNumber);
                Assert.Equal(Amount, document.Consumer.Amount);
                Assert.Equal(ClaimDate, document.Consumer.ClaimDate);
                Assert.Equal(SubscriberName, document.Consumer.SubscriberName);
                Assert.Equal(ClientReferenceId, document.Consumer.ClientReferenceId);
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
                    PayeePostalCode = PayeeZip,
                    PatientName = PatientName,
                    PatientDOB = PatientDOB,
                    ClaimId = ClaimId,
                    PatientAccountNumber = PatientAccountNumber,
                    Amount = Amount,
                    ClaimDate = ClaimDate,
                    SubscriberName = SubscriberName,
                };

                long transactionId = 123456;
                string trnSegment = _trnSegment;
                var tpaAddress = new TpaAddress
                {
                    Address = "TPA Address Line 1",
                    City = "TPA City",
                    State = "TPA State",
                    Zip = "TPA Zip"
                };
                var document = new StpC2BRemittanceDocument();

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
                Assert.Equal(PatientName, document.Consumer.PatientName);
                Assert.Equal(PatientDOB, document.Consumer.PatientDOB);
                Assert.Equal(ClaimId, document.Consumer.ClaimId);
                Assert.Equal(PatientAccountNumber, document.Consumer.PatientAccountNumber);
                Assert.Equal(Amount, document.Consumer.Amount);
                Assert.Equal(ClaimDate, document.Consumer.ClaimDate);
                Assert.Equal(SubscriberName, document.Consumer.SubscriberName);
                Assert.Equal(ClientReferenceId, document.Consumer.ClientReferenceId);
                Assert.Equal(NetworkName, document.Header.NetworkPartner);
                Assert.Equal(transactionId.ToString(), document.Header.OptumTransactionId);
            }

            [Fact]
            public void WhenEobHasLongClaimId_ShouldCreateDocumentWithTruncatedClaimId()
            {
                // Arrange
                var eob = new EobXsl
                {
                    PayeeName = PayeeParty,
                    MainGovtIdNumber = "123456789",
                    PayeeAddress1 = PayeeAddress,
                    PayeeCity = PayeeCity,
                    PayeeState = PayeeState,
                    PayeePostalCode = PayeeZip,
                    PatientName = PatientName,
                    PatientDOB = PatientDOB,
                    ClaimId = ClaimIdTooLong,
                    PatientAccountNumber = PatientAccountNumber,
                    Amount = Amount,
                    ClaimDate = ClaimDate,
                    SubscriberName = SubscriberName,
                };

                long transactionId = 123456;
                string trnSegment = _trnSegment;
                var tpaAddress = new TpaAddress
                {
                    Address = "TPA Address Line 1",
                    City = "TPA City",
                    State = "TPA State",
                    Zip = "TPA Zip"
                };
                var document = new StpC2BRemittanceDocument();

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
                Assert.Equal(PatientName, document.Consumer.PatientName);
                Assert.Equal(PatientDOB, document.Consumer.PatientDOB);
                Assert.Equal(ClaimIdTooLong.TruncateAndTrim(20), document.Consumer.ClaimId);
                Assert.Equal(PatientAccountNumber, document.Consumer.PatientAccountNumber);
                Assert.Equal(Amount, document.Consumer.Amount);
                Assert.Equal(ClaimDate, document.Consumer.ClaimDate);
                Assert.Equal(SubscriberName, document.Consumer.SubscriberName);
                Assert.Equal(ClientReferenceId, document.Consumer.ClientReferenceId);
                Assert.Equal(NetworkName, document.Header.NetworkPartner);
                Assert.Equal(transactionId.ToString(), document.Header.OptumTransactionId);
            }

            [Fact]
            public void WhenEobHasLongClientReferenceId_ShouldCreateDocumentWithTruncatedClientReferenceId()
            {
                // Arrange
                var eob = new EobXsl
                {
                    PayeeName = PayeeParty,
                    MainGovtIdNumber = "123456789",
                    PayeeAddress1 = PayeeAddress,
                    PayeeCity = PayeeCity,
                    PayeeState = PayeeState,
                    PayeePostalCode = PayeeZip,
                    PatientName = PatientName,
                    PatientDOB = PatientDOB,
                    ClaimId = ClaimId,
                    PatientAccountNumber = PatientAccountNumber,
                    Amount = Amount,
                    ClaimDate = ClaimDate,
                    SubscriberName = SubscriberName,
                };

                long transactionId = 123456;
                string trnSegment = _trnSegmentTooLong;
                var tpaAddress = new TpaAddress
                {
                    Address = "TPA Address Line 1",
                    City = "TPA City",
                    State = "TPA State",
                    Zip = "TPA Zip"
                };
                var document = new StpC2BRemittanceDocument();

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
                Assert.Equal(PatientName, document.Consumer.PatientName);
                Assert.Equal(PatientDOB, document.Consumer.PatientDOB);
                Assert.Equal(ClaimId, document.Consumer.ClaimId);
                Assert.Equal(PatientAccountNumber, document.Consumer.PatientAccountNumber);
                Assert.Equal(Amount, document.Consumer.Amount);
                Assert.Equal(ClaimDate, document.Consumer.ClaimDate);
                Assert.Equal(SubscriberName, document.Consumer.SubscriberName);
                Assert.Equal(ClientReferenceIdTooLong.TruncateAndTrim(20), document.Consumer.ClientReferenceId);
                Assert.Equal(NetworkName, document.Header.NetworkPartner);
                Assert.Equal(transactionId.ToString(), document.Header.OptumTransactionId);
            }
        }

        public class ToJsonString : StpC2BRemittanceDocumentTests
        {
            [Fact]
            public void ToJsonString_ShouldReturnFormattedJson()
            {
                // Arrange
                var eob = new EobXsl
                {
                    PayeeName = PayeeParty,
                    MainGovtIdNumber = "123456789",
                    PayeeAddress1 = PayeeAddress,
                    PayeeCity = PayeeCity,
                    PayeeState = PayeeState,
                    PayeePostalCode = PayeeZip,
                    PatientName = PatientName,
                    PatientDOB = PatientDOB,
                    ClaimId = ClaimId,
                    PatientAccountNumber = PatientAccountNumber,
                    Amount = Amount,
                    ClaimDate = ClaimDate,
                    SubscriberName = SubscriberName,
                };
                var networkName = "Test Network";
                long transactionId = 123456;
                string trnSegment = _trnSegment;
                var tpaAddress = new TpaAddress
                {
                    Address = "TPA Address Line 1",
                    City = "TPA City",
                    State = "TPA State",
                    Zip = "TPA Zip"
                };
                var document = new StpC2BRemittanceDocument();
                document.Create(eob, networkName, transactionId, trnSegment, tpaAddress, NetworkUuid);
                // Act
                var jsonString = document.ToJsonString();
                // Assert
                Assert.NotNull(jsonString);
                Assert.Contains("\"networkPartner\": \"Test Network\"", jsonString);
                Assert.Contains("\"optumTransactionId\": \"123456\"", jsonString);
            }
        }

        public class GetClientReferenceIdFromTrnSegment : StpC2BRemittanceDocumentTests
        {
            [Fact]
            public void WhenTrnSegmentIsValid_ShouldReturnClientReferenceId()
            {
                // Arrange
                var trnSegment = "TRN*1*123456789*987654321";
                // Act
                var clientReferenceId = StpC2BRemittanceDocument.GetClientReferenceIdFromTrnSegment(trnSegment);
                // Assert
                Assert.Equal("123456789", clientReferenceId);
            }

            [Fact]
            public void WhenTrnSegmentIsInvalid_ShouldReturnEmptyString()
            {
                // Arrange
                var trnSegment = "TRN*1";
                // Act
                var clientReferenceId = StpC2BRemittanceDocument.GetClientReferenceIdFromTrnSegment(trnSegment);
                // Assert
                Assert.Equal("", clientReferenceId);
            }
        }

        public class GetFormattedDateCust : StpC2BRemittanceDocumentTests
        {
            [Fact]
            public void WhenDateContainsHyphen_ShouldReturnFormattedDate()
            {
                // Arrange
                var date = "2023-10-01";
                // Act
                var formattedDate = StpC2BRemittanceDocument.GetFormattedDateCust(date);
                // Assert
                Assert.Equal("2023-10-01", formattedDate);
            }

            [Fact]
            public void WhenDateContainsHyphenAndFormatIsAmbiguous_ShouldReturnFormattedDate()
            {
                // Arrange
                var date = "2023-06-01";  // YYYY-MM-DD or YYYY-DD-MM ???
                // Act
                var formattedDate = StpC2BRemittanceDocument.GetFormattedDateCust(date);
                // Assert
                Assert.Equal("2023-06-01", formattedDate);
            }

            [Fact]
            public void WhenDateContainsHyphenAndFormatIsInvalid_ShouldReturnOriginalDate()
            {
                // Arrange
                var date = "2023-20-01";  // YYYY-DD-MM ???
                // Act
                var formattedDate = StpC2BRemittanceDocument.GetFormattedDateCust(date);
                // Assert
                Assert.Equal("2023-20-01", formattedDate);
            }

            [Fact]
            public void WhenDateDoesNotContainHyphen_ShouldReturnOriginalDate()
            {
                // Arrange
                var date = "20231001";
                // Act
                var formattedDate = StpC2BRemittanceDocument.GetFormattedDateCust(date);
                // Assert
                Assert.Equal("2023-10-01", formattedDate);
            }

            [Fact]
            public void WhenDateDoesNotContainHyphenAndFormatIsInvalid_ShouldReturnEmpty()
            {
                // Arrange
                var date = "20232001";
                // Act
                var formattedDate = StpC2BRemittanceDocument.GetFormattedDateCust(date);
                // Assert
                Assert.Equal("", formattedDate);
            }
        }
    }
}
