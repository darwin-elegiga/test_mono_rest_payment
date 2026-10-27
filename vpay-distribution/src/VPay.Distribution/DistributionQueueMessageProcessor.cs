using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.Distribution.Common;
using VPay.Distribution.Common.Helpers;
using VPay.Distribution.Common.Models;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.Queuing;

namespace VPay.Distribution
{
    public class DistributionQueueMessageProcessor : IDistributionQueueMessageProcessor
    {
        private readonly ILogger _logger;
        private readonly IDistributionRepository _distributionRepository;
        private readonly IMessagePublisherService _messagePublisherService;
        private readonly DistributionQueueConfigurationSettings _distributionQueueConfigurationSettings;
        private readonly FaxDistributionQueueConfigurationSettings _faxDistributionQueueConfigurationSettings;
        private readonly MailDistributionQueueConfigurationSettings _mailDistributionQueueConfigurationSettings;
        private readonly SendBackDistributionQueueConfigurationSettings _sendBackDistributionQueueConfigurationSettings;
        private readonly STPQueueConfigurationSettings _stpDistributionQueueConfigurationSettings;

        private readonly int _statusCheckDelayInSeconds = 60;       //    60 seconds = 1 minute
        private readonly int _warningIssueDelayInSeconds = 1800;    // 1,800 seconds = 30 minutes

