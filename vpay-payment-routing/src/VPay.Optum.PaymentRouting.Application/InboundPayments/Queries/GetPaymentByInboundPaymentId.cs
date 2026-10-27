using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;

namespace VPay.Optum.PaymentRouting.Application.InboundPayments.Queries
{
    public static class GetPaymentByInboundPaymentId
    {
        public class Query : IRequest<GetInboundPaymentWithClobByIdSql.Result?>
        {
            public long InboundPaymentId { get; set; }
        }

        public class Handler : IRequestHandler<Query, GetInboundPaymentWithClobByIdSql.Result?>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;

            public Handler(ILogger<Handler> logger, IMediator mediator)
            {
                _logger = logger;
                _mediator = mediator;
            }

            public async Task<GetInboundPaymentWithClobByIdSql.Result?> Handle(Query request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Getting InboundPayment {InboundPaymentId}", request.InboundPaymentId);

                var dbQuery = new GetInboundPaymentWithClobByIdSql.Query{InboundPaymentId = request.InboundPaymentId};
                var result = await _mediator.Send(dbQuery, cancellationToken);

                return result;
            }
        }
    }
}
