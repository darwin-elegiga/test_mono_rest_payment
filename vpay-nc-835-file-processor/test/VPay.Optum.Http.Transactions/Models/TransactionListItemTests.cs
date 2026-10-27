using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using VPay.Optum.Http.Transactions.Models;
using Xunit;

namespace VPay.Optum.Http.Transactions.Tests.Models
{
    public class TransactionListItemTests
    {
        [Fact]
        public void TransactionListItem_SerializesCorrectly()
        {
            // Arrange
            var transaction = new TransactionListItem
            {
                TransactionId = 12345,
                PayeeName = "John Doe",
                PayeeAddress1 = "123 Main St",
                PayeeAddress2 = "Apt 4B",
                PayeeAddress3 = "Building 5",
                PayeeCity = "New York",
                PayeeState = "NY",
                PayeeZipCode = "10001",
                LoadedDateTime = new DateTime(2025, 5, 4, 12, 0, 0),
                LoadAmount = 100.50m,
                TaxId = "123-45-6789",
                OriginCode = "ORG123",
                ClientCode = "CLT456",
                BillingEntityCode = "BIL789",
                PaymentTypeCode = "PMT001"
            };

            // Act
            var json = JsonSerializer.Serialize(transaction);

            // Assert
            Assert.Contains("\"SETXID\":12345", json);
            Assert.Contains("\"PAYEENAME\":\"John Doe\"", json);
        }

        [Fact]
        public void TransactionListItem_DeserializesCorrectly()
        {
            // Arrange
            var json = @"
            {
                ""SETXID"": 12345,
                ""PAYEENAME"": ""John Doe"",
                ""PAYADD1"": ""123 Main St"",
                ""PAYADD2"": ""Apt 4B"",
                ""PAYADD3"": ""Building 5"",
                ""PAYCOCITY"": ""New York"",
                ""PAYCOSTATE"": ""NY"",
                ""PAYPC"": ""10001"",
                ""LOADTS"": ""2025-05-04T12:00:00"",
                ""LOADAMOUNT"": 100.50,
                ""PTAXID"": ""123-45-6789"",
                ""ORGNAME"": ""ORG123"",
                ""TPA"": ""CLT456"",
                ""BILENT"": ""BIL789"",
                ""VPPMTTYPE"": ""PMT001""
            }";

            // Act
            var transaction = JsonSerializer.Deserialize<TransactionListItem>(json);

            // Assert
            Assert.NotNull(transaction);
            Assert.Equal(12345, transaction.TransactionId);
            Assert.Equal("John Doe", transaction.PayeeName);
            Assert.Equal("123 Main St", transaction.PayeeAddress1);
            Assert.Equal("Apt 4B", transaction.PayeeAddress2);
            Assert.Equal("Building 5", transaction.PayeeAddress3);
            Assert.Equal("New York", transaction.PayeeCity);
            Assert.Equal("NY", transaction.PayeeState);
            Assert.Equal("10001", transaction.PayeeZipCode);
            Assert.Equal(new DateTime(2025, 5, 4, 12, 0, 0), transaction.LoadedDateTime);
            Assert.Equal(100.50m, transaction.LoadAmount);
            Assert.Equal("123-45-6789", transaction.TaxId);
            Assert.Equal("ORG123", transaction.OriginCode);
            Assert.Equal("CLT456", transaction.ClientCode);
            Assert.Equal("BIL789", transaction.BillingEntityCode);
            Assert.Equal("PMT001", transaction.PaymentTypeCode);
        }
    }
}
