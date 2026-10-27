using Microsoft.Extensions.Logging;

using Polly;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class GreenArrowApiClient : IGreenArrowApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GreenArrowApiClient> _logger;
        private readonly IReadOnlyDictionary<string, string> _endPointDictionary;

        private readonly IAsyncPolicy _retryPolicy;

        public GreenArrowApiClient(
            ApiSettings apiSettings,
            HttpClient httpClient,
            ILogger<GreenArrowApiClient> logger)
        {
            ThrowIf.IsNull(apiSettings, nameof(apiSettings));
            ThrowIf.IsNull(httpClient, nameof(httpClient));
            ThrowIf.IsNull(logger, nameof(logger));

            _httpClient = httpClient;
            _logger = logger;

            _endPointDictionary = apiSettings["GreenArrowApi"].EndPointFragments;

            _retryPolicy = new HttpRequestPolicyHolder(_logger).HttpPostPolicy;
        }

        public async Task SendEmail(IEmailRequest emailRequest)
        {
            ThrowIf.IsNull(emailRequest, nameof(emailRequest));

            var endpoint = _endPointDictionary["SendEmail"];
            var content = emailRequest.ToJsonStringContent();
            var request = _retryPolicy.ExecuteAsync(() => _httpClient.PostAsync(endpoint, content));

            using (_logger.BeginScope(emailRequest.ToDictionary()))
            using (var response = await request)
            {
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Could not POST email to Green Arrow, {StatusCode}", response.StatusCode);

                    throw new Exception("Could not POST email to Green Arrow");
                }
            }
        }
    }
}
