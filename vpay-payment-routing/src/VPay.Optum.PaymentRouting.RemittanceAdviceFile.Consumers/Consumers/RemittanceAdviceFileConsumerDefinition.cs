using System.Diagnostics.CodeAnalysis;
using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using Microsoft.Azure.ServiceBus;
using Microsoft.Data.SqlClient;
using VPay.MassTransit.RabbitMqTransport;

namespace VPay.Optum.PaymentRouting.RemittanceAdviceFile.Consumers
{
    [ExcludeFromCodeCoverage]
    public class RemittanceAdviceFileConsumerDefinition : QuorumConsumerDefinition<RemittanceAdviceFileConsumer>
    {
        public RemittanceAdviceFileConsumerDefinition()
        {
            EndpointName = "RemittanceAdviceFileMessage";
        }
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<RemittanceAdviceFileConsumer> consumerConfigurator)
        {
            endpointConfigurator.UseMessageRetry(r =>
            {
                r.Exponential(5, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(90), TimeSpan.FromSeconds(5));

                // The unique key is Payer835Id/InboundZipfile/UserKey and if we get the same userkey in the same batch (InboundZipfile),
                // it's ok to ignore it/overwrite it, since, ultimately, the pdf file can only be written once to the destination folder.
                r.Ignore<SqlException>(x =>
                    x.Number == 2627    /* Unique constraint error */
                    || x.Number == 547  /* Constraint check violation */
                    || x.Number == 2601 /* Duplicated key row error */ );
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
        }
    }
}