        public DistributionQueueMessageProcessor(
            ILogger<DistributionQueueMessageProcessor> logger,
            IDistributionRepository distributionRepository,
            IMessagePublisherService messagePublisherService,
            DistributionQueueConfigurationSettings distributionQueueConfigurationSettings,
            FaxDistributionQueueConfigurationSettings faxDistributionQueueConfigurationSettings,
            MailDistributionQueueConfigurationSettings mailDistributionQueueConfigurationSettings,
            SendBackDistributionQueueConfigurationSettings sendBackDistributionQueueConfigurationSettings,
            STPQueueConfigurationSettings stpDistributionQueueConfigurationSettings,
            DelayConfig delayConfig)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));
            ArgumentHelpers.ThrowIf.IsNull(distributionRepository, nameof(distributionRepository));
            ArgumentHelpers.ThrowIf.IsNull(messagePublisherService, nameof(messagePublisherService));
            ArgumentHelpers.ThrowIf.IsNull(distributionQueueConfigurationSettings, nameof(distributionQueueConfigurationSettings));
            ArgumentHelpers.ThrowIf.IsNull(faxDistributionQueueConfigurationSettings, nameof(faxDistributionQueueConfigurationSettings));
            ArgumentHelpers.ThrowIf.IsNull(mailDistributionQueueConfigurationSettings, nameof(mailDistributionQueueConfigurationSettings));
            ArgumentHelpers.ThrowIf.IsNull(sendBackDistributionQueueConfigurationSettings, nameof(sendBackDistributionQueueConfigurationSettings));
            ArgumentHelpers.ThrowIf.IsNull(stpDistributionQueueConfigurationSettings, nameof(stpDistributionQueueConfigurationSettings));
            ArgumentHelpers.ThrowIf.IsNull(delayConfig, nameof(delayConfig));

            _logger = logger;
            _distributionRepository = distributionRepository;
            _messagePublisherService = messagePublisherService;
            _distributionQueueConfigurationSettings = distributionQueueConfigurationSettings;
            _faxDistributionQueueConfigurationSettings = faxDistributionQueueConfigurationSettings;
            _mailDistributionQueueConfigurationSettings = mailDistributionQueueConfigurationSettings;
            _sendBackDistributionQueueConfigurationSettings = sendBackDistributionQueueConfigurationSettings;
            _stpDistributionQueueConfigurationSettings = stpDistributionQueueConfigurationSettings;
            _statusCheckDelayInSeconds = delayConfig.StatusCheckDelay;
            _warningIssueDelayInSeconds = delayConfig.WarningIssueDelay;
        }

        public async Task<bool> ProcessMessage(DocDistMsg message, CancellationToken cancellationToken)
        {
            using (_logger.CreateLogScope(message))
            {
                // if its not yet time to process this message
                if (message.NextStatusCheck > DateTime.Now)
                {
                    // requeue it
                    return this.RequeueMessage(message);
                }

                // if the warning time has passed
                if (message.WarningTime <= DateTime.Now)
                {
                    // and any distributions are not complete
                    if (!message.FaxComplete || !message.NonFaxComplete || !message.StraightThruProcessingComplete || (message.IsCustomSendBackTpa && !message.CustomSendBackComplete))
                    {
                        // issue warning
                        this.IssueWarning(message);

                        // increment warning counter
                        message.WarningsIssued++;

                        // set next warning time
                        message.WarningTime = DateTime.Now.Add(TimeSpan.FromSeconds(_warningIssueDelayInSeconds));

                        // requeue message (with no delay so its picked up again immediately)
                        return this.RequeueMessage(message);
                    }
                }

                // if custom-send-backs have not been distributed (and, this is a custom-send-back tpa)
                if (!message.CustomSendBackComplete && message.IsCustomSendBackTpa)
                {
                    // if generation is complete for all custom-send-back payments
                    if (await _distributionRepository.GetIsSendBackPaymentsComplete(message.PaymentStatusFileId))
                    {
                        // publish to custom-send-back queue
                        await this.DistributeMessage(DistributionMethods.CustomSendBack, message);

                        // mark custom-send-backs as completed
                        message.CustomSendBackComplete = true;

                        // requeue message with no delay
                        return this.RequeueMessage(message);
                    }

                    // otherwise, requeue message to be checked again after delay
                    return this.RequeueMessageWithDelay(message);
                }

                // else, if faxes have not been distributed
                else if (!message.FaxComplete)
                {
                    // if generation is complete for all fax payments
                    if (await _distributionRepository.GetIsFaxPaymentsComplete(message.PaymentStatusFileId))
                    {
                        // distribute fax payments
                        await this.DistributeMessage(DistributionMethods.Fax, message);

                        // mark faxes as completed
                        message.FaxComplete = true;

                        // requeue message with no delay
                        return this.RequeueMessage(message);
                    }

                    // otherwise, requeue message to be checked again after delay
                    return this.RequeueMessageWithDelay(message);
                }

                // else, if non-faxes have not been distributed
                // this includes Mail and Display Only
                else if (!message.NonFaxComplete)
                {
                    // if generation is complete for all non-fax payments
                    if (await _distributionRepository.GetIsMailPaymentsComplete(message.PaymentStatusFileId, message.IsCustomSendBackTpa))
                    {
                        // distribute mail (non-fax) payments
                        await this.DistributeMessage(DistributionMethods.Mail, message);

                        // mark non-faxes as completed
                        message.NonFaxComplete = true;

                        // requeue message with no delay
                        return this.RequeueMessage(message);
                    }

                    // otherwise, requeue message to be checked again after delay
                    return this.RequeueMessageWithDelay(message);
                }
                else if (!message.StraightThruProcessingComplete)
                {
                    if (await _distributionRepository.GetIsStraightThruProcessingPaymentsComplete(message.PaymentStatusFileId, cancellationToken))
                    {
                        await this.DistributeMessage(DistributionMethods.StraightThruProcessing, message);

                        // mark straight thru processing payments as completed
                        message.StraightThruProcessingComplete = true;

                        // requeue message with no delay
                        return this.RequeueMessage(message);
                    }
                    // otherwise, requeue message to be checked again after delay
                    return this.RequeueMessageWithDelay(message);
                }

                // otherwise, all payments have been distributed so we're done with this message
                return true;
            }
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

        private void IssueWarning(DocDistMsg message)
        {
            var elapsed = DateTime.Now.Subtract(message.CreateDate);

            var eventId = new EventId(105, "DocSys Generation Warning");

            _logger.LogWarning(eventId, "DocSys Generation Warning: Document generation for status file id {PaymentStatusFileId} has been in process for {elapsed}.", message.PaymentStatusFileId, elapsed);
        }

        private async Task DistributeMessage(DistributionMethod distributionMethod, DocDistMsg message)
        {
            // if transactions have not already been distributed
            if (!(await _distributionRepository.GetDistributionStatus(message.PaymentStatusFileId, distributionMethod)).IsDistributionComplete)
            {
                // create a new message that contains only custom-send-back transactions
                var distMsg = await this.CreateDistributionMessage(message, distributionMethod);

                // if there are any transactions to distribute
                if (distMsg.BatchSize > 0)
                {
                    QueueConfigurationSettings queueConfigurationSettings;

                    switch (distributionMethod)
                    {
                        case DistributionMethod.Fax:
                            queueConfigurationSettings = _faxDistributionQueueConfigurationSettings;
                            break;

                        case DistributionMethod.Mail:
                            queueConfigurationSettings = _mailDistributionQueueConfigurationSettings;
                            break;

                        case DistributionMethod.CustomSendBack:
                            queueConfigurationSettings = _sendBackDistributionQueueConfigurationSettings;
                            break;

                        case DistributionMethod.StraightThruProcessing:
                            queueConfigurationSettings = _stpDistributionQueueConfigurationSettings;
                            break;

                        default:
                            throw new NotSupportedException($"Distribution method '{distributionMethod}' not supported.");
                    }

                    _messagePublisherService.PublishMessage(queueConfigurationSettings, JsonConvert.SerializeObject(distMsg));
                }
            }
        }

        private async Task<DocDistMsg> CreateDistributionMessage(DocDistMsg sourceMessage, DistributionMethod distributionMethod)
        {
            // NOTE: this creates a new DocDistMsg from the sourceMessage that only contains transactions of the specified DistType

            // build a dictionary of the transactions keyed by TxnId
            var sourceRecords = sourceMessage.Transactions.ToDictionary(r => r.TxnId, r => r);

            // this will create a clone, without having to maintain any new model changes
            var distMsg = JsonConvert.DeserializeObject<DocDistMsg>(JsonConvert.SerializeObject(sourceMessage));

            // clear transactions from new message
            distMsg.Transactions.Clear();

            // get all transaction records from the database for this DistType
            var dbRecords = await _distributionRepository.GetPaymentsByDistributionMethod(sourceMessage.PaymentStatusFileId, sourceMessage.IsCustomSendBackTpa, distributionMethod);

            // for each record from database
            foreach (var dbRecord in dbRecords)
            {
                // if the record exists in the dictionary of transactions from the source message
                if (sourceRecords.TryGetValue(dbRecord.TransactionId, out var sourceRecord))
                {
                    using (_logger.BeginScope(dbRecord.ToDictionary()))
                    {
                        // set the dist type from the db record
                        sourceRecord.DistType = dbRecord.FinalDistType ?? dbRecord.OrigDistType;

                        // add transaction to new dist message
                        distMsg.Transactions.Add(sourceRecord);

                        _logger.LogInformation("Add {TransactionId} to {DistributionMethod} Distribution Message", sourceRecord.TxnId, distributionMethod.ToString());
                    }
                }
                else
                {
                    // otherwise, throw exception
                    throw new Exception($"Record not found (TXID: {dbRecord.TransactionId}");
                }
            }

            // set batch size for new dist message
            distMsg.BatchSize = distMsg.Transactions.Count;

            // return new dist message
            return distMsg;
        }
    }
}
