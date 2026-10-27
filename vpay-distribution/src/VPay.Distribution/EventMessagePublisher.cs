using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Helpers;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.Queuing;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.Distribution
{
    public class EventMessagePublisher : IEventMessagePublisher
    {
        private readonly IDistributionRepository _distributionRepository;
        private readonly IMessagePublisherService _messagePublisherService;
        private readonly EventDistributionQueueConfigurationSettings _eventDistributionQueueConfigurationSettings;

        private readonly ILogger<EventMessagePublisher> _logger;

        public EventMessagePublisher(
            IDistributionRepository distributionRepository,
            IMessagePublisherService messagePublisherService,
            EventDistributionQueueConfigurationSettings eventDistributionQueueConfigurationSettings,
            ILogger<EventMessagePublisher> logger)
        {
            ThrowIf.IsNull(distributionRepository, nameof(distributionRepository));
            ThrowIf.IsNull(messagePublisherService, nameof(messagePublisherService));
            ThrowIf.IsNull(eventDistributionQueueConfigurationSettings, nameof(eventDistributionQueueConfigurationSettings));
            ThrowIf.IsNull(logger, nameof(logger));

            _distributionRepository = distributionRepository;
            _messagePublisherService = messagePublisherService;
            _eventDistributionQueueConfigurationSettings = eventDistributionQueueConfigurationSettings;
            _logger = logger;
        }

        public async Task<EventPublishResponse> ValidateAndPublishEventsForTransactionIds(DocDistMsg docDistMsg, CancellationToken cancellationToken)
        {
            ThrowIf.IsNull(docDistMsg, nameof(docDistMsg));
            ThrowIf.IsNullOrEmpty(docDistMsg.TPA, nameof(docDistMsg.TPA));
            ThrowIf.IsNullOrEmpty(docDistMsg.BillingEntity, nameof(docDistMsg.BillingEntity));
            ThrowIf.IsNullOrEmpty(docDistMsg.Origin, nameof(docDistMsg.Origin));

            var response = new EventPublishResponse();

            using (_logger.BeginScope(new
            {
                StatusFileId = docDistMsg.PaymentStatusFileId.ToString().Replace(Environment.NewLine, ""),
                OriginCode = docDistMsg.Origin.Replace(Environment.NewLine, ""),
                BillingEntityCode = docDistMsg.BillingEntity.Replace(Environment.NewLine, ""),
                ClientCode = docDistMsg.TPA.Replace(Environment.NewLine, ""),
                NextWarningTime = docDistMsg.WarningTime.ToString().Replace(Environment.NewLine, ""),
                Transactions = docDistMsg.Transactions?.Count.ToString().Replace(Environment.NewLine, ""),
                IsRedCard = docDistMsg.IsRedCard.ToString().Replace(Environment.NewLine, "")
            }.ToDictionary()))
            {
                if (docDistMsg.Transactions?.Count == 0)
                {
                    _logger.LogInformation("No transactions to distribute events");

                    return response;
                }

                var statuses = (await _distributionRepository.GetPayments(docDistMsg.Transactions
                    .Select(_ => _.TxnId)))
                    .ToDictionary(k => k.TransactionId, v => v);

                foreach (var t in docDistMsg.Transactions)
                {
                    using (_logger.CreateLogScope(t))
                    {
                        statuses.TryGetValue(t.TxnId, out DocumentTransactionStatus status);

                        // not in the db results
                        if (status is null)
                        {
                            _logger.LogWarning("No Distribution Status found for {TransactionId}, skipped", t.TxnId);

                            response.TransactionsWithoutDistributionStatus.Add(t.TxnId);
                        }
                        // not distributed
                        else if (status.FinalDistType is null)
                        {
                            _logger.LogWarning("{TransactionId} has not been distributed, skipped", t.TxnId);

                            response.TransactionsNotDistributed.Add(t.TxnId);
                        }
                        // can publish events
                        else
                        {
                            _logger.LogInformation("{TransactionId} queued for event distributed", t.TxnId);

                            response.TransactionsWithEventsPublished.Add(t.TxnId);
                        }
                    }
                }

                QueueMessages(docDistMsg, response.TransactionsWithEventsPublished);

                return response;
            }
        }

        public void PublishEvents(DocDistMsg docDistMsg)
        {
            ThrowIf.IsNull(docDistMsg, nameof(docDistMsg));
            ThrowIf.IsNullOrEmpty(docDistMsg.TPA, nameof(docDistMsg.TPA));
            ThrowIf.IsNullOrEmpty(docDistMsg.BillingEntity, nameof(docDistMsg.BillingEntity));
            ThrowIf.IsNullOrEmpty(docDistMsg.Origin, nameof(docDistMsg.Origin));

            using (_logger.BeginScope(new
            {
                StatusFileId = docDistMsg.PaymentStatusFileId.ToString().Replace(Environment.NewLine, ""),
                OriginCode = docDistMsg.Origin.Replace(Environment.NewLine, ""),
                BillingEntityCode = docDistMsg.BillingEntity.Replace(Environment.NewLine, ""),
                ClientCode = docDistMsg.TPA.Replace(Environment.NewLine, ""),
                NextWarningTime = docDistMsg.WarningTime.ToString().Replace(Environment.NewLine, ""),
                Transactions = docDistMsg.Transactions?.Count.ToString().Replace(Environment.NewLine, ""),
                IsRedCard = docDistMsg.IsRedCard.ToString().Replace(Environment.NewLine, "")
            }.ToDictionary()))
            {
                var txns = (docDistMsg.Transactions ?? Enumerable.Empty<DistTxnData>())
                    .Select(_ => _.TxnId);

                QueueMessages(docDistMsg, new HashSet<long>(txns));
            }
        }

        private void QueueMessages(DocDistMsg request, HashSet<long> transactions)
        {
            foreach (var t in transactions)
            {
                var txn = request.Transactions?.First(_ => _.TxnId == t);

                using (_logger.CreateLogScope(txn))
                {
                    var payload = new EventPublishQueueMessage
                    {
                        BillingEntityCode = request.BillingEntity,
                        ClientCode = request.TPA,
                        OriginCode = request.Origin,
                        TransactionId = txn.TxnId,
                        ClientPaymentId = txn.ClientPaymentId,
                    };

                    _messagePublisherService.PublishMessage(_eventDistributionQueueConfigurationSettings, JsonConvert.SerializeObject(payload));

                    _logger.LogInformation("Queue {Type} for {TransactionId}", nameof(EventPublishQueueMessage), txn.TxnId);
                }
            }
        }
    }
}
