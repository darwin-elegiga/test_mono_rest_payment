using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;

namespace VPay.Optum.PaymentRouting.Application.Testing.Commands
{
    public class PublishRabbitMessageTest
    {
        public class Command : IRequest
        {
            public string OptumBatchId { get; set; } = default!;
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly IPublishEndpoint _endpoint;

            public Handler(IPublishEndpoint endpoint)
            {
                _endpoint = endpoint;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                //Use the IPUblishEndpoint from Vpay.MassTransit to publish messages to rabbit exchange
                await _endpoint.Publish(request, cancellationToken);
                return Unit.Value;
            }
        }
    }
}
