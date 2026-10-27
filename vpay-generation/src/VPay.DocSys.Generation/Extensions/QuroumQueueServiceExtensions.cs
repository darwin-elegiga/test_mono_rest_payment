using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VPay.DocSys.Common.RabbitMQ;
using VPay.DocSys.Generation.Common.Rabbitmq;
using VPay.Queuing;
using VPay.Queuing.Abstractions;

namespace VPay.DocSys.Generation.Extensions;

public static class QuroumQueueServiceExtensions
{
    //
    // Summary:
    //     Adds the services needed for VPay Queuing into the services collection
    //
    // Parameters:
    //   services:
    public static IServiceCollection AddVPayQuorumQueuingServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IMessagePublisherFactory, QuorumMessagePublisherFactory>();
        services.TryAddSingleton<IMessagePublisherCache, MessagePublisherCache>();
        services.TryAddTransient<IMessagePublisherService, MessagePublisherService>();
        services.TryAddTransient<IMessageRepositoryFactory, MessageRepositoryFactory>();
        return services;
    }

    //
    // Summary:
    //     Configurations the Queue Settings for VPay Queuing
    //
    // Parameters:
    //   services:
    //
    //   configuration:
    //
    //   configurationKeyName:
    //
    // Type parameters:
    //   T:
    public static IServiceCollection ConfigureQuorumQueueSettings<T>(this IServiceCollection services, IConfiguration configuration, string configurationKeyName) where T : QueueConfigurationSettings, new()
    {
        IConfigurationSection section = configuration.GetSection(configurationKeyName);
        T val = section.Get<T>();
        if (val != null)
        {
            services.Configure<T>(configurationKeyName, section);
            services.Configure<T>(val.QueueName, section);
            services.Configure<T>(section);
            services.AddScoped((IServiceProvider cfg) => cfg.GetService<IOptions<T>>().Value);
        }

        return services;
    }

    //
    // Summary:
    //     Add an Microsoft.Extensions.Hosting.IHostedService queue consumer of type TConsumer
    //
    //
    // Parameters:
    //   services:
    //     The Microsoft.Extensions.DependencyInjection.IServiceCollection to register with.
    //
    //
    //   queueConfigurationSettings:
    //     The VPay.Queuing.QueueConfigurationSettings to use for the rabbit connection.
    //
    //
    // Type parameters:
    //   TConsumer:
    //     An VPay.Queuing.Abstractions.IQueueConsumer to register.
    //
    // Returns:
    //     The original Microsoft.Extensions.DependencyInjection.IServiceCollection.
    public static IServiceCollection AddQuorumQueueHostedService<TConsumer>(this IServiceCollection services, QueueConfigurationSettings queueConfigurationSettings) where TConsumer : class, IQueueConsumer
    {
        if (queueConfigurationSettings == null)
        {
            throw new ArgumentNullException("queueConfigurationSettings");
        }

        services.TryAddTransient<TConsumer>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, QuorumQueueHostedService<TConsumer>>((IServiceProvider provider) => ActivatorUtilities.CreateInstance<QuorumQueueHostedService<TConsumer>>(provider, new object[1] { queueConfigurationSettings })));
        return services;
    }

    //
    // Summary:
    //     Add an Microsoft.Extensions.Hosting.IHostedService queue consumer of type TConsumer
    //
    //
    // Parameters:
    //   services:
    //     The Microsoft.Extensions.DependencyInjection.IServiceCollection to register with.
    //
    //
    //   queueConfigurationSettings:
    //     The VPay.Queuing.QueueConfigurationSettings to use for the rabbit connection.
    //
    //
    //   implementationFactory:
    //     A factory to create new instances of the service implementation.
    //
    // Type parameters:
    //   TConsumer:
    //     An VPay.Queuing.Abstractions.IQueueConsumer to register.
    //
    // Returns:
    //     The original Microsoft.Extensions.DependencyInjection.IServiceCollection.
    public static IServiceCollection AddQuorumQueueHostedService<TConsumer>(this IServiceCollection services, QueueConfigurationSettings queueConfigurationSettings, Func<IServiceProvider, TConsumer> implementationFactory) where TConsumer : class, IQueueConsumer
    {
        if (queueConfigurationSettings == null)
        {
            throw new ArgumentNullException("queueConfigurationSettings");
        }

        services.TryAdd(ServiceDescriptor.Transient(implementationFactory));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, QuorumQueueHostedService<TConsumer>>((IServiceProvider provider) => ActivatorUtilities.CreateInstance<QuorumQueueHostedService<TConsumer>>(provider, new object[1] { queueConfigurationSettings })));
        return services;
    }
}
