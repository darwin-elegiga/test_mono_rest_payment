using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Rabbitmq;
using VPay.DocSys.Common;
using VPay.Queuing;

namespace VPay.Distribution.RedCardQueueProcessor
{
    public class MessageProcessor
    {
        private readonly IServiceProvider _services;
        private IMessageReceiver _queueReceiver;

        public MessageProcessor(IServiceProvider services) => _services = services;

        public void Start(QueueConfigurationSettings settings, ILogger queueLogger)
        {
            using (queueLogger.BeginScope(new Dictionary<string, object>()
            {
                { "QueueName", settings.QueueName },
                { "ErrorQueueName", settings.ErrorQueueName },
                { "Exchange", settings.Exchange },
                { "Host", settings.Host },
                { "VirtualHost", settings.VirtualHost },
                { "UserName", settings.UserName },
                { "Port", settings.Port },
                { "MaxPriority", settings.MaxPriority },
            }))
            {
                queueLogger.LogInformation($"{this.GetType().FullName} starting.");
            }

            _queueReceiver = QuorumReceiverFactory.GetReceiver(settings, (string s) => ProcessMessage(s, CancellationToken.None), queueLogger);

            _queueReceiver.Consume();
        }

        public void Stop()
        {
            _queueReceiver.Stop();
        }

        private async Task<bool> ProcessMessage(string message, CancellationToken cancellationToken)
        {
            using (var scope = _services.CreateScope())
            {
                var scopedProcessingService =
                    scope.ServiceProvider
                        .GetRequiredService<IDistributionRedCardQueueMessageProcessor>();

                await scopedProcessingService.ProcessMessage(JsonConvert.DeserializeObject<DocDistMsg>(message), cancellationToken);
            }

            return true;
        }
    }
}
