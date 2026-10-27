using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.DocSys.Common;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.Distribution
{
    public class DocSysHttpClient : IDocSysHttpClient
    {
        private readonly ApiSettings _settings;
        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private readonly HttpRequestPolicyHolder _httpRequestPolicies;

        public DocSysHttpClient(
            ApiSettings settings,
            HttpClient httpClient,
            HttpRequestPolicyHolder httpRequestPolicyHolder,
            IDocSysConfigCache docSysConfigCache,
            ILogger<DocSysHttpClient> logger)
        {
            ThrowIf.IsNull(settings, nameof(settings));
            ThrowIf.IsNull(httpRequestPolicyHolder, nameof(httpRequestPolicyHolder));
            ThrowIf.IsNull(docSysConfigCache, nameof(docSysConfigCache));
            ThrowIf.IsNull(logger, nameof(logger));

            _settings = settings;
            _client = httpClient;
            _logger = logger;
            _httpRequestPolicies = httpRequestPolicyHolder;

            this.DocSysConfigCache = docSysConfigCache;
        }

        public Task<DocSysConfiguration> GetDocSysConfig(string tpa)
        {
            ThrowIf.IsNullOrWhiteSpace(tpa, nameof(tpa));

            // since the cache key is always case-sensitive and the parsing api is not, let's always use the upper case value
            tpa = tpa.ToUpper();

            var endpointUrl = $"{_settings["ParsingAPI"].GetUri()}api/Config/{tpa}";

            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                { "TPA", tpa },
                { "ClientCode", tpa },
                { "Operation", nameof(GetDocSysConfig) },
                { "Url", endpointUrl },
            }))
            {
                _logger.LogInformation($"Checking cache for configuration for '{tpa.Replace(Environment.NewLine, "")}'");

                return this.DocSysConfigCache.GetOrAddAsync(tpa, (k) =>
                {
                    _logger.LogInformation($"Configuration not found in cache; adding to cache for '{tpa.Replace(Environment.NewLine, "")}'.");

                    try
                    {
                        return _httpRequestPolicies.HttpGetPolicy.ExecuteAsync(async () =>
                        {
                            using (var response = await _client.GetAsync(endpointUrl, HttpCompletionOption.ResponseHeadersRead))
                            {
                                response.EnsureSuccessStatusCode();
                                var config = JsonConvert.DeserializeObject<DocSysConfiguration>(await response.Content.ReadAsStringAsync());
                                _logger.LogInformation($"Success: {endpointUrl.Replace(Environment.NewLine, "")}");
                                return config;
                            }

                        }, endpointUrl);
                    }
                    catch (Exception exception)
                    {
                        throw _logger.LogException(exception, $"Error: {endpointUrl.Replace(Environment.NewLine, "")}");
                    }
                });
            }
        }

        public IDocSysConfigCache DocSysConfigCache { get; }
    }
}
