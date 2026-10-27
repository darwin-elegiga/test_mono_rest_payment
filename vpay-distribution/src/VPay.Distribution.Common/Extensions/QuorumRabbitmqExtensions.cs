using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VPay.Distribution.Common.Rabbitmq;
using VPay.Queuing;

namespace VPay.Distribution.Common.Extensions
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
            return services;
        }
    }
}
