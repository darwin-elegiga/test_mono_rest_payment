using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;

namespace VPay.DocSys.Parsing;

public class PaymentEngineConfirmationFileReadyForTransferNotificationHandler : MassTransitFileReadyForTransferNotificationHandlerBase
{
    public PaymentEngineConfirmationFileReadyForTransferNotificationHandler(
        IFileTransferServiceBus fileTransferServiceBus,
        ILogger<PaymentEngineConfirmationFileReadyForTransferNotificationHandler> logger)
        : base(fileTransferServiceBus, logger)
    {
    }

    protected override Task UpdateFileStatus(FileReadyForTransferNotification notification, CancellationToken cancellationToken)
    {
        var fileName = notification.AdditionalProperties!.Value<string>("trackingId");
        Logger.LogInformation("UpdateFileStatus received for {FileName} - Source: {Source} - Destination: {Destination} - SignedFileHash: {SignedFileHash}",
            fileName, notification.Source, notification.Destination, notification.SignedFileHash);
        return Task.CompletedTask;
    }
}
