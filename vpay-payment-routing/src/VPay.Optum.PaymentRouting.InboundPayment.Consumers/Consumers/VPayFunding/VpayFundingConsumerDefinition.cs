using System;
using System.Diagnostics.CodeAnalysis;
using GreenPipes;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using VPay.Optum.PaymentRouting.Common.Configuration;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.VPayFunding
{
    [ExcludeFromCodeCoverage]
    public class VpayFundingConsumerDefinition : ConsumerDefinition<VpayFundingConsumer>
    {
        public VpayFundingConsumerDefinition(OptumQueueNameSettings settings)
        {
            EndpointName = settings.Funding;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<VpayFundingConsumer> consumerConfigurator)
        {
            if (endpointConfigurator is IServiceBusReceiveEndpointConfigurator azure)
            {
                azure.ConfigureConsumeTopology = false;
                azure.PublishFaults = false;
            }

            endpointConfigurator.UseMessageRetry(r =>
            {
                r.Incremental(5, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
                r.Handle<InvalidOperationException>(x => x.Message.Contains("The connection is closed.") || x.Message.Contains("The connection does not support MultipleActiveResultSets."));
            });
            // These settings prevent the consumer from concurrently processing the same message more than once.
            // This consumer logic is not idempotent, concurrently processing the same message twice may not be handled correctly.
            // To increase the throughput scale up the number of pods by setting a higher number of replicas.
            endpointConfigurator.PrefetchCount = 1;
            endpointConfigurator.ConcurrentMessageLimit = 1;
            base.ConfigureConsumer(endpointConfigurator, consumerConfigurator);
        }
    }
}
