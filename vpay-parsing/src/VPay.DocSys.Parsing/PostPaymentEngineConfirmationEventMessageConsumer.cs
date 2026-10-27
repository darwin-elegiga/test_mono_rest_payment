using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;

namespace VPay.DocSys.Parsing;

public class PostPaymentEngineConfirmationEventMessageConsumer : PostTransferEventMessageConsumer
{
    public record TrackingRecord(string FileName);

    public PostPaymentEngineConfirmationEventMessageConsumer(
         IFileReadyForTransferNotificationPublisher fileReadyForTransferNotificationPublisher,
         ILogger<PostPaymentEngineConfirmationEventMessageConsumer> logger)
         : base(fileReadyForTransferNotificationPublisher, logger)
    {
    }

    protected override async Task UpdateFileTrackingStatus(PostTransferEventMessage message, object trackingRecord, TransferStatus status, CancellationToken cancellationToken)
    {
        var record =  (TrackingRecord)trackingRecord!;
        
        Logger.LogInformation("UpdateFileTrackingStatus message received for {FileName}. New Status {Status}", record.FileName, status.ToString());

        // Testing fails if this doesn't use async
        await Task.CompletedTask;
    }

    protected override async Task<bool> ShouldMessageBeProcessed(PostTransferEventMessage message, object trackingRecord, CancellationToken cancellationToken)
    {
        // Testing fails if this doesn't use async
        return await Task.FromResult(true);
    }

    protected override async Task<object> LoadFileTrackingRecord(PostTransferEventMessage message, CancellationToken cancellationToken)
    {
        var fileName = message.OriginalMessage.Parameters.Value<string>("trackingId");

        return await Task.FromResult(new TrackingRecord(fileName));
    }
}
