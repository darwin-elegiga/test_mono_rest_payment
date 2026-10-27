using VPay.DocSys.Generation.Common.Models;
using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    /// <summary>
    /// Tests for <see cref="StpPaymentSearchDtoValidator"/> class
    /// </summary>
    public class StpPaymentSearchDtoTests
    {
        protected readonly StpPaymentSearchDtoValidator _validator = new();

        public class NetworkName : StpPaymentSearchDtoTests
        {
            [Theory]
            [InlineData("ExistingNetwork")]
            public void Valid_WhenNotEmptyAndExists(string networkName)
            {
                StpPaymentSearchDto dto = MockSearch(networkName);
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Theory]
            [InlineData("NonExistingNetwork")]
            public void Valid_WhenNotEmptyAndNotExists(string networkName)
            {
                StpPaymentSearchDto dto = MockSearch(networkName);
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            public void Valid_WhenEmpty(string networkName)
            {
                StpPaymentSearchDto dto = MockSearch(networkName);
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Fact]
            public void Invalid_WhenExceedsMaxCharacters()
            {
                StpPaymentSearchDto dto = MockSearch();
                dto.NetworkName = new string('a', 21);
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentSearchDto.NetworkName));
            }
        }

        public class EnrollType : StpPaymentSearchDtoTests
        {
            [Theory]
            [InlineData("B2B")]
            [InlineData("C2B")]
            public void Valid_WhenNotEmptyAndExists(string enrollType)
            {
                StpPaymentSearchDto dto = MockSearch(enrollType: enrollType);
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Theory]
            [InlineData("ABC")]
            [InlineData("123")]
            public void Valid_WhenNotEmptyAndNotExists(string enrollType)
            {
                StpPaymentSearchDto dto = MockSearch(enrollType: enrollType);
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            public void Valid_WhenEmpty(string enrollType)
            {
                StpPaymentSearchDto dto = MockSearch(enrollType: enrollType);
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Fact]
            public void Invalid_WhenExceedsMaxCharacters()
            {
                StpPaymentSearchDto dto = MockSearch();
                dto.EnrollType = new string('a', 4);
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentSearchDto.EnrollType));
            }
        }

        public class Edi835Type : StpPaymentSearchDtoTests
        {
            [Theory]
            [InlineData("SDS")]
            [InlineData("PRS")]
            public void Valid_WhenNotEmptyAndExists(string edi835Type)
            {
                StpPaymentSearchDto dto = MockSearch(edi835Type: edi835Type);
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Theory]
            [InlineData("ABC")]
            [InlineData("123")]
            public void Valid_WhenNotEmptyAndNotExists(string edi835Type)
            {
                StpPaymentSearchDto dto = MockSearch(edi835Type: edi835Type);
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            public void Valid_WhenEmpty(string edi835Type)
            {
                StpPaymentSearchDto dto = MockSearch(edi835Type: edi835Type);
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Fact]
            public void Invalid_WhenExceedsMaxCharacters()
            {
                StpPaymentSearchDto dto = MockSearch();
                dto.Edi835Type = new string('a', 4);
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentSearchDto.Edi835Type));
            }
        }

        public class PaymentSentRetries : StpPaymentSearchDtoTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(5)]
            [InlineData(100)]
            public void Valid_WhenGreaterThanOrEqualToZero(int retries)
            {
                StpPaymentSearchDto dto = MockSearch();
                dto.PaymentSentRetries = retries;
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Fact]
            public void Valid_WhenNull()
            {
                StpPaymentSearchDto dto = MockSearch();
                dto.PaymentSentRetries = null;
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Fact]
            public void Invalid_WhenLessThanZero()
            {
                StpPaymentSearchDto dto = MockSearch();
                dto.PaymentSentRetries = -1;
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentSearchDto.PaymentSentRetries));
            }
        }

        public class MaxRetries : StpPaymentSearchDtoTests
        {
            [Theory]
            [InlineData(1)]
            [InlineData(5000)]
            [InlineData(10000)]
            public void Valid_WhenBetween1And10000(int maxResults)
            {
                StpPaymentSearchDto dto = MockSearch();
                dto.MaxResults = maxResults;
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Fact]
            public void Invalid_WhenLessThan1()
            {
                StpPaymentSearchDto dto = MockSearch();
                dto.MaxResults = 0;
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentSearchDto.MaxResults));
            }

            [Fact]
            public void Invalid_WhenGreaterThan10000()
            {
                StpPaymentSearchDto dto = MockSearch();
                dto.MaxResults = 10001;
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentSearchDto.MaxResults));
            }
        }

        private static StpPaymentSearchDto MockSearch(
            string networkName = null,
            string enrollType = null,
            string edi835Type = null)
        {
            return new StpPaymentSearchDto
            {
                NetworkName = networkName,
                EnrollType = enrollType,
                Edi835Type = edi835Type
            };
        }
    }
}
