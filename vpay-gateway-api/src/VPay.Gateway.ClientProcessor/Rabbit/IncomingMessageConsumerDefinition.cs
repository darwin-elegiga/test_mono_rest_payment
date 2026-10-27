using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using MassTransit.RabbitMqTransport;
using RabbitMQ.Client;
using VPay.MassTransit.RabbitMqTransport;

namespace VPay.Gateway.ClientProcessor.Rabbit;

public class IncomingMessageConsumerDefinition : QuorumConsumerDefinition<IncomingMessageConsumer>
{
    public IncomingMessageConsumerDefinition()
    {
        EndpointName = "IncomingMessage_Gateway";
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<IncomingMessageConsumer> consumerConfigurator)
    {
        if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rabbitMq)
        {
            // bind the exchange MassTransit will create to the existing message exchange
            rabbitMq.Bind("Vpay.Pep.Kubera.Core.Messages:IIncomingMessage", x =>
            {
                x.Durable = true;
                x.ExchangeType = ExchangeType.Fanout;
            });
        }
        base.ConfigureConsumer(endpointConfigurator, consumerConfigurator);
    }
}
