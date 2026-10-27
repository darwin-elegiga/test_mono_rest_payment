using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader;

namespace VPay.Optum.PaymentRouting.Application.InboundPayments.Queries
{
    public static class GetHeaderByPaymentId
    {
        public record Query(long InboundPaymentId) : IRequest<Data.Sql.InboundHeader.InboundHeader?>;

        public class Handler : IRequestHandler<Query, Data.Sql.InboundHeader.InboundHeader?>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;

            public Handler(ILogger<Handler> logger, IMediator mediator)
            {
                _logger = logger;
                _mediator = mediator;
            }

            public async Task<Data.Sql.InboundHeader.InboundHeader?> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _mediator.Send(new GetInboundHeaderByPaymentIdSql.Query(request.InboundPaymentId), cancellationToken);
            }
        }
    }
}
