using MassTransit;
using VPay.EventBus.Abstractions;
using VPay.EventBus.Messages.DocSys;

namespace VPay.Gateway.DocumentsProcessor.Consumer.Rabbit;

public class DocumentsDeletedConsumer : IEventBusConsumer<DocumentsDeleted>
{
    private readonly ILogger<DocumentsDeletedConsumer> _logger;

    public DocumentsDeletedConsumer(ILogger<DocumentsDeletedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<DocumentsDeleted> context)
    {
        if (context?.Message is null) { throw new NullReferenceException(nameof(context.Message)); }

        var files = context.Message.Files?.Where(f => f != null) ?? Enumerable.Empty<FileInformation>();

        foreach (var file in files)
        {
            _logger.LogInformation("Received ArchiveDetailId = {ArchiveDetailId}, FileName = {FileName}", file.ArchiveDetailId, file.FileName);
        }

        return Task.CompletedTask;
    }
}
