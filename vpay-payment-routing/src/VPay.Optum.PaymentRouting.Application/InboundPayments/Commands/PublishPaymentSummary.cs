using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.Serialization;

namespace VPay.Optum.PaymentRouting.Application.InboundPayments.Commands
{
    public class PublishPaymentSummary
    {
        public class Command : IRequest<Unit>
        {
            public PaymentSummary PaymentSummary { get; set; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IPublishEndpoint _publishEndpoint;

            public Handler(ILogger<Handler> logger, IPublishEndpoint publishEndpoint)
            {
                _logger = logger;
                _publishEndpoint = publishEndpoint;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Received request to publish summary to rabbit");
                await _publishEndpoint.Publish(request.PaymentSummary, cancellationToken);
                return Unit.Value;
            }
        }
    }
}
