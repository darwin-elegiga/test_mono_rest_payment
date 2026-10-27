using FluentValidation.TestHelper;
using VPay.Gateway.Common.Preferencing.Models;

namespace VPay.Gateway.Common.Preferencing.Tests.Models;

/// <summary>
/// Tests for <see cref="NetworkEnrollmentRequestDto"/>
/// </summary>
public class NetworkEnrollmentRequestDtoValidatorTests
{
    private readonly NetworkEnrollmentRequestDtoValidator _validator = new();

    [Fact]
    public void Header_WhenNull_ShouldHaveError()
    {
        var list = new NetworkEnrollmentRequestDto
        {
            Header = null!
        };
        var result = _validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x.Header);
    }

    [Fact]
    public void Header_WhenEmpty_ShouldNotHaveError()
    {
        var list = new NetworkEnrollmentRequestDto
        {
            Header = new NetworkEnrollmentHeaderDto()
        };
        var result = _validator.TestValidate(list);
        result.ShouldNotHaveValidationErrorFor(x => x.Header);
    }

    [Fact]
    public void Header_WhenValid_ShouldNotHaveError()
    {
        var list = new NetworkEnrollmentRequestDto
        {
            Header = new NetworkEnrollmentHeaderDto
            {
                NetworkPartner = "ABC",
                CorrelationId = "123456789-123456789",
                Count = 1,
            }
        };
        var result = _validator.TestValidate(list);
        result.ShouldNotHaveValidationErrorFor(x => x.Header);
    }

    [Fact]
    public void PartnerProviders_WhenNull_ShouldHaveError()
    {
        var list = new NetworkEnrollmentRequestDto
        {
            PartnerProviders = null!
        };
        var result = _validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x.PartnerProviders);
    }

    [Fact]
    public void PartnerProviders_WhenEmpty_ShouldHaveError()
    {
        var list = new NetworkEnrollmentRequestDto
        {
            PartnerProviders = new List<NetworkEnrollmentListDto>()
        };
        var result = _validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x.PartnerProviders);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void PartnerProviders_WhenValid_ShouldNotHaveError(int count)
    {
        var list = CreatePartnerProviders(count);
        var result = _validator.TestValidate(list);
        result.ShouldNotHaveValidationErrorFor(x => x.PartnerProviders);
    }

    private static NetworkEnrollmentRequestDto CreatePartnerProviders(int count)
    {
        var list = new List<NetworkEnrollmentListDto>();

        for (int i = 0; i < count; i++)
        {
            list.Add(new NetworkEnrollmentListDto
            {
                ProviderTin = $"12345678{i}",
                LegalEntityName = $"ABC Company{i}",
                Dba = $"ABC{i}"
            });
        }
        return new NetworkEnrollmentRequestDto
        {
            Header = new NetworkEnrollmentHeaderDto
            {
                NetworkPartner = "ABC",
                CorrelationId = "123456789-123456789",
                Count = count
            },
            PartnerProviders = list
        };
    }
}
