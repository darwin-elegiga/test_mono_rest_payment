using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Helpers;
using VPay.DocSys.Parsing.Common.Models;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Parsing
{
    public class PlatformConfigurationApiClient : IPlatformConfigurationApiClient
    {
        private static readonly JsonSerializer _serializer = new JsonSerializer();

        private readonly HttpClient _httpClient;
        private readonly ILogger<PlatformConfigurationApiClient> _logger;
        private readonly IReadOnlyDictionary<string, string> _endPointDictionary;

        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        public const string ServiceName = "PlatformConfigurationApi";

        public PlatformConfigurationApiClient(
            ApiSettings apiSettings,
            HttpClient httpClient,
            ILogger<PlatformConfigurationApiClient> logger)
        {
            ThrowIf.IsNull(apiSettings, nameof(apiSettings));
            ThrowIf.IsNull(httpClient, nameof(httpClient));
            ThrowIf.IsNull(logger, nameof(logger));

            _httpClient = httpClient;
            _logger = logger;

            _endPointDictionary = apiSettings[ServiceName]?.EndPointFragments;

            // Use a policy that will automatically retry on HTTP request exceptions, timeout exceptions,
            // and when receiving any of the transient HTTP status codes.
            _retryPolicy = new HttpRequestResponsePolicyHolder(retryCount: 2, sleepDuration : TimeSpan.FromMilliseconds(300), _logger).RetryPolicy;
        }

        public async Task<OtbBillingEntityClientDto> GetOtbClientBillingEntities(string tpaCode, ICollection<string> beCodes)
        {
            ThrowIf.IsNullOrWhiteSpace(tpaCode, nameof(tpaCode));
            var root = "api/otb-details";
            if (_endPointDictionary == null || !_endPointDictionary.ContainsKey("GetOtbDetails"))
            {
                _logger.LogInformation("OTB API unable to load endpoint fragments.");
            }
            else
            {
                root = _endPointDictionary["GetOtbDetails"];
            }
            var beCodeList = beCodes.Count > 0 ? "?beCode=" + string.Join("&beCode=", beCodes) : string.Empty;
            var endPoint = $"{root}/clients/{tpaCode}/billingEntities{beCodeList}";
            using (_logger.BeginScope(new { ClientCode = tpaCode, BillingEntityCodes = string.Join(",", beCodes), }.ToDictionary()))
            {
                _logger.LogInformation("OTB API endpoint call started: " + endPoint);
                try
                {
                    using (var responseMessage = await _retryPolicy.ExecuteAsync(async () => await _httpClient.GetAsync(endPoint, HttpCompletionOption.ResponseHeadersRead)))
                    {
                        responseMessage.EnsureSuccessStatusCode();
                        using (var streamReader = new StreamReader(await responseMessage.Content.ReadAsStreamAsync()))
                        using (var jsonReader = new JsonTextReader(streamReader))
                        {
                            var response = _serializer.Deserialize<OtbBillingEntityClientDto>(jsonReader);
                            using (_logger.BeginScope(response.ToDictionary()))
                            {
                                _logger.LogInformation("OTB API successful response for TPA:{ClientCode}, BE:{BillingEntityCode}", tpaCode, beCodes);
                                return response;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"An error occurred calling OTB API: {e.Message}");
                    throw;
                }
            }
        }
    }
}
