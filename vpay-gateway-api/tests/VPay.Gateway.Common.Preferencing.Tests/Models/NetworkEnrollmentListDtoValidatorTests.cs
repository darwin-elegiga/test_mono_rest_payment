using FluentValidation.TestHelper;
using VPay.Gateway.Common.Preferencing.Models;

namespace VPay.Gateway.Common.Preferencing.Tests.Models;

/// <summary>
/// Tests for <see cref="NetworkEnrollmentListDto"/>
/// </summary>
public class NetworkEnrollmentListDtoValidatorTests
{
    private readonly NetworkEnrollmentListDtoValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void ProviderTin_WhenEmpty_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentListDto
        {
            ProviderTin = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.ProviderTin);
    }

    [Fact]
    public void ProviderTin_WhenTooLong_ShouldHaveError()
    {
        var item = new NetworkEnrollmentListDto
        {
            ProviderTin = new string('1', 10)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.ProviderTin);
    }

    [Fact]
    public void ProviderTin_WhenValid_ShouldNotHaveError()
    {
        var item = new NetworkEnrollmentListDto
        {
            ProviderTin = "123456789"
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.ProviderTin);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void LegalEntityName_WhenEmpty_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentListDto
        {
            LegalEntityName = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.LegalEntityName);
    }

    [Fact]
    public void LegalEntityName_WhenTooLong_ShouldHaveError()
    {
        var item = new NetworkEnrollmentListDto
        {
            LegalEntityName = new string('A', 121)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.LegalEntityName);
    }

    [Fact]
    public void LegalEntityName_WhenValid_ShouldNotHaveError()
    {
        var item = new NetworkEnrollmentListDto
        {
            LegalEntityName = "ABC Inc."
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.LegalEntityName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void DoingBusinessAs_WhenEmpty_ShouldNotHaveError(string value)
    {
        var item = new NetworkEnrollmentListDto
        {
            Dba = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.Dba);
    }

    [Fact]
    public void DoingBusinessAs_WhenTooLong_ShouldHaveError()
    {
        var item = new NetworkEnrollmentListDto
        {
            Dba = new string('A', 121)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.Dba);
    }

    [Fact]
    public void DoingBusinessAs_WhenValid_ShouldNotHaveError()
    {
        var item = new NetworkEnrollmentListDto
        {
            Dba = "ABC Ltd."
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.Dba);
    }

    [Fact]
    public void EnrollmentList_WhenNull_ShouldHaveError()
    {
        var list = new NetworkEnrollmentListDto
        {
            EnrollmentList = null!
        };
        var result = _validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x.EnrollmentList);
    }

    [Fact]
    public void EnrollmentList_WhenEmpty_ShouldHaveError()
    {
        var list = new NetworkEnrollmentListDto
        {
            EnrollmentList = new List<NetworkEnrollmentItemDto>()
        };
        var result = _validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x.EnrollmentList);
    }

    [Theory]
    [InlineData(2001)]
    public void EnrollmentList_WhenTooManyEnrollments_ShouldHaveError(int count)
    {
        var list = CreateEnrollmentList(count);
        var result = _validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x.EnrollmentList);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(2000)]
    public void EnrollmentList_WhenValid_ShouldNotHaveError(int count)
    {
        var list = CreateEnrollmentList(count);
        var result = _validator.TestValidate(list);
        result.ShouldNotHaveValidationErrorFor(x => x.EnrollmentList);
    }

    private static NetworkEnrollmentListDto CreateEnrollmentList(int count)
    {
        var list = new List<NetworkEnrollmentItemDto>();

        for (int i = 0; i < count; i++)
        {
            list.Add(new NetworkEnrollmentItemDto
            {
                Npi = $"100{i}",
                Address1 = $"100{i} Some St",
                City = "Plano",
                State = "TX",
                Zip9 = "75000",
                EnrollmentType = "C2B",
                EnrollmentEligibilityStatus = "Y",
                EnrollmentStatusCd = "A",
                EffectiveDate = DateTime.Today,
                InitialEnrollmentDate = DateTime.Today,
                LastUpdatedUtcDate = DateTime.Today,
            });
        }
        return new NetworkEnrollmentListDto
        {
            EnrollmentList = list
        };
    }
}
