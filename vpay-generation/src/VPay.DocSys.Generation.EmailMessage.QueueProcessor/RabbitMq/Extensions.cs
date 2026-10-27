using MassTransit;
using RabbitMQ.Client;
using VPay.DocSys.Generation.EmailMessage.QueueProcessor.Commands;
using VPay.DocSys.Generation.EmailMessage.QueueProcessor.Events;

namespace VPay.DocSys.Generation.EmailMessage.QueueProcessor.RabbitMq;

public static class Extensions
{
    public static void Host(this IRabbitMqBusFactoryConfigurator configurator, RabbitMqConfig config)
    {
        configurator.Host(config.Hostnames[0], (ushort)config.Port, config.VirtualHost, hst =>
        {
            hst.Heartbeat(config.Heartbeat);

            hst.Username(config.Username);
            hst.Password(config.Password);

            hst.UseCluster(clusterConfig =>
            {
                foreach (var node in config.Hostnames)
                {
                    clusterConfig.Node(node);
                }
            });
        });
    }

    public static void ConfigureMessageTopology(this IRabbitMqBusFactoryConfigurator configurator)
    {
        configurator.Message<CreateWarrantyEVPayge>(x => x.SetEntityName("create_warranty_e_v_payge"));

        configurator.Message<WarrantyEVPaygeCreated>(x => x.SetEntityName("warranty_e_v_payge_created"));
        configurator.Publish<WarrantyEVPaygeCreated>(x => { x.ExchangeType = ExchangeType.Topic; });

        configurator.Message<CreateWarrantyEVPaygeRejected>(x => x.SetEntityName("create_warranty_e_v_payge_rejected"));
        configurator.Publish<CreateWarrantyEVPaygeRejected>(x => { x.ExchangeType = ExchangeType.Topic; });
    }
}
