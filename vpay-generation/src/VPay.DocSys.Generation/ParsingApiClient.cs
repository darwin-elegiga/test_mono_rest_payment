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
    public class ParsingApiClient : IParsingApiClient
    {
        private static readonly JsonSerializer _jsonSerializer = new JsonSerializer();

        private readonly HttpClient _httpClient;
        private readonly ILogger<ParsingApiClient> _logger;
        private readonly IReadOnlyDictionary<string, string> _endPointDictionary;

        private readonly IAsyncPolicy _retryPolicy;

        public ParsingApiClient(
            ApiSettings apiSettings,
            HttpClient httpClient,
            ILogger<ParsingApiClient> logger)
        {
            ThrowIf.IsNull(apiSettings, nameof(apiSettings));
            ThrowIf.IsNull(httpClient, nameof(httpClient));
            ThrowIf.IsNull(logger, nameof(logger));

            _httpClient = httpClient;
            _logger = logger;

            _endPointDictionary = apiSettings["ParsingApi"].EndPointFragments;
            _retryPolicy = new HttpRequestPolicyHolder(_logger).HttpGetPolicy;
        }

        public async Task<ClientVPaygeSettingsResponse> GetXmlConfiguration(string tpaCode, string beCode)
        {
            var endPoint = _endPointDictionary["XmlConfig"] + $"/{tpaCode}/xml/{beCode}";

            try
            {
                var request = _httpClient.GetAsync(endPoint, HttpCompletionOption.ResponseHeadersRead);

                using (_logger.BeginScope(new
                {
                    TPA = tpaCode.Replace(Environment.NewLine, ""),
                    BE = beCode.Replace(Environment.NewLine, ""),
                    BillingEntity = beCode.Replace(Environment.NewLine, "")
                }.ToDictionary()))
                using (var response = await _retryPolicy.ExecuteAsync(() => request))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("Could not GET xml configuration for {TPA}, {BE}, {StatusCode}",
                            tpaCode.Replace(Environment.NewLine, ""),
                            beCode.Replace(Environment.NewLine, ""),
                            response.StatusCode);

                        throw new Exception("Could not GET xml configuration from Parsing");
                    }

                    using (var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                    using (var jsonReader = new JsonTextReader(streamReader))
                    {
                        _logger.LogInformation("GET xml configuration successfully");

                        return _jsonSerializer.Deserialize<ClientVPaygeSettingsResponse>(jsonReader);
                    }
                }
            }
            catch (Exception e) when (e.LogWithScope(_logger))
            {
                throw;
            }
        }
    }
}
