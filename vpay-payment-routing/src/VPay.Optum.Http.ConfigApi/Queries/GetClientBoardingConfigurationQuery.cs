using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VPay.Optum.Http.ConfigApi.Models;

namespace VPay.Optum.Http.ConfigApi.Queries;
public static class GetClientBoardingConfiguration
{
    public record Query(string ConfigurationName, string OriginCode, string ClientCode) : IRequest<ConfigurationDto>;

    public class Handler : IRequestHandler<Query, ConfigurationDto>
    {
        private readonly IConfigApi _configApi;
        private readonly string DEFAULT = "DEFAULT";

        public Handler(IConfigApi configApi)
        {
            _configApi = configApi ?? throw new ArgumentNullException(nameof(configApi));
        }

        public Task<ConfigurationDto> Handle(Query request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.ConfigurationName?.Trim()))
            {
                throw new ArgumentException($"ConfigurationName cannot be empty");
            }

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

        public async Task<ConfigurationDto> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            var result = await _configApi.GetSpecificClientBoardingConfiguration(request.ConfigurationName, request.OriginCode, request.ClientCode, cancellationToken).ConfigureAwait(false);

            if (result.IsSuccessStatusCode && result.Content is not null)
            {
                // Config matches OriginCode and ClientCode
                var configuration = result.Content.Items.FirstOrDefault(x =>
                    x.OriginCode.Equals(request.OriginCode, StringComparison.InvariantCultureIgnoreCase) &&
                    x.ClientCode.Equals(request.ClientCode, StringComparison.InvariantCultureIgnoreCase));

                var defaultConfig = result.Content.Items.FirstOrDefault(x =>
                    x.OriginCode.Equals(DEFAULT, StringComparison.InvariantCultureIgnoreCase));

                // Configuration exists, is active and enabled
                if (configuration is not null)
                {
                    return configuration;
                }
                // origin/client specific config doesn't exist, return default if that exists, otherwise throw
                else if (defaultConfig is not null)
                {
                    return defaultConfig;
                }
            }

            throw new ArgumentNullException($"Error retrieving configuration for '{request.ConfigurationName}', OriginCode: '{request.OriginCode}', ClientCode: '{request.ClientCode}', with StatusCode: {result.StatusCode}");
        }
    }
}
