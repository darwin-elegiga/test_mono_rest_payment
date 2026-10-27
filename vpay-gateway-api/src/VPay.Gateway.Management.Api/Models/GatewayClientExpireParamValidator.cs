using FluentValidation;

namespace VPay.Gateway.Management.Api.Models;

public class GatewayClientExpireParamValidator : AbstractValidator<GatewayClientExpireParam>
{
    public GatewayClientExpireParamValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty();
    }
}
