using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Enums;

namespace VPay.DocSys.Generation;

public class RemittancePartnerNetworkApiClient : IRemittancePartnerNetworkApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RemittancePartnerNetworkApiClient> _logger;
    private readonly IReadOnlyDictionary<string, string> _endPointDictionary;
    private readonly IAsyncPolicy _retryPolicy;

    public RemittancePartnerNetworkApiClient(ApiSettings apiSettings, HttpClient httpClient,
        ILogger<RemittancePartnerNetworkApiClient> logger)
    {
        ArgumentHelpers.ThrowIf.IsNull(apiSettings, nameof(apiSettings));
        ArgumentHelpers.ThrowIf.IsNull(httpClient, nameof(httpClient));
        ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

        _httpClient = httpClient;
        _logger = logger;
        _endPointDictionary = apiSettings["RemittancePartnerNetworkApi"].EndPointFragments;
        _retryPolicy = new HttpRequestPolicyHolder(_logger).HttpGetPolicy;
    }

    public async Task<(bool documentAccepted, string responseBody)> ValidateRemittanceDocument(
        RemittanceNetworkEnrollmentTypes enrollmentType,
        string remittanceData,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling Remittance Partner Network API Validate Document Endpoint");

        try
        {
            string endPoint = "";

            if (enrollmentType == RemittanceNetworkEnrollmentTypes.C2B)
            {
                endPoint = _endPointDictionary["ValidateC2BRemittanceDocument"];
            }
            else if (enrollmentType == RemittanceNetworkEnrollmentTypes.B2B)
            {
                endPoint = _endPointDictionary["ValidateB2BRemittanceDocument"];
            }

            var content = new StringContent(remittanceData, Encoding.UTF8, "application/json");

            using var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsync(endPoint, content));
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Unsuccessful response from Remittance Partner Network API Response Code:{ResponseCode}",
                    response.StatusCode.ToString().Replace(Environment.NewLine, ""));
            }

            return (response.IsSuccessStatusCode, responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during call to Remittance Partner Network API");

            return (false, ex.ToString());
        }
    }

    public async Task<(bool documentAccepted, string responseBody)> PostRemittanceDocument(
        RemittanceNetworkEnrollmentTypes enrollmentType,
        string remittanceData,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling Remittance Partner Network API Post Endpoint");

        try
        {
            string endPoint = "";

            if (enrollmentType == RemittanceNetworkEnrollmentTypes.C2B)
            {
                endPoint = _endPointDictionary["PostC2BRemittanceDocument"];
            }
            else if (enrollmentType == RemittanceNetworkEnrollmentTypes.B2B)
            {
                endPoint = _endPointDictionary["PostB2BRemittanceDocument"];
            }

            var content = new StringContent(remittanceData, Encoding.UTF8, "application/json");

            using var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsync(endPoint, content));
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Unsuccessful response from Remittance Partner Network API Response Code:{ResponseCode}",
                    response.StatusCode.ToString().Replace(Environment.NewLine, ""));
            }

            return (response.IsSuccessStatusCode, responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during call to Remittance Partner Network API");

            return (false, ex.ToString());
        }
    }
}
