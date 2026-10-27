using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.Distribution.Common;
using VPay.Distribution.Http.Abstractions;
using VPay.DocSys.Storage;

namespace VPay.Distribution
{

    public class RemittanceService : IRemittanceService
    {
        private readonly IRemittancePartnerNetworkApiClient _remittancePartnerNetworkApiClient;
        private readonly IRemitPartnerNetworkApi _remitPartnerNetworkApi;
        private readonly IStorageManager _storageManager;
        private readonly ILogger<RemittanceService> _logger;

        public RemittanceService(
            IRemittancePartnerNetworkApiClient remittancePartnerNetworkApiClient,
            IRemitPartnerNetworkApi remitPartnerNetworkApi,
            IStorageManager storageManager,
            ILogger<RemittanceService> logger)
        {
            _remittancePartnerNetworkApiClient = remittancePartnerNetworkApiClient ?? throw new ArgumentNullException(nameof(remittancePartnerNetworkApiClient));
            _remitPartnerNetworkApi = remitPartnerNetworkApi ?? throw new ArgumentNullException(nameof(remitPartnerNetworkApi));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Post the remittance document for the given STP payment via Remit Partner Network API.
        /// </summary>
        /// <param name="jsonPayload">The contents of the STP payment to post in the form of a JSON payload.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>True if posting was successful; False otherwise.</returns>
        public async Task<bool> PostB2BRemitDocument(
            string jsonPayload,
            CancellationToken cancellationToken = default)
        {
            // Validate input.
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                _logger.LogWarning("NO JSON payload provided for B2B remit document validation");
                return false;
            }

            bool valid;
            try
            {
                // TODO: Use OAuth token here via injection from configuration settings.
                // var bearer = configuration.GetSection("RemittancePartnerNetworkApiRequestHeaders").GetValue<string>("ApiToken");
                var bearer = string.Empty;
                var (documentAccepted, responseBody) = await _remitPartnerNetworkApi
                    .PostB2BRemittanceDocument(jsonPayload, bearer, cancellationToken)
                    .ConfigureAwait(false);

                valid = documentAccepted;
            }
            catch (Refit.ApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("NOT FOUND during call to Remit Partner Network API for B2B remit document posting ({StatusCode}): {ErrorMessage}", ex.StatusCode, ex.Message);
                    return false;
                }

                _logger.LogError(ex, "ERROR during call to Remit Partner Network API for B2B remit document posting ({StatusCode}): {ErrorMessage}", ex.StatusCode, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during call to Remit Partner Network API for B2B remit posting validation");
                return false;
            }

            return valid;
        }

        /// <summary>
        /// Validate the remittance document for the given STP payment via Remit Partner Network API.
        /// </summary>
        /// <param name="jsonPayload">The contents of the STP payment to post in the form of a JSON payload.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>True if validation was successful; False otherwise.</returns>
        public async Task<bool> ValidateB2BRemitDocument(
            string jsonPayload,
            CancellationToken cancellationToken = default)
        {
            // Validate input.
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                _logger.LogWarning("NO JSON payload provided for B2B remit document validation");
                return false;
            }

            bool valid;
            try
            {
                // TODO: Use OAuth token here via injection from configuration settings.
                // var bearer = configuration.GetSection("RemittancePartnerNetworkApiRequestHeaders").GetValue<string>("ApiToken");
                var bearer = string.Empty;
                var (documentAccepted, responseBody) = await _remitPartnerNetworkApi
                    .ValidateB2BRemittanceDocument(jsonPayload, bearer, cancellationToken)
                    .ConfigureAwait(false);

                valid = documentAccepted;
            }
            catch (Refit.ApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("NOT FOUND during call to Remit Partner Network API for B2B remit document validation ({StatusCode}): {ErrorMessage}", ex.StatusCode, ex.Message);
                    return false;
                }

                _logger.LogError(ex, "ERROR during call to Remit Partner Network API for B2B remit document validation ({StatusCode}): {ErrorMessage}", ex.StatusCode, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR occurred during call to Remit Partner Network API for B2B remit document validation");
                return false;
            }

