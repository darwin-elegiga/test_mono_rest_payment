using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Helpers;
using VPay.Distribution.Common.Models;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.Queuing;

namespace VPay.Distribution
{
    public class DistributionRedCardQueueMessageProcessor : IDistributionRedCardQueueMessageProcessor
    {
        private readonly ILogger _logger;
        private readonly IRedCardRepository _redCardRepository;
        private readonly IDistributionRepository _distributionRepository;
        private readonly IAuxiliaryFileProcessor _auxiliaryFileProcessor;
        private readonly IMessagePublisherService _messagePublisherService;
        private readonly RedCardDistributionQueueConfigurationSettings _distributionQueueConfigurationSettings;
        private readonly FaxDistributionQueueConfigurationSettings _faxDistributionQueueConfigurationSettings;

        private readonly int _statusCheckDelayInSeconds = 600;      // 600 seconds = 10 minutes
        private readonly int _warningIssueDelayInSeconds = 86400;   // 86,400 seconds = 24 hours

        //Added as a static function so unit tests can override the detail so they run faster.
        public static Func<int, Task> Delay = (delayInMs) => Task.Delay(delayInMs);

        public DistributionRedCardQueueMessageProcessor(
            ILogger<DistributionRedCardQueueMessageProcessor> logger,
            IRedCardRepository redCardRepository,
            IDistributionRepository distributionRepository,
            IAuxiliaryFileProcessor auxiliaryFileProcessor,
            IMessagePublisherService messagePublisherService,
            RedCardDistributionQueueConfigurationSettings distributionQueueConfigurationSettings,
            FaxDistributionQueueConfigurationSettings faxDistributionQueueConfigurationSettings,
            DelayConfig delayConfig)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));
            ArgumentHelpers.ThrowIf.IsNull(redCardRepository, nameof(redCardRepository));
            ArgumentHelpers.ThrowIf.IsNull(distributionRepository, nameof(distributionRepository));
            ArgumentHelpers.ThrowIf.IsNull(auxiliaryFileProcessor, nameof(auxiliaryFileProcessor));
            ArgumentHelpers.ThrowIf.IsNull(messagePublisherService, nameof(messagePublisherService));
            ArgumentHelpers.ThrowIf.IsNull(distributionQueueConfigurationSettings, nameof(distributionQueueConfigurationSettings));
            ArgumentHelpers.ThrowIf.IsNull(faxDistributionQueueConfigurationSettings, nameof(faxDistributionQueueConfigurationSettings));
            ArgumentHelpers.ThrowIf.IsNull(delayConfig, nameof(delayConfig));

            _logger = logger;
            _redCardRepository = redCardRepository;
            _distributionRepository = distributionRepository;
            _auxiliaryFileProcessor = auxiliaryFileProcessor;
            _messagePublisherService = messagePublisherService;
            _distributionQueueConfigurationSettings = distributionQueueConfigurationSettings;
            _faxDistributionQueueConfigurationSettings = faxDistributionQueueConfigurationSettings;
            _statusCheckDelayInSeconds = delayConfig.StatusCheckDelay;
            _warningIssueDelayInSeconds = delayConfig.WarningIssueDelay;
        }

        public async Task<bool> ProcessMessage(DocDistMsg message, CancellationToken cancellationToken)
        {
            using (_logger.CreateLogScope(message))
            {
                try
                {
                    // verify message is for a redcard tpa
                    if (!message.IsRedCard)
                    {
                        throw _logger.LogErrorAndReturnException($"Message is for a non-redcard TPA - StatusFileId: {message.PaymentStatusFileId}");
                    }

                    // if its not yet time to process this message
                    if (message.NextStatusCheck > DateTime.Now)
                    {
                        // requeue it
                        return this.RequeueMessage(message);
                    }

                    // get the current redcard file status from database
                    var fileStatus = await _redCardRepository.GetFileStatus(message.PaymentStatusFileId);

                    // if status is parsed
                    if (fileStatus == RedCardFileStatusCode.StatusFileParsed)
                    {
                        // requeue the message to evaluate again later
                        return this.RequeueMessageWithDelay(message);
                    }
                    // else, if status is AuxFileGenerated
                    else if (fileStatus == RedCardFileStatusCode.AuxiliaryFileGenerated)
                    {
                        // and, if all docs are generated
                        if (await this.IsAllTransactionsFilesGenerated(message))
                        {
                            // transfer the aux file
                            await this.TransferAuxiliaryFile(message);

                            // requeue message with no delay
                            return this.RequeueMessage(message);
                        }

                        // otherwise, requeue message to be checked again after delay
                        return this.RequeueMessageWithDelay(message);
                    }
                    // else, if status is AuxFileTransferred
                    else if (fileStatus == RedCardFileStatusCode.AuxiliaryFileTransferred)
                    {
                        // and, if all docs are generated
                        if (await this.IsAllTransactionsFilesGenerated(message))
                        {
                            // distribute the transactions that are ready to be distributed
                            return await this.ProcessTransactions(message);
                        }

                        // otherwise, requeue message to be checked again after delay
                        return this.RequeueMessageWithDelay(message);
                    }
                }
                catch (Exception exception)
                {
                    throw _logger.LogException(exception, "An unexpected error occurred while processing message.");
                }

                // if file status is any other status, then its not valid status file or its not for a redcard TPA
                throw _logger.LogErrorAndReturnException($"Status file record not found or is for a non-redcard TPA - StatusFileID: {message.PaymentStatusFileId}");
            }
        }

        private async Task<bool> IsAllTransactionsFilesGenerated(DocDistMsg message)
        {
            // get transaction status for all transactions in payment status file
            var transactionStatusList = await _redCardRepository.GetCurrentTransactionStatusForStatusFileId(message.PaymentStatusFileId);

            // if the current status for all transactions is DocsGenerated, then they're all done
            return transactionStatusList.All(n => n.StatusCode >= RedCardTransactionStatusCode.DocumentsGenerated);
        }

        private bool RequeueMessage(DocDistMsg message)
        {
            var msgText = JsonConvert.SerializeObject(message);

            _messagePublisherService.PublishMessage(_distributionQueueConfigurationSettings, msgText);

            _logger.LogTrace($"Message Re-queued (As-Is)");

            return true;
        }

        private bool RequeueMessageWithDelay(DocDistMsg message)
        {
            message.NextStatusCheck = DateTime.Now.Add(TimeSpan.FromSeconds(_statusCheckDelayInSeconds));

            var msgText = JsonConvert.SerializeObject(message);

            _messagePublisherService.PublishMessage(_distributionQueueConfigurationSettings, msgText);

            _logger.LogDebug($"Message Re-queued (with {_statusCheckDelayInSeconds} second delay)");

            return true;
        }

        private async Task TransferAuxiliaryFile(DocDistMsg message)
        {
            var result = await _auxiliaryFileProcessor.Process(new AuxiliaryMessage
            {
                PaymentStatusFileId = message.PaymentStatusFileId,
                StatusFileName = message.StatusFilePath,
            });

            foreach (var f in result.FileNames)
            {
                _logger.LogInformation("Auxiliary file transferred: {AuxFileName}", f);
            }
            foreach (var f in result.ControlFileNames)
            {
                _logger.LogInformation("Auxiliary control file transferred: {AuxCtrlFileName}", f);
            }
        }

        private async Task<bool> ProcessTransactions(DocDistMsg message)
        {
            // build a dictionary of TransactionStatusRecords from the SQL table (keyed by TransactionId)
            var statusLookup = (await _redCardRepository.GetCurrentTransactionStatusForStatusFileId(message.PaymentStatusFileId)).ToDictionary(r => r.TransactionId, r => r.StatusCode);

            // initialize new lists to add transactions to based on what's going to be done with it
            var transactionsToDistributeFax = new List<DistTxnData>();
            var transactionsToRequeue = new List<DistTxnData>();

            // for each transaction in the message
            foreach (var transaction in message.Transactions)
            {
                if (statusLookup.TryGetValue(transaction.TxnId, out var status))
                {
                    // if status is Ready for Distribution, send to distribution, then discard
                    if (status == RedCardTransactionStatusCode.ReadyForDistribution)
                    {
                        using (_logger.CreateLogScope(transaction))
                        {
                            transactionsToDistributeFax.Add(transaction);

                            _logger.LogInformation("Add {TransactionId} to {DistributionMethod} Distribution Message", transaction.TxnId, DistributionMethods.Fax);
                        }
                    }
                    // if the status is anything else besides Complete, requeue the transaction
                    else if (status != RedCardTransactionStatusCode.TransactionComplete)
                    {
                        transactionsToRequeue.Add(transaction);
                    }
                }
                // if we didn't find a status record, just requeue the transaction
                else
                {
                    transactionsToRequeue.Add(transaction);
                }
            }

            // for these, publish to the fax queue
            if (transactionsToDistributeFax.Count > 0)
            {
                message.Transactions = transactionsToDistributeFax;
                message.BatchSize = transactionsToDistributeFax.Count;

                _messagePublisherService.PublishMessage(_faxDistributionQueueConfigurationSettings, JsonConvert.SerializeObject(message));
            }

            // requeue the message with only these transactions
            if (transactionsToRequeue.Count > 0)
            {
                message.Transactions = transactionsToRequeue;
                message.BatchSize = transactionsToRequeue.Count;

                return this.RequeueMessageWithDelay(message);
            }

            // no transactions left, so update the file distribution status and we're done with this message
            await _distributionRepository.TryUpdateDistributionStatus(message.PaymentStatusFileId, DistributionMethods.Fax);

            return true;
        }
    }
}
