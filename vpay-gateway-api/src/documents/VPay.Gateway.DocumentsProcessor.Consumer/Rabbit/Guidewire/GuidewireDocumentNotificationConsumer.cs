using MassTransit;
using MediatR;
using VPay.Gateway.Common;
using VPay.Gateway.DocumentsProcessor.Abstractions;
using VPay.Gateway.DocumentsProcessor.Consumer.Logic;

namespace VPay.Gateway.DocumentsProcessor.Consumer.Rabbit.Guidewire;

public class GuidewireDocumentNotificationConsumer : IConsumer<GuidewireDocumentNotification>
{
    private readonly ILogger<GuidewireDocumentNotificationConsumer> _logger;
    private readonly IMediator _mediator;

    public GuidewireDocumentNotificationConsumer(ILogger<GuidewireDocumentNotificationConsumer> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<GuidewireDocumentNotification> context)
    {
        var start = StopwatchHelpers.GetTimestamp();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = context.MessageId,
            ["ArchiveDetailId"] = context.Message.DocumentId,
            ["TransactionId"] = context.Message.TransactionId
        });

        try
        {
            await _mediator.Send(new ProcessGuidewireDocumentNotificationCommand(context.Message.TransactionId, context.Message.DocumentId), context.CancellationToken).ConfigureAwait(false);

            _logger.LogInformation("GuidewireDocumentNotification {Status} in {Elapsed:0.0000} ms", "SUCCESS", start.GetElapsedMilliseconds());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GuidewireDocumentNotification {Status} in {Elapsed:0.0000} ms", "FAILED", start.GetElapsedMilliseconds());
            throw;
        }
    }
}
