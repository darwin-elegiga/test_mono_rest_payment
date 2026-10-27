using System;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using VPay.Distribution.Common.Interfaces;
using VPay.MassTransit.Abstractions;

namespace VPay.Distribution
{
    public static class PmbMessageExtensions
    {
        /// <summary>
        /// Setups mass transit in rabbit mq
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config">Configuration needed to contact rabbit mq</param>
        /// <returns></returns>
        public static IServiceCollection AddPmbMessaging(this IServiceCollection services, RabbitMqConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.AddMassTransit(x =>
            {
                x.SetEndpointNameFormatter(new VPayDefaultEndpointNameFormatter());

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Message<IPmbMessage>(c => c.SetEntityName("PMBExchange"));
                    cfg.Publish<IPmbMessage>(t => t.ExchangeType = ExchangeType.Direct);
                    cfg.Host(config);
                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }

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

    }
}
