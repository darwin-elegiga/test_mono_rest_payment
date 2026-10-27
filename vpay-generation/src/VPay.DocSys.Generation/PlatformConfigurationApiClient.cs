using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Polly;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class PlatformConfigurationApiClient : IPlatformConfigurationApiClient
    {
        private static readonly JsonSerializer _serializer = new JsonSerializer();

        private readonly HttpClient _httpClient;
        private readonly ILogger<PlatformConfigurationApiClient> _logger;
        private readonly IReadOnlyDictionary<string, string> _endPointDictionary;

        private readonly IAsyncPolicy _retryPolicy;

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

            _endPointDictionary = apiSettings["PlatformConfigurationApi"].EndPointFragments;

            _retryPolicy = new HttpRequestPolicyHolder(_logger).HttpGetPolicy;
        }

        public async Task<OtbDetailResponse> GetOtbDetail(string tpaCode, string beCode)
        {
            ThrowIf.IsNullOrWhiteSpace(tpaCode, nameof(tpaCode));
            ThrowIf.IsNullOrWhiteSpace(beCode, nameof(beCode));

            var endPoint = $"{_endPointDictionary["GetBillingEntityDetails"]}{tpaCode}/{beCode}/otb-details";

            try
            {
                var request = _httpClient.GetAsync(endPoint, HttpCompletionOption.ResponseHeadersRead);

                using (_logger.BeginScope(new
                {
                    TPA = tpaCode.Replace(Environment.NewLine, ""),
                    BE = beCode.Replace(Environment.NewLine, ""),
                }.ToDictionary()))
                using (var responseMessage = await _retryPolicy.ExecuteAsync(() => request))
                {
                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        _logger.LogError("Unsuccessful response for TPA:{TPA} BE:{BE}, {StatusCode}",
                            tpaCode.Replace(Environment.NewLine, ""),
                            beCode.Replace(Environment.NewLine, ""),
                            responseMessage.StatusCode);

                        return null;
                    }

                    using (var streamReader = new StreamReader(await responseMessage.Content.ReadAsStreamAsync()))
                    using (var jsonReader = new JsonTextReader(streamReader))
                    {
                        var response = _serializer.Deserialize<OtbDetailResponse>(jsonReader);

                        using (_logger.BeginScope(response.ToDictionary()))
                        {
                            _logger.LogInformation("Successful response for TPA:{TPA}, BE:{BE}",
                                tpaCode.Replace(Environment.NewLine, ""),
                                beCode.Replace(Environment.NewLine, ""));

                            return response;
                        }
                    }
                }
            }
            catch (Exception e) when (e.LogWithScope(_logger))
            {
                return null;
            }
        }
    }
}
