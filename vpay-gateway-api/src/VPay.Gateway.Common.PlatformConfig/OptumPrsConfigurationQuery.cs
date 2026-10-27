using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Refit;
using VPay.Extensions.Caching.Memory;

namespace VPay.Gateway.Common.PlatformConfig;

public record OptumPrsConfigurationQuery(string? OriginCode, string? ClientCode, string? BillingEntityCode) : IRequest<OptumPrsConfigurationDto?>;

public sealed class OptumPrsConfigurationQueryValidator : AbstractValidator<OptumPrsConfigurationQuery>
{
    public OptumPrsConfigurationQueryValidator()
    {
        RuleFor(x => x.OriginCode)
            .NotEmpty().When(y => string.IsNullOrEmpty(y.ClientCode))
            .MaximumLength(50);

        RuleFor(x => x.ClientCode)
            .NotEmpty().When(y => string.IsNullOrEmpty(y.OriginCode))
            .MaximumLength(10);

        RuleFor(x => x.BillingEntityCode)
            .MaximumLength(20);
    }
}

public class OptumPrsConfigurationQueryHandler : IRequestHandler<OptumPrsConfigurationQuery, OptumPrsConfigurationDto?>
{
    private readonly IPlatformConfigApi _platformConfigApi;
    private readonly IMemoryCache<OptumPrsConfigurationDto?> _cache;
    private readonly OptumPrsConfigurationQueryValidator _queryValidator = new();

    public OptumPrsConfigurationQueryHandler(IPlatformConfigApi platformConfigApi, IMemoryCache<OptumPrsConfigurationDto?> cache)
    {
        _platformConfigApi = platformConfigApi ?? throw new ArgumentNullException(nameof(platformConfigApi));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<OptumPrsConfigurationDto?> Handle(OptumPrsConfigurationQuery request, CancellationToken cancellationToken)
    {
        await _queryValidator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);

        string cacheKey = $"OPTUM_PRS_CONFIG_KEY_{request.ClientCode ?? request.OriginCode}_{request.BillingEntityCode ?? "BENULL"}";

        // if the request is for the current date, we'll cache the result
        return await _cache.GetOrCreateAsync(
            cacheKey,
            () => LoadOptumPrsConfiguration(),
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.Today.AddDays(1)),
            false).ConfigureAwait(false);

        async Task<OptumPrsConfigurationDto?> LoadOptumPrsConfiguration()
        {
            try
            {
                return await _platformConfigApi.GetOptumPrsConfiguration(
                    clientCode: request.ClientCode,
                    originCode: request.OriginCode,
                    billingEntityCode: request.BillingEntityCode,
                    token: cancellationToken).ConfigureAwait(false);
            }
            catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}