            return valid;
        }

        /// <summary>
        /// Post the remittance document for the given STP payment via Remit Partner Network API.
        /// </summary>
        /// <param name="transactionId">The transaction Id of the STP payment to post.</param>
        /// <param name="enrollmentType">The enrollment type of the STP payment to post: B2B or C2B.</param>
        /// <param name="jsonPayload">The contents of the STP payment to post in the form of a JSON payload.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>True if post was accepted; False otherwise.</returns>
        public async Task<bool> PostRemitDocument(
            long transactionId,
            string enrollmentType,
            string jsonPayload,
            CancellationToken cancellationToken = default)
        {
            // Validate input.
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                _logger.LogWarning("NO JSON payload provided for B2B remit document validation");
                return false;
            }

            // Parse enrollment type.
            if (!Enum.TryParse(enrollmentType, out RemittanceNetworkEnrollmentTypes networkEnrollmentType))
            {
                //_logger.LogWarning("Invalid enrollment type {EnrollmentType} for STP payment with Transaction ID {TransactionId}", enrollmentType, transactionId);
                _logger.LogWarning("Invalid enrollment type for STP payment");
                return false;
            }

            // Call the Remit Partner Network API
            bool docAccepted;
            string responseBody;
            (docAccepted, responseBody) = await _remittancePartnerNetworkApiClient
                .PostRemittanceDocument(networkEnrollmentType, jsonPayload, cancellationToken)
                .ConfigureAwait(false);

            if (docAccepted)
            {
                //_logger.LogDebug("Validated Remit Document for STP payment with Transaction ID {TransactionId} ({EnrollmentType})", transactionId, enrollmentType);
                _logger.LogDebug("Validated Remit Document for STP payment");
            }
            else
            {
                //_logger.LogError("ERROR Posting Remit Document for STP payment with Transaction ID {TransactionId} ({EnrollmentType}): {ErrorMessage}", transactionId, enrollmentType, responseBody);
                _logger.LogError("ERROR Posting Remit Document for STP payment: {ErrorMessage}", responseBody);
            }

            return docAccepted;
        }

        /// <summary>
        /// Validate the remittance document for the given STP payment via Remit Partner Network API.
        /// </summary>
        /// <param name="transactionId">The transaction Id of the STP payment to post.</param>
        /// <param name="enrollmentType">The enrollment type of the STP payment to post: B2B or C2B.</param>
        /// <param name="jsonPayload">The contents of the STP payment to post in the form of a JSON payload.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>True if validation was successful; False otherwise.</returns>
        public async Task<bool> ValidateRemitDocument(
            long transactionId,
            string enrollmentType,
            string jsonPayload,
            CancellationToken cancellationToken = default)
        {
            // Parse enrollment type.
            if (!Enum.TryParse(enrollmentType, out RemittanceNetworkEnrollmentTypes networkEnrollmentType))
            {
                //_logger.LogWarning("Invalid enrollment type {EnrollmentType} for STP payment with Transaction ID {TransactionId}", enrollmentType, transactionId);
                _logger.LogWarning("Invalid enrollment type for STP payment");
                return false;
            }

            // Call the Remit Partner Network API
            bool docAccepted;
            string responseBody;
            (docAccepted, responseBody) = await _remittancePartnerNetworkApiClient
                .ValidateRemittanceDocument(networkEnrollmentType, jsonPayload, cancellationToken)
                .ConfigureAwait(false);

            if (docAccepted)
            {
                //_logger.LogDebug("Validated Remit Document for STP payment with Transaction ID {TransactionId} ({EnrollmentType})", transactionId, enrollmentType);
                _logger.LogDebug("Validated Remit Document for STP payment");
            }
            else
            { 
                //_logger.LogError("ERROR Validating Remit Document for STP payment with Transaction ID {TransactionId} ({EnrollmentType}): {ErrorMessage}", transactionId, enrollmentType, responseBody);
                _logger.LogError("ERROR Validating Remit Document for STP payment: {ErrorMessage}", responseBody);
            }

            return docAccepted;
        }
    }
}
