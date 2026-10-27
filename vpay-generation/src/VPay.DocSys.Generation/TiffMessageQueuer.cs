using System;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class TiffMessageQueuer : ITiffMessageQueuer
    {
        private readonly QueueConfigurationSettings _tiffQueueConfigurationSettings;
        private readonly IQueueService _queueService;
        private readonly ILogger<TiffMessageQueuer> _logger;

        public TiffMessageQueuer(
            QueueConfigurations queueConfigurations,
            IQueueService queueService,
            ILogger<TiffMessageQueuer> logger)
        {
            ThrowIf.IsChildPropertyNull(queueConfigurations.TiffQueue, nameof(queueConfigurations), nameof(queueConfigurations.TiffQueue));
            ThrowIf.IsLessThanOrEqualToZero(queueConfigurations.TiffQueue.MaxPriority, nameof(queueConfigurations.TiffQueue.MaxPriority));
            ThrowIf.IsNull(queueService, nameof(queueService));
            ThrowIf.IsNull(logger, nameof(logger));

            _queueService = queueService;
            _tiffQueueConfigurationSettings = queueConfigurations.TiffQueue;

            _logger = logger;
        }

        public void QueueTiffMessage(TiffQueueMessage message)
        {
            ThrowIf.IsNull(message, nameof(message));
            ThrowIf.IsNullOrEmpty(message.TransactionIds, nameof(message.TransactionIds));

            using (_logger.BeginScope(message.ToDictionaryExcept(nameof(TiffQueueMessage.TransactionIds))))
            {
                var messageString = JsonConvert.SerializeObject(message);
                var rmqPriority = message.Priority.ConvertAndScalePriority(100, _tiffQueueConfigurationSettings.MaxPriority);

                _queueService.PublishQueueMessage(_tiffQueueConfigurationSettings, messageString, rmqPriority);

                foreach (var txid in message.TransactionIds)
                {
                    _logger.LogInformation("Published message for {TransactionId} to {QueueName} with {RMQPriority}",
                        txid.ToString().Replace(Environment.NewLine, string.Empty),
                        _tiffQueueConfigurationSettings.QueueName,
                        rmqPriority);
                }
            }
        }
    }
}
