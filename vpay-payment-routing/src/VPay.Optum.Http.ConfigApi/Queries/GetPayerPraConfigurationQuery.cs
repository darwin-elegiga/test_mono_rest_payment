using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Refit;
using VPay.Extensions.Caching.Memory;
using VPay.Optum.Http.ConfigApi.Models;

namespace VPay.Optum.Http.ConfigApi.Queries;

public static class GetPayerPraConfigurationQuery
{
    public record Query(string OriginCode, string ClientCode, string BillingEntityCode) : IRequest<PayerPraConfigurationDto>;

    public class PayerPraConfigurationQueryHandler : IRequestHandler<Query, PayerPraConfigurationDto>
    {
        private readonly IConfigApi _configApi;
        private readonly IAsyncPolicy<IApiResponse<PayerPraConfigurationDto>> _policy;
        private readonly IMemoryCache<PayerPraConfigurationDto> _cache;
        private readonly ILogger<PayerPraConfigurationQueryHandler> _logger;

        public static readonly IAsyncPolicy<IApiResponse<PayerPraConfigurationDto>> DefaultRetryPolicy =
            Policy<IApiResponse<PayerPraConfigurationDto>>
                .Handle<HttpRequestException>()
                .OrResult(response =>
                    (int)response.StatusCode >= 500 ||
                    response.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                    response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    retryCount: 10,
                    sleepDurationProvider: retryAttempt => retryAttempt <= 5
                        ? TimeSpan.FromMilliseconds(250)
                        : TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 6)));

        public PayerPraConfigurationQueryHandler(
            IConfigApi configApi,
            IMemoryCache<PayerPraConfigurationDto> cache,
            ILogger<PayerPraConfigurationQueryHandler> logger)
            : this(configApi, cache, logger, DefaultRetryPolicy)
        {
        }

        public PayerPraConfigurationQueryHandler(
            IConfigApi configApi,
            IMemoryCache<PayerPraConfigurationDto> cache,
            ILogger<PayerPraConfigurationQueryHandler> logger,
            IAsyncPolicy<IApiResponse<PayerPraConfigurationDto>> policy)
        {
            _configApi = configApi ?? throw new ArgumentNullException(nameof(configApi));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public Task<PayerPraConfigurationDto> Handle(Query request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.OriginCode?.Trim()))
            {
                throw new ArgumentException($"OriginCode cannot be empty");
            }

            if (string.IsNullOrEmpty(request.ClientCode?.Trim()))
            {
                throw new ArgumentException($"ClientCode cannot be empty");
            }

            return HandleAsync(request, cancellationToken);
        }

        public Task<PayerPraConfigurationDto> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            string cacheKey = $"OPTUM_PAYER_PRA_CONFIG_KEY_{request.OriginCode}_{request.ClientCode}_{request.BillingEntityCode}";

            if (_cache.TryGetValue(cacheKey, out PayerPraConfigurationDto cachedValue))
            {
                _logger.LogInformation(
                    "Cache hit for Payer PRA Configuration with OriginCode: '{OriginCode}', ClientCode: '{ClientCode}', BillingEntityCode: '{BillingEntityCode}'",
                    request.OriginCode, request.ClientCode, request.BillingEntityCode);

                return Task.FromResult(cachedValue);
            }

            return _cache.GetOrCreateAsync(
                cacheKey,
                () => LoadConfiguration(request, cancellationToken),
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1)),
                false);
        }

        private async Task<PayerPraConfigurationDto> LoadConfiguration(Query request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Loading Payer PRA Configuration into cache for OriginCode: '{OriginCode}', ClientCode: '{ClientCode}', BillingEntityCode: '{BillingEntityCode}'",
                request.OriginCode, request.ClientCode, request.BillingEntityCode);

            IApiResponse<PayerPraConfigurationDto> result;

            try
            {
                result = await _policy
                    .ExecuteAsync(
                        ct => _configApi.GetPayerPraConfiguration(
                            request.OriginCode,
                            request.ClientCode,
                            request.BillingEntityCode,
                            ct),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new ConfigApiEndpointException(
                    $"Config API endpoint failed after all retries for OriginCode: '{request.OriginCode}', ClientCode: '{request.ClientCode}'",
                    ex);
            }

            if (!result.IsSuccessStatusCode || result.Content is null)
            {
                throw new ConfigApiEndpointException(
                    $"PRA Configuration not found for OriginCode: '{request.OriginCode}', ClientCode: '{request.ClientCode}', with StatusCode: {result.StatusCode}",
                    result.StatusCode);
            }

            return result.Content.IsPayerPraEnabled
                ? result.Content
                : throw new NotSupportedException($"Payer PRA is NOT enabled for Origin/Client {request.OriginCode}/{request.ClientCode}, cannot continue");
        }
    }
}
