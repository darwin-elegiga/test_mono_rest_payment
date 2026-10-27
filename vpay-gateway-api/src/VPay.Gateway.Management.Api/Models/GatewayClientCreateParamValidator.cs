using FluentValidation;

namespace VPay.Gateway.Management.Api.Models;

public class GatewayClientCreateParamValidator : AbstractValidator<GatewayClientCreateParam>
{
    public GatewayClientCreateParamValidator()
    {
        Transform(x => x.ClientId, value => value?.Trim())
            .NotEmpty()
            .Length(5, 100);

        Transform(x => x.ClientSecret, value => value?.Trim())
            .NotEmpty()
            .Length(10, 200);

        Transform(x => x.ClientCode, value => value?.Trim())
            .NotEmpty()
            .MaximumLength(10);
    }
}
