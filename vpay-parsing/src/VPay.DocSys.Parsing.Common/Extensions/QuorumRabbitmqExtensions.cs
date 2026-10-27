using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VPay.DocSys.Common.RabbitMQ;
using VPay.DocSys.Parsing.Common.Rabbitmq;
using VPay.Queuing;

namespace VPay.DocSys.Parsing.Common.Extensions
{
    public static class QuorumRabbitmqExtensions
    {
        //
        // Summary:
        //     Adds the services needed for VPay Quorum Queuing into the services collection
        //
        // Parameters:
        //   services:
        public static IServiceCollection AddVPayQuorumQueuingServices(this IServiceCollection services)
        {
            services.TryAddSingleton<IMessagePublisherFactory, QuorumMessagePublisherFactory>();
            services.TryAddSingleton<IMessagePublisherCache, MessagePublisherCache>();
            services.TryAddTransient<IMessagePublisherService, MessagePublisherService>();
            services.TryAddTransient<IMessageRepositoryFactory, MessageRepositoryFactory>();
            services.TryAddTransient<IRabbitMqPublisherFactory, RabbitMqPublisherFactory>();
            return services;
        }
    }
}
