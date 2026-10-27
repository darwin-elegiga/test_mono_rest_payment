using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;

namespace VPay.Optum.PaymentRouting.Application.InboundPayments.Queries
{
    public static class GetPaymentByPayerIdUserKey
    {
        public class Query : IRequest<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result?>
        {
            public string PayerId { get; set; } = string.Empty;
            public string UserKey { get; set; } = string.Empty;
        }

        public class Handler : IRequestHandler<Query, GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result?>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;

            public Handler(ILogger<Handler> logger, IMediator mediator)
            {
                _logger = logger;
                _mediator = mediator;
            }

            public async Task<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result?> Handle(Query request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Getting batch {batchId} for {payerId}", request.UserKey, request.PayerId);

                var dbQuery = new GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query(request.UserKey, request.PayerId);
                var result = await _mediator.Send(dbQuery, cancellationToken);

                return result;
            }
        }
    }
}
