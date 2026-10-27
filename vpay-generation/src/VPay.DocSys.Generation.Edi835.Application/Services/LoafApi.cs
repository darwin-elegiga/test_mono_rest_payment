using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Generation.Common.Exceptions;
using VPay.DocSys.Generation.Edi835.Application.Models;
using VPay.DocSys.Generation.Edi835.Application.PayFile;

namespace VPay.DocSys.Generation.Edi835.Application.Services
{
    public class LoafApi : ILoafApi
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public LoafApi(HttpClient httpClient, ILogger<LoafApi> logger)
        {
            _client = httpClient;
            _logger = logger;
        }

        public async Task<LoafTransactionBankInfoDto> GetOptumBankInfo(string transactionId, CancellationToken cancellationToken)
        {
            using var getRequest = await _client.GetAsync($"/api/OptumBankInfo/{transactionId}?referenceId={transactionId}", cancellationToken).ConfigureAwait(false);
            var stream = await getRequest.Content.ReadAsStreamAsync().ConfigureAwait(false);
            if (getRequest.IsSuccessStatusCode)
            {
                return DeserializeJsonFromStream<LoafTransactionBankInfoDto>(stream);
            }

            // If the data cannot be found, we'll return null for the banking info
            if (getRequest.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            var content = await StreamToStringAsync(stream).ConfigureAwait(false);

            _logger.LogError("Failed Getting Optum Bank Info {StatusCode} :\r\n{ResponseBody}", getRequest.StatusCode, content);

            throw new ApiException((int)getRequest.StatusCode, "Failed getting Optum bank info");
        }

        /// <summary>
        /// This will Deserialize the json from a stream
        /// Using https://johnthiriet.com/efficient-api-calls/
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        private static T DeserializeJsonFromStream<T>(Stream stream)
        {
            if (stream?.CanRead != true)
            {
                return default;
            }

            using var sr = new StreamReader(stream);
            using var jtr = new JsonTextReader(sr);
            var js = new JsonSerializer();

            return js.Deserialize<T>(jtr);
        }

        private static async Task<string> StreamToStringAsync(Stream stream)
        {
            if (stream == null)
            {
                return null;
            }

            using var sr = new StreamReader(stream);
            return await sr.ReadToEndAsync().ConfigureAwait(false);

        }
    }
}
