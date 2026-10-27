using FluentValidation.TestHelper;
using VPay.Gateway.Api.Models;
using Xunit;

namespace VPay.Gateway.Api.Tests.Models;

/// <summary>
/// Tests for the <see cref="CreateEnrollmentDtoValidator"/>
/// </summary>
public class CreateEnrollmentDtoValidatorTests
{
    private readonly CreateEnrollmentDtoValidator _validator = new CreateEnrollmentDtoValidator();

    public static IEnumerable<object[]> EmptyStrings { get; } = new[]
    {
            new object[] { null },
            new object[] { "" },
            new object[] { " " }
        };

    public class PayeeName : CreateEnrollmentDtoValidatorTests
    {
        private const int MaxLength = 100;

        [Theory]
        [MemberData(nameof(EmptyStrings))]
        public void WhenEmpty_ShouldBeInvalid(string value)
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                PayeeName = value
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.PayeeName);
        }

        [Fact]
        public void WhenLongerThanMaxLength_ShouldBeInvalid()
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                PayeeName = new string('a', MaxLength + 1)
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.PayeeName);
        }

        [Fact]
        public void WhenMaxLength_ShouldBeValid()
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                PayeeName = new string('a', MaxLength)
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.PayeeName);
        }
    }

    public class MemberId : CreateEnrollmentDtoValidatorTests
    {
        private const int MaxLength = 20;

        [Theory]
        [MemberData(nameof(EmptyStrings))]
        public void WhenEmpty_ShouldBeInvalid(string value)
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                MemberId = value
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.MemberId);
        }

        [Fact]
        public void WhenLongerThanMaxLength_ShouldBeInvalid()
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                MemberId = new string('a', MaxLength + 1)
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.MemberId);
        }

        [Fact]
        public void WhenMaxLength_ShouldBeValid()
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                MemberId = new string('a', MaxLength)
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.MemberId);
        }
    }

    public class PolicyNumber : CreateEnrollmentDtoValidatorTests
    {
        private const int MaxLength = 100;

        [Theory]
        [MemberData(nameof(EmptyStrings))]
        public void WhenEmpty_ShouldBeValid(string value)
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                PolicyNumber = value
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.PolicyNumber);
        }

        [Fact]
        public void WhenLongerThanMaxLength_ShouldBeInvalid()
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                PolicyNumber = new string('a', MaxLength + 1)
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.PolicyNumber);
        }

        [Fact]
        public void WhenMaxLength_ShouldBeValid()
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                PolicyNumber = new string('a', MaxLength)
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.PolicyNumber);
        }
    }

    public class CellPhoneNumber : CreateEnrollmentDtoValidatorTests
    {
        private const int MaxLength = 20;

        [Theory]
        [MemberData(nameof(EmptyStrings))]
        public void WhenEmpty_ShouldBeInvalid(string value)
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                CellPhoneNumber = value
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.CellPhoneNumber);
        }

        [Fact]
        public void WhenLongerThanMaxLength_ShouldBeInvalid()
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                CellPhoneNumber = new string('1', MaxLength + 1)
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.CellPhoneNumber);
        }

        [Theory]
        [InlineData("1-555-867-5309")]
        [InlineData("+1-555-867-5309")]
        [InlineData("555-867-5309")]
        [InlineData("5558675309")]
        [InlineData("(555)867-5309")]
        [InlineData("(5)(5)(5)(8)(6)(7)(5)(3)(0)(9)")]
        public void WhenValueIsPhoneNumber_ShouldBeValid(string value)
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                CellPhoneNumber = value
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.CellPhoneNumber);
        }
    }

    public class PayeeEmail : CreateEnrollmentDtoValidatorTests
    {
        private const int MaxLength = 100;

        [Theory]
        [MemberData(nameof(EmptyStrings))]
        public void WhenEmpty_ShouldBeInvalid(string value)
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                PayeeEmail = value
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.PayeeEmail);
        }

        [Fact]
        public void WhenLongerThanMaxLength_ShouldBeInvalid()
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                PayeeEmail = $"{new string('a', MaxLength - "@test.com".Length + 1)}@test.com"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.PayeeEmail);
        }

        [Fact]
        public void WhenMaxLength_ShouldBeValid()
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                PayeeEmail = $"{new string('a', MaxLength - "@test.com".Length)}@test.com"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.PayeeEmail);
        }

        [Theory]
        [InlineData("user@test.com")]
        [InlineData("a@b.c")]
        public void WhenValueIsEmail_ShouldBeValid(string value)
        {
            CreateEnrollmentDto dto = new CreateEnrollmentDto
            {
                PayeeEmail = value
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.PayeeEmail);
        }
    }
}
