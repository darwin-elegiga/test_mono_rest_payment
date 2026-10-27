using System.Globalization;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Refit;
using VPay.Extensions.Caching.Memory;

namespace VPay.Gateway.Common.PlatformConfig;

public class ExternalClientConfigurationQuery : IRequest<ExternalClientConfigurationDto?>
{
    public ExternalClientConfigurationQuery(string clientCode, DateTime date)
    {
        ClientCode = clientCode;
        Date = date;
    }

    public string ClientCode { get; }

    public DateTime Date { get; }
}

public class ExternalClientConfigurationQueryHandler : IRequestHandler<ExternalClientConfigurationQuery, ExternalClientConfigurationDto?>
{
    private readonly IPlatformConfigApi _platformConfigApi;
    private readonly IMemoryCache<ExternalClientConfigurationDto?> _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public ExternalClientConfigurationQueryHandler(IPlatformConfigApi platformConfigApi, IMemoryCache<ExternalClientConfigurationDto?> cache)
    {
        _platformConfigApi = platformConfigApi ?? throw new ArgumentNullException(nameof(platformConfigApi));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .SetAbsoluteExpiration(TimeSpan.FromHours(1));
    }

    public async Task<ExternalClientConfigurationDto?> Handle(ExternalClientConfigurationQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientCode))
        {
            // if there's no client, we'll just return null (eg. no configuration)
            return null;
        }

        var cacheKey = $"{nameof(IPlatformConfigApi)}.{nameof(IPlatformConfigApi.GetExternalClientConfiguration)}.{request.ClientCode}.{request.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";

        return await _cache.GetOrCreateAsync(cacheKey, FindCachedObject, _cacheOptions).ConfigureAwait(false);

        async Task<ExternalClientConfigurationDto?> FindCachedObject()
        {
            try
            {
                var configurations = await _platformConfigApi.GetExternalClientConfiguration(
                        request.ClientCode,
                        request.Date,
                        ExternalClientConfigurationType.Guidewire,
                        cancellationToken)
                    .ConfigureAwait(false);
                return configurations.FirstOrDefault();
            }
            catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}
