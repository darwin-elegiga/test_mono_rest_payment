using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Generation.Common.Exceptions;
using VPay.DocSys.Generation.Common.Edi835;
using VPay.DocSys.Generation.Edi835.Application.Models;
using VPay.DocSys.Generation.Edi835.Application.PayFile;

namespace VPay.DocSys.Generation.Edi835.Application
{
    public class ReconReportsApi : IReconReportsApi
    {
        private readonly HttpClient _client;
        private readonly ReconReportApiSettings _settings;
        private readonly ILogger _logger;

        public ReconReportsApi(HttpClient httpClient, ReconReportApiSettings settings, ILogger<ReconReportsApi> logger)
        {
            _client = httpClient;
            _settings = settings;
            _logger = logger;
        }

        public async Task<BankAccount> GetMain835Account(CancellationToken cancellationToken)
        {
            using (var getRequest = await _client.GetAsync("/api/bank-accounts/835-metabank", cancellationToken).ConfigureAwait(false))
            {
                var stream = await getRequest.Content.ReadAsStreamAsync().ConfigureAwait(false);

                if (getRequest.IsSuccessStatusCode)
                {
                    return DeserializeJsonFromStream<BankAccount>(stream);
                }

                var content = await StreamToStringAsync(stream).ConfigureAwait(false);

                _logger.LogError("Failed Getting Main 835 Account {StatusCode} :\r\n{ResponseBody}", getRequest.StatusCode, content);

                throw new ApiException((int)getRequest.StatusCode, "Failed Getting Main 835 Account");
            }
        }

        public async Task<TransactionBankAccount> GetBankAccountForTransactionId(long transactionId, CancellationToken cancellationToken)
        {
            using (var getRequest = await _client.GetAsync($"/api/transactions/{transactionId}/bank-account", cancellationToken).ConfigureAwait(false))
            {
                var stream = await getRequest.Content.ReadAsStreamAsync().ConfigureAwait(false);

                if(getRequest.IsSuccessStatusCode)
                {
                    return DeserializeJsonFromStream<TransactionBankAccount>(stream);
                }
                else if(_settings.MockBankAccountForTransactions)
                {
                    return new TransactionBankAccount()
                    {
                        TransactionId = transactionId,
                        AccountNumber = "100001000010010000",
                        RouteNumber = "1000010000",
                    };
                }

                var content = await StreamToStringAsync(stream).ConfigureAwait(false);

                _logger.LogError("Failed Getting Bank Account for the TransactionId {TransactionId} {StatusCode} :\r\n{ResponseBody}", transactionId, getRequest.StatusCode, content);

                throw new ApiException((int)getRequest.StatusCode, "Failed Getting the Bank account for the TransactionId");
            }
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

            using (var sr = new StreamReader(stream))
            using (var jtr = new JsonTextReader(sr))
            {
                var js = new JsonSerializer();

                return js.Deserialize<T>(jtr);
            }
        }

        private static async Task<string> StreamToStringAsync(Stream stream)
        {
            if (stream != null)
            {
                using (var sr = new StreamReader(stream))
                {
                    return await sr.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            return null;
        }
    }
}
