using MediatR;
using VPay.EventBus.Messages.DocSys;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Sql.DocumentNotifications;
using VPay.Gateway.DocumentsProcessor.Abstractions;

namespace VPay.Gateway.DocumentsProcessor.Consumer.Logic;

public sealed record ProcessGuidewireDocumentNotificationCommand(
    long TransactionId,
    long ArchiveDetailId
) : IRequest;

public class ProcessGuidewireDocumentNotificationCommandHandler : IRequestHandler<ProcessGuidewireDocumentNotificationCommand>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessGuidewireDocumentNotificationCommandHandler> _logger;

    public ProcessGuidewireDocumentNotificationCommandHandler(IMediator mediator, ILogger<ProcessGuidewireDocumentNotificationCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(ProcessGuidewireDocumentNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _mediator.Send(new DocumentNotificationByDocumentQuery(request.TransactionId, request.ArchiveDetailId), cancellationToken).ConfigureAwait(false);
        if (notification == null)
        {
            _logger.LogWarning("Could not find Document Notification record for TransactionId = {TransactionId}, ArchiveDetailId = {ArchiveDetailId}", request.TransactionId, request.ArchiveDetailId);
            return;
        }

        switch (notification.CurrentStatus)
        {
            case var e when e.Equals(DocumentNotificationStatus.Completed.Name):
            case var e2 when e2.Equals(DocumentNotificationStatus.Skipped.Name):
            case var e3 when e3.Equals(DocumentNotificationStatus.MarkedCompleted.Name):
                _logger.LogWarning("Document Notification is already in a completed status for TransactionId = {TransactionId}, ArchiveDetailId = {ArchiveDetailId}", request.TransactionId, request.ArchiveDetailId);
                return;
        }

        notification = await _mediator.Send(new DocumentNotificationUpdateCommand(request.TransactionId, request.ArchiveDetailId, DocumentNotificationStatus.Started.ToString(), notification.CurrentStatus), cancellationToken).ConfigureAwait(false);

        var externalClientConfiguration = await _mediator.Send(new ExternalClientConfigurationQuery(notification.ClientCode, notification.CreatedDateTime.Date), cancellationToken).ConfigureAwait(false);
        if (externalClientConfiguration == null)
        {
            // no LogError for the client to send to guidewire.  Just ignore the message
            _logger.LogInformation("Received TransactionId = {TransactionId}.  Client '{ClientCode}' is not configured to send to Guidewire", request.TransactionId, notification);
            await _mediator.Send(new DocumentNotificationUpdateCommand(request.TransactionId, request.ArchiveDetailId, DocumentNotificationStatus.Failed.ToString(), notification.CurrentStatus), cancellationToken).ConfigureAwait(false);
            return;
        }

        string documentName = notification.DocumentDescription.Trim() switch
        {
            "EFT" => "ACH Payment",
            "EFT + EOB" => "ACH Payment",
            "Masked-PAN" => "Virtual Card Payment",
            "Masked-PAN + EOB" => "Virtual Card Payment",
            "NPF" => "Non-payment Notification",
            "NPF + EOB" => "Non-payment Notification",
            "Masked Check" => "Check Payment",
            "Masked Check + EOB" => "Check Payment",
            _ => ""
        };

        if (string.Equals(notification.DocumentType, nameof(FileDetailType.ClearedCheck), System.StringComparison.OrdinalIgnoreCase))
        {
            documentName = "Cleared Check Image";
        }

        if (string.IsNullOrWhiteSpace(documentName))
        {
            _logger.LogInformation("Skipped sending notification due to description not mapped to name for for TransactionId = {TransactionId}, ArchiveDetailId = {ArchiveDetailId}", request.TransactionId, request.ArchiveDetailId);
            await _mediator.Send(new DocumentNotificationUpdateCommand(request.TransactionId, request.ArchiveDetailId, DocumentNotificationStatus.Skipped.ToString(), notification.CurrentStatus), cancellationToken).ConfigureAwait(false);
            return;
        }

        var settings = new GuidewireSettings
        {
            Url = externalClientConfiguration.Url,
            UserName = externalClientConfiguration.Username,
            Password = externalClientConfiguration.Password
        };

        var message = await _mediator.Send(new SendDocumentNoticeCommand(notification.PaymentId, notification.TransactionId, notification.ArchiveDetailId, documentName, "Pdf", documentName, settings), cancellationToken).ConfigureAwait(false);

        if (message.Status == "SUCCESS")
        {
            _logger.LogInformation("TransactionId = {TransactionId}, ArchiveDetailId = {ArchiveDetailId} Result for status update is: {Result} - {ResultMessage}", request.TransactionId, request.ArchiveDetailId, message.Status, message.Message);
            await _mediator.Send(new DocumentNotificationUpdateCommand(request.TransactionId, request.ArchiveDetailId, DocumentNotificationStatus.Completed.ToString(), notification.CurrentStatus), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogError("TransactionId = {TransactionId}, ArchiveDetailId = {ArchiveDetailId} Result for status update is: {Result} - {ResultMessage}", request.TransactionId, request.ArchiveDetailId, message.Status, message.Message);
            await _mediator.Send(new DocumentNotificationUpdateCommand(request.TransactionId, request.ArchiveDetailId, DocumentNotificationStatus.Failed.ToString(), notification.CurrentStatus), cancellationToken).ConfigureAwait(false);
        }
    }
}
