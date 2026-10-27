using System;
using System.Diagnostics.CodeAnalysis;
using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using Microsoft.Azure.ServiceBus;
using Microsoft.Data.SqlClient;
using VPay.MassTransit.RabbitMqTransport;

namespace VPay.Optum.PaymentRouting.ApProcessor.Consumers
{
    [ExcludeFromCodeCoverage]
    public class ISeriesApReapMessageConsumerDefinition : ISeriesQuorumConsumerDefinition<ISeriesApReapMessageConsumer>
    {
        public ISeriesApReapMessageConsumerDefinition()
        {
            EndpointName = "ApReapMessage";
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<ISeriesApReapMessageConsumer> consumerConfigurator)
        {

            endpointConfigurator.UseMessageRetry(r =>
            {
                r.Exponential(5, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(90), TimeSpan.FromSeconds(5));
                r.Handle<SqlException>(x => x.IsTransient);
                r.Handle<ServiceBusException>(x => x.IsTransient);
                r.Handle<InvalidOperationException>(x => x.Message.Contains("The connection is closed.") || x.Message.Contains("The connection does not support MultipleActiveResultSets."));
            });

            // These settings prevent the consumer from concurrently processing the same message more than once.
            // This consumer logic is not idempotent, concurrently processing the same message twice may not be handled correctly.
            // To increase the throughput scale up the number of pods by setting a higher number of replicas.
            endpointConfigurator.PrefetchCount = 1;
            endpointConfigurator.ConcurrentMessageLimit = 1;

            base.ConfigureConsumer(endpointConfigurator, consumerConfigurator);

            //do after the base configure so we can handle the serialization correctly
            endpointConfigurator.UseJsonSerializer();
        }
    }
}
