using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;

namespace VPay.Optum.PaymentRouting.Application.Testing.Queries
{
    public class InsertInboundBatch
    {
        public record Query(
           string OptumBatchId,
           string Payer835Id,
           int TotalPaymentCount,
           decimal TotalPaymentAmount) : IRequest<BatchSummary>;

        public class Handler : IRequestHandler<Query, BatchSummary>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;

            public Handler(ILogger<Handler> logger, IMediator mediator)
            {
                _logger = logger;
                _mediator = mediator;
            }

            public async Task<BatchSummary> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _mediator.Send(
                    new InsertInboundBatchSql.Query(
                        request.OptumBatchId,
                        request.Payer835Id,
                        request.TotalPaymentCount,
                        request.TotalPaymentAmount
                    ), cancellationToken);
            }
        }

    }
}
