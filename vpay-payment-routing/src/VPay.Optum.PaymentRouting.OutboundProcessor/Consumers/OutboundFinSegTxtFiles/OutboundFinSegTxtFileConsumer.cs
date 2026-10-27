using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.Commands;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.OutboundFinSegTxtFiles;

/// <summary>
/// After OutboundFinSegFile has finished, this will create the summary to send
/// via ECG 
/// </summary>
public class OutboundFinSegTxtFileConsumer : IConsumer<OutboundFinSegTxtFileMessage>
{
    private readonly ILogger<OutboundFinSegTxtFileConsumer> _logger;
    private readonly IMediator _mediator;

    public OutboundFinSegTxtFileConsumer(ILogger<OutboundFinSegTxtFileConsumer> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<OutboundFinSegTxtFileMessage> context)
    {
        _logger.LogInformation("Received message to create OutboundFinSeg file.");
        // TODO: JAH-2024-04-12 -002- This is the entry point where we need to separate this into two separate consumers
        await _mediator.Send(new CreateOutboundFinSegTxtFile.Command(context.Message.OutboundFinSegTxtFileId),
            context.CancellationToken).ConfigureAwait(false);
    }
}

