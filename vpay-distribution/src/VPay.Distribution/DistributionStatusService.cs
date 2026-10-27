using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.Distribution.Common;
using VPay.Distribution.Common.Models;

namespace VPay.Distribution
{
    public class DistributionStatusService : IDistributionStatusService
    {
        private readonly ILogger _logger;
        private readonly IDistributionRepository _distributionRepository;
        private readonly IRedCardRepository _redCardRepository;

        public DistributionStatusService(
            IDistributionRepository distributionRepository,
            IRedCardRepository redCardRepository,
            ILogger<DistributionStatusService> logger)
        {
            _logger = logger;
            _distributionRepository = distributionRepository;
            _redCardRepository = redCardRepository;
        }

        public async Task SetDistributionStatusComplete(string statusFilePath, string distType)
        {
            using(_logger.BeginScope(new Dictionary<string, object>()
            {
                {"Service", nameof(DistributionStatusService) },
                {"StatusFile", statusFilePath },
                {"DistType", distType },
            }))
            {
                try
                {
                    await this.UpdateDistributionStatus(statusFilePath, distType);
                }
                catch(Exception exception)
                {
                    _logger.LogError(exception, $"Exception in {nameof(SetDistributionStatusComplete)}: {exception.Message}");
                    throw;
                }
            }
        }

        private async Task UpdateDistributionStatus(string statusFilePath, string distType)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(statusFilePath, nameof(statusFilePath));

            var statusFileId = (await _distributionRepository.GetPaymentStatusFileId(statusFilePath)) ?? 0;

            if(statusFileId == 0)
            {
                throw new Exception("Payment status file not found: '{statusFilePath}'");
            }

            var includeFax = string.IsNullOrEmpty(distType) || distType.Equals("F", StringComparison.OrdinalIgnoreCase);
            var includeMail = string.IsNullOrEmpty(distType) || distType.Equals("M", StringComparison.OrdinalIgnoreCase);

            var transactions = await _distributionRepository.GetTransactions(statusFileId);

            if(includeFax)
            {
                await this.UpdateDistributionStatusForFax(statusFileId, transactions);
            }

            if(includeMail)
            {
                await this.UpdateDistributionStatusForMail(statusFileId, transactions);
            }

            await this.UpdateRedCardTransactionStatus(statusFileId, transactions, includeFax);
        }

        private async Task UpdateDistributionStatusForFax(int statusFileID, IEnumerable<DocumentTransaction> transactions)
        {
            if(transactions.Any(t => t.OriginalDistributionType == 'F' || t.OriginalDistributionType == 'R'))
            {
                await _distributionRepository.TryUpdateDistributionStatus(statusFileID, DistributionMethods.Fax);
                _logger.LogInformation($"Update fax distribution status for Status File ID {statusFileID}");
            }
            else
            {
                _logger.LogInformation($"No fax transaction for update distribution status for Status File ID {statusFileID}");
            }
        }

        private async Task UpdateDistributionStatusForMail(int statusFileID, IEnumerable<DocumentTransaction> transactions)
        {
            if(transactions.Any(t => t.OriginalDistributionType != 'F' && t.OriginalDistributionType != 'R'))
            {
                await _distributionRepository.TryUpdateDistributionStatus(statusFileID, DistributionMethods.Mail);
                _logger.LogInformation($"Update mail distribution status for Status File ID {statusFileID}");
            }
            else
            {
                _logger.LogInformation($"No mail transactions for update distribution status for Status File ID {statusFileID}");
            }
        }

        private async Task UpdateRedCardTransactionStatus(int statusFileID, IEnumerable<DocumentTransaction> transactions, bool includeFax)
        {
            if(await _redCardRepository.GetFileStatus(statusFileID) != RedCardFileStatusCode.NotFound)
            {
                foreach(var transaction in transactions)
                {
                    if(includeFax)
                    {
                        if(transaction.OriginalDistributionType == 'F' || transaction.OriginalDistributionType == 'R')
                        {
                            await _redCardRepository.UpdateTransactionStatus(transaction.TransactionID, RedCardTransactionStatusCode.SentToDistribution);
                            _logger.LogInformation($"Update red card transaction status to '{nameof(RedCardTransactionStatusCode.SentToDistribution)}' for Status File ID {statusFileID}");
                        }
                    }

                    await _redCardRepository.UpdateTransactionStatus(transaction.TransactionID, RedCardTransactionStatusCode.TransactionComplete);
                    _logger.LogInformation($"Update red card transaction status to '{nameof(RedCardTransactionStatusCode.TransactionComplete)}' for Status File ID {statusFileID}");
                }
            }
        }

        /// <summary>
        /// Retrieves the PaymentStatusFileId from the statusFilePath and clears the corresponding DistributionStatus so that it may be re-ran when there's an error
        /// </summary>
        /// <param name="statusFilePath">Full path to the status file</param>
        /// <param name="distMethod">Distribution type, must be 'M', 'F', or 'N' (Mail, Fax, or Both)</param>
        /// <returns></returns>
        public async Task<ClearDistributionStatusResults> ClearDistributionStatus(string statusFilePath, DistributionMethod distMethod)
        {
            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                {"Service", nameof(DistributionStatusService) },
                {"StatusFile", statusFilePath },
                {"DistType", distMethod.ToString() },
            }))
            {
                var statusFileId = (await _distributionRepository.GetPaymentStatusFileId(statusFilePath));

                if (statusFileId == null)
                {
                    throw new Exception($"Payment status file not found: '{statusFilePath}'");
                }

                var results = new ClearDistributionStatusResults();

                try
                {
                    if (distMethod == DistributionMethod.Fax)
                    {
                        results.IsFaxCleared =
                            (await _distributionRepository.ClearDistributionStatus(statusFileId.Value, DistributionMethod.Fax)) == 1;
                    }
                    else if (distMethod == DistributionMethod.Mail)
                    {
                        results.IsMailCleared =
                            (await _distributionRepository.ClearDistributionStatus(statusFileId.Value, DistributionMethod.Mail)) == 1;
                    }
                    else
                    {
                        results.IsFaxCleared =
                            (await _distributionRepository.ClearDistributionStatus(statusFileId.Value, DistributionMethod.Fax)) == 1;

                        results.IsMailCleared =
                            (await _distributionRepository.ClearDistributionStatus(statusFileId.Value, DistributionMethod.Mail)) == 1;
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Exception in {nameof(ClearDistributionStatus)}: {exception.Message}");
                    throw;
                }

                return results;
            }
        }
    }
}
