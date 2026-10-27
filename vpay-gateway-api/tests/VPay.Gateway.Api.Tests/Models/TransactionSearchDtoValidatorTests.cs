using FluentValidation;
using FluentValidation.TestHelper;
using Vpay.Gateway.Common.Vpay.Models;
using VPay.Gateway.Api.Models;
using Xunit;

namespace VPay.Gateway.Api.Tests.Models;
public class TransactionSearchDtoValidatorTests
{
    private readonly TransactionSearchDtoValidator _validator = new();

    private const int PaymendIDMaxLength = 49;
    private const int ProviderTinMaxLength = 9;
    public class DateRange : TransactionSearchDtoValidatorTests
    {
        [Fact]
        public void NullDateRange_ShouldError()
        {
            var dto = new TransactionSearchDto
            {
                StartDate = null,
                EndDate = null,
                PaymentId = "12345678"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(v => v.EndDate)
                   .WithSeverity(Severity.Error);
        }


        [Fact]
        public void SameDateRange_ShouldNotError()
        {
            var dto = new TransactionSearchDto
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today,
                PaymentId = "555555"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ValidDateRange_ShouldNotError()
        {
            var dto = new TransactionSearchDto
            {
                StartDate = DateTime.Today.AddDays(-1),
                EndDate = DateTime.Today,
                PaymentId = "555555"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidDateRange_ShouldError()
        {
            var dto = new TransactionSearchDto
            {
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today,
                PaymentId = "555555"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(v => v.EndDate)
                .WithSeverity(Severity.Error);
        }

        [Fact]
        public void MissingStartDate_ShouldError()
        {
            var dto = new TransactionSearchDto
            {
                StartDate = null,
                EndDate = DateTime.Today,
                PaymentId = "555555"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(v => v.StartDate)
                .WithSeverity(Severity.Error);
        }

        [Fact]
        public void MissingEndDate_ShouldError()
        {
            var dto = new TransactionSearchDto
            {
                StartDate = DateTime.Today,
                EndDate = null,
                PaymentId = "555555"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(v => v.EndDate)
                .WithSeverity(Severity.Error);
        }
    }

    public class PaymentID : TransactionSearchDtoValidatorTests
    {
        [Fact]
        public void ValidPayment_ShouldNotError()
        {
            var dto = new TransactionSearchDto
            {
                StartDate = DateTime.Today.AddDays(-1),
                EndDate = DateTime.Today,
                PaymentId = "44444444"
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void NullOrEmptyPaymentID_ShouldError()
        {
            var dto = new TransactionSearchDto
            {
                StartDate = DateTime.Today.AddDays(-1),
                EndDate = DateTime.Today,
                PaymentId = null
            };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.PaymentId)
                .WithSeverity(Severity.Error);
        }

        [Fact]
        public void MaxLengthExceeded_ShouldError()
        {
            var dto = new TransactionSearchDto
            {
                StartDate = DateTime.Today.AddDays(-1),
                EndDate = DateTime.Today,
                PaymentId = new string('7', PaymendIDMaxLength + 1)
            };

            var result = _validator.TestValidate(dto);

            result
                .ShouldHaveValidationErrorFor(x => x.PaymentId)
                .WithSeverity(Severity.Error);
        }
    }

    public class ProviderTin : TransactionSearchDtoValidatorTests
    {
        [Fact]
        public void MaxLengthExceeded_ShouldError()
        {
            var dto = new TransactionSearchDto
            {
                StartDate = DateTime.Today.AddDays(-1),
                EndDate = DateTime.Today,
                PaymentId = "12546",
                ProviderTin = new string('7', ProviderTinMaxLength + 1)
            };

            var result = _validator.TestValidate(dto);

            result
                .ShouldHaveValidationErrorFor(x => x.ProviderTin)
                .WithSeverity(Severity.Error);
        }

        [Fact]
        public void ValidLengthTin_ShouldNotError()
        {
            var dto = new TransactionSearchDto
            {
                StartDate = DateTime.Today.AddDays(-1),
                EndDate = DateTime.Today,
                PaymentId = "12546",
                ProviderTin = new string('7', ProviderTinMaxLength)
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
