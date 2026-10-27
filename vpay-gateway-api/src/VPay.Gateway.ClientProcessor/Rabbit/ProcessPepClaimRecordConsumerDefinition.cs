using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using Microsoft.Data.SqlClient;
using Refit;
using VPay.MassTransit.RabbitMqTransport;

namespace VPay.Gateway.ClientProcessor.Rabbit;

public class ProcessPepClaimRecordConsumerDefinition : QuorumConsumerDefinition<ProcessPepClaimRecordConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<ProcessPepClaimRecordConsumer> consumerConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Incremental(5, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
            r.Handle<SqlException>();
            r.Handle<InvalidOperationException>(x => x.Message.Contains("The connection is closed.") || x.Message.Contains("The connection does not support MultipleActiveResultSets."));
            r.Handle<ApiException>(x => x.Message.Contains("Response status code does not indicate success: 500"));
        });

        base.ConfigureConsumer(endpointConfigurator, consumerConfigurator);
    }
}
