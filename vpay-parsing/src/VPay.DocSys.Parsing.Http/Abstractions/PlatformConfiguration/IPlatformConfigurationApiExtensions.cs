using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Refit.Insane.PowerPack.Caching;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration
{
    public static class IPlatformConfigurationApiExtensions
    {
        public static async Task<HashSet<string>> GetCustomSendBackEnabledClientCodes(this IPlatformConfigurationApi api, CancellationToken cancellationToken = default)
            => (await api.GetCustomSendBackByClient(cancellationToken:cancellationToken))
            .Items
            .Where(dto => dto.IsCustomSendBack == true)
            .Select(dto => dto.ClientCode)
            .ToHashSet();

        public static async Task<bool> IsCustomSendBackEnabledForClient(this IPlatformConfigurationApi api, string clientCode, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase, CancellationToken cancellationToken = default)
            => (await api.GetCustomSendBackEnabledClientCodes())
            .Contains(clientCode, StringComparer.FromComparison(comparisonType));

        public static async Task<List<EdiDeliveryClientBoardingConfigDto>> GetEdiDeliveryForClient(this IPlatformConfigurationApi api, string clientCode, string billingEntityCode, CancellationToken cancellationToken = default)
        {
            var response = await api.GetEdiDeliveryByClient(clientCode, billingEntityCode, cancellationToken: cancellationToken);

            if (response?.Items == null || response.Items.Count < 1)
            {
                return response.Items;
            }

            return response.Items
                // group by relevance of match
                .GroupBy(dto => dto?.Relevance(clientCode: clientCode, billingEntityCode: billingEntityCode))
                // get most relevant match, ignoring mismatches
                .LastOrDefault(g => g.Key >= 0)
                // convert to list
                .ToList();
        }

        public static async Task<bool> IsEdiDeliveryEnabledForClient(this IPlatformConfigurationApi api, string clientCode, string billingEntityCode, EdiDeliveryDestination destination, EdiDeliveryFileExtension extension, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase, CancellationToken cancellationToken = default)
            => (await api.GetEdiDeliveryForClient(clientCode: clientCode, billingEntityCode: billingEntityCode, cancellationToken: cancellationToken))
            ?.Any(dto => dto.IsEnabled(destination, extension)) == true;

        public static Task ClearGetCustomSendBackByClientCache(this RefitCacheService service, string client = "")
            => service.ClearCache<IPlatformConfigurationApi, Pagination<CustomSendBackClientBoardingConfigDto>>(
                s => s.GetCustomSendBackByClient(client, default));

        public static Task ClearGetEdiDeliveryByClientCache(this RefitCacheService service, string client = "", string beCode = "")
            => service.ClearCache<IPlatformConfigurationApi, Pagination<EdiDeliveryClientBoardingConfigDto>>(
                s => s.GetEdiDeliveryByClient(client, beCode, default));
    }
}
