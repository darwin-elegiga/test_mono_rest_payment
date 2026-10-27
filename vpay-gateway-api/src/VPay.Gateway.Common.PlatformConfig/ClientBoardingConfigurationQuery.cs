using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using VPay.Extensions.Caching.Memory;

namespace VPay.Gateway.Common.PlatformConfig;

public record ClientBoardingConfigurationQuery(string ConfigurationName, string OriginCode, string ClientCode) : IRequest<ClientConfigurationDto>;

public sealed class ClientBoardingConfigurationQueryValidator : AbstractValidator<ClientBoardingConfigurationQuery>
{
    public ClientBoardingConfigurationQueryValidator()
    {
        RuleFor(x => x.ConfigurationName)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.OriginCode)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.ClientCode)
            .NotEmpty()
            .MaximumLength(10);
    }
}

public class ClientBoardingConfigurationQueryHandler : IRequestHandler<ClientBoardingConfigurationQuery, ClientConfigurationDto?>
{
    private readonly IPlatformConfigApi _platformConfig;
    private readonly IMemoryCache<ClientConfigurationDto?> _cache;
    private readonly ClientBoardingConfigurationQueryValidator _queryValidator = new();
    private const string DEFAULT = "DEFAULT";

    public ClientBoardingConfigurationQueryHandler(IPlatformConfigApi platformConfig, IMemoryCache<ClientConfigurationDto?> cache)
    {
        _platformConfig = platformConfig ?? throw new ArgumentNullException(nameof(platformConfig));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<ClientConfigurationDto?> Handle(ClientBoardingConfigurationQuery request, CancellationToken cancellationToken)
    {
        await _queryValidator
            .ValidateAndThrowAsync(request, cancellationToken)
            .ConfigureAwait(false);

        string cacheKey = $"CLIENT_BOARDING_CONFIG_KEY_{request.ConfigurationName}_{request.OriginCode}_{request.ClientCode}".ToUpperInvariant();

        var cacheOption = new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.Today.AddDays(1));

        return await _cache
            .GetOrCreateAsync(cacheKey, CacheItem, cacheOption, false)
            .ConfigureAwait(false);

        Task<ClientConfigurationDto?> CacheItem() =>
            LoadClientBoardingConfiguration(request, cancellationToken);
    }

    private async Task<ClientConfigurationDto?> LoadClientBoardingConfiguration(ClientBoardingConfigurationQuery request, CancellationToken cancellationToken)
    {
        var result = await _platformConfig
            .GetClientBoardingConfiguration(request.ConfigurationName, request.OriginCode, request.ClientCode, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccessStatusCode && result.Content is not null)
        {
            // Config matches OriginCode and ClientCode
            var clientConfig = result.Content.Items.FirstOrDefault(x =>
                x.OriginCode.Equals(request.OriginCode, StringComparison.InvariantCultureIgnoreCase) &&
                x.ClientCode.Equals(request.ClientCode, StringComparison.InvariantCultureIgnoreCase));

            // Configuration exists, is active and enabled
            if (clientConfig is not null)
            {
                return clientConfig;
            }

            var defaultConfig = result.Content.Items.FirstOrDefault(x =>
                x.OriginCode.Equals(DEFAULT, StringComparison.InvariantCultureIgnoreCase));

            // origin/client specific config doesn't exist, return default if that exists, otherwise throw
            if (defaultConfig is not null)
            {
                return defaultConfig;
            }
        }

        throw new ArgumentNullException($"Error retrieving configuration for '{request.ConfigurationName}', OriginCode: '{request.OriginCode}', ClientCode: '{request.ClientCode}', with StatusCode: {result.StatusCode}");
    }
}
