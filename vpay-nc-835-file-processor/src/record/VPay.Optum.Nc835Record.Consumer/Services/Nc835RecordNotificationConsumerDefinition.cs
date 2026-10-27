using System;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using VPay.MassTransit.RabbitMqTransport;
using VPay.Optum.Nc835Record.Application.Models;

namespace VPay.Optum.Nc835Record.Consumer.Services
{
    public class Nc835RecordNotificationConsumerDefinition : QuorumConsumerDefinition<Nc835RecordNotificationConsumer>
    {
        private readonly Settings _settings;

        public Nc835RecordNotificationConsumerDefinition(Settings settings)
        {
            _settings = settings;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<Nc835RecordNotificationConsumer> consumerConfigurator)
        {
            var key = Convert.FromBase64String(_settings.RecordEncryptionKey);
            endpointConfigurator.UseEncryption(key);

            endpointConfigurator.PrefetchCount = _settings.PrefetchCount;
            
            base.ConfigureConsumer(endpointConfigurator, consumerConfigurator);
        }
    }
}
