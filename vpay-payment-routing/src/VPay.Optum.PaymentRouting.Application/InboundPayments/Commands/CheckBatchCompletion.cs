using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;

namespace VPay.Optum.PaymentRouting.Application.InboundPayments.Commands
{
    public static class CheckBatchCompletion
    {
        public record Command(long InboundBatchId) : IRequest;

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly IPublishEndpoint _endpoint;
            private readonly ILogger<Handler> _logger;

            public Handler(ILogger<Handler> logger, IPublishEndpoint endpoint)
            {
                _logger = logger;
                _endpoint = endpoint;
            }

            public async Task<Unit> Handle(Command command, CancellationToken cancellationToken)
            {
                _logger.LogInformation(
                    "Received request to check completion of Batch ID: {batchId}.", command.InboundBatchId);

                await _endpoint.Publish(new CheckBatchCompletionMessage(command.InboundBatchId), cancellationToken)
                    .ConfigureAwait(false);
                return Unit.Value;
            }
        }
    }
}
