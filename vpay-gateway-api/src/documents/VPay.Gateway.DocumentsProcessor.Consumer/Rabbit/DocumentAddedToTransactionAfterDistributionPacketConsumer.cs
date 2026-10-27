using MassTransit;
using MediatR;
using VPay.EventBus.Messages.DocSys;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Sql.DocumentNotifications;
using VPay.Gateway.DocumentsProcessor.Abstractions;

namespace VPay.Gateway.DocumentsProcessor.Consumer.Rabbit;

public class DocumentsAddedToTransactionAfterDistributionPacketConsumer : DocumentMessageNotificationConsumerBase<DocumentsAddedToTransactionAfterDistributionPacket>
{
    private readonly IBus _bus;

    public DocumentsAddedToTransactionAfterDistributionPacketConsumer(
        ILogger<DocumentsAddedToTransactionAfterDistributionPacketConsumer> logger,
        IBus bus,
        IMediator mediator) : base(mediator, logger)
    {
        _bus = bus;
    }

    protected override async Task Notify(ConsumeContext<DocumentsAddedToTransactionAfterDistributionPacket> context, BillingEntityDto billingEntity, List<FileInformation> files)
    {
        var message = context.Message;
        foreach (var file in files)
        {
            var document = await Mediator.Send(new DocumentNotificationByDocumentQuery(message.TransactionId, file.ArchiveDetailId), context.CancellationToken).ConfigureAwait(false);
            if (document == null)
            {
                var insertCommand = new DocumentNotificationInsertCommand(
                    message.TransactionId,
                    file.ArchiveDetailId,
                    message.ClientCode,
                    message.BillingEntityCode,
                    billingEntity?.Type.Name,
                    DocumentNotificationStatus.Scheduled.ToString(),
                    message.ClientPaymentId,
                    file.Description,
                    file.FileDetailType.ToString(),
                    file.FileName);

                await Mediator.Send(insertCommand, context.CancellationToken).ConfigureAwait(false);
            }
        }

        await Task.WhenAll(files.Select(file => _bus.Publish(
            new GuidewireDocumentNotification
            {
                TransactionId = context.Message.TransactionId,
                DocumentId = file.ArchiveDetailId
            },
            x => x.Durable = true,
            context.CancellationToken)))
            .ConfigureAwait(false);
    }
}
