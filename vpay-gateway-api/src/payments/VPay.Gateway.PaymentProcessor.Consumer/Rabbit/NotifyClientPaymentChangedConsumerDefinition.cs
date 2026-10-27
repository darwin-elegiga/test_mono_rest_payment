using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using Microsoft.Data.SqlClient;
using VPay.MassTransit.RabbitMqTransport;

namespace VPay.Gateway.PaymentProcessor.Consumer.Rabbit;

public class NotifyClientPaymentChangedConsumerDefinition : QuorumConsumerDefinition<NotifyClientPaymentChangedConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<NotifyClientPaymentChangedConsumer> consumerConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Incremental(5, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
            r.Handle<SqlException>();
            r.Handle<InvalidOperationException>(x => x.Message.Contains("The connection is closed.") || x.Message.Contains("The connection does not support MultipleActiveResultSets."));
        });

        base.ConfigureConsumer(endpointConfigurator, consumerConfigurator);
    }
}
