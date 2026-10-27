using System.Net.Mime;
using System.Text;
using MassTransit;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.EmailMessage.QueueProcessor.Commands;
using VPay.DocSys.Generation.EmailMessage.QueueProcessor.Events;

namespace VPay.DocSys.Generation.EmailMessage.QueueProcessor.Consumers;

public class CreateWarrantyEVPaygeConsumer : IConsumer<CreateWarrantyEVPayge>
{
    private readonly IEmailMessageService _emailMessageService;
    private readonly ILogger<CreateWarrantyEVPaygeConsumer> _logger;

    public CreateWarrantyEVPaygeConsumer(IEmailMessageService emailMessageService, ILogger<CreateWarrantyEVPaygeConsumer> logger)
    {
        _emailMessageService = emailMessageService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateWarrantyEVPayge> context)
    {
        var emailMessageXmlString = Encoding.UTF8.GetString(Convert.FromBase64String(context.Message.VPaygeXmlBase64));

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TransactionId"] = context.Message.VPTransId
        }))
        {
            try
            {
                _logger.LogInformation("Creating warranty evpayge pdf");

                var archiveFile = await _emailMessageService.CreateArchiveForWarrantyXmlString(emailMessageXmlString);

                await context.Publish<WarrantyEVPaygeCreated>(new WarrantyEVPaygeCreated
                {
                    Id = context.Message.Id,
                    Tpa = context.Message.Tpa,
                    VPTransId = context.Message.VPTransId,
                    EmailAddress = context.Message.EmailAddress,
                    VPaygeXmlBase64 = context.Message.VPaygeXmlBase64,
                    VPayge = archiveFile.ArchiveDetails.First().FileName
                },
                x =>
                {
                    x.SetRoutingKey("warrantyevpaygecreated");
                });

                _logger.LogInformation("Warranty evpayge created");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create the warranty evpayge pdf");

                await context.Publish<CreateWarrantyEVPaygeRejected>(new CreateWarrantyEVPaygeRejected
                {
                    Id = context.Message.Id,
                    Tpa = context.Message.Tpa,
                    VPTransId = context.Message.VPTransId,
                    EmailAddress = context.Message.EmailAddress
                },
                x =>
                {
                    x.SetRoutingKey("createwarrantyevpaygerejected");
                });
            }
        }
    }
}

public class CreateWarrantyEVPaygeConsumerDefinition : ConsumerDefinition<CreateWarrantyEVPaygeConsumer>
{
    public CreateWarrantyEVPaygeConsumerDefinition()
    {
        EndpointName = "create-warranty-evpayge";
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CreateWarrantyEVPaygeConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 1;
        consumerConfigurator.UseConcurrentMessageLimit(1);

        endpointConfigurator.ConfigureConsumeTopology = false;

        endpointConfigurator.DefaultContentType = new ContentType("application/json");
        endpointConfigurator.UseRawJsonDeserializer();
        endpointConfigurator.UseRawJsonSerializer();

        if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rmq)
        {
            rmq.Bind("create_warranty_e_v_payge", x =>
            {
                x.RoutingKey = "createwarrantyevpayge";
                x.ExchangeType = ExchangeType.Topic;
            });
        }
    }
}
