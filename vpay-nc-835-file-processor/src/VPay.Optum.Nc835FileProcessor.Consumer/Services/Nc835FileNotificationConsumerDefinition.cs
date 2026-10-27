using System;
using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using VPay.MassTransit.RabbitMqTransport;
using VPay.Optum.Nc835FileProcessor.Application.Models;

namespace VPay.Optum.Nc835FileProcessor.Consumer.Services
{
    public class Nc835FileNotificationConsumerDefinition : QuorumConsumerDefinition<Nc835FileNotificationConsumer>
    {
        private readonly Nc835FileSettings _settings;

        public Nc835FileNotificationConsumerDefinition(Nc835FileSettings settings)
        {
            _settings = settings;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<Nc835FileNotificationConsumer> consumerConfigurator)
        {
            endpointConfigurator.UseMessageRetry(r =>
            {
                r.Incremental(5, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
                // TODO: uncomment when adding db layer
                //r.Handle<SqlException>();
                r.Handle<InvalidOperationException>(x => x.Message.Contains("The connection is closed.") || x.Message.Contains("The connection does not support MultipleActiveResultSets."));
            });

            endpointConfigurator.PrefetchCount = _settings.PrefetchCount;

            base.ConfigureConsumer(endpointConfigurator, consumerConfigurator);
        }
    }
}
