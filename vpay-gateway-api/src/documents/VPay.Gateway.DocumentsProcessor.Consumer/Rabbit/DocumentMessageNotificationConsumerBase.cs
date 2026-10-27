using MassTransit;
using MassTransit.Util;
using MediatR;
using VPay.EventBus.Abstractions;
using VPay.EventBus.Messages.DocSys;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.PlatformConfig;

namespace VPay.Gateway.DocumentsProcessor.Consumer.Rabbit;

public abstract class DocumentMessageNotificationConsumerBase<TDocumentMessageType> : IEventBusConsumer<TDocumentMessageType>
    where TDocumentMessageType : DocumentMessageBase, IEventBusMessage
{
    protected DocumentMessageNotificationConsumerBase(IMediator mediator, ILogger logger)
    {
        Mediator = mediator;
        Logger = logger;
    }

    protected IMediator Mediator { get; }

    protected ILogger Logger { get; }

    public async Task Consume(ConsumeContext<TDocumentMessageType> context)
    {
        if (context?.Message is null)
        {
            throw new NullReferenceException(nameof(context.Message));
        }

        var files = context.Message.Files?.Where(f => f != null).ToList();
        if (files == null || files.Count == 0)
        {
            Logger.LogInformation("Received TransactionId = {TransactionId} containing no files", context.Message.TransactionId);
            return;
        }

        var externalClientConfiguration = await Mediator.Send(new ExternalClientConfigurationQuery(context.Message.ClientCode, DateTime.Today), context.CancellationToken).ConfigureAwait(false);
        if (externalClientConfiguration == null)
        {
            // no configuration for the client to send to guidewire.  Just ignore the message
            Logger.LogInformation("Received TransactionId = {TransactionId}.  Client '{ClientCode}' is not configured to send to Guidewire", context.Message.TransactionId, context.Message.ClientCode);
            return;
        }

        BillingEntityDto billingEntity = null;
        string billingEntityCode = context.Message.BillingEntityCode;
        if (string.IsNullOrWhiteSpace(billingEntityCode))
        {
            var transaction = await Mediator.Send(new GetTransactionByIdQuery(context.Message.TransactionId), context.CancellationToken).ConfigureAwait(false);
            billingEntityCode = transaction?.BillingEntityCode;
        }

        if (string.IsNullOrWhiteSpace(billingEntityCode))
        {
            Logger.LogInformation("Received TransactionId = {TransactionId}, but could not determine Billing Entity", context.Message.TransactionId);
        }
        else
        {
            billingEntity = await Mediator.Send(new BillingEntityQuery(context.Message.ClientCode, billingEntityCode!), context.CancellationToken).ConfigureAwait(false);
            if (billingEntity == null)
            {
                Logger.LogInformation("Received TransactionId = {TransactionId}.  Could not retrieve Billing Entity for ClientCode = '{ClientCode}', Billing Entity = '{BillingEntityCode}'", context.Message.TransactionId, context.Message.ClientCode, context.Message.BillingEntityCode);
            }
        }

        var filesExcludedFromNotification = (
                from file in files
                let shouldSend = file.FileDetailType switch
                {
                    FileDetailType.Pdf => true,
                    FileDetailType.ClearedCheck => true,
                    FileDetailType.Generic => true,
                    FileDetailType.FaxDocument when billingEntity.IsWarranty() => true,
                    _ => false
                }
                where !shouldSend
                select file)
            .ToList();

        // filter the files down to only the files that a notification will be sent for
        files = files.Except(filesExcludedFromNotification, (x, y) => x.ArchiveDetailId == y.ArchiveDetailId).ToList();
        foreach (var file in files)
        {
            Logger.LogInformation("Received TransactionId = {TransactionId}, ArchiveDetailId = {ArchiveDetailId}, FileName = {FileName}", context.Message.TransactionId, file.ArchiveDetailId, file.FileName);
        }

        // log the files *not* being sent
        foreach (var file in filesExcludedFromNotification)
        {
            Logger.LogInformation("File not being sent: TransactionId = {TransactionId}, ArchiveDetailId = {ArchiveDetailId}, FileName = {FileName}", context.Message.TransactionId, file.ArchiveDetailId, file.FileName);
        }

        await Notify(context, billingEntity, files).ConfigureAwait(false);
    }

    protected abstract Task Notify(ConsumeContext<TDocumentMessageType> context, BillingEntityDto billingEntity, List<FileInformation> files);
}
