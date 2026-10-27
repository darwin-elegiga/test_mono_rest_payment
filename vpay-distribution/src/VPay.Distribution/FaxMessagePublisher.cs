using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using VPay.Distribution.Common;
using VPay.DocSys.Common;
using VPay.Queuing;

namespace VPay.Distribution
{
    public class FaxMessagePublisher : IFaxMessagePublisher
    {
        private readonly TiffQueueConfigurationSettings _tiffQueueConfigurationSettings;
        private readonly IMessagePublisherService _messagePublisherService;
        private readonly ILogger<FaxMessagePublisher> _logger;

        /// <summary>
        /// This is the max priority for a message coming from the ISeries
        /// And is used to convert the <see cref="TiffQueueMessage.Priority"/>
        /// to the correct value for RabbitMQ
        /// </summary>
        private static int PmbMaxPriority => 100;

        public FaxMessagePublisher(
            TiffQueueConfigurationSettings tiffQueueConfigurationSettings,
            IMessagePublisherService messagePublisherService,
            ILogger<FaxMessagePublisher> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(tiffQueueConfigurationSettings, nameof(tiffQueueConfigurationSettings));
            ArgumentHelpers.ThrowIf.IsNull(messagePublisherService, nameof(messagePublisherService));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _tiffQueueConfigurationSettings = tiffQueueConfigurationSettings;
            _messagePublisherService = messagePublisherService;
            _logger = logger;
        }

        public void PublishToTiffQueue(string origin, string tpa, string billingEntity, DateTime boardUploadTimeStamp, params DistTxnData[] faxes)
        {
            using (_logger.BeginScope(new
            {
                Origin = origin,
                OriginCode = origin,
                TPA = tpa,
                ClientCode = tpa,
                BillingEntity = billingEntity,
                BillingEntityCode = billingEntity,
                BE = billingEntity
            }.ToDictionary()))
            {
                ArgumentHelpers.ThrowIf.IsNullOrEmpty(origin, nameof(origin));
                ArgumentHelpers.ThrowIf.IsNullOrEmpty(tpa, nameof(tpa));
                ArgumentHelpers.ThrowIf.IsNullOrEmpty(billingEntity, nameof(billingEntity));
                ArgumentHelpers.ThrowIf.IsNull(faxes, nameof(faxes));

                var invalidData = faxes
                    .Where(_ => !_.CorrespondenceId.HasValue)
                    .Select(_ => _.TxnId)
                    .ToArray();

                if (invalidData.Any())
                {
                    var ex = new ArgumentException($"Cannot publish to {_tiffQueueConfigurationSettings.QueueName} without a CorrespondenceId");

                    _logger.LogError(ex, "{TransactionIds}", string.Join(",", invalidData));

                    throw ex;
                }

                foreach (var transaction in faxes)
                {
                    var message = GenerateMessage(transaction, origin, tpa, billingEntity, boardUploadTimeStamp);

                    PublishMessage(message);
                }
            }
        }

        private TiffQueueMessage GenerateMessage(DistTxnData data, string origin, string tpa, string billingEntity, DateTime boardUploadTimeStamp)
        {
            return new TiffQueueMessage
            {
                BillingEntity = billingEntity,
                // ReSharper disable PossibleInvalidOperationException
                CorrespondenceId = data.CorrespondenceId.Value,
                // ReSharper restore PossibleInvalidOperationException
                FaxNumber = data.FaxNumber,
                Origin = origin,
                PayType = data.PayType,
                PrintVendor = data.PrintVendor,
                Priority = (byte)data.Priority,
                Tpa = tpa,
                TransactionIds = new List<long> { data.TxnId },
                BoardUploadTimeStamp = boardUploadTimeStamp,
                ReserveName = FaxReserveName.MEDICAL
            };
        }

        private void PublishMessage(TiffQueueMessage message)
        {
            var logDictionary = message.ToDictionary();
            logDictionary[nameof(message.TransactionIds)] = string.Join(", ", message.TransactionIds);

            using (_logger.BeginScope(logDictionary))
            {
                var rmqPriority = message.Priority.ConvertAndScalePriority(PmbMaxPriority, _tiffQueueConfigurationSettings.MaxPriority);

                _messagePublisherService.PublishMessage(_tiffQueueConfigurationSettings, JsonConvert.SerializeObject(message), rmqPriority);

                foreach (var txid in message.TransactionIds)
                {
                    _logger.LogInformation("Published message for {TransactionId} to {QueueName} with {RMQPriority}",
                        txid,
                        _tiffQueueConfigurationSettings.QueueName,
                        rmqPriority);
                }
            }
        }
    }
}
