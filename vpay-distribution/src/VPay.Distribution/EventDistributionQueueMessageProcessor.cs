using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Helpers;
using VPay.DocSys.Common;
using VPay.DocSys.Storage;
using VPay.EventBus.Abstractions;
using VPay.EventBus.Messages.DocSys;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.Distribution
{
    public class EventDistributionQueueMessageProcessor : IEventDistributionQueueMessageProcessor
    {
        private readonly IStorageApiClient _storageApiClient;
        private readonly IEventBusPublisher _eventBusPublisher;
        private readonly ILogger _logger;

        public EventDistributionQueueMessageProcessor(
            IStorageApiClient storageApiClient,
            IEventBusPublisher eventBusPublisher,
            ILogger<EventDistributionQueueMessageProcessor> logger)
        {
            ThrowIf.IsNull(storageApiClient, nameof(storageApiClient));
            ThrowIf.IsNull(eventBusPublisher, nameof(eventBusPublisher));
            ThrowIf.IsNull(logger, nameof(logger));

            _storageApiClient = storageApiClient;
            _eventBusPublisher = eventBusPublisher;
            _logger = logger;
        }

        public async Task ProcessMessage(EventPublishQueueMessage message, CancellationToken cancellationToken = default)
        {
            ThrowIf.IsNull(message, nameof(message));
            ThrowIf.IsNullOrEmpty(message.ClientCode, nameof(message.ClientCode));
            ThrowIf.IsNullOrEmpty(message.BillingEntityCode, nameof(message.BillingEntityCode));
            ThrowIf.IsNullOrEmpty(message.OriginCode, nameof(message.OriginCode));
            ThrowIf.IsNullOrEmpty(message.ClientPaymentId, nameof(message.ClientPaymentId));

            using (_logger.CreateLogScope(message))
            {
                var archiveFiles = await GetFileInformationDictionary(message.TransactionId, cancellationToken);

                if (archiveFiles.Count == 0)
                {
                    _logger.LogError("No Archives found for {TransactionId} ", message.TransactionId);

                    return;
                }

                var transactionEvents = CreateEvents(message, archiveFiles).ToList();

                foreach (var te in transactionEvents)
                {
                    var type = te.GetType();

                    await _eventBusPublisher.Publish(te, type, cancellationToken);

                    _logger.LogInformation("Published {EventType} event for {TransactionId}", type.ToString(), te.TransactionId);
                }
            }
        }

        private async Task<Dictionary<DocumentSystemType, List<FileInformation>>> GetFileInformationDictionary(long transactionId, CancellationToken cancellationToken)
        {
            var archives = await _storageApiClient.GetArchivesForTransactionId(transactionId, cancellationToken);

            return archives
                .GroupBy(k => k.DocumentSystemType, v => v)
                .ToDictionary(k => k.Key, v => v.SelectMany(_ => _.ArchiveDetails)
                    .MapToFileInformation()
                    .ToList());
        }

        private IEnumerable<DocumentMessageBase> CreateEvents(EventPublishQueueMessage eventPublishMessage, Dictionary<DocumentSystemType, List<FileInformation>> documents)
        {
            foreach (var (key, details) in documents)
            {
                if (details.Count > 0)
                {
                    switch (key)
                    {
                        case DocumentSystemType.Generic:
                            yield return new DistributionArchiveGenerationComplete
                            {
                                BillingEntityCode = eventPublishMessage.BillingEntityCode,
                                ClientCode = eventPublishMessage.ClientCode,
                                OriginCode = eventPublishMessage.OriginCode,
                                TransactionId = eventPublishMessage.TransactionId,
                                ClientPaymentId = eventPublishMessage.ClientPaymentId,
                                Files = details
                            };
                            break;

                        case DocumentSystemType.Transaction:
                            yield return new DocumentsAddedToTransactionAfterDistributionPacket
                            {
                                BillingEntityCode = eventPublishMessage.BillingEntityCode,
                                ClientCode = eventPublishMessage.ClientCode,
                                OriginCode = eventPublishMessage.OriginCode,
                                TransactionId = eventPublishMessage.TransactionId,
                                ClientPaymentId = eventPublishMessage.ClientPaymentId,
                                Files = details
                            };
                            break;

                        default:
                            _logger.LogError("Could not create EventBus Message for {DocumentSystemType} on {TransactionId}", key.ToString(), eventPublishMessage.TransactionId);
                            break;
                    }
                }
            }
        }
    }
}
