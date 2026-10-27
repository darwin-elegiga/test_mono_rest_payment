using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySqlX.XDevAPI.Common;
using Polly;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Common.Models;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation;

public class PaymentRoutingApiClient : IPaymentRoutingApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentRoutingApiClient> _logger;
    private readonly IReadOnlyDictionary<string, string> _endPointDictionary;

    private readonly IAsyncPolicy _retryPolicy;

    public PaymentRoutingApiClient(
            ApiSettings apiSettings,
            HttpClient httpClient,
            ILogger<PaymentRoutingApiClient> logger)
    {
        ThrowIf.IsNull(apiSettings, nameof(apiSettings));
        ThrowIf.IsNull(httpClient, nameof(httpClient));
        ThrowIf.IsNull(logger, nameof(logger));

        _httpClient = httpClient;
        _logger = logger;

        _endPointDictionary = apiSettings["PaymentRoutingApi"].EndPointFragments;
        _retryPolicy = new HttpRequestPolicyHolder(_logger).HttpGetPolicy;
    }

    public async Task<Outbound835DocumentResponse> GetOutbound835Document(long transactionId, CancellationToken cancellationToken)
    {
        var endPoint = _endPointDictionary["GetOutbound835Document"] + $"{transactionId}/download";
        try
        {
            var request = _httpClient.GetAsync(endPoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            using (_logger.BeginScope(new
            {
                TransactionId = transactionId.ToString().Replace(Environment.NewLine, ""),
            }.ToDictionary()))
            using (var response = await _retryPolicy.ExecuteAsync(() => request))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Could not GET Outbound835Document for {TransactionId}, {StatusCode}",
                        transactionId.ToString().Replace(Environment.NewLine, ""),
                        response.StatusCode);

                    return null;
                }

                var edi835Stream = new MemoryStream(await response.Content.ReadAsByteArrayAsync(), writable: false);
                edi835Stream.Position = 0;

                return new Outbound835DocumentResponse
                {
                    ContentType = response.Content.Headers.ContentType?.MediaType ?? "text/plain",
                    FileName = response.Content.Headers.ContentDisposition?.FileName ?? $"edi-{transactionId}.835",
                    Stream = edi835Stream
                };
            }
        }
        catch (Exception e) when (e.LogWithScope(_logger))
        {
            return null;
        }
    }
}
