using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Common.Rabbitmq;
using VPay.Queuing;

namespace VPay.DocSys.Generation
{
    public class QueueService : IQueueService
    {
        private readonly ILogger<QueueService> _logger;

        public QueueService(
            ILogger<QueueService> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _logger = logger;
        }

        public void PublishQueueMessage(
            QueueConfigurationSettings queueConfigurationSettings,
            string message,
            byte? messagePriority)
        {
            ArgumentHelpers.ThrowIf.IsNull(queueConfigurationSettings, nameof(queueConfigurationSettings));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(message, nameof(message));

            using (var publisher = QuorumPublisherFactory.GetPublisher(queueConfigurationSettings, _logger))
            {
                _logger.LogInformation("Publish message to {QueueName}", queueConfigurationSettings.QueueName);
                publisher.Publish(message, messagePriority);
            }
        }

        public QueueConfigurationSettings GetErrorQueueConfiguration(QueueConfigurationSettings queueConfigurationSettings)
        {
            ArgumentHelpers.ThrowIf.IsNull(queueConfigurationSettings, nameof(queueConfigurationSettings));

            var errorQueueName = queueConfigurationSettings.ErrorQueueName;

            if(string.IsNullOrWhiteSpace(errorQueueName))
            {
                errorQueueName = $"{queueConfigurationSettings.QueueName}Error";
            }

            return new QueueConfigurationSettings
            {
                // only change the queue name, since we want to error out to the same error queue
                QueueName = errorQueueName,

                ErrorQueueName = errorQueueName,
                Password = queueConfigurationSettings.Password,
                Application = queueConfigurationSettings.Application,
                Exchange = queueConfigurationSettings.Exchange,
                ExchangeType = queueConfigurationSettings.ExchangeType,
                Exclusive = queueConfigurationSettings.Exclusive,
                Host = queueConfigurationSettings.Host,
                Port = queueConfigurationSettings.Port,
                UserName = queueConfigurationSettings.UserName,
                VirtualHost = queueConfigurationSettings.VirtualHost
            };
        }
    }
}
