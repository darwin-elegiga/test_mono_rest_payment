using System.Collections.Generic;

using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class FaxMessageCorrespondencePayloadTests
    {
        public class CreateErrorPayload
        {
            [Fact]
            public void ShouldCreateCorrectDefaultValues()
            {
                var actual = new FaxMessageCorrespondencePayload();
                var expected = " ";

                Assert.Equal(expected, actual.CorrespondenceId);
                Assert.Equal(expected, actual.FaxNumber);
                Assert.Equal(expected, actual.IpAddress);
                Assert.Equal(expected, actual.JobId);
                Assert.Equal(expected, actual.Port);
                Assert.Equal(expected, actual.ReasonCode);
                Assert.Equal(expected, actual.ReasonText);
                Assert.Equal(expected, actual.RetryCount);
                Assert.Equal(expected, actual.Status);
                Assert.Equal(new string[] { " " }, actual.TransactionIds);
            }

            [Fact]
            public void ShouldGetDefaultValuesForNullValues()
            {
                var actual = FaxMessageCorrespondencePayload.CreateErrorPayload(null);
                var expected = " ";

                Assert.Equal(expected, actual.CorrespondenceId);
                Assert.Equal(expected, actual.FaxNumber);
                Assert.Equal(expected, actual.IpAddress);
                Assert.Equal(expected, actual.JobId);
                Assert.Equal(expected, actual.Port);
                Assert.Equal(expected, actual.ReasonCode);
                Assert.Equal(expected, actual.ReasonText);
                Assert.Equal(expected, actual.RetryCount);
                Assert.Equal("PARSE_ERR", actual.Status);
                Assert.Equal(new string[] { " " }, actual.TransactionIds);
            }

            [Fact]
            public void ShouldGetCorrectValueForDictionaryKeyTransactionId()
            {
                var result = FaxMessageCorrespondencePayload.CreateErrorPayload(new Dictionary<string, object> { { "tids", "123" } });

                Assert.Equal(new[] { "123" }, result.TransactionIds);
            }

            [Fact]
            public void ShouldGetCorrectValueForDictionaryKeyTransactionIdWhenEmptyString()
            {
                var result = FaxMessageCorrespondencePayload.CreateErrorPayload(new Dictionary<string, object> { { "tids", "" } });

                Assert.Equal(new[] { " " }, result.TransactionIds);
            }

            [Fact]
            public void ShouldGetCorrectValueForDictionaryKeySETXID()
            {
                var result = FaxMessageCorrespondencePayload.CreateErrorPayload(new Dictionary<string, object> { { "SETXID", "456" } });

                Assert.Equal(new[] { "456" }, result.TransactionIds);
            }

            [Fact]
            public void ShouldGetDefaultValueForDictionaryKeySETXIDWhenEmptyString()
            {
                var result = FaxMessageCorrespondencePayload.CreateErrorPayload(new Dictionary<string, object> { { "SETXID", "" } });

                Assert.Equal(new[] { " " }, result.TransactionIds);
            }

            [Fact]
            public void ShouldGetCorrectValueForDictionaryKeyFaxNumber()
            {
                var result = FaxMessageCorrespondencePayload.CreateErrorPayload(new Dictionary<string, object> { { "faxn", "FAX" } });

                Assert.Equal("FAX", result.FaxNumber);
            }

            [Fact]
            public void ShouldGetCorrectValueForDictionaryKeyFaxNumberWhenEmptyString()
            {
                var result = FaxMessageCorrespondencePayload.CreateErrorPayload(new Dictionary<string, object> { { "faxn", "" } });

                Assert.Equal(" ", result.FaxNumber);
            }

            [Fact]
            public void ShouldGetCorrectValueForDictionaryKeyCorrespondenceId()
            {
                var result = FaxMessageCorrespondencePayload.CreateErrorPayload(new Dictionary<string, object> { { "Cid", "789" } });

                Assert.Equal("789", result.CorrespondenceId);
            }

            [Fact]
            public void ShouldGetCorrectValueForDictionaryKeyCorrespondenceIdWhenEmptyString()
            {
                var result = FaxMessageCorrespondencePayload.CreateErrorPayload(new Dictionary<string, object> { { "Cid", "" } });

                Assert.Equal(" ", result.CorrespondenceId);
            }
        }
    }
}
