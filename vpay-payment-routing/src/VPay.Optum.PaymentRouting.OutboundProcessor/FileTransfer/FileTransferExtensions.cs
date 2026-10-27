using System.Diagnostics.CodeAnalysis;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VPay.FileTransfer.Messaging;
using VPay.Optum.PaymentRouting.Common.Configuration;
using VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.Optum835PostTransferEvent;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.FileTransfer
{
    [ExcludeFromCodeCoverage]
    public static class FileTransferExtensions
    {
        public static IServiceCollection ConfigureFileTransferServices(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .Configure<FileTransferServiceSettings>(configuration.GetSection("FileTransferServiceSettings"))
                .AddTransient(cfg => cfg.GetService<IOptions<FileTransferServiceSettings>>()!.Value);

            // add the service used to sign the file hash before sending the file to the File Transfer Service
            services
                .AddHashingService()
                .AddSigningService(options => configuration.Bind("SigningServiceOptions", options));

            // register the handler that will consume the FileReadyForTransferNotification
            services.ConfigureFileReadyForTransferNotificationPublisher(cfg =>
            {
                cfg.AddConsumer<FileReadyForTransferNotificationHandler>();
            });

            // register the objects that will consume the post-transfer message
            services.ConfigureFileTransferServiceBus(
                configureRabbitMq =>
                {
                    configuration.Bind("FileTransferServiceRabbitQueue", configureRabbitMq);
                },
                addConsumers =>
                {
                    addConsumers.AddConsumer<Optum835PostTransferEventMessageConsumer, Optum835PostTransferEventMessageConsumerDefinition>();
                });

            return services;
        }
    }
}
