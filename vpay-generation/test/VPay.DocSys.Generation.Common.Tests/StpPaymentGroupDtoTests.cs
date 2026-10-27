using System.Collections.Generic;
using VPay.DocSys.Generation.Common.Models;
using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    /// <summary>
    /// Tests for <see cref="StpPaymentGroupDtoValidator"/> class
    /// </summary>
    public class StpPaymentGroupDtoTests
    {
        protected readonly StpPaymentGroupDtoValidator _validator = new();

        public class NetworkName : StpPaymentGroupDtoTests
        {
            [Theory]
            [InlineData("SomeNetwork")]
            public void Valid_WhenNotEmpty(string networkName)
            {
                StpPaymentGroupDto dto = MockGroup(networkName);
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            public void Invalid_WhenEmpty(string networkName)
            {
                StpPaymentGroupDto dto = MockGroup(networkName);
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentGroupDto.NetworkName));
            }

            [Fact]
            public void Invalid_WhenExceedsMaxCharacters()
            {
                StpPaymentGroupDto dto = MockGroup();
                dto.NetworkName = new string('a', 21);
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentGroupDto.NetworkName));
            }
        }

        public class EnrollType : StpPaymentGroupDtoTests
        {
            [Theory]
            [InlineData("B2B")]
            [InlineData("C2B")]
            public void Valid_WhenNotEmptyAndValid(string enrollType)
            {
                StpPaymentGroupDto dto = MockGroup(enrollType: enrollType);
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Theory]
            [InlineData("ABC")]
            [InlineData("123")]
            public void Valid_WhenNotEmptyAndInvalid(string enrollType)
            {
                StpPaymentGroupDto dto = MockGroup(enrollType: enrollType);

                var result = _validator.Validate(dto);

                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentGroupDto.EnrollType));
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            public void Invalid_WhenEmpty(string enrollType)
            {
                StpPaymentGroupDto dto = MockGroup(enrollType: enrollType);
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentGroupDto.EnrollType));
            }

            [Fact]
            public void Invalid_WhenExceedsMaxCharacters()
            {
                StpPaymentGroupDto dto = MockGroup();
                dto.EnrollType = new string('a', 4);
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentGroupDto.EnrollType));
            }
        }

        public class Edi835Type : StpPaymentGroupDtoTests
        {
            [Theory]
            [InlineData("SDS")]
            [InlineData("PRS")]
            public void Valid_WhenNotEmptyAndValid(string edi835Type)
            {
                StpPaymentGroupDto dto = MockGroup(edi835Type: edi835Type);
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Theory]
            [InlineData("ABC")]
            [InlineData("123")]
            public void Valid_WhenNotEmptyAndInvalid(string edi835Type)
            {
                StpPaymentGroupDto dto = MockGroup(edi835Type: edi835Type);
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentGroupDto.Edi835Type));
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            public void Invalid_WhenEmpty(string edi835Type)
            {
                StpPaymentGroupDto dto = MockGroup(edi835Type: edi835Type);
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentGroupDto.Edi835Type));
            }

            [Fact]
            public void Invalid_WhenExceedsMaxCharacters()
            {
                StpPaymentGroupDto dto = MockGroup();
                dto.Edi835Type = new string('a', 4);
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentGroupDto.Edi835Type));
            }
        }

        public class TransactionIds : StpPaymentGroupDtoTests
        {
            [Fact]
            public void Valid_WhenNotEmpty()
            {
                StpPaymentGroupDto dto = MockGroup();
                var result = _validator.Validate(dto);
                Assert.True(result.IsValid);
            }

            [Fact]
            public void Invalid_WhenEmpty()
            {
                StpPaymentGroupDto dto = MockGroup();
                dto.TransactionIds = new List<long>();
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentGroupDto.TransactionIds));
            }

            [Fact]
            public void Invalid_WhenNull()
            {
                StpPaymentGroupDto dto = MockGroup();
                dto.TransactionIds = null;
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == nameof(StpPaymentGroupDto.TransactionIds));
            }

            [Fact]
            public void Invalid_WhenZero()
            {
                StpPaymentGroupDto dto = MockGroup();
                dto.TransactionIds = new List<long> { 0 };
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == $"{ nameof(StpPaymentGroupDto.TransactionIds)}[0]");
            }

            [Fact]
            public void Invalid_WhenNegative()
            {
                StpPaymentGroupDto dto = MockGroup();
                dto.TransactionIds = new List<long> { -1 };
                var result = _validator.Validate(dto);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == $"{ nameof(StpPaymentGroupDto.TransactionIds)}[0]");
            }
        }

        private static StpPaymentGroupDto MockGroup(
            string networkName = "SomeNetwork",
            string enrollType = "B2B",
            string edi835Type = "SDS")
        {
            return new StpPaymentGroupDto
            {
                NetworkName = networkName,
                EnrollType = enrollType,
                Edi835Type = edi835Type,
                TransactionIds = new List<long> { 1, 2, 3 }
            };
        }
    }
}
