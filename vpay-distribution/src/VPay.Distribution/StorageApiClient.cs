using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.DocSys.Storage;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.Distribution
{
    public class StorageApiClient : IStorageApiClient
    {
        private static readonly JsonSerializer _jsonSerializer = new JsonSerializer();

        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private readonly HttpRequestPolicyHolder _httpRequestPolicies;

        public StorageApiClient(
            HttpClient httpClient,
            HttpRequestPolicyHolder httpRequestPolicyHolder,
            ILogger<StorageApiClient> logger)
        {
            ThrowIf.IsNull(httpClient, nameof(httpClient));
            ThrowIf.IsNull(httpRequestPolicyHolder, nameof(httpRequestPolicyHolder));
            ThrowIf.IsNull(logger, nameof(logger));

            _client = httpClient;
            _logger = logger;
            _httpRequestPolicies = httpRequestPolicyHolder;
        }

        public async Task<IEnumerable<IArchiveFile>> GetArchivesForTransactionId(long transactionId, CancellationToken cancellationToken)
        {
            var endpoint = $"api/Storage/Transaction/{transactionId}/ArchiveWithoutFilter";
            var request = _client.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            using (_logger.BeginScope(new Dictionary<string, object> { { "TransactionId", transactionId } }))
            using (var response = await _httpRequestPolicies.HttpGetPolicy.ExecuteAsync(() => request))
            {
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Could not get document archives for {TransactionId}, StatusCode: {StatusCode}",
                        transactionId, response.StatusCode);

                    throw new HttpRequestException(response.ReasonPhrase);
                }

                using (var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    return _jsonSerializer.Deserialize<IEnumerable<ArchiveFile>>(jsonReader);
                }
            }
        }

        public async Task<byte[]> GetCustomSendBackCheck(long transactionId, CancellationToken cancellationToken)
        {
            var endpoint = $"api/Storage/Archive/Detail/Download/{transactionId}/CustomSendBackCheck";
            var request = _client.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            using (_logger.BeginScope(new Dictionary<string, object> { { "TransactionId", transactionId } }))
            using (var response = await _httpRequestPolicies.HttpGetPolicy.ExecuteAsync(() => request))
            {
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Could not get custom send back check from archive: TransactionId: {TransactionId}, StatusCode: {StatusCode}", transactionId, response.StatusCode);
                    throw new HttpRequestException(response.ReasonPhrase);
                }

                return await response.Content.ReadAsByteArrayAsync();
            }
        }       
    }
}
