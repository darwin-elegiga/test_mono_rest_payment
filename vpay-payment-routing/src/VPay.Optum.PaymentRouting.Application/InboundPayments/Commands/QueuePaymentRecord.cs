using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.Serialization;

namespace VPay.Optum.PaymentRouting.Application.InboundPayments.Commands
{
    public static class QueuePaymentRecord
    {
        public class Command : IRequest
        {
            public PaymentRecord PaymentRecord { get; set; } = new();
        }

        public class Handler : IRequestHandler<Command>
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
                    "Received request to add payment record to queue with Batch ID: {batchId} FileProcID: {fileProcId}.",
                    command.PaymentRecord.BatchId, command.PaymentRecord.FileProcId);

                await _endpoint.Publish(command.PaymentRecord, cancellationToken)
                    .ConfigureAwait(false);
                return Unit.Value;
            }
        }
    }
}
