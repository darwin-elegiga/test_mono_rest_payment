using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.Commands;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.OutboundFinSegFiles;

/// <summary>
/// When the /api/OutboundFinSeg/start endpoint is called, a message per PayerAlias/OutboundRefEo is created
/// that will break down all the payments to create the OutboundPaymentClob 835 records in the db.  When all
/// of those are completed, will pull them all together into a large file with the appropriate headers to be
/// sent to ECG/Optum.
/// </summary>
public class OutboundFinSegFileConsumer : IConsumer<OutboundFinSegFileMessage>
{
    private readonly ILogger<OutboundFinSegFileConsumer> _logger;
    private readonly IMediator _mediator;

    public OutboundFinSegFileConsumer(ILogger<OutboundFinSegFileConsumer> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<OutboundFinSegFileMessage> context)
    {
        _logger.LogInformation("Received message to create OutboundFinSeg file.");
        await _mediator.Send(new CreateOutboundFinSeg835File.Command(context.Message.OutboundFinSeg835FileId, context.Message.OutboundFinSegTxtFileId),
            context.CancellationToken).ConfigureAwait(false);
    }
}
